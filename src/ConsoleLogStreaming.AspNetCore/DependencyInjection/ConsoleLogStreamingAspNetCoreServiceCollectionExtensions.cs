using ConsoleLogStreaming.Endpoints.MinimalApi.DependencyInjection;
using ConsoleLogStreaming.SignalR.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ConsoleLogStreaming.AspNetCore.DependencyInjection;

/// <summary>
/// Service registration extensions for the ASP.NET Core adapter.
/// </summary>
public static class ConsoleLogStreamingAspNetCoreServiceCollectionExtensions
{
    /// <summary>
    /// Adds SignalR and ASP.NET Core console log stream options.
    /// </summary>
    public static IServiceCollection AddConsoleLogStreamingAspNetCore(
        this IServiceCollection services,
        Action<ConsoleLogStreamingAspNetCoreOptions>? configure = null)
    {
        if (configure is not null)
            services.Configure(configure);

        services.AddOptions<ConsoleLogStreamingAspNetCoreOptions>();
        services.AddConsoleLogStreamingMinimalApi();
        services.AddConsoleLogStreamingSignalR();
        services.AddSingleton<IConfigureOptions<ConsoleLogStreaming.Endpoints.MinimalApi.ConsoleLogStreamingMinimalApiOptions>, ConfigureMinimalApiOptions>();
        services.AddSingleton<IConfigureOptions<ConsoleLogStreaming.SignalR.ConsoleLogStreamingSignalROptions>, ConfigureSignalROptions>();
        return services;
    }

    private sealed class ConfigureMinimalApiOptions(IOptions<ConsoleLogStreamingAspNetCoreOptions> source) :
        IConfigureOptions<ConsoleLogStreaming.Endpoints.MinimalApi.ConsoleLogStreamingMinimalApiOptions>
    {
        public void Configure(ConsoleLogStreaming.Endpoints.MinimalApi.ConsoleLogStreamingMinimalApiOptions options)
        {
            options.RecentPath = source.Value.RecentPath;
            options.SourcesPath = source.Value.SourcesPath;
            options.AuthorizationPolicy = source.Value.AuthorizationPolicy;
        }
    }

    private sealed class ConfigureSignalROptions(IOptions<ConsoleLogStreamingAspNetCoreOptions> source) :
        IConfigureOptions<ConsoleLogStreaming.SignalR.ConsoleLogStreamingSignalROptions>
    {
        public void Configure(ConsoleLogStreaming.SignalR.ConsoleLogStreamingSignalROptions options)
        {
            options.HubPath = source.Value.HubPath;
            options.AuthorizationPolicy = source.Value.AuthorizationPolicy;
        }
    }
}
