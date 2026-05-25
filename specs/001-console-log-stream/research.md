# Research: Console Log Stream

## Decision: Capture managed console writers in v1

**Rationale**: `Console.SetOut` and `Console.SetError` provide a portable managed capture point that
works in standard .NET applications without native hooks or platform-specific file descriptor logic.
A tee writer preserves the previous writers so host-visible output still works.

**Alternatives considered**:

- File-descriptor-level capture: rejected for v1 because it is platform-specific and riskier.
- Child process wrappers: rejected because this library targets the current host process.
- Logging sinks only: rejected because raw console writes are distinct from structured logs.

## Decision: Keep the core package transport-agnostic

**Rationale**: Reuse across workers, CLIs, ASP.NET Core hosts, and future UI adapters requires a
small core API built on models, providers, and async streams. SignalR belongs in a separate adapter.

**Alternatives considered**:

- Put SignalR in the core package: rejected because it would force web dependencies on non-web apps.
- Expose only callbacks/events: rejected because async streams compose better with backpressure and
  cancellation.

## Decision: Redact before provider boundaries

**Rationale**: Providers include in-memory buffers, SignalR, and SQLite. Treating all providers as
external to the raw capture boundary prevents accidental durable or remote exposure of secrets.

**Alternatives considered**:

- Let each provider redact: rejected because every provider would need to prove its own safety.
- Store raw and redact on read: rejected because persistence would retain sensitive material.

## Decision: Use bounded channels and explicit dropped counts

**Rationale**: Console writes must not allocate unbounded memory or wait on slow subscribers. Bounded
queues with drop-newest accounting make overload visible without destabilizing the host.

**Alternatives considered**:

- Blocking writes: rejected because diagnostics should not stall application output.
- Unbounded queues: rejected because sustained output can exhaust memory.

## Decision: Use direct SQLite ADO.NET for persistence

**Rationale**: SQLite persistence is optional, simple, and local. Direct ADO.NET keeps dependencies
small and avoids forcing EF Core or migration frameworks into a diagnostics package.

**Alternatives considered**:

- EF Core: rejected as too heavy for a small optional store.
- FluentMigrator: deferred until multiple relational providers justify a migration abstraction.
- File append logs: rejected because filtering and retention are harder to implement safely.
