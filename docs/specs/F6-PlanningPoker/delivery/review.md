# Review — F6-PlanningPoker

## Step 1 — Done-Check

**Verified independently** (the SubagentStop verify-gate SKIPPED on every developer stop — `unrecognized agent_type: developer-f6` — so `blocking_failed:false` meant "not run," not "passed"). The architect re-ran the full build, all test projects, and the client build firsthand; build/test claims below are first-hand, not taken on trust.

### Independent verification (firsthand, this done-check)

| Check | Result | Command | Evidence |
|-------|--------|---------|----------|
| .NET solution build | **green — 0 errors** | `dotnet build SprintRituals.slnx` | 0 Error(s), 36 NU1903 vuln warnings only (pre-existing, not compile errors per project-rules.md) |
| .NET tests (all 6 projects) | **177 passed, 0 failed, 0 skipped** | `dotnet test SprintRituals.slnx --no-build` | Identity.API 5 · Poker.Domain 26 · Reflekt.Domain 59 · Fokus.Domain 62 · Reflekt.Persistence 19 · Fokus.API 6 = 177 |
| Client build (3 SPAs) | **green** | `npm run build` (client/) | dashboard ✓ · retro ✓ · **poker ✓** (`vue-tsc --build && vite build`, 1812 modules, no TS errors) |

The dev's reported counts (177 .NET; 26 Poker.Domain + 5 Identity; all 3 client apps) reproduce exactly.

### Step dispositions (against actual on-disk code)

| Step | Disposition | Notes |
|------|-------------|-------|
| 1 — Scaffold Poker module | **Implemented (one Trivial doc sub-clause deviated)** | 4 projects exist; Poker.API is a class library with `<FrameworkReference Include="Microsoft.AspNetCore.App"/>` (not Web SDK); **zero cross-module project refs** (no Identity/Fokus/Reflekt). `CLAUDE.md` ✓, `SprintRituals.slnx` ✓. **Deviated (valid reason):** plan acceptance said "add the row to `docs/conventions/project-rules.md` (the `{AA}XY` table)" — that table **does not exist** in the file (CLAUDE.md references it but it was never authored here). No row could be added to a non-existent table; the substantive port/DB wiring landed instead in Step 9 (`PokerDatabase` conn string + `PokerFokus` gRPC URL → existing 5001). LOW/Trivial; breaks no build/test/AC. See Gap below. |
| 2 — Poker domain model | **Implemented** | `PlanningSession : AggregateRoot<Guid>`; `SessionParticipant`/`EstimationItem`/`Vote`; `FibonacciScale` = {1,2,3,5,8,13,21,?}. Rule 6 guard (`'?'` not final) + Rule 8 guard (value must be cast) in `EstimationItem.Decide`; Rule 21 upsert in `CastVote`; Rule 19 idempotent `AddSprintBoundItem`; 5 domain events. 26 TDD tests green. |
| 3 — Poker persistence | **Implemented** | `PokerDbContext` → `poker.db`; 4 entity configs; migration `20260621100029_InitialPokerSchema` (4 tables); filtered-unique `{SessionId, TicketKey} WHERE TicketKey IS NOT NULL` (Rule 19) + unique `{ItemId, ParticipantId}` (Rule 21) both present in configs and migration. |
| 4 — Jira write + 3 Fokus gRPC | **Implemented** | **(highest-stakes, all hold)** `IJiraClient.SetStoryPointsAsync` + `IJiraApi [Put("/rest/api/3/issue/{issueIdOrKey}")]`; `UpdateIssueFieldsCommand` serializes **`customfield_10016`** (NOT `story_points` — corrected deviation landed); non-generic `RequestAsync` overload returns on success without dereferencing `.Content` (204 NPE avoided); `TeamManagedJiraClient` does **not** override the write (inherits base). `IPokerFokusService` (3 ops); `GetAnalyticsSprintsAsync` filter **unchanged** (`Closed||Active`) + **separate** `GetPokerSprintsAsync` adds Future; `GetSprintTicketsAsync` calls `IJiraClient.GetSprintIssuesAsync` **live, no cache**. |
| 5 — REST endpoints | **Implemented** | All 8 slices present with correct routes: `POST /api/poker/sessions`, `GET .../{id}`, `POST .../participants`, `POST .../close`, `GET .../summary`, `GET /api/poker/sprints`, `GET .../tickets`, `POST .../items`. |
| 6 — Poker-local guest auth + SM gate + Identity touch | **Implemented** | Scheme name `"PokerGuestToken"` (distinct from Reflekt's `"GuestToken"`); `ActiveUserGateScope` promoted to an `IReadOnlySet` excluding **both** `"Reflekt.API"` and `"Poker.API"`; Identity test `Should_ExcludeFromGate_When_EndpointDeclaredInPokerApiAssembly` added (5 Identity tests green). The planned Identity cross-module touch was made and reconciled. |
| 7 — PlanningHub + estimation loop | **Implemented** | `DecideEstimate` (Sprint-bound) calls write-back **first**, only calls `session.DecideEstimate` on success, broadcasts `WriteBackFailed` and returns on failure (Rule 10 sequencing correct); `VoteReceived` broadcast carries `{participantId, itemId}` only — **no value** (Rule 7); auto-reveal-on-disconnect + SM gate via `AuthenticateAsync` present. |
| 8 — Broadcast handlers | **Implemented** | 5 `IEventHandler<T>` broadcast handlers under `Features/Sessions/EstimationEvents/` (ItemSelected, VotesRevealed, EstimateDecided, ItemSkipped, SessionClosed). |
| 9 — Host wiring | **Implemented** | `AddPokerModule`, Poker assembly in FastEndpoints `o.Assemblies`, `Migrate<PokerDbContext>()`, `MapPokerEndpoints`, `/poker` static + SPA fallback, `Grpc:Services:PokerFokus:Url`, `PokerDatabase` conn string, Host.csproj → Poker.API. No MassTransit consumer (correct — none needed). |
| 10 — Vue planning room | **Implemented** | `client/apps/poker/` SPA: router → planning-room view, `planningSession` Pinia store, `usePlanningHub` → `/hubs/poker`, guest store key `poker:session:{id}:token`, root `package.json` build script includes `apps/poker`. Poker SPA builds green. |
| 11 — KB entries | **Implemented** | `docs/kb/jira-story-points-write-back.md` + `docs/kb/poker-sprint-tickets-freshness.md` exist; `docs/kb/index.md` has both rows (status verified). |

### Gap (non-blocking, for follow-up — not a FAIL condition)

- **Step 1 doc sub-clause — `project-rules.md` port/DB row not added.** The plan's acceptance pointed at a `{AA}XY` table in `docs/conventions/project-rules.md` that **does not exist** in the file (the CLAUDE.md cites "See `docs/conventions/project-rules.md` for full table," but the table itself was never authored there). The developer could not add a row to a non-existent table; this is a **plan instruction pointing at a stale artifact**, not a developer omission. Origin: **design** (plan/doc), not implementation. Impact: documentation only — no build/test/AC effect; the actual port/DB wiring is correct and present (Step 9). Routes to a follow-up doc fix (author the port/DB convention table, then add Poker's row), not back to the developer this cycle.

