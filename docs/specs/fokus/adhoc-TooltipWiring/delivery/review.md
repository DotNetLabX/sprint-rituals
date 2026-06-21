# TooltipWiring — Review

## Reviewed By
`reviewer` (Sonnet agent) — standard Stage 1 + Stage 2 review. No Codex cross-validation requested.

## Verdict: APPROVE

## Pre-commitment Predictions

1. **Tooltip text mismatches** — with 21 files and many strings copied by hand, expected 1–3 paraphrasing errors vs the source .md files. *Actual: 0 mismatches found on all checked strings.*
2. **Inconsistent icon/cursor-help pattern** — some files might apply `cursor-help` to the icon span, others to the heading div. *Actual: Pattern is consistently applied per-context type — headings get `cursor-help` on the div, card labels use info-icon span with `cursor-help`. Matches reference implementations.*
3. **PageToolbar scope creep** — plan says "add title to BaseSelect wrapper" but the diff shows a full `<select>` → `BaseSelect` refactor was bundled in. *Actual: Confirmed. Unmentioned deviation. Assessed as low-risk since the refactor is correct and the behavior is functionally identical.*
4. **Missing tooltip on single-sprint BugRatioDevTable** — plan says add info icon to heading; the single-sprint variant has no heading icon (plain text heading). *Actual: Confirmed. The info icon is on the multi-sprint heading only. Assessed below.*
5. **ClassificationTable heading tooltip text** — plan gave no canonical text. Developer invented a descriptor. *Actual: Confirmed, reasonable text chosen, noted in implementation.md.*

## Findings

### LOW: PageToolbar includes unreferenced refactor beyond tooltip wiring

**File:** `client/src/components/PageToolbar.vue`
**Issue:** The diff shows a full component refactor — raw `<select>` elements replaced with `BaseSelect`, `computed` and `SelectOption` imports added, `currentSelectValue` helper removed, and `onSprintChange`/`onSubTeamChange` signatures changed from `Event`-based to `value: string`. The plan Step 8 only says "Add `title` to the BaseSelect sprint selector wrapper." The refactor is not mentioned in the plan or implementation.md deviations section.
**Impact:** Build passes, behavior is functionally equivalent, and the result is cleaner code. No data risk or logic regression detected. Downgraded to LOW after self-audit.
**Fix:** No code change needed. Recommend documenting this refactor in implementation.md's Deviations section for traceability.

### LOW: Single-sprint BugRatioDevTable heading has no info icon

**File:** `client/src/components/developers/BugRatioDevTable.vue:97`
**Issue:** The multi-sprint heading (`Developer Bug Ratio`) has the info icon + tooltip wired at line 29–37. The single-sprint heading at line 97 is plain text (`Developer Bug Ratio — {sprint.name}`) with no info icon.
**Plan reference:** Step 5 says `"Developer Bug Ratio" heading: add info icon + title="..."` — it does not distinguish between single and multi-sprint variants.
**Impact:** Minor inconsistency only. The multi-sprint view (the more commonly viewed trend view) has the tooltip. Single-sprint is missing it.
**Fix:** Add the same info icon + title to the single-sprint heading div at line 97, wrapped in a `flex items-center gap-1` div matching the multi-sprint pattern.

### LOW: Sub-team filter tooltip in PageToolbar does not match AppShell help.tooltips.md

**File:** `client/src/components/PageToolbar.vue:117`
**Issue:** The sub-team filter info icon reads `title="Scope all metrics to one sub-team's contributions."`. The AppShell `help.tooltips.md` Sub-Team Filter section reads `"Restrict all page content to developers in a specific sub-team, or show everyone with \"All.\""`. These differ in wording.
**Scope note:** Git diff confirms this text was **pre-existing** — it was present in the old `<select>`-based template and carried forward into the new BaseSelect refactor. It was not introduced by this PR. The TooltipWiring plan does not cover the sub-team filter in PageToolbar (Step 8 only targets the sprint selector). This is a pre-existing mismatch, not a regression.
**Fix:** Out of scope for this PR. No action required here. Recommend a follow-up to align the sub-team filter tooltip with the canonical text in AppShell help.tooltips.md.

## Positive Observations

- Every tooltip text string verified against the source `help.tooltips.md` file matched exactly (no paraphrasing, no truncation) across all 7 feature areas checked. This is precise, disciplined work.
- Pattern consistency is excellent: info-icon + `flex items-center gap-1` wrapper for card label tooltips, `cursor-help` + `title` directly on heading divs for chart/table headings — both patterns match the reference implementations in `EpicSummaryCards.vue` and `CycleTimeMetricCards.vue`.
- The `metricTooltip()`, `singleCardTooltip()`, `categoryTooltip()`, and `singleCardTooltip()` (CarryOver) computed functions follow the exact same shape as the pre-existing `cardTooltip()` in `CycleTimeMetricCards.vue`. No new patterns invented.
- The implementation.md key decisions section explicitly documents the ClassificationTable tooltip text decision and the alert badge replacement. Good transparency.
- BugRatioDevTable alert badge correctly replaced a dynamic interpolated `title` with the static canonical text from help.tooltips.md.
- SettingsView `<h2>` headings are correctly wrapped in `<div class="flex items-center gap-1">` to host the icon without altering heading styling — a sensible layout decision.
- Build passes cleanly with zero type errors or warnings attributable to this change.

## Gaps

- No automated tests for tooltip text content (expected — this is a UI text feature). Visual verification per the plan's testing strategy is the appropriate check here.
- The single-sprint variant of BugRatioDevTable is missing its heading tooltip (see Finding above).

## Open Questions

None. All three LOW findings are clear in their assessment and do not require architecture decisions.

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Build | PASS | `cd client && npm run build` | `153 modules transformed. ✓ built in 612ms` — zero errors, zero type errors |
| Tooltip text fidelity | PASS | Manual comparison of all tooltip strings in code against source help.tooltips.md files | All canonical strings matched exactly |
| Pattern conformance | PASS | Compared against EpicSummaryCards.vue and CycleTimeMetricCards.vue reference implementations | SVG icon shape, cursor-help class, flex wrapper — all consistent |
| PageToolbar scope | INVESTIGATED | `git diff HEAD -- client/src/components/PageToolbar.vue` | Full BaseSelect refactor confirmed; behavior-equivalent; pre-existing sub-team tooltip text confirmed pre-existing |
| Security | PASS | All tooltip text is static string literals | No dynamic injection vectors present |
