## Cycle 1 Re-Review (date: 2026-06-06)

**Verdict:** GO

**CRITICAL resolution status:** RESOLVED

**Evidence**

- The server response DTO is back to the pre-Pass-2 property names at `src/Services/Fokus/Fokus.API/Features/Settings/SaveCycleTimeBoundaries/SaveCycleTimeBoundariesCommand.cs:23`: `public record SaveCycleTimeBoundariesResponse(string StartStage, string EndStage);`
- The endpoint now returns that DTO with the saved settings values at `src/Services/Fokus/Fokus.API/Features/Settings/SaveCycleTimeBoundaries/SaveCycleTimeBoundariesEndpoint.cs:46`: `await Send.OkAsync(new SaveCycleTimeBoundariesResponse(StartStage: settings.CycleTimeStartStage, EndStage: settings.CycleTimeEndStage), ct);`
- The client response type is reverted to `startStage` / `endStage` at `client/src/types/index.ts:870-872`:
  `export interface SaveCycleTimeBoundariesResponse {`
  `  startStage: string`
  `  endStage: string`
  `}`
- The client reads those reverted response keys at `client/src/components/settings/WorkflowTab.vue:75-77`: `boundaryStartStage.value = result.startStage` and `boundaryEndStage.value = result.endStage`
- The approved request-payload rename remains in place at `client/src/api/settings.ts:90-93`: `body: JSON.stringify({ cycleTimeStartStage, cycleTimeEndStage })`

**SaveHealthConfig change verification:** PASS

**Evidence**

- `SaveHealthConfigCommand` still exposes the full five-field write shape at `src/Services/Fokus/Fokus.API/Features/Settings/SaveHealthConfig/SaveHealthConfigCommand.cs:5-9`: `HealthThresholds`, `HealthWeights`, `QaHealthThresholds`, `QualityHealthWeight`, and `QualitySubScoreWeights`
- `SaveHealthConfigEndpoint` explicitly assigns all five fields at `src/Services/Fokus/Fokus.API/Features/Settings/SaveHealthConfig/SaveHealthConfigEndpoint.cs:15-19`:
  `existing.HealthThresholds = req.HealthThresholds;`
  `existing.HealthWeights = req.HealthWeights;`
  `existing.QaHealthThresholds = req.QaHealthThresholds;`
  `existing.QualityHealthWeight = req.QualityHealthWeight;`
  `existing.QualitySubScoreWeights = req.QualitySubScoreWeights;`
- Those assignments target the matching `AppSettings` properties at `src/Services/Fokus/Fokus.Domain/Settings/AppSettings.cs:10-11` and `src/Services/Fokus/Fokus.Domain/Settings/AppSettings.cs:24-26`: `public HealthThresholdConfig HealthThresholds { get; set; } = new();`, `public HealthWeightConfig HealthWeights { get; set; } = new();`, `public QaHealthThresholdConfig QaHealthThresholds { get; set; } = new();`, `public int QualityHealthWeight { get; set; } = 20;`, and `public QualitySubScoreWeightConfig QualitySubScoreWeights { get; set; } = new();`
- The prior Mapster registration only declared a same-name command-to-entity map at `src/Services/Fokus/Fokus.API/Features/Settings/SaveHealthConfig/SaveHealthConfigMappings.cs:5-10` (deleted in this diff): `public class SaveHealthConfigMappings : IRegister` and `config.NewConfig<SaveHealthConfigCommand, AppSettings>().IgnoreNonMapped(true);`
- Inference: `src/Services/Fokus/Fokus.API/DependencyInjection.cs:25` still uses assembly scanning for Mapster registration via `TypeAdapterConfig.GlobalSettings.Scan(typeof(DependencyInjection).Assembly);`, so deleting `SaveHealthConfigMappings.cs` means this specific `IRegister` is no longer registered

# Pass 2 - SliceHygiene: Independent Cross-Model Code Review

**Reviewer:** Codex (independent second opinion)
**Diff reviewed:** df142ce..3f07fa7
**Date:** 2026-06-06

---

## Verdict: NO-GO

---

## Executive Summary

