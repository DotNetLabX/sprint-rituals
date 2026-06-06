# Review — adhoc-Pass1-SkillRepair (plan review, independent critic pass)

**Reviewer:** Critic (fresh-context, independent author≠reviewer gate)
**Artifact under review:** `docs/specs/adhoc-Pass1-SkillRepair/delivery/plan.md`
**Mode:** Plan review (plan vs. spec/ADRs/resolved questions). This is the genuine separation gate: the architect authored the plan AND ran an in-context critic pass; this pass re-derives every claim from source.

---

# RE-REVIEW (revised plan) — Verdict: **GO** (unambiguous)

The plan was revised after the first-pass GO (3 MEDIUM, below), Codex's NO-GO (6 HIGH + 3 MEDIUM), and two user decisions (D-A, D-B). I re-read the full revised plan and re-verified every cited fact and live `file:line` against source. **Every item folds correctly. Nothing remains PARTIAL or NOT-resolved at MEDIUM+. GO.**

The revision is faithful and self-consistent. Notably it does NOT over-correct: the General-Skill Mandate (D-A) is applied as variant-framing without breaking any accept criterion, the grep-based accept criteria (Step 1 HIGH #4, Step 4 MED #7) are objective and verifiable, and the cross-ref set (Step 3 pt7 ↔ Step 7 check 6) is now closed. All live citations the developer will act on are exact.

## Re-verified against source (this pass)

| Claim in revised plan | Source check | Result |
|---|---|---|
| Step 5 HttpClient false positive `GraphQLXrayClient.cs:296` | grep `SendAsync` in `src` | EXACT — `response = await httpClient.SendAsync(request, ct);` line 296 |
| Step 5 SignalR false positive `SyncSprintsEndpoint.cs:46` | grep `SendAsync` in `src` | EXACT — `await hubContext.Clients.All.SendAsync(` line 46 |
| Revision-note #1 / Step 4: "Pass 0 already cleaned all three version forms; repo is the 'after' example" | grep `Version=`/`VersionOverride=`/`<Version>` in all `*.csproj` | CONFIRMED — zero matches repo-wide |
| Step 1 MED #5: `BugRatioService.cs:53-116` defines whole response/result trees | Read file | CONFIRMED — lines 3-116 are the response-record hierarchy (`BugRatioMultiSprintResponse` @53, `BugRatioSingleSprintResponse` @105, `BugRatioResponse` wrapper @113); service class @120 |
| LOW-4: `extract-feature-service` lists `SprintIssueSyncService` (Service-suffix) beside `HealthScoreCalculator` | Read `extract-feature-service/SKILL.md:20-23` | CONFIRMED — the mixed example is real; Step 3 pt2 + flag #4 handle it |
| Light-stack variant facts (no Application layer; `Blocks.FastEndpoints/DomainEventPublisher`; `AppSettings` owns config VOs) | Fokus `CLAUDE.md` (auto-loaded) | CONFIRMED — corroborates Step 2b annotation + Step 2a VO example |

## Per-item resolution table

### My 3 MEDIUMs (first pass)
| Item | Resolved? | Location |
|---|---|---|
| C-1 — FastEndpoints target = installed 8.1.0; verify names vs installed pkg; 7.x illustrative only | YES | Step 5 pt3 (8.1.0 named, "verify, don't trust memory" caution); ADR→skill map ADR-011 row; accept line |
| C-2 — `improve-skills` backlog/CHANGELOG/README housekeeping → PLUGIN repo, not sprint-rituals | YES | New "`improve-skills` housekeeping routing" section (plugin-repo paths, explicit "do NOT touch sprint-rituals `.claude/README.md`"); two-repo table row; Skill-Mapping disposition note |
| C-3 — `--plugin-dir` may not load `nexus` core dep; verify invocation | YES | Cache-model "Dependency caveat (C-3)" + Step 6 pt1 (developer must determine & record working invocation); accept requires "verified working dev-loop command" |

### Codex findings
| Item | Resolved? | Location |
|---|---|---|
| #2 — input rule allows intrinsic primitives/enums | YES | Step 3 pt5 (primitives/enums allowed; VO when input carries domain meaning); ADR-006 map row; Step 1 pt3 inherits carve-out |
| #3 — escape hatch = narrow domain-owned read port; no concrete repos / no `IQueryable` | YES | Step 3 pt3 (interface declared in `{Module}.Domain`, returns domain types, forbids `IRepository`/`RepositoryBase`/`IQueryable`); Step 1 pt3; accept line |
| #4 — whole-skill reframe of analytics + grep accept | YES | Step 1 pt1 (rewrite description/When-to-use/Reference-table/naming); accept = grep `\.API/Features`, `response record`, `Service\.cs` → zero domain-context hits |
| #6 — `domain-patterns` MediatR/Application content annotated, NOT scrubbed | YES | Step 2b (keep + label full-stack variant + add light-stack alongside); accept "nothing scrubbed"; Revision-note #2 reconciles Codex intent with D-A |
| #8 — two-stage `SendAsync` detection | YES | Step 5 pt4 (Stage 1 unqualified-in-endpoint incl. bare overload; Stage 2 manual receiver inspection) |
| #5 — named domain result type (e.g. `BugRatioResult`) | YES | Step 1 pt2 (dedicated domain result records/VOs, `BugRatioResult` example, endpoint maps to `{Action}Response`) |
| #7 — broadened CPM grep, three forms | YES | Step 4 pt3 (inline `Version=`, child `<Version>`, `VersionOverride=`; preserve `PrivateAssets`/`IncludeAssets`); accept line; Revision-note #1 |
| #9 — Step 3 ↔ Step 7 cross-refs aligned | YES | Step 3 pt7 authors `persistence-patterns`+`domain-patterns` cross-refs; Step 7 check 6 lists the full closed set incl. `domain-patterns → persistence-patterns` |
| #1 (general-skill / multi-variant) | YES | = D-A below |

### User decisions
| Item | Resolved? | Location |
|---|---|---|
| D-A — skills GENERAL/multi-variant; ADRs = one labelled variant, not hard-coded | YES | "GENERAL-SKILL MANDATE" section (3 binding rules); per-skill bar check (e); Step 7 check 7; every step states its variant axis (Step 3 pt1/pt6 placement+DI variants, Step 5 general-rule+FE-variant, Step 2b full/light annotation) |
| D-B — endpoint ALWAYS owns response DTO + map, even 1:1 | YES | Step 3 pt4 (explicitly supersedes prior "avoid DTO twins / reuse domain type 1:1"); Step 1 pt2; ADR→skill map ADR-010 row |

## New inconsistencies introduced by the revision — none material

I specifically checked for variant framing that contradicts an accept criterion, cross-refs pointing nowhere, and self-inconsistent Revision notes:

- **D-B supersession is clean.** Step 3 pt4 explicitly states it supersedes the first-pass "avoid gratuitous DTO twins / reuse domain type when 1:1" guidance; no stray remnant of the old rule survives elsewhere (Step 1 pt2 also states the always-own-DTO form). No contradiction.
- **D-A variant framing does not break any accept.** The accept criteria that demand specifics (grep targets, the `BugRatioResult` result type, the read-port shape) are all phrased as requirements *plus* "labelled as a worked example," not as "the only way" — so check (e) and the substantive accepts coexist.
- **Cross-ref graph is closed.** Step 3 pt7 now authors the `domain-patterns` + `persistence-patterns` cross-refs that Step 7 check 6 verifies; no referenced skill name is dangling (`domain-service`, `analytics-computation-service`, `domain-patterns`, `persistence-patterns`, `extract-feature-service`, `service-registration`, `central-package-management`, `framework-currency` all exist in the plugin skills dir).
- **Revision notes are self-consistent.** #1 (MED #7 folded as skill-generality requirement, substantiated by the zero-match grep), #2 (HIGH #6 = annotate-not-scrub per D-A, reconciled with Codex), #3 (Codex HIGH #7 = critic C-1, folded once) are accurate and do not conflict with the step bodies they describe.

## One LOW worth recording (does not affect GO)

- **LOW (attribution, not a defect):** The ADR→skill map ADR-010 row and Step 3 pt4 attach D-B ("endpoint always owns a DTO + map, even 1:1") to ADR-010. ADR-010's *literal* text is a mapping convention ("field names match the entity → trivial Mapster; don't hand-assign field-by-field") and does not itself state the always-own-even-1:1 rule. D-B is an authoritative **user decision** that extends ADR-010 compatibly. The plan honestly tags the rule "(D-B)", so this is correct as-is — just note the rule's authority is D-B, not the ADR-010 wording, in case a future reader cites ADR-010 expecting that sentence. No change required.

## Bottom line

**GO.** All 3 of my MEDIUMs, all 9 Codex findings (6 HIGH + 3 MED), and both user decisions (D-A, D-B) are folded correctly and verified against source. The revision introduces no new MEDIUM+ contradiction. The plan is ready for the developer.

---

# FIRST-PASS REVIEW (pre-revision, retained for record)

## Verdict: GO (with 3 MEDIUM fix-before-implement recommendations)

The plan faithfully encodes the binding design (Q1–Q4) and the governing ADRs. Every load-bearing factual claim I could check against source is accurate: the two-repo model, the Q3 already-fixed exclusions, the three confirmed SMELL/gap claims, the cache/dev-loop model, and the reference anchors. **No CRITICAL or HIGH findings** — nothing in the plan would cause the developer to author a *wrong* skill. The MEDIUMs are real omissions that would cause friction or an unverifiable acceptance step, not a wrong artifact; they are cheap to fix and I recommend folding them in before the developer starts, but they do not block.

ADR-006 (the input side of the domain boundary) — which the prior in-context critic claimed to have added — **is genuinely encoded** (Step 3 point 5, Step 1 point 3, and the ADR→skill map row). That claim is substantiated. I found no other boundary-class rule left half-covered.

---

## What I verified against source (evidence)

| Plan claim | Verified against | Result |
|---|---|---|
| Plugin source repo at `<plugin-repo>`, 5 skills under `plugins/nexus-dotnet/skills/`, remote `DotNetLabX/claude-nexus` | `git -C ... remote -v`; `ls skills/` | CONFIRMED (remote `https://.../DotNetLabX/claude-nexus`) |
| `domain-service`, `central-package-management`, `framework-currency` do NOT yet exist (creates) | `ls skills/{name}` → not found | CONFIRMED |
| No local `.claude/skills/` or `SKILL.md` in sprint-rituals (plugin-canonical, Q1) | `find` → none | CONFIRMED |
| `scripts/` holds only `gen-commands.mjs` + `gen-omni.mjs` (neither writes install cache) | `ls scripts/` (+ `skill-classification.md`, see LOW-5) | CONFIRMED |
| Q3: `create-aggregate` workflow files exist (SMELL resolved) | `ls create-aggregate/workflows/` → 4 files; SKILL.md references them | CONFIRMED |
| Q3: `persistence-patterns` `ref`-param (~105) + `HasDefaultValueSql` (~80) now warnings | `persistence-patterns/SKILL.md:80,105` | CONFIRMED (exact lines) |
| SMELL: `analytics-computation-service` "Response records…all records the service returns" (~31), repo "Exception" (~73) | `analytics-computation-service/SKILL.md:31,73` | CONFIRMED |
| Gap: `domain-patterns` VO section has no "when to use VO vs scalar" rule | `domain-patterns/SKILL.md:27-49` | CONFIRMED |
| Contrast target: `extract-feature-service` is API-layer, injects concrete repos | `extract-feature-service/SKILL.md:56` "No repository interfaces — inject concrete repositories directly" | CONFIRMED |
| Reference anchors §1/§3/§6 exist; §6 "No standalone Domain Service" | `reference-patterns-domain.md` §1/§3/§6 | CONFIRMED |
| CPM reference §1 (`ManagePackageVersionsCentrally`, transitive pinning, bare `PackageReference`) | `reference-patterns-structure.md` §1 | CONFIRMED |
| Live `src/Directory.Packages.props` exists at `src/` root | Read file | CONFIRMED (matches Step 4) |
| Cache version-keyed; `/plugin update` no-op without bump; `--plugin-dir` live; `gen-*.mjs` don't touch cache; ADR-9 release flow; P2 skill-inventory limitation | plugin `docs/architecture/README.md` constraint #3, ADR-1, ADR-9, "Known limitations P2" | CONFIRMED |
| `plugin.json` version `0.1.1` (matches cited cache path) | `plugin.json:3` | CONFIRMED |
| Q4 acceptance bar (anti-pattern: "adapt"/too-simple/over-spec) is a real test | `create-implementation-plan/SKILL.md` §5 + Anti-patterns | CONFIRMED — acceptance bar (a)–(d) is verifiable as written |

---

## Coherence: Q1–Q4 answers correctly appear in the relevant steps

- **Q1 (two-repo, separate plugin source repo):** Encoded prominently — "TWO-REPO MODEL — READ FIRST" section, the per-repo commit-target table, and the explicit "`git -C <plugin-repo> status` to see the diff" instruction. The foreign diff-location is **unmistakable** for developer and reviewer. PASS.
- **Q2(a) coexist/specialization:** Step 3 point 7 + Step 1 point 1 ("specializes `domain-service`"). PASS.
- **Q2(b) no `Service`/`DomainService` suffix, callout, scoped-to-skill:** Step 3 point 2 (with explicit "don't reach for `Service`" callout and "do not present as project-wide convention"). PASS.
- **Q2(c) pure-by-default + justified perf escape hatch:** Step 3 point 3 + Step 1 point 3. PASS.
- **Q2(d) domain-types-out, endpoint owns Mapster map, no gratuitous DTO twins:** Step 3 point 4 + Step 1 point 2. PASS.
- **ADR-006 no-DTO-in (input side):** Step 3 point 5 + Step 1 point 3 + ADR→skill map. PASS.
- **Q3 scope (5 skills, exclusions):** "Already-fixed — EXCLUDED" section + Skill Mapping table. Exclusion justification independently verified. PASS.
- **Q4 acceptance bar (ADR-citation + anti-pattern/over-spec + example-compiles + dry-run):** per-skill bar (a)–(d) on every step + Steps 6–7. PASS — and verifiable.

---

## Findings

| # | Severity | Location | Issue | Recommendation |
|---|---|---|---|---|
| 1 | MEDIUM | Step 5 point 3 & point 6; per-skill bar (c) (plan line 83) | **FastEndpoints version mismatch / unstated target.** The live solution declares **FastEndpoints 8.1.0** (`src/Directory.Packages.props:18`), but Step 5 point 6's only currency anchor is "reference project uses FastEndpoints **7.x** `Send.*`," and Step 5 point 3 says "verify exact modern names against the installed FastEndpoints package … (names shifted across 6→7)" without naming which package/version the developer must check. The `Send.*` surface can shift again 7→8. A developer anchoring on the 7.x reference may encode a name that is stale for the 8.1.0 target. | In Step 5, state that the **target solution is on FastEndpoints 8.1.0** and the migration-map names must be verified against the **8.x** package API (the reference's 7.x is illustrative only). Add "8 names may differ from 7" to the version caution. |
| 2 | MEDIUM | Steps 1–5 disposition ("Follow `improve-skills`"); not addressed anywhere | **`improve-skills` housekeeping artifacts are unrouted in the two-repo model.** `improve-skills` mandates: log every create/fix to `docs/skill-backlog.md`, append a `CHANGELOG.md` entry per modified skill, and update `.claude/README.md` skill count. The plan invokes `improve-skills` but never says (a) whether these housekeeping steps apply, or (b) which repo they target. Under Q1 they belong in the **plugin repo** (`<plugin-repo>`), not sprint-rituals — but the plan is silent, so the developer may skip them or write them into the wrong repo. | Add one line to the Steps 1–5 disposition notes: the `improve-skills` backlog/CHANGELOG/README housekeeping targets the **plugin repo**, or is explicitly waived for this pass (state which). If not waived, name the plugin-repo paths. |
| 3 | MEDIUM | Step 6 point 1 (sync command) | **`--plugin-dir` points at `nexus-dotnet` only; the `nexus` core dependency may not load.** The plan's command is `claude --plugin-dir <plugin-repo>\plugins\nexus-dotnet`. Per the plugin README ADR-3, `nexus-dotnet` declares `dependencies: ["nexus"]`; the README's own dev-loop example (ADR-1) uses `--plugin-dir <repo>/plugins/nexus` (the core). In cache-bypass mode a single `--plugin-dir` at the extension may not auto-load the core dependency, which can break the discoverability dry-run that Step 6 is the gate for. | Verify the exact dev-loop invocation against ADR-1/ADR-3 (likely both plugins, or the core, must be on the `--plugin-dir` path). Record the working command in implementation.md as the Step 6 deliverable already requires. |
| 4 | LOW | Step 3 point 3 (contrast with `extract-feature-service`) | **Latent cross-skill naming inconsistency.** `extract-feature-service` lists `SprintIssueSyncService` (with `Service` suffix) as a *good* example (its line 21) right alongside `HealthScoreCalculator`. Step 3 cross-references this skill for contrast while simultaneously banning the `Service` suffix for domain services. A reader comparing the two skills sees a mixed signal. `extract-feature-service` is out of Pass 1 scope, so this is not a fix here. | When writing the Step 3 contrast, scope the no-suffix rule explicitly to *domain* services and note that API-layer operation services (`extract-feature-service`) follow their own naming — so the two don't read as contradictory. (Plan already gestures at this; make it explicit.) Optionally flag the `extract-feature-service` example to the team-lead as a future cleanup. |
| 5 | LOW | Step 6 point 1 ("Re-confirm `scripts/` contents if it changed") | **`scripts/` has a third entry the plan doesn't name.** `scripts/` also contains `skill-classification.md` (alongside `gen-commands.mjs` + `gen-omni.mjs`). It is a doc, not a build/sync script, so the plan's conclusion ("no cache-build script to run") is unaffected — but the plan's enumeration is presented as exhaustive ("`scripts/` holds only…") and is not. | Update the enumeration to "two `.mjs` scripts (`gen-commands`, `gen-omni`) plus `skill-classification.md` (a doc)" so the re-confirm step has the true baseline. |
| 6 | LOW | Step 1 SMELL description (plan line 105) | **Minor line-number drift.** Plan cites "Service class — primary constructor with repository dependencies" at "line ~34"; it is actually `analytics-computation-service/SKILL.md:32`. The `~` hedges it and the grep-based accept criterion (Step 1 "Grep the edited file for `response record` / `sealed record.*return`") does not depend on the number. Cosmetic. | No action required; optionally correct to line 32. |
| 7 | LOW | Skill Mapping table, Steps 1–5 disposition | **`improve-skills` gap-verification is blind to plugin skills.** `improve-skills`'s "verify the gap is real" step greps `.claude/skills/` — the same P2 blind spot the plan flags for Step 6. For the three *create* steps this could spuriously "confirm" a gap that's actually covered, but the architect already manually verified the gaps against the source repo (recorded in Q3), so the risk is mitigated. | None needed. Optionally note in the disposition that gap-reality was pre-verified against the source repo, so the developer doesn't re-run the blind glob and draw a false conclusion. |

