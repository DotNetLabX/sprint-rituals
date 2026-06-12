I now have sufficient data across all six research threads. Here is the dense, structured research output.

---

## Research: Reverse-Engineering a .NET 10 DDD Codebase into an Automated Test Suite

---

### THREAD 1 — Characterization / Golden-Master / Approval Testing

**Source authority: established practice (2004 origin, continuously evolved)**

**Feathers' framing.**
Michael Feathers coined "characterization test" in *Working Effectively with Legacy Code* (2004, Prentice Hall). Definition: a test that describes the *actual, current* behavior of existing code — not its intended behavior. Goal: create a safety net before refactoring so regressions become visible. The method is: run the code, observe output, encode that observation as an assertion. The test pins what the code does now.
Source: https://michaelfeathers.silvrback.com/characterization-testing

**Characterization vs. Approval vs. Golden-Master.**
These are synonyms with nuanced emphasis. "Approval" emphasizes that a human explicitly approved the snapshot and can revoke that approval. "Golden Master" comes from audio mastering — the one truth copy. "Characterization" emphasizes describing behavior as-found. All three use the same mechanics: capture output → store as baseline file → compare on future runs.
Source: https://understandlegacycode.com/blog/characterization-tests-or-approval-tests/

**Verify (the canonical .NET library).**
Author: Simon Cropp. Actively maintained. Supports xUnit, NUnit, MSTest, Expecto, TUnit. Install: `Verify.XUnit`. On first run, test fails and writes a `.received.*` file. Human reviews the diff, promotes to `.verified.*`. All future runs compare against the verified file. Excellent for complex domain objects (serializes to JSON by default), HTTP responses, HTML, images, source generators. Handle dynamic properties (generated IDs, timestamps) via `IgnoreMember` or `ScrubMember`.
Source: https://github.com/verifytests/verify
Source: https://blog.jetbrains.com/dotnet/2024/07/11/snapshot-testing-in-net-with-verify/ (JetBrains, July 2024)

**Critical limitation (the bug-cementing risk).**
Characterization tests cannot distinguish correct from incorrect behavior in the current version. If the code has a latent bug today, the snapshot captures the bug as the approved behavior. This is the canonical objection: "Golden Master testing does not infer correctness of the results." For a greenfield extraction into business-rule-verifying tests, characterization tests serve as a bootstrap scaffold only — they must be replaced or augmented with intent-encoding assertions.
Source: https://en.wikipedia.org/wiki/Characterization_test

**When characterization tests help vs. harm.**
Help: (1) When you need to refactor before you can write real tests and need a safety net against regression. (2) For complex output shapes (aggregated reports, computed sprint metrics) where assertion exhaustion is impractical. (3) As the first pass in a coverage-then-intention workflow.
Harm: (1) When the existing behavior is wrong — you lock in the bug. (2) Non-deterministic output (timestamps, GUIDs, random ordering) requires masking so many fields the test loses value. (3) If used as final-form tests — mutation testing will score them poorly because they assert the whole output, not individual rule violations.

**Applicability to this project:** Use Verify-based characterization tests as Pass 0 scaffolding only — lock sprint computation outputs as a non-regression baseline, then systematically replace individual assertions with rule-encoding PBT properties and Stryker-gated unit tests.

---

### THREAD 2 — Automated Test Generation (Pre-LLM Tools)

**Source authority: established research + commercial tools**

**EvoSuite (JVM, search-based).**
Generates test suites by optimizing for code coverage using genetic algorithms. Proven effective for branch coverage. Core weakness: all generated oracles are regression oracles — they assert the current (possibly buggy) behavior. Cannot detect faults in the current version. Additionally: "test cases are hard to read and interpret" — low developer acceptance. No .NET port exists; relevant as conceptual precedent only.
Source: https://www.researchgate.net/publication/273188653_A_Large-Scale_Evaluation_of_Automated_Unit_Test_Generation_Using_EvoSuite

**Randoop (JVM, random feedback-based).**
Generates test sequences by random method chaining, pruning sequences that throw exceptions. Also regression-oracle only. JVM only. Not applicable to .NET.

**Pex / IntelliTest (.NET, dynamic symbolic execution).**
Pex (Microsoft Research, 2008) and its Visual Studio successor IntelliTest use dynamic symbolic execution (DSE) to systematically explore branches and generate parameterized unit tests with high path coverage. Key constraint: Visual Studio Enterprise only; supports C# targeting .NET Framework (not .NET Core / .NET 10). The constraint solver uses randomized algorithms → tests not always reproducible. No .NET 10 support confirmed. IntelliTest generates test inputs, not test oracles — the developer still writes assertions. Effectively abandoned for modern .NET.
Source: https://link.springer.com/chapter/10.1007/978-3-540-79124-9_10
Source: https://github.com/MicrosoftDocs/visualstudio-docs/blob/main/docs/test/intellitest-manual/index.md

