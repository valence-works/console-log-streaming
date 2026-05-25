using ConsoleLogStreaming.Core.Capture;
using ConsoleLogStreaming.Core.Hosting;
using ConsoleLogStreaming.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ConsoleLogStreaming.Core.DependencyInjection;

/// <summary>
/// Service registration extensions for the process-wide console log stream host.
/// </summary>
public static class ConsoleLogStreamingHostServiceCollectionExtensions
{
    /// <summary>
    /// Adds services backed by the process-wide <see cref="ConsoleLogStreamingHost"/>.
    /// </summary>
    public static IServiceCollection AddConsoleLogStreamingHost(this IServiceCollection services, Action<ConsoleLogOptions>? configure = null)
    {
        ConsoleStreamHook.Install();

        if (configure is not null)
            services.Configure(configure);

        services.AddOptions<ConsoleLogOptions>();

        var registration = new ConsoleLogStreamingHostRegistration(services);
        services.TryAddSingleton(registration);
        services.TryAddSingleton<IConsoleLogMetadataAccessor>(_ => EmptyConsoleLogMetadataAccessor.Instance);

        services.AddSingleton<IOptions<ConsoleLogOptions>>(sp => registration.GetOptions(sp));
        services.AddSingleton<IConsoleLogSourceRegistry>(sp => registration.GetSourceRegistry(sp));
        services.AddSingleton<IConsoleLogRedactionPipeline>(sp => registration.GetRedactionPipeline(sp));
        services.AddSingleton<ConsoleLineFormatter>(sp => registration.GetFormatter(sp));

        var providerDescriptor = ServiceDescriptor.Singleton<IConsoleLogProvider>(sp => registration.GetProvider(sp));
        registration.HostProviderDescriptor = providerDescriptor;
        services.Add(providerDescriptor);

        services.AddSingleton<IConsoleLogCapture>(sp => registration.GetCapture(sp));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ConsoleLogStreamingHostedService>());

        return services;
    }
}
