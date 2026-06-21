# Xray Sync: Project-Wide TE Discovery — Implementation

## Files Created

None.

## Files Modified

- `src/Modules/Xray/Xray.Contracts/XrayDtos.cs` — Added `XrayTestCaseDto` and `XrayJiraIssueLinkDto` classes; extended `XrayTestExecutionDto` with `TestCases` and `IssueLinks` properties (both default to `[]`)
- `src/Modules/Xray/Xray.Contracts/IXrayClient.cs` — Removed `GetTeKeysForTestCasesAsync`; added `GetAllProjectTestExecutionsAsync(bearerToken, projectKey, ct)`
- `src/Modules/Xray/Xray.GraphQL/GraphQLXrayClient.cs` — Removed `GetTeKeysForTestCasesAsync`, `BuildTestCasesQuery`, `ParseTestCasesResponse`; added `GetAllProjectTestExecutionsAsync` (with `ProjectKeyFormat` regex guard), `BuildProjectTestExecutionsQuery`, `ParseProjectTestExecutionsResponse`, `ParseJiraIssueLinks` helper
- `src/Services/Fokus/Fokus.API/Features/Xray/XrayIssueSyncService.cs` — Full refactor: removed `SyncXrayForIssuesAsync`, `ExtractLinkedIssues`, `FindTeIssueId`, `LinkedIssues` record, `FetchTestExecutionsAsync`; added `SyncTestExecutionsForProjectAsync`, `SyncTestSetsFromIssuesAsync`, `BuildLinkMap`; injected `TicketRepository`
- `src/Services/Fokus/Fokus.API/Features/Xray/SyncXray/SyncXrayEndpoint.cs` — Injected `IOptions<JiraOptions>`; added `ProjectKey` null guard (`BadRequestException`); replaced single `SyncXrayForIssuesAsync` call with two calls: `SyncTestExecutionsForProjectAsync` + `SyncTestSetsFromIssuesAsync`
- `src/Services/Fokus/Fokus.API/Features/Sync/SprintIssueSyncService.cs` — Updated piggyback Xray call: replaced `SyncXrayForIssuesAsync` with `SyncTestSetsFromIssuesAsync` (TE discovery moved to dedicated endpoint)

## Key Decisions

- `ParseJiraIssueLinks` extracted as a shared static helper to avoid duplicating JSON traversal between TE-level and test-case-level issuelink parsing
- The piggyback Xray call in `SprintIssueSyncService.SyncAsync` was updated to call `SyncTestSetsFromIssuesAsync` only — project-wide TE discovery belongs in the dedicated sync endpoint, not per-sprint piggyback
- `knownTicketKeys` collected by extracting all `OutwardIssueKey ?? InwardIssueKey` values from all issuelinks across all TEs and test cases, then resolving via `TicketRepository.GetExistingKeysAsync` — no new repository method needed

## Deviations from Plan

- `SprintIssueSyncService` was not mentioned in the plan but contained a caller of the removed `SyncXrayForIssuesAsync` — updated to use `SyncTestSetsFromIssuesAsync` to keep piggyback TestSet sync working without breaking the build

## Review Fixes (Cycle 1)

- `docs/kb/xray.md` — Sync Flow section rewritten to reflect new two-path architecture (project-wide Xray-driven TE discovery + Jira-driven TestSet sync); Link Type Detection updated to clarify source is now Xray GraphQL issuelinks, not sprint ticket Jira issuelinks
- `src/Modules/Xray/Xray.GraphQL/GraphQLXrayClient.cs` — Added `ProjectKeyFormat` compiled regex (`^[A-Z0-9_]+$`) and format guard in `GetAllProjectTestExecutionsAsync`; throws `BadRequestException` if projectKey contains characters that would break JQL interpolation