---

## Completeness: do the Accept criteria prove each ADR is encoded?

| Step | ADR(s) | Accept criteria prove encoding? | Note |
|---|---|---|---|
| 3 `domain-service` | 005, 006, 010 | YES | Accept explicitly requires: pure-default + escape hatch, return-domain-types (output), no-DTO-input (006), cites 005/006/010, per-skill bar. All four boundary facets covered. |
| 1 `analytics-computation-service` | 005, 006, 010 | YES | Accept requires zero "response records" framing (grep-checkable), returns domain types, no-DTO-input (006), cross-refs `domain-service`, cites 005/006/010. The grep makes SMELL-removal objectively verifiable. |
| 2 `domain-patterns` | 007 | YES | Accept requires the new VO-vs-scalar subsection + cohesion rule + `AppSettings`→config-VO example + points to `persistence-patterns` for `ComplexProperty`. |
| 4 `central-package-management` | 013 | YES | Accept requires concept + migration recipe + verification grep + reference anchors + MSBuild-valid XML. |
| 5 `framework-currency` | 011, 012 | MOSTLY — see Finding 1 | Accept requires `Send.*` rule + bare-`SendAsync` detection + full false-positive list (HttpClient AND SignalR) + version caution + cites 011/012. Gap: the "example-compiles" facet is unverifiable without naming the 8.x target API surface. |
| 6 sync + dry-run | — | YES (verification step) | Accept records sync command, frontmatter validity, dry-run outcome. Subject to Finding 3 (command correctness). |
| 7 acceptance audit | all | YES | Per-skill audit table with six checks; cross-ref integrity check confirms every referenced skill name resolves. Good backstop. |

