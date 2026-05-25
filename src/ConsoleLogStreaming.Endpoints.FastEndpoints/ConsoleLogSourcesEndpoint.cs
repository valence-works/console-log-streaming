using ConsoleLogStreaming.Contracts;
using ConsoleLogStreaming.Core;
using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;

namespace ConsoleLogStreaming.Endpoints.FastEndpoints;

/// <summary>
/// FastEndpoint returning known console log sources.
/// </summary>
public sealed class ConsoleLogSourcesEndpoint(
    IConsoleLogProvider provider,
    IConsoleLogStreamingApiMapper mapper,
    IOptions<ConsoleLogStreamingFastEndpointsOptions> options) : EndpointWithoutRequest<IReadOnlyCollection<ConsoleLogSource>>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get(options.Value.SourcesPath);

        if (!string.IsNullOrWhiteSpace(options.Value.AuthorizationPolicy))
            Options(x => x.RequireAuthorization(options.Value.AuthorizationPolicy));

        options.Value.ConfigureSourcesEndpoint?.Invoke(Definition);
    }

    /// <inheritdoc />
    public override async Task<IReadOnlyCollection<ConsoleLogSource>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var sources = await provider.ListSourcesAsync(cancellationToken).ConfigureAwait(false);
        return sources.Select(mapper.ToApi).ToList();
    }
}
