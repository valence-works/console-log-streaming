namespace ConsoleLogStreaming.Core.Models;

/// <summary>
/// Reports dropped console lines or writes caused by bounded buffers.
/// </summary>
public sealed record ConsoleLogDroppedSummary
{
    /// <summary>
    /// Affected source when known.
    /// </summary>
    public string? SourceId { get; init; }

    /// <summary>
    /// Affected stream when known.
    /// </summary>
    public ConsoleStream? Stream { get; init; }

    /// <summary>
    /// Safe reason for the drop.
    /// </summary>
    public string Reason { get; init; } = "";

    /// <summary>
    /// Number of dropped items.
    /// </summary>
    public long Count { get; init; }

    /// <summary>
    /// Start of the drop period when known.
    /// </summary>
    public DateTimeOffset? From { get; init; }

    /// <summary>
    /// End of the drop period when known.
    /// </summary>
    public DateTimeOffset? To { get; init; }
}
