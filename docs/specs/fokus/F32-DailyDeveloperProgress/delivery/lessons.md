# DailyDeveloperProgress — Lessons

## Developer Lessons

- **`dotnet sln add --solution-folder` rejects slash-separated paths on Windows.** The flag expects a folder name without path separators. When adding a test project to a solution subfolder, omit `--solution-folder` or use a flat name.
- **Sprint aggregate needs `AddMembership` for test fixtures.** The `_memberships` backing collection on Sprint is private. TDD test setup building Sprints with memberships requires either a public add method or a reflection hack. Adding `AddMembership` is the clean approach — it's a legitimate domain behavior.
- **`SprintState` enum lives in `Jira.Contracts`, not `Fokus.Domain`.** Test projects referencing Sprint state must add `global using Jira.Contracts;` to GlobalUsings.cs — this type does not flow through Fokus.Domain.
- **`dotnet test` with `-o` redirects output to avoid DLL lock conflicts** when the dev server is running. Use `-o /tmp/fokus-tests-out` to build into a temp directory instead of the default `bin/` which may be locked by the running process.
- [TRACKED] **SignalR lifecycle belongs in the store, not the component.** Putting `startSignalR`/`stopSignalR` in the store keeps the Vue component stateless with respect to connection management and makes reconnect logic testable without mounting a component.
- **Best-effort broadcast pattern:** Any SignalR broadcast inside a critical operation path must be wrapped in try/catch with a log warning. Hub connection failure must never propagate to the caller.
- **ApexCharts custom tooltip uses `dataPointIndex` only** — `seriesIndex` is not needed when both series share the same x-axis categories and the tooltip shows a unified view per day.
- **PageToolbar `disabled` prop uses CSS-only approach** (`opacity-50 pointer-events-none` on the select wrapper) — no need to wire `:disabled` to the underlying `<select>` element since pointer-events handles interaction suppression cleanly.

## Architect Lessons

- **Critic review catches cross-store coordination gaps.** The critic found four real issues (sub-team coordination in developersStore, switchTab signature, hub auth, loading/error states) that self-review missed. For features that add tabs to existing multi-tab views with shared store state, critic review is worth the cost.
- **Trace broadcast consumers when planning SignalR.** The plan over-scoped by including SyncBacklogSprintsEndpoint for SignalR broadcast, but backlog sync only touches Future sprints which can never be the active sprint. When planning broadcasts, trace which sync operations actually mutate the data the broadcast consumers care about -- don't assume all sync endpoints are relevant.

## Reviewer Lessons

- **stopSignalR not awaited in switchTab is a recurring async-in-Pinia pattern to watch for.** When a Pinia store action calls another store's async cleanup before proceeding, always check whether the call is awaited. Fire-and-forget cleanup can cause brief overlap of connections on rapid tab switching.
- **The sprints.length guard in multi-tab views can silently block always-visible tabs.** When a tab is specified as "always visible regardless of other conditions," verify that view-level empty-state guards (e.g., no sprints loaded yet) don't wrap the entire tab bar. The guard should only block tabs that genuinely require loaded sprint data, not tabs that manage their own empty states internally.

## Skill Gaps

- **Missing skill: `signalr-patterns`** — Needed for: hub class structure, HubContext injection into FastEndpoints handlers, method name constants pattern, frontend HubConnectionBuilder lifecycle in Pinia stores. Reference files used: `Fokus.API/Hubs/SprintHub.cs`, `Fokus.API/Features/Sync/SyncSprints/SyncSprintsEndpoint.cs`, `client/src/stores/dailyProgressStore.ts`. Suggested coverage: hub registration in DI + Program.cs, broadcast best-effort pattern, frontend store lifecycle (start/stop), reconnect strategy.
