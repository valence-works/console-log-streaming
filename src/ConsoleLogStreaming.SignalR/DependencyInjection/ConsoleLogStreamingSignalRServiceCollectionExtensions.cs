using ConsoleLogStreaming.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ConsoleLogStreaming.SignalR.DependencyInjection;

/// <summary>
/// Service registration extensions for ConsoleLogStreaming SignalR.
/// </summary>
public static class ConsoleLogStreamingSignalRServiceCollectionExtensions
{
    /// <summary>
    /// Adds ConsoleLogStreaming SignalR services.
    /// </summary>
    public static IServiceCollection AddConsoleLogStreamingSignalR(
        this IServiceCollection services,
        Action<ConsoleLogStreamingSignalROptions>? configure = null)
    {
        if (configure != null)
            services.Configure(configure);

        services.AddOptions<ConsoleLogStreamingSignalROptions>();
        services.TryAddSingleton<IConsoleLogStreamingApiMapper, ConsoleLogStreamingApiMapper>();
        services.TryAddSingleton<IConsoleLogStreamingHubAuthorizer, DefaultConsoleLogStreamingHubAuthorizer>();
        services.TryAddSingleton<ConsoleLogSubscriptionManager>();
        services.AddSignalR();
        return services;
    }
}
