# Pass 2 — SliceHygiene — Questions

Phase 1 analysis questions. Scope source: `docs/backlog.md` (Pass 2 row "Endpoint split + naming + Mapster" + Pass 1 prerequisite "add Mapster to Directory.Packages.props (CPM)"). Binding: ADR-008 (vertical-slice layout), ADR-009 (endpoint type naming), ADR-010 (mapping convention), ADR-013 (CPM). There is no spec.md — this is an adhoc refactoring pass.

---

## Q1: `*Request` rename scope — does ADR-009 reach gRPC and external-module contracts?
**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis

**Context:** ADR-009 renames endpoint request types from `{X}Request` to `{Action}{Entity}Command`/`Query`. I found ~29 FastEndpoints request types in scope (Identity + Fokus `Features/*`). But three more `*Request` types are NOT FastEndpoints endpoint types:
- `Blocks.Contracts/Grpc/DataContracts/TeamRequest.cs`, `SprintRequest.cs` — gRPC code-first message contracts.
- `Modules/Jira/Jira.Contracts/UpdateJiraSprintRequest.cs` — Jira REST client DTO (external API shape).

ADR-009's text is scoped to "Endpoint type naming." gRPC and Jira contracts are not FastEndpoints commands/queries; renaming them is arguably outside Pass 2 and could imply protobuf-contract/wire churn.

**Question:** Confirm Pass 2 renames only FastEndpoints `Features/*` request types and leaves gRPC `DataContracts/*Request` and Jira `*Request` contract DTOs as-is. (My recommendation: yes — out of scope; they are not endpoint types.)

### Answer
**Rename gRPC/Jira too** (user override of the recommendation). Pass 2 also renames the gRPC code-first DataContracts (`TeamRequest`, `SprintRequest`) and the Jira `UpdateJiraSprintRequest` DTO for naming consistency. Architect: verify protobuf-net.Grpc field-number/`[DataMember]` stability so the C# type rename does not change the wire contract, and flag any breaking member-order risk in the plan.

---

## Q2: ADR-010 field-name alignment — rename request fields to match the entity, or keep current UI-friendly names and map explicitly?
**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis

**Context:** ADR-010 says "Command/query field names **match the entity** so mapping is trivial → Mapster." Several Settings commands deliberately use short UI-facing names that differ from the `AppSettings` entity:
- `SaveBugRatioAlertsRequest.AlertThreshold` → entity `BugRatioAlertThreshold`
- `SaveBugRatioAlertsRequest.ConsecutiveSprintCount` → entity `BugRatioConsecutiveSprintCount`
- (others in `Save*` settings endpoints follow the same pattern)

To make these "trivial → Mapster" per ADR-010, the request field names must be renamed to the entity names. **That changes the JSON wire contract the Vue client sends** (the request type IS the request body, ADR-009). Two valid readings:
- (A) Rename request fields to match entity → enables trivial Mapster → **requires matching frontend changes** (client/src TS + API calls).
- (B) Keep current request field names → mapping stays non-trivial → per ADR-010 non-trivial mapping → factory method in aggregate behavior partial class (which is Pass 3 domain-enrichment territory, not Pass 2).

**Question:** For commands whose fields already match the entity (trivial), Mapster applies cleanly in Pass 2 — agreed. For the mismatched Settings commands, which path: (A) rename request fields + update the Vue client this pass, or (B) leave field names alone and defer their mapping to Pass 3? (My recommendation: B — Pass 2 is slice hygiene; do NOT churn the frontend wire contract or pull domain-factory work forward. Apply Mapster only where field names already match; leave mismatched-field handlers as explicit assignment with a note for Pass 3.)

### Answer
**Path A** (user override). Rename request fields to match the entity AND update the Vue client (TS request shapes + API call payloads) this pass. After renaming, apply Mapster wherever fields now match.

---

## Q3: Pass-2/Pass-3 boundary on `AppSettings` mapping
**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis

