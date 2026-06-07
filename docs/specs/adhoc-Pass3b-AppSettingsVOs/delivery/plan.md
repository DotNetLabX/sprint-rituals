# Pass 3b — AppSettings VOs + Identity Encapsulation (ADR-007)

**Feature Spec:** None (adhoc refactoring pass — scope in `docs/backlog.md` row 3b + `docs/architecture/decisions.md` **ADR-007**; user decisions in `docs/specs/adhoc-Pass3b-AppSettingsVOs/delivery/questions.md` Q1–Q4, Answered. Scope was locked at the Pass-3 split — `docs/specs/adhoc-Pass3-DomainEnrichment/delivery/questions.md` Q3, Answered — and must NOT be re-litigated.)

## Context

ADR-007: aggregates encapsulate state, and **cohesive scalar fields are grouped into value objects / owned types** — "applies to all aggregates including Identity `Person`/`User`." This pass delivers two independent arms of ADR-007:

1. **AppSettings VO grouping (Fokus).** `Fokus.Domain/Settings/AppSettings.cs` is a plain settings class (not an aggregate) carrying ~11 flat scalars plus 4 already-extracted config VOs. Group the flat scalars into **4 new config VOs** (`BugRatioConfig`, `XrayConfig`, `CycleTimeConfig`, `SyncConfig`). The VOs are **domain-internal** — the wire (API response, save commands, Vue client) **stays flat** (Q1=A); endpoints map flat↔grouped at the boundary via Mapster. EF persists the new VOs as **flat columns via `ComplexProperty`** (Q4), not JSON.

2. **Identity encapsulation (Identity).** `Person`/`User` have public setters; tighten them so mutation flows only through the existing factory + behavior methods (Q3). **No new behavior, no wire change** — every Person/User mutation in the codebase already routes through those methods (verified: zero external direct property writes).

The two arms touch different modules (`Fokus` vs `Identity`) and different DBs (`fokus.db` vs `identity.db`); they are independent and can be implemented in either order.

## Scope

**In scope:**
1. Create 4 config VOs in `Fokus.Domain/Settings/ValueObjects/` and regroup the flat scalars under them on `AppSettings`.
2. EF `ComplexProperty` config for the 4 new VOs in `AppSettingsConfiguration.cs` + one Fokus migration (flat columns preserved by rename; defaults verified).
3. Rewrite every internal `settings.{Scalar}` read/write site (~40 across analytics, gRPC, sync, Xray, Settings endpoints, DI seeding) to the grouped path (`settings.BugRatio.AlertThreshold`, etc.).
4. Map flat DTO ↔ grouped VO at the Settings endpoint boundary (Mapster) so the wire contract (`GetSettingsResponse`, the 5 Save commands) and the Vue client stay byte-for-byte flat; and extend `AppSettingsRepository.SaveAsync`'s manual VO re-assignment block to the 4 new complex-type VOs so the write funnel persists them under EF change-tracking.
5. Encapsulate Identity `Person` + `User` (tighten setters; EF ctor if needed; keep factory/behavior methods as the sole mutation path).
6. Update `Fokus/CLAUDE.md` (AppSettings VO list) — and any AppSettings doc line.
7. Verification sweep + build.

**Explicitly out of scope (do NOT touch):**
- **Fokus sync aggregates stay anemic** — `Sprint`, `Ticket`, `Developer`, `SprintMembership`, `StatusTransition`, the TestRun/TestExecution/TestSet/link entities keep public setters. The repo-upsert sync path (B3, sanctioned) depends on them. Do NOT add `internal` setters / `internal` ctor / encapsulation factories to any Fokus aggregate. (Pass-3 Q3, ADR-007 reconciliation deferred.)
- **The wire contract.** `GetSettingsResponse`, the 5 Save commands, the Vue `AppSettings` TS interface, `settingsStore.ts`, and `client/src/api/settings.ts` keep the **flat** scalar shape. No frontend change in this pass (Q1=A). The 4 existing health/quality VOs are already nested in the wire — leave them exactly as-is.
- **The 4 existing health/quality VOs** (`HealthThresholdConfig`, `HealthWeightConfig`, `QaHealthThresholdConfig`, `QualitySubScoreWeightConfig`) and their `OwnsOne(...).ToJson()` mappings — untouched (Q4).
- **`BoardId`, `CompanyDomain`, and the 3 status lists** (`DoneStatuses`, `WorkflowStages`, `ExcludedFromScopeStatuses`) — stay top-level on `AppSettings` (Q2). `CompanyDomain` is auth-deferred.
- **Any behavior, formula, validation rule, or error shape change.** This is a structural pass: same values, same computations, new field grouping + new encapsulation. Pure refactor — no observable behavior difference.
- **Pass 3c** (analytics computation → domain) and **Pass 4** (Identity extraction) — not pre-empted.

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | `domain-patterns` (Value Objects + ADR-007 VO-vs-scalar) | Follow | The 4 VO names + their member fields/types/defaults; place in `Fokus.Domain/Settings/ValueObjects/` | — |
| 2 | `persistence-patterns` (Value Object Mapping → `ComplexProperty`) | Follow | The 4 VOs to map as `ComplexProperty` (flat columns); the 4 existing JSON-owned VOs stay `OwnsOne().ToJson()`; column-default parity per `ef-core.md` | — |
| 3 | (none) | — | Mechanical read/write-site rewrite of ~40 `settings.{Scalar}` references to grouped paths; enumerated file list | Log to lessons (no skill for grouped-path read-site sweep) |
| 4 | (none) | — | Flat-DTO ↔ grouped-VO Mapster mapping at the 6 Settings endpoints; keep wire flat | Log to lessons (boundary flatten/unflatten mapping — same family as Pass-3a boundary mapper gap) |
| 5 | `domain-patterns` (Partial Class Behavior Split + encapsulation) | Follow | `Person`/`User` setters to tighten; keep existing factory/behavior methods as sole mutation path; EF-materialization-of-`required`-props acceptance check | — |
| 6 | (none) | — | `Fokus/CLAUDE.md` AppSettings VO-list doc correction | — |
| 7 | (none) | — | Verification sweep + build | — |

