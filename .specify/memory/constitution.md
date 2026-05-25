<!--
Sync Impact Report
Version change: template -> 1.0.0
Modified principles:
- Template placeholders -> Library-First Reuse
- Template placeholders -> Safe Console Data Boundaries
- Template placeholders -> Bounded Runtime Behavior
- Template placeholders -> Testable Public Contracts
- Template placeholders -> Minimal Dependencies
Added sections:
- Package Standards
- Development Workflow
Removed sections:
- None
Templates requiring updates:
- .specify/templates/plan-template.md: reviewed, no project-specific changes required
- .specify/templates/spec-template.md: reviewed, no project-specific changes required
- .specify/templates/tasks-template.md: reviewed, no project-specific changes required
Follow-up TODOs:
- None
-->

# Console Log Stream Constitution

## Core Principles

### I. Library-First Reuse

Every feature MUST be designed as reusable .NET library functionality before any host-specific
adapter is added. Core capture, redaction, buffering, and persistence contracts MUST not depend on
ASP.NET Core, SignalR, Elsa, or any application framework. Framework packages may adapt the core
contracts, but they MUST NOT contain the only implementation of shared behavior.

### II. Safe Console Data Boundaries

Raw stdout and stderr content MUST remain inside the capture and redaction boundary. Stores,
streaming providers, transports, endpoints, and diagnostics observers MUST receive only normalized
and redacted console line events. The project MUST document capture limitations clearly, especially
that managed `Console.Out` and `Console.Error` interception is different from guaranteed
file-descriptor-level process capture.

### III. Bounded Runtime Behavior

The library MUST protect host applications from unbounded memory growth and blocking console writes.
Recent buffers, live subscriber queues, and persistence queues MUST be bounded. Overload behavior
MUST prefer explicit dropped-line or dropped-write accounting over unbounded allocation or
backpressure that can stall application output.

### IV. Testable Public Contracts

Public contracts MUST be small, deterministic, and covered by tests before release. Tests MUST cover
line framing, stdout/stderr identity, redaction, ANSI handling, truncation, bounded buffers, live
subscription behavior, persistence retention, and graceful disposal where applicable. Public API
changes MUST be intentional and reflected in documentation.

### V. Minimal Dependencies

Core packages MUST use only platform libraries and small, justified dependencies. Optional
capabilities such as ASP.NET Core SignalR or SQLite persistence MUST live in separate packages so
applications can adopt only what they need.

## Package Standards

The project targets modern supported .NET applications first. Package names, namespaces, and
repository documentation MUST be neutral and reusable outside Valence Works products. The default
license is MIT. README examples MUST show a minimal console/worker setup, ASP.NET Core live
streaming setup, and optional SQLite persistence setup.

## Development Workflow

Work proceeds through Speckit specification, planning, tasks, and implementation artifacts. Each
feature MUST define independently testable user stories, a technical plan, actionable tasks, and
verification commands. Before handoff, the solution MUST build and the relevant tests MUST pass, or
the failure must be documented with exact commands and observed errors.

## Governance

This constitution supersedes conflicting ad hoc practices in the repository. Amendments require a
documented version bump, a sync impact report, and review of Speckit templates or runtime guidance
affected by the change. Versioning follows semantic governance: MAJOR for incompatible principle
changes, MINOR for new or materially expanded principles, and PATCH for clarifications.

**Version**: 1.0.0 | **Ratified**: 2026-05-25 | **Last Amended**: 2026-05-25
