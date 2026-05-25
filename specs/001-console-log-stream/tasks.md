# Tasks: Console Log Streaming

**Input**: Design documents from `/specs/001-console-log-stream/`
**Tests**: Required by the specification and constitution.

## Phase 1: Setup

- [X] T001 Create solution, shared build props, central package versions, gitignore, and source/test folders in repository root
- [X] T002 [P] Create `src/ConsoleLogStreaming.Core/ConsoleLogStreaming.Core.csproj`
- [X] T003 [P] Create `src/ConsoleLogStreaming.AspNetCore/ConsoleLogStreaming.AspNetCore.csproj`
- [X] T004 [P] Create `src/ConsoleLogStreaming.Persistence.Sqlite/ConsoleLogStreaming.Persistence.Sqlite.csproj`
- [X] T005 [P] Create `test/ConsoleLogStreaming.Tests/ConsoleLogStreaming.Tests.csproj`

## Phase 2: Foundational

- [X] T006 Implement core models in `src/ConsoleLogStreaming.Core/Models`
- [X] T007 Implement options and redaction contracts in `src/ConsoleLogStreaming.Core`
- [X] T008 Implement source registry and filter matching in `src/ConsoleLogStreaming.Core`
- [X] T009 Implement service registration in `src/ConsoleLogStreaming.Core/DependencyInjection`

## Phase 3: User Story 1 - Capture Managed Console Lines (P1)

**Goal**: Core capture records managed stdout/stderr as redacted bounded line events.

**Independent Test**: Write stdout/stderr through captured console writers and query recent events.

- [X] T010 [P] [US1] Add capture/redaction tests in `test/ConsoleLogStreaming.Tests/Core`
- [X] T011 [US1] Implement bounded in-memory provider in `src/ConsoleLogStreaming.Core/Providers`
- [X] T012 [US1] Implement tee text writer and capture service in `src/ConsoleLogStreaming.Core/Capture`
- [X] T013 [US1] Implement ANSI stripping, truncation, idle flush, and shutdown flush in `src/ConsoleLogStreaming.Core/Capture`

## Phase 4: User Story 2 - Stream From ASP.NET Core (P2)

**Goal**: ASP.NET Core adapter exposes recent/sources endpoints and SignalR live stream.

**Independent Test**: Use in-memory test server and SignalR client to receive recent/live lines.

- [X] T014 [P] [US2] Add ASP.NET Core endpoint and hub tests in `test/ConsoleLogStreaming.Tests/AspNetCore`
- [X] T015 [US2] Implement ASP.NET Core options, endpoints, hub, and mapping extensions in `src/ConsoleLogStreaming.AspNetCore`

## Phase 5: User Story 3 - Persist Redacted Console Logs (P3)

**Goal**: SQLite persistence stores redacted events, survives provider recreation, and enforces retention.

**Independent Test**: Configure SQLite file, publish events, recreate store, query persisted lines.

- [X] T016 [P] [US3] Add SQLite persistence and retention tests in `test/ConsoleLogStreaming.Tests/Sqlite`
- [X] T017 [US3] Implement SQLite options, schema, query mapping, write queue, and retention in `src/ConsoleLogStreaming.Persistence.Sqlite`

## Phase 6: User Story 4 - Publish OSS Project (P4)

**Goal**: Repository is understandable, licensed, and ready for GitHub/NuGet package consumption.

**Independent Test**: Review README/license/package metadata and run documented build/test commands.

- [X] T018 [P] [US4] Add MIT `LICENSE`
- [X] T019 [US4] Add comprehensive `README.md`
- [X] T020 [US4] Add sample ASP.NET Core app in `samples/ConsoleLogStreaming.Sample.AspNetCore`
- [X] T021 [US4] Add package metadata to project files

## Phase 7: Polish & Validation

- [X] T022 Run `dotnet format --verify-no-changes ConsoleLogStreaming.slnx`
- [X] T023 Run `dotnet test ConsoleLogStreaming.slnx`
- [X] T024 Inspect git status and prepare GitHub repository publication

## Dependencies

- Setup (T001-T005) before all implementation.
- Foundational (T006-T009) before user stories.
- US1 before US2 and US3 because adapters depend on core provider behavior.
- US4 can run after package projects exist.

## Parallel Examples

- T002-T005 can run in parallel after T001.
- T010, T014, T016, and T018 can be prepared in parallel once projects exist.

## Implementation Strategy

Deliver US1 as the MVP, then add web streaming, SQLite persistence, documentation, sample app, and
validation.
