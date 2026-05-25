using Microsoft.AspNetCore.Builder;

namespace ConsoleLogStreaming.SignalR;

/// <summary>
/// SignalR transport options.
/// </summary>
public sealed class ConsoleLogStreamingSignalROptions
{
    /// <summary>
    /// SignalR hub path.
    /// </summary>
    public string HubPath { get; set; } = "/hubs/console-logs";

    /// <summary>
    /// Optional authorization policy applied to the hub endpoint.
    /// </summary>
    public string? AuthorizationPolicy { get; set; }

    /// <summary>
    /// Optional endpoint convention applied to the hub endpoint.
    /// </summary>
    public Action<HubEndpointConventionBuilder>? ConfigureHubEndpoint { get; set; }
}
