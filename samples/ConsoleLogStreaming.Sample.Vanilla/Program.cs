using ConsoleLogStreaming.AspNetCore.DependencyInjection;
using ConsoleLogStreaming.Core;
using ConsoleLogStreaming.Core.Capture;
using ConsoleLogStreaming.Core.DependencyInjection;
using ConsoleLogStreaming.Persistence.Sqlite.DependencyInjection;

ConsoleStreamHook.Install();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddConsoleLogStreaming(options =>
{
    options.ServiceName = "console-log-streaming-vanilla-sample";
    options.SourceId = "vanilla-sample";
    options.SourceDisplayName = "Vanilla HTML sample";
    options.RecentCapacity = 2_000;
    options.MaxRecentQuerySize = 250;
    options.SourceMetadata["framework"] = "Vanilla HTML + JavaScript";
});

builder.Services.AddConsoleLogStreamingAspNetCore();
builder.Services.AddConsoleLogStreamingSqlite(options =>
{
    options.ConnectionString = "Data Source=console-log-streaming-vanilla-sample.db";
    options.MaxAge = TimeSpan.FromHours(12);
    options.MaxRows = 10_000;
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/demo/stdout", () =>
{
    Console.WriteLine(CreateDemoLine("stdout", "Manual stdout probe"));
    return Results.Ok(new DemoWriteResponse("stdout", 1));
});

app.MapPost("/demo/stderr", () =>
{
    Console.Error.WriteLine(CreateDemoLine("stderr", "Manual stderr probe"));
    return Results.Ok(new DemoWriteResponse("stderr", 1));
});

app.MapPost("/demo/burst", async () =>
{
    var total = 12;

    for (var index = 1; index <= total; index++)
    {
        var stream = index % 4 == 0 ? "stderr" : "stdout";
        var message = stream == "stderr"
            ? $"Burst item {index:00}: simulated backend warning"
            : $"Burst item {index:00}: processed background job";

        if (stream == "stderr")
            Console.Error.WriteLine(CreateDemoLine(stream, message));
        else
            Console.WriteLine(CreateDemoLine(stream, message));

        await Task.Delay(90);
    }

    return Results.Ok(new DemoWriteResponse("mixed", total));
});

app.MapConsoleLogStreaming();
app.MapFallbackToFile("index.html");

await app.Services.GetRequiredService<IConsoleLogCapture>().StartAsync();
Console.WriteLine("Vanilla sample console capture started. Open the browser UI to watch live output.");
Console.Error.WriteLine("Vanilla sample stderr channel is ready for live streaming.");

await app.RunAsync();

static string CreateDemoLine(string stream, string message)
{
    return $"[{DateTimeOffset.Now:HH:mm:ss.fff}] {stream.ToUpperInvariant()} | {message} | requestId={Guid.NewGuid():N}";
}

internal sealed record DemoWriteResponse(string Stream, int LinesWritten);
