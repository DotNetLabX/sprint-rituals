# TeamManagedSync

**Feature Spec:** None

## Context

Team-managed Jira projects use "simple" board type. Several Agile REST API endpoints (`/rest/agile/1.0/...`) return empty results for these boards. Currently, only `GetSprintIssuesAsync` has a JQL fallback — `GetEpicIssuesAsync` and `GetBoardBacklogIssuesAsync` are broken for team-managed boards (and `GetBoardBacklogIssuesAsync` is dead code with zero callers).

Additionally, the JQL search endpoint (`/rest/api/3/search/jql`) caps changelog at 100 entries per issue on Jira Cloud. Issues with longer histories have truncated changelogs, which silently corrupts `WasCommitted`, `AddedAt`, `RemovedAt` (derived from Sprint field changes) and `StatusTransition` records (derived from status changes). This affects data accuracy for F8 (Sprint Summary) and F9 (Developer Throughput), and would make F10 (Scope Change) and F12 (Cycle Time) unreliable.

The fix: refactor `RestApiJiraClient` into a base class (pure Agile API) with a `TeamManagedJiraClient` subclass (JQL + changelog enrichment). Config-driven DI selects the right implementation. The Agile API path stays untouched — a proper Scrum board sees zero changes.

## Scope

**In scope:**
- Add `IsTeamManaged` and `ProjectKey` to `JiraOptions`
- Add paginated changelog endpoint to `IJiraApi`
- Refactor `RestApiJiraClient` for inheritance (virtual methods, protected helpers)
- Create `TeamManagedJiraClient` with JQL overrides for all issue-fetching methods
- Changelog enrichment for issues with truncated histories
- Config-driven DI factory
- Update `docs/jira-team-managed-workarounds.md`

**Explicitly out of scope:**
- Agile API → `/rest/software/1.0/` migration (separate plan, November 2026 deadline)
- Epic Link (`customfield_10014`) → `parent` field migration (affects both board types, separate concern)
- Subtask gap for team-managed boards (Jira bug JSWCLOUD-18394, no workaround exists)
- Settings UI changes for board type (can be added later; `JiraOptions` is consistent with how credentials are already configured)

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | None | N/A | Config class modification | No |
| 2 | None | N/A | Refit interface + DTO changes | No |
| 3 | None | N/A | Refactoring, no new pattern | No |
| 4 | None | N/A | New class, follows established pattern | No |
| 5 | None | N/A | DI registration change | No |
| 6 | None | N/A | Documentation update | No |

No skills apply — this is Jira module internal refactoring with no domain, persistence, or endpoint changes.

## Domain Model Changes

None. All changes are in the Jira module (`src/Modules/Jira/`).

## Data Model Changes

None. Configuration goes in `JiraOptions` (appsettings.json), not the database.

## Implementation Steps

### Step 1: Add `IsTeamManaged` and `ProjectKey` to `JiraOptions`

Add two properties to `JiraOptions`:
- `IsTeamManaged` (bool, default `false`) — determines which `IJiraClient` implementation is injected
- `ProjectKey` (string, optional) — needed for backlog JQL (`project = {key} AND sprint is EMPTY`); only required when `IsTeamManaged = true` and backlog sync is used

**Files to modify:**
- `src/Modules/Jira/Jira.RestApi/JiraOptions.cs` — add properties

**Pattern:** Follow existing `JiraOptions` structure. No `[Required]` on new fields — they have safe defaults.

### Step 2: Add paginated changelog and issue detail endpoints to `IJiraApi`

Add two new Refit endpoints:
- `GET /rest/api/3/issue/{issueIdOrKey}/changelog` → returns `JiraPagedResult<JiraHistory>` (paginated, ascending order, no cap)
- Update `JiraChangelog` DTO to include `MaxResults`, `Total`, `StartAt` fields for truncation detection

**Files to modify:**
- `src/Modules/Jira/Jira.RestApi/IJiraApi.cs` — add `GetIssueChangelogPageAsync(string issueIdOrKey, int startAt, int maxResults, CancellationToken ct)`
- `src/Modules/Jira/Jira.Contracts/JiraChangelog.cs` — add `MaxResults`, `Total`, `StartAt` properties to `JiraChangelog`

**Note:** The paginated changelog endpoint returns `values` (not `histories`), matching `JiraPagedResult<T>.Values`. Reuse existing `JiraPagedResult<JiraHistory>`.

### Step 3: Refactor `RestApiJiraClient` for inheritance

Clean up the base class to support overriding:

1. **Remove the try/fallback hack** from `GetSprintIssuesAsync` — revert to pure Agile API (single line delegating to `GetAllIssuesAsync`, as it was in commit `1987027`)
2. **Make three methods `virtual`:** `GetSprintIssuesAsync`, `GetBoardBacklogIssuesAsync`, `GetEpicIssuesAsync`
3. **Make `RequestAsync` and `SearchAllIssuesAsync` `protected`** — the subclass needs access to both
4. **Add `protected IJiraApi Api` property** (or change constructor field to protected) — the subclass needs the Refit client for the changelog endpoint
5. **Add `protected async Task<List<JiraHistory>> GetFullChangelogAsync(string issueKey, CancellationToken ct)`** — paginated changelog fetcher using the new `GetIssueChangelogPageAsync` endpoint. Loop until all pages consumed.
6. **Add `protected async Task EnrichChangelogsAsync(List<JiraIssue> issues, CancellationToken ct)`** — for each issue where `changelog.Total > changelog.Histories.Count` (truncated), replace `Histories` with full changelog from `GetFullChangelogAsync`. This logic lives in the base class because it's reusable infrastructure, not policy.

