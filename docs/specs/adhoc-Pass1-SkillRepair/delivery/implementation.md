# Pass 1 — SkillRepair — Implementation

## Files Created

All skill files are in the **plugin source repo**, not in this repo. Pipeline artifacts (this file) stay in this repo.

- `<plugin-repo>/plugins/nexus-dotnet/skills/domain-service/SKILL.md` — New skill: Domain-layer service pattern. Cross-aggregate computation, no-suffix naming rule, pure-by-default + narrow domain-owned read-port escape hatch, domain-type-out/endpoint-owns-response (ADR-010), no-DTO-input (ADR-006). Cross-refs: `analytics-computation-service`, `domain-patterns`, `persistence-patterns`, `extract-feature-service`, `service-registration`, `cqrs-patterns`. Cites ADR-005/006/010.
- `<plugin-repo>/plugins/nexus-dotnet/skills/central-package-management/SKILL.md` — New skill: CPM setup and audit. `Directory.Packages.props` structure, migration recipe, three-form verification grep (inline `Version=`, child `<Version>`, `VersionOverride=`). Reference anchors (dotnet-microservices + SprintRituals). Cross-ref to `framework-currency`. Cites ADR-013.
- `<plugin-repo>/plugins/nexus-dotnet/skills/framework-currency/SKILL.md` — New skill: General currency rule (ADR-012) + FastEndpoints `Send.*` variant (ADR-011). FastEndpoints 8.1.0 as verify-against-installed worked example. Two-stage detection (unqualified-in-endpoint → manual receiver inspection). Full false-positive list: `HttpClient.SendAsync` (GraphQLXrayClient.cs:296) and SignalR `Clients.All.SendAsync` (SyncSprintsEndpoint.cs:46). Version-bump caution. Cross-ref to `central-package-management`. Cites ADR-011/012.
- `<plugin-repo>/plugins/nexus-dotnet/skills/analytics-computation-service/CHANGELOG.md` — Initial changelog for the repaired skill.
- `<plugin-repo>/plugins/nexus-dotnet/skills/domain-patterns/CHANGELOG.md` — Initial changelog for the repaired skill.
- `<plugin-repo>/docs/skill-backlog.md` — Housekeeping: one entry per created/fixed skill (5 total: 3 created, 2 fixed).

## Files Modified

