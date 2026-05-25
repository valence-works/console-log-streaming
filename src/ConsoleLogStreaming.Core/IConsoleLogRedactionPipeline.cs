using ConsoleLogStreaming.Core.Models;

namespace ConsoleLogStreaming.Core;

/// <summary>
/// Applies the ordered console log redaction chain.
/// </summary>
public interface IConsoleLogRedactionPipeline
{
    /// <summary>
    /// Applies configured redactors to a console line.
    /// </summary>
    ConsoleLogLine Redact(ConsoleLogLine line);

    /// <summary>
    /// Applies configured redactors to a source descriptor.
    /// </summary>
    ConsoleLogSource Redact(ConsoleLogSource source);
}
