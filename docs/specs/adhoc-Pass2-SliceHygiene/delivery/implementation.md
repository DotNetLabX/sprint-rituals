# Pass 2 — SliceHygiene — Implementation

## Files Created

### Step 2 — Mapster DI bootstrap (no new files, only edits to DI.cs)

### Step 3 — Command/Query siblings created (16 new files)
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveBugRatioAlerts/SaveBugRatioAlertsCommand.cs`
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveBoard/SaveBoardCommand.cs`
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveSyncConfig/SaveSyncConfigCommand.cs`
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveAnalyticsTargets/SaveAnalyticsTargetsCommand.cs`
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveHealthConfig/SaveHealthConfigCommand.cs`
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveCycleTimeBoundaries/SaveCycleTimeBoundariesCommand.cs`
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveXraySettings/SaveXraySettingsCommand.cs`
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveExcludedStatuses/SaveExcludedStatusesCommand.cs`
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveDoneStatuses/SaveDoneStatusesCommand.cs`
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveWorkflowStages/SaveWorkflowStagesCommand.cs`
- `src/Services/Fokus/Fokus.API/Features/Settings/GetCycleTimeBoundaries/GetCycleTimeBoundariesQuery.cs`
- `src/Services/Fokus/Fokus.API/Features/Team/UpdateTeamConfig/UpdateTeamConfigCommand.cs`
- `src/Services/Fokus/Fokus.API/Features/Team/GetTeamRoster/GetTeamRosterQuery.cs`
- `src/Services/Fokus/Fokus.API/Features/Developers/GetDevelopers/GetDevelopersQuery.cs`
- `src/Services/Fokus/Fokus.API/Features/Xray/SyncXray/SyncXrayCommand.cs`
- `src/Services/Fokus/Fokus.API/Features/Xray/TestConnection/TestConnectionQuery.cs`
- `src/Services/Fokus/Fokus.API/Features/Sprints/GetSprints/GetSprintsQuery.cs` (replaces `GetSprintsModels.cs`)
- `src/Services/Fokus/Fokus.API/Features/Sprints/UpdateSprint/UpdateSprintCommand.cs` (merged from `UpdateSprintModels.cs` + `UpdateSprintValidator.cs`)

Files deleted as part of Step 3 normalisation:
- `src/Services/Fokus/Fokus.API/Features/Sprints/GetSprints/GetSprintsModels.cs`
- `src/Services/Fokus/Fokus.API/Features/Sprints/UpdateSprint/UpdateSprintModels.cs`
- `src/Services/Fokus/Fokus.API/Features/Sprints/UpdateSprint/UpdateSprintValidator.cs`

### Step 5 — gRPC/Jira contract renames (new files, old deleted)
- `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/TeamQuery.cs` (replaces `TeamRequest.cs`)
- `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/SprintQuery.cs` (replaces `SprintRequest.cs`)
- `src/Modules/Jira/Jira.Contracts/UpdateJiraSprintCommand.cs` (replaces `UpdateJiraSprintRequest.cs`)

Files deleted:
- `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/TeamRequest.cs`
- `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/SprintRequest.cs`
- `src/Modules/Jira/Jira.Contracts/UpdateJiraSprintRequest.cs`

