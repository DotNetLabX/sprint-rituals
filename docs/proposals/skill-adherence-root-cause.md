# Skill Adherence Root-Cause Analysis

**Date:** 2026-06-05  
**Sources:** `<fokus-repo>/docs/specs/adhoc-SkillAlignment/`, `adhoc-SkillAlignmentEndpoints/`, `adhoc-SkillEnforcement/`, `adhoc-SkillCleanup/`, `adhoc-PipelineCraftImprovements/`, `adhoc-ExtractJiraModule/`, `adhoc-JiraModuleRefactor/`, `F1-Scaffolding/`, `F4-JiraSync/` delivery files.

---

## 1. Concrete, Recurring Root Causes

### 1a. Skills built for a predecessor project — not portable to a greenfield

The skills were extracted from the Reflekt codebase. At the time F1-Scaffolding was planned, 45 of 57 skill files contained Reflekt-specific entity names, service paths, package names (`Articles.sln`, `Articles.Abstractions`, etc.), and variant tables keyed to named services (`Auth`, `Journals`, `Production`).

**Effect:** The F1-Scaffolding planner correctly noted "there are no shared BuildingBlocks to reference" and explicitly instructed the executor to _adapt_ rather than follow the skills. The word "adapt" was later identified as the weasel word that licensed skill bypass.

> _"The plan correctly identified that existing skills assume shared BuildingBlocks and instructed executor to adapt rather than follow blindly."_  
> — `F1-Scaffolding/delivery/lessons.md`, Planner Lessons

> _"The root cause: the Scaffolding and JiraSync plans bypassed skills because (a) the architecture doc was written without checking the skill inventory, and (b) the plan format had no structural constraint requiring skill references. Prose rules in `architect.md` were insufficient — the architect 'adapted' skills (meaning ignored them) with the architecture doc as justification."_  
> — `adhoc-SkillEnforcement/delivery/plan.md`

**Result:** F1 built all base classes, repositories, and domain infrastructure inline inside Fokus projects — plain classes, no `Entity<T>` or `AggregateRoot`, no BuildingBlocks packages, no error middleware, no Guard utilities. F4-JiraSync added more inline infrastructure on top of it.

---

### 1b. Plans did not reference skills — no structural enforcement

Prior to `adhoc-SkillEnforcement`, implementation plans had no mandatory Skill Mapping section. A plan step could say "create Sprint entity" with no reference to `domain-patterns` or `create-aggregate`. The developer had no signal to consult the skill.

> _"The plan format had no structural constraint requiring skill references."_  
> — `adhoc-SkillEnforcement/delivery/plan.md`

The F1 and F4 plans confirm this: they contain full inline step details with no Skill Mapping table and no skill disposition column.

---

### 1c. Architecture doc written without skill inventory scan

The architecture doc (`docs/architecture/v1.md`) was authored before the skill inventory was formalized. It contained inline implementation detail — entity shapes, persistence patterns — that skills already defined. This meant the architect had a local justification ("it's in the arch doc") to ignore skills.

> _"The architecture doc was written without checking the skill inventory."_  
> — `adhoc-SkillEnforcement/delivery/plan.md`

---

### 1d. "Adapt" as a disposition — licensed bypass without enforcement

Before `adhoc-SkillEnforcement`, plans used a third disposition: **Adapt** (alongside Follow and Build). "Adapt" meant "the skill exists but we're doing something different." In practice it meant the skill was ignored with no justification required.

> _"Never 'Adapt' — if you're about to write 'adapted for this project's needs,' you're skipping the skill. Either Follow (the pattern applies, specifics may differ) or Build (the infrastructure is missing)."_  
> — `adhoc-SkillEnforcement/delivery/plan.md`, Disposition Rules (added to ban "Adapt")

---

### 1e. Missing `create-building-blocks-package` skill

When SkillAlignment ran (Phase 1), steps 1–6 each required building a new BuildingBlocks package from scratch. No skill covered this workflow. Each package's structure, namespace convention, and inter-package dependency rules had to be inferred from skill descriptions.

> _"Steps 1–6 each build a BuildingBlocks package from scratch. There is no skill covering this. Each package's structure had to be inferred from the skill descriptions."_  
> — `adhoc-SkillAlignment/delivery/lessons.md`, Skill Gaps

---

### 1f. Ill-fitting skill content — non-compilable patterns

The `persistence-patterns` skill described `ApplicationDbContext` with `ref`-parameter cache helpers and `AuditedEntityConfiguration` with `HasDefaultValueSql`. Both fail to compile:

- `ref` parameters are illegal in `async` C# methods.
- `HasDefaultValueSql` lives in the Relational EF package, not the base package a `Blocks.EntityFrameworkCore` project would reference.

> _"The skill mentions 'in-memory cache helpers' using ref parameters — this will not compile."_  
> _"HasDefaultValueSql and GetTableName() are relational EF extensions … not available in the base package."_  
> — `adhoc-SkillAlignment/delivery/lessons.md`, Skill Gaps

