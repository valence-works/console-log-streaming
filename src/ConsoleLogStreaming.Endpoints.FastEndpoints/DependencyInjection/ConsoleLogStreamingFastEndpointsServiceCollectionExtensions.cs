using ConsoleLogStreaming.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ConsoleLogStreaming.Endpoints.FastEndpoints.DependencyInjection;

/// <summary>
/// Service registration extensions for ConsoleLogStreaming FastEndpoints.
/// </summary>
public static class ConsoleLogStreamingFastEndpointsServiceCollectionExtensions
{
    /// <summary>
    /// Adds ConsoleLogStreaming FastEndpoints services.
    /// </summary>
    public static IServiceCollection AddConsoleLogStreamingFastEndpoints(
        this IServiceCollection services,
        Action<ConsoleLogStreamingFastEndpointsOptions>? configure = null)
    {
        if (configure != null)
            services.Configure(configure);

        services.AddOptions<ConsoleLogStreamingFastEndpointsOptions>();
        services.TryAddSingleton<IConsoleLogStreamingApiMapper, ConsoleLogStreamingApiMapper>();
        return services;
    }
}
