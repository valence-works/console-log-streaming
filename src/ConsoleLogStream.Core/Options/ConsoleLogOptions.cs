namespace ConsoleLogStream.Core.Options;

/// <summary>
/// Core console log capture and provider options.
/// </summary>
public sealed class ConsoleLogOptions
{
    /// <summary>
    /// Number of recent lines retained in memory.
    /// </summary>
    public int RecentCapacity { get; set; } = 1000;

    /// <summary>
    /// Per-subscriber live queue capacity.
    /// </summary>
    public int SubscriberCapacity { get; set; } = 256;

    /// <summary>
    /// Maximum number of lines returned by recent queries.
    /// </summary>
    public int MaxRecentQuerySize { get; set; } = 500;

    /// <summary>
    /// Maximum line length before truncation.
    /// </summary>
    public int MaxLineLength { get; set; } = 16 * 1024;

    /// <summary>
    /// Flush partial lines after this idle period.
    /// </summary>
    public TimeSpan IdleFlushTimeout { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Preserve ANSI escape sequences instead of stripping them.
    /// </summary>
    public bool PreserveAnsi { get; set; }

    /// <summary>
    /// Stable source identifier. Defaults to machine/process.
    /// </summary>
    public string? SourceId { get; set; }

    /// <summary>
    /// Source display name. Defaults to source ID.
    /// </summary>
    public string? SourceDisplayName { get; set; }

    /// <summary>
    /// Optional service name.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Optional source metadata.
    /// </summary>
    public Dictionary<string, string> SourceMetadata { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Regular expression redaction rules.
    /// </summary>
    public List<ConsoleLogRedactionRule> RedactionRules { get; } =
    [
        new()
        {
            Name = "Bearer token",
            Pattern = @"bearer\s+[a-z0-9._~+/=-]+",
            Replacement = "Bearer [redacted]"
        },
        new()
        {
            Name = "Named secret",
            Pattern = @"(?<name>(password|passwd|pwd|secret|api[-_ ]?key|token|authorization|cookie|connectionstring|connection string)\s*[:=]\s*)(?<value>[^\s;]+)",
            Replacement = "${name}[redacted]"
        },
        new()
        {
            Name = "Shared access signature",
            Pattern = @"(?<name>(sig|signature)=)(?<value>[^&\s]+)",
            Replacement = "${name}[redacted]"
        }
    ];
}
