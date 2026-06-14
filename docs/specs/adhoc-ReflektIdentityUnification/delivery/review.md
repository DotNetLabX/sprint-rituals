# adhoc-ReflektIdentityUnification (RA2) — Review

## Step 1 — Done-Check

**Reviewed by:** architect (independent verification — every step re-verified against working-tree source, not implementation.md claims). Date: 2026-06-14.

**Pre-commitment predictions (made before reading implementation.md):** the 3 likeliest gaps for a 31-call-site rename + 3 hand-edited helper-bypass sites + a wire-stability carve-out + an FK-dropping migration were: (1) a bypass site silently surviving with `NameIdentifier` (value-space bug), (2) the migration renaming the column but not dropping both `HasOne<User>()` FKs / the old unique index, (3) the wire DTO `JoinSprintResponse.UserId` breaking. All three were checked directly — none materialized.

### Per-step dispositions

| Step | Disposition | Notes |
|------|-------------|-------|
| 1 — Remove Reflekt `User` aggregate/repo/config/DbSet + seed-`User` + 2 `HasOne<User>()` FK configs + 4 stub endpoints + dead `JoinWithGoogle`/`GetByUserAndSprintAsync`; keep `AuthenticationConfiguration.cs` as-is | Implemented | `User.cs`, `UserRepository.cs`, `UserConfiguration.cs` deleted (git status: `D`); `DbSet<User>` gone from `ReflektDbContext`; `HasOne<User>()` removed from both `TeamConfiguration` and `ParticipantConfiguration`; seed-`User` block gone from `DbInitializer`; `Features/Auth/{Login,Logout,Me}` + `JoinSprintGoogle` deleted; `JoinWithGoogle`/`GetByUserAndSprintAsync` deleted. **`AuthenticationConfiguration.cs` confirmed ABSENT from the diff** (`git diff --name-only` → no match) — correctly unchanged. Live-source grep for `Reflekt.Domain.Users`/`HasOne<User>`/`DbSet<User>`/`JoinWithGoogle`/`GetByUserAndSprintAsync` (excluding historical `Migrations/`) → **zero**. |
| 2 — Add `person_id` claim to Identity cookie | Implemented | `AuthConfiguration.cs` diff = `+1` line: `new("person_id", user.PersonId.ToString())` added to the `SignInAsync` claims list; existing `NameIdentifier`/`Email`/`Name`/`Role` claims unchanged (additive, no wire break). |
| 3 — Repoint identity read: `Participant.UserId`→`PersonId`, `GetUserId`→`GetPersonId`, **3 helper-bypass repoints**, wire-stable `JoinSprintResponse.UserId` | Implemented | **All four grep-acceptances pass exactly as stated:** `ClaimTypes.NameIdentifier` in `src/Services/Reflekt` → **zero**; `GetUserId` → **zero**; `.UserId` → only the 2 sanctioned wire carve-outs (a comment + the Mapster `dest.UserId` target); `ClaimTypes.Email` → **one** (`CreateCardEndpoint:59`). The 31 endpoint call sites flipped to `GetPersonId()` (diff shows 31 single-line `-/+` endpoint edits). **All three bypass sites verified individually (the critical checks):** `SprintHub.cs:204/207` reads `person_id`, compares Person-id ↔ `team.OwnerId` (`:222`); `GetSprintSummaryEndpoint.cs:28` reads `person_id`, `:32` compares Person-id ↔ Person-id (CRITICAL owner comparison — correct); `EditCard:62`/`DeleteCard:64` presence-checks read `person_id`. `JoinSprintResponse` record member stays literally `UserId`, sourced from `Participant.PersonId` via explicit Mapster `.Map(dest => dest.UserId, src => src.PersonId)` — wire JSON unchanged. |
| 4 — Scope `ActiveUserPreProcessor` off Reflekt via assembly-of-declaring-endpoint (Q4-A) | Implemented | Mechanism is **(A) assembly-of-declaring-endpoint** as required — `httpContext.GetEndpoint()?.Metadata.GetMetadata<EndpointDefinition>()?.EndpointType.Assembly.GetName().Name` compared to `"Reflekt.API"`. **Not** scheme-based (the rejected mechanism is absent). Early-return placed BEFORE the user lookup (`ActiveUserPreProcessor.cs:19-23`, ahead of the `IsAuthenticated`/`NameIdentifier` checks) — an authenticated-but-not-a-User Reflekt request is not 401'd. `IsExcludedModule(null)` → not excluded (fail-safe, gate still applies). Discrimination by assembly *simple name* (string) keeps Identity decoupled from Reflekt — a valid, sanctioned variant of Q4-A (CLAUDE.md forbids cross-module project refs, so `typeof(marker).Assembly` is not available); behavior is identical. **TDD:** `ActiveUserGateScope` is the pure predicate, 4 tests in `Identity.API.Tests`. |
| 5 — Keep Reflekt authorization policies on shared cookie + guest scheme (verify) | Implemented (N/A — verify-only) | No code change (plan: `None (verify)`). implementation.md documents the verification: `ScrumMaster`/`GoogleUser` → Cookie scheme, `Participant` → GuestToken, `OwnerOrScrumMaster` → both; Host registers Identity cookie (`:16`) before Reflekt policies (`:18`). `GoogleUser` noted as pre-existing unused policy (low Carry-Over) — not introduced by RA2. |
| 6 — Join flow: extend single `JoinSprintEndpoint` + `Participant.Join(…, personId?)`; leave gate seam | Implemented | `Participant.Join(string, int, int? personId = null)` — single factory, one nullable param, sets `PersonId`, seam comment present (ADR-021). No separate `JoinWithPerson`/`JoinWithGoogle` resurrected. `JoinSprintEndpoint` kept `[AllowAnonymous]`, reads `User.GetPersonIdOrNull()`, single `Join(...)` call. `GetPersonIdOrNull()` added (nullable sibling, never throws). **TDD:** 2 tests in `Reflekt.Domain.Tests` (personId set / null). |
| 7 — Seed-owner sentinel + migration (`Users` drop, FK drops, column+index rename) + green build | Implemented (Follow `persistence-patterns`) | `SeedOwnerPersonId = 1` sentinel + `Team.Create("Dev Team", SeedOwnerPersonId)` (introduced in Step 1 for compile coupling — documented deviation, sequencing only). Migration `20260614200041_RemoveReflektUserUnifyIdentity` **Up** drops `FK_Participants_Users_UserId` + `FK_Teams_Users_OwnerId` + the `Users` table + the `{SprintId,UserId}` unique index; renames column `UserId`→`PersonId`; recreates `IX_Participants_SprintId_PersonId` unique with filter `"PersonId" IS NOT NULL`. **Down** fully reverses. Model snapshot regenerated — no `User` entity / `Users` table (grep → zero). `ParticipantConfiguration`/`TeamConfiguration` match the migration. Skill `persistence-patterns` invoked (audit log). |

