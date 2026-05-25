using ConsoleLogStreaming.Core.Options;
using Microsoft.Extensions.Options;

namespace ConsoleLogStreaming.Core.Capture;

/// <summary>
/// Applies ANSI stripping and maximum length truncation to completed console lines.
/// </summary>
public sealed class ConsoleLineFormatter
{
    private readonly ConsoleLogOptions _options;

    /// <summary>
    /// Initializes a new instance of the formatter.
    /// </summary>
    public ConsoleLineFormatter(IOptions<ConsoleLogOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// Formats the specified text according to the configured options.
    /// </summary>
    public FormattedConsoleLine Format(string text)
    {
        if (!_options.PreserveAnsi)
            text = AnsiStripper.Strip(text);

        var truncated = text.Length > Math.Max(1, _options.MaxLineLength);
        if (truncated)
            text = text[..Math.Max(1, _options.MaxLineLength)];

        return new(text, truncated);
    }
}

/// <summary>
/// A formatted console line and whether truncation occurred.
/// </summary>
public sealed record FormattedConsoleLine(string Text, bool Truncated);