**Disposition notes:**
- **Step 1 Follow `domain-patterns`** — the skill's "When to Use a Value Object vs a Flat Scalar (ADR-007)" section uses *this exact AppSettings case* as its worked example and points at the 4 existing VOs as the template. The plan supplies only the 4 new VO names + members; the developer invokes the skill for the `ValueObject` base shape and `GetEqualityComponents()` convention. Do not restate the VO structure.
- **Step 2 Follow `persistence-patterns`** — the skill's "Value Object Mapping" section covers `ComplexProperty` vs `OwnsOne`. The plan supplies only which VOs map which way; the developer invokes the skill for the config pattern + reads `ef-core.md` "Migration Defaults" for the default-parity check.
- **Steps 3, 4, 6, 7 are None** — no skill covers a grouped-path read-site sweep, a flatten/unflatten boundary map, a doc edit, or the verification battery. Logged as gaps.
- **Step 5 Follow `domain-patterns`** — encapsulation + partial-split is the skill's domain; the plan supplies only which setters to tighten and the EF-materialization acceptance check. `User.Role` is already `private set` + behavior methods — only `Person` (all-public) and `User`'s `PersonId`/`IsActive`/`RegistrationDate`/`LastLogin` need tightening.
- **No "Build."** All infrastructure exists: `ValueObject` base (`Blocks.Domain/ValueObjects/`), Mapster in CPM + `GlobalSettings.Scan` per-module DI (Pass 2), the 4 existing VOs as the live template.

## Domain Model Changes

**Arm 1 — AppSettings (Fokus).** Four new value objects extending `ValueObject` (same shape as the 4 existing config VOs — `Fokus.Domain/Settings/ValueObjects/HealthThresholdConfig.cs` is the template), each in its own file under `Fokus.Domain/Settings/ValueObjects/`:

| New VO | Members (name : type = default) — moved off `AppSettings` verbatim |
|--------|-------------------------------------------------------------------|
| `BugRatioConfig` | `AlertThreshold : int = 50`, `ConsecutiveSprintCount : int = 2`, `Target : decimal = 30`, `DefaultSpPerBug : int = 3` |
| `XrayConfig` | `Enabled : bool = false`, `ClientId : string? = null`, `ClientSecret : string? = null` |
| `CycleTimeConfig` | `StartStage : string? = null`, `EndStage : string? = null` |
| `SyncConfig` | `BackSprintCount : int = 20`, `PlanningWindowDays : int = 2` |

`AppSettings` after grouping holds: `Id`, `BoardId`, `DoneStatuses`, `WorkflowStages`, `ExcludedFromScopeStatuses` (top-level, unchanged); `HealthThresholds`, `HealthWeights`, `QaHealthThresholds`, `QualitySubScoreWeights`, `QualityHealthWeight` (unchanged); `CompanyDomain` (top-level, unchanged); and the 4 new VO members `BugRatio`, `Xray`, `CycleTime`, `Sync` (each initialized `= new()`). `CreateDefault()` is updated to construct the VOs (same default values). **The member-name mapping (domain → flat-wire) the developer must preserve for Mapster:**

| Old flat scalar (= wire name) | New grouped domain path |
|-------------------------------|-------------------------|
| `BugRatioAlertThreshold` | `BugRatio.AlertThreshold` |
| `BugRatioConsecutiveSprintCount` | `BugRatio.ConsecutiveSprintCount` |
| `BugRatioTarget` | `BugRatio.Target` |
| `DefaultSpPerBug` | `BugRatio.DefaultSpPerBug` |
| `XrayEnabled` | `Xray.Enabled` |
| `XrayClientId` | `Xray.ClientId` |
| `XrayClientSecret` | `Xray.ClientSecret` |
| `CycleTimeStartStage` | `CycleTime.StartStage` |
| `CycleTimeEndStage` | `CycleTime.EndStage` |
| `SyncBackSprintCount` | `Sync.BackSprintCount` |
| `PlanningWindowDays` | `Sync.PlanningWindowDays` |

