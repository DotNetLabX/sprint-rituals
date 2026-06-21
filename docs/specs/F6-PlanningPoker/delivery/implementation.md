# Implementation — F6-PlanningPoker (All 11 Steps + Fix Cycle 1)

## Status

All 11 steps complete. Fix Cycles 1 + 3 (8 findings total) applied and verified.
.NET build green. 177 tests passing (26 Poker.Domain + rest unchanged). Client builds green (retro + poker).

## Fix Cycle 1 — Reviewer + Codex Findings (7 fixes)

### Fix 1 [CRITICAL] — Join response contract mismatch

**Problem:** Server returned `ParticipantResponse` from join endpoint, whose `Id` field serialized as `id`, but client `JoinSessionResponse` typed `participantId`. Server also never emitted `sessionId`, which client type expected.

**Fix:** Created dedicated `JoinSessionResponse(Guid ParticipantId, string GuestToken, string DisplayName, Guid SessionId)` record in `SessionContracts.cs`. Added `ToJoinResponse(Guid sessionId)` extension. Changed `JoinSessionEndpoint` to return `JoinSessionResponse` (not `ParticipantResponse`). Server now serializes as `{ participantId, guestToken, displayName, sessionId }` matching the client interface exactly.

**Files:** `SessionContracts.cs`, `JoinSessionEndpoint.cs`

### Fix 2 [HIGH] — Cookie-authed participants cannot vote

**Problem:** `PlanningHub.GetParticipantIdFromClaims()` only read the `"participantId"` claim (set by `PokerGuestTokenAuthenticationHandler`). Cookie-authed users (SM, invited participants) carry no such claim — they hold `person_id`. `RequireParticipantId()` threw `HubException` for them; `JoinSession` also didn't add them to `ConnectionState` so auto-reveal didn't count them.

**Fix:** Added `ResolveParticipantIdAsync(Guid sessionId)` — fast path reads guest claim, slow path looks up `SessionParticipant` by `PersonId` via `SessionParticipantRepository.GetByPersonIdInSessionAsync`. Replaced synchronous `RequireParticipantId()` with `RequireParticipantIdAsync(sessionId)`. Updated `JoinSession`, `SubmitVote`, and `OnDisconnectedAsync` to use the async variant. Cookie-authed users are now correctly tracked in `ConnectionState`.

**Files:** `PlanningHub.cs`, `SessionParticipantRepository.cs` (already had the method — no change needed)

### Fix 3 [HIGH] — GuestToken leaked via anonymous GET session endpoint

**Problem:** `ParticipantResponse` included `GuestToken`. The `GET /api/poker/sessions/{id}` endpoint is `[AllowAnonymous]` and returns `ParticipantResponse[]`, exposing all participant tokens to anyone with the room URL.

**Fix:** Removed `GuestToken` from `ParticipantResponse`. It is now only returned in `JoinSessionResponse` (to the participant who just joined). `ToResponse()` extension no longer includes GuestToken; `ToJoinResponse()` includes it. Client `ParticipantResponse` type updated to remove `guestToken` field.

**Files:** `SessionContracts.cs`, `client/apps/poker/src/types/index.ts`, `client/apps/poker/src/stores/planningSession.ts`

### Fix 4 [MEDIUM] — isScrumMaster always true

**Problem:** `session?.createdByPersonId !== undefined` is always true for a non-nullable `int`. Any signed-in user saw SM controls.

**Fix:** Added `id: number` field to `AuthUser` in `client/packages/shared/src/auth.ts` (the `/api/auth/me` response already returns `id` as the Person id). Changed poker `auth.ts` to use the shared `AuthUser` type directly. Updated `isScrumMaster` computed to compare `sessionStore.session?.createdByPersonId === authStore.user.id`.

**Files:** `client/packages/shared/src/auth.ts`, `client/apps/poker/src/stores/auth.ts`, `client/apps/poker/src/views/PlanningRoomView.vue`

### Fix 5 [MEDIUM] — Two DB round-trips in GetPokerSprintsAsync

**Problem:** `GetPokerSprintsAsync` made two separate `ToListAsync` calls — one for Active/Future, one for recent Closed.

**Fix:** Collapsed to single `ToListAsync` on `Active | Future | Closed` combined, then filtered and sliced in memory. Result set is always small (<20 rows), so materializing all first is fine. Avoids SQL UNION which EF SQLite doesn't translate.

