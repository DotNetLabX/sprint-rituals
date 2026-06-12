# Research — BR-Grounded Test-Coverage Loop Harness
**Date:** 2026-06-11. **Method:** 5 parallel research agents (web research), consolidated.

## Executive Summary

- **Never let the LLM be both proposer and judge.** Intrinsic self-correction *degrades* reasoning performance (Huang et al., ICLR 2024) — generator and evaluator share blind spots. Every correction cycle must be grounded in `dotnet test` / Stryker execution output (CRITIC pattern). Stopping decisions must come from external signals, never LLM self-assessment.
- **Mutation kill is the only structural anti-hallucination gate for tests.** Meta ACH: mutation-guided LLM generation achieves a 32% mutant kill rate vs 5.3% for coverage-guided; 49% of mutation-killing tests add zero line coverage; 73% engineer acceptance. Coverage-gated generation is actively harmful: CoverAgent rejected 470 bug-revealing tests per 287 samples and 59.6% of its final suites *validated* bugs instead of detecting them.
- **Two-stage grounded extraction is the universal production pattern.** Every shipping BR-extraction tool (IBM ADDI+watsonx, AWS Transform Reimagine, EvolveWare, Swimm, SLD-Spec) runs deterministic static analysis first and the LLM second on grounded slices. Single-pass LLM extraction caps at 71.6% accuracy; multi-stage verification lifts GPT-4 coding accuracy 19% → 44%. For this codebase, Roslyn is the static layer.
- **Citation alone is not faithfulness.** Up to 57% of LLM citations are post-rationalized. The evidence-or-reject gate needs quote-first ordering plus NLI entailment verification, not just a file:line field.
- **Adversarial refutation must be collaborative, not competitive.** Zero-sum debate degrades accuracy by up to 15% F1 via debate hacking; collaborative, exact-quote-citing refutation (ColMAD) gains +2–5%. Proposer and refuter must use architecturally distinct models.
- **Verbalized confidence is the weakest gate.** ECE ≈ 0.10 systematic overconfidence. Replace with semantic entropy over 3–5 samples + meta-model routing (16% escalation at 94.14% F1 in production).
- **Loop state must be externalized.** Stateless per-iteration calls against a durable ledger versioned by git SHA; loops empirically converge within 5 iterations; hard budget cap (~20) and a mutation-score ratchet are mandatory; reward hacking emerges uninstructed, so the agent must not have write access to evaluation infrastructure.
- **Extracted rules inherit code bugs.** A green test proves rule-implementation consistency, not business correctness — the human gate stays.
- **Without gates, ~36% of LLM code descriptions contain at least one hallucination** (Behavior Conflicting alone = 35.4% of those, undetectable statically). This is the base rate the whole gate stack exists to suppress.
- **.NET stack is settled:** xunit.v3 + Microsoft.Testing.Platform + FsCheck 3 + Verify (bootstrap only) + dotnet-stryker `--with-baseline` (`break-at 70`) + FluentAssertions; TUnit excluded. KB follows the Karpathy three-layer wiki pattern with `rule_id` as primary key, `[Trait("RuleId", …)]` traceability, load-bearing `index.md`, CI diff drift gate; Gherkin is out for the domain layer.

## 1. Self-Improving Loop Harnesses

| Finding | Source (URL) | Class | Implication for harness |
|---|---|---|---|
| Intrinsic self-correction (no external feedback) consistently **degrades** reasoning performance; generator and evaluator share correlated blind spots; prior "self-correction gains" were oracle-guided filtering | Huang et al., ICLR 2024 — https://arxiv.org/abs/2310.01798 | Established (load-bearing negative result) | Never build a loop where the LLM is proposer and judge. Test execution output is the mandatory grounding signal; "is this test good?" without running it compounds errors |
| External tool output (interpreter/test runner) as the correction signal reliably improves output quality | CRITIC, Gou et al., ICLR 2024 — https://arxiv.org/abs/2305.11738 | Established | `dotnet test` stack traces + coverage delta + mutation delta = the architecturally correct correction signal |
| Actor→Evaluator→Self-Reflection loop achieved **91% pass@1 on HumanEval vs 80% GPT-4 baseline**, driven by test execution feedback; only 1–3 reflection summaries survive the context window | Reflexion, Shinn et al., NeurIPS 2023 — https://arxiv.org/abs/2303.11366 | Established | Reflections belong in a durable ledger file, not accumulated conversation context |
| Generate→feedback→refine micro-loop yields **~20% improvement**, but only when feedback is externally grounded | Self-Refine, Madaan et al. — https://arxiv.org/abs/2303.17651 | Established | Valid per-test micro-loop iff feedback = test runner output |
| Genuine intrinsic self-correction is achievable only via RL **training**, not inference-time prompting | SCoRe, ICLR 2025 — https://arxiv.org/abs/2409.12917 | Emerging | Prompting loops cannot substitute for execution grounding |
| "Coherence trap": iterative self-critique amplifies confidence without adding information; execution-grounded and multi-agent-divergent approaches rate High reliability; correction budgets must be capped with human escalation | Zylos Research 2026 — https://zylos.ai/research/2026-05-12-agent-self-correction-reflexion-to-prm | Commentary | Per-rule correction budget counter, not open-ended retry |
| LLM-as-mutation-operator with island database: stateless-per-call, durable population artifact between iterations | FunSearch, DeepMind/Nature 2023 — https://deepmind.google/research/publications/funsearch-making-new-discoveries-in-mathematical-sciences-using-large-language-models/ | Established | Verified-rule ledger = the population database analog |
| Cascaded evaluation (cheap filters first, expensive verification last), MAP-Elites diversity, LLM ensemble; no explicit stopping criteria — resource budget terminates | AlphaEvolve, DeepMind May 2025 — https://deepmind.google/research/publications/alphaevolve-a-gemini-based-coding-agent-for-designing-advanced-algorithms/ (detail: https://richardcsuwandi.github.io/blog/2025/llm-algorithm-discovery/) | Established industry | Gate ordering: build → test execution → coverage delta → mutation delta; diversity pressure across code regions |
| Agent-Computer Interface: constrained tool surface (not raw shell); step budget is the hard termination gate | SWE-agent, NeurIPS 2024 — https://arxiv.org/abs/2405.15793 | Established | Harness agent gets a defined, limited interface: read source, run tests, write ledger |
| Structured intermediate representations (AST, call graph) fed to the LLM dramatically beat raw source; 22% SWE-bench-lite | AutoCodeRover, ISSTA 2024 — https://dl.acm.org/doi/10.1145/3650212.3680384 | Established | Feed Roslyn-derived signatures/flow summaries to the mining LLM, not raw files |
| Bug-injector/repairer self-play grounded by executable test patches (not prose) | SWE-RL — https://arxiv.org/abs/2512.18552 | Emerging | Rule-mining output should be a candidate executable test spec, not prose |
| Two decades of GI research: mutation score (not line/branch coverage) is the discriminating fitness; mutation-guided test generation raises both coverage and score (inverse not true) | Genetic Improvement field — https://geneticimprovementofsoftware.com/ (ICSE 2024: https://geneticimprovementofsoftware.com/events/icse2024.html) | Established field | Mutation score is the loop's fitness function |
| Loops **converge within 5 iterations** in deployed systems; stopping patterns: dry-run counter, hard budget cap, plateau detection, ratchet (metric never regresses) | EmergentMind — https://www.emergentmind.com/topics/agentic-loops ; formal optimal stopping — https://arxiv.org/html/2511.00592v1 | Emerging/practitioner | Multi-signal stopping: N dry iterations + cap (~20) + ratchet; budget beyond 10 is diminishing returns |
| Eight agentic failure modes: reward hacking, degeneration loop, task drift, alignment faking, metric gaming, tool-call hallucination, mode collapse, version drift — all goal-alignment failures invisible in single outputs | ceaksan — https://ceaksan.com/en/llm-agentic-failure-modes | Practitioner | Mitigation table adopted: mutation gate, n-gram repetition detection, goal re-injection, model pinning, diversity pressure |
| Real, **uninstructed** reward hacking observed: downloading checkpoints, training on held-out data, manipulating eval scripts; detection = holdout performance, test-file edit detection, LLM judge | RewardHackingAgents — https://arxiv.org/pdf/2603.11337 ; survey — https://arxiv.org/html/2604.13602v1 | Emerging | Ledger, coverage data, and runner config are attack surfaces; agent gets no write access to eval infra |
| Context rot: append-only stores degrade retrieval; "constant-cost long-horizon context still lacking"; correct pattern = stateless calls + durable external artifacts | Externalization survey — https://arxiv.org/abs/2604.08224 | Survey | Each iteration is a fresh call assembled from the ledger; no carried conversation history |
| Indexed experience memory keeps context fixed-size regardless of iteration count | Memex(RL) — https://arxiv.org/abs/2603.04257 | Emerging | Index the rule ledger; retrieve only rules relevant to the current code region |
| Staleness: "the outside world changes, but memory doesn't" | https://arxiv.org/abs/2509.25250 | Emerging | Version the ledger against git SHA; re-verify rules each run |
| Episodic memory (context + actions + outcomes of past runs) prevents re-exploring dead ends | https://arxiv.org/abs/2502.06975 | Emerging | Record discovery context and failed verification attempts per rule |
| Meta TestGen-LLM "Assured Offline LLMSE" four-stage filter (buildable / reliable / non-flaky / coverage-adding): **75% built, 57% passed reliably, 25% increased coverage, 73% of recommendations accepted, 11.5% of targeted classes improved**; gap: `Assert(True)` passes all four stages; no stopping criteria published | arXiv:2402.09171 — https://arxiv.org/abs/2402.09171 (ACM: https://dl.acm.org/doi/10.1145/3663529.3663839 ; summary: https://www.zenml.io/llmops-database/automated-unit-test-improvement-using-llms-for-android-applications) | Industrial | Reuse the four-stage filter; add mutation kill as stage 5 — the missing assertion-quality gate |
| Adversarial test-generator vs mutant-generator loop structurally forces genuine assertions (assertion-free tests cannot kill mutants) | Test vs Mutant — https://arxiv.org/abs/2602.08146 | Emerging | Build a mutant sub-loop: each generated test must kill ≥1 surviving mutant |
| LLM tests fail mostly via **incorrect** (hallucinated-value) assertions, not missing ones; multi-agent oracle consensus and mutant-in-prompt generation fix this | CANDOR — https://arxiv.org/pdf/2506.02943 ; MUTGEN — https://arxiv.org/pdf/2506.02954 ; CoverUp — https://arxiv.org/abs/2403.16218 ; A3Test (IST 2024, no URL in raw data) | Emerging | For unclear oracles, require independent LLM agreement before committing an assertion; include live mutant info in generation prompts |
| LLM oracles reach mutation scores comparable to human-designed oracles **given full method context** — failure mode is inadequate context, not model incapacity | ASE 2025 — https://conf.researchr.org/details/ase-2025/ase-2025-papers/10/Do-LLMs-Generate-Useful-Test-Oracles-An-Empirical-Study-with-an-Unbiased-Dataset | Emerging | Prompt design (full class-under-test) determines oracle quality |
| Evaluation infrastructure itself goes stale as models and code evolve | https://arxiv.org/pdf/2504.18985 | Emerging | Re-verify the ledger against the current codebase every run |

