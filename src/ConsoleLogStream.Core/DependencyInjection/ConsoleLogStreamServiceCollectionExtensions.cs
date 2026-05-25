using ConsoleLogStream.Core.Capture;
using ConsoleLogStream.Core.Options;
using ConsoleLogStream.Core.Providers;
using ConsoleLogStream.Core.Redaction;
using ConsoleLogStream.Core.Sources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ConsoleLogStream.Core.DependencyInjection;

/// <summary>
/// Service registration extensions for the core package.
/// </summary>
public static class ConsoleLogStreamServiceCollectionExtensions
{
    /// <summary>
    /// Adds core console log capture, redaction, source tracking, and in-memory provider services.
    /// </summary>
    public static IServiceCollection AddConsoleLogStream(this IServiceCollection services, Action<ConsoleLogOptions>? configure = null)
    {
        if (configure is not null)
            services.Configure(configure);

        services.AddOptions<ConsoleLogOptions>();
        services.TryAddSingleton<IConsoleLogRedactor, RegexConsoleLogRedactor>();
        services.TryAddSingleton<IConsoleLogSourceRegistry, ConsoleLogSourceRegistry>();
        services.TryAddSingleton<InMemoryConsoleLogProvider>();
        services.TryAddSingleton<IConsoleLogProvider>(sp => sp.GetRequiredService<InMemoryConsoleLogProvider>());
        services.TryAddSingleton<IConsoleLogCapture, global::ConsoleLogStream.Core.Capture.ConsoleCaptureService>();
        return services;
    }
}
