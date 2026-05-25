# SQLite API Contract

## Registration

```csharp
services.AddConsoleLogStream();
services.AddConsoleLogStreamSqlite(options =>
{
    options.ConnectionString = "Data Source=console-logs.db";
    options.MaxAge = TimeSpan.FromDays(7);
    options.MaxRows = 100_000;
});
```

## Store Behavior

SQLite persistence wraps the configured provider and persists redacted `ConsoleLogLine` values.

- Writes are queued and batched.
- Full write queues drop newest writes and increment dropped-write counts.
- Startup schema initialization is enabled by default.
- Retention is opt-in through max age and/or max rows.
- Queries use the same `ConsoleLogFilter` contract as the core provider.

The SQLite package stores source metadata as JSON and stores timestamps as UTC ISO-8601 text.
