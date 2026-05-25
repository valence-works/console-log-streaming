using ConsoleLogStreaming.Core.Models;

namespace ConsoleLogStreaming.Core;

/// <summary>
/// Redacts console lines and source metadata as one component in the redaction pipeline.
/// </summary>
public interface IConsoleLogRedactor
{
    /// <summary>
    /// Ordering value used by the redaction pipeline. Lower values run first.
    /// </summary>
    int Order => 0;

    /// <summary>
    /// Redacts a console line.
    /// </summary>
    ConsoleLogLine Redact(ConsoleLogLine line);

    /// <summary>
    /// Redacts a source descriptor.
    /// </summary>
    ConsoleLogSource Redact(ConsoleLogSource source);
}
