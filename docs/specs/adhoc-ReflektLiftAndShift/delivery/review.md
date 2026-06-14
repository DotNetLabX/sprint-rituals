# Review — adhoc-ReflektLiftAndShift (RA1)

Fresh, independent review. The prior run's review/lessons were voided as untrustworthy and
archived as `*-rogue-voided.md` — they were **not** consulted for this verdict.

## Step 1 — Done-Check

**Reviewed by:** architect (done check), 2026-06-14. Independent verification — every step checked
against on-disk code; the RA1 acceptance gate (full solution build) was re-run, not assumed.

### Pre-commitment predictions
1. Most likely failure: the solution does not actually build green (claim unverified). → **Falsified** — verified green independently (0 errors).
2. Mapster 7.4.0→10.0.0 bump breaks Fokus/Identity (heavy Mapster consumers). → **Falsified** — Fokus.API, Identity.API, Fokus.Domain.Tests all compile against 10.0.0 in the same green build.
3. The out-of-scope Fokus edit is unjustified scope creep. → **Falsified** — confirmed pre-existing FE 8.1 compile bug; fix is necessary for the green-build acceptance criterion.

### Step dispositions

| Step | Disposition | Notes |
|------|-------------|-------|
| 1 — Move projects + solution registration | Implemented | Three projects under `src/Services/Reflekt/`; `/Services/Reflekt/` folder in `SprintRituals.slnx:21-24` with all three; Sdk.Web→Sdk conversion folded in (Q2, see below); `restore` resolves; full build compiles all three. |
| 2 — Repoint BuildingBlocks; generic DbContext base; drop Reflekt BuildingBlocks | Implemented | Grep-acceptance clean: zero `Blocks.Persistence`/bare `Blocks.Domain` refs in source. `ReflektDbContext : ApplicationDbContext<ReflektDbContext>(options)` (F4 fix, `ReflektDbContext.cs:13`). No Reflekt-own `BuildingBlocks` source in tree (only build-output DLLs from the shared blocks). `EvidenceLink : Entity<int>` and `Repository.cs` 3-param rebase are valid lift-specific adaptations (documented). |
| 3 — Swap domain-event dispatch to target interceptor | Implemented | `DependencyInjection.cs:17-18,23` registers `IDomainEventPublisher`→`DomainEventPublisher` + `ISaveChangesInterceptor`→`DispatchDomainEventsInterceptor`, added to `DbContextOptions`. Old `DomainEventPublishingInterceptor` fully removed (zero refs). `persistence-patterns` logged. |
| 4 — CPM re-baseline | Implemented (with sanctioned deviation) | `AngleSharp 1.2.0` + `Mapster.DependencyInjection 10.0.0` declared as `<PackageVersion>`; three-form grep clean (no `Version=` in any Reflekt csproj). Mapster bump to 10.0.0 — see Deviation D1. `central-package-management` logged. |
| 5 — FastEndpoints currency (`Send*Async`→`Send.*`) | Implemented | Zero legacy FE `Send*Async` named methods in `Reflekt.API`. Every bare `SendAsync(` is SignalR (`Clients.*`/`.Group`/`.Caller`) — the documented false-positive, correctly untouched. `framework-currency` logged. |
| 6 — Module DI + hub route (F2) | Implemented | `AddReflektModule`/`MapReflektEndpoints` exist (`DependencyInjection.cs:19,51`); hub at `/hubs/retro` (`:55`, F2 fix); zero module-level `AddFastEndpoints`/`AddSignalR`/`AddOpenApi`/`MapFastEndpoints`/`MapFallbackToFile`/`MapScalarApiReference`. `service-registration` logged. |
| 7 — Auth: guest scheme + authorization policies (F3, Q1=A) | Implemented | `AuthenticationConfiguration.cs:20-23` adds only the guest scheme (`.AddAuthentication().AddScheme<…GuestTokenAuthenticationHandler>`); no `AddCookie`/`AddGoogle`/`OnGoogleTicketReceived`. `AddReflektAuthorization()` retained (`AuthorizationConfiguration.cs:7`, called `DependencyInjection.cs:36`). Four login endpoints unmapped with `_ra1/unmapped/*` routes + `// RA1: … (RA2, ADR-016)` markers. |
| 8 — Host composition + `ReflektDbContext` wiring | Implemented | `Program.cs`: `AddReflektModule` (:18), Reflekt assembly in FE `o.Assemblies` (:26), `Migrate<ReflektDbContext>()` after Fokus (:35), `SeedReflektDataAsync` (:37), `MapReflektEndpoints` (:58). Host.csproj ref (:26); `appsettings.json` `ReflektDatabase→data/reflekt.db` (:17); public `SeedReflektDataAsync` (`DependencyInjection.cs:42`); `DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ReflektDbContext>`. **Identity + Fokus registration untouched** (regression check passed). |
| 9 — Migration + green build | Deviated (valid reason) | EF delta is the `20260614100328_InitialReflektSchema` migration: `AddColumn` for 4 audit props × 7 aggregate tables, on top of Reflekt's 15 preserved migrations. Plan F7 predicted "possibly none"; the plan explicitly said "commit whatever delta EF actually generates" — a non-zero delta is within latitude. Root cause documented correctly (EF convention discovers public audit props on the target `AggregateRoot` base even though `EntityConfiguration<T>` doesn't map them). **RA1 build gate independently verified green.** `persistence-patterns` logged. |

