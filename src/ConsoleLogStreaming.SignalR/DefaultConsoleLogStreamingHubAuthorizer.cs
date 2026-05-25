using Microsoft.AspNetCore.SignalR;

namespace ConsoleLogStreaming.SignalR;

/// <summary>
/// Default hub authorizer that allows calls. Endpoint-level authorization can still be applied while mapping the hub.
/// </summary>
public sealed class DefaultConsoleLogStreamingHubAuthorizer : IConsoleLogStreamingHubAuthorizer
{
    /// <inheritdoc />
    public ValueTask<bool> CanReadAsync(HubCallerContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(true);
    }
}