> **Note — VO member names vs wire names:** the VO members are renamed for cohesion (`BugRatio.Target`, not `BugRatio.BugRatioTarget`). Mapster's flattening maps `BugRatio.Target` ↔ `BugRatioTarget` **only if told** — the prefix differs. The boundary map (Step 4) must use explicit Mapster member rules (e.g. `.Map(dst => dst.BugRatioTarget, src => src.BugRatio.Target)`) for every member where the flat wire name ≠ `{VO}.{Member}` by convention. The full table above is the mapping contract.

**`AppSettings` stays a plain class** (not an aggregate) with public setters — it is mutated directly by the Save endpoints and the DI seeder via `settings.Xray.Enabled = ...`. Do NOT encapsulate `AppSettings`. The VOs themselves use public setters too (matching the 4 existing config VOs, which have public setters and are assigned in `CreateDefault()` / save handlers).

**Arm 2 — Identity (`Person`, `User`).** Tighten setters so mutation flows only through factory + behavior methods (the existing methods already cover every mutation):
- `Person` (`Identity.Domain/Persons/Person.cs`): `FirstName`, `LastName`, `Email`, `PictureUrl`, `Source`, `ExternalId`, `CreatedAt`, `UserId` → `private set` (keep `required` on the three required members; `FullName` stays a computed get-only). Existing mutators: `CreateFromGoogle` (factory), `UpdateFromGoogle`, `LinkUser`, `RaiseCreated` — all in the same assembly, so the object-initializer factory still compiles against `private set`/`required`.
- `User` (`Identity.Domain/Users/User.cs`): `PersonId`, `IsActive`, `RegistrationDate`, `LastLogin` → `private set` (`Role` is already `private set`). Existing mutators: `CreateFromPerson` (factory), `RecordLogin`, `Deactivate`, `Reactivate`, `ChangeRole`.
- No new behavior methods. No EF ctor unless materialization requires one (see Step 5 acceptance — EF Core binds to private setters and uses the implicit/`private` parameterless ctor; add an explicit `private Person() {}` / `private User() {}` ONLY if the build proves it necessary).

## Data Model Changes

**Arm 1 (fokus.db):** One migration. The 11 flat AppSettings columns are restructured into `ComplexProperty` columns. With `ComplexProperty`, EF Core generates **flat columns named `{VO}_{Member}`** by default (e.g. `BugRatio_AlertThreshold`, `Xray_ClientSecret`). Because the column names change (`BugRatioAlertThreshold` → `BugRatio_AlertThreshold`), the migration must be a **rename, not drop+add**, to preserve the seeded singleton row's values (especially `Xray_ClientSecret` and the sync/cycle-time config). Verify each new column's default matches the VO member initializer per `docs/conventions/ef-core.md` "Migration Defaults". `BoardId`, `CompanyDomain`, the status lists, and the 4 JSON-owned VO columns are unchanged.

**Arm 2 (identity.db):** **No migration.** Tightening setter accessibility (`{ get; set; }` → `{ get; private set; }`) changes only CLR accessibility, not the EF model or column shape. `Person`/`User` keep identical tables, columns, indexes. (If an explicit `private` ctor is added, that too is model-neutral.) Confirm with `dotnet ef migrations add` producing an **empty** migration, or by the model snapshot being unchanged — do NOT generate a no-op migration; the build + an unchanged snapshot is the proof.

## Implementation Steps

**Ordering (binding):**
- **Arm 1 order:** Step 1 (VOs + AppSettings regroup) → then Steps 2, 3, 4 in any order, but the build is RED until all three land (regrouping breaks ~40 read sites + the EF config + the boundary maps simultaneously). Treat Steps 1–4 as one atomic Fokus change: the solution will not compile mid-arm. Build green is expected only after Step 4.
- **Arm 2 (Step 5)** is independent of Arm 1 — do it before or after, its own compile unit.
- Step 6 (docs) any time. Step 7 verifies last.

### Step 1 — Create the 4 AppSettings config VOs + regroup `AppSettings`

**Disposition: Follow `domain-patterns`** (Value Objects + the ADR-007 VO-vs-scalar section, which uses this exact AppSettings case as its worked example). **Confidence: high** — the 4 existing config VOs are the live template.

Create the 4 VOs and move the flat scalars onto them. Members, types, and defaults are in **Domain Model Changes → Arm 1** (the table). Update `CreateDefault()` to construct the VOs with the same default values.