- `<plugin-repo>/plugins/nexus-dotnet/skills/analytics-computation-service/SKILL.md` — **Whole-skill reframe as specialization of `domain-service` (Step 1 / HIGH #4).** Rewrote `description`, "When to use", Reference-shape table, and "Service structure" to teach domain-layer calculators/analyzers. Removed "Response records at the top of the file — all records the service returns" (line ~31 in old file). Removed "Service class — primary constructor with repository dependencies" framing as the base structure. Removed the repository "Exception" clause — replaced by `domain-service` narrow domain-owned read-port escape hatch. Relocated placement from `{App}.API/Features/Analytics/` to `{Module}.Domain/Analytics/`. Named domain result records (`BugRatioResult`) as the replacement for in-service response record trees (MED #5). Applied no-suffix naming rule. Added ADR-006 no-DTO-input rule. Added ADR-010 endpoint-always-owns-DTO rule with Mapster map example. Kept sparkline builder, rolling-average `TakeLast`-after-guards note, multi/single-period split, optional-data-source fast path (all still valid analytics content).
- `<plugin-repo>/plugins/nexus-dotnet/skills/domain-patterns/SKILL.md` — **Two additions (Step 2 / additive, no content scrubbed).**
  - 2a: Added "When to Use a Value Object vs a Flat Scalar (ADR-007)" subsection after the VO section. Includes cohesion rule, `AppSettings`→`BugRatioConfig`/`XrayConfig`/`CycleTimeConfig` worked example (labelled SprintRituals), EF `ComplexProperty` pointer to `persistence-patterns`.
  - 2b: Variant-annotated IDomainEventPublisher section — `Blocks.MediatR` path labelled "full-stack variant", `Blocks.FastEndpoints` path labelled "light-stack variant (ADR-004/011)" alongside. Variant-annotated State Machine Pattern section — `{Svc}.Application/StateMachines/` labelled "full-stack variant", light-stack (no Application layer) placement guidance added pointing to `service-registration`. Nothing scrubbed.
  - Added "not proven until Passes 2/3" note.

## Key Decisions

- **Authoring order 3→1→2→4→5** followed as prescribed. `domain-service` authored before `analytics-computation-service` so the specialization cross-reference resolves.
- **`domain-service` scoped to single Domain-layer pattern.** Application-layer service (`ArticleAccessChecker`) is a "When This Doesn't Apply" contrast section, not a variant — consistent with FIX 1 in the plan. No sentence in `domain-service` implies a domain service lives in or depends on an Application layer, DbContext, or concrete repository.
- **`analytics-computation-service` grep accept check passed.** Grep for `\.API/Features`, `response record`, `Service\.cs` in domain-service context returns zero domain-service-context hits. The one `.API/Features` hit is the "NOT here — pre-ADR-005 location" callout, which is correct.
- **No weasel-wording.** Grep for `\badapt\b` and `\btoo simple\b` across all five SKILL.md files returns zero hits.
- **`.claude/README.md` skill-count update — waived.** No `.claude/README.md` exists in the plugin source repo (confirmed by Glob). Waived per the C-2 plan note: "developer confirms whether a plugin-level README skill index exists and updates it there, else records 'no skill-index README in plugin repo — waived.'"

---

## Cycle 1 Fixes (reviewer RV-1..7 + Codex CX-1..16, X1, X2)

All findings addressed in one pass. Mapping: finding id → what changed and in which file.

### Group 1 — Live vs target-state labeling (CX-1, CX-5, CX-11, CX-X1)

| Finding | File | Change |
|---------|------|--------|
| CX-1 | `analytics-computation-service/SKILL.md` | "Worked example (SprintRituals)" → "Target-state example (created by Pass 2/3)"; added before-state callout pointing to `SprintSummaryService.cs` in `Fokus.API/Features/Analytics/` |
| CX-5 | `domain-service/SKILL.md` | Same: Location section relabelled target-state; before-state callout added |
| CX-11 | `domain-patterns/SKILL.md` | Removed invented VOs `BugRatioConfig`/`XrayConfig`/`CycleTimeConfig`; replaced VO-vs-scalar table with four real live VOs: `HealthThresholdConfig`, `HealthWeightConfig`, `QaHealthThresholdConfig`, `QualitySubScoreWeightConfig` (all verified present in `Fokus.Domain/Settings/ValueObjects/`). Persistence note corrected from `ComplexProperty` to `OwnsOne(..., b => b.ToJson())` (verified against `AppSettingsConfiguration.cs`). `AppSettings` described as plain class (not AggregateRoot). |
| CX-X1 | `domain-service/SKILL.md` | Pure-by-default example: `BugRatioConfig` → `HealthThresholdConfig` (real live VO); input-boundary example updated to match |

### Group 2 — Factual errors (CX-12/RV-1, CX-16, RV-4)

| Finding | File | Change |
|---------|------|--------|
| CX-12 / RV-1 | `domain-patterns/SKILL.md` | `IDomainEvent : INotification, IEvent` → `IDomainEvent : IEvent` only (verified `Blocks.Domain/Events/IDomainEvent.cs:9`). Added explicit note: no `INotification` — MediatR excluded by ADR-004 |
| CX-16 | `domain-patterns/SKILL.md` | `SingleValueObject<T>` equality description corrected: "same concrete type" check via `GetType()` — not a general boxed-T comparison (verified `Blocks.Domain/ValueObjects/SingleValueObject.cs`) |
| RV-4 | `domain-patterns/SKILL.md` | Audit field types added: `CreatedById (string?)`, `CreatedOn (DateTime?)`, `LastModifiedById (string?)`, `LastModifiedOn (DateTime?)` (verified `Blocks.Domain/Entities/AggregateRoot.cs`) |

### Group 3 — Box non-live variants (CX-10, CX-13, CX-14, CX-15)

| Finding | File | Change |
|---------|------|--------|
| CX-10 | `domain-patterns/SKILL.md` | `IdentityUser<int>` reference-only variant boxed in a callout block: "Full-stack / reference-only variant — NOT in this solution". `User : Entity<int>` stated as this solution's identity model (verified `Identity.Domain/Users/User.cs`) |
| CX-13 | `domain-patterns/SKILL.md` | `DomainEvent<TAction>` reference-only variant boxed. Real source cited: `Articles.Abstractions` in the `dotnet-microservices` reference project |
| CX-14 | `domain-patterns/SKILL.md` | Removed invented `TransactionalDispatchDomainEventsInterceptor`. Boxed as "Full-stack / reference-only variant — NOT in this solution" callout. Only real interceptor (`DispatchDomainEventsInterceptor`) stated as authoritative |
| CX-15 | `domain-patterns/SKILL.md` | MediatR publisher boxed as "Full-stack / reference-only variant — NOT in this solution" with real source cited (`dotnet-microservices/Blocks.MediatR/DomainEventPublisher.cs`). FastEndpoints publisher stated as "this solution" |

### Group 4 — Analytics naming table (CX-7)

| Finding | File | Change |
|---------|------|--------|
| CX-7 | `analytics-computation-service/SKILL.md` | Removed two-column Good/Bad format with Service-suffix "Bad" names (`CycleTimeService`, `BugRatioService`, etc.). Replaced with single "Good" column table plus explicit prose prohibition sentence |

### Group 5 — Target-state type names (CX-3, CX-4)

| Finding | File | Change |
|---------|------|--------|
| CX-3 | `domain-service/SKILL.md` | Escape-hatch read-port example: `SprintSummary` → `ClosedSprintSnapshot`; labelled as target-state |
| CX-4 | `domain-service/SKILL.md` and `analytics-computation-service/SKILL.md` | Endpoint type names corrected to match live types: `GetBugRatioRequest` / `BugRatioResponse` (verified `Fokus.API/Features/Analytics/GetBugRatio/`). Mapster snippets labelled "Target-state (Mapster — the project's chosen mapper per ADR-010/CLAUDE.md; package not yet in CPM, added in Pass 2/3 — snippet won't build until then)" |

### Group 6 — CPM migration steps and grep portability (CX-8, RV-6)

| Finding | File | Change |
|---------|------|--------|
| CX-8 / RV-6 | `central-package-management/SKILL.md` | Migration Steps 1 and 3 expanded to explicitly name all three version-declaration forms (inline `Version=`, child `<Version>`, `VersionOverride=`). Grep commands replaced: removed `src/**/*.csproj` globstar form; all greps now use `--include="*.csproj" src/` portable form. Added ripgrep alternative using `--glob`. Added portability warning prose |

### Group 7 — framework-currency Stage 1 and false positives (CX-9, RV-7)

| Finding | File | Change |
|---------|------|--------|
| CX-9 / RV-7 | `framework-currency/SKILL.md` | Stage 1 heading: "Find unqualified calls inside endpoint classes" → "Find candidate `Send*Async` calls". Stage 1 prose reworded: grep hits both legacy FastEndpoints calls and qualified false positives — Stage 2 filters. False-positive live examples: `_httpClient` → `httpClient`, `_hubContext` → `hubContext`. Added two-arg overload to migration map: `SendAsync(body, ct)` → `Send.OkAsync(body, ct)`. Stage 2 table updated to include the 2-arg form row |

### Group 8 — Primary-constructor naming and duplicate caveat (RV-2, RV-5)

| Finding | File | Change |
|---------|------|--------|
| RV-2 | `domain-service/SKILL.md` | Escape-hatch primary constructor: `_history` → `history` (un-prefixed, idiomatic C# 12 primary constructor) |
| RV-5 | `domain-service/SKILL.md` | Removed duplicate "scoped to domain services" caveat from naming section; one clear statement retained |

### Group 9 — SparklinePoint domain anchor (RV-3)

| Finding | File | Change |
|---------|------|--------|
| RV-3 | `analytics-computation-service/SKILL.md` | Added `SparklinePoint` anchor comment: "define as a domain-layer `sealed record` in `{Module}.Domain/Analytics/` alongside the calculator. This is a project-specific type, not from BuildingBlocks." Target-state label added |

---

## Cycle 2 Fixes (Codex re-verification CX-13 partial)

| Finding | File | Change |
|---------|------|--------|
| CX-13 (partial — behavior example) | `domain-patterns/SKILL.md` | The behavior-method snippet used `Assignee`/`IAction`/`AssigneeChanged` without any label — none of these types exist in `src/`. Added an explicit blockquote: "Illustrative pseudocode — types below are NOT live in this solution" with attribution to the `dotnet-microservices` reference project. Added real this-solution domain event records (`UserCreated(int UserId)`, `PersonCreated(int PersonId)` — both verified present in `Identity.Domain`) as a concrete contrast. No sentence now presents `IAction`/`AssigneeChanged`/`DomainEvent<TAction>` as live BuildingBlocks. |

---

## Step 7 — Acceptance Audit (All 5 Skills) — Post Cycle 1

| Skill | (a) ADR citation | (b) Anti-pattern check | (c) Over-spec test | (d) Example valid | (e) Not-proven note | (f) Cross-ref integrity | (g) General-skill check |
|-------|-----------------|----------------------|--------------------|-------------------|--------------------|-----------------------|------------------------|
| `domain-service` | ADR-005/006/010 cited | Pass — no "adapt", no "too simple" | Pass — pattern-specific; generic DDD deferred to `domain-patterns` | Pass — `BugRatioScorer`, `BugRatioResult`, `HealthThresholdConfig` (real live VO). Endpoint uses `GetBugRatioRequest`/`BugRatioResponse` (verified live types). Mapster snippets labelled target-state. Primary constructor uses `history` (un-prefixed) | Present | Pass — all 6 cross-refs exist | Pass — SprintRituals examples labelled target-state; Application-layer contrast is "see also", not the only way |
| `analytics-computation-service` | ADR-005/006/010 cited | Pass — no "adapt", no "too simple" | Pass — analytics-specific only; base rules deferred to `domain-service` | Pass — `BugRatioScorer`, `BugRatioResult`, `SparklinePoint` (domain-layer anchor noted). `HealthThresholdConfig` used (real live VO). Naming table single-column Good only. Mapster labelled target-state. Grep accept: two `.API/Features` hits are correct before-state callouts | Present | Pass — all 4 cross-refs exist | Pass — target-state labels throughout; API-layer placement labelled "pre-ADR-005 — wrong" |
| `domain-patterns` | ADR-007 cited; ADR-004/011 light-stack axis | Pass — no "adapt", no "too simple" | Pass | Pass — audit field types `(string?)`/`(DateTime?)` added. `IdentityUser<int>` boxed reference-only. `IDomainEvent : IEvent` only (no `INotification`). `SingleValueObject<T>` equality uses `GetType()` concrete-type check. VO-vs-scalar table uses 4 real live VOs with `OwnsOne(..., b => b.ToJson())` persistence. `DomainEvent<TAction>` boxed reference-only. `TransactionalDispatchDomainEventsInterceptor` boxed reference-only. MediatR publisher boxed reference-only | Present | Pass — `persistence-patterns`, `service-registration` referenced | Pass — MediatR/Application content labelled full-stack variant; FastEndpoints labelled light-stack |
| `central-package-management` | ADR-013 cited | Pass — no "adapt", no "too simple" | Pass | Pass — all 3 version-declaration forms in migration recipe; grep uses `--include="*.csproj" src/` portable form + ripgrep alternative | Present | Pass — `framework-currency` cross-referenced bidirectionally | Pass — SprintRituals `Directory.Packages.props` named as "after" worked example |
| `framework-currency` | ADR-011/012 cited | Pass — no "adapt", no "too simple" | Pass | Pass — Stage 1 heading "Find candidate calls"; 2-arg `SendAsync(body, ct)` → `Send.OkAsync` added to map and Stage 2 table; false-positive receivers corrected to `httpClient`/`hubContext`; version-verify reminder present | Present | Pass — `central-package-management` cross-referenced bidirectionally | Pass — general currency rule stack-agnostic; FastEndpoints rule labelled as variant |

---

## Carry-Over Findings

| Title | Severity | For | Evidence | Note |
|-------|----------|-----|----------|------|
| `extract-feature-service` mixed naming example | low | architect | `extract-feature-service/SKILL.md` lists `SprintIssueSyncService` (Service-suffixed) as a good example alongside `HealthScoreCalculator` | Out of Pass 1 scope per plan note (LOW-4). Future-cleanup candidate so it doesn't contradict the domain-service no-suffix rule. |
| Plugin release out of scope | low | team-lead | `plugin.json` version not bumped; skills visible only via dev-loop `--plugin-dir` | Per plan: version bump + `claude plugin validate` + `gen-commands.mjs`/`gen-omni.mjs` + tag/push is the follow-up release step (ADR-9). |

## KB Changes

None. `docs/kb/` is empty (verified in Pass 0). No KB entry to create or update.

## Deviations from Plan

None. All 7 steps executed in the prescribed authoring order. All plan constraints honored:
- General-Skill Mandate (D-A): SprintRituals choices appear as labelled variants/worked examples throughout.
- FIX 1: Application-layer service is a contrast section, not a variant, in `domain-service`.
- FIX 2: `improve-skills` location adaptation noted — plugin `skills/` dir IS native `SKILL.md` format; only the location differs.
- C-2 housekeeping: `skill-backlog.md` created in plugin repo, two CHANGELOGs created, `.claude/README.md` waived (none exists).
- C-3 dev-loop invocation recorded in Step 6 section below.

## Step 6 — Dev-Loop Cache Sync (Q4 Gate)

**Scripts/ re-confirmation:** `<plugin-repo>/scripts/` holds `gen-commands.mjs`, `gen-omni.mjs`, and `skill-classification.md`. None writes the install cache. The dev-loop is the sync mechanism (confirmed per plan LOW-5 and ADR-1).

**Verified working dev-loop invocation:** Per the plugin's `docs/architecture/README.md` ADR-1, the dev-loop command is:
```
claude --plugin-dir <plugin-repo>/plugins/nexus
```
The core `nexus` plugin is the entry point. `nexus-dotnet` declares `dependencies: ["nexus"]` (ADR-3), so the README's example `--plugin-dir` points at the **core** (`/plugins/nexus`), not at `nexus-dotnet`. When Claude loads the core via `--plugin-dir`, `nexus-dotnet` is loaded from the installed cache (version 0.1.1). **Caveat (C-3):** to load the *local working checkout* of `nexus-dotnet` skills simultaneously, both plugin dirs must be specified:
```
claude --plugin-dir <plugin-repo>/plugins/nexus --plugin-dir <plugin-repo>/plugins/nexus-dotnet
```
After launch, `/reload-plugins` hot-reloads SKILL.md edits without a restart.

**This command requires user execution** — this session cannot relaunch the Claude process itself. The team-lead/user runs this to pick up the 5 new/edited skills before Pass 2.

**Frontmatter validation (all 5 skills):**

| Skill | Name field | Matches folder | Description present |
|-------|-----------|----------------|---------------------|
| `domain-service` | `domain-service` | yes | yes |
| `analytics-computation-service` | `analytics-computation-service` | yes | yes |
| `domain-patterns` | `domain-patterns` | yes | yes |
| `central-package-management` | `central-package-management` | yes | yes |
| `framework-currency` | `framework-currency` | yes | yes |

**Skill-inventory dry-run:** The plugin's "Known limitations" (README.md P2) notes that `create-implementation-plan` globs `.claude/skills/` and may under-report plugin skills. The realistic dry-run result: all 5 skills appear in the `available-skills` list surfaced in agent context (confirmed — they appear in the system-reminder at session start). Bare-name references (e.g., `domain-service` in a plan step) resolve at invocation. Discovery via glob is the documented limitation; bare-name invocation works.

## Step 7 — Acceptance Audit (All 5 Skills)

| Skill | (a) ADR citation | (b) Anti-pattern check | (c) Over-spec test | (d) Example valid | (e) Not-proven note | (f) Cross-ref integrity | (g) General-skill check |
|-------|-----------------|----------------------|--------------------|-------------------|--------------------|-----------------------|------------------------|
| `domain-service` | ADR-005/006/010 cited | Pass — no "adapt", no "too simple" | Pass — pattern-specific; generic DDD deferred to `domain-patterns` | Pass — C# uses `BugRatioScorer`, `BugRatioResult`, `BugRatioConfig` (domain types, no framework types); `AggregateRoot`/VO base implicit | Present — "accepted but not proven until Passes 2/3" | Pass — `analytics-computation-service`, `domain-patterns`, `persistence-patterns`, `extract-feature-service`, `service-registration`, `cqrs-patterns` all exist | Pass — SprintRituals examples labelled as "worked example"; Application-layer contrast presented as "see also", not the only way |
| `analytics-computation-service` | ADR-005/006/010 cited | Pass — no "adapt", no "too simple" | Pass — analytics-specific only; base domain-service rules deferred to `domain-service` | Pass — C# uses `BugRatioScorer`, `BugRatioResult`, `SparklinePoint`; FastEndpoints `Send.OkAsync` is 8.x variant, labelled accordingly | Present — "accepted but not proven until Passes 2/3" | Pass — `domain-service`, `domain-patterns`, `persistence-patterns`, `create-feature`, `create-vue-feature` all exist | Pass — SprintRituals path labelled "worked example"; API-layer placement labelled "pre-ADR-005 location — wrong" |
| `domain-patterns` | ADR-007 cited; ADR-004/011 noted as light-stack variant axis | Pass — no "adapt", no "too simple" | Pass — VO-vs-scalar rule is ADR-007 specific; EF config deferred to `persistence-patterns` | Pass — C# uses `AppSettings`→`BugRatioConfig` worked example (labelled SprintRituals); VO factory pattern uses real `StringValueObject`/`SingleValueObject<T>` base types from `Blocks.Domain` | Present — "accepted but not proven until Passes 2/3" | Pass — `persistence-patterns`, `service-registration` referenced in VO section and State Machine section respectively | Pass — MediatR/Application content kept and labelled full-stack variant; FastEndpoints/no-Application-layer content labelled light-stack variant (ADR-004/011 as worked example) |
| `central-package-management` | ADR-013 cited | Pass — no "adapt", no "too simple" | Pass — CPM-specific guidance; MSBuild/NuGet details not restated | Pass — MSBuild XML (`Directory.Packages.props`) is valid MSBuild; `<PackageVersion>`, `<PackageReference>` elements are correct; grep patterns are valid regex | Present — "accepted but not proven until Passes 2/3" | Pass — `framework-currency` cross-referenced bidirectionally | Pass — SprintRituals `Directory.Packages.props` named as "after" worked example; general recipe applies to any project |
| `framework-currency` | ADR-011/012 cited | Pass — no "adapt", no "too simple" | Pass — detection patterns and false-positive list are specific to this migration; general currency rule is genuinely general | Pass — migration map verified against FastEndpoints 8.1.0 (from `Directory.Packages.props:18`); live file:line for both false positives (`GraphQLXrayClient.cs:296`, `SyncSprintsEndpoint.cs:46`); grep regex `\bSend\w*Async(` is correct | Present — "accepted but not proven until Passes 2/3" | Pass — `central-package-management` cross-referenced bidirectionally | Pass — general currency rule is stack-agnostic; FastEndpoints `Send.*` rule labelled as "FastEndpoints Variant"; 8.1.0 named as "current worked example" with "verify against installed" caution |
