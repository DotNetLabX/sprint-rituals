# Test Execution Timeline — Review

## Reviewed By

`reviewer` (Sonnet agent). No Codex cross-validation requested.

---

## Re-review — Cycle 1 fixes (Cycle 1/3)

**Verdict: APPROVE**

All four findings from Cycle 1 (HIGH-1, HIGH-2, LOW-sealed, LOW-IReadOnlyList) are correctly resolved. Build passes 0 errors. No regressions introduced.

### HIGH-1 — Prior sprint transitions (RESOLVED)

Fix verified at `GetTestTimelineEndpoint.cs:73-86`:
- Single `GetStatusTransitionsForSprintTicketsAsync` call now uses `[sprint.Id, priorSprint.Id]` when a prior sprint exists.
- Transitions split into `statusTransitions` (filtered by `currentTicketIds`) and `priorStatusTransitions` (filtered by `priorTicketIds`) — two independent `HashSet<string>` membership scopes.
- `ComputeTimeline` signature extended with `List<StatusTransition>? priorStatusTransitions` (line 106).
- `priorTransitionsByTicket` built as its own dictionary from `priorStatusTransitions` (lines 123-127) — no longer an alias.
- `ComputeDevToTestGap` guard extended to require `priorTransitionsByTicket is not null` (line 527) before computing delta. Correct.

### HIGH-2 — "Xray enabled, not synced" empty state (RESOLVED)

Fix verified:
- `TestTimelineResponse` record now has `bool IsXrayEnabled` as second positional field (line 7).
- `BuildEmptyResponse` sets `isXrayEnabled: false` when Xray disabled, `isXrayEnabled: true` when Xray enabled but no runs (endpoint lines 35, 50).
- `IsXrayEnabled: true` set on the full response (service line 147).
- `types/index.ts:1270` — `isXrayEnabled: boolean` in `TestTimelineResponse` interface.
- `SprintsView.vue:256` — outer gate is now `store.testTimeline?.isXrayEnabled` (section hidden when Xray off).
- `SprintsView.vue:262-263` — inner `v-if="!store.testTimeline.hasQaData"` renders "Sync sprint to load QA data." prompt correctly.

### LOW — `sealed record` (RESOLVED)

All 11 public response records confirmed `sealed` (lines 5, 18, 28, 35, 42, 49, 56, 63, 68, 75, 81, 89).

### LOW — `IReadOnlyList<T>` (RESOLVED)

All collection properties on records confirmed `IReadOnlyList<T>` (lines 11, 12, 40, 54, 66, 79). Internal helpers still return `List<T>` locally — correct, as `List<T>` is assignable to `IReadOnlyList<T>`. Build confirms no type errors.

### Re-review evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Backend build | PASS | `dotnet build Fokus.API.csproj` | 0 errors, 2 warnings (pre-existing NU1903 only) |
| HIGH-1 transition scope | PASS | grep priorStatusTransitions / sprintIdsForTransitions | Both sprint IDs fetched; distinct dictionaries built |
| HIGH-2 sync prompt | PASS | grep isXrayEnabled / "Sync sprint" in SprintsView.vue | Outer gate on isXrayEnabled; inner prompt on !hasQaData |
| sealed records | PASS | grep "sealed record" TestTimelineService.cs | All 11 public records sealed |
| IReadOnlyList | PASS | grep IReadOnlyList TestTimelineService.cs | 5 collection properties on records use IReadOnlyList |
| No regressions | PASS | Build 0 errors | IReadOnlyList assignable from List<T>; no call sites broken |

---

## Original Review — Cycle 1 (REQUEST CHANGES)

### Verdict: REQUEST CHANGES

Two HIGH issues require fixes before approval.

---

## Pre-commitment Predictions

Predicted problems going in (based on feature complexity: multi-metric computation service, prior sprint delta, static method reuse):

1. **Prior sprint delta using wrong transitions** — probable given the endpoint loads transitions for one sprint ID only. CONFIRMED HIGH.
2. **Chart annotation merge bug** — ApexCharts annotation object spread pattern is easy to get wrong. Investigated: the spread correctly combines both arrays. NOT A BUG.
3. **Empty state gap** — "Xray enabled, not synced" state collapses to hidden rather than showing prompt. CONFIRMED HIGH.
4. **Records not sealed** — convention check. CONFIRMED LOW.
5. **Endpoint 404 for active sprints** — active sprint silently returns 404. INVESTIGATED — likely intentional (single-sprint view is closed sprints only). Moved to open questions.

---

## Findings

### [HIGH] Prior sprint dev-to-test gap uses wrong status transitions

**File:** `src/Services/Fokus/Fokus.API/Features/Sprints/GetTestTimeline/GetTestTimelineEndpoint.cs:58`
**Issue:** Status transitions are loaded only for the selected sprint (`[sprint.Id]`). The prior sprint (for gap delta computation) is loaded separately but its tickets' status transitions are never fetched. In `TestTimelineService.cs:518`, `priorTransitionsByTicket = transitionsByTicket` is just a reference alias — it refers to the identical dictionary built from the current sprint's ticket IDs only.

