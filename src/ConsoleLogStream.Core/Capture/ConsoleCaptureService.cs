using System.Text;
using ConsoleLogStream.Core.Models;
using ConsoleLogStream.Core.Options;
using Microsoft.Extensions.Options;

namespace ConsoleLogStream.Core.Capture;

/// <summary>
/// Managed console capture service that tees stdout and stderr.
/// </summary>
public sealed class ConsoleCaptureService : IConsoleLogCapture
{
    private readonly IConsoleLogProvider _provider;
    private readonly IConsoleLogSourceRegistry _sourceRegistry;
    private readonly ConsoleLogOptions _options;
    private readonly object _gate = new();
    private TextWriter? _originalOut;
    private TextWriter? _originalError;
    private TeeTextWriter? _outWriter;
    private TeeTextWriter? _errorWriter;
    private bool _started;
    private long _sequence;

    /// <summary>
    /// Initializes a new instance of the capture service.
    /// </summary>
    public ConsoleCaptureService(
        IConsoleLogProvider provider,
        IConsoleLogSourceRegistry sourceRegistry,
        IOptions<ConsoleLogOptions> options)
    {
        _provider = provider;
        _sourceRegistry = sourceRegistry;
        _options = options.Value;
    }

    /// <inheritdoc />
    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (_started)
                return ValueTask.CompletedTask;

            _originalOut = Console.Out;
            _originalError = Console.Error;
            _outWriter = new TeeTextWriter(_originalOut, ConsoleStream.Stdout, EmitLine, _options);
            _errorWriter = new TeeTextWriter(_originalError, ConsoleStream.Stderr, EmitLine, _options);
            Console.SetOut(_outWriter);
            Console.SetError(_errorWriter);
            _started = true;
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (!_started)
                return ValueTask.CompletedTask;

            Console.SetOut(_originalOut ?? TextWriter.Null);
            Console.SetError(_originalError ?? TextWriter.Null);
            _outWriter?.FlushBufferedLine();
            _errorWriter?.FlushBufferedLine();
            _outWriter?.Dispose();
            _errorWriter?.Dispose();
            _outWriter = null;
            _errorWriter = null;
            _started = false;
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }

    private void EmitLine(ConsoleStream stream, string text, bool truncated)
    {
        if (!_options.PreserveAnsi)
            text = AnsiStripper.Strip(text);

        var timestamp = DateTimeOffset.UtcNow;
        var line = new ConsoleLogLine
        {
            Timestamp = timestamp,
            ReceivedAt = timestamp,
            Sequence = Interlocked.Increment(ref _sequence),
            Stream = stream,
            Text = text,
            Truncated = truncated,
            Source = _sourceRegistry.Current
        };

        _provider.PublishAsync(line).AsTask().GetAwaiter().GetResult();
    }

    private sealed class TeeTextWriter : TextWriter
    {
        private readonly TextWriter _inner;
        private readonly ConsoleStream _stream;
        private readonly Action<ConsoleStream, string, bool> _emit;
        private readonly ConsoleLogOptions _options;
        private readonly object _gate = new();
        private readonly StringBuilder _buffer = new();
        private readonly Timer _idleTimer;
        private bool _droppingUntilNewline;
        private bool _disposed;

        public TeeTextWriter(
            TextWriter inner,
            ConsoleStream stream,
            Action<ConsoleStream, string, bool> emit,
            ConsoleLogOptions options)
        {
            _inner = inner;
            _stream = stream;
            _emit = emit;
            _options = options;
            _idleTimer = new Timer(_ => FlushBufferedLine(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        public override Encoding Encoding => _inner.Encoding;

        public override void Write(char value)
        {
            _inner.Write(value);
            Capture(value);
        }

        public override void Write(string? value)
        {
            _inner.Write(value);
            if (value is null)
                return;

            foreach (var character in value)
                Capture(character);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            _inner.Write(buffer, index, count);
            for (var i = index; i < index + count; i++)
                Capture(buffer[i]);
        }

        public override void Flush()
        {
            _inner.Flush();
            FlushBufferedLine();
        }

        public void FlushBufferedLine()
        {
            lock (_gate)
            {
                if (_buffer.Length == 0)
                    return;

                EmitBufferedLine(truncated: false);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                FlushBufferedLine();
                _idleTimer.Dispose();
            }

            _disposed = true;
            base.Dispose(disposing);
        }

        private void Capture(char value)
        {
            lock (_gate)
            {
                if (value is '\r')
                    return;

                if (_droppingUntilNewline)
                {
                    if (value is '\n')
                        _droppingUntilNewline = false;
                    return;
                }

                if (value is '\n')
                {
                    EmitBufferedLine(truncated: false);
                    return;
                }

                _buffer.Append(value);
                if (_buffer.Length >= Math.Max(1, _options.MaxLineLength))
                {
                    EmitBufferedLine(truncated: true);
                    _droppingUntilNewline = true;
                    return;
                }

                ResetIdleTimer();
            }
        }

        private void EmitBufferedLine(bool truncated)
        {
            _idleTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            var line = _buffer.ToString();
            _buffer.Clear();
            if (line.Length == 0 && !truncated)
                return;

            _emit(_stream, line, truncated);
        }

        private void ResetIdleTimer()
        {
            if (_options.IdleFlushTimeout <= TimeSpan.Zero)
                return;

            _idleTimer.Change(_options.IdleFlushTimeout, Timeout.InfiniteTimeSpan);
        }
    }
}
