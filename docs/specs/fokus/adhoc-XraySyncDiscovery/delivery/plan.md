# Xray Sync: Project-Wide TE Discovery

**Feature Spec:** None (ad-hoc infrastructure fix)

## Context

The Xray sync (`POST /api/xray/sync`) discovers Test Executions by scanning Jira sprint tickets' `issuelinks` field for links to TE-type issues. TEs created inside Xray's UI don't produce Jira issue links, so they're invisible to the current discovery path — only ~5 TEs are found out of many more that exist. The fix: query Xray GraphQL directly for **all project TEs** using JQL `project = {KEY}`, then derive the TE→ticket link map by traversing TE → test cases → test case issuelinks → sprint stories.

**Services impacted:** Fokus (single service). **Modules impacted:** Xray module (new query + DTOs).

## Scope

**In scope:**
- New `IXrayClient` method for project-wide TE fetch with test cases and issuelinks
- New DTOs for test case info and issue link info within the Xray response
- New GraphQL query including `tests` sub-query and `issuelinks` on both TE and test case `jira()` fields
- Refactored `XrayIssueSyncService` — Xray-driven TE discovery replaces Jira-driven discovery
- Updated `SyncXrayEndpoint` — calls new sync method, keeps Jira fetch for TestSet discovery only
- Dead code removal (`GetTeKeysForTestCasesAsync`, `ExtractLinkedIssues`, `FindTeIssueId`)

**Out of scope:**
- `TestExecutionRepository` changes (creation-date attribution recently fixed — must not touch)
- Sprint attribution logic changes
- TestRun pagination (existing limit:100 cap unchanged)
- Test-case pagination within a TE (same limit:100 cap)

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | (none) | — | Extend Xray.Contracts DTOs + interface method | No skill for extending module contracts |
| 2 | (none) | — | Xray GraphQL query with tests + issuelinks, parser | No skill for external API client queries |
| 3 | (none) | — | Sync service refactoring, link map builder | No skill for sync orchestration refactoring |
| 4 | error-handling | Follow | BadRequestException for missing ProjectKey | |
| 5 | (none) | — | Dead code removal | |

## Domain Model Changes

None — existing TestExecution, TestExecutionLink, TestRun entities unchanged.

## Data Model Changes

None — no schema or migration changes.

## Implementation Steps

### Step 1: Extend Xray DTOs and client contract

Add DTOs for test case info and issue links to support the enriched GraphQL response. Add a new `IXrayClient` method for project-wide TE fetch.

**Files:**
- Modify: `src/Modules/Xray/Xray.Contracts/XrayDtos.cs`
  - Add `XrayTestCaseDto` class — `string? IssueKey`, `List<XrayJiraIssueLinkDto> IssueLinks = []`
  - Add `XrayJiraIssueLinkDto` class — `string LinkTypeName`, `string? OutwardIssueKey`, `string? InwardIssueKey`
  - Extend `XrayTestExecutionDto` with two new properties (default `[]`):
    - `List<XrayTestCaseDto> TestCases` — test cases within the TE (from `tests` sub-query)
    - `List<XrayJiraIssueLinkDto> IssueLinks` — TE's own Jira issuelinks (for "Blocks" link discovery)
- Modify: `src/Modules/Xray/Xray.Contracts/IXrayClient.cs`
  - Add: `Task<XrayTestExecutionResult> GetAllProjectTestExecutionsAsync(string bearerToken, string projectKey, CancellationToken ct)`

**Accept:** DTOs compile, interface has new method, existing `GetTestExecutionsAsync` unchanged.

---

### Step 2: Implement GraphQL query and response parser

Implement `GetAllProjectTestExecutionsAsync` in the GraphQL client. Uses JQL `project = {projectKey}` to fetch all TEs with their test cases and issuelinks.

**Files:**
- Modify: `src/Modules/Xray/Xray.GraphQL/GraphQLXrayClient.cs`

**New query builder** — `BuildProjectTestExecutionsQuery(string projectKey, int start, int limit)`:
```graphql
getTestExecutions(jql: "project = {KEY}", limit: 100, start: 0) {
  total
  results {
    issueId
    jira(fields: ["key", "summary", "status", "assignee", "created", "issuelinks"])
    testRuns(limit: 100) {
      total
      results { id, status { name }, startedOn, finishedOn, assigneeId }
    }
    tests(limit: 100) {
      total
      results {
        jira(fields: ["key", "issuelinks"])
      }
    }
  }
}
```

