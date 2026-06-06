# Pass 3 — DomainEnrichment — Questions

Phase 1 analysis questions. Scope source: `docs/backlog.md` (Pass 3 row "VOs, domain services, no-DTO-in-domain") + `docs/architecture/decisions.md` **ADR-004/005/006/007**; triage items #1d/#4/#5/#6 (`docs/proposals/refactor-triage.md`). There is no `spec.md` — this is an adhoc refactoring pass; the ADR register + triage are the binding inputs.

**Evidence gathered (Phase 1, this session):**
- `Fokus.Domain` references `Jira.Contracts` (`Fokus.Domain.csproj:11`); **8** `From{Jira,Xray}` factories on aggregates/entities consume Jira/Xray DTOs (`Sprint`, `Ticket`, `Developer`, `SprintMembership`, `StatusTransition`, `TestRun`, `TestExecution`, `TestSet`). Consumed by exactly two API sync services (`SprintIssueSyncService`, `XrayIssueSyncService`). **(ADR-006)**
- **15** analytics `*Service` classes in `Fokus.API/Features/Analytics/` (**8,783 lines total**, 327–886 each), all DI-registered. Each defines its own `*Response`/`*Entry`/`*Card` records that are simultaneously the computation return type AND the wire shape consumed by endpoints and the Vue client. **No computation lives in `Fokus.Domain` today** (zero hits for HealthScore/CompletionRate/etc.). **(ADR-005, #4/#5/#6)**
- `AppSettings` is a plain settings class (not an aggregate): 4 config VOs already extracted (`HealthThresholdConfig`, `HealthWeightConfig`, `QaHealthThresholdConfig`, `QualitySubScoreWeightConfig`, JSON-owned); ~12 flat scalars remain (`BugRatioAlertThreshold`, `BugRatioConsecutiveSprintCount`, `CycleTimeStartStage/EndStage`, `XrayEnabled/ClientId/ClientSecret`, `SyncBackSprintCount`, `PlanningWindowDays`, `DefaultSpPerBug`, `BugRatioTarget`, `BoardId`, `CompanyDomain`, status lists). **(ADR-007 / #1d)**
- All Fokus aggregates are partial-split but **anemic** (public `{ get; set; }`, no `internal` ctor, no static `Create` encapsulation factory — `FromJira` is a DTO mapper, not an encapsulation factory). Identity `Person` fully public; `User.Role` has a private setter. Triage **B4** marks Fokus anemia *by-design* (v1.md) but **ADR-007 says "applies to all aggregates."** Direct conflict. **(ADR-007)**

---

## Q1: Pass 3 scope is ~3–4× Pass 2 — split it, and if so, where are the cut lines?
**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis

### Answer
Split approved. **This run = 3a only** (ADR-006 no-DTO-in-domain). 3b (AppSettings VOs + Identity Person/User encapsulation; Fokus sync aggregates stay anemic) and 3c (full ADR-005 migration of all 15 analytics services with endpoint-owned response DTOs) are separate future runs — recorded in the plan's out-of-scope section, not planned now.


**Context:** ADR-005 alone means migrating **all 8,783 lines** of analytics computation from 15 API services into `Fokus.Domain/Analytics/` calculators, each returning domain result records, with the endpoint owning a separate response DTO + Mapster map (ADR-010 / D-B). Today the computation records ARE the wire shape, consumed by the Vue client — so this is ~150+ new domain result records, ~150+ response DTOs, ~15 Mapster map sets, and a wire-stability check per endpoint against the client. ADR-006 (drop Jira from domain, relocate 8 factories) and ADR-007 (enrich every aggregate + group AppSettings scalars into VOs, with EF migrations) are each substantial on their own. This is far larger than Pass 2 (8 steps) and exceeds the 10-step split threshold by a wide margin.

**Question:** How do you want Pass 3 split? My recommendation — split into **three sequential sub-passes**, each its own pipeline run:
- **3a — No-DTO-in-domain (ADR-006):** drop `Fokus.Domain → Jira.Contracts`; move the 8 `From*` factories to API/Persistence-boundary mappers (Mapster `IRegister` or static mappers in the sync feature). Smallest, lowest-risk, unblocks domain purity. **Do first.**
- **3b — AppSettings VOs (ADR-007 / #1d):** group the ~12 remaining flat scalars into config VOs (`BugRatioConfig`, `CycleTimeConfig`, `XrayConfig`, `SyncConfig`, …); EF `OwnsOne(ToJson)` per the 4 existing; one migration; update the `Save*` handlers + Mapster maps from Pass 2. Medium.
- **3c — Computation-to-domain (ADR-005 / #4/#5/#6):** migrate the 15 analytics services to `Fokus.Domain/Analytics/` calculators + endpoint-owned response DTOs. Largest; possibly split again per metric cluster. Do last (depends on 3b VO shapes).

Do you approve the 3a/3b/3c split (and is this run scoped to 3a only, or all three sequentially)? Or do you want a different cut?

---

## Q2: ADR-005 vs triage C1 — migrate ALL 15 analytics services, or only the multi-purpose ones?
**From:** architect
**To:** user
**Status:** Answered (deferred to Pass 3c)
**Step:** Phase 1 analysis

### Answer
Decided: **(A) full migration of all 15 analytics services with endpoint-owned response DTOs.** This belongs to **Pass 3c** (a separate future run) — NOT in scope for this 3a plan. Recorded here so 3a does not pre-empt it.


**Context:** Two binding inputs conflict.
- **ADR-005** (register, authoritative): "Analytics/derived computation is domain logic, not API plumbing… Minimize API service classes. A service class in `*.API/Features/` is a smell." The repaired `analytics-computation-service` skill says the API-layer analytics services "are candidates for migration to the Domain layer" in "Pass 2/3."
- **Triage C1** (`refactor-triage.md`): analytics service size is **by-design** — "Size alone isn't debt. Only split one if it's genuinely multi-purpose (verify case-by-case, not a blanket pass)." And #6 is specifically the *records-bound-to-response* smell, not the location.

These give two different targets: (A) move **all 15** to the Domain layer (full ADR-005), or (B) keep them where they are but fix only the #6 smell (separate computation from the response shape) and migrate only services that are genuinely multi-purpose.

**Question:** For 3c, which target — (A) full migration of all 15 analytics services to `Fokus.Domain/Analytics/` per ADR-005, or (B) the narrower triage-C1 reading (fix the response-coupling #6, migrate selectively)? My recommendation: **A** — ADR-005 is the authoritative register and explicitly supersedes v1.md's compute-in-endpoints line (supersession note); the repaired skill is built for it. But A is a major undertaking and changes no behavior, so I want your explicit sign-off before planning it. If A, confirm the endpoint-owns-a-separate-response-DTO rule (D-B/ADR-010) applies even for 1:1 shapes (it inflates the DTO count but is the locked decision).

---

## Q3: ADR-007 reach — does "rich aggregates" apply to the anemic Fokus aggregates this pass, or only AppSettings VO grouping + Identity?
**From:** architect
**To:** user
**Status:** Answered (deferred to Pass 3b)
**Step:** Phase 1 analysis

### Answer
Decided: **(a)** AppSettings VO grouping + Identity Person/User encapsulation; **Fokus sync aggregates stay anemic** (upsert pattern depends on public setters). This belongs to **Pass 3b** (a separate future run) — NOT in scope for this 3a plan. Recorded so 3a does not add any aggregate encapsulation.


**Context:** ADR-007 says aggregates get `internal` setters, `internal` constructor, static factory, behavior-split, and cohesive-scalar→VO grouping, and "applies to **all** aggregates including Identity." But triage **B4** marks Fokus aggregate anemia (`Sprint`, `Ticket`, `Developer`, etc.) as **by-design** per v1.md ("entities are data containers"), and **B3** confirms the repo-upsert sync pattern mutates entity props directly — which **requires public setters**. Locking down setters on `Sprint`/`Ticket` would break the sanctioned upsert sync path (`SprintRepository`/`TicketRepository` + `RepositoryBase.UpsertAsync` use `CurrentValues.SetValues`). So full ADR-007 encapsulation of the Fokus sync aggregates conflicts with the by-design upsert pattern.

**Question:** For 3b/ADR-007, what is the actual target?
- (a) **AppSettings VO grouping only** (the #1d item — clearly in scope, no setter conflict) + **Identity Person/User encapsulation** (B4-Identity, which triage left as needs-decision) — and **leave the Fokus sync aggregates anemic** (honor B3/B4 by-design, since upsert needs public setters).
- (b) **Full ADR-007** on every aggregate, which requires also redesigning the upsert sync path to go through factory/behavior methods instead of `SetValues` (much larger; touches `RepositoryBase`, B7-latent).

My recommendation: **(a)** — group AppSettings scalars into VOs and encapsulate Identity Person/User (resolving B4-Identity), but keep the Jira-sync aggregates as data containers because the upsert pattern (B3, sanctioned) depends on settable props. Reconciling ADR-007 with the upsert pattern across all sync aggregates is its own decision (and likely belongs with Pass 5 / event-correctness, where mutation goes through behavior). Confirm (a) vs (b), and confirm whether Identity Person/User encapsulation belongs in Pass 3 at all or defers to Pass 4 (IdentityExtraction).

---

## Q4: ADR-006 boundary mapper — where do the relocated Jira/Xray mappers live, and Mapster vs static?
**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis

### Answer
Relocate the 8 `From{Jira,Xray}` factories to **boundary mapper classes in `Fokus.API/Features/Sync` + `Features/Xray`**, using **Mapster `IRegister` with explicit member rules** for the enum/default/derived/multi-arg members. Drop the `Fokus.Domain → Jira.Contracts` project reference. (Plan note: the 3 Group-2 factories take only primitives — no source DTO — so they relocate as plain construction, not Mapster maps; Mapster needs a source type. This is a mechanism detail within the user's directive, not a reversal of it.)


**Context:** ADR-006 says map Jira/Xray DTOs → domain "at the API/Persistence boundary via Mapster" and "remove `FromJira(...)` factories from aggregates." The 8 factories are non-trivial (enum parsing, null-coalescing defaults, `planningWindowDays` parameter, list construction, cross-entity refs like `SprintMembership.FromJira(issue, sprint, …)`). Per ADR-010, non-trivial mapping → "factory method in the aggregate behavior partial class" — but ADR-006 says remove those factories from the aggregate. So non-trivial Jira mapping can't be a Mapster 1:1 map AND can't stay an aggregate factory. The natural home is a **boundary mapper class in the sync feature** (`Fokus.API/Features/Sync/` and `Features/Xray/`), either Mapster `IRegister` with custom `.Map(...)` rules or plain static mapper methods.

**Question:** For 3a/ADR-006, confirm the target: relocate the 8 factories to **boundary mapper(s) in the sync feature folders** (not the domain, not the aggregate), and prefer **Mapster `IRegister` with explicit member rules** for these non-trivial maps (vs plain static mapper methods)? My recommendation: boundary mappers in `Features/Sync/` + `Features/Xray/`; **Mapster `IRegister`** for consistency with Pass 2's mapping convention, with custom rules for the enum/default/derived members. If you'd rather these be plain static mappers (clearer for the multi-arg ones like `SprintMembership`), say so — it changes the plan's disposition.

---

## Q5: Reference-project derivation for 3c — confirm path and whether to inspect before planning
**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis

### Answer
The Pass-1 `domain-service` / `analytics-computation-service` skills are sufficient — **no reference-project re-inspection.** (Applies to 3c; this 3a plan follows `domain-service`'s ADR-006 Input-Boundary section, which is likewise sufficient.)


**Context:** ADR-002/003 + triage O1/O2 say the domain-service / computation-placement pattern is derived from the reference project `<reference-repo>` (O1 noted "4 candidates on the owner's source folder, pending confirmation"). Pass 1 already encoded the pattern into `domain-service` + `analytics-computation-service` skills (reviewed with you), so 3c can follow those skills directly. But if you want the plan to cite a concrete reference exemplar for the 15-service migration shape (e.g. a multi-period calculator in the reference), I should inspect it before planning 3c.

**Question:** For 3c, is following the repaired `domain-service` / `analytics-computation-service` skills sufficient (they were derived from the reference and reviewed with you in Pass 1), or do you want me to inspect `<reference-repo>` for a concrete calculator exemplar first? My recommendation: **skills are sufficient** — they were derived from the reference and signed off in Pass 1; no re-derivation needed. Confirm.
