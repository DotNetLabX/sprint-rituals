# JiraSync — Review

## Reviewed By
`reviewer` (Sonnet agent) + Codex cross-validation via `/codex:rescue`

## Verdict: APPROVE

_Cycle 1 was REQUEST CHANGES. Cycle 2 re-review (below) confirms all HIGH and MEDIUM items resolved._

---

## Cycle 2 Re-review

### Fixes verified

**[HIGH] Custom field deserialization — RESOLVED**
`JiraIssue.cs:16-21`: `[JsonPropertyName("customfield_10016")]` on `StoryPoints`, `[JsonPropertyName("customfield_10008")]` on `EpicKey`, `[JsonPropertyName("customfield_10014")]` on `EpicName`. All three attributes present and correct.

**[HIGH] Rate limiter lease inside retry loop — RESOLVED**
`JiraClient.cs:119-157`: Lease acquisition moved inside the `for` loop at line 124. `lease.IsAcquired` checked at line 125; throws `JiraApiException(TooManyRequests)` if queue full. Each HTTP attempt now acquires its own token.

**[MEDIUM] SyncBacklog silent exception swallowing — RESOLVED**
`SyncBacklogEndpoint.cs:94-98, 131-135`: Both catch blocks now call `logger.LogError(...)` and increment `sprintFailures`/`epicFailures` counters. `SyncBacklogResponse` now carries `SprintFailures` and `EpicFailures` fields. Callers can distinguish partial failures.
Note: the LOW finding about unconditional `developerRepository.SaveChangesAsync` in the epic loop (line 127) remains — it is not blocking and was marked LOW. In the sprint loop it is now correctly guarded (`if (developer is not null)` at line 78-79).

**[MEDIUM] JiraOptions startup validation — RESOLVED**
`JiraOptions.cs:7,9,11`: All three properties marked `[Required]`. `DependencyInjection.cs:18-21`: `AddOptions<JiraOptions>().Bind(...).ValidateDataAnnotations().ValidateOnStart()`. Misconfigured Jira config now fails at startup with a clear message.

### Cycle 2 build evidence
| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Build | pass | `dotnet build Fokus.API.csproj` | 0 errors, 2 warnings (pre-existing NuGet advisory, unrelated to this feature) |

---

## Pre-commitment Predictions
- Expected: rate limiter lease/retry interaction bug → **Confirmed HIGH**
- Expected: custom field deserialization gap → **Confirmed HIGH** (Codex also flagged independently)
- Expected: Sprint required-init bypass → **Not found** — `Name` and `BoardName` are set in MapSprint
- Expected: redundant `DeveloperRepository.SaveChangesAsync` → **Confirmed LOW** (shared DbContext means ticketRepo.SaveChanges already persists developer state)
- Expected: SyncBacklog silent exception swallowing → **Confirmed MEDIUM**
- Expected: JiraOptions validation absence → **Confirmed MEDIUM**

## Findings

### [HIGH] Custom fields never deserialize — StoryPoints, EpicKey, EpicName always null
**File:** `src/Services/Fokus/Fokus.API/Infrastructure/Jira/Dtos/JiraIssue.cs:14-16`
**Issue:** `JiraIssueFields.StoryPoints`, `EpicKey`, and `EpicName` are plain C# property names. The Jira REST API returns these as `customfield_10016` (story points), `customfield_10014` (epic name), and `customfield_10008` (epic link). Without `[JsonPropertyName("customfield_XXXXX")]` attributes, `System.Text.Json` with `PropertyNameCaseInsensitive = true` will never match them. Meanwhile `JiraClient` explicitly requests all three custom fields in the `fields=` query string (lines 80, 87, 94). The entire backlog epic-expansion path in `SyncBacklogEndpoint` depends on `EpicKey` being populated — `TicketRepository.GetAllKeysWithEpicAsync` will always return an empty list, so no epic tickets are ever discovered. Story points will be `null` on every ticket.
**Fix:** Add `[JsonPropertyName]` attributes:
```csharp
[JsonPropertyName("customfield_10016")]
public decimal? StoryPoints { get; set; }

[JsonPropertyName("customfield_10008")]
public string? EpicKey { get; set; }

[JsonPropertyName("customfield_10014")]
public string? EpicName { get; set; }
```
Also add `using System.Text.Json.Serialization;` to the file.

