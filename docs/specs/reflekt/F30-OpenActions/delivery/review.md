# Open Actions from Previous Retro — Review

## Reviewed By

`reviewer` (Sonnet agent). Codex cross-validation not requested.

## Verdict: APPROVE

Cycle 1 fixes verified. All three MEDIUM findings resolved. No CRITICAL or HIGH issues remain.

---

## Pre-commitment Predictions

| Prediction | Actual |
|-----------|--------|
| Domain guard correctness (None throws, non-ActionItem throws) | Correct — both guards present and correct in `Card.cs:Behaviors` |
| SignalR count queries target wrong sprint | Partially confirmed — counts are correct but use inefficient `GetBySprintIdAsync` with eager EvidenceLinks load |
| PATCH endpoint missing a validation step | Not confirmed — all validations present: SM ownership, Active+Writing phase, previous sprint exists, card belongs to previous sprint |
| Frontend store/SignalR drift (server counts vs local computed) | Not a bug — `applyReviewStatusChanged` only updates `item.reviewStatus`; local computed getters recompute from items. Counts stay in sync. Server counts in payload are unused (open question about intent) |
| GetPreviousActions query correctness | Correct — `GetUnreviewedActionItemsBySprintIdAsync` correctly filters `ReviewedInSprintId == null` |

---

## Findings

### [MEDIUM] Advance warning tooltip from help.tooltips.md not wired

**File:** `web/reflekt-web/src/views/RetroRoomView.vue:173-181`
**Issue:** Plan Step 10 explicitly states "Wire the advance warning tooltip from help.tooltips.md." The `help.tooltips.md` file contains an "Advance warning (unreviewed items)" tooltip: "Some action items from the last retro haven't been reviewed yet. You can still advance if needed." The dialog message paragraph in `onAdvanceClick` for the Writing phase has no `title` attribute, and neither does the dialog element. The tooltip is entirely absent.
**Fix:** Add `:title="advanceWarningTooltip"` to the `<p>` element that renders `advanceDialogMessage` inside `BaseModal` when `currentPhase === 'Writing'`, or to the modal container. Define `const advanceWarningTooltip = "Some action items from the last retro haven't been reviewed yet. You can still advance if needed."` in the script block.

---

### [MEDIUM] `GetBySprintIdAsync` used where a targeted query exists — loads EvidenceLinks unnecessarily

**File:** `src/Services/Reflekt/Reflekt.API/Features/Sprints/AdvancePhase/AdvancePhaseCheckEndpoint.cs:66`
**File:** `src/Services/Reflekt/Reflekt.API/Features/PreviousActions/SetReviewStatus/BroadcastReviewStatusChangedHandler.cs:19`
**Issue:** Both locations call `cardRepository.GetBySprintIdAsync(previousSprint.Id, ct)` to retrieve all cards, then filter in-memory for `CardType.ActionItem` and count. `GetBySprintIdAsync` does `.Include(c => c.EvidenceLinks)`, eagerly loading all evidence links for every card in the previous sprint — data never used in either caller. Targeted repository methods exist (`GetUnreviewedActionItemsBySprintIdAsync` and `GetActionItemsReviewedInSprintAsync`) that avoid this. The plan for Step 7 says to "count unreviewed action items and total action items" but does not specify using the generic query method.
**Fix:** In `AdvancePhaseCheckEndpoint.cs`, replace `GetBySprintIdAsync` + in-memory filter with two queries: `GetUnreviewedActionItemsBySprintIdAsync(previousSprint.Id, ct)` for unreviewed count, and either a new `GetActionItemCountBySprintIdAsync` or a count-only query. In `BroadcastReviewStatusChangedHandler.cs`, use `GetUnreviewedActionItemsBySprintIdAsync` to get remaining unreviewed items and derive counts without loading EvidenceLinks.

---

### [MEDIUM] `SetReviewStatus` domain event fires on same-status re-set (spec says "no-op")

