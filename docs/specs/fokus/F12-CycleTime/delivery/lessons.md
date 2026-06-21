# Cycle Time — Lessons

## Developer Lessons

- `List<int>.Average()` returns `double`, not `decimal`. When mixing with `decimal` metric card parameters, cast explicitly with `(decimal)sprintMetrics.Average(...)` before passing. The build error message (`cannot convert from 'double' to 'decimal'`) points directly at the argument position.

- `BaseCard.vue` accepts no props — only a default slot. Do not attempt to pass `title` as a prop. Use native `title` attribute on inner elements (label, heading div) for tooltip text from help.tooltips.md.

- `AppSettingsRepository.SaveAsync` uses `SetValues()` which handles all scalar properties including nullable strings. Only navigation properties and JSON-converted `List<string>` fields need explicit assignment after `SetValues`. The plan note confirming this is accurate — no modification to the repository was needed.

- When the endpoint loads status transitions for both target and prior sprint (for delta computation), load both sprint IDs together in a single `GetSprintsWithMembershipsAsync` call, then collect ticket IDs from both and load all transitions in one `GetStatusTransitionsForTicketsAsync` call. This avoids two round-trips and the service can compute both sprints from the same transition list.

- EF migration commands must run from the solution root (`<fokus-repo>`), not from a subfolder. The `-p` and `-s` paths are relative to the working directory.

- Sprint-start clamping for cycle time: clamp `enterTime = max(transition.Timestamp, sprintStart)`. Sprint-end clamping: clamp `exitTime = min(nextTransition.Timestamp, sprintEnd)`. When ticket is still in stage at sprint end (no next transition), use `sprintEnd` as exit. This is different from F10's bug time-in-progress which does not clamp (intentional per BR18 note in spec).

## Reviewer Lessons

- The spec's "400 when last < 1" error condition can be intentionally overridden by the plan when a cross-feature convention (last=0 = all sprints) takes precedence. Always check whether spec-vs-implementation differences are documented plan deviations before flagging as HIGH.

- When reviewing cycle time accumulation logic, verify that `StageDurations` is populated only within boundary stages — if the accumulation loop already gates on `startIdx/endIdx`, the funnel logic's separate boundary check is redundant but not harmful.

- Frontend percentile recalculation on the client (scatter plot reference lines) vs backend pre-computed values (metric cards) is an intentional UX pattern: the scatter plot line must respond to the percentile toggle without a server round-trip. Not a bug.

- `TicketId` in the domain (SprintMembership, StatusTransition) IS the Jira issue key string (e.g. "FOK-123") — the Ticket aggregate uses string PK. So using `ticketId` as `TicketKey` in service output records is correct, not a mismatch.

- When a response DTO mixes two different logical categories in one list (e.g., `availableStages` = workflowStages + doneStatuses), downstream consumers cannot distinguish the categories from the list alone. Add a separate scalar field (e.g., `WorkflowStageCount`) to the response record so consumers can check category membership without re-parsing the list. This avoids the BR13 empty-state bug where `availableStages.length > 0` was true even when `workflowStages` was empty.

- The `__Other__` sentinel key pattern: when an accumulation dictionary needs to capture unclassified data alongside classified data, use a sentinel key (e.g., `"__Other__"`) that is guaranteed not to collide with real names. The sentinel is transparent to callers that filter by known names, and explicitly handled by callers that need the "Other" bucket. Ensure that `Values.Sum()` across the dictionary includes the sentinel — decide intentionally whether unclassified time should contribute to totals.

- Codex cross-validation runs on cycle 1 only. Fix-cycle re-reviews (cycles 2+) use normal Sonnet review to verify the specific fixes. Do not re-run Codex on subsequent cycles.

## Architect Lessons

- When a response DTO combines two logical categories into one list (e.g., `availableStages` = workflow stages + done statuses), plan an explicit count or flag field so consumers can distinguish categories without re-parsing. The BR13 empty-state bug (HIGH in review) was caused by `availableStages.length > 0` being true when workflow stages were empty but done statuses existed. Adding `WorkflowStageCount` to the response record was the fix. Future plans should anticipate this pattern when combining heterogeneous lists.

- The critic review caught three real gaps that the architect's self-review missed: (1) `last` parameter convention divergence from existing endpoints, (2) multi-sprint stage funnel not explicitly enumerated in the view step, (3) metric cards not enumerated for multi-sprint mode. All three were resolved before developer started. Critic review is worth the overhead for complex plans — it prevented developer confusion and rework.

- When a plan step says "modify SaveAsync" for new nullable scalar fields, verify first whether `SetValues()` already handles them. The MEDIUM finding from the critic saved the developer from writing unnecessary code. Rule of thumb: `SetValues` copies all scalar properties; explicit assignment is only needed for navigation properties, owned types with `ToJson()`, and `List<T>` with JSON conversion.

- Cross-endpoint convention alignment must be explicit in plans. The `last=0` vs `last>=1` divergence was a spec-vs-codebase conflict. The plan chose codebase convention (correct), but the initial draft did not include frontend mapping guidance. The critic caught this. When a plan deviates from the spec to match existing conventions, the plan must: (a) state which convention wins, (b) show the frontend-to-API mapping, (c) note that the spec should be updated.

- For features with both single-sprint and multi-sprint modes sharing components, the view step must explicitly enumerate which components render in each mode. Saying "conditional rendering: single vs multi" is insufficient — the developer needs the full component list per mode to avoid missing shared components like metric cards and stage funnel in the multi-sprint branch.

## Skill Gaps

- **Missing skill:** No skill covers ApexCharts scatter plot configuration in Vue 3 with custom tooltip functions and series-per-category grouping. Suggested name: `apexcharts-scatter`. Reference files used: `CarryOverRateChart.vue` (line chart pattern), `CycleTimeScatterPlot.vue` (new).

- **Missing skill:** No skill covers the cycle time stage accumulation algorithm (transition-walk, sprint clamping, rework counting). This is bespoke business logic but the pattern of walking ordered transitions with index-based exit detection may recur in future features. Suggested name: `stage-time-accumulation`.
