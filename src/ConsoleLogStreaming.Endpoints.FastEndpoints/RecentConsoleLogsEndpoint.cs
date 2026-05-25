using ConsoleLogStreaming.Contracts;
using ConsoleLogStreaming.Core;
using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;

namespace ConsoleLogStreaming.Endpoints.FastEndpoints;

/// <summary>
/// FastEndpoint returning recent console logs.
/// </summary>
public sealed class RecentConsoleLogsEndpoint(
    IConsoleLogProvider provider,
    IConsoleLogStreamingApiMapper mapper,
    IOptions<ConsoleLogStreamingFastEndpointsOptions> options) : Endpoint<ConsoleLogFilter, RecentConsoleLogsResult>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post(options.Value.RecentPath);

        if (!string.IsNullOrWhiteSpace(options.Value.AuthorizationPolicy))
            Options(x => x.RequireAuthorization(options.Value.AuthorizationPolicy));

        options.Value.ConfigureRecentEndpoint?.Invoke(Definition);
    }

    /// <inheritdoc />
    public override async Task<RecentConsoleLogsResult> ExecuteAsync(ConsoleLogFilter request, CancellationToken cancellationToken)
    {
        var result = await provider.GetRecentAsync(mapper.ToCore(request), cancellationToken).ConfigureAwait(false);
        return mapper.ToApi(result);
    }
}
