# Pass 1 — SkillRepair — Questions

## Q1: Where do repaired/created skills physically land? (BLOCKER)
**From:** architect
**To:** user
**Status:** Resolved
**Step:** Phase 1 analysis

**Context:** This project has **zero local skills**. There is no `.claude/skills/` directory and no `SKILL.md` anywhere under `this repo` (verified by `find`). The project deliberately went "plugin-canonical" — `docs/proposals/nexus-format-gap.md` records that all plugin-provided agents/rules/skills were removed locally so the project relies entirely on the installed `nexus` + `nexus-dotnet` plugins.

Every skill Pass 1 targets (`domain-patterns`, `analytics-computation-service`, `create-aggregate`, `persistence-patterns`, `create-feature`, `service-registration`, plus the missing `central-package-management` / `framework-currency`, plus a new `domain-service`) lives only in the **versioned, shared plugin cache**:
`<home>\.claude\plugins\cache\claude-nexus\nexus-dotnet\0.1.1\skills\` and `...\nexus\0.1.2\skills\`.

Editing those files affects **every project on this machine**, is outside this git repo, and is upstream-plugin maintenance — not something the SprintRituals pipeline (developer edits repo → reviewer reviews repo diff → team-lead commits repo) can carry. The pipeline has nothing in-repo to operate on if the work happens in the cache.

The `nexus-format-gap.md` proposal is itself explicit that skill/format fixes belong "in the plugin-build project," not here.

**Question:** Where should Pass 1's skill repairs land? Options:
- **(A) Project-local override** — create `.claude\skills\{name}\SKILL.md` for each repaired/new skill (project copies that shadow the plugin). Reverses the plugin-canonical decision for these specific skills; keeps the work inside this repo and pipeline-reviewable. **Recommended IF the work must run through this project's pipeline.**
- **(B) Upstream the fixes in the plugin-build project** — Pass 1 here produces only the *specifications* (a skill-repair spec doc per skill: exact defect, target wording, ADR mapping) committed to `docs/`, and the actual `SKILL.md` edits are done in the plugin repo as a separate effort. Keeps plugin-canonical intact; this repo stays the source of truth for *what* to change, not *where the skill lives*.
- **(C) Hybrid** — new project-specific skills (`domain-service`, `central-package-management`, `framework-currency`) land project-local (A); edits to existing shared skills (`analytics-computation-service` SMELL, `domain-patterns` VO gap) are specified for upstream (B).

This decision determines the entire shape of the plan (what files the developer creates/edits) and must be made before any plan can be written.

### Answer
**Operating model (none of A/B/C — the keystone is a separate editable plugin SOURCE repo).** Skills are NOT project-local and NOT edited in the read-only cache. They are fixed in the editable plugin source repo `<plugin-repo>` (a separate git repo). All 5 Pass 1 skills live under `plugins/nexus-dotnet/skills/`:
- repair `plugins/nexus-dotnet/skills/analytics-computation-service/`
- repair `plugins/nexus-dotnet/skills/domain-patterns/`
- create `plugins/nexus-dotnet/skills/domain-service/` (new)
- create `plugins/nexus-dotnet/skills/central-package-management/` (new)
- create `plugins/nexus-dotnet/skills/framework-currency/` (new)

Consequences for the plan:
- Pipeline artifacts (plan.md, implementation.md, review.md, lessons.md, summary.md) stay in THIS repo at `docs/specs/adhoc-Pass1-SkillRepair/delivery/`. Skill edits land in the plugin repo. **Two repos, two commit targets** — the diff to review is in `<plugin-repo>`.
- Include a step to **rebuild/sync the installed plugin cache** from source after edits (inspect `<plugin-repo>\scripts\` for a build/sync script) so the Q4 dry-run skill-inventory scan sees the new skills. Verify the cache reflects the edits before the dry-run.

---

## Q2: The `domain-service` skill shape — interactive design checkpoint (BLOCKER)
**From:** architect
**To:** user
**Status:** Resolved
**Step:** Phase 1 analysis

**Context:** `docs/backlog.md` (line 38) states verbatim: *"Pass 1 (skill repair) has open design detail (the new `domain-service` skill shape) — the user wants to review that interactively, so checkpoint before finalizing it."* This is a binding user instruction.

ADR-005 mandates a cross-aggregate **domain service** pattern: services in `{Module}.Domain/{Area}/` (e.g. `Fokus.Domain/Analytics/SprintSummaryCalculator.cs`) that **return domain types / value objects, never API response DTOs**; the endpoint Mapster-maps the domain result to the response DTO. This is the resolution for triage problems #4/#5/#6/#7 and is the single largest skill gap (no skill currently defines a domain-service concept).

Complication: the reference project (`<reference-repo>`, `reference-patterns-domain.md` §6) explicitly found **"No standalone Domain Service with domain-layer dependencies"** — it puts cross-aggregate logic in Application-layer handlers / an `ArticleAccessChecker`-style Application service. But ADR-004 says this project has **no Application layer** (API + Domain + Persistence only). So the reference's chosen home for cross-aggregate logic (the Application layer) **does not exist here**, and ADR-005 deliberately diverges by placing it in the Domain layer instead.

The skill therefore can't be a straight reference port — its shape is a genuine design decision.

**Question:** Please confirm the `domain-service` skill's shape before I write it into the plan:
1. **Location & naming:** `{Module}.Domain/{Area}/{Name}Calculator.cs` (or `{Name}Service`)? ADR-005's example is `SprintSummaryCalculator`. Confirm the suffix convention (`Calculator` vs `Service` vs `DomainService`).
2. **Dependencies:** May a domain service take repository interfaces (to load aggregates), or must it receive pre-loaded aggregates from the endpoint (pure function over domain state)? The reference's analytics services receive pre-loaded data; ADR-005 doesn't say.
3. **Return type boundary:** Confirm domain services return domain types / VOs only, and the endpoint owns the Mapster map to the response DTO (this is what fixes the `analytics-computation-service` SMELL).
4. **Relationship to `analytics-computation-service`:** Does the new `domain-service` skill **replace** `analytics-computation-service` (which currently returns response records in the API layer — directly conflicting ADR-005), or do they **coexist** (analytics = a specialization)? This determines whether Pass 1 deprecates/rewrites `analytics-computation-service` or just cross-references it.

### Answer
Resolved interactively with the user:
- **(a) Coexist as specialization.** Rewrite `analytics-computation-service` as a SPECIALIZATION of `domain-service`: it references the base skill, returns domain types (NOT response records — this kills the SMELL), and adds only analytics-specific guidance (sparklines, rolling averages, delta/direction/polarity helpers).
- **(b) Naming rule: NO `Service`/`DomainService` suffix.** Name the class by what it does — `SprintSummaryCalculator`, `CycleTimeAnalyzer`, `BugRatioScorer`, `VelocityProjector`. State this as an explicit rule in the skill with a "don't reach for `Service`" callout. Keep it inside the domain-service skill; do NOT expand to a project-wide convention (out of Pass 1 scope).
- **(c) Dependencies: PURE by default.** The endpoint handler loads aggregates via repositories and passes them into the domain class; the domain stays DB-free and unit-testable. Escape hatch: inject repository interfaces into the domain ONLY for performance, when the domain genuinely knows better what/how to load, and only with that justification documented. Pure is the default; repo-injection is the justified exception.
- **(d) Return boundary: domain types/VOs out; endpoint owns the Mapster map to the response DTO.** BUT avoid gratuitous DTO duplication — introduce a distinct response DTO only when the wire shape actually diverges from the domain type; don't create type twins. The skill must make this pragmatic point explicitly.

---

## Q3: Exact Pass 1 scope — which skills, repair vs create (CONFIRM)
**From:** architect
**To:** user
**Status:** Resolved
**Step:** Phase 1 analysis

**Context:** Unlike Pass 0, `docs/backlog.md` has **no dedicated scope section for Pass 1** — only the one-line table row "Repair/create skills (root cause)". The scope must be assembled from the ADRs, the triage skill verdicts (`refactor-triage.md` "Skill verdicts" + "Skills most often violated"), and Pass 0's deferrals (`adhoc-Pass0-Foundations/delivery/summary.md` line 20, `lessons.md` Skill Gaps).

**Important:** Two defects named in the proposals are **already fixed** in the currently-installed plugin skills (`nexus-dotnet/0.1.1`), so they are NOT Pass 1 work:
- `create-aggregate` — the referenced `workflows/*.md` files (`AggregateEfCore.md`, `AggregateRedis.md`, `ValueObject.md`, `DomainEvent.md`) **now exist** in the cache. The "references non-existent workflow files" SMELL is resolved.
- `persistence-patterns` — the `ref`-param cache helper and `HasDefaultValueSql` issues are **now documented as explicit warnings/caveats** in the skill (lines ~80, ~105), not silent non-compilable patterns.

Defects I **confirmed still present** in the installed plugin skills (genuine Pass 1 candidates):
- **`analytics-computation-service`** — still declares "Response records at the top of the file — all records the service returns" (line 31). Service returns API response DTOs → **directly conflicts ADR-005**. (Triage #6 SMELL.)
- **`domain-patterns`** — still has **no "when to use a value object vs a flat scalar"** rule. ADR-007's "group cohesive scalars into VOs" is unsupported by the skill. (Triage #1d gap.)
- **`domain-service`** — **MISSING** entirely (see Q2).
- **`central-package-management`** — **MISSING** (Pass 0 deferred; pattern derived from reference).
- **`framework-currency`** — **MISSING** (Pass 0 deferred; must include the bare `SendAsync(...)` form + the HttpClient and SignalR `SendAsync` false positives — see Pass 0 lessons).

Lower-confidence / possibly-out-of-scope:
- `create-feature` / `extract-endpoint-types` — triage marks these "correct, ignored" (the *code* violated them, fixed in Pass 2), not defective skills. Likely **no Pass 1 repair** needed — confirm.
- `cqrs-patterns` — ADR-004 says no MediatR/CQRS-dispatch in this project. The skill may need a "not used in light-stack projects" note, or may be out of scope entirely. Confirm.
- `service-registration` — Pass 0 followed it successfully (the `Migrate<T>()` helper is now built locally). Possibly a small "async `Migrate<T>()`" note (Pass 0 LOW-1). Confirm whether that belongs here.

**Question:** Please confirm the Pass 1 skill set:
1. Repair `analytics-computation-service` (or fold into `domain-service` per Q2.4)? **Yes/fold/no.**
2. Repair `domain-patterns` (add the VO-vs-scalar rule)? **Yes/no.**
3. Create `domain-service` (per Q2)? **Yes/no.**
4. Create `central-package-management`? **Yes/no.**
5. Create `framework-currency`? **Yes/no.**
6. Any of the lower-confidence items (`create-feature`, `cqrs-patterns`, `service-registration`/`Migrate<T>` async) in scope? **Which.**

### Answer
**5 core skills only.** Repair `analytics-computation-service` + `domain-patterns` (add the VO-vs-scalar rule per ADR-007). Create `domain-service` + `central-package-management` + `framework-currency`. Do NOT touch `create-feature`, `cqrs-patterns`, or `service-registration`. Confirm against the **source repo** (`<plugin-repo>`) that `create-aggregate` (workflow files) and `persistence-patterns` (ref-param / HasDefaultValueSql) are already fixed and exclude them. For `framework-currency`, include the bare `SendAsync(...)` form AND the HttpClient / SignalR `SendAsync` false-positives (per Pass 0 lessons).

---

## Q4: Are skill repairs reviewable artifacts, and how is "build green" replaced? (CONFIRM)
**From:** architect
**To:** user
**Status:** Resolved
**Step:** Phase 1 analysis

**Context:** Every prior pass's acceptance bar was `dotnet build SprintRituals.slnx` green. Pass 1 edits Markdown skill files — there is **no compiler**. The done-check (Step 1) and code review (Step 2) need a different verification contract: ADR mapping (each repaired skill cites the ADR it now satisfies), an anti-pattern self-check (the `create-implementation-plan` "scan for 'adapt'" and over-specification tests applied to the *edited* skill), and — where a skill emits code patterns — a "the example compiles against this solution's BuildingBlocks" check.

This also interacts with Q1: if skills land project-local (A), they ARE in-repo and reviewable normally. If they're upstream specs (B), the reviewable artifact is the spec doc, not a `SKILL.md`.

**Question:** Confirm the Pass 1 acceptance bar:
- For each repaired/created skill: cites the governing ADR(s); passes the anti-pattern/over-specification checks; any embedded code example compiles against this solution's actual BuildingBlocks (`Blocks.Domain`, `Blocks.EntityFrameworkCore`, `Blocks.FastEndpoints`).
- Plus: a follow-up note that Passes 2/3 (which *apply* these skills) are the real proving ground, so a Pass 1 skill is "accepted" but not "proven" until consumed.

Acceptable? Any additional gate you want (e.g., a dry-run of `create-implementation-plan`'s skill-inventory scan against the new project-local skills)?

### Answer
**Accepted, plus a mandatory dry-run gate.** Each repaired/created skill must: (1) cite its governing ADR(s); (2) pass the anti-pattern / over-specification checks (the `create-implementation-plan` "scan for 'adapt'" + over-specification tests applied to the edited skill); (3) any embedded code example compiles against this solution's actual BuildingBlocks (`Blocks.Domain`, `Blocks.EntityFrameworkCore`, `Blocks.FastEndpoints`); (4) carry an "accepted but not proven until Passes 2/3 consume it" note. PLUS: a dry-run of `create-implementation-plan`'s skill-inventory scan against the new skills (after the cache sync) to confirm they're discoverable/mappable before Pass 2.
