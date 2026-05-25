# Quickstart

## Build and test

```sh
dotnet build ConsoleLogStreaming.slnx
dotnet test ConsoleLogStreaming.slnx
```

## Core capture

```csharp
var services = new ServiceCollection()
    .AddConsoleLogStreaming()
    .BuildServiceProvider();

var capture = services.GetRequiredService<IConsoleLogCapture>();
await capture.StartAsync();

Console.WriteLine("hello from stdout");
Console.Error.WriteLine("hello from stderr");

var provider = services.GetRequiredService<IConsoleLogProvider>();
var recent = await provider.GetRecentAsync(new ConsoleLogFilter { Limit = 10 });
```

## ASP.NET Core

```csharp
builder.Services.AddConsoleLogStreaming();
builder.Services.AddConsoleLogStreamingAspNetCore();

var app = builder.Build();
app.MapConsoleLogStreaming();
await app.RunAsync();
```

## SQLite

```csharp
builder.Services.AddConsoleLogStreaming();
builder.Services.AddConsoleLogStreamingSqlite(options =>
{
    options.ConnectionString = "Data Source=console-logs.db";
    options.MaxAge = TimeSpan.FromDays(7);
});
```

## Safety note

Version 1 captures managed `Console.Out` and `Console.Error`. It does not guarantee capture of
native writes directly to stdout/stderr file descriptors. Redaction runs before provider boundaries,
but applications should still treat console logs as sensitive operational data.