- The gRPC rename is wire-safe: TeamQuery and SprintQuery preserved the original DataMember Order values, the ServiceContract interfaces still match the server implementations, and the same gRPC services remain registered.
- The Jira Refit rename is wire-safe: UpdateJiraSprintCommand preserved the Name / StartDate / EndDate / Goal property set, and Jira Refit is configured to serialize JSON with camelCase property names.
- The five new Mapster save maps cover the intended current-shape AppSettings fields; no missing same-name fields were found in those five command-to-entity updates.
- The approved Path-A request-field renames were propagated through the Vue client, but the implementation also renamed the SaveCycleTimeBoundaries response keys, creating an extra HTTP contract break that was not part of the approved request-only rename scope.
- All non-trivial handlers touched by the pass were reviewed; no accidental logic drift was found beyond the response-contract issue.

---

## Findings

### [CRITICAL] SaveCycleTimeBoundaries also renamed the response JSON keys

**File:line:**
- src/Services/Fokus/Fokus.API/Features/Settings/SaveCycleTimeBoundaries/SaveCycleTimeBoundariesCommand.cs:23
- src/Services/Fokus/Fokus.API/Features/Settings/SaveCycleTimeBoundaries/SaveCycleTimeBoundariesEndpoint.cs:46
- docs/specs/adhoc-Pass2-SliceHygiene/delivery/plan.md:207

**Evidence:**

Response DTO (SaveCycleTimeBoundariesCommand.cs:23):

    public record SaveCycleTimeBoundariesResponse(string CycleTimeStartStage, string CycleTimeEndStage);

Endpoint send (SaveCycleTimeBoundariesEndpoint.cs:46):

    await Send.OkAsync(new SaveCycleTimeBoundariesResponse(settings.CycleTimeStartStage, settings.CycleTimeEndStage), ct);

Plan note (plan.md:207): Keep the response keys unchanged unless the team decides otherwise

**Issue:** The approved Path-A change set covered request-field renames (StartStage -> CycleTimeStartStage, EndStage -> CycleTimeEndStage) plus matching client payload updates. The implementation also changed the PUT /api/settings/cycle-time-boundaries response keys from startStage / endStage to cycleTimeStartStage / cycleTimeEndStage. The Vue client was updated for that extra break (client/src/types/index.ts:870-872, client/src/components/settings/WorkflowTab.vue:75-77), so the monorepo stays internally consistent, but the HTTP wire contract is broken for any other consumer expecting the prior response shape. That response rename was explicitly called out in the plan as something not to do silently.

**Recommendation:** Revert the save-response DTO back to StartStage / EndStage while keeping the request DTO/entity names aligned, OR explicitly record this as an intentional response-contract change and notify every consumer.

---

## Area Coverage

### Area 1: gRPC Rename Wire-Safety

Compared the old and new data-contract shapes and checked the live service surface.

- TeamQuery still exposes [DataMember(Order = 1)] SubTeam at src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/TeamQuery.cs:11-12, matching the old TeamRequest shape.
- SprintQuery still exposes [DataMember(Order = 1)] SprintId and [DataMember(Order = 2)] SubTeam at src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/SprintQuery.cs:8-15, matching the old SprintRequest shape.
- ServiceContract interfaces checked: ISprintAnalyticsService.cs:6-16, ISprintCatalogService.cs:6-10.
- Server implementations checked: SprintAnalyticsGrpcService.cs:29,54,82 and SprintCatalogGrpcService.cs:13.
- Registrations checked: Fokus.API/DependencyInjection.cs:29,99-100.

**Result: CLEAN** - No member-order drift, no signature mismatches, no registration mismatch.

---

### Area 2: Jira UpdateJiraSprintRequest -> UpdateJiraSprintCommand Refit Rename

- The command still contains only Name, StartDate, EndDate, and Goal at src/Modules/Jira/Jira.Contracts/UpdateJiraSprintCommand.cs:3-8.
- IJiraApi still posts that body to the same endpoint at src/Modules/Jira/Jira.RestApi/IJiraApi.cs:32-33.
- RestApiJiraClient still constructs the same property set at src/Modules/Jira/Jira.RestApi/RestApiJiraClient.cs:61-70.
- Refit is configured with PropertyNamingPolicy = JsonNamingPolicy.CamelCase at src/Modules/Jira/Jira.RestApi/DependencyInjection.cs:22-29; on-wire keys remain name, startDate, endDate, goal.
- No AliasAs / JsonPropertyName attributes existed on the old or new DTO, so none were lost.

