namespace ConsoleLogStream.Core.Models;

/// <summary>
/// A redacted, normalized stdout or stderr line.
/// </summary>
public sealed record ConsoleLogLine
{
    /// <summary>
    /// Unique line identifier.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("n");

    /// <summary>
    /// Timestamp assigned when capture emitted the line.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Timestamp assigned when a provider accepted the line.
    /// </summary>
    public DateTimeOffset ReceivedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Source-local monotonic sequence.
    /// </summary>
    public long Sequence { get; init; }

    /// <summary>
    /// Managed console stream that produced the line.
    /// </summary>
    public ConsoleStream Stream { get; init; }

    /// <summary>
    /// Redacted and normalized line text.
    /// </summary>
    public string Text { get; init; } = "";

    /// <summary>
    /// Redacted source descriptor.
    /// </summary>
    public ConsoleLogSource Source { get; init; } = new();

    /// <summary>
    /// Whether the original line exceeded the configured maximum length.
    /// </summary>
    public bool Truncated { get; init; }

    /// <summary>
    /// Optional dropped-line metadata associated with this line.
    /// </summary>
    public ConsoleLogDroppedSummary? Dropped { get; init; }
}
