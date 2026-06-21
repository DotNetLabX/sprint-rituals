# Xray Integration & Ticket Test Enrichment — Review

## Reviewed By

`reviewer` (Sonnet agent, claude-sonnet-4-6). No Codex cross-validation requested.

---

## Cycle 2 Re-Review — APPROVE

All three HIGH findings and both MEDIUM findings from Cycle 1 are resolved. Build is clean. No regressions found. See evidence table at bottom.

---

## Cycle 1 Verdict: REQUEST CHANGES (superseded)

Three HIGH findings. Two break runtime behavior (Sync QA Data button always 400, TestConnection probe is a no-op). One renders the rate limiter useless (scoped service with instance-level counter).

---

## Pre-commitment Predictions

| Predicted | Found |
|-----------|-------|
| Validator/UI mismatch (architect-flagged) | CONFIRMED — HIGH |
| Secret preservation logic fragile | NOT an issue — SetValues on self is a no-op; secret preserved correctly |
| Domain entities referencing Xray DTOs | NOT an issue — factories take primitive params only |
| TestConnection probe not actually querying | CONFIRMED — HIGH |
| AppSettings SaveAsync overwriting secret | NOT an issue — same entity passed in and out; EF identity map returns same instance |

---

## Findings

### [HIGH] Sync QA Data button always returns 400 — validator rejects empty sprint array

**File:** `src/Services/Fokus/Fokus.API/Features/Xray/SyncXray/SyncXrayEndpoint.cs:14-16`

**Issue:** `SyncXrayRequestValidator` enforces `RuleFor(x => x.SprintIds).NotEmpty()`. The frontend `runXraySync()` sends `syncXrayData(selectedXraySprints.value)` where `selectedXraySprints` is initialized as `ref<number[]>([])` and is never populated — no sprint selector UI was wired. Every click of "Sync QA Data" sends `{ sprintIds: [] }` and receives a `400 Bad Request`. The button is entirely broken.

The implementation.md deviation states "the endpoint handles sprint scope server-side" (implying the backend loads all sprints when given an empty array), but the validator explicitly blocks this. The two halves contradict each other.

**Fix (two options — developer implements, architect picks):**
- Option A: Remove the `NotEmpty()` rule. When `SprintIds` is empty, the handler treats it as "all synced sprints" and loads them from `SprintRepository`. Add the sprint-loading logic to the handler (currently it only calls `GetSprintsWithMembershipsAsync(sprintIds)` which returns nothing for an empty list).
- Option B: Add a sprint multi-select to the Xray tab (per the original plan). Load sprints from `sprintsForRange` or a dedicated call, populate `selectedXraySprints`, keep the validator as-is.

Note: Option A also has a secondary bug — even if `NotEmpty()` is removed and `SprintIds` is `[]`, `GetSprintsWithMembershipsAsync([])` returns no sprints and the sync silently does nothing. The handler needs a branch: if `SprintIds.Length == 0`, load all synced sprints.

---

### [HIGH] TestConnection probe query is a no-op — data access is never verified

**File:** `src/Services/Fokus/Fokus.API/Features/Xray/TestConnection/TestConnectionEndpoint.cs:28`
**Supporting file:** `src/Modules/Xray/Xray.GraphQL/GraphQLXrayClient.cs:26-28`

**Issue:** The plan (Step 11) and acceptance criteria both require the connection test to "authenticate AND perform a probe query (fetch one Test Execution) to verify data access." The implementation calls `await xrayClient.GetTestExecutionsAsync(token, [], ct)` — an empty `issueKeys` list. In `GraphQLXrayClient.GetTestExecutionsAsync`:

```csharp
if (issueKeys.Count == 0)
    return result;   // line 28 — returns immediately, no HTTP call made
```

The GraphQL endpoint is never contacted. A user with valid credentials but no data-access permissions (wrong project, revoked scope) will receive a false-positive "Connected — Xray access verified." Acceptance criterion: "Connection test authenticates AND performs a probe query (not just authentication)" is violated.