**File:** `src/Services/Reflekt/Reflekt.Domain/Cards/Behaviors/Card.cs:103-114`
**Issue:** The domain method unconditionally sets `ReviewStatus` and `ReviewedInSprintId`, then raises `ReviewStatusChangedEvent` on every call, including when the status is unchanged. The spec states "Setting to the same status is a no-op (200)" and Business Rule 12 states "Review status changes are broadcast to all participants." Broadcasting when no change occurred contradicts the spec's language. The implementation.md documents this as an intentional decision ("avoids a conditional in the domain layer"), but the downstream effect is a spurious SignalR broadcast to all sprint participants on same-status re-submission.
**Fix:** Consider adding a guard: `if (ReviewStatus == status && ReviewedInSprintId == reviewingSprintId) return;` before setting and raising the event. This keeps the domain method semantically clean and avoids unnecessary SignalR noise. The 200 response from the endpoint is unaffected.

---

### [LOW] `BroadcastReviewStatusChangedHandler` payload `reviewedCount`/`totalCount` are received but unused by the frontend store

**File:** `web/reflekt-web/src/stores/previousActions.ts:63-73`
**Issue:** `applyReviewStatusChanged` receives `reviewedCount` and `totalCount` in the payload but only uses `cardId` and `reviewStatus`. The store's computed getters (`reviewedCount`, `totalCount`, `allReviewed`) are derived from local `items` state, so they stay correct. However the server-sent counts are silently dropped. This is not a bug — local computation is correct — but it may confuse future developers who see the payload fields going unused, and it means a discrepancy is undetectable if local and server counts ever diverge.
**Fix:** Either remove `reviewedCount` and `totalCount` from the broadcast payload (if they serve no current purpose), or add a comment explaining why they are intentionally ignored in favor of local computed state.

---

## Positive Observations

- **Domain model is clean and correct.** `ReviewStatus.cs` and `ReviewStatusChangedEvent.cs` are minimal and follow existing event patterns (`CardCreatedEvent`, etc.) precisely. Both guards in `SetReviewStatus` (`None` throws `ArgumentException`, non-`ActionItem` throws `InvalidOperationException`) use the correct exception types matching the codebase convention.
- **PATCH endpoint validation is thorough.** All five failure paths are covered in the correct order: team existence, SM ownership, sprint exists + belongs to team, Active+Writing phase (409), previous sprint exists, card belongs to previous sprint. This matches the plan exactly.
- **`PreviousActionsContracts.cs` fields match exactly.** The shared contract record adds `PreviousSprintId` and `PreviousSprintName` as the plan specified, and both GET and PATCH return it consistently.
- **Optimistic UI with correct revert.** `PreviousActionsPanel.vue` saves the `previousStatus` before optimistic update and reverts on failure — the correct pattern.
- **Report `BuildPreviousActionsSectionAsync` is identical in both report endpoints.** Duplication is obvious but the logic is correct and matches the plan's spec for both eligible-but-unreviewed ("Not reviewed") and reviewed items.
- **Migration is correct and non-breaking.** `ReviewStatus` NOT NULL default 0, `ReviewedInSprintId` nullable FK, `OnDelete(SetNull)`, index on `Sprints.ClosedAt` — all match the plan's data model exactly.
- **Tooltips wired in `PreviousActionsPanel.vue`.** All four in-panel tooltips (section header, Done, Carried Over, Dropped, summary line) match the exact text in `help.tooltips.md`.
- **Frontend store computed values are correct.** `reviewedCount` filters `reviewStatus !== 'None'` — appropriate since `None` is the initial/unreviewed state. `allReviewed` correctly requires `items.length > 0` to prevent a vacuous true.

---

## Gaps