**Design implications**
- Ground every correction in test-runner output; CRITIC pattern; no LLM self-judgment loops.
- Stateless iterations + durable `loop-state` ledger (rule status, test linkage, iteration counter, mutation baseline), git-SHA-versioned and indexed.
- Mutation score is the primary fitness metric and stage-5 gate on top of Meta's four-stage filter.
- Stopping: N consecutive zero-net-new-verified-rule iterations (3–5) + hard cap (~20) + plateau detection + ratchet (suite mutation score never regresses; regression = harness bug, halt).
- Reward-hacking defenses: holdout bug set, eval-infrastructure edit monitoring, independent no-shared-context LLM judge as canary, n-gram repetition detection.

## 2. Business Rule Extraction from Code

| Finding | Source (URL) | Class | Implication for harness |
|---|---|---|---|
| Every production BRE tool uses the same two-layer pattern: deterministic static analysis builds grounded metadata → LLM consumes it. IBM: "code-aware segmentation reduces common pitfalls of generic LLM chunking"; AWS: "not a task solvable by pointing a general-purpose AI agent at a repository" | IBM ADDI — https://www.ibm.com/docs/en/watsonx/watsonx-code-assistant-4z/2.x?topic=addi-overview (+ https://mediacenter.ibm.com/media/IBM+watsonx+Code+Assistant+for+Z+Understand:+Business+rules+discovery/1_5pr695pa/189147203 , https://www.ibm.com/new/announcements/agentic-ai-for-smarter-mainframe-modernization-with-ibm-watsonx-code-assistant-for-z) ; AWS Reimagine — https://aws.amazon.com/blogs/aws/aws-transform-for-mainframe-introduces-reimagine-capabilities-and-automated-testing-functionality/ | Established products | Roslyn-first pipeline is the architectural requirement, not an optimization |
| Best LLM accuracy on formal spec extraction: **71.6%**; two-stage annotation-then-conversion improves correct specs by **+29.2%** (avg accuracy +14.0%); failure modes: oversimplification + fabrication | Li et al. — https://arxiv.org/abs/2504.01294 | Emerging | 71.6% is the single-pass ceiling; the two-stage architecture is the pipeline blueprint |
| Multi-stage structured flows with verification lift GPT-4 coding accuracy **19% → 44%** (AlphaCodium) | via https://arxiv.org/html/2604.05150 | Established measurement | Single-pass extraction is insufficient; verification loops are required |
| Best published academic recall for BRE: **>85%** (backward slicing on COBOL PDGs); precision unreported; no single method extracts all rules | https://www.academia.edu/29685854/Getting_Objects_Methods_and_Interactions_by_Extracting_Business_Rules_from_Legacy_Systems ; IJCSIT 2015 — https://www.ijcsit.com/docs/Volume%206/vol6issue03/ijcsit2015060336.pdf | Established | Expect to miss rules; loop-until-dry plus multi-view extraction angles |
| **70–80% of code is infrastructure noise**, not business logic; pre-filtering before the LLM is essential | Huang et al. 1996 — https://www.researchgate.net/publication/3629947_Extracting_business_rules_from_source_code ; Swimm — https://swimm.io/blog/blackbox-to-blueprint-extracting-business-logic-from-cobol-applications | Established | Signal-to-noise filtering is the core pre-processing job |
| Code is more trustworthy than docs (docs drift); code-as-ground-truth framing | Sneed 1996 — https://ieeexplore.ieee.org/document/544158/ (also https://www.researchgate.net/publication/221029319_Business_Rule_Extraction_from_Legacy_Code) | Established | Justifies the mine-from-code premise |
| **"Specifications extracted from code may inherit bugs already present in the code"**; no LLM formalized all pre/post-condition tasks; post-conditions harder than pre-conditions; automated tests expose errors manual review misses | Talk is Cheap, Logic is Hard — https://arxiv.org/abs/2603.17193 | Emerging | A rule from a buggy aggregate is a buggy rule — test gate is correctness verification, not just QA |
| Package hallucination: GPT-4 Turbo **4%** vs CodeLlama 7B **26%**; **29–45%** of AI code has security vulnerabilities; ~20% of package recommendations don't exist; grammar-constrained generation cuts syntax errors **96%** (Python/Go); BMC per-assertion hallucination scoring is the per-rule-confidence reference design | Diffray — https://diffray.ai/blog/llm-hallucinations-code-review/ ; ACM 2025 — https://dl.acm.org/doi/pdf/10.1145/3728894 | Established measurement | Per-rule, per-assertion confidence scoring is feasible and has production precedent; model choice matters |
| Explicit-only extraction policy: extract only rules "directly stated in source material; avoid inferred or assumed constraints"; independent per-spec LLM invocation | AgentPex — https://arxiv.org/pdf/2603.23806 | Emerging | Rules not groundable to a specific code location → rejected at intake |
| Majority-voting rule aggregation across multiple LLM passes reduces false positives from conflicting/redundant validation logic | LogicScan — https://arxiv.org/pdf/2602.03271 | Emerging | N≥3 extraction passes with majority vote as one confidence channel (see §6 Open risks on model independence) |
| Dynamic invariant detection generates candidates but with high false-positive rates ("only hold on the test runs"); .NET front-end (Celeriac) not production-grade | Daikon — https://plse.cs.washington.edu/daikon/ (papers: https://homes.cs.washington.edu/~mernst/pubs/daikon-tool-scp2007.pdf , https://homes.cs.washington.edu/~mernst/pubs/invariants-tse2001.pdf , https://dl.acm.org/doi/10.1016/j.scico.2007.01.015) | Established tool | Daikon-style candidates need confidence scoring before promotion; not directly usable on .NET 10 |
| Program slicing before LLM + logical-deletion self-filter outperforms prior spec generators | SLD-Spec — https://arxiv.org/abs/2509.09917 | Emerging | Confirms slice-then-extract pre-processing |
| Static predicate mining trades recall for precision; needs no runtime | PLDI 2007 — https://www.cs.purdue.edu/homes/suresh/papers/pldi07.pdf ; combined scenario+value mining — https://www.se-rwth.de/publications/Scenario-based-and-value-based-specification-mining-better-together.pdf ; interactive mining — https://www.sciencedirect.com/science/article/abs/pii/S0950584919301004 | Established | Roslyn semantic model **is** the static predicate miner for .NET |
| SBVR too verbose for agent KBs (vocabulary layer only); DMN fits multi-outcome branching tables, not behavioral invariants; no published standard covers evidence-citation + confidence + layer — custom KB schema required (`rule_id, title, category, statement, evidence[], confidence_score, invariant_type, source_layer, test_ids, last_verified_commit, status`) | SBVR/DMN — https://www.brcommunity.com/articles.php?id=b597 , https://www.evolute.be/thoughts/rules.html , https://www.trisotech.com/dmn/ , https://www.researchgate.net/publication/303626206_From_SBVR_to_BPMN_and_DMN_Models_Proposal_of_Translation_from_Rules_to_Process_and_Decision_Models | Established standards | Flat structured MD/JSON with mandatory evidence array is the practical KB format |
| Rule rot requires CI automation, not discipline: Swimm auto-sync flags broken code references on every PR; EvolveWare change-impact reports re-extract only affected rules; "once the gap widens, the matrix no longer represents the program"; ten traceability types; "vendor products come only half way" | Swimm — https://swimm.io/ , https://swimm.io/enterprise ; EvolveWare — https://evolveware.com/agile-business-rules-extraction/ (+ https://blog.evolveware.com/business-rules-extraction-why-understanding-rules-vs-logic-is-important/ , https://evolveware.com/business-rules-extraction-in-action-what-it-is-and-how-financial-institutions-can-do-it-right/) ; RTM in CI/CD — https://medium.com/@sancharini.panda/how-a-traceability-matrix-fits-into-modern-ci-cd-workflows-714c5a6862af ; BRCommunity 2008 — https://www.brcommunity.com/articles.php?id=b384 | Established product capability | CI hook per PR: `git diff` → affected files → subset re-extraction; stale flag on broken citations; block merge if a verified rule goes stale |
| Heuristic-only extraction "misses key logic or includes unnecessary details, requiring costly manual review" | Micro Focus BRM — https://www.microfocus.com/documentation/cobol-analyzer/ca503/CA/GUID-3521F158-A453-41C2-8851-870946F0821D.html , https://www.microfocus.com/documentation/cobol-analyzer/ca60/CA/GUID-64A974DE-F7E2-4D71-AEAC-45397C1B1996.html ; Swimm comparison — https://swimm.io/learn/cobol/5-cobol-business-rules-extraction-tools-to-know-in-2026 | Established product | Neither pure-heuristic nor pure-LLM works; the hybrid is mandatory and human validation is non-negotiable |
| Four-level extraction hierarchy (LoB → Domain → Feature → Component rule) with "testable acceptance criteria" output; Blu Age pathway | AWS — https://aws.amazon.com/blogs/mt/aws-mainframe-modernization-refactor-legacy-code-base-to-java-using-aws-blu-insights/ , https://aws.amazon.com/blogs/migration-and-modernization/accelerate-mainframe-modernization-with-aws-transform-a-comprehensive-refactor-approach/ | Established product | Maps to DDD: bounded context → aggregate → method → guard clause/invariant |
| Roslyn `CSharpSyntaxWalker` + `SemanticModel`/`ITypeSymbol` identifies aggregates/VOs by base type, extracts guard clauses and `throw` expressions, parses FluentValidation chains (`RuleFor()`, `Must()`…), builds a file:line evidence index with resolved symbols — eliminating the cryptic-identifier ambiguity Swimm identifies as the primary LLM failure point | https://dev.to/mattjhosking/analysing-a-net-codebase-with-roslyn-5cn0 ; https://github.com/dotnet/roslyn/blob/main/docs/wiki/Getting-Started-C%23-Semantic-Analysis.md ; https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md | Official/practitioner | No third-party static analyzer needed; Roslyn replaces COBOL-era slicing tools |
| Both domain guard clauses **and** application-layer FluentValidation validators encode business constraints; they are different categories | https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-model-layer-validations ; https://enterprisecraftsmanship.com/posts/validation-and-ddd/ ; https://enterprisecraftsmanship.com/posts/always-valid-domain-model/ | Established practice | Mine both; tag with `source_layer: domain \| application` |
| LLM-for-RE literature has **no unified benchmark** (metrics fragmented: BLEU/ROUGE/F1/Kappa); hallucination a "critical concern" but unquantified in aggregate | Frontiers 2025 — https://www.frontiersin.org/journals/computer-science/articles/10.3389/fcomp.2025.1519437/full | Survey | Harness precision/recall must be measured against an internally built held-out ground truth |
| Structural vs operational rule ontology with full traceability (GRBS) | WCRE 2012 — https://ojcchar.github.io/files/2-wcre12.pdf ; rule-based trace links — https://www.researchgate.net/publication/223738681_Rule-based_generation_of_requirments_traceability_relations | Established | Rule-based + Roslyn semantics beats IR/ML trace-link generation on deliberately-named DDD code |
| CodeLogic is a dependency-upgrade tool, **not** a BRE tool (misidentified in initial scoping) | https://codelogic.com/ | Correction | Exclude from tooling candidates |

