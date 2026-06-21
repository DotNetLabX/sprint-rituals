# F33-DailyProgressEnhancements — Lessons

## Developer Lessons

- **Stall boundary tests require business-day-aware fixtures.** The `new-stall` direction fires only when a ticket crosses the stall threshold today but not yesterday. Testing this requires last transition to be exactly 3 business days before today — which requires a `SubtractBusinessDays` helper. Additionally, the test must guard against weekends (today is Sat/Sun → stall threshold can't be crossed today → test is vacuously true). Added weekend guard `if (today.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) return;`. For future stall-boundary tests, consider whether `TimeProvider` injection would remove this class of weekend fragility.

- **Intermediate tuple pattern for carrying per-developer auxiliary state.** When a service `Select` loop computes auxiliary data (yesterday's stall set) needed later in the same method but not in the response record, use a tuple return `(Entry, AuxData, ...)` and project to the final list separately. This avoids polluting the response record with internal computation artifacts and keeps the alert-building loop clean.


## Architect Lessons

- **Parameterization plan steps should mention the tuple/auxiliary-data pattern as an option.** Plan Step 2 said "call BuildStalledTickets twice" but the developer correctly used an intermediate tuple to carry yesterday's stall keys through the Select pipeline rather than calling it separately. Both approaches are valid, but the plan was overly prescriptive about *how* to thread the data. Better: describe the *what* (need yesterday's stall set available when building alerts) and let the developer decide the threading mechanism.

- **Five-step plan for a scoped enhancement worked well.** Three independent work streams (bug/feature split, delta alerting, chart fixes) that all touch the same service/component pair fit cleanly into 5 steps with clear dependency ordering (types first, backend parallel, frontend after). No step was over-specified, no step was missed.

## Reviewer Lessons

- [TRACKED] **`IsCompletedInSprint` uses sprint window, not referenceDate — affects yesterday-stall recomputation.** When `BuildStalledTickets` is called with `yesterdayDate` as `referenceDate`, it still calls `IsCompletedInSprint(sprintStart, sprintEnd)` — the full sprint window, not a window ending at yesterday. Tickets completed today are therefore excluded from "yesterday's stall set." This means `stall-resolved` does not fire when a stall resolves via ticket completion (it fires as `improving` instead). Future reviews of stall-detection parameterizations should verify whether `IsCompletedInSprint` and `IsStartedInSprint` need time-bounded variants alongside the existing sprint-window variants.

- **"newly stalled count" vs "total stalled count" is a spec ambiguity, not a code error.** When the plan says "derive stall count from stalledTickets list," that wording is satisfied by `stalledTickets.length`. The display string "newly stalled" implies a different semantic. Future plans should distinguish "count of tickets that crossed the threshold today" (requires backend to compute) from "total currently stalled count" (already available).

## Skill Gaps