### [HIGH] Rate limiter lease acquired once but covers up to 4 HTTP requests
**File:** `src/Services/Fokus/Fokus.API/Infrastructure/Jira/JiraClient.cs:121-133`
**Issue:** The plan specifies "Acquire a permit before each request." The implementation acquires a single lease before the retry loop (`line 121`), then issues up to 4 HTTP requests under that one permit (initial + 3 retries on 429). Under concurrent load this can send bursts well above 10 req/s — the rate limiter protects only the first attempt. Additionally, `lease.IsAcquired` is never checked: if the limiter's queue of 100 is full, `AcquireAsync` returns a failed lease silently and the request proceeds with no permit at all.
**Fix:** Move the lease acquisition inside the retry loop so each HTTP attempt acquires its own token, and check `lease.IsAcquired` before sending:
```csharp
for (var attempt = 0; attempt <= 3; attempt++)
{
    using var lease = await _rateLimiter.AcquireAsync(1, ct);
    if (!lease.IsAcquired)
        throw new JiraApiException(HttpStatusCode.TooManyRequests, "Rate limit queue full");
    var response = await _http.GetAsync(path, ct);
    // ... retry logic
}
```

### [MEDIUM] SyncBacklog swallows all exceptions silently — failures invisible to callers
**File:** `src/Services/Fokus/Fokus.API/Features/Sync/SyncBacklog/SyncBacklogEndpoint.cs:92-96, 127-130`
**Issue:** Both the per-sprint and per-epic loops use bare `catch { }` with no logging and no reporting in the response. A total failure of all sprints or all epics still returns `200 OK` with zeroed counts. The plan says "JiraApiException → 502" for the initial fetch, but says nothing about swallowing per-item failures. SyncSprints handles this correctly by populating a `Failures` list. SyncBacklog has no equivalent.
**Fix:** At minimum inject `ILogger<SyncBacklogEndpoint>` and log the exception in each catch block. Ideally add failure counts to `SyncBacklogResponse` mirroring the SyncSprints pattern.

### [MEDIUM] JiraOptions has no startup validation — misconfiguration fails silently at runtime
**File:** `src/Services/Fokus/Fokus.API/Infrastructure/Jira/JiraOptions.cs:3-8` and `DependencyInjection.cs:20-24`
**Issue:** All three properties default to `string.Empty`. A missing `Jira:InstanceUrl` in config causes `new Uri("")` to throw `UriFormatException` at the first request, not at startup. Missing credentials produce a base64-encoded empty string that Jira will reject with 401. There is no `IValidateOptions<JiraOptions>` or `ValidateOnStart()` call.
**Fix:** Add data annotations or `IValidateOptions<JiraOptions>`:
```csharp
services.AddOptions<JiraOptions>()
    .Bind(configuration.GetSection("Jira"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```
And mark properties `[Required]` in `JiraOptions`.

### [LOW] DeveloperRepository.SaveChangesAsync called unconditionally — redundant when no developer upserted
**File:** `src/Services/Fokus/Fokus.API/Features/Sync/SyncSprints/SyncSprintsEndpoint.cs:108` and `SyncBacklogEndpoint.cs:75, 123`
**Issue:** `developerRepository.SaveChangesAsync` is called after every ticket regardless of whether a developer was upserted. Both repositories share the same scoped `FokusDbContext`, so the preceding `ticketRepository.SaveChangesAsync` already flushes any pending developer changes. The extra calls are no-ops but add unnecessary database round-trips per ticket.
**Fix:** Guard with a flag: only call `developerRepository.SaveChangesAsync` when `developer is not null`, or remove the separate call entirely since the shared context means `ticketRepository.SaveChangesAsync` covers it.

### [LOW] Jira error message captured but never forwarded in 502 responses
**File:** `src/Services/Fokus/Fokus.API/Features/Sync/GetBoards/GetBoardsEndpoint.cs:37-39`, `GetJiraSprintsEndpoint.cs:40-42`, `SyncSprintsEndpoint.cs:46-49`, `SyncBacklogEndpoint.cs:47-49`
**Issue:** `JiraClient` correctly parses Jira's error body into `JiraApiException.JiraMessage` (lines 138-150). All endpoints discard it and return an empty response body on 502. Callers cannot distinguish a misconfigured board from a network error.
**Fix:** Include `ex.JiraMessage` in the 502 response body, e.g. `await SendAsync(new { error = ex.JiraMessage ?? "Jira API error" }, 502, ct)`.

