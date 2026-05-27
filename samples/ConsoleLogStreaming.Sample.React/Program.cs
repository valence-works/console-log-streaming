using ConsoleLogStreaming.AspNetCore.DependencyInjection;
using ConsoleLogStreaming.Core;
using ConsoleLogStreaming.Core.Capture;
using ConsoleLogStreaming.Core.DependencyInjection;
using ConsoleLogStreaming.Persistence.Sqlite.DependencyInjection;
using ConsoleLogLine = ConsoleLogStreaming.Core.Models.ConsoleLogLine;
using ConsoleStream = ConsoleLogStreaming.Core.Models.ConsoleStream;

ConsoleStreamHook.Install();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddConsoleLogStreaming(options =>
{
    options.ServiceName = "console-log-streaming-react-sample";
    options.SourceId = "react-sample";
    options.SourceDisplayName = "React sample backend";
    options.SourceMetadata["sample"] = "react";
});

builder.Services.AddConsoleLogStreamingAspNetCore();
builder.Services.AddConsoleLogStreamingSqlite(options =>
{
    options.ConnectionString = "Data Source=react-sample-console-logs.db";
    options.MaxAge = TimeSpan.FromHours(12);
    options.MaxRows = 2_000;
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapConsoleLogStreaming();

app.MapPost("/demo/stdout", async (IConsoleLogProvider provider, IConsoleLogSourceRegistry sources) =>
{
    await PublishDemoLineAsync(provider, sources, ConsoleStream.Stdout, "manual");
    return Results.Ok(new DemoWriteResult("stdout", 1));
});

app.MapPost("/demo/stderr", async (IConsoleLogProvider provider, IConsoleLogSourceRegistry sources) =>
{
    await PublishDemoLineAsync(provider, sources, ConsoleStream.Stderr, "manual");
    return Results.Ok(new DemoWriteResult("stderr", 1));
});

app.MapPost("/demo/burst", async (IConsoleLogProvider provider, IConsoleLogSourceRegistry sources) =>
{
    for (var index = 1; index <= 8; index++)
    {
        var stream = index % 3 == 0 ? ConsoleStream.Stderr : ConsoleStream.Stdout;
        await PublishDemoLineAsync(provider, sources, stream, $"burst-{index}");
    }

    return Results.Ok(new DemoWriteResult("mixed", 8));
});

app.MapFallbackToFile("index.html");

await app.Services.GetRequiredService<IConsoleLogCapture>().StartAsync();

Console.WriteLine("React sample console capture started.");
Console.WriteLine("Open the UI and use the demo buttons to stream stdout and stderr.");
Console.Error.WriteLine("Sample stderr is online and ready for live subscribers.");

await app.RunAsync();

static ValueTask PublishDemoLineAsync(
    IConsoleLogProvider provider,
    IConsoleLogSourceRegistry sources,
    ConsoleStream stream,
    string label)
{
    var streamLabel = stream == ConsoleStream.Stderr ? "stderr" : "stdout";
    var text = stream == ConsoleStream.Stderr
        ? $"[{DateTimeOffset.UtcNow:O}] stderr {label}: simulated warning with token=[redacted-by-sample] retry={Random.Shared.Next(1, 4)}"
        : $"[{DateTimeOffset.UtcNow:O}] stdout {label}: demo pipeline processed request id={Guid.NewGuid().ToString("n")[..8]}";

    return provider.PublishAsync(new ConsoleLogLine
    {
        Source = sources.Current,
        Stream = stream,
        Text = text,
        Timestamp = DateTimeOffset.UtcNow,
        ReceivedAt = DateTimeOffset.UtcNow,
        Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["sample"] = "react",
            ["demo"] = streamLabel
        }
    });
}

internal sealed record DemoWriteResult(string Stream, int LinesWritten);
