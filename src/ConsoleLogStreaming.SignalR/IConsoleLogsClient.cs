using ConsoleLogStreaming.Contracts;

namespace ConsoleLogStreaming.SignalR;

/// <summary>
/// SignalR client contract for pushed console log events.
/// </summary>
public interface IConsoleLogsClient
{
    /// <summary>
    /// Receives a console log line.
    /// </summary>
    Task ReceiveConsoleLogLineAsync(ConsoleLogLine line, CancellationToken cancellationToken = default);

    /// <summary>
    /// Receives a dropped-line summary.
    /// </summary>
    Task ReceiveDroppedLinesAsync(ConsoleLogDroppedSummary summary, CancellationToken cancellationToken = default);

    /// <summary>
    /// Receives a source change.
    /// </summary>
    Task ReceiveSourceChangedAsync(ConsoleLogSource source, CancellationToken cancellationToken = default);
}