### Independent evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Build | **PASS** | `dotnet build SprintRituals.slnx` | `Build succeeded. 8 Warning(s) 0 Error(s)` — `error CS` count = **0**. The 8 warnings are all pre-existing `NU1903` (`System.Security.Cryptography.Xml` transitive advisory), correctly logged as a low Carry-Over, not introduced by RA2. |
| Tests | **PASS — 68/68** | `dotnet test SprintRituals.slnx --no-build` | Identity.API.Tests 4/4, Reflekt.Domain.Tests 2/2, Fokus.Domain.Tests 62/62 = **68 passed, 0 failed, 0 skipped**. Developer's 68/68 claim independently confirmed. |
| Grep: `ClaimTypes.NameIdentifier` in Reflekt | **PASS — zero** | `grep -rn "ClaimTypes.NameIdentifier" src/Services/Reflekt` | exit 1 (no hits). All 5 sites repointed. |
| Grep: `GetUserId` in Reflekt | **PASS — zero** | `grep -rn "GetUserId" src/Services/Reflekt` | exit 1 (no hits). Helper renamed; 31 call sites flipped. |
| Grep: `.UserId` in Reflekt | **PASS** | `grep -rn "\.UserId" src/Services/Reflekt` | 2 hits, both sanctioned wire carve-outs (`ParticipantContracts.cs:5` comment + `ParticipantMappings.cs:13` `dest.UserId` target). Zero in live logic. |
| Grep: `ClaimTypes.Email` in Reflekt | **PASS — one** | `grep -rn "ClaimTypes.Email" src/Services/Reflekt` | `CreateCardEndpoint.cs:59` — the sanctioned presence-check, a different claim. |
| Auth file unchanged | **PASS** | `git diff --name-only \| grep AuthenticationConfiguration` | no match — `AuthenticationConfiguration.cs` is not in the diff (correctly kept guest-scheme-only from RA1). |
| Skill conformance | **PASS** | `.claude/audit/skill-invocations.log` (token `developer:implement`, agent `developer`) | `tdd`, `persistence-patterns`, `boy-scout` all logged in the implement phase. Plan's non-`None` mappings (Step 4 `tdd`, Step 6 `tdd`, Step 7 `persistence-patterns`) all present. Steps 1/2/3/5 are `Skill: None` — none owed. `## Skills Used` section present and corroborates. |