**Context:** `AppSettings` is currently one flat aggregate with ~20 scalar properties. ADR-007 + Pass 3 (DomainEnrichment) will group these into value objects (`BugRatioConfig`, `XrayConfig`, `CycleTimeConfig`, etc.) persisted as owned/complex types. If Pass 2 introduces Mapster maps against today's flat `AppSettings`, Pass 3 reshapes the same target and rewrites those maps.

**Question:** Should Pass 2 apply Mapster to the `Save*` Settings commands at all, or defer ALL settings-command mapping to Pass 3 (when the VO shape is known) and have Pass 2 only (a) split the fat files (ADR-008) and (b) rename the types (ADR-009) for the Settings feature? (My recommendation: defer settings-command *mapping* to Pass 3; Pass 2 still does the ADR-008 split + ADR-009 rename for Settings, but leaves the handler body's explicit assignment untouched to avoid double work.)

### Answer
**Pull settings mapping into Pass 2** (user override). Apply Mapster to the `Save*` settings commands now, against today's flat `AppSettings`, in addition to the ADR-008 split + ADR-009 rename. Accept that Pass 3's VO reshape may rewrite these maps — that rework is acknowledged and approved.

---

## Q4: Mapster wiring location and the "prefer record" sub-clause
**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis

**Context:** Two sub-questions on Mapster setup mechanics:
- **(a) Registration location.** ADR-014 (thin composition root) keeps wiring in each module's `Add{Module}Module`. Mapster needs `TypeAdapterConfig` + `IRegister` scanning registered per module (`Fokus.API/DependencyInjection.cs`, `Identity.API/DependencyInjection.cs`). Confirm Mapster registration goes in each module's DI extension (not Host), and `IRegister` map classes live beside the feature slice they serve (e.g. `Features/Sprints/UpdateSprint/`). Also confirm whether `Mapster.DependencyInjection` (for `AddMapster`/`IServiceCollection` scanning) should be added alongside `Mapster` in CPM, or just core `Mapster` with manual `TypeAdapterConfig.GlobalSettings.Scan(...)`.
- **(b) "Prefer record" (ADR-009).** ADR-009 says "Prefer `record` for commands/queries/responses/DTOs." The existing types are `class` with mutable setters. Is converting class→record in Pass 2 scope, or is Pass 2 naming-only (rename the class, keep `class`) with record-conversion left as non-binding/opportunistic? (My recommendation: naming + file-split are binding this pass; record conversion is opportunistic where it's a clean change and never where it risks FastEndpoints model-binding behavior — not a blocking acceptance criterion.)

### Answer
**(a) Core `Mapster` only, per-module DI, manual Scan.** Add only the core `Mapster` package to CPM (NOT `Mapster.DependencyInjection`). Register per module in each `Add{Module}Module` DI extension (not Host) via `TypeAdapterConfig.GlobalSettings.Scan(...)`; `IRegister` map classes live beside the feature slice they serve.
**(b) Record: opportunistic** (architect's recommendation accepted). Naming + file-split are binding; convert `class`→`record` only where clean and FastEndpoints-model-binding-safe; not a blocking acceptance criterion.

---

## Q5: Frontend (Vue client) blast radius — confirm renames are server-internal only
**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis

**Context:** The `{X}Request` → `{X}Command`/`Query` rename is a C# *type name* change. Because the command record IS the request body (ADR-009), the JSON property names on the wire are unchanged UNLESS request *fields* are renamed (that's Q2). So a pure type-name rename should be invisible to `client/`. I want to confirm there is no C#-side coupling I'm missing (e.g., a shared contracts assembly the client codegen reads) and that the Vue client is hand-written TS (no generated client off these types).

**Question:** Confirm the Vue client consumes these endpoints via hand-written TS request shapes (so a server-side type rename has zero client impact), and that Pass 2's acceptance does NOT require client changes — provided we take path (B) in Q2. (My recommendation: yes, confirm zero client changes for the rename; client changes only arise if Q2 path A is chosen.)

### Answer
**Frontend IS in scope** (path A chosen in Q2). Pass 2 includes the matching Vue client changes (TS request shapes + API call payloads) for every renamed request field. Pure type-name renames remain client-invisible; field renames require the client updates. Architect: confirm the client is hand-written TS (no generated client) and enumerate the client files in the plan's blast radius.
