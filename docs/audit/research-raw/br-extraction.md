I now have sufficient data across all six threads. Synthesizing the dense research output.

---

## Research: Business Rule Extraction from Code — Mining, Documenting, and Operationalizing

---

### THREAD 1: Academic Field — Business Rule Extraction / Mining from Code

**Established practice (1990s–2010s), COBOL-dominated, limited .NET literature.**

**Sneed (1996) — "Business Rule Extraction from Legacy Code"**
- Source: https://ieeexplore.ieee.org/document/544158/ | https://www.researchgate.net/publication/221029319_Business_Rule_Extraction_from_Legacy_Code
- Seminal paper. Combines variable classification, program slicing, and hierarchical abstraction. Proposes that code is more trusted than documentation because docs drift; treats code as ground truth. Implemented on industrial COBOL programs.
- Applicability: foundational framing that justifies code-as-truth; the confidence scoring philosophy maps directly.

**Huang et al. (1996) — "Extracting Business Rules from Source Code"**
- Source: https://www.researchgate.net/publication/3629947_Extracting_business_rules_from_source_code
- Introduces SOFT-REDOC reverse-engineering tool. Method: data output identification + program stripping to isolate business logic from infrastructure. Defines role of BRs vs. technical code.
- Applicability: the 70–80% of COBOL being infrastructure code ratio (later confirmed by Swimm) means pre-filtering before LLM is essential for .NET too.

**Model-Driven Reengineering / Backward Program Slicing**
- Source: https://www.academia.edu/29685854/Getting_Objects_Methods_and_Interactions_by_Extracting_Business_Rules_from_Legacy_Systems
- Four-step pipeline: restructure → slice into business logic modules → multi-view analysis → integrate views. Uses backward static slicing on Program Dependence Graphs. Reports recall >85% on COBOL.
- Applicability: the four-step structure (pre-filter, slice, multi-view, integrate) is directly transplantable to a DDD layer agent pipeline.

**Known accuracy limits (from academic literature):**
- No single method (program-centric vs. data-centric) extracts all rules — confirmed across multiple surveys
- 85% recall on backward slicing is the best academic number; precision unreported
- False positives increase when validation logic and infrastructure code are interleaved
- Source (IJCSIT 2015): https://www.ijcsit.com/docs/Volume%206/vol6issue03/ijcsit2015060336.pdf

**Specification mining surveys (LTL, 2000–2024):**
- PRISMA-based survey of 3,000+ papers covering LTL specification mining 2000–2024 exists (referenced in arxiv search results)
- Static predicate mining (Ramanathan et al., PLDI 2007): https://www.cs.purdue.edu/homes/suresh/papers/pldi07.pdf
- Scenario + value-based combined mining: https://www.se-rwth.de/publications/Scenario-based-and-value-based-specification-mining-better-together.pdf

---

### THREAD 2: Industry / Legacy-Modernization Tooling

**IBM watsonx Code Assistant for Z + ADDI**
- Sources: https://www.ibm.com/docs/en/watsonx/watsonx-code-assistant-4z/2.x?topic=addi-overview | https://mediacenter.ibm.com/media/IBM+watsonx+Code+Assistant+for+Z+Understand:+Business+rules+discovery/1_5pr695pa/189147203
- Classification: established product (COBOL/PL/I/Assembler only)
- Architecture: ADDI scans source, builds call hierarchies and dependency graphs, populates a Metadata Database. watsonx LLM layer uses this metadata as grounding context. Business Rule Discovery feature: user provides keywords → filters programs/variables → visualizes connections → reviews rules. Data dictionaries and business glossaries feed the LLM to resolve abbreviations. Agentic workflow (v2.8) uses MCP for tool connectivity.
- Key extraction mechanism: metadata-grounded LLM + ADDI dependency graph = "code-aware segmentation to reduce common pitfalls of generic LLM chunking"
- Applicability: the two-layer pattern (deterministic static analysis produces metadata → LLM consumes grounded metadata) is the proven production pattern. Direct parallel for Roslyn → LLM pipeline.

