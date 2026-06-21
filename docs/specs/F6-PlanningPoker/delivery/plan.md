# Planning Poker

**Feature Spec:** `docs/specs/F6-PlanningPoker/definition/spec.md`

## Context

Planning Poker is the suite's third sprint ritual, alongside Fokus (analytics) and Reflekt (retro). A Scrum Master runs a real-time estimation ceremony; the team votes with a Fibonacci scale; agreed story points are written back to Jira through Fokus. Two modes: **Sprint-bound** (items pulled from a Fokus sprint, estimates written to Jira) and **Free** (manual items, no write-back).

**Services impacted:**
- **New `Poker` module** (`src/Services/Poker/` — API + Domain + Persistence, `poker.db`). Same shape as Reflekt; scaffolded from it.
- **Fokus** — net-new cross-module surface: a Poker-scoped sprint-catalog query (including `Future` state), a live `GetSprintTickets` pass-through, and a `SetStoryPoints` write-back. The write-back is the highest-risk item (net-new Jira issue-write path).
- **Jira module** — net-new `IJiraClient.SetStoryPointsAsync` + `IJiraApi` `[Put]` to `/rest/api/3/issue/{key}`.
- **Host** — module wiring (DI, MassTransit n/a, FastEndpoints assembly, migration, static files + SPA fallback, gRPC client registration).
- **client/** — new `apps/poker/` Vue SPA + `/poker/:sessionId` routing.

Reflekt is the proven template: a fully-built sibling module with a SignalR hub (`RetroHub` at `/hubs/retro`), a guest-token auth scheme chained onto Identity's auth builder, dual-path join reading the `person_id` cookie claim, and `/retro/*` SPA routing. Poker clones each pattern; **no architectural novelty** (spec §12 of the product draft).

## Scope

**In scope:** the Poker module (4 aggregates/entities, `poker.db`), its REST endpoints, the `PlanningHub` real-time estimation loop, a Poker-local guest-token scheme, the three Fokus gRPC surfaces, the net-new Jira story-points write, the Vue planning room, and all Host wiring.

**Out of scope (spec "Out of Scope"):** observer role, configurable scales, re-vote rounds, arbitrary final value, per-item comments, accuracy analytics, ticket detail panel, session history, backlog browsing, per-item timer, cross-tool integration event to Reflekt, cross-session guest matching. Also out: extracting a **shared** guest-auth building block — Rule 23 ratifies a conscious per-module duplication for v1 (see Open Questions, resolved).

## Skill Mapping

| Step | Skill | Disposition | TDD | Feature-Specific Inputs | Gap? |
|------|-------|-------------|-----|------------------------|------|
| 1 | `create-module` | Follow | no | Poker module CLAUDE.md (archetype: Domain — Domain + Persistence + API class library), `poker.db`, port-convention entry | |
| 2 | `create-aggregate`, `domain-patterns` | Follow | yes | `PlanningSession` aggregate + `SessionParticipant`/`EstimationItem`/`Vote`; mode/status state machines; Fibonacci value rule; one-vote-per-participant rule | |
| 3 | `persistence-patterns` | Follow | no | `PokerDbContext` → `poker.db`, 4 entity configs, initial migration, design-time factory; Identity-cross-module ids are plain `int` columns (no FK) | |
| 4 | `create-grpc-contract` | Build | yes | NET-NEW Jira issue-write path first (`IJiraClient.SetStoryPointsAsync` + `IJiraApi` `[Put] /rest/api/3/issue/{key}`, `customfield_10016`), then the Poker-facing Fokus gRPC: Poker-scoped sprint query (incl. `Future`), `GetSprintTickets` live pass-through, `SetStoryPoints` | |
| 5 | `create-feature` | Follow | yes | Session CRUD endpoints (create, get, join, sprints, tickets, add-item, close, summary) per the API Surface table; SM-authorization gate keyed on session creator's Person id | |
| 6 | `authorization-patterns` | Follow | yes | Poker-local guest-token scheme (distinct scheme name, NOT Reflekt's), open-link join (Rule 20, no waiting room), SM gate = session creator's `person_id` | |
| 7 | (none) | — | yes | `PlanningHub` + estimation-loop events (select/vote/reveal/decide/skip/close); auto-reveal rule; write-back-on-decide; broadcast on each transition | Log to lessons.md |
| 8 | `create-domain-event-handler` | Follow | yes | Domain-event → SignalR broadcast handlers (mirror Reflekt `Broadcast*Handler`): `ItemSelected`, `VotesRevealed`, `EstimateDecided`, `WriteBackFailed`, `ItemSkipped`, `SessionClosed` | |
| 9 | `service-registration` | Follow | no | `AddPokerModule`/`MapPokerEndpoints` in Host; FastEndpoints assembly entry; `Migrate<PokerDbContext>`; gRPC client registration (`ISprintCatalogService` Poker variant); static files + SPA fallback at `/poker` | |
| 10 | `create-vue-feature`, `vue-patterns`, `pinia-patterns` | Follow | yes | `client/apps/poker/` SPA: planning room (3 zones), card fan, reveal, SM controls, summary; `/poker/:sessionId` route; SignalR client; guest-token localStorage | |
| 11 | (none) | — | no | KB entry for the Jira story-points write-back + the live-pass-through freshness contract | Log to lessons.md |

**Anti-pattern check:** no "adapt" wording; no skill dismissed as simple; Follow steps carry only feature-specific inputs; every mapped step names its disposition keyword. Step 4 is **Build** (the Jira issue-write infrastructure does not exist locally — build it, then follow `create-grpc-contract`). Steps 7 and 11 are **None** (no skill covers a bespoke real-time estimation loop or a KB write).

## Domain Model Changes

New module `Poker.Domain`. One aggregate root, one child entity, two more entities (spec "Entities"):

- **`PlanningSession : AggregateRoot<Guid>`** — `Mode` (Sprint-bound | Free, immutable), `SprintId int?` (Sprint-bound only), `SprintName string?` (snapshot), `Title string?` (required Free), `CreatedByPersonId int` (the SM — an Identity Person id, never a guest), `Status` (Active | Closed), `CreatedAt`, `ClosedAt DateTime?`. Owns `SessionParticipant` and `EstimationItem` children. Raises domain events for the real-time loop (item selected, votes revealed, estimate decided, write-back failed, item skipped, session closed).
- **`SessionParticipant : Entity<Guid>`** — `SessionId`, `PersonId int?` (null for guests, ADR-022), `DisplayName`, `IsVoter` (always true v1), `JoinedAt`. Guest = session-scoped, no Person link.
- **`EstimationItem : Entity<Guid>`** — `SessionId`, `Label`, `TicketKey string?` (Sprint-bound only — drives write-back), `Status` (Voting | Revealed | Decided | Skipped), `FinalEstimate decimal?`, `Order int`, `DecidedAt DateTime?`. Owns `Vote` children. **Rule 19:** one item per ticket per session; refresh is idempotent (re-adding a removed ticket reuses the existing item + votes).
- **`Vote : Entity<Guid>`** — `ItemId`, `ParticipantId`, `Value string` (Fibonacci incl. "?"), `SubmittedAt`. **Rule 21:** at most one vote per participant per item; changing a vote replaces it (no duplicate rows).

**Behavior (not anemic — `domain-patterns` state machines):**
- `PlanningSession.CreateSprintBound(...)` / `CreateFree(...)` factories enforce Rule 1 (creator is SM) and the mode invariants (Sprint-bound requires a sprint; Free requires a title).
- `EstimationItem` status machine: Voting → Revealed → Decided | Skipped. `Decide(value)` enforces Rule 6 ("?" never final), Rule 8 (value must be one actually cast), and — Sprint-bound — only transitions to Decided after the caller confirms write-back (Rule 10). `Skip()` is a manual SM action (Rule 9).
- Vote replacement (Rule 21) lives on `EstimationItem.CastVote(participantId, value)` (upsert, not append).
- Auto-reveal trigger (Rule 7) is computed against currently-connected voters — see Step 7 (hub owns connection state; domain owns "all present voters have a vote").

## Data Model Changes

New `poker.db` (SQLite, file-backed under `data/`, connection key TBD per port/db convention). Four tables: `PlanningSessions`, `SessionParticipants`, `EstimationItems`, `Votes`. Identity Person ids (`CreatedByPersonId`, `SessionParticipant.PersonId`) and the Fokus `SprintId` are **plain `int` columns with no FK** (cross-module, owned elsewhere — mirror Reflekt's `Team.OwnerId` / `Participant.PersonId` after RA2). Guest `PersonId` is nullable. One filtered-unique index on `EstimationItems {SessionId, TicketKey}` where `TicketKey IS NOT NULL` (Rule 19 idempotency). One unique index on `Votes {ItemId, ParticipantId}` (Rule 21). Initial migration only.

## Implementation Steps

### Step 1 — Scaffold the Poker module

Create the Poker module skeleton: `src/Services/Poker/` with `Poker.Domain`, `Poker.Persistence`, `Poker.API` (class library with `<FrameworkReference Include="Microsoft.AspNetCore.App" />`, like every non-Host API). Write `src/Services/Poker/CLAUDE.md` first (archetype **Domain**: Domain + Persistence + API), mirroring Reflekt's CLAUDE.md structure — endpoint framework FastEndpoints, database `poker.db`, DDD/Vertical-Slice/CQRS, no cross-module project references, gRPC + integration events for cross-module data. Add the module's projects to `SprintRituals.slnx`. Assign a port-convention DB/connection key and add the row to `docs/conventions/project-rules.md` (the `{AA}XY` table).

**Follow `create-module`** (archetype Domain). Feature-specific: module name `Poker`, `poker.db`, no Application layer (ADR-004, like Reflekt/Fokus).

**Accept:** `src/Services/Poker/{Poker.Domain,Poker.Persistence,Poker.API}` exist with correct csproj references (API → Domain + Persistence; Persistence → Domain); `SprintRituals.slnx` includes them; `dotnet build SprintRituals.slnx` green (filter `error CS`); `Poker/CLAUDE.md` exists.

`Satisfies:` foundational (no single AC; enables all).

### Step 2 — Poker domain model

Create the four domain types and their behavior under `Poker.Domain/` (`PlanningSession/`, `SessionParticipant`, `EstimationItem`, `Vote` — follow Reflekt's aggregate/entity folder layout). Implement the factories, state machines, and rules listed in **Domain Model Changes**. The Fibonacci scale is a fixed set `{ "1","2","3","5","8","13","21","?" }` — a domain constant (Rule 5); "?" is abstain-only (Rule 6).

**Follow `create-aggregate`** for `PlanningSession` (aggregate root, behavior split, domain events, the child entities) and **follow `domain-patterns`** for the `EstimationItem`/`Vote` state machines and value rules.

Feature-specific inputs: property lists in **Domain Model Changes**; the binding rules — Rule 1/6/7/8/9/10/19/21/22 — are domain invariants enforced here (auto-reveal's *connection* half is the hub's, Step 7; the domain exposes "would this vote complete the set of present voters?" as a pure check the hub calls).

**Accept (TDD):** unit tests prove — Sprint-bound create without a sprint throws; Free create without a title throws; `Decide("?")` throws (Rule 6); `Decide` with a value not cast throws (Rule 8); a second `CastVote` by the same participant replaces, leaving one vote (Rule 21); `Skip` works regardless of votes (Rule 9); a re-added removed ticket reuses its item (Rule 19).

`Satisfies:` AC "?"-not-final, AC value-must-be-cast, AC vote-twice-single-vote, AC skip-all-question, AC refresh-preserves-items.

### Step 3 — Poker persistence

Create `Poker.Persistence`: `PokerDbContext` (→ `poker.db`), four `EntityConfigurations`, repositories per Reflekt's `Repository.cs` base pattern, `DesignTimeDbContextFactory`, and the initial migration. Cross-module ids (`CreatedByPersonId`, `SessionParticipant.PersonId`, `SprintId`) are plain `int`/`int?` columns with **no** `HasOne`/FK (owned cross-module — mirror Reflekt post-RA2). Add the two indexes from **Data Model Changes**.

**Follow `persistence-patterns`** (DbContext setup, entity configurations, repository pattern, migration). Persistence is the migration startup project per `project-rules.md`.

Feature-specific: `poker.db` connection key (from Step 1); filtered-unique `{SessionId, TicketKey}` index (Rule 19); unique `{ItemId, ParticipantId}` index (Rule 21); enum-to-string or int conversions consistent with the rest of the repo.

**Accept:** `dotnet ef migrations add InitialPokerSchema` produces a migration matching the model snapshot; `poker.db` creates the four tables + both indexes; build green. (Wiring of `Migrate<PokerDbContext>` is Step 9.)

`Satisfies:` foundational (persists all session state).

### Step 4 — Jira story-points write path + the three Fokus gRPC surfaces

This is the **highest-risk** step (spec Architecture Decisions). It has two parts — build the missing Jira write infrastructure first, then expose the gRPC contracts.

**4a — NET-NEW Jira issue-write (Build).** Fokus's only Jira write today is `UpdateSprintAsync` → `POST /rest/agile/1.0/sprint/{id}` (Agile API, sprint metadata) — a *different* API. Add:
- `IJiraApi` (`src/Modules/Jira/Jira.RestApi/IJiraApi.cs`): `[Put("/rest/api/3/issue/{issueIdOrKey}")] Task<IApiResponse<...>> SetIssueFieldsAsync(string issueIdOrKey, [Body] ... request, CancellationToken ct);` — Jira's issue-edit endpoint returns **204 No Content** on success (no body); type the response accordingly.
- A request DTO in `Jira.Contracts` shaped `{ "fields": { "customfield_10016": <decimal> } }` (reuse the resolved field id `customfield_10016`, spec-confirmed — same id Fokus already *reads*).
- `IJiraClient.SetStoryPointsAsync(string issueKey, decimal points, CancellationToken ct)` + its `RestApiJiraClient` impl, routing through the existing `RequestAsync` retry/error-mapping helper (so 401 → `UnauthorizedException`, other → `BadGatewayException`, consistent with the rest of the client). Note the 204 path: `RequestAsync<T>` currently returns `response.Content!` — a 204 has no content; either add a no-content overload or type it `IApiResponse` without a content body. **Developer's call** which, but it must not NPE on an empty 204 body.
- **Two `IJiraClient` impls exist:** `RestApiJiraClient` and `TeamManagedJiraClient : RestApiJiraClient` (overrides issue-*fetch* for team-managed boards; DI selects by `Jira:ProjectKey` at `Jira.RestApi/DependencyInjection.cs:47-51`). Put `SetStoryPointsAsync` on the **`IJiraClient` interface** and implement it once on the **base** `RestApiJiraClient`; `TeamManagedJiraClient` inherits it. The issue-edit `PUT` is board-type-agnostic — **confirm no override is needed** on `TeamManagedJiraClient` (its overrides exist because issue *fetch* differs, not issue *write*).

**4b — Fokus gRPC surfaces (then follow `create-grpc-contract`).** Three Poker-facing calls. Do **not** reuse `SprintRepository.GetAnalyticsSprintsAsync` (it deliberately filters to `Closed || Active` at `SprintRepository.cs:19` and is shared by ~20 analytics endpoints — widening it leaks `Future` everywhere, per spec):
- **Poker sprint catalog** — a new Poker-scoped sprint query (e.g. `SprintRepository.GetPokerSprintsAsync` returning Active + Future + recent Closed) exposed over gRPC. Decide whether to add a method to `ISprintCatalogService` or a new `IPokerCatalogService` contract — **binding constraint:** the underlying query must include `Future` and must NOT alter the existing `GetAvailableSprintsAsync` behavior. `SprintState.Future` already exists in both `Fokus.Domain` and `Jira.Contracts`, and future sprints already sync via `POST /api/sync/backlog` — this is a query, not new sync.
- **`GetSprintTickets(sprintId)` — live Jira pass-through (RATIFIED Option A, binding contract shape).** The server impl calls the existing `IJiraClient.GetSprintIssuesAsync(sprintId)` live on every call (no caching on this path) and projects to a tickets DTO (key, summary, current story points from `customfield_10016`, issue type). The Poker UI's refresh button (Rule 12) re-invokes it. **Do not** add a cache here — staleness during a live ceremony is the rejected Option B.
- **`SetStoryPoints(ticketKey, points)`** — server impl calls 4a's `IJiraClient.SetStoryPointsAsync`. Error contract: on Jira failure the call surfaces an error the Poker side maps to `WriteBackFailed` (Rule 10) — the item is NOT marked Decided. Define the gRPC response so failure is distinguishable from success (an explicit success flag/reason, not just an exception across the boundary — mirror `InvitationGrpcService`'s pre-checked no-exception pattern noted in Fokus CLAUDE.md).

**Build the Jira write infra, then follow `create-grpc-contract`** for all three (server impl in `Fokus.API/Grpc/`, contract + DataContracts in `Blocks.Contracts/Grpc/`, client registration is Step 9).

Feature-specific: field id `customfield_10016`; live-pass-through (no cache); Poker-scoped sprint filter (Active + Future + recent Closed); 204-on-success Jira edit; failure-distinguishable `SetStoryPoints` response.

**Accept (TDD where behavioral):** a `SetStoryPoints` for a known ticket issues `PUT /rest/api/3/issue/{key}` with `{fields:{customfield_10016}}` and treats 204 as success (test against a stubbed `IJiraApi`); a Jira 4xx/5xx maps to a failure response, not an unhandled exception; `GetSprintTickets` returns the live sprint contents (stubbed `GetSprintIssuesAsync`) with story points populated; the Poker sprint query returns Future sprints while `GetAvailableSprintsAsync` still returns only Active|Closed (grep/assert the analytics filter at `SprintRepository.cs:19` is unchanged).

`Satisfies:` AC sprint-bound-decide-writes-to-Jira, AC write-back-failure-not-decided, AC sprint-selector-future-default (data half), AC sprint-bound-item-list-matches-Jira.

### Step 5 — Poker REST endpoints

Build the REST surface from the spec API Surface table (routes/status codes are **binding** — they are the wire contract; the estimation loop is NOT REST, it's Step 7). Each is a FastEndpoints slice in `Poker.API/Features/` (two-file pattern: `{Feature}Endpoint.cs` + `{Feature}Command.cs`/`Query.cs`, per Reflekt/ADR-008):

| Method | Route | Auth | Notes |
|--------|-------|------|-------|
| POST | `/api/poker/sessions` | Authenticated SM | 201 + room URL; 400 sprint-bound w/o sprint; 400 free w/o title; 401 unauth (Rule 1) |
| GET | `/api/poker/sessions/{id}` | Anyone w/ link | session state (mode, status, items, participants); 404 |
| POST | `/api/poker/sessions/{id}/participants` | Anyone (Google or guest) | join; 200; 404; 409 closed |
| GET | `/api/poker/sprints` | Authenticated SM | calls Step 4 Poker sprint query (gRPC client); default = first future sprint (Rule 16); 401 |
| GET | `/api/poker/sessions/{id}/tickets` | SM | calls Step 4 `GetSprintTickets` live; 403 non-SM; 409 free-mode (Rule 12) |
| POST | `/api/poker/sessions/{id}/items` | SM | free-mode manual item; 201; 403 non-SM; 409 sprint-bound (Rule 3 guests can't) |
| POST | `/api/poker/sessions/{id}/close` | SM | 200 + summary; 403 non-SM; 409 already closed (Rule 14) |
| GET | `/api/poker/sessions/{id}/summary` | Anyone w/ link | summary stats; 200; 404 (Rule 17/18) |

The join endpoint mirrors Reflekt's `JoinRetroEndpoint`: `[AllowAnonymous]`, read the optional `person_id` claim (suite member) → set `PersonId`; guest supplies a display name → `PersonId` null. **Open-link join, NO waiting room** (Rule 20 — intentionally distinct from Reflekt's SM-gated access, ADR-021). Authenticated re-entry is idempotent (return the existing participant — mirror `JoinRetroEndpoint:27-35`).

**Follow `create-feature`** for each slice. Feature-specific: the routes/status codes above; the SM gate (Step 6); the gRPC-client-backed `sprints`/`tickets` reads.

**Accept (TDD):** the AC list maps directly — 401 on unauth create; 201 + room URL on valid sprint-bound create; 400s for missing sprint/title; guest `POST .../items` → 403; non-SM close → 403; `GET .../summary` on a closed session by a non-participant → 200; closed-session mutation → 409.

`Satisfies:` AC unauth-cannot-create, AC sprint-bound-create-201, AC free-mode-requires-title, AC guest-cannot-add-item, AC non-SM-close-403, AC closed-summary-readable, AC create-validation-400s, AC sprint-selector-default.

### Step 6 — Poker-local guest-token auth + SM authorization

Poker defines its **own** session-scoped guest-token scheme — a conscious v1 duplication of Reflekt's, per **Rule 23 / ADR-016** (do NOT share or extract a building block; revisit at a third consumer). Clone Reflekt's `GuestTokenAuthenticationHandler` + `GuestTokenDefaults` + `AuthenticationConfiguration` into `Poker.API/Auth/`, keyed to Poker's `SessionParticipant` repository and Poker claims (`participantId`, `sessionId`, `displayName`).

**Binding constraint:** the scheme name must be **distinct** from Reflekt's. Reflekt registers `GuestTokenDefaults.AuthenticationScheme` onto the shared Host auth builder; two schemes with the same name collide. Use a Poker-specific scheme name (e.g. `"PokerGuestToken"`). Chain it onto the existing auth builder (`AddAuthentication().AddScheme<...>(...)`) exactly as Reflekt does — Identity already registered the cookie/Google default.

SM authorization (Rule 2/22): SM-only actions are gated against the **session creator's Person id** (`PlanningSession.CreatedByPersonId` vs. the caller's `person_id` claim), not a stored participant role — same pattern as Reflekt's owner check. The SM is also an ordinary voting participant (Rule 22).

The global `ActiveUserPreProcessor` (Identity) 401s any authenticated principal whose `NameIdentifier` isn't an active Identity User — a Poker guest must not be 401'd. Scope it off Poker the same way RA2 scoped it off Reflekt (assembly-of-declaring-endpoint — see `adhoc-ReflektIdentityUnification` Step 4). **This touches Identity** (small, Reflekt-established pattern): `ActiveUserGateScope.IsExcludedModule` (`Identity.API/Auth/ActiveUserGateScope.cs:20-21`) is currently a single const equality — `endpointAssemblyName == ReflektApiAssemblyName` ("Reflekt.API"), NOT a list. Adding Poker means **promoting it to a set** that also matches `"Poker.API"` (add a `PokerApiAssemblyName` const + a set/`is`-pattern check) and **updating `Identity.API.Tests/Auth/ActiveUserGateScopeTests.cs`** to cover the Poker case. No project reference (match on assembly simple-name string — Identity stays decoupled).

**Follow `authorization-patterns`** (role gate + resource gate). Feature-specific: distinct scheme name; SM gate on `CreatedByPersonId`; open-link join (no approval gate); the `ActiveUserPreProcessor` Poker-scoping.

**Accept (TDD):** a guest with a valid Poker guest token authenticates and can vote but cannot perform an SM action (403); a non-creator authenticated user is rejected from SM actions (403); the session creator passes; a Poker guest request is not 401'd by the global `ActiveUserPreProcessor`; the Poker scheme name does not collide with Reflekt's at Host boot.

`Satisfies:` AC guest-cannot-create, AC guest-cannot-add-item, AC non-SM-close-403, AC join-as-google-or-guest.

### Step 7 — PlanningHub + the estimation loop (real-time)

**Disposition None** — no skill covers a bespoke real-time estimation loop; the developer owns internal decomposition. Mirror Reflekt's `RetroHub` for structure (connection-group-per-session, server-push broadcasts, SM-gated client→server methods, connection lifecycle tracking) but the loop logic is Poker's own.

Build `Poker.API/Hubs/PlanningHub.cs` at route `/hubs/poker` (Reflekt holds `/hubs/retro`, Fokus `/hubs/sprint` — pick a free route). Connection group keyed `poker:{sessionId}`. Implement the client→server methods and server→client events from the spec Real-time Events table:

- **Client→server (SM-gated where noted):** `SelectItem` (SM), `SubmitVote` (any voter), `ChangeVote` (any voter, pre-reveal only), `RevealVotes` (SM), `DecideEstimate` (SM), `SkipItem` (SM), `CloseSession` (SM). SM gating reuses Step 6's creator-Person-id check.
- **Server→client:** `ParticipantJoined`, `ParticipantLeft`, `ItemSelected`, `VoteReceived` (participant id only, **no value** — Rule 7 hidden until reveal), `VotesRevealed` (full list), `EstimateDecided`, `WriteBackFailed`, `ItemSkipped`, `SessionClosed`.

**Binding behavioral rules:**
- **Auto-reveal (Rule 7):** fires when every *currently-connected* voter has a vote on the active item — including when a disconnect leaves all remaining connected voters already voted. The hub owns connection state (who's connected); it calls the domain's pure "all present voters voted?" check (Step 2) after each vote and each disconnect. The SM may force-reveal early at any time. A voter rejoining before reveal does NOT discard already-cast votes.
- **Decide → write-back (Rule 10, Sprint-bound):** on `DecideEstimate`, the item is marked Decided **only after** the gRPC `SetStoryPoints` write-back is confirmed. On failure, broadcast `WriteBackFailed` (item stays not-Decided, SM may retry); no vote data lost. Free mode: mark Decided locally, no gRPC call (Rule 11).
- **Vote privacy (Rule 7):** `VoteReceived` carries the participant id only; values appear only in `VotesRevealed`.
- **Session liveness (Rule 13):** disconnects — including the SM's — never delete or deactivate the session; it stays Active while ≥1 participant remains. Closing is an explicit SM action only.

Broadcasting on each domain transition is wired via domain-event handlers (Step 8) where the transition originates in the domain; purely hub-side signals (`VoteReceived`, `ParticipantLeft`) broadcast directly from the hub.

**Accept (TDD):** during voting, a non-voting observer of the broadcast sees only "voted", not values; auto-reveal fires when the last connected voter votes AND when a disconnect leaves all remaining voters voted; force-reveal works; deciding in Sprint-bound mode does not mark Decided until write-back confirms, and a write-back failure emits `WriteBackFailed` with the item still not Decided; SM disconnect leaves the session Active and actionable by another participant.

`Satisfies:` AC voting-hides-values, AC auto-reveal-and-force, AC sprint-bound-decide-writes (loop half), AC write-back-failure-not-decided, AC SM-disconnect-session-stays-active, AC reload-rejoin-current-state, AC auto-reveal-on-disconnect.

### Step 8 — Domain-event → SignalR broadcast handlers

For each domain event raised in Step 2 (`ItemSelected`, `VotesRevealed`, `EstimateDecided`, `ItemSkipped`, `SessionClosed`, and the `WriteBackFailed` signal), add an `IEventHandler<T>` in the owning feature slice that broadcasts the matching SignalR event to the `poker:{sessionId}` group — mirror Reflekt's `Broadcast*Handler` classes (named by effect). The FastEndpoints `IEvent` bus + the `DispatchDomainEventsInterceptor` in Persistence publish these after save (same infra as Fokus/Reflekt).

**Follow `create-domain-event-handler`** (vertical-slice placement, named by effect). Feature-specific: the event→broadcast pairs above; group key `poker:{sessionId}`; inject `IHubContext<PlanningHub>`.

**Accept (TDD):** deciding an estimate raises the domain event and the handler pushes `EstimateDecided` to the group; skipping pushes `ItemSkipped`; closing pushes `SessionClosed` with summary stats.

`Satisfies:` AC sprint-bound-decide-writes (broadcast half), AC close-makes-readonly-summary.

### Step 9 — Host wiring

Wire Poker into `src/Host/Program.cs` and the persistence interceptor, mirroring the Reflekt/Fokus registrations:
- `builder.Services.AddPokerModule(builder.Configuration);` (Add section).
- Add `typeof(Poker.API.DependencyInjection).Assembly` to the FastEndpoints `o.Assemblies` list.
- `app.Migrate<PokerDbContext>();` (Init section, after the other migrates).
- `app.MapPokerEndpoints();` (Use section) — maps endpoints + `MapHub<PlanningHub>("/hubs/poker")`.
- gRPC **client** registrations: Poker dials Fokus for the sprint catalog, `GetSprintTickets`, and `SetStoryPoints` (`AddCodeFirstGrpcClient<...>(grpcOptions, "...")` — mirror Reflekt's `ISprintAnalyticsService` client registration and Fokus's `IPersonService` client). The corresponding gRPC **server** impls (Step 4) are mapped via `MapFokusEndpoints` → `MapGrpcService<...>` (mirror `InvitationGrpcService`).
- Static files + SPA fallback for `/poker` (mirror the `/retro` `UseStaticFiles` + `MapFallbackToFile` blocks).
- MassTransit: Poker has **no** integration-event consumers in v1 (cross-module data is gRPC pull) — do not add an `AddPokerMessaging` unless a consumer exists. (Confirm none needed; the spec lists no consumed events.)

**Follow `service-registration`** (Host composition for the modular monolith — where each dependency type goes). Feature-specific: the five wiring points above; gRPC config keys.

**Accept:** Host boots; `poker.db` migrates with four tables; `/api/poker/*`, `/hubs/poker`, and `/poker/*` static all route; Poker's gRPC client resolves Fokus; `dotnet build SprintRituals.slnx` green. The two guest schemes (Reflekt + Poker) coexist without a name collision (Step 6).

`Satisfies:` foundational (composition — makes all endpoints reachable).

### Step 10 — Vue planning room

Create `client/apps/poker/` (new SPA, mirror `client/apps/retro/` structure — own router, store, components, built into `wwwroot/poker`). Build:
- Router `/poker/:sessionId` → planning room view; the create flow (mode select, sprint selector defaulting to first future sprint per Rule 16, free-mode title) for the SM.
- **Three-zone planning room** (spec §UI): left ticket/item list (status colors: estimated/current/pending/skipped), center estimation area (card fan 1/2/3/5/8/13/21/?, selectable, dim others, change before reveal, simultaneous flip on reveal, SM winner-selection from cast values with confirm), right participant panel (voted/waiting → value after reveal).
- Session summary view on close (items estimated, skipped, total story points, per-item estimates).
- SignalR client to `/hubs/poker`; optimistic UI reconciled by broadcasts (suite convention).
- Guest-token in `localStorage` keyed per session (mirror `apps/retro/src/stores/guest.ts`); join-as-Google-or-guest form.

**Follow `create-vue-feature`** (full slice: types, API module, store, components, view, route), **`vue-patterns`** (Composition API), **`pinia-patterns`** (store boundaries). Feature-specific: the three-zone layout; card-fan interaction; the real-time event handlers matching Step 7's server→client events; guest-token persistence; `/poker/:sessionId` route.

**Accept (TDD where component logic warrants):** opening the room URL lets a second user join as Google or guest and appear in the participant list in real time; during voting others see only "voted"; reveal shows values with names; the SM can pick a final only from cast values and not "?"; a closed session renders the read-only summary. (Manual/e2e for the full real-time flow; unit where store/computed logic is testable.)

`Satisfies:` AC join-as-google-or-guest-realtime, AC voting-hides-values, AC SM-picks-from-cast-only, AC close-readonly-summary (UI half), AC reload-rejoin.

### Step 11 — KB entry

Add a `docs/kb/` entry documenting (a) the Jira **story-points write-back** path (`PUT /rest/api/3/issue/{key}`, `customfield_10016`, 204-on-success) — the first issue-field write in the suite, and (b) the **live-pass-through freshness contract** for `GetSprintTickets` (no caching on this path; refresh re-pulls live — the rejected cached alternative and why). These are non-obvious cross-module facts a future contributor needs.

**Disposition None** (a KB write — no pattern skill). Follow the KB entry section order (`kb-entry-schema`) for headings.

**Accept:** the two KB facts exist under `docs/kb/`, cross-linked from the spec.

`Satisfies:` ADR — documentation obligation for the net-new Jira write + freshness decision.

## Cross-Service Changes

- **Jira module:** net-new `IJiraClient.SetStoryPointsAsync` + `IJiraApi` `[Put] /rest/api/3/issue/{key}` + request DTO (Step 4a). First issue-field write — all prior Jira writes were sprint-metadata (Agile API).
- **Fokus gRPC server:** new Poker-scoped sprint query, `GetSprintTickets` (live pass-through), `SetStoryPoints` — contracts in `Blocks.Contracts/Grpc/`, server impls in `Fokus.API/Grpc/` (Step 4b), mapped in `MapFokusEndpoints` (Step 9).
- **Poker gRPC client:** registered in Host (Step 9), dials the three Fokus surfaces.
- **No integration events** in v1 (the cross-tool event to Reflekt is explicitly out of scope). Poker pulls cross-module data synchronously via gRPC; it publishes/consumes nothing on the MassTransit bus.
- **Identity:** one **small** change (Step 6) — `ActiveUserGateScope` (`Identity.API/Auth/ActiveUserGateScope.cs`) promotes its single-module const equality to also exclude `"Poker.API"` from the global active-user gate, plus its test. This is the Reflekt-established RA2 pattern extended by one module, not a domain/auth-model change. Otherwise no Identity changes: Poker reads the existing `person_id` cookie claim (added in RA2) and never mints a Person, so it needs no `IPersonService` client. (The product draft's "Identity: no changes" referred to the domain model; the gate-scope edit is infrastructure.)

## Migration Notes

One migration: `InitialPokerSchema` (Step 3), Persistence-as-startup per `project-rules.md`. No data seed required for v1 (sessions are user-created; no demo seed needed — but a dev seed mirroring Reflekt's `DbInitializer` is optional, developer's call). No schema changes to other modules' databases (Fokus's `Future` state and story-points field already exist).

## Testing Strategy

- **Domain unit tests (Step 2):** the rule set — Fibonacci/"?" abstain, vote replacement, decide constraints, skip, refresh idempotency.
- **Endpoint tests (Step 5/6):** auth gates (401 unauth-create, 403 non-SM, guest-cannot-add), mode validation 400s, closed-session 409, summary readability.
- **gRPC/Jira tests (Step 4):** `SetStoryPoints` issues the correct PUT and treats 204 as success; failure maps to a distinguishable response, not an exception; `GetSprintTickets` returns live contents; the Poker sprint query includes Future while the analytics filter is untouched.
- **Real-time (Step 7):** auto-reveal triggers (last-vote and disconnect cases), vote privacy, decide-gated-on-write-back, session liveness across SM disconnect.
- **Frontend (Step 10):** join/real-time appearance, value hiding, winner constraint, read-only summary — e2e/manual for the full loop, unit for store/computed logic.

## KB Impact

Two new `docs/kb/` entries (Step 11): the Jira story-points write-back path, and the `GetSprintTickets` live-pass-through freshness contract. Both are net-new cross-module facts not derivable from existing KB.

## Open Questions

**All resolved at plan time — none blocking.**

- **Person for a not-yet-known Google participant (spec's one open item) — RESOLVED: mirror Reflekt, read-only, never mint.** Reflekt's `JoinRetroEndpoint` reads the `person_id` cookie claim and links authenticated participants to their existing Person; it never mints a Person (minting is deferred to `adhoc-RetroWaitingRoom`, ADR-016 timing note). Poker has no waiting room (Rule 20) and the same invitation-gated Identity login, so a non-suite Google user cannot authenticate into a session in v1 — they join as a **guest** (`PersonId` null). Authenticated participation = existing suite members who already carry `person_id`. Poker therefore needs **no `IPersonService` gRPC client** and mints nothing. (Same stance and limitation RA2 documented for Reflekt; record it in implementation.md as expected, not a defect.)
- **Shared guest-auth building block — RESOLVED: duplicate for v1 (Rule 23 / ADR-016).** Poker clones Reflekt's guest-token scheme into its own module with a distinct scheme name; no shared block extracted with only two consumers. Revisit at a third consumer.
- **gRPC contract home for the Poker sprint catalog (`ISprintCatalogService` method vs. new `IPokerCatalogService`)** — developer's call at Step 4b, bounded by the binding constraint that `GetAvailableSprintsAsync`'s Active|Closed behavior must not change. Either is acceptable; a separate contract is cleaner if the tickets/write methods cluster with it.