### RA1 acceptance gate — independently verified

`dotnet build SprintRituals.slnx` re-run from a clean invocation:

```
Build succeeded.  4 Warning(s)  0 Error(s)
```

- All projects compiled: Host, Reflekt.{API,Domain,Persistence}, **Fokus.{API,Domain,Persistence}, Identity.{API,Domain,Persistence}, Fokus.Domain.Tests** — confirming the Mapster bump did not break the existing modules.
- The 4 warnings are a **pre-existing** `NU1903` advisory (`System.Security.Cryptography.Xml 10.0.0`) in `Blocks.Contracts` — not introduced by RA1, unrelated to this work.

### Cross-cutting deviations — judged

- **D1 — Mapster 7.4.0 → 10.0.0 (repo-wide, `Directory.Packages.props`).** Sanctioned. The plan's rule was "target wins on overlaps," but the target's overlap version (7.4.0) has **no** matching `Mapster.DependencyInjection 7.4.0` on NuGet (NU1603) — so 7.4.0 was unavailable as a coherent pair. 10.0.0 used for both. The green build (incl. Fokus/Identity heavy Mapster consumers: `.Adapt<>`, `IRegister`, `ServiceMapper`) proves no API breakage. This is the **plan-sanctioned downgrade-flag path** resolving in the opposite direction (an upgrade), documented in implementation.md "Key Decisions" + "Deviations." **Resolves Q3** (the developer's open question): proceed-with-build-as-checkpoint was the recommendation; build is green, so the question is answered by evidence.

- **D2 — One-line edit to `Fokus.API/Features/Auth/RevokeInvitation/RevokeInvitationEndpoint.cs`.** Sanctioned (necessary). Confirmed pre-existing bug, not RA1-introduced: HEAD (committed pre-RA1) used `Send.Results(Results.Conflict(), ct)`; FastEndpoints **8.1.0 has no `Results` member** on the `Send` responder (verified by reflection against the 8.1.0 assembly — only `ResponseAsync` and `ResultAsync` exist). HEAD therefore did not compile against the target's pinned FE 8.1. The fix `Send.ResponseAsync((object?)null, 409, ct)` is the correct migration and is **required** to satisfy RA1's whole-solution green-build acceptance criterion. It is the **only** source edit outside the Reflekt tree.

- **D3 — EF migration delta.** Sanctioned (Step 9, see above). Real, minimal, additive (audit columns), correctly explained, plan explicitly authorized "whatever delta EF generates."

### Skill conformance (scored against the log)

`.claude/audit/skill-invocations.log`, scoped to `agent: developer`, this session (`c4179b23…`):
all four mapped pattern skills are present and map to their Follow steps —
`nexus-dotnet:persistence-patterns` (Steps 3/9), `nexus-dotnet:central-package-management` (Step 4),
`nexus-dotnet:framework-currency` (Step 5), `nexus-dotnet:service-registration` (Steps 6/8).
`## Skills Used` section present in implementation.md and corroborates the log. **Pass.**

Note (lessons, not a gap): the logged entries carry token `developer:analyze`, not
`developer:implement` — the developer never advanced the pipeline-state token from analyze→implement,
so all invocations logged under the analyze label. The substance is satisfied (every Follow step's
skill really invoked, by the developer, in this session); the token label is a process nuance, not a
conformance failure.

### Scope check
Clean. Changes confined to: the three new Reflekt projects; the five plan-sanctioned config/wiring
files (`SprintRituals.slnx`, `Directory.Packages.props`, `Host.csproj`, `Program.cs`,
`appsettings.json`); and the one necessary pre-existing-bug fix (D2). `Reflekt.UnitTests` correctly
excluded per plan. No unexpected files, no scope creep.

### Carry-overs for the reviewer (not done-check failures)
- The global `ActiveUserPreProcessor` is registered as a **blanket** pre-processor on all endpoints
  (`Program.cs:50-52`). Plan Step 7 flagged it may 401 guest-scheme retro endpoints; plan says
  scoping it is an **RA2 escalation** ("document, don't fix"). Reviewer should confirm guest-join
  behavior at boot — this is the one runtime-behavioral risk the static check can't close.
- SignalR group prefix still `"sprint:{sprintId}"` though the hub route moved to `/hubs/retro`
  (cosmetic; RA2). Reflekt hub class still named `SprintHub` (rename is RA2/ADR-018).
- Audit columns are in the schema but never populated (Reflekt's `EntityConfiguration<T>` doesn't
  wire `AuditedEntityConfiguration`) — harmless dead columns until RA2.

**Verdict: PASS**

*Status: COMPLETE — architect, 2026-06-14*

## Step 2 — Code Review

## Reviewed By
Reviewer agent (nexus:reviewer), 2026-06-14. Fresh build run during review; carry-over findings explicitly traced.

## Verdict: APPROVED

## Pre-commitment Predictions

1. `ActiveUserPreProcessor` 401s guest-scheme participants (architect carry-over): Falsified — traced line-by-line, see CO1 below.
2. `AddSignalR()` double-registered: Falsified — zero hits in `src/Services/Reflekt`.
3. Legacy FE `Send*Async` surviving: Falsified — all 28 `SendAsync` matches are SignalR `Clients.*` chains.
4. CPM stray `Version=` in Reflekt csproj: Falsified — zero hits.
5. `ReflektDatabase` connection string missing: Falsified — present in `appsettings.json:17`, key matches persistence DI.

## Findings

All findings are severity LOW (style/cosmetic/RA2-deferred). No CRITICAL, no HIGH.

### [LOW] `CreateCardEndpoint` imports `CookieAuthenticationDefaults` directly in a feature endpoint
**File:** `src/Services/Reflekt/Reflekt.API/Features/Cards/CreateCard/CreateCardEndpoint.cs:8`
**Certainty:** Confirmed — line exists.
**Issue:** Endpoint calls `HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme)` inline to gate action-item creation during Discussion phase. Lifted behavior; works in RA1 because Identity owns the cookie scheme. Scheme constant inside handler is a code-smell deferred to RA2 auth unification (ADR-016).
**Fix:** RA2 — encapsulate in helper or policy during auth unification pass.

### [LOW] Hub class named `SprintHub`; group prefix `"sprint:{sprintId}"` (carry-over CO2)
**File:** `src/Services/Reflekt/Reflekt.API/Hubs/SprintHub.cs`
**Certainty:** Confirmed — name matches source.
**Issue:** Hub route is `/hubs/retro` (F2 fixed), but class name and group prefix are cosmetically inconsistent. Deferred to RA2/ADR-018.
**Fix:** RA2 — rename as part of `RetroSession` rename.

### [LOW] Audit columns migrated but never populated (carry-over CO3)
**File:** `src/Services/Reflekt/Reflekt.Persistence/Migrations/20260614100328_InitialReflektSchema.cs`
**Certainty:** Confirmed — migration adds nullable audit props to 7 tables; no `AuditedEntityConfiguration` used.
**Issue:** `CreatedById`/`CreatedOn`/`LastModifiedById`/`LastModifiedOn` are in schema but never written. All nullable — no INSERT/UPDATE failure. Harmless dead columns until RA2.
**Fix:** RA2 — wire `AuditedEntityConfiguration` or drop columns.

## Carry-Over Finding Resolution

| Finding | Status | Evidence |
|---------|--------|---------|
| CO1 — `ActiveUserPreProcessor` may reject guest-scheme participants | Closed — safe | Line 14 returns early if `IsAuthenticated != true`. Line 17 reads `ClaimTypes.NameIdentifier` — absent from guest token claims. Line 18: `int.TryParse(null, out _)` returns `false` → early return before any 401. No RA2 escalation triggered. |
| CO2 — SignalR group prefix / hub class name | Documented RA2 — not a blocker | Route correctly at `/hubs/retro`. Name/prefix cosmetic only. Raised as LOW above. |
| CO3 — Audit columns in schema but never populated | Documented RA2 — not a blocker | All 7 tables have nullable audit columns per migration. No runtime impact. Raised as LOW above. |

## Positive Observations

- The `ActiveUserPreProcessor` name-identifier parse-failure short-circuit is an accidental safety net for guest tokens — no claims to parse, so the preprocessor is a no-op for guest users without any code change. RA2 can scope it at leisure.
- The four login-endpoint unmapping pattern (`_ra1/unmapped/*` routes + `[AllowAnonymous]` + `Task.CompletedTask`) is clean — FastEndpoints discovery satisfied, no real path exposed, RA2 markers explicit.
- `SeedReflektDataAsync` mirrors the Fokus pattern exactly; `DesignTimeDbContextFactory` follows the Persistence-as-startup pattern from `ef-core.md`.
- CPM three-form verification confirmed clean.

## Gaps

- No boot/runtime smoke test during this review. The pre-processor interaction was closed analytically; the RA1 build gate was independently verified by the architect. For a lift-and-shift this is acceptable.
- No test coverage for lifted Reflekt code — per plan, `Reflekt.UnitTests` excluded and TDD set to `no` throughout.

## Open Questions
None.

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Build | PASS | `dotnet build SprintRituals.slnx --no-restore` | `Build succeeded. 2 Warning(s) 0 Error(s)` — both warnings are pre-existing `NU1903` on `Blocks.Contracts`, not introduced by RA1 |
| CPM three-form grep | PASS | grep `Version=` in `src/Services/Reflekt/**/*.csproj` | Zero hits |
| Old namespace grep | PASS | grep `Blocks\.Persistence\|using Blocks\.Domain;` in `src/Services/Reflekt` | Zero hits |
| Legacy FE Send*Async | PASS | grep legacy send methods in `Reflekt.API/Features` | All `SendAsync` hits are SignalR `Clients.*` chains; zero FastEndpoints legacy methods |
| AddSignalR double-reg check | PASS | grep `AddSignalR` in `src/Services/Reflekt` | Zero hits |
| ReflektDatabase connection string | PASS | Read `appsettings.json:17` | `"ReflektDatabase": "Data Source=data/reflekt.db"` matches `GetConnectionString("ReflektDatabase")` |
| ActiveUserPreProcessor guest trace | PASS — guest users safe | Code trace `ActiveUserPreProcessor.cs:14,17,18` | Guest claims lack `ClaimTypes.NameIdentifier`; parse returns false; early exit before 401 path |

*Status: COMPLETE — reviewer, 2026-06-14*
