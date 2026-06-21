# Epic Test Health — Review

## Reviewed By

`reviewer` (Sonnet agent, claude-sonnet-4-6). No Codex cross-validation requested.

## Verdict: APPROVE

No CRITICAL or HIGH issues found. Two MEDIUM findings (one dead-code, one minor reactivity convention deviation) and one LOW finding (an extreme edge-case test status). One open question escalated for architect on a spec ambiguity.

---

## Pre-commitment Predictions

| Predicted area | Actual finding |
|---|---|
| BR4 Blocks link inversion logic (complex data structure) | Confirmed correct, but found dead-code loop inside the per-ticket forEach that was replaced by the epicTeIds approach below it (MEDIUM) |
| BR5/BR8 test status edge cases (all-aborted) | Found: all-aborted falls through to "Passed" — technically incorrect per spec wording, but extreme edge case (LOW) |
| Frontend reactivity / storeToRefs for cross-store read | isXrayEnabled computed accesses settingsStore.settings directly — technically reactive, minor convention deviation (MEDIUM) |
| BR22 vs BR17 summary card conflict | Confirmed spec ambiguity — implementation follows the more specific BR17 (open question) |
| Frontend build TypeScript errors | TS error in SettingsView.vue confirmed pre-existing (present in commit 8c75050, before F31 implementation) |

---

## Findings

### [MEDIUM] Dead-code loop in ComputeEpicQaMetrics

**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/EpicProgressService.cs:429-435`

**Issue:** Inside the `foreach (var ticket in featureTickets)` loop, there is a block:
```csharp
var blocksTeIds = qaData.BlocksLinksByTicket.GetValueOrDefault(ticketKey, []);
foreach (var teId in blocksTeIds)
{
    // The Blocks link TicketKey is the bug ticket key
    // We already have blocksLinksByTicket keyed by the linked ticket's key
}
```
The inner loop has an empty body — it does nothing. The actual BR4 bugs-found computation was correctly moved to the post-loop `epicTeIds` approach below (lines 441-457), which is correct. The dead loop is a leftover from an earlier implementation approach. It doesn't affect correctness but is misleading to future readers and violates the C# convention that methods should not contain unreachable or no-op logic.

**Fix:** Remove `var blocksTeIds = ...` and the empty `foreach` block entirely. Keep the correct `epicTeIds`-based implementation below it.

---

### [MEDIUM] Cross-store computed does not use storeToRefs

**File:** `client/src/stores/epicsStore.ts:32-34`

**Issue:** The `isXrayEnabled` computed reads `settingsStore.settings.xrayEnabled` directly:
```typescript
const isXrayEnabled = computed<boolean>(() =>
  settingsStore.settings.xrayEnabled
)
```
The `pinia-patterns` skill (referenced in the plan for Step 5) requires `storeToRefs()` for cross-store reads: "Never destructure state without `storeToRefs` — loses reactivity." While direct property access inside a `computed()` callback is tracked by Vue's reactive system (so reactivity works at runtime), the skill mandates the `storeToRefs` pattern for cross-store reads for consistency and safety. The pattern should be:
```typescript
const { settings } = storeToRefs(settingsStore)
const isXrayEnabled = computed<boolean>(() => settings.value.xrayEnabled)
```

**Fix:** Add `import { storeToRefs } from 'pinia'` and destructure `settings` via `storeToRefs(settingsStore)`, then use `settings.value.xrayEnabled` in the computed.

---

### [LOW] All-aborted test runs fall through to "Passed" status

**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/EpicProgressService.cs:550`

**Issue:** The `BuildTicketEntry` test status derivation has a final fallback:
```csharp
else
    testStatus = "Passed"; // all runs are pass (or aborted only)
```
This branch is reached when: `failCount = 0, passCount = 0, todoCount = 0, executingCount = 0` — meaning all runs are Aborted. BR5 defines "Passed" as "all test runs are PASS." A ticket with only Aborted runs is not "Passed." However, `testPassRate` will be `null` (since `executedRuns = 0`) which partially signals the absence of real results. The practical frequency of this scenario (a ticket whose TEs have exclusively aborted runs) is very low.

