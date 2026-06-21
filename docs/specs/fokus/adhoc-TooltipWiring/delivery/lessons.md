# TooltipWiring — Lessons

## Architect Lessons
- Many tooltip definitions in help.tooltips.md describe backend behavior concepts (e.g., Rate Limiting, Assignee Attribution, Commitment Status) that have no corresponding UI element. Future help.tooltips.md files should distinguish between "UI tooltip" (wirable to an element) and "documentation note" (informational only) to avoid confusion about coverage gaps.
- The tooltip wiring pattern is inconsistent across features: CycleTime and EpicProgress were fully wired at implementation time, while SprintSummaryCard, CarryOverTracker, BugRatio, ScopeChangeDisruption, DeveloperThroughput were not. The help-tooltips rule in `.claude/rules/help-tooltips.md` exists but was evidently not enforced during those features' implementation. Consider making tooltip wiring a reviewer checklist item.
- AppShell tooltips are mostly structural descriptions (sidebar, layout, design tokens) rather than metric explanations. These don't map to info-icon tooltips. The help.tooltips.md format works well for analytics features but less well for chrome/navigation features.
- The established tooltip pattern (info-icon SVG + `title` attribute + `cursor-help` class) is lightweight and consistent. No need for a dedicated tooltip component at current scale.

## Developer Lessons
- When a heading uses a plain `<div class="text-sm font-medium...">` (chart/table cards), the simplest tooltip pattern is `cursor-help` + `title` directly on the div — no wrapper needed. Reserve the `flex items-center gap-1` wrapper + info icon SVG for cards where a visible icon is desired beside a label.
- SettingsView uses `<h2>` elements for section headings, not `<div>` labels, so info icons require wrapping with `<div class="flex items-center gap-1">` rather than just adding attributes to the h2.
- `replace_all: true` on Edit is safe for delta span patterns when the same pattern appears multiple times and should receive the same treatment — avoids multiple sequential edits on the same block.
- When a plan says "add info icon" to a heading without providing tooltip text, use a neutral descriptor of what the section shows. Do not leave the title empty.
- Dynamic interpolated titles (like alert badge template literals) should be replaced with static text when help.tooltips.md specifies a fixed string.

## Skill Gaps
- **Missing skill:** A tooltip wiring skill covering the three established patterns (cursor-help+title on div, info-icon span beside label, title on th) with code snippets would save exploration time. Suggested name: `wire-tooltips`. Reference files: `EpicSummaryCards.vue`, `CycleTimeMetricCards.vue`, `EpicTable.vue`.

## Reviewer Lessons
- For tooltip wiring reviews, the most efficient verification path is: (1) read all source help.tooltips.md files first, (2) read all modified Vue files in parallel batches, (3) compare tooltip strings in code against canonical source. String-by-string comparison is the critical check — pattern conformance is secondary.
- When a plan step says "add X to Y" and the diff shows a full component refactor (e.g., raw `<select>` → `BaseSelect`), use `git diff HEAD -- <file>` to determine whether the refactor was bundled in silently. Check implementation.md deviations section — if the refactor is not listed there, flag it as LOW even if the code is correct, because traceability matters.
- Pre-existing tooltip text mismatches (text present before the current PR) should be noted as open gaps rather than findings, with a clear "pre-existing" label. Use `git show HEAD:<path>` to confirm the text predates the PR when in doubt.
- For multi/single-sprint component variants (like BugRatioDevTable), always check both template branches for tooltip coverage, not just the first branch encountered.
