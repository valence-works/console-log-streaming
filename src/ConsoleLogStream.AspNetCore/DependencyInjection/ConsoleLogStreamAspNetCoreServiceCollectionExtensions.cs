using Microsoft.Extensions.DependencyInjection;

namespace ConsoleLogStream.AspNetCore.DependencyInjection;

/// <summary>
/// Service registration extensions for the ASP.NET Core adapter.
/// </summary>
public static class ConsoleLogStreamAspNetCoreServiceCollectionExtensions
{
    /// <summary>
    /// Adds SignalR and ASP.NET Core console log stream options.
    /// </summary>
    public static IServiceCollection AddConsoleLogStreamAspNetCore(
        this IServiceCollection services,
        Action<ConsoleLogStreamAspNetCoreOptions>? configure = null)
    {
        if (configure is not null)
            services.Configure(configure);

        services.AddOptions<ConsoleLogStreamAspNetCoreOptions>();
        services.AddSignalR();
        return services;
    }
}
