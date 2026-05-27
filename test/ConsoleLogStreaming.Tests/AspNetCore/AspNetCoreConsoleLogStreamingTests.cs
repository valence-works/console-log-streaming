using ConsoleLogStreaming.AspNetCore.DependencyInjection;
using ConsoleLogStreaming.Contracts;
using ConsoleLogStreaming.Core;
using ConsoleLogStreaming.Core.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;

namespace ConsoleLogStreaming.Tests.AspNetCore;

public sealed class AspNetCoreConsoleLogStreamingTests
{
    [Fact]
    public async Task RecentEndpointReturnsPublishedLines()
    {
        await using var app = await CreateAppAsync();
        var provider = app.Services.GetRequiredService<IConsoleLogProvider>();
        var source = app.Services.GetRequiredService<IConsoleLogSourceRegistry>().Current;

        await provider.PublishAsync(new ConsoleLogStreaming.Core.Models.ConsoleLogLine { Source = source, Stream = ConsoleLogStreaming.Core.Models.ConsoleStream.Stdout, Text = "from endpoint" });

        var client = app.GetTestClient();
        var result = await client.GetFromJsonAsync<RecentConsoleLogsResult>("/diagnostics/console-logs/recent?limit=10");

        Assert.NotNull(result);
        Assert.Contains(result.Items, x => x.Text == "from endpoint");
    }

    [Fact]
    public async Task SignalRStreamReturnsLiveLines()
    {
        await using var app = await CreateAppAsync();
        var connection = new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/console-logs", options =>
            {
                options.HttpMessageHandlerFactory = _ => app.GetTestServer().CreateHandler();
            })
            .Build();

        await connection.StartAsync();
        var channel = await connection.StreamAsChannelAsync<ConsoleLogStreamingItem>("StreamAsync", new ConsoleLogFilter { Limit = 10 });

        var provider = app.Services.GetRequiredService<IConsoleLogProvider>();
        var source = app.Services.GetRequiredService<IConsoleLogSourceRegistry>().Current;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var readTask = channel.ReadAsync(cts.Token).AsTask();

        while (!readTask.IsCompleted)
        {
            await provider.PublishAsync(new ConsoleLogStreaming.Core.Models.ConsoleLogLine { Source = source, Stream = ConsoleLogStreaming.Core.Models.ConsoleStream.Stdout, Text = "from hub" });
            if (await Task.WhenAny(readTask, Task.Delay(50, cts.Token)) == readTask)
                break;
        }

        var item = await readTask;

        Assert.Equal("from hub", item.Line?.Text);
        await connection.DisposeAsync();
    }

    private static async Task<WebApplication> CreateAppAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddConsoleLogStreaming(options => options.SourceId = "aspnet-test");
        builder.Services.AddConsoleLogStreamingAspNetCore();

        var app = builder.Build();
        app.MapConsoleLogStreaming();
        await app.StartAsync();
        return app;
    }
}
