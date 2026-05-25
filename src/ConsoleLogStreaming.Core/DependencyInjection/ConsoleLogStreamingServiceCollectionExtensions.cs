using ConsoleLogStreaming.Core.Capture;
using ConsoleLogStreaming.Core.Options;
using ConsoleLogStreaming.Core.Providers;
using ConsoleLogStreaming.Core.Redaction;
using ConsoleLogStreaming.Core.Sources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ConsoleLogStreaming.Core.DependencyInjection;

/// <summary>
/// Service registration extensions for the core package.
/// </summary>
public static class ConsoleLogStreamingServiceCollectionExtensions
{
    /// <summary>
    /// Adds core console log capture, redaction, source tracking, and in-memory provider services.
    /// </summary>
    public static IServiceCollection AddConsoleLogStreaming(this IServiceCollection services, Action<ConsoleLogOptions>? configure = null)
    {
        if (configure is not null)
            services.Configure(configure);

        services.AddOptions<ConsoleLogOptions>();
        services.TryAddSingleton<ConsoleLineFormatter>();
        services.TryAddSingleton<IConsoleLogMetadataAccessor>(_ => EmptyConsoleLogMetadataAccessor.Instance);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConsoleLogRedactor, RegexConsoleLogRedactor>());
        services.TryAddSingleton<IConsoleLogRedactionPipeline, ConsoleLogRedactionPipeline>();
        services.TryAddSingleton<IConsoleLogSourceRegistry, ConsoleLogSourceRegistry>();
        services.TryAddSingleton<InMemoryConsoleLogProvider>();
        services.TryAddSingleton<IConsoleLogProvider>(sp => sp.GetRequiredService<InMemoryConsoleLogProvider>());
        services.TryAddSingleton<IConsoleLogCapture, global::ConsoleLogStreaming.Core.Capture.ConsoleCaptureService>();
        return services;
    }
}