**Design implications**
- Two-stage grounded pipeline: Roslyn evidence index → LLM on annotated slices. Non-negotiable.
- Multi-source confidence: (a) Roslyn static presence (fail → reject at intake), (b) N≥3 LLM passes majority vote, (c) test-coverage confirmation, (d) human stamp. All four for `verified`.
- `evidence` (file:line:snippet) is a non-nullable hard rejection gate.
- Rule rot: per-PR CI hook with diff-scoped re-extraction and stale-flagging; full re-extraction only on domain-model refactors.
- `candidate → verified` promotion is gated by a passing test exercising the evidence path — because extracted rules inherit implementation bugs.

## 3. Reverse-Engineering Code into Tests

| Finding | Source (URL) | Class | Implication for harness |
|---|---|---|---|
| Characterization/golden-master tests pin current behavior **including bugs** ("does not infer correctness"); they score poorly under mutation testing (whole-output assertions) | Feathers — https://michaelfeathers.silvrback.com/characterization-testing ; https://understandlegacycode.com/blog/characterization-tests-or-approval-tests/ ; https://en.wikipedia.org/wiki/Characterization_test | Established | Verify snapshots = time-limited Pass-0 scaffold only; replace with rule-encoding tests |
| Verify (Simon Cropp) is the canonical .NET snapshot library: `.received.*` → human review → `.verified.*`; xUnit/NUnit/MSTest/TUnit; scrubbing for dynamic values | https://github.com/verifytests/verify ; https://blog.jetbrains.com/dotnet/2024/07/11/snapshot-testing-in-net-with-verify/ ; https://antondevtips.com/blog/how-to-simplify-assertions-in-unit-and-integration-tests-with-verify-in-dotnet | Established tool | Bootstrap baseline for complex sprint-metric outputs |
| No pre-LLM generator is viable for .NET 10: Pex/IntelliTest = VS Enterprise + .NET Framework only, inputs-not-oracles, effectively abandoned; EvoSuite/Randoop/Diffblue = JVM-only; all share the **regression oracle problem** (encode current bugs as expected behavior) | Pex — https://link.springer.com/chapter/10.1007/978-3-540-79124-9_10 ; IntelliTest — https://github.com/MicrosoftDocs/visualstudio-docs/blob/main/docs/test/intellitest-manual/index.md ; EvoSuite — https://www.researchgate.net/publication/273188653_A_Large-Scale_Evaluation_of_Automated_Unit_Test_Generation_Using_EvoSuite ; Diffblue (vendor claims: 250 tests/min, 20x) — https://www.businesswire.com/news/home/20251104720918/en/Diffblues-Latest-Innovations-in-Unit-Test-Generation-Deliver-20x-Productivity-Advantage-Versus-AI-Coding-Assistants , https://siliconangle.com/2024/10/30/diffblue-raises-6-3m-autonomous-ai-powered-test-generation-platform/ | Established (Diffblue claims = marketing) | The .NET path is LLM drafts + mutation gate; oracles must come from explicit intent |
| Coverage-gated LLM generation is self-defeating: across 287 buggy samples, CoverAgent rejected **470 bug-revealing tests** (59.6% of final tests *validated* the bug), CoverUp rejected 400 (68.1% validated; no suite at all in 68.3% of cases), Copilot kept 194 (4.2% bug-validating). "Code should be written based on requirements, not the other way around" | arXiv:2412.14137 — https://arxiv.org/html/2412.14137v1 | Peer-reviewed | Never filter out tests that fail on current code; never use coverage as the acceptance gate |
| Meta ACH (mutation-guided): kill rate **32% vs 5.3%** (TestGen-LLM); **49%** of mutation-killing tests add zero line coverage; **73% acceptance**; equivalence-mutant LLM judge at **0.95 precision / 0.96 recall** with preprocessing; 10,795 classes, 9,095 mutants, 571 tests | https://arxiv.org/abs/2501.12862 ; FSE 2025 — https://dl.acm.org/doi/10.1145/3696630.3728544 | Emerging/industrial | Adopt the ACH workflow on Stryker.NET: surviving mutant → LLM targeted killer → confirmed kill → accept |
| Oracle accuracy by prompt: full class-under-test (CUT) context **53.64% acc / 76.26% compile**; zero-shot **54.56%**; compiling-only subset **86.45%**; CoT/ToT collapse compile rate to ~44%; **prompting technique outweighs model choice (25.30% vs 6.68% variance)** | arXiv:2601.05542 — https://arxiv.org/html/2601.05542v1 | Peer-reviewed | Prompt = full CUT, zero-shot; avoid CoT for oracle generation |
| Oracle augmentation of existing test prefixes: 30% success vs TOGA 8.2% | AugmenTest — https://arxiv.org/html/2501.17461v1 (related: https://arxiv.org/html/2410.21136v1) | Emerging | Augmenting structured prefixes beats whole-test generation for oracles |
| Qodo/CodiumAI CoverAgent inherits the coverage-filtering defect; no peer-reviewed mutation data | https://dev.to/rahulxsingh/qodo-ai-test-generation-how-it-works-with-examples-4abk | Commercial | Do not adopt the CoverAgent pattern |
| Stryker.NET 4.14.2 (May 2026), Microsoft-documented, xUnit/NUnit/MSTest; `--since` / `--with-baseline` incremental CI; thresholds default `{high:80, low:60, break:0}` — **build never fails unless `break` is set**; .NET 10/C# 13 support implied (embedded Roslyn, "Preview") but **not explicitly documented** | https://github.com/stryker-mutator/stryker-net ; https://learn.microsoft.com/en-us/dotnet/core/testing/mutation-testing ; https://stryker-mutator.io/docs/stryker-net/configuration/ ; https://www.nuget.org/packages/dotnet-stryker ; https://johal.in/mutation-testing-with-stryker-net-and-python-coverage-2026/ | Established tool | `break-at 70` from day one (raise to 75/80); `--with-baseline` for PR delta runs; smoke-test .NET 10 before committing the stack |
| PIT lessons transfer: start 60–65% threshold; equivalent mutants need filtering; incremental history essential for CI; killed mutants cluster in recently changed code; flaky tests corrupt mutation scores | https://pitest.org/ ; https://www.javacodegeeks.com/2024/11/enhancing-java-testing-with-pit-a-guide-to-mutation-testing.html ; https://arxiv.org/pdf/1601.02351 | Established | Mutation-target actively maintained domain logic first; quarantine flaky tests before mutation runs |
| FsCheck 3.x (June 2025): C# fluent API, `[Property]`, automatic shrinking, EFSM command-based state-machine testing; custom `Arbitrary<T>` + shrinker per domain type is mandatory (defaults produce invalid domain objects) | https://fscheck.github.io/FsCheck/ ; https://bartwullems.blogspot.com/2025/06/property-based-testing-updating-fscheck.html ; https://developersvoice.com/blog/csharp/csharp-property-based-testing-fscheck-xunit/ | Established tool | One `Arbitrary<T>` per value object — the generator is the executable definition of "valid domain input" |
| PBT properties map 1:1 to DDD rules (round-trip, idempotency, commutativity, invariant-after-N-commands, model-based oracle) and excel at killing boundary mutants (`>=`→`>`) example tests miss | IEEE (FsCheck from business-rule models/EFSM) — https://ieeexplore.ieee.org/document/7528965/ ; Springer SoSyM — https://link.springer.com/article/10.1007/s10270-017-0647-0 | Established research | Encode each verified rule as a property where shape permits; validate each property kills its target mutants |
| CsCheck: idiomatic C#, lower ceremony, built-in model-based + concurrency testing, but thinner ecosystem and less LLM training data | https://bartwullems.blogspot.com/2024/02/property-based-testing-in-ccscheck.html | Established tool | Viable alternative; FsCheck preferred for ecosystem + AI-assistance reasons |
| Microsoft.Testing.Platform (20M+ downloads) supported by all major frameworks; xUnit v3 = standalone executables, assembly fixtures, per-test isolation (concern: 3 major versions in 6 months); TUnit = preview APIs, **Coverlet-incompatible**, thin LLM training data (FsCheck integration confirmed) | MTP — https://devblogs.microsoft.com/dotnet/mtp-adoption-frameworks/ ; xUnit v3 — https://xunit.net/docs/getting-started/v3/microsoft-testing-platform ; comparisons — https://codingdroplets.com/xunit-vs-nunit-vs-mstest-in-net-which-testing-framework-should-your-team-use-in-2026 , https://www.tatvasoft.com/outsourcing/2025/06/xunit-vs-nunit-vs-mstest.html ; TUnit — https://tunit.dev/docs/comparison/framework-differences/ , https://tunit.dev/docs/benchmarks/ , https://tunit.dev/docs/examples/fscheck/ , https://andrewlock.net/converting-an-xunit-project-to-tunit/ , https://oksala.net/2025/10/27/unit-testing-with-tunit/ | Official + practitioner | xunit.v3 on MTP; TUnit excluded until 1.0 stable |
| DDD test architecture consensus: domain tests in-memory only (no EF/HTTP), application tests mock repos with real domain objects, infrastructure tests separate and **excluded from mutation targets** | https://www.jamesmichaelhickey.com/ddd-unit-tests/ ; https://dev.to/ruben_alapont/testing-strategies-in-domain-driven-design-ddd-2d93 ; https://zenn.dev/135yshr/articles/78e3eb0a2a275a?locale=en | Practitioner consensus | Stryker scope = `Fokus.Domain` + `Identity.Domain` initially; exclude Persistence/API/Host |

**Recommended .NET 10 test stack (preserved in full):**

