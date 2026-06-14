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
| 4 | adhoc-Pass4-IdentityExtraction | Finish Fokus→Identity cut — redemption-at-login + company-domain auto-seed + dual gRPC (Invitation redeem + Person upsert) | **Done** | scope (A2) ✓ resolved at launch |
| 5 | adhoc-Pass5-EventCorrectness | Domain-event dispatch fixes — B1 retract (already-fixed), B2 `UserCreated` placeholder, B6 `Role` DB-default removal + migration, B7 `RepositoryBase` backing-field copy | **Done** | Pass 3c-B ✓ + 3c-C ✓ |
| 6 | adhoc-Pass6-AnalyticsSuffixAlign | Retro-rename 3c-A/3c-B domain calculators from uniform `*Calculator` to semantic suffixes (`*Analyzer`/`*Scorer`/`*Calculator` by what each computes), aligning with 3c-C and the `analytics-computation-service` naming guidance. Internal domain names only — zero wire impact | Ready | Pass 3c-C ✓ (learner consolidation 2026-06-10, owner decision) |

**Pass 3 split (decided at the Pass 3a launch):** Pass 3 was ~3–4× Pass 2, so it was split into 3a/3b/3c, each its own pipeline run. 3a delivered (no-DTO-in-domain). Decisions for 3b/3c are locked in `docs/specs/adhoc-Pass3-DomainEnrichment/delivery/communication-log.md` + `summary.md` — those runs should not re-litigate scope (Q2=full ADR-005 migration; Q3=AppSettings VOs + Identity now; Fokus sync aggregates stay anemic per B3).

**Pass 3c sub-split (decided at the 3c launch):** Pass 3c (all 15 analytics services) was further split into three sequential runs — **3c-A** (Sprint core: SprintSummary, QaMetrics, TestTimeline + 4 static helpers + gRPC adaptation) **✓ Done**; **3c-B** (5 delivery metrics) and **3c-C** (7 dev/epic/QA) both unblocked by 3c-A. Clean-break decision locked (domain calculators return new `*Result`/`*Data` records; endpoints + gRPC map from them; no wire-flavored names in the domain — ADR-010). 3c-B/3c-C reuse 3c-A's shared wire types (`MetricCard`, `SparklinePoint`, `TestingCrunchResult`) now consolidated in the `GetSprintSummary` slice. The 4 MEDIUM critic-review gaps on the 3c-A plan were accepted unresolved (detail lost to an agent malfunction) but independently re-derived and cleared by the reviewer + Codex — see 3c-A communication-log Malfunctions Log.

---

## Candidate Bug — Negative-Amber Pathology in ScoreHigherIsBetter

**Slug:** adhoc-CoverHealthScore (surfaced during mutation-gating Step 3)
**Priority:** Low — production never passes negative thresholds; no user-visible impact today.

`ScoreHigherIsBetter` has no guard preventing a negative `amber` argument. When `amber < 0` and `value < amber`, the formula `value / amber * 49` produces a result above 49 (e.g. `amber=-10, value=-20 → 98`), violating the intended [0,49] range of the below-amber band.

Characterization test: `Characterization_NegativeAmber_ProducesAbove49_HigherIsBetter` in `HealthScoreCalculatorTests` pins the actual behavior (returns 98) and is tagged as a candidate bug.

Resolution options (to decide if negative thresholds ever become possible):
1. Add `if (amber < 0) return 0;` guard before the below-amber division.
2. Validate at the call site that thresholds are non-negative.
3. Accept as won't-fix if invariant is enforced upstream.

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
- **Pass 4 (Identity extraction)** — **Done.** Scope (A2) resolved at launch: Invitation stays in Fokus (L1); full cut delivered (redemption-at-login, company-domain auto-seed, dual gRPC). Jira→Person keyed on ExternalId (Jira Cloud hides email). See `docs/specs/adhoc-Pass4-IdentityExtraction/delivery/summary.md`. Follow-ups: NU1903 transitive-dep advisory; optional inactive-user reactivation policy; frontend invite pages (Q12 deferred).
- Use the **nexus** pipeline agents (`/nexus:{agent}`), not OMC equivalents (per the `pipeline-guardrails` rule).

