# Xray Integration & Ticket Test Enrichment

**Traces to:** `docs/product/v2.md` §3, §4, §5.1, §6.2, §6.3
**Source:** Scratch
**Dependencies:** F3 (Settings System — AppSettings entity), F5 (Sprint Sync — sync pipeline), F4 (Jira Integration — Jira client; must be extended to read issue links, which are not currently fetched)
**Status:** Done
**Plan:** `docs/specs/F25-XrayIntegration/delivery/plan.md`

---

## Purpose

Fokus v1 delivers sprint health analytics for delivery — velocity, scope change, carry-over, bug ratio, cycle time. It has no visibility into testing. Scrum Masters cannot answer "was this sprint actually tested?" or "did the tests pass?" without manually checking Xray.

This feature adds the foundation for QA analytics by integrating with Xray Cloud. It introduces the Xray client (authentication and GraphQL queries), four new domain entities for test data, a dual-API sync pipeline (Jira REST for coverage links + Xray GraphQL for test run results), and a Settings UI for configuring the integration. The entire QA module is gated behind a feature flag — when disabled, Fokus behaves exactly as v1.

This is infrastructure — no QA metrics or visualizations appear in existing pages. All analytics display (dashboard cards, developer quality, QA workload) is delivered by F26–F31.

## Entities

Four new entities. The existing Developer entity is reused for QA members — the team's audit showed QA and developer roles overlap on the same people.

Reference `docs/product/v2.md` §4 for canonical definitions.

### Test Execution

The primary QA entity. Represents a Test Execution issue from Xray, linked to sprint stories via issue links.

- **Issue ID** — string, unique identifier from Xray (primary key)
- **Issue Key** — string, Jira issue key (e.g., "ABC-102")
- **Summary** — string, human-readable title
- **Status** — string, Jira issue status (To Do, In Progress, Done, Cancelled)
- **Assignee** — references a Developer by account ID (who ran the execution). Nullable.
- **Created Date** — date/time when the TE was created

A Test Execution belongs to zero or more sprints, derived from its linked stories (see Business Rules §3).

### Test Execution Link

The relationship between tickets and their linked Test Executions. One record per link.

- **Test Execution** — references a Test Execution by issue ID
- **Ticket** — references a Ticket by key
- **Link Type** — one of:
  - **Tests** — the TE tests this story (coverage relationship)
  - **Blocks** — a bug was found by this TE (defect traceability)

### Test Run

The individual result of running one test within one Test Execution. This is where pass/fail lives. Only accessible via the Xray GraphQL API — Jira cannot provide this data.

- **ID** — string, Xray test run identifier (primary key)
- **Test Execution** — references a Test Execution by issue ID
- **Status** — one of: PASS, FAIL, TODO, EXECUTING, ABORTED
- **Status Name** — string, display name from Xray
- **Started At** — date/time, nullable (when execution began)
- **Finished At** — date/time, nullable (when execution completed)
- **Executed By** — references a Developer by account ID, nullable (who executed this specific run)

### Test Set

Represents a Test Set issue from Xray, used to group tests by feature area (e.g., "Dashboard", "Verification"). Linked to stories via issue links.

- **Issue ID** — string, unique identifier from Xray (primary key)
- **Issue Key** — string, Jira issue key (e.g., "ABC-103")
- **Summary** — string, human-readable title (e.g., "v6 | Dashboard")
- **Assignee** — references a Developer by account ID, nullable
- **Status** — string

Test Sets are stored for context (feature area grouping) but are not the primary analytics entity — Test Executions with their test runs drive all QA metrics.

### Entities NOT modeled

- **Test Case** — the "Test" issue type exists in Xray but the team creates zero Test issues. All testing flows through Test Executions directly. Deferred until the team adopts standalone test cases.
- **Test Plan** — Test Plans exist (5 total) but have no issue links to epics or stories. Named by version/milestone convention only. Cannot be traversed programmatically. Deferred until the team establishes a linking convention.
- **QA Member** — QA and developer roles overlap (the same people execute tests and write code). The Developer entity is reused. QA analytics surfaces filter by "developers who have Test Execution assignments."

### Settings Extensions

The existing application settings entity gains three new fields:

- **Xray Enabled** — boolean, feature flag gating the entire QA module. Default: false.
- **Xray Client ID** — string, from Xray Global Settings API Keys. Required when enabled.
- **Xray Client Secret** — string, sensitive credential from Xray Global Settings API Keys. Required when enabled.

## User Flows

```
Flow 1: Enable and Configure Xray Integration
1. User navigates to Settings
2. The Xray section shows a toggle (default: off) with credentials fields hidden
3. User enables the Xray toggle
4. Client ID and Client Secret fields appear, along with a "Test Connection" button and a "Sync QA Data" button (disabled until credentials are saved)
5. User enters their Xray API credentials (Client ID + Client Secret from Xray Global Settings → API Keys)
6. User clicks "Test Connection"
7. System authenticates with Xray (POST to Xray authenticate endpoint with credentials)
8. System makes a lightweight probe query (fetch one Test Execution) to verify data access
9. On success: confirmation message shown (e.g., "Connected — Xray access verified")
10. On failure: error message shown with reason (e.g., "Authentication failed — check your Client ID and Secret")
11. User saves settings
```

```
Flow 2: Sprint Sync with QA Data (Piggyback)
1. User triggers a sprint sync (existing flow — single sprint or all sprints)
2. System syncs Jira data as before (sprint, tickets, status transitions)
3. If Xray is enabled and credentials are configured:
   a. For each synced ticket, system reads Jira issue links to discover linked Test Executions and Test Sets (link type "Test" / "is tested by")
   b. For each discovered Test Execution, system also reads Jira issue links for "Blocks" / "is blocked by" links (bug traceability)
   c. System authenticates with Xray (or reuses cached bearer token)
   d. For each discovered Test Execution, system queries the Xray GraphQL API to fetch test runs and their statuses (PASS/FAIL/TODO/EXECUTING/ABORTED)
   e. System stores all Test Executions, Test Execution Links, Test Runs, and Test Sets
4. If Xray is enabled but sync fails (invalid credentials, Xray unreachable, API error):
   a. The Jira sprint sync completes normally — sprint data is unaffected
   b. The sync response includes a warning message describing the Xray failure (e.g., "Xray sync failed: bearer token expired")
   c. Frontend displays the warning as a toast or inline alert
5. Sync response returns the usual sync summary plus a QA section: Test Executions synced, Test Runs synced, Test Sets synced, and any warnings
```

```
Flow 3: Standalone QA Sync
1. User navigates to Settings → Xray section
2. User selects sprint(s) from a dropdown of synced sprints (defaults to the most recently synced sprint)
3. User clicks "Sync QA Data" (enabled only when Xray is enabled and credentials are saved)
4. System reads Jira issue links for all tickets in the selected sprint(s) to discover linked TEs and Test Sets
5. System queries Xray GraphQL API for test run results on discovered TEs
6. System upserts all QA entities (idempotent — re-syncing overwrites with fresh data)
7. On success: confirmation with counts (e.g., "Synced 12 Test Executions, 47 Test Runs for Sprint 28")
8. On failure: warning message with reason; previously synced QA data is unchanged
```

```
Flow 4: Disable Xray Integration
1. User navigates to Settings
2. User toggles Xray off
3. System hides Xray credential fields and sync button
4. Previously synced QA data is retained in the database but hidden from all surfaces
5. Re-enabling Xray restores access to previously synced data without re-syncing
6. Credentials are preserved — user does not need to re-enter them after toggling
```

```
Flow 5: Sprint Sync with Xray Disabled
1. User triggers a sprint sync while Xray is disabled
2. System syncs Jira data normally (sprint, tickets, status transitions)
3. No Xray API calls are made
4. No QA-related data appears in the sync response
5. Existing QA data from prior syncs remains in the database, untouched
```

## API Surface

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|--------------|
| POST | /api/xray/test-connection | Required | None (reads credentials from saved settings) | `{ success, message }` | 200, 400 (not configured), 401 (invalid credentials), 502 (Xray unreachable) |
| POST | /api/xray/sync | Required | `{ sprintIds }` (array of sprint IDs to sync QA data for) | `{ testExecutionsSynced, testRunsSynced, testSetsSynced, warnings[] }` | 200, 400 (Xray not enabled), 502 (Xray unreachable) |