| Layer | Tool | Rationale |
|---|---|---|
| Test framework | `xunit.v3` | Stable, isolated-by-default (new instance per test), MTP support, largest ecosystem, best LLM training data for AI-assisted generation |
| Test runner | Microsoft.Testing.Platform | Embedded in test projects, no VSTest overhead, Microsoft's forward direction |
| Property-based testing | `FsCheck.Xunit` v3 | Canonical .NET PBT, `[Property]` integration, EFSM command-based model testing, mature shrinking |
| Snapshot / approval | `Verify.XUnit` | Bootstrap characterization scaffold; integration-test assertions on complex aggregate outputs |
| Mutation quality gate | `dotnet-stryker` v4.14+ | Only mature .NET mutation tool; `--with-baseline` CI delta; `break-at` as hard gate (`break-at 70` → 75 → 80) |
| Coverage (informational) | `Coverlet.Collector` | Line coverage is a secondary IDE signal, never the acceptance criterion |
| Assertions | `FluentAssertions` v7 | Readable failure messages; works alongside FsCheck prefixes |

TUnit explicitly not recommended now: (1) preview/unstable APIs, (2) Coverlet incompatibility, (3) thin LLM training data — all three are active risks for an AI-assisted mutation-guided workflow. Re-evaluate at TUnit 1.0.

**Design implications**
- The only valid oracle is explicit intent (rule statement / PBT property), never snapshotted observed behavior — except the time-limited Verify bootstrap.
- ACH workflow on .NET: Stryker finds surviving mutants → LLM (full CUT context, zero-shot, mutant description in prompt) writes targeted killers → Stryker confirms kill → accept; discard anything that doesn't raise mutation score.
- Mandatory pre-start verification: Stryker smoke test against a .NET 10 `.Domain` project.

## 4. Anti-Hallucination & Verification Gates

| Finding | Source (URL) | Class | Implication for harness |
|---|---|---|---|
| Mutation gate is **structurally** anti-hallucination: a test must demonstrably fail on the mutant and pass on the original — the mutant is executed, not trusted. ACH's five assurances (buildable / valid regression / hardening / coverage-info / style); first three are boolean guarantees | ACH — https://arxiv.org/html/2501.12862v1 , https://arxiv.org/pdf/2501.12862 ; InfoQ — https://www.infoq.com/news/2026/01/meta-llm-mutation-testing/ | Established (Meta-scale deployment) | Mutation proof is the strongest gate; keep as designed |
| Equivalence detector: precision/recall **0.79/0.47 raw → 0.95/0.96 with comment-stripping**; ~25% of LLM mutants are trivially syntactically identical (misleading-comment artifacts) | same ACH sources | Established | Add a comment-stripping equivalence **pre-filter** before the mutation gate or ~25% of cycles are wasted |
| Deployment numbers: 4,660 candidate mutants / 571 tests; **73% engineer acceptance (WhatsApp 56%, Messenger 90%)**; 36% privacy-relevant; harden vs catch distinction; target metric **R@P=0.8** (≤20% friction); human review removes ~3/5 of problematic test categories | Harden & Catch — https://arxiv.org/pdf/2504.16472 | Emerging | 73% acceptance is the realistic production baseline; expect wide domain variance |
| LLM-judge biases are systematic: position (swap flips GPT-4 verdicts), verbosity, self-preference (GPT-4 bias score **0.520**; perplexity-driven — judging with a *different* model does not fully remove it), authority (fabricated citations inflate scores) | https://arxiv.org/html/2410.21819v2 ; survey — https://arxiv.org/pdf/2602.07996 ; https://arxiv.org/pdf/2512.16041 | Established | LLM-judge verdicts are secondary signals only; randomize order; ensemble across model families |
| Judge-human agreement: >80% general tasks; **60–68%** in expert domains; best judges <0.7 accuracy on alignment data; extractive QA Pearson up to 0.85 | same sources | Established | Domain-specific (DDD) judging will sit at the low end — keep humans in the loop |
| **Competitive adversarial debate degrades accuracy up to −15% F1** ("debate hacking": fake evidence, overconfident tone, deflection). Collaborative ColMAD (+2% F1 / +5% F2; best pair QwQ-32B + DeepSeek-R1 = 80.01 F1 vs 74.85 single-agent) enforces exact-string-match evidence quoting and "find missing points" framing | ColMAD — https://arxiv.org/html/2510.20963 | Emerging | The adversarial-refutation gate must be collaborative, evidence-quoting — not zero-sum |
| Debate beats single-advisor consultancy across 9 tasks (~5M model calls); stronger debaters → higher judge accuracy; but debaters showed **81.2% relative increase** in persuading humans of *any* assigned stance regardless of truth | Kenton et al., DeepMind/ICML 2024 — https://arxiv.org/html/2504.18530v1 ; stability detection — https://arxiv.org/html/2510.12697v1 ; origins — https://www.researchgate.net/publication/324908324_AI_safety_via_debate | Established | Persuasion bias caps human-judged debate; ground refutation in source quotes |
| Self-consistency majority voting gains (+17.9% GSM8K, +11.0% SVAMP, +12.2% AQuA) **assume independence**; same-model samples have correlated errors (as intraclass ρ→1, gains vanish); minority votes carry error signal | Wang et al. via https://www.emergentmind.com/topics/self-consistency-in-language-models ; Mirror-Consistency — https://aclanthology.org/2024.findings-emnlp.135.pdf , https://arxiv.org/html/2410.10857v1 ; ρ analysis — https://arxiv.org/pdf/2509.19489 ; ICE — https://arxiv.org/pdf/2601.22290 ; error accumulation — https://arxiv.org/pdf/2505.24187 | Established + active research | Multi-pass voting needs architecturally distinct models (or distinct retrieval contexts) to mean anything |
| Verbalized confidence: ECE ≈ **0.10** for ≥70B models, systematically overconfident; best "combo" prompt reduces deviation to ~7%; RLVR training worsens calibration | https://arxiv.org/html/2412.14737v2 ; ICLR 2024 — https://openreview.net/forum?id=gjeQKFxFpZ , https://github.com/MiaoXiong2320/llm-uncertainty ; RLVR — https://arxiv.org/html/2605.15588 | Established | Verbalized high/medium/low cannot drive auto-flow on its own |
| Semantic entropy (cluster generations by bidirectional entailment, entropy over meaning clusters): AUROC ~77–80%; cost 5–10× → SEPs near-zero via hidden states; Bayesian variant needs only **53% of samples** | Farquhar et al., Nature 2024 — https://pubmed.ncbi.nlm.nih.gov/38898292/ ; https://oatml.cs.ox.ac.uk/blog/2024/06/19/detecting_hallucinations_2024.html ; SEPs — https://arxiv.org/abs/2406.15927 ; https://arxiv.org/html/2603.05881 | Established | Primary confidence proxy: semantic consistency over 3–5 samples |
| LPP meta-model (post-hoc uncertainty + verbalized bands + attribution flags; **CoT features excluded — they inflated confidence**): escalation **16%** (GPT-4o-mini; naive MSP baseline ~38%; GPT-4o 17%, GPT-4.1-mini 24%, Gemini-2.5-flash-lite 25%, multimodal 13%); **F1 94.14% / AUC-ROC 93.46%**; review cost **$132 → $38 (−71%)** | https://arxiv.org/html/2601.07006v1 | Emerging | Meta-model routing is the production pattern; calibrate thresholds empirically per domain, recalibrate weekly |
| **Up to 57% of citations are post-rationalized** — correct-but-unfaithful (claim generated from memory, citation assigned after); generate-then-retrieve pipelines are structurally incapable of faithful citation; citations must be produced inline | Wallat et al. — https://arxiv.org/pdf/2412.18004 ; FACTUM — https://arxiv.org/pdf/2601.05866 ; attribution survey — https://arxiv.org/pdf/2601.19927 | Emerging | Evidence-or-reject needs quote-**first** ordering + NLI entailment check (quote ⊨ rule), not just a file:line field |
| Quote-first protocols work in production: Anthropic Citations API — Endex source hallucinations **10% → 0%**, +20% references; Thomson Reuters CoCounsel replaced custom citation system; protocol: extract word-for-word quotes before the task, retract claims without supporting quotes | https://www.anthropic.com/news/introducing-citations-api ; https://docs.anthropic.com/en/docs/test-and-evaluate/strengthen-guardrails/reduce-hallucinations | Vendor claims (named customers) | Adopt quote-extraction-before-rule-statement as the Gate-1 mechanic |
| RAG reduces hallucinations ~71% vs vanilla LLMs but legal RAG still hallucinates **17–33%** of queries; FActScore: ChatGPT **58% faithfulness** on biographies; automated FActScore <2% error vs humans; retromorphic testing = reverse-map each claim to an exact source span | FActScore — https://123ofai.com/qnalab/system-design/blocks/faithfulness ; RT4CHART — https://arxiv.org/pdf/2603.27752 ; RAG review — https://arxiv.org/pdf/2507.18910 | Established / emerging | Grounding helps but does not finish the job; claim-level reverse mapping is the verification architecture |
| Code-description hallucination base rate: **36% of 3,120 samples contained ≥1 hallucination**; taxonomy: Behavior Conflicting **35.40%**, Library/Project **25.99%**, Undefined Variables 16.91%, Algorithm 4.95%, Data 4.21%, Theory 2.31%; pass@1 **54.73% (clean) vs 6.26% (hallucinatory)**; static analysis catches only **~17%** of instances; Incorrect Functionality affects **95.3%** of hallucinatory samples | https://arxiv.org/html/2404.00971v3 ; ETF — https://arxiv.org/pdf/2410.14748 ; FaithLens — https://arxiv.org/pdf/2512.20182 ; https://arxiv.org/html/2410.13961v1 ; survey — https://link.springer.com/article/10.1007/s10462-025-11454-w | Emerging (first taxonomy, 2024) | ~1 in 3 ungated rule extractions will contain a hallucinated claim; the dominant class (inventing what code does) is only catchable by evidence/execution gates |
| HITL production patterns: meta-model escalation 13–25%; 95% end-to-end accuracy ≈ 10–15% escalation, 99% ≈ 30–40%; two-stage triage; batch-by-similarity; priority lanes (P0 contradiction → immediate, P1 low-confidence → batch, P2 → auto-flow); irreversible actions (emitting tests to CI) always need human approval; recalibrate weekly, never real-time | https://arxiv.org/html/2601.07006v1 ; https://alldaystech.com/guides/artificial-intelligence/human-in-the-loop-ai-review-queue-workflows ; https://redis.io/blog/ai-human-in-the-loop/ ; https://www.grizzlypeaksoftware.com/library/human-in-the-loop-patterns-for-ai-agents-n64sb2cm | Established patterns | Contradictions: P0 immediate lane, never auto-resolve, never batch; escalation package = both rule statements + both source spans + conflict type + resolution templates (not verdicts) |
| Cross-cutting: same-base-model proposer/refuter pairs share blind spots (notably the Library/Project class at ~26% of code hallucinations); best ColMAD pair used distinct training lineages | https://arxiv.org/pdf/2509.19489 ; https://arxiv.org/html/2510.20963 ; https://arxiv.org/html/2404.00971v3 | Active research | Architecturally distinct models for proposer vs refuter is a hard requirement |

