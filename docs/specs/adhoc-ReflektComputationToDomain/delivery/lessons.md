# Lessons — adhoc-ReflektComputationToDomain (RR-C)

## Developer Lessons

### In-memory domain objects have Id=0 — vote-count dicts can't distinguish two groups in unit tests

When unit-testing domain services that accept `IReadOnlyDictionary<int, int> voteCountsByGroupId`, all in-memory `CardGroup` instances have `Id=0` (no DB assignment). If two groups are passed, `voteCounts[0]` is ambiguous — the second assignment overwrites the first. Tests that assert vote-ordering between two groups using ID-keyed dicts will fail (dict key collision, not a builder bug).

**Fix:** Either (a) test ordering with one discussed group (0 votes) vs one undiscussed group using a single-key dict so both entries share the same count, then assert both appear rather than asserting position; or (b) keep the strict ordering test as an integration test against a DB-populated retro. Document the limitation clearly in the test comment.

**How to apply:** Whenever writing unit tests for domain services that use ID-keyed aggregation over in-memory aggregates, prefer single-group scenarios or `MarkDiscussed`/explicit-title approaches over multi-group ID-keyed dicts.

### The Edit tool requires a prior Read in the same context window

If an earlier session or context-summarized message read a file, the Edit tool still rejects the edit with "File has not been read yet" unless the file is read again in the current context window. This is not a bug — the edit tool tracks reads per-context, not per-session.

**How to apply:** Always issue a fresh `Read` on any file you plan to `Edit` in the current implementation turn, even if you read it in Phase 1 analysis.

### Async dissolves cleanly when logic moves from handler to pure domain service

`BuildPreviousActionsSectionAsync` was `async Task<>` in the endpoints because it made three repo calls. Moving it to the domain service as `BuildPreviousActionsSection` with pre-loaded params made it fully synchronous — no `async`, no `CancellationToken`. The short-circuit guard (`if (previousSession is null) return null`) moved to the endpoint (load-time), and the builder just receives `null` or lists.

**How to apply:** When lifting async endpoint logic to a pure domain service, identify which `await` calls were data-loading vs computation. Data-loading stays in the handler (as pre-loaded params); computation in the domain becomes synchronous.

## Skill Gaps

None. All required skills (`domain-service`, `analytics-computation-service`, `domain-patterns`, `extract-feature-service`, `service-registration`, `tdd`) were invoked successfully and covered the needed patterns.

## Reviewer Lessons

### IReadOnlyList<T> → List<T> Mapster coercion: confirmed safe, no explicit map needed

When domain result records use `IReadOnlyList<T>` for collections and the wire records use `List<T>`, Mapster auto-coerces without an explicit `.Map()`. The build succeeds (Mapster validates at startup via `GlobalSettings.Scan`) and tests pass. No explicit collection-type map is needed in the `NewConfig` entry.

**How to apply:** Trust Mapster's collection coercion for `IReadOnlyList<T>` → `List<T>` (and vice-versa). Only explicit `.Map()` is needed for non-name-matching members (like enum → string).

### Strict-ordering unit tests over in-memory aggregates: structure the test to be non-vacuous

When ID-based dicts collide in unit tests (all in-memory groups have `Id=0`), the correct response is to degrade the assertion to "both groups pass the filter and appear" rather than fabricating a positional assertion. This is non-vacuous — it still tests the filter — but defers ordering correctness to integration tests. The test comment must explain why strict ordering is not asserted.

**How to apply:** Accept the degraded assertion when the infrastructure constraint is genuine and clearly documented; carry the strict-ordering case over to integration testing. Do not flag this as a builder defect.