- **Create** `Fokus.Domain/Settings/ValueObjects/BugRatioConfig.cs`, `XrayConfig.cs`, `CycleTimeConfig.cs`, `SyncConfig.cs` — each extends `ValueObject`, implements `GetEqualityComponents()`, public setters (match `HealthThresholdConfig.cs`).
- **Modify** `Fokus.Domain/Settings/AppSettings.cs` — remove the 11 flat scalars; add `public BugRatioConfig BugRatio { get; set; } = new();` and the other 3; update `CreateDefault()`.

**Accept:**
- 4 VO files exist, each extending `ValueObject` with the members/defaults from the table; `GetEqualityComponents()` yields every member.
- `AppSettings` no longer declares any of the 11 flat scalars; it declares `BugRatio`/`Xray`/`CycleTime`/`Sync` initialized `= new()`. `BoardId`, `CompanyDomain`, status lists, and the 4 existing health/quality VOs are unchanged.
- `CreateDefault()` produces the same effective values as before (e.g. `BugRatio.AlertThreshold == 50`, `Xray.Enabled == false`, `Sync.PlanningWindowDays == 2`).

### Step 2 — EF `ComplexProperty` config + Fokus migration

**Disposition: Follow `persistence-patterns`** (Value Object Mapping → `ComplexProperty`). **Confidence: medium** — two delicate points: (1) the rename-not-drop migration (data preservation on the seeded singleton), and (2) the all-/mostly-nullable-member VOs (`CycleTimeConfig`, `XrayConfig`) under EF's complex-type rules.

Map the 4 new VOs as `ComplexProperty` (flat columns) in `AppSettingsConfiguration.cs`; leave the 4 existing VOs as `OwnsOne(...).ToJson()`. Preserve the `HasMaxLength` on the former `XrayClientId`/`XrayClientSecret` (now `Xray.ClientId`/`Xray.ClientSecret`, 512) and `CompanyDomain` (256, still top-level). Generate one migration.

- **Modify** `src/Services/Fokus/Fokus.Persistence/Configurations/AppSettingsConfiguration.cs` — add `builder.ComplexProperty(s => s.BugRatio)`, `.Xray`, `.CycleTime`, `.Sync` with per-member max-length where applicable; remove the 11 obsolete `builder.Property(s => s.{flat})` lines.
- **EF complex-type nullability (binding constraint — pre-empts an EF diagnostic detour):** EF Core `ComplexProperty` requires the complex *instance* to be **non-null** (optional complex properties are not supported in EF 8/9) — the `= new()` initializer on each `AppSettings` VO member (Step 1) satisfies this. Individual *member* nullability is fine: the nullable members (`Xray.ClientId`, `Xray.ClientSecret`, `CycleTime.StartStage`, `CycleTime.EndStage`) map as **nullable columns**, and the non-nullable members (`int`/`bool`/`decimal`) map as **non-null** columns. `CycleTimeConfig` is all-nullable members but a non-null instance — that is valid; do NOT mark the `ComplexProperty` itself optional. Confirm the migration emits nullable columns for those 4 members and non-null for the rest.
- **Create** the migration: `dotnet ef migrations add GroupAppSettingsScalarsIntoVOs --project src/Services/Fokus/Fokus.Persistence --startup-project src/Host` (use the `IDesignTimeDbContextFactory` if startup fails per `ef-core.md`).
- **Edit the generated migration to RENAME** the 11 columns (`BugRatioAlertThreshold` → `BugRatio_AlertThreshold`, etc. — full list in the Domain Model Changes mapping table, with `_` separator) rather than drop+add, so the seeded singleton's values survive. Verify defaults match the VO initializers per `ef-core.md` "Migration Defaults".

**Accept:**
- `AppSettingsConfiguration.cs` maps the 4 new VOs via `ComplexProperty`; the 4 existing VOs remain `OwnsOne().ToJson()`.
- The migration renames (not drops) the 11 columns; the model snapshot reflects the `ComplexProperty` columns; column defaults match the VO member initializers.
- `Xray_ClientId`/`Xray_ClientSecret` keep `HasMaxLength(512)`.
- `dotnet build SprintRituals.slnx` green (after Steps 3 & 4 also land — see ordering).

### Step 3 — Rewrite all internal `settings.{Scalar}` read/write sites to grouped paths

**Disposition: None** (mechanical cross-file rewrite; no skill). **Confidence: high** — call sites fully enumerated; the rename is the grouped path from the Domain Model Changes table.

Replace every `settings.{flatScalar}` reference with the grouped path (`settings.BugRatio.AlertThreshold`, etc.). This is read sites in analytics/gRPC/sync/Xray AND write sites in the DI seeder. Do NOT change any logic — only the member path.

