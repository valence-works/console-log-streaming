namespace ConsoleLogStreaming.Core.Models;

/// <summary>
/// Describes the application process or host source that produced console output.
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
    /// Most recent line timestamp observed for this source.
    /// </summary>
    public DateTimeOffset? LastSeen { get; init; }

    /// <summary>
    /// Source health.
    /// </summary>
    public ConsoleLogSourceHealth Health { get; init; } = ConsoleLogSourceHealth.Connected;

    /// <summary>
    /// Optional redacted metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