### Deviations — all valid

- **First green build after Step 3, not Step 1.** Deleting the `User` type makes `ParticipantConfiguration` uncompilable until Step 3 repoints it; Steps 1+2+3 land together for the first full build. No plan *content* changed — build-checkpoint cadence dictated by compiler coupling. Each step's edits recorded separately. Valid.
- **`SeedOwnerPersonId` introduced in Step 1 not Step 7.** `Team.Create(..., user.Id)` cannot compile once the seed-`User` block is removed in Step 1. Functionally identical to Step 7's intent; sequencing only. Step 7's migration work done in full. Valid.
- **Step 4 TDD subject is the pure predicate, not a `WebApplicationFactory` integration test.** Repo has no web/integration harness; `testing.md` lists endpoint routing under skip-tests. The risk-bearing discrimination logic (`IsExcludedModule`) is fully unit-tested; the HTTP plumbing is compiler-verified. Mechanism (Q4-A) unchanged. Valid — flagged as a medium Carry-Over for the reviewer (add 2 integration tests if a web harness is later added).
- **Local-variable renames beyond the plan's letter (boy-scout, in-file).** Owner-comparison locals `userId`→`personId` etc. in the 3 value-space-critical files. In-file clarity, no behavior change. Valid.

### Carry-overs for the reviewer

- **Medium:** Step 4 guest-not-401 / inactive-still-401 behavior is covered only by the unit predicate, not an end-to-end integration test (none exists in repo). Mechanism + wiring confirmed by build; only the automated HTTP-pipeline assertion is deferred. Reviewer should sanity-check the pre-processor wiring (`GetEndpoint() → EndpointDefinition.EndpointType.Assembly`) and the acceptance-run path.
- **Low (pre-existing, not RA2):** ~27 endpoints retain a local named `userId` holding a Person id (cosmetic); `GoogleUser` policy defined-but-unused; Reflekt Migrations folder retains old RA0/RA1 migration files; `NU1903` transitive advisory.

**Verdict: PASS** — all 7 steps Implemented (Step 5 verify-only, 3 documented valid deviations). Build green (0 `error CS`), 68/68 tests pass, all four grep-acceptances hold, the three critical bypass repoints (incl. the CRITICAL `GetSprintSummary` owner comparison) are correct, the wire DTO is stable, the auth file is unchanged, the migration drops both FKs + the old index, Step-4 scoping uses mechanism (A), and skill conformance holds against the audit log.

*Status: COMPLETE — architect, 2026-06-14*

---

## Step 2 — Code Review

## Reviewed By
reviewer (nexus:reviewer) — independent code review against the working-tree diff. Every load-bearing file read from source; build + tests + grep-acceptances re-run this session. Conventions index `docs/conventions/coding-conventions.md` + referenced files (`csharp.md`, `ef-core.md`, `testing.md`; `vue.md`/`mutation-testing.md` n/a) and `project-rules.md` loaded.

## Verdict: APPROVED

Highest-severity finding present is **Low**. No findings at either blocking tier. (Severity words elsewhere in this section describe predictions that did NOT materialize and positive confirmations of high-impact sites that are CORRECT — not open findings. The Findings list below is authoritative.)

## Pre-commitment Predictions
Predicted the 5 likeliest problem areas before reading source; investigated each directly. Each verified correct — none became a blocking-tier finding:
1. **Pre-processor early-return ordering / null-endpoint handling** — VERIFIED correct. Skip at `ActiveUserPreProcessor.cs:19-23` precedes `IsAuthenticated`/`NameIdentifier`/User-lookup (`:25-35`); `IsExcludedModule(null)` returns false → gate NOT weakened for Fokus/Identity.
2. **`person_id` claim contract (producer↔consumer)** — VERIFIED agree. Producer `AuthConfiguration.cs:290` always writes `user.PersonId.ToString()` (non-null int FK) on every cookie; consumers `GetPersonId()` (throw) and `GetPersonIdOrNull()` (null) both parse int. Throw-variant only on cookie-gated paths where the claim is guaranteed present.
3. **The 3 bypass repoints** — VERIFIED all compare Person-id ↔ Person-id. `SprintHub.cs:207/222`, `GetSprintSummaryEndpoint.cs:28/32`, `EditCard`/`DeleteCard` presence-check `:62/:64` AND the second owner-compare via `GetPersonId()` in the `else` branch (`:45-48`).
4. **Migration ↔ model-snapshot parity, Down reversibility** — VERIFIED. `Up` drops both FKs + `Users` table + old unique index, renames column + index, recreates filtered unique index `"PersonId" IS NOT NULL`. `Down` fully reverses (recreates `Users`, both FKs, old index). `ReflektDbContextModelSnapshot.cs:208-225` matches.
5. **Mapster wire-stability** — VERIFIED. `ParticipantMappings.cs:13` `.Map(dest => dest.UserId, src => src.PersonId)`; `JoinSprintEndpoint.cs:34` adapts to `JoinSprintResponse`. Wire field `UserId` preserved.

