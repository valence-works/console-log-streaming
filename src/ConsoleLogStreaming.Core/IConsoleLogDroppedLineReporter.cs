using ConsoleLogStreaming.Core.Models;

namespace ConsoleLogStreaming.Core;

/// <summary>
/// Receives summaries when bounded console log queues drop lines.
/// </summary>
public interface IConsoleLogDroppedLineReporter
{
    /// <summary>
    /// Reports a dropped-line summary.
    /// </summary>
    void ReportDropped(ConsoleLogDroppedSummary summary);
}
