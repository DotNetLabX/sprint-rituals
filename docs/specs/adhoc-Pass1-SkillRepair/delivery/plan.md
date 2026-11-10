# Pass 1 — SkillRepair

**Feature Spec:** None (ad-hoc skill-repair pass — scope in `docs/backlog.md` row 1 + `docs/architecture/decisions.md` ADRs; design answers in `docs/specs/adhoc-Pass1-SkillRepair/delivery/questions.md` Q1–Q4, all Resolved; revised after two independent reviews — see Revision notes).

## Context

Pass 1 is the root-cause arm of the refactor program: the earlier passes fix *code*; Pass 1 fixes the *skills* so the same defects don't regenerate. The refactor-triage (`docs/proposals/refactor-triage.md` "Skill verdicts") and the root-cause analysis (`docs/proposals/skill-adherence-root-cause.md`) identified skills that are missing, smell, or have gaps. The ADRs (`decisions.md`) are the binding target: each repaired/created skill must encode the ADR's rule **as one documented variant** (see the General-Skill Mandate below) — not as the only way.

**This is a documentation/skill pass, not a code pass.** The deliverable is Markdown skill files. There is no compiler — the acceptance bar is redefined per Q4 (ADR citation + anti-pattern checks + embedded-example-valid + a skill-inventory dry-run).

**Decision order (ADR-003):** correct skill → the reference codebase → fix/create the skill. The reference patterns are already extracted into `docs/proposals/reference-patterns-domain.md` and `reference-patterns-structure.md` — use those as the anchor; re-read the reference repo only if a proposal omits a needed detail. Every fix is a **problem class** (fix the skill so all future instances are right, not one example).

## GENERAL-SKILL MANDATE — READ FIRST (D-A, binding)

**These are SHARED, multi-variant skills used across many .NET projects — not SprintRituals-private files.** The nexus-dotnet skills are explicitly multi-variant by design (`cqrs-patterns` = MediatR; `create-feature` = FastEndpoints / Carter / Minimal API; `create-aggregate` = EF Core / Redis). SprintRituals' ADR choices (ADR-004 no Application layer; ADR-011 FastEndpoints) are **ONE variant, not universal**.

Therefore every repair/creation in this pass MUST:

1. **Present SprintRituals' choice as a documented variant / worked example — never hard-code it as the only way.** Do not write absolutes like "since ADR-004 there is no Application layer." Write the variant form: *"In light-stack projects without an Application layer (e.g. this solution), register in API/Persistence; in projects with an Application layer, place it there (see reference)."*
2. **Keep existing full-stack content; annotate it, don't scrub it.** Where a skill currently teaches a full-stack pattern (MediatR, Application layer, `Send.*`), label it the full-stack/FastEndpoints variant and add the light-stack/stack-agnostic guidance alongside.
3. **Pass the `improve-skills` Quality Gate** ("generic and reusable across projects — no project-specific names/paths/assumptions baked into the skill logic; use placeholders, derive specifics at runtime"). SprintRituals types (`Sprint`, `AppSettings`, `Fokus.Domain`) appear only as **worked examples**, clearly marked as such.

This mandate governs all five steps. Each step below states its variant axis explicitly.

## TWO-REPO MODEL — READ FIRST (Q1, binding)

Skill files are **NOT** in this project and **NOT** edited in the read-only cache. They live in a **separate editable plugin source git repo**:

```
<plugin-repo>            ← plugin SOURCE repo (separate git repo, remote: DotNetLabX/claude-nexus)
  plugins/nexus-dotnet/skills/         ← all 5 Pass 1 skills live here
```

| Repo | What lives here | Commit target |
|------|-----------------|---------------|
| this repo | Pipeline artifacts ONLY: `plan.md`, `implementation.md`, `review.md`, `lessons.md`, `summary.md`, `questions.md` under `docs/specs/adhoc-Pass1-SkillRepair/delivery/` | this repo |
| the plugin source repo (plugin source) | The `SKILL.md` edits/creations (Steps 1–5) **and** `improve-skills` housekeeping (see C-2 note) | plugin repo |

**Consequences the developer and reviewer must internalize:**
- The diff to review for Steps 1–5 is in the plugin source repo, not in this repo. `git status` in this project will NOT show the skill work. Run `git -C "<plugin-repo>" status` / `diff` to see it.
- implementation.md (in THIS repo) documents what changed; the actual changed files are in the other repo. Cite the plugin-repo paths.
- The two repos are committed separately. The team-lead commits each to its own repo (commit/push gated to user request as always).

## Cache / discoverability model (Q1 sync step + Q4 dry-run — binding facts)

Verified against the plugin repo's own architecture record (`<plugin-repo>/docs/architecture/README.md`, "Platform constraints" #3 + ADR-1 + ADR-3 + ADR-9):