**Files:** `src/Services/Fokus/Fokus.Persistence/Repositories/SprintRepository.cs`

### Fix 6 [LOW] — JoinAuthenticated generates unused GuestToken

**Problem:** `SessionParticipant.JoinAuthenticated` factory generated a `Guid.NewGuid()` guest token for authenticated users, creating a credential that could theoretically be used for impersonation if the token were ever exposed.

**Fix:** `GuestToken = string.Empty` for authenticated participants. Comment explains the NOT NULL constraint and that the value is never returned or used for auth. Defense-in-depth alongside Fix 3.

**Files:** `src/Services/Poker/Poker.Domain/Planning/Participants/SessionParticipant.cs`

### Fix 7 [LOW] — ChangeVote missing pre-reveal guard comment

**Fix:** Added XML doc comment to `ChangeVote` explaining that the domain's `CastVote` upsert handles the replacement and that post-reveal changes are rejected by the domain guard on `EstimationItem.CastVote`.

**Files:** `src/Services/Poker/Poker.API/Hubs/PlanningHub.cs`

## Gate results — Fix Cycle 1

- .NET build: passed (0 errors)
- .NET tests: 177 passed, 0 failed
- Client build (retro + poker): passed (vue-tsc + vite both green)

## Fix Cycle 3 — Reviewer Finding (1 fix)

### Fix 7 [HIGH] — Client join URL did not match server route

**Problem:** `client/apps/poker/src/api/sessions.ts` POSTed to `/api/poker/sessions/${sessionId}/join` but the server route (introduced in Step 2 and unchanged through Fix Cycle 1) is `/api/poker/sessions/{id}/participants`. Every join call returned 404 — nobody could join a session. Fix Cycle 1's response-type refactor was TS-only; the URL string was not caught by the type-checker.

**Fix:** Changed `sessions.ts:44` to POST to `/api/poker/sessions/${sessionId}/participants`.

**Files:** `client/apps/poker/src/api/sessions.ts`

**Also:** Corrected status header test count from 172 → 177 (transcription typo from Fix Cycle 1).

## Gate results — Fix Cycle 3

- .NET build: passed (0 errors)
- .NET tests: 177 passed, 0 failed
- Client build (dashboard + retro + poker): passed (vue-tsc + vite all 3 green)

## Steps

### Step 1 — Scaffold Poker Module

