# Feature Specification: Console Log Streaming

**Feature Branch**: `001-console-log-stream`

**Created**: 2026-05-25

**Status**: Draft

**Input**: User description: "Build a reusable open-source .NET library family for capturing current-process stdout and stderr as redacted line-oriented console log events, keeping bounded recent history, streaming live events through ASP.NET Core SignalR, and optionally persisting redacted events to SQLite with retention. Publish it as an MIT-licensed GitHub project under valence-works with comprehensive documentation and tests."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Capture Managed Console Lines (Priority: P1)

A .NET application developer installs the core package and captures current-process managed
`Console.Out` and `Console.Error` writes as normalized console line events while the original console
output remains visible to the host environment.

**Why this priority**: Reliable capture, framing, redaction, and bounded recent history are the
foundation for every adapter and persistence option.

**Independent Test**: Configure the core capture service with an in-memory store, write stdout and
stderr lines, and verify ordered redacted line events are recorded without suppressing the original
console writers.

**Acceptance Scenarios**:

1. **Given** console capture is active, **When** a complete stdout line is written, **Then** a line
   event is published with stream identity, text, timestamps, source metadata, and sequence.
2. **Given** console capture is active, **When** stderr text is written, **Then** the event stream
   identifies it as stderr independently from stdout.
3. **Given** partial writes arrive without a newline, **When** the line later completes or reaches an
   idle flush or maximum length boundary, **Then** subscribers receive one complete line event.
4. **Given** sensitive text is written, **When** capture publishes the line, **Then** stores and
   subscribers receive only redacted content.

---

### User Story 2 - Stream From ASP.NET Core (Priority: P2)

An ASP.NET Core developer adds the web adapter package and exposes recent and live console logs to
authorized browser or service clients through framework-native endpoints and SignalR streaming.

**Why this priority**: Many target applications are web hosts or workers with an admin UI that needs
live console diagnostics without shell access.

**Independent Test**: Start a test ASP.NET Core host, connect to the SignalR hub, request recent
lines, write console output, and verify the client receives live and recent redacted events.

**Acceptance Scenarios**:

1. **Given** the ASP.NET Core adapter is registered, **When** a client calls the recent endpoint,
   **Then** it receives bounded recent console lines filtered by stream, source, text, and time.
2. **Given** a client is subscribed to the live hub, **When** new console lines are captured, **Then**
   the client receives matching line events and dropped-line summaries.
3. **Given** authorization is configured by the host, **When** an unauthorized client requests recent
   or live console logs, **Then** access is denied by the host policy.

---

### User Story 3 - Persist Redacted Console Logs (Priority: P3)

An application developer opts into SQLite persistence so redacted console lines survive application
restart for short-term troubleshooting, with retention controls to prevent accidental long-term data
growth.

**Why this priority**: Reuse across apps benefits from an easy local durable store, but persistence
must stay optional because console logs can contain sensitive operational data.

**Independent Test**: Configure SQLite persistence, capture several redacted lines, recreate the
service with the same database file, and verify recent queries return persisted events subject to
retention.

**Acceptance Scenarios**:

1. **Given** SQLite persistence is configured, **When** redacted console lines are captured, **Then**
   they are queued and written durably without blocking console writes.
2. **Given** the application restarts with the same database, **When** recent lines are queried,
   **Then** previously persisted redacted events can be returned.
3. **Given** retention is configured, **When** cleanup runs, **Then** old or excess rows are deleted
   according to the configured limits.

---

### User Story 4 - Publish OSS Project (Priority: P4)

A developer evaluating the repository can understand the purpose, safety model, limitations,
installation steps, package layout, and common usage patterns without reading the source first.

**Why this priority**: The project is intended for reuse outside one application, so documentation,
license, and repository metadata are part of the deliverable.

**Independent Test**: Review the repository README, license, package metadata, and build/test
instructions from a clean checkout and verify the documented quickstart compiles.

**Acceptance Scenarios**:

1. **Given** a developer opens the repository, **When** they read the README, **Then** they can tell
   what the project does, why it exists, what it does not guarantee, and how to install each package.
2. **Given** the repository is published, **When** package metadata is inspected, **Then** it includes
   MIT license information, useful descriptions, repository URL, and tags.

### Edge Cases

