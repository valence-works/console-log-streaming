using Microsoft.Extensions.Hosting;

namespace ConsoleLogStreaming.Core.Hosting;

/// <summary>
/// Keeps the process-wide console log stream host alive and flushes idle captured lines.
/// </summary>
public sealed class ConsoleLogStreamingHostedService(ConsoleLogStreamingHostRegistration registration, IServiceProvider serviceProvider) : BackgroundService
{
    /// <inheritdoc />
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        registration.Configure(serviceProvider);
        ConsoleLogStreamingHost.AddReference();
        ConsoleLogStreamingHost.EnsureInitialized();
        return base.StartAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
        await ConsoleLogStreamingHost.ReleaseReferenceAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMilliseconds(Math.Max(100, ConsoleLogStreamingHost.Options.Value.IdleFlushTimeout.TotalMilliseconds / 2));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
                await ConsoleLogStreamingHost.Capture.FlushIdleAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
