using ConsoleLogStreaming.Core.Models;

namespace ConsoleLogStreaming.Core.Redaction;

/// <summary>
/// Ordered redaction pipeline composed from registered redactors.
/// </summary>
public sealed class ConsoleLogRedactionPipeline : IConsoleLogRedactionPipeline
{
    private readonly IReadOnlyList<IConsoleLogRedactor> _redactors;

    /// <summary>
    /// Initializes a new instance of the pipeline.
    /// </summary>
    public ConsoleLogRedactionPipeline(IEnumerable<IConsoleLogRedactor> redactors)
    {
        _redactors = redactors.OrderBy(x => x.Order).ToArray();
    }

    /// <inheritdoc />
    public ConsoleLogLine Redact(ConsoleLogLine line)
    {
        foreach (var redactor in _redactors)
            line = redactor.Redact(line);

        return line;
    }

    /// <inheritdoc />
    public ConsoleLogSource Redact(ConsoleLogSource source)
    {
        foreach (var redactor in _redactors)
            source = redactor.Redact(source);

        return source;
    }
}
