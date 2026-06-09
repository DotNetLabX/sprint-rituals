# Backlog

Refactoring program from the refactor-triage session. **Binding constraints for every pass:** `docs/architecture/decisions.md` (ADR register) + `.claude/rules/engineering-discipline.md` (auto-loaded). Evidence: `docs/proposals/refactor-triage.md`.

Order is dependency-driven (skills before the domain work that applies them; mechanical before deep). Each pass is one pipeline run (architect plan → developer → reviewer). Manual testing after each pass (no automated suite by decision).

| ID | Slug | Pass | Status | Depends on |
|----|------|------|--------|-----------|
| 0 | adhoc-Pass0-Foundations | Foundations (cross-cutting, mechanical) | **Done** | — |
| 1 | adhoc-Pass1-SkillRepair | Repair/create skills (root cause) | **Done** | — |
| 2 | adhoc-Pass2-SliceHygiene | Endpoint split + naming + Mapster | **Done** | Pass 1 ✓ (+ add Mapster to CPM ✓) |
| 3a | adhoc-Pass3-DomainEnrichment | No-DTO-in-domain (ADR-006) — relocate 8 Jira/Xray factories to boundary mappers | **Done** | Pass 1 ✓, Pass 2 ✓ |
| 3b | adhoc-Pass3b-AppSettingsVOs | AppSettings scalars → config VOs + Identity Person/User encapsulation (ADR-007) | **Done** | Pass 3a ✓ |
| 3c-A | adhoc-Pass3c-ComputationToDomain | ComputationToDomain — Sprint core: SprintSummary/QaMetrics/TestTimeline calculators + 4 static helpers + gRPC adaptation (ADR-005/010) | **Done** | Pass 3b ✓ |
| 3c-B | adhoc-Pass3c-B-DeliveryMetrics | CarryOver, ScopeChange, CycleTime, BugRatio, Leaderboard calculators (ADR-005/010) | **Done** | Pass 3c-A ✓ |
| 3c-C | adhoc-Pass3c-C-DevEpicAnalytics | 7 dev/epic/QA calculators: DeveloperThroughput, DeveloperProgress, DeveloperDetail, DeveloperQuality, EpicProgress, QaWorkload, QaTrends (ADR-005/010) | **Done** | Pass 3c-A ✓ |
| 4 | adhoc-Pass4-IdentityExtraction | Finish Fokus→Identity cut | Blocked | needs user scope (A2) |
| 5 | adhoc-Pass5-EventCorrectness | Domain-event dispatch fixes — B1 retract (already-fixed), B2 `UserCreated` placeholder, B6 `Role` DB-default removal + migration, B7 `RepositoryBase` backing-field copy | **Done** | Pass 3c-B ✓ + 3c-C ✓ |

**Pass 3 split (decided at the Pass 3a launch):** Pass 3 was ~3–4× Pass 2, so it was split into 3a/3b/3c, each its own pipeline run. 3a delivered (no-DTO-in-domain). Decisions for 3b/3c are locked in `docs/specs/adhoc-Pass3-DomainEnrichment/delivery/communication-log.md` + `summary.md` — those runs should not re-litigate scope (Q2=full ADR-005 migration; Q3=AppSettings VOs + Identity now; Fokus sync aggregates stay anemic per B3).

**Pass 3c sub-split (decided at the 3c launch):** Pass 3c (all 15 analytics services) was further split into three sequential runs — **3c-A** (Sprint core: SprintSummary, QaMetrics, TestTimeline + 4 static helpers + gRPC adaptation) **✓ Done**; **3c-B** (5 delivery metrics) and **3c-C** (7 dev/epic/QA) both unblocked by 3c-A. Clean-break decision locked (domain calculators return new `*Result`/`*Data` records; endpoints + gRPC map from them; no wire-flavored names in the domain — ADR-010). 3c-B/3c-C reuse 3c-A's shared wire types (`MetricCard`, `SparklinePoint`, `TestingCrunchResult`) now consolidated in the `GetSprintSummary` slice. The 4 MEDIUM critic-review gaps on the 3c-A plan were accepted unresolved (detail lost to an agent malfunction) but independently re-derived and cleared by the reviewer + Codex — see 3c-A communication-log Malfunctions Log.

---

## Pass 0 — Foundations  (slug: `adhoc-Pass0-Foundations`, Status: Done)

Cross-cutting, mechanical, no per-feature semantics. No user input required — fully specified by the ADRs below.

