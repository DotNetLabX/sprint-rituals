# SyncEndpointCleanup — Implementation

## Files Created
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncBacklogSprints/SyncBacklogSprintsEndpoint.cs` — renamed from SyncBacklog, slimmed to thin orchestrator
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncBacklogSprints/SyncBacklogSprintsResponse.cs` — extracted response type

## Files Modified
- `src/Services/Fokus/Fokus.API/Features/Sync/SprintIssueSyncService.cs` — added `SyncSprintsFromJiraAsync` (batch sprint sync loop), `SyncEpicDiscoveryAsync` (epic discovery loop), result records (`SprintBatchSyncResult`, `SprintSyncFailure`, `EpicDiscoveryResult`). Constructor now takes `IJiraClient` and `ILogger<SprintIssueSyncService>` via DI instead of per-method parameters.
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncSprints/SyncSprintsEndpoint.cs` — removed `SprintRepository`, `TicketRepository`, `DeveloperRepository` from constructor. Replaced inline sync loop with `syncService.SyncSprintsFromJiraAsync` call. Kept `IJiraClient` for sprint range validation.
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncSprints/SyncSprintsCommand.cs` — removed local `SprintSyncFailure` class (now a record on the service), added `using Fokus.API.Features.Sync`.

## Files Deleted
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncBacklog/SyncBacklogEndpoint.cs` — replaced by SyncBacklogSprints
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncBacklog/SyncBacklogCommand.cs` — replaced by SyncBacklogSprintsResponse.cs

## Key Decisions
- Kept `IJiraClient` in both endpoints for fetching sprint lists (which sprints to sync is endpoint-level concern). Service uses it for per-sprint issue fetching.
- `SprintSyncFailure` changed from mutable class to positional record — JSON shape unchanged.
- Sprint sync failures in batch method are not logged (only returned in response). Epic failures are logged via service's constructor-injected logger.
