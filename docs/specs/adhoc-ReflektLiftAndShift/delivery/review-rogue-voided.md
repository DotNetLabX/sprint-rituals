# Review — adhoc-ReflektLiftAndShift (RA1)

## Step 1 — Done-Check

**Done-check method:** ADR-mapping + grep-checkable acceptance (adhoc, no `spec.md`). Authoritative skill evidence: `.claude/audit/skill-invocations.log` scoped to `agent=developer` (token in force during implement). implementation.md `## Skills Used` cross-checked against the log.

**Pre-commitment predictions (made before reading implementation):**
1. Skill log would not corroborate all Follow self-reports — **did not materialize**; all 4 distinct mapped skills present in the log.
2. The out-of-scope Fokus fix is the highest deviation risk (pre-existing? crosses "do not touch Fokus"?) — **investigated; verified pre-existing via git diff**.
3. Audit-columns migration may contradict the plan's F7 reasoning — **investigated; internally consistent**.

| Step | Disposition | Notes |
|------|-------------|-------|
| 1 — Move projects + solution registration | Implemented | Three projects under `src/Services/Reflekt/`; `/Services/Reflekt/` folder added to `SprintRituals.slnx`; SDK Web→class-library conversion done; restore + build succeed. |
| 2 — Repoint BuildingBlocks; generic DbContext base; drop Reflekt BuildingBlocks | Implemented | GlobalUsings repointed to target namespaces; `ReflektDbContext : ApplicationDbContext<ReflektDbContext>` (F4); `EvidenceLink : Entity<int>` verified in source (src/Services/Reflekt/Reflekt.Domain/Cards/EvidenceLink.cs:4). |
| 3 — Swap domain-event dispatch to target interceptor | Implemented | `DispatchDomainEventsInterceptor` + `DomainEventPublisher` registered; Reflekt's old interceptor removed. Skill `persistence-patterns` logged. |
| 4 — CPM re-baseline | Deviated (valid) | Mapster `7.4.0`→`10.0.0` for base + DI package: `Mapster.DependencyInjection 7.4.0` does not exist on NuGet (NU1603). Plan said "target wins on overlaps"; target pin was unavailable, so next available concrete version used. No 10.x API breakage. AngleSharp + Mapster.DependencyInjection added. Skill `central-package-management` logged. |
| 5 — FastEndpoints currency (`Send*Async`→`Send.*`) | Implemented | Independently grep-verified: **zero** legacy FastEndpoints `Send*Async`/`Send.Results` remain in `src/Services/Reflekt`; all remaining `SendAsync` hits are SignalR `Clients.*.SendAsync` (documented false-positive, correctly untouched). Skill `framework-currency` logged. |
| 6 — Module DI `AddReflektModule`/`MapReflektEndpoints`; hub route | Implemented | Module-level FastEndpoints/OpenApi/fallback/SignalR re-registration removed; hub mapped at `/hubs/retro` (F2). Skill `service-registration` logged. |
| 7 — Auth: guest scheme + authorization policies on Identity cookie | Implemented | `AuthenticationConfiguration` adds only the guest scheme; `AddReflektAuthorization()` retained; four login endpoints unmapped with the RA2 marker + `_ra1/unmapped/*` placeholder (LoginEndpoint.cs:1-15 verified). |
| 8 — Host composition + `ReflektDbContext` wiring | Implemented | Program.cs verified: `AddReflektModule`, assembly added to FastEndpoints `o.Assemblies`, `Migrate<ReflektDbContext>` after Fokus, `SeedReflektDataAsync`, `MapReflektEndpoints`; Identity/Fokus registration untouched; `ConnectionStrings:ReflektDatabase` added; public seed + design-time factory present (F5). Skills `service-registration` + `persistence-patterns` logged. |
| 9 — Migration + green build | Deviated (valid) | Non-zero migration: audit columns (CreatedById/On, LastModifiedById/On) on all 7 aggregate tables (Votes, Users, Teams, Sprints, Participants, Cards, CardGroups) — migration file verified. Plan F7 predicted "possibly none"; the delta is correct behavior (EF discovers the public audit properties on the target `AggregateRoot` base even though `EntityConfiguration<T>` does not map them). Build green (0 errors, fresh). |

### Deviation evaluations (the four flagged)

