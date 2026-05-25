using System.Runtime.CompilerServices;
using ConsoleLogStreaming.Core;
using ConsoleLogStreaming.Core.Capture;
using ConsoleLogStreaming.Core.DependencyInjection;
using ConsoleLogStreaming.Core.Hosting;
using ConsoleLogStreaming.Core.Models;
using ConsoleLogStreaming.Core.Options;
using ConsoleLogStreaming.Core.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ConsoleLogStreaming.Tests.Core;

[Collection("Console capture")]
public class ConsoleLogStreamingHostRegistrationTests : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await ConsoleLogStreamingHost.ShutdownAsync();
        ConsoleStreamHook.Uninstall();
    }

    public async Task DisposeAsync()
    {
        await ConsoleLogStreamingHost.ShutdownAsync();
        ConsoleStreamHook.Uninstall();
    }

    [Fact]
    public async Task AddConsoleLogStreamingHost_ExposesProcessWideServices()
    {
        var services = new ServiceCollection();
        services.AddConsoleLogStreamingHost(options => options.RecentCapacity = 17);

        await using var serviceProvider = services.BuildServiceProvider();

        Assert.Equal(17, serviceProvider.GetRequiredService<IOptions<ConsoleLogOptions>>().Value.RecentCapacity);
        Assert.Same(ConsoleLogStreamingHost.Provider, serviceProvider.GetRequiredService<IConsoleLogProvider>());
        Assert.Same(ConsoleLogStreamingHost.SourceRegistry, serviceProvider.GetRequiredService<IConsoleLogSourceRegistry>());
        Assert.Same(ConsoleLogStreamingHost.RedactionPipeline, serviceProvider.GetRequiredService<IConsoleLogRedactionPipeline>());
        Assert.Same(ConsoleLogStreamingHost.Formatter, serviceProvider.GetRequiredService<ConsoleLineFormatter>());
        Assert.Same(ConsoleLogStreamingHost.Capture, serviceProvider.GetRequiredService<IConsoleLogCapture>());
        Assert.Contains(serviceProvider.GetServices<IHostedService>(), x => x.GetType() == typeof(ConsoleLogStreamingHostedService));
    }

    [Fact]
    public async Task AddConsoleLogStreamingHost_UsesCustomProviderRegistration()
    {
        var provider = new CustomProvider();
        var services = new ServiceCollection();
        services.AddSingleton<IConsoleLogProvider>(provider);
        services.AddConsoleLogStreamingHost();

        await using var serviceProvider = services.BuildServiceProvider();

        Assert.Same(provider, serviceProvider.GetRequiredService<IConsoleLogProvider>());
        Assert.Same(provider, ConsoleLogStreamingHost.Provider);
    }

    [Fact]
    public async Task AddConsoleLogStreamingHost_UsesCustomProviderRegisteredAfterHost()
    {
        var provider = new CustomProvider();
        var services = new ServiceCollection();
        services.AddConsoleLogStreamingHost();
        services.AddSingleton<IConsoleLogProvider>(provider);

        await using var serviceProvider = services.BuildServiceProvider();

        foreach (var hostedService in serviceProvider.GetServices<IHostedService>())
            await hostedService.StartAsync(CancellationToken.None);

        Assert.Same(provider, serviceProvider.GetRequiredService<IConsoleLogProvider>());
        Assert.Same(provider, ConsoleLogStreamingHost.Provider);

        foreach (var hostedService in serviceProvider.GetServices<IHostedService>())
            await hostedService.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task AddConsoleLogStreamingHost_CreatesCustomProviderWithHostOwnedDependencies()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConsoleLogProvider, ProviderWithHostDependencies>();
        services.AddConsoleLogStreamingHost();

        await using var serviceProvider = services.BuildServiceProvider();

        var provider = Assert.IsType<ProviderWithHostDependencies>(serviceProvider.GetRequiredService<IConsoleLogProvider>());

        Assert.Same(ConsoleLogStreamingHost.Options, provider.Options);
        Assert.Same(ConsoleLogStreamingHost.SourceRegistry, provider.SourceRegistry);
        Assert.Same(ConsoleLogStreamingHost.RedactionPipeline, provider.RedactionPipeline);
        Assert.Same(ConsoleLogStreamingHost.Formatter, provider.Formatter);
    }

    [Fact]
    public async Task AddConsoleLogStreamingHost_UsesCustomMetadataAccessor()
    {
        var metadataAccessor = new CustomMetadataAccessor();
        var services = new ServiceCollection();
        services.AddSingleton<IConsoleLogMetadataAccessor>(metadataAccessor);
        services.AddConsoleLogStreamingHost();

        await using var serviceProvider = services.BuildServiceProvider();

        Assert.Same(metadataAccessor, serviceProvider.GetRequiredService<IConsoleLogMetadataAccessor>());
        _ = serviceProvider.GetRequiredService<IConsoleLogCapture>();
        Assert.Same(metadataAccessor, ConsoleLogStreamingHost.MetadataAccessor);
    }

    private sealed class CustomMetadataAccessor : IConsoleLogMetadataAccessor
    {
        public IReadOnlyDictionary<string, string> GetMetadata() => new Dictionary<string, string>();
    }

    private sealed class CustomProvider : IConsoleLogProvider
    {
        public ValueTask PublishAsync(ConsoleLogLine line, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public ValueTask<RecentConsoleLogsResult> GetRecentAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new RecentConsoleLogsResult());

        public async IAsyncEnumerable<ConsoleLogStreamingItem> SubscribeAsync(ConsoleLogFilter filter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public ValueTask<IReadOnlyCollection<ConsoleLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlyCollection<ConsoleLogSource>>([]);
    }

    private sealed class ProviderWithHostDependencies(
        IOptions<ConsoleLogOptions> options,
        IConsoleLogSourceRegistry sourceRegistry,
        IConsoleLogRedactionPipeline redactionPipeline,
        ConsoleLineFormatter formatter) : IConsoleLogProvider
    {
        public IOptions<ConsoleLogOptions> Options { get; } = options;
        public IConsoleLogSourceRegistry SourceRegistry { get; } = sourceRegistry;
        public IConsoleLogRedactionPipeline RedactionPipeline { get; } = redactionPipeline;
        public ConsoleLineFormatter Formatter { get; } = formatter;

        public ValueTask PublishAsync(ConsoleLogLine line, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public ValueTask<RecentConsoleLogsResult> GetRecentAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new RecentConsoleLogsResult());

        public async IAsyncEnumerable<ConsoleLogStreamingItem> SubscribeAsync(ConsoleLogFilter filter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public ValueTask<IReadOnlyCollection<ConsoleLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlyCollection<ConsoleLogSource>>([]);
    }
}
