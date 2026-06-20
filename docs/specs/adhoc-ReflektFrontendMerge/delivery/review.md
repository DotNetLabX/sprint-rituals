# RA5-A — Frontend Merge & Routing Shell: Review

## Step 1 — Done-Check

**Pre-commitment predictions (before reading implementation.md):** given the plan's risk profile, the three most likely gaps were (1) the C1 hub-path fix silently missed in the copy-in, (2) one of the four H1 absolute redirects left unprefixed, and (3) the Host fallback registered before the endpoint maps (swallowing `/api`, `/auth`, `/hubs`). All three were checked specifically — none materialized.

**Verify gate note:** the platform verify gate skipped this run (custom developer agent name `dev-ra5a`), so build/conformance claims were re-confirmed by re-running `dotnet build` and `npm run build` and grepping every cited file:line — not assumed from implementation.md.

| Step | Disposition | Notes |
|------|-------------|-------|
| 1 — Establish client workspace; move Fokus, bring Reflekt in | Implemented | `client/` is an npm workspace (`workspaces: ["apps/*","packages/*"]`); `apps/dashboard`, `apps/retro`, `packages/shared/src/{auth.ts,index.ts}` all present on disk. Shared deps hoisted to root; app-unique deps (`apexcharts`+`vue-router@4` dashboard, `lucide`+`vue-router@5` retro) kept per-app. `<reflekt-repo>\web\reflekt-web` SPA source confirmed unmodified (`git status` clean for that path). Two documented deviations (build-script `run-p`→`vue-tsc --build && vite build`; `@tsconfig/node24`→inline `compilerOptions`) — both valid (root-unavailable deps, steered by team lead). |
| 2 — Two Vite builds, path-prefixed bases → `wwwroot/{dashboard,retro}` | Implemented | `base:'/dashboard/'` + `outDir:'../../../src/Host/wwwroot/dashboard'` + scoped `emptyOutDir` confirmed in dashboard `vite.config.ts`; retro `base:'/retro/'` + matching `outDir`. Dashboard router `createWebHistory('/dashboard/')`. All four H1 redirects fixed at source: `client.ts:15`→`/dashboard/login`, `authStore.ts:36`→`/dashboard/login`, `auth.ts:26`→`/retro/`, `RetroJoinForm.vue:88` returnUrl→`/retro/sprint/${id}`. Root `build` script chains both workspace builds. |
| — retro router base | Deviated (valid) | Plan said `createWebHistory('/retro/')`; impl kept retro's existing `createWebHistory(import.meta.env.BASE_URL)`, which resolves to `/retro/` from Vite's `base`. Reason documented; functionally equivalent and less invasive. |
| 3 — Shared build-time package: auth-state + thin UI kit | Implemented | `packages/shared/src/auth.ts` exports `fetchMe()` wrapping **only** `GET /auth/me` with explicit `credentials:'same-origin'`; returns `null` on non-ok (no over-extraction of the fetch wrapper — M1 honored). Both apps delegate via `@sprint-rituals/shared` (alias + tsconfig `paths` + dep entry). Guest auth (X-Guest-Token, localStorage, `accessTokenFactory`) confirmed untouched and retro-local. No shared UI kit invented (no genuine duplicate) — plan explicitly permits this. |
| 4 — Host serves both SPAs under prefixes + thin `/` entry + C1 fix | Implemented | `Program.cs`: unscoped `UseStaticFiles` replaced by two `RequestPath`-scoped providers (`/dashboard`→`wwwroot/dashboard`, `/retro`→`wwwroot/retro`); bare `MapFallbackToFile("index.html")` removed; two prefixed fallbacks added **after** `MapFokus/ReflektEndpoints` and Identity maps (`:94-95`); thin `/` `MapGet` redirect (signed-in→`/dashboard/`, else→`/dashboard/login`). **C1 verified**: `useSprintHub.ts:32` now `.withUrl('/hubs/retro', …)` — collision with Fokus hub removed. Open question (cold `/` for unauth user) resolved to dashboard login per plan default. |
| 5 — End-to-end verification | Implemented (build-verified; manual smoke deferred) | `dotnet build SprintRituals.slnx` re-run by architect: **green, 0 errors** (6 pre-existing NU1903 warnings; impl said 9 — count is not load-bearing and unrelated to this feature). `npm run build` re-run: **green** — both apps emit `index.html`+assets under their own prefixes; `vue-tsc --build` type-check passed for both; asset paths confirmed prefix-scoped (no collision). Manual browser smoke (cross-prefix auth, live SignalR, `/` redirect in browser) explicitly deferred and documented as a reviewer/operator item — acceptable: the plan's Step 5 acceptance is `dotnet build` green + `npm run build` green + documented results, all met; the four browser bullets are manual-verification items with no automated frontend suite by project decision. |

**Pre-existing-bug fixes (in-scope, documented):** `TeamSelector.vue` (`hasTeam`→`hasTeams`, `currentTeam`→`teams[0]`, `createSprint` arity) and `previousActions.ts:52-53` (undefined guard under `noUncheckedIndexedAccess`). These were required for retro's stricter type-check to build green; fixing them at copy-in is correct (a broken build is not a valid hand-off). Confirmed by the green `vue-tsc --build`.

**Skill conformance:** All-`None` plan (every step `Skill: None`, `TDD: no`). The all-None exemption applies — no pattern-skill log entries are expected. Scoped audit log (`agent ∈ {developer, main, dev-ra5a}` for the `developer:implement` token window of this run) shows the developer-role lifecycle skill `nexus:developer` only — consistent with an all-None infra pass. `## Skills Used` section present in implementation.md and matches. No fabricated invocations. No conformance gap.

**Carry-overs noted (not blockers):** legacy `wwwroot/index.html` + `wwwroot/assets/` orphaned by the move (not served by any route now; safe to delete — left for reviewer confirmation); `lucide-vue-next@1.0.0` deprecation warning (follow-up); manual browser smoke of the four Step-5 bullets (operator/reviewer).

**Verdict: PASS**

*Status: COMPLETE — architect, 2026-06-20*

---

## Step 2 — Code Review

## Reviewed By
Reviewer agent (nexus:reviewer), 2026-06-20. Cycle 2/3 re-review. All five Cycle 1 fixes verified against actual source files + backend endpoint attributes. Fresh builds run independently.

## Verdict: APPROVED

## Cycle 1 Fixes — Verified

| Fix # | Severity | Claim | Verified against | Result |
|-------|----------|-------|-----------------|--------|
| 1 | BLOCKER | `shared/src/auth.ts:13` → `fetch('/api/auth/me', …)` | `GetMeEndpoint.cs:5` `[HttpGet("/api/auth/me")]` | ✓ Match |
| 2 | BLOCKER | `retro/stores/auth.ts:21` → `window.location.href = '/auth/login'` | `LoginEndpoint.cs:13` `app.MapGet("/auth/login", …)` | ✓ Match |
| 3 | BLOCKER | `RetroJoinForm.vue:88` computed → `` `/auth/login?returnUrl=/retro/sprint/${props.sprintId}` `` | Same `LoginEndpoint.cs:13` | ✓ Match |
| 4 | BLOCKER | `retro/stores/auth.ts:24` → `fetch('/api/auth/logout', …)` | `LogoutEndpoint.cs:6` `[HttpPost("/api/auth/logout")]` | ✓ Match |
| 5 | MAJOR | `givenName?: string` (optional) in shared + retro local `User` interface | `packages/shared/src/auth.ts:8`; `retro/stores/auth.ts:4` | ✓ Fixed |