When `ComputeGapTickets` runs for the prior sprint (line 519), it looks up prior sprint ticket keys in `priorTransitionsByTicket`. Tickets that existed only in the prior sprint (not in the current sprint's membership) will have no entry in the dictionary, so `devDone` will always be `null`, and those tickets will be excluded from the gap calculation. The prior sprint median gap will be under-counted or zero, making the delta wrong.

**Evidence:**
- `GetTestTimelineEndpoint.cs:58` — `GetStatusTransitionsForSprintTicketsAsync([sprint.Id])` — only current sprint ID
- `GetTestTimelineEndpoint.cs:79` — the same `statusTransitions` list is passed to `ComputeTimeline`
- `TestTimelineService.cs:518` — `var priorTransitionsByTicket = transitionsByTicket; // same transitions dictionary loaded for window`
- `TestTimelineService.cs:519` — `ComputeGapTickets(priorSprintTEs, filteredPriorMemberships, priorTransitionsByTicket, ...)` — wrong dictionary

**Fix:** Load transitions for both sprints: `GetStatusTransitionsForSprintTicketsAsync([sprint.Id, priorInfo.Id], ct)` in the endpoint. The repository method already accepts a list of sprint IDs and union-queries them correctly (seen at `TicketRepository.cs:81-95`). Alternatively, load prior sprint transitions separately and pass them distinctly to `ComputeTimeline`.

---

### [HIGH] Missing "Xray enabled but not synced" empty state — section silently hidden

**File:** `client/src/views/SprintsView.vue:256`
**Issue:** The spec (Flow 8b) and plan (Step 7) both explicitly require: when Xray is enabled but the sprint has not been synced with QA data, the section shows "Sync sprint to load QA data" (matching the F26 pattern). The endpoint returns `hasQaData: false` for two distinct cases: (a) Xray disabled, (b) Xray enabled but zero test runs/TEs. Both return the same response shape. The frontend gates the entire section on `store.testTimeline?.hasQaData` — so for case (b), the section is hidden entirely, no prompt shown.

The spec treats these as distinct empty states:
- 8a: Xray disabled → section not rendered (correct)
- 8b: Xray enabled, not synced → "Sync sprint to load QA data" prompt (missing)
- 8c: Zero test runs → empty chart + hidden sections (partially covered)

There is no way to distinguish 8b from 8a at the frontend given the current API response. Either the backend needs a new field (e.g. `isXrayEnabled: bool`) in the response, or the frontend needs access to `settingsStore.settings.xrayEnabled` to differentiate.

**Evidence:**
- Spec `spec.md:248-249` — "Xray enabled, sprint not synced with QA data: section shows 'Sync sprint to load QA data' prompt"
- Plan Step 7 — "Empty state: when `hasQaData` but Xray not synced → show 'Sync sprint to load QA data'"
- `SprintsView.vue:256` — `v-if="store.sprintMode === 'single' && store.testTimeline?.hasQaData"` — no sync prompt rendered
- Search for "Sync sprint" text in `client/src/` — not present in SprintsView or any F30 component

**Fix:** Add `isXrayEnabled: bool` to `TestTimelineResponse` (or reuse `settingsStore` which is already accessible in other views). When `store.testTimeline` is loaded, `!hasQaData && isXrayEnabled` → show sync prompt; `!isXrayEnabled` → hide section entirely.

---

### [LOW] Response records not marked `sealed`

**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/TestTimelineService.cs:5-91`
**Issue:** All response records (`TestTimelineResponse`, `BurnupDayEntry`, `ScopeChangeDayEntry`, etc.) use `public record` without `sealed`. The C# conventions require `sealed record` for DTOs. The internal `TerminalRunEntry` (line 676) is correctly `sealed record`. The public-facing records are inconsistent with this.

**Fix:** Add `sealed` to all 11 public response records in the file.

---

### [LOW] Record collection properties use `List<T>` instead of `IReadOnlyList<T>`

**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/TestTimelineService.cs:5-91`
**Issue:** The C# convention states "IReadOnlyList<T> for collection properties on records — shallow immutability doesn't protect List<T> contents." Records like `TestTimelineResponse`, `TestingCrunchResult`, `PostSprintTestingResult`, `UntestedAtCloseResult`, `DevToTestGapResult` all use `List<T>` for their collection properties.

**Fix:** Change collection properties on records from `List<T>` to `IReadOnlyList<T>`. The internal helper methods that build them can still use `List<T>` locally.

---

### [LOW] Confusing but functionally correct annotation merge in BurnupChart

**File:** `client/src/components/sprints/TestExecutionBurnupChart.vue:175-178`
**Issue:** The `annotations` object is constructed as:
```js
annotations: {
  xaxis: xAnnotations,
  ...(backgroundAnnotations.length > 0 ? { xaxis: [...backgroundAnnotations, ...xAnnotations] } : {})
}
```
The first `xaxis` key is overwritten by the spread when `backgroundAnnotations.length > 0`. This is functionally correct (the combined array is what's needed) but the first `xaxis: xAnnotations` assignment is dead code when there are any background annotations. The intent is clearer as:
```js
annotations: {
  xaxis: backgroundAnnotations.length > 0
    ? [...backgroundAnnotations, ...xAnnotations]
    : xAnnotations
}
```
**Fix:** Simplify the expression (optional — no behavioral impact).

---

## Positive Observations

- **Business rule fidelity is excellent.** All 11 business rules from the plan are implemented exactly: terminal-status filter, effective date resolution (FinishedAt ?? StartedAt), crunch threshold (>50% strictly), post-sprint boundary, untested-at-close logic (zero terminal runs, not zero TEs), negative gap preservation, and the static `ComputeCrunchFlag` reuse pattern.

- **The `priorTransitionsByTicket` comment is self-documenting.** The developer correctly identified this was a shared dictionary and added a comment. The intent was clearly to reuse the existing dictionary as a performance shortcut, but the scope mismatch between current and prior sprint was not caught.

- **DI design is clean.** Making `ComputeCrunchFlag` static eliminates an unnecessary DI dependency in `GetSprintSummaryEndpoint`. The deviation from the plan (plan said to inject `TestTimelineService`) is correct and well-justified in `implementation.md`.

- **TypeScript types are complete and accurate.** All 11 interfaces match the C# records exactly. The `FlagsResult` extension with optional `testingCrunch` field is correct.

- **Chart implementation is solid.** Phase shading (planning/execution/crunch zones), scope change vertical markers, post-sprint separator, and the custom dark tooltip all implemented. The BurnupChart follows the existing `BurnupChart.vue` pattern as required.

- **Sub-team filter guard in `CollectTerminalRunsWithDates:617`** correctly handles the no-filter case (`filteredMemberships.Count > 0` check) to avoid filtering all TEs when no sub-team is selected.

- **KB entry is accurate and well-structured.** Covers all computation formulas, the static method design decision, and relationship to F26.

- **Build passes clean** with 0 errors. All warnings are pre-existing (from `QaWorkloadService.cs` — out of scope).

---

## Gaps

- **No unit tests shipped.** The plan's Testing Strategy calls for `TestTimelineService` unit tests covering burnup accumulation, crunch threshold boundary (exactly 50% vs >50%), zero-denominator, and negative gap values. These are not in the diff. The plan listed them under Testing Strategy (not as acceptance criteria), so this is a documentation gap rather than a blocking issue, but the computation service is well-suited for unit testing and the absence is notable.

- **`hasQaData=false` doesn't distinguish "zero TEs" from "has TEs but all non-terminal."** The endpoint checks `sprintTEs.Any(te => te.TestRuns.Any())` (line 47) — returns false if all TEs have zero runs. But a sprint could have TEs with only TODO/EXECUTING runs (non-terminal), which would also produce `hasAnyRuns = true` and proceed to compute an empty burnup. The service handles this correctly (returns empty burnup), but the `hasQaData=true` in the response would be misleading for truly non-started TEs.

---

## Open Questions

- **Active sprint support:** The endpoint searches only `GetClosedSprintsAsync`, so an active sprint ID returns 404. The spec's user flows show the Sprints page single-sprint view which appears to be closed-sprint only. If active sprints are ever expected to have test timelines, this will need extension. Flagged as design question — not a defect given current scope.

- **Prior sprint transitions scope:** Related to the HIGH finding — if the intent was that prior sprint gap delta doesn't matter much (it's "best effort"), the shared dictionary approach could be considered acceptable with a comment clarifying the limitation. However, the spec explicitly describes the delta as a meaningful metric, so this is treated as a bug. Escalate if architect disagrees.

---

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Backend build | PASS | `dotnet build Fokus.API.csproj` | 0 errors, 13 pre-existing warnings (all in QaWorkloadService.cs) |
| TypeScript check | PASS | `npx tsc --noEmit` | Exit code 0, no errors |
| Debug artifacts | PASS | grep for Console.WriteLine, TODO, HACK, FIXME | None found in F30 files |
| Prior sprint transitions | FAIL | `grep -n "priorTransitionsByTicket" TestTimelineService.cs` | Line 518: same dict reference as current sprint |
| Endpoint transition scope | FAIL | `grep -n "GetStatusTransitionsForSprintTickets" GetTestTimelineEndpoint.cs` | Line 58: only `[sprint.Id]` — prior sprint not included |
| Sync prompt | FAIL | `grep -rn "Sync sprint to load" client/src/` | Not present in SprintsView or F30 components |
| Tooltip text match | PASS | Manual comparison vs help.tooltips.md | All tooltip strings match exactly |
| F30 type interfaces | PASS | Diff review of types/index.ts | All 11 interfaces correct, match C# records |
| Plan step conformance | PASS | All 7 plan steps traced to implementation | Steps 1-7 all have corresponding code |
