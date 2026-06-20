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

- **Backend (26 routes — critic H1):** every `/api/sprints/{sprintId}/…` template → `/api/retro-sessions/{retroSessionId}/…` across `Reflekt.API/Features/**`. There is **no** `join/google` backend route in this repo (`JoinSprintEndpoint` is the only `/join`). Rename the bound request property `SprintId → RetroSessionId` on **23 request/command DTOs** (3 are already migrated — `SetReviewStatusCommand`, `GetSprintReportRequest`, `SprintContextRequest`; finish the rest).
- **Frontend (lockstep):** `client/apps/retro/src/stores/{participant,vote,card,cardGroup,previousActions,sprint,report}.ts` — every `` `/api/sprints/${sprintId}/…` `` → `` `/api/retro-sessions/${retroSessionId}/…` ``, **including** `participant.ts` `joinWithGoogle` → `/api/retro-sessions/${retroSessionId}/join/google`. (~30 call sites.) **Flag (not fixed here):** that `join/google` call already hits no backend endpoint — a pre-existing Google-assisted-guest-join gap; rename the string in lockstep but record the dead call as a follow-up.

**Accept:** `rg "/api/sprints/" src/Services/Reflekt client/apps/retro` returns **zero**; each renamed route resolves (manual smoke or existing endpoint tests green); the retro app's network calls hit `/api/retro-sessions/*`.
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

---

## Skill Mapping

| Step | Disposition | TDD | Notes |
|------|-------------|-----|-------|
| 1 | None | no | EF property+column rename + rename migration (precedent: RA3 migration) |
| 2 | None | no | FastEndpoints route + request-DTO rename; frontend callers lockstep |
| 3 | None | no | SignalR/claim wire-string rename; carve out 24 non-sprint methods |
| 4 | None | N/A | Migration apply + grep sweep + build/test verification |

**All-`None` rename pass** — the value is in the old→new tables, the homonym carve-out, and the grep-scoped acceptance, not pattern skills; the done-check all-None exemption applies. Each step traces to completing the RA3 `Sprint→RetroSession` rename (ADR-017/018) under the owner's "complete, not partial" endgame.

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