**Fix:** Either (a) add a dedicated probe method to `IXrayClient` that sends a minimal `getTestExecutions(jql: "", limit: 1)` query regardless of issue key list, or (b) send a small hardcoded JQL that always returns results (e.g., `limit: 1, start: 0` with empty/wildcard JQL). The current `issueKeys` guard is correct for production sync but must be bypassed for the probe case.

---

### [HIGH] Rate limiter is ineffective — scoped service resets counter on every request

**File:** `src/Modules/Xray/Xray.GraphQL/GraphQLXrayClient.cs:17-19`

**Issue:** `GraphQLXrayClient` is registered as `AddScoped<IXrayClient, GraphQLXrayClient>()` (DependencyInjection.cs:20). The rate limit state lives on instance fields:

```csharp
private int _requestCount = 0;
private DateTime _windowStart = DateTime.UtcNow;
private readonly SemaphoreSlim _rateLock = new(1, 1);
```

Each HTTP request creates a fresh `GraphQLXrayClient` instance — `_requestCount` resets to `0` and `_windowStart` resets to `DateTime.UtcNow` on every incoming request. The sliding window never accumulates across requests. A burst of concurrent syncs can each send up to 290 requests before their per-instance counters trip, with no cross-request coordination.

The plan (Step 6) specifies "track request count per 5-minute window." This only works if the counter survives across requests. `XrayBearerTokenManager` is correctly a singleton; `GraphQLXrayClient` needs the same treatment, or its rate limit state must be extracted into a singleton helper.

**Fix:** Either (a) register `GraphQLXrayClient` as singleton (aligns with `XrayBearerTokenManager`), or (b) extract `_requestCount` / `_windowStart` / `_rateLock` into a new `XrayRateLimiter` singleton class and inject it into the scoped `GraphQLXrayClient`. Option (b) is architecturally cleaner since `HttpClient` itself is managed by the factory with its own lifetime.

---

### [MEDIUM] Duplicate Jira issues processed in standalone sync

**File:** `src/Services/Fokus/Fokus.API/Features/Xray/SyncXray/SyncXrayEndpoint.cs:52-59`

**Issue:** When a sprint overlap occurs or a ticket belongs to multiple requested sprints, `jiraClient.GetSprintIssuesAsync(sprint.Id)` may return overlapping issues. `allJiraIssues.AddRange(issues)` accumulates all results without deduplication. `XrayIssueSyncService` then processes the same TE key multiple times — duplicating `UpsertAsync`, `ReplaceLinksAsync`, `ReplaceTestRunsAsync`, and `SaveChangesAsync` calls per duplicate ticket. Upsert semantics mean the final result is correct, but it triggers redundant Jira fetches and DB round-trips per duplicate.

**Fix:** Deduplicate by issue key after collecting: `allJiraIssues = allJiraIssues.DistinctBy(i => i.Key).ToList()`.

---

### [MEDIUM] `saveXrayPanel` passes empty string for null ClientSecret — triggers unnecessary secret clear attempt

**File:** `client/src/views/SettingsView.vue:175`

**Issue:** `saveXrayPanel` calls `store.saveXraySettingsAction(form.xrayEnabled, form.xrayClientId ?? '', form.xrayClientSecret ?? '')`. When the user has not changed the secret field (`form.xrayClientSecret` is `null` from the masked GET response), this sends `xrayClientSecret: ""` to the PUT endpoint. The backend's `if (!string.IsNullOrEmpty(req.XrayClientSecret))` guard correctly blocks the overwrite — the secret is preserved. However, the API contract for `saveXraySettings` in `settings.ts:84` declares `xrayClientSecret: string` (not `string | null`), forcing the always-send pattern. This is a leaky abstraction — the secret is implicitly "don't overwrite me" when empty. It works correctly today but is fragile if the guard is ever loosened.

