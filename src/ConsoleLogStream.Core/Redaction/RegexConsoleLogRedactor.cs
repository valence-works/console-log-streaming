using System.Text.RegularExpressions;
using ConsoleLogStream.Core.Models;
using ConsoleLogStream.Core.Options;
using Microsoft.Extensions.Options;

namespace ConsoleLogStream.Core.Redaction;

/// <summary>
/// Regex-based console log redactor.
/// </summary>
public sealed class RegexConsoleLogRedactor : IConsoleLogRedactor
{
    private readonly IReadOnlyList<(Regex Regex, string Replacement)> _rules;

    /// <summary>
    /// Initializes a new instance of the redactor.
    /// </summary>
    public RegexConsoleLogRedactor(IOptions<ConsoleLogOptions> options)
    {
        _rules = options.Value.RedactionRules.Select(x => (x.CreateRegex(), x.Replacement)).ToArray();
    }

    /// <inheritdoc />
    public ConsoleLogLine Redact(ConsoleLogLine line)
    {
        var source = Redact(line.Source);
        return line with { Text = RedactValue(line.Text), Source = source };
    }

    /// <inheritdoc />
    public ConsoleLogSource Redact(ConsoleLogSource source)
    {
        var metadata = source.Metadata.ToDictionary(x => RedactValue(x.Key), x => RedactValue(x.Value), StringComparer.OrdinalIgnoreCase);
        return source with
        {
            Id = RedactValue(source.Id),
            DisplayName = RedactValue(source.DisplayName),
            ServiceName = source.ServiceName is null ? null : RedactValue(source.ServiceName),
            MachineName = source.MachineName is null ? null : RedactValue(source.MachineName),
            Metadata = metadata
        };
    }

    private string RedactValue(string value)
    {
        var redacted = value;
        foreach (var (regex, replacement) in _rules)
            redacted = regex.Replace(redacted, replacement);
        return redacted;
    }
}