- Console writers are disposed or capture is stopped while partial lines are buffered.
- A line exceeds the configured maximum line length.
- Output arrives faster than recent buffers, subscribers, or persistence queues can process.
- ANSI escape sequences are present in output.
- Redaction rules match source metadata or line text.
- Multiple subscribers apply different filters and disconnect independently.
- SQLite is unavailable, slow, locked, or has not been initialized.
- An application or dependency caches the original console writer before capture starts.
- Native code writes directly to process file descriptors instead of managed `Console` writers.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The core package MUST expose reusable models for console line events, sources, filters,
  dropped summaries, and stream items.
- **FR-002**: The core package MUST capture managed `Console.Out` and `Console.Error` writes while
  preserving writes to the previously configured writers.
- **FR-003**: Capture MUST publish line-oriented events with stdout/stderr identity, sequence,
  timestamps, source metadata, text, truncation state, and dropped metadata when available.
- **FR-004**: Partial writes MUST be buffered until newline, maximum length, idle flush, or capture
  shutdown.
- **FR-005**: Oversized lines MUST be truncated to a configured maximum and marked as truncated.
- **FR-006**: ANSI escape sequences MUST be stripped by default and MAY be preserved by option.
- **FR-007**: Redaction MUST run before any line or source reaches stores, live subscribers,
  persistence providers, endpoints, or hubs.
- **FR-008**: The core package MUST provide configurable redaction rules for common secret patterns
  and custom patterns.
- **FR-009**: The in-memory provider MUST keep bounded recent history and bounded subscriber queues.
- **FR-010**: Overload behavior MUST track and expose dropped-line or dropped-write counts instead
  of growing memory without bound.
- **FR-011**: Recent queries and live subscriptions MUST support source, stream, text, time, and
  limit filters.
- **FR-012**: The ASP.NET Core adapter MUST expose registration APIs for capture, endpoints, and
  SignalR hub mapping.
- **FR-013**: The ASP.NET Core adapter MUST allow hosts to configure authorization policy names for
  endpoints and hubs.
- **FR-014**: The SQLite package MUST persist only redacted line events and redacted source metadata.
- **FR-015**: SQLite writes MUST use a bounded asynchronous queue and MUST NOT block console write
  calls on disk I/O.
- **FR-016**: SQLite persistence MUST support startup schema initialization and configurable
  retention by maximum age and maximum row count.
- **FR-017**: The README MUST document installation, quickstarts, safety boundaries, known
  limitations, and package responsibilities.
- **FR-018**: The repository MUST include an MIT license and package metadata suitable for NuGet
  publishing.

### Key Entities *(include if feature involves data)*

- **Console Log Line**: A redacted stdout or stderr line with sequence, timestamps, stream identity,
  text, source descriptor, truncation state, and optional dropped metadata.
- **Console Log Source**: The application process or host source that produced console output,
  including process ID, machine name, service name, display name, health, and optional metadata.
- **Console Log Filter**: Criteria used by recent queries and live subscriptions.
- **Console Log Provider**: Store and live feed for redacted console line events.
- **Console Capture Service**: Runtime service that installs and removes managed console tee writers.
- **Redaction Rule**: Configured pattern and replacement used to mask sensitive data.
- **SQLite Store**: Optional durable store for redacted console lines and source metadata.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Capturing 100 complete test lines produces 100 ordered redacted events within one
  second under normal test conditions.
- **SC-002**: Recent queries never return more lines than the configured maximum or requested limit,
  whichever is lower.
- **SC-003**: Tests demonstrate stdout and stderr are captured as distinct streams.
- **SC-004**: Tests demonstrate default redaction masks common secret-like values before storage or
  streaming.
- **SC-005**: Tests demonstrate sustained overload remains bounded and reports dropped counts.
- **SC-006**: ASP.NET Core integration tests demonstrate recent endpoint and SignalR live streaming.
- **SC-007**: SQLite tests demonstrate persisted redacted lines survive service recreation and
  retention deletes old or excess rows.
- **SC-008**: A clean checkout can run `dotnet build` and `dotnet test` successfully.

## Assumptions

- Version 1 targets managed .NET console writers, not guaranteed native file-descriptor capture.
- Packages target modern supported .NET applications first.
- SignalR is optional and belongs only to the ASP.NET Core adapter package.
- SQLite persistence is for short-term troubleshooting, not compliance audit logging.
- NuGet publishing automation can be added later; this feature prepares package metadata but does
  not publish packages.
