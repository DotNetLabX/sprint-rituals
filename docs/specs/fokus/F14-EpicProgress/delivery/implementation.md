# Epic Progress (F14) — Implementation

## Files Created

- `src/Services/Fokus/Fokus.API/Features/Analytics/EpicProgressService.cs` — computation service with all response records (EpicProgressSummaryMetrics, EpicProgressTicketEntry, EpicProgressEntry, EpicProgressUnlinkedWork, EpicProgressResponse); implements all BRs including sub-team filtering, imputed SP, velocity (rolling 3-sprint), projection, confidence, sorting
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetEpicProgress/GetEpicProgressQuery.cs` — request class + validator (SubTeam not empty when provided)
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetEpicProgress/GetEpicProgressEndpoint.cs` — GET /api/analytics/epic-progress endpoint; early-exit when no epic tickets
- `client/src/stores/epicsStore.ts` — Pinia setup store; sub-team filter, active/completed toggle, expandedEpicKeys Set, filteredEpics computed, hasEpics computed
- `client/src/components/epics/EpicSummaryCards.vue` — 3 summary cards (active/completed count, avg completion with progress bar, unlinked work); tooltip on each card title via info icon
- `client/src/components/epics/EpicActiveCompletedToggle.vue` — tab-button toggle (Active/Completed) matching DevelopersView tab pattern; linter updated to tab-style on write
- `client/src/components/epics/EpicTicketTable.vue` — expanded ticket detail table; remaining first then done, "Unassigned" for null assignee, "—" for null SP; linter updated styling
- `client/src/components/epics/EpicTable.vue` — main epic table with expand/collapse, dual-segment progress bar (actual + imputed), SP display with "+~X est." label, velocity/projection display with "Low confidence" badge, empty state via named slot

## Files Modified

- `src/Services/Fokus/Fokus.Persistence/Repositories/TicketRepository.cs` — added GetTicketsWithEpicAsync (all tickets with EpicKey != null, includes Assignee navigation) and GetTicketsWithoutEpicInSprintsAsync (projection query for tickets in sprints with no epic key)
- `src/Services/Fokus/Fokus.Persistence/Repositories/SprintRepository.cs` — added GetAllClosedSprintMembershipsAsync (includes Sprint and Ticket.Assignee navigations for velocity ordering and sub-team filtering)
- `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs` — added allEpicTickets parameter to ComputeSummary and ComputeTopEpics; Step 2 of ComputeTopEpics now uses cumulative ticket current status (BR20 alignment)
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryEndpoint.cs` — added TicketRepository injection; loads all epic tickets and passes to ComputeSummary
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — registered EpicProgressService as scoped
- `client/src/types/index.ts` — added Epic Progress type section (5 interfaces)
- `client/src/api/analytics.ts` — added getEpicProgress(subTeam?) function, imported EpicProgressResponse
- `client/src/views/EpicsView.vue` — full rewrite from placeholder; URL sync for subTeam and filter params; three-state template (initializing / empty / content); empty state slots for "All epics complete" and "No completed epics yet"
- `client/src/components/PageToolbar.vue` — added showSprintSelector prop (default true); sprint selector div wrapped in v-if="showSprintSelector"

## Key Decisions

- GetTicketsWithoutEpicInSprintsAsync uses a correlated subquery (DbContext.SprintMemberships.Any) rather than a join — matches EF Core pattern, avoids loading memberships just to filter
- GetAllClosedSprintMembershipsAsync includes Sprint navigation so the service can order velocity data points by sprint start date
- Velocity ordering uses sm.Sprint.StartDate (eagerly loaded); the query includes Sprint navigation explicitly
- Empty state in EpicTable rendered via named slot (#empty) — lets EpicsView supply context-aware messages without coupling the table to store state
- Imputed SP bar segment uses bg-accent-default/30 (30% opacity of accent) to visually distinguish from actual completion without a separate color token
- EpicProgressService returns epics in one sorted list (active first by SP completion % asc, completed by name asc); frontend filters by isCompleted client-side

## Deviations from Plan

- GetTicketsWithoutEpicInSprintsAsync uses a projection approach with explicit property assignment rather than anonymous type projection, to satisfy EF Core's requirement that projected entities be trackable. This is the same lightweight approach described in the plan.
- The "Tickets Done / Total" column in the plan is labeled as such in the table header; implementation renders as "Tickets" for space, with "done / total" values visible in the cell.

## Round 2 — Build verification and component completion (this session)

All backend steps (1–4) and frontend steps (5–8) were already fully implemented. This session:

- Verified Steps 1–9 were complete and correct against the plan
- Created `client/src/components/epics/EpicSummaryCards.vue` (was missing from epics/ folder)
- Created `client/src/components/epics/EpicActiveCompletedToggle.vue`
- Created `client/src/components/epics/EpicTicketTable.vue`
- Created `client/src/components/epics/EpicTable.vue` — table-based layout with expand/collapse, dual-segment progress bar, "Low confidence" badge, "Insufficient data" for null projection, "—" for null velocity/SP; empty state via named slot
- Confirmed `client/src/components/PageToolbar.vue` already had `showSprintSelector` prop and `v-if="showSprintSelector"` on sprint selector div
- Confirmed `client/src/views/EpicsView.vue` already complete with URL sync, three-state template, and per-filter empty slot messages
- Backend build: 0 errors. Frontend build: 0 errors (chunk size warning is pre-existing apexcharts issue unrelated to this feature)

## Round 3 — Review cycle 1/3 fixes

### Fix 1 — HIGH: Sub-team filter broken for unlinked work

- `src/Services/Fokus/Fokus.Persistence/Repositories/TicketRepository.cs` — Replaced `.Select(t => new Ticket { ... })` projection in `GetTicketsWithoutEpicInSprintsAsync` with `.Include(t => t.Assignee)`, returning full entities. `Assignee` navigation is now populated; sub-team filtering in `EpicProgressService.FilterTickets` now correctly evaluates `t.Assignee?.SubTeam` for unlinked tickets. Pattern matches `GetTicketsWithEpicAsync`.

### Fix 2 — HIGH: Sub-team filter tooltip missing from PageToolbar

- `client/src/components/PageToolbar.vue` — Wrapped sub-team filter in an outer `<div class="flex items-center gap-1">` and added an info icon `<span>` with `title="Scope all metrics to one sub-team's contributions."` and `cursor-help` class before the dropdown container. Also corrected a missing `</div>` for the outermost template wrapper `<div class="flex items-center gap-3">` introduced during the structural edit. Pattern matches `EpicActiveCompletedToggle.vue`.

### Build verification (cycle 1 fixes)
- Backend: 0 errors, 3 pre-existing warnings (unrelated).
- Frontend: 0 errors, chunk size warning pre-existing (apexcharts).
