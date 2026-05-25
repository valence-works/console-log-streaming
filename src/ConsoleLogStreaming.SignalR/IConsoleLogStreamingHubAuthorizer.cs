using Microsoft.AspNetCore.SignalR;

namespace ConsoleLogStreaming.SignalR;

/// <summary>
/// Authorizes SignalR hub calls.
/// </summary>
public interface IConsoleLogStreamingHubAuthorizer
{
    /// <summary>
    /// Returns true when the caller may read console logs.
    /// </summary>
    ValueTask<bool> CanReadAsync(HubCallerContext context, CancellationToken cancellationToken = default);
}
