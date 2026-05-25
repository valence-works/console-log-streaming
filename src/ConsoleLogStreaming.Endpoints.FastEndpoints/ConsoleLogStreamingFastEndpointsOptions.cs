using FastEndpoints;

namespace ConsoleLogStreaming.Endpoints.FastEndpoints;

/// <summary>
/// FastEndpoints endpoint options.
/// </summary>
public sealed class ConsoleLogStreamingFastEndpointsOptions
{
    /// <summary>
    /// Recent console logs endpoint path.
    /// </summary>
    public string RecentPath { get; set; } = "/diagnostics/console-logs/recent";

    /// <summary>
    /// Console sources endpoint path.
    /// </summary>
    public string SourcesPath { get; set; } = "/diagnostics/console-logs/sources";

    /// <summary>
    /// Optional authorization policy applied to both endpoints.
    /// </summary>
    public string? AuthorizationPolicy { get; set; }

    /// <summary>
    /// Optional FastEndpoints configuration for the recent endpoint.
    /// </summary>
    public Action<EndpointDefinition>? ConfigureRecentEndpoint { get; set; }

    /// <summary>
    /// Optional FastEndpoints configuration for the sources endpoint.
    /// </summary>
    public Action<EndpointDefinition>? ConfigureSourcesEndpoint { get; set; }
}