**Implementation method** — `GetAllProjectTestExecutionsAsync`:
- Reuse existing pagination loop pattern from `FetchTestExecutionsBatchAsync` (line 50): `do/while (start < total)` with `EnforceRateLimitAsync` per page
- No batching by issue keys needed — single JQL drives all pages

**New parser** — `ParseProjectTestExecutionsResponse(string json)`:
- Extend existing `ParseTestExecutionsResponse` (line 196) pattern for base TE fields (issueId, key, summary, status, assignee, created, testRuns)
- Parse TE-level `issuelinks` from `jira.issuelinks` array: each element has `type.name`, `outwardIssue.key`, `inwardIssue.key` → map to `XrayJiraIssueLinkDto`
- Parse `tests.results[]`: each test case's `jira.key` and `jira.issuelinks` (same structure) → map to `XrayTestCaseDto`
- The `issuelinks` JSON structure from Xray's `jira()` passthrough matches Jira's format: `{ type: { name }, outwardIssue: { key }, inwardIssue: { key } }` — same shape as `JiraIssueLink` in `Jira.Contracts/JiraIssue.cs` (lines 32-58)

**Pattern reference:** `GraphQLXrayClient.ParseTestExecutionsResponse` (line 196) for JSON traversal, `GraphQLXrayClient.FetchTestExecutionsBatchAsync` (line 50) for pagination loop.

**Accept:** Calling with a valid project key returns all TEs with populated `TestCases` and `IssueLinks`.

**Depends on:** Step 1.

---

### Step 3: Refactor XrayIssueSyncService for Xray-driven discovery

Replace the Jira-driven TE discovery with Xray-driven discovery. The sync service no longer receives `List<JiraIssue>` for TE discovery — it receives a project key and queries Xray directly.

**Files:**
- Modify: `src/Services/Fokus/Fokus.API/Features/Xray/XrayIssueSyncService.cs`

**New method** — `SyncTestExecutionsForProjectAsync(string projectKey, CancellationToken ct) → Task<XraySyncResult>`:
1. Authenticate with Xray (reuse existing `AuthenticateAsync`, line 158)
2. Call `xrayClient.GetAllProjectTestExecutionsAsync(bearerToken, projectKey, ct)`
3. Build link map via new `BuildLinkMap` method (see below)
4. Reuse existing `SyncTestExecutionsAsync` (line 189) / `SyncSingleTestExecutionAsync` (line 231) for per-TE persistence — these methods take the TE DTOs and link map, which have the same shape

**New private method** — `BuildLinkMap(List<XrayTestExecutionDto> executions, HashSet<string> knownTicketKeys) → Dictionary<string, List<(string TicketKey, TestExecutionLinkType)>>`:
- Derives TE→ticket links from two sources within the Xray response:
  1. **Test case issuelinks** — "Test" link type → `TestExecutionLinkType.Tests` (test-case-mediated coverage)
  2. **TE-level issuelinks** — "Test" link type → `Tests` (direct Story↔TE links), "Blocks" link type → `Blocks`
- Direction-agnostic link resolution — same pattern as current `ExtractLinkedIssues` (line 100): `OutwardIssue ?? InwardIssue`
- Only include links where ticket key exists in `knownTicketKeys` — prevents FK violations on `TestExecutionLink.TicketKey` (hard FK to Ticket table)
- Jira link type name is `"Test"` (singular, case-insensitive) — not our enum value `Tests`

**Loading `knownTicketKeys`:** Inject `TicketRepository` into `XrayIssueSyncService`. In `SyncTestExecutionsForProjectAsync`, before calling `BuildLinkMap`:
1. Collect all candidate ticket keys from the Xray response — every `OutwardIssueKey` and `InwardIssueKey` across all TE-level and test-case-level issuelinks
2. Call `ticketRepository.GetExistingKeysAsync(candidateKeys, ct)` — returns `HashSet<string>` of only those that exist in the DB (single query, no full-table scan)
3. Pass that HashSet as the `knownTicketKeys` parameter to `BuildLinkMap`

This uses the existing `TicketRepository.GetExistingKeysAsync` method (line 29) which was built for exactly this FK-safety pattern — no new repository method needed.

