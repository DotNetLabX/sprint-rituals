# Bug Ratio (F13) — Review

## Reviewed By
`reviewer` (Sonnet 4.6 agent). Codex cross-validation requested — see Cross-Validation section.

## Verdict: APPROVE (Cycle 2 — all issues resolved)

---

## Pre-commitment Predictions

| Predicted problem area | Found? |
|---|---|
| Migration default values (0 instead of spec defaults 50/2) | YES — HIGH finding (alert fires for everyone) |
| Alert streak logic (BR11 zero-SP break vs continue) | No issue — correctly uses `break` |
| Sub-team filter applying to memberships vs developers (BR16) | Correctly applied at developer list level for developer entries |
| `SetValues` not covering List<string> JSON-serialized columns | Not an issue — repo explicitly copies them |
| Frontend `selectedLast` null coalescing edge cases | Pre-existing pattern, not a bug in this feature |
| Stale data on tab switch after filter change | Not predicted — Codex found it. HIGH finding added. |

---

## Findings

### [HIGH] Migration default values are 0, not the spec defaults of 50 and 2 — causes alert to fire for every developer

**File:** `src/Services/Fokus/Fokus.Persistence/Migrations/20260509101214_AddBugRatioAlertSettings.cs:18,24`
**Issue:** Both `BugRatioAlertThreshold` and `BugRatioConsecutiveSprintCount` use `defaultValue: 0` in the migration. The plan specifies defaults of 50 and 2 respectively. For an existing database row (Id=1), the migration sets both columns to 0. `BugRatioService.cs:368` evaluates `consecutiveAbove >= consecutiveCount` — when `consecutiveCount=0`, this is always true regardless of sprint history, so every developer will have `Alert.IsActive = true` on any installation that migrates an existing DB without immediately saving settings. The domain property defaults (`= 50`, `= 2`) only apply to new rows created via `CreateDefault()`, not to the migration column default for existing rows.
**Fix:** Change `defaultValue: 0` to `defaultValue: 50` for `BugRatioAlertThreshold` and `defaultValue: 2` for `BugRatioConsecutiveSprintCount`.

### [HIGH] Bug ratio data goes stale when filters change while on the Throughput tab

**File:** `client/src/stores/developersStore.ts:49,59,67`
**Issue:** `selectSprint`, `selectLastN`, and `selectSubTeam` all re-fetch bug ratio data only when `activeTab.value === 'bugRatio'`. When the user is on the Throughput tab and changes sprint/sub-team, bug ratio data is not refreshed. When they switch back to Bug Ratio, `switchTab` checks `bugRatio.value === null` — since old data is present (not null), it skips the fetch and renders stale data silently. The plan (Step 6) specifies: "Modify `selectSprint()`, `selectLastN()`, `selectSubTeam()` to also re-fetch bug ratio data if the Bug Ratio tab is active" — the condition is correct — but the plan also implies the lazy-load on switch should cover the rest. The lazy-load guard `=== null` is the problem: it should be `=== null || selectionChanged` or bug ratio should simply always be refetched on `switchTab`.
**Fix:** In `switchTab`, remove the `bugRatio.value === null` guard — always refetch when switching to the Bug Ratio tab. This ensures the data reflects the current sprint/sub-team selection regardless of when the filter changed.

### [MEDIUM] Alert badge tooltip shows "threshold" literally instead of the threshold value

**File:** `client/src/components/developers/BugRatioDevTable.vue:64,123`
**Issue:** The plan specifies the tooltip as `"Bug ratio above {threshold}% for {N} consecutive sprints"`. The component renders `"Bug ratio above threshold for ${dev.alert.consecutiveSprintCount} consecutive sprints"` — the word "threshold" is literal text, not the actual percentage value. The `BugRatioAlertStatus` record only carries `IsActive` and `ConsecutiveSprintCount`; the threshold value is not surfaced.
**Fix:** Either (a) add `ThresholdPercent: int` to `BugRatioAlertStatus` in the backend so the frontend can render it without prop drilling, or (b) pass the settings threshold down as a prop from `BugRatioTab` → `BugRatioDevTable`. Option (a) is cleaner and matches the spec intent.

---

## Positive Observations

- All 18 BR business rules are traceable to code: BR1 `IsBug` exact case-sensitive match, BR2/BR4/BR5 `CompletedMemberships`, BR3 ratio formula, BR6 `StoryPoints ?? 0m` for SP but `Count()` for tickets, BR7 zero-SP guard, BR8 ratio-of-totals comment, BR9-BR11 `EvaluateAlert` descending with `break` on zero-SP, BR12/BR13 all active developers in list, BR16 sub-team filter applied to developer list not ticket list, BR17 `OrderBy(StartDate)`, BR18 issue type from completed tickets only.
- `SetValues` + explicit copy pattern is consistent with the existing `AppSettingsRepository`. The new int scalar properties are covered by `SetValues`; JSON-serialized `List<string>` properties are explicitly copied. No regression introduced.
- Endpoint orchestration precisely follows the `GetScopeChangeEndpoint` pattern: lightweight sprint load → early empty return → settings → full memberships → developers → delegate to service.
- Validator covers all plan rules: SprintId > 0, Last ≥ 1, mutual exclusion, SubTeam not empty when present.
- All 14 TypeScript interfaces are complete and match backend records with correct camelCase names and nullable typing.
- Tab lazy-loading is correct — `switchTab` fetches only when `bugRatio.value === null`; `onMounted` handles URL-seeded tab.
- Alert streak `break` on zero-SP is correct per BR11 (not `continue`).
- Delta polarity matches spec BR15: bug ratio % → positive-down, non-bug SP → positive-up, bug SP → positive-down.
- Chart colors match spec: Bug SP `#ef4444`, Non-Bug SP `#3b82f6`.
- `BugRatioStackedChart` aggregates to team-level (sum across developers per sprint) — reasonable and noted deviation that improves chart clarity.
- Settings form Bug Ratio Alerts section placed after Health Weights with correct min/max (0-100, 1-10) and helper text.

