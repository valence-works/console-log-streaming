using System.Text.RegularExpressions;

namespace ConsoleLogStreaming.Core.Options;

/// <summary>
/// Configured regular expression used to redact console line text or source metadata.
/// </summary>
public sealed record ConsoleLogRedactionRule
{
    /// <summary>
    /// Rule name.
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// Regular expression pattern.
    /// </summary>
    public string Pattern { get; init; } = "";

    /// <summary>
    /// Replacement text.
    /// </summary>
    public string Replacement { get; init; } = "[redacted]";

    /// <summary>
    /// Compiles the configured pattern.
    /// </summary>
    public Regex CreateRegex() => new(Pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(250));
}
