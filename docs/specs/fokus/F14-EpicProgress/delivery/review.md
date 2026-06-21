# Epic Progress (F14) — Review

## Reviewed By

`reviewer` (Sonnet 4.6 agent) — Sonnet-only review. Codex cross-validation was not requested for this cycle.

## Verdict: APPROVE

Both HIGH findings from cycle 1 are resolved. No remaining CRITICAL or HIGH issues.

**Cycle 1/3 — REQUEST CHANGES:** Two HIGH findings: sub-team filter broken for unlinked work (projection dropped Assignee navigation); sub-team filter tooltip absent from PageToolbar.
**Cycle 2/3 — APPROVE:** Both fixes verified in source. Fresh backend and frontend builds pass. Verdict upgraded to APPROVE.

---

## Pre-commitment Predictions

1. **Tooltip coverage gaps** — Step 9 lists 9 tooltip targets; implementation.md only explicitly confirms summary cards. Predicted: likely partial. **Actual: confirmed gap** — sub-team filter tooltip absent from PageToolbar.vue in cycle 1.
2. **Sub-team filtering on unlinked tickets** — plan Step 1 calls for `Include(t => t.Assignee)` on `GetTicketsWithoutEpicInSprintsAsync`; developer noted a "projection approach." Predicted: risk of missing Assignee. **Actual: confirmed** — projection omitted AssigneeId, Assignee always null, filter silently returned nothing.
3. **F8 alignment / ComputeTopEpics signature** — passing `allEpicTickets` through ComputeSummary to ComputeTopEpics. Predicted: correct but worth checking for sub-team scope leak. **Actual: clean** — sub-team filter applied inside ComputeTopEpics Step 2.
4. **Imputed SP edge cases** — zero-estimated-ticket epics, zero-remaining-unestimated. Predicted: might return 0m where null expected. **Actual: handled correctly** — `imputedSp = 0m` when no remaining unestimated tickets; frontend guards on `> 0` before showing segment.
5. **Projection confidence** — only set when velocity has value. Predicted: might miss the null velocity case. **Actual: correct** — confidence only set inside `if (velocity.HasValue && velocity.Value > 0)` block.

---

## Findings

### Cycle 1 findings (both resolved in cycle 2)

#### [HIGH — RESOLVED] Sub-team filter silently broken for unlinked work

**File:** `src/Services/Fokus/Fokus.Persistence/Repositories/TicketRepository.cs:82-86`

**Cycle 1 issue:** `GetTicketsWithoutEpicInSprintsAsync` used an EF Core projection (`Select(t => new Ticket { ... })`) that did not assign `AssigneeId` and did not include the `Assignee` navigation. All projected `Ticket` objects had `Assignee = null`. Sub-team filter in `EpicProgressService.FilterTickets` therefore excluded all unlinked tickets when any sub-team was selected.

**Cycle 2 fix verified:** The projection is gone. The method now uses `.Include(t => t.Assignee)` and returns full entities — identical pattern to `GetTicketsWithEpicAsync`. Lines 82-86 confirmed:
```
.Where(t => t.EpicKey == null && DbContext.SprintMemberships.Any(sm => sm.TicketId == t.Id))
.Include(t => t.Assignee)
.ToListAsync(ct);
```
Assignee navigation is now populated. Sub-team filtering on unlinked tickets will evaluate correctly.

---

#### [HIGH — RESOLVED] Sub-team filter tooltip missing from PageToolbar

**File:** `client/src/components/PageToolbar.vue:97-105`

**Cycle 1 issue:** No `title` attribute, no info icon, no tooltip of any kind on the sub-team filter in PageToolbar.vue.

**Cycle 2 fix verified:** The sub-team filter section (line 97) is now wrapped in `<div class="flex items-center gap-1">`. An info icon `<span>` at lines 98-105 carries:
- `class="text-xs text-text-muted cursor-help"` — correct styling
- `title="Scope all metrics to one sub-team's contributions."` — exact text from `help.tooltips.md` § Sub-Team Filter