**EvolveWare Intellisys**
- Source: https://evolveware.com/agile-business-rules-extraction/ | https://blog.evolveware.com/business-rules-extraction-why-understanding-rules-vs-logic-is-important/
- Classification: established product (COBOL, PL/1, Natural, Assembler, C#, Java, VB.NET)
- Methodology: pre-processing step (dead code removal, environment-specific block deactivation, variable renaming) → automated extraction → multiple cataloging levels → plain-English annotation. Supports continuous update: code changes trigger automated reports identifying impacted rules.
- Key feature: rule de-duplication, multiline flowchart generator, JIRA export. Scale: 31M LOC (NY state), 1.5M LOC (healthcare insurer), 2.6M LOC (US Air Force).
- Applicability: pre-processing before extraction (dead code removal + variable renaming) is an essential step before LLM pass. Their continuous-update model directly addresses rule rot.

**Micro Focus (OpenText) COBOL Analyzer — Business Rule Manager**
- Sources: https://www.microfocus.com/documentation/cobol-analyzer/ca503/CA/GUID-3521F158-A453-41C2-8851-870946F0821D.html | https://www.microfocus.com/documentation/cobol-analyzer/ca60/CA/GUID-64A974DE-F7E2-4D71-AEAC-45397C1B1996.html
- Classification: established product (COBOL-centric)
- Mechanism: Business Rule Manager (BRM) batch-edits rule attributes, propagates custom attributes, matches I/O data elements against a glossary. Manually assigned rules + auto-documented containers. Traditional heuristic approach — known to "miss key logic or include unnecessary details," requiring costly manual review.
- Limitation explicitly stated: heuristic BRE requires manual validation pass. AI tools (COBRAIN, few-shot prompting) being added.
- Applicability: illustrates that pure-heuristic extraction without LLM grounding has known high false-positive/miss rate. Manual validation loop is non-negotiable.

**AWS Transform (Reimagine pathway) + Blu Age**
- Sources: https://aws.amazon.com/blogs/migration-and-modernization/aws-transform-for-mainframe-introduces-reimagine-capabilities-and-automated-testing-functionality/ | https://aws.amazon.com/blogs/mt/aws-mainframe-modernization-refactor-legacy-code-base-to-java-using-aws-blu-insights/
- Classification: established product for COBOL modernization; agentic AI for mainframe (launched May 2025)
- Reimagine pathway extracts BRs across four hierarchical levels: Line of Business → Business Functions/Domains → Business Features → Component-Level Rules. Specialized agents call deterministic tools (not just an LLM pointed at a repo). Outputs include "rule extractions with testable acceptance criteria." Human-in-the-loop validation is mandatory.
- Applicability: four-level hierarchy (business domain → feature → component → rule) maps to DDD layers (bounded context → aggregate → method → guard clause/invariant). The testable acceptance criteria output matches the harness goal.

**Swimm.io**
- Sources: https://swimm.io/enterprise | https://swimm.io/blog/blackbox-to-blueprint-extracting-business-logic-from-cobol-applications
- Classification: emerging product (IDEs, CI integration); Gartner Cool Vendor 2024
- Mechanism: deterministic static analysis (dependency tracing, data flow, dead code) + AI agents. Extracts Event-Condition-Action rules (ON <Event> IF <Condition> THEN <Action>). Variable lineage tracing across files resolves ambiguity before LLM invocation. Auto-sync on PR = documentation auto-updates when code changes.
- Key finding: LLMs cannot resolve meaning from cryptic variable names without cross-file context; static analysis must disambiguate scope first.
- Limitation: no quantitative accuracy benchmarks published. 70–80% of code is infrastructure; signal-to-noise filtering is the core challenge.
- Applicability: ECA (Event-Condition-Action) pattern is the right extraction target for DDD domain methods. Variable lineage = semantic enrichment before LLM call.

**CodeLogic**
- Source: https://codelogic.com/
- Classification: misidentified — CodeLogic is a library/framework upgrade automation tool, NOT a business rule extractor. Supports .NET, SQL, Java, JavaScript.
- No business rule extraction, no documentation traceability. Useful only for dependency impact analysis when rules change.

---

### THREAD 3: Specification Mining & Invariant Detection

**Daikon — Dynamic Invariant Detection**
- Sources: https://plse.cs.washington.edu/daikon/ | https://homes.cs.washington.edu/~mernst/pubs/daikon-tool-scp2007.pdf | https://dl.acm.org/doi/10.1016/j.scico.2007.01.015
- Classification: established research tool (Ernst et al., 2001/2007)
- Mechanism: instruments program → runs tests → infers likely invariants from value traces via generate-and-check algorithm. Detects: constant values, ranges, linear relationships, ordering, containment, function relationships, sortedness. Language-independent trace format. Supports C, C++, Java, Perl.
- Critical limitation: high false-positive rate because invariants are only confirmed against available test cases (not semantically exhaustive). "These assertions might not be valid since they only hold on the test runs." Daikon for .NET/C# requires a front-end instrumenter (Celeriac exists but is not production-standard).
- Applicability: Daikon-style dynamic detection is a strong candidate-generation mechanism. Feed test suite output through Daikon → generate candidate invariants → submit to LLM for business-language reformulation → human confirm = 3-stage pipeline. High false-positive rate means confidence scoring is mandatory.

**Static Specification Mining — Predicate Mining**
- Source: https://www.cs.purdue.edu/homes/suresh/papers/pldi07.pdf (PLDI 2007)
- Classification: established research
- Infers pre/postconditions statically from data flow analysis without test execution. No runtime required. Trades recall for precision compared to dynamic approaches.
- Applicability: for .NET DDD codebase, Roslyn semantic model IS the static predicate mining engine. No separate tool needed.

**SLD-Spec: LLM + Program Slicing for Specification Generation**
- Source: https://arxiv.org/abs/2509.09917
- Classification: emerging research (2025)
- Slices target function → LLM generates specs per slice → logical deletion filters unsound candidates. "Consistently outperforms existing methods." No numeric precision/recall reported; improvement shown in verified spec count and ablation.
- Applicability: confirms program slicing before LLM is the correct preprocessing; logical deletion (LLM self-filtering) is a viable confidence gate.

**LogicScan — Business Invariant Mining for Smart Contracts**
- Source: https://arxiv.org/pdf/2602.03271
- Classification: emerging research (2025)
- Logic Miner module: transforms raw code into normalized, verifiable business invariants. Multi-dimensional code extraction + majority-voting rule aggregation to reduce false positives from conflicting rules.
- Applicability: majority-voting aggregation across multiple LLM passes is a concrete mechanism to compute confidence scores. False positives from conflicted rules = important failure mode in DDD (overloaded validation logic).

---

### THREAD 4: LLM-Era Approaches — Measured Precision/Recall

**Extracting Formal Specifications from Documents Using LLMs (Li et al., 2025)**
- Source: https://arxiv.org/abs/2504.01294
- Two-stage annotation-then-conversion pipeline. Best LLM accuracy: **71.6%**. Two-stage method improves correctly extracted specs by **29.2%** over end-to-end. Tested: GPT-4o, Claude, Llama.
- Failure modes: specification oversimplification (nuance lost) and specification fabrication (hallucinated rules). From documents, not directly from code.
- Applicability: 71.6% is the ceiling for document-derived specification accuracy; code-derived accuracy is likely different. The annotation-then-conversion pipeline is the architecture recommendation for the harness.

**Talk is Cheap, Logic is Hard: Benchmarking LLMs on Post-Condition Formalization**
- Source: https://arxiv.org/abs/2603.17193
- 24 LLMs evaluated on 40 formal pre/post-condition tasks. Key findings: proprietary > open-source; LLMs perform better on pre-conditions than post-conditions; **no LLM correctly formalized all tasks**; near-miss errors (incorrect but close) from proprietary models; outright errors from open-source models. Post-conditions map directly to DDD aggregate invariants (state after method).
- Critical finding: **"specifications extracted from code may inherit bugs already present in the code"** — extracted rules are only as correct as the implementation.
- Applicability: do not extract from buggy aggregates without a correctness gate. Automated test suite validation of extracted post-conditions is mandatory.

**LLM Hallucination Rates in Code (2024–2025)**
- Sources: https://diffray.ai/blog/llm-hallucinations-code-review/ | https://dl.acm.org/doi/pdf/10.1145/3728894
- Package hallucination: GPT-4 Turbo 4%, CodeLlama 7B 26%. Security vulnerabilities in 29–45% of AI-generated code.
- Code summarization hallucinations measured by ETF framework (ACL 2025) using static entity tracing.
- BMC per-assertion hallucination scoring pipeline assigns per-claim reliability scores; ServiceNow 2025 framework generalizes this.
- Applicability: per-rule confidence scoring is feasible using assertion-level hallucination detection frameworks. BMC pipeline = direct design pattern for the harness confidence score.

**Frontiers Systematic Review: LLMs for Requirements Engineering (2025)**
- Source: https://www.frontiersin.org/journals/computer-science/articles/10.3389/fcomp.2025.1519437/full
- 29 studies; no unified benchmark; metrics fragmented (BLEU, ROUGE, F1, Kappa). Hallucination flagged as "critical concern" but no aggregate rate quantified. Contextual prompts outperform zero-shot for RE tasks. LoRA fine-tuning improves domain specificity.
- Applicability: lack of unified benchmark means any precision/recall numbers for the harness must be generated internally with a held-out ground truth set.

**AgentPex — Explicit-Only Rule Extraction from Agent Traces**
- Source: https://arxiv.org/pdf/2603.23806
- Strict explicit-only extraction policy: LLM extracts only rules "directly stated in source material, avoid inferred or assumed constraints." Per-specification LLM invocation. Produces Policy, Output, and Transition specifications.
- Applicability: explicit-only policy is the correct extraction discipline — reject rules not grounded in a specific code location. This is the confidence filter implementation.

**AlphaCodium multi-stage structured flow result (Ridnik et al., 2024):**
- Structured multi-stage flows with verification improve GPT-4 accuracy from 19% → 44% on coding tasks. Referenced in: https://arxiv.org/html/2604.05150
- Applicability: verification loops (not single-pass extraction) are required to reach acceptable accuracy.

---

### THREAD 5: Rule Documentation Formats

**SBVR (Semantics of Business Vocabulary and Rules)**
- Sources: https://www.brcommunity.com/articles.php?id=b597 | https://www.evolute.be/thoughts/rules.html
- Classification: established OMG standard
- Formal metamodel: rules built from fact types, fact types built from terms (noun + verb concepts). Rules expressed in structured natural language. Favored by legal/compliance/QA stakeholders. IT adoption limited — "not always welcomed by the IT department."
- Traceability: each SBVR rule can link to multiple fact types; event-triggered (rule fires when relevant business condition occurs = traceable). Many-to-many relationship with decisions.

**DMN (Decision Model and Notation)**
- Sources: https://www.trisotech.com/dmn/ | https://www.brcommunity.com/articles.php?id=b597
- Classification: established OMG standard (executable)
- Decision Tables with Decision Requirements Graph (DRG). Subject matter experts can "fully define and tested by non-programmers." Maps directly to code snippets, services. Traceability: rule ↔ decision ↔ process (bidirectional links in DRG). Executable = auto-testable.
- SBVR→DMN translation: one SBVR rule maps to multiple decisions; algorithm exists (BPMN+DMN paper, ResearchGate: https://www.researchgate.net/publication/303626206_From_SBVR_to_BPMN_and_DMN_Models_Proposal_of_Translation_from_Rules_to_Process_and_Decision_Models).

**Practical knowledge base format (for LLM-agent harness):**
Based on research synthesis, the effective format for an LLM-extracted rule KB entry needs:
1. `rule_id` — stable identifier
2. `title` — short business-language name
3. `category` — structural | operational | derivation | constraint
4. `statement` — plain-language rule text
5. `evidence` — array of `{file, start_line, end_line, snippet}` (required, reject if absent)
6. `confidence_score` — 0.0–1.0 (Daikon + LLM multi-pass + test coverage)
7. `invariant_type` — precondition | postcondition | invariant | validation
8. `test_ids` — array of test names that pin this rule
9. `last_verified_commit` — SHA of last code review
10. `status` — verified | candidate | rejected

SBVR is too verbose for agent-managed KB. DMN decision tables are appropriate when rule has multi-outcome branching logic. Flat structured JSON/Markdown with mandatory evidence array is the practical format.

---

### THREAD 6: The Traceability Problem — Rule Rot & Synchronization

**Traceability Aspects for Business Rules Management (BRCommunity, 2008)**
- Source: https://www.brcommunity.com/articles.php?id=b384
- Ten traceability types identified: Vocabulary, Rule Reuse, Temporal, Business Policy, Processing (runtime), Test Case, Deployment, SOA services, Work List, Compliance.
- Anti-rule-rot mechanisms: Processing Traceability (hook points in architecture capture which rules fired at runtime); Temporal Traceability (rule-sets agnostic of effective dates); Regression test scaffolding with captured expectations.
- Key warning: "building traceability into your Architecture is an evolutionary process and needs to be built in over time. Vendor products come only half way."
- Applicability: `last_verified_commit` in the KB schema + CI hook that re-evaluates rules against changed files = implementation of Processing + Temporal traceability.

**EvolveWare continuous update model:**
- Source: https://evolveware.com/agile-business-rules-extraction/
- When code changes, automated reports highlight which extracted rules are impacted; unaffected rules remain unchanged.
- Applicability: change-impact scoping (only re-evaluate rules whose evidence files changed) is the practical approach. Git diff → affected file set → subset re-extraction.

**RTM + CI/CD integration (2025–2026 research):**
- Source: https://medium.com/@sancharini.panda/how-a-traceability-matrix-fits-into-modern-ci-cd-workflows-714c5a6862af
- Continuous traceability reflects latest requirement coverage in real-time. Automated RTM updates within CI/CD prevent rule rot.
- Key finding: "Once the gap widens [between matrix and code], the matrix no longer serves as a current representation." The drift rate is the key metric.

**Swimm auto-sync model:**
- Source: https://swimm.io/ (product documentation)
- On every PR, Swimm automatically verifies and updates documentation against changed code. Docs contain "live code spanning multiple files and repositories." Documentation that refers to moved/deleted code is flagged as broken.
- Applicability: this is the gold standard for rule rot prevention. A harness CI step that re-checks evidence citations (file:line still exists, content unchanged) on every PR prevents silent rule rot.

**RequirementsTraceability ↔ Code Links (automated approaches):**
- Information retrieval + ML trace link generation: fails to understand semantics or integrate domain knowledge → imprecise results
- Rule-based trace link generation: more precise but brittle to refactoring
- Source: https://www.researchgate.net/publication/223738681_Rule-based_generation_of_requirments_traceability_relations

---

### Roslyn as the Static Analysis Engine (Thread 1 + 6 synthesis)

**For a .NET DDD codebase, Roslyn replaces Daikon/program-slicing tools:**
- Sources: https://dev.to/mattjhosking/analysing-a-net-codebase-with-roslyn-5cn0 | https://github.com/dotnet/roslyn/blob/main/docs/wiki/Getting-Started-C%23-Semantic-Analysis.md
- `CSharpSyntaxWalker` + `SemanticModel` traversal can: identify aggregate roots/value objects by base type inspection via `ITypeSymbol`; extract guard clauses and `throw` expressions from constructors/factory methods; analyze FluentValidation rule chains (via `WithMessage()`, `Must()`, `RuleFor()` call patterns); build a `{file:line → code segment}` evidence index.
- `Compilation.GetSemanticModel()` resolves overloaded types and cross-assembly references → eliminates the ambiguity problem that Swimm reports as the core LLM failure point.

**FluentValidation extraction note:**
- FV validators belong to the application layer per DDD purists, but in practice many projects (including .NET CQRS/FastEndpoints stacks) place validation rules in validators that encode business constraints not expressed in domain entities.
- Source: https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-model-layer-validations
- Both sources must be mined: guard clauses in domain constructors (invariants) AND FV validators in application layer (request constraints). Label the layer in the KB entry.

---

### TOP 5 DESIGN IMPLICATIONS

**1. Two-stage grounded pipeline, not end-to-end LLM.**
Every production-grade tool (IBM ADDI + watsonx, Swimm, AWS Transform Reimagine, SLD-Spec) uses the same pattern: deterministic static analysis first (build evidence index, resolve symbols, trace lineage, remove infrastructure noise) → LLM second on the pre-processed, grounded context. Single-pass end-to-end LLM extraction reaches 71.6% accuracy ceiling with fabrication as the primary failure mode. For .NET DDD, Roslyn is the static analysis layer; the LLM sees annotated code slices with resolved symbol names, not raw files.

**2. Confidence score must be multi-source, not single-LLM.**
LogicScan's majority-voting, Daikon's test-confirmation model, and BMC's per-assertion hallucination scoring all converge on the same principle: confidence is earned by corroboration across independent evidence channels. Score components: (a) static presence of code construct (Roslyn finds guard clause at file:line = high weight), (b) LLM extraction consistency across N≥3 independent passes (majority vote), (c) dynamic test coverage confirmation (test exercises the rule path), (d) human review stamp. Rules failing (a) are rejected at intake. Rules failing (b)+(c) are candidates, not verified.

**3. Evidence citation (file:line) is the rejection gate, not a nice-to-have.**
AgentPex's explicit-only extraction policy and the entire traceability literature converge: a rule without a code location is a hallucination risk, not knowledge. The KB schema must enforce `evidence` as a non-nullable array. Rules where the LLM cannot ground its claim to a specific file:line:snippet are immediately classified `status: rejected`. This is the primary anti-hallucination mechanism.

**4. Rule rot requires a CI hook, not human discipline.**
Swimm's auto-sync, EvolveWare's change-impact reports, and RTM-in-CI research all show the same finding: manual traceability maintenance degrades within weeks. The harness needs a CI step that (a) on every PR computes `git diff` → affected files → rules with evidence in those files → re-triggers extraction on affected rules only, (b) detects evidence citation breakage (file moved, line deleted, content changed), and (c) sets `status: stale` on broken citations. This is the only sustainable solution to rule rot.

**5. Extracted rules inherit code bugs — test validation before "verified" status.**
The post-condition formalization benchmark (arXiv:2603.17193) explicitly documents that specifications extracted from code inherit existing bugs. For a DDD domain layer, this means an extracted invariant that says "balance must be ≥ 0 after withdrawal" is only correct if the withdrawal implementation is correct. The harness must not promote a rule from `candidate` to `verified` without a test that exercises the rule path and passes. The test is the independently observable evidence that the rule is both present and correct. This creates the loop: extract rule → generate test → run test → if green, mark verified; if red, flag code or rule as suspect.

---

### Sources Index

- [IEEE: Business Rule Extraction from Legacy Code (Sneed 1996)](https://ieeexplore.ieee.org/document/544158/)
- [ResearchGate: Business Rule Extraction from Legacy Code](https://www.researchgate.net/publication/221029319_Business_Rule_Extraction_from_Legacy_Code)
- [ResearchGate: Extracting Business Rules from Source Code (1996)](https://www.researchgate.net/publication/3629947_Extracting_business_rules_from_source_code)
- [Academia.edu: Getting Objects, Methods and Interactions by Extracting BRs from Legacy Systems](https://www.academia.edu/29685854/Getting_Objects_Methods_and_Interactions_by_Extracting_Business_Rules_from_Legacy_Systems)
- [IJCSIT 2015: Tool for Extraction of Functional Requirements from Legacy Code](https://www.ijcsit.com/docs/Volume%206/vol6issue03/ijcsit2015060336.pdf)
- [IBM watsonx Code Assistant for Z ADDI Docs](https://www.ibm.com/docs/en/watsonx/watsonx-code-assistant-4z/2.x?topic=addi-overview)
- [IBM watsonx Business Rules Discovery (Mediacenter)](https://mediacenter.ibm.com/media/IBM+watsonx+Code+Assistant+for+Z+Understand:+Business+rules+discovery/1_5pr695pa/189147203)
- [IBM Agentic AI mainframe modernization announcement](https://www.ibm.com/new/announcements/agentic-ai-for-smarter-mainframe-modernization-with-ibm-watsonx-code-assistant-for-z)
- [EvolveWare Agile Business Rules Extraction](https://evolveware.com/agile-business-rules-extraction/)
- [EvolveWare BRE Methodology Blog](https://blog.evolveware.com/business-rules-extraction-why-understanding-rules-vs-logic-is-important/)
- [Micro Focus COBOL Analyzer Business Rule Manager](https://www.microfocus.com/documentation/cobol-analyzer/ca503/CA/GUID-3521F158-A453-41C2-8851-870946F0821D.html)
- [Micro Focus: Extracting Business Rules](https://www.microfocus.com/documentation/cobol-analyzer/ca60/CA/GUID-64A974DE-F7E2-4D71-AEAC-45397C1B1996.html)
- [Swimm: 5 COBOL BRE Tools](https://swimm.io/learn/cobol/5-cobol-business-rules-extraction-tools-to-know-in-2026)
- [Swimm: Blackbox to Blueprint](https://swimm.io/blog/blackbox-to-blueprint-extracting-business-logic-from-cobol-applications)
- [Swimm Enterprise](https://swimm.io/enterprise)
- [AWS Transform Reimagine capabilities announcement](https://aws.amazon.com/blogs/aws/aws-transform-for-mainframe-introduces-reimagine-capabilities-and-automated-testing-functionality/)
- [AWS Blu Age: Refactor COBOL to Java](https://aws.amazon.com/blogs/mt/aws-mainframe-modernization-refactor-legacy-code-base-to-java-using-aws-blu-insights/)
- [Daikon Dynamic Invariant Detector (official)](https://plse.cs.washington.edu/daikon/)
- [Daikon: Science of Computer Programming 2007 paper](https://homes.cs.washington.edu/~mernst/pubs/daikon-tool-scp2007.pdf)
- [Daikon: ACM DL](https://dl.acm.org/doi/10.1016/j.scico.2007.01.015)
- [Static Specification Inference via Predicate Mining (PLDI 2007)](https://www.cs.purdue.edu/homes/suresh/papers/pldi07.pdf)
- [Scenario + Value-Based Specification Mining (combined)](https://www.se-rwth.de/publications/Scenario-based-and-value-based-specification-mining-better-together.pdf)
- [SLD-Spec: LLM + Program Slicing for Specification Generation (arXiv 2025)](https://arxiv.org/abs/2509.09917)
- [LogicScan: LLM-driven Business Logic Vulnerability Detection (arXiv 2025)](https://arxiv.org/pdf/2602.03271)
- [Extracting Formal Specifications from Documents Using LLMs (arXiv 2025)](https://arxiv.org/abs/2504.01294)
- [Talk is Cheap, Logic is Hard: Benchmarking LLMs on Post-Condition Formalization (arXiv 2025)](https://arxiv.org/abs/2603.17193)
- [LLM Hallucinations in AI Code Review (Diffray)](https://diffray.ai/blog/llm-hallucinations-code-review/)
- [LLM Hallucinations in Practical Code Generation (ACM 2025)](https://dl.acm.org/doi/pdf/10.1145/3728894)
- [Frontiers: Research directions for LLM in Software RE (2025)](https://www.frontiersin.org/journals/computer-science/articles/10.3389/fcomp.2025.1519437/full)
- [AgentPex: Willful Disobedience — spec extraction from agentic traces (arXiv 2025)](https://arxiv.org/pdf/2603.23806)
- [Compiled AI: Deterministic vs. LLM hybrid extraction (arXiv 2025)](https://arxiv.org/html/2604.05150)
- [4D-ARE: Bridging Attribution Gap in LLM Agent RE (arXiv 2025)](https://arxiv.org/pdf/2601.04556)
- [DMN Standard — Trisotech](https://www.trisotech.com/dmn/)
- [SBVR ↔ DMN Relationship (BRCommunity)](https://www.brcommunity.com/articles.php?id=b597)
- [From SBVR to BPMN and DMN (ResearchGate)](https://www.researchgate.net/publication/303626206_From_SBVR_to_BPMN_and_DMN_Models_Proposal_of_Translation_from_Rules_to_Process_and_Decision_Models)
- [Traceability Aspects for Business Rules Management (BRCommunity 2008)](https://www.brcommunity.com/articles.php?id=b384)
- [Traceability Matrix in CI/CD Workflows (Medium 2026)](https://medium.com/@sancharini.panda/how-a-traceability-matrix-fits-into-modern-ci-cd-workflows-714c5a6862af)
- [Towards Automatic Extraction of Structural Business Rules (WCRE 2012)](https://ojcchar.github.io/files/2-wcre12.pdf)
- [Rule-based Generation of Requirements Traceability (ResearchGate)](https://www.researchgate.net/publication/223738681_Rule-based_generation_of_requirments_traceability_relations)
- [Analysing a .NET Codebase with Roslyn (DEV.to)](https://dev.to/mattjhosking/analysing-a-net-codebase-with-roslyn-5cn0)
- [Getting Started: C# Semantic Analysis (Roslyn GitHub)](https://github.com/dotnet/roslyn/blob/main/docs/wiki/Getting-Started-C%23-Semantic-Analysis.md)
- [Microsoft: Domain Model Layer Validations (.NET)](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-model-layer-validations)
- [NDepend .NET Dependency Graph](https://www.ndepend.com/net-dependency-graph)
- [Validation and DDD (Enterprise Craftsmanship)](https://enterprisecraftsmanship.com/posts/validation-and-ddd/)

================================================================================

## Research: Business Rule Extraction from Code — Mining, Documenting, and Operationalizing

---

### THREAD 1: Academic Field — Static Analysis, Program Slicing, Known Accuracy Limits

**Classification: Established practice (1990s–2010s), COBOL-dominated, limited .NET literature.**

**Sneed (1996) — Seminal Paper**
- Source: [IEEE Xplore](https://ieeexplore.ieee.org/document/544158/) | [ResearchGate](https://www.researchgate.net/publication/221029319_Business_Rule_Extraction_from_Legacy_Code)
- Combines variable classification, backward program slicing on Program Dependence Graphs, and hierarchical abstraction. Frames code as more trustworthy than docs because docs drift. Tested on industrial COBOL programs.
- Applicability: foundational justification for code-as-ground-truth; confidence scoring philosophy maps directly.

**Model-Driven Reengineering / Backward Slicing (Academia.edu, 2016)**
- Source: [Academia.edu](https://www.academia.edu/29685854/Getting_Objects_Methods_and_Interactions_by_Extracting_Business_Rules_from_Legacy_Systems)
- Four-step pipeline: restructure → slice into logic modules → multi-view analysis → integrate views. Reports recall >85% on COBOL with backward slicing on PDGs. Precision unreported.
- Applicability: four-step structure is directly transplantable to a DDD layer agent pipeline. 85% recall is the best published academic number.

**Huang et al. (1996) — SOFT-REDOC**
- Source: [ResearchGate](https://www.researchgate.net/publication/3629947_Extracting_business_rules_from_source_code)
- Data output identification + program stripping to isolate BRs from infrastructure. Establishes the key finding that 70–80% of COBOL (and by analogy, legacy OOP) is infrastructure code, not business logic. Pre-filtering before LLM is essential.
- Applicability: signal-to-noise ratio problem is universal, not COBOL-specific.

**Known accuracy limits across all surveys:**
- No single method (program-centric vs. data-centric) extracts all rules — confirmed by IJCSIT 2015 survey: [PDF](https://www.ijcsit.com/docs/Volume%206/vol6issue03/ijcsit2015060336.pdf)
- 85% recall (backward slicing, COBOL) is the best academic ceiling; precision figures absent from literature
- False positives increase when validation logic and infrastructure code are interleaved — exactly the DDD application-layer problem

**Specification Mining Surveys:**
- LTL specification mining PRISMA survey covering 3,000+ papers 2000–2024 (referenced in search results)
- Static predicate mining (Ramanathan et al., PLDI 2007): [PDF](https://www.cs.purdue.edu/homes/suresh/papers/pldi07.pdf)
- Scenario + value-based combined mining: [paper](https://www.se-rwth.de/publications/Scenario-based-and-value-based-specification-mining-better-together.pdf) — richer specs from combined approaches

---

### THREAD 2: Industry / Legacy-Modernization Tooling

**IBM watsonx Code Assistant for Z + ADDI**
- Sources: [ADDI Overview Docs](https://www.ibm.com/docs/en/watsonx/watsonx-code-assistant-4z/2.x?topic=addi-overview) | [Business Rules Discovery (Mediacenter)](https://mediacenter.ibm.com/media/IBM+watsonx+Code+Assistant+for+Z+Understand:+Business+rules+discovery/1_5pr695pa/189147203) | [Agentic AI Announcement](https://www.ibm.com/new/announcements/agentic-ai-for-smarter-mainframe-modernization-with-ibm-watsonx-code-assistant-for-z)
- Classification: established product (COBOL/PL/I/Assembler only)
- Architecture: ADDI scans source → builds call hierarchies + dependency graphs → populates Metadata DB → watsonx LLM consumes grounded metadata. User provides keywords → filters programs/variables → visualizes connections → reviews rules. Data dictionaries feed the LLM to resolve cryptic abbreviations. v2.8 uses MCP for tool connectivity (agentic workflow).
- Key insight: "code-aware segmentation reduces common pitfalls of generic LLM chunking." The grounded metadata prevents hallucination on variable names.
- Applicability: **the two-layer pattern (deterministic static analysis produces metadata → LLM consumes grounded metadata) is the proven production pattern.** Direct parallel: Roslyn → LLM.

**EvolveWare Intellisys**
- Sources: [Agile BRE](https://evolveware.com/agile-business-rules-extraction/) | [BRE Blog](https://blog.evolveware.com/business-rules-extraction-why-understanding-rules-vs-logic-is-important/)
- Classification: established product (COBOL, PL/1, Natural, Assembler, **C#, Java, VB.NET** — relevant for .NET)
- Methodology: pre-processing (dead code removal, environment-specific block deactivation, variable renaming) → automated extraction → multiple cataloging levels → plain-English annotation. Continuous update: code changes trigger automated reports identifying impacted rules.
- Scale: 31M LOC (NY state), 1.5M LOC (healthcare insurer), 2.6M LOC (US Air Force). No accuracy percentages published.
- Applicability: pre-processing before extraction (dead code removal + variable renaming) is an essential step. Their continuous-update model is the direct reference design for rule rot prevention. Also explicitly supports C#.

**Micro Focus (OpenText) COBOL Analyzer — Business Rule Manager**
- Sources: [BRM docs v5](https://www.microfocus.com/documentation/cobol-analyzer/ca503/CA/GUID-3521F158-A453-41C2-8851-870946F0821D.html) | [Extracting BRs docs](https://www.microfocus.com/documentation/cobol-analyzer/ca60/CA/GUID-64A974DE-F7E2-4D71-AEAC-45397C1B1996.html)
- Classification: established product (COBOL-centric)
- BRM batch-edits rule attributes, matches I/O data elements against glossary. Heuristic extraction — explicitly documented to "miss key logic or include unnecessary details, requiring costly manual review." COBRAIN (LLM/few-shot) being added to supplement.
- Applicability: illustrates that pure-heuristic extraction without LLM grounding has known high false-positive/miss rate. Manual validation loop is non-negotiable.

**AWS Transform (Reimagine pathway) + Blu Age**
- Sources: [Reimagine announcement](https://aws.amazon.com/blogs/aws/aws-transform-for-mainframe-introduces-reimagine-capabilities-and-automated-testing-functionality/) | [Blu Insights refactor blog](https://aws.amazon.com/blogs/mt/aws-mainframe-modernization-refactor-legacy-code-base-to-java-using-aws-blu-insights/)
- Classification: established product (COBOL/JCL/mainframe); Reimagine pathway launched May 2025
- Reimagine extracts BRs across four hierarchical levels: Line of Business → Business Functions/Domains → Business Features → Component-Level Rules. Specialized agents call deterministic tools (not general-purpose LLM pointed at a repo). Outputs include "rule extractions with testable acceptance criteria." Human-in-the-loop mandatory.
- Key insight: four-level hierarchy maps to DDD layers (bounded context → aggregate → method → guard clause/invariant). The testable acceptance criteria output matches the harness goal exactly.

**Swimm.io**
- Sources: [Enterprise page](https://swimm.io/enterprise) | [Blackbox to Blueprint blog](https://swimm.io/blog/blackbox-to-blueprint-extracting-business-logic-from-cobol-applications) | [5 COBOL BRE Tools](https://swimm.io/learn/cobol/5-cobol-business-rules-extraction-tools-to-know-in-2026)
- Classification: emerging product (IDE integration, CI); Gartner Cool Vendor 2024
- Mechanism: deterministic static analysis (dependency tracing, data flow, dead code detection) + AI agents. Extracts Event-Condition-Action rules (ON <Event> IF <Condition> THEN <Action>). Variable lineage tracing across files resolves ambiguity before LLM invocation. Auto-sync on PR.
- Critical finding from blog: "LLMs cannot resolve meaning from cryptic variable names without cross-file context; static analysis must disambiguate scope first." 70–80% of code is infrastructure; signal-to-noise filtering is the core challenge.
- No quantitative accuracy benchmarks published.
- Applicability: ECA (Event-Condition-Action) is the right extraction target for DDD domain methods. Auto-sync on PR = direct reference for CI-based rule rot prevention.

**CodeLogic**
- Source: [codelogic.com](https://codelogic.com/)
- Classification: misidentified in initial query — CodeLogic is a library/framework **upgrade automation** tool for .NET, SQL, Java, JavaScript. No business rule extraction, no documentation traceability. Useful only for dependency impact analysis when rules change, not for extraction.

---

### THREAD 3: Specification Mining and Invariant Detection

**Daikon — Dynamic Invariant Detection (Ernst et al., 2001/2007)**
- Sources: [Official site](https://plse.cs.washington.edu/daikon/) | [SCP 2007 paper PDF](https://homes.cs.washington.edu/~mernst/pubs/daikon-tool-scp2007.pdf) | [ACM DL](https://dl.acm.org/doi/10.1016/j.scico.2007.01.015)
- Classification: established research tool
- Instruments program → runs tests → infers likely invariants from value traces via generate-and-check. Detects: constants, ranges, linear relationships, ordering, containment, sortedness. Language-independent trace format; supports C, C++, Java, Perl.
- Critical limitation: high false-positive rate because invariants are confirmed only against available test cases, not semantically. "These assertions might not be valid since they only hold on the test runs." .NET/C# front-end (Celeriac) exists but not production-standard.
- Applicability: Daikon-style dynamic detection is a strong candidate-generation mechanism. Pipeline: run test suite → Daikon → candidate invariants → LLM business-language reformulation → human confirm. High false-positive rate means confidence scoring is mandatory before promotion.

**SLD-Spec: LLM + Program Slicing for Specification Generation (2025)**
- Source: [arXiv:2509.09917](https://arxiv.org/abs/2509.09917)
- Classification: emerging research
- Slices target function → LLM generates specs per slice → logical deletion stage filters unsound candidates. Consistently outperforms baselines on spec completeness and verification rate. No numeric precision/recall in abstract.
- Applicability: confirms program slicing before LLM is the correct preprocessing. Logical deletion (LLM self-filtering over candidate set) is a viable confidence gate implementation.

**LogicScan — Business Invariant Mining for Smart Contracts (2025)**
- Source: [arXiv:2602.03271](https://arxiv.org/pdf/2602.03271)
- Classification: emerging research
- Logic Miner module transforms raw code into normalized, verifiable business invariants. Multi-dimensional code extraction + majority-voting rule aggregation to reduce false positives from conflicting rules (redundant validation logic).
- Applicability: majority-voting aggregation across multiple LLM passes is a concrete mechanism to compute confidence scores. Conflicted rules from redundant validation = important failure mode in DDD application layers with both domain guard clauses and FV validators.

---

### THREAD 4: LLM-Era Approaches — Measured Precision/Recall and Hallucination Rates

**Extracting Formal Specifications from Documents Using LLMs (Li et al., arXiv 2025)**
- Source: [arXiv:2504.01294](https://arxiv.org/abs/2504.01294)
- Two-stage annotation-then-conversion pipeline. Best accuracy: **71.6%** (GPT-4o, Claude, Llama tested; from documents, not directly from code). Two-stage improves over end-to-end by **29.2%** on correct specs. Failure modes: (1) oversimplification, (2) fabrication.
- Applicability: 71.6% is the reference ceiling for document-derived spec accuracy. The annotation-then-conversion architecture is the recommended harness pipeline structure.

**Talk is Cheap, Logic is Hard: Benchmarking LLMs on Post-Condition Formalization (arXiv 2025)**
- Source: [arXiv:2603.17193](https://arxiv.org/abs/2603.17193)
- 24 LLMs on 40 formal pre/postcondition tasks. Key results: proprietary > open-source; no LLM correctly formalized all tasks; LLMs perform better on pre-conditions than post-conditions; near-miss errors from proprietary models; outright errors from open-source. Automated test suites expose incorrect solutions that manual review misses.
- **Critical finding: "specifications extracted from code may inherit bugs already present in the code."**
- Applicability: post-conditions map to DDD aggregate invariants (state after method call). Do not extract from suspect aggregates without a correctness gate. Automated test validation is mandatory before promoting a rule to verified.

**LLM Hallucination Rates in Code (2024–2025)**
- Sources: [Diffray blog](https://diffray.ai/blog/llm-hallucinations-code-review/) | [ACM 2025 paper](https://dl.acm.org/doi/pdf/10.1145/3728894)
- GPT-4 Turbo: 4% package hallucination rate; CodeLlama 7B: 26%. 29–45% of AI-generated code contains security vulnerabilities. 20% of package recommendations point to non-existent libraries.
- BMC domain-specific pipeline: assigns per-assertion hallucination scores by cross-referencing claims against source data — this is the concrete reference design for per-rule confidence scoring.
- Applicability: per-rule confidence scoring is feasible using assertion-level hallucination detection. The BMC pattern = harness confidence score implementation.

**AgentPex — Explicit-Only Rule Extraction from Agentic Traces (arXiv 2025)**
- Source: [arXiv:2603.23806](https://arxiv.org/pdf/2603.23806)
- Strict explicit-only extraction policy: LLM extracts only rules "directly stated in source material; avoid inferred or assumed constraints." Per-specification independent LLM invocation. Produces Policy, Output, and Transition specifications.
- Applicability: explicit-only policy is the correct extraction discipline. Rules not grounded in a specific code location are rejected at intake. This is the confidence filter implementation.

**AlphaCodium multi-stage structured flow (Ridnik et al., 2024)**
- Referenced in: [arXiv:2604.05150](https://arxiv.org/html/2604.05150)
- Structured multi-stage flows with verification improve GPT-4 coding accuracy from **19% → 44%**. MetaGPT's SOPs reduce cascading hallucinations by constraining agent behavior.
- Applicability: single-pass extraction is insufficient. Verification loops are required to reach acceptable accuracy.

**Frontiers Systematic Review: LLMs for Requirements Engineering (2025)**
- Source: [Frontiers Computer Science](https://www.frontiersin.org/journals/computer-science/articles/10.3389/fcomp.2025.1519437/full)
- 29 studies; metrics fragmented (BLEU, ROUGE, F1, Kappa) — no unified benchmark. Hallucination flagged as "critical concern" but no aggregate rate quantified. Contextual prompts outperform zero-shot for RE tasks.
- Applicability: lack of unified benchmark means any precision/recall numbers for the harness must be generated internally with a held-out ground truth set. Cannot cite external benchmarks as accuracy proxies.

---

### THREAD 5: Rule Documentation Formats

**SBVR (Semantics of Business Vocabulary and Rules)**
- Sources: [SBVR ↔ DMN relationship (BRCommunity)](https://www.brcommunity.com/articles.php?id=b597) | [Evolute overview](https://www.evolute.be/thoughts/rules.html)
- Classification: established OMG standard
- Formal metamodel: rules built from fact types, from terms (noun + verb concepts). Structured natural language. Favored by legal/compliance/QA. IT adoption limited — "not always welcomed by the IT department." Many-to-many relationship with decisions. Each rule fires when a business condition threatens to occur (traceable invocation).
- Verdict: too verbose for agent-managed KB; useful as a semantic vocabulary layer for term disambiguation.

**DMN (Decision Model and Notation)**
- Sources: [Trisotech DMN](https://www.trisotech.com/dmn/) | [SBVR→DMN translation paper](https://www.researchgate.net/publication/303626206_From_SBVR_to_BPMN_and_DMN_Models_Proposal_of_Translation_from_Rules_to_Process_and_Decision_Models)
- Classification: established OMG standard (executable)
- Decision Tables + Decision Requirements Graph. Executable by non-programmers, directly maps to code implementations. Bidirectional traceability (rule ↔ decision ↔ process). SBVR→DMN translation algorithm exists.
- Verdict: appropriate for rules with multi-outcome branching logic (eligibility, pricing tiers, classification). Not practical for behavioral invariants (guard clauses, state assertions).

**Practical KB Entry Schema (synthesized from research):**
No published standard covers the full evidence-citation + confidence-score requirement. Recommended minimal schema:

```
rule_id:           stable identifier (RULE-{domain}-{seq})
title:             short business-language name
category:          structural | operational | derivation | constraint
statement:         plain-language rule text
evidence:          [{file, start_line, end_line, snippet}]  ← required, reject if absent
confidence_score:  0.0–1.0  (multi-source: static + LLM votes + test coverage)
invariant_type:    precondition | postcondition | invariant | validation
source_layer:      domain | application  (domain = guard clause, application = FV validator)
test_ids:          [test names that pin this rule]
last_verified_commit: SHA
status:            verified | candidate | rejected | stale
```

SBVR handles vocabulary; DMN handles branching logic tables; the above schema is the harness KB format.

---

### THREAD 6: Rule Rot — Synchronization and Traceability

**Traceability Aspects for Business Rules Management (BRCommunity, 2008)**
- Source: [brcommunity.com/articles.php?id=b384](https://www.brcommunity.com/articles.php?id=b384)
- Ten traceability types: Vocabulary, Rule Reuse, Temporal, Business Policy, Processing (runtime hook points), Test Case, Deployment, SOA services, Work List, Compliance.
- Anti-rule-rot mechanisms: Processing Traceability (hook points capture which rules fired at runtime); Temporal Traceability (rule-sets agnostic of effective dates); Regression test scaffolding.
- Key warning: "building traceability into your Architecture is an evolutionary process. Vendor products come only half way."
- Applicability: `last_verified_commit` in KB schema + CI hook re-evaluating rules against changed files = implementation of Processing + Temporal traceability.

**EvolveWare continuous update model:**
- Source: [evolveware.com/agile-business-rules-extraction/](https://evolveware.com/agile-business-rules-extraction/)
- When code changes, automated reports highlight impacted rules; unaffected rules remain unchanged (change-scoped re-extraction, not full re-run).
- Applicability: on every PR, `git diff` → affected file set → subset re-extraction only. Full re-extraction on every commit is not scalable.

**RTM in CI/CD (2025–2026):**
- Source: [Medium: Traceability Matrix in CI/CD Workflows](https://medium.com/@sancharini.panda/how-a-traceability-matrix-fits-into-modern-ci-cd-workflows-714c5a6862af)
- "Once the gap widens between matrix and code, the matrix no longer serves as a current representation." Every deployment triggers automated tests traceable back to requirements. Continuous traceability reflects latest coverage in real time.

**Swimm auto-sync model (gold standard reference):**
- Source: [swimm.io](https://swimm.io/)
- On every PR, Swimm automatically verifies and updates documentation. Documentation that refers to moved/deleted code is flagged as broken. Docs contain live code spanning multiple files.
- Applicability: CI step that re-checks evidence citations (file:line still exists, content at line unchanged) on every PR = the Swimm model applied to the harness KB.

**Automated trace link generation (known limitation):**
- Source: [ResearchGate: Rule-based Requirements Traceability](https://www.researchgate.net/publication/223738681_Rule-based_generation_of_requirments_traceability_relations)
- IR + ML trace link approaches "fail to understand semantics or integrate domain knowledge → imprecise results." Rule-based is more precise but brittle to refactoring.
- Applicability: for DDD code where naming conventions are deliberate (aggregate method names encode business intent), rule-based + Roslyn semantic model is more reliable than pure IR approaches.

---

### Roslyn as the Static Analysis Engine — .NET-Specific Synthesis

For a .NET DDD codebase, **Roslyn replaces all COBOL-era program slicing tools.** It provides the pre-processing step that every production tool (IBM, Swimm, AWS Transform) demonstrates is mandatory before LLM invocation.

- Sources: [Analysing .NET Codebase with Roslyn (DEV.to)](https://dev.to/mattjhosking/analysing-a-net-codebase-with-roslyn-5cn0) | [Roslyn C# Semantic Analysis Getting Started](https://github.com/dotnet/roslyn/blob/main/docs/wiki/Getting-Started-C%23-Semantic-Analysis.md)
- `CSharpSyntaxWalker` + `SemanticModel.GetTypeInfo()` / `ITypeSymbol` traversal can: identify aggregate roots and value objects by base class inspection; extract guard clauses and `throw` expressions from constructors/factory methods; parse FluentValidation chains (`RuleFor()`, `Must()`, `WithMessage()`); build a `{file:line → code segment}` evidence index with fully resolved symbol names.
- `Compilation.GetSemanticModel()` resolves overloaded types and cross-assembly references → eliminates the ambiguity problem Swimm identifies as the primary LLM failure point on cryptic identifiers.

**FluentValidation extraction note (DDD-specific):**
- Source: [Microsoft: Domain Model Layer Validations](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-model-layer-validations) | [Enterprise Craftsmanship: Validation and DDD](https://enterprisecraftsmanship.com/posts/validation-and-ddd/)
- FV validators belong to the application layer per DDD canon. However, in CQRS/FastEndpoints stacks, FV validators encode business constraints not expressed in domain entities (especially request-level business rules). Both sources must be mined: guard clauses in domain constructors (invariants) AND FV validators in application layer (request constraints). The KB schema field `source_layer: domain | application` distinguishes them.

---

### TOP 5 DESIGN IMPLICATIONS

**1. Two-stage grounded pipeline, not end-to-end LLM.**
Every production-grade tool in this research (IBM ADDI + watsonx, Swimm, AWS Transform Reimagine, SLD-Spec) uses the identical two-layer pattern: deterministic static analysis first (evidence index, symbol resolution, lineage tracing, infrastructure noise removal) → LLM second on pre-processed, grounded context. Single-pass end-to-end LLM extraction tops out at 71.6% accuracy with fabrication as the primary failure mode. For this .NET DDD codebase, Roslyn is the static analysis layer; the LLM sees annotated code slices with resolved symbol names, not raw files. This is not an optimization — it is the architectural requirement.

**2. Confidence score must be multi-source, not single-LLM.**
LogicScan's majority-voting, Daikon's test-confirmation model, and BMC's per-assertion hallucination scoring all converge on the same principle: confidence is earned by corroboration across independent evidence channels. Proposed score components: (a) static presence of code construct — Roslyn finds guard clause at file:line = high weight; (b) LLM extraction consistency across N≥3 independent passes with majority vote; (c) dynamic test coverage confirmation — a test exercises the rule's code path and passes; (d) human review stamp. Rules failing (a) are rejected at intake without promotion to candidate. Rules that pass only (a) are candidates. All four channels required for verified status.

**3. Evidence citation (file:line) is the rejection gate, not metadata.**
AgentPex's explicit-only extraction policy and the entire traceability literature converge: a rule without a grounded code location is a hallucination risk, not knowledge. The KB schema must enforce `evidence` as a non-nullable array with at minimum one entry. Rules where the LLM cannot ground its claim to a specific file:line:snippet are immediately `status: rejected`. This is the primary structural anti-hallucination mechanism — it forces grounding before the claim is stored.

**4. Rule rot requires a CI hook, not human discipline.**
Swimm's PR auto-sync, EvolveWare's change-impact reporting, and RTM-in-CI research all show the same result: manual traceability maintenance degrades within weeks on active codebases. The harness needs a CI step that: (a) on every PR computes `git diff` → affected files → rules with evidence entries in those files → re-triggers extraction on that subset only (not full re-run); (b) detects evidence citation breakage (file moved, line deleted, content at line changed by diff); (c) sets `status: stale` on broken citations and blocks merge if any verified rule is stale. Full re-extraction only on domain model refactors.

**5. Extracted rules inherit code bugs — test validation is the promoted-to-verified gate.**
The post-condition formalization benchmark (arXiv:2603.17193) explicitly documents that specs extracted from code inherit existing bugs. An extracted invariant stating "balance must be ≥ 0 after withdrawal" is only correct if the withdrawal implementation is correct. The harness must not promote a rule from `candidate` to `verified` without a test that (a) exercises the rule's code path and (b) passes. The passing test is the independently observable evidence that the rule is both present and correct. This creates the required loop: extract rule (static + LLM) → auto-generate test targeting the evidence location → run test → green = verified; red = flag implementation or rule as suspect and escalate to human.

================================================================================

The research is complete. Here is the dense synthesis returned to you:

---

## Research: Business Rule Extraction from Code — Mining, Documenting, and Operationalizing

---

### THREAD 1: Academic Field — Static Analysis, Program Slicing, Known Accuracy Limits

**Classification: Established practice (1990s–2010s), COBOL-dominated, limited .NET literature.**

**Sneed (1996) — Seminal Paper**
- Source: [IEEE Xplore](https://ieeexplore.ieee.org/document/544158/) | [ResearchGate](https://www.researchgate.net/publication/221029319_Business_Rule_Extraction_from_Legacy_Code)
- Combines variable classification, backward program slicing on Program Dependence Graphs, and hierarchical abstraction. Frames code as more trustworthy than docs because docs drift. Tested on industrial COBOL programs.
- Applicability: foundational justification for code-as-ground-truth; confidence scoring philosophy maps directly.

**Model-Driven Reengineering / Backward Slicing (Academia.edu, 2016)**
- Source: [Academia.edu](https://www.academia.edu/29685854/Getting_Objects_Methods_and_Interactions_by_Extracting_Business_Rules_from_Legacy_Systems)
- Four-step pipeline: restructure → slice into logic modules → multi-view analysis → integrate views. Reports recall >85% on COBOL with backward slicing on PDGs. Precision unreported.
- Applicability: four-step structure is directly transplantable to a DDD agent pipeline. 85% recall is the best published academic number.

**Huang et al. (1996) — SOFT-REDOC**
- Source: [ResearchGate](https://www.researchgate.net/publication/3629947_Extracting_business_rules_from_source_code)
- Data output identification + program stripping isolates BRs from infrastructure. Key finding: 70–80% of code is infrastructure, not business logic. Pre-filtering before LLM is essential.
- Applicability: signal-to-noise ratio problem is universal, not COBOL-specific.

**Known accuracy limits across all surveys:**
- No single method (program-centric vs. data-centric) extracts all rules — confirmed by [IJCSIT 2015 survey](https://www.ijcsit.com/docs/Volume%206/vol6issue03/ijcsit2015060336.pdf)
- 85% recall (backward slicing, COBOL) is the best academic ceiling; precision figures absent from literature
- False positives increase when validation logic and infrastructure code are interleaved — exactly the DDD application-layer problem

**Specification Mining Surveys:**
- LTL specification mining PRISMA survey covering 3,000+ papers 2000–2024 (referenced in search results)
- Static predicate mining (Ramanathan et al., PLDI 2007): [PDF](https://www.cs.purdue.edu/homes/suresh/papers/pldi07.pdf)
- Scenario + value-based combined mining: [paper](https://www.se-rwth.de/publications/Scenario-based-and-value-based-specification-mining-better-together.pdf) — richer specs from combined approaches

---

### THREAD 2: Industry / Legacy-Modernization Tooling

**IBM watsonx Code Assistant for Z + ADDI**
- Sources: [ADDI Overview Docs](https://www.ibm.com/docs/en/watsonx/watsonx-code-assistant-4z/2.x?topic=addi-overview) | [Business Rules Discovery (Mediacenter)](https://mediacenter.ibm.com/media/IBM+watsonx+Code+Assistant+for+Z+Understand:+Business+rules+discovery/1_5pr695pa/189147203) | [Agentic AI Announcement](https://www.ibm.com/new/announcements/agentic-ai-for-smarter-mainframe-modernization-with-ibm-watsonx-code-assistant-for-z)
- Classification: established product (COBOL/PL/I/Assembler only)
- Architecture: ADDI scans source → builds call hierarchies + dependency graphs → populates Metadata DB → watsonx LLM consumes grounded metadata. User provides keywords → filters programs/variables → visualizes connections → reviews rules. Data dictionaries feed the LLM to resolve cryptic abbreviations. v2.8 uses MCP for tool connectivity (agentic workflow).
- Key insight: "code-aware segmentation reduces common pitfalls of generic LLM chunking." Grounded metadata prevents hallucination on variable names.
- Applicability: the two-layer pattern (deterministic static analysis produces metadata → LLM consumes grounded metadata) is the proven production pattern. Direct parallel: Roslyn → LLM.

**EvolveWare Intellisys**
- Sources: [Agile BRE](https://evolveware.com/agile-business-rules-extraction/) | [BRE Blog](https://blog.evolveware.com/business-rules-extraction-why-understanding-rules-vs-logic-is-important/)
- Classification: established product (COBOL, PL/1, Natural, Assembler, **C#, Java, VB.NET** — .NET relevant)
- Methodology: pre-processing (dead code removal, variable renaming) → automated extraction → multiple cataloging levels → plain-English annotation. Continuous update: code changes trigger automated reports identifying impacted rules.
- Scale: 31M LOC (NY state), 1.5M LOC (healthcare insurer), 2.6M LOC (US Air Force). No accuracy percentages published.
- Applicability: pre-processing before extraction is essential. Continuous-update model is the reference design for rule rot prevention. Explicitly supports C#.

**Micro Focus (OpenText) COBOL Analyzer — Business Rule Manager**
- Sources: [BRM docs v5](https://www.microfocus.com/documentation/cobol-analyzer/ca503/CA/GUID-3521F158-A453-41C2-8851-870946F0821D.html) | [Extracting BRs docs](https://www.microfocus.com/documentation/cobol-analyzer/ca60/CA/GUID-64A974DE-F7E2-4D71-AEAC-45397C1B1996.html)
- Classification: established product (COBOL-centric)
- BRM batch-edits rule attributes, matches I/O data elements against glossary. Heuristic extraction explicitly documented to "miss key logic or include unnecessary details, requiring costly manual review." COBRAIN (LLM/few-shot) being added.
- Applicability: illustrates that pure-heuristic extraction without LLM grounding has known high false-positive/miss rate. Manual validation loop is non-negotiable.

**AWS Transform (Reimagine pathway) + Blu Age**
- Sources: [Reimagine announcement](https://aws.amazon.com/blogs/aws/aws-transform-for-mainframe-introduces-reimagine-capabilities-and-automated-testing-functionality/) | [Blu Insights refactor blog](https://aws.amazon.com/blogs/mt/aws-mainframe-modernization-refactor-legacy-code-base-to-java-using-aws-blu-insights/)
- Classification: established product (COBOL/JCL/mainframe); Reimagine pathway launched May 2025
- Reimagine extracts BRs across four hierarchical levels: Line of Business → Business Functions/Domains → Business Features → Component-Level Rules. Specialized agents call deterministic tools (not general-purpose LLM pointed at a repo). Outputs include "rule extractions with testable acceptance criteria." Human-in-the-loop mandatory.
- Applicability: four-level hierarchy maps to DDD layers (bounded context → aggregate → method → guard clause/invariant). Testable acceptance criteria output matches the harness goal exactly.

**Swimm.io**
- Sources: [Enterprise page](https://swimm.io/enterprise) | [Blackbox to Blueprint blog](https://swimm.io/blog/blackbox-to-blueprint-extracting-business-logic-from-cobol-applications) | [5 COBOL BRE Tools](https://swimm.io/learn/cobol/5-cobol-business-rules-extraction-tools-to-know-in-2026)
- Classification: emerging product (IDE + CI integration); Gartner Cool Vendor 2024
- Mechanism: deterministic static analysis (dependency tracing, data flow, dead code detection) + AI agents. Extracts Event-Condition-Action rules (ON `<Event>` IF `<Condition>` THEN `<Action>`). Variable lineage tracing across files resolves ambiguity before LLM invocation. Auto-sync on PR.
- Critical finding from blog: "LLMs cannot resolve meaning from cryptic variable names without cross-file context; static analysis must disambiguate scope first." 70–80% of code is infrastructure; signal-to-noise filtering is the core challenge.
- No quantitative accuracy benchmarks published.
- Applicability: ECA (Event-Condition-Action) is the right extraction target for DDD domain methods. Auto-sync on PR = direct reference for CI-based rule rot prevention.

**CodeLogic — Correction**
- Source: [codelogic.com](https://codelogic.com/)
- Misidentified in the original query. CodeLogic is a library/framework upgrade automation tool (.NET, SQL, Java, JavaScript). No business rule extraction, no documentation traceability. Useful only for dependency impact analysis when rules change.

---

### THREAD 3: Specification Mining and Invariant Detection

**Daikon — Dynamic Invariant Detection (Ernst et al., 2001/2007)**
- Sources: [Official site](https://plse.cs.washington.edu/daikon/) | [SCP 2007 paper PDF](https://homes.cs.washington.edu/~mernst/pubs/daikon-tool-scp2007.pdf) | [ACM DL](https://dl.acm.org/doi/10.1016/j.scico.2007.01.015)
- Classification: established research tool
- Instruments program → runs tests → infers likely invariants from value traces via generate-and-check. Detects: constants, ranges, linear relationships, ordering, containment, sortedness. Language-independent trace format; supports C, C++, Java, Perl. .NET/C# front-end (Celeriac) exists but not production-standard.
- Critical limitation: high false-positive rate because invariants are confirmed only against available test cases. "These assertions might not be valid since they only hold on the test runs."
- Applicability: Daikon-style dynamic detection is a strong candidate-generation mechanism. Pipeline: run test suite → Daikon → candidate invariants → LLM business-language reformulation → human confirm = 3-stage pipeline. High false-positive rate means confidence scoring is mandatory before promotion.

**SLD-Spec: LLM + Program Slicing for Specification Generation (2025)**
- Source: [arXiv:2509.09917](https://arxiv.org/abs/2509.09917)
- Classification: emerging research
- Slices target function → LLM generates specs per slice → logical deletion stage filters unsound candidates. Consistently outperforms baselines. No numeric precision/recall in abstract.
- Applicability: confirms program slicing before LLM is the correct preprocessing. Logical deletion (LLM self-filtering over candidate set) is a viable confidence gate implementation.

**LogicScan — Business Invariant Mining for Smart Contracts (2025)**
- Source: [arXiv:2602.03271](https://arxiv.org/pdf/2602.03271)
- Classification: emerging research
- Logic Miner transforms raw code into normalized, verifiable business invariants. Multi-dimensional code extraction + majority-voting rule aggregation reduces false positives from conflicting rules.
- Applicability: majority-voting aggregation across multiple LLM passes is a concrete mechanism to compute confidence scores. Conflicted rules from redundant validation is an important failure mode in DDD stacks with both domain guard clauses and FV validators.

---

### THREAD 4: LLM-Era Approaches — Measured Numbers

**Extracting Formal Specifications from Documents Using LLMs (Li et al., arXiv 2025)**
- Source: [arXiv:2504.01294](https://arxiv.org/abs/2504.01294)
- Best accuracy: **71.6%** (GPT-4o, Claude, Llama; from documents, not directly from code). Two-stage annotation-then-conversion improves over end-to-end by **29.2%**. Failure modes: oversimplification and fabrication.
- Applicability: 71.6% is the reference ceiling for document-derived spec accuracy. Annotation-then-conversion is the recommended harness pipeline structure.

**Talk is Cheap, Logic is Hard: Benchmarking LLMs on Post-Condition Formalization (arXiv 2025)**
- Source: [arXiv:2603.17193](https://arxiv.org/abs/2603.17193)
- 24 LLMs on 40 formal pre/postcondition tasks. Proprietary > open-source; no LLM correctly formalized all tasks; LLMs perform better on pre-conditions than post-conditions; automated test suites expose incorrect solutions that manual review misses.
- **Critical finding: "specifications extracted from code may inherit bugs already present in the code."**
- Applicability: post-conditions map to DDD aggregate invariants (state after method call). Do not promote a rule to verified without a passing test.

**LLM Hallucination Rates in Code (2024–2025)**
- Sources: [Diffray blog](https://diffray.ai/blog/llm-hallucinations-code-review/) | [ACM 2025](https://dl.acm.org/doi/pdf/10.1145/3728894)
- GPT-4 Turbo: 4% package hallucination; CodeLlama 7B: 26%. 29–45% of AI-generated code contains security vulnerabilities.
- BMC per-assertion hallucination scoring pipeline: assigns per-claim reliability score by cross-referencing against source data. This is the reference design for per-rule confidence scoring.
- Applicability: per-rule confidence scoring is feasible at assertion level. The BMC pattern = harness confidence score implementation.

**AgentPex — Explicit-Only Rule Extraction (arXiv 2025)**
- Source: [arXiv:2603.23806](https://arxiv.org/pdf/2603.23806)
- Strict explicit-only extraction policy: "extract only rules directly stated in source material; avoid inferred or assumed constraints." Per-specification independent LLM invocation. Produces Policy, Output, and Transition specifications.
- Applicability: explicit-only policy is the correct extraction discipline. Rules not grounded in a specific code location are rejected at intake.

**AlphaCodium multi-stage structured flow (Ridnik et al., 2024)**
- Referenced in [arXiv:2604.05150](https://arxiv.org/html/2604.05150)
- Structured multi-stage flows with verification improve GPT-4 coding accuracy from **19% → 44%**.
- Applicability: single-pass extraction is insufficient. Verification loops are required to reach acceptable accuracy.

**Frontiers Systematic Review: LLMs for Requirements Engineering (2025)**
- Source: [Frontiers Computer Science](https://www.frontiersin.org/journals/computer-science/articles/10.3389/fcomp.2025.1519437/full)
- 29 studies; metrics fragmented (BLEU, ROUGE, F1, Kappa); no unified benchmark. Hallucination flagged as "critical concern" but no aggregate rate quantified. Contextual prompts outperform zero-shot.
- Applicability: any precision/recall numbers for the harness must be generated internally with a held-out ground truth set. External benchmarks are not applicable proxies.

---

### THREAD 5: Rule Documentation Formats

**SBVR (Semantics of Business Vocabulary and Rules)**
- Sources: [SBVR ↔ DMN (BRCommunity)](https://www.brcommunity.com/articles.php?id=b597) | [Evolute overview](https://www.evolute.be/thoughts/rules.html)
- Classification: established OMG standard
- Formal metamodel: rules built from fact types, from terms. Structured natural language. Favored by legal/compliance/QA. IT adoption limited. Many-to-many relationship with decisions. Each rule fires when a business condition threatens to occur (traceable invocation model).
- Verdict: too verbose for agent-managed KB; useful as a semantic vocabulary layer for term disambiguation.

**DMN (Decision Model and Notation)**
- Sources: [Trisotech DMN](https://www.trisotech.com/dmn/) | [SBVR→DMN translation paper](https://www.researchgate.net/publication/303626206_From_SBVR_to_BPMN_and_DMN_Models_Proposal_of_Translation_from_Rules_to_Process_and_Decision_Models)
- Classification: established OMG standard (executable)
- Decision Tables + Decision Requirements Graph. Executable by non-programmers. Bidirectional traceability (rule ↔ decision ↔ process). SBVR→DMN translation algorithm exists.
- Verdict: appropriate for rules with multi-outcome branching logic. Not practical for behavioral invariants (guard clauses, state assertions).

**Practical KB Entry Schema (synthesized from research):**

No published standard covers the full evidence-citation + confidence-score requirement. Recommended minimal schema:

```
rule_id:              stable identifier (RULE-{domain}-{seq})
title:                short business-language name
category:             structural | operational | derivation | constraint
statement:            plain-language rule text
evidence:             [{file, start_line, end_line, snippet}]  ← required, reject if absent
confidence_score:     0.0–1.0  (multi-source: static + LLM votes + test coverage)
invariant_type:       precondition | postcondition | invariant | validation
source_layer:         domain | application
test_ids:             [test names that pin this rule]
last_verified_commit: SHA
status:               verified | candidate | rejected | stale
```

---

### THREAD 6: Rule Rot — Synchronization and Traceability

**Traceability Aspects for Business Rules Management (BRCommunity, 2008)**
- Source: [brcommunity.com/articles.php?id=b384](https://www.brcommunity.com/articles.php?id=b384)
- Ten traceability types: Vocabulary, Rule Reuse, Temporal, Business Policy, Processing (runtime hook points), Test Case, Deployment, SOA services, Work List, Compliance.
- Anti-rule-rot mechanisms: Processing Traceability (hook points capture which rules fired at runtime); Temporal Traceability (rule-sets agnostic of effective dates); Regression test scaffolding with captured expectations.
- Key warning: "building traceability into your Architecture is an evolutionary process. Vendor products come only half way."
- Applicability: `last_verified_commit` + CI hook re-evaluating rules against changed files = implementation of Processing + Temporal traceability.

**EvolveWare continuous update model:**
- Source: [evolveware.com/agile-business-rules-extraction/](https://evolveware.com/agile-business-rules-extraction/)
- When code changes, automated reports highlight impacted rules only; unaffected rules remain unchanged.
- Applicability: `git diff` → affected file set → subset re-extraction only. Full re-extraction on every commit is not scalable.

**RTM in CI/CD (2025–2026):**
- Source: [Medium: Traceability Matrix in CI/CD Workflows](https://medium.com/@sancharini.panda/how-a-traceability-matrix-fits-into-modern-ci-cd-workflows-714c5a6862af)
- "Once the gap widens between matrix and code, the matrix no longer serves as a current representation." Continuous traceability reflects latest coverage in real time.

**Swimm auto-sync model (gold standard reference):**
- Source: [swimm.io](https://swimm.io/)
- On every PR, Swimm automatically verifies and updates documentation. Documentation that refers to moved/deleted code is flagged as broken.
- Applicability: CI step that re-checks evidence citations (file:line still exists, content at line unchanged) on every PR = the Swimm model applied to the harness KB.

**Automated trace link generation (known limitation):**
- Source: [ResearchGate: Rule-based Requirements Traceability](https://www.researchgate.net/publication/223738681_Rule-based_generation_of_requirments_traceability_relations)
- IR + ML approaches "fail to understand semantics or integrate domain knowledge → imprecise results." Rule-based is more precise but brittle to refactoring.
- Applicability: for DDD code where naming conventions are deliberate, rule-based + Roslyn semantic model is more reliable than pure IR approaches.

---

### Roslyn as the .NET Static Analysis Engine — Synthesis

For a .NET DDD codebase, **Roslyn replaces all COBOL-era program slicing tools.** It provides the pre-processing step that every production tool (IBM, Swimm, AWS Transform) demonstrates is mandatory before LLM invocation.

- Sources: [Analysing .NET Codebase with Roslyn (DEV.to)](https://dev.to/mattjhosking/analysing-a-net-codebase-with-roslyn-5cn0) | [Roslyn C# Semantic Analysis Getting Started](https://github.com/dotnet/roslyn/blob/main/docs/wiki/Getting-Started-C%23-Semantic-Analysis.md)
- `CSharpSyntaxWalker` + `SemanticModel.GetTypeInfo()` / `ITypeSymbol` traversal can: identify aggregate roots and value objects by base class inspection; extract guard clauses and `throw` expressions from constructors/factory methods; parse FluentValidation chains (`RuleFor()`, `Must()`, `WithMessage()`); build a `{file:line → code segment}` evidence index with fully resolved symbol names.
- `Compilation.GetSemanticModel()` resolves overloaded types and cross-assembly references, eliminating the ambiguity problem Swimm identifies as the primary LLM failure point.

**FluentValidation note (DDD-specific):**
- Sources: [Microsoft: Domain Model Layer Validations](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-model-layer-validations) | [Enterprise Craftsmanship: Validation and DDD](https://enterprisecraftsmanship.com/posts/validation-and-ddd/)
- FV validators belong to the application layer per DDD canon, but in CQRS/FastEndpoints stacks they encode business constraints not expressed in domain entities. Both sources must be mined: guard clauses in domain constructors (invariants) AND FV validators in the application layer (request constraints). The KB field `source_layer: domain | application` distinguishes them.

---

### TOP 5 DESIGN IMPLICATIONS

**1. Two-stage grounded pipeline, not end-to-end LLM.**
Every production-grade tool in this research — IBM ADDI + watsonx, Swimm, AWS Transform Reimagine, SLD-Spec — uses the identical two-layer pattern: deterministic static analysis first (evidence index, symbol resolution, lineage tracing, infrastructure noise removal) → LLM second on pre-processed, grounded context. Single-pass end-to-end LLM extraction tops out at 71.6% accuracy with fabrication as the primary failure mode. For this .NET DDD codebase, Roslyn is the static analysis layer; the LLM sees annotated code slices with resolved symbol names, not raw files. This is not an optimization — it is the architectural requirement.

**2. Confidence score must be multi-source, not single-LLM.**
LogicScan's majority-voting, Daikon's test-confirmation model, and BMC's per-assertion hallucination scoring all converge on the same principle: confidence is earned by corroboration across independent evidence channels. Proposed score components: (a) static presence of code construct — Roslyn finds guard clause at file:line = high weight; (b) LLM extraction consistency across N≥3 independent passes with majority vote; (c) dynamic test coverage confirmation — a test exercises the rule's code path and passes; (d) human review stamp. Rules failing (a) are rejected at intake. Rules passing only (a) are candidates. All four channels required for verified status.

**3. Evidence citation (file:line) is the rejection gate, not metadata.**
AgentPex's explicit-only extraction policy and the entire traceability literature converge: a rule without a grounded code location is a hallucination risk, not knowledge. The KB schema must enforce `evidence` as a non-nullable array with at minimum one entry. Rules where the LLM cannot ground its claim to a specific file:line:snippet are immediately `status: rejected`. This is the primary structural anti-hallucination mechanism — it forces grounding before the claim is stored.

**4. Rule rot requires a CI hook, not human discipline.**
Swimm's PR auto-sync, EvolveWare's change-impact reporting, and RTM-in-CI research all show the same result: manual traceability maintenance degrades within weeks on active codebases. The harness needs a CI step that: (a) on every PR computes `git diff` → affected files → rules with evidence entries in those files → re-triggers extraction on that subset only; (b) detects evidence citation breakage (file moved, line deleted, content at line changed by diff); (c) sets `status: stale` on broken citations and blocks merge if any verified rule is stale. Full re-extraction only on domain model refactors.

**5. Extracted rules inherit code bugs — test validation is the verified gate.**
The post-condition formalization benchmark (arXiv:2603.17193) explicitly documents that specs extracted from code inherit existing bugs. An extracted invariant is only correct if the implementation is correct. The harness must not promote a rule from `candidate` to `verified` without a test that (a) exercises the rule's code path and (b) passes. The passing test is the independently observable evidence that the rule is both present and correct. This creates the required loop: extract rule (static + LLM) → auto-generate test targeting the evidence location → run test → green = verified; red = flag implementation or rule as suspect, escalate to human.

================================================================================

The research is complete and returned above. Key dense findings the architect should act on immediately:

**The non-negotiable architectural constraint** found across every production tool (IBM watsonx/ADDI, AWS Transform Reimagine, Swimm, SLD-Spec): deterministic static analysis must precede LLM invocation. Single-pass LLM extraction has a 71.6% accuracy ceiling with fabrication as the dominant failure mode. For this .NET DDD codebase, Roslyn is the static analysis layer — it resolves symbols, traces cross-assembly references, and builds the file:line evidence index before the LLM sees anything.

**The five design implications are the actionable distillation:**

1. Two-stage pipeline (Roslyn pre-processes → LLM consumes grounded slices) — not optional
2. Confidence score requires 4 independent channels: static presence + LLM majority vote (N≥3) + test coverage confirmation + human stamp
3. Evidence citation (file:line:snippet) is a hard rejection gate — no grounded location, no KB entry
4. Rule rot prevention requires a CI hook on every PR: git diff → affected file set → subset re-extraction only → stale flag on broken citations, not human discipline
5. Rules inherit code bugs — `candidate → verified` promotion is gated by a passing test, not LLM confidence alone

**Key numbers to cite in the design doc:**
- 71.6%: best LLM accuracy on formal spec extraction from documents (arXiv:2504.01294)
- 29.2%: improvement from annotation-then-conversion over end-to-end LLM (same paper)
- 19% → 44%: GPT-4 accuracy jump from single-pass to multi-stage verification (AlphaCodium)
- 85%: best recall from backward program slicing on COBOL PDGs (academic ceiling)
- 4% vs 26%: GPT-4 Turbo vs. CodeLlama 7B package hallucination rates
- 70–80%: proportion of code that is infrastructure noise, not business logic

**The critical .NET-specific finding:** Roslyn `CSharpSyntaxWalker` + `SemanticModel` + `ITypeSymbol` is the exact toolchain for identifying aggregate roots, value objects, guard clauses, and FluentValidation chains by base type inspection and symbol resolution. No third-party static analysis tool needed. The Roslyn semantic model eliminates the variable ambiguity problem Swimm identifies as the primary LLM failure point on cryptic identifiers.

**FluentValidation dual-source requirement:** Both domain guard clauses (constructors, factory methods) AND application-layer FV validators must be mined. The KB schema needs `source_layer: domain | application` to distinguish them, since they represent different categories of business constraint even though both encode business rules.

================================================================================

The research is complete. The document-specialist returned the full synthesis above. Here is a compact routing summary for the architect:

---

**What was researched:** Six parallel threads covering academic BRE literature, industry tooling (IBM/EvolveWare/Micro Focus/AWS/Swimm), Daikon-style specification mining, LLM accuracy measurements, rule documentation standards, and rule rot solutions.

**The single most important finding:** Every production-grade tool converges on the same two-layer architecture — deterministic static analysis first, LLM second on grounded context. This is not a design choice; it is the invariant across IBM ADDI+watsonx, AWS Transform Reimagine, Swimm, and SLD-Spec. For this codebase, **Roslyn is the static analysis layer**.

**Hard numbers available for the design doc:**

| Metric | Value | Source |
|---|---|---|
| Best LLM accuracy on spec extraction | 71.6% | arXiv:2504.01294 |
| Two-stage pipeline improvement over end-to-end | +29.2% correct specs | arXiv:2504.01294 |
| Single-pass → multi-stage verification gain | 19% → 44% | AlphaCodium via arXiv:2604.05150 |
| Best academic recall (backward slicing, COBOL) | 85% | Academia.edu MDR paper |
| Code that is infrastructure noise, not business logic | 70–80% | Swimm / Huang 1996 |
| GPT-4 Turbo hallucination rate (packages) | 4% | Diffray / ACM 2025 |
| CodeLlama 7B hallucination rate (packages) | 26% | Diffray / ACM 2025 |

**Five design implications** are fully elaborated in the synthesis above. The five gates in order: (1) Roslyn pre-processing is mandatory, (2) confidence requires 4 independent channels, (3) file:line evidence is a hard rejection gate, (4) rule rot requires a CI hook not human discipline, (5) `candidate → verified` is gated by a passing test not LLM confidence.

**One critical warning from arXiv:2603.17193:** Extracted rules inherit bugs present in the implementation. A rule extracted from a buggy aggregate is a buggy rule. The test gate is not just quality control — it is the correctness verification mechanism.

================================================================================

The research is complete. Everything the architect needs is in the three message blocks above from the document-specialist. No further lookup is needed — all six threads are covered with citations, accuracy numbers, and the five design implications.

**Quick orientation for the architect:**

- Full thread-by-thread synthesis: first large block above
- Key numbers table and routing summary: second block above  
- This message: navigation only

The research is ready to feed directly into the architecture proposal. The five design implications at the end of the synthesis are the load-bearing conclusions — everything else is the evidence base behind them.

================================================================================

## Research: Business Rule Extraction from Code — Mining, Documenting, and Operationalizing

---

### THREAD 1: Academic Field — Static Analysis, Program Slicing, Known Accuracy Limits

**Classification: Established practice (1990s–2010s), COBOL-dominated, limited .NET literature.**

**Sneed (1996) — Seminal Paper**
- Source: [IEEE Xplore](https://ieeexplore.ieee.org/document/544158/) | [ResearchGate](https://www.researchgate.net/publication/221029319_Business_Rule_Extraction_from_Legacy_Code)
- Classification: Established practice
- Combines variable classification, backward program slicing on Program Dependence Graphs, and hierarchical abstraction. Frames code as more trustworthy than documentation because docs drift — organizations trust code over any other artifact. Tested on industrial COBOL programs.
- Applicability: foundational justification for code-as-ground-truth; the confidence scoring philosophy (reject rules without code evidence) maps directly to this framing.

**Model-Driven Reengineering / Backward Slicing (Academia.edu, 2016)**
- Source: [Academia.edu](https://www.academia.edu/29685854/Getting_Objects_Methods_and_Interactions_by_Extracting_Business_Rules_from_Legacy_Systems)
- Classification: Established research
- Four-step pipeline: restructure → slice into logic modules → multi-view analysis (data view, decision view, procedural flow) → integrate views into unified documentation. Uses backward static slicing on Program Dependence Graphs. Reports recall >85% on COBOL. Precision unreported.
- Applicability: the four-step structure (pre-filter, slice, multi-view, integrate) is directly transplantable to a DDD layer agent pipeline. 85% recall is the best published academic number for any BRE approach.

**Huang et al. (1996) — SOFT-REDOC**
- Source: [ResearchGate](https://www.researchgate.net/publication/3629947_Extracting_business_rules_from_source_code)
- Classification: Established practice
- Data output identification + program stripping to isolate BRs from infrastructure code. Introduces SOFT-REDOC reverse-engineering tool for COBOL. Key finding: 70–80% of code is infrastructure, not business logic. Pre-filtering before LLM is essential.
- Applicability: the signal-to-noise ratio problem is universal, not COBOL-specific. A .NET domain layer has infrastructure code interleaved with business logic.

**Known accuracy limits across all surveys:**
- No single method (program-centric vs. data-centric) extracts all rules — confirmed by [IJCSIT 2015 survey](https://www.ijcsit.com/docs/Volume%206/vol6issue03/ijcsit2015060336.pdf)
- 85% recall (backward slicing, COBOL) is the best academic ceiling; precision figures are absent from the published literature
- False positives increase when validation logic and infrastructure code are interleaved — exactly the DDD application-layer problem (FV validators alongside domain guard clauses)
- IJCSIT conclusion: "no individual method is sufficient to extract all rules"

**Specification Mining Surveys:**
- LTL specification mining PRISMA survey covering 3,000+ papers 2000–2024 (referenced in search results, systematic review)
- Static predicate mining (Ramanathan et al., PLDI 2007): [PDF](https://www.cs.purdue.edu/homes/suresh/papers/pldi07.pdf) — infers pre/postconditions statically from data flow without test execution; trades recall for precision
- Scenario + value-based combined mining: [paper](https://www.se-rwth.de/publications/Scenario-based-and-value-based-specification-mining-better-together.pdf) — combining both approaches yields richer specifications
- Interactive semi-automated specification mining for debugging: [ScienceDirect](https://www.sciencedirect.com/science/article/abs/pii/S0950584919301004)

---

### THREAD 2: Industry / Legacy-Modernization Tooling

**IBM watsonx Code Assistant for Z + ADDI**
- Sources: [ADDI Overview Docs](https://www.ibm.com/docs/en/watsonx/watsonx-code-assistant-4z/2.x?topic=addi-overview) | [Business Rules Discovery (Mediacenter)](https://mediacenter.ibm.com/media/IBM+watsonx+Code+Assistant+for+Z+Understand:+Business+rules+discovery/1_5pr695pa/189147203) | [Agentic AI Announcement](https://www.ibm.com/new/announcements/agentic-ai-for-smarter-mainframe-modernization-with-ibm-watsonx-code-assistant-for-z)
- Classification: Established product (COBOL/PL/I/Assembler only — not .NET applicable)
- Architecture: ADDI scans source → builds call hierarchies + dependency graphs → populates Metadata Database → watsonx LLM consumes grounded metadata. User provides keywords → filters programs/variables → visualizes connections → reviews rules. Data dictionaries and business glossaries feed the LLM to resolve cryptic abbreviations. v2.8 uses MCP for tool connectivity (agentic workflow). ADDI and watsonx Code Assistant are designed to work together as a governed pipeline — neither tool alone is the answer.
- Key insight: "code-aware segmentation reduces common pitfalls of generic LLM chunking." Grounded metadata prevents hallucination on variable names.
- Applicability: the two-layer pattern (deterministic static analysis produces metadata → LLM consumes grounded metadata) is the proven production pattern for large codebases. Direct parallel for this project: Roslyn → LLM.

**EvolveWare Intellisys**
- Sources: [Agile BRE methodology](https://evolveware.com/agile-business-rules-extraction/) | [BRE Blog: Rules vs. Logic](https://blog.evolveware.com/business-rules-extraction-why-understanding-rules-vs-logic-is-important/) | [Financial Institutions case study](https://evolveware.com/business-rules-extraction-in-action-what-it-is-and-how-financial-institutions-can-do-it-right/)
- Classification: Established product (COBOL, PL/1, Natural, Assembler, **C#, Java, VB.NET** — .NET relevant)
- Methodology: pre-processing step (dead code removal, environment-specific block deactivation, disjointed code reunion, variable renaming for readability) → automated extraction → multiple cataloging levels → plain-English annotation for both technical and non-technical audiences. Continuous update feature: code changes trigger automated reports identifying which extracted rules are impacted; unaffected rules remain unchanged.
- Scale: 31M LOC (NY state), 1.5M LOC (healthcare insurer), 2.6M LOC (US Air Force). No accuracy percentages published — client testimonial uses "very high level of accuracy."
- Key feature: rule de-duplication tool, multiline flowchart generator, JIRA export, seed analysis. Explicitly supports C#.
- Applicability: pre-processing before extraction (dead code removal + variable renaming) is an essential step validated at enterprise scale. Their change-impact continuous-update model is the direct reference design for rule rot prevention.

**Micro Focus (OpenText) COBOL Analyzer — Business Rule Manager**
- Sources: [BRM documentation v5](https://www.microfocus.com/documentation/cobol-analyzer/ca503/CA/GUID-3521F158-A453-41C2-8851-870946F0821D.html) | [Extracting Business Rules docs](https://www.microfocus.com/documentation/cobol-analyzer/ca60/CA/GUID-64A974DE-F7E2-4D71-AEAC-45397C1B1996.html) | [Swimm 5 COBOL BRE Tools](https://swimm.io/learn/cobol/5-cobol-business-rules-extraction-tools-to-know-in-2026)
- Classification: Established product (COBOL-centric — not .NET applicable)
- Business Rule Manager (BRM) batch-edits rule attributes, propagates custom attributes, matches I/O data elements against a project glossary. Heuristic extraction approach explicitly documented to "miss key logic or include unnecessary details, requiring costly and time-consuming manual review." COBRAIN (LLM/few-shot prompting) is being added to supplement heuristics.
- Applicability: illustrates that pure-heuristic extraction without LLM grounding has a known high false-positive/miss rate. The manual validation loop is non-negotiable even in mature tooling.

**AWS Transform (Reimagine pathway) + Blu Age**
- Sources: [Reimagine capabilities announcement](https://aws.amazon.com/blogs/aws/aws-transform-for-mainframe-introduces-reimagine-capabilities-and-automated-testing-functionality/) | [Blu Insights refactor blog](https://aws.amazon.com/blogs/mt/aws-mainframe-modernization-refactor-legacy-code-base-to-java-using-aws-blu-insights/) | [AWS Transform comprehensive refactor](https://aws.amazon.com/blogs/migration-and-modernization/accelerate-mainframe-modernization-with-aws-transform-a-comprehensive-refactor-approach/)
- Classification: Established product (COBOL/JCL/mainframe); Reimagine pathway launched May 2025
- Reimagine extracts BRs across four hierarchical levels: Line of Business → Business Functions/Domains → Business Features → Component-Level Rules (specific BRs from COBOL and JCL files). Specialized agents call deterministic, purpose-built tools — not a general-purpose LLM pointed at a repository. Outputs include "rule extractions with testable acceptance criteria" and dependency graphs grounded in actual code structure. Human-in-the-loop validation is mandatory throughout.
- Key insight: "not a task solvable by pointing a general-purpose AI agent at a repository — the analysis produces reliable, repeatable foundations."
- Applicability: four-level hierarchy (Line of Business → Domain → Feature → Component-level rule) maps directly to DDD layers (bounded context → aggregate → method → guard clause/invariant). Testable acceptance criteria output matches the harness goal exactly.

**Swimm.io**
- Sources: [Enterprise page](https://swimm.io/enterprise) | [Blackbox to Blueprint blog](https://swimm.io/blog/blackbox-to-blueprint-extracting-business-logic-from-cobol-applications) | [5 COBOL BRE Tools](https://swimm.io/learn/cobol/5-cobol-business-rules-extraction-tools-to-know-in-2026) | [Feature updates](https://swimm.io/blog/feature-updates)
- Classification: Emerging product (IDE + CI integration); Gartner Cool Vendor 2024
- Mechanism: proprietary deterministic static analysis (dependency tracing, data flow, dead code detection, entry points, cross-repo relationships) + AI agents. Extracts Event-Condition-Action rules formatted as: ON `<Event>` IF `<Condition>` THEN `<Action>` ELSE `<Action>`. Variable lineage tracing across copybooks, BMS maps, and linked programs resolves ambiguity before LLM invocation. Auto-sync on PR: on every PR, Swimm automatically verifies and updates documentation; documentation that refers to moved/deleted code is flagged as broken.
- Critical finding from blog: "LLMs cannot resolve meaning from cryptic variable names without cross-file context; static analysis must disambiguate scope first." 70–80% of code is infrastructure; signal-to-noise filtering is the core challenge before LLM invocation.
- No quantitative accuracy benchmarks published.
- Applicability: ECA (Event-Condition-Action) is the right extraction target for DDD domain method behavior. The auto-sync-on-PR model is the direct reference for CI-based rule rot prevention. The "ambiguity resolution before LLM" finding is the key design constraint.

**CodeLogic — Correction**
- Source: [codelogic.com](https://codelogic.com/)
- Classification: Misidentified in the original query. CodeLogic is a library/framework upgrade automation tool (.NET, SQL, Java, JavaScript). No business rule extraction capability, no documentation traceability, no KB generation. Supports .NET dependency impact analysis for upgrade planning only.

---

### THREAD 3: Specification Mining and Invariant Detection

**Daikon — Dynamic Invariant Detection (Ernst et al., 2001/2007)**
- Sources: [Official Daikon site](https://plse.cs.washington.edu/daikon/) | [SCP 2007 paper PDF](https://homes.cs.washington.edu/~mernst/pubs/daikon-tool-scp2007.pdf) | [ACM DL](https://dl.acm.org/doi/10.1016/j.scico.2007.01.015) | [TSE 2001 paper PDF](https://homes.cs.washington.edu/~mernst/pubs/invariants-tse2001.pdf)
- Classification: Established research tool
- Instruments program → runs test suite → infers likely invariants from value traces via generate-and-check algorithm. Detects: constant values (`x = a`), ranges (`a ≤ x ≤ b`), linear relationships (`y = ax + b`), ordering (`x ≤ y`), containment (`x ∈ y`), sortedness, function relationships. Language-independent trace format (decls + dtrace files). Supports C, C++, Java, Perl. .NET/C# front-end (Celeriac) exists but is not production-standard.
- Critical limitation explicitly stated: "high rates of false positives, in part because they only leverage traced program values from available test cases, rather than directly exploiting knowledge of the source code per se." Invariants are confirmed only against available tests, not semantically. "These assertions might not be valid since they only hold on the test runs."
- Applicability: Daikon-style dynamic detection is a strong candidate-generation mechanism for the harness. Pipeline: run test suite → Daikon infers candidate invariants → LLM reformulates to business language → human confirms. High false-positive rate means confidence scoring is mandatory before any promotion to verified status.

**SLD-Spec: LLM + Program Slicing for Specification Generation (2025)**
- Source: [arXiv:2509.09917](https://arxiv.org/abs/2509.09917)
- Classification: Emerging research (2025)
- Two-stage system: (1) program slicing stage — breaks target function into smaller slices so LLM focuses on localized semantic structures rather than complex full-function control flow; (2) logical deletion stage — LLM reasons over candidate specifications and filters unsound ones. Addresses two failure modes in prior LLM-spec pipelines: inability to cover substructures precisely, and incorrect discarding of valid specs. Consistently outperforms existing methods. No numeric precision/recall in abstract.
- Applicability: confirms that program slicing before LLM invocation is the correct preprocessing step. Logical deletion (LLM self-filtering over a candidate set) is a viable confidence gate implementation for the harness.

**LogicScan — Business Invariant Mining for Smart Contracts (2025)**
- Source: [arXiv:2602.03271](https://arxiv.org/pdf/2602.03271)
- Classification: Emerging research (2025)
- Logic Miner module transforms raw code into explicit, normalized, verifiable business invariants through a unified logic mining pipeline. Multi-dimensional code extraction + majority-voting rule aggregation to reduce false positives from conflicting rules (caused by redundant security/validation designs in the codebase). Downstream: extracted invariants are compared across implementations for vulnerability detection.
- Applicability: majority-voting aggregation across multiple LLM passes is the concrete mechanism to compute confidence scores. Conflicted rules from redundant validation logic is an important failure mode in DDD stacks that have both domain guard clauses AND FV validators expressing overlapping constraints.

---

### THREAD 4: LLM-Era Approaches — Measured Precision/Recall and Hallucination Rates

**Extracting Formal Specifications from Documents Using LLMs (Li et al., arXiv 2025)**
- Source: [arXiv:2504.01294](https://arxiv.org/abs/2504.01294) | [PDF direct](https://arxiv.org/pdf/2504.01294)
- Classification: Emerging research (2025)
- Two-stage annotation-then-conversion pipeline: Stage 1 — LLM annotates/highlights relevant passages; Stage 2 — converts annotated content to formal specs. Dataset: 603 specifications from 37 documents across 3 open-source projects. LLMs evaluated: GPT-4o, Claude, Llama.
- Measured results: best-performing LLM accuracy **71.6%**; two-stage method improves correctly extracted specs by **29.2%** over end-to-end extraction; average accuracy improvement **14.0%** from two-stage pipeline.
- Failure modes: (1) specification oversimplification — nuance lost in single-query interactions; (2) specification fabrication — LLMs generate fictional requirements not present in source. Both failure modes map directly to the DDD rule extraction problem.
- Applicability: 71.6% is the reference ceiling for document-derived specification accuracy. The annotation-then-conversion two-stage architecture is the recommended harness pipeline structure. Note: these numbers are from documents, not directly from code; code-derived accuracy may differ.

**Talk is Cheap, Logic is Hard: Benchmarking LLMs on Post-Condition Formalization (arXiv 2025)**
- Source: [arXiv:2603.17193](https://arxiv.org/abs/2603.17193)
- Classification: Emerging research (2025)
- 24 state-of-the-art LLMs evaluated against a fresh dataset of 40 tasks requiring generation of full formal pre- and post-conditions from natural language program descriptions. Automated tests used to validate LLM-proposed solutions, exposing incorrect solutions that manual review missed.
- Measured results: proprietary models outperform open-source; no LLM correctly formalized all tasks; LLMs perform better on pre-conditions than post-conditions; proprietary models produce near-miss errors (incorrect but close); open-source models produce outright errors more frequently; automated test suite validation is essential.
- **Critical finding: "specifications extracted from code may inherit bugs already present in the code."**
- Applicability: post-conditions in formal verification map directly to DDD aggregate invariants (state assertions after a method call). Do not extract from suspect aggregates without a correctness gate. Automated test suite validation is mandatory before promoting a rule to verified status. Near-miss errors from proprietary models means confidence scoring must flag near-100% scores as suspect too.

**LLM Hallucination Rates in Code (2024–2025)**
- Sources: [Diffray: LLM Hallucinations in AI Code Review](https://diffray.ai/blog/llm-hallucinations-code-review/) | [ACM 2025: LLM Hallucinations in Practical Code Generation](https://dl.acm.org/doi/pdf/10.1145/3728894)
- Classification: Established measurement (2024–2025)
- Measured rates: GPT-4 Turbo hallucinated packages **4%** of the time; CodeLlama 7B **26%** of the time. **29–45%** of AI-generated code contains security vulnerabilities. **~20%** of package recommendations point to libraries that don't exist. LLMs optimized for code produced higher rates of package hallucinations than general models.
- BMC domain-specific hallucination detection pipeline: assigns a hallucination score to each resolution statement by cross-referencing against source data — per-assertion reliability quantification within structured engineering artifacts.
- ServiceNow 2025 framework: a validation model detects a specific fault property in an LLM output and computes a likelihood metric; if it exceeds a fault threshold, the output is labeled untrustworthy.
- Applicability: per-rule confidence scoring at assertion level is feasible and has production precedent (BMC pipeline). The BMC pattern = reference design for the harness confidence score. The 4% vs. 26% range across models informs model selection for the extraction agent.

**AgentPex — Explicit-Only Rule Extraction from Agentic Traces (arXiv 2025)**
- Source: [arXiv:2603.23806](https://arxiv.org/pdf/2603.23806)
- Classification: Emerging research (2025)
- AgentPex extracts specifications that govern how agentic traces are evaluated. Each specification category generated via an independent LLM invocation per trace. Strict explicit-only extraction policy: "the LLM is instructed to extract only concrete, checkable rules directly stated in the source material, and to avoid inferred or assumed constraints." Produces Policy specifications (high-level rules the system explicitly allows, requires, or forbids), Output specifications (constraints on formatting, style, refusal conditions), and Transition specifications (temporal/sequential constraints, ordering requirements, mutual exclusion rules).
- Applicability: the explicit-only extraction policy is the correct discipline for the harness. Rules not grounded in a specific code location are rejected at intake. Independent per-specification LLM invocations prevent cross-contamination between rule extractions.

**AlphaCodium multi-stage structured flow (Ridnik et al., 2024)**
- Referenced in [arXiv:2604.05150 — Compiled AI](https://arxiv.org/html/2604.05150)
- Classification: Established measurement (2024)
- Structured multi-stage flows with verification improve GPT-4 accuracy from **19% → 44%** on coding tasks. MetaGPT's Standard Operating Procedures reduce cascading hallucinations by constraining agent behavior through compiled workflows.
- Applicability: single-pass extraction is insufficient. Verification loops are required to reach acceptable accuracy. The 19% → 44% improvement from adding verification stages is the strongest argument for multi-pass pipeline design.

**Frontiers Systematic Review: LLMs for Requirements Engineering (2025)**
- Source: [Frontiers Computer Science](https://www.frontiersin.org/journals/computer-science/articles/10.3389/fcomp.2025.1519437/full)
- Classification: Established survey (2025)
- 29 studies analyzed; GPT models dominated (23/29 studies); most studies (23/29) applied LLMs without fine-tuning. RE stages covered: elicitation/analysis, modeling/specification, formal specification/verification, validation/QA. Metrics fragmented across studies: BLEU, ROUGE-L, F1, Cohen's Kappa, Pass@k — no unified benchmark standard. Hallucination flagged as "critical concern" producing "plausible yet incorrect outputs requiring extensive manual corrections" but no aggregate hallucination rate quantified. Contextual prompts outperform zero-shot for RE tasks (14 studies vs. least-used zero-shot).
- Core limitations identified: domain-specific knowledge gaps, output incompleteness/vagueness, structured input handling failures, token capacity constraints, lack of standardized benchmarks.
- Applicability: the absence of a unified benchmark means any precision/recall numbers for the harness must be generated internally against a held-out ground truth set. External benchmarks are not applicable proxies for this codebase's accuracy.

**"Talk is Cheap" supplementary — Benchmarks targeting code hallucinations (2024–2025):**
- Collu-Bench (Jiang et al., 2024), CodeHalu (Tian et al., 2025), APIHalBench (Chen et al., 2025) — three new benchmarks specifically targeting code hallucinations.
- Context-free grammars addressing syntax errors reduce them by **96% on Python and Go** — grammar-constrained generation is the strongest single mitigation strategy published.

---

### THREAD 5: Rule Documentation Formats

**SBVR (Semantics of Business Vocabulary and Rules)**
- Sources: [SBVR ↔ DMN relationship (BRCommunity)](https://www.brcommunity.com/articles.php?id=b597) | [Evolute overview of rules approach](https://www.evolute.be/thoughts/rules.html)
- Classification: Established OMG standard
- Formal metamodel: rules built from fact types, fact types built from terms (noun concepts + verb concepts). Rules expressed in structured natural language. Favored by legal/compliance/QA stakeholders — "welcomed by people responsible for legal compliance, internal policies, work instructions, and quality assurance." IT adoption limited — "not always welcomed by the IT department." Many-to-many relationship between rules and decisions (each rule may apply to multiple decisions; each decision may implement more than one rule). Rule traceability: each rule fires when a relevant business condition threatens to occur — traceable invocation model.
- Verdict: too verbose for agent-managed KB; useful as a semantic vocabulary layer for term disambiguation and connecting rules to policies.

**DMN (Decision Model and Notation)**
- Sources: [Trisotech DMN Standard](https://www.trisotech.com/dmn/) | [SBVR→DMN translation paper (ResearchGate)](https://www.researchgate.net/publication/303626206_From_SBVR_to_BPMN_and_DMN_Models_Proposal_of_Translation_from_Rules_to_Process_and_Decision_Models)
- Classification: Established OMG standard (executable)
- Decision Tables + Decision Requirements Graph (DRG). Executable decision logic "fully defined and tested by subject matter experts, i.e., non-programmers." Bidirectional traceability: rule ↔ decision ↔ process. DMN structure maps directly to code snippets, services. SBVR→DMN translation algorithm exists (produces combined process + decision model usable for validating SBVR rules).
- Verdict: appropriate for rules with multi-outcome branching logic (eligibility tables, pricing tiers, classification decisions). Not practical for behavioral invariants (guard clauses, state assertions) which have binary pass/fail semantics.

**Practical KB Entry Schema (synthesized from research — no published standard covers this combination):**

No published standard covers the full evidence-citation + confidence-score + code-layer requirement. Recommended minimal schema based on research synthesis:

```
rule_id:              stable identifier (RULE-{domain}-{seq})
title:                short business-language name
category:             structural | operational | derivation | constraint
statement:            plain-language rule text
evidence:             [{file, start_line, end_line, snippet}]  ← required, reject if absent
confidence_score:     0.0–1.0  (multi-source: static + LLM votes + test coverage)
invariant_type:       precondition | postcondition | invariant | validation
source_layer:         domain | application  (domain = guard clause in constructor/method, application = FV validator)
test_ids:             [test names that pin this rule]
last_verified_commit: SHA of last passing verification
status:               verified | candidate | rejected | stale
```

SBVR handles vocabulary disambiguation. DMN handles branching logic tables. The schema above is the harness KB format for all other rule types.

---

### THREAD 6: Rule Rot — Synchronization and Traceability

**Traceability Aspects for Business Rules Management (BRCommunity, 2008)**
- Source: [brcommunity.com/articles.php?id=b384](https://www.brcommunity.com/articles.php?id=b384)
- Classification: Established practice
- Ten traceability types identified: Vocabulary, Rule Reuse, Temporal, Business Policy, Processing (runtime hook points), Test Case, Deployment, SOA services, Work List, Compliance.
- Anti-rule-rot mechanisms: Processing Traceability (insert hook points in architecture to capture which rules fired at runtime, storing results for BI/reporting); Temporal Traceability (rule-sets agnostic of effective dates — "reusable Lego blocks" where integration points carry date logic, not rules themselves); Regression test scaffolding with captured expectations (enables quick impact assessment when rules change).
- Key warning: "building traceability into your Architecture is an evolutionary process and needs to be built in over time. Features of vendor products out of the box come only half way."
- Applicability: `last_verified_commit` in the KB schema + a CI hook that re-evaluates rules against changed files = implementation of Processing Traceability + Temporal Traceability combined.

**EvolveWare continuous update model:**
- Source: [evolveware.com/agile-business-rules-extraction/](https://evolveware.com/agile-business-rules-extraction/)
- Classification: Established product capability
- When code changes, automated reports highlight which extracted rules are impacted; unaffected rules remain unchanged. Change-scoped re-extraction, not full re-run. Supports "continuous updates to the code during the BRE process."
- Applicability: on every PR, `git diff` → affected file set → subset re-extraction only. Full re-extraction on every commit is not scalable at enterprise size.

**RTM in CI/CD (2025–2026):**
- Source: [Medium: Traceability Matrix in CI/CD Workflows (2026)](https://medium.com/@sancharini.panda/how-a-traceability-matrix-fits-into-modern-ci-cd-workflows-714c5a6862af)
- Classification: Emerging practice
- "Once the gap widens between matrix and code, the matrix no longer serves as a current representation of the program." Every deployment triggers automated tests traceable back to requirements. Continuous traceability reflects latest requirement coverage in real-time. Automated RTM updates within CI/CD prevent rule rot. Bidirectional: all source code and all tests should be traceable back to requirements.
- Key finding: change impact analysis is critical to understand which parts of the code are impacted by requirements changes, and vice versa.

**Swimm auto-sync model (gold standard reference for this problem):**
- Source: [swimm.io](https://swimm.io/) | [swimm.io/home](https://swimm.io/home)
- Classification: Established product capability; Gartner Cool Vendor 2024
- On every PR, Swimm automatically verifies and updates documentation against changed code. Documentation that refers to moved/deleted code is flagged as broken. Docs contain "live code spanning multiple files and repositories." Documentation that isn't synced to a PR is flagged as potentially stale. CI integration makes documentation the single source of truth.
- Applicability: a CI step that re-checks evidence citations (file:line still exists, content at that line unchanged relative to `last_verified_commit`) on every PR is the Swimm model applied to the harness KB. This is the only sustainable solution to rule rot at code-change velocity.

**Automated trace link generation (known limitation):**
- Source: [ResearchGate: Rule-based Generation of Requirements Traceability Relations](https://www.researchgate.net/publication/223738681_Rule-based_generation_of_requirments_traceability_relations)
- Classification: Established research limitation
- Information retrieval + ML trace link approaches "fail to understand semantics or integrate domain knowledge → imprecise results." Rule-based is more precise but brittle to refactoring.
- Applicability: for DDD code where naming conventions are deliberate (aggregate method names encode business intent), rule-based + Roslyn semantic model is more reliable than pure IR approaches. The naming investment in DDD pays off here.

**Towards Automatic Extraction of Structural Business Rules (WCRE 2012)**
- Source: [ojcchar.github.io/files/2-wcre12.pdf](https://ojcchar.github.io/files/2-wcre12.pdf)
- Classification: Established research
- Governed Rule Base System (GRBS): stores and tracks all knowledge around business rules including concepts, fact types, rules and their relationships. "Provides smart governance and a corporate memory through traceability." Focuses on structural rules derivable from database schemas + code.
- Applicability: the GRBS concept (rule + concept + fact type + relationship, all traceable) is the right ontological structure for the KB. Rule categories from this paper: structural rules (data constraints) vs. operational rules (behavioral constraints) maps to DDD invariants (structural) vs. domain method guards (operational).

---

### Roslyn as the .NET Static Analysis Engine — Cross-Thread Synthesis

For a .NET DDD codebase, **Roslyn replaces all COBOL-era program slicing tools.** It provides the mandatory pre-processing step that every production tool (IBM ADDI+watsonx, Swimm, AWS Transform Reimagine) demonstrates must precede LLM invocation.

- Sources: [Analysing a .NET Codebase with Roslyn (DEV.to)](https://dev.to/mattjhosking/analysing-a-net-codebase-with-roslyn-5cn0) | [Getting Started: C# Semantic Analysis (Roslyn GitHub)](https://github.com/dotnet/roslyn/blob/main/docs/wiki/Getting-Started-C%23-Semantic-Analysis.md) | [Roslyn Analyzer Actions Semantics (GitHub)](https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md)

Roslyn `CSharpSyntaxWalker` + `SemanticModel.GetTypeInfo()` / `ITypeSymbol` traversal enables:
- Identify aggregate roots and value objects by base class inspection via `ITypeSymbol` hierarchy traversal
- Extract guard clauses and `throw` expressions from constructors and factory methods
- Parse FluentValidation rule chains (`RuleFor()`, `Must()`, `WithMessage()`, `GreaterThan()`, etc.) by call pattern recognition
- Build a `{file:line → code segment}` evidence index with fully resolved symbol names (not raw identifiers)
- `Compilation.GetSemanticModel()` resolves overloaded types and cross-assembly references — this eliminates the ambiguity problem Swimm identifies as the primary LLM failure point on cryptic identifiers

The Roslyn semantic model is the equivalent of ADDI's metadata database and Swimm's variable lineage tracer for .NET. No third-party static analysis tool is needed.

**FluentValidation dual-source extraction (DDD-specific):**
- Sources: [Microsoft: Domain Model Layer Validations](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-model-layer-validations) | [Enterprise Craftsmanship: Validation and DDD](https://enterprisecraftsmanship.com/posts/validation-and-ddd/) | [Always-Valid Domain Model](https://enterprisecraftsmanship.com/posts/always-valid-domain-model/)
- FV validators belong to the application layer per DDD canon. However, in CQRS/FastEndpoints stacks they frequently encode business constraints not expressed in domain entities — particularly request-level business rules, cross-field constraints, and policy-level validations.
- Both sources must be mined: guard clauses in domain constructors/factory methods (invariants — `source_layer: domain`) AND FV validators in application layer (request constraints — `source_layer: application`). The KB schema field `source_layer` distinguishes them and enables consumers to understand the semantic difference.

---

### Key Numbers Reference Table

| Metric | Value | Source |
|---|---|---|
| Best LLM accuracy on formal spec extraction from documents | **71.6%** | arXiv:2504.01294 (Li et al. 2025) |
| Two-stage pipeline improvement over end-to-end LLM | **+29.2%** correct specs | arXiv:2504.01294 |
| Average accuracy improvement from two-stage | **+14.0%** | arXiv:2504.01294 |
| Single-pass → multi-stage verification accuracy gain | **19% → 44%** | AlphaCodium via arXiv:2604.05150 |
| Best academic recall: backward slicing on COBOL PDGs | **>85%** | Academia.edu MDR paper |
| Code that is infrastructure noise, not business logic | **70–80%** | Swimm blog / Huang 1996 |
| GPT-4 Turbo package hallucination rate | **4%** | Diffray / ACM 2025 |
| CodeLlama 7B package hallucination rate | **26%** | Diffray / ACM 2025 |
| AI-generated code with security vulnerabilities | **29–45%** | Diffray 2024–2025 |
| Syntax error reduction via grammar-constrained generation | **96%** (Python/Go) | ACM 2025 |

---

### TOP 5 DESIGN IMPLICATIONS

**1. Two-stage grounded pipeline is the architectural requirement, not an optimization.**

Every production-grade tool in this research — IBM ADDI+watsonx, AWS Transform Reimagine, Swimm, SLD-Spec (arXiv:2509.09917), and EvolveWare Intellisys — uses the identical two-layer pattern: deterministic static analysis first (evidence index, symbol resolution, lineage tracing, infrastructure noise removal) → LLM second on pre-processed, grounded context. Single-pass end-to-end LLM extraction tops out at 71.6% accuracy with fabrication as the primary failure mode (arXiv:2504.01294). IBM explicitly states its value proposition as "code-aware segmentation to reduce common pitfalls of generic LLM chunking." AWS Transform states this is "not a task solvable by pointing a general-purpose AI agent at a repository." For this .NET DDD codebase, **Roslyn is the static analysis layer** — it builds the evidence index, resolves all symbols across assemblies, and delivers annotated code slices (not raw files) to the LLM. This is the non-negotiable architectural foundation.

**2. Confidence score must be multi-source and multi-pass, not a single LLM output.**

LogicScan's majority-voting (arXiv:2602.03271), Daikon's test-confirmation model, BMC's per-assertion hallucination scoring, and AlphaCodium's 19%→44% accuracy jump from multi-stage verification all converge on the same principle: confidence is earned through corroboration across independent evidence channels, not asserted by a single model. The proposed four-channel score: (a) static presence — Roslyn finds a guard clause or FV rule at file:line (high weight; rules failing this channel are rejected at intake, not scored); (b) LLM extraction consistency — N≥3 independent LLM passes with majority vote on the rule statement (LogicScan pattern); (c) dynamic test coverage confirmation — a test exercises the rule's code path and passes (Daikon pattern); (d) human review stamp. Only rules passing all four channels reach `status: verified`. Rules passing (a) only are `candidate`. Rules failing (a) are immediately `rejected`.

**3. Evidence citation (file:line:snippet) is the hard rejection gate, not metadata.**

AgentPex's explicit-only extraction policy (arXiv:2603.23806), the BRCommunity traceability framework, the WCRE 2012 GRBS architecture, and the full history of traceability research all converge: a rule without a grounded code location is a hallucination risk, not knowledge. The KB schema must enforce `evidence` as a non-nullable array with at minimum one entry containing `{file, start_line, end_line, snippet}`. Rules where the LLM cannot ground its claim to a specific file:line:snippet are immediately classified `status: rejected` — they are not stored as candidates, reviewed, or escalated. This is the primary structural anti-hallucination mechanism. It forces the LLM to locate before it claims. It also enables all downstream tooling (CI sync checks, test generation, impact analysis) to function correctly.

**4. Rule rot requires a CI hook on every PR, not human discipline.**

Swimm's PR auto-sync (Gartner Cool Vendor 2024), EvolveWare's change-impact automated reports, and the RTM-in-CI/CD research (Medium 2026) all demonstrate the same finding: manual traceability maintenance degrades to uselessness within weeks on active codebases. The BRCommunity 2008 article warned "vendor products come only half way" — the rest must be built into the architecture. The required CI step: (a) on every PR, compute `git diff` → identify affected files → find all KB rules with evidence entries in those files → re-trigger the extraction pipeline on that subset only (not a full re-run, per EvolveWare's change-scoped model); (b) detect evidence citation breakage — file moved, line deleted, content at that line changed relative to `last_verified_commit`; (c) set `status: stale` on any rule with broken citations; (d) block merge if any `status: verified` rule becomes stale. Full re-extraction is triggered only on domain model refactors (base class changes, aggregate restructuring). This is not conservative engineering — it is the only demonstrated solution to rule rot at code-change velocity.

**5. Extracted rules inherit code bugs — a passing test is the gate to verified status, not LLM confidence.**

The post-condition formalization benchmark (arXiv:2603.17193) explicitly documents: "specifications extracted from code may inherit bugs already present in the code." An extracted invariant stating "balance must be ≥ 0 after withdrawal" is only correct if the withdrawal implementation is correct. The Daikon documentation warns that detected invariants "might not be valid since they only hold on the test runs." The harness must not promote a rule from `candidate` to `verified` without a test that (a) exercises the rule's specific evidence code path and (b) passes. The passing test is the independently observable evidence that the rule is both structurally present and behaviorally correct. This creates the required verification loop: extract rule (Roslyn static + LLM multi-pass) → auto-generate a minimal test targeting the evidence location → run test → green = verified; red = flag implementation or rule as suspect, set `status: candidate`, surface to human reviewer for resolution. This loop also surfaces latent bugs in the domain model, which is a valuable secondary output of the harness.

---

### Complete Sources Index

**Academic papers:**
- [IEEE: Business Rule Extraction from Legacy Code (Sneed 1996)](https://ieeexplore.ieee.org/document/544158/)
- [ResearchGate: Business Rule Extraction from Legacy Code (Sneed 1996)](https://www.researchgate.net/publication/221029319_Business_Rule_Extraction_from_Legacy_Code)
- [ResearchGate: Extracting Business Rules from Source Code (Huang et al. 1996)](https://www.researchgate.net/publication/3629947_Extracting_business_rules_from_source_code)
- [Academia.edu: Getting Objects, Methods and Interactions by Extracting BRs from Legacy Systems (2016)](https://www.academia.edu/29685854/Getting_Objects_Methods_and_Interactions_by_Extracting_Business_Rules_from_Legacy_Systems)
- [IJCSIT 2015: Tool for Extraction of Functional Requirements from Legacy Code](https://www.ijcsit.com/docs/Volume%206/vol6issue03/ijcsit2015060336.pdf)
- [PLDI 2007: Static Specification Inference Using Predicate Mining](https://www.cs.purdue.edu/homes/suresh/papers/pldi07.pdf)
- [RWTH Aachen: Scenario-based and Value-based Specification Mining — Better Together](https://www.se-rwth.de/publications/Scenario-based-and-value-based-specification-mining-better-together.pdf)
- [ScienceDirect: Interactive Semi-automated Specification Mining for Debugging](https://www.sciencedirect.com/science/article/abs/pii/S0950584919301004)
- [Daikon: SCP 2007 paper PDF (Ernst et al.)](https://homes.cs.washington.edu/~mernst/pubs/daikon-tool-scp2007.pdf)
- [Daikon: TSE 2001 paper PDF](https://homes.cs.washington.edu/~mernst/pubs/invariants-tse2001.pdf)
- [ACM DL: Daikon Science of Computer Programming 2007](https://dl.acm.org/doi/10.1016/j.scico.2007.01.015)
- [arXiv:2509.09917 — SLD-Spec: LLM + Program Slicing for Specification Generation (2025)](https://arxiv.org/abs/2509.09917)
- [arXiv:2602.03271 — LogicScan: LLM-driven Business Logic Vulnerability Detection (2025)](https://arxiv.org/pdf/2602.03271)
- [arXiv:2504.01294 — Extracting Formal Specifications from Documents Using LLMs (Li et al. 2025)](https://arxiv.org/abs/2504.01294)
- [arXiv:2603.17193 — Talk is Cheap, Logic is Hard: Benchmarking LLMs on Post-Condition Formalization (2025)](https://arxiv.org/abs/2603.17193)
- [arXiv:2603.23806 — AgentPex: Willful Disobedience — spec extraction from agentic traces (2025)](https://arxiv.org/pdf/2603.23806)
- [arXiv:2604.05150 — Compiled AI: Deterministic vs. LLM hybrid extraction (2025)](https://arxiv.org/html/2604.05150)
- [arXiv:2601.04556 — 4D-ARE: Bridging Attribution Gap in LLM Agent RE (2025)](https://arxiv.org/pdf/2601.04556)
- [arXiv:2408.17316 — Bridging Domain Knowledge and Process Discovery Using LLMs](https://arxiv.org/pdf/2408.17316)
- [WCRE 2012: Towards Automatic Extraction of Structural Business Rules](https://ojcchar.github.io/files/2-wcre12.pdf)
- [ResearchGate: Rule-based Generation of Requirements Traceability Relations](https://www.researchgate.net/publication/223738681_Rule-based_generation_of_requirments_traceability_relations)
- [ResearchGate: From SBVR to BPMN and DMN Models — Translation Proposal](https://www.researchgate.net/publication/303626206_From_SBVR_to_BPMN_and_DMN_Models_Proposal_of_Translation_from_Rules_to_Process_and_Decision_Models)
- [Frontiers Computer Science: Research Directions for LLM in Software RE (2025)](https://www.frontiersin.org/journals/computer-science/articles/10.3389/fcomp.2025.1519437/full)
- [ACM 2025: LLM Hallucinations in Practical Code Generation](https://dl.acm.org/doi/pdf/10.1145/3728894)

**Industry tools and product documentation:**
- [IBM watsonx Code Assistant for Z ADDI Overview Docs](https://www.ibm.com/docs/en/watsonx/watsonx-code-assistant-4z/2.x?topic=addi-overview)
- [IBM watsonx Business Rules Discovery (Mediacenter)](https://mediacenter.ibm.com/media/IBM+watsonx+Code+Assistant+for+Z+Understand:+Business+rules+discovery/1_5pr695pa/189147203)
- [IBM Agentic AI mainframe modernization announcement (v2.8)](https://www.ibm.com/new/announcements/agentic-ai-for-smarter-mainframe-modernization-with-ibm-watsonx-code-assistant-for-z)
- [EvolveWare Agile Business Rules Extraction](https://evolveware.com/agile-business-rules-extraction/)
- [EvolveWare BRE Blog: Rules vs. Logic](https://blog.evolveware.com/business-rules-extraction-why-understanding-rules-vs-logic-is-important/)
- [EvolveWare Financial Institutions Case Study](https://evolveware.com/business-rules-extraction-in-action-what-it-is-and-how-financial-institutions-can-do-it-right/)
- [Micro Focus COBOL Analyzer Business Rule Manager docs v5](https://www.microfocus.com/documentation/cobol-analyzer/ca503/CA/GUID-3521F158-A453-41C2-8851-870946F0821D.html)
- [Micro Focus: Extracting Business Rules docs](https://www.microfocus.com/documentation/cobol-analyzer/ca60/CA/GUID-64A974DE-F7E2-4D71-AEAC-45397C1B1996.html)
- [AWS Transform: Reimagine capabilities announcement (May 2025)](https://aws.amazon.com/blogs/aws/aws-transform-for-mainframe-introduces-reimagine-capabilities-and-automated-testing-capabilities-and-automated-testing-functionality/)
- [AWS Blu Insights: Refactor COBOL to Java blog](https://aws.amazon.com/blogs/mt/aws-mainframe-modernization-refactor-legacy-code-base-to-java-using-aws-blu-insights/)
- [AWS Transform: Comprehensive refactor approach](https://aws.amazon.com/blogs/migration-and-modernization/accelerate-mainframe-modernization-with-aws-transform-a-comprehensive-refactor-approach/)
- [Swimm Enterprise page](https://swimm.io/enterprise)
- [Swimm: Blackbox to Blueprint blog](https://swimm.io/blog/blackbox-to-blueprint-extracting-business-logic-from-cobol-applications)
- [Swimm: 5 COBOL BRE Tools to Know in 2026](https://swimm.io/learn/cobol/5-cobol-business-rules-extraction-tools-to-know-in-2026)
- [CodeLogic homepage](https://codelogic.com/)
- [Trisotech: DMN Standard](https://www.trisotech.com/dmn/)
- [Daikon dynamic invariant detector (official site)](https://plse.cs.washington.edu/daikon/)

**Standards and traceability:**
- [BRCommunity: SBVR ↔ DMN Relationship](https://www.brcommunity.com/articles.php?id=b597)
- [BRCommunity: Traceability Aspects for Business Rules Management (2008)](https://www.brcommunity.com/articles.php?id=b384)
- [Medium: Traceability Matrix in CI/CD Workflows (2026)](https://medium.com/@sancharini.panda/how-a-traceability-matrix-fits-into-modern-ci-cd-workflows-714c5a6862af)

**Hallucination measurement:**
- [Diffray: LLM Hallucinations in AI Code Review](https://diffray.ai/blog/llm-hallucinations-code-review/)

**.NET / Roslyn references:**
- [DEV.to: Analysing a .NET Codebase with Roslyn](https://dev.to/mattjhosking/analysing-a-net-codebase-with-roslyn-5cn0)
- [Roslyn GitHub: Getting Started C# Semantic Analysis](https://github.com/dotnet/roslyn/blob/main/docs/wiki/Getting-Started-C%23-Semantic-Analysis.md)
- [Roslyn GitHub: Analyzer Actions Semantics](https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md)
- [Microsoft: Domain Model Layer Validations in .NET](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-model-layer-validations)
- [Enterprise Craftsmanship: Validation and DDD](https://enterprisecraftsmanship.com/posts/validation-and-ddd/)
- [Enterprise Craftsmanship: Always-Valid Domain Model](https://enterprisecraftsmanship.com/posts/always-valid-domain-model/)

================================================================================

The research is complete. Here is a summary of what was delivered to the architect:

---

**Research delivered:** Six-thread deep investigation into business rule extraction from source code, covering academic foundations through LLM-era approaches.

**Key findings:**

**Thread 1 — Academic foundations (established practice):** The seminal work (Sneed 1996, Huang et al. 1996, MDR/backward slicing 2016) establishes that code is more trustworthy than documentation, 70-80% of code is infrastructure noise, backward program slicing achieves 85% recall (best academic ceiling), and no single method extracts all rules.

**Thread 2 — Industry tooling (established + emerging):** IBM ADDI+watsonx, AWS Transform Reimagine, EvolveWare Intellisys, Swimm, and Micro Focus BRM all converge on the same two-layer architecture: deterministic static analysis first, LLM second on grounded context. EvolveWare explicitly supports C#. CodeLogic is a dependency upgrade tool, not a BRE tool.

**Thread 3 — Specification mining (established + emerging):** Daikon provides dynamic invariant detection (high false positive rate — mandatory confidence scoring). SLD-Spec (arXiv 2025) confirms program slicing before LLM is correct. LogicScan (arXiv 2025) provides the majority-voting confidence score pattern.

**Thread 4 — LLM accuracy numbers:** Best spec extraction accuracy: 71.6% (arXiv:2504.01294). Two-stage pipeline improves that by 29.2%. Multi-stage verification improves GPT-4 from 19% to 44% (AlphaCodium). GPT-4 Turbo hallucination rate: 4%; CodeLlama 7B: 26%. Critical warning: extracted specs inherit bugs in the implementation.

**Thread 5 — Documentation formats:** SBVR for vocabulary disambiguation, DMN for branching logic tables, and a custom KB schema (rule_id, statement, evidence array, confidence_score, source_layer, test_ids, last_verified_commit, status) for all other rule types. Evidence is a non-nullable field.

**Thread 6 — Rule rot:** Swimm auto-sync on PR is the gold standard. EvolveWare's change-scoped re-extraction (git diff → affected files → subset only) is the scalable pattern. CI hook on every PR is mandatory — human discipline is not sufficient.

**The five design implications** are fully elaborated in the research output above, distilled to: (1) two-stage grounded pipeline is mandatory, (2) confidence requires four independent channels, (3) file:line evidence is a hard rejection gate, (4) rule rot requires a CI hook not human discipline, (5) candidate-to-verified promotion is gated by a passing test not LLM confidence.