**Files created:**
- `src/Services/Poker/Poker.Domain/Poker.Domain.csproj` — mirrors Reflekt.Domain pattern; references Blocks.Domain only
- `src/Services/Poker/Poker.Domain/GlobalUsings.cs` — Blocks.Domain.Entities + Blocks.Domain.Events
- `src/Services/Poker/Poker.Persistence/Poker.Persistence.csproj` — FrameworkReference, EF SQLite, references Domain + Blocks.EntityFrameworkCore + Blocks.FastEndpoints
- `src/Services/Poker/Poker.Persistence/GlobalUsings.cs` — includes all domain sub-namespaces (Planning, Items, Participants, Votes)
- `src/Services/Poker/Poker.API/Poker.API.csproj` — class library, FrameworkReference, FastEndpoints, Mapster, MassTransit, references Domain + Persistence + Blocks.AspNetCore + Blocks.FastEndpoints + Blocks.Contracts
- `src/Services/Poker/Poker.Domain.Tests/Poker.Domain.Tests.csproj` — xunit.v3 + AwesomeAssertions; matches Reflekt.Domain.Tests pattern
- `src/Services/Poker/Poker.API/Auth/PokerGuestTokenDefaults.cs` — scheme constant `"PokerGuestToken"` (distinct from Reflekt's `"GuestToken"`, per Rule 23 / ADR-016)
- `src/Services/Poker/Poker.API/Auth/PokerGuestTokenAuthenticationHandler.cs` — reads X-Guest-Token / ?access_token / Bearer; delegates to `SessionParticipantRepository.GetByGuestTokenAsync`; returns NoResult (not Fail) when no token present; returns Fail on invalid token
- `src/Services/Poker/Poker.API/Auth/AuthenticationConfiguration.cs` — chains PokerGuestToken scheme
- `src/Services/Poker/Poker.API/Auth/Policies.cs` — PokerScrumMaster, PokerVoter, PokerScrumMasterOrVoter constants
- `src/Services/Poker/Poker.API/DependencyInjection.cs` — AddPokerModule (persistence + Mapster + auth + gRPC client) + MapPokerEndpoints stub
- `src/Services/Poker/Poker.Persistence/DependencyInjection.cs` — AddPokerPersistence (DbContext, DispatchDomainEventsInterceptor, 3 repositories)
- `src/Services/Poker/Poker.Persistence/PokerDbContext.cs` — ApplicationDbContext<PokerDbContext>, 4 DbSets
- `src/Services/Poker/Poker.Persistence/DesignTimeDbContextFactory.cs` — UseSqlite("Data Source=poker.db") for ef tools
- `SprintRituals.slnx` — updated with /Services/Poker/ folder + 4 projects

**Decisions:**
- Guid PKs throughout (session/participant/item/vote all client-generated) — matches plan spec
- Scheme name `"PokerGuestToken"` — consciously different from Reflekt's to avoid auth collision

---

### Step 2 — Domain Model with TDD Coverage

**Files created:**
- `src/Services/Poker/Poker.Domain/Planning/FibonacciScale.cs` — fixed set {1,2,3,5,8,13,21,?}; IsNumeric excludes "?"
- `src/Services/Poker/Poker.Domain/Planning/Votes/Vote.cs` — Entity<Guid>; Cast factory + ChangeValue (upsert); `SubmittedAt` is `private set` (not `init`) so ChangeValue can mutate it
- `src/Services/Poker/Poker.Domain/Planning/Participants/SessionParticipant.cs` — JoinAuthenticated + JoinAsGuest factories; GuestToken auto-generated Guid.NewGuid().ToString("N"); PersonId nullable (null for guests per ADR-022)
- `src/Services/Poker/Poker.Domain/Planning/Items/EstimationItem.cs` (state) + `Items/Behaviors/EstimationItem.cs` (behavior): CastVote upsert (Rule 21), Reveal, Decide (Rules 6/8), Skip (Rule 9), AllPresentVotersHaveVoted
- `src/Services/Poker/Poker.Domain/Planning/PlanningSession.cs` (state) + `Planning/Behaviors/PlanningSession.cs` (behavior): CreateSprintBound + CreateFree factories; AddAuthenticatedParticipant idempotent by PersonId; AddSprintBoundItem idempotent by TicketKey (Rule 19); all session-level operations raise domain events
- Domain events: `ItemSelectedEvent`, `VotesRevealedEvent`+`VoteResult`, `EstimateDecidedEvent`, `ItemSkippedEvent`, `SessionClosedEvent`

**Build fix:** `Vote.SubmittedAt` was `required init` — `ChangeValue` tries to assign it post-construction. Fixed to `DateTime SubmittedAt { get; private set; }` initialized in the Cast factory.

**Tests:** 26 tests in `Poker.Domain.Tests/Planning/`:
- `PlanningSessionCreateTests.cs` — Sprint-bound + Free mode creation
- `EstimationItemDecideTests.cs` — Rules 6, 8, all-"?" guard, pre-reveal guard
- `EstimationItemVoteTests.cs` — Rule 21 upsert, AllPresentVotersHaveVoted, post-reveal guard
- `EstimationItemSkipTests.cs` — Rule 9 (Voting + Revealed + all-"?"), cannot skip Decided
- `PlanningSessionItemTests.cs` — Rule 19 idempotent ticket refresh, SessionClosedEvent stats

---

### Step 3 — Persistence

**Files created:**
- `src/Services/Poker/Poker.Persistence/EntityConfigurations/PlanningSessionConfiguration.cs` — Guid PK (HasGeneratedId=false); Mode/Status as string; navigations via public IReadOnlyList<T> properties with `UsePropertyAccessMode(Field)`
- `src/Services/Poker/Poker.Persistence/EntityConfigurations/SessionParticipantConfiguration.cs` — GuestToken unique index; filtered unique index `{SessionId, PersonId} WHERE PersonId IS NOT NULL` (Rule 15 idempotency)
- `src/Services/Poker/Poker.Persistence/EntityConfigurations/EstimationItemConfiguration.cs` — filtered unique index `{SessionId, TicketKey} WHERE TicketKey IS NOT NULL` (Rule 19); Votes navigation with Field access mode
- `src/Services/Poker/Poker.Persistence/EntityConfigurations/VoteConfiguration.cs` — unique index `{ItemId, ParticipantId}` (Rule 21); FK to EstimationItems via EstimationItemConfiguration (not redeclared here)
- `src/Services/Poker/Poker.Persistence/Repositories/PlanningSessionRepository.cs` — includes Participants and Items.Votes
- `src/Services/Poker/Poker.Persistence/Repositories/SessionParticipantRepository.cs` — GetByGuestTokenAsync, GetByPersonIdInSessionAsync
- `src/Services/Poker/Poker.Persistence/Repositories/EstimationItemRepository.cs` — GetByIdWithVotesAsync
- `src/Services/Poker/Poker.Persistence/Migrations/20260621100029_InitialPokerSchema.cs` — all 4 tables + 7 indexes including both non-standard filtered-unique indexes

**Notes:**
- VoteConfiguration does NOT declare the HasMany/HasOne relationship for ItemId — that is owned by EstimationItemConfiguration to avoid EF model collision
- `UsePropertyAccessMode(Field)` set via lambda on public `IReadOnlyList<T>` property — string-form `.Navigation("_field")` does not work with EF when the navigation property has a different name than the backing field

---

### Step 4a — Jira Write-Back

**Files created/modified:**
- `src/Modules/Jira/Jira.Contracts/UpdateIssueFieldsCommand.cs` — nested `IssueFields` class with `StoryPoints decimal?`; serialized with `JsonIgnoreCondition.WhenWritingNull`
- `src/Modules/Jira/Jira.Contracts/IJiraClient.cs` — added `SetStoryPointsAsync(issueKey, storyPoints, ct)`
- `src/Modules/Jira/Jira.RestApi/IJiraApi.cs` — added `[Put("/rest/api/3/issue/{issueIdOrKey}")] UpdateIssueFieldsAsync`; returns `IApiResponse` (no-body form, not `IApiResponse<T>`)
- `src/Modules/Jira/Jira.RestApi/RestApiJiraClient.cs` — added `SetStoryPointsAsync` impl + void `RequestAsync(Func<Task<IApiResponse>>, ct)` overload

**204 NPE fix:** The existing `RequestAsync<T>` returns `response.Content!` — on a 204 No Content response this would NPE. The new void overload takes `Func<Task<IApiResponse>>` (non-generic Refit response) and simply returns on `IsSuccessStatusCode`. The PUT to `/rest/api/3/issue/` goes through the void overload exclusively.

**Deviation:** `UpdateIssueFieldsCommand.Fields.StoryPoints` uses field name `story_points` in the JSON body — classic Jira projects use `customfield_10016`. The caller (Step 7 / DecideEstimate endpoint) may need to map to the correct custom field key depending on project type. This is a known Jira field-name ambiguity; the DTO structure supports swapping the field name in Step 7.

---

### Step 4b — Fokus gRPC

**Files created/modified:**
- `src/BuildingBlocks/Blocks.Contracts/Grpc/IPokerFokusService.cs` — new contract with `[ServiceContract]`; 3 operations: GetPokerSprintsAsync, GetSprintTicketsAsync, SetStoryPointsAsync
- `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/PokerSprintsQuery.cs` — `RecentClosedCount` (default 3)
- `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/PokerSprintsResponse.cs` — list of `PokerSprintInfo` (distinct type from existing `SprintInfo` to avoid namespace collision)
- `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/PokerSprintInfo.cs` — SprintId, Name, Start/EndDate, State (lowercase string)
- `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/SprintTicketsQuery.cs` — SprintId
- `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/SprintTicketsResponse.cs` — list of `PokerTicket` (Key, Summary, StoryPoints decimal?)
- `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/SetStoryPointsRequest.cs` — IssueKey, StoryPoints
- `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/SetStoryPointsResponse.cs` — Success bool, ErrorMessage string?
- `src/Services/Fokus/Fokus.Persistence/Repositories/SprintRepository.cs` — added `GetPokerSprintsAsync(int recentClosedCount, ct)` as sibling (Active+Future ordered by StartDate, then N recent Closed ordered by EndDate desc); `GetAnalyticsSprintsAsync` untouched
- `src/Services/Fokus/Fokus.API/Grpc/PokerFokusGrpcService.cs` — implements IPokerFokusService; injects SprintRepository + IJiraClient
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — `MapFokusEndpoints` registers `PokerFokusGrpcService`
- `src/Services/Poker/Poker.API/DependencyInjection.cs` — `AddCodeFirstGrpcClient<IPokerFokusService>(grpcOptions, "PokerFokus")` with config key `Grpc:Services:PokerFokus:Url`

---

---

### Step 5 — REST Endpoints

**Files created:**
- `src/Services/Poker/Poker.API/GlobalUsings.cs` — FastEndpoints, MAUI builder, Sessions namespace; `Poker.API.Auth` NOT added globally (Policies() method conflict with FastEndpoints base class)
- `src/Services/Poker/Poker.API/Auth/ClaimsPrincipalExtensions.cs` — `GetPersonId()` and `GetPersonIdOrNull()` mirroring Reflekt pattern
- `src/Services/Poker/Poker.API/Features/Sessions/SessionContracts.cs` — shared DTOs: ParticipantResponse, VoteResponse, EstimationItemResponse, SessionResponse, SessionSummaryResponse + static SessionMappings
- `CreateSession/CreateSessionEndpoint.cs` + `CreateSessionCommand.cs` — `[Authorize(Policy = AuthPolicies.ScrumMaster)]`, POST /api/poker/sessions, 201 + room URL; uses `using AuthPolicies = Poker.API.Auth.Policies;` alias
- `GetSession/GetSessionEndpoint.cs` — AllowAnonymous, GET /api/poker/sessions/{id}
- `JoinSession/JoinSessionEndpoint.cs` — AllowAnonymous, idempotent by PersonId (Rule 20), 409 on closed session
- `CloseSession/CloseSessionEndpoint.cs` — SM gate via `session.CreatedByPersonId != personId`
- `GetSessionSummary/GetSessionSummaryEndpoint.cs` — AllowAnonymous, GET /api/poker/sessions/{id}/summary
- `Sprints/GetPokerSprints/GetPokerSprintsEndpoint.cs` — SM auth, defaultSprintId = first Future sprint
- `Tickets/GetSessionTickets/GetSessionTicketsEndpoint.cs` — SM gate + resource gate, 409 for Free mode
- `Items/AddSessionItem/AddSessionItemEndpoint.cs` — SM gate + resource gate, Free mode only

**Key decision:** `using AuthPolicies = Poker.API.Auth.Policies;` alias per endpoint file (not a global using) — FastEndpoints base class has a `Policies()` method; a global `using Poker.API.Auth;` makes `Policies.ScrumMaster` ambiguous in `[Authorize]` attributes.

---

### Step 6 — Identity Touch: ActiveUserGateScope + Poker Exclusion

**Files modified:**
- `src/Services/Identity/Identity.API/Auth/ActiveUserGateScope.cs` — promoted from single const equality (`== "Reflekt.API"`) to `IReadOnlySet<string> ExcludedModules = {"Reflekt.API", "Poker.API"}`; added `PokerApiAssemblyName` const; `IsExcludedModule` now calls `.Contains()`
- `src/Services/Identity/Identity.API.Tests/Auth/ActiveUserGateScopeTests.cs` — added `Should_ExcludeFromGate_When_EndpointDeclaredInPokerApiAssembly` test; all 5 Identity auth tests pass

---

### Step 7 — PlanningHub + Field Key Correction

**Field key correction (carry-over resolved):**
- `src/Modules/Jira/Jira.Contracts/UpdateIssueFieldsCommand.cs` — `[JsonPropertyName("customfield_10016")]` replacing `story_points`; v1 pins this id (same field Fokus reads via JiraIssueFields.StoryPoints)

**Files created:**
- `src/Services/Poker/Poker.API/Hubs/PlanningHub.cs` — `[Authorize(AuthenticationSchemes = "Cookies,PokerGuestToken")]`; ConcurrentDictionary connection tracking (sessionId → connectionId → participantId); JoinSession, SubmitVote, ChangeVote, SelectItem, RevealVotes, DecideEstimate, SkipItem, CloseSession, OnDisconnectedAsync
  - **DecideEstimate sequencing (Rule 10):** Sprint-bound calls `TrySetStoryPointsAsync` first; `session.DecideEstimate` only on success; broadcasts `WriteBackFailed` on gRPC failure without marking Decided; Free mode goes direct
  - **Auto-reveal on disconnect (Rule 7):** `TryAutoRevealOnDisconnectAsync` checks `AllPresentVotersHaveVoted` for remaining connected voters after each disconnect
  - **Vote privacy:** `VoteReceived` carries `participantId + itemId` only — no value until `VotesRevealed`
  - **SM gate:** loads session from repo, compares `session.CreatedByPersonId` vs cookie `person_id` claim

---

### Step 8 — Domain-Event → SignalR Broadcast Handlers

**Files created** (all in `Poker.API/Features/Sessions/EstimationEvents/`):
- `BroadcastItemSelectedHandler.cs` — `IEventHandler<ItemSelectedEvent>` → `SendAsync("ItemSelected", {sessionId, itemId, label})`
- `BroadcastVotesRevealedHandler.cs` — `IEventHandler<VotesRevealedEvent>` → `SendAsync("VotesRevealed", {votes with values})` — first point where vote values are visible
- `BroadcastEstimateDecidedHandler.cs` — `IEventHandler<EstimateDecidedEvent>` → `SendAsync("EstimateDecided", {finalEstimate})`
- `BroadcastItemSkippedHandler.cs` — `IEventHandler<ItemSkippedEvent>` → `SendAsync("ItemSkipped")`
- `BroadcastSessionClosedHandler.cs` — `IEventHandler<SessionClosedEvent>` → `SendAsync("SessionClosed", {stats})`

Pattern mirrors Reflekt's `Broadcast*Handler` — `IHubContext<PlanningHub>`, group key `poker:{sessionId}`.

---

### Step 9 — Host Wiring

**Files modified:**
- `src/Host/Host.csproj` — added `<ProjectReference>` to `Poker.API.csproj`
- `src/Host/Program.cs` — Add section: `builder.Services.AddPokerModule(...)` + Poker assembly in FastEndpoints `o.Assemblies`; Init section: `app.Migrate<PokerDbContext>()`; Use section: `app.MapPokerEndpoints()` + Poker static-file prefix + `/poker/{*path:nonfile}` SPA fallback
- `src/Host/appsettings.json` — added `Grpc:Services:PokerFokus:Url` + `ConnectionStrings:PokerDatabase`

**Note:** No MassTransit consumers for Poker v1 per plan.

---

## Skills Used

| Step | Skill | Channel |
|------|-------|---------|
| 1 | nexus-dotnet:create-module | Tool |
| 2 | nexus-dotnet:create-aggregate, nexus-dotnet:domain-patterns, nexus:tdd | Tool (loaded in session) |
| 3 | nexus-dotnet:persistence-patterns | Tool |
| 4a | Skill: None (plan inline) | N/A |
| 4b | Skill: None (plan inline) | N/A |
| 5 | nexus-dotnet:create-feature, nexus-dotnet:authorization-patterns | Tool (loaded in session) |
| 6 | Skill: None (plan inline) | N/A |
| 7 | nexus-dotnet:domain-patterns (hub pattern from Reflekt) | Read-channel |
| 8 | nexus-dotnet:create-domain-event-handler | Tool |
| 9 | nexus-dotnet:service-registration | Tool |
| 10 | nexus-dotnet:create-vue-feature, nexus-dotnet:vue-patterns, nexus-dotnet:pinia-patterns | Tool |
| 11 | nexus:kb-entry-schema | Tool |

## Carry-Over

None — all carry-over items from Run 1 resolved:
- customfield_10016 field key: corrected in Step 7
- ActiveUserGateScope Poker exclusion: done in Step 6
- Host wiring: done in Step 9
- appsettings.json PokerFokus URL + PokerDatabase connection string: done in Step 9

---

### Step 10 — Vue Planning Room SPA

**Files created** under `client/apps/poker/`:
- `package.json`, `tsconfig.json`, `tsconfig.app.json`, `tsconfig.node.json`, `vite.config.ts`, `index.html`, `env.d.ts` — SPA scaffold mirrors `client/apps/retro/` exactly; base `/poker/`, outDir `wwwroot/poker`
- `src/assets/main.css` — Tailwind + CSS design tokens (dark/light theme, same token set as retro; storage key `poker:theme`)
- `src/types/index.ts` — all API response types + SignalR broadcast payload shapes
- `src/api/sessions.ts` — `sessionsApi` (create/get/join/close/addItem/getSummary) + `sprintsApi` (getSprints/getTickets); direct fetch mirroring retro pattern; guest token via `X-Guest-Token` header
- `src/stores/guest.ts` — localStorage key `poker:session:{sessionId}:token` (conscious per-module duplication, Rule 23)
- `src/stores/auth.ts` — mirrors retro auth store; uses shared `fetchMe()`; redirects to `/poker/`
- `src/stores/planningSession.ts` — setup store; state: session/sprints/tickets/activeItemId/writeBackError; SignalR reconciliation methods: applyParticipantJoined/Left/ItemSelected/VoteReceived/VotesRevealed/EstimateDecided/ItemSkipped/SessionClosed/WriteBackFailed
- `src/composables/usePlanningHub.ts` — mirrors `useRetroHub`; builds SignalR connection to `/hubs/poker`; `accessTokenFactory` reads guestStore.token; exposes SM methods (selectItem/revealVotes/decideEstimate/skipItem/closeSession) and voter methods (submitVote/changeVote)
- `src/composables/useTheme.ts` — dark/light toggle, module-level ref, storage key `poker:theme`
- `src/composables/useToast.ts` — identical to retro; module-level toasts ref
- `src/components/ui/BaseButton.vue`, `LoadingSpinner.vue`, `ToastContainer.vue` — copied from retro (L4 primitives)
- `src/components/AppHeader.vue` — "Planning Poker" branding, theme toggle, SM sign-out
- `src/components/poker/PokerJoinForm.vue` — join gate; guest name input for non-authenticated users; "Sign in with Google" option
- `src/components/poker/FibonacciCardFan.vue` — card fan for voting; selected card highlighted + scaled
- `src/components/poker/VoteBoard.vue` — pre-reveal: checkmarks; post-reveal: actual values + display names
- `src/components/poker/ItemQueue.vue` — sidebar list; SM can select next Voting item; status badges
- `src/components/poker/SmControls.vue` — reveal/decide (value picker)/skip; WriteBackFailed inline error + close session
- `src/components/poker/SessionSummaryPanel.vue` — read-only: estimated/skipped/SP totals + per-item rows
- `src/views/PlanningRoomView.vue` — L0 orchestrator: join gate → 3-panel room (sidebar/main/SM controls) → summary; hub connect/disconnect on mount/unmount; `watch(writeBackError)` → toast
- `src/router/index.ts` — single route `/:id` → PlanningRoomView; `createWebHistory(BASE_URL)`
- `src/App.vue`, `src/main.ts` — standard Pinia + router app shell

**Root package.json updated**: `build` script adds `--workspace=apps/poker`; `dev:poker` script added.

**Key decisions:**
- Poker is a standalone SPA (like retro/dashboard) — separate `client/apps/poker/` app with its own router, store, and hub composable
- Guest token key distinct from retro (`poker:session:...` vs `reflekt:retro-session:...`) per Rule 23
- SM detection in the view is UI-only (hide/show controls); actual enforcement is server-side policy gate
- `isScrumMaster` check simplified for UX — server enforces the real gate

---

### Step 11 — KB Entries

**Files created:**
- `docs/kb/jira-story-points-write-back.md` — 6 rules covering: `customfield_10016` field name, 204 no-body pattern, write-first sequencing (Rule 10), WriteBackFailed on failure, Free-mode bypass, no-`?` guard; key files, edge cases (204 NPE guard, field-id variance caveat, SM-driven retry), relationships chain
- `docs/kb/poker-sprint-tickets-freshness.md` — 5 rules covering: live pass-through (no cache), SM gate, Sprint-bound-only (Rule 12 / 409 for Free), gRPC delegation chain, `customfield_10016` round-trip; key files, edge cases (no fallback, stale snapshot behaviour, Rule 19 idempotency, analytics split guard), relationships
- `docs/kb/index.md` — 2 rows added for both new entries (status: verified)

Plan: docs/specs/F6-PlanningPoker/delivery/plan.md
