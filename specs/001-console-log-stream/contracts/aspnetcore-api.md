# ASP.NET Core API Contract

## Registration

```csharp
builder.Services.AddConsoleLogStream();
builder.Services.AddConsoleLogStreamAspNetCore(options =>
{
    options.AuthorizationPolicy = "diagnostics.console";
});

app.MapConsoleLogStream();
```

## HTTP Endpoints

- `GET /diagnostics/console-logs/recent`
  - Query: `sourceId`, `stream`, `query`, `from`, `to`, `limit`
  - Returns: `RecentConsoleLogsResult`
- `GET /diagnostics/console-logs/sources`
  - Returns: `ConsoleLogSource[]`

## SignalR Hub

Default path: `/hubs/console-logs`

Client methods:

- `Subscribe(ConsoleLogFilter filter)`
- `UpdateFilter(ConsoleLogFilter filter)`
- `Unsubscribe()`

Server methods:

- `ConsoleLogStreamItem` messages through `ConsoleLogStreamItem`.

Authorization is host-configurable through standard ASP.NET Core authorization policy names.
