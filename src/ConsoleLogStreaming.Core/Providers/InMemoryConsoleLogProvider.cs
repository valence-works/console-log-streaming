using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using ConsoleLogStreaming.Core.Internal;
using ConsoleLogStreaming.Core.Models;
using ConsoleLogStreaming.Core.Options;
using Microsoft.Extensions.Options;

namespace ConsoleLogStreaming.Core.Providers;

/// <summary>
/// Bounded in-memory console log provider.
/// </summary>
public sealed class InMemoryConsoleLogProvider : IConsoleLogProvider, IConsoleLogDroppedLineReporter
{
    private readonly object _gate = new();
    private readonly int _recentCapacity;
    private readonly int _subscriberCapacity;
    private readonly int _maxRecentQuerySize;
    private readonly IConsoleLogRedactionPipeline _redactionPipeline;
    private readonly IConsoleLogSourceRegistry _sourceRegistry;
    private readonly Queue<ConsoleLogLine> _recent = new();
    private readonly List<ConsoleLogDroppedSummary> _dropped = [];
    private readonly ConcurrentDictionary<Guid, Subscriber> _subscribers = new();

    /// <summary>
    /// Initializes a new instance of the provider.
    /// </summary>
    public InMemoryConsoleLogProvider(
        IOptions<ConsoleLogOptions> options,
        IConsoleLogRedactionPipeline redactionPipeline,
        IConsoleLogSourceRegistry sourceRegistry)
    {
        var value = options.Value;
        _recentCapacity = Math.Max(1, value.RecentCapacity);
        _subscriberCapacity = Math.Max(1, value.SubscriberCapacity);
        _maxRecentQuerySize = Math.Max(1, value.MaxRecentQuerySize);
        _redactionPipeline = redactionPipeline;
        _sourceRegistry = sourceRegistry;
    }

    /// <inheritdoc />
    public ValueTask PublishAsync(ConsoleLogLine line, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var receivedAt = line.ReceivedAt;
        var redacted = _redactionPipeline.Redact(line with { ReceivedAt = receivedAt });
        var source = _sourceRegistry.MarkSeen(redacted.Source, receivedAt);
        redacted = redacted with { Source = source };

        lock (_gate)
        {
            if (_recent.Count == _recentCapacity)
            {
                var dropped = _recent.Dequeue();
                AddDropped(new ConsoleLogDroppedSummary
                {
                    SourceId = dropped.Source.Id,
                    Stream = dropped.Stream,
                    Reason = "recent-buffer-overflow",
                    Count = 1,
                    From = dropped.ReceivedAt,
                    To = receivedAt
                });
            }

            _recent.Enqueue(redacted);
        }

        var item = ConsoleLogStreamingItem.FromLine(redacted);
        foreach (var subscriber in _subscribers.Values)
            subscriber.TryWrite(item, redacted);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<RecentConsoleLogsResult> GetRecentAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var take = Math.Clamp(filter.Limit ?? _maxRecentQuerySize, 1, _maxRecentQuerySize);
        ConsoleLogLine[] items;
        ConsoleLogDroppedSummary[] dropped;

        lock (_gate)
        {
            items = _recent
                .Where(x => ConsoleLogFilterMatcher.IsMatch(x, filter))
                .OrderBy(x => x.ReceivedAt)
                .ThenBy(x => x.Timestamp)
                .ThenBy(x => x.Source.Id, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Sequence)
                .ThenBy(x => x.Id, StringComparer.Ordinal)
                .TakeLast(take)
                .ToArray();
            dropped = _dropped.ToArray();
        }

        var result = new RecentConsoleLogsResult
        {
            Items = items,
            Dropped = dropped,
            Sources = _sourceRegistry.List().ToArray()
        };

        return ValueTask.FromResult(result);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ConsoleLogStreamingItem> SubscribeAsync(
        ConsoleLogFilter filter,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var subscriber = new Subscriber(filter, _subscriberCapacity);
        _subscribers[subscriber.Id] = subscriber;

        try
        {
            await foreach (var item in subscriber.Channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                yield return item;
        }
        finally
        {
            _subscribers.TryRemove(subscriber.Id, out _);
            subscriber.Channel.Writer.TryComplete();
        }
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyCollection<ConsoleLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(_sourceRegistry.List());
    }

    /// <inheritdoc />
    public void ReportDropped(ConsoleLogDroppedSummary summary)
    {
        lock (_gate)
            AddDropped(summary);
    }

    private void AddDropped(ConsoleLogDroppedSummary summary)
    {
        _dropped.Add(summary);
        if (_dropped.Count > 100)
            _dropped.RemoveAt(0);
    }

    private sealed class Subscriber(ConsoleLogFilter filter, int capacity)
    {
        private long _droppedCount;
        private DateTimeOffset? _firstDrop;

        public Guid Id { get; } = Guid.NewGuid();

        public Channel<ConsoleLogStreamingItem> Channel { get; } = System.Threading.Channels.Channel.CreateBounded<ConsoleLogStreamingItem>(
            new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = true,
                SingleWriter = false
            });

        public void TryWrite(ConsoleLogStreamingItem item, ConsoleLogLine line)
        {
            if (!ConsoleLogFilterMatcher.IsMatch(line, filter))
                return;

            if (Channel.Writer.TryWrite(item))
                return;

            _firstDrop ??= DateTimeOffset.UtcNow;
            var dropped = Interlocked.Increment(ref _droppedCount);
            if (dropped % capacity != 0)
                return;

            var summary = new ConsoleLogDroppedSummary
            {
                SourceId = line.Source.Id,
                Stream = line.Stream,
                Reason = "subscriber-overflow",
                Count = dropped,
                From = _firstDrop,
                To = DateTimeOffset.UtcNow
            };
            Channel.Writer.TryWrite(ConsoleLogStreamingItem.FromDropped(summary));
        }
    }
}
