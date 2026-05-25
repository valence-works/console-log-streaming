namespace ConsoleLogStreaming.Contracts;

/// <summary>
/// DTO item emitted by live console log streams.
/// </summary>
public sealed record ConsoleLogStreamingItem(
    ConsoleLogLine? Line = null,
    ConsoleLogDroppedSummary? DroppedLines = null,
    ConsoleLogSource? Source = null)
{
    /// <summary>
    /// Creates a line stream item.
    /// </summary>
    public static ConsoleLogStreamingItem FromLine(ConsoleLogLine line) => new(Line: line);

    /// <summary>
    /// Creates a dropped summary stream item.
    /// </summary>
    public static ConsoleLogStreamingItem FromDroppedLines(ConsoleLogDroppedSummary summary) => new(DroppedLines: summary);

    /// <summary>
    /// Creates a source change stream item.
    /// </summary>
    public static ConsoleLogStreamingItem FromSource(ConsoleLogSource source) => new(Source: source);
}