1. **Mapster 10.0.0 vs 7.4.0 — ACCEPTABLE.** `Mapster.DependencyInjection 7.4.0` is not published on NuGet; 7.4.0 base + a non-existent DI package cannot coexist. 10.0.0 is the lowest concrete version that satisfies both. Plan's "target wins on overlaps" assumed the target pin was installable; it was not, so the next-available rule applies. No API breakage surfaced at build. Within plan latitude (Step 4 explicitly anticipated 10.x breakage being caught at build).
2. **Non-zero migration — CORRECT BEHAVIOR.** The plan (F7, Step 9) said "commit whatever delta `migrations add` actually generates … possibly none." The audit-property delta is the real EF output of the base swap and is exactly what Step 9 instructed. The developer's reasoning (public props on base discovered by convention, not by `AuditedEntityConfiguration`) is sound and matches the F7 note. `Deviated (valid)` — the plan sanctioned committing the real delta.
3. **Pre-existing `Send.Results` bug in Fokus — ACCEPTABLE out-of-scope fix.** Verified via `git diff HEAD`: the committed line was `Send.Results(Microsoft.AspNetCore.Http.Results.Conflict(), ct)` (landed in `8e0f033`, Pass4); the only working-tree change is the one-line fix to `Send.ResponseAsync((object?)null, 409, ct)`. The bug **predates** RA1 and was surfaced by the solution-wide green-build acceptance criterion (Step 9 / overall accept). A green solution build is a hard acceptance gate, so the minimal fix is justified and is documented as a carry-over. Note for reviewer: this is a behavior-preserving transport-shape fix (still returns 409), not a Fokus domain change — consistent with "do not touch Fokus *semantics*."
4. **`EvidenceLink : Entity` → `Entity<int>` — CORRECT FIX.** Source used a non-generic `Entity` alias that the target BuildingBlocks does not expose (target has only `Entity<TId>`). `int` matches the entity's `CardId`/PK shape and the sibling repository's `int` key. Source-verified. No behavior change.

### Skill conformance (scored against the log)

Authoritative log (`.claude/audit/skill-invocations.log`), developer entries:
- `nexus-dotnet:persistence-patterns` ✓ (steps 3, 8, 9)
- `nexus-dotnet:central-package-management` ✓ (step 4)
- `nexus-dotnet:framework-currency` ✓ (step 5)
- `nexus-dotnet:service-registration` ✓ (steps 6, 8)

All four distinct non-`None` mapped skills appear in the log; no self-reported invocation is absent from the log; no fabrication. `## Skills Used` section present (structural gate satisfied). Steps 1, 2, 7 are `None` (mechanical/lift-specific) — exempt. All steps `TDD: no` (wiring/mechanical) — no `tdd` obligation. **PASS.**

