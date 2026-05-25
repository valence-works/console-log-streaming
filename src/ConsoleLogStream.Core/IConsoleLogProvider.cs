using ConsoleLogStream.Core.Models;

namespace ConsoleLogStream.Core;

/// <summary>
/// Stores recent redacted console lines and streams live lines to subscribers.
/// </summary>
public interface IConsoleLogProvider
{
    /// <summary>
    /// Publishes a console line.
    /// </summary>
    ValueTask PublishAsync(ConsoleLogLine line, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries recent console lines.
    /// </summary>
    ValueTask<RecentConsoleLogsResult> GetRecentAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to live console log stream items.
    /// </summary>
    IAsyncEnumerable<ConsoleLogStreamItem> SubscribeAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists known console log sources.
    /// </summary>
    ValueTask<IReadOnlyCollection<ConsoleLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default);
}