**Verdict: PASS**

All 11 plan steps are Implemented against actual code; the three highest-stakes correctness checks (`customfield_10016` not `story_points`; analytics filter unchanged with a separate Poker query; write-back-before-Decided + vote privacy) all hold. Build, 177 tests, and all 3 client SPAs are green — verified firsthand, not taken on trust. The single gap is a Trivial documentation sub-clause whose root is a stale plan pointer (design-origin), breaks nothing, and is dispositioned as a non-blocking follow-up. No step is Missing.

*Status: COMPLETE — architect, 2026-06-21*

---

## Step 2 — Code Review

**Verdict: APPROVED** (Cycle 3/3)

All findings from cycles 1 and 2 resolved. Build, tests, and client build verified firsthand.

### Evidence (Cycle 3 — firsthand, this re-review)

| Check | Command | Result |
|-------|---------|--------|
| .NET build | `dotnet build SprintRituals.slnx` | **0 errors**, 36 NU1903 vuln warnings only (pre-existing) |
| .NET tests | `dotnet test SprintRituals.slnx --no-build --verbosity quiet` | **177 passed, 0 failed** — Poker.Domain 26 · Identity.API 5 · Reflekt.Domain 59 · Reflekt.Persistence 19 · Fokus.Domain 62 · Fokus.API 6 |
| Client build | `npm run build` (client/) | **green** — poker ✓ (1812 modules, 0 TS errors, 192 kB JS bundle) |

---

### Carry-Over Findings (Cycles 1–2 — disposition)

| Finding | Status |
|---------|--------|
| F1 — Cookie-auth users cannot vote | **FIXED** — `ResolveParticipantIdAsync` present; PersonId lookup path verified. `PlanningHub.cs:249–263`. |
| F2 — GuestToken leaked via anonymous GET | **FIXED** — `GuestToken` absent from `ParticipantResponse`; `JoinSessionResponse` carries it exactly once. `SessionContracts.cs:14–29`. |
| F3 — isScrumMaster always true | **FIXED** — compares `authStore.user.id === session.createdByPersonId`. `PlanningRoomView.vue:157–162`. |
| F4 — Two DB round-trips | **FIXED** — single `ToListAsync`, in-memory split. `SprintRepository.cs:29–50`. |
| F5 — Unused GuestToken for auth users | **FIXED** — `GuestToken = string.Empty` for authenticated participants. `SessionParticipant.cs:46`. |
| F6 — ChangeVote missing comment | **FIXED** — XML doc comment added. `PlanningHub.cs:128–134`. |
| F7 — Client join URL does not match server route | **FIXED** — `sessions.ts:44` now POSTs to `/api/poker/sessions/${sessionId}/participants`; matches `JoinSessionEndpoint.cs:15`. |

---

### Findings (Cycle 3)

None. No regressions introduced by the F7 one-line fix.

---

*Reviewer: reviewer-F6, 2026-06-21 (Cycle 3/3)*