> Token note: the log entries carry token `developer:analyze` and `.pipeline-state` reads `developer:analyze` — they match, so the entries are in-window. (The Skill tool was invoked during the developer's analyze phase, which is the expected place to load pattern skills before implementing; the state was not re-stamped to `developer:implement` for this run.)

### Structural completeness
Files Created, Files Modified (per step), Key Decisions, Skills Used, Carry-Over Findings (3 entries, all routed to reviewer), KB Changes (None — justified, structural lift), Deviations from Plan (4, all matching the above) — all present. Carry-over table present. Completion footer present.

**Verdict: PASS**

*Status: COMPLETE — architect, 2026-06-14*

---

## Step 2 — Code Review

## Reviewed By
nexus:reviewer (Claude Sonnet 4.6), 2026-06-14

## Verdict: APPROVED

## Pre-commitment Predictions

1. **FastEndpoints currency incomplete** — plan noted 43+ files; expected risk that some Send*Async calls slipped through. Did not materialize — grep confirms zero legacy FastEndpoints Send*Async in src/Services/Reflekt; all remaining SendAsync hits are SignalR hubContext.Clients.*.SendAsync (confirmed in Broadcast handler files).
2. **AddAuthentication() double-call risk** — plan warned about Reflekt colliding with Identity's auth builder. Confirmed safe — AuthenticationConfiguration.cs:21 calls .AddAuthentication() (parameterless) then .AddScheme<...>(). ASP.NET Core's parameterless AddAuthentication() simply returns the existing builder without resetting the default scheme; Identity's AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme) already set the default. The chain form is the correct pattern for this scenario.
3. **ActiveUserPreProcessor rejecting guest participants** — the primary carry-over risk. Traced and confirmed safe — the preprocessor checks IsAuthenticated == true, then parses ClaimTypes.NameIdentifier as int. Guest principals carry participantId/sprintId/displayName claims (not ClaimTypes.NameIdentifier). int.TryParse(null, ...) returns false, so the if (!int.TryParse...) branch returns early without emitting a 401.
4. **CPM three-form grep clean** — grep across all Reflekt .csproj files finds zero Version= attributes. Both Mapster and Mapster.DependencyInjection are in Directory.Packages.props at 10.0.0.
5. **Namespace repointing incomplete** — grep Blocks\.Persistence|using Blocks\.Domain; across src/Services/Reflekt finds zero hits.

## Findings

No CRITICAL or severity-HIGH findings. All findings below are LOW or MEDIUM severity only.

### [LOW] Dead middleware file in Reflekt.API
**File:** `src/Services/Reflekt/Reflekt.API/Middleware/GlobalExceptionMiddleware.cs`
**Issue:** The plan (Step 6) said to delete this file — "delete it — Host uses Blocks.AspNetCore.GlobalExceptionMiddleware". The file was retained and never registered by the Host. It is dead code.
**Fix:** Delete the file in a follow-up commit (RA2 or boy-scout pass). No behavior impact since it is never registered.
**Confidence:** HIGH

### [LOW] Hub group prefix "sprint:{sprintId}" cosmetic mismatch
**File:** `src/Services/Reflekt/Reflekt.API/DependencyInjection.cs:62`, `SprintHub.cs:37,73,95,126,240,280,291`
**Issue:** Hub mapped at /hubs/retro correctly, but internal SignalR group names still use "sprint:{sprintId}". The timer expiry broadcast and the SprintHub.JoinSprint both use the same prefix so they are internally consistent — no functional breakage. Cosmetic mismatch with the renamed hub route.
**Fix:** Defer to RA2 rename pass (ADR-018). No RA1 action required.
**Confidence:** HIGH (documented carry-over, confirmed internally consistent)

### [LOW] Reflekt.API.csproj carries redundant Microsoft.EntityFrameworkCore.Design reference
**File:** `src/Services/Reflekt/Reflekt.API/Reflekt.API.csproj:19`
**Issue:** Microsoft.EntityFrameworkCore.Design with PrivateAssets="all" is declared on the API project. This belongs in Reflekt.Persistence.csproj (where the design-time factory lives). Having it in both is harmless (PrivateAssets="all" prevents leaking to consumers) but redundant.
**Fix:** Remove from Reflekt.API.csproj in a future cleanup pass.
**Confidence:** MEDIUM (redundant but harmless)

## Carry-Over Findings — Addressed

| Carry-Over | Disposition | Evidence |
|------------|-------------|---------|
| ActiveUserPreProcessor may reject guest-scheme participants | REFUTED — safe. | ActiveUserPreProcessor.cs:14-18: guard exits early when ClaimTypes.NameIdentifier fails int.TryParse — guest claims do not carry that claim, so int.TryParse(null, ...) = false -> return. No 401 emitted. Guest participants proceed. |
| Audit props unmapped but migrated | CONFIRMED — acceptable, per plan. | InitialReflektSchema.cs adds 4 audit columns to all 7 tables. EntityConfiguration does not call AuditedEntityConfiguration, so columns will always be nullable and never populated. Schema is correct; behavior gap is documented RA1 limitation. |
| Reflekt hub sends "sprint:{sprintId}" groups | CONFIRMED — internally consistent, cosmetic. | All group-name writes and all Clients.Group("sprint:...") reads use the same prefix — no functional mismatch. Frontend not yet served; realignment deferred to RA5 per plan. |

## Positive Observations

- Excellent plan discipline: every plan acceptance criterion has a corresponding verifiable artifact. The four unmapped login endpoints use _ra1/unmapped/* placeholder routes — discoverable by FastEndpoints without exposing real paths, and the RA2 comment is in place.
- Carry-over risk tracing: the developer correctly flagged ActiveUserPreProcessor x guest as a risk. The implementation is safe without any special scoping — the preprocessor's early-exit on non-integer NameIdentifier is the natural guard.
- FastEndpoints currency completeness: Send.OkAsync migration across 43+ files with no false positives or false negatives. SignalR SendAsync correctly untouched.
- Out-of-scope Fokus fix documented transparently — the pre-existing Send.Results bug was a blocking build issue; the fix is behavior-preserving (still 409) and its pre-existing status is verifiable via git history.
- Generic DbContext base (F4) and EvidenceLink : Entity<int> (non-generic alias) fixed correctly; no compiler workarounds needed.
- SeedReflektDataAsync as a public extension matches the Fokus pattern exactly and is callable from the Host's Init section without InternalsVisibleTo.

## Gaps

- No behavior-level test exercises the guest token -> endpoint -> hub round-trip. Acceptable for a structural lift (all steps TDD: no) and appropriate for RA1 scope, but should be addressed in RA2 when the auth model solidifies.
- The GlobalExceptionMiddleware in Reflekt.API is dead code (not registered). Does not affect correctness but adds noise to the Reflekt module footprint.

## Open Questions
None.

## Evidence
| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Build | PASS (0 errors) | dotnet build SprintRituals.slnx --no-incremental | Build succeeded. 7 Warning(s), 0 Error(s). Time Elapsed 00:00:07.14 |
| Legacy Send*Async in Reflekt | 0 files | Grep SendOkAsync\|Send\.Results in src/Services/Reflekt | No files found |
| Old BuildingBlocks namespaces | 0 hits | Grep Blocks\.Persistence\|using Blocks\.Domain; in src/Services/Reflekt | No files found |
| Version= in Reflekt csproj files | 0 hits | Grep Version= in src/Services/Reflekt csproj files | No matches found |
| Module-level AddSignalR/AddFastEndpoints | 0 files | Grep in src/Services/Reflekt | No files found |
| Old DomainEventPublishingInterceptor | 0 files | Grep in src/Services/Reflekt | No files found |
| Hub route | /hubs/retro | DependencyInjection.cs:55 | app.MapHub<SprintHub>("/hubs/retro") confirmed |
| 4 unmapped endpoints | Confirmed | LoginEndpoint.cs:15 | Get("_ra1/unmapped/auth/login") with RA1 marker |
| ReflektDatabase connection string | Added | appsettings.json:17 | "ReflektDatabase": "Data Source=data/reflekt.db" |
| Guest pre-processor trace | Safe | ActiveUserPreProcessor.cs:14-18 | int.TryParse(null, out _) = false -> early return; no 401 for guests |

*Status: COMPLETE — reviewer, 2026-06-14*
