# Sync Refactoring — Implementation

## Files Created

- `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsQuery.cs` — extracted `GetSettingsResponse` from endpoint file; follows `GetJiraSprintsQuery.cs` pattern
- `src/Services/Fokus/Fokus.API/Features/Sync/GetBoards/GetBoardsQuery.cs` — extracted `GetBoardsResponse` and `BoardDto` from endpoint file
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncBacklog/SyncBacklogCommand.cs` — extracted `SyncBacklogResponse` from endpoint file; follows `SyncSprintsCommand.cs` pattern
- `src/Services/Fokus/Fokus.API/Features/Sync/SprintIssueSyncService.cs` — focused operation service owning the shared sprint-issue sync loop; includes nested `SprintIssueSyncResult` record

## Files Modified

- `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsEndpoint.cs` — removed `GetSettingsResponse` class and unused `Fokus.Domain` using
- `src/Services/Fokus/Fokus.API/Features/Sync/GetBoards/GetBoardsEndpoint.cs` — removed `GetBoardsResponse` and `BoardDto` classes
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncBacklog/SyncBacklogEndpoint.cs` — removed `SyncBacklogResponse` class; added `SprintIssueSyncService` to constructor; replaced inner foreach loop (sprint-issue section) with `syncService.SyncAsync(sprint, issues, forcedNotCommitted: true, ct)`; added `Fokus.API.Features.Sync` using
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncSprints/SyncSprintsEndpoint.cs` — removed `TicketRepository` and `DeveloperRepository` from constructor; added `SprintIssueSyncService`; replaced inner foreach loop with `syncService.SyncAsync(sprint, issues, forcedNotCommitted: false, ct)`; maps `result.TicketsUpserted` and `result.DevelopersDiscovered` to response; added `Fokus.API.Features.Sync` using
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — added `services.AddScoped<SprintIssueSyncService>()` and `Fokus.API.Features.Sync` using
- `CLAUDE.md` — amended "No service layer classes" guardrail to distinguish entity-wrapper services (prohibited) from focused operation services (allowed)
- `src/Services/Fokus/CLAUDE.md` — added "Focused operation services" bullet under Key patterns

## Key Decisions

- `SprintIssueSyncService` is in namespace `Fokus.API.Features.Sync` (not a subfolder), consistent with the plan's "feature area root" placement. Endpoints in child namespaces need an explicit `using Fokus.API.Features.Sync;`.
- `SyncBacklogEndpoint` retains both `TicketRepository` and `DeveloperRepository` — both are still used in the epic sync section that was not extracted.
- `SprintIssueSyncResult` is a nested record in the same file as the service, as specified in the plan.
- The pre-existing NuGet vulnerability warning (`Microsoft.Build.Tasks.Core`) was present before this change and is unrelated to it.

## Deviations from Plan

None.
