# Codex Cross-Check — F6-PlanningPoker

**Verdict: CONDITIONAL GO**

The four hot spots the team-lead scoped are mostly sound. Two real bugs found (one contract mismatch, one cookie-auth gap in authenticated hub participants); the Jira write path is clean; `GetAnalyticsSprintsAsync` is untouched. No issues block a demo, but the contract mismatch will break guest join in the browser before the first session is created.

---

## Findings

### CRITICAL — Join endpoint response shape vs. client type mismatch

**File:** `src/Services/Poker/Poker.API/Features/Sessions/JoinSession/JoinSessionEndpoint.cs` (returns `ParticipantResponse`) vs. `client/apps/poker/src/types/index.ts:89-95` (`JoinSessionResponse`) and `client/apps/poker/src/api/sessions.ts:43-53`

The join endpoint (`POST /api/poker/sessions/{id}/participants`) returns `ParticipantResponse`:
```
{ id, displayName, isVoter, personId, guestToken }
```

The client types and the `sessionsApi.join()` call cast the response as `JoinSessionResponse`:
```ts
{ participantId: string; guestToken: string; displayName: string; sessionId: string }
```

Two mismatches:
1. Server field is `id`; client reads `participantId` — guest token storage and hub join will silently use `undefined`.
2. Client expects `sessionId` in the response; server never emits it — the caller has to carry it in from the call site, but `JoinSessionResponse` implies it comes from the server.

The `guestStore` presumably reads `participantId` and `guestToken` from the join response; if `participantId` is `undefined`, the guest claim in the hub auth handler cannot be resolved and the hub will throw "Participant identity could not be resolved" on every guest `SubmitVote`.

**Fix:** Either rename the server response field from `Id` to `ParticipantId` in `SessionContracts.cs:9`, or update the client type to match the actual shape (`id` not `participantId`; drop `sessionId` from the server-response type). The `sessionId` is already known at the call site — remove it from `JoinSessionResponse` or fill it client-side.

---

### HIGH — Cookie-authenticated hub participants cannot vote

**File:** `src/Services/Poker/Poker.API/Hubs/PlanningHub.cs:219-228` (`GetParticipantIdFromClaims`)

The hub resolves `participantId` only from a `"participantId"` claim in the guest token. Cookie-authenticated participants (suite members who joined via the REST endpoint) do not get a `participantId` claim injected — they have a `person_id` cookie claim, not a Poker participant id. The comment at line 225 acknowledges this gap ("Cookie-authed participants don't have a Poker participantId claim") but returns `null` without a fallback.

Consequence: `RequireParticipantId()` (called in `SubmitVote`) throws `HubException("Participant identity could not be resolved.")` for every Google-authenticated voter. The SM (also a voter in v1, Rule 22) and all suite-member participants are blocked from casting votes.

`JoinSession` at line 39 also conditionally tracks the connection only if `participantId.HasValue` — so authenticated participants are not tracked in `ConnectionState` and cannot appear in the "connected voters" auto-reveal check.

**Fix:** After REST join, the server creates a `SessionParticipant` record with a `PersonId`. In `JoinSession` hub method (or `SubmitVote`), resolve the participant id by looking up `SessionParticipant` by `PersonId` from the `person_id` claim. One option: include a `"participantId"` claim in the cookie-auth response for the join endpoint (not ideal — bloats the cookie), or do a repo lookup in the hub. The RetroHub pattern likely does the same lookup; check `RetroHub` to align.

---

### MEDIUM — `SetStoryPointsAsync` void overload is called correctly, but not via the no-content overload in `RestApiJiraClient`

**File:** `src/Modules/Jira/Jira.RestApi/RestApiJiraClient.cs:185-192`

The `SetStoryPointsAsync` method calls:
```csharp
await RequestAsync(() => Api.UpdateIssueFieldsAsync(issueKey, command, ct), ct);
```

`IJiraApi.UpdateIssueFieldsAsync` returns `Task<IApiResponse>` (non-generic). The call resolves to the void `RequestAsync(Func<Task<IApiResponse>>, ct)` overload at line 198, NOT the generic `RequestAsync<T>` that would NPE on `response.Content!`. This is correct. The NPE guard is solid.

**No action required** — documenting as verified clean.

---

### VERIFIED CLEAN — `customfield_10016` field name

**File:** `src/Modules/Jira/Jira.Contracts/UpdateIssueFieldsCommand.cs:22`

`[JsonPropertyName("customfield_10016")]` is correctly set on `StoryPoints`. The Step 4a deviation note in implementation.md flagged `story_points` as the initial serialization name, but Step 7 corrected it to `customfield_10016`. The file in the repo is correct.

---

### VERIFIED CLEAN — `TeamManagedJiraClient` does not override write-back

**File:** `src/Modules/Jira/Jira.RestApi/TeamManagedJiraClient.cs`

