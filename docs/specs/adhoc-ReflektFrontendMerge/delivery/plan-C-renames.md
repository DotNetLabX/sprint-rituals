# RA5-C — Backend Rename Completion (board → RetroSession)

**Parent:** `plan.md` · **Depends on:** RA5-A (Reflekt frontend in-repo at `client/apps/retro` — needed for the lockstep frontend caller updates) · **Type:** Refactoring (rename/type-move; no behavior change)

Complete the `Sprint → RetroSession` rename RA3 deferred. **Endgame (owner-decided 2026-06-19): COMPLETE, not partial** — eliminate every `sprint`-named identifier that actually means *the retro board*, across DB columns, C# properties, HTTP routes, and SignalR/claim wire strings. `sprint` survives **only** where it means a *Fokus* sprint (the 1..N sprints a RetroSession references). Rationale: since RA3 a RetroSession references multiple Fokus sprints, so "sprint" is now ambiguous; board-meaning `SprintId` is a semantic lie, and a partial rename perpetuates the exact ambiguity (already biting — `GetSprintReportEndpoint` reads `req.RetroSessionId` while siblings read `req.SprintId`). The in-code "stays sprintId-named (Q2 DO-NOT-TOUCH)" note was a deferral to *this* pass; frontend is now in-repo so all renames are lockstep with no external consumer.

## ⚠️ Homonym carve-out — DO NOT TOUCH (these mean a *Fokus* sprint, not the board)

| Keep `sprint`-named | Why | Evidence |
|---------------------|-----|----------|
| `FokusSprintSnapshot.*`, `FokusSprintRef` | Reflekt's projection of the referenced Fokus sprint(s) | `Reflekt.Domain/FokusSprints/` |
| gRPC `AnonymizedSprintSummary`, `ISprintAnalyticsService` | cross-module wire contract in `Blocks.Contracts` — renaming is a contract break, out of scope | RA4 |
| `Features/RetroSessions/GetSprintContext/*` | pulls the *Fokus* sprint metrics | RA4 |
| **All EF migrations** (`Migrations/*.cs`, `*.Designer.cs`, `ReflektDbContextModelSnapshot.cs`) | immutable history — the rename is expressed by a **new** migration, never by editing old ones | — |
| 24 non-`sprint` SignalR methods (`CardCreated`, `PhaseChanged`, `VoteCast`, …) | already retro-semantic | inventory |

> A blanket `SprintId` replace is **catastrophic**: 368 occurrences across 101 files, the majority in the carve-out above. Every step's acceptance is a grep **scoped to exclude** the carve-out.
> **Optional future clarity (NOT in scope — flagged):** `FokusSprintSnapshot.SprintId → FokusSprintId` would fully disambiguate, but it borders RA4's fresh code and the gRPC mapping; leave for a follow-up unless the reviewer wants it folded.

### Carve-out allowlist — the exhaustive set of files the strengthened Step-5 gate still permits (grep-verified 2026-06-20)

A literal `rg -i "sprint"` over `src/Services/Reflekt` (excl. Migrations) returns **781 occurrences** before the cleanup; the strengthened gate passes only when every remaining hit lives in one of these **14 carve-out files** (each verified to contain ONLY carve-out `sprint` — a *Fokus* sprint, or the gRPC/`SprintSynced` wire surface):

| Carve-out file | Why kept |
|----------------|----------|
| `Reflekt.Domain/FokusSprints/FokusSprintSnapshot.cs` | FokusSprint projection |
| `Reflekt.Domain/RetroSessions/FokusSprintRef.cs` | Fokus-sprint linkage entity |
| `Reflekt.Persistence/EntityConfigurations/FokusSprintRefConfiguration.cs` | FokusSprintRef EF config |
| `Reflekt.Persistence/EntityConfigurations/FokusSprintSnapshotConfiguration.cs` | FokusSprintSnapshot EF config (`SprintId` = natural Fokus key) |
| `Reflekt.Persistence/Repositories/FokusSprintSnapshotRepository.cs` | FokusSprintSnapshot repo |
| `Reflekt.API/Features/FokusSprints/Consumers/RefreshFokusSprintSnapshotOnSprintSyncedConsumer.cs` | `SprintSynced` integration-event consumer |
| `Reflekt.API/Features/RetroSessions/GetSprintContext/GetSprintContextAssembler.cs` | RA4 Fokus-metrics assembler + `ISprintAnalyticsService`/`AnonymizedSprintSummary` |
| `Reflekt.API/Features/RetroSessions/GetSprintContext/GetSprintContextEndpoint.cs` | RA4 `sprint-context` route |
| `Reflekt.API/Features/RetroSessions/GetSprintContext/GetSprintContextQuery.cs` | RA4 SprintContext DTOs |
| `Reflekt.Persistence.Tests/SprintContext/GetSprintContextAssemblerTests.cs` | RA4 assembler tests (gRPC stubs) |
| `Reflekt.Persistence.Tests/FokusSprints/FokusSprintSnapshotRepositoryTests.cs` | FokusSprint repo tests |
| `Reflekt.Persistence.Tests/FokusSprints/RefreshFokusSprintSnapshotConsumerTests.cs` | SprintSynced consumer tests |
| `Reflekt.Persistence/Repositories/SprintRepository.cs` | **filename-only** — class inside is already `RetroSessionRepository`; rename the FILE to `RetroSessionRepository.cs` in Step 5 (then it leaves the allowlist) |
| `Reflekt.Persistence/EntityConfigurations/SprintConfiguration.cs` | **filename-only** — class inside is already `RetroSessionConfiguration`; rename the FILE to `RetroSessionConfiguration.cs` in Step 5 (then it leaves the allowlist) |

