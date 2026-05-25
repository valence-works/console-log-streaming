# Data Model: Console Log Stream

## ConsoleLogLine

Redacted, normalized stdout or stderr line.

- `Id`: unique line identifier.
- `Timestamp`: timestamp assigned when the line is emitted by capture.
- `ReceivedAt`: timestamp assigned when the provider accepts the line.
- `Sequence`: source-local monotonic sequence.
- `Stream`: `stdout` or `stderr`.
- `Text`: redacted and ANSI-normalized line text.
- `Source`: source descriptor.
- `Truncated`: indicates maximum length truncation.
- `Dropped`: optional dropped-line metadata attached to the source or provider.

## ConsoleLogSource

Application process or host source that produced the line.

- `Id`: stable source identifier.
- `DisplayName`: human-readable source name.
- `ServiceName`: optional application/service name.
- `ProcessId`: current process identifier.
- `MachineName`: current machine name.
- `LastSeen`: most recent line timestamp.
- `Health`: connected, stale, or disconnected.
- `Metadata`: optional redacted key/value metadata.

## ConsoleLogFilter

Criteria for recent queries and live subscriptions.

- `SourceId`: optional exact source match.
- `Stream`: optional stdout/stderr match.
- `Query`: optional case-insensitive text search.
- `From`, `To`: optional received time range.
- `Limit`: requested result count, clamped by options.

## ConsoleLogDroppedSummary

Bounded buffer, subscriber, or persistence loss summary.

- `SourceId`: affected source when known.
- `Stream`: affected stream when known.
- `Reason`: safe reason string.
- `Count`: number of dropped lines or writes.
- `From`, `To`: optional time span covered by the summary.

## ConsoleLogStreamItem

Union-style item for live streams.

- `Line`: populated for a console line event.
- `Dropped`: populated for a dropped summary.

## ConsoleLogOptions

Capture and provider configuration.

- Recent buffer capacity.
- Subscriber queue capacity.
- Maximum recent query size.
- Maximum line length.
- Idle flush timeout.
- ANSI preservation flag.
- Default/custom redaction rules.
- Source metadata settings.

## SqliteConsoleLogOptions

SQLite persistence configuration.

- Connection string.
- Write queue capacity.
- Batch size.
- Flush interval.
- Maximum age retention.
- Maximum row retention.
- Startup schema initialization flag.