This is not a bug today (backend guard is in place) but worth making explicit.

**Fix (optional):** Change the API function signature to `xrayClientSecret: string | null` and send `null` explicitly when unchanged, or add a comment in the endpoint explaining the preservation contract.

---

### [LOW] `TestRunStatus` not in `XrayTestRunDto` — status enum parsing happens in service, not contract

**File:** `src/Modules/Xray/Xray.Contracts/XrayDtos.cs:19-26`

**Issue:** `XrayTestRunDto` has `StatusName` (string) but no parsed `TestRunStatus` enum field. The plan's DTO shape includes `status` as a mapped value. The parsing of `StatusName → TestRunStatus` is done in `XrayIssueSyncService.ParseTestRunStatus`. This is a reasonable placement (API layer maps to domain), but the contract DTO does not carry any structured status. This is consistent but worth noting if the Xray module is ever reused outside Fokus — consumers must implement their own parser.

No fix required; design is acceptable.

---

### [LOW] `SprintIssueSyncService.SyncAsync` calls `appSettingsRepository.GetAsync` twice

**File:** `src/Services/Fokus/Fokus.API/Features/Sync/SprintIssueSyncService.cs:37` and `:167`

**Issue:** `SyncSprintsFromJiraAsync` calls `appSettingsRepository.GetAsync(ct)` (as `settings` on line 37) for `PlanningWindowDays`. Then `SyncAsync` calls `appSettingsRepository.GetAsync(ct)` again (as `settings2` on line 167) just to check `XrayEnabled`. Since EF's identity map returns the same instance on the second call (same DbContext scope), there is no extra DB query — but it reads confusingly. The `settings` from the outer call could be passed into `SyncAsync` to avoid the redundant call pattern.

**Fix (optional):** Pass `settings` as a parameter to `SyncAsync`, or cache it and pass down. Low priority since EF identity map neutralizes the perf concern.

---

### [LOW] `GraphQLXrayClient` query uses `executedById` field name inconsistently with plan

**File:** `src/Modules/Xray/Xray.GraphQL/GraphQLXrayClient.cs:110`

**Issue:** The GraphQL query string requests the `assignee` field for test runs (`"results { id status { name } startedOn finishedOn assignee }"`), and the parser reads `assignee.accountId` as `executedById`. The plan's GraphQL query shape in Step 6 uses `executedById` as the field name. This may work if Xray's actual API uses `assignee` on test runs — the implementation is potentially more accurate than the plan. However, it cannot be verified without live API access.

No change needed; note this as an open question for first live test.

---

## Positive Observations

- Domain entities are cleanly separated from Xray DTOs. Factories take primitive parameters; no Xray contract leaks into `Fokus.Domain`. Plan constraint followed exactly.
- Secret preservation in `SaveXraySettingsEndpoint` is implemented correctly. The order-of-operations (modify `existing` → pass `existing` to `SaveAsync`) is safe given EF's identity map behavior.
- `XrayIssueSyncService` never throws. Every Xray API call is wrapped in try/catch with graceful degradation to warnings. Sprint sync cannot be broken by Xray failures.
- `XrayBearerTokenManager` is correctly a singleton with per-clientId cache keyed for credential changes without restart. Thread safety via `SemaphoreSlim` is correct.
- `TestExecutionRepository` follows the established `ReplaceTransitionsAsync` pattern (delete-then-insert) for links and runs. Idempotent sync semantics are consistent with the rest of the codebase.
- Help tooltips are wired to all five Xray UI elements using exact text from `help.tooltips.md`. Plan and spec requirement satisfied.
- `MaskSecret` in `GetSettingsEndpoint` is correct: non-empty → `"****"`, null/empty → `""`. GET endpoint never returns plaintext secret. BR 9 satisfied.
- Migration `AddXraySupport` is additive-only. All four new tables, three new AppSettings columns confirmed in the migration file. No drops or renames.
- Build: clean. `dotnet build` produces 0 errors, 3 warnings (2 pre-existing NU1903 advisory, 1 pre-existing CS9107). No new warnings introduced by this feature.
- Feature flag gating: `SyncAsync` checks `settings2.XrayEnabled` before any Xray call. Piggyback path correctly skipped when disabled.
- Frontend Xray tab correctly gated to Admin only (`v-if="authStore.isAdmin"`).