**Inject** `IOptions<JiraOptions>` — needed to pass `ProjectKey` if the endpoint doesn't pass it explicitly. Alternatively, accept `projectKey` as a parameter (architect prefers: accept as parameter — the endpoint owns the config source).

**Extract TestSet sync** — `SyncTestSetsFromIssuesAsync(List<JiraIssue> jiraIssues, CancellationToken ct) → Task<int>`:
- Pull out TestSet-specific branches from `ExtractLinkedIssues` (lines 127-139): walk Jira issue links, filter `type == "Test"` + `issuetype == "Test Set"`, collect TestSet data
- Call existing `SyncTestSetsAsync` (line 290) with extracted TestSets
- This method stays Jira-driven — TestSets are only discoverable from Jira issue links

**Remove:**
- `SyncXrayForIssuesAsync` — replaced by `SyncTestExecutionsForProjectAsync`
- `ExtractLinkedIssues` — replaced by `BuildLinkMap`
- `FindTeIssueId` — issueId now comes directly from Xray response (no fallback needed)
- `LinkedIssues` record — no longer used

**Accept:** Calling with project key syncs all discoverable TEs. Existing `SyncSingleTestExecutionAsync` persistence path unchanged. TestSets sync separately from Jira issues.

**Depends on:** Step 2.

---

### Step 4: Update SyncXrayEndpoint

Change the endpoint to use the new Xray-driven sync for TEs while keeping Jira-driven sync for TestSets.

**Follow** `error-handling` for `BadRequestException` usage.

**Files:**
- Modify: `src/Services/Fokus/Fokus.API/Features/Xray/SyncXray/SyncXrayEndpoint.cs`

**Changes:**
- Inject `IOptions<JiraOptions>` via constructor
- Validate `jiraOptions.Value.ProjectKey` is non-null at the start of `HandleAsync` — throw `BadRequestException` if missing (per error-handling skill)
- Replace the single `xraySyncService.SyncXrayForIssuesAsync(allJiraIssues, ct)` call with two calls:
  1. `xraySyncService.SyncTestExecutionsForProjectAsync(projectKey, ct)` — Xray-driven TE sync
  2. `xraySyncService.SyncTestSetsFromIssuesAsync(allJiraIssues, ct)` — Jira-driven TestSet sync (kept)
- Combine results into `SyncXrayResponse`
- Keep existing sprint/Jira-issue fetch for TestSet discovery. `SprintIds` still controls TestSet scope.

**Accept:** Endpoint compiles, `ProjectKey` validation works, both sync paths called.

**Depends on:** Step 3.

---

### Step 5: Remove dead code

Clean up methods that are no longer called after the Xray-driven refactoring.

**Files:**
- Modify: `src/Modules/Xray/Xray.Contracts/IXrayClient.cs` — remove `GetTeKeysForTestCasesAsync` (line 12)
- Modify: `src/Modules/Xray/Xray.GraphQL/GraphQLXrayClient.cs` — remove:
  - `GetTeKeysForTestCasesAsync` implementation (line 78)
  - `BuildTestCasesQuery` (line 105)
  - `ParseTestCasesResponse` (line 111)
- Verify: grep for `GetTeKeysForTestCasesAsync`, `ExtractLinkedIssues`, `FindTeIssueId` across codebase — confirm zero remaining callers

**Accept:** Build passes. No references to removed methods.

**Depends on:** Step 4.

## Cross-Service Changes

None. Single-service application.

## Migration Notes

None — no schema changes.

## Testing Strategy

- Trigger `POST /api/xray/sync` — verify significantly more TEs discovered than before (~5 → many more)
- Spot-check `TestExecutionLinks` in DB — verify "Tests" links correctly derived from test case issuelinks
- Verify "Blocks" links still present (from TE-level issuelinks)
- Verify TestSet sync count unchanged (Jira-driven path preserved)
- Verify TEs with no test-case links to known tickets are synced with empty link set
- Verify `ProjectKey` null → 400 BadRequest
- Build passes: `dotnet build`

## KB Impact

Update `docs/kb/xray.md` — the sync flow section (currently describes Jira-driven discovery) must be updated to reflect Xray-driven discovery via `getTestExecutions(jql: "project = KEY")` with test case issuelink traversal.

## Open Questions

None.