**Scope:**
1. **Central Package Management** (ADR-013) — add `src/Directory.Packages.props` (`ManagePackageVersionsCentrally=true`); move every version to `<PackageVersion>`; strip `Version=` from all `<PackageReference>` across the 18 projects.
2. **Framework currency** (ADR-011, ADR-012) — replace deprecated `SendOkAsync` / legacy `Send*Async` with the modern `Send.*` API (**~89 calls across 54 files** — grep `Send(Ok|NoContent|String|Created|...)Async`); bump FastEndpoints (and core packages) to latest stable; fix any breaking changes.
3. **Thin composition root** (ADR-014) — move per-module wiring out of `Host/Program.cs` into each module's `Add{Module}Module` + `Map{Module}Endpoints`; introduce/use an `app.Migrate<TDbContext>()` helper; relocate MassTransit, gRPC server/client registration, and DB seeding into the owning module. `Program.cs` ends as Add / Init / Use only. Pattern: `service-registration` skill + reference (`<reference-repo>`).
4. **Convention alignment** (ADR-001, finding E1) — rewrite the stack-conventions block (CLAUDE.md / `docs/conventions/`) that describes "Services/ = Microservices", `ApiGateway/` (YARP), and `docker-compose` to reflect the decided modular monolith.

**Acceptance:** `dotnet build SprintRituals.slnx` green; no `SendOkAsync` remains; `Host/Program.cs` contains no per-module wiring; CPM active (no per-project versions); conventions text matches the modular monolith.

**Artifacts:** plan → `docs/specs/adhoc-Pass0-Foundations/delivery/plan.md`.

---

## Pass 1 — Skill Repair  (slug: `adhoc-Pass1-SkillRepair`, Status: Done)

Root-cause arm: the skills that encode the patterns the later passes apply. **Skills are GENERAL / multi-variant** (D-A) — SprintRituals' ADRs are documented as *one* variant, not hard-coded. Deliverables live in the **shared plugin repo** `<plugin-repo>\plugins\nexus-dotnet\skills\` (two-repo model — separate git target from these artifacts).

**Scope (5 core skills):**
1. **Repair `analytics-computation-service`** — whole-skill reframe: computation lives in Domain (ADR-005), not an API/Service class; endpoint owns the response DTO + Mapster map (D-B/ADR-010). Renamed away from the `-service` weasel-suffix framing.
2. **Repair `domain-patterns`** — add VO-vs-flat-scalar guidance (ADR-007); annotate MediatR/Application as a *full-stack variant* (retained, labelled) alongside the light-stack (FastEndpoints `IEvent`) variant — not scrubbed.
3. **Create `domain-service`** — pure-domain service shape; no Application layer / `DbContext` / concrete-repo dependency (ADR-004/005/006); the endpoint handler does the loading and passes loaded aggregates in.
4. **Create `central-package-management`** (ADR-013) — three-form version grep (`Version=`, `<Version>` child, `VersionOverride=`).
5. **Create `framework-currency`** (ADR-011/012) — two-stage `Send.*` detection; verify-against-installed (FastEndpoints 8.1.0); HttpClient + SignalR false-positives with live `file:line`.

**Acceptance:** all 5 SKILL.md present; cross-references resolve (zero dangling); examples compile against `Blocks.Domain`; reviewer APPROVED + Codex GO.

**Prerequisite surfaced for Pass 2/3:** Mapster is ADOPTED (ADR-010) but **not yet in the stack** — "add Mapster to `Directory.Packages.props` (CPM)" is a Pass 2 prerequisite. Skills label it target-state.

**Artifacts:** `docs/specs/adhoc-Pass1-SkillRepair/delivery/` (plan, implementation, review, codex-review-impl, done-check, summary, communication-log).

---

## Notes for the team-lead

- **Read first:** `docs/architecture/index.md` (→ `decisions.md`) and `.claude/rules/engineering-discipline.md`. They are binding.
- **Decision order (ADR-003):** correct skill → reference project `<reference-repo>` → fix/create the skill. Every fix is a problem *class* (fix all instances).
- **Pass 1 (skill repair)** — **Done.** `domain-service` shape reviewed interactively with the user (D-A general/multi-variant; D-B endpoint-owns-DTO; repo stays out of domain — handler loads). Skills committed to the plugin repo.
- **Pass 4 (Identity extraction)** needs the user to define what stays in Fokus vs moves to Identity before planning.
- Use the **nexus** pipeline agents (`/nexus:{agent}`), not OMC equivalents (per the `pipeline-guardrails` rule).
