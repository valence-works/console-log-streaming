using ConsoleLogStreaming.Core.Models;

namespace ConsoleLogStreaming.Core;

/// <summary>
/// Tracks source metadata and source last-seen state.
/// </summary>
public interface IConsoleLogSourceRegistry
{
    /// <summary>
    /// Raised when a source is added or its health changes.
    /// </summary>
    event Action<ConsoleLogSource>? SourceChanged;

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