**Adjacent-area check:** dashboard `api/auth.ts` still calls `apiFetch('/auth/me')` and `apiFetch('/auth/logout')`. These go through `client.ts` which prepends `BASE_URL = '/api'`, reaching `/api/auth/me` and `/api/auth/logout` — both correct server routes. The guard at `client.ts:14` (`path !== '/auth/me'`) checks the pre-prefix path, which remains correct. No regression introduced.

## Findings

### [LOW] `getMe()` in `dashboard/src/api/auth.ts:4` is dead code
**File:** `client/apps/dashboard/src/api/auth.ts:4`
**Origin:** implementation
**Issue:** `getMe()` was the pre-shared-package auth fetch; `authStore.ts` now delegates to `sharedFetchMe()`. `getMe()` is exported but no file imports it — zero call sites. Harmless but adds noise.
**Fix:** Remove `getMe()` from `auth.ts` in a follow-up cleanup pass (RA5-B or boy-scout). No immediate action required.
**Confidence:** 92/100

### [LOW] Dashboard `authStore.ts` casts shared `AuthUser` to richer local `AuthUser`
**File:** `client/apps/dashboard/src/stores/authStore.ts:20`
**Origin:** implementation
**Issue:** Local `AuthUser` has `id: number`, `avatarUrl: string | null` that the shared `AuthUser` type doesn't declare. The `as AuthUser | null` cast silences TS structural checking. Runtime is correct — `/api/auth/me` returns both fields. `AppSidebar.vue:159-160` accesses `user.avatarUrl` correctly. Fragile if the shared type or API changes.
**Fix:** Cleanup candidate when shared type stabilizes (RA5-B or later). No immediate action required.
**Confidence:** 90/100

## Carry-Over Findings (from implementation.md)

| Carry-Over Item | Resolution |
|----------------|-----------|
| Legacy `wwwroot/index.html` + `wwwroot/assets/` orphaned | Confirmed safe (both cycles): no unscoped `UseStaticFiles` handler; no unscoped fallback. Files are inert dead build artifacts — not served. Deletion safe; out of RA5-A scope. |
| `lucide-vue-next@1.0.0` deprecation warning | Acknowledged. Non-blocking. Follow-up in RA5-B or later. |
| Manual browser smoke (four Step-5 bullets) | Marked deferred-by-policy in implementation.md — confirmed correct disposition. No automated frontend suite by project decision. |

## Positive Observations
- All three Codex-caught BLOCKER route mismatches are now correct and verified against backend endpoint attributes (not assumed from comments or documentation).
- `givenName` made optional in both shared type and retro local `User` interface — type contract now accurately reflects the API.
- Dashboard `apiFetch` path routing through `BASE_URL = '/api'` is correct and unaffected by the shared-package changes.
- C1 hub-path, H1 redirects, two-build topology, `UseStaticFiles` registration order all held from Cycle 1.
- Both builds green post-fix (confirmed in this review session).

## Gaps
- `returnUrl` parameter in `RetroJoinForm.vue:88` is sent to `/auth/login` but `LoginEndpoint.cs` ignores it (hardcodes `RedirectUri = "/auth/callback"`). Pre-existing behavior from original Reflekt source — out of RA5-A scope (route/behavior renames are RA5-C). Follow-up.
- Manual browser smoke of the four Step-5 bullets remains deferred per project policy. Documented.

## Open Questions
None.

## Evidence
| Check | Result | Command | Output |
|-------|--------|---------|--------|
| .NET build (Cycle 2) | PASS | `dotnet build SprintRituals.slnx --no-restore` | `Build succeeded. 6 Warning(s) 0 Error(s)` (pre-existing NU1903 only) |
| Frontend build (Cycle 2) | PASS | `npm run build` (at `client/`) | Dashboard + retro both emit `index.html` + assets under their prefixes; both `vue-tsc --build` clean |
| Fix 1 — `/api/auth/me` | PASS | Read `packages/shared/src/auth.ts:13` | `fetch('/api/auth/me', …)` — matches `GetMeEndpoint.cs:5` |
| Fix 2 — `/auth/login` signIn | PASS | Read `retro/stores/auth.ts:21` | `window.location.href = '/auth/login'` — matches `LoginEndpoint.cs:13` |
| Fix 3 — `/auth/login` JoinForm | PASS | Read `RetroJoinForm.vue:88` | computed `` `/auth/login?returnUrl=…` `` — matches `LoginEndpoint.cs:13` |
| Fix 4 — `/api/auth/logout` | PASS | Read `retro/stores/auth.ts:24` | `fetch('/api/auth/logout', …)` — matches `LogoutEndpoint.cs:6` |
| Fix 5 — `givenName?` optional | PASS | Read `shared/src/auth.ts:8`; `retro/stores/auth.ts:4` | Both declare `givenName?: string` |
| Adjacent auth paths (dashboard) | PASS | Grep `auth/me\|auth/logout\|auth/google/login` in `client/` | Dashboard uses `apiFetch(…)` with `BASE_URL='/api'` — routes correctly; zero `/auth/google/login` matches |

*Status: COMPLETE — reviewer, 2026-06-20*

---

# RA5-C — Backend Rename Completion (board → RetroSession): Review

## RA5-C — Step 1 Done-Check

**Plan:** `plan-C-renames.md` · **Implementation:** `implementation.md` (RA5-C section, lines 147-292)

**Pre-commitment predictions (before reconciling):** the three most likely gaps were (1) the 27-vs-26 route count signalling a 27th over-eager rename touching the carve-out; (2) a Fokus-sprint identifier (`FokusSprintSnapshot`/`FokusSprintRef`/gRPC/`GetSprintContext*`) wrongly renamed; (3) the EF migration doing column drop/recreate instead of `RenameColumn`. All three were checked specifically against source — none materialized as a defect (see reconciliations below).

**Verify-gate note:** the platform verify gate recorded a vacuous pass (0 configured commands). Build/test/grep claims below were **re-run by the architect**, not assumed from implementation.md — `dotnet build` (green, 0 errors), `dotnet test ~Reflekt` (Domain 7/7, Persistence 14/14 green), `dotnet ef migrations has-pending-model-changes` ("No changes"), `npm run build` (both apps green, `vue-tsc --build` clean), and every acceptance grep re-executed.

### Required reconciliations

**1 — ROUTE COUNT (26 confirmed correct; the "27" is a documentation miscount, not an over-eager rename).**
The flat `/api/retro-sessions/{retroSessionId}/…` grep returns **27** endpoint declarations. Exactly **one** of those — `GetSprintContextEndpoint.cs:13` → `/api/retro-sessions/{retroSessionId}/sprint-context` — is the **RA4 carve-out**, which was *already* `/api/retro-sessions/` before this pass (born that way in RA4) and was **not** renamed here. Subtract the carve-out: **26 board-meaning routes were renamed from `/api/sprints/`**, matching the plan's Q1-verified count exactly. The flat `/api/sprints/` grep over `src/Services/Reflekt` returns **zero** — every board route migrated. implementation.md line 203 says "27 endpoint files" but its own enumeration (line 204) names only 26 and includes neither `GetSprintContext` (carve-out) nor `GetSprintReport` (team-scoped). The "27" is an off-by-one in the prose header; **no route was wrongly included** — the carve-out `sprint-context` route is intact and was never touched. **Verdict: 26 is correct; the developer's "27" is a benign label error, the rename set is right.**

