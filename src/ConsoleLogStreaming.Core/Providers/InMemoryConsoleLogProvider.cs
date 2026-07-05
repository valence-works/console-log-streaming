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
    private readonly TimeSpan _streamReleaseInterval;
    private readonly TimeProvider _timeProvider;
    private readonly IConsoleLogRedactionPipeline _redactionPipeline;
    private readonly IConsoleLogSourceRegistry _sourceRegistry;
    private readonly Queue<ConsoleLogLine> _recent = new();
    private readonly List<ConsoleLogDroppedSummary> _dropped = [];
    private readonly ConcurrentDictionary<Guid, Subscriber> _subscribers = new();

    /// <summary>
    /// Initializes a new instance of the provider. The optional <paramref name="timeProvider"/>
    /// (default <see cref="TimeProvider.System"/>) drives release gating and drop-summary
    /// timestamps; when the provider is activated from a service container, a registered
    /// <see cref="TimeProvider"/> is injected automatically.
    /// </summary>
    public InMemoryConsoleLogProvider(
        IOptions<ConsoleLogOptions> options,
        IConsoleLogRedactionPipeline redactionPipeline,
        IConsoleLogSourceRegistry sourceRegistry,
        TimeProvider? timeProvider = null)
    {
        var value = options.Value;
        _recentCapacity = Math.Max(1, value.RecentCapacity);
        _subscriberCapacity = Math.Max(1, value.SubscriberCapacity);
        _maxRecentQuerySize = Math.Max(1, value.MaxRecentQuerySize);
        _streamReleaseInterval = value.StreamReleaseInterval;
        _timeProvider = timeProvider ?? TimeProvider.System;
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
        var subscriber = new Subscriber(filter, _subscriberCapacity, _timeProvider);
        _subscribers[subscriber.Id] = subscriber;

        try
        {
            if (_streamReleaseInterval <= TimeSpan.Zero)
            {
                await foreach (var item in subscriber.Channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                    yield return item;
                yield break;
            }

            // Time-gated batch releases: a batch is the run of items synchronously available from
            // the subscriber channel; the gate applies only at the drained-buffer boundary, so a
            // request/log feedback loop (one pushed line completing one pending long poll) is
            // capped at one release per interval while floods and bursts drain ungated.
            var reader = subscriber.Channel.Reader;
            long? lastReleaseTimestamp = null;

            while (true)
            {
                var waitToRead = reader.WaitToReadAsync(cancellationToken);

                // An asynchronous wait means the buffer is drained: the current batch is complete
                // and the next item opens a new batch behind the release gate. A synchronous
                // completion means the buffer refilled while the previous batch drained — a
                // sustained flood with no request feedback to dampen — so it extends the batch
                // ungated.
                var opensNewBatch = !waitToRead.IsCompleted;

                if (!await waitToRead.ConfigureAwait(false))
                    yield break;

                if (opensNewBatch)
                {
                    if (lastReleaseTimestamp is { } previousRelease)
                    {
                        var wait = _streamReleaseInterval - _timeProvider.GetElapsedTime(previousRelease);
                        if (wait > TimeSpan.Zero)
                            await Task.Delay(wait, _timeProvider, cancellationToken).ConfigureAwait(false);
                    }

                    lastReleaseTimestamp = _timeProvider.GetTimestamp();
                }

                while (reader.TryRead(out var item))
                    yield return item;
            }
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

    private sealed class Subscriber(ConsoleLogFilter filter, int capacity, TimeProvider timeProvider)
    {
        private long _droppedCount;
        private DateTimeOffset? _firstDrop;
        private ConsoleLogDroppedSummary? _pendingDropSummary;

        public Guid Id { get; } = Guid.NewGuid();

        // Wait mode so TryWrite reports rejection when the queue is full (only TryWrite is used,
        // so nothing ever blocks); DropWrite makes TryWrite return true while silently discarding
        // the item, which would turn the drop accounting below into dead code.
        public Channel<ConsoleLogStreamingItem> Channel { get; } = System.Threading.Channels.Channel.CreateBounded<ConsoleLogStreamingItem>(
            new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });

        public void TryWrite(ConsoleLogStreamingItem item, ConsoleLogLine line)
        {
            if (!ConsoleLogFilterMatcher.IsMatch(line, filter))
                return;

            FlushPendingDropSummary();

            if (Channel.Writer.TryWrite(item))
                return;

            _firstDrop ??= timeProvider.GetUtcNow();
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
                To = timeProvider.GetUtcNow()
            };

            // The channel that just rejected the line is usually still full, so this write tends
            // to fail as well; park the summary and retry on the next publish, once the reader
            // has had a chance to drain. Counts are cumulative, so a newer summary superseding a
            // parked one loses no information.
            if (!Channel.Writer.TryWrite(ConsoleLogStreamingItem.FromDropped(summary)))
                _pendingDropSummary = summary;
        }

        private void FlushPendingDropSummary()
        {
            var pending = Interlocked.Exchange(ref _pendingDropSummary, null);
            if (pending is not null && !Channel.Writer.TryWrite(ConsoleLogStreamingItem.FromDropped(pending)))
                _pendingDropSummary = pending;
        }
    }
}