---

## Gaps

- **Standalone sync with empty sprint list does nothing silently:** Even after fixing the validator (removing `NotEmpty`), `GetSprintsWithMembershipsAsync([])` returns no sprints and the sync completes with 0 counts and no warning. User gets no feedback that nothing happened. The handler should either load all synced sprints, or return a 400 "No sprint IDs provided."
- **`TestConnection` success message could mislead users:** Since the probe query is a no-op, a user with invalid project permissions receives a success response. This could lead to confusing "connection works but no data syncs" situations until the probe is fixed.
- **Rate limiter gap:** No cross-request rate limiting until the scoped/singleton issue is fixed. On a server with concurrent sync requests, Xray could receive bursts exceeding 300 req/5min.
- **`IssueLinks` null handling in `XrayIssueSyncService`:** `JiraLinkedIssueFields.Summary` is `string Summary { get; set; } = string.Empty;` (non-nullable), so reading `linkedIssue.Fields.Summary` is safe. Verified OK.
- **Sub-task issue links:** The plan says sub-tasks are processed the same way — confirmed, `XrayIssueSyncService` iterates all `jiraIssues` including sub-tasks without special-casing. Spec BR 19 satisfied.

---

## Open Questions

- **GraphQL `assignee` vs `executedById` field name on test runs:** The Xray GraphQL schema may expose the executor as `assignee` (not `executedById`). The implementation uses `assignee`; the plan spec used `executedById`. Without live API verification, this cannot be confirmed either way. Should be validated on first real sync.
- **`SyncXrayEndpoint` behavior when no sprints match the requested IDs:** If none of the requested sprint IDs exist in the repository, `sprints` is empty, `allJiraIssues` is empty, and the sync returns 0 counts with no warning. Should this be a 404 or a 200 with a warning?

---

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Build | PASS | `dotnet build src/Fokus.slnx --no-incremental -v minimal` | 0 errors, 3 warnings (all pre-existing) |
| Validator mismatch | FAIL | Code inspection: `SyncXrayRequestValidator` line 14, `runXraySync` line 207 | `NotEmpty()` rejects `[]`; frontend always sends `[]` |
| Probe query no-op | FAIL | Code inspection: `TestConnectionEndpoint` line 28, `GraphQLXrayClient` line 26-28 | `issueKeys.Count == 0` guard short-circuits before GraphQL call |
| Rate limiter scope | FAIL | Code inspection: `DependencyInjection.cs` line 20, `GraphQLXrayClient` fields 17-19 | Scoped service; counter resets per request |
| Secret preservation | PASS | Code inspection: `SaveXraySettingsEndpoint` lines 29-38, `AppSettingsRepository.SaveAsync` line 27 | `SetValues(existing)` on same EF-tracked instance; no overwrite |
| Domain/Xray isolation | PASS | Code inspection: all factories in `Behaviors/` | Primitive params only; no Xray.Contracts imports in Domain |
| Migration completeness | PASS | `Migrations/20260514173107_AddXraySupport.cs` | 4 tables + 3 AppSettings columns; additive only |
| Tooltip text accuracy | PASS | Template vs `help.tooltips.md` | Exact text match on all 5 elements |

---

## Cycle 2 — Fix Verification

### HIGH-1: Validator/UI mismatch — RESOLVED

