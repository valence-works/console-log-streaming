using ConsoleLogStreaming.Contracts;
using ConsoleLogStreaming.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ConsoleLogStreaming.Endpoints.MinimalApi.DependencyInjection;

/// <summary>
/// Endpoint mapping extensions for ConsoleLogStreaming Minimal API endpoints.
/// </summary>
public static class ConsoleLogStreamingMinimalApiEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps recent console logs and sources endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapConsoleLogStreamingMinimalApi(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<ConsoleLogStreamingMinimalApiOptions>>().Value;

        var recent = endpoints.MapPost(options.RecentPath, async (
            ConsoleLogFilter filter,
            IConsoleLogProvider provider,
            IConsoleLogStreamingApiMapper mapper,
            CancellationToken cancellationToken) =>
        {
            var result = await provider.GetRecentAsync(mapper.ToCore(filter), cancellationToken).ConfigureAwait(false);
            return Results.Ok(mapper.ToApi(result));
        });

        var sources = endpoints.MapGet(options.SourcesPath, async (
            IConsoleLogProvider provider,
            IConsoleLogStreamingApiMapper mapper,
            CancellationToken cancellationToken) =>
        {
            var sources = await provider.ListSourcesAsync(cancellationToken).ConfigureAwait(false);
            return Results.Ok(sources.Select(mapper.ToApi).ToArray());
        });

        if (!string.IsNullOrWhiteSpace(options.AuthorizationPolicy))
        {
            recent.RequireAuthorization(options.AuthorizationPolicy);
            sources.RequireAuthorization(options.AuthorizationPolicy);
        }

        options.ConfigureRecentEndpoint?.Invoke(recent);
        options.ConfigureSourcesEndpoint?.Invoke(sources);

        return endpoints;
    }
}