**Existing endpoint changes:**

| Method | Route | Change |
|--------|-------|--------|
| POST | /api/sync/sprints | Response gains optional `xray` object: `{ testExecutionsSynced, testRunsSynced, testSetsSynced, warnings[] }`. Null when Xray is disabled. |
| POST | /api/sync/backlog | Same response extension as sprint sync. |
| GET | /api/settings | Response gains `xrayEnabled`, `xrayClientId`, `xrayClientSecret` fields. Secret is masked (e.g., "****") or empty string — never the actual secret. |

**New settings endpoint (follows per-section save pattern):**

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|--------------|
| PUT | /api/settings/xray | Required | `{ xrayEnabled, xrayClientId, xrayClientSecret }` | `{ success }` | 200, 400 (validation error) |

The existing settings pattern uses per-section save endpoints (e.g., `/api/settings/board`, `/api/settings/health-config`). Xray settings follow the same pattern with a dedicated `/api/settings/xray` endpoint. The Client Secret is write-only — it can be saved but never read back in plaintext via the GET endpoint.

**Error responses:**

- `400` — Xray not enabled, or missing required fields
- `401` — Xray authentication failed (invalid Client ID or Client Secret)
- `502` — Xray API unreachable or returned an unexpected error (Fokus acts as a gateway)

## Business Rules

1. **Feature flag gates everything.** When Xray is disabled: no Xray API calls are made, no QA UI elements appear (F26+ concern), no QA data is included in sync responses, and the health score is computed from delivery metrics only. Enabling the flag does not trigger a sync — the user must sync manually or wait for the next sprint sync.

2. **Dual-API sync.** QA data requires two API sources: (a) Jira REST API for issue links — discovers which Test Executions and Test Sets are linked to sprint tickets, and which bugs are linked to TEs via "Blocks" links; (b) Xray GraphQL API for test run results — fetches the PASS/FAIL/TODO/EXECUTING/ABORTED status of each test run within each Test Execution. Jira provides the coverage graph; Xray provides the results.

3. **Sprint derivation for Test Executions.** A TE's sprint membership is derived from its linked stories, not from the TE's own sprint field (which is inconsistently populated). If a TE is linked to story ABC-101 and ABC-101 belongs to Sprint 28, the TE belongs to Sprint 28. If a TE links to stories in multiple sprints, it belongs to the most recent sprint.

4. **Idempotent sync.** Re-syncing QA data for a sprint overwrites all existing QA data for that sprint's tickets with fresh data from Jira and Xray. No manual merge or conflict resolution.

5. **Graceful degradation.** If Xray sync fails during a sprint sync, the Jira data sync completes normally. The failure is reported as a warning in the sync response — it does not block or roll back sprint data. The user sees a toast or inline alert with the failure reason.

6. **Bearer token management.** Xray bearer tokens expire after 24 hours. Tokens are cached in memory and refreshed proactively before expiry (at approximately 23 hours). No persistent token storage. If a token refresh fails mid-sync, the sync reports the error via the warnings mechanism.

7. **Cancelled Test Executions are synced but excluded.** TEs with Jira status "Cancelled" are stored in the database for data completeness but excluded from all metric computations in F26+. This preserves the audit trail without distorting quality numbers.

8. **Feature flag toggle retains data.** Disabling Xray does not delete previously synced QA data. The data remains in the database and becomes visible again immediately if Xray is re-enabled, without requiring a re-sync. Credentials are also preserved.

9. **Credential security.** The Xray Client Secret is write-only via the settings API — it can be saved but never read back in plaintext. GET requests return a masked value. The secret is stored using the same protection mechanism as existing sensitive settings (Jira credentials pattern).

10. **Connection test validates both auth and access.** The "Test Connection" button authenticates with Xray (verifying credentials) and then makes a lightweight probe query (fetch one Test Execution) to verify the credentials have project-level data access. Both steps must succeed for a positive result.

11. **Best-effort historical backfill.** When syncing past sprints, Xray data is fetched for all tickets regardless of sprint age. Older sprints may have incomplete QA data due to inconsistent issue links — this is acceptable. The system does not distinguish between "no QA data exists" and "QA data was not linkable."

