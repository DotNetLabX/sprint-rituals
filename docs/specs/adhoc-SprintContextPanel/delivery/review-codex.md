# Code Review — adhoc-SprintContextPanel (RA4)

**Reviewer:** Codex cross-check (independent)
**Scope:** Backend-only RA4 as declared. Vue frontend absence is expected (deferred to RA5) — not flagged.
**Verdict:** **GO** — with one Medium documentation correction and one Low observation. No blocking bugs.

---

## Summary

The implementation is correct on every high-risk axis: contract fields are additive, DisruptionRag is wired directly from the analyzer without recomputation, per-sprint gRPC isolation is solid, the anonymization boundary has no leaks, ordering is deterministic and null-dates-last, and the Rule 6 gate behaves exactly as specified. The 10 assembler tests cover every specified scenario; the 3 Fokus-side gRPC tests validate the enriched mapping end-to-end through real domain logic.

One documentation claim in `implementation.md` is factually incorrect — it states that `DisruptionRag` keys on `ScopeDisruptionRate` only, when it actually keys on the combined `DisruptionRate` (scope + bug). This is NOT a bug in the mapping (the behavior is correct for Rule 6 and consistent with the plan's intent), but the written provenance note is wrong and will mislead future reviewers.

---

## Findings

### [MEDIUM] `implementation.md` misstates DisruptionRag's computation source

**File:** `docs/specs/adhoc-SprintContextPanel/delivery/implementation.md` — "Design decisions made during implementation" section.

**Claim (wrong):**
> `DisruptionRate` on the contract is `ScopeDisruptionRate + BugDisruptionRate` (summed). `DisruptionRag` is Fokus's `health.DisruptionRag`, which keys on `ScopeDisruptionRate` only (that is how the Fokus analyzer computes it).

**What the code actually does (`SprintSummaryAnalyzer.cs:257, 497`):**
```
var disruptionRate = scopeDisruptionRate + bugDisruptionRate;   // line 257
// ...
HealthScoreCalculator.MetricRag(metrics.DisruptionRate, ...)    // line 497
```
`metrics.DisruptionRate` is the **combined** scope+bug rate, not the scope-only rate. The RAG and the displayed `DisruptionRate` on the contract both key on the same combined value. There is **no asymmetry** — the documentation invents one.

**Impact on behavior:** None. The mapping `DisruptionRag = health?.DisruptionRag ?? "red"` in `SprintAnalyticsGrpcService.cs:235` correctly reads the analyzer's authoritative band, which *does* key on the combined rate. The Rule 6 gate `DisruptionRag != "green"` is therefore correct. Only the explanation in `implementation.md` is wrong.

**Fix:** Update the implementation.md note to read:
> `DisruptionRag` is Fokus's `health.DisruptionRag`, which keys on the combined `DisruptionRate` (scope + bug) — the same rate exposed on the contract. The RAG and displayed rate are consistent. The plan's note about a "rate-vs-RAG asymmetry" was based on an incorrect reading of the analyzer; no asymmetry exists.

The CLAUDE.md in `Reflekt` (`Fokus linkage` bullet) repeats the same wrong claim about `ScopeDisruptionRate only` — fix that too.

---

### [LOW] `BuildLiveSection` passes `StartDate`/`EndDate` as non-nullable `DateTime` from the contract into a nullable `DateTime?` section field — silent truncation risk on default values

**File:** `src/Services/Reflekt/Reflekt.API/Features/RetroSessions/GetSprintContext/GetSprintContextAssembler.cs:63-64`

The contract `AnonymizedSprintSummary.StartDate` / `EndDate` are `DateTime` (non-nullable, default = `DateTime.MinValue`). If Fokus ever returns a summary where these fields were not set (e.g., if the Fokus gRPC serializer skips uninitialized fields under a future protobuf upgrade, or a sprint has no dates in the domain), the section would carry `StartDate = DateTime.MinValue` instead of `null`, and it would sort to the front of the ordering rather than last.

**Current risk:** Low — `SprintAnalyticsGrpcService.cs:226-228` always populates `StartDate`/`EndDate` from `selectedSprint`, which is required (`LoadSprintSummaryDataAsync` throws `InvalidOperationException` if there are no sprints). But the type system does not enforce this at the boundary.

**Recommendation:** No change required now. If the contract is ever extended or the gRPC serializer changes, the `BuildLiveSection` null-coalescing pattern should be revisited. Worth a comment on the mapping site.

---

## Checklist against specified risks

| Risk axis | Result |
|-----------|--------|
| Contract additive — Orders 1–8 unchanged | PASS. Verified by reading `AnonymizedSprintSummary.cs`: Orders 1–8 are byte-identical; new fields are Orders 9–15. |
| `DisruptionRag` from analyzer, not recomputed | PASS. `SprintAnalyticsGrpcService.cs:235`: `health?.DisruptionRag ?? "red"` — straight from analyzer output. |
| Per-sprint gRPC isolation (Rule 9) | PASS. `GetSprintContextAssembler.cs:49-55`: per-sprint try/catch; failure falls back without affecting other sections. |
| Rule 6 gate: `DisruptionRag != "green"` | PASS. `GetSprintContextAssembler.cs:90-91`. |
| Scope additions 3-cap + overflow math | PASS. `GetSprintContextAssembler.cs:94-100`. `Math.Max(0, top.Count - 3)` with Fokus server-side `.Take(5)` → max overflow = 2. Assembler test slice 7 asserts this. |
| Ordering: earliest StartDate first, null-date sections last, tie-break by SprintId | PASS. `GetSprintContextAssembler.cs:148-151`: `OrderBy(s => s.StartDate.HasValue ? 0 : 1).ThenBy(s => s.StartDate).ThenBy(s => s.SprintId)`. Assembler tests slices 2 and 10 cover both cases. |
| Anonymization boundary — no assignee/developer data in response | PASS. `ScopeAdditionDto` has exactly `TicketKey`, `Summary`, `StoryPoints`. `ScopeAdditionEntry` contract has no assignee field. Assembler test slice 8 asserts the DTO property set structurally via reflection. |
| `cached`/`unavailable` sections have null metrics | PASS. `GetSprintContextAssembler.cs:113-137`: both fallback paths set all metric fields to `null`. |
| gRPC client registration (Reflekt first client) | PASS. `Reflekt.API/DependencyInjection.cs:31-32`: mirrors Fokus's pattern. Config key `Grpc:Services:SprintAnalytics:Url` present in `appsettings.json`. |
| Snapshot `FokusSprintRefs` eager-load | PASS. `SprintRepository.cs:27`: `.Include(s => s.FokusSprintRefs)` with `FirstOrDefaultAsync`. |
| 404 on unknown retroSessionId | PASS. `GetSprintContextEndpoint.cs:24-27`. |
| `[AllowAnonymous]` on the endpoint | PASS. `GetSprintContextEndpoint.cs:12`. |
| No cross-module project references introduced | PASS. Reflekt.API references `Blocks.Contracts` (shared, not a module reference) and `Blocks.AspNetCore.Grpc` (shared). No Fokus.* reference. |

---

## Test coverage assessment

**`Fokus.API.Tests/Grpc/SprintAnalyticsGrpcServiceTests.cs` (3 tests):**
- Sprint identity fields populated: PASS.
- Per-rate RAG fields populated with valid values: PASS.
- Green-path health band (100% completion, 0% disruption → CompletionRag=green, DisruptionRag=green): PASS.
- Note: Tests exercise the full real pipeline including `SprintSummaryAnalyzer` — this is correctly not mocked.

**`Reflekt.Persistence.Tests/SprintContext/GetSprintContextAssemblerTests.cs` (10 tests):**
All 10 specified scenarios from the plan are covered. The fakes (`StubSprintAnalytics`, `ThrowingSprintAnalytics`, `SelectiveThrowingSprintAnalytics`) are clean and scoped to the test file.

No gaps identified relative to the plan's acceptance matrix.
