using ConsoleLogStreaming.Core;
using ConsoleLogStreaming.Core.DependencyInjection;
using ConsoleLogStreaming.Core.Models;
using ConsoleLogStreaming.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace ConsoleLogStreaming.Tests.Core;

/// <summary>
/// Fake-clock tests for <see cref="ConsoleLogOptions.StreamReleaseInterval"/>: one published line
/// must not be able to complete one pending pull 1:1 (the long-polling feedback storm shape).
/// Releases are gated to at most one batch per interval, while items synchronously available from
/// the subscriber buffer drain together without further waits.
/// </summary>
public sealed class InMemoryConsoleLogProviderStreamReleaseIntervalTests : IAsyncDisposable
{
    private static readonly TimeSpan ReleaseInterval = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan RealTimeout = TimeSpan.FromSeconds(10);

    // Long enough for the gate to observably hold a line that an ungated subscription would
    // deliver instantly, short enough to keep the suite fast. The gate runs on fake time, so
    // this real-time wait can never open it.
    private static readonly TimeSpan HeldAssertionWindow = TimeSpan.FromMilliseconds(250);

    private readonly FakeTimeProvider _time = new();
    private readonly IConsoleLogProvider _provider;
    private readonly ConsoleLogSource _source;
    private readonly CancellationTokenSource _cts = new();
    private readonly IAsyncEnumerator<ConsoleLogStreamingItem> _subscription;
    private long _sequence;

    public InMemoryConsoleLogProviderStreamReleaseIntervalTests()
    {
        (_provider, _source) = CreateProvider(ReleaseInterval, _time);
        _subscription = _provider.SubscribeAsync(new ConsoleLogFilter(), _cts.Token).GetAsyncEnumerator();
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        try
        {
            await _subscription.DisposeAsync();
        }
        catch (NotSupportedException)
        {
            // Only reachable when a test already failed with a pull still pending: the iterator
            // rejects disposal mid-MoveNextAsync and the cancellation above unwinds it instead,
            // so disposal noise cannot mask the real assertion failure.
        }

        _cts.Dispose();
    }

    [Fact]
    public async Task ReleasesTheFirstLineWithoutDelay()
    {
        await ReceiveFirstLineAsync();
        Assert.Equal("first", _subscription.Current.Line!.Text);
    }

    [Fact]
    public async Task HoldsAFollowUpLineUntilTheReleaseIntervalElapses()
    {
        await ReceiveFirstLineAsync();

        var next = _subscription.MoveNextAsync().AsTask();
        await PublishAsync("second");
        await Task.Delay(HeldAssertionWindow);

        // The 1:1 storm shape: without the gate, "second" would have completed this pull instantly.
        Assert.False(next.IsCompleted);

        _time.Advance(ReleaseInterval);
        Assert.True(await WaitDrivingFakeTimeAsync(next));
        Assert.Equal("second", _subscription.Current.Line!.Text);
    }

    [Fact]
    public async Task ReleasesLinesBufferedBehindTheGateAsOneBatch()
    {
        await ReceiveFirstLineAsync();

        var next = _subscription.MoveNextAsync().AsTask();
        await PublishAsync("second");
        await PublishAsync("third");
        await PublishAsync("fourth");
        await Task.Delay(HeldAssertionWindow);
        Assert.False(next.IsCompleted);

        _time.Advance(ReleaseInterval);
        Assert.True(await WaitDrivingFakeTimeAsync(next));
        Assert.Equal("second", _subscription.Current.Line!.Text);

        // The rest of the batch drains on real time alone: no further clock advancement.
        await AssertNextLineAsync("third");
        await AssertNextLineAsync("fourth");
    }

    [Fact]
    public async Task LinesArrivingWhileABatchDrainsJoinItWithoutGating()
    {
        await ReceiveFirstLineAsync();

        var next = _subscription.MoveNextAsync().AsTask();
        await PublishAsync("second");
        _time.Advance(ReleaseInterval);
        Assert.True(await WaitDrivingFakeTimeAsync(next));
        Assert.Equal("second", _subscription.Current.Line!.Text);

        // Published while the iterator is suspended mid-batch at the yield: the batch is still
        // draining, so these stream on real time alone — a flood is never gated.
        await PublishAsync("third");
        await PublishAsync("fourth");
        await AssertNextLineAsync("third");
        await AssertNextLineAsync("fourth");
    }

    [Fact]
    public async Task ZeroIntervalStreamsWithoutGating()
    {
        var (provider, source) = CreateProvider(TimeSpan.Zero, _time);
        await using var subscription = provider.SubscribeAsync(new ConsoleLogFilter()).GetAsyncEnumerator();

        // The default path never touches the clock: every line streams on real time alone.
        await PublishAndReceiveAsync("first");
        await PublishAndReceiveAsync("second");

        async Task PublishAndReceiveAsync(string text)
        {
            var next = subscription.MoveNextAsync().AsTask();
            await provider.PublishAsync(Line(source, text));
            Assert.True(await next.WaitAsync(RealTimeout));
            Assert.Equal(text, subscription.Current.Line!.Text);
        }
    }

    private static (IConsoleLogProvider Provider, ConsoleLogSource Source) CreateProvider(
        TimeSpan streamReleaseInterval,
        TimeProvider timeProvider)
    {
        var services = new ServiceCollection()
            .AddConsoleLogStreaming(options =>
            {
                options.SourceId = "source-a";
                options.StreamReleaseInterval = streamReleaseInterval;
            })
            .AddSingleton(timeProvider)
            .BuildServiceProvider();

        return (services.GetRequiredService<IConsoleLogProvider>(), services.GetRequiredService<IConsoleLogSourceRegistry>().Current);
    }

    /// <summary>
    /// Starts the pull first — registering the subscriber and parking the iterator at the channel
    /// wait — then publishes; a line published before the first pull would be missed.
    /// </summary>
    private async Task ReceiveFirstLineAsync()
    {
        var next = _subscription.MoveNextAsync().AsTask();
        await PublishAsync("first");
        Assert.True(await next.WaitAsync(RealTimeout));
    }

    private async Task AssertNextLineAsync(string expectedText)
    {
        Assert.True(await _subscription.MoveNextAsync().AsTask().WaitAsync(RealTimeout));
        Assert.Equal(expectedText, _subscription.Current.Line!.Text);
    }

    private ValueTask PublishAsync(string text) => _provider.PublishAsync(Line(_source, text));

    private ConsoleLogLine Line(ConsoleLogSource source, string text) => new()
    {
        Source = source,
        Stream = ConsoleStream.Stdout,
        Text = text,
        Sequence = Interlocked.Increment(ref _sequence)
    };

    /// <summary>
    /// Awaits the pending pull while nudging the fake clock, so the assertion cannot race the
    /// gate registering its release timer. The extra fake time is capped at one additional
    /// interval — enough to cover the registration race, small enough that a gate wrongly
    /// holding for two or more intervals still fails the test.
    /// </summary>
    private async Task<bool> WaitDrivingFakeTimeAsync(Task<bool> pendingMoveNext)
    {
        var step = TimeSpan.FromMilliseconds(20);
        var advanced = TimeSpan.Zero;
        var deadline = DateTime.UtcNow + RealTimeout;

        while (!pendingMoveNext.IsCompleted && DateTime.UtcNow < deadline && advanced < ReleaseInterval)
        {
            _time.Advance(step);
            advanced += step;
            await Task.WhenAny(pendingMoveNext, Task.Delay(TimeSpan.FromMilliseconds(10)));
        }

        return await pendingMoveNext.WaitAsync(TimeSpan.FromSeconds(1));
    }
}