**Fix:** Add an explicit branch before the fallback:
```csharp
else if (passCount > 0)
    testStatus = "Passed";
else
    testStatus = "NoTests"; // TEs exist but all runs are aborted — treat as no effective tests
```
Or keep "Passed" only for all-pass: the existing second branch `passCount > 0 && todoCount == 0 && executingCount == 0` already handles that. The final `else` should map to "NoTests" or a new "Aborted" status.

---

## Positive Observations

- **No N+1 queries.** The repository method `GetTestExecutionDataForTicketsAsync` loads all TE links and test runs in exactly 2 queries, then builds in-memory lookups. This is a textbook bulk-load pattern matching the established approach in `GetFeatureTicketsWithCoverageAsync`.
- **`EpicQaData` named record.** Using a named record instead of 3 tuple parameters for the QA data transfer is clean and extensible — aligns with the C# conventions (named records for 3+ field returns).
- **`qaData = null` sentinel.** Using null to gate all QA computation paths is clean: a single null check disables all QA logic without introducing a separate boolean parameter.
- **Conditional rendering discipline.** All frontend QA elements use `v-if="hasQaData"` consistently — columns, progress bar, summary card all respect the feature flag. No QA elements leak through when Xray is disabled.
- **BR12 sub-task inheritance.** The parent-ticket TE fallback is implemented identically in both `ComputeEpicQaMetrics` and `BuildTicketEntry`, and the repository returns the data needed for this without extra queries.
- **KB entry quality.** The `docs/kb/analytics/epic-progress.md` QA section is thorough: formulas, key BRs, data-structure semantics (the Blocks inversion), and file references. Exactly the level of non-obvious business logic that belongs in the KB.
- **Colspan update.** The `EpicTable.vue` expanded row colspan correctly changes from 8 to 11 when `hasQaData` is true — a detail that is easy to miss and was caught.
- **`pr-4` spacing on Sprints column.** The implementation added `pr-4` spacing to the last non-QA column when QA columns are present — a sensible UI fix not explicitly in the plan, correctly noted as a deviation.

---

## Gaps

- **No test for the all-aborted run scenario** (tied to LOW finding above). Edge case but worth a unit test if the codebase ever adds backend tests.
- **BR22 empty state ("Xray enabled, no data synced")** assumes all epics have null coverage. If some epics have feature tickets with 0% coverage (covered = 0 but feature tickets exist), those get `coverageRate = 0%` (not null), so the summary card WOULD show with data. The scenario "truly no data" only occurs when epic TE links don't exist. The QA columns will show "0%" rather than "—" in that case — which matches spec flow 1c ("epic has zero TEs → Coverage 0%") but may look odd to a user who expects "—" when no sync has happened. This is a product nuance, not a code bug.

---

## Open Questions

### BR17 vs BR22: summary card visibility when Xray enabled but no coverage data

**Context:** BR22 says "summary card shows '—'" when Xray enabled but no QA data synced. BR17 says "card appears only when... at least one visible epic has a non-null coverage rate." The implementation follows BR17: card is hidden when `averageTestCoverage` is null (the `v-if="hasQaData && averageTestCoverage !== null"` guard). When Xray is enabled but all epics have null coverage (all bug-only epics, or no sync), the card does not render.

These are contradictory. If a user expects to see a card with "—" (BR22), they won't see the card at all (BR17 implementation). The implementation choice (BR17) is the more defensible product decision — a hidden card is cleaner than a visible "—" card — but it contradicts the spec flow statement.

**Confidence:** LOW (this may be intentional and BR22 may have been superseded by BR17 during spec evolution).

**Escalate to:** Architect to confirm whether the implementation (hide card) is correct, or whether BR22 should be implemented (show card with "—").

