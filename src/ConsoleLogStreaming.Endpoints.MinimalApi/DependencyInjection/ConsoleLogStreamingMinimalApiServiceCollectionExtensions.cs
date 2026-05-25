using ConsoleLogStreaming.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ConsoleLogStreaming.Endpoints.MinimalApi.DependencyInjection;

/// <summary>
/// Service registration extensions for ConsoleLogStreaming Minimal API endpoints.
/// </summary>
public static class ConsoleLogStreamingMinimalApiServiceCollectionExtensions
{
    /// <summary>
    /// Adds ConsoleLogStreaming Minimal API endpoint services.
    /// </summary>
    public static IServiceCollection AddConsoleLogStreamingMinimalApi(
        this IServiceCollection services,
        Action<ConsoleLogStreamingMinimalApiOptions>? configure = null)
    {
        if (configure != null)
            services.Configure(configure);

        services.AddOptions<ConsoleLogStreamingMinimalApiOptions>();
        services.TryAddSingleton<IConsoleLogStreamingApiMapper, ConsoleLogStreamingApiMapper>();
        return services;
    }
}
