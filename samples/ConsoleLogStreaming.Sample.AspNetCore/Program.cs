using ConsoleLogStreaming.AspNetCore.DependencyInjection;
using ConsoleLogStreaming.Core;
using ConsoleLogStreaming.Core.DependencyInjection;
using ConsoleLogStreaming.Persistence.Sqlite.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddConsoleLogStreaming(options =>
{
    options.ServiceName = "console-log-streaming-sample";
    options.SourceId = "sample";
});

builder.Services.AddConsoleLogStreamingAspNetCore();
builder.Services.AddConsoleLogStreamingSqlite(options =>
{
    options.ConnectionString = "Data Source=sample-console-logs.db";
    options.MaxAge = TimeSpan.FromDays(1);
});

var app = builder.Build();

app.MapGet("/", () => Results.Text("Console Log Streaming sample. Try /write, /diagnostics/console-logs/recent, or /hubs/console-logs."));
app.MapGet("/write", () =>
{
    Console.WriteLine("Sample stdout line with password=secret");
    Console.Error.WriteLine("Sample stderr line");
    return Results.Ok(new { Written = true });
});

app.MapConsoleLogStreaming();

await app.Services.GetRequiredService<IConsoleLogCapture>().StartAsync();
await app.RunAsync();