- The installed cache is **version-keyed**: `<home>/.claude/plugins/cache/claude-nexus/nexus-dotnet/0.1.1/`. `/plugin update` is a **no-op unless `plugin.json`'s `version` changes**.
- The installed `claude-nexus` marketplace points at the **GitHub remote** (`DotNetLabX/claude-nexus`), not the local source dir. So `/plugin update` would pull from GitHub — it will NOT pick up local-only edits without a commit+push+version-bump.
- **Therefore the dev loop is the sync mechanism, NOT a build script.** Per ADR-1, `claude --plugin-dir <repo>/plugins/...` loads the working checkout live (cache-bypassed); `/reload-plugins` hot-reloads so `SKILL.md` edits apply immediately. **Dependency caveat (C-3):** `nexus-dotnet` declares `dependencies: ["nexus"]` (ADR-3) and the README's own dev-loop example points `--plugin-dir` at the **core** (`/plugins/nexus`). A single `--plugin-dir .../nexus-dotnet` may not load the core dependency in cache-bypass mode. Step 6 must **verify and record the working invocation** (likely both plugins on the path, or the core) — do not assume the nexus-dotnet-only command works.
- There is no `scripts/` step that writes the cache: `scripts/` contains `gen-commands.mjs` (regenerates persona commands), `gen-omni.mjs` (mirrors nexus→the omni twin), and `skill-classification.md` (a doc) — none writes the install cache. (LOW-5: enumeration corrected from the prior "holds only two scripts.")
- **Out of Pass 1 scope (flag to team-lead, do NOT do here):** the eventual *release* of these skills (version bump in `plugin.json` → `claude plugin validate` → `gen-commands.mjs` / `gen-omni.mjs` → tag/push per ADR-9) is a plugin-release decision for the user, separate from authoring the skills.

## `improve-skills` housekeeping routing (C-2, binding)

Steps 1–5 invoke `improve-skills`, which mandates three housekeeping artifacts: log every create/fix to `docs/skill-backlog.md`, append a per-skill `CHANGELOG.md` entry, and update `.claude/README.md` skill count/sections. **In the two-repo model these target the PLUGIN repo, not sprint-rituals:**

- `<plugin-repo>/docs/skill-backlog.md` (create if absent) — one entry per created/fixed skill.
- `<plugin-repo>/plugins/nexus-dotnet/skills/{name}/CHANGELOG.md` — for the two *repairs* (Steps 1, 2); creates may add an initial CHANGELOG, optional.
- `.claude/README.md` skill-count update — **applies to the plugin repo if it carries one**; the developer confirms whether a plugin-level README skill index exists and updates it there, else records "no skill-index README in plugin repo — waived." **Do NOT touch any `.claude/README.md` in sprint-rituals.**

State in implementation.md which housekeeping artifacts were written (with plugin-repo paths) and which were waived with reason.

## Already-fixed — EXCLUDED from scope (Q3, verified in source)

Confirmed by inspecting `<plugin-repo>/plugins/nexus-dotnet/skills/`:
- **`create-aggregate`** — the `workflows/` dir exists with `AggregateEfCore.md`, `AggregateRedis.md`, `ValueObject.md`, `DomainEvent.md`. The "references non-existent workflow files" SMELL is resolved. **Do not touch.**
- **`persistence-patterns`** — the `ref`-param helper (line ~105) and `HasDefaultValueSql` (line ~80) are now documented as explicit warnings/caveats, not silent non-compilable patterns. **Do not touch.**

