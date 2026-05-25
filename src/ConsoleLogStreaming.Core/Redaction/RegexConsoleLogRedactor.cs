using System.Text.RegularExpressions;
using ConsoleLogStreaming.Core.Capture;
using ConsoleLogStreaming.Core.Models;
using ConsoleLogStreaming.Core.Options;
using Microsoft.Extensions.Options;

namespace ConsoleLogStreaming.Core.Redaction;

/// <summary>
/// Regex-based console log redactor.
/// </summary>
public sealed class RegexConsoleLogRedactor : IConsoleLogRedactor
{
    private readonly ConsoleLogOptions _options;
    private readonly HashSet<string> _sensitiveNames;
    private readonly IReadOnlyList<(Regex Regex, string Replacement)> _rules;

    /// <summary>
    /// Initializes a new instance of the redactor.
    /// </summary>
    public RegexConsoleLogRedactor(IOptions<ConsoleLogOptions> options)
    {
        _options = options.Value;
        _sensitiveNames = _options.SensitiveNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        _rules = _options.RedactionRules.Select(x => (x.CreateRegex(), x.Replacement)).ToArray();
    }

    /// <inheritdoc />
    public ConsoleLogLine Redact(ConsoleLogLine line)
    {
        var source = Redact(line.Source);
        var metadata = line.Metadata.ToDictionary(x => x.Key, x => RedactValue(x.Key, x.Value), StringComparer.OrdinalIgnoreCase);
        return line with { Text = RedactSensitiveText(line.Text), Source = source, Metadata = metadata };
    }

    /// <inheritdoc />
    public ConsoleLogSource Redact(ConsoleLogSource source)
    {
        var metadata = source.Metadata.ToDictionary(x => x.Key, x => RedactValue(x.Key, x.Value), StringComparer.OrdinalIgnoreCase);
        return source with
        {
            Id = RedactValue("id", source.Id),
            DisplayName = RedactValue("displayName", source.DisplayName),
            ServiceName = source.ServiceName is null ? null : RedactValue("serviceName", source.ServiceName),
            MachineName = source.MachineName is null ? null : RedactValue("machineName", source.MachineName),
            Metadata = metadata
        };
    }

    private string RedactValue(string name, string value)
    {
        if (IsSensitiveName(name))
            return _options.RedactionReplacement;

        return RedactSensitiveText(value);
    }

    private string RedactSensitiveText(string value)
    {
        var redacted = ApplyRules(value);
        var normalized = AnsiStripper.Strip(value);
        if (normalized == value)
            return redacted;

        var normalizedRedacted = ApplyRules(normalized);
        return normalizedRedacted == normalized ? redacted : normalizedRedacted;
    }

    private string ApplyRules(string value)
    {
        var current = value;
        foreach (var (regex, replacement) in _rules)
            current = ApplyRule(regex, current, replacement);

        return current;
    }

    private static string ApplyRule(Regex regex, string value, string replacement)
    {
        try
        {
            return regex.Replace(value, replacement);
        }
        catch (RegexMatchTimeoutException)
        {
            return value;
        }
    }

    private bool IsSensitiveName(string name) => _sensitiveNames.Any(sensitiveName => name.Contains(sensitiveName, StringComparison.OrdinalIgnoreCase));
}