**Read/write sites to rewrite (verified — the targeted scalars only; `ExcludedFromScopeStatuses`/`DoneStatuses`/`WorkflowStages` are NOT targeted and must NOT change):**
- **DI seeder (writes):** `src/Services/Fokus/Fokus.API/DependencyInjection.cs:83-85` — `settings.XrayEnabled/XrayClientId/XrayClientSecret =` → `settings.Xray.Enabled/ClientId/ClientSecret =`.
- **gRPC:** `Fokus.API/Grpc/SprintAnalyticsGrpcService.cs` (`XrayEnabled` :40; `DefaultSpPerBug` :231; `PlanningWindowDays` :239).
- **Sync:** `Features/Sync/SprintIssueSyncService.cs` (`PlanningWindowDays` :62; `XrayEnabled` :179).
- **Xray:** `Features/Xray/XrayIssueSyncService.cs` (:28 ×3, :83, :170); `Features/Xray/TestConnection/TestConnectionEndpoint.cs` (:15 ×3, :19 ×2); `Features/Xray/SyncXray/SyncXrayEndpoint.cs` (:21).
- **Sprints:** `Features/Sprints/GetTestTimeline/GetTestTimelineEndpoint.cs` (`XrayEnabled` :33; `PlanningWindowDays` :111).
- **Analytics services & endpoints (the bulk):** `BugRatioService.cs` (note: carries `BugRatioConsecutiveSprintCount` at :219/:342 plus `BugRatioAlertThreshold`/`DefaultSpPerBug` — all 11 members are real reads, the grep does not over-match), `CarryOverService.cs`, `CompletionChecker.cs`, `CycleTimeService.cs`, `DeveloperDetailService.cs`, `DeveloperProgressService.cs`, `DeveloperQualityService.cs`, `DeveloperThroughputService.cs`, `EpicProgressService.cs`, `LeaderboardService.cs`, `QaMetricsService.cs`, `QaTrendsService.cs`, `ScopeChangeService.cs`, `SprintSummaryService.cs`, `TestTimelineService.cs`, `TransitionAttributionChecker.cs`, and the endpoints `GetDeveloperDetail`, `GetDeveloperQuality`, `GetEpicProgress`, `GetFailingTickets`, `GetQaMetrics`, `GetQaWorkload`, `GetSprintSummary`, `GetUntestedTickets`, `GetQaTrends` — all under `Fokus.API/Features/Analytics/`.
- **Settings endpoints (non-boundary reads):** `Features/Settings/GetCycleTimeBoundaries/GetCycleTimeBoundariesEndpoint.cs` (:17, :20 — `CycleTimeStartStage`/`EndStage` null-coalesce). (The 5 Save endpoints + `GetSettings` are handled in Step 4 as boundary maps, not here.)

**Verification sub-step (binding — grep before declaring done):** after the rewrite,
`rg -n "\.(BugRatioAlertThreshold|BugRatioConsecutiveSprintCount|DefaultSpPerBug|BugRatioTarget|CycleTimeStartStage|CycleTimeEndStage|XrayEnabled|XrayClientId|XrayClientSecret|SyncBackSprintCount|PlanningWindowDays)\b" src --glob '*.cs' -g '!*/obj/*' -g '!*/bin/*'`
must return **only** hits that are the *command/validator/response* field references inside the Settings `Save*` feature folders (those are the flat wire DTO fields, which stay flat — NOT `AppSettings` reads) — and **zero** `AppSettings`-variable hits. Note the member access is matched without a `settings.` prefix because at least one site uses a differently-named variable (`settings2.XrayEnabled` at `SprintIssueSyncService.cs:179`); match the member name, then confirm each remaining hit is a flat DTO field, not an `AppSettings` read. If any `AppSettings` read/write remains flat, it's a missed site — fix before proceeding.

**Accept:**
- The grep above returns zero hits.
- No logic changed — only member paths (diff shows only `settings.X` → `settings.Group.X`).
- The DI seeder writes via `settings.Xray.*`.

### Step 4 — Flat-DTO ↔ grouped-VO boundary mapping at the Settings endpoints + the repository write funnel (wire stays flat)

**Disposition: None** (flatten/unflatten boundary map + EF change-tracking write path; no skill — same family as the Pass-3a boundary mapper). **Confidence: medium** — two delicate points: (1) Mapster member-name rules must be explicit because the VO member names differ from the flat wire names (see the Domain Model Changes note); (2) `AppSettingsRepository.SaveAsync`'s `SetValues` + manual-reassignment write path must be extended to the 4 new complex-type VOs (see the repository bullet below).

Keep the wire flat (Q1=A): `GetSettingsResponse`, the 5 Save commands, and the Vue client are **unchanged**. Map flat-DTO ↔ grouped-VO at the boundary so reads project grouped→flat and writes apply flat→grouped. **Every Save endpoint funnels through `AppSettingsRepository.SaveAsync`** — that write path must carry the new VOs too (see the repository bullet).

