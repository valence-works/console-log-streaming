using ConsoleLogStreaming.Core.Capture;
using ConsoleLogStreaming.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ConsoleLogStreaming.Core.Hosting;

/// <summary>
/// Bridges dependency injection to the process-wide <see cref="ConsoleLogStreamingHost"/>.
/// </summary>
public sealed class ConsoleLogStreamingHostRegistration
{
    private readonly IServiceCollection _services;
    private readonly object _lock = new();
    private bool _configured;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleLogStreamingHostRegistration"/> class.
    /// </summary>
    public ConsoleLogStreamingHostRegistration(IServiceCollection services)
    {
        _services = services;
    }

    internal ServiceDescriptor? HostProviderDescriptor { get; set; }

    /// <summary>
    /// Gets the host options.
    /// </summary>
    public IOptions<ConsoleLogOptions> GetOptions(IServiceProvider serviceProvider)
    {
        Configure(serviceProvider);
        return ConsoleLogStreamingHost.Options;
    }

    /// <summary>
    /// Gets the host source registry.
    /// </summary>
    public IConsoleLogSourceRegistry GetSourceRegistry(IServiceProvider serviceProvider)
    {
        Configure(serviceProvider);
        return ConsoleLogStreamingHost.SourceRegistry;
    }

    /// <summary>
    /// Gets the host redaction pipeline.
    /// </summary>
    public IConsoleLogRedactionPipeline GetRedactionPipeline(IServiceProvider serviceProvider)
    {
        Configure(serviceProvider);
        return ConsoleLogStreamingHost.RedactionPipeline;
    }

    /// <summary>
    /// Gets the host line formatter.
    /// </summary>
    public ConsoleLineFormatter GetFormatter(IServiceProvider serviceProvider)
    {
        Configure(serviceProvider);
        return ConsoleLogStreamingHost.Formatter;
    }

    /// <summary>
    /// Gets the host provider.
    /// </summary>
    public IConsoleLogProvider GetProvider(IServiceProvider serviceProvider)
    {
        Configure(serviceProvider);
        return ConsoleLogStreamingHost.Provider;
    }

    /// <summary>
    /// Gets the host capture service.
    /// </summary>
    public IConsoleLogCapture GetCapture(IServiceProvider serviceProvider)
    {
        Configure(serviceProvider);
        return ConsoleLogStreamingHost.Capture;
    }

    /// <summary>
    /// Applies dependency-injection configuration to the process-wide host before first use.
    /// </summary>
    public void Configure(IServiceProvider serviceProvider)
    {
        lock (_lock)
        {
            if (_configured)
                return;

            ConsoleLogStreamingHost.Configure(options =>
            {
                foreach (var configureOptions in serviceProvider.GetServices<IConfigureOptions<ConsoleLogOptions>>())
                    configureOptions.Configure(options);

                foreach (var postConfigureOptions in serviceProvider.GetServices<IPostConfigureOptions<ConsoleLogOptions>>())
                    postConfigureOptions.PostConfigure(Microsoft.Extensions.Options.Options.DefaultName, options);
            });

            ConsoleLogStreamingHost.ConfigureMetadataAccessor(_ => serviceProvider.GetRequiredService<IConsoleLogMetadataAccessor>());

            var providerDescriptor = FindCustomProviderDescriptor();
            if (providerDescriptor is not null)
                ConsoleLogStreamingHost.ConfigureProvider(context => ResolveCustomProvider(providerDescriptor, serviceProvider, context));

            _configured = true;
        }
    }

    private ServiceDescriptor? FindCustomProviderDescriptor() =>
        _services
            .Where(x => x.ServiceType == typeof(IConsoleLogProvider) && !ReferenceEquals(x, HostProviderDescriptor))
            .LastOrDefault();

    private IConsoleLogProvider ResolveCustomProvider(ServiceDescriptor descriptor, IServiceProvider serviceProvider, ConsoleLogStreamingHostContext context)
    {
        if (descriptor.ImplementationInstance is IConsoleLogProvider provider)
            return provider;

        var contextServiceProvider = new ConsoleLogStreamingHostContextServiceProvider(_services, serviceProvider, context, HostProviderDescriptor);

        if (descriptor.ImplementationFactory is not null)
            return (IConsoleLogProvider)descriptor.ImplementationFactory(contextServiceProvider)!;

        if (descriptor.ImplementationType is not null)
            return (IConsoleLogProvider)ActivatorUtilities.CreateInstance(contextServiceProvider, descriptor.ImplementationType);

        throw new InvalidOperationException("The custom console log provider registration is invalid.");
    }

    private sealed class ConsoleLogStreamingHostContextServiceProvider(
        IServiceCollection services,
        IServiceProvider inner,
        ConsoleLogStreamingHostContext context,
        ServiceDescriptor? hostProviderDescriptor) : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IOptions<ConsoleLogOptions>))
                return context.Options;

            if (serviceType == typeof(IConsoleLogSourceRegistry))
                return context.SourceRegistry;

            if (serviceType == typeof(IConsoleLogRedactionPipeline))
                return context.RedactionPipeline;

            if (serviceType == typeof(ConsoleLineFormatter))
                return context.Formatter;

            if (serviceType == typeof(IConsoleLogMetadataAccessor))
                return context.MetadataAccessor;

            var descriptor = services
                .Where(x => x.ServiceType == serviceType && !ReferenceEquals(x, hostProviderDescriptor))
                .LastOrDefault();

            if (descriptor is null)
                return inner.GetService(serviceType);

            if (descriptor.ImplementationInstance is not null)
                return descriptor.ImplementationInstance;

            if (descriptor.ImplementationFactory is not null)
                return descriptor.ImplementationFactory(this);

            if (descriptor.ImplementationType is not null)
                return ActivatorUtilities.CreateInstance(this, descriptor.ImplementationType);

            return inner.GetService(serviceType);
        }
    }
}
