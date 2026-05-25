namespace ConsoleLogStreaming.Core.Models;

/// <summary>
/// Criteria applied to recent queries and live subscriptions.
/// </summary>
public sealed record ConsoleLogFilter
{
    /// <summary>
    /// Optional exact source identifier.
    /// </summary>
    public string? SourceId { get; init; }

    /// <summary>
    /// Optional stream filter.
    /// </summary>
    public ConsoleStream? Stream { get; init; }

    /// <summary>
    /// Optional case-insensitive text query.
    /// </summary>
    public string? Query { get; init; }

    /// <summary>
    /// Optional exact line metadata filters. All provided key/value pairs must match.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Inclusive lower bound for provider receive time.
    /// </summary>
    public DateTimeOffset? From { get; init; }

    /// <summary>
    /// Inclusive upper bound for provider receive time.
    /// </summary>
    public DateTimeOffset? To { get; init; }

    /// <summary>
    /// Requested maximum result count.
    /// </summary>
    public int? Limit { get; init; }
}
