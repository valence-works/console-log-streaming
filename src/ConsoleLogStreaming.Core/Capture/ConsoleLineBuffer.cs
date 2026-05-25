using System.Text;
using ConsoleLogStreaming.Core.Options;
using Microsoft.Extensions.Options;

namespace ConsoleLogStreaming.Core.Capture;

/// <summary>
/// Reassembles arbitrary stdout/stderr write chunks into complete lines.
/// </summary>
public sealed class ConsoleLineBuffer
{
    private readonly StringBuilder _buffer = new();
    private readonly ConsoleLogOptions _options;
    private DateTimeOffset? _lastWriteAt;
    private bool _logicalLineHasContent;
    private bool _metadataCaptured;
    private bool _droppingUntilNewline;
    private IReadOnlyDictionary<string, string> _metadata = EmptyMetadata;

    /// <summary>
    /// Initializes a new instance of the line buffer.
    /// </summary>
    public ConsoleLineBuffer(IOptions<ConsoleLogOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// Appends a raw console chunk and returns completed lines.
    /// </summary>
    public IReadOnlyCollection<BufferedConsoleLine> Append(
        string value,
        DateTimeOffset now,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        var lines = new List<BufferedConsoleLine>();

        foreach (var ch in value)
        {
            if (ch == '\r')
                continue;

            if (ch == '\n')
            {
                if (_droppingUntilNewline)
                {
                    _droppingUntilNewline = false;
                    _logicalLineHasContent = false;
                    continue;
                }

                if (_buffer.Length > 0)
                    lines.Add(FlushBuffer());
                else if (!_logicalLineHasContent)
                    lines.Add(new(string.Empty, NormalizeMetadata(metadata)));

                _logicalLineHasContent = false;
                continue;
            }

            if (_droppingUntilNewline)
                continue;

            CaptureMetadata(metadata);
            _buffer.Append(ch);
            _logicalLineHasContent = true;

            if (_buffer.Length >= Math.Max(1, _options.MaxLineLength))
            {
                lines.Add(FlushBuffer(truncated: true));
                _droppingUntilNewline = true;
            }
        }

        _lastWriteAt = _buffer.Length > 0 ? now : null;
        return lines;
    }

    /// <summary>
    /// Flushes the buffered line when it has been idle longer than the configured timeout.
    /// </summary>
    public BufferedConsoleLine? FlushIfIdle(DateTimeOffset now)
    {
        if (_buffer.Length == 0 || _lastWriteAt == null)
            return null;

        return now - _lastWriteAt >= _options.IdleFlushTimeout ? FlushBuffer() : null;
    }

    /// <summary>
    /// Flushes the currently buffered line if present.
    /// </summary>
    public BufferedConsoleLine? Flush()
    {
        return _buffer.Length == 0 ? null : FlushBuffer();
    }

    private void CaptureMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if (_metadataCaptured)
            return;

        _metadataCaptured = true;
        _metadata = NormalizeMetadata(metadata);
    }

    private BufferedConsoleLine FlushBuffer()
    {
        return FlushBuffer(truncated: false);
    }

    private BufferedConsoleLine FlushBuffer(bool truncated)
    {
        var line = _buffer.ToString();
        var metadata = _metadata;
        _buffer.Clear();
        _metadataCaptured = false;
        _metadata = EmptyMetadata;
        return new(line, metadata, truncated);
    }

    private static IReadOnlyDictionary<string, string> NormalizeMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata == null || metadata.Count == 0)
            return EmptyMetadata;

        return metadata
            .Where(x => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.Value))
            .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata = new Dictionary<string, string>();
}

/// <summary>
/// A complete console line assembled from one or more raw console write chunks.
/// </summary>
public readonly record struct BufferedConsoleLine(string Text, IReadOnlyDictionary<string, string> Metadata, bool Truncated = false);
