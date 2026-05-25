namespace ConsoleLogStreaming.Core.Models;

/// <summary>
/// Result returned by recent console log queries.
/// </summary>
public sealed record RecentConsoleLogsResult
{
    /// <summary>
    /// Ordered matching console lines.
    /// </summary>
    public IReadOnlyList<ConsoleLogLine> Items { get; init; } = [];

    /// <summary>
    /// Dropped summaries relevant to the result.
    /// </summary>
    public IReadOnlyList<ConsoleLogDroppedSummary> Dropped { get; init; } = [];

    /// <summary>
    /// Source snapshot when available.
    /// </summary>
    public IReadOnlyList<ConsoleLogSource> Sources { get; init; } = [];
}