- **`GetSettings`** (`Features/Settings/GetSettings/GetSettingsEndpoint.cs`): the response builder currently reads `settings.BugRatioAlertThreshold` etc. — repoint to `settings.BugRatio.AlertThreshold` etc. (the response DTO stays flat; keep `MaskSecret(settings.Xray.ClientSecret)`). `GetSettingsResponse` shape is unchanged.
- **The 5 Save endpoints** currently apply the flat command to `AppSettings` via `req.Adapt(existing)` (BugRatioAlerts, SyncConfig, AnalyticsTargets) or explicit assignment (XraySettings, CycleTimeBoundaries):
  - `SaveBugRatioAlerts`, `SaveSyncConfig`, `SaveAnalyticsTargets` use `req.Adapt(existing)` — this now needs an explicit Mapster rule set mapping flat command fields → grouped VO members (e.g. `SaveBugRatioAlertsCommand.BugRatioAlertThreshold` → `existing.BugRatio.AlertThreshold`). Add a Mapster `IRegister` config (or per-endpoint `TypeAdapterConfig`) covering each Save command → `AppSettings` grouped path. Reference the Pass-2/3a `IRegister` pattern (`Features/Sync/JiraDomainMapper.cs`) for the registration shape.
  - `SaveXraySettings` (explicit assignment, with the secret-preservation rule) — repoint `existing.XrayEnabled/ClientId` → `existing.Xray.Enabled/ClientId`, and keep the "only overwrite secret if non-empty" rule on `existing.Xray.ClientSecret`.
  - `SaveCycleTimeBoundaries` (explicit assignment + ordered-stage validation) — repoint `settings.CycleTimeStartStage/EndStage` → `settings.CycleTime.StartStage/EndStage`; the validation logic and response shape are unchanged.
- **Mapping contract:** the flat↔grouped member pairs are the Domain Model Changes table. Where the flat name ≠ `{VO}.{Member}` by Mapster's default flattening convention (it always differs here — the VO members dropped their prefixes), the rule must be **explicit**.
- **`AppSettingsRepository.SaveAsync` — the write funnel (binding; the single path every Save endpoint flows through):** `src/Services/Fokus/Fokus.Persistence/Repositories/AppSettingsRepository.cs:27-32` does `db.Entry(existing).CurrentValues.SetValues(settings)` (line 27) and then **manually re-assigns each non-scalar member that `SetValues` does not propagate** — the 2 JSON-owned VOs (`HealthThresholds`, `HealthWeights`) and the 3 `List<string>` converters (lines 28-32). The 11 flat scalars are copied today only because `SetValues` propagates plain scalars. After Step 1 groups them into the 4 `ComplexProperty` VOs (`BugRatio`, `Xray`, `CycleTime`, `Sync`), they become complex-type members — the **same category `CurrentValues.SetValues(object)` does NOT deep-copy** (exactly why lines 28-29 already exist for the JSON-owned VOs). **Extend the manual re-assignment block (after line 32) with:** `existing.BugRatio = settings.BugRatio; existing.Xray = settings.Xray; existing.CycleTime = settings.CycleTime; existing.Sync = settings.Sync;` — mirroring the existing JSON-VO handling. The in-code precedent comment at `SaveHealthConfigEndpoint.cs:12-14` already pre-warned this Pass-3 hazard ("ComplexProperty … reference-swap semantics must be preserved for EF change-tracking") — honor it. **Modify** `src/Services/Fokus/Fokus.Persistence/Repositories/AppSettingsRepository.cs`.

**Accept:**
- `GetSettingsResponse`, all 5 Save command/response records, `client/src/types/index.ts` `AppSettings`, `client/src/api/settings.ts`, and `settingsStore.ts` are **unchanged** (verify via diff — zero changes under `client/`).
- A round-trip is value-preserving: a `GET /api/settings` after each `PUT` returns the same flat values as before the refactor (same field names, same values).
- **`AppSettingsRepository.SaveAsync` re-assigns all 4 new VOs** (`existing.BugRatio/Xray/CycleTime/Sync = settings.{VO}`) alongside the existing JSON-VO/list re-assignments (lines 28-32).
- **Round-trip from a detached `settings`:** calling `SaveAsync` with a `settings` instance that is NOT the tracked `existing` (e.g. a freshly-constructed/`Adapt`-ed `AppSettings`) followed by a fresh `GetAsync` still persists all 4 new VOs' values — proving the re-assignment block (not just the tracked-instance in-place mutation path) carries the grouped VOs. This guards the "adapt onto a fresh instance / refactor SaveAsync to take a detached arg" failure mode that `SetValues` would silently drop.
- `SaveXraySettings` still preserves the existing secret when the request secret is empty; `GetSettings` still masks the secret.
- `dotnet build SprintRituals.slnx` green (Arm 1 compiles end-to-end).

### Step 5 — Encapsulate Identity `Person` + `User`

**Disposition: Follow `domain-patterns`** (Partial Class Behavior Split + encapsulation). **Confidence: high** — zero external direct property writes; all mutation already routes through existing factory/behavior methods.

Tighten setters on `Person` and `User` so they can only be set inside the domain (factory + behavior methods). No new behavior, no wire change, no migration.