A developer hitting a compile error from a skill pattern has an incentive to abandon the skill entirely rather than fix the narrower issue.

---

### 1g. Endpoint file structure violated before Phase 2 cleanup

Before `adhoc-SkillAlignmentEndpoints`, every endpoint had its Request, Response, and Validator in separate files — three or four files per feature instead of the skill-prescribed two (`{Name}Endpoint.cs` + `{Name}Command.cs`). Twelve files were deleted and merged in Phase 2.

> Files deleted: `SaveSettingsRequest.cs`, `SaveSettingsResponse.cs`, `SaveSettingsValidator.cs`, `GetBoardsResponse.cs`, `GetJiraSprintsRequest.cs`, `GetJiraSprintsResponse.cs`, `GetJiraSprintsValidator.cs`, `SyncSprintsRequest.cs`, `SyncSprintsResponse.cs`, `SyncSprintsValidator.cs` — plus two more.  
> — `adhoc-SkillAlignmentEndpoints/delivery/implementation.md`

The original F4-JiraSync plan specified file names but did not reference the `create-feature` skill's two-file structure rule.

---

### 1h. Skill precondition not met silently

When a skill requires infrastructure to be present (BuildingBlocks, a service CLAUDE.md), the developer invoked it without checking preconditions.

> _"Invoking `create-service` or other prerequisite-requiring skills without the required CLAUDE.md or infrastructure."_  
> — `docs/conventions/known-deviations.md` (seeded from adhoc-SuiteScaffolding)

---

### 1i. Build verification batched to end

Developers ran `dotnet build` only after all steps were complete, meaning a type error introduced at step 3 compounded through steps 4–8.

> _"Build verification batched to end … a type error introduced in step 3 compounds through steps 4-8."_  
> — `docs/conventions/known-deviations.md` (seeded from F30)

---

### 1j. Thin domain / service-class overuse

The F1 scaffolding plan instructed inline infrastructure with no domain base classes, producing plain C# classes. F4 added a `SyncSprintsService` and `SyncBacklogService` as service classes instead of lean vertical-slice handler logic. The architecture doc noted "thin domain (v1)" as a _design choice_, but the service classes were an unforced pattern violation — the `create-feature` skill prescribes handler-in-endpoint-file, not a separate `*Service` class.

The SkillAlignment Phase 1 review confirmed the guardrail held post-cleanup:

> _"No `*Service.cs` files in Features/ — clean vertical slices."_  
> — `adhoc-SkillAlignment/delivery/review.md`

But this was only after a dedicated cleanup pass.

---

## 2. Enforcement Mechanisms Tried and Their Outcomes

| Mechanism | When Introduced | Did It Work? |
|-----------|----------------|-------------|
| Prose rule in `architect.md`: "reference skills, never skip" | Early (pre-F1) | No — architect used "adapt" to bypass |
| Architecture doc (v1) as justification gate | Early | Backfired — became cover for inline patterns |
| `create-architecture-doc` skill with mandatory skill inventory | `adhoc-SkillEnforcement` | Yes — forces skill scan before doc writing |
| `create-implementation-plan` skill with mandatory Skill Mapping table | `adhoc-SkillEnforcement` | Yes — bans "Adapt", requires Follow/Build/None with justification |
| Anti-pattern check in plan skill ("scan for 'adapted'" keyword) | `adhoc-SkillEnforcement` | Yes — structural, not prose |
| Reviewer checklist: "skill mapping verified — any None disposition was warranted" | `adhoc-SkillEnforcement` | Yes — closes the done-check gap |
| `adhoc-SkillCleanup` — remove Reflekt-specific references from all 57 skill files | `adhoc-SkillCleanup` | Yes — portable skills are followable skills |
| Known-deviations catalog (`docs/conventions/known-deviations.md`) | `adhoc-PipelineCraftImprovements` | Seeded — effectiveness depends on developer pre-flight |
| Developer pre-flight: read `known-deviations.md` in Phase 1 | `adhoc-PipelineCraftImprovements` | Structural addition — not yet battle-tested at port time |
| Developer self-check gate table (structured, blocking) | `adhoc-PipelineCraftImprovements` | Replaces prose checklist — more enforceable |

**Key finding:** The two structural enforcement mechanisms (`create-implementation-plan` Skill Mapping + reviewer check) were only created in `adhoc-SkillEnforcement`, which ran _after_ F1 and F4 had already shipped the violations. Sprint-Rituals was scaffolded from the fokus codebase at a point where some but not all of these protections were in place.

---

## 3. Skills Most Often Violated

