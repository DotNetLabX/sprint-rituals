# Pass 3b — AppSettings VOs + Identity Encapsulation — Critic Plan Review

## Reviewed By
Critic (nexus:critic), Mode 2 — plan cross-referenced against binding inputs: ADR-007 (`docs/architecture/decisions.md`), backlog row 3b (`docs/backlog.md:13`), Q1–Q4 (`delivery/questions.md`, Answered), and the Pass-3 Q3 locked scope (`docs/specs/adhoc-Pass3-DomainEnrichment/delivery/questions.md` Q3, Answered). No `spec.md` (adhoc refactoring pass). Every plan step and acceptance check was cross-referenced against the live codebase; the read/write-site enumeration was independently re-derived via grep, not sampled.

## Verdict: APPROVE

**Cycle 2 (re-review of revised plan):** All four cycle-1 findings (HIGH-1, MEDIUM-2, MEDIUM-3, LOW) are fully resolved. No CRITICAL or HIGH issues remain; no new issues introduced by the revisions. See "Cycle 2 Resolution" below. The cycle-1 findings are retained verbatim further down for the record.

## Cycle 2 Resolution

| Cycle-1 finding | Status | Where resolved in revised plan |
|-----------------|--------|-------------------------------|
| **HIGH-1** — `AppSettingsRepository.SaveAsync` write funnel unaddressed | **RESOLVED** | Scope item 4 (`plan.md:21`); Step 4 title + confidence note (`:157`, `:159`); funnel statement (`:161`); full binding bullet citing `AppSettingsRepository.cs:27-32`, the `SetValues` complex-type limitation, the exact 4 re-assignment lines, and the `SaveHealthConfigEndpoint.cs:12-14` precedent (`:169`); **two** new acceptance lines — re-assignment present (`:174`) AND the explicit detached-round-trip guard (`:175`); Step 7 check 6 grep proving 4 re-assignments (`:214`); Testing Strategy item 5 (`:236`); Open Questions (`:253`). The detached-round-trip acceptance is technically sound: `SaveAsync` fetches `existing` fresh as the tracked entity (`AppSettingsRepository.cs:20`), and `existing.BugRatio = settings.BugRatio` copies onto that tracked entity, so EF detects the complex-property mutation regardless of whether `settings` is tracked. |
| **MEDIUM-2** — brittle fixed-type setter regex in Step 7 | **RESOLVED** | Step 7 check 2 (`:210`) replaced the fixed type allow-list with the type-agnostic regex `public\s+[\w\.<>\[\]?]+\s+\w+\s*\{\s*get;\s*set;\s*\}` AND added a manual property-by-property confirmation recorded in `implementation.md` — exactly the recommended fix. |
| **MEDIUM-3** — EF nullable-complex-member edge unflagged in Step 2 | **RESOLVED** | Step 2 confidence note now names it as delicate point (2) (`:118`); full binding-constraint bullet (`:123`) — non-null instance via `= new()`, nullable members → nullable columns, `CycleTimeConfig` all-nullable-but-non-null-instance is valid, "do NOT mark the `ComplexProperty` itself optional"; Open Questions echo (`:251`). |
| **LOW** — Step 3 prose/regex `BugRatioConsecutiveSprintCount` note | **RESOLVED** | `:145` annotates `BugRatioService.cs` with "carries `BugRatioConsecutiveSprintCount` at :219/:342 plus `BugRatioAlertThreshold`/`DefaultSpPerBug` — all 11 members are real reads, the grep does not over-match." |

**New issues introduced by the revisions:** none. The added repository bullet, the nullability constraint, the type-agnostic regex, and the new acceptance/Step-7 checks are consistent with the codebase verified in cycle 1 (file paths, line numbers, and the `SetValues`/JSON-VO precedent all match). Scope discipline is unchanged — no re-litigation of locked scope, no scope creep; the only file added to the touch-list is `AppSettingsRepository.cs`, which is correct and necessary. Step 7 check 6's grep will yield exactly 4 matches (one per `existing.{VO} =` re-assignment line), as asserted.

---

## Cycle 1 Review (retained for the record)

### Verdict (cycle 1): REQUEST CHANGES

One HIGH finding (an unaddressed write path — `AppSettingsRepository.SaveAsync` — that the plan never mentions but that every Save flows through, with a real reference-swap/`SetValues` hazard the codebase has already flagged in-code as a Pass-3 concern). Everything else is accurate and well-scoped. The HIGH is a plan-completeness gap, not a wrong-approach error: it is a one-paragraph addition to Step 4 + one acceptance line, not a redesign.