## Positive Observations
- Rate limiter instantiation is correct — `TokenBucketRateLimiter` with 10 tokens/sec, `AutoReplenishment = true`, `QueueLimit = 100` matches the plan exactly.
- `JiraChangeItem.ToString` renamed to `ToStringValue` with `[JsonPropertyName("toString")]` is the right call — prevents hiding `object.ToString()` while keeping deserialization correct.
- `UpsertMembershipsAsync` delete-then-insert is clean and idempotent. Same pattern for `ReplaceTransitionsAsync`. Both correct.
- Commitment derivation in `JiraMapper.MapMembership` is logically sound: changelog ordered ascending, first entry where sprint appears in `toString` sets `addedAt`, fallback to `sprint.StartDate` if no changelog entry found, `WasCommitted = addedAt <= sprint.StartDate`.
- `forcedNotCommitted` parameter for future sprints is a pragmatic and correct deviation from the plan.
- `GetBoardBacklogIssuesAsync` exists in `JiraClient` even though the plan listed it — not referenced by any endpoint, but present. Not a violation.
- DI wiring is correct — `IConfiguration` added to `AddFokusServices`, `JiraOptions` bound from `"Jira"` section, `JiraClient` registered as typed HttpClient with base address from options.
- `SprintState.Future` addition to the domain enum is clean and justified.
- Build passes with 0 errors.

## Gaps
- No tests exist for any of the 10 testing scenarios listed in the plan. The plan's "Testing Strategy" is entirely uncovered. This is noted as a gap, not a blocking finding per the review protocol (testing was listed as a strategy, not a deliverable step).
- `GetBoardBacklogIssuesAsync` is implemented in `JiraClient` but never called by any endpoint. The plan does not require its use, but it represents dead code.
- Sprint changelog field matching uses string comparison (`"Sprint"` or `"sprint"`) — Jira's actual field name for sprint changes in changelog items is `"Sprint"` (capital S). The dual check covers both, but the `FromString` field contains sprint IDs as integers in comma-separated form; parsing them as strings and comparing with `sprintIdStr` is fragile if Jira includes sprint names rather than IDs in that field depending on API version.

## Open Questions
- None elevated from self-audit. All CRITICAL/HIGH findings have HIGH confidence backed by direct code evidence.

## Cross-Validation

### Agreed (both Sonnet and Codex flagged)
- Custom fields never deserialize (HIGH) — both identified independently with same root cause
- Rate limiter single lease covers retry loop (HIGH) — both flagged, Codex also noted `IsAcquired` not checked
- SyncBacklog silent exception swallowing (MEDIUM)
- JiraOptions no startup validation (MEDIUM)
- Redundant `DeveloperRepository.SaveChangesAsync` (LOW)

### Sonnet only
- Fragile sprint ID parsing in changelog (LOW) — Codex did not flag

### Codex only
- Jira error message not forwarded in 502 (LOW) — Sonnet upgraded to LOW after Codex surfaced it; agreed finding

### Disagreements
- Sprint required-init bypass: Sonnet predicted as a possible finding; Codex investigated and found no violation. Confirmed no issue — `Name` and `BoardName` are both set in `JiraMapper.MapSprint`.

## Evidence
| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Build | pass | `dotnet build Fokus.API.csproj` | 0 errors, 2 warnings (known NuGet vuln unrelated to this feature) |
| Custom field attrs | fail | grep `JsonPropertyName` in JiraIssue.cs | No attributes on StoryPoints/EpicKey/EpicName |
| Rate limiter scope | fail | Read JiraClient.cs:119-155 | Single lease wraps retry loop of up to 4 requests |
| Sprint init | pass | Read JiraMapper.cs:19-29 + Sprint.cs | Name and BoardName both set at construction |
| Codex cross-validation | complete | `/codex:rescue` | 6 findings reported, 5 agreed, 1 Sonnet-only, 1 Codex-only |
