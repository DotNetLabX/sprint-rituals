# DailyDeveloperProgress — Implementation

## Files Created

- `src/Services/Fokus/Fokus.Tests/Fokus.Tests.csproj` — new xUnit test project with FluentAssertions 8.* + NSubstitute 5.*, references Fokus.API
- `src/Services/Fokus/Fokus.Tests/GlobalUsings.cs` — global usings: Xunit, FluentAssertions, Fokus.Domain, Fokus.API.Features.Analytics, Jira.Contracts, Fokus.Tests._Fixtures
- `src/Services/Fokus/Fokus.Tests/_Fixtures/TestData.cs` — builder methods: DefaultSettings, ActiveSprint, ActiveDeveloper, FeatureTicket, BugTicket, Membership, Transition
- `src/Services/Fokus/Fokus.Tests/Features/Analytics/DeveloperProgressServiceTests.cs` — 9 TDD test slices covering: null sprint, pace+breakdown, zero SP, grace period, behind-pace, stall detection, bug SP inclusion, sub-team filter, excluded developer IDs
- `src/Services/Fokus/Fokus.API/Features/Analytics/DeveloperProgressService.cs` — computation service: pace formula, grace period, behind-pace rule, stall detection, daily breakdown, alerts; records: DeveloperProgressSprintInfo, DeveloperProgressAlert, CompletedTicketEntry, DayBreakdownEntry, StalledTicketEntry, DeveloperProgressEntry, DeveloperProgressResponse
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperProgress/GetDeveloperProgressQuery.cs` — request record with optional SubTeam
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperProgress/GetDeveloperProgressEndpoint.cs` — GET /api/analytics/developer-progress; loads active sprint with memberships, developers, capacities, settings, transitions; computes excluded IDs; calls DeveloperProgressService
- `src/Services/Fokus/Fokus.API/Hubs/SprintHub.cs` — SignalR hub class + SprintHubMethods static constants class
- `client/src/stores/dailyProgressStore.ts` — Pinia store: fetchProgress, startSignalR (HubConnectionBuilder → SprintSynced → re-fetch), stopSignalR
- `client/src/components/developers/DailyProgressTab.vue` — tab layout: empty states (no sprint / no devs), grace period note, behind-pace alert banner, sprint info line, developer card grid
- `client/src/components/developers/DeveloperProgressCard.vue` — per-developer card: avatar, SP progress line, ApexCharts mini burnup (actual solid + expected dashed), behind-pace border+badge, stall badge with expandable ticket list
- `docs/kb/analytics/daily-progress.md` — new KB entry: pace formula, grace period, behind-pace, stall detection, SignalR auto-refresh

## Files Modified

- `src/Fokus.slnx` — registered Fokus.Tests project (without solution folder; --solution-folder flag rejected slashes in folder name)
- `src/Services/Fokus/Fokus.Domain/Sprint/Behaviors/Sprint.cs` — added `AddMembership(SprintMembership)` public method needed by test fixtures to build Sprint with memberships without hitting private backing collection
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — added `services.AddSignalR()`, `services.AddScoped<DeveloperProgressService>()`
- `src/Services/Fokus/Fokus.API/Program.cs` — added `app.MapHub<SprintHub>("/hubs/sprint")`
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncSprints/SyncSprintsEndpoint.cs` — injected `IHubContext<SprintHub>` and `ILogger<SyncSprintsEndpoint>`; broadcasts SprintSynced after successful sync in best-effort try/catch
- `client/src/types/index.ts` — added Daily Progress types: DeveloperProgressSprintInfo, DeveloperProgressAlert, CompletedTicketEntry, DayBreakdownEntry, StalledTicketEntry, DeveloperProgressEntry, DeveloperProgressResponse
- `client/src/api/analytics.ts` — added getDeveloperProgress(subTeam?)
- `client/src/stores/developersStore.ts` — added dailyProgress to activeTab union type; selectSprint/selectLastN guard (no-op on dailyProgress tab); selectSubTeam re-fetches progress; switchTab starts/stops SignalR lifecycle
- `client/src/components/PageToolbar.vue` — added `disabled?: boolean` prop; sprint selector gets `opacity-50 pointer-events-none` when disabled
- `client/src/views/DevelopersView.vue` — added dailyProgressStore, onUnmounted (stopSignalR), URL sync for daily-progress tab, Daily Progress tab button (always visible, last), Daily Progress tab content block, `:disabled` binding on PageToolbar
- `docs/architecture/v1.md` — added SignalR section under Cross-Cutting Concerns; removed SignalR from out-of-scope list; added signalr-patterns gap entry
- `docs/kb/cross-cutting.md` — added Daily Progress exception note under Feature-Only Metrics: DeveloperProgressService uses all ticket types and non-transition-gated assigned scope (not feature-only)
- `docs/kb/frontend-map.md` — updated DevelopersView row (6 tabs, dailyProgressStore, SignalR), added SignalR lifecycle pattern, added DailyProgressTab + DeveloperProgressCard to component locations
- `docs/kb/index.md` — registered daily-progress KB entry

## Key Decisions

- **All ticket types included in progress** (not feature-only): DailyProgress tracks actual delivery pace across bugs, tasks, features — throughput's feature-only filter does not apply here.
- **Assigned SP = all non-removed memberships** (not transition-gated): represents committed scope, not started scope, for pace target accuracy.
- **Business day counting excludes transition day**: `daysSinceLastTransition` starts counting the day after the transition, consistent with "how many full business days have passed."
- **Sprint selector disabled on Daily Progress tab**: daily progress is always active sprint — no sprint selection makes sense. PageToolbar `disabled` prop uses CSS-only approach (no JS event blocking needed, pointer-events handles click suppression).
- **SignalR broadcast best-effort**: wrapped in try/catch in SyncSprintsEndpoint so hub connection failure never breaks sync response.
- **AddMembership on Sprint aggregate**: added to support test fixture construction. Sprint's `_memberships` backing collection was private with no public add method. This is a legitimate behavior method (aggregates can expose collection mutation).
- **Fokus.Tests registered without solution folder**: `--solution-folder` flag with path containing slashes fails on Windows; registered at solution root instead.

## Review Fixes (Cycle 1)

- `client/src/stores/developersStore.ts` line 119 — added `await` to `dailyProgressStore.stopSignalR()` in `switchTab`. The disconnect promise was previously dropped.
- `DevelopersView.vue` Daily Progress tab guard — verified non-issue: `GetAnalyticsSprintsAsync` includes `SprintState.Active` sprints, so `store.sprints` is always populated when an active sprint exists. Tab is correctly inside the `sprints.length > 0` guard.

## Deviations from Plan

- **SyncBacklogSprintsEndpoint not modified**: Q1 answered Option B — skip entirely. Only SyncSprintsEndpoint modified for SignalR broadcast.
- **dailyProgressStore reads selectedSubTeam from developersStore on SprintSynced**: rather than storing subTeam locally, the store reads `useDevelopersStore().selectedSubTeam` at event time. This avoids state duplication.