- **Modify** `src/Services/Identity/Identity.Domain/Persons/Person.cs` — `FirstName`, `LastName`, `Email`, `PictureUrl`, `Source`, `ExternalId`, `CreatedAt`, `UserId` → `private set`. Keep `required` on `FirstName`/`LastName`/`Email`; `FullName` stays computed get-only.
- **Modify** `src/Services/Identity/Identity.Domain/Users/User.cs` — `PersonId`, `IsActive`, `RegistrationDate`, `LastLogin` → `private set` (`Role` already `private set`).
- The existing factory/behavior methods (`CreateFromGoogle`, `UpdateFromGoogle`, `LinkUser`, `RaiseCreated`, `CreateFromPerson`, `RecordLogin`, `ChangeRole`, `Deactivate`, `Reactivate`) are the only mutation path — leave them. They are in the same assembly, so object-initializer factories compile against `private set` + `required`.
- Add an explicit `private Person() {}` / `private User() {}` **only if** the build proves EF needs it for materialization (it generally does not — EF binds private setters and uses the implicit parameterless ctor).

**Accept:**
- `Person` and `User` expose no public setter (every property is `get`-only or `private set`; `FullName` get-only computed).
- All 9 factory/behavior methods remain and still compile; `AuthConfiguration.cs`, `UpdateRole`/`UpdateStatus` endpoints, and `ActiveUserPreProcessor` compile unchanged (they already use only methods + getters).
- **No identity.db migration** — `dotnet ef migrations add` (dry, then discard) produces an empty migration, OR the model snapshot is provably unchanged. EF still materializes `Person`/`User` (verify: build green; if a login round-trip is run, claims still populate).
- `dotnet build SprintRituals.slnx` green.

### Step 6 — Update `Fokus/CLAUDE.md` AppSettings VO list

**Disposition: None** (doc correction). **Confidence: high.**

`src/Services/Fokus/CLAUDE.md` "Domain model → Value objects" lists only `HealthThresholdConfig, HealthWeightConfig` and the "AppSettings singleton" line says it "Holds board config, done-status list, workflow stages, health thresholds, health weights." Update both to reflect the full current VO set (the 4 existing + the 4 new `BugRatioConfig`, `XrayConfig`, `CycleTimeConfig`, `SyncConfig`) and note the wire stays flat (mapped at the Settings endpoint boundary). Do not invent other doc changes.

**Accept:**
- `Fokus/CLAUDE.md` names all 8 AppSettings config VOs and states the wire-flat boundary-mapping rule (ADR-007 + Q1=A).

### Step 7 — Verification sweep + build

**Disposition: None** (verification). **Confidence: high.** Objective done-gate.

1. `rg -n "\.(BugRatioAlertThreshold|BugRatioConsecutiveSprintCount|DefaultSpPerBug|BugRatioTarget|CycleTimeStartStage|CycleTimeEndStage|XrayEnabled|XrayClientId|XrayClientSecret|SyncBackSprintCount|PlanningWindowDays)\b" src --glob '*.cs' -g '!*/obj/*' -g '!*/bin/*'` → the **only** remaining hits are flat command/validator/response DTO fields inside the Settings `Save*` feature folders (the wire stays flat); **zero** `AppSettings`-variable reads/writes remain flat (prefix-agnostic match catches `settings2.` too).
2. **Zero public setters on `Person`/`User`** — type-agnostic grep (do NOT use a fixed type allow-list, which would silently miss a property of an un-listed type): `rg -n "public\s+[\w\.<>\[\]?]+\s+\w+\s*\{\s*get;\s*set;\s*\}" src/Services/Identity/Identity.Domain/Persons/Person.cs src/Services/Identity/Identity.Domain/Users/User.cs` → **zero** matches. Because the file set is exactly these two small files, also do a **manual confirmation** that every declared property is `get`-only (computed) or `private set` — record the property-by-property confirmation in `implementation.md` (the regex is the fast gate; the manual read is the proof).
3. `git diff --stat client/` → **empty** (frontend untouched; wire flat).
4. The 4 new VO files exist under `Fokus.Domain/Settings/ValueObjects/`; `AppSettingsConfiguration.cs` uses `ComplexProperty` for them and `OwnsOne().ToJson()` for the 4 existing.
5. Exactly one new Fokus migration; it renames (not drops) the 11 columns; no identity.db migration.
6. **`AppSettingsRepository.SaveAsync` carries the new VOs:** `rg -n "existing\.(BugRatio|Xray|CycleTime|Sync)\s*=" src/Services/Fokus/Fokus.Persistence/Repositories/AppSettingsRepository.cs` → 4 matches (the manual re-assignment block was extended). Plus the detached round-trip acceptance from Step 4 is recorded.
7. `dotnet build SprintRituals.slnx` → green (filter `error CS`; ignore MSB3026/3027/3492 file-lock per `project-rules`).

**Accept:** all 7 checks pass; build green; `implementation.md` records each grep's actual output, the two-file property confirmation, and the migration diff summary.

## Cross-Service Changes

