namespace ConsoleLogStreaming.Contracts;

/// <summary>
/// Reports dropped console lines or writes caused by bounded buffers.
/// </summary>
public sealed record ConsoleLogDroppedSummary(
    string? SourceId,
    ConsoleLogStreaming? Stream,
    string Reason,
    long Count,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null);