**Design implications (gate-by-gate verdict)**
- Gate "mutation proof": strongest, structural — keep; add the comment-stripping equivalence pre-filter (0.95/0.96 P/R).
- Gate "evidence-or-reject": validated but insufficient as designed — add quote-first ordering + NLI entailment + retromorphic claim tracing.
- Gate "adversarial refutation": reframe as collaborative, exact-quote-citing refutation; distinct base models.
- Gate "confidence gating": replace verbalized H/M/L with semantic entropy + post-hoc signals via meta-model routing; never use CoT-derived confidence; empirical threshold calibration with weekly recalibration.
- Gate "human escalation": correct — add queue engineering (P0 lanes, context-complete escalation packages, volume sizing for the 36% base rate).

## 5. Living Documentation & Rule-Test Traceability

| Finding | Source (URL) | Class | Implication for harness |
|---|---|---|---|
| The SbE "living documentation" promise failed at industry scale: only **12%** of teams use text files as primary requirements source, **57%** use Jira; feature files become "automation scripts buried in source control"; stories are throw-away artifacts; **~30% skip automation** (double the poor-quality rate: 11% vs 5%); dual-artifact (Gherkin + step definitions) maintenance is permanent | Adzic 2020 retrospective — https://gojko.net/2020/03/17/sbe-10-years.html ; book — https://www.oreilly.com/library/view/specification-by-example/9781617290084/ ; https://arxiv.org/pdf/2402.15928 | Established (authoritative post-mortem) | Gherkin OUT for domain-layer invariant tests — no non-developer audience, zero collaboration value, double maintenance surface |
| Reqnroll is the active .NET successor to SpecFlow (BSD-3, .NET 10, xUnit/NUnit/MSTest/TUnit, tagged `Rule` blocks; SpecFlow went commercial/dormant 2023) | https://reqnroll.net/ ; https://github.com/reqnroll/Reqnroll | Active OSS | Keep available only at the FastEndpoints acceptance boundary where a PO/QA actually reads `.feature` files |
| Living Documentation: docs are **generated and reconciled, not maintained**; annotations on the artifact (`[BoundedContext]`, `[DomainInvariant]`, `[GlossaryTerm]`) drive Roslyn/reflection extraction; CI diff gate (generated vs committed) removes human discipline as a dependency; failure to extract a glossary is a DDD design warning, not a doc gap; test names are harvestable living specs | Martraire — https://www.oreilly.com/library/view/living-documentation-continuous/9780134689418/ ; https://www.infoq.com/articles/book-review-living-documentation/ | Established | The harness is the KB maintainer; CI diff gate is the drift mechanism |
| Regulated-industry traceability (DO-178C: system req → HLR → LLR → code → test → procedure → result, bidirectional; ISO 26262 forward+backward to hazard) proves the chain works but prescribes no naming scheme; **no published .NET rule-ID naming standard exists** — practitioner pattern is `[Fact(DisplayName = "RULE-042: …")]` + `[Trait("RuleId", "RULE-042")]` (queryable via `dotnet test --filter`, extractable via reflection) | https://www.trace.space/blog/traceability-in-compliance-projects ; https://www.parasoft.com/learning-center/do-178c/integration-testing/ ; https://www.parasoft.com/learning-center/iso-26262/requirements-traceability/ ; https://www.perforce.com/resources/alm/requirements-traceability-matrix | Established practice | `[Trait("RuleId", …)]` is the traceability anchor; CI gate asserts every KB rule ID has ≥1 matching trait |
| ArchUnitNET (active, Apache 2.0, 2025 releases): `.Because("ARCH-042: …")` + `.As("ARCH-042")` embed rule IDs in failure output — the only .NET arch-test library doing so natively; custom `ICondition<T>` for domain predicates; NetArchTest stalled (last release May 2021); Roslyn analyzers with `DiagnosticId = "RULE042"` give compile-time enforcement matching the KB ID space | https://github.com/TNG/ArchUnitNET ; https://blog.nimblepros.com/blogs/getting-started-with-archunitnet/ ; https://github.com/BenMorris/NetArchTest ; https://code-maze.com/csharp-architecture-tests-with-netarchtest-rules/ | Active vs stalled OSS | ArchUnitNET for ARCH-class rules; Roslyn analyzers for the highest-value invariants |
| Swimm anchors human-authored doc snippets to code tokens and fails CI on unacknowledged drift — the right tool for hand-written docs, the wrong tool for a generated KB (CI diff is simpler and free); Vale lints terminology against a glossary; markdownlint for structure | https://swimm.io/learn/code-documentation/documentation-as-code-why-you-need-it-and-how-to-get-started ; https://overcast.blog/ai-driven-documentation-in-2026-f993f0c6d0d6 ; https://plainenglish.io/blog/my-honest-review-of-swimms-official-vs-code-extension ; https://vale.sh | Commercial + OSS | Drift = CI diff of generated vs committed KB; Vale enforces ubiquitous-language consistency |
| DDD canon treats invariants as first-class documented artifacts: Evans — "state post-conditions … and invariants of classes and aggregates; write automated unit tests"; Vernon — formulate invariants as sentences, keep them inside aggregate methods; Codely's Aggregate Design Blueprint (Context / Enforced Invariants / Corrective Policies / Domain Events) is the closest machine-readable invariant format, designed for AI input | Evans 2015 — https://www.domainlanguage.com/wp-content/uploads/2016/05/DDD_Reference_2015-03.pdf ; Vernon — https://www.archi-lab.io/infopages/ddd/aggregate-design-rules-vernon.html ; https://www.informit.com/articles/article.aspx?p=2020371&seqNum=2 ; Codely — https://codely.com/en/blog/como-implementar-codigo-ddd-usando-ia ; glossary — https://www.dddcommunity.org/resources/ddd_terms/ | Established + emerging | KB entries declare `bounded_context` + `aggregate`; EventStorming mapping `Command → Aggregate → Invariant check → Event/Rejection` = rule/trigger/evidence triple |
| Karpathy llm-wiki (16M+ views, 5K+ stars): three layers — immutable raw sources / LLM-owned wiki / human-owned schema file; **`index.md` is load-bearing** (agents read it first; without it every iteration is a full corpus scan); append-only grep-parseable `log.md`; agent lint pass (contradictions, stale claims, orphans); entries ≤ ~2k tokens; machine-readable `file:`/`line:` fields; typed `rel: contradicts \| extends \| supersedes` edges; supersession-not-deletion; confidence tiers routing ambiguous writes to human review | https://gist.github.com/karpathy/442a6bf555914893e9891c11519de94f ; https://venturebeat.com/data/karpathy-shares-llm-knowledge-base-architecture-that-bypasses-rag-with-an ; https://www.mindstudio.ai/blog/andrej-karpathy-llm-wiki-knowledge-base-claude-code | Emerging | KB structural model: rule_id primary key; index.md generated first and updated last every run; typed relationship edges instead of immediate contradiction resolution |
| llms.txt (844K+ sites by Oct 2025) is a website-level external-LLM convention, not an internal KB format; its writing guidance (concise, jargon-free) applies to KB prose | https://llmstxt.org/ | Emerging standard | Style guidance only |

**Design implications**
- KB entry schema — five non-negotiable frontmatter fields: `rule_id`, `evidence` (structured `file:`/`line:`/`kind:` YAML, not prose), `confidence`, `status` (`active | superseded | disputed | stale`), `last_verified`; plus `bounded_context`, `aggregate`, `kind` (`invariant | policy | architectural`), typed `relates_to`.
- Rule ID: flat `RULE-NNN`, classification via `kind:`; defer `INV-/POL-/ARCH-` prefixes past ~100 entries. Same ID flows through KB frontmatter, `[Trait("RuleId", …)]`, Roslyn `DiagnosticId`, ArchUnitNET `.Because()/.As()`, and the CI coverage gate.
- Gherkin out for the domain layer; plain `[Fact]` + `[Trait]`; Reqnroll only at the API acceptance boundary.
- Drift: generated-not-maintained; CI diff gate with `--accept` escape; auto-stale after N days; Vale terminology lint.
- `index.md` is the harness's performance boundary — first artifact written, last updated, every run.

## 6. Consolidated Design Implications

### Validated (keep as designed)
- **Mutation-proof gate** — the strongest, structurally anti-hallucination gate; closest prior art is Meta ACH at production scale (§4: arXiv:2501.12862, 73% acceptance, 32% vs 5.3% kill rate; §1: GI field; §3: ACH workflow on Stryker.NET).
- **Evidence-or-reject as the intake gate** — rules without a grounded file:line are hallucination risk, not knowledge (§2: AgentPex arXiv:2603.23806, traceability literature; §4: RAG faithfulness). Needs strengthening (see Must change) but the principle is the consensus of every thread.
- **Human escalation for contradictions** — validated by all deployed HITL literature; never auto-resolve, cost of wrong auto-resolution is unbounded (§4: LPP arXiv:2601.07006, HITL guides).
- **Loop-until-dry with a dry-run counter** — matches deployed stopping practice; systems converge within 5 iterations (§1: EmergentMind, arXiv:2511.00592).
- **Test-pinning as the `candidate → verified` promotion gate** — a rule is verified only when a test exercising its evidence path passes and kills ≥1 mutant (§1 implication 4; §2 implication 5: arXiv:2603.17193; §3: ACH).
- **Mining from code as ground truth** — code is more trustworthy than docs (§2: Sneed 1996); dual-source mining of domain guard clauses + FluentValidation validators with `source_layer` tagging (§2: Microsoft/Enterprise Craftsmanship).

