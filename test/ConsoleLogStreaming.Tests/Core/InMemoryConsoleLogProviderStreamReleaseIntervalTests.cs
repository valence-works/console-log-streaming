using ConsoleLogStreaming.Core.Models;
using ConsoleLogStreaming.Core.Options;
using ConsoleLogStreaming.Core.Providers;
using ConsoleLogStreaming.Core.Redaction;
using ConsoleLogStreaming.Core.Sources;
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
    private readonly InMemoryConsoleLogProvider _provider;
    private readonly ConsoleLogSource _source;
    private readonly IAsyncEnumerator<ConsoleLogStreamingItem> _subscription;
    private long _sequence;

    public InMemoryConsoleLogProviderStreamReleaseIntervalTests()
    {
        (_provider, _source) = CreateProvider(ReleaseInterval, _time);
        _subscription = _provider.SubscribeAsync(new ConsoleLogFilter()).GetAsyncEnumerator();
    }

    public async ValueTask DisposeAsync() => await _subscription.DisposeAsync();

    [Fact]
    public async Task ReleasesTheFirstLineWithoutDelay()
    {
        // Starting the pull registers the subscriber and parks the iterator at the channel wait.
        var next = _subscription.MoveNextAsync().AsTask();
        await PublishAsync("first");

        Assert.True(await next.WaitAsync(RealTimeout));
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
    public async Task ZeroIntervalStreamsWithoutGating()
    {
        var (provider, source) = CreateProvider(TimeSpan.Zero, _time);
        await using var subscription = provider.SubscribeAsync(new ConsoleLogFilter()).GetAsyncEnumerator();

        var first = subscription.MoveNextAsync().AsTask();
        await provider.PublishAsync(Line(source, "first"));
        Assert.True(await first.WaitAsync(RealTimeout));
        Assert.Equal("first", subscription.Current.Line!.Text);

        // The default path never touches the clock: a follow-up line streams immediately.
        var next = subscription.MoveNextAsync().AsTask();
        await provider.PublishAsync(Line(source, "second"));
        Assert.True(await next.WaitAsync(RealTimeout));
        Assert.Equal("second", subscription.Current.Line!.Text);
    }

    private static (InMemoryConsoleLogProvider Provider, ConsoleLogSource Source) CreateProvider(
        TimeSpan streamReleaseInterval,
        TimeProvider timeProvider)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new ConsoleLogOptions
        {
            SourceId = "source-a",
            StreamReleaseInterval = streamReleaseInterval
        });
        var sourceRegistry = new ConsoleLogSourceRegistry(options);
        var redactionPipeline = new ConsoleLogRedactionPipeline([new RegexConsoleLogRedactor(options)]);
        var provider = new InMemoryConsoleLogProvider(options, redactionPipeline, sourceRegistry, timeProvider);
        return (provider, sourceRegistry.Current);
    }

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
    /// gate registering its release timer.
    /// </summary>
    private async Task<bool> WaitDrivingFakeTimeAsync(Task<bool> pendingMoveNext)
    {
        var deadline = DateTime.UtcNow + RealTimeout;
        while (!pendingMoveNext.IsCompleted && DateTime.UtcNow < deadline)
        {
            _time.Advance(TimeSpan.FromMilliseconds(20));
            await Task.WhenAny(pendingMoveNext, Task.Delay(TimeSpan.FromMilliseconds(10)));
        }

        return await pendingMoveNext.WaitAsync(TimeSpan.FromSeconds(1));
    }
}