`TeamManagedJiraClient` overrides `GetSprintIssuesAsync`, `GetEpicIssuesAsync`, and `GetBoardBacklogIssuesAsync` only — all issue-*fetch* paths. `SetStoryPointsAsync` is not overridden and is correctly inherited from `RestApiJiraClient`. The issue-edit `PUT` is board-type-agnostic. Clean.

---

### VERIFIED CLEAN — `GetAnalyticsSprintsAsync` filter is unchanged

**File:** `src/Services/Fokus/Fokus.Persistence/Repositories/SprintRepository.cs:17-21`

```csharp
.Where(s => s.State == SprintState.Closed || s.State == SprintState.Active)
```

This is exactly the pre-F6 filter. `GetPokerSprintsAsync` is a separate method (lines 28–42) that operates independently. The analytics path is untouched.

---

### VERIFIED CLEAN — `GetPokerSprintsAsync` is a sibling, not a modifier

**File:** `src/Services/Fokus/Fokus.Persistence/Repositories/SprintRepository.cs:28-42`

Adds Active+Future in one query, N recent Closed in a second query, then concatenates with collection-expression spread. No modification to any shared query. Clean.

---

### VERIFIED CLEAN — `GetSprintTickets` live pass-through, no cache

**File:** `src/Services/Fokus/Fokus.API/Grpc/PokerFokusGrpcService.cs:34-48`

Calls `jiraClient.GetSprintIssuesAsync` directly on each call. No caching layer. Comment confirms "Live pass-through — no local caching". Clean.

---

### VERIFIED CLEAN — `DecideEstimate` sequencing (Rule 10)

**File:** `src/Services/Poker/Poker.API/Hubs/PlanningHub.cs:153-182`

The sequence is:
1. Check `SprintBound && TicketKey != null`
2. Call `TrySetStoryPointsAsync` — gRPC to Fokus → Jira `PUT`
3. On failure: broadcast `WriteBackFailed`, return without calling `session.DecideEstimate`
4. On success: call `session.DecideEstimate` + save + domain event fires handler which broadcasts `EstimateDecided`

The write-back-first, decide-only-on-success contract is correctly implemented. Free mode goes direct to `DecideEstimate` (no gRPC). Clean.

---

### VERIFIED CLEAN — Vote values do not leak before reveal

**File:** `src/Services/Poker/Poker.API/Hubs/PlanningHub.cs:112-114`

`VoteReceived` broadcast carries only `{ participantId, itemId }` — no `value`. Values appear only in `VotesRevealed` via `BroadcastVotesRevealedHandler`. Client `applyVoteReceived` stores a placeholder `'?'` until reveal, then replaces with actual values on `VotesRevealed`. Rule 7 vote privacy is maintained end-to-end.

---

### LOW — `GetPokerSprintsResponse` casing vs. client `PokerSprintsResponse`

**File:** `src/Services/Poker/Poker.API/Features/Sprints/GetPokerSprints/GetPokerSprintsEndpoint.cs:33-35` vs. `client/apps/poker/src/types/index.ts:68-71`

Server record fields are `Sprints` (PascalCase) and `DefaultSprintId`. ASP.NET Core's default JSON serializer uses `camelCase` for responses, so wire shape will be `{ sprints: [...], defaultSprintId: ... }`. Client type `PokerSprintsResponse` correctly uses `sprints` and `defaultSprintId` (camelCase). This matches — but it relies on the Host's global `JsonSerializerOptions` having `PropertyNamingPolicy = CamelCase` (standard for FastEndpoints). Worth confirming at host wiring; a mismatch silently produces empty arrays. Low risk given it's the project's established default.

---

## Summary Table

| # | Severity | Area | Issue |
|---|----------|------|-------|
| 1 | **CRITICAL** | Client/Server contract | Join endpoint returns `id`; client reads `participantId` + expects `sessionId` — guest hub auth will silently fail |
| 2 | **HIGH** | Hub participant tracking | Cookie-authed voters get `null` participantId in hub; cannot vote or trigger auto-reveal |
| 3 | VERIFIED CLEAN | Jira write-back | `customfield_10016` correct; 204 NPE avoided via void overload |
| 4 | VERIFIED CLEAN | TeamManagedJiraClient | No write-back override; inherits correctly |
| 5 | VERIFIED CLEAN | SprintRepository | `GetAnalyticsSprintsAsync` filter unchanged; `GetPokerSprintsAsync` is a proper sibling |
| 6 | VERIFIED CLEAN | Fokus gRPC live pass-through | No cache; direct Jira call |
| 7 | VERIFIED CLEAN | DecideEstimate sequencing | Write-first, mark-Decided-only-on-success, WriteBackFailed on failure |
| 8 | VERIFIED CLEAN | Vote privacy | `VoteReceived` carries no value; values only in `VotesRevealed` |
| 9 | **LOW** | JSON casing | `GetPokerSprintsResponse` PascalCase fields — safe if host sets camelCase policy (standard) |
