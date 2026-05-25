using System.Threading.Channels;
using ConsoleLogStreaming.Core.Models;
using ConsoleLogStreaming.Core.Options;
using Microsoft.Extensions.Options;

namespace ConsoleLogStreaming.Core.Capture;

/// <summary>
/// Managed console capture service that tees stdout and stderr into a provider without blocking console writers.
/// </summary>
public sealed class ConsoleCaptureService : IConsoleLogCapture
{
    private readonly IConsoleLogProvider _provider;
    private readonly IConsoleLogSourceRegistry _sourceRegistry;
    private readonly IConsoleLogRedactionPipeline _redactionPipeline;
    private readonly IConsoleLogMetadataAccessor _metadataAccessor;
    private readonly ConsoleLineFormatter _formatter;
    private readonly ConsoleLogOptions _options;
    private readonly object _bufferLock = new();
    private readonly ConsoleLineBuffer _stdoutBuffer;
    private readonly ConsoleLineBuffer _stderrBuffer;
    private readonly Channel<ConsoleLogLine> _publishChannel;
    private readonly CancellationTokenSource _shutdownCts = new();
    private readonly Task _publishTask;
    private IDisposable? _subscription;
    private long _sequence;
    private int _disposed;
    private bool _started;

    /// <summary>
    /// Initializes a new instance of the capture service.
    /// </summary>
    public ConsoleCaptureService(
        IConsoleLogProvider provider,
        IConsoleLogSourceRegistry sourceRegistry,
        IConsoleLogRedactionPipeline redactionPipeline,
        IConsoleLogMetadataAccessor metadataAccessor,
        ConsoleLineFormatter formatter,
        IOptions<ConsoleLogOptions> options)
    {
        _provider = provider;
        _sourceRegistry = sourceRegistry;
        _redactionPipeline = redactionPipeline;
        _metadataAccessor = metadataAccessor;
        _formatter = formatter;
        _options = options.Value;
        _stdoutBuffer = new(options);
        _stderrBuffer = new(options);
        _publishChannel = Channel.CreateBounded<ConsoleLogLine>(new BoundedChannelOptions(Math.Max(1, _options.CaptureChannelCapacity))
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
        _publishTask = Task.Run(() => PublishQueuedLinesAsync(_publishChannel.Reader, _shutdownCts.Token));
    }

    /// <inheritdoc />
    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_disposed != 0)
            throw new ObjectDisposedException(nameof(ConsoleCaptureService));

        lock (_bufferLock)
        {
            if (_started)
                return ValueTask.CompletedTask;

            _subscription = ConsoleStreamHook.Subscribe(OnChunkCaptured);
            _started = true;
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        IDisposable? subscription;

        lock (_bufferLock)
        {
            if (!_started)
                return;

            subscription = _subscription;
            _subscription = null;
            _started = false;
        }

        subscription?.Dispose();
        ConsoleStreamHook.UninstallIfIdle();

        FlushRemaining(ConsoleStream.Stdout);
        FlushRemaining(ConsoleStream.Stderr);
        _publishChannel.Writer.TryComplete();

        try
        {
            await _publishTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _shutdownCts.Cancel();
        }
        catch (OperationCanceledException)
        {
        }
    }

    /// <inheritdoc />
    public ValueTask FlushIdleAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = DateTimeOffset.UtcNow;
        Publish(ConsoleStream.Stdout, FlushIfIdle(_stdoutBuffer, now), now);
        Publish(ConsoleStream.Stderr, FlushIfIdle(_stderrBuffer, now), now);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        await StopAsync().ConfigureAwait(false);
        _shutdownCts.Dispose();
    }

    private void OnChunkCaptured(CapturedChunk chunk)
    {
        IReadOnlyCollection<BufferedConsoleLine> lines;
        var metadata = _metadataAccessor.GetMetadata();

        lock (_bufferLock)
            lines = GetBuffer(chunk.Stream).Append(chunk.Text, chunk.TimestampUtc, metadata);

        foreach (var line in lines)
            Publish(chunk.Stream, line, chunk.TimestampUtc);
    }

    private BufferedConsoleLine? FlushIfIdle(ConsoleLineBuffer buffer, DateTimeOffset now)
    {
        lock (_bufferLock)
            return buffer.FlushIfIdle(now);
    }

    private void FlushRemaining(ConsoleStream stream)
    {
        BufferedConsoleLine? line;
        lock (_bufferLock)
            line = GetBuffer(stream).Flush();

        Publish(stream, line, DateTimeOffset.UtcNow);
    }

    private ConsoleLineBuffer GetBuffer(ConsoleStream stream) =>
        stream == ConsoleStream.Stdout ? _stdoutBuffer : _stderrBuffer;

    private void Publish(ConsoleStream stream, BufferedConsoleLine? capturedLine, DateTimeOffset timestamp)
    {
        if (capturedLine == null)
            return;

        var formatted = _formatter.Format(capturedLine.Value.Text);
        var line = new ConsoleLogLine
        {
            Timestamp = timestamp,
            ReceivedAt = timestamp,
            Sequence = Interlocked.Increment(ref _sequence),
            Stream = stream,
            Text = formatted.Text,
            Metadata = capturedLine.Value.Metadata,
            Source = _sourceRegistry.Current,
            Truncated = capturedLine.Value.Truncated || formatted.Truncated
        };

        if (!_publishChannel.Writer.TryWrite(line) && _provider is IConsoleLogDroppedLineReporter reporter)
        {
            reporter.ReportDropped(new ConsoleLogDroppedSummary
            {
                SourceId = line.Source.Id,
                Stream = stream,
                Reason = "capture-channel-overflow",
                Count = 1,
                From = timestamp,
                To = timestamp
            });
        }
    }

    private async Task PublishQueuedLinesAsync(ChannelReader<ConsoleLogLine> reader, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var line in reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                ConsoleStreamHook.SuppressCapture = true;
                try
                {
                    await _provider.PublishAsync(_redactionPipeline.Redact(line), cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception)
                {
                }
                finally
                {
                    ConsoleStreamHook.SuppressCapture = false;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
