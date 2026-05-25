# Quickstart

## Build and test

```sh
dotnet build ConsoleLogStream.slnx
dotnet test ConsoleLogStream.slnx
```

## Core capture

```csharp
var services = new ServiceCollection()
    .AddConsoleLogStream()
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
builder.Services.AddConsoleLogStream();
builder.Services.AddConsoleLogStreamAspNetCore();

var app = builder.Build();
app.MapConsoleLogStream();
await app.RunAsync();
```

## SQLite

```csharp
builder.Services.AddConsoleLogStream();
builder.Services.AddConsoleLogStreamSqlite(options =>
{
    options.ConnectionString = "Data Source=console-logs.db";
    options.MaxAge = TimeSpan.FromDays(7);
});
```

## Safety note

Version 1 captures managed `Console.Out` and `Console.Error`. It does not guarantee capture of
native writes directly to stdout/stderr file descriptors. Redaction runs before provider boundaries,
but applications should still treat console logs as sensitive operational data.