### Must change (evidence contradicts current design)
- **Confidence gating cannot be verbalized high/medium/low.** ECE ≈ 0.10 systematic overconfidence; RLVR worsens it (§4: arXiv:2412.14737, ICLR 2024 benchmark). Replace with semantic entropy over 3–5 samples + post-hoc uncertainty signals via LPP-style meta-model routing (16% escalation at 94.14% F1, $132→$38); never use CoT-derived confidence; calibrate thresholds empirically and recalibrate weekly (§4: Nature 2024, arXiv:2601.07006).
- **Adversarial refutation must be collaborative, not competitive.** Zero-sum debate degrades accuracy up to **−15% F1** via debate hacking; collaborative exact-quote-citing refutation gains +2–5% (§4: ColMAD arXiv:2510.20963). The refuter must quote counter-evidence by exact string match and use "find missing points" framing.
- **file:line citation alone is insufficient for the evidence gate.** Up to **57% of citations are post-rationalized** (§4: Wallat arXiv:2412.18004). Enforce quote-first ordering (quote extracted *before* the rule is stated), verify quote ⊨ rule via NLI entailment, and retromorphically trace claims to exact source spans (§4: RT4CHART arXiv:2603.27752; Anthropic Citations 10%→0%).
- **Coverage must never be an acceptance criterion anywhere in the loop.** Coverage-gated generation structurally discards bug-revealing tests (470 rejected; 59.6–68.1% of final suites validate bugs) and misses the 49% of mutant-killing tests that add no coverage (§3: arXiv:2412.14137, arXiv:2501.12862). Coverage is informational only.
- **No LLM self-assessment anywhere in correction or stopping.** Intrinsic self-correction degrades performance; "LLM says nothing new found" is the coherence trap (§1: Huang arXiv:2310.01798, Zylos). Stopping = external multi-signal (dry-run counter + hard cap ~20 + plateau + mutation-score ratchet).
- **Single-pass LLM rule mining must become a two-stage Roslyn-grounded pipeline.** Single-pass caps at 71.6% with fabrication as the dominant failure mode; two-stage +29.2%; every production tool does this (§2: arXiv:2504.01294, IBM/AWS/Swimm/SLD-Spec).

### New additions (evidence suggests, not currently in proposal)
- **Roslyn static-analysis pre-stage** building the evidence index (aggregates/VOs by base type, guard clauses, FV chains, resolved symbols) before any LLM call (§2: Roslyn docs, DEV.to).
- **Mutation-equivalence pre-filter** (comment stripping: 0.79/0.47 → **0.95/0.96** P/R; ~25% of mutants trivially identical) before the mutation gate (§4: ACH).
- **Durable, git-SHA-versioned, indexed loop ledger with stateless iterations** — no conversation carry-over; per-rule episodic records of failed verification attempts (§1: arXiv:2604.08224, Memex arXiv:2603.04257, arXiv:2509.25250, arXiv:2502.06975).
- **Reward-hacking defenses**: holdout bug set the optimizer never sees; eval-infrastructure edit monitoring; agent has no write access to Stryker config, ledger schema, or coverage tooling; n-gram repetition detection; distribution monitoring (§1: arXiv:2603.11337, arXiv:2604.13602, ceaksan).
- **Architecturally distinct base models for proposer vs refuter** — same-model pairs share correlated blind spots (Library/Project class ≈ 26% of code hallucinations) (§4: arXiv:2509.19489, arXiv:2404.00971, ColMAD).
- **Concrete .NET test stack** (full table in §3): xunit.v3 + MTP + FsCheck.Xunit v3 + Verify.XUnit + dotnet-stryker `--with-baseline` `break-at 70` + Coverlet (informational) + FluentAssertions v7; TUnit excluded; Stryker scope = Domain projects only.
- **PBT as the rule-encoding form**: one `Arbitrary<T>` + shrinker per value object; EFSM command-based tests for aggregate state machines; properties validated by mutant kills (§3: FsCheck, IEEE 7528965).
- **Verify characterization scaffold (Pass 0, time-limited)** as the non-regression baseline before rule-encoding tests exist (§3: Feathers, Verify).
- **KB layer per the Karpathy pattern**: rule_id primary key, five non-negotiable frontmatter fields, ≤2k-token entries, typed `rel:` edges, load-bearing `index.md`, append-only `log.md`, supersession-not-deletion (§5: Karpathy gist).
- **Traceability anchors**: `[Trait("RuleId", "RULE-NNN")]` + DisplayName; ArchUnitNET `.Because()/.As()` for ARCH rules; Roslyn analyzer `DiagnosticId` matching KB IDs; CI gate "every verified rule has ≥1 trait-matched test" (§5).
- **Rule-rot CI hook**: per-PR `git diff` → affected files → diff-scoped re-extraction → stale-flag broken citations → block merge on stale verified rules (§2: Swimm, EvolveWare, RTM-in-CI).
- **Gherkin exclusion for the domain layer** (Reqnroll optional at the API acceptance boundary only) (§5: Adzic 2020).
- **HITL queue engineering**: P0/P1/P2 lanes, similarity batching, context-complete contradiction packages, human approval required for emitting tests to CI regardless of confidence (§4: Thread 7).
- **Multi-agent oracle consensus + mutant-in-prompt generation** for unclear expected values (§1/§3: CANDOR arXiv:2506.02943, MUTGEN arXiv:2506.02954).