`NotEmpty()` removed from `SyncXrayRequestValidator`. The validator now only contains `RuleForEach(x => x.SprintIds).GreaterThan(0)`, which passes vacuously for an empty array. Handler branches on `req.SprintIds.Length == 0`: calls `sprintRepository.GetAllAsync(ct)` for "all synced sprints", or `GetSprintsWithMembershipsAsync(sprintIds)` for specific IDs. When no sprints exist in either case, throws `BadRequestException("No synced sprints found. Run a Jira sync first.")` — the silent-nothing gap identified in Cycle 1 is also fixed. `SprintRepository.GetAllAsync` confirmed present (`Repositories/SprintRepository.cs:14`).

### HIGH-2: TestConnection probe is a no-op — RESOLVED

`ProbeAsync` added to `IXrayClient` (`IXrayClient.cs:13`). Implementation in `GraphQLXrayClient.ProbeAsync` sends a literal `{ getTestExecutions(limit: 1, start: 0) { total } }` query directly to `PostGraphQLAsync` — bypasses the empty-keys guard entirely. `TestConnectionEndpoint` now calls `ProbeAsync` after auth (`TestConnectionEndpoint.cs:28`). `PostGraphQLAsync` already checks the GraphQL `errors` array and throws `BadGatewayException` on GraphQL-level errors, so permission failures are correctly surfaced.

### HIGH-3: Rate limiter scoped-service reset — RESOLVED

`XrayRateLimiter` extracted as a new singleton class (`XrayRateLimiter.cs`). All rate-limit state (`_requestCount`, `_windowStart`, `_lock`) lives in the singleton. `XrayRateLimiter` registered as `AddSingleton<XrayRateLimiter>()` (`DependencyInjection.cs:20`). `GraphQLXrayClient` (still scoped) injects it via constructor and delegates `EnforceRateLimitAsync` to `rateLimiter.WaitForSlotAsync(ct)`. Counter now survives across requests. Logic is identical to Cycle 1 implementation — only the lifetime boundary moved.

### MEDIUM-4: Duplicate Jira issues — RESOLVED

`allJiraIssues = allJiraIssues.DistinctBy(i => i.Key).ToList()` added at `SyncXrayEndpoint.cs:64`. Deduplication runs after all sprints are fetched, before passing to `XrayIssueSyncService`.

### MEDIUM-5: Secret null contract — RESOLVED

`saveXraySettings` in `settings.ts:85` now takes `xrayClientSecret: string | null`. Comment added explaining the null-means-preserve contract. `saveXrayPanel` in `SettingsView.vue:176` sends `form.xrayClientSecret || null` — empty string coerces to null, null passes through. Store action `saveXraySettingsAction` signature updated to `xrayClientSecret: string | null` (`settingsStore.ts:144`). Comment added there too. Full chain consistent.

### Regression check

No regressions found. `GetTestExecutionsAsync` short-circuit for empty keys is untouched — production sync paths unaffected. `PostGraphQLAsync` unchanged. `SprintRepository.GetAllAsync` is an existing method, not new code. Build: 0 errors, 3 warnings (identical pre-existing set to Cycle 1).

### Cycle 2 Evidence

| Check | Result | Evidence |
|-------|--------|----------|
| Build | PASS | `dotnet build` — 0 errors, 3 warnings (all pre-existing) |
| HIGH-1 validator | PASS | `SyncXrayRequestValidator`: only `RuleForEach.GreaterThan(0)`; handler branches on empty |
| HIGH-1 empty-sprint guard | PASS | `BadRequestException` thrown when `sprints.Count == 0` |
| HIGH-2 probe query | PASS | `ProbeAsync` sends literal GraphQL to `PostGraphQLAsync`; GraphQL errors surfaced |
| HIGH-3 rate limiter | PASS | `XrayRateLimiter` singleton; `DependencyInjection.cs:20` registers as singleton |
| MEDIUM-4 dedup | PASS | `DistinctBy(i => i.Key)` at `SyncXrayEndpoint.cs:64` |
| MEDIUM-5 secret null | PASS | API fn, store action, and view all carry `string \| null`; null = preserve |
