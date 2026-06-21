# Developer Throughput — Implementation

## Files Created

- `src/Services/Fokus/Fokus.Domain/Developer/DeveloperSprintCapacity.cs` — New plain entity with composite key (DeveloperAccountId + SprintId), CapacityPercent, navigation properties to Developer and Sprint. No base class, matches SprintMembership pattern.
- `src/Services/Fokus/Fokus.Persistence/Configurations/DeveloperSprintCapacityConfiguration.cs` — EF Core config: composite PK, HasMaxLength(128) on DeveloperAccountId, two cascade FK relationships, index on SprintId.
- `src/Services/Fokus/Fokus.API/Features/Analytics/DeveloperThroughputService.cs` — Pure computation service + full response record hierarchy (SprintSummaryItem, SprintBreakdown, DeveloperThroughputEntry, DeveloperThroughputResponse). Implements sub-team filtering (C2), per-developer per-sprint metrics, capacity-aware 3-sprint rolling average, and delta computation with polarity.
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperThroughput/GetDeveloperThroughputQuery.cs` — Request type + validator (mutual exclusivity of SprintId and Last, range checks).
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperThroughput/GetDeveloperThroughputEndpoint.cs` — GET /api/analytics/developer-throughput endpoint. Handles single/last-N/all modes, rolling average window expansion (+2 sprints before earliest target), bulk loads memberships and capacity records.
- `src/Services/Fokus/Fokus.API/Features/Developers/SetCapacity/SetCapacityCommand.cs` — SetCapacityRequest, SetCapacityResponse, validator (SprintId > 0, CapacityPercent 0-100).
- `src/Services/Fokus/Fokus.API/Features/Developers/SetCapacity/SetCapacityEndpoint.cs` — PUT /api/developers/{accountId}/capacity endpoint. 404 on unknown developer, upserts capacity, saves.
- `src/Services/Fokus/Fokus.API/Features/Developers/GetCapacity/GetCapacityQuery.cs` — GetCapacityRequest, CapacityEntry response type.
- `src/Services/Fokus/Fokus.API/Features/Developers/GetCapacity/GetCapacityEndpoint.cs` — GET /api/developers/{accountId}/capacity endpoint. 404 on unknown developer.
- `client/src/api/developers.ts` — New API module: setDeveloperCapacity (PUT), getDeveloperCapacity (GET).
- `client/src/stores/developersStore.ts` — Pinia store for Developers page. Manages sprint mode (single/multi), selectedLast, subTeam filter, throughput data, optimistic capacity editing with revert on error.
- EF Core migration generated: AddDeveloperSprintCapacity (via `dotnet ef migrations add`).

## Files Modified

- `src/Services/Fokus/Fokus.Persistence/FokusDbContext.cs` — Added `DbSet<DeveloperSprintCapacity> DeveloperSprintCapacities`.
- `src/Services/Fokus/Fokus.Persistence/Repositories/DeveloperRepository.cs` — Added 4 new methods: GetCapacityAsync, GetCapacitiesForDeveloperAsync, UpsertCapacityAsync, GetCapacitiesForSprintsAsync.
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — Registered `DeveloperThroughputService` as scoped.
- `client/src/types/index.ts` — Added SprintSummaryItem, SprintBreakdown, DeveloperThroughputEntry (with accountId), DeveloperThroughputResponse, CapacityEntry, SetCapacityRequest, SetCapacityResponse.
- `client/src/api/analytics.ts` — Added getDeveloperThroughput function.
- `client/src/components/PageToolbar.vue` — Added sprintMode/selectedLast props, aggregate sprint options (Last 3, Last 5, All) with visual divider, update:sprintMode emit, backward-compatible (DashboardView unchanged).
- `client/src/views/DevelopersView.vue` — Complete rewrite: throughput table (single-sprint with deltas + capacity editing, multi-sprint with averages), trend chart (ApexCharts multi-line, rolling average), URL sync, store integration.

## Key Decisions

- Added `AccountId` to `DeveloperThroughputEntry` response (both C# record and TS interface). The plan's response shape omitted it but the frontend's `updateCapacity` action requires it to call the PUT endpoint. Without it there is no way to identify which developer to update.
- Used pre-computed `singleSprintRows` computed array in DevelopersView rather than `v-if="expr as var"` template narrowing, which is not supported by Vue 3's TypeScript compiler.
- `SaveChangesAsync` was not re-added to DeveloperRepository — base class `RepositoryBase` already provides it. The initial addition caused a CS0108 warning and was removed.
- Rolling average window expansion: loaded up to 2 extra sprints before the earliest target sprint to satisfy the 3-sprint lookback without N+1 queries.
- `currentSelectValue` is a plain function in PageToolbar (not a computed) because it receives props as a plain object — this keeps the component's props reactive binding intact without introducing a computed that would need to destructure the props.

## Deviations from Plan

- `DeveloperThroughputEntry` gains `AccountId` field (plan listed DisplayName/SubTeam/AvatarUrl/SprintBreakdowns only). Required to support capacity editing from the frontend; without it the PUT endpoint cannot be called.
- `getBreakdown` helper function was not used in the template — replaced by `singleSprintRows` computed to work around Vue 3 TypeScript template narrowing limitations.
