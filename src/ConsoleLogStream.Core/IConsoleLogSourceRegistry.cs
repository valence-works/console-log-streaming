using ConsoleLogStream.Core.Models;

namespace ConsoleLogStream.Core;

/// <summary>
/// Tracks source metadata and source last-seen state.
/// </summary>
public interface IConsoleLogSourceRegistry
{
    /// <summary>
    /// Current local source.
    /// </summary>
    ConsoleLogSource Current { get; }

    /// <summary>
    /// Marks a source as seen at the specified timestamp.
    /// </summary>
    ConsoleLogSource MarkSeen(ConsoleLogSource source, DateTimeOffset timestamp);

    /// <summary>
    /// Lists known sources.
    /// </summary>
    IReadOnlyCollection<ConsoleLogSource> List();
}
