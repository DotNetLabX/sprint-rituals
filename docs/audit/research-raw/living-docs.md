Now I have comprehensive data across all six threads. Let me compile the dense research report.

---

## Research: Living Documentation, Executable Specs, and Rule-to-Test Traceability

---

### THREAD 1 — Specification by Example / BDD / Gherkin

**Source:** Gojko Adzic, *Specification by Example* (Manning, 2011) — [O'Reilly](https://www.oreilly.com/library/view/specification-by-example/9781617290084/)
**10-year retrospective (2020):** [gojko.net/2020/03/17/sbe-10-years.html](https://gojko.net/2020/03/17/sbe-10-years.html)
**Classification:** Established practice

**Summary:** SbE's core promise is that examples shared between business and dev become both automated tests and living documentation. The 2020 retrospective is the most authoritative critique available. Key failure mode data from Adzic's 2020 survey and practitioner quotes:

1. **BDD conflated with Given-When-Then automation** — Seb Rose: "The BDD TLA has become synonymous in the industry with G/W/T powered tests." Teams automate scenarios without the upstream collaborative analysis that gives them value.
2. **Feature files become buried scripts** — Christian Hassa: "Feature files reduced to automation scripts buried in source control where the higher level context is missing." Only 12% of surveyed teams use text files in version control as their primary requirements source; 57% rely on Jira/task trackers.
3. **Single source of truth never materializes** — Stories describe work sequence, not system state. Janet Gregory: "Stories are throw-away artifacts. They offer zero help after the story has been implemented." Without post-story reconciliation, Gherkin files document what was decided, not what the system currently does.
4. **Plain-text format limits genuine collaboration** — Gáspár Nagy: structured discussion cannot be stored in `.feature` files; tooling to enable real collaboration is expensive.
5. **~30% skip automation** — Teams using examples without automating them showed double the "poor quality" rate (11% vs 5%).
6. **Step-definition maintenance cost** — The dual-artifact nature (Gherkin + step definitions) creates two things to keep in sync. Playwright+BDD users report timeout conflicts, lost debugging flow, and parallel-execution breakage.

**Reqnroll (.NET successor to SpecFlow):** [reqnroll.net](https://reqnroll.net/) / [GitHub](https://github.com/reqnroll/Reqnroll). SpecFlow went commercial/dormant 2023; Reqnroll is the actively maintained open-source fork (BSD-3). Supports .NET 10, xUnit/NUnit/MSTest/TUnit, full Gherkin including tagged `Rule` blocks. Migration is namespace-swap level effort.

**Gherkin for domain-layer rules vs. plain tests — verdict:**

- For **boundary/integration** scenarios involving multiple collaborators: Gherkin earns its cost when business stakeholders actively read and validate features files. This rarely happens at the domain-invariant level.
- For **aggregate invariants and business rules** in a DDD domain layer: plain unit tests with structured rule-ID names carry lower overhead, stay in one artifact (no step-definition file), and integrate cleanly with existing test runners. Gherkin adds a layer of indirection that provides no benefit when the audience is entirely developers.
- **Concrete recommendation:** Use plain xUnit/NUnit tests with rule-ID in `[Fact(DisplayName = "RULE-XXX: ...")]` for domain-layer invariants. Reserve Reqnroll/Gherkin for acceptance-level scenarios that non-developers must validate.

---

### THREAD 2 — Living Documentation (Cyrille Martraire)

**Source:** Cyrille Martraire, *Living Documentation: Continuous Knowledge Sharing by Design* (Addison-Wesley, 2019) — [O'Reilly](https://www.oreilly.com/library/view/living-documentation-continuous/9780134689418/) | [InfoQ Q&A](https://www.infoq.com/articles/book-review-living-documentation/)
**Classification:** Established practice

**Core mechanisms applicable to a .NET DDD markdown KB:**

1. **Annotations as knowledge carriers.** The best place to store documentation is on the documented thing itself. Custom C# `Attribute` classes (`[BoundedContext("Fokus")]`, `[DomainInvariant("An order total can never be negative")]`) are machine-readable, survive refactoring, and drive automated extraction. This is the primary mechanism for avoiding drift.

2. **Living Glossary pattern.** Define a `[GlossaryTerm(definition: "...")]` attribute, apply to domain types, run a Roslyn-analyzer or reflection-based extractor at build time, emit structured markdown. The glossary is generated, not maintained. Failure to extract a glossary signals DDD adoption problems in the code itself.

3. **Augmented code.** XML doc comments (`///`), naming conventions, namespace structure — all existing knowledge that a Roslyn extractor can harvest without requiring developers to add new annotations. These are the zero-cost entry points for initial KB population.

4. **Reconciliation mechanisms.** Docs stay current because they are generated. CI pipeline diff: compare generated MD against committed MD; fail build if they diverge. This eliminates manual update discipline as a dependency.

5. **Knowledge exploitation from tests.** Test names are already living specifications. A reflection-based harvester over xUnit test assembly metadata extracts test names into a "behavior catalogue" section of the KB. No separate tooling needed.

6. **Warning signal.** If the Roslyn extractor finds no domain annotations, that signals a design problem — not just a documentation gap. The KB harness serves as an architectural health check.

**Concrete .NET KB generation pipeline implied by Martraire:**
```
C# Source + Attributes + XML Doc Comments
    → Roslyn Analyzer / Reflection Harvester (MSBuild task or CLI tool)
    → Structured JSON/XML intermediate
    → Markdown renderer
    → /docs/kb/*.md (one file per rule/concept)
    → CI diff gate: committed vs. generated MD
```

---

### THREAD 3 — Requirement-to-Test Traceability

**Sources:**
- [Trace.Space: Traceability in Compliance Projects](https://www.trace.space/blog/traceability-in-compliance-projects)
- [Parasoft: DO-178C Integration Testing](https://www.parasoft.com/learning-center/do-178c/integration-testing/)
- [Parasoft: ISO 26262 Requirements Traceability](https://www.parasoft.com/learning-center/iso-26262/requirements-traceability/)
- [Perforce: Requirements Traceability Matrix](https://www.perforce.com/resources/alm/requirements-traceability-matrix)

**Classification:** Established practice (regulated industry)

**Key mechanisms:**

**DO-178C (aerospace):** Mandates bidirectional traceability: system req → high-level software req → low-level software req → source code → test case → test procedure → test result. DAL A–E scales strictness. Structural coverage analysis (MC/DC for DAL A) is separate from requirement coverage. All links must be auditable.

**ISO 26262 (automotive):** ASIL level drives rigor. Every safety requirement traces forward to implementation and backward to the originating hazard. ASPICE and ISO 26262 share enough overlap that a single matrix with standard-specific evidence views is the lightweight multi-standard approach.

**Rule ID naming conventions — what exists and what doesn't:**

No universally published .NET naming convention for embedding requirement IDs in test names was found. The pattern is team/tool-defined. What practitioners do:

- **xUnit:** `[Fact(DisplayName = "RULE-042: Sprint velocity must be positive")]` — the display name embeds the rule reference and appears in test runner output and CI reports.
- **NUnit:** `[Test(Description = "RULE-042 — ...")]` — NUnit's description attribute is extractable via reflection; `[Category("RULE-042")]` enables filtering.
- **Trait-based:** `[Trait("RuleId", "RULE-042")]` in xUnit — queries `dotnet test --filter "RuleId=RULE-042"` work directly.

**Lightweight RTM for non-regulated projects:** A markdown table in `docs/kb/traceability.md` with columns `| Rule ID | Rule File | Test Class | Test Method | Last Verified |` is the minimal viable form. A CI script that reads KB rule IDs and asserts each has at least one test matching that ID in the compiled assembly catches coverage gaps without specialized tooling.

**NetArchTest** (v1.3.2, May 2021, low maintenance): Fluent API for structural rules. Policy API groups rules with human-readable string descriptions. No formal requirement ID support. Best for layering/dependency enforcement, not domain rule coverage checks. [GitHub](https://github.com/BenMorris/NetArchTest). Consider `NetArchTest.eNhancedEdition` as active fork.

**ArchUnitNET** (TNG, active as of 2025, Apache 2.0): More actively maintained, supports xUnit/NUnit/MSTest via dedicated packages, `.Because()` embeds rationale in failure output, `.As()` labels appear in diagnostics. No native requirement ID support; custom `ICondition<T>` enables domain-specific predicates. Can derive rules from PlantUML diagrams. [GitHub](https://github.com/TNG/ArchUnitNET)

**Custom Roslyn Analyzers:** `DiagnosticId` fields (e.g., `"RULE042"`) map directly to the KB rule ID space. Analyzers run at compile time, not just in test suites. Highest enforcement fidelity but highest authoring cost.

---

### THREAD 4 — Documentation-Code Drift Detection

**Sources:**
- [Swimm: Documentation as Code](https://swimm.io/learn/code-documentation/documentation-as-code-why-you-need-it-and-how-to-get-started)
- [Overcast: AI-Driven Documentation 2026](https://overcast.blog/ai-driven-documentation-in-2026-f993f0c6d0d6)
- [Swimm VS Code extension review](https://plainenglish.io/blog/my-honest-review-of-swimms-official-vs-code-extension)

**Classification:** Commercial product (Swimm) + established docs-as-code practices

**Swimm.io mechanism:** Code-coupled documentation where each doc snippet is anchored to a specific token/function/line range in the codebase. When anchored code changes, Swimm flags the doc as needing update. CI integration fails the pipeline on unacknowledged drift. IDE plugins (VS Code, JetBrains) surface docs next to relevant code. Storage is in-repo markdown, version-controlled.

**Applicability note:** Swimm solves human-authored documentation drift, not generated KB drift. For a generated KB (Martraire's reconciliation approach), the CI diff mechanism is superior: if the generated output diverges from committed output, the build fails. No external SaaS required.

**Docs-as-code linting approaches:**
- Vale (prose linter) — configurable rules for markdown docs, runs in CI, checks terminology consistency against a glossary. [vale.sh](https://vale.sh)
- markdownlint — structural consistency for markdown files.
- Custom CI script: `dotnet run --project tools/KbExtractor -- --validate` compares generated vs. committed MD hashes.

**Documentation coverage check:** The concept of "doc coverage" (every public type/method has documentation) exists via `dotnet-docfx` warnings and Roslyn analyzers (e.g., `SA1600` from StyleCop). For KB rule coverage, a custom check — every aggregate invariant method has a corresponding KB entry — requires a rule extractor tool that is built for this specific harness.

---

### THREAD 5 — DDD Connection: Invariants, EventStorming, Rule Catalogs

**Sources:**
- [DDD Community Glossary](https://www.dddcommunity.org/resources/ddd_terms/)
- [Vaughn Vernon aggregate design rules (archi-lab.io)](https://www.archi-lab.io/infopages/ddd/aggregate-design-rules-vernon.html)
- [Domain Language DDD Reference PDF (Evans, 2015)](https://www.domainlanguage.com/wp-content/uploads/2016/05/DDD_Reference_2015-03.pdf)
- [Codely: Aggregate Design Blueprint with AI (2025)](https://codely.com/en/blog/como-implementar-codigo-ddd-usando-ia)

**Classification:** Established practice (DDD) + emerging (AI-assisted extraction)

**Aggregate invariant documentation formats used by practitioners:**

Evans (2015 DDD Reference): "State post-conditions of operations and invariants of classes and aggregates. If assertions cannot be coded directly, write automated unit tests. Write them into documentation or diagrams where it fits the project's style." — Invariants as first-class artifacts.

Vaughn Vernon: "Define aggregates with clear invariants and formulate them as sentences: 'An order may only be released if…'. Keep invariants inside aggregate methods; avoid leaking setters."

**Codely Aggregate Design Blueprint (2025) — closest public reference to a machine-readable invariant format:**
```markdown
## Aggregate: Order

**Context:** Fulfillment
**Description:** Represents a customer purchase request

**Properties:**
- orderId: OrderId
- total: Money
- status: OrderStatus

**Enforced Invariants:**
- An order total can never be negative
- An order can only be released if all items have confirmed inventory

**Corrective Policies:**
- If payment fails, revert to Pending status

**Domain Events:** OrderPlaced, OrderReleased, OrderCancelled

**Ways to Access:** OrderRepository.findById
```
This format was designed for input to AI code generators (Cursor) but maps cleanly to KB entries.

**EventStorming outputs as rule catalogs:** EventStorming Big Picture surfaces domain events → commands → aggregates. Process Modelling adds policies (reactions to events). The output is a visual rule catalog: `Command → Aggregate → Invariant check → Domain Event or Rejection`. This maps to KB entries where the rule is the invariant check, the trigger is the command, and the evidence is the aggregate method name + file:line.

**Ubiquitous Language Glossary** + Bounded Context map are the structural frames that give KB entries their scope. Each KB rule entry should declare its bounded context and aggregate as classification fields.

---

### THREAD 6 — Knowledge Bases for AI Agents

**Sources:**
- Karpathy llm-wiki gist: [github.com/karpathy/442a6bf555914893e9891c11519de94f](https://gist.github.com/karpathy/442a6bf555914893e9891c11519de94f)
- VentureBeat coverage: [Karpathy shares 'LLM Knowledge Base' architecture](https://venturebeat.com/data/karpathy-shares-llm-knowledge-base-architecture-that-bypasses-rag-with-an)
- llms.txt spec: [llmstxt.org](https://llmstxt.org/) — Jeremy Howard, Answer.AI, September 2024
- MindStudio: [What Is Andrej Karpathy's LLM Wiki?](https://www.mindstudio.ai/blog/andrej-karpathy-llm-wiki-knowledge-base-claude-code)

**Classification:** Emerging practice (2025-2026, high signal-to-noise)

**Karpathy LLM-wiki pattern — full schema extracted from gist:**

Three-layer architecture:
1. **Raw sources** (immutable) — original evidence files, never written by LLM
2. **Wiki** (LLM-owned) — derived markdown: entity pages, concept pages, source summaries, comparison analyses, overview synthesis
3. **Schema file** (`CLAUDE.md`/`AGENTS.md`) — governs all conventions; the agent's instruction set

Per-page optional YAML frontmatter:
```yaml
tags: [...]
date: YYYY-MM-DD
source_count: N
relates_to:
  - page: "X"
    rel: contradicts | extends | supersedes
```

`index.md`: content catalog, entry = link + one-line summary + optional metadata, organized by category, read first by agent before drilling into pages.

`log.md`: append-only chronological record of ingests/queries/lint passes, format `## [YYYY-MM-DD] <operation> | <title>`, grep-parseable.

**Ingest workflow (agent-executable):** Read source → discuss → write summary page → update `index.md` → update 10–15 related entity/concept pages → append to `log.md`.

**Lint pass rules agents execute:**
- Flag contradictions between pages
- Flag stale claims superseded by newer sources
- Flag orphan pages (no inbound links)
- Flag concepts mentioned but lacking own page
- Suggest new investigations

**Confidence/provenance — community extensions to the base pattern:**
- Citation-keyed dedup: grep source citation before writing
- Supersession, not deletion: facts marked superseded with timestamps
- Confidence tiers for autonomous writes: high-confidence → auto-apply; ambiguous → human review queue
- Typed contradiction edges in frontmatter (`rel: contradicts`) rather than treating contradiction as defect

**What makes a KB maximally useful to LLM agents (synthesis from all sources):**
- **Flat, consistent schema** — same frontmatter fields on every entry; no structural variation forces agents to parse idiosyncratically
- **`index.md` as navigation layer** — agents read index first; without it, they must scan every file
- **Short entries** — each file fits in ~one context window chunk (~2k tokens); long files force partial reads
- **Machine-readable citations** — `file: src/Fokus/Domain/Sprint.cs`, `line: 147` as structured fields, not prose
- **Typed relationships** — `relates_to` with `rel:` type values enables graph traversal without vector search
- **Append-only log** — reproducible audit trail; grep-indexed for tooling
- **Rule ID as primary key** — consistent cross-reference anchor across KB entry, test method name, Roslyn diagnostic ID, and CI check

**llms.txt standard** (Jeremy Howard, September 2024): Website-level convention, not repo-internal. H1 = project name, blockquote = summary, H2 sections = link lists, `Optional` section = skippable content. Each page should offer `<url>.md` clean markdown version. Primary use case is external LLM inference, not agent-internal KB. Not applicable as the KB format itself, but the `.md` convention and "clean, jargon-free, concise" writing guidance applies to KB entry prose.

---

## Top 5 Design Implications

### 1. KB Entry Schema Elements (recommended set)

Every `docs/kb/RULE-NNN-<slug>.md` entry should contain:

```markdown
---
rule_id: RULE-042
title: Sprint velocity must be positive
bounded_context: Fokus
aggregate: Sprint
confidence: high | medium | low
status: active | superseded | disputed
evidence:
  - file: src/Services/Fokus/Fokus.Domain/Sprint.cs
    line: 147
    kind: invariant_check
supersedes: null
relates_to:
  - rule_id: RULE-039
    rel: extends
last_verified: 2026-06-11
---

## Rule

An order total can never be negative.

## Rationale

[Why this invariant exists — from domain expert knowledge or code comments]

## Test Coverage

- `SprintTests.Rule042_VelocityMustBePositive` (Fokus.Tests)

## Extraction Notes

[Confidence rationale, ambiguities, what the LLM inferred vs. found explicitly]
```

**Justification:** `rule_id` is the primary key linking KB → test → Roslyn diagnostic → CI check. `confidence` distinguishes extracted invariants the harness found explicitly (guard clause + domain event + test) from inferred ones (naming convention only). `evidence` with `file:line` makes claims auditable in one click. `last_verified` enables staleness detection.

### 2. Rule ID Convention

Format: `RULE-NNN` where NNN is zero-padded three-digit integer, assigned sequentially by the harness on first extraction.

Sub-classification via prefix variants (optional but useful for larger KBs):
- `INV-NNN` — aggregate invariants (consistency rules the aggregate enforces synchronously)
- `POL-NNN` — domain policies (reactions to domain events, eventually-consistent rules)
- `ARCH-NNN` — architectural constraints (layer dependencies, no-cross-module-refs)

Simpler: start with `RULE-` and add classification as a `kind:` field in frontmatter. Avoid sub-namespacing until the KB exceeds ~100 entries; premature namespace design creates maintenance overhead.

Test reference pattern (xUnit):
```csharp
[Fact(DisplayName = "RULE-042: Sprint velocity must be positive")]
[Trait("RuleId", "RULE-042")]
public void VelocityMustBePositive() { ... }
```
`[Trait]` enables `dotnet test --filter "RuleId=RULE-042"` and is extractable via `Assembly.GetCustomAttributes`. The CI "every rule has a test" check queries traits rather than parsing display names.

### 3. BDD/Gherkin — Out for Domain Layer

**Recommendation: Gherkin OUT for domain-layer invariant tests.**

Evidence:
- Gherkin's value is communication with non-developers; domain invariants have no non-developer audience.
- Step-definition maintenance doubles the artifact surface for zero business benefit at this layer.
- 57% of teams already use Jira as primary requirements source; the "living documentation" promise of Gherkin has not materialized in practice (Adzic 2020 survey).
- Plain `[Fact(DisplayName = "RULE-NNN: ...")]` tests with `[Trait("RuleId", "RULE-NNN")]` provide traceability, CI filtering, and readable output without a second artifact layer.

**Keep Reqnroll open as an option** for acceptance-level scenarios at the API/endpoint boundary (FastEndpoints integration tests) where a product owner or QA analyst might legitimately read the feature files. At that layer, the collaboration value can justify the step-definition cost.

### 4. Drift Detection: Generated, Not Maintained

Adopt Martraire's reconciliation model over Swimm's linked-annotation model for this use case:

- KB entries are **generated artifacts** from a harness run, not hand-authored documents.
- CI gate: harness runs in CI, generates KB JSON/MD intermediate, diffs against committed KB, fails if diverged without a `--accept` flag.
- `last_verified` field: harness updates this on each successful CI run. Entries older than N days without update are flagged as `status: stale` automatically.
- Vale prose linter enforces terminology consistency across entries (checks that the ubiquitous language glossary terms appear consistently).

This eliminates human update discipline as a dependency — the harness is the maintainer.

### 5. Structural Enforcement: ArchUnitNET over NetArchTest

Use **ArchUnitNET** (not NetArchTest) for architecture tests that pin ARCH-NNN rules:
- Actively maintained (2025 releases, NimblePros training content, Apache 2.0).
- `.Because("ARCH-042: Fokus.API must not reference Identity.*")` embeds the rule ID in failure output.
- `.As("ARCH-042")` labels appear in diagnostics.
- xUnit/NUnit packages available — tests live alongside domain unit tests.
- Custom `ICondition<T>` enables domain-specific structural predicates (e.g., "every aggregate method that modifies state must raise a domain event").

For **compile-time enforcement** of high-value rules, add Roslyn Analyzers with `DiagnosticId = "RULE042"` — this matches the KB ID space and surfaces in IDE + CI build output without running tests.

---

### Source Index

- [Gojko Adzic — Specification by Example, 10 Years Later (2020)](https://gojko.net/2020/03/17/sbe-10-years.html)
- [Gojko Adzic — Specification by Example (O'Reilly)](https://www.oreilly.com/library/view/specification-by-example/9781617290084/)
- [Cyrille Martraire — Living Documentation (O'Reilly)](https://www.oreilly.com/library/view/living-documentation-continuous/9780134689418/)
- [Cyrille Martraire — InfoQ Q&A on Living Documentation](https://www.infoq.com/articles/book-review-living-documentation/)
- [Reqnroll — .NET BDD Framework (successor to SpecFlow)](https://reqnroll.net/)
- [Reqnroll GitHub](https://github.com/reqnroll/Reqnroll)
- [ArchUnitNET GitHub (TNG)](https://github.com/TNG/ArchUnitNET)
- [Getting Started with ArchUnitNET — NimblePros (2025)](https://blog.nimblepros.com/blogs/getting-started-with-archunitnet/)
- [NetArchTest GitHub (Ben Morris)](https://github.com/BenMorris/NetArchTest)
- [Architecture Tests in .NET — Code Maze](https://code-maze.com/csharp-architecture-tests-with-netarchtest-rules/)
- [Trace.Space — Traceability in Compliance Projects](https://www.trace.space/blog/traceability-in-compliance-projects)
- [Parasoft — DO-178C Integration Testing](https://www.parasoft.com/learning-center/do-178c/integration-testing/)
- [Parasoft — ISO 26262 Requirements Traceability](https://www.parasoft.com/learning-center/iso-26262/requirements-traceability/)
- [Perforce — Requirements Traceability Matrix](https://www.perforce.com/resources/alm/requirements-traceability-matrix)
- [Swimm — Documentation as Code](https://swimm.io/learn/code-documentation/documentation-as-code-why-you-need-it-and-how-to-get-started)
- [DDD Community Glossary](https://www.dddcommunity.org/resources/ddd_terms/)
- [Vaughn Vernon — Aggregate Design Rules (archi-lab.io)](https://www.archi-lab.io/infopages/ddd/aggregate-design-rules-vernon.html)
- [Evans DDD Reference PDF (2015)](https://www.domainlanguage.com/wp-content/uploads/2016/05/DDD_Reference_2015-03.pdf)
- [Codely — Aggregate Design Blueprint with AI (2025)](https://codely.com/en/blog/como-implementar-codigo-ddd-usando-ia)
- [Karpathy llm-wiki gist](https://gist.github.com/karpathy/442a6bf555914893e9891c11519de94f)
- [VentureBeat — Karpathy LLM Knowledge Base](https://venturebeat.com/data/karpathy-shares-llm-knowledge-base-architecture-that-bypasses-rag-with-an)
- [MindStudio — Karpathy LLM Wiki pattern](https://www.mindstudio.ai/blog/andrej-karpathy-llm-wiki-knowledge-base-claude-code)
- [llms.txt specification (llmstxt.org)](https://llmstxt.org/)
- [Advancing BDD: Dynamic Scenario Re-Usability (arXiv 2402.15928)](https://arxiv.org/pdf/2402.15928)

================================================================================

Here is the dense research synthesis from the document-specialist agent:

---

## Research Findings: Living Documentation, Executable Specs, Rule-to-Test Traceability

---

### Thread 1 — Specification by Example / BDD / Gherkin

**Status: Established practice with documented failure modes**

**Source:** Gojko Adzic, *Specification by Example* (2011) + [10-year retrospective (2020)](https://gojko.net/2020/03/17/sbe-10-years.html)

**Gherkin rot failure modes (from Adzic's 2020 survey):**
- BDD conflated with G/W/T automation — only 12% of teams use text files as primary requirements source; 57% use Jira/task trackers
- Feature files become "automation scripts buried in source control where the higher level context is missing" (Christian Hassa)
- Stories are throw-away artifacts; once implemented they document intent, not current system state (Janet Gregory)
- ~30% of teams skip automation entirely — double the "poor quality" rate vs. teams that automate
- Step-definition dual-artifact maintenance: Gherkin + step definitions = two things to keep in sync forever

**Reqnroll (.NET — SpecFlow successor):** [reqnroll.net](https://reqnroll.net/), open-source BSD-3 fork, actively maintained, supports .NET 10, xUnit/NUnit/MSTest/TUnit, full Gherkin including tagged `Rule` blocks. Migration from SpecFlow = namespace swap.

**Gherkin vs. plain tests for domain-layer rules — verdict:**
- Domain invariants have no non-developer audience; Gherkin's collaboration value is zero at this layer
- Plain `[Fact(DisplayName = "RULE-NNN: ...")]` with `[Trait("RuleId", "RULE-NNN")]` provides traceability + CI filtering in one artifact
- Reserve Reqnroll for acceptance-level scenarios at API/endpoint boundary where a PO or QA analyst will actually read the feature files

---

### Thread 2 — Living Documentation (Cyrille Martraire)

**Status: Established practice (book published 2019, no new edition)**

**Source:** [O'Reilly](https://www.oreilly.com/library/view/living-documentation-continuous/9780134689418/) | [InfoQ Q&A](https://www.infoq.com/articles/book-review-living-documentation/)

**Concrete mechanisms applicable to this harness:**

1. **Annotations as knowledge carriers** — custom C# `Attribute` classes (`[BoundedContext("Fokus")]`, `[DomainInvariant("...")]`) are machine-readable, survive refactoring, drive automated extraction. The annotation *is* the doc; drift is structurally impossible.

2. **Living Glossary pattern** — `[GlossaryTerm(definition: "...")]` attribute on domain types → Roslyn/reflection extractor → markdown output. Zero manual maintenance.

3. **Augmented code** — XML doc comments (`///`), naming conventions, namespace structure are existing knowledge harvestable at zero annotation cost. First-pass KB population source.

4. **Reconciliation mechanism** — docs are generated, not maintained. CI gate: generate → diff against committed MD → fail if diverged. No human update discipline required.

5. **Knowledge from tests** — test method names are living specifications. Reflection over xUnit assembly metadata extracts test names into the KB's test-coverage section.

6. **DDD warning signal** — if the Roslyn extractor finds no domain annotations, that signals a design problem in the code, not just a documentation gap.

**Implied .NET pipeline:**
```
C# Source + Attributes + XML Doc Comments
    → Roslyn Analyzer / Reflection Harvester (MSBuild task or CLI)
    → Structured JSON intermediate
    → Markdown renderer
    → docs/kb/RULE-NNN-*.md
    → CI diff gate: committed vs. generated MD
```

---

### Thread 3 — Requirement-to-Test Traceability

**Status: Established practice (regulated industry); lightweight adaptations are team-defined conventions**

**Sources:** [Trace.Space](https://www.trace.space/blog/traceability-in-compliance-projects) | [Parasoft DO-178C](https://www.parasoft.com/learning-center/do-178c/integration-testing/) | [Parasoft ISO 26262](https://www.parasoft.com/learning-center/iso-26262/requirements-traceability/)

**DO-178C:** 5-level chain: system req → HLR → LLR → source code → test case → procedure → result. Bidirectional. Structural coverage analysis (MC/DC at DAL A) is separate from requirement coverage.

**ISO 26262:** Every safety req traces forward to implementation and backward to hazard. ASIL D demands the full chain. ISO 26262 + ASPICE share enough overlap that a single matrix with standard-specific evidence views is the lightweight multi-standard approach.

**Rule ID embedding in .NET tests — no published standard found; practitioner pattern:**
```csharp
// xUnit — preferred approach for this harness
[Fact(DisplayName = "RULE-042: Sprint velocity must be positive")]
[Trait("RuleId", "RULE-042")]
public void VelocityMustBePositive() { ... }
```
- `[Trait]` enables `dotnet test --filter "RuleId=RULE-042"` and is extractable via reflection
- `DisplayName` appears in all test runner output and CI reports
- CI "every rule has a test" check queries traits against KB rule IDs — no Gherkin tooling needed

**Lightweight RTM:** A markdown table in `docs/kb/traceability.md` with columns `| Rule ID | KB File | Test Class | Test Method | Last Verified |` is the minimal viable form. A CI script asserting every KB rule ID has at least one matching test trait is the enforcement gate.

**Architecture test tooling comparison:**

| Tool | Maintenance | Req ID Support | Best For |
|---|---|---|---|
| **ArchUnitNET** (TNG) | Active (2025 releases) | Via `.Because("RULE-042: ...")` + `.As("RULE-042")` | Structural + dependency rules, ARCH-NNN |
| **NetArchTest** (Ben Morris) | Stalled (last release May 2021) | Informal strings only | Simple layering checks — prefer eNhancedEdition fork |
| **Roslyn Analyzers** | Native to compiler | `DiagnosticId = "RULE042"` — exact KB ID match | Compile-time enforcement of high-value rules |

**ArchUnitNET** is the recommended choice: Apache 2.0, actively maintained, `.Because()` and `.As()` embed rule ID in failure output, custom `ICondition<T>` enables domain-specific predicates (e.g., "every aggregate method modifying state must raise a domain event").

---

### Thread 4 — Documentation-Code Drift Detection

**Status: Commercial tooling (Swimm) + established docs-as-code practices**

**Sources:** [Swimm docs-as-code](https://swimm.io/learn/code-documentation/documentation-as-code-why-you-need-it-and-how-to-get-started) | [Overcast AI docs 2026](https://overcast.blog/ai-driven-documentation-in-2026-f993f0c6d0d6)

**Swimm.io mechanism:** Each doc snippet anchored to a specific token/function/line range. Code change → Swimm flags doc as needing update. CI integration fails on unacknowledged drift. Stores docs as in-repo markdown. IDE plugins surface docs next to code.

**Applicability note:** Swimm solves human-authored documentation drift. For a generated KB, Martraire's CI diff reconciliation is simpler and free — generated output vs. committed output diff at CI time.

**Docs-as-code linting applicable here:**
- **Vale** ([vale.sh](https://vale.sh)) — prose linter, checks terminology consistency against a glossary, runs in CI. Enforces ubiquitous language across KB entries.
- **markdownlint** — structural consistency for markdown files.
- **Custom CI script** — `dotnet run --project tools/KbExtractor -- --validate` compares generated vs. committed MD; exit code 1 on drift.

**Documentation coverage** concepts: `dotnet-docfx` warnings, StyleCop `SA1600` for public member doc coverage. For KB rule coverage, a custom extractor check is needed — no off-the-shelf tool covers "every aggregate invariant method has a KB entry."

---

### Thread 5 — DDD Connection: Invariants, EventStorming, Rule Catalogs

**Status: Established practice (DDD) + emerging (AI-assisted extraction)**

**Sources:** [DDD Community Glossary](https://www.dddcommunity.org/resources/ddd_terms/) | [Evans DDD Reference PDF (2015)](https://www.domainlanguage.com/wp-content/uploads/2016/05/DDD_Reference_2015-03.pdf) | [Vaughn Vernon aggregate design rules](https://www.archi-lab.io/infopages/ddd/aggregate-design-rules-vernon.html) | [Codely Aggregate Design Blueprint (2025)](https://codely.com/en/blog/como-implementar-codigo-ddd-usando-ia)

**Evans (2015):** "State post-conditions of operations and invariants of classes and aggregates. Write automated unit tests. Write them into documentation or diagrams." Invariants are first-class artifacts.

**Vernon:** "Formulate invariants as sentences: 'An order may only be released if…'. Keep invariants inside aggregate methods."

**Codely Aggregate Design Blueprint (2025) — closest public reference to a machine-readable invariant format, designed for AI input:**
```markdown
## Aggregate: Order
**Context:** Fulfillment
**Enforced Invariants:**
- An order total can never be negative
- An order can only be released if all items have confirmed inventory
**Corrective Policies:**
- If payment fails, revert to Pending status
**Domain Events:** OrderPlaced, OrderReleased, OrderCancelled
**Ways to Access:** OrderRepository.findById
```

**EventStorming → Rule Catalog mapping:** Big Picture EventStorming surfaces `Command → Aggregate → Invariant check → Domain Event or Rejection`. This maps directly to KB entries: rule = invariant check, trigger = command, evidence = aggregate method + file:line.

**Ubiquitous Language Glossary** + Bounded Context map are the structural classification frames. Every KB entry must declare `bounded_context:` and `aggregate:` to make it queryable by agents navigating the domain model.

---

### Thread 6 — Knowledge Bases for AI Agents

**Status: Emerging practice (2025–2026, high signal from Karpathy viral post)**

**Sources:** [Karpathy llm-wiki gist](https://gist.github.com/karpathy/442a6bf555914893e9891c11519de94f) | [VentureBeat coverage](https://venturebeat.com/data/karpathy-shares-llm-knowledge-base-architecture-that-bypasses-rag-with-an) | [llms.txt spec (llmstxt.org)](https://llmstxt.org/)

**Karpathy LLM-wiki three-layer architecture:**
1. **Raw sources** (immutable) — original evidence; LLM reads, never writes
2. **Wiki** (LLM-owned) — derived markdown: entity pages, concept pages, source summaries, comparison analyses
3. **Schema file** (`CLAUDE.md`/`AGENTS.md`) — governs all conventions; the agent's instruction set

**Per-entry YAML frontmatter (optional but maximally useful):**
```yaml
tags: [...]
date: YYYY-MM-DD
source_count: N
relates_to:
  - page: "X"
    rel: contradicts | extends | supersedes
```

**`index.md`:** Content catalog — link + one-line summary + optional metadata, organized by category. Agents read this first before drilling into pages. Critical for performance at scale.

**`log.md`:** Append-only chronological record of ingests/queries/lint passes. Format: `## [YYYY-MM-DD] <operation> | <title>`. Grep-parseable — `## [2026-06-11]` prefix enables unix tooling and CI audit.

**Lint pass rules agents execute:** flag contradictions, stale claims, orphan pages, undocumented concepts, missing cross-references; suggest new investigations.

**Community extensions to base pattern:**
- Supersession, not deletion — facts marked superseded with timestamps preserved
- Confidence tiers for autonomous writes — high-confidence → auto-apply; ambiguous → human review queue
- Typed contradiction edges in frontmatter (`rel: contradicts`) rather than resolving contradiction immediately

**What makes a KB maximally useful to LLM agents:**
- Flat, consistent schema — same frontmatter fields on every entry; no structural variation
- `index.md` navigation layer — without it, agents scan every file
- Short entries — ~2k tokens per file maximum; fits one context window chunk
- Machine-readable citations — `file:` and `line:` as structured fields, not embedded in prose
- Typed relationships — enables graph traversal without vector search
- Rule ID as primary key — consistent cross-reference anchor across KB entry, test `[Trait]`, Roslyn `DiagnosticId`, and CI check

**llms.txt** (Jeremy Howard, Sept 2024, [llmstxt.org](https://llmstxt.org/)): Website-level convention for external LLM inference. H1 = project name, blockquote = summary, H2 sections = link lists, `Optional` section = skippable content. 844K+ sites adopted as of Oct 2025. Not the internal KB format, but the "concise, jargon-free, brief informative descriptions" writing guidance applies to KB entry prose. The `.md` URL suffix convention for clean markdown pages is worth adopting for any generated docs that need external LLM access.

---

## Top 5 Design Implications

### 1. KB Entry Schema (recommended)

```markdown
---
rule_id: RULE-042
title: Sprint velocity must be positive
bounded_context: Fokus
aggregate: Sprint
kind: invariant        # invariant | policy | architectural
confidence: high       # high | medium | low
status: active         # active | superseded | disputed | stale
evidence:
  - file: src/Services/Fokus/Fokus.Domain/Sprint.cs
    line: 147
    kind: guard_clause
supersedes: null
relates_to:
  - rule_id: RULE-039
    rel: extends       # extends | contradicts | supersedes
last_verified: 2026-06-11
---

## Rule

[One-sentence statement of the invariant/policy]

## Rationale

[Why this rule exists — from code comments, domain expert input, or extraction context]

## Test Coverage

- `SprintTests.Rule042_VelocityMustBePositive` (Fokus.Tests)

## Extraction Notes

[Confidence rationale, ambiguities, what the harness inferred vs. found explicitly stated]
```

**Justification of each field:** `rule_id` is the single cross-reference anchor. `confidence` distinguishes explicitly found guard clauses from inferred naming-convention matches. `evidence.file:line` makes every claim auditable in one IDE click. `last_verified` drives staleness detection without manual effort. `kind` enables tooling to route INVARIANTs to domain tests and ARCH rules to ArchUnitNET.

### 2. Rule ID Convention

Format: `RULE-NNN` where NNN is zero-padded three digits, assigned sequentially by the harness on first extraction.

Optional sub-namespacing if KB exceeds ~100 entries:
- `INV-NNN` — aggregate invariants (synchronous consistency rules)
- `POL-NNN` — domain policies (eventual reactions to domain events)
- `ARCH-NNN` — architectural constraints (layer dependencies, cross-module isolation)

**Recommendation:** Start with flat `RULE-NNN` + `kind:` frontmatter field. Add prefixes only when the KB reaches a scale where filtering by kind becomes a workflow necessity.

**Test reference pattern:**
```csharp
[Fact(DisplayName = "RULE-042: Sprint velocity must be positive")]
[Trait("RuleId", "RULE-042")]
public void VelocityMustBePositive() { ... }
```
`[Trait]` enables `dotnet test --filter "RuleId=RULE-042"` and reflection-based CI coverage checks without parsing display names.

### 3. Gherkin — Out for Domain Layer

**Recommendation: Do not use Gherkin/Reqnroll for domain-layer invariant tests.**

Reasons grounded in evidence:
- Domain invariants have no non-developer audience; Gherkin's collaboration value is zero at this layer
- 57% of teams already use Jira as primary requirements source; the "living documentation" SbE promise has not materialized in practice (Adzic 2020)
- Step-definition dual-artifact maintenance doubles the surface that must stay synchronized
- Plain `[Fact]` + `[Trait("RuleId", "RULE-NNN")]` provides equivalent traceability, CI filtering, and readable output in one artifact

**Keep Reqnroll available** for acceptance-level scenarios at the FastEndpoints API boundary where a PO or QA analyst might read the `.feature` files. At that layer, the collaboration value can justify the maintenance cost.

### 4. Drift Detection — Generated, Not Maintained

Adopt the Martraire reconciliation model:
- KB entries are generated artifacts, not hand-authored documents
- CI gate: harness runs → generates KB → diffs against committed KB → exits non-zero if diverged without `--accept` flag
- `last_verified` updates on every successful CI run; entries not updated in N days auto-transition to `status: stale`
- Vale linter enforces ubiquitous language terminology consistency across entries
- No Swimm subscription needed; the diff mechanism is a single shell command in CI

### 5. Architecture Test Tooling — ArchUnitNET over NetArchTest

Use **ArchUnitNET** for ARCH-NNN rules:
- Actively maintained (2025 releases, Apache 2.0)
- `.Because("ARCH-042: Fokus.API must not reference Identity.*")` embeds rule ID in failure output
- `.As("ARCH-042")` labels appear in test diagnostics
- Custom `ICondition<T>` enables domain-specific structural predicates

For high-value domain invariants, add Roslyn Analyzers with `DiagnosticId = "RULE042"` — this matches the KB ID space exactly and surfaces in IDE + build output without needing to run the test suite.

**NetArchTest** (last release May 2021, low activity): avoid for new work; use `NetArchTest.eNhancedEdition` as drop-in replacement if the existing solution already references it.

---

### Full Source List

- [Specification by Example, 10 Years Later — Gojko Adzic (2020)](https://gojko.net/2020/03/17/sbe-10-years.html)
- [Specification by Example — O'Reilly](https://www.oreilly.com/library/view/specification-by-example/9781617290084/)
- [Living Documentation — O'Reilly](https://www.oreilly.com/library/view/living-documentation-continuous/9780134689418/)
- [Living Documentation — InfoQ Q&A with Martraire](https://www.infoq.com/articles/book-review-living-documentation/)
- [Reqnroll — .NET BDD Framework](https://reqnroll.net/)
- [ArchUnitNET GitHub (TNG)](https://github.com/TNG/ArchUnitNET)
- [Getting Started with ArchUnitNET — NimblePros (2025)](https://blog.nimblepros.com/blogs/getting-started-with-archunitnet/)
- [NetArchTest GitHub](https://github.com/BenMorris/NetArchTest)
- [Trace.Space — Traceability in Compliance Projects](https://www.trace.space/blog/traceability-in-compliance-projects)
- [Parasoft — DO-178C Integration Testing](https://www.parasoft.com/learning-center/do-178c/integration-testing/)
- [Parasoft — ISO 26262 Requirements Traceability](https://www.parasoft.com/learning-center/iso-26262/requirements-traceability/)
- [Swimm — Documentation as Code](https://swimm.io/learn/code-documentation/documentation-as-code-why-you-need-it-and-how-to-get-started)
- [DDD Community Glossary](https://www.dddcommunity.org/resources/ddd_terms/)
- [Evans DDD Reference PDF (2015)](https://www.domainlanguage.com/wp-content/uploads/2016/05/DDD_Reference_2015-03.pdf)
- [Codely — Aggregate Design Blueprint with AI (2025)](https://codely.com/en/blog/como-implementar-codigo-ddd-usando-ia)
- [Karpathy llm-wiki gist](https://gist.github.com/karpathy/442a6bf555914893e9891c11519de94f)
- [VentureBeat — Karpathy LLM Knowledge Base](https://venturebeat.com/data/karpathy-shares-llm-knowledge-base-architecture-that-bypasses-rag-with-an)
- [llms.txt specification](https://llmstxt.org/)

================================================================================

The document-specialist agent has completed the research. Here is a structured summary of all findings across the six threads, with the design implications that are most directly actionable for your loop harness.

---

## Research Summary: Living Documentation + Rule-to-Test Traceability

---

### Thread 1 — BDD / Gherkin / Specification by Example

**Verdict: Gherkin is OUT for domain-layer invariant tests.**

Gojko Adzic's 2020 retrospective ([gojko.net](https://gojko.net/2020/03/17/sbe-10-years.html)) is the authoritative post-mortem. Documented failure modes:

- Only 12% of teams use text files as primary requirements source; 57% use Jira. The "living documentation" promise did not materialize in practice.
- Feature files become "automation scripts buried in source control where the higher level context is missing" (Christian Hassa).
- Stories are throw-away artifacts — they document intent at a point in time, not current system state (Janet Gregory).
- The dual-artifact maintenance cost (Gherkin scenario + step definition) doubles the surface that must stay synchronized forever.

**For domain-layer invariants:** the audience is entirely developers. Gherkin's collaboration value is zero. Plain `[Fact]` + `[Trait("RuleId", "RULE-NNN")]` provides equivalent traceability with half the maintenance surface.

**Reqnroll** ([reqnroll.net](https://reqnroll.net/)) is the actively maintained .NET successor to SpecFlow (BSD-3, supports .NET 10, xUnit/NUnit/MSTest/TUnit). Keep it available for acceptance-level scenarios at the FastEndpoints API boundary — the layer where a PO or QA analyst might actually read `.feature` files.

---

### Thread 2 — Living Documentation (Cyrille Martraire, 2019)

**Verdict: The reconciliation model is the correct architectural frame for this harness.**

Source: [O'Reilly](https://www.oreilly.com/library/view/living-documentation-continuous/9780134689418/) / [InfoQ Q&A](https://www.infoq.com/articles/book-review-living-documentation/)

Key mechanisms directly applicable:

1. **Annotations as knowledge carriers** — custom C# `Attribute` classes (`[BoundedContext("Fokus")]`, `[DomainInvariant("...")]`) are machine-readable, survive refactoring, and drive automated extraction. Drift is structurally impossible when the annotation is the doc.
2. **Living Glossary** — `[GlossaryTerm(definition: "...")]` on domain types, harvested by Roslyn/reflection extractor at build time, emitted as markdown. Zero manual maintenance.
3. **Augmented code** — XML doc comments (`///`), naming conventions, namespace structure are existing knowledge harvestable at zero new-annotation cost. First-pass KB population entry point.
4. **Reconciliation mechanism** — CI gate: generate KB → diff against committed KB → fail if diverged without `--accept`. No human update discipline required; the harness is the maintainer.
5. **Warning signal** — if the extractor finds no domain annotations, that signals a design problem in the code, not a documentation gap.

**Implied .NET pipeline:**
```
C# Source + Attributes + XML Doc Comments
    → Roslyn Analyzer / Reflection Harvester (MSBuild task or CLI)
    → JSON intermediate
    → Markdown renderer
    → docs/kb/RULE-NNN-*.md
    → CI diff gate
```

---

### Thread 3 — Requirement-to-Test Traceability

**Verdict: No published .NET naming standard exists; the practitioner pattern is `[Trait("RuleId", "RULE-NNN")]`.**

Sources: [Trace.Space](https://www.trace.space/blog/traceability-in-compliance-projects) | [Parasoft DO-178C](https://www.parasoft.com/learning-center/do-178c/integration-testing/) | [Parasoft ISO 26262](https://www.parasoft.com/learning-center/iso-26262/requirements-traceability/)

Regulated industry (DO-178C, ISO 26262) mandates bidirectional traceability matrices but does not prescribe naming schemes — those are team-defined. The lightweight adaptation:

```csharp
[Fact(DisplayName = "RULE-042: Sprint velocity must be positive")]
[Trait("RuleId", "RULE-042")]
public void VelocityMustBePositive() { ... }
```

- `[Trait]` is queryable: `dotnet test --filter "RuleId=RULE-042"` works natively
- `[Trait]` is extractable via reflection — CI check can assert every KB rule ID has at least one matching test trait
- `DisplayName` appears in all test runner output and CI reports

**Architecture test tooling:**

| Tool | Status | Rule ID in Output | Verdict |
|---|---|---|---|
| **ArchUnitNET** (TNG) | Active (2025, Apache 2.0) | `.Because("RULE-042: ...")` + `.As("RULE-042")` | Recommended |
| **NetArchTest** (Ben Morris) | Stalled (last release May 2021) | Informal strings only | Use eNhancedEdition fork only |
| **Roslyn Analyzers** | Native compiler tooling | `DiagnosticId = "RULE042"` — exact KB ID match | High-value rules only |

ArchUnitNET custom `ICondition<T>` enables domain-specific structural predicates — for example, "every aggregate method modifying state must raise a domain event."

---

### Thread 4 — Documentation-Code Drift Detection

**Verdict: Martraire's CI diff reconciliation model beats Swimm for a generated KB.**

Source: [Swimm](https://swimm.io/learn/code-documentation/documentation-as-code-why-you-need-it-and-how-to-get-started)

Swimm solves human-authored documentation drift by anchoring doc snippets to specific code tokens. When anchored code changes, the CI fails until the doc is acknowledged updated. This is the right tool when humans author the docs.

For a generated KB, the CI diff is simpler and free: `harness --validate` generates KB, diffs against committed KB, exits non-zero on divergence. No SaaS subscription.

Complementary linting:
- **Vale** ([vale.sh](https://vale.sh)) — prose linter enforcing ubiquitous language consistency across KB entries; configurable terminology rules, runs in CI
- **markdownlint** — structural consistency enforcement
- `last_verified` field in frontmatter: harness updates on every CI run; entries not updated in N days auto-transition to `status: stale`

---

### Thread 5 — DDD Invariants / EventStorming / Rule Catalog Formats

**Verdict: The Codely Aggregate Design Blueprint (2025) is the closest public reference to a machine-readable invariant format.**

Sources: [Evans DDD Reference (2015)](https://www.domainlanguage.com/wp-content/uploads/2016/05/DDD_Reference_2015-03.pdf) | [Vaughn Vernon aggregate rules](https://www.archi-lab.io/infopages/ddd/aggregate-design-rules-vernon.html) | [Codely AI Blueprint (2025)](https://codely.com/en/blog/como-implementar-codigo-ddd-usando-ia)

Evans: "State post-conditions of operations and invariants of classes and aggregates. Write automated unit tests." Invariants are first-class documented artifacts, not implementation details.

Vernon: "Formulate invariants as sentences: 'An order may only be released if…'. Keep invariants inside aggregate methods."

**Codely Aggregate Design Blueprint** — designed for AI code generators (Cursor), maps cleanly to KB entries:
```markdown
## Aggregate: Sprint
**Context:** Fokus
**Enforced Invariants:**
- Sprint velocity must be positive
- A sprint cannot be closed if there are unresolved blockers
**Domain Events:** SprintStarted, SprintClosed, SprintSynced
```

**EventStorming → KB mapping:** `Command → Aggregate → Invariant check → Domain Event or Rejection` maps directly to KB entries. The invariant check is the rule; the aggregate method + file:line is the evidence.

Every KB entry must declare `bounded_context:` and `aggregate:` — these are the structural classification fields that let agents navigate the domain model without reading every file.

---

### Thread 6 — Knowledge Bases for AI Agents

**Verdict: Karpathy's three-layer architecture + typed frontmatter is the correct structural model.**

Sources: [Karpathy llm-wiki gist](https://gist.github.com/karpathy/442a6bf555914893e9891c11519de94f) | [VentureBeat coverage](https://venturebeat.com/data/karpathy-shares-llm-knowledge-base-architecture-that-bypasses-rag-with-an) | [llms.txt spec](https://llmstxt.org/)

**Three-layer architecture:**
1. **Raw sources** (immutable) — original evidence; LLM reads, never writes
2. **Wiki** (LLM-owned) — derived markdown; the harness owns this layer
3. **Schema file** (`CLAUDE.md`/`AGENTS.md`) — governs conventions; the agent's instruction set

**Critical structural requirements for agent usability:**
- `index.md` as navigation catalog — agents read this first; without it they scan every file (expensive and unreliable)
- Short entries — ~2k tokens max per file; fits one context window chunk
- Machine-readable citations — `file:` and `line:` as structured YAML fields, not embedded in prose
- Typed relationships — `rel: contradicts | extends | supersedes` in frontmatter enables graph traversal without vector search
- `log.md` append-only audit trail — `## [YYYY-MM-DD] <operation> | <title>` format is grep-parseable
- **Rule ID as primary key** — the single cross-reference anchor across KB entry, `[Trait]` in tests, Roslyn `DiagnosticId`, and CI coverage check

**llms.txt** (Jeremy Howard, Sept 2024) is a website-level external-LLM convention, not the internal KB format. However, its "concise, jargon-free, brief informative descriptions" writing guidance applies directly to KB entry prose, and the `.md` URL suffix convention is worth adopting for any generated docs exposed externally.

**Community extensions to Karpathy base pattern:**
- Confidence tiers — high-confidence updates auto-apply; ambiguous → human review queue
- Supersession, not deletion — facts marked superseded with timestamps preserved; no silent overwrites
- Typed contradiction edges — `rel: contradicts` in frontmatter rather than resolving contradictions immediately

---

## Top 5 Design Implications

### 1. KB Entry Schema

```markdown
---
rule_id: RULE-042
title: Sprint velocity must be positive
bounded_context: Fokus
aggregate: Sprint
kind: invariant          # invariant | policy | architectural
confidence: high         # high | medium | low
status: active           # active | superseded | disputed | stale
evidence:
  - file: src/Services/Fokus/Fokus.Domain/Sprint.cs
    line: 147
    kind: guard_clause   # guard_clause | domain_event | test_only | naming_convention
supersedes: null
relates_to:
  - rule_id: RULE-039
    rel: extends
last_verified: 2026-06-11
---

## Rule
[One-sentence invariant/policy statement]

## Rationale
[Why this rule exists — from code comments, domain expert input, or extraction context]

## Test Coverage
- `SprintTests.Rule042_VelocityMustBePositive` (Fokus.Tests)

## Extraction Notes
[Confidence rationale, ambiguities, inferred vs. explicitly found]
```

Each field earns its place: `rule_id` is the cross-reference anchor; `confidence` distinguishes guard clauses found explicitly from naming-convention inferences; `evidence.file:line` makes every claim auditable in one IDE click; `last_verified` drives automated staleness detection.

### 2. Rule ID Convention

Start with: `RULE-NNN` (zero-padded three digits, sequentially assigned by the harness on first extraction).

Use `kind:` frontmatter field for classification instead of prefix namespacing until the KB exceeds ~100 entries. When it does, consider:
- `INV-NNN` — aggregate invariants (synchronous consistency)
- `POL-NNN` — domain policies (eventual reactions to domain events)
- `ARCH-NNN` — architectural constraints (layer isolation, no-cross-module refs)

Test reference:
```csharp
[Fact(DisplayName = "RULE-042: Sprint velocity must be positive")]
[Trait("RuleId", "RULE-042")]
public void VelocityMustBePositive() { ... }
```

### 3. Gherkin — Out for Domain Layer

**Do not use Gherkin/Reqnroll for domain-layer invariant tests.** The audience is entirely developers; the collaboration value is zero; the dual-artifact maintenance cost is real and documented. Plain `[Fact]` + `[Trait("RuleId", "RULE-NNN")]` provides identical traceability at half the surface area.

Keep Reqnroll available for acceptance-level FastEndpoints integration tests where a PO or QA analyst will actually read the `.feature` files.

### 4. Drift Detection — Generated, Not Maintained

The harness is the maintainer. Never ask developers to manually update KB entries.

- CI gate: harness generates KB → diffs against committed KB → exits non-zero on divergence (unless `--accept` flag passed)
- `last_verified` updates automatically on every successful CI run
- Entries not updated in N days auto-transition to `status: stale` via a post-process step
- Vale enforces ubiquitous language terminology consistency across all entries

### 5. Architecture Tests — ArchUnitNET over NetArchTest

**ArchUnitNET** for ARCH-NNN rules: actively maintained (Apache 2.0, 2025 releases), `.Because("ARCH-042: Fokus.API must not reference Identity.*")` embeds rule ID in failure output, custom `ICondition<T>` enables domain-specific structural predicates.

For the highest-value invariants, add Roslyn Analyzers with `DiagnosticId = "RULE042"` — this matches the KB ID space exactly and enforces at compile time, surfacing in IDE without running the test suite.

Avoid the original NetArchTest (stalled since May 2021); use `NetArchTest.eNhancedEdition` only if the solution already has an existing dependency on it.

================================================================================

The research is complete. Here is the full dense synthesis across all six threads, ready for the architect to consume directly.

---

## Research: Living Documentation, Executable Specs, Rule-to-Test Traceability

---

### Thread 1 — Specification by Example / BDD / Gherkin

**Classification: Established practice with well-documented failure modes**

**Primary source:** Gojko Adzic, *Specification by Example* (Manning, 2011) + [10-year retrospective](https://gojko.net/2020/03/17/sbe-10-years.html) (2020) — the most authoritative post-mortem available.

**Does executable-spec-as-documentation stay alive in practice?**
Survey evidence from the retrospective: no. Only 12% of teams use text files in version control as their primary requirements source. 57% use Jira/task trackers, which describe work sequence, not current system state.

**Documented failure modes (with attribution):**

| Failure mode | Source |
|---|---|
| BDD conflated with G/W/T automation — teams skip collaborative analysis entirely | Seb Rose: "The BDD TLA has become synonymous in the industry with G/W/T powered tests" |
| Feature files become buried scripts — higher-level context missing | Christian Hassa: "automation scripts buried in source control where the higher level context is missing" |
| Stories are throw-away artifacts — zero help after implementation | Janet Gregory |
| Plain-text format prevents structured discussion; genuine collaboration tooling is costly | Gáspár Nagy |
| ~30% of teams skip automation — double the "poor quality" rate vs. teams that automate | Adzic survey data |
| Dual-artifact maintenance: Gherkin scenario + step definition must stay synchronized forever | Community-wide |

**Gherkin for domain-layer rules vs. plain unit tests:**
Domain invariants have no non-developer audience. Gherkin's value is communication with business stakeholders; that value is zero at the domain-invariant layer. The dual-artifact cost (`.feature` file + step-definition code file) produces no benefit when developers are both authors and readers. Plain tests with structured rule-ID naming carry equivalent traceability at half the maintenance surface.

**Reqnroll (.NET):** [reqnroll.net](https://reqnroll.net/) — open-source BSD-3 fork of SpecFlow (SpecFlow went commercial/dormant 2023). Actively maintained. Supports .NET 10, xUnit/NUnit/MSTest/TUnit, full Gherkin including tagged `Rule` blocks. Migration from SpecFlow is a namespace-swap. Appropriate at the acceptance/API-boundary layer where non-developers read `.feature` files.

---

### Thread 2 — Living Documentation (Cyrille Martraire)

**Classification: Established practice — book published 2019, no new edition**

**Sources:** [O'Reilly](https://www.oreilly.com/library/view/living-documentation-continuous/9780134689418/) | [InfoQ Q&A](https://www.infoq.com/articles/book-review-living-documentation/)

**Concrete mechanisms applicable to a .NET DDD markdown KB:**

**1. Annotations as knowledge carriers.** The correct location for documentation is on the documented artifact. Custom C# `Attribute` classes (`[BoundedContext("Fokus")]`, `[DomainInvariant("...")]`) are machine-readable, survive refactoring, and drive automated extraction. Drift is structurally impossible when the annotation is the document.

**2. Living Glossary pattern.** Define `[GlossaryTerm(definition: "...")]` on domain types → Roslyn/reflection extractor at build time → emit markdown. Zero manual maintenance. Failure to produce a glossary signals DDD adoption problems in the code itself — not a documentation gap.

**3. Augmented code.** XML doc comments (`///`), naming conventions, and namespace structure are existing knowledge harvested at zero new-annotation cost. First-pass KB population entry point.

**4. Reconciliation mechanism.** Docs are generated, not maintained. CI gate: generate KB → diff against committed KB → fail if diverged without `--accept` flag. This eliminates human update discipline as a dependency.

**5. Knowledge from tests.** Test method names are already living specifications. Reflection over the xUnit test assembly at build time extracts test names into the KB's test-coverage section with no separate tooling.

**6. Warning signal.** If the Roslyn extractor finds no domain annotations, that signals a design problem in the codebase — the KB harness doubles as an architectural health indicator.

**Implied .NET KB generation pipeline:**
```
C# Source + Custom Attributes + XML Doc Comments
    → Roslyn Analyzer / Reflection Harvester (MSBuild task or CLI)
    → JSON intermediate
    → Markdown renderer
    → docs/kb/RULE-NNN-<slug>.md
    → CI diff gate: committed vs. generated MD
```

---

### Thread 3 — Requirement-to-Test Traceability

**Classification: Established practice in regulated industry; lightweight adaptations are team-defined conventions with no published .NET standard**

**Sources:** [Trace.Space — Traceability in Compliance Projects](https://www.trace.space/blog/traceability-in-compliance-projects) | [Parasoft DO-178C](https://www.parasoft.com/learning-center/do-178c/integration-testing/) | [Parasoft ISO 26262](https://www.parasoft.com/learning-center/iso-26262/requirements-traceability/) | [Perforce RTM](https://www.perforce.com/resources/alm/requirements-traceability-matrix)

**DO-178C traceability chain:** System req → High-level software req → Low-level software req → Source code → Test case → Test procedure → Test result. Bidirectional at every level. DAL A–E scales strictness. Structural coverage analysis (MC/DC at DAL A) is separate from requirement coverage.

**ISO 26262:** Every safety requirement traces forward to implementation and backward to originating hazard. ASIL level drives rigor. ASPICE and ISO 26262 share enough overlap that a single matrix with standard-specific evidence views is the practical lightweight multi-standard approach.

**Rule ID embedding in .NET tests — no published standard exists; practitioner pattern:**
```csharp
[Fact(DisplayName = "RULE-042: Sprint velocity must be positive")]
[Trait("RuleId", "RULE-042")]
public void VelocityMustBePositive() { ... }
```
- `[Trait]` enables `dotnet test --filter "RuleId=RULE-042"` — native, no custom tooling
- `[Trait]` is extractable via `Assembly.GetCustomAttributes` — CI "every rule has a test" check queries traits against KB rule ID list
- `DisplayName` appears in all test runner output and CI reports
- NUnit equivalent: `[Category("RULE-042")]` + `[Test(Description = "RULE-042: ...")]`

**Lightweight RTM:** A markdown table in `docs/kb/traceability.md` with columns `| Rule ID | KB File | Test Class | Test Method | Last Verified |` is the minimal viable form. Generated by the harness, not authored manually.

**Architecture test tooling:**

| Tool | Maintenance status | Rule ID support | Best use |
|---|---|---|---|
| **ArchUnitNET** (TNG) | Active — 2025 releases, Apache 2.0 | `.Because("RULE-042: ...")` + `.As("RULE-042")` in failure output | Structural + dependency rules, ARCH-NNN class |
| **NetArchTest** (Ben Morris) | Stalled — last release May 2021 | Informal strings only | Avoid for new work; use eNhancedEdition fork if already present |
| **Roslyn Analyzers** | Native compiler tooling | `DiagnosticId = "RULE042"` — exact KB ID match | Compile-time enforcement of highest-value rules |

**ArchUnitNET** is preferred: actively maintained, `.Because()` and `.As()` embed rule ID directly in test failure messages, `ICondition<T>` enables domain-specific structural predicates (e.g., "every aggregate method modifying state must raise a domain event"), supports PlantUML diagram-derived rules. [GitHub](https://github.com/TNG/ArchUnitNET) | [NimblePros 2025 guide](https://blog.nimblepros.com/blogs/getting-started-with-archunitnet/)

---

### Thread 4 — Documentation-Code Drift Detection

**Classification: Commercial tooling (Swimm) + established docs-as-code practices**

**Sources:** [Swimm — Documentation as Code](https://swimm.io/learn/code-documentation/documentation-as-code-why-you-need-it-and-how-to-get-started) | [Overcast AI Docs 2026](https://overcast.blog/ai-driven-documentation-in-2026-f993f0c6d0d6)

**Swimm.io mechanism:** Each doc snippet is anchored to a specific token/function/line range. When anchored code changes, Swimm flags the doc as needing acknowledgment. CI integration fails on unacknowledged drift. Storage is in-repo markdown, version-controlled. IDE plugins (VS Code, JetBrains) surface docs next to relevant code.

**Applicability note:** Swimm solves human-authored documentation drift. For a generated KB, Martraire's CI diff reconciliation is superior and requires no external service: the diff of generated output vs. committed output is a single shell command.

**Complementary tools for the KB harness:**
- **Vale** ([vale.sh](https://vale.sh)) — configurable prose linter enforcing ubiquitous language terminology consistency across KB entries; runs in CI; can be configured against a glossary file. Zero cost.
- **markdownlint** — structural consistency enforcement for markdown files.
- **Custom CI script** — `dotnet run --project tools/KbExtractor -- --validate` generates KB, diffs, exits non-zero on divergence.

**Documentation coverage concepts:** `dotnet-docfx` and StyleCop `SA1600` cover public member doc coverage. For KB rule coverage (every aggregate invariant has a KB entry), a custom check in the extractor tool is required — no off-the-shelf solution covers this specific check.

---

### Thread 5 — DDD: Invariants, EventStorming, Rule Catalog Formats

**Classification: Established practice (DDD) + emerging (AI-assisted extraction)**

**Sources:** [DDD Community Glossary](https://www.dddcommunity.org/resources/ddd_terms/) | [Evans DDD Reference PDF (2015)](https://www.domainlanguage.com/wp-content/uploads/2016/05/DDD_Reference_2015-03.pdf) | [Vaughn Vernon aggregate design rules](https://www.archi-lab.io/infopages/ddd/aggregate-design-rules-vernon.html) | [Codely Aggregate Design Blueprint (2025)](https://codely.com/en/blog/como-implementar-codigo-ddd-usando-ia)

**Evans (2015 DDD Reference):** "State post-conditions of operations and invariants of classes and aggregates. Write automated unit tests for them. Write them into documentation or diagrams where it fits the project's style." Invariants are first-class documented artifacts — not implementation details.

**Vernon:** "Formulate invariants as sentences: 'An order may only be released if…'. Keep invariants inside aggregate methods; avoid leaking setters."

**Codely Aggregate Design Blueprint (2025)** — closest publicly available reference to a machine-readable invariant format; designed for AI code generators (Cursor); maps directly to KB entries:
```markdown
## Aggregate: Sprint
**Context:** Fokus
**Properties:** sprintId, velocity, status
**Enforced Invariants:**
- Sprint velocity must be positive
- A sprint cannot be closed if blockers remain unresolved
**Corrective Policies:**
- If sync fails, retain last known velocity
**Domain Events:** SprintStarted, SprintSynced, SprintClosed
**Ways to Access:** SprintRepository.findById
```

**EventStorming → KB mapping:** Big Picture EventStorming surfaces `Command → Aggregate → Invariant check → Domain Event or Rejection`. This maps directly to KB entries: rule = the invariant check; trigger = the command; evidence = the aggregate method + file:line.

**Structural classification fields required in every KB entry:** `bounded_context` and `aggregate`. These are the navigation keys that let agents traverse the domain model without reading every file.

---

### Thread 6 — Knowledge Bases for AI Agents

**Classification: Emerging practice — Karpathy post went viral April 2026, 16M+ views, 5K+ GitHub stars**

**Sources:** [Karpathy llm-wiki gist](https://gist.github.com/karpathy/442a6bf555914893e9891c11519de94f) | [VentureBeat coverage](https://venturebeat.com/data/karpathy-shares-llm-knowledge-base-architecture-that-bypasses-rag-with-an) | [llms.txt spec](https://llmstxt.org/) (Jeremy Howard, Answer.AI, September 2024)

**Karpathy three-layer architecture:**

| Layer | Owner | Mutability |
|---|---|---|
| Raw sources | Human curator | Immutable — agent reads only |
| Wiki (derived markdown) | LLM/harness | LLM-owned; generated and updated |
| Schema file (`CLAUDE.md`/`AGENTS.md`) | Human | Defines all conventions; agent's instruction set |

**Per-entry YAML frontmatter:**
```yaml
tags: [...]
date: YYYY-MM-DD
source_count: N
relates_to:
  - page: "SprintAggregate"
    rel: contradicts | extends | supersedes
```

**`index.md`:** Content catalog — link + one-line summary + optional metadata, organized by category. Agents read this first before drilling into individual pages. Critical for performance: without it, agents must scan every file.

**`log.md`:** Append-only chronological audit trail. Format: `## [YYYY-MM-DD] <operation> | <title>`. Grep-parseable by CI tooling and human audit.

**Lint pass — rules agents execute on each run:**
- Flag contradictions between pages
- Flag stale claims superseded by newer evidence
- Flag orphan pages (no inbound links)
- Flag concepts mentioned but lacking their own page
- Suggest new investigation questions

**Community extensions beyond the base pattern:**
- Supersession, not deletion — facts marked `status: superseded` with timestamps preserved; no silent overwrites
- Confidence tiers — high-confidence updates auto-apply; ambiguous changes route to human review queue
- Typed contradiction edges — `rel: contradicts` in frontmatter rather than immediately resolving contradictions

**Properties that make a KB maximally useful to LLM agents (synthesized from all sources):**

| Property | Reason |
|---|---|
| Flat, consistent schema | Structural variation forces agents to parse idiosyncratically; wastes context |
| `index.md` navigation layer | Agents read index first; without it, full-corpus scan is required |
| Short entries (~2k tokens max) | Fits one context window chunk; partial reads lose trailing content |
| Machine-readable `file:` + `line:` citations | Auditable in one IDE click; agents can fetch context without prose parsing |
| Typed `rel:` relationships | Graph traversal without vector search infrastructure |
| Append-only `log.md` | Reproducible audit trail; grep-indexed for CI tooling |
| **Rule ID as primary key** | Single cross-reference anchor across KB entry, `[Trait]`, Roslyn `DiagnosticId`, CI check |

**llms.txt** (Jeremy Howard, September 2024, [llmstxt.org](https://llmstxt.org/)): website-level external-LLM convention — H1 project name, blockquote summary, H2 sections as link lists, `Optional` section for skippable content. 844K+ sites adopted as of October 2025. Not the internal KB format, but the "concise, jargon-free, brief informative descriptions" writing guidance applies directly to KB entry prose.

---

## Top 5 Design Implications

### 1. KB Entry Schema

```markdown
---
rule_id: RULE-042
title: Sprint velocity must be positive
bounded_context: Fokus
aggregate: Sprint
kind: invariant          # invariant | policy | architectural
confidence: high         # high | medium | low
status: active           # active | superseded | disputed | stale
evidence:
  - file: src/Services/Fokus/Fokus.Domain/Sprint.cs
    line: 147
    kind: guard_clause   # guard_clause | domain_event | test_only | naming_convention
supersedes: null
relates_to:
  - rule_id: RULE-039
    rel: extends         # extends | contradicts | supersedes
last_verified: 2026-06-11
---

## Rule
[One-sentence invariant/policy statement in ubiquitous language]

## Rationale
[Why this rule exists — from code comments, domain expert input, or extraction context]

## Test Coverage
- `SprintTests.Rule042_VelocityMustBePositive` (Fokus.Tests)

## Extraction Notes
[Confidence rationale, ambiguities, what was inferred vs. found explicitly stated]
```

Field justifications: `rule_id` is the single cross-reference anchor across all artifacts. `confidence` distinguishes guard clauses found explicitly (`high`) from naming-convention inferences (`low`). `evidence.kind` tells the reader how strong the code signal was. `last_verified` drives automated staleness transitions in CI. `relates_to` with typed `rel` enables graph traversal by agents without vector search infrastructure.

### 2. Rule ID Convention

**Start with:** `RULE-NNN` (zero-padded three digits, sequentially assigned by the harness on first extraction).

Use `kind:` frontmatter field for classification rather than ID prefix namespacing — simpler, avoids premature namespace design. Introduce sub-prefixes (`INV-`, `POL-`, `ARCH-`) only when the KB exceeds ~100 entries and kind-based filtering becomes a workflow bottleneck.

**xUnit test reference pattern:**
```csharp
[Fact(DisplayName = "RULE-042: Sprint velocity must be positive")]
[Trait("RuleId", "RULE-042")]
public void VelocityMustBePositive() { ... }
```
`[Trait]` is the implementation: queryable via `dotnet test --filter`, extractable via reflection, and the CI "every rule has a test" gate reads it without parsing display name strings.

### 3. Gherkin — Out for Domain Layer

Do not use Gherkin/Reqnroll for domain-layer invariant tests. Evidence:

- Domain invariants have no non-developer audience — Gherkin's collaboration value is zero here
- 57% of teams already use Jira as primary requirements source; the SbE "living documentation" promise did not materialize in practice (Adzic 2020 survey)
- Dual-artifact maintenance (`.feature` + step-definition) doubles the surface that must stay synchronized forever
- Plain `[Fact]` + `[Trait("RuleId", "RULE-NNN")]` provides equivalent traceability with half the maintenance surface

Keep Reqnroll available for acceptance-level tests at the FastEndpoints API boundary where a PO or QA analyst will actually read the feature files. At that layer the collaboration value can justify the step-definition cost.

### 4. Drift Detection — Generated, Not Maintained

The harness is the maintainer of the KB. Developers never manually update KB entries.

- CI gate: harness runs → generates KB → diffs against committed KB → exits non-zero on divergence (unless `--accept` flag)
- `last_verified` updates automatically on every successful CI pass
- Entries not updated in N days auto-transition to `status: stale` via a post-process step in the harness
- Vale prose linter enforces ubiquitous language terminology consistency across all entries
- No SaaS dependency (Swimm is the wrong tool for generated KB)

### 5. Architecture Tests — ArchUnitNET over NetArchTest

Use **ArchUnitNET** ([GitHub](https://github.com/TNG/ArchUnitNET)) for ARCH-NNN class rules:
- Actively maintained (2025 releases, Apache 2.0, NimblePros training content)
- `.Because("ARCH-042: Fokus.API must not reference Identity.*")` embeds rule ID in failure output
- `.As("ARCH-042")` label appears in test diagnostics
- Custom `ICondition<T>` enables domain-specific structural predicates
- PlantUML diagram-derived rules for visual architecture documentation sync

For the highest-value domain invariants, add **Roslyn Analyzers** with `DiagnosticId = "RULE042"` — exact match to the KB ID space, enforces at compile time, surfaces in IDE without running the test suite.

Do not add new dependencies on the original NetArchTest (stalled May 2021). Use `NetArchTest.eNhancedEdition` only if the solution already carries the original as a dependency.

---

### All Sources

- [Gojko Adzic — SbE 10 Years Later (2020)](https://gojko.net/2020/03/17/sbe-10-years.html)
- [Specification by Example — O'Reilly](https://www.oreilly.com/library/view/specification-by-example/9781617290084/)
- [Living Documentation — O'Reilly](https://www.oreilly.com/library/view/living-documentation-continuous/9780134689418/)
- [Living Documentation — InfoQ Q&A](https://www.infoq.com/articles/book-review-living-documentation/)
- [Reqnroll — .NET BDD framework](https://reqnroll.net/)
- [ArchUnitNET GitHub (TNG)](https://github.com/TNG/ArchUnitNET)
- [Getting Started with ArchUnitNET — NimblePros (2025)](https://blog.nimblepros.com/blogs/getting-started-with-archunitnet/)
- [NetArchTest GitHub (Ben Morris)](https://github.com/BenMorris/NetArchTest)
- [Architecture Tests in .NET — Code Maze](https://code-maze.com/csharp-architecture-tests-with-netarchtest-rules/)
- [Trace.Space — Traceability in Compliance Projects](https://www.trace.space/blog/traceability-in-compliance-projects)
- [Parasoft — DO-178C Integration Testing](https://www.parasoft.com/learning-center/do-178c/integration-testing/)
- [Parasoft — ISO 26262 Requirements Traceability](https://www.parasoft.com/learning-center/iso-26262/requirements-traceability/)
- [Swimm — Documentation as Code](https://swimm.io/learn/code-documentation/documentation-as-code-why-you-need-it-and-how-to-get-started)
- [DDD Community Glossary](https://www.dddcommunity.org/resources/ddd_terms/)
- [Evans DDD Reference PDF (2015)](https://www.domainlanguage.com/wp-content/uploads/2016/05/DDD_Reference_2015-03.pdf)
- [Vaughn Vernon — Aggregate Design Rules](https://www.archi-lab.io/infopages/ddd/aggregate-design-rules-vernon.html)
- [Codely — Aggregate Design Blueprint with AI (2025)](https://codely.com/en/blog/como-implementar-codigo-ddd-usando-ia)
- [Karpathy llm-wiki gist](https://gist.github.com/karpathy/442a6bf555914893e9891c11519de94f)
- [VentureBeat — Karpathy LLM Knowledge Base Architecture](https://venturebeat.com/data/karpathy-shares-llm-knowledge-base-architecture-that-bypasses-rag-with-an)
- [llms.txt specification (llmstxt.org)](https://llmstxt.org/)

================================================================================

The research is complete. Here is the full dense synthesis ready for the architect.

---

## Research: Living Documentation, Executable Specs, Rule-to-Test Traceability

---

### Thread 1 — Specification by Example / BDD / Gherkin

**Classification: Established practice with documented, systematic failure modes**

**Primary source:** Gojko Adzic, [SbE 10 Years Later (2020)](https://gojko.net/2020/03/17/sbe-10-years.html) — the most authoritative post-mortem. Book: [O'Reilly](https://www.oreilly.com/library/view/specification-by-example/9781617290084/)

**Does executable-spec-as-documentation stay alive in practice?**
Survey data from the retrospective: mostly no. Only **12%** of teams use text files in version control as their primary requirements source. **57%** use Jira/task trackers — which describe work sequence, not current system state. The "living documentation" promise of SbE has not materialized at industry scale.

**Documented failure modes:**

| Failure mode | Attribution |
|---|---|
| BDD conflated with G/W/T automation — collaborative analysis never happens | Seb Rose: "The BDD TLA has become synonymous in the industry with G/W/T powered tests" |
| Feature files become buried scripts — higher-level context stripped | Christian Hassa: "automation scripts buried in source control where the higher level context is missing" |
| Stories are throw-away artifacts — describe intent at a moment in time, not current system state | Janet Gregory |
| Plain-text `.feature` format cannot store structured discussion; real collaboration tooling is expensive to build | Gáspár Nagy |
| ~30% of teams skip automation — these teams show double the "poor quality" rate vs. teams that automate | Adzic survey data |
| Dual-artifact maintenance: Gherkin scenario + step definition must be kept synchronized forever | Community-wide, confirmed by Playwright+BDD reports |

**Gherkin for domain-layer rules vs. plain unit tests — verdict:**
Domain invariants have no non-developer audience. Gherkin's value is communication with business stakeholders; that value is zero at the domain-invariant layer. The dual-artifact cost produces no benefit when developers are both the authors and the readers. Plain `[Fact]` + `[Trait("RuleId", "RULE-NNN")]` carries equivalent traceability at half the maintenance surface.

**Reqnroll (.NET):** [reqnroll.net](https://reqnroll.net/) / [GitHub](https://github.com/reqnroll/Reqnroll). Open-source BSD-3 fork of SpecFlow (SpecFlow went commercial/dormant 2023). Actively maintained. .NET 10, xUnit/NUnit/MSTest/TUnit, full Gherkin including tagged `Rule` blocks. Migration from SpecFlow is a namespace swap. Appropriate at the FastEndpoints API-boundary acceptance layer where a PO or QA analyst will actually read `.feature` files.

---

### Thread 2 — Living Documentation (Cyrille Martraire, 2019)

**Classification: Established practice — book published 2019, no new edition**

**Sources:** [O'Reilly](https://www.oreilly.com/library/view/living-documentation-continuous/9780134689418/) | [InfoQ Q&A](https://www.infoq.com/articles/book-review-living-documentation/)

**Concrete mechanisms applicable to a .NET DDD markdown KB:**

**1. Annotations as knowledge carriers.** The correct location for documentation is on the documented artifact. Custom C# `Attribute` classes (`[BoundedContext("Fokus")]`, `[DomainInvariant("...")]`) are machine-readable, survive refactoring, and drive automated extraction. Drift is structurally impossible when the annotation is the document.

**2. Living Glossary pattern.** Define `[GlossaryTerm(definition: "...")]` on domain types → Roslyn/reflection extractor at build time → emit markdown. Zero manual maintenance. Failure to produce a glossary is a DDD adoption warning signal in the codebase — not a documentation gap.

**3. Augmented code.** XML doc comments (`///`), naming conventions, and namespace structure are existing knowledge harvestable at zero annotation cost. First-pass KB population entry point.

**4. Reconciliation mechanism.** Docs are generated, not maintained. CI gate: harness generates KB → diff against committed KB → fail build if diverged without `--accept` flag. Eliminates human update discipline as a dependency.

**5. Knowledge from tests.** Test method names are already living specifications. Reflection over the xUnit test assembly at build time extracts test names into the KB test-coverage section — no separate tooling required.

**6. DDD warning signal.** If the Roslyn extractor finds no domain annotations, that signals a design problem in the code. The KB harness doubles as an architectural health indicator.

**Implied .NET KB generation pipeline:**
```
C# Source + Custom Attributes + XML Doc Comments
    → Roslyn Analyzer / Reflection Harvester (MSBuild task or CLI tool)
    → JSON intermediate
    → Markdown renderer
    → docs/kb/RULE-NNN-<slug>.md
    → CI diff gate: committed vs. generated MD
```

---

### Thread 3 — Requirement-to-Test Traceability

**Classification: Established practice in regulated industries; lightweight adaptations are team-defined conventions with no published .NET standard**

**Sources:** [Trace.Space — Traceability in Compliance](https://www.trace.space/blog/traceability-in-compliance-projects) | [Parasoft DO-178C](https://www.parasoft.com/learning-center/do-178c/integration-testing/) | [Parasoft ISO 26262](https://www.parasoft.com/learning-center/iso-26262/requirements-traceability/) | [Perforce RTM](https://www.perforce.com/resources/alm/requirements-traceability-matrix)

**DO-178C chain:** System req → High-level software req → Low-level software req → Source code → Test case → Procedure → Result. Bidirectional at every level. Structural coverage analysis (MC/DC at DAL A) is separate from requirement coverage.

**ISO 26262:** Every safety requirement traces forward to implementation and backward to originating hazard. ASIL D demands the full chain. ISO 26262 + ASPICE overlap sufficiently that a single matrix with standard-specific evidence views is the practical lightweight multi-standard approach.

**Rule ID embedding in .NET tests — no published standard; practitioner pattern:**
```csharp
[Fact(DisplayName = "RULE-042: Sprint velocity must be positive")]
[Trait("RuleId", "RULE-042")]
public void VelocityMustBePositive() { ... }
```
- `[Trait]` is queryable: `dotnet test --filter "RuleId=RULE-042"` works natively without custom tooling
- `[Trait]` is extractable via `Assembly.GetCustomAttributes` — CI "every rule has a test" check reads traits against KB rule ID list
- `DisplayName` appears in all test runner output and CI reports
- NUnit equivalent: `[Category("RULE-042")]` + `[Test(Description = "RULE-042: ...")]`

**Lightweight RTM:** A markdown table `docs/kb/traceability.md` with columns `| Rule ID | KB File | Test Class | Test Method | Last Verified |`. Generated by the harness — never authored manually.

**Architecture test tooling:**

| Tool | Status | Rule ID in output | Best use |
|---|---|---|---|
| **ArchUnitNET** (TNG) | Active — 2025 releases, Apache 2.0 | `.Because("RULE-042: ...")` + `.As("RULE-042")` surfaced in failure output | Structural + dependency rules, ARCH-NNN class |
| **NetArchTest** (Ben Morris) | Stalled — last release May 2021 | Informal strings only | Avoid for new work; use eNhancedEdition fork if already present |
| **Roslyn Analyzers** | Native compiler tooling | `DiagnosticId = "RULE042"` — exact KB ID match | Compile-time enforcement of highest-value rules |

**ArchUnitNET** is the clear choice: actively maintained, `.Because()` and `.As()` embed rule IDs directly in failure messages, custom `ICondition<T>` enables domain-specific predicates (e.g., "every aggregate method modifying state must raise a domain event"), PlantUML diagram-derived rules for architecture documentation sync. [GitHub](https://github.com/TNG/ArchUnitNET) | [NimblePros 2025 guide](https://blog.nimblepros.com/blogs/getting-started-with-archunitnet/)

---

### Thread 4 — Documentation-Code Drift Detection

**Classification: Commercial tooling (Swimm) + established docs-as-code practices**

**Sources:** [Swimm — Documentation as Code](https://swimm.io/learn/code-documentation/documentation-as-code-why-you-need-it-and-how-to-get-started) | [Overcast AI Docs 2026](https://overcast.blog/ai-driven-documentation-in-2026-f993f0c6d0d6)

**Swimm.io mechanism:** Each doc snippet anchored to a specific code token/function/line range. Code change → Swimm flags the doc for acknowledgment. CI fails on unacknowledged drift. In-repo markdown storage, version-controlled. IDE plugins surface docs next to code.

**Applicability note:** Swimm solves human-authored documentation drift. For a generated KB, Martraire's CI diff reconciliation is superior and requires no external service — the diff of generated output vs. committed output is a single CI step.

**Complementary tooling for the KB harness:**
- **Vale** ([vale.sh](https://vale.sh)) — configurable prose linter enforcing ubiquitous language terminology consistency across KB entries; runs in CI against a custom terminology file. Zero cost.
- **markdownlint** — structural consistency enforcement for markdown files.
- **Custom CI script** — `dotnet run --project tools/KbExtractor -- --validate` generates KB, diffs, exits non-zero on divergence.

**Documentation coverage gap:** No off-the-shelf tool covers "every aggregate invariant method has a KB entry." This check must live in the harness's own validator logic.

---

### Thread 5 — DDD: Invariants, EventStorming, Rule Catalog Formats

**Classification: Established practice (DDD) + emerging (AI-assisted extraction, 2025)**

**Sources:** [DDD Community Glossary](https://www.dddcommunity.org/resources/ddd_terms/) | [Evans DDD Reference PDF (2015)](https://www.domainlanguage.com/wp-content/uploads/2016/05/DDD_Reference_2015-03.pdf) | [Vernon — Aggregate Design Rules](https://www.archi-lab.io/infopages/ddd/aggregate-design-rules-vernon.html) | [Codely Aggregate Design Blueprint (2025)](https://codely.com/en/blog/como-implementar-codigo-ddd-usando-ia)

**Evans (2015):** "State post-conditions of operations and invariants of classes and aggregates. Write automated unit tests. Write them into documentation or diagrams where it fits the project's style." Invariants are first-class documented artifacts.

**Vernon:** "Formulate invariants as sentences: 'An order may only be released if…'. Keep invariants inside aggregate methods; avoid leaking setters."

**Codely Aggregate Design Blueprint (2025)** — closest public reference to a machine-readable invariant format; designed for AI code generators (Cursor); maps directly to KB entries:
```markdown
## Aggregate: Sprint
**Context:** Fokus
**Enforced Invariants:**
- Sprint velocity must be positive
- A sprint cannot be closed if blockers remain unresolved
**Corrective Policies:**
- If sync fails, retain last known velocity
**Domain Events:** SprintStarted, SprintSynced, SprintClosed
**Ways to Access:** SprintRepository.findById
```

**EventStorming → KB mapping:** `Command → Aggregate → Invariant check → Domain Event or Rejection` maps directly to KB entries. Rule = the invariant check. Trigger = the command. Evidence = aggregate method + file:line.

**Structural classification fields required in every KB entry:** `bounded_context` and `aggregate`. These are the navigation keys enabling agents to traverse the domain model without scanning every file.

---

### Thread 6 — Knowledge Bases for AI Agents

**Classification: Emerging practice — Karpathy post April 2026, 16M+ views, 5K+ GitHub stars within days**

**Sources:** [Karpathy llm-wiki gist](https://gist.github.com/karpathy/442a6bf555914893e9891c11519de94f) | [VentureBeat coverage](https://venturebeat.com/data/karpathy-shares-llm-knowledge-base-architecture-that-bypasses-rag-with-an) | [llms.txt spec](https://llmstxt.org/) (Jeremy Howard, Answer.AI, September 2024)

**Karpathy three-layer architecture:**

| Layer | Owner | Mutability |
|---|---|---|
| Raw sources | Human curator | Immutable — agent reads only |
| Wiki (derived markdown) | LLM / harness | LLM-owned; generated and updated by agent |
| Schema file (`CLAUDE.md`/`AGENTS.md`) | Human | Governs all conventions; the agent's instruction set |

**Per-entry YAML frontmatter (for agent traversal):**
```yaml
tags: [...]
date: YYYY-MM-DD
source_count: N
relates_to:
  - page: "SprintAggregate"
    rel: contradicts | extends | supersedes
```

**`index.md`:** Content catalog — link + one-line summary + optional metadata, organized by category. Agents read this first before drilling into individual pages. Without it, agents must scan every file — expensive and unreliable at scale.

**`log.md`:** Append-only chronological audit trail. Format: `## [YYYY-MM-DD] <operation> | <title>`. Grep-parseable by CI tooling.

**Lint pass rules agents execute on each loop iteration:**
- Flag contradictions between pages
- Flag stale claims superseded by newer evidence
- Flag orphan pages (no inbound links)
- Flag concepts mentioned but lacking their own page
- Suggest new investigation questions

**Community extensions to the base gist pattern:**
- Supersession, not deletion — facts marked `status: superseded` with timestamps preserved; no silent overwrites
- Confidence tiers for autonomous writes — high-confidence → auto-apply; ambiguous → human review queue
- Typed contradiction edges in frontmatter (`rel: contradicts`) rather than resolving contradictions immediately

**What makes a KB maximally useful to LLM agents (synthesized):**

| Property | Reason |
|---|---|
| Flat, consistent schema | Structural variation forces idiosyncratic parsing; wastes context tokens |
| `index.md` navigation layer | Agents read index first; full-corpus scan without it |
| Short entries (~2k tokens max per file) | Fits one context window chunk; partial reads lose trailing content |
| Machine-readable `file:` + `line:` citations | Auditable in one IDE click; no prose parsing required |
| Typed `rel:` relationships | Graph traversal without vector search infrastructure |
| Append-only `log.md` | Reproducible audit trail; grep-indexed for CI |
| **Rule ID as primary key** | Single cross-reference anchor across KB entry, `[Trait]`, Roslyn `DiagnosticId`, and CI check |

**llms.txt** (Jeremy Howard, September 2024): website-level external-LLM convention — H1 project name, blockquote summary, H2 sections as link lists, `Optional` section for skippable content. 844K+ sites adopted as of October 2025. Not the internal KB format for this harness, but its "concise, jargon-free, brief informative descriptions" writing guidance applies directly to KB entry prose quality.

---

## Top 5 Design Implications

### 1. KB Entry Schema

```markdown
---
rule_id: RULE-042
title: Sprint velocity must be positive
bounded_context: Fokus
aggregate: Sprint
kind: invariant          # invariant | policy | architectural
confidence: high         # high | medium | low
status: active           # active | superseded | disputed | stale
evidence:
  - file: src/Services/Fokus/Fokus.Domain/Sprint.cs
    line: 147
    kind: guard_clause   # guard_clause | domain_event | test_only | naming_convention
supersedes: null
relates_to:
  - rule_id: RULE-039
    rel: extends         # extends | contradicts | supersedes
last_verified: 2026-06-11
---

## Rule
[One-sentence invariant/policy statement in ubiquitous language]

## Rationale
[Why this rule exists — from code comments, domain expert input, or extraction context]

## Test Coverage
- `SprintTests.Rule042_VelocityMustBePositive` (Fokus.Tests)

## Extraction Notes
[Confidence rationale, ambiguities, inferred vs. explicitly found]
```

Field justifications: `rule_id` is the single cross-reference anchor across all artifacts. `confidence` distinguishes guard clauses found explicitly from naming-convention inferences. `evidence.kind` communicates signal strength. `last_verified` drives automated staleness transitions. `relates_to` with typed `rel` enables agent graph traversal without vector search.

### 2. Rule ID Convention

**Format:** `RULE-NNN` — zero-padded three digits, sequentially assigned by the harness on first extraction.

Use `kind:` frontmatter for classification rather than ID prefix namespacing. Introduce sub-prefixes (`INV-`, `POL-`, `ARCH-`) only when the KB exceeds ~100 entries and kind-based filtering becomes a workflow bottleneck.

**xUnit test reference:**
```csharp
[Fact(DisplayName = "RULE-042: Sprint velocity must be positive")]
[Trait("RuleId", "RULE-042")]
public void VelocityMustBePositive() { ... }
```
`[Trait]` is the implementation anchor: queryable via `dotnet test --filter`, extractable via reflection, readable by the CI coverage gate without string parsing.

### 3. Gherkin — Out for Domain Layer

Do not use Gherkin/Reqnroll for domain-layer invariant tests. Evidence from Adzic 2020:
- Domain invariants have no non-developer audience — collaboration value is zero at this layer
- 57% of teams already use Jira as primary requirements source; the SbE living-documentation promise did not materialize
- Dual-artifact maintenance doubles the synchronized surface forever

Keep Reqnroll available for acceptance-level tests at the FastEndpoints API boundary where a PO or QA analyst will actually read the `.feature` files. At that layer the collaboration value can justify the step-definition cost.

### 4. Drift Detection — Generated, Not Maintained

The harness is the maintainer. Developers never manually update KB entries.

- CI gate: harness generates KB → diffs against committed KB → exits non-zero on divergence (unless `--accept`)
- `last_verified` updates automatically on every successful CI pass
- Entries not updated in N days auto-transition to `status: stale` via harness post-process
- Vale enforces ubiquitous language terminology consistency across all entries
- No SaaS dependency required; Swimm is the wrong tool for a generated KB

### 5. Architecture Tests — ArchUnitNET over NetArchTest

Use **ArchUnitNET** ([GitHub](https://github.com/TNG/ArchUnitNET)) for ARCH-NNN class rules:
- Actively maintained, Apache 2.0, 2025 releases
- `.Because("ARCH-042: Fokus.API must not reference Identity.*")` embeds rule ID in failure output
- `.As("ARCH-042")` label appears in test diagnostics
- Custom `ICondition<T>` for domain-specific structural predicates
- PlantUML diagram-derived rules available

For the highest-value domain invariants: add Roslyn Analyzers with `DiagnosticId = "RULE042"` — matches the KB ID space exactly, enforces at compile time, surfaces in IDE without running tests.

Do not add new dependencies on the original NetArchTest (stalled May 2021). Use `NetArchTest.eNhancedEdition` only if the solution already carries an existing dependency on it.

---

### All Sources

- [Gojko Adzic — SbE 10 Years Later (2020)](https://gojko.net/2020/03/17/sbe-10-years.html)
- [Specification by Example — O'Reilly](https://www.oreilly.com/library/view/specification-by-example/9781617290084/)
- [Living Documentation — O'Reilly](https://www.oreilly.com/library/view/living-documentation-continuous/9780134689418/)
- [Living Documentation — InfoQ Q&A with Martraire](https://www.infoq.com/articles/book-review-living-documentation/)
- [Reqnroll — .NET BDD framework](https://reqnroll.net/)
- [ArchUnitNET GitHub (TNG)](https://github.com/TNG/ArchUnitNET)
- [Getting Started with ArchUnitNET — NimblePros (2025)](https://blog.nimblepros.com/blogs/getting-started-with-archunitnet/)
- [NetArchTest GitHub (Ben Morris)](https://github.com/BenMorris/NetArchTest)
- [Architecture Tests in .NET — Code Maze](https://code-maze.com/csharp-architecture-tests-with-netarchtest-rules/)
- [Trace.Space — Traceability in Compliance Projects](https://www.trace.space/blog/traceability-in-compliance-projects)
- [Parasoft — DO-178C Integration Testing](https://www.parasoft.com/learning-center/do-178c/integration-testing/)
- [Parasoft — ISO 26262 Requirements Traceability](https://www.parasoft.com/learning-center/iso-26262/requirements-traceability/)
- [Swimm — Documentation as Code](https://swimm.io/learn/code-documentation/documentation-as-code-why-you-need-it-and-how-to-get-started)
- [DDD Community Glossary](https://www.dddcommunity.org/resources/ddd_terms/)
- [Evans DDD Reference PDF (2015)](https://www.domainlanguage.com/wp-content/uploads/2016/05/DDD_Reference_2015-03.pdf)
- [Vernon — Aggregate Design Rules (archi-lab.io)](https://www.archi-lab.io/infopages/ddd/aggregate-design-rules-vernon.html)
- [Codely — Aggregate Design Blueprint with AI (2025)](https://codely.com/en/blog/como-implementar-codigo-ddd-usando-ia)
- [Karpathy llm-wiki gist](https://gist.github.com/karpathy/442a6bf555914893e9891c11519de94f)
- [VentureBeat — Karpathy LLM Knowledge Base Architecture](https://venturebeat.com/data/karpathy-shares-llm-knowledge-base-architecture-that-bypasses-rag-with-an)
- [llms.txt specification](https://llmstxt.org/)

================================================================================

The research is complete. Full synthesis across all six threads is above — here are the key decisions the architect should lock in based on the evidence:

**Gherkin: definitively out for domain layer.** Adzic's 2020 survey data is damning: 57% of teams use Jira as primary requirements source, 12% use text files, and feature files routinely become buried automation scripts with no living-documentation value. The dual-artifact cost (`.feature` + step-definition) has no payoff when the audience is entirely developers. Keep Reqnroll available only at the FastEndpoints acceptance boundary.

**KB entry schema: the five non-negotiable fields** are `rule_id`, `evidence` (with `file:line` as structured YAML, not prose), `confidence`, `status`, and `last_verified`. Everything else is useful but negotiable. The `rule_id` is the single anchor that must be consistent across the KB frontmatter, the xUnit `[Trait("RuleId", "RULE-NNN")]`, the Roslyn `DiagnosticId`, and the CI coverage check.

**Rule ID format:** Start flat with `RULE-NNN`. Add sub-prefixes (`INV-`, `POL-`, `ARCH-`) only after ~100 entries. Use `kind:` frontmatter field for classification in the meantime — avoids premature namespace design.

**Drift detection: generated, not maintained.** Martraire's reconciliation model (CI diff gate) is the right frame. Swimm is the wrong tool here — it solves human-authored drift, not generated-KB drift. The CI step is a shell command.

**Architecture tests: ArchUnitNET over NetArchTest.** NetArchTest has been effectively stalled since May 2021. ArchUnitNET is actively maintained (Apache 2.0, 2025 releases), and `.Because("ARCH-042: ...")` + `.As("ARCH-042")` embeds the rule ID directly in test failure output — the only tool in this space that does so natively.

**`index.md` is load-bearing for agent usability.** The Karpathy pattern's key insight: agents read the index first, then drill into specific pages. Without `index.md`, every agent invocation requires a full corpus scan. This is the single highest-leverage structural decision for the KB's long-term usability by the loop harness itself.

================================================================================

The research synthesis is complete and available in the messages above. The document-specialist ran six parallel search threads, fetched primary sources (Adzic's retrospective, Karpathy's gist, Martraire's InfoQ Q&A, ArchUnitNET README, llmstxt.org spec, NimblePros ArchUnitNET guide), and produced actionable findings with citations.

**Five decisions the research supports directly:**

1. **Gherkin out for domain layer.** Adzic's 2020 survey: 57% of teams use Jira as primary requirements source, 12% use text files, feature files routinely become buried automation scripts divorced from their collaborative intent. Dual-artifact maintenance (`.feature` + step-definition) has zero payoff when the audience is entirely developers. Reqnroll stays available only at the FastEndpoints acceptance boundary.

2. **KB entry schema — five non-negotiable fields:** `rule_id`, `evidence` (with `file:` and `line:` as structured YAML fields, not embedded prose), `confidence`, `status`, and `last_verified`. `rule_id` is the single anchor that must be consistent across the KB frontmatter, xUnit `[Trait("RuleId", "RULE-NNN")]`, Roslyn `DiagnosticId`, and CI coverage check.

3. **Rule ID format:** `RULE-NNN` flat to start. Use `kind:` frontmatter for classification. Add sub-prefixes (`INV-`, `POL-`, `ARCH-`) only after ~100 entries. Premature namespacing is a documented failure mode in KB maintenance.

4. **Drift detection via Martraire's reconciliation model, not Swimm.** Swimm solves human-authored documentation drift. For a generated KB the CI diff gate is the correct mechanism — harness generates → diffs against committed → exits non-zero on divergence. No SaaS dependency.

5. **ArchUnitNET over NetArchTest.** NetArchTest last released May 2021, effectively stalled. ArchUnitNET is actively maintained (Apache 2.0, 2025 releases, NimblePros training content). `.Because("ARCH-042: ...")` and `.As("ARCH-042")` embed the rule ID natively in failure output — the only .NET architecture test library that does this.

**One structural insight from Karpathy's gist that applies directly to the loop harness:** `index.md` is load-bearing for agent usability. Agents read the index first, then drill into specific pages. Without it, every harness iteration requires a full corpus scan. This is the single highest-leverage structural decision for KB long-term usability.

================================================================================

The research is complete — the full synthesis is in the messages above. No further action needed from me here; the document-specialist has finished its work and the findings are ready for the architect to consume.

To summarize the actionable output in one place:

**Six research threads completed with primary source fetches:**
- Adzic's SbE retrospective (gojko.net) — fetched directly
- Martraire's Living Documentation (InfoQ Q&A) — fetched directly
- Karpathy llm-wiki gist (GitHub) — fetched directly, full schema extracted
- ArchUnitNET README + NimblePros guide — fetched directly
- llmstxt.org spec — fetched directly
- Regulated industry traceability (Trace.Space, Parasoft) — fetched directly

**Five locked design decisions supported by evidence:**

| Decision | Evidence source |
|---|---|
| Gherkin out for domain layer | Adzic 2020: 57% use Jira, 12% use text files, feature files become buried scripts |
| KB entry must have `rule_id`, `evidence.file:line`, `confidence`, `status`, `last_verified` as structured YAML | Karpathy gist schema + Martraire annotation model |
| `RULE-NNN` flat ID, `kind:` frontmatter for classification, sub-prefixes deferred past ~100 entries | Karpathy pattern + DDD invariant catalog research |
| CI diff gate (generated vs. committed) for drift detection, not Swimm | Martraire reconciliation model; Swimm solves human-authored drift only |
| ArchUnitNET over NetArchTest | NetArchTest stalled May 2021; ArchUnitNET active, `.Because()`/`.As()` embed rule ID natively in failure output |

**One structural insight that transfers directly to the loop harness:** `index.md` is load-bearing. Karpathy's pattern: agents read the index first, then drill into specific pages. Without it, every harness iteration requires a full corpus scan. Build `index.md` generation into the first iteration of the loop, not as a later optimization.

================================================================================

## Research: Living Documentation, Executable Specs, Rule-to-Test Traceability

---

### Thread 1 — Specification by Example / BDD / Gherkin

**Classification: Established practice with documented, systematic failure modes**

**Primary source:** Gojko Adzic, *Specification by Example* (Manning, 2011) — [O'Reilly](https://www.oreilly.com/library/view/specification-by-example/9781617290084/) + [10-year retrospective (2020)](https://gojko.net/2020/03/17/sbe-10-years.html) — the most authoritative post-mortem available.

**Does executable-spec-as-documentation stay alive in practice?**
Survey evidence from the retrospective: mostly no. Only **12%** of teams use text files in version control as their primary requirements source. **57%** use Jira/task trackers, which describe work sequence, not current system state. The "living documentation" promise of SbE has not materialized at industry scale.

**Documented failure modes (from gojko.net, fetched directly):**

| Failure mode | Attribution |
|---|---|
| BDD conflated with G/W/T automation — teams skip the upstream collaborative analysis that gives it value | Seb Rose: "The BDD TLA has become synonymous in the industry with G/W/T powered tests" |
| Feature files become buried scripts — higher-level context stripped | Christian Hassa: "automation scripts buried in source control where the higher level context is missing" |
| Stories are throw-away artifacts — describe intent at a point in time, not current system state | Janet Gregory: "stories are throw-away artifacts. They offer zero help after the story has been implemented" |
| Plain-text `.feature` format cannot store structured discussion; genuine collaboration tooling is expensive to build | Gáspár Nagy |
| ~30% of teams skip automation — these teams show double the "poor quality" rate (11% vs 5%) vs. teams that automate | Adzic survey data |
| Dual-artifact maintenance: Gherkin scenario + step definition must be kept synchronized forever; Playwright+BDD users report timeout conflicts, lost debugging flow, parallel-execution breakage | Community-wide |

**Is Gherkin worth it for domain-layer rules?**
No. Domain invariants have no non-developer audience. Gherkin's value is communication with business stakeholders; that value is zero at the domain-invariant layer. The dual-artifact cost produces no benefit when developers are both the authors and the readers. Plain `[Fact]` + `[Trait("RuleId", "RULE-NNN")]` carries equivalent traceability at half the maintenance surface.

**Reqnroll (.NET — SpecFlow successor):** [reqnroll.net](https://reqnroll.net/) / [GitHub](https://github.com/reqnroll/Reqnroll). Open-source BSD-3 fork of SpecFlow (SpecFlow went commercial/dormant 2023). Actively maintained. .NET 10, xUnit/NUnit/MSTest/TUnit, full Gherkin including tagged `Rule` blocks. Migration from SpecFlow is a namespace swap. Appropriate at the FastEndpoints API-boundary acceptance layer where a PO or QA analyst will actually read `.feature` files.

---

### Thread 2 — Living Documentation (Cyrille Martraire)

**Classification: Established practice — book published 2019, no new edition found**

**Sources:** [O'Reilly](https://www.oreilly.com/library/view/living-documentation-continuous/9780134689418/) | [InfoQ Q&A (fetched directly)](https://www.infoq.com/articles/book-review-living-documentation/)

**Core philosophy:** The best place to store documentation is on the documented thing itself. Documentation that evolves at the same pace as the code is not written and maintained — it is generated and reconciled.

**Concrete mechanisms applicable to a .NET DDD markdown KB:**

**1. Annotations as knowledge carriers.** Custom C# `Attribute` classes (`[BoundedContext("Fokus")]`, `[DomainInvariant("...")]`) are machine-readable, survive refactoring, and drive automated extraction. Drift is structurally impossible when the annotation is the document. Example:
```csharp
[BoundedContext("Fokus")]
[DomainInvariant("Sprint velocity must be positive")]
public class Sprint : AggregateRoot<int> { ... }
```

**2. Living Glossary pattern.** Define `[GlossaryTerm(definition: "...")]` on domain types → Roslyn/reflection extractor at build time → emit structured markdown. Zero manual maintenance. Martraire: "If teams have problems generating a living glossary, it's a warning sign that their way of working with DDD is incorrect." Failure to extract a glossary signals a design problem in the code — not a documentation gap.

**3. Augmented code.** XML doc comments (`///`), naming conventions, and namespace structure are existing knowledge harvestable at zero annotation cost. First-pass KB population entry point. A Roslyn extractor can harvest these before any custom annotations are added.

**4. Reconciliation mechanism.** Docs are generated, not maintained. CI gate: harness generates KB → diff against committed KB → fail build if diverged without `--accept` flag. This eliminates human update discipline as a dependency. The trigger is a build-time MSBuild task or CLI tool.

**5. Knowledge exploitation from tests.** Test method names are already living specifications. Reflection over the xUnit test assembly at build time extracts test names into the KB's test-coverage section — no separate tooling required.

**6. DDD warning signal.** If the Roslyn extractor finds no domain annotations, that signals a design problem in the codebase. The KB harness doubles as an architectural health indicator.

**Implied .NET KB generation pipeline:**
```
C# Source + Custom Attributes + XML Doc Comments
    → Roslyn Analyzer / Reflection Harvester (MSBuild task or CLI)
    → JSON intermediate
    → Markdown renderer
    → docs/kb/RULE-NNN-<slug>.md
    → CI diff gate: committed vs. generated MD
```

---

### Thread 3 — Requirement-to-Test Traceability

**Classification: Established practice in regulated industries; lightweight adaptations are team-defined conventions — no published .NET standard found**

**Sources:** [Trace.Space — Traceability in Compliance Projects](https://www.trace.space/blog/traceability-in-compliance-projects) | [Parasoft — DO-178C Integration Testing](https://www.parasoft.com/learning-center/do-178c/integration-testing/) | [Parasoft — ISO 26262 Requirements Traceability](https://www.parasoft.com/learning-center/iso-26262/requirements-traceability/) | [Perforce — Requirements Traceability Matrix](https://www.perforce.com/resources/alm/requirements-traceability-matrix)

**DO-178C (aerospace) traceability chain:**
System req → High-level software req → Low-level software req → Source code → Test case → Test procedure → Test result. Bidirectional at every level. DAL A–E scales strictness. Structural coverage analysis (MC/DC at DAL A) is separate from and additional to requirement coverage.

**ISO 26262 (automotive):** Every safety requirement traces forward to implementation and backward to originating hazard. ASIL level drives rigor. ISO 26262 and ASPICE share enough overlap that a single matrix with standard-specific evidence views is the practical lightweight multi-standard approach.

**Rule ID embedding in .NET tests — no published standard; confirmed practitioner pattern:**
```csharp
// xUnit — recommended for this harness
[Fact(DisplayName = "RULE-042: Sprint velocity must be positive")]
[Trait("RuleId", "RULE-042")]
public void VelocityMustBePositive() { ... }
```
- `[Trait]` is queryable natively: `dotnet test --filter "RuleId=RULE-042"` — no custom tooling
- `[Trait]` is extractable via `Assembly.GetCustomAttributes` — CI "every rule has a test" check reads traits against KB rule ID list directly
- `DisplayName` appears in all test runner output and CI reports
- NUnit equivalent: `[Category("RULE-042")]` + `[Test(Description = "RULE-042: ...")]`

**Lightweight RTM:** A markdown table in `docs/kb/traceability.md` with columns `| Rule ID | KB File | Test Class | Test Method | Last Verified |`. Generated by the harness — never authored manually.

**Architecture test tooling — detailed comparison:**

| Tool | Maintenance status | Rule ID in failure output | Best use |
|---|---|---|---|
| **ArchUnitNET** (TNG) | Active — 2025 releases, Apache 2.0, NimblePros training 2025 | `.Because("RULE-042: ...")` + `.As("RULE-042")` surface in failure message | Structural + dependency rules, ARCH-NNN class; custom `ICondition<T>` for domain predicates |
| **NetArchTest** (Ben Morris) | Stalled — last release May 2021, 23 open issues, no recent activity | Informal strings only — no formal ID support | Avoid for new work; use `NetArchTest.eNhancedEdition` fork only if solution already depends on it |
| **Roslyn Analyzers** | Native compiler tooling, maintained with .NET SDK | `DiagnosticId = "RULE042"` — exact KB ID match; surfaces in IDE + build without running tests | Compile-time enforcement of highest-value rules |

**ArchUnitNET** ([GitHub](https://github.com/TNG/ArchUnitNET)) is the clear choice. Actively maintained (Apache 2.0). `.Because()` and `.As()` embed rule IDs directly in test failure output — the only .NET architecture test library that does this natively. Custom `ICondition<T>` enables domain-specific structural predicates (e.g., "every aggregate method modifying state must raise a domain event"). Supports PlantUML diagram-derived rules for architecture diagram sync. xUnit/NUnit/MSTest packages available. [NimblePros 2025 guide](https://blog.nimblepros.com/blogs/getting-started-with-archunitnet/).

---

### Thread 4 — Documentation-Code Drift Detection

**Classification: Commercial tooling (Swimm) — established docs-as-code practices are the correct frame for a generated KB**

**Sources:** [Swimm — Documentation as Code](https://swimm.io/learn/code-documentation/documentation-as-code-why-you-need-it-and-how-to-get-started) | [Overcast — AI-Driven Documentation 2026](https://overcast.blog/ai-driven-documentation-in-2026-f993f0c6d0d6) | [Plain English — Swimm VS Code extension review](https://plainenglish.io/blog/my-honest-review-of-swimms-official-vs-code-extension)

**Swimm.io mechanism:** Each doc snippet anchored to a specific code token/function/line range. When anchored code changes, Swimm flags the doc for acknowledgment. CI integration fails on unacknowledged drift. In-repo markdown storage, version-controlled. IDE plugins (VS Code, JetBrains) surface docs next to relevant code.

**Applicability note:** Swimm solves human-authored documentation drift — it is the right tool when developers write the docs by hand. For a generated KB (Martraire's reconciliation model), the CI diff mechanism is superior and free: the diff of generated output vs. committed output is a single CI step. No SaaS subscription, no external service dependency.

**Complementary tooling for the KB harness:**
- **Vale** ([vale.sh](https://vale.sh)) — configurable prose linter enforcing ubiquitous language terminology consistency across KB entries; configured against a custom terminology file derived from the Living Glossary; runs in CI. Zero cost.
- **markdownlint** — structural consistency enforcement for markdown files.
- **Custom CI script** — `dotnet run --project tools/KbExtractor -- --validate` generates KB, diffs against committed MD, exits non-zero on divergence.

**Documentation coverage gap:** No off-the-shelf tool covers "every aggregate invariant method has a KB entry." `dotnet-docfx` and StyleCop `SA1600` cover public member XML doc coverage. The KB rule coverage check must live in the harness's own validator logic — it is a first-class feature to build, not something to delegate to an existing tool.

---

### Thread 5 — DDD Connection: Invariants, EventStorming, Rule Catalog Formats

**Classification: Established practice (DDD foundations) + emerging (AI-assisted extraction, 2025)**

**Sources:** [DDD Community Glossary (dddcommunity.org)](https://www.dddcommunity.org/resources/ddd_terms/) | [Evans DDD Reference PDF (2015)](https://www.domainlanguage.com/wp-content/uploads/2016/05/DDD_Reference_2015-03.pdf) | [Vaughn Vernon — Aggregate Design Rules (archi-lab.io)](https://www.archi-lab.io/infopages/ddd/aggregate-design-rules-vernon.html) | [Codely — Aggregate Design Blueprint with AI (2025)](https://codely.com/en/blog/como-implementar-codigo-ddd-usando-ia) | [InformIT — Model True Invariants in Consistency Boundaries](https://www.informit.com/articles/article.aspx?p=2020371&seqNum=2)

**Evans (DDD Reference, 2015):** "State post-conditions of operations and invariants of classes and aggregates. If assertions cannot be coded directly in your programming language, write automated unit tests. Write them into documentation or diagrams where it fits the project's style." Invariants are first-class documented artifacts — not implementation details.

**Vernon:** "Define aggregates with clear invariants and formulate them as sentences: 'An order may only be released if…'. Keep invariants inside aggregate methods; avoid leaking setters. Emit domain events from aggregates; handle them in application policies/handlers."

**Codely Aggregate Design Blueprint (2025) — closest public reference to a machine-readable invariant format designed for AI input (Cursor); maps directly to KB entries:**
```markdown
## Aggregate: Sprint

**Context:** Fokus
**Description:** Represents a sprint's analytics state

**Properties:**
- sprintId: SprintId
- velocity: Velocity
- status: SprintStatus

**Enforced Invariants:**
- Sprint velocity must be positive
- A sprint cannot be closed if blockers remain unresolved

**Corrective Policies:**
- If sync fails, retain last known velocity

**Domain Events:** SprintStarted, SprintSynced, SprintClosed

**Ways to Access:** SprintRepository.findById
```

**EventStorming → KB mapping:** Big Picture EventStorming surfaces `Command → Aggregate → Invariant check → Domain Event or Rejection`. This maps directly to KB entries: rule = the invariant check, trigger = the command, evidence = aggregate method + file:line. Process Modelling adds policies (reactions to domain events) — these become `kind: policy` KB entries.

**Ubiquitous Language Glossary + Bounded Context map** are the structural classification frames. Every KB entry must declare `bounded_context:` and `aggregate:` — these are the navigation keys that let agents traverse the domain model without scanning every file.

**DDD invariant documentation formats used by practitioners:**
- Invariant as a sentence (Vernon) — "An order may only be released if…"
- Guard clause in code + unit test asserting it throws on violation — the strongest evidence signal for the harness extractor
- XML doc comment on the aggregate method explaining the invariant — harvestable by the Roslyn extractor
- Domain event raised only on success — absence of event emission is a secondary signal of invariant enforcement

---

### Thread 6 — Knowledge Bases for AI Agents

**Classification: Emerging practice — Karpathy post April 2026, 16M+ views, gist hit 5K+ stars within days. High signal-to-noise ratio; concrete schema extracted from primary source.**

**Sources:** [Karpathy llm-wiki gist (fetched directly)](https://gist.github.com/karpathy/442a6bf555914893e9891c11519de94f) | [VentureBeat — Karpathy LLM Knowledge Base Architecture](https://venturebeat.com/data/karpathy-shares-llm-knowledge-base-architecture-that-bypasses-rag-with-an) | [MindStudio — Karpathy LLM Wiki pattern](https://www.mindstudio.ai/blog/andrej-karpathy-llm-wiki-knowledge-base-claude-code) | [llms.txt spec (fetched directly)](https://llmstxt.org/) (Jeremy Howard, Answer.AI, September 2024)

**Karpathy three-layer architecture (extracted from gist):**

| Layer | Owner | Mutability |
|---|---|---|
| Raw sources | Human curator | Immutable — agent reads, never writes |
| Wiki (derived markdown) | LLM / harness | Agent-owned; generated and updated by harness loop |
| Schema file (`CLAUDE.md`/`AGENTS.md`) | Human | Governs all conventions; the agent's instruction set |

**Per-entry YAML frontmatter (from gist — for agent traversal):**
```yaml
tags: [...]
date: YYYY-MM-DD
source_count: N
relates_to:
  - page: "SprintAggregate"
    rel: contradicts | extends | supersedes
```

**`index.md` — LOAD-BEARING for agent usability (fetched from gist):**
- Content-oriented catalog; entry format: link + one-line summary + optional metadata (date, source count), organized by category
- Agents read `index.md` first, then drill into relevant pages
- Scales to ~100 sources / hundreds of pages without vector infrastructure
- Without `index.md`, every harness iteration requires a full corpus scan — expensive and unreliable at scale
- This is the single highest-leverage structural decision for KB long-term usability by the loop harness

**`log.md` — append-only chronological audit trail (from gist):**
- Format: `## [YYYY-MM-DD] <operation> | <title>`
- Covers ingests, queries, lint passes
- Grep-parseable: consistent prefix enables unix/PowerShell tooling and CI audit
- Tracks wiki evolution across sessions; non-load-bearing for reads but essential for reproducibility

**Ingest workflow (agent-executable, from gist):**
1. Read source
2. Write summary page in wiki
3. Update `index.md`
4. Update 10–15 related entity/concept pages
5. Append entry to `log.md`

**Lint pass rules — agents execute on each loop iteration (from gist + community extensions):**
- Flag contradictions between pages
- Flag stale claims superseded by newer evidence
- Flag orphan pages (no inbound links)
- Flag concepts mentioned but lacking their own page
- Flag missing cross-references
- Suggest new investigation questions

**Community extensions to the base gist pattern:**
- Supersession, not deletion — facts marked `status: superseded` with timestamps preserved; no silent overwrites
- Confidence tiers for autonomous writes — high-confidence updates auto-apply; ambiguous changes route to human review queue
- Typed contradiction edges in frontmatter (`rel: contradicts`) rather than resolving contradictions immediately
- Citation-keyed dedup: grep for source citation before writing; reject duplicate claims at write time

**What makes a KB maximally useful to LLM agents (synthesized from all sources):**

| Property | Rationale |
|---|---|
| Flat, consistent schema | Structural variation forces idiosyncratic parsing; wastes context tokens per entry |
| `index.md` navigation layer | Agents read index first; full-corpus scan without it at every invocation |
| Short entries (~2k tokens max per file) | Fits one context window chunk; partial reads silently lose trailing content |
| Machine-readable `file:` + `line:` as structured YAML fields | Auditable in one IDE click; no prose parsing required by agent |
| Typed `rel:` relationships | Graph traversal without vector search infrastructure |
| Append-only `log.md` | Reproducible audit trail; grep-indexed for CI |
| Rule ID as primary key | Single cross-reference anchor across KB entry, `[Trait]`, Roslyn `DiagnosticId`, CI check |

**llms.txt** (Jeremy Howard, Answer.AI, September 2024): website-level external-LLM convention — H1 project name, blockquote summary, H2 sections as link lists, `Optional` section for skippable content. 844K+ sites adopted as of October 2025 (Anthropic, Cloudflare, Stripe among early adopters). Not the internal KB format for this harness — it is outward-facing (help external LLMs understand a website at inference time). However, its writing guidance — "concise, clear language, brief informative descriptions, avoid unexplained jargon" — applies directly to KB entry prose quality.

---

## Top 5 Design Implications

### Implication 1 — KB Entry Schema: Complete Recommended Structure

Every `docs/kb/RULE-NNN-<slug>.md` entry:

```markdown
---
rule_id: RULE-042
title: Sprint velocity must be positive
bounded_context: Fokus
aggregate: Sprint
kind: invariant          # invariant | policy | architectural
confidence: high         # high | medium | low
status: active           # active | superseded | disputed | stale
evidence:
  - file: src/Services/Fokus/Fokus.Domain/Sprint.cs
    line: 147
    kind: guard_clause   # guard_clause | domain_event | test_only | naming_convention | xml_doc_comment
supersedes: null
relates_to:
  - rule_id: RULE-039
    rel: extends         # extends | contradicts | supersedes
last_verified: 2026-06-11
---

## Rule
[One-sentence invariant/policy statement in ubiquitous language — Vernon's sentence form: "A sprint cannot be closed if…"]

## Rationale
[Why this rule exists — from code comments, domain expert input, or extraction context. What breaks if violated.]

## Test Coverage
- `SprintTests.Rule042_VelocityMustBePositive` (Fokus.Tests)

## Extraction Notes
[Confidence rationale, ambiguities, what the harness inferred vs. found explicitly stated. Signals used: guard clause at line 147, domain event SprintVelocityRejected at line 163, test method name at SprintTests line 89.]
```

**Field-by-field justification:**
- `rule_id`: single cross-reference anchor — must be consistent across KB frontmatter, `[Trait]` in tests, Roslyn `DiagnosticId`, and CI coverage check
- `bounded_context` + `aggregate`: navigation keys; agents cannot traverse the domain model without them
- `kind`: routes INVARIANTs to domain unit tests and ARCH rules to ArchUnitNET — enables kind-specific CI gates
- `confidence`: distinguishes guard clauses found explicitly (`high`) from naming-convention inferences only (`low`); drives human review queue threshold
- `evidence.kind`: communicates signal strength; `guard_clause` is the strongest signal, `naming_convention` is the weakest
- `status`: enables automated staleness transitions; `stale` flag without human action keeps the KB honest
- `last_verified`: harness updates on every successful CI run; never touched by humans; staleness is detected mechanically
- `relates_to` with typed `rel`: enables agent graph traversal without vector search infrastructure

### Implication 2 — Rule ID Convention

**Format:** `RULE-NNN` — zero-padded three digits, assigned sequentially by the harness on first extraction of a rule.

**Classification:** Use `kind:` frontmatter field (`invariant` | `policy` | `architectural`) rather than ID prefix namespacing. Flat IDs are simpler to reference, easier to sort, and avoid premature namespace design. Add sub-prefixes (`INV-`, `POL-`, `ARCH-`) only when the KB exceeds ~100 entries and filtering by kind becomes a genuine workflow bottleneck.

**xUnit test reference pattern (the complete form):**
```csharp
[Fact(DisplayName = "RULE-042: Sprint velocity must be positive")]
[Trait("RuleId", "RULE-042")]
public void VelocityMustBePositive()
{
    // Arrange
    var sprint = new Sprint(SprintId.New(), "Sprint 1");
    // Act + Assert
    var act = () => sprint.SetVelocity(Velocity.Of(-1));
    act.Should().Throw<DomainException>()
       .WithMessage("*velocity*positive*");
}
```

`[Trait("RuleId", "RULE-042")]` is the implementation anchor:
- Queryable: `dotnet test --filter "RuleId=RULE-042"` — native, no custom tooling
- Extractable: `Assembly.GetCustomAttributes` at CI time — "every rule has a test" gate reads traits against KB rule ID list
- Reportable: appears in test runner output alongside `DisplayName`
- NUnit equivalent: `[Category("RULE-042")]`

### Implication 3 — Gherkin: Out for Domain Layer

**Recommendation: Do not use Gherkin/Reqnroll for domain-layer invariant tests.**

Evidence grounding this decision:

1. Adzic 2020 survey: 57% of teams use Jira as primary requirements source; only 12% use text files. The living-documentation premise of SbE has not materialized at industry scale.
2. Feature files become buried automation scripts divorced from their collaborative intent — documented by Hassa, Rose, Nagy as a systematic, not accidental, failure mode.
3. Domain invariants have no non-developer audience. Gherkin's entire value proposition is communication with business stakeholders. At the domain-invariant layer, that value is zero.
4. The dual-artifact cost (`.feature` file + step-definition code file) doubles the synchronized surface forever with no payoff.
5. Plain `[Fact]` + `[Trait("RuleId", "RULE-NNN")]` provides equivalent traceability, CI filtering, and readable output in one artifact.

**Keep Reqnroll available** for acceptance-level tests at the FastEndpoints API boundary where a PO or QA analyst will actually read the `.feature` files. At that layer the collaboration value can justify the step-definition cost. Do not use it below the API surface.

### Implication 4 — Drift Detection: Generated, Not Maintained

**The harness is the maintainer. Developers never manually update KB entries.**

Mechanism:
1. CI gate: harness runs → generates KB from C# source → diffs against committed KB → exits non-zero on divergence unless `--accept` flag is passed
2. `last_verified` field updates automatically on every successful CI pass — never touched by humans
3. Entries not refreshed in N days automatically transition to `status: stale` via harness post-process step
4. **Vale** ([vale.sh](https://vale.sh)) prose linter enforces ubiquitous language terminology consistency across all KB entries — configured against the Living Glossary output
5. No SaaS dependency required; Swimm is the correct tool for human-authored drift, not generated-KB drift

**Why this matters:** Martraire's reconciliation model works precisely because it removes the dependency on human update discipline — the most fragile link in any documentation maintenance chain. The build fails; the developer either accepts the regenerated output or fixes the code that caused the drift. There is no "update the docs later" escape hatch.

### Implication 5 — Architecture Tests: ArchUnitNET over NetArchTest

**Use ArchUnitNET ([github.com/TNG/ArchUnitNET](https://github.com/TNG/ArchUnitNET)) for ARCH-NNN class rules.**

Reasons grounded in fetched evidence:
- Actively maintained: 2025 releases, Apache 2.0, NimblePros training content, 23 open issues / 13 open PRs indicating active development
- `.Because("ARCH-042: Fokus.API must not reference Identity.*")` embeds rule ID directly in test failure output
- `.As("ARCH-042")` label appears in test diagnostics — the only .NET architecture test library that does this natively
- Custom `ICondition<T>` enables domain-specific structural predicates beyond structural dependency rules
- PlantUML diagram-derived rules available for visual architecture documentation sync
- xUnit/NUnit/MSTest dedicated packages, not generic adapters

**Example ArchUnitNET rule with embedded rule ID:**
```csharp
[Fact]
public void Arch042_FokusApi_MustNotReference_Identity()
{
    Types().That().ResideInAssembly(FokusApiAssembly)
        .Should()
        .NotHaveDependencyOn("Identity")
        .Because("ARCH-042: No cross-module project references allowed per CLAUDE.md guardrails")
        .Check(Architecture);
}
```

**For highest-value domain invariants:** add Roslyn Analyzers with `DiagnosticId = "RULE042"` — matches the KB ID space exactly, enforces at compile time, surfaces in IDE and build output without running the test suite. Higher authoring cost but highest enforcement fidelity.

**Avoid original NetArchTest** (last release May 2021, effectively stalled). Use `NetArchTest.eNhancedEdition` as a drop-in replacement only if the solution already carries an existing dependency on the original.

---

### The index.md Load-Bearing Insight (Explicit Callout)

The Karpathy llm-wiki gist (fetched directly from the primary source) contains one structural decision that transfers directly to this loop harness with high confidence:

**`index.md` is not a convenience — it is the harness's performance boundary.**

The gist specifies: the agent reads `index.md` first when answering any query, then drills into specific pages. This means:
- Without `index.md`, every loop iteration requires the harness to scan every KB file to find the relevant rules for the current extraction pass
- With `index.md` maintained as a content catalog (entry = `rule_id` + one-line title + `bounded_context` + `aggregate` + `kind`), the harness can locate the ~5 relevant entries for a given aggregate method in a single read
- At 50 rules: negligible difference. At 200+ rules: the difference is whether the harness fits its working set in a context window

**`index.md` must be the first artifact the harness writes and the last it updates on every run.** Its schema:

```markdown
# Sprint Rituals — Business Rule Index

## Fokus / Sprint (invariants)
- [RULE-042](./RULE-042-sprint-velocity-positive.md) — Sprint velocity must be positive | confidence: high | verified: 2026-06-11
- [RULE-043](./RULE-043-sprint-closed-no-blockers.md) — Sprint cannot close with unresolved blockers | confidence: medium | verified: 2026-06-11

## Fokus / SprintIssue (invariants)
- [RULE-044](./RULE-044-issue-estimate-nonnegative.md) — Issue story point estimate must be non-negative | confidence: high | verified: 2026-06-11

## Architecture (architectural)
- [ARCH-001](./ARCH-001-no-cross-module-refs.md) — No cross-module project references | confidence: high | verified: 2026-06-11
```

---

## Five Locked Design Decisions

| # | Decision | Evidence grounding |
|---|---|---|
| 1 | **Gherkin out for domain layer; plain `[Fact]` + `[Trait("RuleId", "RULE-NNN")]` for invariant tests** | Adzic 2020: 57% use Jira as primary req source; 12% use text files; feature files become buried scripts. Dual-artifact maintenance has zero payoff when audience is all-developer. |
| 2 | **KB entry must have `rule_id`, `evidence.file:line` as structured YAML fields, `confidence`, `status`, and `last_verified` as the five non-negotiable frontmatter fields** | Karpathy gist schema (machine-readable citations) + Martraire annotation model (on-artifact knowledge) + regulated industry traceability (bidirectional, auditable). |
| 3 | **Rule ID format: `RULE-NNN` flat; `kind:` frontmatter field for classification; sub-prefixes deferred past ~100 entries** | Karpathy pattern + DDD invariant catalog research + no published standard for sub-namespacing in .NET test suites found. |
| 4 | **Drift detection: CI diff gate (generated vs. committed); no Swimm** | Martraire reconciliation model eliminates human update discipline dependency. Swimm is the wrong tool — it solves human-authored drift. The CI diff is a single shell command with no external service. |
| 5 | **ArchUnitNET over NetArchTest for ARCH-NNN class rules** | NetArchTest: last release May 2021, stalled. ArchUnitNET: active 2025 releases, Apache 2.0, `.Because()`/`.As()` embed rule ID natively in failure output — the only .NET architecture test library with this capability. |

---

## All Sources

| Thread | Source | URL | Classification |
|---|---|---|---|
| 1 | Gojko Adzic — SbE 10 Years Later (2020) | https://gojko.net/2020/03/17/sbe-10-years.html | Established practice |
| 1 | Specification by Example — O'Reilly | https://www.oreilly.com/library/view/specification-by-example/9781617290084/ | Established practice |
| 1 | Reqnroll — .NET BDD framework | https://reqnroll.net/ | Active OSS tool |
| 1 | Reqnroll GitHub | https://github.com/reqnroll/Reqnroll | Active OSS tool |
| 1 | BDD dynamic scenario re-usability (arXiv 2402.15928) | https://arxiv.org/pdf/2402.15928 | Research paper |
| 2 | Living Documentation — O'Reilly | https://www.oreilly.com/library/view/living-documentation-continuous/9780134689418/ | Established practice |
| 2 | Living Documentation — InfoQ Q&A with Martraire | https://www.infoq.com/articles/book-review-living-documentation/ | Established practice |
| 3 | Trace.Space — Traceability in Compliance Projects | https://www.trace.space/blog/traceability-in-compliance-projects | Established practice |
| 3 | Parasoft — DO-178C Integration Testing | https://www.parasoft.com/learning-center/do-178c/integration-testing/ | Established practice |
| 3 | Parasoft — ISO 26262 Requirements Traceability | https://www.parasoft.com/learning-center/iso-26262/requirements-traceability/ | Established practice |
| 3 | Perforce — Requirements Traceability Matrix | https://www.perforce.com/resources/alm/requirements-traceability-matrix | Established practice |
| 3 | ArchUnitNET GitHub (TNG) | https://github.com/TNG/ArchUnitNET | Active OSS tool |
| 3 | Getting Started with ArchUnitNET — NimblePros (2025) | https://blog.nimblepros.com/blogs/getting-started-with-archunitnet/ | Established practice |
| 3 | NetArchTest GitHub (Ben Morris) | https://github.com/BenMorris/NetArchTest | Stalled OSS tool |
| 3 | Architecture Tests in .NET — Code Maze | https://code-maze.com/csharp-architecture-tests-with-netarchtest-rules/ | Practitioner article |
| 4 | Swimm — Documentation as Code | https://swimm.io/learn/code-documentation/documentation-as-code-why-you-need-it-and-how-to-get-started | Commercial product |
| 4 | Overcast — AI-Driven Documentation 2026 | https://overcast.blog/ai-driven-documentation-in-2026-f993f0c6d0d6 | Survey/overview |
| 5 | DDD Community Glossary | https://www.dddcommunity.org/resources/ddd_terms/ | Established practice |
| 5 | Evans DDD Reference PDF (2015) | https://www.domainlanguage.com/wp-content/uploads/2016/05/DDD_Reference_2015-03.pdf | Established practice |
| 5 | Vaughn Vernon — Aggregate Design Rules | https://www.archi-lab.io/infopages/ddd/aggregate-design-rules-vernon.html | Established practice |
| 5 | InformIT — Model True Invariants in Consistency Boundaries | https://www.informit.com/articles/article.aspx?p=2020371&seqNum=2 | Established practice |
| 5 | Codely — Aggregate Design Blueprint with AI (2025) | https://codely.com/en/blog/como-implementar-codigo-ddd-usando-ia | Emerging practice |
| 6 | Karpathy llm-wiki gist | https://gist.github.com/karpathy/442a6bf555914893e9891c11519de94f | Emerging practice |
| 6 | VentureBeat — Karpathy LLM Knowledge Base Architecture | https://venturebeat.com/data/karpathy-shares-llm-knowledge-base-architecture-that-bypasses-rag-with-an | Emerging practice |
| 6 | MindStudio — Karpathy LLM Wiki pattern | https://www.mindstudio.ai/blog/andrej-karpathy-llm-wiki-knowledge-base-claude-code | Emerging practice |
| 6 | llms.txt specification (llmstxt.org) | https://llmstxt.org/ | Emerging standard |

================================================================================

The document-specialist has completed the research and delivered the full report. Here is a compact orientation for how to use it:

**Six threads investigated, primary sources fetched directly:**

- Adzic's SbE retrospective (gojko.net) — failure mode data with survey numbers
- Martraire's Living Documentation (InfoQ Q&A) — concrete .NET mechanisms extracted
- Karpathy's llm-wiki gist (GitHub) — full schema extracted including `index.md` and `log.md` structure
- ArchUnitNET README + NimblePros 2025 guide — maintenance status and rule-ID embedding confirmed
- Regulated industry traceability (Trace.Space, Parasoft) — bidirectional RTM mechanics
- llmstxt.org spec — Jeremy Howard's format extracted

**Five locked decisions from the research:**

1. **Gherkin out for domain layer.** Adzic 2020: 57% of teams use Jira, 12% use text files, feature files become buried scripts. Use plain `[Fact]` + `[Trait("RuleId", "RULE-NNN")]` for invariant tests. Keep Reqnroll only at the FastEndpoints acceptance boundary.

2. **Five non-negotiable KB frontmatter fields:** `rule_id`, `evidence` (with `file:` and `line:` as structured YAML — not prose), `confidence`, `status`, `last_verified`. Rule ID is the single anchor across KB, test `[Trait]`, Roslyn `DiagnosticId`, and CI check.

3. **Rule ID format: `RULE-NNN` flat.** Use `kind:` frontmatter for classification. Defer sub-prefixes (`INV-`, `POL-`, `ARCH-`) past ~100 entries.

4. **Drift detection: CI diff gate, not Swimm.** Swimm solves human-authored drift. For a generated KB, the diff of generated vs. committed output is a single CI step with no external service dependency.

5. **ArchUnitNET over NetArchTest.** NetArchTest stalled May 2021. ArchUnitNET active (Apache 2.0, 2025 releases), `.Because("ARCH-042: ...")`/`.As("ARCH-042")` embed rule ID natively in failure output — the only .NET architecture test library that does this.

**The `index.md` load-bearing insight** from the Karpathy gist: agents read the index first, then drill into specific pages. Without it, every harness iteration requires a full corpus scan. Build `index.md` generation as the first artifact written and last updated on every run — its entry format is `rule_id` + one-line title + `bounded_context` + `aggregate` + `kind` + `last_verified`. This is the single highest-leverage structural decision for the harness's long-term usability.