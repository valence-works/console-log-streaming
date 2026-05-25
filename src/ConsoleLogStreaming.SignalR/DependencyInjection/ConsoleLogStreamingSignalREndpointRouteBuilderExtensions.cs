using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ConsoleLogStreaming.SignalR.DependencyInjection;

/// <summary>
/// Endpoint mapping extensions for ConsoleLogStreaming SignalR.
/// </summary>
public static class ConsoleLogStreamingSignalREndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the ConsoleLogStreaming SignalR hub.
    /// </summary>
    public static IEndpointRouteBuilder MapConsoleLogStreamingSignalR(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<ConsoleLogStreamingSignalROptions>>().Value;
        var hub = endpoints.MapHub<ConsoleLogsHub>(options.HubPath);

        if (!string.IsNullOrWhiteSpace(options.AuthorizationPolicy))
            hub.RequireAuthorization(options.AuthorizationPolicy);

        options.ConfigureHubEndpoint?.Invoke(hub);
        return endpoints;
    }
}
