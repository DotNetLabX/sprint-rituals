# Pass 3b — AppSettings VOs + Identity Encapsulation — Questions

Phase 1 analysis. Scope source: `docs/backlog.md` row 3b + `docs/architecture/decisions.md` **ADR-007**; decisions locked in `docs/specs/adhoc-Pass3-DomainEnrichment/delivery/questions.md` Q3 (Answered) — **do not re-litigate scope**. There is no `spec.md` (adhoc refactoring pass; the ADR register + the Pass-3 split decisions are the binding inputs).

**Scope (locked by Pass-3 Q3):** (a) group AppSettings flat scalars into config VOs + (b) encapsulate Identity `Person`/`User`; **Fokus sync aggregates stay anemic** (upsert depends on public setters). ADR-007.

**Evidence gathered (Phase 1, this session):**
- `AppSettings` (`Fokus.Domain/Settings/AppSettings.cs`) is a plain settings class (not an aggregate; no `: AggregateRoot`). 4 config VOs already extracted (`HealthThresholdConfig`, `HealthWeightConfig`, `QaHealthThresholdConfig`, `QualitySubScoreWeightConfig`), each `OwnsOne(...).ToJson()`. ~11 flat scalars remain targetable for grouping: `BugRatioAlertThreshold`, `BugRatioConsecutiveSprintCount`, `DefaultSpPerBug`, `BugRatioTarget` (bug-ratio cluster); `CycleTimeStartStage`, `CycleTimeEndStage` (cycle-time cluster); `XrayEnabled`, `XrayClientId`, `XrayClientSecret` (xray cluster); `SyncBackSprintCount`, `PlanningWindowDays` (sync cluster).
- **Consumer blast radius (the dominant cost):** the targeted scalars are read at **~40 `settings.X` sites across ~25 files** — every analytics service (`SprintSummaryService` ×11, `ScopeChangeService` ×9, `BugRatioService` ×6, plus ~15 others), the gRPC service, both sync services, the Xray feature, and 6 Settings endpoints. Grouping into VOs renames every read site (`settings.DefaultSpPerBug` → `settings.BugRatio.DefaultSpPerBug`, etc.).
- **Wire contract is entirely flat for these scalars:** `GetSettingsResponse`, the 5 Save commands (`SaveBugRatioAlertsCommand`, `SaveXraySettingsCommand`, `SaveSyncConfigCommand`, `SaveCycleTimeBoundariesCommand`, `SaveAnalyticsTargetsCommand`), the Vue `AppSettings` TS interface (`client/src/types/index.ts:198`), and `settingsStore.ts` all expose the scalars flat. The 4 existing VOs ARE exposed nested in the wire — so both flat and nested precedents exist.
- **Persistence:** the targeted scalars are flat columns today (`BugRatioAlertThreshold INTEGER`, `XrayClientId TEXT`, …). The 4 existing VOs use `OwnsOne(...).ToJson()`.
- **Identity encapsulation is genuinely low-risk:** `Person` has all-public setters + `required` props, no ctor encapsulation; `User` is partially encapsulated (`Role` private-set + behavior methods). **Zero external direct property writes anywhere in the codebase** — all mutation already routes through factory + behavior methods (`CreateFromGoogle`, `UpdateFromGoogle`, `LinkUser`, `RaiseCreated`, `CreateFromPerson`, `RecordLogin`, `ChangeRole`, `Deactivate`, `Reactivate`). EF persists via `IEntityTypeConfiguration` (no JSON owned types on Person/User).

---

## Q1: AppSettings VOs are domain-internal — keep the wire (response + commands + Vue client) FLAT?
**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis

**Context:** ADR-007's intent is **domain encapsulation** ("group cohesive scalar fields into value objects / owned types"), not reshaping the API contract. The Save commands, `GetSettingsResponse`, the Vue `AppSettings` interface, and `settingsStore` are all flat for these scalars today, and Pass 3a established that refactoring passes have **no wire impact**. Mapster flattens/unflattens by name automatically (`BugRatio.AlertThreshold` ↔ `BugRatioAlertThreshold`), so the endpoints can map domain-VO ↔ flat-DTO at the boundary with no frontend change.

**Question:** Confirm the target is **domain-only VO grouping with the wire kept flat** (Option A): group the scalars into VOs inside `AppSettings`, keep `GetSettingsResponse` + the 5 Save commands + the Vue client flat, and map flat↔grouped at the endpoint boundary via Mapster. The alternative (Option B — nest the VOs in the response/commands/Vue types) breaks the wire, touches the frontend, and yields no UI benefit (the client already groups these into its own sections).

My recommendation: **Option A (wire stays flat, frontend untouched).** It satisfies ADR-007 (the *domain* gets the VOs) while honoring the refactor-pass no-wire-impact rule. Confirm A, or say if you want the VOs exposed in the wire (B).

### Answer
**A.** Wire stays flat; map flat↔grouped at the endpoint boundary via Mapster. Frontend untouched. (user, 2026-06-07)

---

## Q2: Which scalar clusters become VOs, and what are the VO names?
**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis

**Context:** ADR-007 names three example VOs explicitly: `BugRatioConfig`, `XrayConfig`, `CycleTimeConfig`. The remaining sync scalars (`SyncBackSprintCount`, `PlanningWindowDays`) form a natural fourth cluster. The grouping I propose, by cohesion:
- **`BugRatioConfig`** ← `BugRatioAlertThreshold`, `BugRatioConsecutiveSprintCount`, `BugRatioTarget`, `DefaultSpPerBug` (all bug-ratio analytics inputs; `DefaultSpPerBug` is the SP-per-bug used in bug-ratio math, set via the `SaveBugRatioAlerts` endpoint, so it belongs here).
- **`XrayConfig`** ← `XrayEnabled`, `XrayClientId`, `XrayClientSecret`.
- **`CycleTimeConfig`** ← `CycleTimeStartStage`, `CycleTimeEndStage`.
- **`SyncConfig`** ← `SyncBackSprintCount`, `PlanningWindowDays`.

`BoardId`, `CompanyDomain`, and the three status lists (`DoneStatuses`, `WorkflowStages`, `ExcludedFromScopeStatuses`) stay as top-level members (no cohesive cluster; `CompanyDomain` is auth-deferred). The 4 existing health/quality VOs are untouched.

**Question:** Confirm the four-VO grouping and names above (`BugRatioConfig`, `XrayConfig`, `CycleTimeConfig`, `SyncConfig`), and confirm `DefaultSpPerBug` belongs in `BugRatioConfig` (vs `SyncConfig` or top-level). Names are a binding contract once chosen — if you prefer different names (e.g. `BugRatioSettings`), say so now.

My recommendation: the four VOs as named, `DefaultSpPerBug` in `BugRatioConfig`. **Binding only on whether to group and on the public names; the EF persistence mechanism (below) and read-site rewrites are my call.**

### Answer
**Accepted verbatim.** Four VOs: `BugRatioConfig`, `XrayConfig`, `CycleTimeConfig`, `SyncConfig`. `DefaultSpPerBug` in `BugRatioConfig`. `BoardId`, `CompanyDomain`, and the three status lists stay top-level. The 4 existing health/quality VOs untouched. (user, 2026-06-07)

---

## Q3: Identity `Person`/`User` encapsulation depth — and does it stay in 3b or defer to Pass 4?
**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis

**Context:** Pass-3 Q3 (Answered) put Identity `Person`/`User` encapsulation in 3b. Evidence shows it is low-risk: zero external direct property writes; all mutation already flows through factory + behavior methods. The encapsulation work is therefore: tighten setters (`{ get; set; }` → `{ get; private set; }`/`init`), add an `internal`/`private` parameterless ctor for EF if needed, and confirm EF still materializes the `required` props. `User.Role` is already private-set; the gap is `Person` (all-public) and `User`'s `PersonId`/`IsActive`/`RegistrationDate`/`LastLogin`.

But Pass 4 is **IdentityExtraction** (finish the Fokus→Identity cut), and ADR-015 marks Identity extraction as in-flight. Encapsulating Person/User now (3b) is independent of the extraction mechanics, so doing it here is safe — but I want to confirm you still want it in 3b vs folding it into Pass 4.

**Question:** Confirm Identity `Person`/`User` encapsulation stays in **Pass 3b** (not deferred to Pass 4), and confirm the depth: tighten all setters to `private set`/`init` + add an EF ctor as needed, keeping all existing factory/behavior methods as the only mutation path. No new behavior, no API/wire change.

My recommendation: **keep it in 3b** (Pass-3 Q3 already decided this; it's low-risk and orthogonal to the Pass-4 extraction). Confirm, or defer to Pass 4.

### Answer
**Stays in 3b.** Depth: tighten all setters to `private set`/`init` + add an EF ctor as needed; mutation only via the existing factory/behavior methods. No new behavior, no API/wire change. (user, 2026-06-07)

---

## Q4 (flag, not a blocker): EF persistence mechanism for the new VOs — `ComplexProperty` (flat columns) vs `OwnsOne().ToJson()` (JSON column)
**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis

**Context:** This is a **technical decision within my authority** (no user requirement governs it) — flagged for visibility, not as a blocker. The 4 existing config VOs use `OwnsOne(...).ToJson()` (one JSON column each). For the new scalar VOs there are two mechanisms:
- **`ComplexProperty`** (ADR-007 explicitly lists it) → keeps the values as **flat columns** (`BugRatio_AlertThreshold`, etc., or same names via config). Lowest data-migration risk (column rename, values preserved), no value-converter overhead, queryable.
- **`OwnsOne(...).ToJson()`** (matches the 4 existing VOs) → one JSON column per VO; the old flat columns are dropped and replaced, so the migration must **copy the singleton row's existing values into the new JSON column** or the seeded values reset (AppSettings is a single Id=1 row that re-seeds on startup if missing, so blast is limited, but a sync-config/xray-secret reset on deploy is undesirable).

**Plan decision (mine, recordable):** use **`ComplexProperty`** for the new scalar VOs (flat columns, values preserved through a column rename in the migration), leaving the 4 existing health/quality VOs as JSON-owned. Rationale: ADR-007 sanctions ComplexProperty; it avoids the data-copy migration and keeps the columns queryable. The migration must verify defaults match the entity initializers per `docs/conventions/ef-core.md` "Migration Defaults".

**Question (optional override):** Any objection to `ComplexProperty` (flat columns) over JSON-owned for the new VOs? If you'd rather the new VOs match the 4 existing JSON-owned VOs for consistency, the plan will add an explicit data-copy step to the migration. Default if no answer: **ComplexProperty**.

### Answer
**ComplexProperty** (flat columns) for the new VOs; the 4 existing health/quality VOs stay JSON-owned (untouched). (user, 2026-06-07)
