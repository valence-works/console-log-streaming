# Console Log Streaming Samples

These samples demonstrate the same backend console streaming flow through several
frontend approaches:

- Blazor Server
- React served by ASP.NET Core
- Vanilla HTML, CSS, and JavaScript served by ASP.NET Core

Each sample hosts its own ASP.NET Core backend, registers the console log
streaming services, maps the default diagnostics endpoints, and provides a UI
that backfills recent lines and streams live console output.

## Shared Behavior

All samples use the same backend routes:

- `GET /diagnostics/console-logs/recent?limit=100`
- `GET /diagnostics/console-logs/sources`
- SignalR hub at `/hubs/console-logs`
- Demo write endpoints under `/demo/*`

The UI should expose the same core workflow in each framework:

- connection status and source summary
- text query filter
- stdout, stderr, and combined stream views
- recent backfill on load and after filter changes
- live streaming through SignalR
- buttons for one stdout line, one stderr line, and a short mixed burst

## Implementation Plan

### Slice 1: Shared Sample Contract

Tasks:

- Define common backend routes for demo writes.
- Keep service registration consistent across samples.
- Keep UI labels, states, and filter semantics aligned.
- Document run commands and expected behavior.

### Slice 2: Blazor Sample

Tasks:

- Create `ConsoleLogStreaming.Sample.Blazor`.
- Add local project references for the ASP.NET Core adapter, core package, and
  SQLite persistence.
- Build a Blazor UI that connects to the SignalR hub and renders recent/live
  lines.
- Add scoped styling for the dashboard, filters, actions, and log rows.
- Verify the project builds with warnings as errors.

### Slice 3: React Sample

Tasks:

- Create `ConsoleLogStreaming.Sample.React`.
- Serve static React assets from `wwwroot`.
- Use the SignalR JavaScript client for live lines.
- Build the same dashboard, filter, action, and log row experience.
- Verify the project builds without requiring an npm install.

### Slice 4: Vanilla HTML + JavaScript Sample

Tasks:

- Create `ConsoleLogStreaming.Sample.Vanilla`.
- Serve static HTML, CSS, and JavaScript from `wwwroot`.
- Use the SignalR JavaScript client for live lines.
- Build the same dashboard, filter, action, and log row experience.
- Verify the project builds without a frontend toolchain.

### Slice 5: Integration and Verification

Tasks:

- Add sample projects to `ConsoleLogStreaming.slnx`.
- Update repository documentation with sample run commands.
- Build all sample projects.
- Run the test suite.
- Start each sample and verify the UI can backfill, connect, and receive new
  stdout/stderr lines.
- Review visual and interaction consistency across all three samples.