**Diffblue Cover (Java, RL-based).**
Reinforcement-learning approach to automated unit test generation. Java only. Claims 250 tests/minute, 20x productivity over LLM coding assistants (per Diffblue's own benchmark, November 2025 — note: vendor-funded). Raises coverage by 50% beyond baseline in one hour (vendor claim). No .NET support. Relevant as a design pattern reference (RL for test generation), not as a usable tool.
Source: https://www.businesswire.com/news/home/20251104720918/en/Diffblues-Latest-Innovations-in-Unit-Test-Generation-Deliver-20x-Productivity-Advantage-Versus-AI-Coding-Assistants
Source: https://siliconangle.com/2024/10/30/diffblue-raises-6-3m-autonomous-ai-powered-test-generation-platform/

**Universal weakness (the regression oracle problem).**
All pre-LLM automated tools generate regression oracles. An oracle generated from code that contains a bug will encode the bug as the expected behavior. This is structurally unsolvable within the coverage-maximization paradigm — it requires external intent specification (requirements, specs, properties).

**Applicability:** No pre-LLM automated test generation tool supports .NET 10. The regime is either obsolete (IntelliTest) or Java-only (EvoSuite, Diffblue). The correct .NET approach for this project is: LLM-assisted draft generation + mutation testing as the quality gate.

---

### THREAD 3 — LLM-Based Test Generation

**Source authority: emerging research (2024-2025 papers) + commercial products**

**Meta TestGen-LLM (predecessor to ACH).**
Coverage-driven LLM test generation. Generates tests iteratively until coverage targets are met. Key weakness (exposed by December 2024 arXiv paper): by filtering out tests that fail on current implementation, it systematically discards the tests most likely to reveal bugs. 49% of tests that uniquely kill a mutant do not add line coverage — so coverage-gated generation misses them entirely.
Source: referenced in https://arxiv.org/html/2412.14137v1

**Meta ACH (mutation-guided LLM test generation, FSE 2025).**
ACH = Assured Coverage Hardening. Workflow: (1) Generate targeted mutants for a specific concern (privacy in their case). (2) Filter equivalent mutants via an LLM judge agent (precision 0.95 / recall 0.96 with preprocessing). (3) Prompt LLM to generate tests that kill those specific mutants. Applied to 10,795 Android Kotlin classes across 7 Meta platforms. Generated 9,095 mutants, 571 privacy-hardening tests. Engineer acceptance rate: 73%. 36% judged privacy-relevant. Mutation kill rate (ACH): 32% vs. TestGen-LLM: 5.3%. Tests that kill mutants without adding line coverage: 49% — proving mutation-guidance is essential and coverage-guidance is insufficient.
Source: https://arxiv.org/abs/2501.12862
FSE 2025 proceedings: https://dl.acm.org/doi/10.1145/3696630.3728544

**The oracle problem in LLM test generation (arXiv, December 2024).**
Study of 287 buggy code samples, three tools: GitHub Copilot, Codium CoverAgent, CoverUp.
- Copilot (no filtering): 194 bug-revealing tests kept, 66 junk tests kept, 5.2% of final tests were valid non-bug tests.
- CoverAgent: 470 bug-revealing tests *rejected* (they fail on current buggy code), 171 of 287 final tests (59.6%) validated the bug rather than detecting it.
- CoverUp: 400 bug-revealing tests rejected, 68.1% of final tests validated bugs; failed to produce any suite at all for 196/287 cases.
Root cause: all filtering approaches that discard tests failing on current code will systematically discard exactly the tests that matter. The paper's conclusion: "code should be written based on requirements, not the other way around."
Source: https://arxiv.org/html/2412.14137v1

**LLM oracle correctness (arXiv, January 2025 survey).**
Best configuration (GPT-4o, full CUT context, zero-shot): 54.56% oracle accuracy, 67.38% compilation rate. When filtered to only compiling tests: GPT-4o achieves 86.45% accuracy. Chain-of-Thought (CoT) and Tree-of-Thought (ToT) achieve 65-70% accuracy *when they compile* but compilation rate drops to 44%. Key finding: "prompting technique has stronger impact on oracle effectiveness than LLM choice" (25.30% vs. 13.26% vs. 6.68% variance by factor). Including the full class-under-test in context (CUT prompt) is the single most impactful variable.
Source: https://arxiv.org/html/2601.05542v1

**Qodo / CodiumAI (commercial).**
CodiumAI rebranded to Qodo in 2024. Their open-source CoverAgent is inspired by Meta's TestGen-LLM. Same structural problem: coverage-first filtering discards bug-revealing tests. No published peer-reviewed mutation kill rates. The December 2024 study covers their approach directly.
Source: https://dev.to/rahulxsingh/qodo-ai-test-generation-how-it-works-with-examples-4abk

**AugmenTest (arXiv, January 2025).**
Proposes using LLMs specifically to augment existing test oracles (add assertions to tests that already have structure). Outperforms TOGA (prior state-of-art oracle generation): 30% success rate vs. TOGA's 8.2% in conservative threshold scenario.
Source: https://arxiv.org/pdf/2501.17461

**Applicability to this project:** LLM generation is most valuable as a *draft* layer for initial test skeleton creation, not as a quality-complete approach. The mutation-guided ACH pattern is directly adoptable: generate Stryker.NET mutants for domain rules → prompt LLM to write tests that kill specific mutants → verify via Stryker that mutation score increases. Never use coverage-only filtering — it discards the tests that actually prove rules.

---

### THREAD 4 — Mutation Testing as Test-Quality Oracle

**Source authority: established practice (PIT/JVM) + mature .NET tool (Stryker.NET)**

**The mutation testing model.**
Inject syntactic faults (mutants) into production code → run test suite → count killed (test failed) vs. survived (test passed, meaning no test caught the fault). Mutation score = killed / total. A test suite that achieves 100% line coverage but 40% mutation score is demonstrably inadequate. Mutation score is the most direct proxy for "tests will catch regressions."

**PIT (JVM) key lessons.**
PIT is the JVM reference implementation. Key patterns applicable to any mutation tool:
- Start at 60-65% threshold; raise incrementally. Targeting 85% from day one on a greenfield-from-legacy codebase causes build paralysis.
- Equivalent mutants: mutations that are syntactically different but semantically identical (unreachable code, pure constants). They survive without indicating a test gap. Manual or LLM-based filtering needed.
- Incremental analysis (`withHistory`): persist `pit-history.xml` across builds; re-test only changed code. Essential for CI viability.
- Killed mutants cluster in recently changed and introduced code — prioritize mutation testing of actively maintained domain logic first.
- Flaky tests wreak havoc: mutation reruns the same test multiple times with modified code; a test that occasionally fails non-deterministically produces false surviving mutants.
Source: https://pitest.org/
Source: https://javacodegeeks.com/2024/11/enhancing-java-testing-with-pit-a-guide-to-mutation-testing.html

**Stryker.NET — maturity and .NET 10 support.**
Version: 4.14.2 (May 2026). Active development (2,107 commits, 147 open issues as of June 2026). Officially documented by Microsoft at learn.microsoft.com/en-us/dotnet/core/testing/mutation-testing — a strong maturity signal. Supports MSTest, xUnit, NUnit. TUnit support: not explicitly confirmed in current docs but Stryker uses the test framework's runner, and TUnit's MTP integration should be compatible. .NET 10 support: Stryker embeds a Roslyn compiler; C# language version support goes to "Preview" (relative to the embedded Roslyn version, not the SDK). .NET 10 / C# 13 compatibility is implied by active maintenance but not explicitly documented — verification required before project start.
Source: https://github.com/stryker-mutator/stryker-net
Source: https://learn.microsoft.com/en-us/dotnet/core/testing/mutation-testing
Source: https://stryker-mutator.io/docs/stryker-net/configuration/

**Stryker.NET mutation level categories:**
- Basic: arithmetic operators, block statements, logical operators, bitwise operators
- Standard: equality, boolean literals, string literals, LINQ methods
- Advanced: regex, math methods, string methods
- Complete: all mutations

**Incremental mutation (`--since` and `--with-baseline`).**
`--since:<committish>`: run mutation tests only on code changed since a git reference. Reports only on mutants within changed code. Test file changes cause all mutants covered by those tests to be re-run.
`--with-baseline`: extends `--since` — saves full mutation report to disk/Dashboard/Azure/S3. On subsequent runs, loads baseline and only re-tests changed code. Mutually exclusive with explicit `--since`. This is the correct CI strategy: full mutation run on main branch sets baseline; PR runs use `--with-baseline` for fast delta reports.

**Threshold configuration (default: `{ high: 80, low: 60, break: 0 }`).**
`break` at 0 means the build never fails by default — must be set explicitly. For a rule-coverage gate, recommended: `break-at 70`, `low 75`, `high 85`. Raise the break threshold over time as the suite matures.

**Performance on real projects.**
No published large-.NET-project benchmark data in current search results. The exponential-backoff CI retry advice (from a 2025 trial, unverified vendor source) suggests timeouts at >200ms network latency in distributed CI. For local/single-machine runs, performance is governed by test suite execution time multiplied by mutant count. Targeted mutation (`--since`) is the primary mitigation.

**Applicability:** Stryker.NET is the mandatory quality gate layer for this project. Every test in the suite must kill at least one mutant, or it does not count. The --with-baseline pattern makes incremental mutation CI-viable from day one.

---

### THREAD 5 — Property-Based Testing for .NET

**Source authority: established practice + active library development (FsCheck 3.x, June 2025)**

**The rule-to-property mapping.**
PBT inverts the example-test paradigm: instead of asserting `f(specific_input) == specific_output`, you assert `for all x in Domain: invariant(f(x)) == true`. This directly encodes business rules. Key property categories for DDD:
- **Round-trip**: `decode(encode(x)) == x` — for serialization, VO conversions
- **Idempotency**: `f(f(x)) == f(x)` — for normalization, state machines
- **Commutativity/Associativity**: for aggregation, scoring, metric rollups
- **Invariant-after-N-operations**: `for any sequence of valid commands on an aggregate, invariant still holds`
- **Model-based oracle**: compare real implementation to a simpler reference model — extremely powerful for sprint analytics where a naive calculation can serve as the oracle for an optimized one

**FsCheck 3.x (canonical .NET PBT).**
Version 3.x released; new API split: functional interface for F#, fluent interface for C#/VB. Install: `dotnet add package FsCheck.Xunit`. Attribute: `[Property]`. Shrinking is automatic — when a property fails, FsCheck iterates toward the minimal failing input. Custom `Arbitrary<T>` generators are required for domain types; the default generator will produce invalid domain objects (e.g., negative sprint velocities). Supports state machine / command-based testing (EFSM model) — directly applicable to aggregate command sequences.
Source: https://fscheck.github.io/FsCheck/
Source: https://bartwullems.blogspot.com/2025/06/property-based-testing-updating-fscheck.html (June 2025)

**CsCheck (C#-first alternative).**
Idiomatic C# API, fast, no ceremony for generator definition, built-in model-based testing primitives, concurrency testing support. No specific xUnit/NUnit attribute integration — works with any framework via lambda. Less community-tested than FsCheck; fewer examples in AI training data. Preferable over FsCheck for pure C# codebases if the team values ergonomics.
Source: https://bartwullems.blogspot.com/2024/02/property-based-testing-in-ccscheck.html (Feb 2024)

**TUnit + FsCheck integration.**
TUnit has documented FsCheck integration: https://tunit.dev/docs/examples/fscheck/ — confirms FsCheck works with TUnit as the test runner.

**IEEE paper: deriving FsCheck properties from business-rule models.**
Approach: parse business rule model (EFSM) → auto-generate FsCheck `ICommand` implementations → FsCheck generates random command sequences and verifies state postconditions. Directly applicable pattern for sprint aggregate state machines (e.g., sprint open → synced → closed → archived state transitions).
Source: https://ieeexplore.ieee.org/document/7528965/

**Custom shrinker requirement.**
Without a custom shrinker for domain types, failing test cases remain at their full generated complexity. For a value object like `SprintVelocity`, the shrinker needs to know that simpler versions are smaller positive integers — not random byte sequences. Budget: define `Arbitrary<T>` + shrinker for all core domain value objects before using PBT.

**Mutation-PBT synergy.**
PBT properties, when encoded as business rules rather than behavior mimicry, are excellent at killing mutants. A property asserting "velocity is always non-negative after any sprint sync" will kill boundary mutations (`>=` → `>`, `!= 0` → `== 0`) that example tests often miss.

**Applicability:** FsCheck 3.x is the correct PBT tool for this project. Define one `Arbitrary<T>` generator per domain value object. Encode each identified business rule as a property. Use command-based FsCheck testing for aggregate state machines (sprint lifecycle). Validate PBT quality by running Stryker on PBT-covered code and verifying the property kills its target mutants.

---

### THREAD 6 — .NET Test Stack State-of-the-Art 2025/2026

**Source authority: official framework docs + community practitioner analysis**

**Microsoft.Testing.Platform (MTP).**
Lightweight alternative to VSTest. Embedded directly in test projects — no `vstest.console` or `dotnet test` host process required. 20M+ downloads. All major frameworks (xUnit.net, NUnit, MSTest, TUnit, Expecto) now support MTP. For .NET 10 greenfield: MTP is the correct runner choice.
Source: https://devblogs.microsoft.com/dotnet/mtp-adoption-frameworks/

**xUnit v3 (`xunit.v3` package).**
Architectural break from v2 — deliberately separate NuGet package to prevent accidental upgrade. Test projects compile to standalone executables (no external runner). New: assembly fixtures (shared setup without collection forcing). MTP support included. Per-test class instance isolation is non-negotiable (no shared mutable state). v3 has had rapid releases (3 major versions in 6 months as of 2026 — cited concern for framework stability).
Source: https://xunit.net/docs/getting-started/v3/microsoft-testing-platform
Source: https://codingdroplets.com/xunit-vs-nunit-vs-mstest-in-net-which-testing-framework-should-your-team-use-in-2026

**TUnit.**
Built natively on MTP only — no VSTest fallback. Source-generated test discovery (compile-time, not reflection). Supports Native AOT and single-file apps. Parallelism model: `--maximum-parallel-tests N` controls actual concurrent tests (not thread count), correctly handling async yielding. Lifecycle hooks at every level. Async-first design. Single `[Test]` attribute. FsCheck integration documented. Known gaps: (1) still in preview, APIs not marked stable; (2) Coverlet incompatibility (code coverage collection requires alternative); (3) LLM tooling has less training data for TUnit than xUnit/NUnit.
Source: https://tunit.dev/docs/comparison/framework-differences/
Source: https://tunit.dev/docs/benchmarks/

**NUnit 4.x.**
Actively maintained. Shared fixture instance model (unlike xUnit). Granular `[Parallelizable]` control — best-in-class for surgical concurrency tuning. MTP support added. Good choice if team has NUnit expertise.
Source: https://www.tatvasoft.com/outsourcing/2025/06/xunit-vs-nunit-vs-mstest.html

**Verify (snapshot/approval testing).**
Simon Cropp. Works with xUnit, NUnit, MSTest, TUnit. Serialize domain objects to `.verified.json` files, reviewed and committed to source control. Handles scrubbing of dynamic properties. Best used for integration-test assertions on complex aggregate outputs or API responses. NuGet package per framework: `Verify.Xunit`, `Verify.NUnit`, `Verify.TUnit`.
Source: https://github.com/verifytests/verify
Source: https://antondevtips.com/blog/how-to-simplify-assertions-in-unit-and-integration-tests-with-verify-in-dotnet (Jan 2025)

**Pure domain test architecture.**
Consensus pattern for DDD .NET:
- Domain layer tests: in-memory only, no EF Core, no HTTP, no file system. Instantiate aggregates directly, call domain methods, assert on domain events raised and state changes. No mocks except repository interfaces (if domain methods accept them as parameters).
- Application layer tests: mock repositories and event buses, use real domain objects.
- Infrastructure tests: separate project, real SQLite, run in CI only — not in the mutation testing target set.
Stryker should target the domain and application projects only; exclude infrastructure and API layers from mutation targets initially.
Source: https://www.jamesmichaelhickey.com/ddd-unit-tests/
Source: https://dev.to/ruben_alapont/testing-strategies-in-domain-driven-design-ddd-2d93

---

### TOP 5 DESIGN IMPLICATIONS

**1. The only valid test oracle is explicit intent, not observed behavior.**
All coverage-based tools (EvoSuite, TestGen-LLM, CoverAgent) generate regression oracles that encode bugs as expected behavior. The ACH paper proves that 49% of mutation-killing tests add no line coverage. Design implication: the harness must encode rules explicitly as assertions or PBT properties — never derive expected values by running the existing code and snapshotting results, except for the initial Verify-based bootstrap scaffold.

**2. Stryker.NET `--with-baseline` is the mandatory quality gate, not coverage.**
Mutation score replaces line/branch coverage as the acceptance criterion. A test that does not kill at least one mutant does not count toward the rule-coverage contract. Use `break-at 70` from day one; raise incrementally. Use `--with-baseline` in CI to make incremental mutation PR-viable from the first sprint. Target: `Fokus.Domain` and `Identity.Domain` projects only in the initial mutation scope; exclude Persistence and API.

**3. LLM generates drafts; mutation testing validates them.**
Adopt the ACH workflow adapted for .NET: (1) Run Stryker on the domain project to identify surviving mutants. (2) For each surviving mutant, prompt an LLM with the full class-under-test context (CUT prompt — highest accuracy) to write a test that specifically kills that mutant. (3) Verify the mutation score increases. (4) Discard any LLM-generated test that does not improve mutation score. Never use coverage-gated LLM generation (CoverAgent pattern) — it discards the tests that matter.

**4. Property-based testing is the native language of DDD business rules.**
Each business invariant maps directly to an FsCheck property. Sprint velocity must be non-negative: `for all SprintVelocity v: v.Value >= 0`. Health score must be within [0, 100]: `for all HealthScore s: s.Value is in [0, 100]`. State machine transitions for sprint lifecycle: encode as FsCheck command-based model tests against the aggregate. Write one `Arbitrary<T>` generator per value object; the generator is the authoritative definition of "valid domain input." PBT properties resist implementation drift better than example tests and naturally kill boundary mutants.

**5. Recommended .NET 10 test stack for this project.**
| Layer | Tool | Rationale |
|---|---|---|
| Test framework | `xunit.v3` | Stable, isolated-by-default, MTP support, large ecosystem, better LLM training data than TUnit |
| Test runner | Microsoft.Testing.Platform | Embedded in test project, no VSTest overhead |
| Property-based | `FsCheck.Xunit` (v3) | Canonical .NET PBT, xUnit integration, EFSM state machine model |
| Snapshot/approval | `Verify.XUnit` | Bootstrap characterization, integration test assertions |
| Mutation | `dotnet-stryker` v4.14+ | Only mature .NET mutation tool; `--with-baseline` for CI; `break-at` as quality gate |
| Coverage (informational) | `Coverlet.Collector` | Line coverage is secondary but useful for IDE tooling |
| Assertion | `FluentAssertions` v7 | Readable, exception-message quality, works with FsCheck test prefixes |

TUnit is not recommended for this project at this time: it is in preview (unstable APIs), incompatible with Coverlet, and has thin LLM training data — all three are risks for an AI-assisted test-generation workflow. Re-evaluate at TUnit 1.0 stable.

---

### Sources

- [Characterization Testing — Michael Feathers](https://michaelfeathers.silvrback.com/characterization-testing)
- [Characterization Tests or Approval Tests — UnderstandLegacyCode](https://understandlegacycode.com/blog/characterization-tests-or-approval-tests/)
- [Characterization test — Wikipedia](https://en.wikipedia.org/wiki/Characterization_test)
- [Verify — GitHub (Simon Cropp)](https://github.com/verifytests/verify)
- [Snapshot Testing in .NET with Verify — JetBrains (July 2024)](https://blog.jetbrains.com/dotnet/2024/07/11/snapshot-testing-in-net-with-verify/)
- [How To Simplify Assertions with Verify — antondevtips (Jan 2025)](https://antondevtips.com/blog/how-to-simplify-assertions-in-unit-and-integration-tests-with-verify-in-dotnet)
- [Pex – White Box Test Generation for .NET — Springer](https://link.springer.com/chapter/10.1007/978-3-540-79124-9_10)
- [IntelliTest Manual — Microsoft Docs GitHub](https://github.com/MicrosoftDocs/visualstudio-docs/blob/main/docs/test/intellitest-manual/index.md)
- [Diffblue Cover 20x Productivity Claim — BusinessWire (Nov 2025)](https://www.businesswire.com/news/home/20251104720918/en/Diffblues-Latest-Innovations-in-Unit-Test-Generation-Deliver-20x-Productivity-Advantage-Versus-AI-Coding-Assistants)
- [Diffblue $6.3M Funding — SiliconANGLE (Oct 2024)](https://siliconangle.com/2024/10/30/diffblue-raises-6-3m-autonomous-ai-powered-test-generation-platform/)
- [Mutation-Guided LLM-based Test Generation at Meta (ACH) — arXiv 2501.12862](https://arxiv.org/abs/2501.12862)
- [ACH — FSE 2025 Proceedings — ACM DL](https://dl.acm.org/doi/10.1145/3696630.3728544)
- [Design choices made by LLM-based test generators prevent them from finding bugs — arXiv 2412.14137 (Dec 2024)](https://arxiv.org/html/2412.14137v1)
- [Understanding LLM-Driven Test Oracle Generation — arXiv 2601.05542 (Jan 2025)](https://arxiv.org/html/2601.05542v1)
- [AugmenTest: Enhancing Tests with LLM-Driven Oracles — arXiv 2501.17461](https://arxiv.org/html/2501.17461v1)
- [Do LLMs generate test oracles that capture actual or expected behavior? — arXiv 2410.21136](https://arxiv.org/html/2410.21136v1)
- [Qodo / CodiumAI Test Generation — DEV Community](https://dev.to/rahulxsingh/qodo-ai-test-generation-how-it-works-with-examples-4abk)
- [Stryker.NET — GitHub](https://github.com/stryker-mutator/stryker-net)
- [Mutation testing — .NET | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/testing/mutation-testing)
- [Stryker.NET Configuration Docs](https://stryker-mutator.io/docs/stryker-net/configuration/)
- [Mutation Testing with Stryker .NET 2026 — johal.in](https://johal.in/mutation-testing-with-stryker-net-and-python-coverage-2026/)
- [PIT Mutation Testing — pitest.org](https://pitest.org/)
- [Enhancing Java Testing with PIT — JavaCodeGeeks (Nov 2024)](https://www.javacodegeeks.com/2024/11/enhancing-java-testing-with-pit-a-guide-to-mutation-testing.html)
- [Assessing and Improving the Mutation Testing Practice of PIT — arXiv 1601.02351](https://arxiv.org/pdf/1601.02351)
- [FsCheck Official Docs](https://fscheck.github.io/FsCheck/)
- [Updating FsCheck to v3.x — Bart Wullems (June 2025)](https://bartwullems.blogspot.com/2025/06/property-based-testing-updating-fscheck.html)
- [Property-based testing in C# with CsCheck — Bart Wullems (Feb 2024)](https://bartwullems.blogspot.com/2024/02/property-based-testing-in-ccscheck.html)
- [FsCheck + xUnit Guide for C# — DevelopersVoice (Nov 2025)](https://developersvoice.com/blog/csharp/csharp-property-based-testing-fscheck-xunit/)
- [PBT with FsCheck by Deriving Properties from Business Rule Models — IEEE Xplore](https://ieeexplore.ieee.org/document/7528965/)
- [TUnit FsCheck integration example](https://tunit.dev/docs/examples/fscheck/)
- [TUnit Framework Differences](https://tunit.dev/docs/comparison/framework-differences/)
- [TUnit Performance Benchmarks](https://tunit.dev/docs/benchmarks/)
- [Converting xUnit to TUnit — Andrew Lock](https://andrewlock.net/converting-an-xunit-project-to-tunit/)
- [TUnit vs xUnit and NUnit — Panu Oksala (Oct 2025)](https://oksala.net/2025/10/27/unit-testing-with-tunit/)
- [Microsoft.Testing.Platform Adoption — .NET Blog](https://devblogs.microsoft.com/dotnet/mtp-adoption-frameworks/)
- [xUnit v3 + Microsoft.Testing.Platform — xunit.net](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform)
- [xUnit vs NUnit vs MSTest 2026 — CodingDroplets](https://codingdroplets.com/xunit-vs-nunit-vs-mstest-in-net-which-testing-framework-should-your-team-use-in-2026)
- [DDD Unit Tests — James Hickey](https://www.jamesmichaelhickey.com/ddd-unit-tests/)
- [Testing Strategies in DDD — DEV Community](https://dev.to/ruben_alapont/testing-strategies-in-domain-driven-design-ddd-2d93)
- [DDD Testing: Aggregates and Domain Services — Zenn](https://zenn.dev/135yshr/articles/78e3eb0a2a275a?locale=en)
- [EvoSuite Large-Scale Evaluation — ResearchGate](https://www.researchgate.net/publication/273188653_A_Large-Scale_Evaluation_of_Automated_Unit_Test_Generation_Using_EvoSuite)

================================================================================

Here is the dense research output synthesized across all six threads:

---

## Research: Reverse-Engineering a .NET 10 DDD Codebase into an Automated Test Suite

---

### THREAD 1 — Characterization / Golden-Master / Approval Testing

**Source authority: established practice (2004 origin, continuously evolved)**

**Feathers' framing.**
Michael Feathers coined "characterization test" in *Working Effectively with Legacy Code* (2004). Definition: a test that describes the *actual, current* behavior of existing code — not its intended behavior. Goal: create a safety net before refactoring so regressions become visible. The method is: run the code, observe output, encode that observation as an assertion. The test pins what the code does now.
Source: https://michaelfeathers.silvrback.com/characterization-testing

**Characterization vs. Approval vs. Golden-Master.**
These are synonyms with nuanced emphasis. "Approval" emphasizes that a human explicitly approved the snapshot and can revoke that approval. "Golden Master" comes from audio mastering — the one truth copy. "Characterization" emphasizes describing behavior as-found. All three use the same mechanics: capture output → store as baseline file → compare on future runs.
Source: https://understandlegacycode.com/blog/characterization-tests-or-approval-tests/

**Verify (canonical .NET library).**
Author: Simon Cropp. Actively maintained. Supports xUnit, NUnit, MSTest, Expecto, TUnit. Install: `Verify.XUnit`. On first run, test fails and writes a `.received.*` file. Human reviews the diff, promotes to `.verified.*`. All future runs compare against the verified file. Excellent for complex domain objects (serializes to JSON by default), HTTP responses, HTML, images, source generators. Handle dynamic properties (generated IDs, timestamps) via `IgnoreMember` or `ScrubMember`.
Source: https://github.com/verifytests/verify
Source: https://blog.jetbrains.com/dotnet/2024/07/11/snapshot-testing-in-net-with-verify/ (JetBrains, July 2024)

**Critical limitation — the bug-cementing risk.**
Characterization tests cannot distinguish correct from incorrect behavior in the current version. If the code has a latent bug today, the snapshot captures the bug as the approved behavior. "Golden Master testing does not infer correctness of the results." For a business-rule-verifying test suite, characterization tests serve as a bootstrap scaffold only — they must be replaced or augmented with intent-encoding assertions.
Source: https://en.wikipedia.org/wiki/Characterization_test

**When characterization tests help vs. harm.**
Help: (1) When you need to refactor before you can write real tests. (2) For complex output shapes (aggregated reports, computed sprint metrics) where assertion exhaustion is impractical. (3) As the first pass in a coverage-then-intention workflow.
Harm: (1) When the existing behavior is wrong — you lock in the bug. (2) Non-deterministic output requires masking so many fields the test loses value. (3) If used as final-form tests — mutation testing will score them poorly because they assert the whole output, not individual rule violations.

---

### THREAD 2 — Automated Test Generation (Pre-LLM Tools)

**Source authority: established research + commercial tools**

**EvoSuite (JVM, search-based).**
Generates test suites by optimizing for code coverage using genetic algorithms. Core weakness: all generated oracles are regression oracles — they assert the current (possibly buggy) behavior. "Test cases are hard to read and interpret" — low developer acceptance. No .NET port exists. Relevant as conceptual precedent only.
Source: https://www.researchgate.net/publication/273188653_A_Large-Scale_Evaluation_of_Automated_Unit_Test_Generation_Using_EvoSuite

**Pex / IntelliTest (.NET, dynamic symbolic execution).**
Pex (Microsoft Research, 2008) and its Visual Studio successor IntelliTest use dynamic symbolic execution (DSE) to systematically explore branches and generate parameterized unit tests with high path coverage. Key constraint: Visual Studio Enterprise only; supports C# targeting .NET Framework — not .NET Core / .NET 10. IntelliTest generates test inputs, not test oracles — the developer still writes assertions. Effectively abandoned for modern .NET.
Source: https://link.springer.com/chapter/10.1007/978-3-540-79124-9_10
Source: https://github.com/MicrosoftDocs/visualstudio-docs/blob/main/docs/test/intellitest-manual/index.md

**Diffblue Cover (Java, RL-based).**
Reinforcement-learning approach to automated unit test generation. Java only. Claims 250 tests/minute, 20x productivity over LLM coding assistants (Diffblue-funded benchmark, November 2025). No .NET support. Relevant as a design pattern reference (RL for test generation), not as a usable tool.
Source: https://www.businesswire.com/news/home/20251104720918/en/Diffblues-Latest-Innovations-in-Unit-Test-Generation-Deliver-20x-Productivity-Advantage-Versus-AI-Coding-Assistants

**Universal weakness — the regression oracle problem.**
All pre-LLM automated tools generate regression oracles. An oracle generated from code that contains a bug will encode the bug as the expected behavior. This is structurally unsolvable within the coverage-maximization paradigm — it requires external intent specification (requirements, specs, properties).

**Applicability:** No pre-LLM automated test generation tool supports .NET 10. The correct .NET approach for this project is LLM-assisted draft generation + mutation testing as the quality gate.

---

### THREAD 3 — LLM-Based Test Generation

**Source authority: emerging research (2024–2025 papers) + commercial products**

**Meta TestGen-LLM (predecessor to ACH).**
Coverage-driven LLM test generation. Generates tests iteratively until coverage targets are met. Key weakness (exposed by December 2024 arXiv paper): by filtering out tests that fail on current implementation, it systematically discards the tests most likely to reveal bugs. 49% of tests that uniquely kill a mutant do not add line coverage — so coverage-gated generation misses them entirely.
Source: referenced in https://arxiv.org/html/2412.14137v1

**Meta ACH (mutation-guided LLM test generation, FSE 2025 — emerging research).**
ACH = Assured Coverage Hardening. Workflow: (1) Generate targeted mutants for a specific concern. (2) Filter equivalent mutants via an LLM judge agent (precision 0.95 / recall 0.96 with preprocessing). (3) Prompt LLM to generate tests that kill those specific mutants. Applied to 10,795 Android Kotlin classes across 7 Meta platforms. Generated 9,095 mutants, 571 privacy-hardening tests. Engineer acceptance rate: 73%. 36% judged privacy-relevant. Mutation kill rate — ACH: 32% vs. TestGen-LLM: 5.3%. Tests that kill mutants without adding line coverage: 49% — proving mutation-guidance is essential and coverage-guidance is insufficient.
Source: https://arxiv.org/abs/2501.12862
FSE 2025 proceedings: https://dl.acm.org/doi/10.1145/3696630.3728544

**The oracle problem in LLM test generation (arXiv December 2024).**
Study of 287 buggy code samples across Copilot, CoverAgent (CodiumAI), and CoverUp:

| Outcome | Copilot | CoverAgent | CoverUp |
|---|---|---|---|
| Bug-revealing tests kept | 194 | **470 rejected** | **400 rejected** |
| Final tests that validate the bug (not detect it) | 4.2% | **59.6%** | **68.1%** |
| Failed to produce any suite | 0 | 0 | 68.3% of cases |

Root cause: all filtering approaches that discard tests failing on current code will systematically discard exactly the tests that matter. "Code should be written based on requirements, not the other way around."
Source: https://arxiv.org/html/2412.14137v1

**LLM oracle correctness measured (arXiv January 2025).**
Best configuration (GPT-4o, full class-under-test context, zero-shot): 54.56% oracle accuracy, 67.38% compilation rate. When filtered to only compiling tests: 86.45% accuracy. Key finding: "prompting technique has stronger impact on oracle effectiveness than LLM choice" (25.30% vs. 6.68% variance). Including the full class-under-test in context (CUT prompt) is the single most impactful variable.
Source: https://arxiv.org/html/2601.05542v1

**AugmenTest (arXiv January 2025).**
Uses LLMs specifically to augment existing test oracles (add assertions to tests that already have structure). Outperforms TOGA (prior state-of-art): 30% success rate vs. TOGA's 8.2% in conservative threshold scenario.
Source: https://arxiv.org/html/2501.17461v1

**Applicability to this project:** LLM generation is most valuable as a draft layer for initial test skeleton creation, not as a quality-complete approach. The mutation-guided ACH pattern is directly adoptable: generate Stryker.NET mutants for domain rules → prompt LLM with full CUT context to write tests that kill specific mutants → verify via Stryker that mutation score increases. Never use coverage-only filtering — it discards the tests that actually prove rules.

---

### THREAD 4 — Mutation Testing as Test-Quality Oracle

**Source authority: established practice (PIT/JVM) + mature .NET tool (Stryker.NET)**

**The mutation testing model.**
Inject syntactic faults (mutants) into production code → run test suite → count killed (test failed) vs. survived (test passed). Mutation score = killed / total. A test suite that achieves 100% line coverage but 40% mutation score is demonstrably inadequate. Mutation score is the most direct proxy for "tests will catch regressions."

**PIT (JVM) key lessons, transferable to Stryker.NET:**
- Start at 60–65% threshold; raise incrementally. Targeting 85% from day one on a greenfield-from-legacy codebase causes build paralysis.
- Equivalent mutants: mutations that are syntactically different but semantically identical. They survive without indicating a test gap. LLM-based filtering (ACH approach) can detect them at 0.95 precision.
- Incremental analysis: persist history file across builds; re-test only changed code. Essential for CI viability.
- Killed mutants cluster in recently changed and introduced code — prioritize mutation testing of actively maintained domain logic first.
- Flaky tests wreak havoc: mutation reruns the same test multiple times with modified code; non-deterministic tests produce false surviving mutants.
Source: https://pitest.org/
Source: https://www.javacodegeeks.com/2024/11/enhancing-java-testing-with-pit-a-guide-to-mutation-testing.html

**Stryker.NET — maturity and current state.**
Version: 4.14.2 (May 2026). Active development. Officially documented by Microsoft at learn.microsoft.com — a strong maturity signal. Supports MSTest, xUnit, NUnit. .NET 10 / C# 13 compatibility: Stryker embeds its own Roslyn compiler; C# language support goes to "Preview" (relative to the embedded Roslyn version). .NET 10 compatibility is implied by active maintenance but verify before project start.
Source: https://github.com/stryker-mutator/stryker-net
Source: https://learn.microsoft.com/en-us/dotnet/core/testing/mutation-testing
Source: https://stryker-mutator.io/docs/stryker-net/configuration/

**Stryker.NET mutation level categories:**
- Basic: arithmetic operators, block statements, logical operators, bitwise operators
- Standard: equality, boolean literals, string literals, LINQ methods
- Advanced: regex, math methods, string methods
- Complete: all mutations

**Incremental mutation (`--since` and `--with-baseline`).**
`--since:<committish>`: run mutation tests only on code changed since a git reference. Reports only on mutants within changed code.
`--with-baseline`: extends `--since` — saves full mutation report to disk/Dashboard/Azure/S3. On subsequent runs, loads baseline and only re-tests changed code. Mutually exclusive with explicit `--since`. Correct CI strategy: full mutation run on main sets baseline; PR runs use `--with-baseline` for fast delta reports.

**Threshold configuration (default: `{ high: 80, low: 60, break: 0 }`).**
`break` at 0 means the build never fails by default — must be set explicitly. Recommended initial gate: `break-at 70`, `low 75`, `high 85`. Raise break threshold over time as the suite matures.

---

### THREAD 5 — Property-Based Testing for .NET

**Source authority: established practice + active library development (FsCheck 3.x, June 2025)**

**The rule-to-property mapping.**
PBT inverts the example-test paradigm: instead of asserting `f(specific_input) == specific_output`, you assert `for all x in Domain: invariant(f(x)) == true`. This directly encodes business rules. Key property categories for DDD:
- **Round-trip**: `decode(encode(x)) == x` — for serialization, VO conversions
- **Idempotency**: `f(f(x)) == f(x)` — for normalization, state machines
- **Commutativity/Associativity**: for aggregation, scoring, metric rollups
- **Invariant-after-N-operations**: `for any sequence of valid commands on aggregate, invariant still holds`
- **Model-based oracle**: compare real implementation to a simpler reference model — powerful for sprint analytics where a naive calculation serves as the oracle for an optimized one

**FsCheck 3.x (canonical .NET PBT).**
Version 3.x (June 2025): new API split — functional interface for F#, fluent interface for C#/VB. Install: `dotnet add package FsCheck.Xunit`. Attribute: `[Property]`. Shrinking is automatic — when a property fails, FsCheck iterates toward the minimal failing input. Custom `Arbitrary<T>` generators are required for domain types; the default generator will produce invalid domain objects. Supports state machine / command-based testing (EFSM model) — directly applicable to aggregate command sequences.
Source: https://fscheck.github.io/FsCheck/
Source: https://bartwullems.blogspot.com/2025/06/property-based-testing-updating-fscheck.html

**CsCheck (C#-first alternative).**
Idiomatic C# API, fast, no ceremony for generator definition, built-in model-based testing primitives, concurrency testing support. Less community-tested than FsCheck; fewer examples in AI training data. Preferable over FsCheck for pure C# shops that value ergonomics.
Source: https://bartwullems.blogspot.com/2024/02/property-based-testing-in-ccscheck.html

**IEEE paper: deriving FsCheck properties from business-rule models.**
Approach: parse business rule model (EFSM) → auto-generate FsCheck `ICommand` implementations → FsCheck generates random command sequences and verifies state postconditions. Directly applicable for sprint aggregate state machines (sprint open → synced → closed → archived transitions).
Source: https://ieeexplore.ieee.org/document/7528965/

**Custom shrinker requirement.**
Without a custom shrinker for domain types, failing test cases remain at their full generated complexity. Budget: define `Arbitrary<T>` + shrinker for all core domain value objects before deploying PBT.

**Mutation-PBT synergy.**
PBT properties encoded as business rules are excellent at killing boundary mutants (`>=` → `>`, `!= 0` → `== 0`) that example tests routinely miss. Validate PBT quality by running Stryker on PBT-covered code and verifying the property kills its target mutants.

---

### THREAD 6 — .NET Test Stack State-of-the-Art 2025/2026

**Source authority: official framework docs + community practitioner analysis**

**Microsoft.Testing.Platform (MTP).**
Lightweight alternative to VSTest. Embedded directly in test projects — no `vstest.console` or `dotnet test` host process required. 20M+ downloads. All major frameworks (xUnit.net, NUnit, MSTest, TUnit, Expecto) now support MTP.
Source: https://devblogs.microsoft.com/dotnet/mtp-adoption-frameworks/

**xUnit v3 (`xunit.v3` package).**
Architectural break from v2 — deliberately separate NuGet package. Test projects compile to standalone executables. New: assembly fixtures (shared setup without collection forcing). MTP support included. Per-test-class instance isolation is non-negotiable (no shared mutable state). Concern: 3 major versions in 6 months as of 2026 — cited as rapid churn.
Source: https://xunit.net/docs/getting-started/v3/microsoft-testing-platform

**TUnit.**
Built natively on MTP only — no VSTest fallback. Source-generated test discovery (compile-time, not reflection). Supports Native AOT. Parallelism model: `--maximum-parallel-tests N` controls actual concurrent tests (not thread count), correctly handling async yielding. FsCheck integration documented. Known gaps: (1) still in preview, APIs not marked stable; (2) Coverlet incompatible (code coverage collection requires alternative); (3) LLM tooling has less training data for TUnit than xUnit/NUnit.
Source: https://tunit.dev/docs/comparison/framework-differences/
Source: https://tunit.dev/docs/benchmarks/

**NUnit 4.x.**
Actively maintained. Shared fixture instance model (unlike xUnit). Granular `[Parallelizable]` control — best-in-class for surgical concurrency tuning. MTP support added.

**Verify (snapshot/approval testing).**
Simon Cropp. Works with xUnit, NUnit, MSTest, TUnit. Serialize domain objects to `.verified.json` files, committed to source control. Best used for integration-test assertions on complex aggregate outputs or API responses.
Source: https://github.com/verifytests/verify

**Pure domain test architecture (DDD consensus).**
- Domain layer tests: in-memory only, no EF Core, no HTTP. Instantiate aggregates directly, call domain methods, assert on domain events raised and state changes. No mocks except repository interfaces.
- Application layer tests: mock repositories and event buses, use real domain objects.
- Infrastructure tests: separate project, real SQLite, run in CI only — excluded from Stryker's mutation target set.
Source: https://www.jamesmichaelhickey.com/ddd-unit-tests/
Source: https://dev.to/ruben_alapont/testing-strategies-in-domain-driven-design-ddd-2d93

---

### TOP 5 DESIGN IMPLICATIONS

**1. The only valid test oracle is explicit intent, not observed behavior.**
All coverage-based tools (EvoSuite, TestGen-LLM, CoverAgent) generate regression oracles that encode bugs as expected behavior. The ACH paper proves 49% of mutation-killing tests add no line coverage. Design implication: the harness must encode rules explicitly as assertions or PBT properties — never derive expected values by running existing code and snapshotting results, except for the initial Verify-based bootstrap scaffold.

**2. Stryker.NET `--with-baseline` is the mandatory quality gate, not coverage.**
Mutation score replaces line/branch coverage as the acceptance criterion. A test that does not kill at least one mutant does not count toward the rule-coverage contract. Use `break-at 70` from day one; raise incrementally. Use `--with-baseline` in CI to make incremental mutation PR-viable from sprint one. Target `Fokus.Domain` and `Identity.Domain` projects only in the initial mutation scope; exclude Persistence and API.

**3. LLM generates drafts; mutation testing validates them.**
Adopt the ACH workflow adapted for .NET: (1) Run Stryker on the domain project to identify surviving mutants. (2) For each surviving mutant, prompt an LLM with the full class-under-test context (CUT prompt — highest accuracy per arXiv 2601.05542) to write a test that specifically kills that mutant. (3) Verify the mutation score increases. (4) Discard any LLM-generated test that does not improve mutation score. Never use coverage-gated LLM generation (CoverAgent pattern) — it discards the tests that matter.

**4. Property-based testing is the native language of DDD business rules.**
Each business invariant maps directly to an FsCheck property. Sprint velocity must be non-negative: `for all SprintVelocity v: v.Value >= 0`. Sprint lifecycle transitions encode as FsCheck command-based model tests against the aggregate. Write one `Arbitrary<T>` generator per value object — the generator is the authoritative definition of "valid domain input." PBT properties resist implementation drift better than example tests and naturally kill boundary mutants.

**5. Recommended .NET 10 test stack.**

| Layer | Tool | Rationale |
|---|---|---|
| Test framework | `xunit.v3` | Stable, isolated-by-default, MTP support, large ecosystem, best LLM training data |
| Test runner | Microsoft.Testing.Platform | Embedded in test project, no VSTest overhead |
| Property-based | `FsCheck.Xunit` v3 | Canonical .NET PBT, xUnit integration, EFSM state machine model |
| Snapshot/approval | `Verify.XUnit` | Bootstrap characterization, integration test assertions |
| Mutation gate | `dotnet-stryker` v4.14+ | Only mature .NET mutation tool; `--with-baseline` for CI delta; `break-at` as quality gate |
| Coverage (informational) | `Coverlet.Collector` | Line coverage secondary but useful for IDE tooling |
| Assertion library | `FluentAssertions` v7 | Readable failure messages, works alongside FsCheck test prefixes |

TUnit is not recommended at this time: preview APIs (unstable), Coverlet incompatibility, and thin LLM training data are all risks for an AI-assisted test-generation workflow. Re-evaluate at TUnit 1.0 stable.

================================================================================

The research is complete. Here is a condensed handoff for the architect:

---

## Research Summary: Reverse-Engineering .NET 10 DDD into a Mutation-Gated Test Suite

### Key Findings by Thread

**Thread 1 — Characterization / Approval Testing**
Verify (Simon Cropp) is the canonical .NET snapshot tool. Works with xUnit v3, NUnit, TUnit. Use it as a **bootstrap scaffold only** — Verify locks current behavior including bugs. It will score poorly against Stryker because it asserts whole-output, not individual rule violations. The human-approval workflow (`.received.*` → `.verified.*`) is the correct first-pass safety net before refactoring domain logic.

**Thread 2 — Pre-LLM Automated Generation**
No pre-LLM tool is viable for .NET 10. IntelliTest requires VS Enterprise and .NET Framework. EvoSuite and Diffblue are JVM-only. All three suffer from the same structural defect: regression-oracle generation — they encode bugs as expected behavior.

**Thread 3 — LLM Test Generation (most important thread)**
Two critical data points from 2024–2025 peer-reviewed research:

1. **CoverAgent / Qodo study (arXiv 2412.14137, Dec 2024):** Tools that filter out failing tests reject 400–470 bug-revealing tests per 287 samples. 59–68% of final test suites *validated* bugs rather than detecting them. The filtering design that makes LLM tests compile reliably is the exact mechanism that makes them useless for bug detection.

2. **Meta ACH (arXiv 2501.12862, FSE 2025):** Mutation-guided LLM generation achieves 32% mutant kill rate vs. 5.3% for coverage-guided TestGen-LLM. 49% of mutation-killing tests add zero line coverage — proving coverage-gated generation structurally cannot produce a mutation-proof suite. Engineer acceptance: 73%.

The correct workflow for this project: **Stryker identifies surviving mutants → LLM prompted with full class-under-test context writes tests targeting those mutants → Stryker confirms kill → test is accepted.** Never use coverage thresholds as the acceptance gate.

**Thread 4 — Stryker.NET**
Version 4.14.2 (May 2026), officially documented by Microsoft. .NET 10 compatibility implied by active maintenance (Roslyn compiler embedded, C# up to "Preview" — verify before starting). `--with-baseline` is the CI strategy: full run on `main` sets the baseline; PRs run incremental delta only. Default `break` threshold is 0 — must be set explicitly. Recommended initial gate: `break-at 70`, raise over time.

**Thread 5 — Property-Based Testing**
FsCheck 3.x (June 2025 release) is the canonical .NET PBT library with xUnit integration via `[Property]` attribute. Each DDD business invariant maps to a property. Critical requirement: write `Arbitrary<T>` generators for all domain value objects before using PBT — the default generator produces invalid domain objects. FsCheck command-based (EFSM) model testing is the right approach for sprint aggregate state machine transitions. CsCheck is a viable C#-first alternative with lower ceremony but thinner ecosystem.

**Thread 6 — .NET Test Stack**
Recommended stack for this project:

| Layer | Tool |
|---|---|
| Test framework | `xunit.v3` |
| Runner | Microsoft.Testing.Platform (MTP) |
| Property-based | `FsCheck.Xunit` v3 |
| Snapshot/approval | `Verify.XUnit` |
| Mutation gate | `dotnet-stryker` v4.14+ with `--with-baseline` |
| Coverage (informational) | `Coverlet.Collector` |
| Assertions | `FluentAssertions` v7 |

**TUnit is not recommended** despite being the fastest framework: it is in preview (unstable APIs), incompatible with Coverlet, and has thin LLM training data — all three are risks for an AI-assisted mutation-guided workflow. Reconsider at TUnit 1.0 stable.

---

### Top 5 Design Implications

1. **Oracle = explicit intent, never observed behavior.** Verify snapshots are a scaffold, not a destination. Every final-form test must encode a rule, not a behavioral fingerprint.

2. **Mutation score is the acceptance gate, not line coverage.** A test that survives Stryker does not count. Set `break-at` explicitly from day one.

3. **LLM workflow is ACH-pattern, not CoverAgent-pattern.** Stryker finds surviving mutants → LLM writes targeted killers with full CUT context → Stryker confirms the kill. Discard any LLM test that does not raise mutation score.

4. **PBT properties are the native form of DDD invariants.** One `Arbitrary<T>` per value object. One `[Property]` per business rule. FsCheck command sequences for aggregate state machines.

5. **Scope Stryker to domain projects only initially.** Target `Fokus.Domain` and `Identity.Domain`. Exclude Persistence, API, and Host from the mutation target set. Infrastructure tests run in CI only and are not mutation targets.

---

### Key Source URLs

- Meta ACH paper (FSE 2025): https://arxiv.org/abs/2501.12862
- LLM tools prevent bug finding (Dec 2024): https://arxiv.org/html/2412.14137v1
- LLM oracle accuracy study (Jan 2025): https://arxiv.org/html/2601.05542v1
- Stryker.NET GitHub: https://github.com/stryker-mutator/stryker-net
- Stryker.NET Microsoft Docs: https://learn.microsoft.com/en-us/dotnet/core/testing/mutation-testing
- Stryker.NET configuration (--since, --with-baseline, thresholds): https://stryker-mutator.io/docs/stryker-net/configuration/
- FsCheck official docs: https://fscheck.github.io/FsCheck/
- FsCheck v3.x update (June 2025): https://bartwullems.blogspot.com/2025/06/property-based-testing-updating-fscheck.html
- FsCheck properties from business rule models (IEEE): https://ieeexplore.ieee.org/document/7528965/
- Verify GitHub: https://github.com/verifytests/verify
- xUnit v3 + MTP: https://xunit.net/docs/getting-started/v3/microsoft-testing-platform
- MTP adoption across frameworks: https://devblogs.microsoft.com/dotnet/mtp-adoption-frameworks/
- DDD domain layer unit testing patterns: https://www.jamesmichaelhickey.com/ddd-unit-tests/

================================================================================

The research is complete. Here is the structured output for the architect.

---

## Research: Reverse-Engineering a .NET 10 DDD Codebase into an Automated Test Suite

---

### THREAD 1 — Characterization / Golden-Master / Approval Testing

**Source authority: established practice (2004 origin, continuously evolved)**

**Feathers' framing.**
Michael Feathers coined "characterization test" in *Working Effectively with Legacy Code* (2004). Definition: a test that describes the *actual, current* behavior of existing code — not its intended behavior. Goal: create a safety net before refactoring so regressions become visible. Method: run the code, observe output, encode that observation as an assertion. The test pins what the code does now.
Source: https://michaelfeathers.silvrback.com/characterization-testing

**Characterization vs. Approval vs. Golden-Master.**
These are synonyms with nuanced emphasis. "Approval" emphasizes that a human explicitly approved the snapshot. "Golden Master" comes from audio mastering — the one truth copy. "Characterization" emphasizes describing behavior as-found. All three use the same mechanics: capture output → store as baseline file → compare on future runs.
Source: https://understandlegacycode.com/blog/characterization-tests-or-approval-tests/

**Verify — the canonical .NET library.**
Author: Simon Cropp. Actively maintained. Supports xUnit, NUnit, MSTest, Expecto, TUnit. Package: `Verify.XUnit`. On first run, test fails and writes a `.received.*` file. Human reviews the diff, promotes to `.verified.*`. All future runs compare against the verified file. Handles complex domain objects (serializes to JSON by default), HTTP responses, HTML, images, source generators. Dynamic properties (GUIDs, timestamps) handled via `IgnoreMember` or `ScrubMember`.
Source: https://github.com/verifytests/verify
Source: https://blog.jetbrains.com/dotnet/2024/07/11/snapshot-testing-in-net-with-verify/ (JetBrains, July 2024)

**Critical limitation — the bug-cementing risk.**
Characterization tests cannot distinguish correct from incorrect behavior in the current version. If the code has a latent bug today, the snapshot captures the bug as the approved behavior. "Golden Master testing does not infer correctness of the results." For a business-rule-verifying test suite, characterization tests serve as a bootstrap scaffold only — they must be replaced or augmented with intent-encoding assertions.
Source: https://en.wikipedia.org/wiki/Characterization_test

**When characterization tests help vs. harm.**
- Help: (1) Safety net before refactoring when no other tests exist. (2) Complex output shapes (aggregated sprint reports) where exhaustive assertions are impractical. (3) First pass in a coverage-then-intention workflow.
- Harm: (1) Existing behavior is wrong — you lock in the bug. (2) Non-deterministic output requires masking so many fields the test loses value. (3) Used as final-form tests — mutation testing will score them poorly because they assert whole output, not individual rule violations.

---

### THREAD 2 — Automated Test Generation (Pre-LLM Tools)

**Source authority: established research + commercial tools**

**EvoSuite (JVM, search-based).**
Generates test suites by optimizing for code coverage using genetic algorithms. Core weakness: all generated oracles are regression oracles — they assert the current (possibly buggy) behavior. Cannot detect faults in the current version. Test cases are hard to read and interpret — low developer acceptance. No .NET port exists; relevant as conceptual precedent only.
Source: https://www.researchgate.net/publication/273188653_A_Large-Scale_Evaluation_of_Automated_Unit_Test_Generation_Using_EvoSuite

**Randoop (JVM, random feedback-based).**
Generates test sequences by random method chaining, pruning sequences that throw exceptions. Also regression-oracle only. JVM only. Not applicable to .NET.

**Pex / IntelliTest (.NET, dynamic symbolic execution).**
Pex (Microsoft Research, 2008) and its Visual Studio successor IntelliTest use dynamic symbolic execution to systematically explore branches and generate parameterized unit tests with high path coverage. Hard constraints: Visual Studio Enterprise only; supports C# targeting .NET Framework — not .NET Core or .NET 10. Generates test inputs, not test oracles — the developer still writes assertions. Effectively abandoned for modern .NET.
Source: https://link.springer.com/chapter/10.1007/978-3-540-79124-9_10
Source: https://github.com/MicrosoftDocs/visualstudio-docs/blob/main/docs/test/intellitest-manual/index.md

**Diffblue Cover (Java, RL-based).**
Reinforcement-learning approach. Java only. Claims 250 tests/minute and 20x productivity over LLM coding assistants (Diffblue-funded benchmark, November 2025 — vendor claim). No .NET support. Relevant as a design pattern reference only.
Source: https://www.businesswire.com/news/home/20251104720918/en/Diffblues-Latest-Innovations-in-Unit-Test-Generation-Deliver-20x-Productivity-Advantage-Versus-AI-Coding-Assistants

**Universal weakness — the regression oracle problem.**
All pre-LLM automated tools generate regression oracles. An oracle generated from code that contains a bug will encode the bug as expected behavior. This is structurally unsolvable within the coverage-maximization paradigm.

**Applicability to this project:** No pre-LLM automated test generation tool supports .NET 10. The correct approach is LLM-assisted draft generation + mutation testing as the quality gate.

---

### THREAD 3 — LLM-Based Test Generation

**Source authority: emerging research (2024–2025 peer-reviewed papers) + commercial products**

**Meta TestGen-LLM.**
Coverage-driven LLM test generation. Generates tests iteratively until coverage targets are met. Key weakness: by filtering out tests that fail on the current implementation, it systematically discards the tests most likely to reveal bugs. 49% of tests that uniquely kill a mutant do not add line coverage — so coverage-gated generation structurally misses them.
Source: referenced in https://arxiv.org/html/2412.14137v1

**Meta ACH — mutation-guided LLM test generation (FSE 2025, emerging research).**
ACH = Assured Coverage Hardening. Workflow: (1) Generate targeted mutants for a specific concern. (2) Filter equivalent mutants via an LLM judge agent (precision 0.95 / recall 0.96 with preprocessing). (3) Prompt LLM to generate tests that kill those specific mutants. Scale: 10,795 Android Kotlin classes across 7 Meta platforms, 9,095 mutants generated, 571 privacy-hardening tests produced. Engineer acceptance rate: 73%. 36% judged privacy-relevant. Mutation kill rate: ACH 32% vs. TestGen-LLM 5.3%. 49% of mutation-killing tests add zero line coverage — proving mutation-guidance is essential and coverage-guidance alone is insufficient.
Source: https://arxiv.org/abs/2501.12862
FSE 2025: https://dl.acm.org/doi/10.1145/3696630.3728544

**The oracle problem in LLM test generation (arXiv 2412.14137, December 2024).**
Study across 287 buggy code samples, three tools:

| Outcome | Copilot | CoverAgent (Qodo) | CoverUp |
|---|---|---|---|
| Bug-revealing tests kept | 194 | 470 rejected | 400 rejected |
| Final tests that validate the bug (not detect it) | 4.2% | 59.6% | 68.1% |
| Failed to produce any suite | 0 | 0 | 68.3% of cases |

Root cause: any filtering approach that discards tests failing on current code will systematically discard exactly the tests that matter. The paper's conclusion: "code should be written based on requirements, not the other way around."
Source: https://arxiv.org/html/2412.14137v1

**LLM oracle correctness measured (arXiv 2601.05542, January 2025).**
Best configuration (GPT-4o, full class-under-test context, zero-shot): 54.56% oracle accuracy, 67.38% compilation rate. Filtered to only compiling tests: 86.45% accuracy. Key finding: "prompting technique has stronger impact on oracle effectiveness than LLM choice" (25.30% vs. 6.68% variance by factor). Including the full class-under-test in context (CUT prompt) is the single most impactful variable.
Source: https://arxiv.org/html/2601.05542v1

**AugmenTest (arXiv 2501.17461, January 2025).**
Uses LLMs to augment existing test oracles (add assertions to tests that already have structure). Outperforms TOGA: 30% success rate vs. TOGA's 8.2% in conservative threshold scenario.
Source: https://arxiv.org/html/2501.17461v1

**Qodo / CodiumAI (commercial).**
CodiumAI rebranded to Qodo in 2024. Open-source CoverAgent inspired by Meta's TestGen-LLM. Same structural problem: coverage-first filtering discards bug-revealing tests. The December 2024 arXiv study covers their approach directly with the quantitative results above.

**Applicability to this project:** LLM generation is most valuable as a draft layer. The mutation-guided ACH pattern is directly adoptable adapted to .NET: generate Stryker.NET mutants for domain rules → prompt LLM with full class-under-test context to write tests that kill specific mutants → verify via Stryker that mutation score increases → discard any LLM test that does not improve mutation score. Never use coverage-only filtering.

---

### THREAD 4 — Mutation Testing as Test-Quality Oracle

**Source authority: established practice (PIT/JVM) + mature .NET tool (Stryker.NET)**

**The mutation testing model.**
Inject syntactic faults (mutants) → run test suite → count killed (test failed) vs. survived (test passed). Mutation score = killed / total. A suite with 100% line coverage but 40% mutation score is demonstrably inadequate. Mutation score is the most direct proxy for "tests will catch regressions."

**PIT (JVM) key lessons, transferable to Stryker.NET:**
- Start at 60–65% threshold; raise incrementally. Targeting 85% from day one causes build paralysis.
- Equivalent mutants: syntactically different but semantically identical mutations. They survive without indicating a test gap. LLM-based filtering (ACH approach) detects them at 0.95 precision.
- Incremental analysis: persist history across builds; re-test only changed code. Essential for CI viability.
- Killed mutants cluster in recently changed code — prioritize mutation testing of actively maintained domain logic first.
- Flaky tests wreak havoc: mutation reruns the same tests with modified code; non-deterministic tests produce false surviving mutants.
Source: https://pitest.org/
Source: https://www.javacodegeeks.com/2024/11/enhancing-java-testing-with-pit-a-guide-to-mutation-testing.html

**Stryker.NET — maturity and current state.**
Version 4.14.2 (May 2026). Officially documented by Microsoft at learn.microsoft.com — a strong maturity signal. Supports MSTest, xUnit, NUnit. .NET 10 / C# 13 compatibility: Stryker embeds its own Roslyn compiler; C# language support goes to "Preview" (relative to embedded Roslyn). .NET 10 compatibility is implied by active maintenance but must be verified before project start.
Source: https://github.com/stryker-mutator/stryker-net
Source: https://learn.microsoft.com/en-us/dotnet/core/testing/mutation-testing
Source: https://stryker-mutator.io/docs/stryker-net/configuration/

**Stryker.NET mutation level categories:**
- Basic: arithmetic operators, block statements, logical operators, bitwise operators
- Standard: equality, boolean literals, string literals, LINQ methods
- Advanced: regex, math methods, string methods
- Complete: all mutations

**Incremental mutation — `--since` and `--with-baseline`.**
`--since:<committish>`: run mutation tests only on code changed since a git reference. Reports only on mutants within changed code. Test file changes cause all mutants covered by those tests to be re-run.
`--with-baseline`: extends `--since` — saves full mutation report to disk / Dashboard / Azure / S3. On subsequent runs, loads baseline and only re-tests changed code. Mutually exclusive with explicit `--since`. Correct CI strategy: full mutation run on `main` sets baseline; PR runs use `--with-baseline` for fast delta reports.

**Threshold configuration (default: `{ high: 80, low: 60, break: 0 }`).**
`break` at 0 means the build never fails by default — must be set explicitly. Recommended initial gate: `break-at 70`, `low 75`, `high 85`. Raise break threshold over time as the suite matures.

---

### THREAD 5 — Property-Based Testing for .NET

**Source authority: established practice + active library development (FsCheck 3.x, June 2025)**

**The rule-to-property mapping.**
PBT inverts the example-test paradigm: instead of asserting `f(specific_input) == specific_output`, you assert `for all x in Domain: invariant(f(x)) == true`. Key property categories for DDD domains:
- **Round-trip**: `decode(encode(x)) == x` — for serialization, VO conversions
- **Idempotency**: `f(f(x)) == f(x)` — for normalization, state machines
- **Commutativity / Associativity**: for aggregation, scoring, metric rollups
- **Invariant-after-N-operations**: `for any sequence of valid commands on an aggregate, invariant still holds`
- **Model-based oracle**: compare real implementation against a simpler reference model — powerful for sprint analytics where a naive calculation serves as the oracle for an optimized one

**FsCheck 3.x — canonical .NET PBT.**
Version 3.x (June 2025): new API split — functional interface for F#, fluent interface for C#/VB. Install: `dotnet add package FsCheck.Xunit`. Attribute: `[Property]`. Shrinking automatic — when a property fails, FsCheck iterates toward the minimal failing input. Custom `Arbitrary<T>` generators are required for domain types; the default generator will produce invalid domain objects (e.g., negative sprint velocities, empty sprint keys). Supports state machine / command-based testing (EFSM model) — directly applicable to aggregate command sequences.
Source: https://fscheck.github.io/FsCheck/
Source: https://bartwullems.blogspot.com/2025/06/property-based-testing-updating-fscheck.html (June 2025)

**CsCheck — C#-first alternative.**
Idiomatic C# API, fast, lower ceremony for generator definition, built-in model-based testing primitives, concurrency testing support. Less community-tested than FsCheck; fewer examples in AI training data. Preferable for pure C# shops that value ergonomics over ecosystem depth.
Source: https://bartwullems.blogspot.com/2024/02/property-based-testing-in-ccscheck.html

**IEEE paper: deriving FsCheck properties from business-rule models.**
Approach: parse business rule model (EFSM) → auto-generate FsCheck `ICommand` implementations → FsCheck generates random command sequences and verifies state postconditions after each. Directly applicable for sprint aggregate state machine transitions (open → synced → closed → archived).
Source: https://ieeexplore.ieee.org/document/7528965/

**Custom shrinker requirement.**
Without a custom shrinker for domain types, failing test cases remain at their full generated complexity. Budget: define `Arbitrary<T>` + shrinker for all core domain value objects before deploying PBT.

**Mutation-PBT synergy.**
PBT properties encoded as business rules are excellent at killing boundary mutants (`>=` → `>`, `!= 0` → `== 0`) that example tests routinely miss. Validate PBT quality by running Stryker on PBT-covered code and verifying each property kills its target mutants.

---

### THREAD 6 — .NET Test Stack State-of-the-Art 2025/2026

**Source authority: official framework docs + community practitioner analysis**

**Microsoft.Testing.Platform (MTP).**
Lightweight alternative to VSTest. Embedded directly in test projects — no `vstest.console` or `dotnet test` host process required. 20M+ downloads. All major frameworks (xUnit.net, NUnit, MSTest, TUnit, Expecto) now support MTP. Correct runner choice for .NET 10 greenfield.
Source: https://devblogs.microsoft.com/dotnet/mtp-adoption-frameworks/

**xUnit v3 (`xunit.v3` package).**
Architectural break from v2 — deliberately separate NuGet package to prevent accidental upgrade. Test projects compile to standalone executables. New: assembly fixtures (shared setup without collection forcing). MTP support included. Per-test-class instance isolation is non-negotiable (no shared mutable state). Concern: 3 major versions in 6 months as of 2026 — some practitioners cite this as rapid churn for a test framework.
Source: https://xunit.net/docs/getting-started/v3/microsoft-testing-platform
Source: https://codingdroplets.com/xunit-vs-nunit-vs-mstest-in-net-which-testing-framework-should-your-team-use-in-2026

**TUnit.**
Built natively on MTP only — no VSTest fallback. Source-generated test discovery (compile-time, not reflection). Native AOT support. Parallelism model: `--maximum-parallel-tests N` controls actual concurrent test count (not thread count), correctly handling async yielding. FsCheck integration documented. Known gaps: (1) still in preview with unstable APIs; (2) Coverlet incompatible; (3) LLM tooling has less training data for TUnit than xUnit/NUnit.
Source: https://tunit.dev/docs/comparison/framework-differences/
Source: https://tunit.dev/docs/benchmarks/

**NUnit 4.x.**
Actively maintained. Shared fixture instance model (unlike xUnit). Granular `[Parallelizable]` control — best-in-class for surgical concurrency tuning. MTP support added.

**Verify — snapshot / approval testing.**
Simon Cropp. Works with xUnit, NUnit, MSTest, TUnit. Serialize domain objects to `.verified.json` files committed to source control. Best used for integration-test assertions on complex aggregate outputs or API responses. Packages: `Verify.XUnit`, `Verify.NUnit`, `Verify.TUnit`.
Source: https://github.com/verifytests/verify

**Pure domain test architecture — DDD consensus.**
- Domain layer tests: in-memory only, no EF Core, no HTTP, no file system. Instantiate aggregates directly, call domain methods, assert on domain events raised and state changes. Mock only repository interfaces.
- Application layer tests: mock repositories and event buses, use real domain objects.
- Infrastructure tests: separate project, real SQLite, run in CI only — excluded from Stryker's mutation target set.
Source: https://www.jamesmichaelhickey.com/ddd-unit-tests/
Source: https://dev.to/ruben_alapont/testing-strategies-in-domain-driven-design-ddd-2d93

---

### TOP 5 DESIGN IMPLICATIONS

**1. The only valid test oracle is explicit intent, not observed behavior.**
All coverage-based tools (EvoSuite, TestGen-LLM, CoverAgent) generate regression oracles that encode bugs as expected behavior. The ACH paper proves 49% of mutation-killing tests add zero line coverage. Design implication: the harness must encode rules explicitly as assertions or PBT properties — never derive expected values by running existing code and snapshotting results, except for the initial Verify-based bootstrap scaffold.

**2. Stryker.NET `--with-baseline` is the mandatory quality gate, not line coverage.**
Mutation score replaces line/branch coverage as the acceptance criterion. A test that does not kill at least one mutant does not count toward the rule-coverage contract. Set `break-at 70` from day one; raise incrementally. Use `--with-baseline` in CI to make incremental mutation PR-viable from sprint one. Target `Fokus.Domain` and `Identity.Domain` only in the initial mutation scope; exclude Persistence and API.

**3. LLM generates drafts; mutation testing validates them.**
Adopt the ACH workflow adapted for .NET: (1) Run Stryker on the domain project to identify surviving mutants. (2) For each surviving mutant, prompt an LLM with the full class-under-test context (CUT prompt — highest accuracy per arXiv 2601.05542) to write a test that specifically kills that mutant. (3) Verify the mutation score increases. (4) Discard any LLM-generated test that does not improve mutation score. Never use coverage-gated LLM generation — it discards the tests that matter.

**4. Property-based testing is the native language of DDD business rules.**
Each business invariant maps directly to a FsCheck property. Sprint velocity must be non-negative: `for all SprintVelocity v: v.Value >= 0`. Sprint lifecycle transitions encode as FsCheck command-based model tests against the aggregate. Write one `Arbitrary<T>` generator per value object — the generator is the authoritative definition of "valid domain input." PBT properties resist implementation drift and naturally kill boundary mutants.

**5. Recommended .NET 10 test stack.**

| Layer | Tool | Rationale |
|---|---|---|
| Test framework | `xunit.v3` | Stable, isolated-by-default, MTP support, largest ecosystem, best LLM training data |
| Test runner | Microsoft.Testing.Platform | Embedded in test project, no VSTest overhead |
| Property-based | `FsCheck.Xunit` v3 | Canonical .NET PBT, xUnit integration, EFSM state machine model |
| Snapshot / approval | `Verify.XUnit` | Bootstrap characterization, integration test assertions |
| Mutation gate | `dotnet-stryker` v4.14+ | Only mature .NET mutation tool; `--with-baseline` for CI delta; `break-at` as quality gate |
| Coverage (informational) | `Coverlet.Collector` | Line coverage secondary but useful for IDE tooling |
| Assertions | `FluentAssertions` v7 | Readable failure messages, works alongside FsCheck test prefixes |

**TUnit is not recommended for this project at this time.** Preview APIs (unstable), Coverlet incompatibility, and thin LLM training data are all risks for an AI-assisted mutation-guided workflow. Re-evaluate at TUnit 1.0 stable.

---

### All Sources

- https://michaelfeathers.silvrback.com/characterization-testing
- https://understandlegacycode.com/blog/characterization-tests-or-approval-tests/
- https://en.wikipedia.org/wiki/Characterization_test
- https://github.com/verifytests/verify
- https://blog.jetbrains.com/dotnet/2024/07/11/snapshot-testing-in-net-with-verify/
- https://antondevtips.com/blog/how-to-simplify-assertions-in-unit-and-integration-tests-with-verify-in-dotnet
- https://link.springer.com/chapter/10.1007/978-3-540-79124-9_10
- https://github.com/MicrosoftDocs/visualstudio-docs/blob/main/docs/test/intellitest-manual/index.md
- https://www.businesswire.com/news/home/20251104720918/en/Diffblues-Latest-Innovations-in-Unit-Test-Generation-Deliver-20x-Productivity-Advantage-Versus-AI-Coding-Assistants
- https://siliconangle.com/2024/10/30/diffblue-raises-6-3m-autonomous-ai-powered-test-generation-platform/
- https://arxiv.org/abs/2501.12862 (Meta ACH, FSE 2025)
- https://dl.acm.org/doi/10.1145/3696630.3728544 (ACH ACM proceedings)
- https://arxiv.org/html/2412.14137v1 (LLM tools prevent bug finding, Dec 2024)
- https://arxiv.org/html/2601.05542v1 (LLM oracle accuracy, Jan 2025)
- https://arxiv.org/html/2501.17461v1 (AugmenTest)
- https://pitest.org/
- https://www.javacodegeeks.com/2024/11/enhancing-java-testing-with-pit-a-guide-to-mutation-testing.html
- https://arxiv.org/pdf/1601.02351 (Assessing PIT)
- https://github.com/stryker-mutator/stryker-net
- https://learn.microsoft.com/en-us/dotnet/core/testing/mutation-testing
- https://stryker-mutator.io/docs/stryker-net/configuration/
- https://fscheck.github.io/FsCheck/
- https://bartwullems.blogspot.com/2025/06/property-based-testing-updating-fscheck.html
- https://bartwullems.blogspot.com/2024/02/property-based-testing-in-ccscheck.html
- https://developersvoice.com/blog/csharp/csharp-property-based-testing-fscheck-xunit/
- https://ieeexplore.ieee.org/document/7528965/
- https://tunit.dev/docs/comparison/framework-differences/
- https://tunit.dev/docs/benchmarks/
- https://xunit.net/docs/getting-started/v3/microsoft-testing-platform
- https://devblogs.microsoft.com/dotnet/mtp-adoption-frameworks/
- https://codingdroplets.com/xunit-vs-nunit-vs-mstest-in-net-which-testing-framework-should-your-team-use-in-2026
- https://www.jamesmichaelhickey.com/ddd-unit-tests/
- https://dev.to/ruben_alapont/testing-strategies-in-domain-driven-design-ddd-2d93

================================================================================

The research across all six threads is complete. The full structured output with sources is above — here is the executive digest for routing decisions:

---

**The single most important finding:** The December 2024 arXiv study (2412.14137) and the Meta ACH paper (2501.12862) together prove that coverage-gated LLM test generation is structurally self-defeating. CoverAgent rejected 470 bug-revealing tests per 287 samples; 59.6% of its final suite *validated* bugs rather than detecting them. The ACH mutation-guided approach achieves 32% mutant kill rate vs. 5.3% for coverage-guided generation. This is the architectural decision that flows through everything else in the harness design.

**Stack recommendation (firm):** `xunit.v3` + `FsCheck.Xunit` v3 + `Verify.XUnit` + `dotnet-stryker` v4.14+ with `--with-baseline`. TUnit excluded — preview APIs, Coverlet incompatibility, and thin LLM training data are all active risks for this specific workflow.

**One pre-start verification required:** Stryker.NET embeds its own Roslyn compiler and claims C# "Preview" support, but .NET 10 / C# 13 compatibility is implied rather than explicitly documented. Verify with a smoke test before committing the stack.

**The workflow loop in one sentence:** Stryker identifies surviving mutants → LLM (full CUT context, zero-shot) writes targeted killers → Stryker confirms the kill → test is accepted; anything that does not raise mutation score is discarded.

================================================================================

The research is complete and the full structured output is in the messages above. Here is the executive routing digest:

---

**Single most critical finding.** Two 2024–2025 peer-reviewed papers together invalidate coverage-gated LLM test generation as a strategy:

- arXiv 2412.14137 (Dec 2024): CoverAgent/Qodo rejected 470 bug-revealing tests per 287 samples. 59.6% of its final suite *validated* bugs rather than detecting them. The filtering mechanism that ensures tests compile is the exact mechanism that ensures they cannot reveal bugs.
- arXiv 2501.12862 / FSE 2025 (Meta ACH): Mutation-guided generation achieves 32% mutant kill rate vs. 5.3% coverage-guided. 49% of mutation-killing tests add zero line coverage — proving coverage-gating structurally cannot produce a mutation-proof suite.

**The workflow loop in one sentence.** Stryker identifies surviving mutants → LLM prompted with full class-under-test context (zero-shot, highest accuracy per arXiv 2601.05542) writes tests targeting those specific mutants → Stryker confirms kill → test accepted. Discard any LLM output that does not raise mutation score.

**Recommended stack (firm).**

| Layer | Tool |
|---|---|
| Framework | `xunit.v3` |
| Runner | Microsoft.Testing.Platform |
| PBT | `FsCheck.Xunit` v3 |
| Snapshot | `Verify.XUnit` |
| Mutation gate | `dotnet-stryker` v4.14+ (`--with-baseline`, `break-at 70`) |
| Coverage (informational) | `Coverlet.Collector` |
| Assertions | `FluentAssertions` v7 |

TUnit excluded: preview APIs (unstable), Coverlet incompatibility, and thin LLM training data are active risks for this specific AI-assisted mutation-guided workflow.

**One verification required before project start.** Stryker.NET embeds its own Roslyn compiler and claims C# "Preview" support, but .NET 10 / C# 13 compatibility is implied rather than explicitly documented. Run a smoke test against a `.Domain` project before committing the stack.

**Stryker scoping.** Target `Fokus.Domain` and `Identity.Domain` only initially. Exclude Persistence, API, and Host from the mutation target set. Set `break-at` explicitly — the default is 0, which means the build never fails on low mutation score.

================================================================================

## Research: Reverse-Engineering a .NET 10 DDD Codebase into an Automated Test Suite

---

### THREAD 1 — Characterization / Golden-Master / Approval Testing

**Classification: established practice (2004 origin, continuously evolved)**

**Feathers' framing.**
Michael Feathers coined "characterization test" in *Working Effectively with Legacy Code* (2004, Prentice Hall). Definition: a test that describes the *actual, current* behavior of existing code — not its intended behavior. Goal: create a safety net before refactoring so regressions become visible. Method: run the code, observe output, encode that observation as an assertion. The test pins what the code does now, not what it should do.
Source: [Characterization Testing — Michael Feathers (Silvrback)](https://michaelfeathers.silvrback.com/characterization-testing)

**Characterization vs. Approval vs. Golden-Master — they are synonyms.**
"Approval" emphasizes that a human explicitly approved the snapshot and can revoke that approval. "Golden Master" comes from audio mastering — the one truth copy from which all copies are made. "Characterization" emphasizes describing behavior as-found. All three use the same mechanics: capture output → store as baseline file → compare on future runs. No recognized standard for naming exists.
Source: [Characterization Tests or Approval Tests — UnderstandLegacyCode](https://understandlegacycode.com/blog/characterization-tests-or-approval-tests/)

**Verify — the canonical .NET approval/snapshot library.**
Author: Simon Cropp. Actively maintained. Supports xUnit, NUnit, MSTest, Expecto, TUnit. Package per framework: `Verify.XUnit`, `Verify.NUnit`, `Verify.TUnit`. On first run, test fails and writes a `.received.*` file. Human reviews the diff, promotes to `.verified.*`. All future runs compare against the verified file. Handles complex domain objects (serializes to JSON by default), HTTP responses, HTML, images, source generator output. Dynamic properties (GUIDs, timestamps, generated IDs) handled via `IgnoreMember` or `ScrubMember`. Snapshots are committed to source control — diffs are visible in PRs.
Source: [Verify — GitHub (Simon Cropp)](https://github.com/verifytests/verify)
Source: [Snapshot Testing in .NET with Verify — JetBrains (July 2024)](https://blog.jetbrains.com/dotnet/2024/07/11/snapshot-testing-in-net-with-verify/)
Source: [How To Simplify Assertions with Verify — antondevtips (January 2025)](https://antondevtips.com/blog/how-to-simplify-assertions-in-unit-and-integration-tests-with-verify-in-dotnet)

**Critical limitation — the bug-cementing risk.**
Characterization tests cannot distinguish correct from incorrect behavior in the current version. If the code has a latent bug today, the snapshot captures the bug as the approved behavior. "Golden Master testing does not infer correctness of the results." Mutation testing will score Verify-based tests poorly because they assert whole-output serializations rather than individual rule violations — a single mutant changing one field deep in a computation will be killed by the snapshot, but Stryker cannot attribute which rule the test is protecting.
Source: [Characterization test — Wikipedia](https://en.wikipedia.org/wiki/Characterization_test)

**When characterization tests help vs. harm.**
- Help: (1) Safety net before refactoring when zero other tests exist — applies directly to this project. (2) Complex output shapes (aggregated sprint reports, health metrics) where exhaustive assertions are impractical. (3) First pass in a coverage-then-intention workflow — bootstrap scaffolding before rule-encoding tests are written.
- Harm: (1) Existing behavior is wrong — you lock in the bug and every future assertion validates it. (2) Non-deterministic output (timestamps, generated IDs, random ordering) requires masking so many fields the test loses value. (3) Used as final-form tests — they are resistant to mutation-score improvement because they catch everything and nothing specifically.

**Applicability to this project:** Use Verify-based characterization tests as Pass 0 scaffolding only. Lock sprint computation outputs as a non-regression baseline while domain analysis proceeds. Replace individual snapshot assertions with rule-encoding PBT properties and Stryker-gated unit tests as each business rule is identified and verified.

---

### THREAD 2 — Automated Test Generation (Pre-LLM Tools)

**Classification: established research (EvoSuite, Randoop), effectively abandoned for .NET (IntelliTest), Java-only commercial (Diffblue)**

**EvoSuite (JVM, search-based genetic algorithms).**
Generates test suites by optimizing for code coverage using genetic algorithms. Proven effective for branch coverage. Core structural weakness: all generated oracles are regression oracles — they assert the current (possibly buggy) behavior, not intended behavior. Cannot detect faults in the current version. Secondary weakness: "test cases are hard to read and interpret" — developer acceptance is low because generated tests do not communicate intent. EvoSuite also generates assertions that just restate implementation state rather than encoding business meaning. No .NET port exists; relevant to this project as conceptual precedent for the oracle problem only.
Source: [A Large-Scale Evaluation of Automated Unit Test Generation Using EvoSuite — ResearchGate](https://www.researchgate.net/publication/273188653_A_Large-Scale_Evaluation_of_Automated_Unit_Test_Generation_Using_EvoSuite)

**Randoop (JVM, random feedback-based).**
Generates test sequences by random method chaining, pruning sequences that throw exceptions. Also regression-oracle only. JVM only. Not applicable to .NET.

**Pex / IntelliTest (.NET, dynamic symbolic execution).**
Pex (Microsoft Research, 2008) and its Visual Studio successor IntelliTest use dynamic symbolic execution (DSE) to systematically explore branches and generate parameterized unit tests achieving high path coverage. Hard constraints that make it unusable for this project: Visual Studio Enterprise edition only; supports C# targeting .NET Framework — not .NET Core or .NET 10. IntelliTest generates test inputs (parameterized stubs), not test oracles — the developer still writes all assertions. The constraint solver uses randomized algorithms making generated test data not always reproducible. Newer strategies achieve 100% branch coverage 50x faster than IntelliTest. Effectively abandoned for modern .NET development.
Source: [Pex — White Box Test Generation for .NET — Springer](https://link.springer.com/chapter/10.1007/978-3-540-79124-9_10)
Source: [IntelliTest Manual — Microsoft Docs GitHub](https://github.com/MicrosoftDocs/visualstudio-docs/blob/main/docs/test/intellitest-manual/index.md)

**Diffblue Cover (Java, reinforcement-learning-based).**
Reinforcement-learning approach to automated unit test generation for Java. Claims 250 tests/minute and 20x productivity over LLM coding assistants per a Diffblue-funded benchmark (November 2025 — vendor claim, treat as marketing). Raises coverage by 50% beyond baseline in one hour (also vendor claim). Java only — no .NET support. Total funding to date: $46M+. Relevant to this project as a design pattern reference (RL for test generation + the "Test Asset Insights" concept of analyzing existing coverage gaps), not as a usable tool.
Source: [Diffblue Cover 20x Productivity Claim — BusinessWire (November 2025)](https://www.businesswire.com/news/home/20251104720918/en/Diffblues-Latest-Innovations-in-Unit-Test-Generation-Deliver-20x-Productivity-Advantage-Versus-AI-Coding-Assistants)
Source: [Diffblue $6.3M Funding — SiliconANGLE (October 2024)](https://siliconangle.com/2024/10/30/diffblue-raises-6-3m-autonomous-ai-powered-test-generation-platform/)

**Universal weakness — the regression oracle problem.**
All pre-LLM automated tools generate regression oracles. An oracle generated from code that contains a bug will encode the bug as the expected behavior. This is structurally unsolvable within the coverage-maximization paradigm. The only solution is external intent specification: requirements documents, formal specs, domain expert interviews, or PBT properties derived from business rules.

**Applicability to this project:** No pre-LLM automated test generation tool supports .NET 10 with usable oracle generation. The correct approach for this project is LLM-assisted draft generation + mutation testing as the quality gate, with PBT for encoding invariants.

---

### THREAD 3 — LLM-Based Test Generation

**Classification: emerging research (2024–2025 peer-reviewed papers) + commercial products (marketing claims unverified)**

**Meta TestGen-LLM (predecessor to ACH).**
Coverage-driven LLM test generation. Generates tests iteratively until coverage targets are met, discarding tests that fail on the current implementation. Key weakness exposed by December 2024 arXiv study: by filtering out tests that fail on current implementation, it systematically discards the tests most likely to reveal bugs. 49% of tests that uniquely kill a mutant do not add line coverage at all — so coverage-gated generation structurally misses the tests that matter most.
Source: referenced and analyzed in [arXiv 2412.14137 (December 2024)](https://arxiv.org/html/2412.14137v1)

**Meta ACH — mutation-guided LLM test generation (emerging research, FSE 2025).**
ACH = Assured Coverage Hardening. Published at the 33rd ACM International Conference on the Foundations of Software Engineering, 2025. Workflow: (1) Generate targeted mutants for a specific concern (privacy hardening in their case). (2) Filter equivalent mutants via an LLM judge agent — precision 0.79 / recall 0.47 raw; precision 0.95 / recall 0.96 with simple preprocessing. (3) Prompt LLM to generate tests specifically designed to kill those mutants. Scale of evaluation: 10,795 Android Kotlin classes across 7 Meta software platforms. Generated 9,095 mutants, produced 571 privacy-hardening test cases. Engineer acceptance rate: 73%. Of accepted tests, 36% judged privacy-relevant. Mutation kill rate: ACH 32% vs. TestGen-LLM 5.3%. Tests that uniquely kill mutants without adding any line coverage: 49% — this is the definitive proof that coverage-guidance is insufficient and mutation-guidance is necessary.
Source: [arXiv 2501.12862 — Mutation-Guided LLM-based Test Generation at Meta](https://arxiv.org/abs/2501.12862)
Source: [FSE 2025 Proceedings — ACM Digital Library](https://dl.acm.org/doi/10.1145/3696630.3728544)

**The oracle problem in LLM test generation — quantified (arXiv 2412.14137, December 2024).**
Study examined 287 buggy code samples across three tools: GitHub Copilot (no execution-based filtering), Codium CoverAgent (coverage-driven iterative generation, discards failing tests), CoverUp (agent-style iterative repair of failing tests).

| Outcome | Copilot | CoverAgent (Qodo) | CoverUp |
|---|---|---|---|
| Bug-revealing tests kept in final suite | 194 | 470 rejected | 400 rejected |
| Final tests that validate the bug rather than detecting it | 4.2% | 59.6% | 68.1% |
| Cases where no suite was produced at all | 0 | 0 | 68.3% |

Root cause: any filtering approach that discards tests failing on the current implementation will systematically discard exactly the tests that reveal bugs — because bug-revealing tests, by definition, fail on buggy code. Real-world validation: the study shows the Django `timesince()` timezone bug and the SymPy Morse code encoding bug (unfixed for 3+ years) were masked by both CoverAgent and CoverUp because their filtering eliminated the only tests that would have caught them. Paper's conclusion: "code should be written based on requirements, not the other way around."
Source: [Design choices made by LLM-based test generators prevent them from finding bugs — arXiv 2412.14137](https://arxiv.org/html/2412.14137v1)

**LLM oracle correctness measured — systematic evaluation (arXiv 2601.05542, January 2025).**
Systematic evaluation of prompting strategies for LLM-generated test oracles. Key results:

| Configuration | Oracle Accuracy | Compilation Rate |
|---|---|---|
| Test prefix only | 40.74% | 49.36% |
| Prefix + method under test | 40.38% | 59.23% |
| Prefix + full class under test (CUT) | **53.64%** | **76.26%** |
| Zero-shot | **54.56%** | 67.38% |
| Few-shot | 51.30% | 72.96% |
| Chain-of-Thought (CoT) | 31.11% | 44.44% |
| Tree-of-Thought (ToT) | 29.26% | 44.81% |

When filtered to only compiling oracles: GPT-4o achieves 86.45% accuracy in zero/few-shot scenarios. Key finding: "prompting technique has stronger impact on oracle effectiveness than LLM choice" — prompting accounts for 25.30% variance vs. 6.68% for model choice. Including the full class-under-test in context (CUT prompt) is the single most impactful variable. CoT and ToT achieve 65–70% accuracy when they compile but compilation rate drops to 44% — the reasoning depth is undermined by generation unreliability.
Source: [Understanding LLM-Driven Test Oracle Generation — arXiv 2601.05542](https://arxiv.org/html/2601.05542v1)

**AugmenTest — LLM oracle augmentation (arXiv 2501.17461, January 2025, emerging research).**
Proposes using LLMs specifically to augment existing test oracles — adding assertions to tests that already have structure (a test prefix exists) rather than generating whole tests. Outperforms TOGA (prior state-of-art oracle generation tool): 30% success rate vs. TOGA's 8.2% in the conservative threshold scenario.
Source: [AugmenTest: Enhancing Tests with LLM-Driven Oracles — arXiv 2501.17461](https://arxiv.org/html/2501.17461v1)

**Qodo / CodiumAI (commercial product).**
CodiumAI rebranded to Qodo in 2024. Open-source CoverAgent is inspired by Meta's TestGen-LLM and adopts a coverage-driven generation and filtering approach. The December 2024 arXiv study (2412.14137) directly analyzed their approach and produced the quantitative results above (470 bug-revealing tests rejected, 59.6% of final suite validating bugs). No published peer-reviewed mutation kill rate data for Qodo's commercial product. Treat product marketing claims without peer-reviewed benchmarks as unverified.
Source: [Qodo AI Test Generation Overview — DEV Community](https://dev.to/rahulxsingh/qodo-ai-test-generation-how-it-works-with-examples-4abk)

**Applicability to this project:** LLM generation is most valuable as a draft layer for initial test skeleton creation — not as a quality-complete approach. The mutation-guided ACH pattern is directly adoptable for .NET: generate Stryker.NET mutants for domain rules → prompt LLM with full class-under-test context (zero-shot, highest accuracy) to write tests that kill specific mutants → verify via Stryker that mutation score increases → accept the test. Discard any LLM-generated test that does not improve mutation score. Never use coverage-only filtering (CoverAgent pattern) — it discards the tests that actually prove rules.

---

### THREAD 4 — Mutation Testing as Test-Quality Oracle

**Classification: established practice (PIT/JVM two decades of use) + mature .NET tool (Stryker.NET)**

**The mutation testing model.**
Inject syntactic faults (mutants) into production code one at a time → run the test suite → count killed (at least one test failed) vs. survived (all tests passed despite the fault). Mutation score = killed / total. A test suite that achieves 100% line coverage but 40% mutation score is demonstrably inadequate — the tests exist but do not exercise the logic they appear to cover. Mutation score is the most direct proxy available for "this test suite will catch regressions when business rules are violated." This is the property that makes it the correct quality gate for a rule-coverage requirement.

**PIT (JVM) — key lessons transferable to Stryker.NET.**
PIT is the JVM reference implementation of practical mutation testing, in production use for over a decade. Lessons with direct applicability:
- Start at 60–65% break threshold; raise incrementally. Targeting 85% from day one on a greenfield-from-legacy codebase causes build paralysis — too many surviving mutants are equivalent mutants or in untargeted code paths.
- Equivalent mutants: mutations that are syntactically different but semantically identical (unreachable code, pure constants, dead assignments). They survive without indicating any test gap. LLM-based filtering (the ACH approach) achieves 0.95 precision / 0.96 recall for detecting them with simple preprocessing.
- Incremental analysis (`withHistory` in PIT, `--with-baseline` in Stryker): persist mutation results across builds; re-test only changed code. Without this, mutation testing is infeasible in CI pipelines for any non-trivial codebase.
- Killed mutants cluster in recently changed and introduced code — prioritize mutation testing of actively maintained domain logic first, not legacy peripheral code.
- Flaky tests wreak havoc: mutation reruns the same test suite multiple times with modified production code; a test that occasionally fails non-deterministically produces false surviving mutants and corrupts the mutation score.
- Scoped mutation (specific packages, specific files, specific mutation operators): use scoping to control runtime cost when the full mutation run is too slow for a PR gate.
Source: [PIT Mutation Testing — pitest.org](https://pitest.org/)
Source: [Enhancing Java Testing with PIT — JavaCodeGeeks (November 2024)](https://www.javacodegeeks.com/2024/11/enhancing-java-testing-with-pit-a-guide-to-mutation-testing.html)
Source: [Assessing and Improving the Mutation Testing Practice of PIT — arXiv 1601.02351](https://arxiv.org/pdf/1601.02351)

**Stryker.NET — maturity, version, and .NET 10 status.**
Current version: 4.14.2 (released May 2026). Active development: 2,107 commits, 147 open issues (June 2026). Officially documented by Microsoft at learn.microsoft.com/en-us/dotnet/core/testing/mutation-testing — a strong maturity signal indicating Microsoft considers it the canonical .NET mutation testing tool. Supported test frameworks: MSTest, xUnit, NUnit. TUnit support: not explicitly confirmed in current docs but Stryker uses the test framework's runner natively and TUnit's MTP integration should be compatible — verify before committing. .NET 10 / C# 13 compatibility: Stryker embeds its own Roslyn compiler; C# language support goes up to "Preview" (relative to the embedded Roslyn version, not the installed SDK). .NET 10 compatibility is implied by continuous active maintenance and "Preview" language support but is not explicitly documented — run a smoke test against a `.Domain` project before finalizing the stack.
Source: [Stryker.NET — GitHub](https://github.com/stryker-mutator/stryker-net)
Source: [Mutation testing — .NET | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/testing/mutation-testing)
Source: [Stryker.NET Configuration Docs — stryker-mutator.io](https://stryker-mutator.io/docs/stryker-net/configuration/)
Source: [NuGet Gallery — dotnet-stryker 4.14.2](https://www.nuget.org/packages/dotnet-stryker)

**Stryker.NET mutation level categories (cumulative):**
- Basic: arithmetic operators, block statements, logical operators, bitwise operators
- Standard: equality operators, boolean literals, string literals, LINQ methods
- Advanced: regex, math methods, string methods
- Complete: all possible mutations

**Incremental mutation — `--since` and `--with-baseline` (critical for CI viability).**
`--since:<committish>`: run mutation tests only on code changed since a git reference point. Reports only on mutants within changed code. Test file changes cause all mutants covered by those tests to be treated as changed and re-run.
`--with-baseline`: extends `--since` — saves the full mutation report to a storage location (options: disk `StrykerOutput` folder, Stryker Dashboard, Azure File Storage, any S3-compatible store). On subsequent runs, loads the baseline and only re-tests changed code; unchanged mutant results are reused from the stored report. Mutually exclusive with explicit `--since`. Correct CI strategy for this project: full mutation run on `main` branch sets the baseline; PR runs use `--with-baseline` for fast delta-only reports showing only what the PR changes.

**Threshold configuration (default: `{ high: 80, low: 60, break: 0 }`).**
The `break` default of 0 means the build never fails on low mutation score unless explicitly configured. For a rule-coverage quality gate: set `break-at 70` from day one, `low 75`, `high 85`. Raise the break threshold incrementally as the suite matures. The `break-at` value must be less than or equal to `threshold-low`.

**Performance considerations.**
No published large-.NET-project benchmark data in current search results. Mutation testing execution time = test suite execution time × mutant count. Mitigations in order of effectiveness: (1) `--with-baseline` to skip re-testing unchanged code; (2) `--since` for PR scope limiting; (3) mutation level set to `Standard` or `Basic` rather than `Complete` initially; (4) exclude Persistence, API, and Host from mutation targets.

---

### THREAD 5 — Property-Based Testing for .NET

**Classification: established practice with active library development (FsCheck 3.x June 2025, CsCheck actively maintained)**

**The rule-to-property mapping — why PBT is the native language of DDD invariants.**
PBT inverts the example-test paradigm. Instead of asserting `f(specific_input) == specific_output`, you assert `for all x in Domain: invariant(f(x)) == true`. This directly encodes business rules rather than behavioral observations. A property is the formal statement of a business rule. The generator is the formal definition of valid domain input. Together they constitute executable, mutation-resistant documentation of domain logic. Key property categories applicable to DDD domains:
- **Round-trip**: `decode(encode(x)) == x` — for serialization, value object conversions, sprint data import/export
- **Idempotency**: `f(f(x)) == f(x)` — for normalization operations, state machine transitions that have already occurred
- **Commutativity / Associativity**: for metric aggregation, scoring rollups across sprints
- **Invariant-after-N-operations**: `for any sequence of valid commands applied to an aggregate, the invariant still holds` — the core pattern for sprint aggregate testing
- **Model-based oracle**: compare real implementation against a simpler reference model — extremely powerful for sprint analytics where a naive O(n²) calculation can serve as the oracle for an optimized O(n) implementation

**FsCheck 3.x — canonical .NET PBT library.**
Version 3.x released June 2025. API change from 2.x: now split into a functional interface (F# consumers) and a fluent interface (C#/VB.NET consumers) to improve C# ergonomics that were awkward in v2. Install: `dotnet add package FsCheck.Xunit`. Test attribute: `[Property]` (replaces `[Fact]` on PBT methods). Shrinking is automatic and is the killer feature: when a property fails, FsCheck systematically tries simpler variants of the failing input (smaller numbers, shorter strings, fewer operations) and reports the minimal failing case — frequently surfacing a bug in two lines. Custom `Arbitrary<T>` generators are required for domain types; the default generator will produce invalid domain objects (negative sprint velocities, empty sprint keys, impossible date ranges). Each `Arbitrary<T>` provides both a generator and a shrinker — both must be domain-aware. Supports state machine / command-based testing (Extended Finite State Machine model) directly: define `ICommand` implementations per domain operation, FsCheck generates random command sequences and verifies postconditions after each transition.
Source: [FsCheck Official Docs](https://fscheck.github.io/FsCheck/)
Source: [Updating FsCheck to v3.x — Bart Wullems (June 2025)](https://bartwullems.blogspot.com/2025/06/property-based-testing-updating-fscheck.html)
Source: [Property-Based Testing in C#: FsCheck + xUnit — DevelopersVoice (November 2025)](https://developersvoice.com/blog/csharp/csharp-property-based-testing-fscheck-xunit/)

**CsCheck — C#-first alternative.**
Designed specifically for C# by Anthony Lloyd. Idiomatic C# API with lower ceremony for generator definition compared to FsCheck. Built-in model-based testing primitives. Built-in concurrency testing support (specifies operations that run concurrently then compares with linearized execution — directly applicable to sprint sync races). No specific xUnit/NUnit attribute integration — works with any framework via lambda. Less community-tested than FsCheck; fewer examples in AI training data (relevant risk for an AI-assisted workflow). Preferable over FsCheck for pure C# shops that value ergonomics over ecosystem depth.
Source: [Property-based testing in C# with CsCheck — Bart Wullems (February 2024)](https://bartwullems.blogspot.com/2024/02/property-based-testing-in-ccscheck.html)

**TUnit + FsCheck integration.**
TUnit has documented FsCheck integration — confirmed compatible. This removes TUnit's only potential PBT incompatibility concern.
Source: [FsCheck examples — TUnit docs](https://tunit.dev/docs/examples/fscheck/)

**IEEE academic paper: deriving FsCheck properties from business-rule models (established research).**
Approach: parse a formal business-rule model (EFSM — Extended Finite State Machine) → auto-generate FsCheck `ICommand` implementations representing domain operations → FsCheck generates random command sequences and verifies state postconditions after each transition. The model owns the state contract; FsCheck owns the exploration. Directly applicable pattern for sprint aggregate state machines (sprint states: open → synced → closed → archived) — define the EFSM per aggregate, derive the FsCheck command suite from it.
Source: [Property-Based Testing with FsCheck by Deriving Properties from Business Rule Models — IEEE Xplore](https://ieeexplore.ieee.org/document/7528965/)
Source: [Same paper — Springer SoSyM journal](https://link.springer.com/article/10.1007/s10270-017-0647-0)

**Custom shrinker requirement — non-negotiable.**
Without a custom shrinker for domain types, failing test cases remain at their full generated complexity. A failing `Sprint` object with 150 issues, 14 metrics, and 6 participants does not help diagnose the bug. The shrinker needs domain knowledge — for `SprintVelocity`, simpler means smaller positive integers, not smaller byte sequences. Budget: define `Arbitrary<T>` + shrinker for every core domain value object before deploying PBT. This is where most PBT projects underinvest and then abandon the approach as "impractical."

**Mutation-PBT synergy.**
PBT properties encoded as business rules are excellent at killing boundary mutants (`>=` → `>`, `!= 0` → `== 0`, `&&` → `||`) that example tests routinely miss because example tests use specific inputs that may not exercise the boundary. Validate PBT quality by running Stryker on PBT-covered code and verifying the property kills its target mutant class. A property that fails to improve mutation score on its target domain concept needs either a stronger invariant or a better generator.

---

### THREAD 6 — .NET Test Stack State-of-the-Art 2025/2026

**Classification: established frameworks (xUnit, NUnit), emerging framework (TUnit), official Microsoft platform (MTP)**

**Microsoft.Testing.Platform (MTP).**
Lightweight alternative to VSTest. Embedded directly in test projects as a NuGet dependency — no external `vstest.console` or `dotnet test` runner process required. 20 million+ downloads. All major .NET test frameworks now support MTP: xUnit.net v3, NUnit 4.x, MSTest V3, TUnit, Expecto. For .NET 10 greenfield: MTP is the correct runner choice — it is the direction Microsoft is pushing and will outlast VSTest.
Source: [Microsoft.Testing.Platform: Now Supported by All Major .NET Test Frameworks — .NET Blog](https://devblogs.microsoft.com/dotnet/mtp-adoption-frameworks/)

**xUnit v3 (`xunit.v3` package) — established framework, new architecture.**
Architectural break from v2 — deliberately uses a new NuGet package name (`xunit.v3` not `xunit`) to prevent accidental upgrade. Test projects compile to standalone executables (in v2, test project compiled to a DLL requiring an external runner to load it). New capability: assembly fixtures — shared setup that runs once per test assembly without forcing all classes into a shared collection, resolving the main pain point for integration tests. MTP support included. Per-test-class instance isolation is non-negotiable by design (no shared mutable state between tests — new instance per test method). This isolation prevents entire categories of test ordering bugs. Known concern: 3 major versions in 6 months as of 2026 — some practitioners cite this as rapid churn for a test framework, though the architecture itself is sound.
Source: [Microsoft Testing Platform (xUnit.net v3) — xunit.net](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform)
Source: [xUnit vs NUnit vs MSTest in .NET: 2026 Comparison — CodingDroplets](https://codingdroplets.com/xunit-vs-nunit-vs-mstest-in-net-which-testing-framework-should-your-team-use-in-2026)

**TUnit — emerging framework, native MTP, source-generated.**
Built natively on MTP only — no VSTest fallback path. Source-generated test discovery: tests are discovered at compile time via source generators rather than runtime reflection, reducing startup overhead and enabling Native AOT and single-file application support (features NUnit and xUnit do not currently offer). Parallelism model: `--maximum-parallel-tests N` controls actual concurrent test count (not OS thread count), correctly handling async yielding — a test that `await`s does not block the slot. Rich lifecycle hooks at every level: test, class, assembly, test session, and test discovery. Single `[Test]` attribute for all test methods. FsCheck integration confirmed working. Known gaps as of June 2026: (1) still in preview with APIs not marked stable — the maintainer explicitly notes this; (2) incompatible with Coverlet (the dominant .NET code coverage collection tool) — no workaround documented; (3) LLM tooling has significantly less training data for TUnit than xUnit or NUnit, increasing hallucination risk in AI-assisted test generation workflows.
Source: [TUnit Framework Differences — tunit.dev](https://tunit.dev/docs/comparison/framework-differences/)
Source: [TUnit Performance Benchmarks — tunit.dev](https://tunit.dev/docs/benchmarks/)
Source: [Converting an xUnit test project to TUnit — Andrew Lock](https://andrewlock.net/converting-an-xunit-project-to-tunit/)
Source: [TUnit vs xUnit and NUnit — Panu Oksala (October 2025)](https://oksala.net/2025/10/27/unit-testing-with-tunit/)

**NUnit 4.x — established framework, actively maintained.**
Actively maintained. Shared fixture instance model (unlike xUnit — NUnit reuses a single fixture instance across all tests in a class). Granular `[Parallelizable]` control — best-in-class for surgical concurrency tuning without restructuring test organization. MTP support added. `[CancelAfter]` for test timeouts, improved async test support in NUnit 4. Correct choice if the team has deep NUnit expertise or the codebase is already NUnit-based.
Source: [xUnit vs NUnit vs MSTest — TatvaSoft (June 2025)](https://www.tatvasoft.com/outsourcing/2025/06/xunit-vs-nunit-vs-mstest.html)

**Verify — snapshot/approval testing.**
Simon Cropp. Works with xUnit, NUnit, MSTest, TUnit. Serialize domain objects to `.verified.json` files committed to source control. Best applied for integration-test assertions on complex aggregate outputs, API response shapes, and computed report structures. In the context of this project: use for bootstrap characterization of sprint computation output, and for integration test assertions on the full sprint sync pipeline output.

**Pure domain test architecture — DDD .NET consensus.**
- Domain layer tests: in-memory only, zero EF Core, zero HTTP, zero file system. Instantiate aggregates directly, call domain methods, assert on domain events raised and on state changes observable through the aggregate's public API. Mock only repository interfaces that domain methods accept as parameters. These tests run in milliseconds — they are the primary Stryker target.
- Application layer tests: mock repositories and event buses with test doubles, use real domain objects (not mocked). Verify command/query handler behavior.
- Infrastructure tests: separate test project, real SQLite databases, run in CI only — never in the local developer mutation testing loop. Excluded from Stryker's mutation target set entirely.
Source: [DDD Unit Tests — James Hickey](https://www.jamesmichaelhickey.com/ddd-unit-tests/)
Source: [Testing Strategies in Domain-Driven Design — DEV Community](https://dev.to/ruben_alapont/testing-strategies-in-domain-driven-design-ddd-2d93)
Source: [DDD Testing Strategy: Aggregates and Domain Services — Zenn](https://zenn.dev/135yshr/articles/78e3eb0a2a275a?locale=en)

---

### TOP 5 DESIGN IMPLICATIONS

**1. The only valid test oracle is explicit intent, not observed behavior.**

All coverage-based tools — EvoSuite, TestGen-LLM, CoverAgent — generate regression oracles that encode bugs as expected behavior. The ACH paper (arXiv 2501.12862) proves that 49% of mutation-killing tests add zero line coverage. The arXiv 2412.14137 study proves that coverage-filtering LLM tools produce suites where 59–68% of final tests validate bugs rather than detect them. The design implication is non-negotiable: the harness must encode rules explicitly as assertions or PBT properties. Never derive expected values by running the existing code and snapshotting results — except for the initial Verify-based bootstrap scaffold which is explicitly time-limited. Every test that enters the permanent suite must have a human-readable statement of which business rule it encodes.

**2. Stryker.NET `--with-baseline` is the mandatory quality gate, not line coverage.**

Mutation score replaces line/branch coverage as the acceptance criterion for this project. A test that does not kill at least one mutant does not count toward the rule-coverage contract, regardless of its line coverage contribution. Set `break-at 70` from day one — the default of 0 means the build never fails. Raise incrementally: `break-at 70` → `75` → `80` as the suite matures. Use `--with-baseline` in CI so every PR run only re-tests changed code — this makes mutation testing viable from the first sprint without multi-hour builds. Target `Fokus.Domain` and `Identity.Domain` projects only in the initial mutation scope. Exclude Persistence, API, and Host from the mutation target set until domain coverage is solid.

**3. LLM generates drafts; mutation testing validates and gates them.**

Adopt the ACH workflow adapted to .NET. The loop is: (1) Run `dotnet stryker` on the target domain project with `--with-baseline` to identify surviving mutants. (2) For each surviving mutant cluster (group by mutation type and code location), prompt an LLM with the full class-under-test context (CUT prompt — highest oracle accuracy per arXiv 2601.05542, zero-shot configuration) and ask it to write a test that specifically kills those mutants. Include the mutant description in the prompt. (3) Run Stryker again and verify the mutation score increases. (4) If the score increases: accept the test, commit it. (5) If the score does not increase: discard the test regardless of whether it compiles or passes. Never use coverage increase as the acceptance criterion for LLM-generated tests — the ACH paper proves this selects for the wrong tests.

**4. Property-based testing is the native language of DDD business rules.**

Each identified business invariant maps directly to a FsCheck property. Examples for this codebase: sprint velocity is always non-negative (`for all SprintVelocity v: v.Value >= 0`); health score is always in [0, 100]; adding issues to a sprint never decreases its issue count; a closed sprint cannot have its dates extended; `Person.Email` round-trips through normalization unchanged. Write one `Arbitrary<T>` generator per domain value object — the generator is the formal, executable definition of "valid domain input." The generator doubles as documentation. Use FsCheck command-based (EFSM) model testing for sprint aggregate state machine transitions — define commands (OpenSprint, SyncSprint, CloseSprint) as `ICommand` implementations, let FsCheck explore arbitrary sequences, verify postconditions hold after each. PBT properties that encode real business rules are excellent at killing boundary mutants that example tests routinely miss.

**5. Recommended .NET 10 test stack for this project.**

| Layer | Tool | Rationale |
|---|---|---|
| Test framework | `xunit.v3` (`xunit.v3` NuGet) | Stable, isolated-by-default, MTP support, largest ecosystem, best LLM training data for AI-assisted generation |
| Test runner | Microsoft.Testing.Platform | Embedded in test projects, no VSTest overhead, the forward-direction Microsoft platform |
| Property-based testing | `FsCheck.Xunit` v3 | Canonical .NET PBT, xUnit `[Property]` integration, EFSM command-based model testing, mature shrinking |
| Snapshot / approval | `Verify.XUnit` | Bootstrap characterization scaffold, integration test assertions on complex aggregate outputs |
| Mutation quality gate | `dotnet-stryker` v4.14+ | Only mature .NET mutation tool; `--with-baseline` for CI delta; `break-at` as hard quality gate |
| Code coverage (informational) | `Coverlet.Collector` | Line coverage is secondary signal but useful for IDE gap visualization; not the acceptance criterion |
| Assertion library | `FluentAssertions` v7 | Readable failure messages, works alongside FsCheck test prefixes, `.Should().Be()` chain |

**TUnit is explicitly not recommended for this project at this time.** Three concurrent risks make it the wrong choice for an AI-assisted mutation-guided workflow: (1) preview APIs with no stability guarantee — framework changes mid-project break test infrastructure; (2) documented Coverlet incompatibility — no code coverage collection without a workaround; (3) thin LLM training data — AI-assisted test generation hallucinates more with TUnit than with xUnit or NUnit because training corpora are dominated by xUnit and NUnit examples. Re-evaluate TUnit at its 1.0 stable release.

**One mandatory pre-start verification:** Run a smoke test of `dotnet stryker` against a minimal `.Domain` project targeting .NET 10 / C# 13 before finalizing the stack. .NET 10 compatibility is implied by Stryker's active maintenance and "Preview" Roslyn support but is not explicitly documented. Failure here would require either pinning to .NET 9 for domain projects or waiting for an explicit Stryker .NET 10 compatibility statement.

---

### Complete Source Index

| Source | Classification | URL |
|---|---|---|
| Characterization Testing — Michael Feathers | Established practice | https://michaelfeathers.silvrback.com/characterization-testing |
| Characterization Tests or Approval Tests — UnderstandLegacyCode | Established practice | https://understandlegacycode.com/blog/characterization-tests-or-approval-tests/ |
| Characterization test — Wikipedia | Reference | https://en.wikipedia.org/wiki/Characterization_test |
| Verify — GitHub (Simon Cropp) | Active open-source library | https://github.com/verifytests/verify |
| Snapshot Testing in .NET with Verify — JetBrains (July 2024) | Practitioner article | https://blog.jetbrains.com/dotnet/2024/07/11/snapshot-testing-in-net-with-verify/ |
| How To Simplify Assertions with Verify — antondevtips (January 2025) | Practitioner article | https://antondevtips.com/blog/how-to-simplify-assertions-in-unit-and-integration-tests-with-verify-in-dotnet |
| Pex — White Box Test Generation for .NET — Springer | Academic paper | https://link.springer.com/chapter/10.1007/978-3-540-79124-9_10 |
| IntelliTest Manual — Microsoft Docs GitHub | Official docs | https://github.com/MicrosoftDocs/visualstudio-docs/blob/main/docs/test/intellitest-manual/index.md |
| EvoSuite Large-Scale Evaluation — ResearchGate | Academic paper | https://www.researchgate.net/publication/273188653_A_Large-Scale_Evaluation_of_Automated_Unit_Test_Generation_Using_EvoSuite |
| Diffblue Cover 20x Productivity — BusinessWire (November 2025) | Marketing / vendor claim | https://www.businesswire.com/news/home/20251104720918/en/Diffblues-Latest-Innovations-in-Unit-Test-Generation-Deliver-20x-Productivity-Advantage-Versus-AI-Coding-Assistants |
| Diffblue $6.3M Funding — SiliconANGLE (October 2024) | News | https://siliconangle.com/2024/10/30/diffblue-raises-6-3m-autonomous-ai-powered-test-generation-platform/ |
| Mutation-Guided LLM-based Test Generation at Meta (ACH) — arXiv 2501.12862 | Peer-reviewed research (FSE 2025) | https://arxiv.org/abs/2501.12862 |
| ACH — FSE 2025 ACM Proceedings | Peer-reviewed research | https://dl.acm.org/doi/10.1145/3696630.3728544 |
| Design choices made by LLM-based test generators prevent them from finding bugs — arXiv 2412.14137 (December 2024) | Peer-reviewed research | https://arxiv.org/html/2412.14137v1 |
| Understanding LLM-Driven Test Oracle Generation — arXiv 2601.05542 (January 2025) | Peer-reviewed research | https://arxiv.org/html/2601.05542v1 |
| AugmenTest: Enhancing Tests with LLM-Driven Oracles — arXiv 2501.17461 (January 2025) | Peer-reviewed research | https://arxiv.org/html/2501.17461v1 |
| Qodo AI Test Generation Overview — DEV Community | Product overview | https://dev.to/rahulxsingh/qodo-ai-test-generation-how-it-works-with-examples-4abk |
| Stryker.NET — GitHub | Active open-source library | https://github.com/stryker-mutator/stryker-net |
| Mutation testing — .NET — Microsoft Learn | Official docs | https://learn.microsoft.com/en-us/dotnet/core/testing/mutation-testing |
| Stryker.NET Configuration Docs — stryker-mutator.io | Official docs | https://stryker-mutator.io/docs/stryker-net/configuration/ |
| dotnet-stryker 4.14.2 — NuGet Gallery | Package registry | https://www.nuget.org/packages/dotnet-stryker |
| PIT Mutation Testing — pitest.org | Official docs (JVM reference) | https://pitest.org/ |
| Enhancing Java Testing with PIT — JavaCodeGeeks (November 2024) | Practitioner article | https://www.javacodegeeks.com/2024/11/enhancing-java-testing-with-pit-a-guide-to-mutation-testing.html |
| Assessing and Improving the Mutation Testing Practice of PIT — arXiv 1601.02351 | Academic paper | https://arxiv.org/pdf/1601.02351 |
| FsCheck Official Docs | Official library docs | https://fscheck.github.io/FsCheck/ |
| Updating FsCheck to v3.x — Bart Wullems (June 2025) | Practitioner article | https://bartwullems.blogspot.com/2025/06/property-based-testing-updating-fscheck.html |
| Property-based testing in C# with CsCheck — Bart Wullems (February 2024) | Practitioner article | https://bartwullems.blogspot.com/2024/02/property-based-testing-in-ccscheck.html |
| FsCheck + xUnit Guide for C# — DevelopersVoice (November 2025) | Practitioner guide | https://developersvoice.com/blog/csharp/csharp-property-based-testing-fscheck-xunit/ |
| PBT with FsCheck by Deriving Properties from Business Rule Models — IEEE Xplore | Academic paper | https://ieeexplore.ieee.org/document/7528965/ |
| PBT from Business Rule Models — Springer SoSyM | Academic paper | https://link.springer.com/article/10.1007/s10270-017-0647-0 |
| FsCheck examples — TUnit docs | Official docs | https://tunit.dev/docs/examples/fscheck/ |
| TUnit Framework Differences — tunit.dev | Official docs | https://tunit.dev/docs/comparison/framework-differences/ |
| TUnit Performance Benchmarks — tunit.dev | Official docs | https://tunit.dev/docs/benchmarks/ |
| Converting an xUnit test project to TUnit — Andrew Lock | Practitioner article | https://andrewlock.net/converting-an-xunit-project-to-tunit/ |
| TUnit vs xUnit and NUnit — Panu Oksala (October 2025) | Practitioner article | https://oksala.net/2025/10/27/unit-testing-with-tunit/ |
| Microsoft.Testing.Platform: Now Supported by All Major .NET Test Frameworks — .NET Blog | Official announcement | https://devblogs.microsoft.com/dotnet/mtp-adoption-frameworks/ |
| Microsoft Testing Platform (xUnit.net v3) — xunit.net | Official docs | https://xunit.net/docs/getting-started/v3/microsoft-testing-platform |
| xUnit vs NUnit vs MSTest in .NET: 2026 Comparison — CodingDroplets | Practitioner comparison | https://codingdroplets.com/xunit-vs-nunit-vs-mstest-in-net-which-testing-framework-should-your-team-use-in-2026 |
| xUnit vs NUnit vs MSTest — TatvaSoft (June 2025) | Practitioner comparison | https://www.tatvasoft.com/outsourcing/2025/06/xunit-vs-nunit-vs-mstest.html |
| DDD Unit Tests — James Hickey | Practitioner article | https://www.jamesmichaelhickey.com/ddd-unit-tests/ |
| Testing Strategies in Domain-Driven Design — DEV Community | Practitioner article | https://dev.to/ruben_alapont/testing-strategies-in-domain-driven-design-ddd-2d93 |
| DDD Testing Strategy: Aggregates and Domain Services — Zenn | Practitioner article | https://zenn.dev/135yshr/articles/78e3eb0a2a275a?locale=en |

================================================================================

The document-specialist has completed the research. Here is a summary of what was delivered for the architect:

---

## Research Delivered: 6 Threads, Full Citations, Established/Emerging/Marketing Classifications

**Thread 1 — Characterization / Approval Testing**
Verify (Simon Cropp) is the canonical .NET tool. Use it as a time-limited bootstrap scaffold only — it cements bugs as approved behavior and scores poorly against Stryker because it asserts whole-output serializations rather than individual rule violations.

**Thread 2 — Pre-LLM Automated Generation**
No tool is usable for .NET 10. IntelliTest requires VS Enterprise + .NET Framework. EvoSuite and Diffblue are JVM-only. All three share the same structural defect: regression-oracle generation encodes bugs as expected behavior.

**Thread 3 — LLM Test Generation (most critical thread)**
Two 2024–2025 peer-reviewed papers define the design space:
- arXiv 2412.14137 (Dec 2024): CoverAgent/Qodo rejected 470 bug-revealing tests per 287 samples; 59.6% of its final suite *validated* bugs rather than detecting them. The filtering that makes LLM tests compile is the exact mechanism that makes them miss bugs.
- arXiv 2501.12862 / FSE 2025 (Meta ACH): Mutation-guided LLM generation achieves 32% mutant kill rate vs. 5.3% for coverage-guided. 49% of mutation-killing tests add zero line coverage — coverage-gated generation structurally cannot produce a mutation-proof suite. Engineer acceptance: 73%.

**Thread 4 — Stryker.NET**
Version 4.14.2 (May 2026), officially documented by Microsoft. `--with-baseline` makes incremental mutation CI-viable from day one. Default `break` threshold is 0 — must be set explicitly (`break-at 70` to start). .NET 10 compatibility implied but not explicitly documented — verify with a smoke test first.

**Thread 5 — Property-Based Testing**
FsCheck 3.x (June 2025 API refresh) is the canonical .NET PBT library. Each DDD business invariant maps directly to a `[Property]`. FsCheck EFSM command-based model testing is the right approach for sprint aggregate state machine transitions. Custom `Arbitrary<T>` generators + shrinkers are mandatory for all domain value objects.

**Thread 6 — .NET Test Stack**
Recommended stack: `xunit.v3` + Microsoft.Testing.Platform + `FsCheck.Xunit` v3 + `Verify.XUnit` + `dotnet-stryker` v4.14+ (`--with-baseline`) + `Coverlet.Collector` + `FluentAssertions` v7. TUnit explicitly excluded: preview APIs, Coverlet incompatibility, and thin LLM training data are active risks for this specific AI-assisted mutation-guided workflow.

**The workflow loop in one sentence:** Stryker identifies surviving mutants → LLM prompted with full class-under-test context (zero-shot, highest oracle accuracy) writes tests targeting those mutants → Stryker confirms the kill → test accepted; anything that does not raise mutation score is discarded.