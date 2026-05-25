using Microsoft.AspNetCore.Builder;

namespace ConsoleLogStreaming.Endpoints.MinimalApi;

/// <summary>
/// Minimal API endpoint options.
/// </summary>
public sealed class ConsoleLogStreamingMinimalApiOptions
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
    /// Optional endpoint convention applied to the recent endpoint.
    /// </summary>
    public Action<RouteHandlerBuilder>? ConfigureRecentEndpoint { get; set; }

    /// <summary>
    /// Optional endpoint convention applied to the sources endpoint.
    /// </summary>
    public Action<RouteHandlerBuilder>? ConfigureSourcesEndpoint { get; set; }
}