## Pre-commitment Predictions
- Expected the read-site enumeration (Step 3) to be the highest-risk area for omissions (~40 sites, 25 files). **Found it complete** — every `AppSettings` read in the grep maps to a file the plan lists; no missed site.
- Expected the `req.Adapt(existing)` → grouped-VO Mapster concern to be the plan's weak point. **Found it correctly flagged** (Step 4 + Open Questions) — but the adjacent repository write path is missed (see HIGH-1).
- Expected scope-creep risk on the Fokus sync aggregates. **Found none** — the out-of-scope section honors Pass-3 Q3 / B3 verbatim.
- Expected Identity "zero external direct writes" to be overstated. **Verified true** — the only `.Property =` writes to the targeted members are inside `Person`/`User` behavior methods (same assembly); no external writer exists.

## Findings

### [HIGH] `AppSettingsRepository.SaveAsync` is the single write path for every Save endpoint and is never mentioned in the plan — its `CurrentValues.SetValues` + manual-VO-reassignment pattern must be extended to the 4 new ComplexProperty VOs
**File:** `src/Services/Fokus/Fokus.Persistence/Repositories/AppSettingsRepository.cs:27-32`
**Issue:** `SaveAsync` does `db.Entry(existing).CurrentValues.SetValues(settings)` (line 27) and then **manually re-assigns each non-scalar member** that `SetValues` does not propagate: `HealthThresholds`, `HealthWeights` (JSON-owned VOs) and the three `List<string>` converters (lines 28-32). The 11 flat scalars are currently copied by `SetValues` because they are plain scalars on the entity. After Step 1 groups them into the 4 ComplexProperty VOs (`BugRatio`, `Xray`, `CycleTime`, `Sync`), they are **no longer plain scalars** — they are complex-type members, the same category EF Core's `CurrentValues.SetValues(object)` does not deep-copy, which is exactly why lines 28-29 exist for the JSON-owned VOs.

In the *current* call flow this is latent rather than fatal: every Save endpoint passes the **tracked** `existing` instance (returned by `GetAsync`, mutated in place, then handed to `SaveAsync` on the same DbContext scope), so `settings == existing` and the in-place mutations are already on the tracked entity before `SaveAsync` runs. So a strict round-trip may still pass. But the plan (a) never names this file, (b) gives the developer no directive to extend the manual re-assignment block to `existing.BugRatio = settings.BugRatio;` (and the other 3), and (c) the codebase has already pre-warned about this exact hazard: `SaveHealthConfigEndpoint.cs:12-14` carries the comment *"Pass 3 will introduce ComplexProperty owned-entity configuration, and reference-swap semantics must be preserved for EF change-tracking."* That foreknowledge belongs in this plan, not left to the developer to rediscover mid-RED-build.

The risk this materializes into data loss is real on two adjacent moves the plan's wording invites: if the developer "fixes" the Step-4 `req.Adapt` mapping by adapting onto a **fresh** `AppSettings` (instead of the tracked `existing`), or refactors `SaveAsync` to take a detached `settings`, the grouped-VO values will be silently dropped by `SetValues` — and the plan's grep/build acceptance would not catch it (it compiles; the grep targets member-access strings, not the repository).
**Fix:** Add `AppSettingsRepository.cs` to the Step 4 file list with an explicit instruction: extend the manual re-assignment block (lines 28-32) with `existing.BugRatio = settings.BugRatio; existing.Xray = settings.Xray; existing.CycleTime = settings.CycleTime; existing.Sync = settings.Sync;` (mirroring the existing JSON-VO handling), and add a Step 4 acceptance line: "a Save through `AppSettingsRepository.SaveAsync` followed by a fresh `GetAsync` round-trips all 4 new VOs' values (not just the tracked-instance path)." Reference the `SaveHealthConfigEndpoint.cs:12-14` precedent comment.
**Confidence:** HIGH (the file, the call flow, and the in-code Pass-3 warning are all verified; the severity of the latent bug is bounded by the tracked-instance flow, which is why this is HIGH not CRITICAL).