12. **Rate limiting.** Xray Cloud imposes rate limits (300 requests per 5 minutes on Standard tier, 1,000 on Enterprise). A typical sprint of 30 stories requires 2–5 GraphQL calls. The sync pipeline respects rate limits with appropriate throttling. Assume Standard tier limits until confirmed otherwise.

13. **Test Execution Link types.** Two link types are tracked: (a) "Tests" / "is tested by" — a TE tests a story, establishing the coverage relationship; (b) "Blocks" / "is blocked by" — a bug was discovered during a TE's execution, establishing defect traceability. Both are read from Jira issue links during sync.

14. **Test Set storage is contextual.** Test Sets are synced and stored for feature area grouping context (e.g., "Dashboard", "Verification") but do not drive metrics. Test Executions and their test runs are the primary analytics entities.

15. **Ticket enrichment is data-layer only.** F25 stores per-ticket test data (linked TEs, test run results, coverage status, bug count) but does not expose it in any existing UI surface. All visualization is deferred to F26+. The enrichment data is available via the domain model for downstream features to query.

16. **A TE linked to multiple tickets counts for each independently.** If a Test Execution is linked to three stories, it contributes coverage to all three. The Test Execution Link entity supports this — one record per TE-ticket pair.

17. **A ticket with multiple TEs uses worst-result aggregation.** When a ticket has multiple linked Test Executions, some passing and some failing, the overall coverage status for that ticket is the worst result (FAIL beats PASS beats TODO). Both counts are stored so downstream features can show the breakdown.

18. **A TE with multiple test runs uses worst-result aggregation.** All test runs within a TE count. The TE's overall status is the worst across all its runs — one FAIL makes the TE status FAIL regardless of other passing runs.

19. **Sub-tasks inherit parent ticket's test coverage for aggregation but can have their own TEs.** During sync, issue links are read on both parent tickets and sub-tasks. Sub-tasks with their own linked TEs contribute those TEs independently. For aggregation in downstream features, sub-tasks without their own TEs inherit the parent's coverage status.

20. **GraphQL pagination constraints.** The Xray GraphQL API limits responses to 1–100 items per connection, 10,000 items per call, and 25 resolvers per call. JQL filter results are capped at 100 issues per query. The sync pipeline must paginate when results exceed these limits.

21. **Test Set links are not tracked separately.** Test Sets are stored as standalone entities for context (feature area grouping). Their issue links to stories are not tracked in a separate link table — only Test Execution links are tracked, since TEs drive all metrics. Test Set to story relationships can be derived from Jira issue links on-demand if needed by future features.

22. **Jira module must be extended for issue link reading.** The existing Jira integration does not fetch issue links during sprint sync. F25 requires the Jira sync to include `issuelinks` in the ticket fields retrieved, and to parse the link types ("Test"/"is tested by", "Blocks"/"is blocked by") to discover linked Test Executions, Test Sets, and bugs. This extension is part of F25 scope, not a pre-existing capability.

## Acceptance Criteria

### Settings UI

- [ ] Settings page shows an Xray section with a toggle, default off
- [ ] When Xray toggle is off, credential fields, Test Connection button, and Sync QA Data button are hidden
- [ ] When Xray toggle is on, Client ID field, Client Secret field, Test Connection button, and Sync QA Data button are visible
- [ ] Client Secret field uses a password input (masked characters)
- [ ] GET /api/settings never returns the Xray Client Secret in plaintext — returns masked value or empty string
- [ ] PUT /api/settings/xray accepts and persists xrayEnabled, xrayClientId, xrayClientSecret fields
- [ ] Credentials are preserved when the toggle is switched off and back on

### Connection Test

- [ ] POST /api/xray/test-connection returns 200 with `{ success: true, message }` when credentials are valid and data is accessible
- [ ] POST /api/xray/test-connection returns 401 when Client ID or Client Secret is invalid
- [ ] POST /api/xray/test-connection returns 400 when Xray is not enabled or credentials are not configured
- [ ] POST /api/xray/test-connection returns 502 when Xray API is unreachable
- [ ] Connection test authenticates AND performs a probe query (not just authentication)

### Sprint Sync with QA Data

