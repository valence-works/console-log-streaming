using ConsoleLogStreaming.Core;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleLogStreaming.Persistence.Sqlite.DependencyInjection;

/// <summary>
/// Service registration extensions for SQLite persistence.
/// </summary>
public static class ConsoleLogStreamingSqliteServiceCollectionExtensions
{
    /// <summary>
    /// Adds SQLite persistence as the active console log provider.
    /// </summary>
    public static IServiceCollection AddConsoleLogStreamingSqlite(
        this IServiceCollection services,
        Action<SqliteConsoleLogOptions>? configure = null)
    {
        if (configure is not null)
            services.Configure(configure);

        services.AddOptions<SqliteConsoleLogOptions>();
        services.AddSingleton<SqliteConsoleLogProvider>();
        services.AddSingleton<IConsoleLogProvider>(sp => sp.GetRequiredService<SqliteConsoleLogProvider>());
        return services;
    }
}