**Result: CLEAN** - No JSON key drift.

---

### Area 3: Five Mapster Save* Settings Maps

Entity target: src/Services/Fokus/Fokus.Domain/Settings/AppSettings.cs:6-26.

| Command | Fields Mapped | AppSettings Target Lines | Config File |
|---|---|---|---|
| SaveBugRatioAlertsCommand | BugRatioAlertThreshold, BugRatioConsecutiveSprintCount, DefaultSpPerBug | 12-18 | SaveBugRatioAlertsMappings.cs:9-10 |
| SaveBoardCommand | BoardId | 6 | SaveBoardMappings.cs:9-10 |
| SaveSyncConfigCommand | SyncBackSprintCount, PlanningWindowDays | 16-17 | SaveSyncConfigMappings.cs:9-10 |
| SaveAnalyticsTargetsCommand | BugRatioTarget | 19 | SaveAnalyticsTargetsMappings.cs:9-10 |
| SaveHealthConfigCommand | HealthThresholds, HealthWeights, QaHealthThresholds, QualityHealthWeight, QualitySubScoreWeights | 10-11, 24-26 | SaveHealthConfigMappings.cs:9-10 |

Each mapping config uses IgnoreNonMapped(true) and each endpoint applies it in-place with req.Adapt(existing).

**Result: CLEAN** - No current-shape command properties lack a same-name AppSettings target; no extra assignments lost compared with pre-Mapster handlers.

---

### Area 4: Path-A Field Renames - Server/Client Wire Contract

- SaveBugRatioAlertsCommand exposes BugRatioAlertThreshold / BugRatioConsecutiveSprintCount at SaveBugRatioAlertsCommand.cs:5-6.
  - Vue client sends matching camelCase keys in client/src/api/settings.ts:42-46. VERIFIED
  - Store action signature updated at client/src/stores/settingsStore.ts:132-137. VERIFIED
  - Call site correct at client/src/components/settings/HealthTab.vue:57. VERIFIED
- SaveCycleTimeBoundariesCommand exposes CycleTimeStartStage / CycleTimeEndStage at SaveCycleTimeBoundariesCommand.cs:5-6.
  - Vue client sends matching keys in client/src/api/settings.ts:90-94. VERIFIED
- GET read contract (StartStage / EndStage) unchanged on server at GetCycleTimeBoundariesQuery.cs:3-7; read as result.startStage / result.endStage in client/src/components/settings/WorkflowTab.vue:41-44. VERIFIED

**Result: ONE ISSUE** - The save-response rename (described in the CRITICAL finding above) is the only gap. All request-field renames have matching client-side changes.

---

### Area 5: Business Logic Integrity

- UpdateSprintEndpoint: still calls jiraClient.UpdateSprintAsync(sprint.Id, req.Name, req.StartDate, req.EndDate, req.Goal, ct) with the same argument set and order at UpdateSprintEndpoint.cs:11-35. VERIFIED
- UpdateTeamConfigEndpoint: still forwards the same seven arguments to developerRepository.UpdateTeamConfigAsync in the same order at UpdateTeamConfigEndpoint.cs:11-18. VERIFIED
- UpdateRoleEndpoint and UpdateStatusEndpoint: last-admin protections and state-change calls retained at UpdateRoleEndpoint.cs:20-42 and UpdateStatusEndpoint.cs:20-46. VERIFIED
- SaveXraySettingsEndpoint, SaveDoneStatusesEndpoint, SaveExcludedStatusesEndpoint, SaveWorkflowStagesEndpoint: conditional/list-replacement logic unchanged. VERIFIED

**Result: CLEAN** - No confirmed accidental logic drift outside the response-contract change already called out.

---

Review generated by Codex independent reviewer. Report written to file by Claude Code on behalf of the reviewer.