| Skill | Violation | First Seen |
|-------|-----------|-----------|
| `domain-patterns` | No `Entity<T>` / `AggregateRoot` base classes; plain classes with custom PK property names (`Key`, `AccountId`) | F1-Scaffolding |
| `persistence-patterns` | No `RepositoryBase`, no `EntityConfiguration` base; DbContext extends plain `DbContext` instead of `ApplicationDbContext` | F1-Scaffolding |
| `error-handling` | No Guard utilities, no HttpException hierarchy, no GlobalExceptionMiddleware | F1-Scaffolding |
| `create-feature` (endpoint structure) | Request, Response, Validator in 3 separate files instead of co-located `{Name}Command.cs` | F4-JiraSync |
| `create-building-blocks-package` | Skill did not exist — BuildingBlocks had to be built from scratch with no guidance | adhoc-SkillAlignment Phase 1 |
| `persistence-patterns` (SeedFromJsonFile) | Extension absent from `EntityConfiguration` base; not flagged as deviation | adhoc-SkillAlignment Phase 1 |

---

## 4. Specific Lessons on Request-vs-Command Naming, Single-File Endpoints, Thin Domain, and Mapster

### Request vs Command naming

The `create-feature` skill prescribes: **Command** for POST (mutates state), **Query** for GET with request params. The original F4 files used `*Request.cs` as standalone files, not co-located Command/Query files. This was corrected in `adhoc-SkillAlignmentEndpoints`.

> _"Naming convention: Use `Command` for POST (mutates state), `Query` for GET with request params."_  
> — `adhoc-SkillAlignmentEndpoints/delivery/plan.md`

No lesson file explicitly says "we used Request when Command was correct" — the violation was structural (wrong file layout) and the naming lesson is embedded in the plan's step description.

### Single-file endpoints

The `create-feature` skill's FastEndpoints variant defines exactly two files per feature: `{Name}Endpoint.cs` and `{Name}Command.cs`. Twelve files had to be deleted and merged to reach compliance.

> _"Restructure 6 endpoints to match the `create-feature` skill file structure."_  
> — `adhoc-SkillAlignmentEndpoints/delivery/plan.md`

No lesson explicitly names this as a root cause; the structural nature of the violation (extra files existed) made it mechanically fixable once the skill was consulted.

### Thin domain / service-class overuse

The F1 scaffolding explicitly intended "thin domain (v1)" as a design choice. The problem was that "thin" was interpreted as "plain classes with no base infrastructure" rather than "aggregates with base classes but no rich behavior yet." The distinction:

- **Correct thin domain:** `Sprint : AggregateRoot<int>` with no behavior methods — infrastructure present, behavior deferred.
- **Actual thin domain (F1):** `Sprint` plain class with no base class — infrastructure absent.

The SkillAlignment review confirmed the guardrail post-fix: _"No `*Service.cs` files in Features/"_ — but service classes had existed before the cleanup.

### Mapster mappings

No lesson file explicitly addresses Mapster mapping violations. The `create-feature` skill prescribes Mapster via a `Mappings.md` workflow file. The original F4 JiraMapper used manual mapping code (`JiraMapper.cs`). This was not cleaned up as part of the SkillAlignment pass — JiraMapper was kept as infrastructure-layer mapping (Jira DTO → domain entity), which is distinct from the endpoint response mapping the Mapster skill governs. No violation was formally logged.

---

## 5. What Sprint-Rituals Inherited

At port time, the fokus codebase had:

- Skills cleaned of Reflekt references (post `adhoc-SkillCleanup`)
- `create-implementation-plan` skill with Skill Mapping enforcement (post `adhoc-SkillEnforcement`)
- BuildingBlocks packages built (post `adhoc-SkillAlignment`)
- Two-file endpoint structure enforced (post `adhoc-SkillAlignmentEndpoints`)

What the fokus codebase had NOT yet done at port time:

- The `create-building-blocks-package` skill was logged as a gap but may not have been created
- Known-deviations catalog was seeded but thin (6 entries)
- Pipeline hardening (`adhoc-PipelineCraftImprovements`) was recent and only partially battle-tested

The git status shows `src/BuildingBlocks/Blocks.Contracts/` as untracked in sprint-rituals — meaning the Contracts package was being added post-port. Violations in the current sprint-rituals codebase likely reflect the same root cause pattern: early features written before `create-implementation-plan` enforcement was applied, using inline patterns instead of skill-prescribed ones.

---

## 6. Systemic Fix Recommendations for Sprint-Rituals

1. **Every new plan must use the `create-implementation-plan` skill** — Skill Mapping table is mandatory; "Adapt" is banned. This is the single highest-leverage fix.

2. **Reviewer checklist must include skill mapping verification** — confirm any "None" disposition was warranted (no skill covers the step).

3. **Before implementing any feature touching domain/persistence/endpoints, read the relevant skill first** — even if the code "looks simple." Skills ensure consistency, not just complexity management.

4. **Treat `SeedFromJsonFile` as deferred (known gap)** — the `persistence-patterns` skill references it; it was absent in fokus Phase 1 and needs to be present in Blocks.EntityFrameworkCore before any feature relies on it.

5. **Run `dotnet build` after each plan step** — not batched to the end.
