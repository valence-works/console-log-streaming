using ConsoleLogStream.Core;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleLogStream.Persistence.Sqlite.DependencyInjection;

/// <summary>
/// Service registration extensions for SQLite persistence.
/// </summary>
public static class ConsoleLogStreamSqliteServiceCollectionExtensions
{
    /// <summary>
    /// Adds SQLite persistence as the active console log provider.
    /// </summary>
    public static IServiceCollection AddConsoleLogStreamSqlite(
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
