# Core API Contract

## Registration

```csharp
services.AddConsoleLogStreaming(options =>
{
    options.RecentCapacity = 1000;
    options.MaxLineLength = 16 * 1024;
});
```

## Capture

```csharp
public interface IConsoleLogCapture : IAsyncDisposable
{
    ValueTask StartAsync(CancellationToken cancellationToken = default);

    ValueTask StopAsync(CancellationToken cancellationToken = default);
}
```

## Provider

```csharp
public interface IConsoleLogProvider
{
    ValueTask PublishAsync(ConsoleLogLine line, CancellationToken cancellationToken = default);

    ValueTask<RecentConsoleLogsResult> GetRecentAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default);

    IAsyncEnumerable<ConsoleLogStreamingItem> SubscribeAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyCollection<ConsoleLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default);
}
```

## Redaction

```csharp
public interface IConsoleLogRedactor
{
    ConsoleLogLine Redact(ConsoleLogLine line);

    ConsoleLogSource Redact(ConsoleLogSource source);
}
```

Core providers and subscribers receive only redacted lines and redacted sources.
