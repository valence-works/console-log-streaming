using System.Runtime.CompilerServices;
using ConsoleLogStreaming.Contracts;
using ConsoleLogStreaming.Core;
using Microsoft.AspNetCore.SignalR;

namespace ConsoleLogStreaming.SignalR;

/// <summary>
/// SignalR hub for live console log streaming.
/// </summary>
public sealed class ConsoleLogsHub(
    IConsoleLogProvider provider,
    IConsoleLogStreamingApiMapper mapper,
    IConsoleLogStreamingHubAuthorizer authorizer,
    ConsoleLogSubscriptionManager subscriptionManager) : Hub<IConsoleLogsClient>
{
    /// <summary>
    /// Streams matching console log items as a SignalR streaming method.
    /// </summary>
    public async IAsyncEnumerable<ConsoleLogStreamingItem> StreamAsync(
        ConsoleLogFilter? filter,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await EnsureCanReadAsync(cancellationToken).ConfigureAwait(false);
        filter = ValidateFilter(filter);

        await foreach (var item in provider.SubscribeAsync(mapper.ToCore(filter), cancellationToken).ConfigureAwait(false))
            yield return mapper.ToApi(item);
    }

    /// <summary>
    /// Starts pushing stream items to the caller through typed client methods.
    /// </summary>
    public async Task SubscribeAsync(ConsoleLogFilter? filter)
    {
        await EnsureCanReadAsync(Context.ConnectionAborted).ConfigureAwait(false);
        await subscriptionManager.SubscribeAsync(Context.ConnectionId, ValidateFilter(filter), Context.ConnectionAborted).ConfigureAwait(false);
    }

    /// <summary>
    /// Replaces the current pushed subscription filter.
    /// </summary>
    public async Task UpdateFilterAsync(ConsoleLogFilter? filter)
    {
        await EnsureCanReadAsync(Context.ConnectionAborted).ConfigureAwait(false);
        await subscriptionManager.UpdateFilterAsync(Context.ConnectionId, ValidateFilter(filter), Context.ConnectionAborted).ConfigureAwait(false);
    }

    /// <summary>
    /// Stops the current pushed subscription.
    /// </summary>
    public Task UnsubscribeAsync()
    {
        return subscriptionManager.UnsubscribeAsync(Context.ConnectionId);
    }

    /// <inheritdoc />
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await UnsubscribeAsync().ConfigureAwait(false);
        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }

    private static ConsoleLogFilter ValidateFilter(ConsoleLogFilter? filter)
    {
        filter ??= new();

        if (filter.From is { } from && filter.To is { } to && from > to)
            throw new HubException("The console log filter 'from' timestamp must be earlier than or equal to 'to'.");

        return filter;
    }

    private async ValueTask EnsureCanReadAsync(CancellationToken cancellationToken)
    {
        if (!await authorizer.CanReadAsync(Context, cancellationToken).ConfigureAwait(false))
            throw new HubException("Access denied.");
    }
}