The SVG info icon is present and correct. The outer `</div>` closing the template root `<div class="flex items-center gap-3">` is in place at line 125 — the structural fix noted in implementation.md is confirmed.

---

## Positive Observations

- **Fix 1 pattern matches existing code exactly.** `GetTicketsWithoutEpicInSprintsAsync` now mirrors `GetTicketsWithEpicAsync` — same `.Where()` + `.Include(t => t.Assignee)` + `.ToListAsync(ct)` structure. No new patterns introduced.

- **Fix 2 tooltip text is verbatim from help.tooltips.md.** "Scope all metrics to one sub-team's contributions." — character-for-character match. No paraphrasing.

- **Fix 2 structural integrity.** The developer correctly identified and fixed a missing closing `</div>` for the template root introduced during the structural edit. The template is now well-formed.

- **All eight tooltip targets within epics-specific components remain correctly wired** (unchanged from cycle 1 verification): EpicSummaryCards.vue, EpicTable.vue, EpicTicketTable.vue, EpicActiveCompletedToggle.vue.

- **Velocity computation, BR12 sort, weighted average, F8 alignment, empty-state slot pattern** — all previously verified correct and unchanged.

- **Backend build:** 0 errors, 2 pre-existing NU1903 vulnerability warnings unrelated to this feature.

- **Frontend build:** 0 errors. EpicsView-BlvBOx3H.js chunk emitted at 14.63 kB. Chunk size warning is pre-existing apexcharts issue unrelated to this feature.

---

## Gaps

- **No automated test coverage.** The plan's Testing Strategy lists ~20 backend scenarios. All rely on manual verification. Accepted for v1 per the plan's framing.

- **Active sprint count (BR17) scoping under sub-team filter.** Previously flagged as open question: `activeSprintCount` is computed from `filteredMemberships` (sub-team-filtered), but BR17 says "count of distinct sprints where this epic had at least one ticket in a sprint membership" without qualifying by sub-team. Both interpretations are defensible. Not a blocker for approval — moved to open questions.

---

## Open Questions

- **Active sprint count scoping (BR17 vs sub-team filter):** The service computes `activeSprintCount` from `filteredMemberships`, which is already sub-team-filtered when a sub-team is selected. BR17 does not explicitly qualify "for the selected sub-team." The sub-team filter philosophy (BR13: "all computations restrict to tickets assigned to developers in that sub-team") could support either interpretation. Confidence LOW that this is a bug. Architect should confirm intended behavior before shipping.

---

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Backend build (cycle 2) | PASS | `dotnet build src/Services/Fokus/Fokus.API` | 0 errors, 2 pre-existing NU1903 warnings |
| Frontend build (cycle 2) | PASS | `cd client && npm run build` | 0 errors, chunk size warning pre-existing |
| Fix 1: `.Include(t => t.Assignee)` present in `GetTicketsWithoutEpicInSprintsAsync` | PASS | Read TicketRepository.cs:82-86 | `.Where(...EpicKey == null...).Include(t => t.Assignee).ToListAsync(ct)` |
| Fix 1: Old projection removed | PASS | Read TicketRepository.cs:82-86 | No `.Select()` call present |
| Fix 2: `title=` on sub-team filter | PASS | Grep `title=\|cursor-help\|Scope all metrics` in PageToolbar.vue | Lines 99-100: `cursor-help`, `title="Scope all metrics to one sub-team's contributions."` |
| Fix 2: Tooltip text matches help.tooltips.md | PASS | Compare PageToolbar.vue:100 vs help.tooltips.md §Sub-Team Filter | Verbatim match |
| Fix 2: Template `</div>` structural integrity | PASS | Read PageToolbar.vue:125 | Closing `</div>` present |
| Cycle 1 HIGH findings — any remaining | NONE | — | — |