## Findings

Highest severity present is Low. No findings at the two top severity tiers; no medium-tier findings. Two Low observations and the Carry-Over disposition below.

### [LOW] `GetPersonId()` (throwing) in EditCard/DeleteCard `else` branch — 500 instead of 403 for a stale pre-deploy cookie
**File:** `src/Services/Reflekt/Reflekt.API/Features/Cards/EditCard/EditCardEndpoint.cs:45`, `.../DeleteCard/DeleteCardEndpoint.cs:47`
**Issue:** The `else` branch (no `participantId` claim) calls `User.GetPersonId()`, which throws if `person_id` is absent. The branch is reachable only by a Cookie-authenticated request (GuestToken requests always carry `participantId` — confirmed in `GuestTokenAuthenticationHandler.cs:31` — and take the `if` branch). Post-Step-2 every Identity cookie carries `person_id`, so the throw-path is unreachable for validly-authorized requests. Only residual: a cookie issued *before* this deploy (no `person_id`) → unhandled 500 instead of a clean 403.
**Fix:** Optional, out of RA2 scope — dev cookies re-issue on next login and `reflekt.db` is disposable (plan Q3c). If hardened later, use `GetPersonIdOrNull()` here and treat null as Forbidden, matching the presence-check two blocks down. Not a blocker.
**Confidence:** high (behavior traced; reachability bounded by the policy + guest-handler claim set).

