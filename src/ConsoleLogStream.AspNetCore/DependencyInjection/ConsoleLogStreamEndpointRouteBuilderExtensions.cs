using ConsoleLogStream.AspNetCore.Realtime;
using ConsoleLogStream.Core;
using ConsoleLogStream.Core.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ConsoleLogStream.AspNetCore.DependencyInjection;

/// <summary>
/// Endpoint mapping extensions for the ASP.NET Core adapter.
/// </summary>
public static class ConsoleLogStreamEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps recent/source endpoints and the live SignalR hub.
    /// </summary>
    public static IEndpointRouteBuilder MapConsoleLogStream(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<ConsoleLogStreamAspNetCoreOptions>>().Value;

        var recent = endpoints.MapGet(options.RecentPath, async (
            string? sourceId,
            ConsoleStream? stream,
            string? query,
            DateTimeOffset? from,
            DateTimeOffset? to,
            int? limit,
            IConsoleLogProvider provider,
            CancellationToken cancellationToken) =>
        {
            var filter = new ConsoleLogFilter
            {
                SourceId = sourceId,
                Stream = stream,
                Query = query,
                From = from,
                To = to,
                Limit = limit
            };
            return Results.Ok(await provider.GetRecentAsync(filter, cancellationToken).ConfigureAwait(false));
        });

        var sources = endpoints.MapGet(options.SourcesPath, async (
            IConsoleLogProvider provider,
            CancellationToken cancellationToken) =>
        {
            return Results.Ok(await provider.ListSourcesAsync(cancellationToken).ConfigureAwait(false));
        });

        var hub = endpoints.MapHub<ConsoleLogsHub>(options.HubPath);

        if (!string.IsNullOrWhiteSpace(options.AuthorizationPolicy))
        {
            recent.RequireAuthorization(options.AuthorizationPolicy);
            sources.RequireAuthorization(options.AuthorizationPolicy);
            hub.RequireAuthorization(options.AuthorizationPolicy);
        }

        return endpoints;
    }
}