**2 — CARVE-OUT (critical — fully preserved).** `GET /api/retro-sessions/{retroSessionId}/sprint-context` survives unchanged; its handler reads `retro.FokusSprintRefs.Select(r => r.FokusSprintId)` — Fokus identifiers intact. All carve-out symbols verified present and untouched: `FokusSprintSnapshot.SprintId` (natural Fokus key, `HasKey`/`ValueGeneratedNever` config intact), `FokusSprintRef.FokusSprintId` (composite key intact), gRPC `ISprintAnalyticsService` + `AnonymizedSprintSummary.SprintId` + `SprintQuery.SprintId` + `SprintInfo.SprintId` in `Blocks.Contracts` (all untouched — wire contract, out of scope), `SprintSynced.SprintId` integration event, `GetSprintContext*` namespace/class/assembler/query (125 carve-out occurrences across 24 files, all intact). `PreviousActionsContracts.PreviousSprintId` (a Fokus-sprint ref) correctly left. **No 27th over-eager rename reached the carve-out.**

**3 — BOTH acceptance greps return zero (confirmed).**
- `rg "SprintId" src/Services/Reflekt -g '!**/Migrations/**'` → every remaining hit categorized: all are **Fokus-sprint carve-outs** (`FokusSprintSnapshot`, `FokusSprintRef`, gRPC, `SprintSynced`, `GetSprintContext*`, `PreviousSprintId`) or **camelCase local variables** (`claimSprintId`, `parsedSprintId`, `sprintIdClaim`, method params `int sprintId` in hub). **Zero board-meaning `SprintId`** remain. (The local-var residue is a documented cosmetic carry-over, not a board-FK occurrence — outside the PascalCase acceptance grep's intent.)
- `rg "/api/teams/[^/]*/sprints" client/apps/retro` → **zero**. (The 9 Q3-fold-in nested team-scoped frontend calls were renamed to `…/retro-sessions/…`; backend team-routes confirmed at `/api/teams/{teamId}/retro-sessions/…`.)
- Also confirmed: `rg "/api/sprints/" client/apps/retro` → zero (flat frontend routes done).

**4 — EF migration uses `RenameColumn` (confirmed).** `20260620090016_RenameBoardFkSprintIdToRetroSessionId.cs` uses `migrationBuilder.RenameColumn` on `Participants.SprintId`, `Cards.SprintId`, `Cards.ReviewedInSprintId`, `CardGroups.SprintId` (+ matching `RenameIndex`). The FK drop/AddForeignKey pairs are the standard SQLite FK-rename handling (the column data is preserved by `RenameColumn`, not recreated). `has-pending-model-changes` → "No changes" confirms the migration fully expresses the model rename. Carve-out respected: no existing migration or the snapshot edited by hand.

**5 — Builds re-run (not assumed).** `dotnet build SprintRituals.slnx` → **0 errors**, 6 pre-existing NU1903 warnings (unrelated infra). `dotnet test ~Reflekt` → **all green** (Domain 7/7, Persistence 14/14). `npm run build` → **both apps green**, `vue-tsc --build` clean (no type errors from the frontend `sprintId → retroSessionId` interface/param renames).

### Step dispositions

| Step | Disposition | Notes |
|------|-------------|-------|
| 1 — Board FK rename (C# property + DB column + repo methods + EF migration) | Implemented | 4 props renamed on `Card`/`CardGroup`/`Participant`; repo method family renamed (`…BySprintIdAsync` → `…ByRetroSessionIdAsync`, incl. Q2's `VoteRepository.GetParticipantIdsWithVotesForRetroSessionAsync` + call site `AdvancePhaseCheckEndpoint.cs:86`); `.HasColumnName("SprintId")` removed from 3 configs (enabled `RenameColumn` generation); migration uses `RenameColumn`; `has-pending-model-changes` clean; all 26 board-meaning DTOs renamed. Carve-outs untouched. |
| 2 — Flat + team-scoped route family rename | Implemented | 26 board flat routes `/api/sprints/` → `/api/retro-sessions/` (carve-out sprint-context excluded); 26 request/command DTO `SprintId → RetroSessionId`; frontend flat stores + Q3 nested team-scoped 9 calls + interfaces renamed. Both acceptance greps zero. |
| 3 — SignalR + guest-token claim wire strings | Implemented | `SprintHub` → `RetroHub` (class + file + DI `MapHub<RetroHub>`); `JoinSprint`/`LeaveSprint` → `JoinRetro`/`LeaveRetro`; claim `"sprintId"` → `"retroSessionId"` (issue + all read sites); group `sprint:{}` → `retro:{}` (hub + DI timer + 27 broadcast handlers); `"SprintClosed"` + payload key → `"RetroClosed"` + `retroSessionId`; frontend `useSprintHub.ts` lockstep; stale DO-NOT-TOUCH comments removed. Scoped grep `sprint:\{|"sprintId"|"SprintClosed"` in Reflekt.API → zero. 24 non-sprint hub methods untouched. |
| 4 — Migration apply, full grep sweep, build + test verification | Implemented | All three carve-out-scoped greps zero; build green; migration model-changes clean; Reflekt tests green; client builds. Re-verified independently by architect. |

### Notes / non-blocking observations (for the reviewer, not gaps)

- **Plan acceptance-grep over-broadness (Step 3) — not an implementation defect.** The plan's literal Step-3 accept grep includes `JoinSprint|LeaveSprint`, which also matches the **REST** feature names `JoinSprintEndpoint`/`JoinSprintCommand`/`JoinSprintResponse` and the `Features.Participants.JoinSprint` namespace/folder. Those are **internal C# class/namespace names** (not wire-visible; the REST join route was correctly renamed to `/api/retro-sessions/{retroSessionId}/join` in Step 2, and the response is JSON-by-shape). The plan's Step-3 table scoped the rename to the **hub method** `JoinSprint`/`LeaveSprint` only — which *was* renamed to `JoinRetro`/`LeaveRetro`. So the literal accept grep does not return zero, but every **wire string it was meant to catch** (hub method, claim, group, event) is correctly renamed. The residual `JoinSprint*` REST type names are a developer's-call internal-naming matter, deferred — flag for the reviewer to confirm whether RA5-C wants the REST `JoinSprint*` feature folder/types renamed to `JoinRetro*` for consistency (the parent endgame is "complete rename" — this is the one board-meaning *type* family left, though it is internal, not wire). Recorded as architect lesson + a reviewer decision item.
- **camelCase local-var residue** (`claimSprintId`, `parsedSprintId`, `sprintIdClaim`, hub param `int sprintId`) — documented carry-over; functional, not board-FK, outside the PascalCase acceptance grep. Cosmetic follow-up.

**Verdict: PASS**

*Status: COMPLETE — architect, 2026-06-20*

---

## RA5-C — Step 2 Code Review

## Reviewed By
Reviewer agent (nexus:reviewer). Cycle 2/3 verdict 2026-06-20 (re-review against the AMENDED plan with Step 5 "complete rename" expansion); Cycle 1/3 record retained below. All acceptance gates re-run independently; builds, tests, and migration check re-run from scratch.

## Verdict: APPROVED

---

## Cycle 2/3 — Re-review against AMENDED plan (Step 5 complete-rename expansion)

**Context:** After my Cycle-1 APPROVE, Codex round-1 found a real wire break + the user expanded scope to a COMPLETE rename. The plan grew a **Step 5** (board-meaning cleanup of locals/params/strings/comments + structural type/namespace/folder/file renames + the whole frontend store/route/prop/storage-key layer) plus a strengthened `rg -i "sprint"` gate. My Cycle-1 MEDIUM finding (`JoinSprint*` internal type family) is now **resolved** — Step 5b renamed it to `JoinRetro*`. This re-review verifies the expanded scope.

**Re-review verdict: APPROVED.** All amended-plan acceptance gates pass; the round-1 BLOCKER is byte-matched repaired; the carve-out invariant held across the namespace merge; builds/tests/migration all green.

### Round-1 BLOCKER fix verified (byte-match, both sides)

| Side | File:line | Payload key | Result |
|------|-----------|-------------|--------|
| Backend send | `Features/RetroSessions/UpdateColumns/BroadcastColumnConfigUpdatedHandler.cs:14` | `retroSessionId = ev.RetroSession.Id` | ✓ |
| Frontend read | `composables/useRetroHub.ts:147,155` | `payload.retroSessionId` | ✓ |

Both sides use `retroSessionId` — byte-identical. The live column-config update wire contract is intact. The matched key survived the `useSprintHub.ts → useRetroHub.ts` rename. (Old `useSprintHub.ts` confirmed deleted; zero `useSprintHub`/`SprintHub` references remain in `client/apps/retro/src`.)

### Strengthened acceptance gate — PASS

`rg -i "sprint" src/Services/Reflekt client/apps/retro -g '!**/Migrations/**'` piped through the carve-out exclusion (`FokusSprint|GetSprintContext|SprintContext|ISprintAnalyticsService|AnonymizedSprintSummary|SprintSynced|sprint-context`) + the `RetroSession.cs:66` Fokus doc-comment + `@sprint-rituals` package name returns **only Fokus-carve-out survivors**. Every survivor categorized and confirmed legitimate:

| Survivor | Meaning | Disposition |
|----------|---------|-------------|
| `RetroSessionRepository.cs` doc — "the retro's Fokus sprint references" | Fokus-sprint | carve-out OK |
| `RetroSession.cs:66` doc — "skipped-retro covers two sprints" | Fokus-sprint (plan-allowlisted line) | carve-out OK |
| `RetroSessionConfiguration.cs` comment — "Fokus sprint link" | Fokus-sprint | carve-out OK |
| `DbInitializer.cs` comment — "Fokus sprint id [1] … no real Fokus sprint 1" | Fokus-sprint | carve-out OK |
| `DependencyInjection.cs` — `ISprintAnalyticsService`, `Grpc:Services:SprintAnalytics:Url`, `/hubs/sprint taken by Fokus`, `Fokus.SprintHub` | gRPC contract + Fokus's own hub | carve-out OK |
| `CLAUDE.md` — "sprint-retrospective tool", "sprint analytics from Fokus", "sprint data" | module doc describing the Fokus relationship | carve-out OK (doc, not a renameable code identifier) — see LOW note below |
| `@sprint-rituals` workspace package name | npm package name (RA5-A) | sanctioned remainder (coordinator-confirmed) |

Frontend retro app: `rg -i "sprint"` over `client/apps/retro/src` (excl. `@sprint-rituals`) → **zero**. Store `useRetroSessionStore`, storage key `reflekt:retro-session:${retroSessionId}:token`, router paths `/teams/:teamId/retro-sessions/:retroSessionId` + names `team-retro-sessions`/`retro-session`/`retro-room`, guest deep-link `/retro/:retroSessionId` all renamed.

### Structural renames + namespace merge — sound

- `Features/Sprints/` folder **gone**; merged into `Features/RetroSessions/` (folders now `CloseRetro`/`CreateRetro`/`GetRetro`/`GetRetroSummary`/`GetTeamRetros`/`StartRetro`). Zero `Features.Sprints` namespace declarations remain. Build green confirms no broken usings.
- **GetSprintContext carve-out subfolder intact**: `Features/RetroSessions/GetSprintContext/` retains all 3 files with namespace `Reflekt.API.Features.RetroSessions.GetSprintContext` — the merge did not disturb it (the collision the plan flagged was resolved by intentional merge, carve-out preserved).
- Filename-only renames done: `SprintRepository.cs` → `RetroSessionRepository.cs`; `SprintConfiguration.cs` → `RetroSessionConfiguration.cs` (both gone from old names).
- Board-meaning class names all zero: `SprintHub`, `JoinSprint*`, `CloseSprintEndpoint`, `StartSprintEndpoint`, `GetSprintEndpoint`, `GetSprintSummaryEndpoint`, `GetTeamSprintsEndpoint`, `BroadcastSprintClosedHandler`, `GetSprintReportEndpoint`, `SprintRepository`, `SprintConfiguration` → all renamed.
- Carve-out invariant held: `ISprintAnalyticsService`, `AnonymizedSprintSummary`, `SprintSynced`, `FokusSprintSnapshot`, `FokusSprintRef` all present and intact.
- `/join/google` repointed to `/join` (`participant.ts:34`); the Google-assisted-guest-join 400-on-empty-DisplayName is a tracked follow-up, NOT flagged here per scope.

### Cycle-2 Findings

### [LOW] `CLAUDE.md` retains board-flavored product prose ("sprint-retrospective tool")
**File:** `src/Services/Reflekt/CLAUDE.md` (Purpose section)
**Origin:** implementation
**Issue:** The module doc describes Reflekt as a "real-time collaborative sprint-retrospective tool" and references "sprint analytics"/"sprint data". These are module documentation describing the product and the Fokus relationship — not renameable code identifiers, and most uses are genuine Fokus-sprint references. The strengthened gate's scope (`src/Services/Reflekt`) technically includes this file, but the plan's intent targets board-meaning *identifiers*, not narrative docs. Non-blocking.
**Fix (optional follow-up):** A doc-polish pass could reword "sprint-retrospective tool" → "retrospective tool" for consistency. No code impact.
**Confidence:** 85/100

### Cycle-2 Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| .NET build | PASS | `dotnet build SprintRituals.slnx --no-restore` | `Build succeeded. 6 Warning(s) 0 Error(s)` |
| EF pending model changes | PASS | `dotnet ef migrations has-pending-model-changes` | `No changes have been made to the model since the last migration.` (namespace merge did not disturb model) |
| Reflekt Domain tests | PASS | `dotnet test Reflekt.Domain.Tests.csproj --no-build` | `Failed: 0, Passed: 7, Total: 7` |
| Reflekt Persistence tests | PASS | `dotnet test Reflekt.Persistence.Tests.csproj --no-build` | `Failed: 0, Passed: 14, Total: 14` |
| Frontend build | PASS | `npm run build` at `client/` | `vue-tsc --build` clean; retro emits `wwwroot/retro/`; 0 type errors |
| Strengthened `rg -i sprint` gate | PASS | gate cmd excl. carve-outs + `@sprint-rituals` | Only Fokus-carve-out doc/gRPC survivors (table above) |
| Frontend board-meaning sprint | PASS | `rg -i "sprint" client/apps/retro/src` excl. `@sprint-rituals` | Zero |
| Round-1 BLOCKER byte-match | PASS | Read handler:14 + composable:147 | Both `retroSessionId` |
| `Features/Sprints` merge | PASS | `ls` + `rg "Features.Sprints"` | Folder gone; zero namespace decls; GetSprintContext intact |
| Filename-only renames | PASS | `ls SprintRepository.cs / SprintConfiguration.cs` | Both gone (renamed to RetroSession*) |
| Board-meaning class names | PASS | `rg` for 11 board class names | All zero |
| Carve-out invariant | PASS | `rg -c` for 5 carve-out symbols | All present |

*Status: COMPLETE — reviewer, 2026-06-20 (Cycle 2/3)*

---

## Cycle 1/3 — original review (record; superseded by Cycle 2 above)

## Pre-commitment Predictions

| Prediction | Outcome |
|-----------|---------|
| Step-3 accept grep would not return literal-zero due to `JoinSprint*` REST type family — architect pre-flagged as DECISION ITEM | Confirmed — grep matches `JoinSprintEndpoint`, `JoinSprintCommand`, `JoinSprintResponse`, `Features.Participants.JoinSprint` namespace. Wire strings are correct; residue is internal type names only. |
| Carve-out leak: a FokusSprintSnapshot/gRPC identifier wrongly renamed | Refuted — all carve-out symbols verified intact via targeted grep. |
| Migration might use drop/recreate instead of `RenameColumn` | Refuted — `RenameColumn` confirmed for all 4 columns; FK churn is expected SQLite behavior; `has-pending-model-changes` clean. |
| `ColumnConfigUpdated` payload key in frontend might still say `sprintId` | Refuted — `useSprintHub.ts:147` payload field is `retroSessionId`. |
| `DependencyInjection.cs` timer expiry inline send might still use `sprint:{id}` | Refuted — `DependencyInjection.cs:81` confirmed `"retro:{sprintId}"`. |

## Carry-Over Findings (from implementation.md)

| Carry-Over Item | Resolution |
|----------------|-----------|
| camelCase local-var residue (`claimSprintId`, `parsedSprintId`, `sprintIdClaim`, hub `int sprintId` param) | Confirmed functional; confirmed outside PascalCase acceptance grep scope. Not a board-FK identifier. Disposition: cosmetic follow-up accepted. |
| `RetroHub` camelCase param `sprintId` (hub method wire param) | SignalR passes values positionally, not by param name — client is unaffected. Cosmetic. Disposition: accepted. |
| `PreviousActionsContracts.cs` `PreviousSprintId` | Correctly left per carve-out rule — references a Fokus sprint, not the board FK. Disposition: confirmed correct. |
| `AdvancePhaseCheckEndpoint.cs:86` VoteRepository call site | Confirmed updated to `GetParticipantIdsWithVotesForRetroSessionAsync` per Q2 ruling. Disposition: done. |

## Findings

### [MEDIUM] `JoinSprint*` internal REST type/namespace family not renamed

**File:** `src/Services/Reflekt/Reflekt.API/Features/Participants/JoinSprint/JoinSprintEndpoint.cs:7,12,13`; `JoinSprintCommand.cs:3,5,8`; `ParticipantContracts.cs:6`; `client/apps/retro/src/stores/participant.ts:11`
**Origin:** implementation
**Issue:** The hub method rename `JoinSprint` → `JoinRetro` was correctly applied (wire-visible). The REST feature type family retains `Sprint` naming: class `JoinSprintEndpoint`, records `JoinSprintCommand`/`JoinSprintResponse`/`JoinSprintCommandValidator`, folder `Features.Participants.JoinSprint`, C# namespace `Reflekt.API.Features.Participants.JoinSprint`, and the TypeScript interface `JoinSprintResponse` in `participant.ts:11`. The plan endgame is "complete rename — eliminate every sprint-named identifier that actually means the retro board." These identifiers mean the retro board join operation.

**Ruling — deferring is acceptable for this cycle.** These are internal C# class/namespace names, not wire-visible: the route is correctly `/api/retro-sessions/{retroSessionId}/join` (renamed in Step 2); the response is deserialized by JSON shape, not by the TS interface name. No wire string is wrong. The plan's Step-3 table scoped the `JoinSprint` rename to the hub method only; the REST type family is a Step-2 descendant that can follow in a cleanup pass. **Track for next rename/cleanup pass (RA5-B or boy-scout).**
**Fix (follow-up):** Rename folder and namespace to `Features.Participants.JoinRetro`; rename classes to `JoinRetroEndpoint`/`JoinRetroCommand`/`JoinRetroResponse`/`JoinRetroCommandValidator`; update `ParticipantContracts.cs` and `ParticipantMappings.cs`; rename TS interface `JoinSprintResponse` → `JoinRetroResponse` in `participant.ts`.
**Confidence:** 97/100

## Positive Observations

- All wire-string surfaces verified correct: hub methods `JoinRetro`/`LeaveRetro` match client `.invoke('JoinRetro')`; claim `"retroSessionId"` issued in `GuestTokenAuthenticationHandler` and read by all endpoint sites; group `"retro:{}"` in hub + all broadcast handlers + `DependencyInjection.cs:81` timer expiry; `"RetroClosed"` + `retroSessionId` payload in `BroadcastSprintClosedHandler` matched by frontend `.on('RetroClosed')`.
- Carve-out discipline is exemplary: `FokusSprintSnapshot.SprintId`, `FokusSprintRef.FokusSprintId`, gRPC contracts (`ISprintAnalyticsService`, `AnonymizedSprintSummary.SprintId`, `SprintQuery.SprintId`), `GetSprintContext*`, `PreviousSprintId`, `SprintSynced.SprintId` all intact. 125 carve-out occurrences across 24 files untouched.
- Migration `RenameColumn` is correct: data-preserving column rename for all 4 target columns. FK churn (`DropForeignKey`/`AddForeignKey`) is the correct SQLite constraint-rename workaround — column data is preserved by `RenameColumn`. Reversible `Down()` present. `has-pending-model-changes` confirms no schema drift.
- Both acceptance greps return zero: `rg "/api/sprints/"` → zero; `rg "/api/teams/[^/]*/sprints" client/apps/retro` → zero.
- Stale `DO-NOT-TOUCH Q1/Q2` comments in `SetReviewStatusEndpoint.cs` and `GetSprintReportEndpoint.cs` removed as plan required.
- `useSprintHub.ts` hub URL remains `/hubs/retro` (correct from RA5-A Step 4 — not regressed here).

## Gaps

- Manual end-to-end SignalR smoke (join → card create → vote → phase change → close) remains deferred per project no-automated-frontend-suite policy. Same deferred-by-policy item from RA5-A.
- `joinWithGoogle` in `participant.ts:34` calls `/api/retro-sessions/${retroSessionId}/join/google` — correctly renamed, but this endpoint does not exist in the backend (pre-existing dead call, flagged in plan as follow-up). Not introduced by this pass.

## Open Questions

None. No sub-80-confidence findings.

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| .NET build | PASS | `dotnet build SprintRituals.slnx --no-restore` | `Build succeeded. 6 Warning(s) 0 Error(s)` (pre-existing NU1903 only) |
| Reflekt Domain tests | PASS | `dotnet test Reflekt.Domain.Tests.csproj --no-build` | `Failed: 0, Passed: 7, Total: 7` |
| Reflekt Persistence tests | PASS | `dotnet test Reflekt.Persistence.Tests.csproj --no-build` | `Failed: 0, Passed: 14, Total: 14` |
| EF pending model changes | PASS | `dotnet ef migrations has-pending-model-changes` | `No changes have been made to the model since the last migration.` |
| Frontend build | PASS | `npm run build` at `client/` | `vue-tsc --build` clean; retro emits `wwwroot/retro/index.html`; 0 type errors |
| Step 1 SprintId board-meaning grep | PASS | `rg "SprintId" src/Services/Reflekt --type cs -g '!**/Migrations/**'` filtered for carve-outs | Zero board-meaning hits; all remaining occurrences are Fokus-sprint carve-outs or camelCase locals |
| Step 2 flat route grep | PASS | `rg "/api/sprints/" src/Services/Reflekt client/apps/retro` | Zero matches |
| Step 2 team-scoped route grep | PASS | `rg "/api/teams/[^/]*/sprints" client/apps/retro` | Zero matches |
| Step 3 group string grep | PASS | `rg 'sprint:\{' src/Services/Reflekt --type cs` (excl. Migrations) | Zero matches — all group strings use `"retro:{…}"` |
| Step 3 SprintClosed event grep | PASS | `rg '"SprintClosed"' src/Services/Reflekt --type cs` | Zero matches — handler uses `"RetroClosed"` |
| Step 3 class SprintHub grep | PASS | `rg 'class SprintHub' src/Services/Reflekt --type cs` | Zero matches — class is `RetroHub` |
| Step 3 claim key grep | PASS | `rg '"sprintId"' src/Services/Reflekt --type cs` | Zero matches — all claim reads use `"retroSessionId"` |
| Frontend JoinRetro invoke | PASS | Read `useSprintHub.ts:216` | `.invoke('JoinRetro', retroSessionId)` confirmed |
| Frontend RetroClosed on | PASS | Read `useSprintHub.ts:166` | `.on('RetroClosed', …)` confirmed |
| DependencyInjection timer expiry group string | PASS | Read `DependencyInjection.cs:81` | `$"retro:{sprintId}"` confirmed |
| Migration shape | PASS | Read `20260620090016_RenameBoardFkSprintIdToRetroSessionId.cs` | `RenameColumn` for all 4 columns; FK drop/recreate for SQLite constraint update (data preserved) |

*Status: SUPERSEDED by Cycle 2/3 — reviewer, 2026-06-20 (Cycle 1/3 record)*

---

# RA5-B — Sprint-Context Panel UI: Review

## RA5-B — Step 1 Done-Check

**Plan:** `plan-B-panel.md` · **Implementation:** `implementation.md` (RA5-B section, lines 374-459)

**Pre-commitment predictions (before reading implementation against source):** given the critic-folded H1/H2, the three most likely gaps were (1) the button still mounted on a never-rendered host (`PageToolbar`) so AC2 silently fails; (2) the TS types drifting from `GetSprintContextQuery.cs` on a nullable/camelCase field; (3) `sprintContextOpen` smuggled into store state rather than a local view ref. All three were checked at source — none materialized.

**Verify-gate note:** the platform verify gate skipped this run (developer agent is named). Build claims were **re-run by the architect** — `dotnet build` (green, 0 errors) and `npm run build` (both apps green, `vue-tsc --build` clean) — not assumed from implementation.md.

### Required panel-UI checks

**1 — Folded critic findings closed (H1 + H2).**
- **H1 (button host actually mounts):** the "Sprint Context" button lives on `RetroPhaseHeader.vue:47-54` (`@click="$emit('toggle-sprint-context')"`), which `RetroRoomView.vue` renders inside the `v-else` branch (line 23-24) — the `joined === true` arm (reached only after `kicked`/`loading`/`!joined` are ruled out). So it mounts on an actually-rendered host, not the never-mounted `PageToolbar`. AC2/AC4 (button in every phase, incl. guest) holds: no phase or role gate on the button.
- **H2 (hydration locus):** `useRetroRoom.ts:58` calls `sprintContextStore.fetchSprintContext(retroSessionId)` inside `enterRoom()`'s `Promise.all([...])` (line 51-58) — the existing hydration orchestrator, after guest join. The store (`stores/sprintContext.ts`) is **cache-only**: a `fetchSprintContext` action with a `loaded` guard (`if (this.loaded) return`, line 14), no self-`onMounted`. Composable-orchestrates / store-holds split honored.

**2 — Endpoint + carve-out + guest-aware fetch.**
- The panel reads `GET /api/retro-sessions/${retroSessionId}/sprint-context` (`api/sprintContext.ts:9`). The backend carve-out endpoint `GetSprintContextEndpoint.cs:13` is `[HttpGet("/api/retro-sessions/{retroSessionId}/sprint-context")]` — **NOT renamed** (the `sprint-context` segment means a *Fokus* sprint; it survived RA5-C, re-confirmed). Client targets it correctly, byte-for-byte.
- **Guest-aware fetch:** `X-Guest-Token` header set from `useGuestStore().token` when present + `credentials: 'include'` (lines 5-12), matching the `report.ts`/`previousActions.ts` convention (critic M1 closed). 404 → typed error (`{ status: 404 }`) the store can branch on; the store only sets `error` on transport/404, treats any 200 (incl. `cached`/`unavailable`) as success — Flow 3 honored.

**3 — Wire shape field-for-field vs `GetSprintContextQuery.cs` (camelCase) + RAG tokens.**
Verified field-by-field against the shipped DTO — exact camelCase match:

| C# (`GetSprintContextQuery.cs`) | TS (`types/sprintContext.ts`) |
|---|---|
| `SprintContextResponse(Sprints)` | `{ sprints }` |
| `SprintContextSection`: `SprintId/SprintName/StartDate/EndDate/Freshness` | `sprintId/sprintName/startDate/endDate/freshness` |
| `CompletionRate/CompletionRag/DisruptionRate/DisruptionRag/CarryOverRate/CarryOverRag/HealthScore/HealthRag` | matching camelCase, all `?: number\|null` / `string\|null` |
| `AddedMidSprint/AddedMidSprintOverflow` | `addedMidSprint?: ScopeAdditionDto[]\|null` / `addedMidSprintOverflow?` |
| `ScopeAdditionDto(TicketKey, Summary, StoryPoints)` | `{ ticketKey, summary, storyPoints? }` — **no assignee** (Rule 4 structurally held) |

RAG mapping (`RagBadge.vue:15-20`): `"green"→success`, `"amber"→warning`, `"red"→danger`, else `default` — badge tokens (`badge-success/warning/danger`, already in the `main.css` safelist). Plan's "green/amber/red → badge tokens" satisfied.

**4 — `sprintContextOpen` is a local view ref (not store state).** `RetroRoomView.vue:134` `const sprintContextOpen = ref(false)` — local view state, same pattern as `reportingView`. Passed down as `:sprint-context-open` prop and toggled via `@toggle-sprint-context="sprintContextOpen = !sprintContextOpen"`. The store holds only data (`sections/loading/error/loaded`), never panel open/close. Confirmed correct.

**5 — Builds re-run (not assumed).** `dotnet build SprintRituals.slnx` → **0 errors**, 6 pre-existing NU1903 warnings. `npm run build` → **both apps green**, retro 1881 modules transformed, `vue-tsc --build` clean (the new panel SFCs + TS types compile without type errors). (implementation.md line 439 says "9 warnings"; actual is 6 — benign count drift, not load-bearing, same as the RA5-A section.)

### Step dispositions

| Step | Disposition | Notes |
|------|-------------|-------|
| 1 — Types + API module | Implemented | `types/sprintContext.ts` mirrors the DTO field-for-field (camelCase, no assignee); `api/sprintContext.ts` `getSprintContext()` hits the carve-out endpoint with guest-aware fetch + typed 404. Documented deviation: retro app has no `apiFetch` wrapper → raw `fetch` + inline guest headers per `report.ts` (valid — codebase convention; skill prescribes the dashboard pattern). |
| 2 — Pinia store + hydration wire | Implemented | `stores/sprintContext.ts` cache-only with `loaded` guard (Rule 8); fetch wired into `useRetroRoom.enterRoom()` `Promise.all` (H2). Documented deviation: Options API store (not setup store) to match retro app's exclusive Options-API convention (valid). |
| 3 — Panel components + top-bar button | Implemented | `RagBadge.vue` (L4), `SprintContextSectionCard.vue` (L3, null-guards every metric, `addedMidSprint != null` gate, first-3 + "+N more" overflow, cached/unavailable states, no assignee), `SprintContextPanel.vue` (L1, `<Transition>` slide-out not `BaseModal`, multi-sprint header on `sections.length > 1`); button on `RetroPhaseHeader.vue` (H1), every phase/role/guest. |
| 4 — Integration + verification | Implemented | Panel + `sprintContextOpen` local ref wired into `RetroRoomView.vue` as `v-if="joined"` sibling overlay; AC checklist documented (code-inspection + build-verified; manual browser smoke deferred-by-policy — no automated frontend suite). |

### Skill-conformance note (not a gap)

Steps 1-3 carried non-`None` skills (`create-vue-feature`, `pinia-patterns`, `vue-component-architecture`, `tailwind-theme`, `vue-patterns`). implementation.md `## Skills Used` reports the **Skill tool failed to invoke all five** (not registered in this run's available-skills list) — the developer read each `SKILL.md` from the plugin cache and applied the pattern (the levels hierarchy L4/L3/L1, token-based classes, cache-once store are all visibly present in the code). This is a **read-channel substitution**, honestly disclosed, not a skipped skill — the skill *content* was applied, only the Skill-tool invocation channel failed. Two documented pattern deviations (raw `fetch` vs `apiFetch`; Options API vs setup store) are both "match the established retro-app convention" — valid, not violations. Logged as a tooling/skill-gap lesson (the nexus-dotnet frontend skills not being surfaced to a named developer agent is the recurring seam). No conformance gap that blocks the done-check.

### Notes / non-blocking observations (for the reviewer)

- **Manual browser smoke** of the AC checklist (button visible in every phase + as guest, Fokus-down → cached/unavailable rendering, reopen → no second fetch) is deferred-by-policy — the reviewer is the runtime gate. Code inspection confirms each AC's structural mechanism (the `loaded` guard, the `v-if`/`v-else-if` state branches, the no-gate button).
- **`storyPoints` type:** C# `decimal? StoryPoints` ↔ TS `storyPoints?: number | null` — JSON serializes `decimal` as a number, so the TS `number` is correct (System.Text.Json default). Same for the `*Rate` decimals. No precision concern at the display layer (rendered as `Math.round(rate * 100)%`).

**Verdict: PASS**

*Status: COMPLETE — architect, 2026-06-20*

---

## RA5-B — Step 2 Code Review

## Reviewed By
Reviewer agent (nexus:reviewer). Cycle 2/3 verdict 2026-06-20 (re-review after fixes); Cycle 1/3 record retained below. All fixes traced at source; frontend build re-run independently.

## Verdict: APPROVED

The Cycle-1 HIGH (cache not keyed by `retroSessionId`) and both LOWs are fixed correctly, with no regressions. All Cycle-1 positives (wire shape, RAG tokens, guest fetch, states, slide-out, mount gate, hydration) still hold.

---

## Cycle 2/3 — Re-review of fixes

**Re-review verdict: APPROVED.** Three fixes verified at source + a behavioral trace of the multi-retro path; frontend build green.

### HIGH fix — cache now keyed by `retroSessionId` (verified)

`stores/sprintContext.ts:11-28`:
- `loadedFor: number | null` replaces the bare `loaded` boolean (line 11).
- Guard `if (this.loadedFor === retroSessionId) return` (line 15) — same-retro reopen still triggers exactly one pull (Rule 8 / AC honored).
- `loadedFor` set **only on success** (line 22, inside `try` after the await) — a failed fetch leaves `loadedFor` at its prior value (≠ the failed id), so re-entry retries.
- `sections` cleared at fetch entry (line 18, before the await) — combined with `loading = true` (line 16), the panel shows the loading state during the B-fetch, never A's stale sections.

**Behavioral trace (the path the bug was on):**
- A→B: `enterRoom(B)` → `fetchSprintContext(B)` → `loadedFor===A` ≠ B → proceeds → `sections=[]` + `loading=true` → on success `sections=B`, `loadedFor=B`. **No stale-flash** (loading state covers the gap; sections cleared synchronously before await).
- B-fetch failure: `catch` sets `error`, leaves `loadedFor=A` (≠B). Panel shows error, sections empty. Re-entering B re-runs `enterRoom(B)` → guard `A≠B` → **retries**. Correct.
- No lingering readers of the old `.loaded` field on this store (`rg` confirms the only `.loaded` hit is the unrelated `authStore.loaded`).

### LOW fixes — verified

- **Unused prop removed:** `SprintContextPanel.vue:69-71` declares only `open` now; `RetroRoomView.vue:72-76` no longer passes `:retro-session-id`. No `retroSessionId` reference remains in the panel.
- **Button a11y:** `RetroPhaseHeader.vue:48-54` — `type="button"` (line 49) + `:aria-pressed="sprintContextOpen"` (line 53) added. Toggle state now exposed to assistive tech; the close button's `aria-label` was already correct.

### Cycle-2 Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Cache keyed by retroSessionId | PASS | Read `stores/sprintContext.ts:11-28` | `loadedFor` guard; set on success only; sections cleared at entry |
| No stale `.loaded` readers | PASS | `rg "\.loaded\b"` over store/panel/room/view | Only unrelated `authStore.loaded` |
| Unused prop removed | PASS | Read `SprintContextPanel.vue:69-71` + `RetroRoomView.vue:72-76` | `open` only; no `:retro-session-id` |
| Button a11y | PASS | Read `RetroPhaseHeader.vue:48-54` | `type="button"` + `:aria-pressed="sprintContextOpen"` |
| Frontend build | PASS | `npm run build` at `client/` | `vue-tsc --build` clean; retro 1881 modules; emits `wwwroot/retro/`; 0 type errors |

### Gaps (unchanged from Cycle 1, non-blocking)
- Manual browser smoke of the AC checklist (incl. the now-fixed multi-retro nav: A→B shows B's context, no stale-flash) remains deferred-by-policy — no automated frontend suite, reviewer/operator is the runtime gate. The fix makes the multi-retro path correct by construction; a smoke would confirm it end-to-end.

*Status: COMPLETE — reviewer, 2026-06-20 (Cycle 2/3)*

---

## Cycle 1/3 — original review (record; superseded by Cycle 2 above)

## Pre-commitment Predictions

| Prediction | Outcome |
|-----------|---------|
| TS types drift from `GetSprintContextQuery.cs` on a nullable/camelCase field | Refuted — field-for-field exact match, all nullables optional. |
| RAG green/amber/red → badge tokens inverted | Refuted — `green→success`, `amber→warning`, `red→danger`, `default` fallback. Correct, not inverted. |
| `addedMidSprint` first-3 + overflow logic wrong | Refuted — `slice(0, 3)` + `addedMidSprintOverflow` "+N more"; matches Rule 7 (ordering is backend-owned, deferred to RA5-D, transparent to panel). |
| `loaded` guard satisfies "fetch once" but loses per-`retroSessionId` keying | **Confirmed — HIGH finding below.** The done-check verified the guard against Rule 8 "fetch once" but not against the plan's "keyed by `retroSessionId`" clause. |
| Button mounted on a never-rendered host / phase-gated | Refuted — on `RetroPhaseHeader` (renders every phase), no phase/role gate. |

## Findings

### [HIGH] Session cache is not keyed by `retroSessionId` — second retro shows the first retro's context
**File:** `client/apps/retro/src/stores/sprintContext.ts:13-20`
**Origin:** implementation
**Issue:** The store's fetch-once guard is a bare boolean `loaded`, not keyed by `retroSessionId`. The plan Step 2 instruction is explicit: *"fetch the context at most once per retro session (Rule 8) and cache it for the session, **keyed by `retroSessionId`**"*. The implementation dropped the keying.

The `sprintContext` store is a Pinia **singleton** (app-global, survives component remount) and is **never reset** (`rg "\$reset|loaded = false"` over `client/apps/retro/src` → zero). Critically, it is the **only** room-hydration store with a permanent fetch-once guard — every sibling store (`card`, `cardGroup`, `participant`, `report`, `previousActions`) re-fetches unconditionally in `enterRoom()`, so they self-correct on re-entry. `sprintContext` will not:

1. User opens retro A → `enterRoom(A)` → `fetchSprintContext(A)` sets `loaded = true`, caches A's `sections`.
2. User navigates to retro B (`/retro/:retroSessionId` deep-link or `router.push({ name: 'retro-session' })` from the list — both mount `RetroRoomView`) → `enterRoom(B)` → `fetchSprintContext(B)` hits `if (this.loaded) return` and **no-ops**.
3. The panel for retro B renders the cached **retro-A** `sections` — wrong sprint metrics for the retro the user is in.

This is a cross-retro data-correctness bug (silent wrong data, not an error), and it directly contradicts the plan mandate + the spec's privacy/accuracy intent (showing one retro's Fokus metrics under another retro's header). Reachable in normal SPA navigation: switching between two retros without a hard page reload is a first-class flow (the retro list → retro detail → room path, and guest tokens are per-retro).
**Fix:** Key the guard by the requested id. Minimal change in `fetchSprintContext`:
```ts
async fetchSprintContext(retroSessionId: number) {
  if (this.loadedFor === retroSessionId) return   // replace the bare `loaded` flag
  this.loading = true; this.error = null
  try {
    const response = await getSprintContext(retroSessionId)
    this.sections = response.sprints
    this.loadedFor = retroSessionId
  } catch (e) { this.error = ...; this.loadedFor = null }
  finally { this.loading = false }
}
```
(add `loadedFor: number | null` to state, drop `loaded`; update `SprintContextPanel`'s no-op reads if any). Reopening the *same* retro's panel still triggers exactly one pull (Rule 8 / AC honored); entering a *different* retro re-fetches. Alternatively, `$reset()` the store on room unmount in `useRetroRoom.onUnmounted`, but per-id keying is the plan's stated design and is more robust.
**Confidence:** 92/100

### [LOW] `SprintContextPanel` declares `retroSessionId` prop but never uses it
**File:** `client/apps/retro/src/components/retro/SprintContextPanel.vue:71`; passed at `RetroRoomView.vue:75`
**Origin:** implementation
**Issue:** The panel takes a `retroSessionId` prop but never reads it — the fetch is orchestrated by `useRetroRoom.enterRoom()`, and the panel reads `useSprintContextStore()` directly. Dead prop. (It would become *used* if the keyed-fetch fix moved a guard into the panel, but as written it is unused.)
**Fix:** Remove the prop (and the `:retro-session-id` binding) unless the HIGH fix repurposes it. Non-blocking.
**Confidence:** 88/100

### [LOW] Sprint Context toggle button has no `aria-pressed` / `type="button"`
**File:** `client/apps/retro/src/components/retro/RetroPhaseHeader.vue:48-55`
**Origin:** implementation
**Issue:** The button is a real toggle (open/closed, reflected visually via the `sprintContextOpen` class swap) but exposes no `aria-pressed` state to assistive tech, and omits `type="button"`. The visible text label "Sprint Context" + `title` tooltip satisfy *labeling*, so this is a minor a11y polish, not a labeling failure. (No enclosing `<form>`, so the missing `type` is harmless here.)
**Fix:** Add `type="button"` and `:aria-pressed="sprintContextOpen"`. The close button (`SprintContextPanel.vue:19`) already has a correct `aria-label` — good. Non-blocking.
**Confidence:** 85/100

## Positive Observations
- **Wire shape is exact.** `types/sprintContext.ts` matches `GetSprintContextQuery.cs` field-for-field in camelCase, including top-level `Sprints`→`sprints` and every nullable metric as optional. No JSON naming-policy override exists in Host/Reflekt, so default camelCase serialization holds the contract. `ScopeAdditionDto` has no assignee field — Rule 4 privacy structurally guaranteed end-to-end.
- **RAG mapping correct and not inverted:** `green→success→badge-success`, `amber→warning→badge-warning`, `red→danger→badge-danger`, `default` for null/unknown. Per-field RagBadge gated on the rag string being present.
- **Metric/state gating sound:** metrics grid suppressed for `unavailable`; each rate null-guarded (shows "—"); cached badge + tooltip on `freshness === 'cached'`; `addedMidSprint != null` gate; first-3 (`slice(0,3)`) + `addedMidSprintOverflow` "+N more" — matches Rule 6/7. Health score displayed raw (Rule 3 — never recomputed).
- **Tooltip copy byte-matches `help.tooltips.md`** for all 10 elements (button, multi-sprint header, 4 metrics, added-mid-sprint, +N more, cached, unavailable).
- **Mount gate + hydration sound:** button on `RetroPhaseHeader` (every phase, no role gate — Rule 1/Flow 4); panel is a `v-if="joined"` **sibling** overlay (not nested in the joined block); fetch in `enterRoom()`'s `Promise.all` after join; `loaded` guard prevents double-fetch within a single retro entry. `sprintContextOpen` is a local view `ref` (not store state) — correct local-ref toggle.
- **Guest-aware fetch** (`X-Guest-Token` + `credentials: 'include'`) matches `report.ts`; endpoint is `[AllowAnonymous]` so the header is harmless and convention-uniform; 404 → typed error; any 200 (incl. cached/unavailable) → success, error only on transport/404 (Flow 3).
- **Slide-out** uses `<Transition>` with `translate-x-full`↔`translate-x-0` (not `BaseModal`, per plan); right-aligned `fixed inset-y-0 right-0`, z-40.

## Gaps
- Manual browser smoke of the AC checklist (button in every phase/as guest; Fokus-down cached/unavailable rendering; reopen → no second fetch) remains deferred-by-policy (no automated frontend suite). Note the HIGH bug means the "reopen → no second fetch" AC passes for the *same* retro but the cache is *wrong* across *different* retros — a manual smoke that only reopens within one retro would not surface it; the multi-retro path must be smoked after the fix.
- Initial-fetch failure leaves `loaded=false` (or `loadedFor=null` after the fix) and shows the error state with no in-panel retry — acceptable (a fresh room entry retries), not a finding.

## Open Questions
None — all findings ≥80 confidence.

## Evidence
| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Frontend build | PASS | `npm run build` at `client/` | `vue-tsc --build` clean; retro 1881 modules; emits `wwwroot/retro/`; 0 type errors |
| Wire shape vs DTO | PASS | Read `types/sprintContext.ts` vs `GetSprintContextQuery.cs` | Field-for-field camelCase match; no assignee |
| Endpoint route match | PASS | Read `api/sprintContext.ts:9` vs `GetSprintContextEndpoint.cs:13` | `/api/retro-sessions/{retroSessionId}/sprint-context` byte-match; `[AllowAnonymous]` |
| JSON casing override | PASS (none) | `rg "PropertyNamingPolicy\|JsonSerializerOptions"` Host + Reflekt | Zero — default camelCase holds |
| RAG token mapping | PASS | Read `RagBadge.vue:15-20` + `BaseBadge.vue:21-28` | green→success, amber→warning, red→danger, default fallback — not inverted |
| Tooltip copy | PASS | Diff components vs `help.tooltips.md` | All 10 strings byte-match |
| Cache keyed by retroSessionId | **FAIL** | Read `stores/sprintContext.ts:13-20`; `rg "\$reset\|loaded = false"` → zero | Bare `loaded` boolean, never reset, not keyed by id — HIGH finding |
| Mount gate / hydration | PASS | Read `RetroRoomView.vue:23,72-77`, `useRetroRoom.ts:51-67` | Button in joined arm; panel `v-if="joined"` sibling; fetch in `enterRoom` Promise.all |
| Sibling stores re-fetch pattern | PASS | `rg "loaded\|return"` across stores | sprintContext is the ONLY store with a permanent fetch-once guard (others self-correct) |

*Status: SUPERSEDED by Cycle 2/3 — reviewer, 2026-06-20 (Cycle 1/3 record)*
