using ConsoleLogStreaming.Endpoints.MinimalApi.DependencyInjection;
using ConsoleLogStreaming.SignalR.DependencyInjection;
using Microsoft.AspNetCore.Routing;

namespace ConsoleLogStreaming.AspNetCore.DependencyInjection;

/// <summary>
/// Endpoint mapping extensions for the ASP.NET Core adapter.
/// </summary>
public static class ConsoleLogStreamingEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps recent/source endpoints and the live SignalR hub.
    /// </summary>
    public static IEndpointRouteBuilder MapConsoleLogStreaming(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapConsoleLogStreamingMinimalApi();
        endpoints.MapConsoleLogStreamingSignalR();
        return endpoints;
    }
}