- [ ] When Xray is enabled, POST /api/sync/sprints syncs Jira data AND Xray data for the sprint's tickets
- [ ] Sync reads Jira issue links on each ticket to discover linked Test Executions (link type "Test" / "is tested by")
- [ ] Sync reads Jira issue links on each TE to discover linked bugs (link type "Blocks" / "is blocked by")
- [ ] Sync queries Xray GraphQL API for test runs and statuses on each discovered TE
- [ ] Sync response includes `xray` object with testExecutionsSynced, testRunsSynced, testSetsSynced counts
- [ ] When Xray is disabled, sync response has no `xray` object — no Xray API calls are made
- [ ] When Xray sync fails, Jira data sync completes normally and the response includes a warning in the `xray.warnings` array
- [ ] Re-syncing a sprint overwrites existing QA data (idempotent)
- [ ] POST /api/sync/backlog includes Xray data for all synced sprints (best-effort)

### Standalone QA Sync

- [ ] POST /api/xray/sync accepts an array of sprint IDs and syncs QA data for those sprints
- [ ] POST /api/xray/sync returns 400 when Xray is not enabled
- [ ] POST /api/xray/sync returns 200 with counts of synced entities on success
- [ ] POST /api/xray/sync returns warnings array when partial failures occur (e.g., some TEs unreachable)
- [ ] Standalone sync is idempotent — re-syncing overwrites with fresh data

### Data Model

- [ ] Test Executions are stored with issue ID, issue key, summary, status, assignee, and created date
- [ ] Test Execution Links are stored with TE reference, ticket reference, and link type (Tests or Blocks)
- [ ] Test Runs are stored with ID, TE reference, status (PASS/FAIL/TODO/EXECUTING/ABORTED), status name, timestamps, and executed-by reference
- [ ] Test Sets are stored with issue ID, issue key, summary, assignee, and status
- [ ] Cancelled Test Executions (status = "Cancelled") are stored but flagged for exclusion from metrics
- [ ] Sprint membership for TEs is derived from linked stories' sprint membership, not from the TE's own sprint field
- [ ] When a TE links to stories in multiple sprints, it is attributed to the most recent sprint
- [ ] A TE linked to multiple tickets creates one TestExecutionLink per ticket (counts independently)
- [ ] Sub-task issue links are traversed during sync — sub-tasks can have their own linked TEs
- [ ] Test Sets are stored as standalone entities without a separate link table to tickets
- [ ] Jira issue link reading is extended to include `issuelinks` field during sprint ticket fetch

### Feature Flag

- [ ] When Xray is disabled, no Xray API calls are made during sprint sync
- [ ] When Xray is disabled, previously synced QA data remains in the database (not deleted)
- [ ] Re-enabling Xray makes previously synced QA data accessible without re-syncing

### Authentication & Token Management

- [ ] System authenticates with Xray using Client ID + Client Secret to obtain a bearer token
- [ ] Bearer token is cached in memory and reused for subsequent requests within its validity period
- [ ] Bearer token is refreshed proactively before the 24-hour expiry
- [ ] If token refresh fails during sync, the failure is reported via the warnings mechanism

## Out of Scope

- **QA metrics display** — dashboard cards, developer quality columns, QA workload page, health score integration. All visualization is F26–F31.
- **Test case management** — Fokus reads test results, it does not create or manage test cases. That stays in Xray.
- **Automated test result import** — CI/CD pipeline results (JUnit, Cucumber). F25 covers manual testing data only.
- **Test flakiness detection** — tracking tests that flip between PASS and FAIL across executions. Requires execution history depth not available in F25.
- **Xray Server/DC support** — different API (REST-only, different entity model). Cloud-only for now.
- **Multi-project Xray support** — tests spanning multiple Jira projects. Assumes single project.
- **Test Plan traversal** — Test Plans have no issue links to epics or stories. The `getTestPlans` Xray GraphQL query is not called. Deferred until the team establishes a linking convention.
- **Test Set analytics** — regression vs. smoke vs. feature test breakdowns. Team's Test Set naming conventions are not yet stable enough to categorize.
- **Quality health thresholds in Settings** — configurable thresholds for QA health scoring. Delivered with F26 when the health score integration ships.

## Open Questions

- [ ] **Xray license tier** — Standard (300 req/5min) or Enterprise (1,000 req/5min)? Affects sync batching strategy. Assuming Standard until confirmed.
