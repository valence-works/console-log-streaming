namespace ConsoleLogStream.Core.Models;

/// <summary>
/// Item emitted by live console log streams.
/// </summary>
public sealed record ConsoleLogStreamItem
{
    /// <summary>
    /// Console line event when this item carries a line.
    /// </summary>
    public ConsoleLogLine? Line { get; init; }

    /// <summary>
    /// Dropped summary when this item carries loss information.
    /// </summary>
    public ConsoleLogDroppedSummary? Dropped { get; init; }

    /// <summary>
    /// Creates a line stream item.
    /// </summary>
    public static ConsoleLogStreamItem FromLine(ConsoleLogLine line) => new() { Line = line };

    /// <summary>
    /// Creates a dropped summary stream item.
    /// </summary>
    public static ConsoleLogStreamItem FromDropped(ConsoleLogDroppedSummary dropped) => new() { Dropped = dropped };
}