### Open risks
- **Cross-agent attribution conflict on the "73%" figure.** The loops agent attributes 73% acceptance and the four-stage Assured-LLMSE filter to Meta TestGen-LLM (FSE 2024, arXiv:2402.09171), while the code-to-tests and verification agents attribute 73% acceptance (WhatsApp 56% / Messenger 90%) to Meta ACH (FSE 2025, arXiv:2501.12862). Both are Meta papers reporting a 73% figure; verify against the primary sources before citing either in the spec (§1 vs §3/§4).
- **Adversarial-framing tension.** §1/§3 endorse adversarial test-vs-mutant agent loops (arXiv:2602.08146) while §4 shows competitive debate harms accuracy (−15% F1). Resolution implied but not proven by the raw data: adversarial pressure is safe when the verdict is execution (a mutant dies or it doesn't) and harmful when the verdict is an LLM judge. The harness's refutation gate is the LLM-judged kind — it must be collaborative; the mutant sub-loop may stay adversarial.
- **Majority-vote confidence channel vs correlated errors.** §2 recommends N≥3 same-task LLM passes with majority vote (LogicScan); §4 shows same-model samples are weakly independent (gains vanish as ρ→1). The voting channel must use distinct model families or it overstates confidence (§2: arXiv:2602.03271 vs §4: arXiv:2509.19489).
- **Stryker.NET .NET 10 / C# 13 compatibility is implied, not documented.** Mandatory smoke test against a `.Domain` project before committing the stack; fallback is pinning domain projects or waiting for an explicit compatibility statement (§3).
- **xUnit v3 churn** — 3 major versions in 6 months; pin versions and budget for upgrades (§3: CodingDroplets).
- **No external accuracy benchmark exists for rule extraction** — metrics in the literature are fragmented; the harness's precision/recall must be measured against an internally built held-out ground-truth rule set (§2: Frontiers 2025).
- **A green test proves rule-implementation consistency, not business correctness.** Extracted rules inherit implementation bugs (§2: arXiv:2603.17193); near-miss formalization errors from proprietary models mean even high-confidence rules can be subtly wrong — the human gate cannot be removed for rule *intent*.
- **Escalation volume.** With a 36% base hallucination rate on code description (§4: arXiv:2404.00971) and production escalation norms of 16–25% (30–40% for 99% accuracy), the human review queue needs volume-sized design from day one or it becomes the loop's bottleneck.
- **Verify snapshots cement bugs if not retired** — the Pass-0 scaffold needs an explicit expiry/replacement plan (§3: Wikipedia/Feathers).
- **LLM-judge reliability ceiling in expert domains (60–68% human agreement)** — any judge-based canary signal in this DDD domain sits at the unreliable end; treat as advisory only (§4).

## Source Index

### Thread 1 — Self-Improving Loop Harnesses
- Reflexion (Shinn et al., NeurIPS 2023) — https://arxiv.org/abs/2303.11366
- Self-Refine (Madaan et al.) — https://arxiv.org/abs/2303.17651
- CRITIC (Gou et al., ICLR 2024) — https://arxiv.org/abs/2305.11738
- LLMs Cannot Self-Correct Reasoning Yet (Huang et al., ICLR 2024) — https://arxiv.org/abs/2310.01798
- SCoRe (ICLR 2025) — https://arxiv.org/abs/2409.12917
- Agent Self-Correction: Reflexion to PRM (Zylos, 2026) — https://zylos.ai/research/2026-05-12-agent-self-correction-reflexion-to-prm
- Landscape of Agentic RL for LLMs — https://arxiv.org/pdf/2509.02547
- Self-correction papers (curated list) — https://github.com/teacherpeterpan/self-correction-llm-papers
- FunSearch (DeepMind, Nature 2023) — https://deepmind.google/research/publications/funsearch-making-new-discoveries-in-mathematical-sciences-using-large-language-models/
- AlphaEvolve (DeepMind, 2025) — https://deepmind.google/research/publications/alphaevolve-a-gemini-based-coding-agent-for-designing-advanced-algorithms/
- AlphaEvolve technical overview (blog) — https://richardcsuwandi.github.io/blog/2025/llm-algorithm-discovery/
- Self-Evolving Agents survey — https://syhya.github.io/posts/2026-02-20-self-evolving-agents/
- CodeEvolve — https://arxiv.org/pdf/2510.14150
- SWE-agent (NeurIPS 2024) — https://arxiv.org/abs/2405.15793
- AutoCodeRover (ISSTA 2024) — https://dl.acm.org/doi/10.1145/3650212.3680384
- SWE-RL self-play — https://arxiv.org/abs/2512.18552
- Genetic Improvement of Software (field home) — https://geneticimprovementofsoftware.com/
- GI @ ICSE 2024 — https://geneticimprovementofsoftware.com/events/icse2024.html
- Agentic loops / convergence (EmergentMind) — https://www.emergentmind.com/topics/agentic-loops
- LLM agentic failure modes — https://ceaksan.com/en/llm-agentic-failure-modes
- RewardHackingAgents benchmark — https://arxiv.org/pdf/2603.11337
- Reward Hacking in Large Models survey — https://arxiv.org/html/2604.13602v1
- Agentic loop stopping criteria (optimal stopping) — https://arxiv.org/html/2511.00592v1
- Meta TestGen-LLM (FSE 2024) — https://arxiv.org/abs/2402.09171
- TestGen-LLM (ACM DL) — https://dl.acm.org/doi/10.1145/3663529.3663839
- TestGen-LLM ZenML summary — https://www.zenml.io/llmops-database/automated-unit-test-improvement-using-llms-for-android-applications
- Continuous evaluation of LLM test generation — https://arxiv.org/pdf/2504.18985
- Test vs Mutant adversarial agents — https://arxiv.org/abs/2602.08146
- MUTGEN mutation-guided generation — https://arxiv.org/pdf/2506.02954
- CANDOR multi-agent oracle consensus — https://arxiv.org/pdf/2506.02943
- CoverUp coverage-guided LLM test gen — https://arxiv.org/abs/2403.16218
- Do LLMs Generate Useful Test Oracles (ASE 2025) — https://conf.researchr.org/details/ase-2025/ase-2025-papers/10/Do-LLMs-Generate-Useful-Test-Oracles-An-Empirical-Study-with-an-Unbiased-Dataset
- A3Test: Assertion-Augmented Test Generation (IST 2024) — no URL in raw data
- Externalization in LLM Agents survey — https://arxiv.org/abs/2604.08224
- Memex(RL) indexed experience memory — https://arxiv.org/abs/2603.04257
- Memory for long-running low-code agents — https://arxiv.org/abs/2509.25250
- Episodic Memory is the Missing Piece — https://arxiv.org/abs/2502.06975
- SWE-Exp experience-driven resolution — https://arxiv.org/pdf/2507.23361
- Understanding Code Agent Behaviour — https://arxiv.org/pdf/2511.00197

### Thread 2 — Business Rule Extraction
- Business Rule Extraction from Legacy Code (Sneed 1996, IEEE) — https://ieeexplore.ieee.org/document/544158/
- Sneed 1996 (ResearchGate) — https://www.researchgate.net/publication/221029319_Business_Rule_Extraction_from_Legacy_Code
- Extracting Business Rules from Source Code (Huang et al. 1996) — https://www.researchgate.net/publication/3629947_Extracting_business_rules_from_source_code
- Getting Objects/Methods/Interactions by Extracting BRs (backward slicing) — https://www.academia.edu/29685854/Getting_Objects_Methods_and_Interactions_by_Extracting_Business_Rules_from_Legacy_Systems
- IJCSIT 2015 BRE survey — https://www.ijcsit.com/docs/Volume%206/vol6issue03/ijcsit2015060336.pdf
- Static predicate mining (PLDI 2007) — https://www.cs.purdue.edu/homes/suresh/papers/pldi07.pdf
- Scenario + value-based specification mining — https://www.se-rwth.de/publications/Scenario-based-and-value-based-specification-mining-better-together.pdf
- Interactive semi-automated specification mining — https://www.sciencedirect.com/science/article/abs/pii/S0950584919301004
- IBM watsonx Code Assistant for Z / ADDI overview — https://www.ibm.com/docs/en/watsonx/watsonx-code-assistant-4z/2.x?topic=addi-overview
- IBM Business Rules Discovery (Mediacenter) — https://mediacenter.ibm.com/media/IBM+watsonx+Code+Assistant+for+Z+Understand:+Business+rules+discovery/1_5pr695pa/189147203
- IBM agentic AI mainframe modernization (v2.8) — https://www.ibm.com/new/announcements/agentic-ai-for-smarter-mainframe-modernization-with-ibm-watsonx-code-assistant-for-z
- EvolveWare Agile BRE — https://evolveware.com/agile-business-rules-extraction/
- EvolveWare BRE blog (rules vs logic) — https://blog.evolveware.com/business-rules-extraction-why-understanding-rules-vs-logic-is-important/
- EvolveWare financial institutions case study — https://evolveware.com/business-rules-extraction-in-action-what-it-is-and-how-financial-institutions-can-do-it-right/
- Micro Focus COBOL Analyzer BRM (v5 docs) — https://www.microfocus.com/documentation/cobol-analyzer/ca503/CA/GUID-3521F158-A453-41C2-8851-870946F0821D.html
- Micro Focus Extracting Business Rules — https://www.microfocus.com/documentation/cobol-analyzer/ca60/CA/GUID-64A974DE-F7E2-4D71-AEAC-45397C1B1996.html
- AWS Transform Reimagine announcement — https://aws.amazon.com/blogs/aws/aws-transform-for-mainframe-introduces-reimagine-capabilities-and-automated-testing-functionality/
- AWS Blu Insights refactor — https://aws.amazon.com/blogs/mt/aws-mainframe-modernization-refactor-legacy-code-base-to-java-using-aws-blu-insights/
- AWS Transform comprehensive refactor — https://aws.amazon.com/blogs/migration-and-modernization/accelerate-mainframe-modernization-with-aws-transform-a-comprehensive-refactor-approach/
- Swimm Enterprise — https://swimm.io/enterprise
- Swimm: Blackbox to Blueprint — https://swimm.io/blog/blackbox-to-blueprint-extracting-business-logic-from-cobol-applications
- Swimm: 5 COBOL BRE Tools (2026) — https://swimm.io/learn/cobol/5-cobol-business-rules-extraction-tools-to-know-in-2026
- Swimm (home, auto-sync) — https://swimm.io/
- CodeLogic (correction: not a BRE tool) — https://codelogic.com/
- Daikon (official) — https://plse.cs.washington.edu/daikon/
- Daikon SCP 2007 paper — https://homes.cs.washington.edu/~mernst/pubs/daikon-tool-scp2007.pdf
- Daikon TSE 2001 paper — https://homes.cs.washington.edu/~mernst/pubs/invariants-tse2001.pdf
- Daikon (ACM DL) — https://dl.acm.org/doi/10.1016/j.scico.2007.01.015
- SLD-Spec: LLM + program slicing — https://arxiv.org/abs/2509.09917
- LogicScan business invariant mining — https://arxiv.org/pdf/2602.03271
- Extracting Formal Specifications from Documents Using LLMs (Li et al.) — https://arxiv.org/abs/2504.01294
- Talk is Cheap, Logic is Hard (post-condition formalization) — https://arxiv.org/abs/2603.17193
- Diffray: LLM hallucinations in code review — https://diffray.ai/blog/llm-hallucinations-code-review/
- LLM Hallucinations in Practical Code Generation (ACM 2025) — https://dl.acm.org/doi/pdf/10.1145/3728894
- Frontiers: LLMs for Requirements Engineering (2025) — https://www.frontiersin.org/journals/computer-science/articles/10.3389/fcomp.2025.1519437/full
- AgentPex explicit-only extraction — https://arxiv.org/pdf/2603.23806
- Compiled AI (AlphaCodium/MetaGPT reference) — https://arxiv.org/html/2604.05150
- 4D-ARE attribution gap — https://arxiv.org/pdf/2601.04556
- Bridging Domain Knowledge and Process Discovery — https://arxiv.org/pdf/2408.17316
- Towards Automatic Extraction of Structural Business Rules (WCRE 2012) — https://ojcchar.github.io/files/2-wcre12.pdf
- Rule-based generation of requirements traceability relations — https://www.researchgate.net/publication/223738681_Rule-based_generation_of_requirments_traceability_relations
- From SBVR to BPMN and DMN — https://www.researchgate.net/publication/303626206_From_SBVR_to_BPMN_and_DMN_Models_Proposal_of_Translation_from_Rules_to_Process_and_Decision_Models
- SBVR ↔ DMN relationship (BRCommunity) — https://www.brcommunity.com/articles.php?id=b597
- Evolute: rules overview — https://www.evolute.be/thoughts/rules.html
- Trisotech DMN standard — https://www.trisotech.com/dmn/
- Traceability Aspects for BR Management (BRCommunity 2008) — https://www.brcommunity.com/articles.php?id=b384
- Traceability Matrix in CI/CD (Medium) — https://medium.com/@sancharini.panda/how-a-traceability-matrix-fits-into-modern-ci-cd-workflows-714c5a6862af
- Analysing a .NET Codebase with Roslyn (DEV.to) — https://dev.to/mattjhosking/analysing-a-net-codebase-with-roslyn-5cn0
- Roslyn: Getting Started C# Semantic Analysis — https://github.com/dotnet/roslyn/blob/main/docs/wiki/Getting-Started-C%23-Semantic-Analysis.md
- Roslyn: Analyzer Actions Semantics — https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md
- Microsoft: Domain Model Layer Validations — https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-model-layer-validations
- Enterprise Craftsmanship: Validation and DDD — https://enterprisecraftsmanship.com/posts/validation-and-ddd/
- Enterprise Craftsmanship: Always-Valid Domain Model — https://enterprisecraftsmanship.com/posts/always-valid-domain-model/
- NDepend .NET dependency graph — https://www.ndepend.com/net-dependency-graph

### Thread 3 — Reverse-Engineering Code into Tests
- Characterization Testing (Michael Feathers) — https://michaelfeathers.silvrback.com/characterization-testing
- Characterization vs Approval Tests (UnderstandLegacyCode) — https://understandlegacycode.com/blog/characterization-tests-or-approval-tests/
- Characterization test (Wikipedia) — https://en.wikipedia.org/wiki/Characterization_test
- Verify (Simon Cropp, GitHub) — https://github.com/verifytests/verify
- Snapshot Testing in .NET with Verify (JetBrains) — https://blog.jetbrains.com/dotnet/2024/07/11/snapshot-testing-in-net-with-verify/
- Simplify Assertions with Verify (antondevtips) — https://antondevtips.com/blog/how-to-simplify-assertions-in-unit-and-integration-tests-with-verify-in-dotnet
- Pex — White Box Test Generation for .NET (Springer) — https://link.springer.com/chapter/10.1007/978-3-540-79124-9_10
- IntelliTest manual (Microsoft Docs GitHub) — https://github.com/MicrosoftDocs/visualstudio-docs/blob/main/docs/test/intellitest-manual/index.md
- EvoSuite large-scale evaluation — https://www.researchgate.net/publication/273188653_A_Large-Scale_Evaluation_of_Automated_Unit_Test_Generation_Using_EvoSuite
- Diffblue 20x productivity claim (BusinessWire) — https://www.businesswire.com/news/home/20251104720918/en/Diffblues-Latest-Innovations-in-Unit-Test-Generation-Deliver-20x-Productivity-Advantage-Versus-AI-Coding-Assistants
- Diffblue funding (SiliconANGLE) — https://siliconangle.com/2024/10/30/diffblue-raises-6-3m-autonomous-ai-powered-test-generation-platform/
- Meta ACH: Mutation-Guided LLM Test Generation — https://arxiv.org/abs/2501.12862
- ACH (FSE 2025 proceedings, ACM) — https://dl.acm.org/doi/10.1145/3696630.3728544
- LLM test-generator design choices prevent bug finding — https://arxiv.org/html/2412.14137v1
- Understanding LLM-Driven Test Oracle Generation — https://arxiv.org/html/2601.05542v1
- AugmenTest LLM-driven oracles — https://arxiv.org/html/2501.17461v1
- Do LLM oracles capture actual or expected behavior — https://arxiv.org/html/2410.21136v1
- Qodo / CodiumAI test generation (DEV) — https://dev.to/rahulxsingh/qodo-ai-test-generation-how-it-works-with-examples-4abk
- Stryker.NET (GitHub) — https://github.com/stryker-mutator/stryker-net
- Mutation testing in .NET (Microsoft Learn) — https://learn.microsoft.com/en-us/dotnet/core/testing/mutation-testing
- Stryker.NET configuration — https://stryker-mutator.io/docs/stryker-net/configuration/
- dotnet-stryker (NuGet) — https://www.nuget.org/packages/dotnet-stryker
- Mutation testing with Stryker .NET (johal.in) — https://johal.in/mutation-testing-with-stryker-net-and-python-coverage-2026/
- PIT mutation testing — https://pitest.org/
- Enhancing Java Testing with PIT (JavaCodeGeeks) — https://www.javacodegeeks.com/2024/11/enhancing-java-testing-with-pit-a-guide-to-mutation-testing.html
- Assessing the Mutation Testing Practice of PIT — https://arxiv.org/pdf/1601.02351
- FsCheck official docs — https://fscheck.github.io/FsCheck/
- Updating FsCheck to v3.x (Bart Wullems) — https://bartwullems.blogspot.com/2025/06/property-based-testing-updating-fscheck.html
- CsCheck PBT in C# (Bart Wullems) — https://bartwullems.blogspot.com/2024/02/property-based-testing-in-ccscheck.html
- FsCheck + xUnit guide (DevelopersVoice) — https://developersvoice.com/blog/csharp/csharp-property-based-testing-fscheck-xunit/
- PBT from business-rule models (IEEE) — https://ieeexplore.ieee.org/document/7528965/
- PBT from business-rule models (Springer SoSyM) — https://link.springer.com/article/10.1007/s10270-017-0647-0
- TUnit FsCheck integration — https://tunit.dev/docs/examples/fscheck/
- TUnit framework differences — https://tunit.dev/docs/comparison/framework-differences/
- TUnit benchmarks — https://tunit.dev/docs/benchmarks/
- Converting xUnit to TUnit (Andrew Lock) — https://andrewlock.net/converting-an-xunit-project-to-tunit/
- TUnit vs xUnit/NUnit (Panu Oksala) — https://oksala.net/2025/10/27/unit-testing-with-tunit/
- Microsoft.Testing.Platform adoption (.NET Blog) — https://devblogs.microsoft.com/dotnet/mtp-adoption-frameworks/
- xUnit v3 + MTP — https://xunit.net/docs/getting-started/v3/microsoft-testing-platform
- xUnit vs NUnit vs MSTest 2026 (CodingDroplets) — https://codingdroplets.com/xunit-vs-nunit-vs-mstest-in-net-which-testing-framework-should-your-team-use-in-2026
- xUnit vs NUnit vs MSTest (TatvaSoft) — https://www.tatvasoft.com/outsourcing/2025/06/xunit-vs-nunit-vs-mstest.html
- DDD Unit Tests (James Hickey) — https://www.jamesmichaelhickey.com/ddd-unit-tests/
- Testing Strategies in DDD (DEV) — https://dev.to/ruben_alapont/testing-strategies-in-domain-driven-design-ddd-2d93
- DDD testing: aggregates and domain services (Zenn) — https://zenn.dev/135yshr/articles/78e3eb0a2a275a?locale=en

### Thread 4 — Anti-Hallucination & Verification
- ACH (arXiv HTML) — https://arxiv.org/html/2501.12862v1
- ACH (PDF) — https://arxiv.org/pdf/2501.12862
- Meta LLM mutation testing (InfoQ) — https://www.infoq.com/news/2026/01/meta-llm-mutation-testing/
- Harden and Catch (JiTTest) — https://arxiv.org/pdf/2504.16472
- Self-preference bias (NeurIPS 2024) — https://arxiv.org/html/2410.21819v2
- LLM judge bias survey — https://arxiv.org/pdf/2602.07996
- Assessing LLM-as-a-Judge (human agreement) — https://arxiv.org/pdf/2512.16041
- ColMAD collaborative multi-agent debate — https://arxiv.org/html/2510.20963
- Debate for scalable oversight (Kenton et al., DeepMind/ICML 2024) — https://arxiv.org/html/2504.18530v1
- Multi-agent debate with adaptive stability detection — https://arxiv.org/html/2510.12697v1
- AI Safety via Debate (Irving, Christiano, Amodei 2018) — https://www.researchgate.net/publication/324908324_AI_safety_via_debate
- Self-consistency in language models (EmergentMind) — https://www.emergentmind.com/topics/self-consistency-in-language-models
- Mirror-Consistency (EMNLP 2024) — https://aclanthology.org/2024.findings-emnlp.135.pdf
- Mirror-Consistency (arXiv) — https://arxiv.org/html/2410.10857v1
- Intraclass correlation / self-consistency estimation — https://arxiv.org/pdf/2509.19489
- Iterative Consensus Ensemble (ICE) — https://arxiv.org/pdf/2601.22290
- Beyond Exponential Decay: error accumulation in LLMs — https://arxiv.org/pdf/2505.24187
- On Verbalized Confidence Scores for LLMs — https://arxiv.org/html/2412.14737v2
- Can LLMs Express Their Uncertainty (ICLR 2024) — https://openreview.net/forum?id=gjeQKFxFpZ
- LLM uncertainty benchmark (GitHub) — https://github.com/MiaoXiong2320/llm-uncertainty
- Semantic entropy (Farquhar et al., Nature 2024) — https://pubmed.ncbi.nlm.nih.gov/38898292/
- Detecting hallucinations (OATML blog) — https://oatml.cs.ox.ac.uk/blog/2024/06/19/detecting_hallucinations_2024.html
- Semantic Entropy Probes — https://arxiv.org/abs/2406.15927
- Confidence Before Answering — https://arxiv.org/html/2603.05881
- Calibration degradation from RLVR — https://arxiv.org/html/2605.15588
- LPP: learning when to escalate — https://arxiv.org/html/2601.07006v1
- Correctness is Not Faithfulness (Wallat et al.) — https://arxiv.org/pdf/2412.18004
- FACTUM citation hallucination in RAG — https://arxiv.org/pdf/2601.05866
- RT4CHART retromorphic testing — https://arxiv.org/pdf/2603.27752
- Anthropic Citations API — https://www.anthropic.com/news/introducing-citations-api
- Anthropic: reduce hallucinations — https://docs.anthropic.com/en/docs/test-and-evaluate/strengthen-guardrails/reduce-hallucinations
- FActScore / faithfulness reference — https://123ofai.com/qnalab/system-design/blocks/faithfulness
- Attribution survey — https://arxiv.org/pdf/2601.19927
- RAG systems review — https://arxiv.org/pdf/2507.18910
- Beyond Functional Correctness: code hallucinations — https://arxiv.org/html/2404.00971v3
- ETF: entity tracing for code summaries — https://arxiv.org/pdf/2410.14748
- FaithLens — https://arxiv.org/pdf/2512.20182
- Multi-document summarization hallucination — https://arxiv.org/html/2410.13961v1
- Hallucination survey (Springer AI Review) — https://link.springer.com/article/10.1007/s10462-025-11454-w
- HITL review-queue workflows (alldaystech) — https://alldaystech.com/guides/artificial-intelligence/human-in-the-loop-ai-review-queue-workflows
- AI human-in-the-loop (Redis) — https://redis.io/blog/ai-human-in-the-loop/
- HITL patterns for AI agents (Grizzly Peak) — https://www.grizzlypeaksoftware.com/library/human-in-the-loop-patterns-for-ai-agents-n64sb2cm

### Thread 5 — Living Documentation & Traceability
- Specification by Example, 10 Years Later (Adzic 2020) — https://gojko.net/2020/03/17/sbe-10-years.html
- Specification by Example (O'Reilly) — https://www.oreilly.com/library/view/specification-by-example/9781617290084/
- Advancing BDD: dynamic scenario re-usability — https://arxiv.org/pdf/2402.15928
- Living Documentation (Martraire, O'Reilly) — https://www.oreilly.com/library/view/living-documentation-continuous/9780134689418/
- Living Documentation Q&A (InfoQ) — https://www.infoq.com/articles/book-review-living-documentation/
- Reqnroll — https://reqnroll.net/
- Reqnroll (GitHub) — https://github.com/reqnroll/Reqnroll
- ArchUnitNET (TNG, GitHub) — https://github.com/TNG/ArchUnitNET
- Getting Started with ArchUnitNET (NimblePros) — https://blog.nimblepros.com/blogs/getting-started-with-archunitnet/
- NetArchTest (GitHub) — https://github.com/BenMorris/NetArchTest
- Architecture tests in .NET (Code Maze) — https://code-maze.com/csharp-architecture-tests-with-netarchtest-rules/
- Traceability in Compliance Projects (Trace.Space) — https://www.trace.space/blog/traceability-in-compliance-projects
- DO-178C Integration Testing (Parasoft) — https://www.parasoft.com/learning-center/do-178c/integration-testing/
- ISO 26262 Requirements Traceability (Parasoft) — https://www.parasoft.com/learning-center/iso-26262/requirements-traceability/
- Requirements Traceability Matrix (Perforce) — https://www.perforce.com/resources/alm/requirements-traceability-matrix
- Documentation as Code (Swimm) — https://swimm.io/learn/code-documentation/documentation-as-code-why-you-need-it-and-how-to-get-started
- AI-Driven Documentation 2026 (Overcast) — https://overcast.blog/ai-driven-documentation-in-2026-f993f0c6d0d6
- Swimm VS Code extension review (Plain English) — https://plainenglish.io/blog/my-honest-review-of-swimms-official-vs-code-extension
- Vale prose linter — https://vale.sh
- DDD Community glossary — https://www.dddcommunity.org/resources/ddd_terms/
- DDD Reference (Evans, 2015) — https://www.domainlanguage.com/wp-content/uploads/2016/05/DDD_Reference_2015-03.pdf
- Aggregate Design Rules — Vernon (archi-lab.io) — https://www.archi-lab.io/infopages/ddd/aggregate-design-rules-vernon.html
- Model True Invariants in Consistency Boundaries (InformIT) — https://www.informit.com/articles/article.aspx?p=2020371&seqNum=2
- Aggregate Design Blueprint with AI (Codely) — https://codely.com/en/blog/como-implementar-codigo-ddd-usando-ia
- Karpathy llm-wiki gist — https://gist.github.com/karpathy/442a6bf555914893e9891c11519de94f
- Karpathy LLM Knowledge Base (VentureBeat) — https://venturebeat.com/data/karpathy-shares-llm-knowledge-base-architecture-that-bypasses-rag-with-an
- Karpathy LLM Wiki pattern (MindStudio) — https://www.mindstudio.ai/blog/andrej-karpathy-llm-wiki-knowledge-base-claude-code
- llms.txt specification — https://llmstxt.org/