### [MEDIUM] Step 7 verification regex #2 will not reliably prove "zero public setters" on `Person`/`User`
**File:** `delivery/plan.md:206` (Step 7 check 2)
**Issue:** The regex `public (string|int|bool|decimal|DateTime|PersonSource)\??\s+\w+\s*\{\s*get;\s*set;` enumerates a fixed type list. `Person`/`User` happen to fit it today (`Person.cs:8-16`, `User.cs:7-11` use `string`/`int`/`bool`/`DateTime`/`PersonSource`/`int?`), so it works **now** — but it is a brittle allow-list: it would miss any property of a type not in the list (e.g. a future `Guid`, or `string?` is covered but a wrapped type is not), and it does not assert get-only computed members stay get-only. As a *proof of absence of public setters* it is weaker than the requirement.
**Fix:** Either broaden to a type-agnostic form (`public\s+[\w\.<>?]+\s+\w+\s*\{\s*get;\s*set;\s*\}` scoped to the two files) or, since the file set is exactly two small files, make the acceptance "manual confirmation that every property in `Person.cs` + `User.cs` is `get`-only or `private set`" — which the build already enforces via callers. Low effort; prevents a false-green.
**Confidence:** HIGH (regex limitation is verifiable against the current files; the two files are small enough that this is a precision nit, hence MEDIUM not HIGH).

### [MEDIUM] ComplexProperty + all-nullable VO members (`CycleTimeConfig`) — flag the EF "complex type cannot be null / all members optional" edge before the developer hits it
**File:** `delivery/plan.md:61` (CycleTimeConfig: `StartStage : string? = null`, `EndStage : string? = null`) and Step 2
**Issue:** `CycleTimeConfig` has **both** members nullable, and `XrayConfig` has 2 of 3 nullable. EF Core's `ComplexProperty` requires the complex *instance* itself to be non-null (complex properties are required by design in EF 8/9; optional complex properties are not supported), while individual nullable *members* are fine. The plan initializes each VO `= new()` (AppSettings.cs side) which satisfies the non-null-instance rule — so this is navigable — but the plan's Step 2 confidence note ("medium — the rename-not-drop migration is the delicate part") does not call out the nullable-member mapping as a second delicate point. A developer mapping `ComplexProperty(s => s.CycleTime)` whose members are all-nullable can trip EF's "no properties to map" / required-complex-instance diagnostics if a member is mis-configured.
**Fix:** Add one line to Step 2's feature-specific inputs: "all VO instances are non-null (`= new()` on AppSettings satisfies EF's required-complex-instance rule); nullable members (`Xray.ClientId/ClientSecret`, `CycleTime.StartStage/EndStage`) map as optional columns — confirm the migration emits nullable columns for them and non-null for the `int/bool/decimal` members." It is already implied by the table defaults, but naming it pre-empts an EF diagnostic detour.
**Confidence:** MEDIUM (the `= new()` initialization makes this navigable; flagging it is a robustness improvement, not a blocker).

### [LOW] Step 3 verification grep omits `BugRatioConsecutiveSprintCount` from one prose enumeration but includes it in the regex — cosmetic inconsistency
**File:** `delivery/plan.md:148` vs `:205`
**Issue:** Both Step 3 (line 148) and Step 7 (line 205) regexes correctly include all 11 member names including `BugRatioConsecutiveSprintCount`. The prose read-site list (Step 3, lines 139-145) enumerates services by name and is complete, but does not separately call out `BugRatioConsecutiveSprintCount`'s sites (`BugRatioService.cs:219,342`). They are inside `BugRatioService.cs` which is listed, so they are covered — this is purely a prose-vs-regex completeness note, no action strictly required.
**Fix:** None required; optionally note `BugRatioService` carries `BugRatioConsecutiveSprintCount` reads at :219/:342 so the developer does not assume the regex over-matches.
**Confidence:** HIGH (verified the sites are inside a listed file; impact is nil).