- **No guard against calling `SetReviewStatus` on a card that already has `ReviewedInSprintId` set to a different sprint.** The PATCH endpoint guards by verifying `card.SprintId == previousSprint.Id`, but if a card was already reviewed by sprint N and `ReviewedInSprintId` is set, the endpoint would still allow the current sprint (N+1) to overwrite it. This is unlikely in practice (the GET endpoint filters `ReviewedInSprintId == null`), but the domain method has no guard against overwriting a review from a different sprint.
- **`BuildPreviousActionsSectionAsync` not deduplicated.** The identical private method exists in both `GetSprintReportEndpoint.cs` and `GetParticipantReportEndpoint.cs`. If the logic changes, both must be updated. A shared helper or service would be cleaner, though this is an architecture call, not a code correctness issue.
- **`previousActionsStore` is not reset between rooms.** If a user navigates from one retro room to another, the `items`, `previousSprintId`, and `previousSprintName` state persists until `fetchPreviousActions` completes in `enterRoom`. This could cause a flash of stale data. `enterRoom` runs the fetch in parallel so the window is small, but there is no explicit `reset()` action.

---

## Open Questions

- **Report `reviewedItems` source sprint mismatch (low confidence):** `GetActionItemsReviewedInSprintAsync(sprintId)` returns all cards where `ReviewedInSprintId == sprintId` — their `SprintId` (the sprint they belong to) could theoretically be from a different sprint than the "most recently closed" one if sprint ordering changed. In practice the PATCH endpoint only allows reviewing cards from the most recent closed sprint, so this path should be closed. Mentioning it in case the developer can confirm the PATCH gate makes this impossible.
- **Server-sent `reviewedCount`/`totalCount` in `ReviewStatusChanged` payload intent:** Was the plan to eventually use these for a progress indicator without local computation, or were they always intended to be redundant? The current implementation ignores them. This may be fine, but the redundancy should be documented or removed.

---

## Evidence

### Cycle 1 (initial review)

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| .NET build | PASS | `dotnet build Reflekt.API.csproj --configuration Release --no-incremental` | `Build succeeded. 0 Warning(s) 0 Error(s)` |
| Frontend TS type-check (F30 files) | PASS | `npx vue-tsc --noEmit` filtered to F30 files | No errors in any F30 file |
| Frontend Vite build | FAIL (pre-existing) | `npm run build` | Fails in `TimerControls.vue` from F29 commit `30f2892` — not introduced by this feature |

### Cycle 2 (fix verification)

| Fix | File | Verified | Notes |
|-----|------|----------|-------|
| Domain no-op guard | `Reflekt.Domain/Cards/Behaviors/Card.cs:111-112` | PASS | `if (ReviewStatus == status && ReviewedInSprintId == reviewingSprintId) return;` inserted before mutation and event raise |
| Targeted query — AdvancePhaseCheck | `Reflekt.API/Features/Sprints/AdvancePhase/AdvancePhaseCheckEndpoint.cs:65-67` | PASS | Now calls `GetUnreviewedActionItemsBySprintIdAsync` + `GetActionItemsReviewedInSprintAsync`; no `GetBySprintIdAsync` in Writing branch |
| Targeted query — BroadcastHandler | `Reflekt.API/Features/PreviousActions/SetReviewStatus/BroadcastReviewStatusChangedHandler.cs:18-20` | PASS | Now calls `CountActionItemsBySprintIdAsync` (count-only DB query) + `GetUnreviewedActionItemsBySprintIdAsync`; `GetBySprintIdAsync` removed |
| `CountActionItemsBySprintIdAsync` exists | `Reflekt.Persistence/Repositories/CardRepository.cs:34-36` | PASS | Translates to `CountAsync` — no EvidenceLinks load |
| Advance warning tooltip | `web/reflekt-web/src/views/RetroRoomView.vue:75,169,185,188` | PASS | `advanceWarningTooltip` const defined; `:title` bound on dialog `<p>` conditioned on `advanceIsWritingWarning`; flag set/cleared correctly in `onAdvanceClick` |
| LOW comment on unused payload fields | `web/reflekt-web/src/stores/previousActions.ts:66-69` | PASS | Explanatory comment added inline in `applyReviewStatusChanged` parameter block |
| .NET build (cycle 2) | PASS | `dotnet build Reflekt.API.csproj --configuration Release --no-incremental` | `Build succeeded. 0 Warning(s) 0 Error(s)` |