### [LOW] ~27 endpoints retain a local named `userId` holding a Person id
**File:** e.g. `src/Services/Reflekt/Reflekt.API/Features/Teams/CreateTeam/CreateTeamEndpoint.cs:20` (`var userId = User.GetPersonId();`)
**Issue:** Cosmetic naming drift — the *value* is correct (Person id), only the local name is legacy. Developer renamed the 4 value-space-critical files and left the rest to avoid 27-file churn (boy-scout shouldn't go looking beyond touched sites).
**Fix:** None required. A future Reflekt cleanup pass may rename. Already recorded as a low Carry-Over by the developer.
**Confidence:** high.

## Positive Observations
- **The most behavior-sensitive bypass (`GetSprintSummaryEndpoint`, the critic's top-tier finding) is correct.** `:28` reads `person_id`, `:32` compares `team.OwnerId == personId` — Person-id ↔ Person-id. The `[AllowAnonymous]` guest path correctly defaults `isScrumMaster = false` (the `if` at `:29` is skipped when the claim is absent). No value-space bug.
- **`ActiveUserPreProcessor` scoping is exactly mechanism (A) and does not weaken the gate.** Assembly-of-declaring-endpoint via `GetEndpoint()?.Metadata.GetMetadata<EndpointDefinition>()?.EndpointType.Assembly.GetName().Name`; the rejected scheme-based skip is absent. By-simple-name discrimination correctly avoids a cross-module project reference (CLAUDE.md guardrail) — a sanctioned Q4-A variant. The `ep.PreProcessor<…>(Order.Before)` in `Program.cs:50-53` runs only on FastEndpoints endpoints, so the SignalR hub (its own `AuthenticateAsync` auth) never reaches this gate — no null-`EndpointDefinition` gap.
- **Guest backstop is intact.** `GuestTokenAuthenticationHandler` sets no `NameIdentifier`, so guests also early-return at `:29-30` even absent the assembly-skip — defense in depth, with the assembly-skip carrying the ADR-021 authenticated-Person-without-User case.
- **Migration is faithful and reversible**; model snapshot matches; both EF configs (`ParticipantConfiguration`, `TeamConfiguration`) dropped `HasOne<User>()` and preserved the required column + non-User indexes.
- **Join seam preserved per ADR-021** — single `Participant.Join(displayName, sprintId, int? personId = null)` factory, one nullable param, one interceptable creation point; no `JoinWithGoogle`/`JoinWithPerson` resurrected.
- **Wire-stability carve-out is clean** — `JoinSprintResponse.UserId` kept by name, sourced from `PersonId` via explicit Mapster map; the only two `.UserId` hits in Reflekt are that map target + a comment.

## Gaps
- **Step-4 guest-not-401 / inactive-still-401 has no automated end-to-end test** — covered only by the `ActiveUserGateScope.IsExcludedModule` unit predicate (4 tests, verified passing). Confirmed accurate and acceptably deferred: the repo has no web/integration harness and `testing.md` reserves `WebApplicationFactory` for integration tests while listing endpoint routing under skip-tests. HTTP wiring is compiler-verified (build green against FE 8.1.0) and the configurator guarantees `EndpointDefinition` is in metadata for every FE endpoint. If a web harness is later added, add 2 integration tests (guest→Reflekt endpoint not 401; inactive suite user→Fokus endpoint still 401). Not a defect — see Carry-Over disposition.
- No frontend in scope (`JoinSprintResponse.UserId` JSON field intentionally stable); `vue.md` conventions n/a.

## Carry-Over Findings — disposition
- **[Medium] Step-4 covered only by unit predicate (developer + architect carry-over): REFUTED as a blocker, CONFIRMED as a deferred-test gap.** Sanity-checked both items the carry-over names: (1) the `GetEndpoint() → EndpointDefinition.EndpointType.Assembly` wiring — `EndpointDefinition` is attached by FastEndpoints to every FE endpoint's metadata (the same object `Program.cs:50-53`'s configurator mutates), `EndpointType.Assembly.GetName().Name` compiles against FE 8.1.0 (build green) and resolves to `"Reflekt.API"`; non-FE requests don't reach this pre-processor; (2) the early-return ordering — the skip precedes `IsAuthenticated`/`NameIdentifier`/User-lookup, and `IsExcludedModule(null)` → not excluded (fail-safe, gate still applies to Fokus/Identity). Mechanism + wiring sound; only the HTTP-pipeline assertion is deferred for want of a harness the project deliberately omits. Acceptable.
- **[Low] `GoogleUser` policy defined-but-unused (for architect):** pre-existing RA1 lift residue, not introduced by RA2. Confirmed no `[Authorize(Policy = …)]` references it. Out of scope; left as-is.
- **[Low] Reflekt Migrations history retained; NU1903 advisory; ~27 stale `userId` locals (for architect):** all pre-existing or cosmetic, end-state DB correct, none introduced by RA2. Confirmed.

## Open Questions
None. All findings reached high confidence; nothing moved here by self-audit.

## Evidence
| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Build | **PASS** | `dotnet build SprintRituals.slnx` | `Build succeeded. 8 Warning(s) 0 Error(s)` — `error CS` = 0; 8 warnings pre-existing NU1903 |
| Tests | **PASS — 68/68** | `dotnet test SprintRituals.slnx --no-build` | Identity.API.Tests 4/4, Reflekt.Domain.Tests 2/2, Fokus.Domain.Tests 62/62 — 0 failed, 0 skipped |
| Grep: `ClaimTypes.NameIdentifier` in Reflekt | **PASS — zero** | Grep `ClaimTypes\.NameIdentifier` `src/Services/Reflekt` | No matches |
| Grep: User-type residue (live) | **PASS — zero** | Grep `GetUserId`/`HasOne<User>`/`DbSet<User>`/`Reflekt.Domain.Users`/`JoinWithGoogle`/`GetByUserAndSprintAsync` | hits only under `Migrations/*.Designer.cs` (frozen snapshots, correctly not hand-edited); live `ReflektDbContextModelSnapshot.cs` clean |
| Grep: `.UserId` in Reflekt (excl. Migrations) | **PASS** | Grep `\.UserId` | 2 hits = sanctioned wire carve-out (Mapster `dest.UserId` target + comment) |
| Model snapshot parity | **PASS** | Read `ReflektDbContextModelSnapshot.cs:208-225` | `PersonId int?`, `{SprintId,PersonId}` unique filtered `"PersonId" IS NOT NULL`, no `Users`/`User` |
| Pre-processor wiring | **PASS** | Read `Program.cs:48-54`, `ActiveUserPreProcessor.cs`, `GuestTokenAuthenticationHandler.cs` | skip before gate; FE-only pipeline; guest sets no `NameIdentifier` (backstop) |

*Status: COMPLETE — reviewer, 2026-06-14*