---

## Gaps

- No frontend display of the actual alert threshold percentage in the badge tooltip (see MEDIUM finding).
- `BugRatioTrendChart` uses hardcoded amber `#f59e0b` instead of the accent CSS token. Minor style inconsistency with other charts that use theme colors.
- If `store.bugRatio` is null, `store.bugRatioLoading` is false, and `store.bugRatioError` is null simultaneously (impossible in normal flow but possible in edge-case state), the Bug Ratio tab renders nothing. Silent blank — not a bug but a latent UX gap.

---

## Open Questions

- **`>=` vs `>` for alert threshold comparison** (`BugRatioService.cs:358`): Codex flagged `ratio >= threshold` as possibly wrong (spec says "above threshold"). The plan text does not specify the operator explicitly. If "above" means strictly greater than, the fix is `ratio > threshold`. Architect should clarify intent — LOW confidence this is a bug, but worth confirming.

---

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Backend build | PASS | `dotnet build src/Services/Fokus/Fokus.API` | Build succeeded. 0 errors. 2 warnings (NU1903 — pre-existing vulnerability in Microsoft.Build.Tasks.Core, unrelated to this feature). |
| Frontend build | PASS | `npm run build` (client/) | Built in 707ms, no errors. |
| Migration columns | PASS | Read migration file | Two INTEGER NOT NULL columns added; Down() correctly drops them. |
| Migration defaults | MEDIUM | Read migration file | `defaultValue: 0` for both columns — should be 50 and 2. |
| Plan step coverage | PASS | All 10 steps traced | All steps present in implementation.md and confirmed in code. No skipped steps or silent deviations. |
| AppSettingsRepository | PASS | Read current file + git show HEAD~1 | `SetValues` + explicit list copies — new int scalars handled correctly. |

---

## Cycle 2 — Fix Verification

All 4 issues from cycle 1 confirmed resolved:

| Issue | Fix | Verified |
|-------|-----|---------|
| [HIGH] Migration defaultValue=0 | `defaultValue: 50` and `defaultValue: 2` in migration | Read migration file — confirmed |
| [HIGH] Stale data on tab switch | `bugRatio.value === null` guard removed from `switchTab` — always fetches | Read developersStore.ts:74 — confirmed |
| [MEDIUM] Tooltip missing threshold | `${dev.alert.thresholdPercent}%` in both multi and single table | Read BugRatioDevTable.vue:64,122 — confirmed |
| [MEDIUM] `ThresholdPercent` not in record/type | Added to `BugRatioAlertStatus` record and TS interface; passed as `threshold` in `EvaluateAlert` return | Grep BugRatioService.cs + types/index.ts — confirmed |

Backend build: PASS (0 errors). Frontend build: PASS (built in 763ms).

---

## Cross-Validation

Codex independently reviewed the same implementation. Results reconciled below.

### Agreed (both Sonnet and Codex flagged)
- **Migration defaultValue: 0** — Both flagged. Codex correctly identified the downstream consequence (`consecutiveAbove >= 0` always true → every developer alerts). Upgraded from MEDIUM to HIGH based on Codex's analysis.
- **Alert tooltip missing threshold value** — Both flagged. Codex LOW, Sonnet MEDIUM. Kept as MEDIUM.

### Sonnet only (Codex missed)
- None material.

### Codex only (Sonnet missed)
- **Stale bug ratio data when filter changes on Throughput tab** — Codex MEDIUM. Confirmed on code inspection: `switchTab` guard `bugRatio.value === null` prevents refetch after filter changes. Added as HIGH because the user sees silently wrong data with no indication it is stale.
- **"All Sprints" maps to last 5 silently** — Codex HIGH. After investigation: `selectedLast = null` in the store passes `undefined` to the API, which defaults to last 5. However, this is the same behavior as the throughput store (`selectedLast.value ?? undefined`) and is a pre-existing shared store pattern, not introduced by this feature. The Bug Ratio endpoint's "default last to 5" behavior is explicitly documented in the plan (Step 3). NOT added as a finding for this feature.

### Disagreements
- **Codex MEDIUM: `>=` vs `>` for threshold comparison** — Plan text does not specify the comparison operator explicitly. The `>=` implementation is a reasonable reading of "above or equal to threshold." No plan text contradicts it. Moved to Open Questions.
- **Codex MEDIUM: BR16 ticket-level vs developer-level filtering for team metrics** — The plan says "Sub-team filter applies to developers, not tickets (BR16)" in the context of developer entries. For team-level metrics, filtering by ticket assignee sub-team is the only semantically coherent option (there is no other way to attribute a ticket to a sub-team for aggregate counts). This is not a violation of BR16's intent. Not added as a finding.
- **Codex LOW: stacked chart is team-level** — Documented deviation in `implementation.md`. Intentional and noted as Positive Observation.
