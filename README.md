# Console Log Stream

Console Log Stream is a small .NET library family for capturing managed
`Console.Out` and `Console.Error` output as redacted, line-oriented diagnostics
events. It gives applications a reusable foundation for recent console log
backfill, live streaming, and optional short-term SQLite persistence without
coupling the core package to ASP.NET Core, SignalR, SQLite, Elsa, or any one UI.

## Why

Many .NET apps need an admin-facing view of raw backend console output: worker
progress, boot diagnostics, library `Console.WriteLine` calls, stderr messages,
and troubleshooting traces that are not always represented as structured
`ILogger` events.

The usual choices are awkward:

- SSH into the host or inspect container logs.
- Build a one-off `Console.SetOut` wrapper per app.
- Force everything through a logging framework and lose raw stdout/stderr
  semantics.
- Add a vendor log stack when all you need is short-term operational visibility.

Console Log Stream packages the reusable middle layer: safe managed capture,
redaction, bounded buffers, filters, live subscriptions, optional web transport,
and optional local persistence.

## Packages

| Package | Purpose |
|---------|---------|
| `ConsoleLogStream.Core` | Framework-neutral capture, redaction, models, bounded in-memory provider, and async live subscriptions. |
| `ConsoleLogStream.AspNetCore` | Minimal API endpoints and SignalR hub for web hosts. |
| `ConsoleLogStream.Persistence.Sqlite` | Optional durable store for redacted console lines with retention. |

## Safety Model

Console output often contains sensitive data. The library is designed around a
strict boundary:

1. Managed stdout/stderr writes are captured by a tee writer.
2. Lines are normalized, optionally stripped of ANSI escapes, and redacted.
3. Only redacted line events reach providers, live subscribers, web endpoints,
   SignalR clients, or SQLite persistence.

Default redaction masks common bearer tokens, password/secret/token/API-key style
values, cookies, authorization values, and connection string style values. Add
your own rules for application-specific secrets.

## Important Limitations

Version 1 captures managed .NET `Console.Out` and `Console.Error` writes. It does
not guarantee capture of:

- Native code writing directly to stdout/stderr file descriptors.
- Libraries that cached `Console.Out` or `Console.Error` before capture started.
- Output emitted by child processes unless the child output is redirected back
  into the current process and written to managed console writers.

Use platform/container logs for authoritative process-level log capture. Use this
library for reusable in-app diagnostics surfaces.

## Core Usage

```csharp
using ConsoleLogStream.Core;
using ConsoleLogStream.Core.DependencyInjection;
using ConsoleLogStream.Core.Models;
using Microsoft.Extensions.DependencyInjection;

await using var services = new ServiceCollection()
    .AddConsoleLogStream(options =>
    {
        options.SourceId = "worker-1";
        options.RecentCapacity = 1000;
        options.MaxLineLength = 16 * 1024;
        options.RedactionRules.Add(new()
        {
            Name = "Tenant token",
            Pattern = @"tenant-token=[^\s]+",
            Replacement = "tenant-token=[redacted]"
        });
    })
    .BuildServiceProvider();

var capture = services.GetRequiredService<IConsoleLogCapture>();
await capture.StartAsync();

Console.WriteLine("Hello from stdout");
Console.Error.WriteLine("Hello from stderr");

var provider = services.GetRequiredService<IConsoleLogProvider>();
var recent = await provider.GetRecentAsync(new ConsoleLogFilter
{
    Stream = ConsoleStream.Stdout,
    Limit = 50
});

await capture.StopAsync();
```

## ASP.NET Core Usage

```csharp
using ConsoleLogStream.AspNetCore.DependencyInjection;
using ConsoleLogStream.Core.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddConsoleLogStream(options =>
{
    options.ServiceName = "orders-api";
});

builder.Services.AddConsoleLogStreamAspNetCore(options =>
{
    options.AuthorizationPolicy = "diagnostics.console";
    options.RecentPath = "/diagnostics/console-logs/recent";
    options.SourcesPath = "/diagnostics/console-logs/sources";
    options.HubPath = "/hubs/console-logs";
});

var app = builder.Build();
app.MapConsoleLogStream();

await app.Services.GetRequiredService<IConsoleLogCapture>().StartAsync();
await app.RunAsync();
```

Endpoints:

- `GET /diagnostics/console-logs/recent`
- `GET /diagnostics/console-logs/sources`
- SignalR hub: `/hubs/console-logs`

The hub exposes a streaming method:

```csharp
var channel = await connection.StreamAsChannelAsync<ConsoleLogStreamItem>(
    "Stream",
    new ConsoleLogFilter { Query = "startup", Limit = 100 });
```

It also supports push-style methods:

- `Subscribe(ConsoleLogFilter filter)`
- `UpdateFilter(ConsoleLogFilter filter)`
- `Unsubscribe()`

## SQLite Persistence

SQLite persistence is optional and intended for short-term troubleshooting, not
compliance audit logging.

```csharp
using ConsoleLogStream.Persistence.Sqlite.DependencyInjection;

builder.Services.AddConsoleLogStream();
builder.Services.AddConsoleLogStreamSqlite(options =>
{
    options.ConnectionString = "Data Source=console-logs.db";
    options.MaxAge = TimeSpan.FromDays(7);
    options.MaxRows = 100_000;
});
```

SQLite stores redacted line text and redacted source metadata only. Writes are
queued and batched so console writes do not wait on disk I/O. Configure retention
for production use.

## Filtering

Recent queries and live subscriptions use `ConsoleLogFilter`:

- `SourceId`
- `Stream`
- `Query`
- `From`
- `To`
- `Limit`

Providers clamp requested limits to configured maximums.

## Build

```sh
dotnet build ConsoleLogStream.slnx
dotnet test ConsoleLogStream.slnx
```

## Package Publishing

The base package version is controlled by `VersionPrefix` in
`Directory.Build.props`.

GitHub Actions publishes packages from `.github/workflows/nuget.yml`:

- Pushes to `main` publish preview packages using
  `{VersionPrefix}-preview.{GITHUB_RUN_NUMBER}`.
- Manual `workflow_dispatch` runs publish preview packages using the same
  preview version format when run from `main`. Dispatches from other branches
  build, test, pack, and upload artifacts without publishing to NuGet.org.
- Published GitHub releases publish stable packages using `VersionPrefix`.
- Release tags must match the `VersionPrefix` in `Directory.Build.props`, e.g.
  `1.2.3` or `v1.2.3`.

Configure the repository secret `NUGET_API_KEY` with a NuGet.org API key before
publishing. If the secret is not configured, the workflow still builds, tests,
packs, and uploads artifacts, but skips the NuGet.org publish step.

## Repository Layout

```text
src/
├── ConsoleLogStream.Core/
├── ConsoleLogStream.AspNetCore/
└── ConsoleLogStream.Persistence.Sqlite/

test/
└── ConsoleLogStream.Tests/

samples/
└── ConsoleLogStream.Sample.AspNetCore/
```

## License

MIT.