None across module boundaries. No gRPC, integration-event, or wire-contract change. `SprintAnalyticsGrpcService` is rewritten only at its internal `settings.X` read sites (Step 3) — its proto contract and outputs are identical. The Vue client is untouched.

## Migration Notes

- **Fokus:** one migration (Step 2) — `ComplexProperty` flat columns via **column rename** to preserve the seeded singleton's data. Verify defaults per `docs/conventions/ef-core.md` "Migration Defaults". `HasData`/`ToJson` incompatibility is not a concern (AppSettings seeds at runtime via `CreateDefault()` in `DependencyInjection.cs`, not `HasData`).
- **Identity:** no migration — setter-accessibility change is model-neutral.
- Package set unchanged (Mapster + `ValueObject` base already present). `dotnet build` is the only restore.

## Testing Strategy

No automated suite (project decision — manual testing after each pass). Verification is:
1. **Grep-checkable acceptance** (Step 7 battery) — primary objective gate.
2. **Build** — `dotnet build SprintRituals.slnx` green (expect RED mid-Arm-1 until Steps 1–4 all land; green after).
3. **Wire-stability (read-level):** `git diff --stat client/` empty; `GetSettingsResponse` + the 5 Save command/response records structurally unchanged.
4. **Behavioral parity (read-level):** for each rewritten read site, confirm only the member path changed (no logic edit); `CreateDefault()` values match the pre-refactor defaults; `SaveXraySettings` secret-preservation + `GetSettings` masking intact.
5. **Write-funnel round-trip (read-level + manual):** `AppSettingsRepository.SaveAsync` re-assigns all 4 new VOs; a Save→fresh-`GetAsync` round-trip preserves them even when `settings` is detached (not the tracked `existing`) — guards the `SetValues`-drops-complex-types failure mode (HIGH-1).
6. **Manual smoke (user, post-pass):**
   - Settings: load `/settings`, change bug-ratio alerts / sync config / analytics target / cycle-time boundaries / Xray settings, save, reload — values persist identically; Xray secret stays masked and is preserved when left blank.
   - Analytics: open a sprint summary / bug-ratio / cycle-time view with real data — numbers identical to pre-pass (the VO grouping changes no computation).
   - Identity: Google login round-trip — bootstrap + regular login still create/update Person/User and populate claims; user activate/deactivate + role change still work.
   - Migration: app starts, `fokus.db` migrates, the existing AppSettings row keeps its values (Xray secret/sync config not reset).

## KB Impact

None. `docs/kb/` does not exist (verified — no `docs/kb/index.md`). This pass regroups settings fields and tightens setters; it changes no business rule, formula, or edge case. No KB step warranted.

## Open Questions

None blocking — Q1–Q4 are Answered (questions.md). Developer-time judgments flagged inline (not blockers):
- **Step 2 — column rename vs drop+add:** the migration MUST rename to preserve the seeded singleton's values. If the EF-generated migration emits drop+add, hand-edit to `RenameColumn` calls (full name map in Domain Model Changes).
- **Step 2 — nullable complex members:** keep the `ComplexProperty` instance non-null (`= new()`); `CycleTimeConfig` (both members) + `Xray.ClientId/ClientSecret` map as nullable columns. Do not mark the complex property itself optional.
- **Step 4 — Mapster member rules are explicit, not convention:** the VO members dropped their prefixes (`BugRatio.Target`, not `BugRatio.BugRatioTarget`), so default flattening will NOT line up — every flat↔grouped pair needs an explicit `.Map(...)` rule per the mapping table. Do not rely on Mapster auto-flattening here.
- **Step 4 — repository write funnel (HIGH from critic cycle 1):** `AppSettingsRepository.SaveAsync`'s `CurrentValues.SetValues` does NOT deep-copy the new complex-type VOs (same reason the JSON-VO/list re-assignments exist at lines 28-32). Extend the manual re-assignment block with `existing.BugRatio/Xray/CycleTime/Sync = settings.{VO}`. The in-code precedent is `SaveHealthConfigEndpoint.cs:12-14`. If you instead `Adapt` onto a fresh `AppSettings` or feed `SaveAsync` a detached instance, `SetValues` silently drops the grouped VOs — covered by the detached-round-trip acceptance.
- **Step 5 — EF ctor:** add a `private` parameterless ctor only if the build proves EF needs it; default is none (EF uses the implicit ctor + private setters).

**Flags for the team-lead (not blockers):**
1. **Skill gap — VO-grouping refactor.** Steps 3–4 (group existing flat scalars → VOs + read-site sweep + flatten/unflatten boundary map) have no dedicated skill. Strongest candidate: a `group-scalars-into-value-object` skill (refactoring family, same as the Pass-3a "relocate-domain-dto-factory" gap). Logged to lessons.
2. **Atomic Arm-1 build window.** Steps 1–4 leave the solution non-compiling between them — this is expected (a wide rename), not a developer error. The done-gate is build-green after Step 4, not after each sub-step.
