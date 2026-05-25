namespace ConsoleLogStreaming.Contracts;

/// <summary>
/// Console log source DTO.
/// </summary>
public sealed record ConsoleLogSource
{
    /// <summary>
    /// Stable source identifier.
    /// </summary>
    public string Id { get; init; } = "";

    /// <summary>
    /// Human-readable source name.
    /// </summary>
    public string DisplayName { get; init; } = "";

    /// <summary>
    /// Optional service or application name.
    /// </summary>
    public string? ServiceName { get; init; }

    /// <summary>
    /// Process identifier when available.
    /// </summary>
    public int? ProcessId { get; init; }

    /// <summary>
    /// Machine or host name.
    /// </summary>
    public string? MachineName { get; init; }

    /// <summary>
    /// Kubernetes pod name when available.
    /// </summary>
    public string? PodName { get; init; }

    /// <summary>
    /// Container name when available.
    /// </summary>
    public string? ContainerName { get; init; }

    /// <summary>
    /// Kubernetes namespace when available.
    /// </summary>
    public string? Namespace { get; init; }

    /// <summary>
    /// Kubernetes node name when available.
    /// </summary>
    public string? NodeName { get; init; }

    /// <summary>
    /// Process start timestamp when available.
    /// </summary>
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>
    /// Most recent line timestamp observed for this source.
    /// </summary>
    public DateTimeOffset? LastSeen { get; init; }

    /// <summary>
    /// Source health.
    /// </summary>
    public ConsoleLogSourceHealth Health { get; init; } = ConsoleLogSourceHealth.Unknown;

    /// <summary>
    /// Optional redacted metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