---

# Reflekt Integration Program

Bring Reflekt (real-time collaborative retro tool, `<reflekt-repo>`) into the monolith as a module. **Binding constraints:** `docs/architecture/decisions.md` ADR-016…020 (+ the full register) and `.claude/rules/engineering-discipline.md`. Design narrative: `docs/proposals/suite-integration-architecture.md` (ADRs win on conflict).

**Strategy (owner-decided 2026-06-14):** lift-and-shift first, then refactor/integrate — keep the mechanical move bisectable from semantic change, same mechanical-before-deep order as the refactor suite. Reflekt already matches the stack and the API/Domain/Persistence layout, so the lift is low-risk.

| ID | Slug | Type | Status | Depends on | Scope |
|----|------|------|--------|-----------|-------|
| RA0 | architecture capture | architect doc work | **Done** (2026-06-14) | — | ADR-016…020; `src/Services/Reflekt/CLAUDE.md`; proposal stamp. Not a pipeline pass. |
| RA1 | adhoc-ReflektLiftAndShift | adhoc / mechanical | Ready to plan | RA0 ✓ | Move `Services/Reflekt` + BuildingBlocks deltas in; CPM; in-memory MassTransit (not RabbitMQ); `AddReflektModule`/`MapReflektEndpoints` in Host; `reflekt.db` via `app.Migrate<T>()`; green `dotnet build`. Auth conflicts stubbed minimally to compile — no semantic change. |
| RA2 | adhoc-ReflektIdentityUnification | adhoc | ✓ **Done** (2026-06-14) — impl + critic + reviewer + Codex; accepted (2 carry-overs) | RA1 | **Plumbing only.** Remove Reflekt `User`/repo/config + Google ticket handler; keep guest scheme; `Participant.PersonId` set from the Identity cookie for **authenticated participants only** (no Person for guests — ADR-022); scope `ActiveUserPreProcessor` off Reflekt; leave the SM-approval gate as an **unbuilt seam** (ADR-021). Mirrors Pass 4 (ADR-016). |
| RA3 | adhoc-ReflektSprintOwnership | adhoc | Blocked → RA1 | RA1 (RA2 preferred) | Rename `Sprint`→`RetroSession`; add 1..N Fokus-sprint reference; `FokusSprintSnapshot` projection; consume `SprintSynced`; drop standalone sprint creation (ADR-017/018/019). |
| RA4 | adhoc-SprintContextPanel | feature (spec + plan) | Blocked → RA3 | RA3 | `ISprintAnalyticsService`/`AnonymizedSprintSummary` gRPC (Fokus server, Reflekt client) + the slide-out anonymized sprint-context panel. PO writes `spec.md`. Integration payoff (ADR-019, proposal §9). |
| RA5 | adhoc-ReflektFrontendMerge | adhoc | Blocked → RA1 | RA1 | Unify the two Vue SPAs under one routing shell — `/retro/*` (Reflekt), `/dashboard/*` (Fokus), `/settings/*` (shared). Build topology (one Vite app vs two) decided here (ADR-020). |
| RW | adhoc-RetroWaitingRoom | feature (spec + plan) | Later — needs spec | RA2 | SM waiting-room access gate (ADR-021): pending-join state, SM approve/reject + SignalR, joiner + SM UI. Open for the spec: per-retro approval vs approve-once-per-suite; `JoinRequest` entity vs `Participant` status. Deliberate departure from proposal §10's zero-friction guest framing. |

**Notes:**
- RA2/RA3/RA5 depend only on RA1's green build and can run in any order after it. RA3 reads cleaner if RA2 lands first (auth/Person wiring in place) but it is not a hard dependency. RA4 is the only one that needs RA3 — it renders the snapshot the sprint-ownership pass introduces.
- Reflekt is newer than pre-refactor Fokus and already follows ADR-008/009 (2-file split, `Command`/`Query` naming), so the ADR-alignment burden (Send.* currency, thin composition root, VOs) is light — folded into RA1 where it diverges, not a separate pass.
- The Reflekt source carries its own design provenance under `<reflekt-repo>\ai-coding\plans\` (~25 feature plans/lessons) — use it to attest intent when porting behavior.
