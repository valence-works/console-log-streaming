namespace ConsoleLogStreaming.Contracts;

/// <summary>
/// Result returned by recent console log queries.
/// </summary>
public sealed record RecentConsoleLogsResult(
    IReadOnlyCollection<ConsoleLogLine> Items,
    IReadOnlyCollection<ConsoleLogDroppedSummary>? Dropped = null,
    IReadOnlyCollection<ConsoleLogSource>? Sources = null);