**Files to modify:**
- `src/Modules/Jira/Jira.RestApi/RestApiJiraClient.cs`

**What must NOT change:** `GetBoardsAsync`, `GetStatusesAsync`, `GetSprintsAsync` stay untouched — they work for both board types. The Agile API issue-fetching path (used by the base class) must produce identical behavior to today for company-managed boards.

### Step 4: Create `TeamManagedJiraClient`

New class inheriting from `RestApiJiraClient`. Constructor takes `IJiraApi api` (passed to base) plus `string? projectKey` (for backlog JQL).

Override three methods:

**`GetSprintIssuesAsync(int sprintId, CancellationToken ct)`:**
```
JQL: sprint = {sprintId}
→ SearchAllIssuesAsync(jql, ct)
→ EnrichChangelogsAsync(issues, ct)
→ return issues
```

**`GetEpicIssuesAsync(string epicKey, CancellationToken ct)`:**
```
JQL: "Epic Link" = {epicKey}
→ SearchAllIssuesAsync(jql, ct)
→ EnrichChangelogsAsync(issues, ct)
→ return issues
```
Note: Use `"Epic Link"` for now since it still works in JQL. The migration to `parent` is out of scope (affects both board types, separate plan). The developer should check which JQL field works for the target Jira instance — team-managed projects may need `parent = {epicKey}` instead. If `"Epic Link"` fails, fall back to `parent = {epicKey}`. Log which one worked for debugging.

**`GetBoardBacklogIssuesAsync(int boardId, CancellationToken ct)`:**
```
Guard: if projectKey is null/empty, throw BadRequestException("ProjectKey is required...")
JQL: project = {projectKey} AND sprint is EMPTY
→ SearchAllIssuesAsync(jql, ct)
→ EnrichChangelogsAsync(issues, ct)
→ return issues
```
Note: This method currently has zero callers — it's forward-looking coverage. The guard ensures a clear error if someone wires it up without configuring `ProjectKey`.

**Files to create:**
- `src/Modules/Jira/Jira.RestApi/TeamManagedJiraClient.cs`

**Pattern:** Follow `RestApiJiraClient` structure. Primary constructor with base call.

### Step 5: Update DI registration

Modify `AddRestApiJira` to register the correct implementation based on `JiraOptions.IsTeamManaged`:

- Read `JiraOptions` from configuration
- If `IsTeamManaged` is `true`: register `TeamManagedJiraClient` as `IJiraClient`, passing `projectKey` from options
- If `IsTeamManaged` is `false` (default): register `RestApiJiraClient` as `IJiraClient` (current behavior)

Both classes need `IJiraApi` which is already registered as a Refit client. The factory resolves it from the service provider.

**Files to modify:**
- `src/Modules/Jira/Jira.RestApi/DependencyInjection.cs`

**Key constraint:** The Refit client registration (`AddRefitClient<IJiraApi>`) stays exactly as-is. Only the `IJiraClient` registration changes from a direct `AddTransient` to a factory.

### Step 6: Update documentation

Update `docs/jira-team-managed-workarounds.md`:

- Correct the changelog cap: **100 items on Jira Cloud** (not 20 — that's Jira Server/Data Center)
- Document the inheritance architecture (base class vs subclass, when each is used)
- Document the changelog enrichment strategy (detect truncation via `Total > Histories.Count`, fetch full changelog via paginated endpoint)
- Document `JiraOptions.IsTeamManaged` and `JiraOptions.ProjectKey` configuration
- Add the epic endpoint deprecation note (May 2026, removal November 2026)
- Add the Agile API deprecation timeline as a future migration note

**Files to modify:**
- `docs/jira-team-managed-workarounds.md`

## Migration Notes

No database migrations. Configuration change only: users with team-managed boards add to `appsettings.json`:

```json
{
  "Jira": {
    "InstanceUrl": "...",
    "Email": "...",
    "ApiToken": "...",
    "IsTeamManaged": true,
    "ProjectKey": "FOK"
  }
}
```

## Testing Strategy

**Build verification:**
- `dotnet build` the solution — no compilation errors

**Company-managed board (regression):**
- Set `IsTeamManaged: false` (or omit — default)
- Sync a sprint → verify identical behavior to current code
- Verify `GetSprintIssuesAsync` uses Agile API path only (no JQL)

**Team-managed board:**
- Set `IsTeamManaged: true`, `ProjectKey: "FOK"`
- Sync a sprint → verify issues are fetched via JQL
- Check sync result: `TicketsUpserted` should be non-zero
- For a ticket with many status changes: verify `StatusTransition` records are complete (compare count against Jira UI changelog)
- Verify `WasCommitted` accuracy: check a ticket that was added mid-sprint — should show `WasCommitted = false`

**Changelog enrichment:**
- Find or create a Jira issue with 100+ changelog entries
- Sync it → verify all transitions are captured, not just the most recent 100

## Open Questions

1. **Epic JQL for team-managed:** Team-managed projects never had `"Epic Link"` — they use `parent`. Should the `GetEpicIssuesAsync` override try `"Epic Link"` first and fall back to `parent`, or just use `parent` directly? Using `parent` is future-proof but may not work for company-managed boards using the subclass. Recommendation: try `"Epic Link"` first, catch JQL parse error, retry with `parent`.

2. **Agile API deprecation timeline:** The `/rest/agile/1.0/` endpoints are deprecated with removal November 2026. Should we plan the migration to `/rest/software/1.0/` now, or defer? Recommendation: defer — it's a separate 6-month concern and doesn't block this work.
