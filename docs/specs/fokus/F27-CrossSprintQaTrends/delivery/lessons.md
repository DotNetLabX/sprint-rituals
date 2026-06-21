# Cross-Sprint QA Trends — Lessons

## Developer Lessons

- [TRACKED] **N+1 correlation requires one extra sprint beyond the selected range** — The defect correlation pairs sprint N coverage with sprint N+1 bug ratio. The endpoint must include one additional sprint in the BugRatioService call so the last selected sprint's next-sprint bug ratio is available. This sprint is filtered out of the QA computation itself (only the target QA sprints are passed to QaTrendsService).
- **AppSidebar needs reactive settings access** — When gating a sidebar nav item on a settings flag, the sidebar must explicitly fetch settings on mount if the settingsStore hasn't been hydrated. The store's default value (xrayEnabled: false) correctly hides the entry before settings load, then shows it if enabled.
- **Pearson r computation uses decimal arithmetic throughout** — Using `Math.Sqrt` requires a `(double)` cast since C# Math.Sqrt takes double. The result cast back to decimal for rounding. Pattern: `(decimal)Math.Sqrt((double)(decimalExpr))`.
- **Vue computed writable for BaseSelect v-model with store** — When binding a BaseSelect v-model to store state that needs a type conversion (number stored as string in the select), use a writable computed with get/set. This avoids template logic clutter and keeps the type conversion in one place.

## Architect Lessons

- **Plan's N+1 correlation data loading was well-specified** — The explicit instruction to "include one additional sprint beyond the selected range" in Step 2 point 9 prevented what would have been a subtle data gap. Future analytics plans with cross-sprint lookups should always call out the boundary sprint explicitly.
- **No ambiguity issues in this plan** — All 7 steps mapped cleanly to implementation. The combination of explicit record definitions, computation rules with BR references, and pattern references to existing files gave the developer enough to work with while leaving decomposition decisions to them. This is the right level of plan detail for analytics features.
- **Sidebar conditional entry pattern now established** — The approach of splitting navItems into baseNavItems + conditional items + tailNavItems with a computed property is a clean pattern. Future plans that add conditional sidebar entries should reference AppSidebar.vue as the pattern file.

## Reviewer Lessons

- **Vue computed reading `.value` of another computed inside a computed getter is valid** — Accessing `discreteMarkers.value` inside the `options` computed in `QualityTrendsChart.vue` is correct Vue 3 reactivity. Vue tracks `.value` accesses inside computed getters. Do not flag this as a reactivity bug — it is idiomatic when the outer computed needs to read from an inner computed.
- **Plan-level `last=0` convention overrides spec wording** — The spec says "All is expressed by omitting `last`". The plan explicitly chose `last=0` as the frontend convention. Always read the plan's implementation notes for API function conventions before checking spec conformance — the plan is the authoritative contract for implementation decisions.

## Skill Gaps

- **Missing skill: multi-sprint metric computation pattern** — Step 1 had no matching skill. A skill covering the pattern of: (1) accepting pre-loaded sprint data dictionaries, (2) iterating sprints to compute per-sprint metrics, (3) aggregating results, and (4) returning a structured response would accelerate future analytics services. Reference files: QaTrendsService.cs, QaMetricsService.cs, BugRatioService.cs.
