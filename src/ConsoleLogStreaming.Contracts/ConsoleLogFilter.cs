namespace ConsoleLogStreaming.Contracts;

/// <summary>
/// DTO filter used by HTTP and realtime transports.
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
    public ConsoleLogStreaming? Stream { get; init; }

    /// <summary>
    /// Optional case-insensitive text query.
    /// </summary>
    public string? Query { get; init; }

    /// <summary>
    /// Optional exact metadata filters. All provided key/value pairs must match.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

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