---

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Backend build | PASS | `dotnet build Fokus.API.csproj --no-incremental -v quiet` | 0 errors, 13 warnings (all pre-existing, none in F31 files) |
| Frontend build (vue-tsc + vite) | FAIL (pre-existing) | `npm run build` | 1 TS error in `SettingsView.vue:5` — `saveXraySettings` imported but not directly used. Confirmed pre-existing: same import line present in commit `8c75050` (before F31 implementation). Not introduced by F31. |
| Debug artifacts backend | PASS | grep Console.WriteLine/TODO/HACK/FIXME | None found in modified files |
| Debug artifacts frontend | PASS | grep console/TODO/HACK/FIXME | None found in modified files |
| Plan step 1 (repo method) | PASS | Code review | `GetTestExecutionDataForTicketsAsync` implemented, 2-query bulk load, filters non-cancelled TEs, returns (testsLinks, blocksLinks, runsByTeId) tuple |
| Plan step 2 (service extension) | PASS | Code review | All record fields added, `ComputeEpicQaMetrics` and `BuildTicketEntry` helpers, BR1–BR16 implemented |
| Plan step 3 (endpoint QA load) | PASS | Code review | Step 9 in `HandleAsync` gates on `settings.XrayEnabled`, no DB round-trip when disabled |
| Plan step 4 (TS types) | PASS | Code review | All fields added to `EpicProgressTicketEntry`, `EpicProgressEntry`, `EpicProgressResponse` |
| Plan step 5 (epics store) | PARTIAL | Code review | `isXrayEnabled` and `averageTestCoverage` computeds added, `fetchSettings()` in `Promise.all`; `storeToRefs` pattern not followed (MEDIUM finding) |
| Plan step 6 (UI components) | PASS | Code review | All 4 components updated; conditional rendering, RAG coloring, dual progress bar, colspan update all present |
| KB entry | PASS | Code review | `docs/kb/analytics/epic-progress.md` QA section added with formulas, BRs, file references |
| BR1 (feature ticket scope) | PASS | `EpicProgressService.cs` | `featureTickets = epicTickets.Where(t => t.IssueType != "Bug")` |
| BR2 (coverage rate) | PASS | `EpicProgressService.cs` | `coveredCount / featureCount * 100` with Tests link check |
| BR3 (pass rate) | PASS | `EpicProgressService.cs` | PASS/(PASS+FAIL) only, TODO/EXECUTING/ABORTED excluded |
| BR4 (bugs found) | PASS | `EpicProgressService.cs` | epicTeIds set → BlocksLinksByTicket intersection (correct, though dead loop present) |
| BR5/BR8 (test status) | PARTIAL | `EpicProgressService.cs:540-550` | Failed > Passed > InProgress priority correct; all-aborted fallback incorrect (LOW finding) |
| BR9 (cancelled TEs excluded) | PASS | `TestExecutionRepository.cs` | `te.Status != "Cancelled"` join filter |
| BR12 (sub-task inheritance) | PASS | `EpicProgressService.cs` | Parent fallback in both `ComputeEpicQaMetrics` and `BuildTicketEntry` |
| BR13 (zero feature tickets) | PASS | `EpicProgressService.cs` | `if (featureTickets.Count == 0) return (null, null, 0, 0, 0, null, null)` |
| BR16 (average test coverage) | PASS | `EpicProgressService.cs` | Arithmetic mean of non-null coverage rates |
| BR17 (summary card visibility) | PASS | `EpicSummaryCards.vue` | `v-if="hasQaData && averageTestCoverage !== null"` |
| BR18 (dual progress bar) | PASS | `EpicTable.vue` | `h-1 bg-emerald-500` bar below `h-2 bg-accent-default` SP bar |
| BR19 (SP bar unchanged) | PASS | `EpicTable.vue` | SP bar markup identical to original pattern |
| BR20 (RAG coloring) | PASS | `EpicTable.vue` | `ragClass()` maps green/amber/red to `text-status-success/warning/danger` |
| BR21 (Xray disabled) | PASS | `GetEpicProgressEndpoint.cs` + Vue | No QA DB calls when disabled; `hasQaData=false` propagated; all QA elements hidden |