> The last two are **filename-only** board-meaning: their class is already `RetroSession*`, so a content grep passes today — but the complete-rename endgame requires the file name too. Rename both files in Step 5; the post-cleanup allowlist is then the **12** genuine Fokus carve-outs.
> **Legit-Fokus comment the gate must tolerate:** `Reflekt.Domain/RetroSessions/Behaviors/RetroSession.cs:66` — `/// Adds a Fokus sprint reference … "skipped-retro covers two sprints" case` is correct *Fokus*-sprint domain language, not board-meaning. Keep it (it lives in a board-meaning file, so the gate's file-level allowlist won't cover it — exclude this **line** explicitly, see Step 5 gate).
> **Carve-out tokens left explicitly out of scope (unchanged):** `FokusSprintSnapshot.SprintId`, `SprintSynced`, `ISprintAnalyticsService`, `AnonymizedSprintSummary`, `sprint-context` — none renamed.

**What must NOT change:** runtime behavior of any endpoint/hub; the `/hubs/retro` URL (already correct — RA3); the gRPC contract; wire-visible JSON *values* (only key/route *names* change, in lockstep with their sole consumer, the in-repo retro app).

---

## Step 1 — Board FK rename: C# property + DB column on the three aggregates

Rename the child→RetroSession FK on `Card`, `CardGroup`, `Participant` (property **and** column), updating all consumers outside the carve-out:

| Old (C# property → column) | New |
|----------------------------|-----|
| `Card.SprintId` | `Card.RetroSessionId` |
| `Card.ReviewedInSprintId` (nullable FK) | `Card.ReviewedInRetroSessionId` |
| `CardGroup.SprintId` | `CardGroup.RetroSessionId` |
| `Participant.SprintId` | `Participant.RetroSessionId` |

- Update domain (`Reflekt.Domain/{Cards,CardGroups,Participants}/` incl. `Behaviors/` partials), repositories, entity configurations (`*Configuration.cs`), command/query/contract records, endpoint handlers, broadcast handlers, and the affected `*.Tests`.
- **Rename the board-meaning repo method family too** (a `\b`-anchored grep misses these — `…SprintIdAsync` has no word boundary — yet they encode the renamed concept, so leaving them is the partial rename the endgame forbids): `CardRepository.GetBySprintIdAsync` / `GetUnreviewedActionItemsBySprintIdAsync` / `CountActionItemsBySprintIdAsync` / `GetActionItemsReviewedInSprintAsync`, `ParticipantRepository.GetBySprintIdAsync`, `CardGroupRepository.GetBySprintIdAsync` → `…ByRetroSessionId…` + their ~20 call sites. The `JoinSprint`/`LeaveSprint` hub-method `sprintId` params and local variables (`claimSprintId`, `sprintIds`) follow, so the widened acceptance grep returns zero. (Fokus-sprint carve-out still excluded.)
- Then `dotnet ef migrations add RenameBoardFkSprintIdToRetroSessionId` (RetroSession startup project) — expect a `RenameColumn` on `Cards.SprintId`, `Cards.ReviewedInSprintId`, `CardGroups.SprintId`, `Participants.SprintId`. **Pattern precedent:** the RA3 rename migration `20260615212500_RenameSprintToRetroSessionAddFokusLink`.
- **DO NOT edit existing migrations or the snapshot by hand** — the new migration + EF regenerates the snapshot.

**Accept (grep is the check — widened per critic H2):** `rg "SprintId" src/Services/Reflekt --type cs -g '!**/Migrations/**' -g '!**/FokusSprint*'` piped through `rg -v "FokusSprintId"` and excluding `GetSprintContext*` returns **zero** (catches `…BySprintIdAsync` method names that `\bSprintId\b` would miss, while excluding the `FokusSprintId` carve-out token); `dotnet ef migrations has-pending-model-changes` → "No changes"; all Reflekt `.Tests` green (renamed, behavior identical).
**Skill:** None (mechanical rename + EF rename migration; reference RA3's migration) · **TDD:** no (rename — existing tests stay green) · **Confidence:** medium (homonym carve-out is the risk; grep-scoped acceptance contains it)

## Step 2 — Flat route family: `/api/sprints/{sprintId}` → `/api/retro-sessions/{retroSessionId}`

Rename all flat board routes + their route-bound request properties; update the in-repo retro frontend callers in lockstep.

- **Backend (26 routes — critic H1):** every `/api/sprints/{sprintId}/…` template → `/api/retro-sessions/{retroSessionId}/…` across `Reflekt.API/Features/**`. There is **no** `join/google` backend route in this repo (`JoinSprintEndpoint` is the only `/join`). Rename the bound request property `SprintId → RetroSessionId` on **26 request/command DTOs** (3 are already migrated — `SetReviewStatusCommand`, `GetSprintReportRequest`, `SprintContextRequest`; finish the rest). *(Q1: count corrected 23→26 — the trio `UpdateColumnsCommand` / `GetSprintSummaryRequest` / `JoinSprintCommand` was under-counted; all three are board-meaning flat-route DTOs, no carve-out. Verified: exactly 26 `[HttpX("/api/sprints/…")]` declarations.)*
- **Frontend — flat routes (lockstep):** `client/apps/retro/src/stores/{participant,vote,card,cardGroup,previousActions,sprint,report}.ts` — every `` `/api/sprints/${sprintId}/…` `` → `` `/api/retro-sessions/${retroSessionId}/…` ``. (~30 call sites.) **Correction (verified 2026-06-20):** there is **no** `join/google` literal in the frontend — `participant.ts` `joinWithGoogle` already POSTs to `/api/retro-sessions/${retroSessionId}/join` (the renamed flat join route, body `{}`). The Google-assisted-guest-join is a **broken behavior** (the `/join` validator requires a non-empty `DisplayName`, so `{}` 400s), not a route-rename target — tracked as the Step-5 round-1 follow-up, not a string to rename here.
- **Frontend — nested team-scoped routes (lockstep — Q3 fold-in, scope-approved 2026-06-20):** 9 calls to `` `/api/teams/${teamId}/sprints/…` `` are **404-ing today** — RA3 moved these backend routes to `/api/teams/{teamId}/retro-sessions/…`, and these callers were never updated. Rename the path segment **and** the board-id interpolation variable `${sprintId} → ${retroSessionId}` (keep `${teamId}` unchanged). All 9 backend targets verified present:

  | Frontend call (broken today) | → backend route (confirmed present) |
  |---|---|
  | `sprint.ts:85` `/api/teams/${teamId}/sprints` | `…/retro-sessions` (`GetTeamSprintsEndpoint`) |
  | `sprint.ts:90` `…/sprints/${sprintId}` | `…/retro-sessions/${retroSessionId}` (`GetSprintEndpoint`) |
  | `sprint.ts:102` `…/sprints` (create) | `…/retro-sessions` (`CreateSprintEndpoint`) |
  | `sprint.ts:134` `…/sprints/${sprintId}/start` | `StartSprintEndpoint` |
  | `sprint.ts:145` `…/sprints/${sprintId}/advance-phase/check` | `AdvancePhaseCheckEndpoint` |
  | `sprint.ts:155` `…/sprints/${sprintId}/advance-phase` | `AdvancePhaseEndpoint` |
  | `sprint.ts:166` `…/sprints/${sprintId}/close` | `CloseSprintEndpoint` |
  | `report.ts:85` `…/sprints/${sprintId}/report` | `GetSprintReportEndpoint` |
  | `PreviousActionsPanel.vue:112` `…/sprints/${sprintId}/previous-actions/${cardId}` | `SetReviewStatusEndpoint` (PATCH) |

  **Carve-out unaffected** (verified): none of these team-routes touches `FokusSprintSnapshot`/`FokusSprintRef`, the gRPC contract, or `GetSprintContext*` — the `/api/teams` backend grep returns zero Fokus/context hits.

**Accept:** `rg "/api/sprints/" src/Services/Reflekt client/apps/retro` returns **zero** **AND** `rg "/api/teams/[^/]*/sprints" client/apps/retro` returns **zero** (the complementary Q3 grep — catches the nested team-scoped prefix the flat grep misses; the `[^/]*` form does not match the now-correct `…/retro-sessions/…` paths); each renamed route resolves (manual smoke or existing endpoint tests green); the retro app's network calls hit `/api/retro-sessions/*` and `/api/teams/*/retro-sessions/*`.
**Skill:** None (FastEndpoints route-string + request-DTO rename) · **TDD:** no · **Confidence:** high (mechanical; grep-anchored)

## Step 3 — SignalR + guest-token claim wire strings → retro

Rename the board-meaning wire strings; update the in-repo retro frontend in lockstep; supersede the stale Q2 note.

| Old | New | Sites |
|-----|-----|-------|
| `SprintHub` (class) | `RetroHub` | `Hubs/SprintHub.cs` + DI registration (`DependencyInjection.cs`) |
| group `"sprint:{id}"` | `"retro:{id}"` | hub + ~24 broadcast handlers + 4 endpoint inline sends + **`DependencyInjection.cs:81`** (timer-expiry inline send — list explicitly so DI isn't treated as out of scope; the `sprint:\{` accept grep catches it) |
| claim `"sprintId"` | `"retroSessionId"` | issue site `GuestTokenAuthenticationHandler.cs:32` + **15** read sites (`SprintHub`, ListCards, RetractVote, CastVote, GetMyVotes, GetParticipants, GetCurrentParticipant, GetGroups, GetPreviousActions, GetParticipantReport, CreateCard, GetSprintReport) — safe to rename: the claim is minted + read per-request, no persisted/signed token carries it |
| hub method `JoinSprint` / `LeaveSprint` | `JoinRetro` / `LeaveRetro` | `SprintHub` public methods |
| `"SprintClosed"` + payload key `sprintId` | `"RetroClosed"` + `retroSessionId` | `BroadcastSprintClosedHandler.cs` |

- **Frontend (lockstep):** `client/apps/retro/src/composables/useSprintHub.ts` — `.on('SprintClosed')` → `.on('RetroClosed')` (+ payload key), `.invoke('JoinSprint')` → `.invoke('JoinRetro')`. The hub **URL** `/hubs/retro` is already correct (fixed in RA5-A Step 4) — do not change.
- The `/hubs/retro` route string and the file/composable names (`SprintHub.cs`, `useSprintHub.ts`) may be renamed for consistency (`RetroHub.cs`, `useRetroHub.ts`) — developer's call; if renamed, update all imports.
- **Remove the now-superseded board-side `Q1/Q2 DO-NOT-TOUCH` comments** (critic M2): `GetSprintReportEndpoint.cs:32`, `SetReviewStatusEndpoint.cs:11-12,77` — they said "stays sprintId-named," now contradicted by the rename. **Keep** the FokusSprint natural-key justification comments (those carve-outs remain).

**Accept:** `rg '"sprintId"|sprint:\{|"SprintClosed"|JoinSprint|LeaveSprint|class SprintHub' src/Services/Reflekt` returns **zero**; `rg "SprintClosed|JoinSprint" client/apps/retro` returns **zero**; the 24 non-`sprint` method names are untouched; retro real-time smoke passes (join, card create, vote, phase change, close all broadcast).
**Skill:** None (wire-string rename; DO-NOT-TOUCH carve-out is the 24 non-sprint methods) · **TDD:** no · **Confidence:** medium

## Step 4 — Migration apply, full grep sweep, build + test verification

- Apply the Step-1 migration to a fresh dev `reflekt.db` (disposable per RA3 Q5).
- Run the three carve-out-scoped grep sweeps from Steps 1–3 — all must return zero.
- `dotnet build SprintRituals.slnx` green; `dotnet ef migrations has-pending-model-changes` clean; all Reflekt test projects green; `client/` builds.
- Retro app end-to-end smoke (with RA5-A's merged shell): create/group/vote/close a retro, confirm real-time + all renamed routes work.

**Accept:** all four bullets pass; documented in implementation.md with the grep outputs and test counts.
**Skill:** None · **TDD:** N/A (verification step) · **Confidence:** high

## Step 5 — Complete board-meaning cleanup: locals, params, structural renames, frontend identifiers + the strengthened gate

**User-approved "complete rename" expansion (2026-06-20, relayed — see changelog).** Steps 1–3 renamed the *wire-visible* surfaces (FK columns, routes, claim/group/method strings). Codex round-1 flagged that board-meaning `sprint` still survives in **non-wire** identifiers — locals, params, type/namespace/folder names, UI/seed strings, comments, and the frontend store/route/prop layer. The endgame is COMPLETE, so these are now in scope. The gate is the strengthened `rg -i "sprint"` (below); the enumerated targets are the grep-derived inventory it must zero out.

**Carve-out invariant (unchanged):** `FokusSprint*`, `FokusSprintRef`, `GetSprintContext*`, `sprint-context`, `ISprintAnalyticsService`, `AnonymizedSprintSummary`, `SprintSynced`, all Migrations — **untouched**. The `RetroSession.cs:66` Fokus-sprint doc-comment stays (it is correct domain language).

### 5a — Backend C# locals / params / strings / comments

- **`Hubs/RetroHub.cs`** — every `sprintId` param + `claimSprintId`/`parsedSprintId`/`sprintIdClaim` local + `sprintRepo` local + the method names `RequireScrumMasterAsync`/`LoadTimedSprintAsync` (param) + the `"…this sprint."` / `"Sprint not found."` HubException strings + the `// sprintId -> …` comments → retro-board naming (`retroSessionId`, etc.). (`retro:{…}` group strings already correct — keep.)
- **`Features/Sprints/Timer/TimerService.cs`** — all 10 `(int sprintId)` params + the `"No timer state found for sprint {…}"` string → `retroSessionId`.
- **`DependencyInjection.cs` (Reflekt.API)** — timer-expiry lambda param `sprintId` (line ~79) + the `// … to sprint group` comment (line ~77) → `retroSessionId` / "retro group".
- **`Features/Sprints/AdvancePhase/BroadcastPhaseChangedHandler.cs`** — local `var sprintId = ev.RetroSession.Id` → `retroSessionId`.
- **`Reflekt.Persistence/Repositories/TeamRepository.cs`** — local `sprintIds` (the `RetroSessions.Select(s => s.Id)` list) → `retroSessionIds`; **`TeamSummaryDto` fields** `SprintCount` / `LastSprintDate` / `HasActiveSprint` → `RetroSessionCount` / `LastRetroSessionDate` / `HasActiveRetroSession` (and `GetMyTeams` DTO + frontend `teams.ts` fields in lockstep — 5c).
- **11 endpoint guest-claim locals** — `claimSprintId` / `sprintIdClaim` in: `Cards/CreateCard`, `Cards/ListCards`, `CardGroups/GetGroups`, `Votes/{CastVote,GetMyVotes,RetractVote}`, `Participants/{GetCurrentParticipant,GetParticipants}`, `PreviousActions/GetPreviousActions`, `Reports/{GetParticipantReport,GetSprintReport}` → `claimRetroSessionId`. (The claim *name* `"retroSessionId"` is already correct from Step 3 — this is only the local holding it.)
- **`var sprint` locals + `"Sprint …"` user-facing strings across the endpoint set** (JoinRetro, Close, Update, Start, Get, AdvancePhase, all Card/CardGroup/Vote/Participant/EvidenceLink endpoints) → `var session` (developer's call on the local name) + retro-worded errors (`"Sprint is closed." → "Retro is closed."`, `"Sprint must be Active." → "Retro must be Active."`, etc.).
- **`Reflekt.Persistence/DbInitializer.cs`** — seed local `RetroSession sprint` → `session`; seed strings `"Sprint 1"`, `"…this sprint!"`, `"Sprint planning took too long."` are dev-seed card text — reword to retro-neutral or accept as DO-NOT-MATCH only if the gate excludes the file (it does **not**; rename them).
- **`Auth/ClaimsPrincipalExtensions.cs:9`** — comment `(Team/Sprint)` → `(Team/RetroSession)` (comment-only; no `GetSprintId()` method exists).
- **Contracts:** `Features/Reports/ReportContracts.cs` — `ReportSprintInfo` type + `Sprint` field + `PreviousSprintName` → retro naming; `Features/PreviousActions/PreviousActionsContracts.cs` — `PreviousSprintId` / `PreviousSprintName` → `PreviousRetroSessionId` / `PreviousRetroSessionName` (lockstep with the frontend report panel reading these — 5c). **These are response-body wire names** → the frontend consumer changes in the same step.

### 5b — Backend structural renames (type / namespace / folder / file)

- **`Features/Sprints/` folder → retro-board equivalent** + the **15** `namespace Reflekt.API.Features.Sprints.*` declarations (grep-confirmed list: AdvancePhase ×3, CloseSprint ×2, CreateSprint ×2, GetSprint, GetSprintSummary, GetTeamSprints, StartSprint, Timer, UpdateColumns ×3). Folder name and namespace segment → the retro-board name (developer's call: `RetroSessions` would collide with the existing `Features/RetroSessions/GetSprintContext/` — pick a non-colliding board name, e.g. `Features/Board/` or merge intentionally; **call out the collision explicitly so the developer resolves it, not discovers it mid-build**).
- **`JoinSprint*` type family → `JoinRetro*`** — **ALREADY DONE** (verified: `rg "JoinSprint" src/Services/Reflekt` returns zero; folder is `Participants/JoinRetro/`, types are `JoinRetroCommand`/`JoinRetroEndpoint`/`JoinRetroResponse`). No action — recorded for the done-check so it is not re-flagged as missing.
- **Board-meaning class/file names still present:** `BroadcastSprintClosedHandler` → `BroadcastRetroClosedHandler`; `CloseSprintEndpoint` / `StartSprintEndpoint` / `GetSprintEndpoint` / `GetSprintSummaryEndpoint` / `GetTeamSprintsEndpoint` → retro-board equivalents; `GetSprintReportEndpoint` (+ `GetSprintReportRequest`) → `GetRetroReport*`; the **filename-only** `SprintRepository.cs` → `RetroSessionRepository.cs` and `SprintConfiguration.cs` → `RetroSessionConfiguration.cs` (classes already renamed). Update all `using`/refs.

### 5c — Frontend board-meaning identifiers (lockstep with the backend wire renames above)

- **Store:** `stores/sprint.ts` → `stores/retroSession.ts` — `useSprintStore` → `useRetroSessionStore`, interfaces `Sprint`/`SprintSummary`/`CreateSprintPayload` → `RetroSession*`, state `sprints`/`currentSprint`/`currentSummary`, all action names, `const sprint` locals. Update every importer (`useSprintHub`, `useRetroRoom`, `useRetroBoardActions`, all sprint views/components).
- **Composable:** `composables/useSprintHub.ts` → `useRetroHub.ts` (function + var + import).
- **Router:** `router/index.ts` — route **path** segments `/teams/:teamId/sprints` and `/teams/:teamId/sprints/:sprintId[/report]` → `…/retro-sessions/:retroSessionId…`; the guest deep-link path **`/sprint/:sprintId` → `/retro/:retroSessionId`** (or chosen retro path); route **names** `'team-sprints'`/`'sprint'` → retro names; `:sprintId` param → `:retroSessionId`; view imports. **Update every `router.push({ name: 'sprint' … })` / `route.params.sprintId` / `${origin}/sprint/${id}` deep-link builder in lockstep** (`SprintListView`, `SprintView`, `RetroRoomView:66`, `RetroJoinForm:88` `returnUrl=/retro/sprint/…`).
- **Storage key:** `stores/guest.ts` — `guestTokenKey` template **`reflekt:sprint:${id}:token` → `reflekt:retro-session:${id}:token`** (or chosen key) + the locked-format comment + `sprintId` params. ⚠️ **Wire/persistence note:** this localStorage key is the guest-token lookup; changing it orphans any token written under the old key. Since guest tokens are per-retro and short-lived and the frontend is the sole reader (in-repo, lockstep), a clean break is acceptable — **state this explicitly** so it is a sanctioned break, not a silent one.
- **Views/components — the file list is the gate, not a hand-curated subset.** Every frontend file the strengthened gate flags is in scope; do NOT work from a partial component list. Confirmed board-meaning files include (non-exhaustive — the gate is authoritative): `views/{SprintView,SprintListView,RetroRoomView,ReportView,TeamDashboardView}.vue`; `components/sprints/{SprintDetailPanel,SprintList,CreateSprintForm}.vue`; `components/reports/SprintReportPanel.vue`; `components/retro/{RetroBoardPanel,RetroPhaseHeader,RetroReportPanel,RetroJoinForm}.vue`; **`components/teams/{TeamSelector,TeamCard,DeleteTeamDialog}.vue`** (these reference `useSprintStore`, `name: 'sprint'`/`'team-sprints'` route names, `:sprints`/`@create-sprint`, and `team.sprintCount`/`lastSprintDate`/`hasActiveSprint` fields + "sprint(s)" UI text). Rename to retro-board names; `:sprint-id`/`:sprint-name`/`@close-sprint`/`@create-sprint` props+events → retro equivalents; `sprintId`/`sprintName`/`sprintStore` locals; the `report.sprint.*` / `previousSprintName` / `team.sprintCount` field reads (lockstep with 5a contracts + `teams.ts`); UI strings (`New Sprint`/`Create Sprint`/`Close Sprint`/`Sprint Report`/`Sprint name`/`"{n} sprints"`/`"active sprint"`/`"all its sprints"`) → retro wording; the `name: 'sprint'`/`'team-sprints'` `router.push` targets (lockstep with the 5c router rename).

### Round-1 runtime fixes folded into this cycle (record + verify)

1. **BLOCKER — `ColumnConfigUpdated` payload-key wire break.** Round-1 found backend `BroadcastColumnConfigUpdatedHandler` and frontend `useSprintHub.ts` disagreed on the payload key, silently breaking live column-config updates. **Current state (verified 2026-06-20): both sides now use `retroSessionId`** (`BroadcastColumnConfigUpdatedHandler.cs` send + `useSprintHub.ts:~147` receive) — the break is **already repaired**. Step 5 must **preserve the matched key** when it renames `useSprintHub.ts → useRetroHub.ts`: backend send key and frontend read key stay byte-identical (`retroSessionId`). Acceptance: a real-time column-config smoke (rename a column, confirm other clients update).
2. **`/join/google` 404 / dead Google-guest-join.** Verified: there is **no** `/join/google` backend route. `participant.ts` `joinWithGoogle(retroSessionId)` POSTs `{}` to `/api/retro-sessions/{id}/join`, which fails `JoinRetroCommand`'s `DisplayName.NotEmpty()` validator (400, not 404 — the round-1 "404" label was imprecise; the call is **broken either way**). The Google-authed auto-join (fill `DisplayName` from the Person/cookie claim) was never implemented. **Decision for this cycle:** the *string* is renamed in lockstep (Step 2 already handles the path); the **broken Google-join behavior is a pre-existing functional gap** — record it as a **follow-up** (`FEAT/BUG-{N}: Google-assisted guest join not implemented`), do **not** silently leave it looking done. (No `/join/google` literal exists to rename — Step 2's earlier `join/google` note was based on a stale path; corrected here.)

### Strengthened acceptance gate (the binding check for the whole "complete rename")

```
rg -i "sprint" src/Services/Reflekt client/apps/retro -g '!**/Migrations/**'
```
must return **only** the carve-out allowlist — i.e. pipe out the sanctioned remainders and expect **zero**:
```
rg -i "sprint" src/Services/Reflekt client/apps/retro -g '!**/Migrations/**' \
  | rg -iv "FokusSprint|GetSprintContext|SprintContext|ISprintAnalyticsService|AnonymizedSprintSummary|SprintSynced|sprint-context" \
  | rg -v "Behaviors/RetroSession.cs.*Fokus sprint"     # the one legit-Fokus doc-comment line
  → ZERO
```
(After the 5b file renames, `SprintRepository.cs`/`SprintConfiguration.cs` no longer appear as filenames; the only permitted hits are the 12 genuine Fokus carve-out files + the single `RetroSession.cs:66` Fokus doc-comment line.) Plus: `dotnet build SprintRituals.slnx` green; all Reflekt `.Tests` green; `client/` builds; the two round-1 smokes (column-config real-time; guest+Google join path — Google documented as a known gap) recorded in implementation.md.

**Skill:** None (mechanical identifier rename across locals/types/namespaces/frontend; the gate is the inventory) · **TDD:** no (rename — existing tests stay green; behavior unchanged) · **Confidence:** medium — the `Features/Sprints/` namespace collision with the existing `Features/RetroSessions/` and the breadth (781 starting hits) are the risks; the strengthened gate + carve-out allowlist contain them. **Satisfies:** the owner-approved "complete, not partial" endgame (parent plan locked decision 2026-06-19, extended 2026-06-20).

---

## Skill Mapping

| Step | Disposition | TDD | Notes |
|------|-------------|-----|-------|
| 1 | None | no | EF property+column rename + rename migration (precedent: RA3 migration) |
| 2 | None | no | FastEndpoints route + request-DTO rename; frontend callers lockstep |
| 3 | None | no | SignalR/claim wire-string rename; carve out 24 non-sprint methods |
| 4 | None | N/A | Migration apply + grep sweep + build/test verification |
| 5 | None | no | Complete board-meaning cleanup: locals/params/strings/comments + structural type/namespace/folder/file renames + frontend store/route/prop/storage-key; strengthened `rg -i "sprint"` gate |

**All-`None` rename pass** — the value is in the old→new tables, the homonym carve-out, the carve-out allowlist, and the grep-scoped acceptance, not pattern skills; the done-check all-None exemption applies. Each step traces to completing the RA3 `Sprint→RetroSession` rename (ADR-017/018) under the owner's "complete, not partial" endgame.

## Review mode

**Critic — code-grounded, mandatory.** This is a rename/type-move pass against a live schema + a homonym with 368 occurrences; a doc-conformance review is structurally blind to a mis-scoped grep. The critic must verify the carve-out boundary against source (no Fokus-sprint ref renamed, no migration edited) and that the grep-scoped acceptance actually excludes the carve-out.

---

## Plan Review (critic, Mode 2 — code-grounded)

**Verdict: ACCEPT-WITH-CHANGES → all findings folded above.** Critic agent `aa257291cb1e2831c`. Coverage: the carve-out boundary is correct (every board-meaning identifier is a target; every Fokus-sprint ref + all migrations correctly excluded; `RenameColumn` is the right mechanism). Defects were inventory-accuracy, not boundary-inversion.

| # | Severity | Finding | Disposition |
|---|----------|---------|-------------|
| H1 | HIGH | Route count is **26**, not 27 — `join/google` has no backend endpoint (frontend-only call, already 404s) | Step 2 corrected to 26 + frontend `join/google` flagged as a pre-existing dead call |
| H2 | HIGH | `\bSprintId\b` accept grep silently misses board-meaning `…BySprintIdAsync` repo methods → could pass a *partial* rename | Step 1 grep widened to `SprintId` minus `FokusSprintId`/carve-out; repo method family + params/locals added as explicit targets |
| M1 | MEDIUM | DTO scope understated — **23** request/command records carry `SprintId` (only 3 pre-migrated), not "several" | Step 2 states 23 + names the 3 done |
| M2 | MEDIUM | Stale board-side `Q1/Q2 DO-NOT-TOUCH` comments (`GetSprintReportEndpoint:32`, `SetReviewStatusEndpoint:11-12,77`) must be removed | Step 3 bullet added; FokusSprint natural-key comments kept |
| L1 | LOW | Claim read-sites = **15**, not "~16" | Step 3 table corrected to 15 |

**Resolved, no action:** guest-token claim rename is safe (minted+read per-request, no persisted/signed token); `FokusSprintSnapshot.SprintId → FokusSprintId` correctly left out of scope (borders RA4 gRPC mapping). `DependencyInjection.cs:81` board-meaning group string now listed explicitly (grep already caught it).

**Status: Approved — ready for developer** (all critic findings folded; boundary verified code-grounded; no open CRITICAL/HIGH).

---

## Post-approval amendments (architect, 2026-06-20 — from developer questions Q1–Q4)

Developer raised 4 questions before implementing; rulings in `questions.md` (To: developer). Three touched the plan:

| Q | Ruling | Plan change applied |
|---|--------|---------------------|
| Q1 | Proceed with all **26** flat-route DTOs (was a 23 count typo) | Step 2: "23 request/command DTOs" → "26" + the under-counted trio named |
| Q2 | Rename `VoteRepository.GetParticipantIdsWithVotesInSprintAsync` → `…ForRetroSessionAsync` + call site `AdvancePhaseCheckEndpoint.cs:86` | None — already mandated by Step 1's repo-method-family rule + widened grep (line 44 `g.SprintId` would fail the gate); explicit list under-enumerated |
| Q3 | **FOLD IN** the 9 nested `/api/teams/${teamId}/sprints/…` frontend fixes (404-ing today) + complementary acceptance grep | Step 2: added the nested team-scoped frontend bullet (9 calls, all backend targets verified) + `rg "/api/teams/[^/]*/sprints" client/apps/retro → zero` to Accept |
| Q4 | Rename all **17** `"sprintId"` claim occurrences; grep is the gate | None — Step 3's `'"sprintId"'` grep already governs all occurrences (SprintHub ×4 explains the "15" prose count) |

**Q3 scope-expansion attribution:** the Q3 fold-in grows Step-2 scope beyond the original critic-approved boundary. Approval was **relayed by the team lead** ("user decided Q3: FOLD IN") — recorded as **relayed approval, not verified in the architect's own user channel.** Technical recommendation was fold-in at HIGH confidence (broken-today calls, same op, carve-out verified clean). If a later interview reverses this, the 9-call fold-in + complementary grep is the unit to revert. The carve-out remains intact (`FokusSprintSnapshot`/`FokusSprintRef`/gRPC/`GetSprintContext*` untouched) under this amendment.

---

## Changelog — "complete rename" expansion (Step 5 added, architect, 2026-06-20)

**What changed:** Added **Step 5** (full board-meaning local-identifier cleanup + structural type/namespace/folder/file renames + frontend store/route/prop/storage-key) and the **strengthened gate** `rg -i "sprint" src/Services/Reflekt client/apps/retro` (excl. Migrations) → only the carve-out allowlist. Added the **carve-out allowlist** (14 files, → 12 after the 2 filename renames). Recorded the **two round-1 runtime fixes** (ColumnConfigUpdated payload key — verified already repaired, must stay matched; `/join/google` — no such route, Google-guest-join is a pre-existing broken behavior → follow-up). Corrected the stale Step-2 `join/google` rename note.

**Why:** Codex round-1 flagged board-meaning `sprint` surviving in non-wire identifiers (locals, params, type/namespace/folder names, UI/seed strings, comments, the whole frontend store/route/prop layer). Steps 1–3 only covered the wire surface; the owner's endgame is COMPLETE, not partial, so these are in scope.

**Grounding (this amendment is code-verified, not memory):**
- `rg -i "sprint"` backend (excl. Migrations) = **781** occurrences pre-cleanup; carve-out allowlist = **14 files** (each read to confirm ONLY carve-out `sprint`).
- `Features/Sprints/` namespace declarations = **15** (grep-confirmed). ⚠️ The folder's target name **collides** with the existing `Features/RetroSessions/GetSprintContext/` — Step 5b calls this out for the developer to resolve, not discover.
- `JoinSprint*` family = **already renamed** to `JoinRetro*` (`rg "JoinSprint"` → zero); recorded as done so the done-check doesn't mis-flag it Missing.
- `ColumnConfigUpdated` key = **already matched** on both sides (`retroSessionId`); the round-1 break is repaired — Step 5 must preserve it through the `useSprintHub → useRetroHub` rename.
- `/join/google` = **no backend route exists**; `joinWithGoogle` posts `{}` to `/join` → 400 on `DisplayName.NotEmpty()`. Broken behavior, not a rename target.
- Filename-only board cases: `SprintRepository.cs` / `SprintConfiguration.cs` (classes already `RetroSession*`) — file renames added to Step 5b.

**Attribution (hard rule — relayed, NOT user-confirmed in my channel):** this expansion was **relayed by the team lead** as a user decision ("User decided to complete the RA5-C rename fully … after Codex round-1"). The relaying message itself states coordinator-relayed approval carries no user authority. Recorded therefore as **relayed approval — durable record + re-review gate, not architect-verified user consent.** The expansion is technically sound and code-grounded (above), and the developer is implementing against this scope in parallel, so the durable record + strengthened gate are the right artifact regardless. **If a later direct-user interview narrows "complete" back to wire-only,** the revert unit is all of Step 5 (locals/params/strings/structural/frontend-identifier) — Steps 1–3 (wire surface) stand independently. **Carve-out invariant held** across the whole expansion: `FokusSprint*`/`FokusSprintRef`/`GetSprintContext*`/`sprint-context`/`ISprintAnalyticsService`/`AnonymizedSprintSummary`/`SprintSynced`/Migrations — none renamed; the `RetroSession.cs:66` Fokus-sprint doc-comment is preserved and explicitly excluded from the gate.