### Step 7 — IRegister Mapster mapping classes (5 new files)
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveBugRatioAlerts/SaveBugRatioAlertsMappings.cs`
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveBoard/SaveBoardMappings.cs`
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveSyncConfig/SaveSyncConfigMappings.cs`
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveAnalyticsTargets/SaveAnalyticsTargetsMappings.cs`
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveHealthConfig/SaveHealthConfigMappings.cs`

## Files Modified

### Step 1 — Mapster to CPM
- `src/Directory.Packages.props` — added `<PackageVersion Include="Mapster" Version="7.4.0"/>` under new "Mapster" group before "Other third-party".
- `src/Services/Fokus/Fokus.API/Fokus.API.csproj` — added bare `<PackageReference Include="Mapster"/>`.
- `src/Services/Identity/Identity.API/Identity.API.csproj` — added bare `<PackageReference Include="Mapster"/>`.

### Step 2 — Mapster DI bootstrap
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — added `using Mapster;` + `TypeAdapterConfig.GlobalSettings.Scan(typeof(DependencyInjection).Assembly);` as first line inside `AddFokusModule`.
- `src/Services/Identity/Identity.API/DependencyInjection.cs` — added `using Mapster;` + `TypeAdapterConfig.GlobalSettings.Scan(typeof(DependencyInjection).Assembly);` as first line inside `AddIdentityModule`.

### Step 3 — Endpoint files stripped of inline types (16 endpoint files edited)
All 16 fat endpoint files had their inline type definitions removed, leaving only the endpoint class and handler.

### Step 4 — Request→Command/Query renames (37 types across 37 files)
All `*Request` types in FastEndpoints feature slices renamed:
- 10 Settings `*Request` → `*Command` (SaveBugRatioAlerts, SaveBoard, SaveSyncConfig, SaveAnalyticsTargets, SaveHealthConfig, SaveCycleTimeBoundaries, SaveXraySettings, SaveExcludedStatuses, SaveDoneStatuses, SaveWorkflowStages)
- 3 other Commands: UpdateTeamConfig, SyncXray, UpdateSprint
- 3 Identity Commands: SetCapacity, CreateInvitation, RevokeInvitation, UpdateRole, UpdateStatus
- 3 Queries: GetCapacity, GetTestTimeline, ValidateInvitation
- 16 Analytics Queries (GetBugRatio, GetCycleTime, GetCarryOver, GetDeveloperQuality, GetDeveloperDetail, GetDeveloperProgress, GetQaMetrics, GetSprintSummary, GetQaTrends, GetFailingTickets, GetLeaderboard, GetDeveloperThroughput, GetScopeChange, GetQaWorkload, GetEpicProgress, GetUntestedTickets)
- All `*RequestValidator` → `*CommandValidator` or `*QueryValidator` (matching pattern)
- All 37 corresponding `*Endpoint.cs` files updated to reference new type names

### Step 5 — gRPC/Jira renames (MAXIMAL scope, wire-safe)
- `src/BuildingBlocks/Blocks.Contracts/Grpc/ISprintAnalyticsService.cs` — `SprintRequest`→`SprintQuery`, `TeamRequest`→`TeamQuery`
- `src/BuildingBlocks/Blocks.Contracts/Grpc/ISprintCatalogService.cs` — `TeamRequest`→`TeamQuery`
- `src/Services/Fokus/Fokus.API/Grpc/SprintAnalyticsGrpcService.cs` — all 3 method signatures updated
- `src/Services/Fokus/Fokus.API/Grpc/SprintCatalogGrpcService.cs` — method signature updated
- `src/Modules/Jira/Jira.RestApi/IJiraApi.cs` — `UpdateJiraSprintRequest`→`UpdateJiraSprintCommand`
- `src/Modules/Jira/Jira.RestApi/RestApiJiraClient.cs` — instantiation updated

Wire stability confirmed: protobuf-net.Grpc uses `[DataMember(Order=N)]` for wire encoding (unchanged); Refit serializes by JSON property name not C# type name (unchanged).

### Step 6 — Path-A field renames + Vue client
C# changes:
- `SaveBugRatioAlertsCommand.cs` — `AlertThreshold`→`BugRatioAlertThreshold`, `ConsecutiveSprintCount`→`BugRatioConsecutiveSprintCount`; validator lambda targets updated
- `SaveBugRatioAlertsEndpoint.cs` — handler updated to use renamed fields
- `SaveCycleTimeBoundariesCommand.cs` — `StartStage`→`CycleTimeStartStage`, `EndStage`→`CycleTimeEndStage`; validator messages updated; response record fields renamed
- `SaveCycleTimeBoundariesEndpoint.cs` — all field references, AddError lambdas, and response construction updated

Vue client changes:
- `client/src/api/settings.ts` — `saveBugRatioAlerts` param names and JSON payload keys updated (`alertThreshold`→`bugRatioAlertThreshold`, `consecutiveSprintCount`→`bugRatioConsecutiveSprintCount`); `saveCycleTimeBoundaries` params updated (`startStage`→`cycleTimeStartStage`, `endStage`→`cycleTimeEndStage`)
- `client/src/stores/settingsStore.ts` — `saveBugRatioAlertsAction` param names updated
- `client/src/types/index.ts` — `SaveCycleTimeBoundariesResponse` fields updated to `cycleTimeStartStage`/`cycleTimeEndStage`
- `client/src/components/settings/WorkflowTab.vue` — `result.startStage`→`result.cycleTimeStartStage`, `result.endStage`→`result.cycleTimeEndStage` in save response handler

Call sites NOT changed (correct):
- `HealthTab.vue` line 57 — passes `form.bugRatioAlertThreshold` and `form.bugRatioConsecutiveSprintCount` positionally; store action signature was updated to match
- `WorkflowTab.vue` lines 42-43 — `getCycleTimeBoundaries` GET response fields (`result.startStage`, `result.endStage`) left unchanged; GET endpoint still returns `StartStage`/`EndStage` from `GetCycleTimeBoundariesResponse`
- `QaWorkloadDevTable.vue:145` — `dev.workloadAlert.consecutiveSprintCount` is a domain response field, NOT a request payload field; collision avoidance confirmed

### Step 7 — Mapster applied to trivial-map handlers
- `src/Services/Fokus/Fokus.API/GlobalUsings.cs` — added `global using Mapster;`
- `SaveBugRatioAlertsEndpoint.cs` — replaced 3 manual assignments with `req.Adapt(existing)`
- `SaveBoardEndpoint.cs` — replaced 1 manual assignment with `req.Adapt(existing)`
- `SaveSyncConfigEndpoint.cs` — replaced 2 manual assignments with `req.Adapt(existing)`
- `SaveAnalyticsTargetsEndpoint.cs` — replaced 1 manual assignment with `req.Adapt(existing)`
- `SaveHealthConfigEndpoint.cs` — replaced 5 manual assignments with `req.Adapt(existing)`

NOT eligible (per plan Q3/decisions):
- `SaveXraySettingsEndpoint` — conditional secret handling (`xrayClientSecret` only updated when non-null)
- `SaveCycleTimeBoundariesEndpoint` — stage-order validation logic, non-trivial
- `GetCycleTimeBoundariesEndpoint` — read-only endpoint with custom default-value logic (not a mapping target)
- `UserResponse` assembly — two-source mapping (Person + User), excluded per ADR-010

## Step 8 — Verification Sweep Results

### Grep checks
1. `grep -rn "class.*Request" --include="*.cs" src/Services/` → only `EndpointWithoutRequest` base class (correct)
2. `grep -rn "class.*Request" --include="*.cs" src/Modules/Jira/Jira.Contracts/` → 0 hits (all renamed)
3. `grep -n "Mapster" src/Directory.Packages.props` → `<PackageVersion Include="Mapster" Version="7.4.0"/>`
4. `grep -rn "GlobalSettings.Scan"` → 2 hits (Fokus + Identity DI bootstrap)
5. `grep -rn "TeamRequest\|SprintRequest"` in src/ → 0 hits (all renamed)
6. `grep -rn ": IRegister"` → 5 hits (5 Mappings classes)
7. `grep -rn "RequestValidator"` in src/ → 0 hits (all renamed to CommandValidator/QueryValidator)

### Build results
- `dotnet build SprintRituals.slnx` — Build succeeded, 0 errors, 6 pre-existing warnings (NU1903 vulnerability advisory, CS8629, CS9107)
- `npm run build` (client/) — built in 1.28s, 0 errors

## Key Decisions
- Mapster 7.4.0 selected as latest stable 7.x per plan guidance.
- MAXIMAL gRPC scope applied: `TeamRequest`→`TeamQuery`, `SprintRequest`→`SprintQuery`, `UpdateJiraSprintRequest`→`UpdateJiraSprintCommand`. Wire stability confirmed via `[DataMember(Order=N)]` (gRPC) and JSON property names (Refit).
- Path-A field renames: `AlertThreshold`→`BugRatioAlertThreshold`, `ConsecutiveSprintCount`→`BugRatioConsecutiveSprintCount` (bug ratio); `StartStage`→`CycleTimeStartStage`, `EndStage`→`CycleTimeEndStage` (cycle time).
- `IRegister` with `IgnoreNonMapped(true)` — ensures Mapster only maps the fields declared in the command, leaving all other `AppSettings` fields unchanged on `.Adapt(existing)`.
- `global using Mapster;` added to `Fokus.API/GlobalUsings.cs` rather than per-endpoint `using` statements.

## Carry-Over Findings
| Title | Severity | For | Evidence | Note |
|-------|----------|-----|----------|------|
| (none) | | | | |

## KB Changes
*(none — no business rule changes in this pass)*

## Deviations from Plan
- Step 4 "~29 types" header (critic LOW): treated 37 as truth — all 37 types confirmed renamed.
- Step 6 "2 concrete Vue call sites" (critic LOW): identified as `HealthTab.vue` (saveBugRatioAlertsAction call) and `WorkflowTab.vue` (saveBoundaries + onMounted boundary response). Both updated correctly.
