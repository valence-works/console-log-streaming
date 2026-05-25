# Implementation Plan: Console Log Streaming

**Branch**: `001-console-log-stream` | **Date**: 2026-05-25 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-console-log-stream/spec.md`

## Summary

Build an MIT-licensed reusable .NET library family that captures managed stdout/stderr through a
teeing `TextWriter`, normalizes output into redacted line events, keeps bounded recent/live buffers,
offers optional ASP.NET Core SignalR streaming, and persists redacted events to SQLite when enabled.
The core package remains framework-neutral; ASP.NET Core and SQLite live in separate packages.

## Technical Context

**Language/Version**: C# latest, nullable reference types enabled, implicit usings enabled.

**Primary Dependencies**: .NET 8, `System.Threading.Channels`, `Microsoft.Extensions.Options`,
`Microsoft.Extensions.Hosting.Abstractions`, `Microsoft.AspNetCore.SignalR`, `Microsoft.AspNetCore.Routing`,
`Microsoft.Data.Sqlite`, `xunit.v3`.

**Storage**: Bounded in-memory provider by default. Optional SQLite durable store stores only
redacted line text and redacted source metadata.

**Testing**: xUnit v3 unit and integration tests through `dotnet test`.

**Target Platform**: Modern .NET applications, workers, and ASP.NET Core hosts.

**Project Type**: Multi-package .NET class library repository with integration tests.

**Performance Goals**: Normal line capture should avoid blocking console writes on subscriber or
SQLite I/O. Buffers remain bounded under sustained overload and report dropped counts.

**Constraints**: Core must not depend on ASP.NET Core or SQLite. Redaction happens before provider
boundaries. Version 1 captures managed `Console.Out` and `Console.Error`, not guaranteed native
file-descriptor writes.

**Scale/Scope**: Four library projects, one test project, one ASP.NET Core integration test slice,
comprehensive README, MIT license, and GitHub repository metadata.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Verdict | Evidence |
|-----------|---------|----------|
| I. Library-First Reuse | PASS | Core capture, models, redaction, buffers, and provider contracts live in `ConsoleLogStreaming.Core`; web and SQLite are adapters. |
| II. Safe Console Data Boundaries | PASS | Plan requires redaction before stores, subscribers, SQLite, endpoints, or hubs. |
| III. Bounded Runtime Behavior | PASS | Recent buffers, subscriber queues, and SQLite writes are bounded with dropped counts. |
| IV. Testable Public Contracts | PASS | Tests cover capture, redaction, framing, buffers, SignalR, and SQLite retention. |
| V. Minimal Dependencies | PASS | Optional dependencies are isolated in ASP.NET Core and SQLite packages. |

## Project Structure

### Documentation (this feature)

```text
specs/001-console-log-stream/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── core-api.md
│   ├── aspnetcore-api.md
│   └── sqlite-api.md
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── ConsoleLogStreaming.Core/
├── ConsoleLogStreaming.AspNetCore/
└── ConsoleLogStreaming.Persistence.Sqlite/

test/
└── ConsoleLogStreaming.Tests/

samples/
└── ConsoleLogStreaming.Sample.AspNetCore/
```

**Structure Decision**: Keep shared runtime behavior in `ConsoleLogStreaming.Core`; add focused adapter
packages for ASP.NET Core and SQLite; use one test project initially to keep the OSS seed compact
while still covering cross-package behavior.

## Phase 0 Output

See [research.md](./research.md).

Resolved decisions:

- Use managed `Console.SetOut` and `Console.SetError` tee writers for v1.
- Keep core transport-agnostic and expose `IAsyncEnumerable` subscriptions.
- Use channels for bounded live subscriber queues and SQLite write queues.
- Strip ANSI by default and expose a preserve option.
- Use SQLite direct ADO.NET through `Microsoft.Data.Sqlite`; no ORM or migration framework in v1.
- Document native/file-descriptor capture as out of scope for v1.

## Phase 1 Output

- [data-model.md](./data-model.md)
- [contracts/core-api.md](./contracts/core-api.md)
- [contracts/aspnetcore-api.md](./contracts/aspnetcore-api.md)
- [contracts/sqlite-api.md](./contracts/sqlite-api.md)
- [quickstart.md](./quickstart.md)

## Post-Design Constitution Re-Check

| Principle | Verdict | Post-design evidence |
|-----------|---------|----------------------|
| I. Library-First Reuse | PASS | Contracts keep core independent from web and persistence packages. |
| II. Safe Console Data Boundaries | PASS | Data model marks provider-facing lines as redacted normalized events. |
| III. Bounded Runtime Behavior | PASS | Contracts include capacities and dropped summaries. |
| IV. Testable Public Contracts | PASS | Quickstart and tasks define build/test validation. |
| V. Minimal Dependencies | PASS | Package boundaries avoid forcing SignalR or SQLite into the core package. |

## Complexity Tracking

No constitution violations identified.
