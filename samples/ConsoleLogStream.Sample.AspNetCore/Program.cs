using ConsoleLogStream.AspNetCore.DependencyInjection;
using ConsoleLogStream.Core;
using ConsoleLogStream.Core.DependencyInjection;
using ConsoleLogStream.Persistence.Sqlite.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddConsoleLogStream(options =>
{
    options.ServiceName = "console-log-stream-sample";
    options.SourceId = "sample";
});

builder.Services.AddConsoleLogStreamAspNetCore();
builder.Services.AddConsoleLogStreamSqlite(options =>
{
    options.ConnectionString = "Data Source=sample-console-logs.db";
    options.MaxAge = TimeSpan.FromDays(1);
});

var app = builder.Build();

app.MapGet("/", () => Results.Text("Console Log Stream sample. Try /write, /diagnostics/console-logs/recent, or /hubs/console-logs."));
app.MapGet("/write", () =>
{
    Console.WriteLine("Sample stdout line with password=secret");
    Console.Error.WriteLine("Sample stderr line");
    return Results.Ok(new { Written = true });
});

app.MapConsoleLogStream();

await app.Services.GetRequiredService<IConsoleLogCapture>().StartAsync();
await app.RunAsync();
