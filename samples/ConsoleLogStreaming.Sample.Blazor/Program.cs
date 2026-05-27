using ConsoleLogStreaming.AspNetCore.DependencyInjection;
using ConsoleLogStreaming.Core;
using ConsoleLogStreaming.Core.DependencyInjection;
using ConsoleLogStreaming.Persistence.Sqlite.DependencyInjection;
using ConsoleLogStreaming.Sample.Blazor.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient();

builder.Services.AddConsoleLogStreaming(options =>
{
    options.ServiceName = "console-log-streaming-blazor-sample";
    options.SourceId = "blazor-sample";
    options.SourceDisplayName = "Blazor sample backend";
    options.RecentCapacity = 500;
    options.MaxRecentQuerySize = 200;
});

builder.Services.AddConsoleLogStreamingAspNetCore();
builder.Services.AddConsoleLogStreamingSqlite(options =>
{
    options.ConnectionString = $"Data Source={Path.Combine(builder.Environment.ContentRootPath, "sample-console-logs.db")}";
    options.MaxAge = TimeSpan.FromDays(1);
    options.MaxRows = 5_000;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Error");

app.UseStaticFiles();
app.UseAntiforgery();

app.MapConsoleLogStreaming();

app.MapPost("/demo/stdout", () =>
{
    Console.WriteLine("Demo stdout line: request processed in {0} ms", Random.Shared.Next(12, 140));
    return Results.Ok(new { written = true, stream = "stdout" });
});

app.MapPost("/demo/stderr", () =>
{
    Console.Error.WriteLine("Demo stderr line: payment retry failed with token=sample-secret-{0}", Random.Shared.Next(100, 999));
    return Results.Ok(new { written = true, stream = "stderr" });
});

app.MapPost("/demo/burst", async (CancellationToken cancellationToken) =>
{
    for (var i = 1; i <= 8; i++)
    {
        Console.WriteLine("Burst stdout {0}/8: queued job {1:n0}", i, Random.Shared.Next(1_000, 9_999));

        if (i % 3 == 0)
            Console.Error.WriteLine("Burst stderr {0}/8: transient backend warning", i);

        await Task.Delay(90, cancellationToken);
    }

    return Results.Ok(new { written = true, stream = "mixed", count = 10 });
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.Services.GetRequiredService<IConsoleLogCapture>().StartAsync();

Console.WriteLine("Console Log Streaming Blazor sample started. Open the app and try the demo controls.");

await app.RunAsync();
