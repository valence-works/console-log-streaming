<!-- SPECKIT START -->
For additional context about technologies to be used, project structure,
shell commands, and other important information, read `specs/001-console-log-stream/plan.md`
<!-- SPECKIT END -->

## Workroom execution

For implementation tasks, use Sol 5.6 High for the root agent. If unavailable, fall back in order to the closest available Sol/Terra model at high reasoning, then the closest available frontier model; report the exact fallback used.

Use Luna Extra High for delegates. If unavailable, fall back in order to Luna High, then the closest available model at high reasoning; report the exact fallback used. Treat delegation timeouts or failures separately from model unavailability: after a bounded wait, the root agent continues and owns integration and QA, and reports when no delegated result was available for review.
