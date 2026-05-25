using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using ConsoleLogStream.Core;
using ConsoleLogStream.Core.Models;
using Microsoft.AspNetCore.SignalR;

namespace ConsoleLogStream.AspNetCore.Realtime;

/// <summary>
/// SignalR hub for live console log streaming.
/// </summary>
public sealed class ConsoleLogsHub : Hub
{
    private static readonly ConcurrentDictionary<string, CancellationTokenSource> Subscriptions = new();
    private readonly IConsoleLogProvider _provider;

    /// <summary>
    /// Initializes a new instance of the hub.
    /// </summary>
    public ConsoleLogsHub(IConsoleLogProvider provider)
    {
        _provider = provider;
    }

    /// <summary>
    /// Streams matching console log items as a SignalR streaming method.
    /// </summary>
    public async IAsyncEnumerable<ConsoleLogStreamItem> Stream(
        ConsoleLogFilter filter,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var item in _provider.SubscribeAsync(filter, cancellationToken).ConfigureAwait(false))
            yield return item;
    }

    /// <summary>
    /// Starts pushing stream items to the caller through the `ConsoleLogStreamItem` client method.
    /// </summary>
    public Task Subscribe(ConsoleLogFilter filter)
    {
        StartPushing(filter);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Replaces the current pushed subscription filter.
    /// </summary>
    public Task UpdateFilter(ConsoleLogFilter filter)
    {
        CancelCurrentSubscription();
        StartPushing(filter);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the current pushed subscription.
    /// </summary>
    public Task Unsubscribe()
    {
        CancelCurrentSubscription();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        CancelCurrentSubscription();
        return base.OnDisconnectedAsync(exception);
    }

    private void StartPushing(ConsoleLogFilter filter)
    {
        var cts = new CancellationTokenSource();
        Subscriptions[Context.ConnectionId] = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (var item in _provider.SubscribeAsync(filter, cts.Token).ConfigureAwait(false))
                    await Clients.Caller.SendAsync("ConsoleLogStreamItem", item, cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }, cts.Token);
    }

    private void CancelCurrentSubscription()
    {
        if (!Subscriptions.TryRemove(Context.ConnectionId, out var cts))
            return;

        cts.Cancel();
        cts.Dispose();
    }
}