Also out of scope (Q3): `create-feature`, `cqrs-patterns`, `service-registration` — not defective skills (the *code* violated them; that's Pass 2). Do not touch.

## Skill Mapping

This pass *edits skills*; the work is not "apply skill X to code." The relevant meta-skills are `create-implementation-plan`'s anti-pattern/over-specification tests (the acceptance bar, Q4) and `improve-skills` (the nexus skill for creating/fixing skills). Disposition column reflects whether an authoring skill governs the step.

| Step | What | Authoring skill | Disposition | Gap? |
|------|------|-----------------|-------------|------|
| 1 | Repair `analytics-computation-service` (specialization of domain-service; return domain types; full skill reframe) | `improve-skills` | Follow | — |
| 2 | Repair `domain-patterns` (add VO-vs-scalar rule; variant-annotate MediatR/Application content) | `improve-skills` | Follow | — |
| 3 | Create `domain-service` (new; variant-aware placement) | `improve-skills` | Follow | — |
| 4 | Create `central-package-management` (new) | `improve-skills` | Follow | — |
| 5 | Create `framework-currency` (new; general currency + FastEndpoints variant) | `improve-skills` | Follow | — |
| 6 | Sync cache via dev-loop + skill-inventory dry-run (Q4 gate; verify dev-loop invocation) | (none — tooling verification) | None | — |
| 7 | Acceptance audit: ADR-citation + anti-pattern/over-spec + example-valid + general-skill check for all 5 skills | (none — uses `create-implementation-plan` tests as a checklist) | None | — |

**Disposition notes:**
- Steps 1–5 are **Follow `improve-skills`** — the nexus skill that governs creating new SKILL.md files and fixing existing ones (scaffolding, frontmatter, structure, **and the backlog/CHANGELOG/README housekeeping routed per the C-2 section above**). Invoke it; do not restate skill-file boilerplate here. The plan supplies only the *content* each skill must encode (the ADR rule + its variant framing, the defect to remove). **Gap-reality was pre-verified against the source repo (recorded in Q3); do not re-run `improve-skills`'s blind `.claude/skills/` glob and draw a false "covered/not-covered" conclusion (LOW-7).**
- **`improve-skills` format/location adaptation (FIX 2).** `improve-skills` is written around native `.claude/skills/` scaffolding and states it creates "only native Claude Code `.claude/skills/` format" (improve-skills/SKILL.md ~lines 21–35, 111). That is **the same `SKILL.md` format** (YAML frontmatter `name`+`description` + body + the Quality Gate) used in `plugins/nexus-dotnet/skills/` — a plugin's `skills/` dir IS native Claude Code skill format; only the **location** differs. So Steps 1–5 **Follow `improve-skills` for STRUCTURE / frontmatter / Quality-Gate / Cross-Reference-Lint / housekeeping**, with the authoring **LOCATION** being the plugin repo's `plugins/nexus-dotnet/skills/{name}/` (NOT `.claude/skills/`) and the housekeeping artifacts routed to the plugin repo per the C-2 section. This is a location adaptation, not a format deviation — `improve-skills`'s "no non-native formats" constraint is honored (the output is native `SKILL.md`). The binding is therefore internally consistent: `improve-skills` governs *how* each skill file is shaped; this plan + Q1 govern *where* it lands.
- Steps 6–7 are **None** — they verify tooling/discoverability and audit the deliverable; no authoring skill covers "run the dev loop" or "audit against the acceptance bar." Justified inline.

## Binding ADR → skill map

Each ADR is encoded **as a documented variant** per the General-Skill Mandate.

| ADR | Rule the skill must encode (as a variant) | Skill(s) |
|-----|----------------------------|----------|
| ADR-005 | Computation in Domain; cross-aggregate logic → `domain-service` is the **Domain-layer** calculator in `{Module}.Domain/{Area}/`, pure (read-port escape hatch), returns domain types/VOs, never response DTOs; minimize API service classes. **(The Application-layer service of reference §6 is a DISTINCT pattern, not a variant of `domain-service` — see Step 3 pt1 contrast.)** | `domain-service`, `analytics-computation-service` |
| ADR-006 | No external/integration/response DTO enters the Domain (input side). Intrinsic computation primitives/enums are allowed; use a VO when an input carries domain meaning/invariant | `domain-service`, `analytics-computation-service` |
| ADR-007 | Group cohesive scalars into VOs/owned types (not flat scalars); when to use a VO vs a flat scalar | `domain-patterns` |
| ADR-010 | Endpoint **always** owns its own response contract and maps domain→DTO — even when shapes are identical (D-B) | `domain-service`, `analytics-computation-service` |
| ADR-011 | FastEndpoints variant: modern `Send.*` API; never legacy `Send*Async`. (The general currency rule is stack-agnostic; `Send.*` is the FastEndpoints specialization.) | `framework-currency` |
| ADR-012 | Always-latest stable framework versions (stack-agnostic) | `framework-currency` |
| ADR-013 | Central Package Management — `Directory.Packages.props`, bare `<PackageReference>` | `central-package-management` |

## Implementation Steps

**Authoring order (binding): 3 → 1 → 2 → 4 → 5 → 6 → 7.** Author `domain-service` (Step 3) FIRST because Step 1 (analytics) reframes itself as a specialization of it and cross-references it by name. The steps are *numbered* by topic (repairs 1–2, creates 3–5, verify 6–7) but *authored* in dependency order. Step 7 verifies every cross-reference resolves as a backstop. All five SKILL.md files live under `<plugin-repo>/plugins/nexus-dotnet/skills/{name}/SKILL.md`.

Each step's acceptance includes the **per-skill bar (Q4)**: (a) cites its governing ADR(s) by number; (b) passes the anti-pattern check (no "adapt" weasel-wording, no skill dismissed as too-simple) and the over-specification test (feature-specific guidance only, not restating generic DDD); (c) any embedded example is valid for its language — **C#** examples compile conceptually against this solution's actual BuildingBlocks (`Blocks.Domain`: `AggregateRoot`/`Entity<T>`/`ValueObject`/`StringValueObject`/`SingleValueObject<T>`; `Blocks.EntityFrameworkCore`; `Blocks.FastEndpoints`), **MSBuild XML** (CPM props) validates as MSBuild, **regex/grep** detection patterns are correct; (d) carries an "accepted but not proven until Passes 2/3 consume it" note; (e) **general-skill check** — SprintRituals choices appear as a labelled variant/worked example, not as the only way (D-A).

### Step 3 — Create `domain-service` skill (new) — author this first

**File:** `<plugin-repo>/plugins/nexus-dotnet/skills/domain-service/SKILL.md` (create folder + file). **Disposition: Follow `improve-skills`.** **Confidence: low** — no direct precedent skill; this is a genuine design encoded from ADR-005 + the Q2 interactive answers. Explore the reference proposals (`reference-patterns-domain.md` §1, §6) and the existing `extract-feature-service` / `analytics-computation-service` skills before authoring, to position this skill against them. **Note: reference §6 (`ArticleAccessChecker`) is an Application-layer service, a DISTINCT pattern — Step 3 treats it as a contrast, not a variant of `domain-service` (FIX 1).**

Encode the Q2-resolved design (governing ADRs: 005, 006, 010), **variant-aware** throughout (D-A):

1. **Concept & location (one coherent pattern).** A *domain service* is a **Domain-layer** class in `{Module}.Domain/{Area}/` that holds cross-aggregate computation/derivation (single-aggregate logic stays in aggregate behavior methods — ADR-005). Worked example: `Fokus.Domain/Analytics/SprintSummaryCalculator.cs`. This skill is scoped to this single pattern — Domain-layer, pure (pt3), returns domain types (pt4). It does NOT move to, or depend on, an Application layer or a DbContext.
   - **Contrast — "see also / when this doesn't apply" (NOT a variant, FIX 1):** In projects WITH an Application layer, cross-aggregate **orchestration** commonly lives in an **Application-layer service** instead (reference §6 `ArticleAccessChecker`) — a **DISTINCT** pattern with different rules: it may depend on a `DbContext`/repositories directly and is **not** bound by the domain-service purity rule (pt3). `domain-service` is the **domain-layer** approach, used when cross-aggregate computation belongs in the Domain (and the natural fit when there is no Application layer). Add this as a brief contrast section so the skill acknowledges the Application-service alternative (satisfying D-A — the domain-layer form is not hard-coded as the only way) **without** calling that alternative a variant of `domain-service`. Point the cross-ref for the Application-layer world at `cqrs-patterns` (MediatR-handler placement) where useful. Critically: no sentence in this skill may imply a domain service can live in / depend on the Application layer, a `DbContext`, or concrete repositories.
2. **Naming rule (binding, scoped to DOMAIN services).** NO `Service`/`DomainService` suffix — name by what it does: `SprintSummaryCalculator`, `CycleTimeAnalyzer`, `BugRatioScorer`, `VelocityProjector`. Include a "don't reach for `Service`" callout. **Scope the rule explicitly to domain services** and note that API-layer operation services (`extract-feature-service`) keep their own naming — so the two skills don't contradict (LOW-4). State the rule is scoped to this skill, not a project-wide naming convention.
3. **Dependencies — PURE by default; escape hatch is a NARROW DOMAIN-OWNED READ PORT (HIGH #3).** The class operates on pre-loaded aggregates passed in by the caller; it is DB-free and unit-testable. The performance escape hatch is **NOT** a concrete repository and **NOT** `IQueryable` — it is a narrow read port: an interface **declared in `{Module}.Domain`**, returning **domain types only**, named for the read it performs (e.g. `IClosedSprintHistory.LoadForBoard(...)`). The persistence layer implements it; the domain depends only on the domain-owned abstraction. Forbid injecting `IRepository`/`RepositoryBase`/`IQueryable` into the domain class. Use the escape hatch only for performance, with the justification written at the call site. (Contrast with `extract-feature-service`, an API-layer operation service that injects concrete repositories — different layer, different rule; cross-reference it so the two don't read as contradictory.)
4. **Output boundary — endpoint ALWAYS owns a response DTO + map (D-B, ADR-010).** Methods return **domain types / value objects**, never API response DTOs. The endpoint **always defines its own response contract and maps domain→DTO via Mapster — even when the shapes are identical** (the domain VO and the wire contract evolve independently; do not return the domain type directly from the endpoint, and do not reuse the domain type as the response). *(Supersedes the prior "avoid gratuitous DTO twins / reuse domain type when 1:1" guidance.)*
5. **Input boundary — INPUT side (ADR-006), corrected scope (HIGH #2).** **No external/integration/response DTOs enter the Domain** (no `JiraIssueDto`, no `*Response` record as a parameter). **Primitives and enums intrinsic to the computation ARE allowed** (e.g. `windowSize`, `anchorIndex`, look-back counts). Use a **value object** when an input carries domain meaning or an invariant; use a plain primitive when it is an incidental computation parameter. If raw external data is needed, it is mapped to domain types at the boundary *before* the calculator sees it.
6. **DI registration.** The domain service (a Domain-layer class, pt1) registers as scoped in the owning module's DI — its API or Persistence `DependencyInjection.cs` (no Application layer is required for it, and it never lives in one). The read-port escape-hatch interface (pt3, declared in `{Module}.Domain`) is implemented in Persistence and registered there. Point to `service-registration` for the where-DI-goes structure (it already provides the "skip layer 2 when no Application project" fallback); do not restate it. *(If a project instead uses the distinct Application-layer service pattern from the pt1 contrast, that service is registered in the Application layer — but that is the other pattern, not this skill's domain service.)*
7. **Relationship section.** State that `analytics-computation-service` is a *specialization* of this skill for time-series analytics. Cross-reference `domain-patterns` (for the domain types/VOs the service returns and consumes) and `persistence-patterns` (for how the read-port implementation is built) — these cross-refs are required so the Step 7 integrity check passes (MED #9).

**Accept:** file exists; encodes points 1–7; scoped to the single Domain-layer pattern in `{Module}.Domain/{Area}/` with the Application-layer service as a **contrast/see-also section, NOT a variant** (FIX 1); **no sentence implies a domain service lives in / depends on an Application layer, a `DbContext`, or concrete repositories**; naming callout present and scoped to domain services; pure-default + **narrow domain-owned read-port** escape hatch (no concrete repos / no `IQueryable` / no `DbContext`); endpoint-always-owns-DTO rule (D-B); no-DTO-input rule with primitives-allowed carve-out (ADR-006); DI in module API/Persistence (never Application); cross-refs `analytics-computation-service` + `domain-patterns` + `persistence-patterns` + `extract-feature-service` + `service-registration` (+ `cqrs-patterns` from the contrast); cites ADR-005/006/010; per-skill bar (a)–(e) incl. general-skill check. Any C# example uses real `Blocks.Domain` types and compiles conceptually.

### Step 1 — Repair `analytics-computation-service` skill

**File:** `<plugin-repo>/plugins/nexus-dotnet/skills/analytics-computation-service/SKILL.md`. **Disposition: Follow `improve-skills`.** **Confidence: medium** — this is a **whole-skill reframe**, not a surgical bullet edit (HIGH #4). The API/Service framing is pervasive: the `description` (line 3), "When to use" (line 13), the Reference-shape table (lines 21–27), and "Service structure" (lines 31–32) all teach `*.API/Features/Analytics/*Service.cs`, Service-suffixed names, and "response records at the top." Preserve the genuinely useful analytics correctness content (delta/direction/polarity triplet, sparkline builder, rolling average, multi/single-period, optional-data-source fast path).

The SMELL to remove (governing ADRs: 005, 006, 010): "Response records at the top of the file — all records the service returns" (line ~31), "Service class — primary constructor with repository dependencies" (line ~32), and the repository "Exception" clause (line ~73). This couples computation to API response shape and to the DB.

Edits:
1. **Reframe the WHOLE skill as a specialization of `domain-service` (HIGH #4).** Rewrite the `description`, "When to use," the Reference-shape table, and the naming examples so they consistently teach **domain-layer calculators/analyzers**, not API services. The base `domain-service` rules (domain-layer location, name-by-what-it-does, pure-by-default, return domain types, endpoint-always-owns-DTO) apply; this skill adds only the analytics-specific helpers.
2. **Name the domain result-type replacement (MED #5).** Removing the in-service response-record trees (live example: `BugRatioService.cs:53-116` defines whole wrapper/result trees) requires saying what replaces them: analytics calculators return **dedicated DOMAIN result records/VOs** (e.g. a `BugRatioResult` with domain-typed `Single` / `Multi` members), which the **endpoint** maps to `{Action}Response` (D-B — always its own contract + map, even 1:1).
3. **Flip the dependency rule.** Pure by default (receives pre-loaded period data); the repository "Exception" becomes the `domain-service` escape hatch (a **narrow domain-owned read port**, performance-only, justified) — not casual repo access. Inputs are domain types + intrinsic primitives only — no external/integration/response DTOs into the calculator (ADR-006, primitives-allowed carve-out inherited from `domain-service`).
4. **Relocate placement language.** Reference-shape paths move from `{App}.API/Features/Analytics/` to `{Module}.Domain/Analytics/`. Naming examples follow the no-`Service`-suffix rule (`CycleTimeAnalyzer`, not `WorkloadService`).
5. **Keep** the sparkline builder, rolling-average `TakeLast`-after-guards note, multi/single-period split, optional-data-source fast path — still valid.

**Accept:** whole-skill reframe done — **grep the edited file for `\.API/Features`, `response record`, and `Service\.cs` → zero domain-service-context hits remain** (HIGH #4 acceptance); the skill returns named domain result types (MED #5); placement is the domain layer; naming follows the no-suffix rule; no-DTO-input rule with primitives carve-out (ADR-006); endpoint-always-owns-DTO (D-B); escape hatch is the narrow read port; cross-references `domain-service` as its base; cites ADR-005/006/010; per-skill bar (a)–(e). (Line-number refs above use `~` and are illustrative — `:32` for "Service structure"; the grep accept criteria, not the line numbers, are authoritative. LOW-6.)

### Step 2 — Repair `domain-patterns` skill (add VO-vs-scalar rule; variant-annotate full-stack content)

**File:** `<plugin-repo>/plugins/nexus-dotnet/skills/domain-patterns/SKILL.md`. **Disposition: Follow `improve-skills`.** **Confidence: high** — additive + annotation; do not delete content.

**2a. Add the VO-vs-scalar rule (gap, ADR-007).** The "Value Objects" section lists the three base types and the factory pattern but has **no rule for *when* to use a VO vs a flat scalar**. Add a subsection "When to use a value object (vs a flat scalar)":
1. The rule: when two or more scalar fields are *cohesive* (always used together, share an invariant, or represent one concept), group them into a VO / owned type rather than flat scalars. A single standalone scalar with its own validation/identity (e.g. `EmailAddress`) is also a VO; a single incidental scalar (a count, a flag) stays a plain property.
2. Worked example: `AppSettings`'s 20+ flat scalars → `BugRatioConfig`, `XrayConfig`, `CycleTimeConfig` owned types (labelled as a SprintRituals worked example, per D-A).
3. Persistence pointer: cohesive VOs map via EF `ComplexProperty` / owned types — point to `persistence-patterns` and `reference-patterns-domain.md` §3; do not restate the EF config.
4. Keep the existing "per-bounded-context duplication is intentional" note.

**2b. Variant-annotate the full-stack content (HIGH #6 → D-A — annotate, do NOT scrub).** The skill's "Domain Events" (`IDomainEvent : INotification` — MediatR), "IDomainEventPublisher — Framework Variants" (`Blocks.MediatR` vs `Blocks.FastEndpoints`), and "State Machine Pattern" (`{Svc}.Application/StateMachines/`) sections describe the **full-stack (MediatR + Application layer) variant**. Per D-A, **keep them** and add light-stack guidance:
- Annotate the MediatR `IDomainEvent`/`Blocks.MediatR` publisher path as the **full-stack variant**; add the **light-stack variant** alongside: FastEndpoints-native `IEvent` + `Blocks.FastEndpoints/DomainEventPublisher` (the variant this solution uses — ADR-004/011, as a worked example). The skill already lists both publishers; make explicit which goes with which stack and that a light-stack project uses the FastEndpoints path.
- Annotate the `{Svc}.Application/StateMachines/` path as the full-stack location; note that light-stack projects without an Application layer place such factories in the module's API or Domain composition (point to `service-registration`). Do not delete the Application-layer guidance.

**Accept:** (2a) new "when to use a VO" subsection with the cohesion rule + the `AppSettings`→config-VOs worked example + a pointer to `persistence-patterns` for `ComplexProperty`; (2b) MediatR / `Blocks.MediatR` / `.Application/StateMachines/` content **retained and labelled as the full-stack variant**, with light-stack (FastEndpoints `IEvent`; no-Application-layer DI) guidance added alongside — **nothing scrubbed**; cites ADR-007 (and notes ADR-004/011 as the light-stack variant axis); per-skill bar (a)–(e) incl. general-skill check.

### Step 4 — Create `central-package-management` skill (new)

**File:** `<plugin-repo>/plugins/nexus-dotnet/skills/central-package-management/SKILL.md` (create folder + file). **Disposition: Follow `improve-skills`.** **Confidence: high** — pattern derived in Pass 0 from the reference (`reference-patterns-structure.md` §1) and already applied in this solution (`src/Directory.Packages.props` — use as the live worked example).

Encode (governing ADR: 013):
1. **What CPM is + when to apply** — one `Directory.Packages.props` at the solution `src/` root; `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>` + (recommended) `<CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>`; one `<PackageVersion Include="..." Version="..."/>` per package; `.csproj` uses bare `<PackageReference Include="..."/>` with no version.
2. **Migration recipe (problem-class)** — create the props file declaring each currently-pinned version; remove the version from every `<PackageReference>`; preserve other attributes/child elements (`PrivateAssets`/`IncludeAssets`, e.g. on `EntityFrameworkCore.Design`).
3. **Verification grep — detect ALL version sources under a `PackageReference` (MED #7).** The check must catch: inline `Version="..."` attributes; child `<Version>...</Version>` elements; and `VersionOverride="..."` attributes — while preserving legitimate children (`PrivateAssets`, `IncludeAssets`). A single `Version=`-only grep is insufficient as a general skill. (In *this* repo Pass 0 already removed all three forms — `src/Directory.Packages.props` is clean and the csproj files carry only `PrivateAssets`/`IncludeAssets` children — so SprintRituals is the "after" worked example; the broadened grep is a skill-generality requirement for projects not yet clean. See Revision notes.)
4. **Reference anchors** — reference `<reference-repo>/src/Directory.Packages.props`; live in-solution worked example `src/Directory.Packages.props`.
5. **Interplay note** — CPM + "bump everything to latest" can surface transitive breaking changes beyond the targeted package (Pass 0 hit a Refit major-version break). Cross-reference `framework-currency`.

**Accept:** file exists; covers concept + migration recipe + **the broadened three-form verification grep** + reference anchors; cites ADR-013; per-skill bar (a)–(e) (the props XML validates as MSBuild).

### Step 5 — Create `framework-currency` skill (new)

**File:** `<plugin-repo>/plugins/nexus-dotnet/skills/framework-currency/SKILL.md` (create folder + file). **Disposition: Follow `improve-skills`.** **Confidence: medium** — detection patterns and false-positives are documented in Pass 0 lessons; the risk is detection completeness + version-target correctness.

Encode (governing ADRs: 011, 012), **general rule + FastEndpoints variant (D-A):**
1. **Currency rule (stack-agnostic).** Track latest stable versions of the core stack (declared centrally via CPM — cross-reference `central-package-management`); target framework .NET 10. This is the general rule.
2. **FastEndpoints `Send.*` rule (FastEndpoints variant).** Use the modern `Send.*` static API (`Send.OkAsync`, `Send.ResponseAsync`, `Send.NoContentAsync`, `Send.NotFoundAsync`, `Send.ErrorsAsync`, …); never the legacy instance methods. Label this the FastEndpoints specialization of the general currency rule.
3. **Version target — verify against the INSTALLED package (C-1).** The migration-map method names MUST be verified against the **installed FastEndpoints version**. **This solution is on FastEndpoints 8.1.0** (`src/Directory.Packages.props:18`) — use 8.x as the current worked example. The reference project's 7.x is **illustrative only**. Add the caution: **names shifted 6→7 and may shift 7→8 — never trust memory; verify against the installed package.**
4. **Detection — TWO-STAGE rule (HIGH #8).** A naive `SendAsync(` grep in a mostly-migrated repo yields only false positives (this repo: only `httpClient.SendAsync` + SignalR `Clients.All.SendAsync` remain; modern `Send.*` is already dominant). Specify:
   - *Stage 1:* search `*.cs` for **UNQUALIFIED** `Send*Async(` / `SendAsync(` calls **inside endpoint classes** (legacy FastEndpoints instance calls have no receiver before `Send`). Include the bare `SendAsync(body, statusCode, ct)` overload.
   - *Stage 2:* manually inspect each remaining hit — **receiver-qualified** calls (`httpClient.`, `Clients.All.`, `hubContext.`) are NOT FastEndpoints and must be left alone.
   Migration map: `SendOkAsync`→`Send.OkAsync`, `SendAsync(x, statusCode, ct)`→`Send.ResponseAsync`, `SendNoContentAsync`→`Send.NoContentAsync`, `SendNotFoundAsync`→`Send.NotFoundAsync`, `SendErrorsAsync`→`Send.ErrorsAsync`, etc. (verify names per point 3).
5. **False positives (binding — enumerate ALL, per Pass 0 lessons).** Do NOT migrate:
   - `System.Net.Http.HttpClient.SendAsync(request, ct)` (GraphQL/REST client) — live: `GraphQLXrayClient.cs:296`.
   - SignalR `hubContext.Clients.*.SendAsync(...)` — live: `SyncSprintsEndpoint.cs:46`.
   The skill instructs inspecting the receiver type, not regex-blind replacement.
6. **Version-bump caution** — bumping to latest can surface breaking changes (signature/namespace shifts); cross-reference `central-package-management`'s transitive-break note.

**Accept:** file exists; general currency rule + FastEndpoints `Send.*` variant; **8.1.0 named as the verify-against-installed target with the "verify, don't trust memory" caution**; **two-stage detection (unqualified-in-endpoint → manual receiver inspection)**; full false-positive list (HttpClient AND SignalR, with live file:line); version-bump caution; cites ADR-011 + ADR-012; per-skill bar (a)–(e).

### Step 6 — Sync the dev-loop cache and run the skill-inventory dry-run (Q4 gate)

**Disposition: None** (tooling verification). **Confidence: medium** — depends on the dev-loop behaving as the plugin's ADR-1/ADR-3 document.

This step makes the 5 edited/new skills *visible to the tooling* and confirms they're discoverable before Pass 2 consumes them.

1. **Sync via the dev loop (NOT `/plugin update`) — verify the invocation (C-3).** The installed cache is version-keyed and the marketplace points at GitHub, so a local edit is only visible via the working-checkout dev loop. **`nexus-dotnet` depends on `nexus` (ADR-3), and the README's dev-loop example points `--plugin-dir` at the core** — so a single `--plugin-dir .../nexus-dotnet` may not load the core. The developer must **determine and record the working invocation** (e.g. `--plugin-dir` pointing at both plugins, or at the core, per ADR-1/ADR-3), then `/reload-plugins` for SKILL.md edits to apply. Record the exact working command in implementation.md as the Step 6 deliverable. (If the developer cannot relaunch the session, record this as the verification the team-lead/user runs — do NOT bump the plugin version or push, that's the out-of-scope release path.)
   - **Closes the Q1 instruction.** Q1 asked to "inspect `<plugin-repo>/scripts/` for a build/sync script." Done during planning: `scripts/` holds `gen-commands.mjs`, `gen-omni.mjs`, and `skill-classification.md` (a doc) — **none writes the install cache** (LOW-5). So there is no cache-build script; the dev-loop is the sync. This is the closed answer to the instruction, not the instruction being skipped. Re-confirm `scripts/` contents if it changed since planning.
2. **Verify the skills are present and well-formed.** Confirm each of the 5 skill files exists with valid frontmatter (`name` matching the folder, a `description`). The `name` must exactly match how plans/agents reference it (bare name, e.g. `domain-service`).
3. **Skill-inventory dry-run.** Confirm the 5 skills would be discovered/mappable by `create-implementation-plan`'s skill-inventory step. The plugin's architecture record flags discovery as a known limitation (it globs `.claude/skills/`, missing plugin skills) — so the realistic dry-run is: (a) the skills appear in the available-skills list surfaced to an agent's context, and (b) a trial mapping (e.g. "planning a Pass-3 domain-enrichment step, does `domain-service` show up?") resolves by name. Record the result.

**Accept:** implementation.md records the **verified working dev-loop command**, confirms all 5 files exist with valid frontmatter and folder-matching names, and reports the dry-run outcome (skills discoverable by name, or the documented limitation if discovery is blind — with the mitigation that bare-name references still resolve at invocation).

### Step 7 — Acceptance audit against the Q4 bar (all 5 skills)

**Disposition: None** (deliverable audit using `create-implementation-plan`'s anti-pattern/over-specification tests as a checklist). **Confidence: high.**

For each of the 5 skills, verify and record a per-skill audit row in implementation.md:
1. **ADR citation** — `domain-service` cites 005/006/010; `analytics-computation-service` cites 005/006/010; `domain-patterns` cites 007 (notes 004/011 as the light-stack variant axis); `central-package-management` cites 013; `framework-currency` cites 011/012.
2. **Anti-pattern check** — no "adapt"/"adapted" weasel-wording; no section dismissed as "too simple"; no restating of generic boilerplate other skills own.
3. **Over-specification test** — feature/pattern-specific guidance, not a restatement of generic DDD/EF; cross-reference instead of duplicate.
4. **Example-valid** — every embedded C# example uses real BuildingBlocks types and compiles conceptually; CPM props XML is valid MSBuild; framework-currency detection regexes are correct.
5. **"Not proven yet" note** — each skill carries the "accepted but unproven until Passes 2/3 apply it" line.
6. **Cross-reference integrity** — `analytics-computation-service` → `domain-service`; `domain-service` → `extract-feature-service` (contrast) + `service-registration` (DI) + `persistence-patterns` + `domain-patterns`; `framework-currency` ↔ `central-package-management`; `domain-patterns` → `persistence-patterns`. Confirm every referenced skill name resolves to an existing skill. (Step 3 is required to author the `persistence-patterns`+`domain-patterns` cross-refs so this check passes — MED #9.)
7. **General-skill check (D-A)** — each skill presents SprintRituals choices as a labelled variant/worked example, not the only way; the `improve-skills` Quality-Gate "generic and reusable" criterion holds (no project-specific names baked into skill *logic*).

**Accept:** a per-skill audit table in implementation.md with all seven checks passing for all five skills.

## Domain Model Changes

None. This pass edits skill documentation; it touches no aggregate, entity, VO, or domain event in the SprintRituals codebase. (The *skills describe* domain patterns, but no code in either repo's domain model changes.)

## Data Model Changes

None. No migrations, no schema.

## Cross-Service Changes

None in the SprintRituals solution. The changes are entirely in the plugin source repo.

## Migration Notes

No code migration. The only "migration" is the dev-loop cache sync (Step 6) so the tooling sees the new skills. The plugin *release* (version bump + push + `/plugin update`) is deliberately out of scope — flag to the team-lead as the follow-up that makes these skills available to other machines/projects (per the plugin repo's ADR-9 release flow).

## Testing Strategy

No automated suite (project decision; and these are Markdown files). Verification is:
1. **Per-step accept criteria** above (content present, SMELL removed via grep, rules encoded, variants present).
2. **Step 6 dry-run** — skills discoverable/mappable after the verified dev-loop sync.
3. **Step 7 audit** — the Q4 bar (ADR-citation + anti-pattern/over-spec + example-valid + not-proven note + cross-ref integrity + general-skill check) for all five.
4. **Real proving ground is Pass 2/3** — when plans consume these skills. Each skill carries the "not proven until consumed" caveat; Pass 2/3 lessons will feed back any defects.

## KB Impact

Expected **none** — Pass 1 changes skill documentation, not business rules, formulas, or edge cases. `docs/kb/` is empty (verified in Pass 0). No KB entry to update. (Recorded here so the done-check confirms it; no separate numbered KB step is warranted because there is no KB to touch.)

## Revision notes (divergences / nuances vs. the two reviews)

I folded **all** D-A, D-B, Codex (HIGH #1–#4, #6, #8; MED #5, #7, #9), and critic (C-1, C-2, C-3; LOW-4, LOW-5, LOW-6, LOW-7) findings. No finding was dropped. Two nuances where I qualified rather than diverged:

1. **MED #7 (CPM verification grep) — folded as a skill-generality requirement, not a SprintRituals defect.** I verified the cited files: `Host.csproj` and `Identity.Persistence.csproj` carry only `PrivateAssets`/`IncludeAssets` children with **no** `Version=`/`<Version>`/`VersionOverride=`, and a repo-wide grep for those forms returns zero — Pass 0 already cleaned all three. So broadening the grep changes nothing for *this* repo. It is still correct and necessary because `central-package-management` is a **general, multi-project skill** (D-A) that will run against projects not yet clean. Step 4 encodes the broadened three-form grep on that basis; SprintRituals is the "after" worked example. (Substantiated qualification, not a divergence — the fix is in.)

2. **Codex HIGH #6 — resolved per D-A by ANNOTATE, not scrub.** Codex recommended "minimal scrub or reference-only annotation" of `domain-patterns`' MediatR/Application content. D-A is more specific and binding: **keep the full-stack content, label it the full-stack variant, and add light-stack guidance alongside.** Step 2b follows D-A (annotate + add), explicitly NOT scrubbing. This satisfies Codex's intent (ADR-004 incompatibility no longer misleads) while honoring D-A's general-skill mandate.

3. **Codex HIGH #7 (FastEndpoints `Send.*`)** and the critic's overlapping **C-1** are the same issue from two reviewers; folded once in Step 5 point 3 (verify against installed 8.1.0).

LOWs folded: LOW-4 (scope no-suffix rule to domain services; note API-layer services keep their naming — Step 3 point 2), LOW-5 (`scripts/` enumeration corrected to include `skill-classification.md` — cache model + Step 6), LOW-6 (Step 1 line refs marked illustrative; grep accepts are authoritative), LOW-7 (don't re-run `improve-skills`' blind glob — Skill Mapping disposition note).

### Touch-up cycle (2 new issues the prior revision introduced; targeted fixes only)

4. **FIX 1 (HIGH) — `domain-service` no longer conflated with an Application-layer service.** The prior revision presented reference §6 `ArticleAccessChecker` as "the full-stack variant of domain-service." But that reference pattern is an **Application-layer** service that depends on `SubmissionDbContext` directly — it contradicts Step 3 pt3's purity rule (narrow domain-owned read port; no concrete repos / `IQueryable` / `DbContext`). Calling it a "variant" blurred two genuinely different patterns, and the ambiguity propagated to Step 1 (which specializes Step 3). Fix: `domain-service` is now scoped to the **one** Domain-layer pattern (pt3 purity unchanged); the Application-layer service is reframed as a **contrast / "see also / when this doesn't apply"** section — a DISTINCT pattern with different rules (may use a `DbContext`/repos; not bound by purity), cross-ref'd to `cqrs-patterns` for the Application-layer world. Updated: ADR-005 map row, Step 3 confidence line, pt1 (placement → single pattern + contrast), pt6 (DI → always module API/Persistence, never Application), and Accept (added "no sentence implies a domain service lives in/depends on an Application layer, DbContext, or concrete repos"). This still satisfies D-A — the skill acknowledges the Application-service alternative, it just stops mislabeling it a variant of `domain-service`. (The Step 2 `domain-patterns` MediatR/Application event-publisher *variant* is a different skill and a legitimate D-A variant — untouched.)

5. **FIX 2 (MED) — `improve-skills` format/location adaptation note.** `improve-skills` is scoped to native `.claude/skills/` scaffolding and says it creates "only native Claude Code `.claude/skills/` format" (improve-skills/SKILL.md ~21–35, 111), which could read as inconsistent with authoring under `plugins/nexus-dotnet/skills/`. Added a Skill-Mapping disposition note: a plugin's `skills/` dir IS native `SKILL.md` format — Steps 1–5 Follow `improve-skills` for STRUCTURE/frontmatter/Quality-Gate/Cross-Reference-Lint/housekeeping, with the authoring LOCATION being the plugin repo (not `.claude/skills/`). Location adaptation, not a format deviation — the binding is now internally consistent.

## Open Questions

None blocking — Q1–Q4 are Resolved (see questions.md); the two reviews' findings are folded above.

**Flags for the team-lead (not blockers):**
1. **Two commit targets.** Steps 1–5 (incl. `improve-skills` housekeeping) commit to the plugin source repo; the pipeline artifacts commit to this repo. Commit/push only on user request.
2. **Plugin release is out of scope.** Making these skills available beyond this machine's dev-loop requires a `plugin.json` version bump + `claude plugin validate` + `gen-commands.mjs`/`gen-omni.mjs` + tag/push (plugin ADR-9). Queue after Pass 1 closes.
3. **Skill-inventory discovery limitation** (plugin "Known limitations" P2): `create-implementation-plan` globs `.claude/skills/` and may under-report plugin skills. Step 6 verifies discoverability empirically; if blind, bare-name references still resolve at invocation. A proper fix is plugin-repo work, not Pass 1.
4. **`extract-feature-service` mixed naming example** (LOW-4 origin): that skill lists `SprintIssueSyncService` (Service-suffixed) as a good example next to `HealthScoreCalculator`. Out of Pass 1 scope, but a future-cleanup candidate so it doesn't contradict the domain-service no-suffix rule.