---

## Contradictions / unjustified dispositions checked — none material

- **"None" dispositions (Steps 6–7):** Justified inline — no authoring skill governs "run the dev loop" or "audit against the acceptance bar." Correct per `create-implementation-plan`'s None rules (these steps perform verification, not a skill-covered action). PASS.
- **Step 3 DI placement vs `service-registration`:** The plan says register the domain class in API/Persistence DI "as scoped — differs from the reference (Application layer)." `service-registration` lists domain-service factories under the Application layer but already provides the no-Application-layer fallback ("skip layer 2"). The plan's framing is consistent, not contradictory. PASS.
- **Excluded skills (`create-feature`, `cqrs-patterns`, `service-registration`):** Q3 dispositions match the triage verdicts (these were code violations / out of scope, not defective skills). PASS.

---

## Evidence table

| Item | Path |
|---|---|
| Plan under review | `docs\specs\adhoc-Pass1-SkillRepair\delivery\plan.md` |
| Resolved design answers | `docs\specs\adhoc-Pass1-SkillRepair\delivery\questions.md` |
| ADRs | `docs\architecture\decisions.md` |
| SMELL skill (input) | `<plugin-repo>\plugins\nexus-dotnet\skills\analytics-computation-service\SKILL.md` (lines 31, 73) |
| Gap skill | `<plugin-repo>\plugins\nexus-dotnet\skills\domain-patterns\SKILL.md` (lines 27–49) |
| Already-fixed (verified) | `...\skills\persistence-patterns\SKILL.md` (lines 80, 105); `...\skills\create-aggregate\workflows\` (4 files) |
| Contrast target | `...\skills\extract-feature-service\SKILL.md` (line 56; Service-suffix example line 21) |
| Authoring skill | `<plugin-repo>\plugins\nexus\skills\improve-skills\SKILL.md` |
| Acceptance-bar source | `<plugin-repo>\plugins\nexus\skills\create-implementation-plan\SKILL.md` (§5) |
| Cache/release model | `<plugin-repo>\docs\architecture\README.md` (constraint #3, ADR-1, ADR-3, ADR-9, Known limitations P2) |
| Live CPM example (FE 8.1.0) | `src\Directory.Packages.props` (line 18) |
| Reference anchors | `docs\proposals\reference-patterns-domain.md` (§1/§3/§6); `reference-patterns-structure.md` (§1) |
| Plugin version | `<plugin-repo>\plugins\nexus-dotnet\.claude-plugin\plugin.json` (0.1.1) |

---

Reviewed: docs/specs/adhoc-Pass1-SkillRepair/delivery/plan.md

---

# STEP 2 — CODE REVIEW (Cycle 1/3)

## Reviewed By
Reviewer agent — read all 5 SKILL.md bodies directly, verified C# examples against BuildingBlocks source, verified FastEndpoints Send.* against live codebase, verified CPM XML syntax.

## Verdict: REQUEST CHANGES

One HIGH finding: a factual error in `domain-patterns` that would cause a developer to write an incorrect `IDomainEvent` interface declaration. All other findings are MEDIUM or LOW.

## Pre-commitment Predictions

- Expected the analytics reframe to be clean (SMELL "response records at the top" and repo "Exception" clause removed). **Confirmed** — both gone, replaced correctly.
- Expected framework-currency false-positive list and 8.1.0 anchor to be present. **Confirmed** — both `GraphQLXrayClient.cs:296` and `SyncSprintsEndpoint.cs:46` cited with correct live file:line.
- Expected a risk of stale BuildingBlocks type descriptions in domain-patterns. **Found one** — `IDomainEvent` interface claim is wrong.
- Expected possible constructor-syntax issue in domain-service escape-hatch example. **Did not occur** — primary constructor syntax is valid .NET 10.

## Findings

### [HIGH] IDomainEvent described with wrong base interface

**File:** `plugins/nexus-dotnet/skills/domain-patterns/SKILL.md:107`

**Issue:** The Domain Events section opens with:

> `Interface: IDomainEvent : INotification, IEvent (dual MediatR + FastEndpoints)`

The actual source `Blocks.Domain/Events/IDomainEvent.cs` declares:

```csharp
public interface IDomainEvent : IEvent;
```

There is no `INotification` — that is a MediatR interface and it does not appear anywhere in BuildingBlocks. `INotification` is not referenced in any BuildingBlocks `.cs` file. A developer reading this skill who creates a new domain event or a new module would write `IDomainEvent : INotification, IEvent`, which would fail to compile unless they also add a MediatR package dependency. The claim is factually wrong against this solution.

**Fix:** Change line 107 to:

> `Interface: IDomainEvent : IEvent` (FastEndpoints in-process dispatch — implements `IEventHandler<T>`)

Add a variant note if a full-stack MediatR project also needs `INotification`, but make clear that in this solution `IDomainEvent` only extends `IEvent`.

**Confidence:** HIGH — verified directly against `Blocks.Domain/Events/IDomainEvent.cs`.

---

### [MEDIUM] domain-service escape-hatch: `_history` field name in primary constructor

**File:** `plugins/nexus-dotnet/skills/domain-service/SKILL.md:82`

**Issue:** The primary constructor example writes:

```csharp
public class VelocityProjector(IClosedSprintHistory _history)
```

In C# 12 primary constructors, the parameter name becomes the field — using an underscore-prefixed name as a primary constructor parameter is unconventional. The C# compiler will treat `_history` as a parameter name (not a backing field with an underscore prefix), and `this._history` is not accessible; you reference it as `_history` directly. This compiles and works, but it is unusual style: primary constructor parameters normally don't have the `_` prefix because they are not fields. A developer may copy this and expect `_history` to behave like a field declaration, which it does not (it is captured by value in the closure, not a class field). The more idiomatic form is either a named parameter (`history`) or a classic constructor + field.

**Fix:** Use either the idiomatic primary constructor form:

```csharp
public class VelocityProjector(IClosedSprintHistory history)
{
    public async Task<VelocityProjection> ProjectAsync(int boardId, int count, CancellationToken ct)
    {
        var result = await history.LoadForBoard(boardId, count, ct);
        // pure derivation
    }
}
```

or a classic constructor with an underscore-prefixed private field if the style guide prefers that. Either is unambiguous. The current form compiles but could mislead.

**Confidence:** MEDIUM — compiles correctly in .NET 10; the concern is style clarity for a developer reading the example.

---

### [MEDIUM] analytics-computation-service: SparklinePoint used without definition anchor

**File:** `plugins/nexus-dotnet/skills/analytics-computation-service/SKILL.md:76`

**Issue:** `BugRatioResult` is declared with `SparklinePoint[] Sparkline` and the Sparkline Builder section references `SparklinePoint` records, but there is no anchor explaining where `SparklinePoint` is defined or what its shape is. A developer implementing a new analytics feature will need to create this type; as written, they must guess whether it is a BuildingBlocks type, a domain-layer record, or an API-layer DTO.

**Fix:** Add one sentence anchoring `SparklinePoint` — e.g., "Define `SparklinePoint` as a domain-layer `sealed record` in `{Module}.Domain/Analytics/` alongside the result records — it is a project-specific type, not from BuildingBlocks." This removes the ambiguity without over-specifying the shape.

**Confidence:** HIGH — `SparklinePoint` does not exist in BuildingBlocks (confirmed via Glob); the type origin is undefined in the skill.

---

### [MEDIUM] domain-patterns: AggregateRoot described as having audit fields but source uses `string?` not `int?` for CreatedById

**File:** `plugins/nexus-dotnet/skills/domain-patterns/SKILL.md:12-16`

**Issue:** The skill lists the audit fields correctly (`CreatedById`, `CreatedOn`, `LastModifiedById`, `LastModifiedOn`) and says `CreatedById` — the source confirms these names. However the skill omits their types entirely. A developer who needs to set `CreatedById` from a `Person.Id` (which is `int`) will face a type mismatch: the source declares `public string? CreatedById { get; set; }`. This is a potential confusion point for identity-related code.

**Fix:** Add the types inline: `CreatedById (string?)`, `LastModifiedById (string?)`, `CreatedOn (DateTime?)`, `LastModifiedOn (DateTime?)`. This is a brief addition that prevents the `int` vs `string?` confusion.

**Confidence:** MEDIUM — the type mismatch is real and verified against source; whether it causes an actual bug depends on usage.

---

### [LOW] domain-service: "scoped to domain services" naming caveat repeated twice

**File:** `plugins/nexus-dotnet/skills/domain-service/SKILL.md:40-49`

**Issue:** The naming section states "This naming rule is scoped to domain-layer services" at line 49 after the table, and the same point is also clear from the header "Naming Rule (scoped to domain services)". The repetition is minor but slightly clutters the section.

**Fix:** Remove the repeated trailing sentence at line 49; the header already scopes it.

**Confidence:** LOW — cosmetic only.

---

### [LOW] central-package-management: grep commands use shell glob `src/**/*.csproj` which may not work in all shells

**File:** `plugins/nexus-dotnet/skills/central-package-management/SKILL.md:83-103`

**Issue:** The three verification greps use `src/**/*.csproj` as the path argument. In bash with `globstar` disabled (the default in many CI environments and on Windows shells), `**` does not recurse — the grep would silently find nothing even if violations exist. On PowerShell, `**` glob expansion in grep (rg/grep) behaves differently. The skill presents these as authoritative verification commands without noting the shell dependency.

**Fix:** Use `rg` (ripgrep) with `--glob` instead, or note that `**` requires `globstar` enabled (`shopt -s globstar`) in bash. Alternatively use `-r src/ --include="*.csproj"` form which works portably:

```
grep -rn 'Version="' --include="*.csproj" src/
```

**Confidence:** MEDIUM on the shell-portability concern; rated LOW because the target environment (Windows + PowerShell or bash-via-tool) is not explicitly stated, and the reader may understand the intent.

---

### [LOW] framework-currency: migration table lists SendAsync→ResponseAsync but does not call out the status-code parameter difference

**File:** `plugins/nexus-dotnet/skills/framework-currency/SKILL.md:47`

**Issue:** The table maps `await SendAsync(body, statusCode, ct)` → `await Send.ResponseAsync(body, statusCode, ct)`. This is correct for three-argument overloads, but the legacy `SendAsync` also had a two-argument form (`SendAsync(body, ct)`) that maps differently. A developer who finds a two-arg legacy call and tries to apply the table mechanically may pick the wrong modern method. This edge case is not documented.

**Fix:** Add a footnote: "Two-argument `SendAsync(body, ct)` (no status code) maps to `Send.OkAsync(body, ct)` — the three-argument form above is the generic status-code variant."

**Confidence:** LOW — the three-arg form covers the stated scenario; the two-arg form is a minor gap.

## Positive Observations

- The de-conflation of `domain-service` (Domain-layer only) vs `ArticleAccessChecker` (Application-layer contrast) is clean throughout. The hard guarantee at `domain-service/SKILL.md:182` ("No sentence in this skill implies a domain service can live in or depend on an Application layer...") is an excellent safeguard against misreading.
- The analytics-computation-service SMELL is genuinely gone. "Response records at the top of the file — all records the service returns" (old line 31) and the repository "Exception" clause (old line 73) are both removed and replaced with the correct domain-layer pattern. The reframe is complete, not partial.
- The `domain-service` escape-hatch section is clear and well-bounded: the forbidden list (`IRepository<T>`, `IQueryable<T>`, `DbContext`) is explicit, and the justification-comment requirement at the call site is a good guardrail.
- The `framework-currency` two-stage detection (Stage 1 unqualified grep → Stage 2 manual receiver inspection) is correct and the false-positive table with live `file:line` citations is exactly what a developer needs to avoid over-migrating.
- The D-A mandate (SprintRituals types appear only as labelled worked examples) is correctly applied in all five skills — every SprintRituals-specific reference is explicitly labelled.
- The D-B rule (endpoint always owns the response DTO + Mapster map, even 1:1) is consistently applied across `domain-service`, `analytics-computation-service`, and `domain-patterns`. No remnant of the old "reuse domain type when shapes match" guidance survives.
- The `domain-patterns` variant-annotation of `IDomainEventPublisher` and State Machine sections is additive and non-destructive — full-stack content retained, light-stack guidance added cleanly.

## Gaps

- `SparklinePoint` shape/location — addressed in MEDIUM finding above.
- No test examples are included in any skill. This is expected for SKILL.md files (they are guidance, not test harnesses), but future passes may want to add an "Example test" snippet to `domain-service` to reinforce the "pure, unit-testable" claim.

## Open Questions

- None — all CRITICAL/HIGH findings are confirmed with direct source evidence.

## Evidence

| Check | Result | File / Command | Notes |
|-------|--------|----------------|-------|
| `IDomainEvent` interface | `IDomainEvent : IEvent` only | `Blocks.Domain/Events/IDomainEvent.cs:9` | No `INotification`; MediatR not in BuildingBlocks |
| `INotification` in BuildingBlocks | 0 matches | `Grep INotification src/BuildingBlocks` | Confirms `INotification` is absent |
| `AggregateRoot` audit fields | `CreatedById string?`, `CreatedOn DateTime?` etc. | `Blocks.Domain/Entities/AggregateRoot.cs:11-14` | Field names correct; types `string?` not `int?` |
| `StringValueObject` Value setter | `protected set` | `Blocks.Domain/ValueObjects/StringValueObject.cs:5` | EmailAddress example constructor `Value = value` compiles |
| `Guard.ThrowIfNullOrWhiteSpace` | Exists | `Blocks.Core/Guard.cs` | EmailAddress factory example compiles |
| `ValueObject.GetEqualityComponents` | Abstract method | `Blocks.Domain/ValueObjects/ValueObject.cs:5` | Pattern correct |
| FastEndpoints Send API | `Send.OkAsync`, `Send.NoContentAsync`, `Send.ResponseAsync` | Live grep on `src/` | All three forms present in codebase; migration table accurate |
| analytics SMELL removal | 0 hits for "response record", "Service.cs" in analytics skill | Done-check greps confirmed | SMELL fully removed |
| `SparklinePoint` in BuildingBlocks | Not found | `Glob **/*.cs Blocks.Domain` | Project-specific type; no anchor in skill |
| CPM XML structure | Valid MSBuild | `central-package-management/SKILL.md:28-39` | `ManagePackageVersionsCentrally`, `CentralPackageTransitivePinningEnabled` are correct property names |
| Build | Not applicable | Skill files are Markdown | No build step for SKILL.md artifacts |

---

## STEP 2 — RE-REVIEW (Cycle 1/3)

**Reviewed by:** Reviewer agent — re-read all 5 SKILL.md bodies, verified every RV finding against the updated text, checked adjacent areas around each fix for regressions.

## Verdict: APPROVED

All seven RV findings are resolved. No regressions introduced by the fixes. No new CRITICAL or HIGH issues found.

## Per-finding resolution table

| # | Original severity | Finding | Status | Evidence |
|---|-------------------|---------|--------|----------|
| RV-1 | HIGH | `IDomainEvent : INotification, IEvent` — wrong interface | **RESOLVED** | `domain-patterns/SKILL.md:102` now shows `public interface IDomainEvent : IEvent;` verbatim. Line 105 adds an explicit "no `INotification`" guard. Full-stack variant correctly boxed in a `> NOT in this solution` callout (lines 116–124). |
| RV-2 | MEDIUM | Primary constructor `_history` underscore-prefixed parameter | **RESOLVED** | `domain-service/SKILL.md:87` now uses `VelocityProjector(IClosedSprintHistory history)` — un-prefixed, idiomatic C# 12. Comment on line 86 explains the convention. |
| RV-3 | MEDIUM | `SparklinePoint` used without definition anchor | **RESOLVED** | `analytics-computation-service/SKILL.md:74-77` now defines `SparklinePoint` inline as a `sealed record` with an explicit comment: "define as a domain-layer sealed record in `{Module}.Domain/Analytics/` … project-specific type, not from BuildingBlocks." |
| RV-4 | MEDIUM | Audit field types omitted (`CreatedById` is `string?` not `int?`) | **RESOLVED** | `domain-patterns/SKILL.md:16` now lists `CreatedById (string?)`, `CreatedOn (DateTime?)`, `LastModifiedById (string?)`, `LastModifiedOn (DateTime?)` inline with types. |
| RV-5 | LOW | Naming-rule scope caveat repeated twice | **RESOLVED** | `domain-service/SKILL.md:40-51`: header is now plain "Naming Rule" (no parenthetical), trailing repetition gone. One clean sentence at line 51 scopes the rule to domain-layer services and contrasts with `extract-feature-service`. |
| RV-6 | LOW | CPM grep used `src/**/*.csproj` shell glob (portability) | **RESOLVED** | `central-package-management/SKILL.md:84-111` now uses `grep -rn --include="*.csproj"` for all three forms. Adds a note that `**` with bare `grep` is off by default, and provides an `rg --glob` alternative on line 109–111. |
| RV-7 | LOW | Two-arg `SendAsync(body, ct)` overload undocumented | **RESOLVED** | `framework-currency/SKILL.md:55-57` now has a "Bare overloads to include in migration" block covering both the three-arg and two-arg forms. Two-arg explicitly maps to `Send.OkAsync(body, ct)`. Also appears in Stage 1 candidate list (line 81) and Stage 2 receiver table (line 93). |

## Adjacent-area regression check

- **`domain-patterns` Domain Events section:** The full-stack `DomainEvent<TAction>` variant block correctly states that MediatR projects add `INotification` to `IDomainEvent` in *their own* definition — it does not claim BuildingBlocks's `IDomainEvent` has it. The fix to RV-1 introduced no self-contradiction. The `IDomainEventPublisher` section (lines 143–168) is consistent: light-stack (`Blocks.FastEndpoints`) is the primary path; MediatR variant is clearly boxed as "NOT in this solution."
- **`domain-service` endpoint example (lines 134–146):** The `_sprintRepo`, `_scorer`, `_settings` field references are legitimately injected fields not shown in the snippet — the comment "Live type names from Fokus.API/Features/Analytics/GetBugRatio/" makes this explicit and the snippet is labelled "Target-state … won't build until Pass 2/3." No issue.
- **`domain-patterns` VO-vs-scalar example:** Revised from the hypothetical `BugRatioConfig`/`XrayConfig` before/after table to a live table of actual `HealthThresholdConfig`, `HealthWeightConfig`, etc. VOs already in `Fokus.Domain/Settings/ValueObjects/`. The change is an improvement in D-A compliance (live examples over invented ones) and introduces no new errors.
- **`domain-service` naming section:** The "(scoped to domain services)" parenthetical is removed from the heading; one scoping sentence at the bottom of the section contrasts cleanly with `extract-feature-service`. No information lost.
- **`framework-currency` Stage 1 grep:** Adding the two-arg bare overload to the candidate list and Stage 2 table is consistent with the overall two-stage approach. The false-positive section is unchanged and correct.
- **`central-package-management` SprintRituals note (line 116):** Still contains `src/**/*.csproj` in a descriptive note ("As of Pass 0, all three forms return zero hits across `src/**/*.csproj`"). This is a prose description of the repo state, not a command the reader runs — the actual grep commands above it all use `--include="*.csproj"`. Not an issue.

## Positive observations

- The `domain-patterns` Domain Events rewrite is substantially better than the original: the before/fix showed the correct interface; the re-review shows it now anchors directly on the source file and provides a clear negative statement ("no `INotification` in this solution's BuildingBlocks"). The full-stack variant is well-boxed.
- The `SparklinePoint` anchor in `analytics-computation-service` is exactly the right shape: one-line `sealed record` example with a comment explaining it is project-specific and where to put it. No over-specification.
- The VO-vs-scalar worked example in `domain-patterns` is now grounded in live types that already exist in the repo rather than hypothetical ones. This is a meaningful improvement in D-A compliance.
- The `framework-currency` two-arg overload fix is thorough: it appears in three places (bare-overload callout, Stage 1 candidate list, Stage 2 table) so a developer cannot miss it.

## Evidence

| Check | Result | Source |
|-------|--------|--------|
| `IDomainEvent` declaration | `IDomainEvent : IEvent` only — confirmed | `domain-patterns/SKILL.md:102` |
| `INotification` in full-stack callout | Correctly boxed as "NOT in this solution" | `domain-patterns/SKILL.md:116-124` |
| Primary constructor parameter name | `history` (un-prefixed) | `domain-service/SKILL.md:87` |
| `SparklinePoint` anchor | `sealed record SparklinePoint(int PeriodId, decimal Value)` with placement comment | `analytics-computation-service/SKILL.md:74-77` |
| Audit field types | `CreatedById (string?)`, `CreatedOn (DateTime?)` etc. inline | `domain-patterns/SKILL.md:16` |
| Naming section — no repeated caveat | Single scoping sentence at line 51; heading is clean | `domain-service/SKILL.md:40-51` |
| CPM grep commands | All use `--include="*.csproj"` or `--glob '*.csproj'` | `central-package-management/SKILL.md:88-111` |
| Two-arg bare overload | Documented in migration map, Stage 1, and Stage 2 table | `framework-currency/SKILL.md:55-57, 81, 93` |
| Build | Not applicable — Markdown skill files | N/A |
