using ConsoleLogStream.Core.Models;

namespace ConsoleLogStream.Core;

/// <summary>
/// Redacts console lines and source metadata before provider boundaries.
/// </summary>
public interface IConsoleLogRedactor
{
    /// <summary>
    /// Redacts a console line.
    /// </summary>
    ConsoleLogLine Redact(ConsoleLogLine line);

    /// <summary>
    /// Redacts a source descriptor.
    /// </summary>
    ConsoleLogSource Redact(ConsoleLogSource source);
}