## Positive Observations
- **Read-site enumeration is exhaustive and accurate.** Independently re-running the plan's own Step-3 grep across `src` returned exactly the files the plan lists. The DI seeder (`DependencyInjection.cs:83-85`), gRPC (`SprintAnalyticsGrpcService.cs:40,231,239`), both sync services, the Xray feature, and all 9 analytics endpoints + 16 analytics services are correctly inventoried. No `AppSettings` read/write site is missed outside the repository (HIGH-1).
- **The flat-wire / domain-internal split (Q1=A) is correctly threaded.** `GetSettings` builds the response by explicit hand-assignment (`GetSettingsEndpoint.cs:12-31`), not Mapster, so Step 4's "repoint the reads" is precisely scoped; `GetSettingsResponse` and the 5 Save command records are confirmed flat and the plan keeps them so.
- **The Mapster convention-break is correctly anticipated.** The deleted `*Mappings.cs` files (staged deletions, last touched in Pass-2 commit 3f07fa7) confirm the current `req.Adapt(existing)` works *purely by field-name convention* with no explicit config. The plan correctly identifies that grouping breaks that convention and mandates explicit `.Map()` rules (Step 4, Domain Model Changes note line 80, Open Questions line 245). This is the single most likely silent-failure point and the plan does not under-state it.
- **Identity encapsulation claims verified true.** `Person.cs`/`User.cs` are all-public-setter (except `User.Role` already `private set`); all 9 factory/behavior methods (`Person/Behaviors/Person.cs`, `User/Behaviors/User.cs`) are in-assembly and are the only mutators; the codebase-wide `.Property =` grep finds zero external writers. The "object-initializer factory compiles against `private set`+`required`" claim is correct (C# allows in-assembly object initializers to set `private set`/`required` members).
- **Scope discipline is clean.** The out-of-scope section honors Pass-3 Q3 / triage B3 (Fokus sync aggregates stay anemic for upsert) and Q4 (4 existing JSON-owned VOs untouched) verbatim. No re-litigation of locked scope. The `ComplexProperty`-vs-JSON decision matches Q4. Backlog row 3b (`docs/backlog.md:13`) matches the plan title and scope.
- **`AppSettings` stays-a-plain-class is correct.** It is not an aggregate (`AppSettings.cs:3` — no `: AggregateRoot`); the plan correctly declines to encapsulate it (the Save endpoints + seeder mutate it directly), distinguishing it from the Identity arm. Good ADR-007 nuance.
- **EF migration data-preservation reasoning is sound.** Rename-not-drop to preserve the seeded singleton (`Xray_ClientSecret`, sync config) is the right call; `HasData`/`ToJson` incompatibility correctly noted as non-applicable (runtime seeding via `CreateDefault()`).

## Gaps
- **`AppSettingsRepository.SaveAsync` write path** — the dominant gap (HIGH-1). It is the funnel every Save endpoint passes through and the plan's file list, steps, and acceptance never reference it.
- **No round-trip-from-detached acceptance.** Step 4's round-trip check (line 171) and the manual smoke (line 232) exercise the tracked-instance happy path. Neither asserts the behavior survives if `SaveAsync` is fed a non-tracked `settings` — the exact scenario HIGH-1's fix should guard.
- **EF nullable-complex-member edge** (MEDIUM-3) — navigable but unflagged in Step 2's delicacy note.

## Open Questions
- None. All four findings are evidence-backed against specific files/lines; none are low-confidence enough to demote here.

## Evidence
| Check | Result | Command / Source | Output |
|-------|--------|------------------|--------|
| Read-site inventory complete | pass | `rg` of the 11 member names across `src/*.cs` | Every `AppSettings`-read file matches the plan's Step-3 list; no omission outside `AppSettingsRepository` |
| Write-site inventory | gap | `rg "(member)\s*=" src/*.cs` | Seeder (`DependencyInjection.cs:83-85`) + `SaveXraySettings`/`SaveCycleTimeBoundaries` explicit + `CreateDefault()` — all in plan; `req.Adapt` endpoints write via Mapster; **`AppSettingsRepository.SaveAsync:27-32` write path absent from plan** |
| Mapster convention break real | confirmed | `git status` + `rg IRegister\|\.Map\( Features/Settings` | `*Mappings.cs` staged-deleted; zero remaining Mapster config in Settings → current `req.Adapt` is convention-only; plan correctly flags the break |
| Identity zero external writes | confirmed | `rg "\.(FirstName\|...\|LastLogin)\s*=" src/*.cs` | Only in-assembly behavior methods write; no external writer; plan claim accurate |
| Person/User current shape | confirmed | Read `Person.cs`, `User.cs`, both `Behaviors/*.cs` | All-public except `User.Role` (already `private set`); 9 mutators in-assembly — matches plan |
| Scope vs Pass-3 Q3 / backlog 3b | pass | `decisions.md` ADR-007, `Pass3/questions.md` Q3, `backlog.md:13` | No re-litigation; out-of-scope honors B3/Q4; title/scope match backlog |
| ComplexProperty nullable edge | flagged | `CycleTimeConfig`/`XrayConfig` member nullability vs EF complex-type rules | Navigable via `= new()`; unflagged in Step 2 delicacy note |
| Build | not run | n/a (plan review, no implementation yet) | Critic does not build; this is a pre-implementation plan review |

Reviewed: docs/specs/adhoc-Pass3b-AppSettingsVOs/delivery/plan.md
