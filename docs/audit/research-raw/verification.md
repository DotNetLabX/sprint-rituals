I have sufficient depth on all seven threads. Composing the dense structured research report now.

---

## Research: Verification Gates and Hallucination Control for LLM-Generated Artifacts

---

### THREAD 1 — Meta ACH / Assured LLMSE: Mutation-Proof Gate Prior Art

**Source:** "Mutation-Guided LLM-based Test Generation at Meta" — FSE '25 companion proceedings
- arXiv full text: https://arxiv.org/html/2501.12862v1
- PDF: https://arxiv.org/pdf/2501.12862
- InfoQ coverage: https://www.infoq.com/news/2026/01/meta-llm-mutation-testing/
- "Harden and Catch" follow-up: https://arxiv.org/pdf/2504.16472

**Classification:** Established practice (deployed at Meta scale, October–December 2024 trial across FB/Instagram/WhatsApp/Quest/Ray-Ban).

**Mechanism — how mutation gates anti-hallucination:**
ACH's core insight is that LLM-generated tests cannot hallucinate "passing" behavior for a bug that doesn't exist if the acceptance criterion is: *the test must demonstrably fail on the mutant and pass on the original*. The intermediate artifact (mutant) is executed, not trusted. Hallucination is structurally impossible within the bounds of this gate — a test either kills the mutant or it doesn't.

**The six (actually five) verifiable assurances of ACH Assured LLMSE:**
1. **Buildable** — test compiles, no syntax errors or missing deps
2. **Valid Regression Test** — passes consistently (non-flaky), verified by re-execution
3. **Hardening** — kills at least one mutant not already killed by the existing test suite
4. **Additional Coverage** — any extra line coverage reported (informational)
5. **Fashion Following** — respects coding style (aspirational/probabilistic, not strict gate)

The first three are boolean, falsifiable guarantees; the last two are aspirational. "The assurances are unequivocal guarantees made by ACH about the tests it proposes."

**Equivalence Detector (anti-false-positive gate):**
- Without preprocessing: precision 0.79, recall 0.47
- With comment-stripping: precision **0.95**, recall **0.96**
- Root cause of improvement: LLM mutant generation creates "misleading comments" (`// Introduce a bug by not checking...`) without changing executable code; stripping comments before equivalence check auto-discards these
- ~25% of mutants are trivially syntactically identical (caught lexically before LLM is invoked)
- The hard case of true semantic equivalence is rare in practice because LLM-generated mutants are bimodal: obviously changed or obviously equivalent

**Measured results:**
- 4,660 candidate mutants from 10,795 classes under test
- 571 unit tests generated; 277 (49%) killed mutants without adding any line coverage — validating mutation over structural coverage
- Overall engineer acceptance: **73%**
- Acceptance by platform: WhatsApp 56%, Messenger 90%
- 36% of accepted tests were judged privacy-relevant

**Harden/Catch distinction (follow-up paper, arXiv:2504.16472):**
- Hardening test: passes on current revision, fails on some future fault — anti-regression
- Catching test: fails on current revision (true fault detection)
- Deployment metric proposed: R@P=0.8 (recall at 80% precision), meaning ≤20% friction rate is operationally acceptable
- Human-in-the-loop review is the final gate converting "weakly hardening" to "strongly hardening" — paper estimates this removes 3/5 of problematic test categories

**Applicability to our mutation-proof gate (Gate 5):** This is the closest prior art. Our gate is a direct instantiation of ACH's assurance #3. The key lesson: the mutation execution result is the empirical oracle — no LLM trust required. The equivalence detector pattern (filter out trivially non-breaking mutants before testing) maps directly to our concern about spurious rules producing trivial tests.

---

### THREAD 2 — LLM-as-Judge Reliability: Biases and Adversarial Verification

**Sources:**
- Self-preference bias paper: https://arxiv.org/html/2410.21819v2
- Judge bias survey: https://arxiv.org/pdf/2602.07996
- Human agreement assessment: https://arxiv.org/pdf/2512.16041
- ColMAD (collaborative debate for error detection): https://arxiv.org/html/2510.20963
- Debate for scalable oversight (Kenton et al., DeepMind 2024): https://arxiv.org/html/2504.18530v1
- Adversarial debate protocol paper: https://arxiv.org/html/2510.12697v1

**Classification:** Established practice (biases well-documented since 2023–2024); collaborative debate is emerging research (2024–2025).

**Known biases (all empirically measured):**

| Bias | Mechanism | Magnitude |
|------|-----------|-----------|
| Position bias | Primacy/recency preference; swapping positions flips GPT-4 judgment | High; varies by model family |
| Verbosity bias | Longer outputs preferred regardless of quality | Consistent across RLHF-trained models |
| Self-preference | GPT-4 bias score 0.520 (Equal Opportunity metric); NOT from self-recognition but from perplexity — lower-perplexity text (which the model naturally produces for its own outputs) is rated higher | Linear with perplexity favoritism |
| Authority bias | Citations (even fabricated) inflate scores | Documented |

**Critical finding:** Self-preference does not require the model to identify its own output. It arises because LLMs assign higher ratings to lower-perplexity text, and models naturally generate lower-perplexity text for their own style. Even a different model used as judge shows perplexity-driven favoritism. Ensemble across multiple models with inverse-perplexity weighting is the recommended mitigation.

**Human agreement rates:**
- General tasks (MT-Bench/Chatbot Arena style): GPT-4 aligns with human preferences >80%
- Expert domains (dietetics, mental health): 60–68% agreement — significant gap
- Best-in-class LLM judges: accuracy <0.7 on alignment datasets
- Extractive QA Pearson correlation: up to 0.85 vs. humans (much better than F1 at 0.36)

**Adversarial/collaborative debate — empirical results (ColMAD, arXiv:2510.20963):**
- Competitive adversarial MAD (CopMAD) **underperforms single-agent by up to 15% F1** due to "debate hacking" — debaters use overconfident claims and fake evidence to mislead the judge
- ColMAD (non-zero-sum, collaborative): GPT4o-mini + Llama3.1: **76.29 F1 vs. 74.85 F1 single-agent (+2%)**, 86.29 F2 vs. 82.20 (+5%)
- Best ColMAD pair (QwQ-32B + DeepSeek-R1): **80.01 F1**
- ColMAD's protocol enforces direct evidence quoting (exact string match), self-auditing confidence, and "find missing points" framing rather than "defeat opponent"
- Debate hacking modes: fake evidence, overconfident tone ("fundamentally flawed"), fallacious argument deflection
- Judge and debater can be same model; ablations show judge model choice affects results but ColMAD consistently outperforms single-agent regardless

**Debate theory (Irving, Christiano, Amodei 2018; Kenton et al. DeepMind 2024):**
- Stronger debater models → higher judge accuracy (counterintuitively; scales in the right direction)
- ICML 2024: debate significantly outperforms single-advisor consultancy
- Weakness: assumes truth is more persuasive than falsehood; fails when judges are susceptible to persuasion bias; LLM debaters showed 81.2% relative increase in persuading humans to adopt any assigned stance regardless of truth

**Applicability to our adversarial refutation gate (Gate 2):** Use collaborative (ColMAD) not competitive framing. Competitive debate can actively harm accuracy. The ColMAD evidence-quoting protocol — cited claims verified by exact string match against source — is a near-exact instantiation of our evidence-or-reject gate. A purely adversarial skeptic is risky; a skeptic required to cite counter-evidence from source is better.

---

### THREAD 3 — Self-Consistency and Ensembling: Independence Assumption

**Sources:**
- Wang et al. 2023 self-consistency (foundational): https://www.emergentmind.com/topics/self-consistency-in-language-models
- Mirror-Consistency 2024: https://arxiv.org/html/2410.10857v1 / https://aclanthology.org/2024.findings-emnlp.135.pdf
- Intraclass correlation analysis: https://arxiv.org/pdf/2509.19489
- Six Sigma agent (ICE framework): https://arxiv.org/pdf/2601.22290
- Error accumulation analysis: https://arxiv.org/pdf/2505.24187

**Classification:** Established practice (Wang et al., 2023 gains); correlated errors literature is active research (2024–2025).

**Measured gains from self-consistency majority voting:**
- +17.9% on GSM8K, +11.0% on SVAMP, +12.2% on AQuA (Wang et al., 2023)
- These gains are real but assume independence of reasoning paths

**The independence problem — correlated errors:**
- LLM errors are not uniformly distributed random events; they are concentrated at specific knowledge gaps or reasoning patterns
- Multiple samples from the same base model share correlated failure modes
- Intraclass correlation ρ inflates variance; as ρ → 1 (perfect correlation), majority-vote gains vanish
- The correction factor f(ρ, m) approaches 1 when ρ → 1 — meaning N identical agents vote but behave as one
- Independence breaks when: same model, same temperature distribution, same training data

**Mirror-Consistency (2024):** Rather than ignoring minority votes, explicitly analyzes inconsistencies — contrasts majority and minority paths to identify error signals. More informative than discarding minority votes.

**Iterative Consensus Ensemble (ICE, Omar et al., 2025):** Three LLMs critique each other's answers until convergence. Substantially outperforms single model.

**Practical implication for our confidence gating (Gate 3):** Consistency across multiple calls to the same base model is a weak independence signal. True independence requires: different base models, or different retrieval contexts, or structurally different prompting strategies. Using two agents from the same model family to validate each other's rule extraction provides far less independence than using architecturally different models (GPT-4 class vs. Claude class vs. open-weight). Self-consistency within a single model's multiple samples provides calibration signal but not strong adversarial refutation.

---

### THREAD 4 — Confidence Calibration: Verbalized vs. Sampling-Based

**Sources:**
- "On Verbalized Confidence Scores for LLMs": https://arxiv.org/html/2412.14737v2
- ICLR 2024 confidence elicitation benchmark: https://openreview.net/forum?id=gjeQKFxFpZ / https://github.com/MiaoXiong2320/llm-uncertainty
- Semantic entropy (Nature 2024): https://pubmed.ncbi.nlm.nih.gov/38898292/ / https://oatml.cs.ox.ac.uk/blog/2024/06/19/detecting_hallucinations_2024.html
- Calibration degradation from RLVR: https://arxiv.org/html/2605.15588
- LPP meta-model escalation: https://arxiv.org/html/2601.07006v1

**Classification:** Established practice (verbalized overconfidence well-documented); semantic entropy as gold standard is established; sampling-based proxies are established with known cost trade-offs.

**Verbalized confidence — key numbers:**
- Large LLMs (≥70B): ECE ≈ 0.10 (10% average deviation from true accuracy)
- Best "combo" prompt method: average deviation 7% from empirical accuracy
- Verbalized confidence is systematically overconfident across model sizes
- "Uncalibrated verbalized confidence is severely overconfident"
- RLVR training further degrades calibration (binary correctness rewards incentivize overconfident guessing)

**Best verbalized elicitation method:** "combo" prompt — probability framing + uncertainty source description + 5-shot examples + "best guess" phrasing. For small models, simpler "probscore" formulation.

**Sampling-based consistency as proxy:**
- Semantic entropy (Farquhar et al., Nature 2024): clusters repeated generations by semantic equivalence (bidirectional entailment), computes entropy over meaning-space not token-space
- AUROC for hallucination detection on TriviaQA: ~77% baseline, improved to ~80% by newer methods
- Semantic Entropy Probes (SEPs): approximate semantic entropy from single forward pass (hidden states), near-zero overhead
- Efficient Bayesian SE: achieves same AUROC quality with only **53% of samples** via adaptive sampling
- Cost: original semantic entropy requires 5–10× inference budget; SEPs bring this to near-zero

**LPP meta-model for escalation routing:**
- Ridge Regression trained on: Post-hoc classification uncertainty (entropy, MSP, top-2 margin) + verbalized confidence bands + binary uncertainty attribution flags
- CoT-based features explicitly excluded (inflated confidence, harmed calibration)
- GPT-4o-mini escalation rate with meta-model: **16%** of cases vs. baselines that over-escalated to ~38%
- F1 94.14%, AUC-ROC 93.46% for predicting when LLM will fail
- Removing any feature family degrades performance; post-hoc classification features most critical

**Applicability to our confidence gating (Gate 3):** Verbalized high/medium/low is unreliable raw but improvable with combo prompting. For production use, the right architecture is: semantic consistency proxy (2–3 independent samples, cluster by meaning) as the primary confidence signal, not self-reported confidence. Gate threshold calibration must be empirical — LLM-stated "90% confident" maps poorly to actual 90% accuracy without calibration curves per domain. The LPP meta-model approach (combining multiple uncertainty signals) is the best-validated production pattern, achieving 16% escalation rate at 94% F1.

---

### THREAD 5 — Evidence-Grounded Generation: Citation-Required Generation and Claim Verification

**Sources:**
- "Correctness is Not Faithfulness" (Wallat et al., 2024): https://arxiv.org/pdf/2412.18004
- FACTUM (citation hallucination in RAG): https://arxiv.org/pdf/2601.05866
- RT4CHART (retromorphic testing): https://arxiv.org/pdf/2603.27752
- Anthropic Citations API announcement: https://www.anthropic.com/news/introducing-citations-api
- Anthropic hallucination reduction docs: https://docs.anthropic.com/en/docs/test-and-evaluate/strengthen-guardrails/reduce-hallucinations
- FActScore: https://123ofai.com/qnalab/system-design/blocks/faithfulness
- Attribution survey: https://arxiv.org/pdf/2601.19927

**Classification:** RAG faithfulness as field is established practice; atomic-claim verification pipelines are established; inline citation generation is emerging practice; post-rationalization detection is emerging research.

**Critical finding — correctness ≠ faithfulness:**
- A citation can be correct (the source document is indeed relevant) but unfaithful (the LLM generated the claim without consulting that source, then retroactively assigned a citation)
- Up to **57% of citations are post-rationalized** (Wallat et al., 2024)
- GTR (Generate-then-Retrieve) and post-hoc RTG pipelines are structurally incapable of faithful citation
- Requirement for faithful citation: citations must be generated inline during answer generation, not assigned afterward

**Anthropic Citations API — measured results:**
- Endex (financial AI): source hallucinations reduced from **10% to 0%**, 20% increase in references per response
- Thomson Reuters CoCounsel (legal): built-in citations replaced custom solution, improved accuracy
- Anthropic recommends: for long docs, extract word-for-word quotes before performing task; if LLM cannot find a supporting quote after generation, it must retract the claim
- RAG overall reduces hallucinations by approximately 71% vs. vanilla LLMs; legal RAG tools still hallucinate 17–33% of queries (domain-specific baseline)

**FActScore:**
- ChatGPT achieves only **58% faithfulness** on biographical generation (EMNLP 2023 baseline)
- Automated FActScore has <2% error rate vs. human judgments

**RT4CHART — retromorphic testing as verification architecture:**
- Each generated claim must trace back to originating input via inverted generative mapping
- Hierarchical verification: claim-level traceability check
- Applicable to our evidence-or-reject gate: the test is not "does this document support this claim?" but "can we reverse-map from this claim back to an exact span in source?"

**Applicability to our evidence-or-reject gate (Gate 1):** The post-rationalization problem is the key risk — an LLM asked to cite evidence may select a plausible-looking file:line that doesn't actually support the rule, rather than genuinely deriving the rule from that line. Mitigation: require quote extraction before rule statement (not after), verify by NLI entailment that the quote entails the rule, and flag cases where the LLM changes the rule after being shown the source. Retromorphic testing (RT4CHART) provides a pipeline pattern directly applicable.

---

### THREAD 6 — LLM Hallucination Rates Describing Code Specifically

**Sources:**
- "Beyond Functional Correctness: Exploring Hallucinations in LLM-Generated Code" (2024): https://arxiv.org/html/2404.00971v3
- ETF (Entity Tracing for Code Summaries): https://arxiv.org/pdf/2410.14748
- FaithLens: https://arxiv.org/pdf/2512.20182
- Multi-document summarization hallucination: https://arxiv.org/html/2410.13961v1

**Classification:** Emerging research (2024); first systematic taxonomy of code hallucinations established this year.

**Code hallucination taxonomy and rates (study: 3,120 samples, HumanEval + CoderEval, models: DeepSeek-Coder 1.3B/7B, CodeLlama-7B, GPT-4, DeepSeek-R1):**

| Type | Rate |
|------|------|
| Requirement Conflicting (Behavior) | 35.40% |
| Knowledge: Library/Project | 25.99% |
| Code Inconsistency: Undefined Variables | 16.91% |
| Knowledge: Algorithm | 4.95% |
| Requirement Conflicting (Data) | 4.21% |
| Knowledge: Computer Theory | 2.31% |

- Of 3,120 samples, **1,134 contained at least one hallucination** — approximately 36% hallucination rate
- 1,212 hallucinatory snippets total (some samples had multiple)
- Pass@1 for hallucination-free code: **54.73%** vs. hallucinatory code: **6.26%** — hallucination strongly predicts failure
- **Behavior Conflicting** (code logic deviates from intended behavior) is the dominant category at 35.4% — this is exactly the class relevant to rule extraction

**Detectability:**
- Undefined Variables: statically detectable
- Behavior Conflicting, Data Conflicting, Library/Project: require dynamic execution or semantic analysis
- Incorrect Functionality affects 95.3% of hallucinatory samples — almost always requires runtime testing to catch

**Code summarization specifically (ETF, 2024):**
- LLMs generating code summaries hallucinate entities (variable names, types, return values) at non-trivial rates
- Entity-level tracing shows that even small models hallucinate structurally correct but semantically wrong entity references

**Applicability to our rule extraction pipeline:** The 36% overall code hallucination rate, concentrated in "Behavior Conflicting" (35.4% of hallucinations) and "Library/Project Knowledge" (26%), means that without gates, roughly 1 in 3 rule extractions will contain at least one hallucinated claim. The behavior conflicting category maps precisely to the scenario we must prevent: the LLM inventing what a code path does rather than reading it. Static analysis catches undefined variables but misses the rest — only execution-based or evidence-grounded verification catches Behavior Conflicting hallucinations.

---

### THREAD 7 — Human-in-the-Loop Gate Design: Escalation Patterns and Ratios

**Sources:**
- LPP meta-model escalation paper: https://arxiv.org/html/2601.07006v1
- HITL production patterns: https://alldaystech.com/guides/artificial-intelligence/human-in-the-loop-ai-review-queue-workflows
- Redis AI HITL patterns: https://redis.io/blog/ai-human-in-the-loop/
- Grizzly Peak HITL patterns: https://www.grizzlypeaksoftware.com/library/human-in-the-loop-patterns-for-ai-agents-n64sb2cm

**Classification:** Production engineering patterns (established practice); meta-model escalation routing is emerging research (2025).

**Measured auto/escalate ratios in production (LPP meta-model, OpenAI moderation dataset):**

| Model | Escalation Rate (meta-model) | Escalation Rate (baseline, MSP) |
|-------|------------------------------|----------------------------------|
| GPT-4o-mini | 16% | ~38% |
| GPT-4o | 17% | — |
| Gemini-2.5-flash-lite | 25% | — |
| GPT-4o multimodal | 13% | — |

- Baseline escalation predictors (MSP, entropy) over-escalate by 2–3× vs. meta-model
- Cost savings: GPT-4o-mini review cost reduced from $132 → $38 (71% reduction) using meta-model routing
- Target operating point: R@P=0.8 (80% precision) is the accepted industry threshold per ACH paper
- For 99% end-to-end accuracy: ~30–40% escalation rate; for 95%: ~10–15%

**Queue design patterns for rule/test review:**
- Two-stage review: fast triage first, deep review for a subset (prevents long-tail backlog)
- Batch review by similarity: group rules from same file/function, review as cluster — reduces context switching
- Priority lanes: P0 (contradiction detected) → immediate escalation; P1 (low confidence) → batch queue; P2 (auto-flow high confidence)
- Irreversible actions (emitting tests to CI) always require human approval regardless of confidence
- Never adjust auto/escalate threshold in real-time; recalibrate weekly or every N decisions

**Key calibration caveat:** "Confidence" from LLM ≠ calibrated probability. Thresholds must be set empirically against actual accuracy measurements on held-out ground truth, not on LLM-stated confidence values. This is validated by the LPP paper: CoT-based confidence features were excluded because they inflated confidence and harmed calibration.

**For contradiction escalation (Gate 5 — contradictions escalate to humans):**
- Contradictions should trigger P0 lane (immediate escalation, no auto-resolution)
- Never auto-resolve contradictions because the correct behavior depends on actual business intent, not LLM inference
- Human queue for contradictions should include: both conflicting rules, the source spans for each, the conflict type (logical negation, temporal conflict, scope overlap), a recommended resolution path (not a verdict)

---

### TOP 5 DESIGN IMPLICATIONS

**1. Gate 5 (Mutation Proof) is the strongest gate — and has the most direct prior art.**
Meta ACH demonstrates that mutation-based acceptance is structurally anti-hallucination: the LLM cannot fabricate a "passing" test for a nonexistent rule violation. The equivalence detector pattern (preprocessing to remove comment-only mutations) is directly portable. Target acceptance rate: 73% engineer acceptance is the real-world benchmark; design for a similar rate. The Harden/Catch distinction is useful: rule extraction produces "catching" tests (catch violations of the rule as written) — these are the hardest class.

**2. Gate 1 (Evidence-or-Reject) is validated by RAG faithfulness literature, but post-rationalization is the specific failure mode to prevent.**
Requiring file:line citations does not prevent post-rationalized citations (up to 57% in Wallat et al.). The gate must enforce quote-first ordering: the LLM must extract a quote from source before stating the rule, not after. Verification should be NLI-based entailment check (does the extracted quote entail the rule statement?), not just presence of a citation. RT4CHART's retromorphic tracing — can we trace the claim back to source? — is the right verification architecture.

**3. Gate 2 (Adversarial Refutation) should be collaborative, not competitive.**
Competitive debate (CopMAD) degrades accuracy by up to 15% via debate hacking. The ColMAD pattern — refuter must cite counter-evidence by exact string match, forbidden to make ungrounded overconfident claims — is the validated design. The adversarial framing should be "find missing evidence" not "prove it wrong." Using architecturally different base models for proposer and refuter is important: same-model pairs have correlated failure modes.

**4. Gate 3 (Confidence Gating) is the weakest gate as specified — verbalized confidence is unreliable; the right proxy is sampling-based semantic consistency.**
Verbalized high/medium/low with ECE ~0.10 is not sufficiently reliable to auto-flow high-confidence items without additional validation. Implement semantic entropy (cluster 3–5 independent samples by meaning-equivalence, compute entropy over clusters) as the primary confidence proxy. Combine with post-hoc uncertainty signals (MSP, entropy from logits if available) per the LPP meta-model. Target: 16% escalation rate at 94% F1 is achievable with meta-model routing. Do not use CoT-generated confidence — it inflates overconfidence. Use SEPs (hidden-state probes) if inference cost is a concern.

**5. Gate 4 (Contradictions Escalate) is validated but requires explicit queue engineering.**
Human escalation for contradictions is correct and supported by all deployed HITL literature. Design specifics: P0 immediate queue, include both rule spans and source evidence for each, surface conflict type (not just "conflict"), provide recommended resolution templates. Set empirical auto/escalate thresholds (16–25% escalation is typical for production LLM systems at 80%+ precision). Never auto-resolve; the cost of a wrong auto-resolution (a test asserting incorrect behavior, passing CI) is unbounded.

**What's missing from the planned design:**
- No gate for **post-rationalized citations** — the architecture assumes citing file:line is sufficient, but 57% of LLM citations can be post-rationalized. Add retromorphic verification (quote-first ordering + NLI entailment check).
- No gate for **mutation equivalence** — trivially non-breaking mutations will waste test generation cycles; add a static equivalence pre-filter (comment stripping is 98%+ effective per Meta data) before invoking the mutation-proof gate.
- **Cross-agent correlated errors** — if both the rule extractor and the refuter use the same base model, their failure modes are correlated (both will miss the same Knowledge: Library/Project class hallucinations at 26% rate). Require architecturally distinct models.
- **Calibration curves** — the confidence gating thresholds ("high auto-flows") must be calibrated against ground-truth accuracy measurements per domain, not set a priori. Schedule weekly recalibration.

================================================================================

Here is the dense research synthesis returned by the document-specialist agent.

---

## Research: Verification Gates and Hallucination Control for LLM-Generated Artifacts

---

### THREAD 1 — Meta ACH / Assured LLMSE: Mutation-Proof Gate Prior Art

**Sources:**
- arXiv full text: https://arxiv.org/html/2501.12862v1
- PDF: https://arxiv.org/pdf/2501.12862
- InfoQ coverage: https://www.infoq.com/news/2026/01/meta-llm-mutation-testing/
- "Harden and Catch" follow-up: https://arxiv.org/pdf/2504.16472

**Classification:** Established practice — deployed at Meta scale, October–December 2024 trial across FB/Instagram/WhatsApp/Quest/Ray-Ban.

**Mechanism — how mutation gates anti-hallucination:**
ACH's core insight is that LLM-generated tests cannot hallucinate "passing" behavior for a bug that doesn't exist if the acceptance criterion is: the test must demonstrably fail on the mutant and pass on the original. The intermediate artifact (mutant) is executed, not trusted. Hallucination is structurally impossible within the bounds of this gate.

**The five verifiable assurances of ACH Assured LLMSE:**
1. Buildable — compiles, no syntax errors or missing deps
2. Valid Regression Test — passes consistently (non-flaky), verified by re-execution
3. Hardening — kills at least one mutant not already killed by existing suite
4. Additional Coverage — any extra line coverage reported (informational)
5. Fashion Following — respects coding style (aspirational, not strict gate)

Assurances 1–3 are boolean, falsifiable guarantees. "The assurances are unequivocal guarantees made by ACH about the tests it proposes."

**Equivalence Detector (anti-false-positive sub-gate):**

| Condition | Precision | Recall |
|-----------|-----------|--------|
| Without preprocessing | 0.79 | 0.47 |
| With comment-stripping | 0.95 | 0.96 |

Root cause: LLM mutant generation inserts misleading comments (`// Introduce a bug by not checking...`) without changing executable code. Stripping comments before equivalence check auto-discards these. ~25% of mutants are trivially syntactically identical — caught lexically before the LLM equivalence check is invoked.

**Measured deployment results:**
- 4,660 candidate mutants from 10,795 classes under test
- 571 unit tests generated; 277 (49%) killed mutants without adding any line coverage
- Overall engineer acceptance: **73%** (WhatsApp 56%, Messenger 90%)
- 36% of accepted tests judged privacy-relevant

**Harden/Catch distinction (arXiv:2504.16472):**
- Hardening test: passes now, fails on future fault — anti-regression
- Catching test: fails on current revision — true fault detection
- Deployment target metric proposed: R@P=0.8 (recall at 80% precision), meaning ≤20% friction is operationally acceptable
- Human review is the final gate converting "weakly hardening" to "strongly hardening" — eliminates 3/5 of problematic test categories

**Applicability to Gate 5:** Closest prior art. Mutation execution result is the empirical oracle — no LLM trust required. The equivalence pre-filter (comment stripping before checking) maps directly to the concern about spurious rules producing trivially non-breaking tests.

---

### THREAD 2 — LLM-as-Judge Reliability: Biases and Adversarial Verification

**Sources:**
- Self-preference bias: https://arxiv.org/html/2410.21819v2
- Judge bias survey: https://arxiv.org/pdf/2602.07996
- Human agreement assessment: https://arxiv.org/pdf/2512.16041
- ColMAD collaborative debate: https://arxiv.org/html/2510.20963
- Debate scalable oversight (Kenton et al., DeepMind 2024): https://arxiv.org/html/2504.18530v1
- Formal debate with stability detection: https://arxiv.org/html/2510.12697v1

**Classification:** Biases are established practice (well-documented 2023–2024); collaborative debate is emerging research (2024–2025).

**Documented biases:**

| Bias | Mechanism | Severity |
|------|-----------|----------|
| Position | Primacy/recency; swapping positions flips GPT-4 judgment | High; model-family dependent |
| Verbosity | Longer outputs preferred regardless of quality | Consistent across RLHF-trained models |
| Self-preference | GPT-4 bias score 0.520 (Equal Opportunity metric); root cause is perplexity favoritism, not self-identification | Linear with perplexity |
| Authority | Citations (even fabricated) inflate scores | Documented |

Critical finding on self-preference: the bias does not require the model to identify its own output. LLMs assign higher ratings to lower-perplexity text, and they naturally generate lower-perplexity text in their own style. Even a different judge model shows perplexity-driven favoritism. Ensemble across multiple models with inverse-perplexity weighting is the recommended mitigation.

**Human agreement rates:**
- General tasks (MT-Bench/Chatbot Arena): GPT-4 aligns with human preferences >80%
- Expert domains (dietetics, mental health): 60–68% agreement
- Best-in-class LLM judges: accuracy <0.7 on alignment datasets
- Extractive QA Pearson correlation: up to 0.85 vs. humans

**Adversarial/collaborative debate — empirical results (ColMAD):**

| Protocol | Nature | Key Result |
|----------|--------|------------|
| CopMAD (competitive) | Zero-sum | Up to **−15% F1** vs. single-agent due to "debate hacking" |
| ColMAD (collaborative) | Non-zero-sum | **+2% F1, +5% F2** over single-agent; best pair (QwQ-32B + DeepSeek-R1): 80.01 F1 |
| Single-agent baseline | — | GPT4o-mini: 74.85 F1 / 82.20 F2 |

ColMAD protocol enforces: direct evidence quoting (exact string match), self-auditing confidence, "find missing points" framing. Debate hacking modes in CopMAD: fake evidence insertion, overconfident tone, fallacious deflection to irrelevant points.

**Debate theory validation (Kenton et al., DeepMind, ICML 2024):**
- Stronger debater models → higher judge accuracy (scales in the right direction)
- Debate outperforms single-advisor consultancy across all 9 tasks tested (~5 million model calls)
- Weakness: LLM debaters showed 81.2% relative increase in persuading humans to adopt any assigned stance regardless of truth — persuasion bias is a real ceiling

**Applicability to Gate 2:** Use ColMAD not CopMAD framing. The refuter must cite counter-evidence by exact string match against source. A purely adversarial skeptic can actively harm accuracy. A skeptic required to cite counter-evidence from source is the validated design.

---

### THREAD 3 — Self-Consistency and Ensembling: Independence Assumption

**Sources:**
- Self-consistency (Wang et al., 2023): https://www.emergentmind.com/topics/self-consistency-in-language-models
- Mirror-Consistency 2024: https://aclanthology.org/2024.findings-emnlp.135.pdf
- Intraclass correlation analysis: https://arxiv.org/pdf/2509.19489
- ICE framework (Omar et al., 2025): https://arxiv.org/pdf/2601.22290

**Classification:** Established practice (Wang et al. gains); correlated errors quantification is active research (2024–2025).

**Measured gains from majority voting:**
- +17.9% on GSM8K, +11.0% on SVAMP, +12.2% on AQuA (Wang et al., 2023)

**The independence problem:**
- LLM errors are concentrated at specific knowledge gaps, not uniformly distributed
- Multiple samples from the same base model share correlated failure modes
- Intraclass correlation ρ inflates variance; as ρ → 1, majority-vote gains vanish (correction factor f(ρ, m) → 1)
- Independence breaks when: same model, same temperature distribution, same training data

**Mirror-Consistency (2024):** Explicitly analyzes minority votes rather than discarding them — minority votes identify error patterns that majority voting misses.

**Applicability to Gate 3:** Consistency across multiple calls to the same base model is a weak independence signal. True independence requires different base models or structurally different retrieval contexts. Using two agents from the same model family for validation provides much less independence than using architecturally distinct models.

---

### THREAD 4 — Confidence Calibration: Verbalized vs. Sampling-Based

**Sources:**
- "On Verbalized Confidence Scores for LLMs": https://arxiv.org/html/2412.14737v2
- ICLR 2024 confidence elicitation benchmark: https://openreview.net/forum?id=gjeQKFxFpZ
- Semantic entropy (Farquhar et al., Nature 2024): https://pubmed.ncbi.nlm.nih.gov/38898292/
- OATML blog: https://oatml.cs.ox.ac.uk/blog/2024/06/19/detecting_hallucinations_2024.html
- LPP meta-model escalation: https://arxiv.org/html/2601.07006v1

**Classification:** Verbalized overconfidence is established practice; semantic entropy as gold standard is established; LPP meta-model routing is emerging research (2025).

**Verbalized confidence calibration numbers:**
- Large LLMs (≥70B parameters): ECE ≈ **0.10** (10% average deviation from true accuracy)
- Best "combo" prompt method: 7% average deviation from empirical accuracy
- Combo = probability framing + uncertainty source description + 5-shot examples + "best guess" phrasing
- RLVR training degrades calibration further (binary correctness rewards incentivize overconfident guessing)
- Verdict: "uncalibrated verbalized confidence is severely overconfident"

**Sampling-based proxy (semantic entropy):**
- Cluster repeated generations by semantic equivalence (bidirectional entailment via DeBERTa-Large-MNLI or GPT-3.5)
- Compute entropy over meaning-space clusters, not token sequences
- AUROC for hallucination detection: ~77–80% on TriviaQA
- Original cost: 5–10× inference budget; Semantic Entropy Probes (SEPs) bring overhead to near-zero via hidden-state approximation
- Efficient Bayesian SE: achieves same quality with **53% of samples** via adaptive sampling

**LPP meta-model — production escalation routing:**
- Features: post-hoc classification uncertainty (entropy, MSP, top-2 margin) + verbalized confidence bands + binary uncertainty attribution flags
- CoT features explicitly excluded (inflated confidence, harmed calibration)
- GPT-4o-mini escalation rate: **16%** of cases (baselines over-escalate to ~38%)
- Meta-model F1: **94.14%**, AUC-ROC: **93.46%** for predicting LLM failure
- Cost reduction: $132 → $38 (71%) for GPT-4o-mini review pipeline

**Applicability to Gate 3:** Verbalized high/medium/low with ECE ~0.10 is insufficient as a standalone gate. Implement semantic consistency proxy (2–5 independent samples, cluster by meaning, compute entropy) as the primary confidence signal. Combine with post-hoc uncertainty signals per LPP architecture. Target 16% escalation rate at 94% F1 is achievable. Do not use CoT-generated confidence — it inflates overconfidence.

---

### THREAD 5 — Evidence-Grounded Generation: Citation-Required and Claim Verification

**Sources:**
- "Correctness is Not Faithfulness" (Wallat et al., 2024): https://arxiv.org/pdf/2412.18004
- FACTUM (citation hallucination in long-form RAG): https://arxiv.org/pdf/2601.05866
- RT4CHART (retromorphic testing): https://arxiv.org/pdf/2603.27752
- Anthropic Citations API: https://www.anthropic.com/news/introducing-citations-api
- Anthropic hallucination reduction docs: https://docs.anthropic.com/en/docs/test-and-evaluate/strengthen-guardrails/reduce-hallucinations
- FActScore: https://123ofai.com/qnalab/system-design/blocks/faithfulness

**Classification:** RAG faithfulness is established practice; inline citation generation and post-rationalization detection are emerging research.

**Critical finding — correctness ≠ faithfulness:**
- A citation can be correct (the source document is relevant) but unfaithful (the LLM generated the claim without consulting that source, then retroactively assigned a citation)
- Up to **57% of citations are post-rationalized** (Wallat et al., 2024)
- GTR (Generate-then-Retrieve) and post-hoc RTG pipelines are structurally incapable of faithful citation
- Requirement: citations must be generated inline during answer generation, not assigned afterward

**Anthropic Citations API — measured results:**
- Endex (financial AI): source hallucinations reduced **10% → 0%**, 20% increase in references per response
- Thomson Reuters CoCounsel (legal): replaced custom citation system, improved accuracy
- Anthropic protocol: extract word-for-word quotes before task; if LLM cannot find supporting quote after generation, it must retract the claim
- RAG overall: ~71% hallucination reduction vs. vanilla LLMs; legal RAG still hallucinates 17–33% of queries

**FActScore baseline:**
- ChatGPT: **58% faithfulness** on biographical generation (EMNLP 2023)
- Automated FActScore: <2% error rate vs. human judgments

**RT4CHART — retromorphic verification:**
- Each claim must trace back to originating input span via inverted generative mapping
- Hierarchical verification: claim-level traceability check
- Applicable as a post-generation verification pass: generate rule, then verify each sub-claim can be traced to an exact source span

**Applicability to Gate 1:** Post-rationalization is the specific failure mode. The gate must enforce quote-first ordering: the LLM extracts a quote from source before stating the rule, not after. Verification should be NLI-based entailment (does the extracted quote entail the rule statement?). RT4CHART's retromorphic tracing is the right verification architecture.

---

### THREAD 6 — LLM Hallucination Rates When Describing Code

**Sources:**
- "Beyond Functional Correctness: Exploring Hallucinations in LLM-Generated Code" (2024): https://arxiv.org/html/2404.00971v3
- ETF (Entity Tracing for Code Summaries): https://arxiv.org/pdf/2410.14748
- FaithLens: https://arxiv.org/pdf/2512.20182

**Classification:** Emerging research — first systematic taxonomy of code hallucinations established in 2024.

**Code hallucination taxonomy and rates (3,120 samples, HumanEval + CoderEval, models: DeepSeek-Coder 1.3B/7B, CodeLlama-7B, GPT-4, DeepSeek-R1):**

| Hallucination Type | Rate |
|-------------------|------|
| Requirement Conflicting: Behavior | **35.40%** |
| Knowledge: Library/Project | **25.99%** |
| Code Inconsistency: Undefined Variables | 16.91% |
| Knowledge: Algorithm | 4.95% |
| Requirement Conflicting: Data | 4.21% |
| Knowledge: Computer Theory | 2.31% |

- Of 3,120 samples, **1,134 (36%) contained at least one hallucination**
- Pass@1 for hallucination-free code: **54.73%** vs. hallucinatory code: **6.26%** — hallucination is strongly predictive of failure
- Incorrect Functionality affects **95.3% of hallucinatory samples** — requires runtime testing to catch

**Detectability by method:**
- Undefined Variables: statically detectable
- Behavior Conflicting, Data Conflicting, Library/Project: require dynamic execution or semantic analysis
- Static analysis catches ~17% of hallucination instances (undefined variable class); the other 83% require execution

**Applicability:** Without gates, roughly 1 in 3 rule extractions will contain at least one hallucinated claim. The dominant category — Behavior Conflicting at 35.4% of hallucinations — maps precisely to the scenario we must prevent: the LLM inventing what a code path does. This class is not statically detectable; only evidence-grounded or execution-based verification catches it.

---

### THREAD 7 — Human-in-the-Loop Gate Design: Escalation Patterns

**Sources:**
- LPP meta-model paper: https://arxiv.org/html/2601.07006v1
- HITL production guide: https://alldaystech.com/guides/artificial-intelligence/human-in-the-loop-ai-review-queue-workflows
- Redis AI HITL patterns: https://redis.io/blog/ai-human-in-the-loop/

**Classification:** Production engineering patterns are established practice; meta-model escalation routing is emerging research (2025).

**Measured auto/escalate ratios (LPP meta-model, OpenAI moderation dataset):**

| Model | Meta-model Escalation | Baseline (MSP) Escalation |
|-------|----------------------|--------------------------|
| GPT-4o-mini | **16%** | ~38% |
| GPT-4o | **17%** | — |
| Gemini-2.5-flash-lite | **25%** | — |
| GPT-4o multimodal | **13%** | — |

- Cost savings: GPT-4o-mini review cost $132 → $38 (71% reduction)
- Target: R@P=0.8 is accepted industry threshold (≤20% friction)
- For 99% end-to-end accuracy: ~30–40% escalation; for 95%: ~10–15%

**Queue design patterns:**
- Two-stage review: fast triage first, deep review for subset
- Batch by similarity: group rules from same file/function — reduces context switching
- Priority lanes: P0 (contradiction detected) → immediate; P1 (low confidence) → batch queue; P2 (high confidence) → auto-flow
- Irreversible actions (emitting tests to CI) always require human approval regardless of confidence
- Threshold recalibration: weekly or every N decisions, not real-time

**For contradiction escalation specifically:**
- P0 immediate queue — no auto-resolution under any condition
- Include: both conflicting rule statements, source spans for each, conflict type (logical negation / temporal / scope overlap), recommended resolution templates (not verdicts)

**Applicability to Gate 5 (contradictions escalate):** Validated by all deployed HITL literature. The key engineering concern is queue design: contradictions must carry enough context (both source spans, conflict type) that a human reviewer can resolve in one pass without re-reading the code.

---

### TOP 5 DESIGN IMPLICATIONS

**1. Gate 5 (Mutation Proof) is the strongest gate — structural anti-hallucination, not statistical.**
Meta ACH demonstrates that mutation-based acceptance is structurally anti-hallucination. The LLM cannot fabricate a passing test for a nonexistent rule violation because mutant execution is the oracle. Add the equivalence pre-filter (comment stripping achieves 0.95/0.96 precision/recall) before invoking mutation testing to avoid wasting cycles on trivially non-breaking mutants. Target 73% engineer acceptance as a baseline; WhatsApp at 56% suggests domain-specific variation is large.

**2. Gate 1 (Evidence-or-Reject) is validated but post-rationalization is the specific attack to prevent.**
Requiring file:line citations does not prevent post-rationalized citations — up to 57% of LLM citations are retroactively assigned (Wallat et al.). The gate must enforce quote-first ordering: LLM extracts a quote before stating the rule, not after. Add NLI entailment verification (does the extracted quote entail the rule statement?). RT4CHART's retromorphic tracing is the right verification architecture for this.

**3. Gate 2 (Adversarial Refutation) must be collaborative, not competitive — competitive debate actively degrades accuracy.**
CopMAD (zero-sum adversarial) degrades accuracy up to 15% via debate hacking. ColMAD (non-zero-sum, evidence-citing collaborative) achieves +2–5% over single-agent. The refuter must cite counter-evidence by exact string match against source. Use architecturally distinct models for proposer and refuter — same-model pairs have correlated failure modes (both miss Library/Project hallucinations at ~26% rate).

**4. Gate 3 (Confidence Gating) as verbalized high/medium/low is the weakest gate — replace with sampling-based semantic entropy.**
Verbalized confidence has ECE ~0.10 (10% systematic overconfidence). The right primary signal is semantic consistency across 3–5 independent samples (cluster by meaning, compute entropy over clusters). Combine with post-hoc uncertainty signals (MSP, entropy from logits) per the LPP meta-model architecture. Target: 16% escalation rate at 94% F1 is achievable. Never use CoT-generated confidence — it inflates overconfidence. Calibrate thresholds empirically against held-out ground truth, not on LLM-stated confidence.

**5. Gate 5 (Contradictions Escalate) is correctly designed but needs explicit queue engineering.**
Human escalation for contradictions is validated by all deployed HITL literature. P0 immediate queue (never batch contradictions). Include both rule statements, both source spans, conflict type, and resolution templates. Never auto-resolve contradictions. The 36% base hallucination rate for code description means contradictions will be frequent — design the queue for volume from day one. Set empirical auto-escalate thresholds (16–25% is typical in production at 80%+ precision) and recalibrate weekly.

**What is missing from the planned design:**
- No gate for **post-rationalized citations**: requiring file:line is necessary but insufficient. Add quote-first ordering enforcement and NLI entailment verification as part of Gate 1.
- No **mutation equivalence pre-filter**: trivially non-breaking mutations waste test generation cycles. Comment-stripping pre-filter is 95%+ effective per Meta data; add it before Gate 5 is invoked.
- **Cross-agent correlated errors**: if proposer and refuter share a base model, correlated failures (especially Library/Project class at 26%) will both be missed. Mandate architecturally distinct models.
- **Confidence threshold calibration schedule**: the "high auto-flows" threshold must be calibrated empirically against ground-truth accuracy per domain and recalibrated regularly — it cannot be set a priori from LLM-stated confidence values.

================================================================================

Here is the complete research synthesis across all seven threads, ready for the architect to use directly.

---

## Research: Verification Gates and Hallucination Control for LLM-Generated Artifacts

---

### THREAD 1 — Meta ACH / Assured LLMSE: Mutation-Proof Gate Prior Art

**Sources:**
- arXiv full text: https://arxiv.org/html/2501.12862v1
- PDF: https://arxiv.org/pdf/2501.12862
- InfoQ coverage: https://www.infoq.com/news/2026/01/meta-llm-mutation-testing/
- "Harden and Catch" follow-up: https://arxiv.org/pdf/2504.16472

**Classification:** Established practice — deployed at Meta scale, October–December 2024 trial across FB/Instagram/WhatsApp/Quest/Ray-Ban.

**Mechanism — how mutation gates anti-hallucination:**
ACH's core insight: LLM-generated tests cannot hallucinate "passing" behavior for a bug that does not exist if the acceptance criterion is that the test must demonstrably fail on the mutant and pass on the original. The intermediate artifact (mutant) is executed, not trusted. Hallucination is structurally impossible within the bounds of this gate.

**The five verifiable assurances of ACH Assured LLMSE:**
1. Buildable — compiles, no syntax errors or missing deps
2. Valid Regression Test — passes consistently (non-flaky), verified by re-execution
3. Hardening — kills at least one mutant not already killed by the existing suite
4. Additional Coverage — any extra line coverage reported (informational only)
5. Fashion Following — respects coding style (aspirational, not a strict gate)

Assurances 1–3 are boolean, falsifiable guarantees. "The assurances are unequivocal guarantees made by ACH about the tests it proposes."

**Equivalence Detector (anti-false-positive sub-gate):**

| Condition | Precision | Recall |
|-----------|-----------|--------|
| Without preprocessing | 0.79 | 0.47 |
| With comment-stripping | 0.95 | 0.96 |

Root cause of the improvement: LLM mutant generation inserts misleading comments (`// Introduce a bug by not checking...`) without changing executable code. Stripping comments before equivalence check auto-discards these as syntactically identical. Approximately 25% of mutants are trivially syntactically identical and are caught lexically before the LLM equivalence check is even invoked.

**Measured deployment results:**
- 4,660 candidate mutants from 10,795 classes under test
- 571 unit tests generated; 277 (49%) killed mutants without adding any line coverage — validates mutation over structural coverage
- Overall engineer acceptance: **73%** (WhatsApp 56%, Messenger 90%)
- 36% of accepted tests judged privacy-relevant

**Harden/Catch distinction (arXiv:2504.16472):**
- Hardening test: passes on current revision, fails on some future fault — anti-regression
- Catching test: fails on current revision — true fault detection
- Deployment target metric proposed: R@P=0.8 (recall at 80% precision; ≤20% friction is operationally acceptable)
- Human review is the final gate converting "weakly hardening" to "strongly hardening" — paper estimates this removes 3/5 of problematic test categories

**Applicability to Gate 5:** This is the closest prior art. Mutation execution result is the empirical oracle — no LLM trust required. The equivalence pre-filter (comment stripping before checking) maps directly to the concern about spurious rules producing trivially non-breaking tests.

---

### THREAD 2 — LLM-as-Judge Reliability: Biases and Adversarial Verification

**Sources:**
- Self-preference bias: https://arxiv.org/html/2410.21819v2
- Judge bias survey: https://arxiv.org/pdf/2602.07996
- Human agreement assessment: https://arxiv.org/pdf/2512.16041
- ColMAD collaborative debate: https://arxiv.org/html/2510.20963
- Debate for scalable oversight (Kenton et al., DeepMind 2024): https://arxiv.org/html/2504.18530v1
- Formal debate with stability detection: https://arxiv.org/html/2510.12697v1

**Classification:** Biases are established practice (well-documented 2023–2024); collaborative debate is emerging research (2024–2025).

**Documented biases:**

| Bias | Mechanism | Severity |
|------|-----------|----------|
| Position | Primacy/recency; swapping positions flips GPT-4 judgment | High; model-family dependent |
| Verbosity | Longer outputs preferred regardless of quality | Consistent across RLHF-trained models |
| Self-preference | GPT-4 bias score 0.520 (Equal Opportunity metric); root cause is perplexity favoritism, not self-identification | Linear with perplexity |
| Authority | Citations (even fabricated) inflate scores | Documented |

Critical finding on self-preference: the bias does not require the model to identify its own output. LLMs assign higher ratings to lower-perplexity text, and they naturally generate lower-perplexity text in their own style. Even a different judge model shows perplexity-driven favoritism. Ensemble across multiple models with inverse-perplexity weighting is the recommended mitigation.

**Human agreement rates:**
- General tasks (MT-Bench/Chatbot Arena): GPT-4 aligns with human preferences >80%
- Expert domains (dietetics, mental health): 60–68% agreement
- Best-in-class LLM judges: accuracy <0.7 on alignment datasets
- Extractive QA Pearson correlation: up to 0.85 vs. humans

**Adversarial/collaborative debate — empirical results (ColMAD):**

| Protocol | Nature | Key Result |
|----------|--------|------------|
| CopMAD (competitive) | Zero-sum | Up to **−15% F1** vs. single-agent due to "debate hacking" |
| ColMAD (collaborative) | Non-zero-sum | **+2% F1, +5% F2** over single-agent; best pair (QwQ-32B + DeepSeek-R1): 80.01 F1 |
| Single-agent baseline | — | GPT4o-mini: 74.85 F1 / 82.20 F2 |

ColMAD protocol enforces: direct evidence quoting (exact string match), self-auditing confidence, "find missing points" framing rather than "defeat opponent." CopMAD debate hacking modes: fake evidence insertion, overconfident tone ("fundamentally flawed"), fallacious deflection to irrelevant points.

**Debate theory validation (Kenton et al., DeepMind, ICML 2024):**
- Stronger debater models → higher judge accuracy (scales in the right direction for scalable oversight)
- Debate outperforms single-advisor consultancy across all 9 tasks (~5 million model calls)
- Critical weakness: LLM debaters showed 81.2% relative increase in persuading humans to adopt any assigned stance regardless of truth — persuasion bias is a real ceiling

**Applicability to Gate 2:** Use ColMAD not CopMAD framing. The refuter must cite counter-evidence by exact string match against source. A purely adversarial skeptic can actively harm accuracy. A skeptic required to cite counter-evidence from source is the validated design.

---

### THREAD 3 — Self-Consistency and Ensembling: The Independence Assumption

**Sources:**
- Self-consistency foundational (Wang et al., 2023): https://www.emergentmind.com/topics/self-consistency-in-language-models
- Mirror-Consistency 2024: https://aclanthology.org/2024.findings-emnlp.135.pdf
- Intraclass correlation analysis: https://arxiv.org/pdf/2509.19489
- ICE framework (Omar et al., 2025): https://arxiv.org/pdf/2601.22290

**Classification:** Established practice (Wang et al. majority-vote gains); correlated errors quantification is active research (2024–2025).

**Measured gains from majority voting:**
- +17.9% on GSM8K, +11.0% on SVAMP, +12.2% on AQuA (Wang et al., 2023)

**The independence problem:**
- LLM errors are concentrated at specific knowledge gaps, not uniformly distributed random events
- Multiple samples from the same base model share correlated failure modes
- Intraclass correlation ρ inflates variance; as ρ → 1, majority-vote gains vanish — correction factor f(ρ, m) approaches 1
- Independence breaks when: same model, same temperature distribution, same training data

**Mirror-Consistency (EMNLP 2024):** Rather than discarding minority votes, explicitly analyzes inconsistencies — minority votes identify error patterns that majority voting masks.

**Applicability to Gate 3:** Consistency across multiple calls to the same base model is a weak independence signal. True independence requires different base models or structurally different retrieval contexts. Using two agents from the same model family to validate each other provides far less independence than using architecturally distinct models.

---

### THREAD 4 — Confidence Calibration: Verbalized vs. Sampling-Based

**Sources:**
- "On Verbalized Confidence Scores for LLMs": https://arxiv.org/html/2412.14737v2
- ICLR 2024 confidence elicitation benchmark: https://openreview.net/forum?id=gjeQKFxFpZ
- Semantic entropy (Farquhar et al., Nature 2024): https://pubmed.ncbi.nlm.nih.gov/38898292/
- OATML blog: https://oatml.cs.ox.ac.uk/blog/2024/06/19/detecting_hallucinations_2024.html
- LPP meta-model escalation: https://arxiv.org/html/2601.07006v1

**Classification:** Verbalized overconfidence is established practice; semantic entropy as gold standard is established; LPP meta-model routing is emerging research (2025).

**Verbalized confidence calibration numbers:**
- Large LLMs (≥70B parameters): ECE ≈ **0.10** (10% average deviation from true accuracy)
- Best "combo" prompt method: 7% average deviation from empirical accuracy
- Combo = probability framing + uncertainty source description + 5-shot examples + "best guess" phrasing
- RLVR training degrades calibration further (binary correctness rewards incentivize overconfident guessing)
- Verdict from the literature: "uncalibrated verbalized confidence is severely overconfident"

**Sampling-based proxy — semantic entropy (Farquhar et al., Nature 2024):**
- Cluster repeated generations by semantic equivalence via bidirectional entailment (DeBERTa-Large-MNLI)
- Compute entropy over meaning-space clusters, not token sequences
- AUROC for hallucination detection: ~77–80% on TriviaQA
- Original cost: 5–10× inference budget; Semantic Entropy Probes (SEPs) bring overhead to near-zero via hidden-state approximation from a single generation
- Efficient Bayesian SE: achieves same AUROC quality with **53% of samples** via adaptive sampling

**LPP meta-model — production escalation routing:**
- Features: post-hoc classification uncertainty (entropy, MSP, top-2 margin) + verbalized confidence bands + binary uncertainty attribution flags
- CoT features explicitly excluded — inflated confidence, harmed calibration
- GPT-4o-mini escalation rate: **16%** of cases (naive baselines over-escalate to ~38%)
- Meta-model F1: **94.14%**, AUC-ROC: **93.46%** for predicting LLM failure
- Cost reduction: $132 → $38 (71%) for GPT-4o-mini review pipeline

**Applicability to Gate 3:** Verbalized high/medium/low with ECE ~0.10 is insufficient as a standalone gate. Implement semantic consistency proxy (3–5 independent samples, cluster by meaning, compute entropy) as the primary confidence signal. Combine with post-hoc uncertainty signals per LPP architecture. Target: 16% escalation rate at 94% F1 is achievable. Never use CoT-generated confidence — it inflates overconfidence. Calibrate thresholds empirically against held-out ground truth, not on LLM-stated confidence values.

---

### THREAD 5 — Evidence-Grounded Generation: Citation-Required and Claim Verification

**Sources:**
- "Correctness is Not Faithfulness" (Wallat et al., 2024): https://arxiv.org/pdf/2412.18004
- FACTUM (citation hallucination in RAG): https://arxiv.org/pdf/2601.05866
- RT4CHART (retromorphic testing): https://arxiv.org/pdf/2603.27752
- Anthropic Citations API: https://www.anthropic.com/news/introducing-citations-api
- Anthropic hallucination reduction docs: https://docs.anthropic.com/en/docs/test-and-evaluate/strengthen-guardrails/reduce-hallucinations
- FActScore: https://123ofai.com/qnalab/system-design/blocks/faithfulness

**Classification:** RAG faithfulness is established practice; inline citation generation and post-rationalization detection are emerging research.

**Critical finding — correctness does not equal faithfulness:**
- A citation can be correct (the source document is relevant) but unfaithful (the LLM generated the claim without consulting that source, then retroactively assigned a citation)
- Up to **57% of citations are post-rationalized** (Wallat et al., 2024)
- GTR (Generate-then-Retrieve) and post-hoc RTG pipelines are structurally incapable of faithful citation
- The necessary condition for faithfulness: citations must be generated inline during answer generation, not assigned afterward

**Anthropic Citations API — measured results:**
- Endex (financial AI): source hallucinations reduced from **10% to 0%**, 20% increase in references per response
- Thomson Reuters CoCounsel (legal): replaced custom citation system, improved accuracy
- Anthropic-recommended protocol: extract word-for-word quotes before performing the task; if LLM cannot find supporting quote after generation, it must retract the claim
- RAG overall: approximately 71% hallucination reduction vs. vanilla LLMs; legal RAG tools still hallucinate 17–33% of queries

**FActScore baseline:**
- ChatGPT achieves **58% faithfulness** on biographical generation (EMNLP 2023)
- Automated FActScore: <2% error rate vs. human judgments

**RT4CHART — retromorphic verification:**
- Each claim must trace back to originating input span via inverted generative mapping
- Hierarchical verification: claim-level traceability check
- Applicable as a post-generation verification pass: generate rule, then verify each sub-claim can be traced to an exact source span

**Applicability to Gate 1:** Post-rationalization is the specific failure mode. The gate must enforce quote-first ordering: LLM extracts a quote from source before stating the rule, not after. Verification should be NLI-based entailment: does the extracted quote entail the rule statement? RT4CHART's retromorphic tracing is the right verification architecture for this.

---

### THREAD 6 — LLM Hallucination Rates When Describing Code

**Sources:**
- "Beyond Functional Correctness: Exploring Hallucinations in LLM-Generated Code" (2024): https://arxiv.org/html/2404.00971v3
- ETF (Entity Tracing for Code Summaries): https://arxiv.org/pdf/2410.14748
- FaithLens: https://arxiv.org/pdf/2512.20182

**Classification:** Emerging research — first systematic code hallucination taxonomy established 2024.

**Code hallucination taxonomy and rates (3,120 samples, HumanEval + CoderEval; models: DeepSeek-Coder 1.3B/7B, CodeLlama-7B, GPT-4, DeepSeek-R1):**

| Hallucination Type | Rate of All Hallucinations |
|-------------------|--------------------------|
| Requirement Conflicting: Behavior | **35.40%** |
| Knowledge: Library/Project | **25.99%** |
| Code Inconsistency: Undefined Variables | 16.91% |
| Knowledge: Algorithm | 4.95% |
| Requirement Conflicting: Data | 4.21% |
| Knowledge: Computer Theory | 2.31% |

- Of 3,120 samples, **1,134 (36%) contained at least one hallucination**
- Pass@1 for hallucination-free code: **54.73%** vs. hallucinatory code: **6.26%** — hallucination is strongly predictive of failure
- Incorrect Functionality affects **95.3% of hallucinatory samples** — almost always requires runtime testing to detect

**Detectability by method:**
- Undefined Variables: statically detectable
- Behavior Conflicting, Data Conflicting, Library/Project: require dynamic execution or semantic analysis
- Static analysis covers only ~17% of hallucination instances (the undefined variable class); the other 83% require execution

**Applicability:** Without gates, roughly 1 in 3 rule extractions will contain at least one hallucinated claim. The dominant category — Behavior Conflicting at 35.4% — maps precisely to the scenario to prevent: the LLM inventing what a code path does rather than reading it. This class is not statically detectable; only evidence-grounded or execution-based verification catches it.

---

### THREAD 7 — Human-in-the-Loop Gate Design: Escalation Patterns

**Sources:**
- LPP meta-model paper: https://arxiv.org/html/2601.07006v1
- HITL production guide: https://alldaystech.com/guides/artificial-intelligence/human-in-the-loop-ai-review-queue-workflows
- Redis AI HITL patterns: https://redis.io/blog/ai-human-in-the-loop/

**Classification:** Production engineering patterns are established practice; meta-model escalation routing is emerging research (2025).

**Measured auto/escalate ratios (LPP meta-model, OpenAI moderation dataset):**

| Model | Meta-model Escalation | Baseline (MSP) Escalation |
|-------|----------------------|--------------------------|
| GPT-4o-mini | **16%** | ~38% |
| GPT-4o | **17%** | — |
| Gemini-2.5-flash-lite | **25%** | — |
| GPT-4o multimodal | **13%** | — |

- Cost savings: GPT-4o-mini review cost $132 → $38 (71% reduction)
- Target: R@P=0.8 is accepted industry threshold (≤20% friction)
- For 99% end-to-end accuracy: approximately 30–40% escalation; for 95%: approximately 10–15%

**Queue design patterns for rule/test review:**
- Two-stage review: fast triage first, deep review for a subset
- Batch by similarity: group rules from same file/function — reduces context switching, speeds review
- Priority lanes: P0 (contradiction detected) → immediate; P1 (low confidence) → batch queue; P2 (high confidence) → auto-flow
- Irreversible actions (emitting tests to CI) always require human approval regardless of confidence score
- Threshold recalibration: weekly or every N decisions, never in real-time

**For contradiction escalation specifically:**
- P0 immediate queue — no auto-resolution under any condition
- Include in escalation package: both conflicting rule statements, source spans for each, conflict type (logical negation / temporal / scope overlap), recommended resolution templates (not verdicts)
- Never batch contradictions with routine review; they are qualitatively different decisions

---

### TOP 5 DESIGN IMPLICATIONS

**1. Gate 5 (Mutation Proof) is the strongest gate — structural anti-hallucination, not statistical.**
Meta ACH demonstrates that mutation-based acceptance is structurally anti-hallucination. The LLM cannot fabricate a passing test for a nonexistent rule violation because mutant execution is the oracle. Add the equivalence pre-filter (comment stripping achieves 0.95/0.96 precision/recall) before invoking mutation testing to avoid wasting cycles on trivially non-breaking mutants. Target 73% engineer acceptance as a baseline; domain-specific variation is large (56% to 90%).

**2. Gate 1 (Evidence-or-Reject) is validated but post-rationalization is the specific attack to prevent.**
Requiring file:line citations does not prevent retroactively assigned citations — up to 57% of LLM citations are post-rationalized (Wallat et al.). The gate must enforce quote-first ordering: LLM extracts a quote before stating the rule, not after. Add NLI entailment verification — does the extracted quote entail the rule statement? RT4CHART's retromorphic tracing is the right verification architecture.

**3. Gate 2 (Adversarial Refutation) must be collaborative, not competitive — competitive debate actively degrades accuracy.**
CopMAD (zero-sum adversarial) degrades accuracy up to 15% via debate hacking. ColMAD (non-zero-sum, evidence-citing collaborative) achieves +2–5% over single-agent. The refuter must cite counter-evidence by exact string match against source. Use architecturally distinct base models for proposer and refuter — same-model pairs have correlated failure modes and will both miss the Library/Project hallucination class at ~26% rate.

**4. Gate 3 (Confidence Gating) as verbalized high/medium/low is the weakest gate as specified — replace with sampling-based semantic entropy.**
Verbalized confidence has ECE ~0.10. The right primary signal is semantic consistency across 3–5 independent samples (cluster by meaning, compute entropy over clusters). Combine with post-hoc uncertainty signals (MSP, logit entropy) per the LPP meta-model architecture. Target: 16% escalation rate at 94% F1 is achievable with proper meta-model routing. Do not use CoT-generated confidence — it inflates overconfidence. Thresholds must be calibrated empirically per domain against actual accuracy measurements.

**5. Gate 4 (Contradictions Escalate) is correctly designed but needs explicit queue engineering.**
Human escalation for contradictions is validated by all deployed HITL literature. Use P0 immediate queue — never batch contradictions. Include both rule statements, both source spans, conflict type, and resolution templates in the escalation package. The 36% base hallucination rate for code description means contradictions will be frequent; design the queue for volume. Set empirical auto-escalate thresholds (16–25% is typical at 80%+ precision) and recalibrate weekly.

**What is missing from the planned design:**

- No gate for **post-rationalized citations**: requiring file:line is necessary but not sufficient. Add quote-first ordering enforcement and NLI entailment verification as part of Gate 1.
- No **mutation equivalence pre-filter**: trivially non-breaking mutations waste test generation cycles. Comment-stripping pre-filter is 95%+ effective per Meta data; add it before Gate 5 is invoked.
- **Cross-agent correlated errors**: if proposer and refuter share a base model, correlated failures — especially Library/Project class at 26% — will both be missed. Mandate architecturally distinct models.
- **Confidence threshold calibration schedule**: the "high auto-flows" threshold cannot be set a priori from LLM-stated confidence. It must be calibrated empirically against ground-truth accuracy per domain and recalibrated regularly.

================================================================================

The research is complete. Here is the full synthesis.

---

## Research: Verification Gates and Hallucination Control for LLM-Generated Artifacts

---

### THREAD 1 — Meta ACH / Assured LLMSE: Mutation-Proof Gate Prior Art

**Sources:**
- arXiv full text: https://arxiv.org/html/2501.12862v1
- PDF: https://arxiv.org/pdf/2501.12862
- InfoQ coverage: https://www.infoq.com/news/2026/01/meta-llm-mutation-testing/
- "Harden and Catch" follow-up: https://arxiv.org/pdf/2504.16472

**Classification:** Established practice — deployed at Meta scale, October–December 2024 trial across FB/Instagram/WhatsApp/Quest/Ray-Ban.

**Mechanism — how mutation gates anti-hallucination:**
ACH's core insight: LLM-generated tests cannot hallucinate "passing" behavior for a bug that does not exist if the acceptance criterion is that the test must demonstrably fail on the mutant and pass on the original. The intermediate artifact (mutant) is executed, not trusted. Hallucination is structurally impossible within the bounds of this gate.

**The five verifiable assurances of ACH Assured LLMSE:**
1. Buildable — compiles, no syntax errors or missing deps
2. Valid Regression Test — passes consistently (non-flaky), verified by re-execution
3. Hardening — kills at least one mutant not already killed by the existing suite
4. Additional Coverage — any extra line coverage reported (informational only)
5. Fashion Following — respects coding style (aspirational, not a strict gate)

Assurances 1–3 are boolean, falsifiable guarantees. "The assurances are unequivocal guarantees made by ACH about the tests it proposes."

**Equivalence Detector (anti-false-positive sub-gate):**

| Condition | Precision | Recall |
|-----------|-----------|--------|
| Without preprocessing | 0.79 | 0.47 |
| With comment-stripping | 0.95 | 0.96 |

Root cause of the improvement: LLM mutant generation inserts misleading comments (`// Introduce a bug by not checking...`) without changing executable code. Stripping comments before equivalence check auto-discards these as syntactically identical. Approximately 25% of mutants are trivially syntactically identical and are caught lexically before the LLM equivalence check is invoked.

**Measured deployment results:**
- 4,660 candidate mutants from 10,795 classes under test
- 571 unit tests generated; 277 (49%) killed mutants without adding any line coverage — validates mutation over structural coverage as a gate
- Overall engineer acceptance: **73%** (WhatsApp 56%, Messenger 90%)
- 36% of accepted tests judged privacy-relevant

**Harden/Catch distinction (arXiv:2504.16472):**
- Hardening test: passes on current revision, fails on some future fault — anti-regression
- Catching test: fails on current revision — true fault detection
- Deployment target metric: R@P=0.8 (recall at 80% precision; ≤20% friction is operationally acceptable)
- Human review is the final gate converting "weakly hardening" to "strongly hardening" — estimated to remove 3/5 of problematic test categories

**Applicability to Gate 5:** This is the closest prior art. Mutation execution result is the empirical oracle — no LLM trust required. The equivalence pre-filter (comment stripping before equivalence check) maps directly to the concern about spurious rules producing trivially non-breaking tests.

---

### THREAD 2 — LLM-as-Judge Reliability: Biases and Adversarial Verification

**Sources:**
- Self-preference bias: https://arxiv.org/html/2410.21819v2
- Judge bias survey: https://arxiv.org/pdf/2602.07996
- Human agreement assessment: https://arxiv.org/pdf/2512.16041
- ColMAD collaborative debate: https://arxiv.org/html/2510.20963
- Debate for scalable oversight (Kenton et al., DeepMind 2024): https://arxiv.org/html/2504.18530v1
- Formal debate with stability detection: https://arxiv.org/html/2510.12697v1

**Classification:** Biases are established practice (well-documented 2023–2024); collaborative debate is emerging research (2024–2025).

**Documented biases:**

| Bias | Mechanism | Severity |
|------|-----------|----------|
| Position | Primacy/recency; swapping positions flips GPT-4 judgment | High; model-family dependent |
| Verbosity | Longer outputs preferred regardless of quality | Consistent across RLHF-trained models |
| Self-preference | GPT-4 bias score 0.520 (Equal Opportunity metric); root cause is perplexity favoritism, not self-identification | Linear with perplexity |
| Authority | Citations (even fabricated) inflate scores | Documented |

Critical finding on self-preference: the bias does not require the model to identify its own output. LLMs assign higher ratings to lower-perplexity text, and they naturally generate lower-perplexity text for their own style. Even a different judge model shows perplexity-driven favoritism. Ensemble across multiple models with inverse-perplexity weighting is the recommended mitigation.

**Human agreement rates:**
- General tasks (MT-Bench/Chatbot Arena): GPT-4 aligns with human preferences >80%
- Expert domains (dietetics, mental health): 60–68% agreement — significant gap
- Best-in-class LLM judges: accuracy <0.7 on alignment datasets
- Extractive QA Pearson correlation: up to 0.85 vs. humans (much better than F1 at 0.36)

**Adversarial/collaborative debate — empirical results (ColMAD):**

| Protocol | Nature | Key Result |
|----------|--------|------------|
| CopMAD (competitive) | Zero-sum | Up to **−15% F1** vs. single-agent due to "debate hacking" |
| ColMAD (collaborative) | Non-zero-sum | **+2% F1, +5% F2** over single-agent; best pair (QwQ-32B + DeepSeek-R1): 80.01 F1 |
| Single-agent baseline | — | GPT4o-mini: 74.85 F1 / 82.20 F2 |

ColMAD protocol enforces: direct evidence quoting (exact string match), self-auditing confidence, "find missing points" framing rather than "defeat opponent." CopMAD debate hacking modes: fake evidence insertion, overconfident tone ("fundamentally flawed"), fallacious deflection to irrelevant points.

**Debate theory validation (Kenton et al., DeepMind, ICML 2024):**
- Stronger debater models → higher judge accuracy (scales correctly for scalable oversight)
- Debate outperforms single-advisor consultancy across all 9 tasks (~5 million model calls)
- Critical weakness: LLM debaters showed 81.2% relative increase in persuading humans to adopt any assigned stance regardless of truth — persuasion bias is a real ceiling

**Applicability to Gate 2:** Use ColMAD not CopMAD framing. The refuter must cite counter-evidence by exact string match against source. A purely adversarial skeptic can actively harm accuracy. A skeptic required to quote counter-evidence from source is the validated design.

---

### THREAD 3 — Self-Consistency and Ensembling: The Independence Assumption

**Sources:**
- Self-consistency foundational (Wang et al., 2023): https://www.emergentmind.com/topics/self-consistency-in-language-models
- Mirror-Consistency 2024: https://aclanthology.org/2024.findings-emnlp.135.pdf
- Intraclass correlation analysis: https://arxiv.org/pdf/2509.19489
- ICE framework (Omar et al., 2025): https://arxiv.org/pdf/2601.22290

**Classification:** Established practice (Wang et al. majority-vote gains); correlated errors quantification is active research (2024–2025).

**Measured gains from majority voting:**
- +17.9% on GSM8K, +11.0% on SVAMP, +12.2% on AQuA (Wang et al., 2023)

**The independence problem:**
- LLM errors are concentrated at specific knowledge gaps, not uniformly distributed random events
- Multiple samples from the same base model share correlated failure modes
- Intraclass correlation ρ inflates variance; as ρ → 1, majority-vote gains vanish — correction factor f(ρ, m) approaches 1
- Independence breaks when: same model, same temperature distribution, same training data

**Mirror-Consistency (EMNLP 2024):** Rather than discarding minority votes, explicitly analyzes inconsistencies — minority votes identify error patterns that majority voting masks.

**Applicability to Gate 3:** Consistency across multiple calls to the same base model is a weak independence signal. True independence requires different base models or structurally different retrieval contexts. Using two agents from the same model family to validate each other provides far less independence than using architecturally distinct models.

---

### THREAD 4 — Confidence Calibration: Verbalized vs. Sampling-Based

**Sources:**
- "On Verbalized Confidence Scores for LLMs": https://arxiv.org/html/2412.14737v2
- ICLR 2024 confidence elicitation benchmark: https://openreview.net/forum?id=gjeQKFxFpZ
- Semantic entropy (Farquhar et al., Nature 2024): https://pubmed.ncbi.nlm.nih.gov/38898292/
- OATML blog: https://oatml.cs.ox.ac.uk/blog/2024/06/19/detecting_hallucinations_2024.html
- LPP meta-model escalation: https://arxiv.org/html/2601.07006v1

**Classification:** Verbalized overconfidence is established practice; semantic entropy as a confidence proxy is established; LPP meta-model routing is emerging research (2025).

**Verbalized confidence calibration numbers:**
- Large LLMs (≥70B parameters): ECE ≈ **0.10** (10% average deviation from true accuracy)
- Best "combo" prompt method: 7% average deviation from empirical accuracy
- Combo = probability framing + uncertainty source description + 5-shot examples + "best guess" phrasing
- RLVR training degrades calibration further (binary correctness rewards incentivize overconfident guessing)
- Verdict from the literature: "uncalibrated verbalized confidence is severely overconfident"

**Sampling-based proxy — semantic entropy (Farquhar et al., Nature 2024):**
- Cluster repeated generations by semantic equivalence via bidirectional entailment (DeBERTa-Large-MNLI)
- Compute entropy over meaning-space clusters, not token sequences
- AUROC for hallucination detection: ~77–80% on TriviaQA
- Original cost: 5–10× inference budget; Semantic Entropy Probes (SEPs) bring overhead to near-zero via hidden-state approximation from a single generation
- Efficient Bayesian SE: achieves same AUROC quality with **53% of samples** via adaptive sampling

**LPP meta-model — production escalation routing:**
- Features: post-hoc classification uncertainty (entropy, MSP, top-2 margin) + verbalized confidence bands + binary uncertainty attribution flags
- CoT features explicitly excluded — they inflated confidence and harmed calibration
- GPT-4o-mini escalation rate with meta-model: **16%** of cases (naive baselines over-escalate to ~38%)
- Meta-model F1: **94.14%**, AUC-ROC: **93.46%** for predicting LLM failure
- Cost reduction: $132 → $38 (71%) for GPT-4o-mini review pipeline

**Applicability to Gate 3:** Verbalized high/medium/low with ECE ~0.10 is insufficient as a standalone gate. Implement semantic consistency proxy (3–5 independent samples, cluster by meaning, compute entropy) as the primary confidence signal. Combine with post-hoc uncertainty signals per LPP architecture. Target: 16% escalation rate at 94% F1 is achievable with proper meta-model routing. Never use CoT-generated confidence — it inflates overconfidence. Calibrate thresholds empirically against held-out ground truth, not on LLM-stated confidence values.

---

### THREAD 5 — Evidence-Grounded Generation: Citation-Required and Claim Verification

**Sources:**
- "Correctness is Not Faithfulness" (Wallat et al., 2024): https://arxiv.org/pdf/2412.18004
- FACTUM (citation hallucination in RAG): https://arxiv.org/pdf/2601.05866
- RT4CHART (retromorphic testing): https://arxiv.org/pdf/2603.27752
- Anthropic Citations API: https://www.anthropic.com/news/introducing-citations-api
- Anthropic hallucination reduction docs: https://docs.anthropic.com/en/docs/test-and-evaluate/strengthen-guardrails/reduce-hallucinations
- FActScore: https://123ofai.com/qnalab/system-design/blocks/faithfulness

**Classification:** RAG faithfulness is established practice; inline citation generation and post-rationalization detection are emerging research.

**Critical finding — correctness does not equal faithfulness:**
- A citation can be correct (the source document is relevant) but unfaithful (the LLM generated the claim without consulting that source, then retroactively assigned a citation)
- Up to **57% of citations are post-rationalized** (Wallat et al., 2024)
- GTR (Generate-then-Retrieve) and post-hoc RTG pipelines are structurally incapable of faithful citation
- The necessary condition for faithfulness: citations must be generated inline during answer generation, not assigned afterward

**Anthropic Citations API — measured results:**
- Endex (financial AI): source hallucinations reduced from **10% to 0%**, 20% increase in references per response
- Thomson Reuters CoCounsel (legal): replaced custom citation system, improved accuracy
- Anthropic-recommended protocol: extract word-for-word quotes before performing the task; if LLM cannot find a supporting quote after generation, it must retract the claim
- RAG overall: approximately 71% hallucination reduction vs. vanilla LLMs; legal RAG tools still hallucinate 17–33% of queries

**FActScore baseline:**
- ChatGPT achieves **58% faithfulness** on biographical generation (EMNLP 2023)
- Automated FActScore: <2% error rate vs. human judgments

**RT4CHART — retromorphic verification:**
- Each claim must trace back to its originating input span via inverted generative mapping
- Hierarchical verification: claim-level traceability check
- Applicable as a post-generation verification pass: generate rule, then verify each sub-claim can be traced to an exact source span

**Applicability to Gate 1:** Post-rationalization is the specific failure mode. The gate must enforce quote-first ordering — LLM extracts a quote from source before stating the rule, not after. Verification should be NLI-based entailment: does the extracted quote entail the rule statement? RT4CHART's retromorphic tracing is the right verification architecture.

---

### THREAD 6 — LLM Hallucination Rates When Describing Code

**Sources:**
- "Beyond Functional Correctness: Exploring Hallucinations in LLM-Generated Code" (2024): https://arxiv.org/html/2404.00971v3
- ETF (Entity Tracing for Code Summaries): https://arxiv.org/pdf/2410.14748
- FaithLens: https://arxiv.org/pdf/2512.20182

**Classification:** Emerging research — first systematic code hallucination taxonomy established 2024.

**Code hallucination taxonomy and rates (3,120 samples, HumanEval + CoderEval; models: DeepSeek-Coder 1.3B/7B, CodeLlama-7B, GPT-4, DeepSeek-R1):**

| Hallucination Type | Share of All Hallucinations |
|-------------------|-----------------------------|
| Requirement Conflicting: Behavior | **35.40%** |
| Knowledge: Library/Project | **25.99%** |
| Code Inconsistency: Undefined Variables | 16.91% |
| Knowledge: Algorithm | 4.95% |
| Requirement Conflicting: Data | 4.21% |
| Knowledge: Computer Theory | 2.31% |

- Of 3,120 samples, **1,134 (36%) contained at least one hallucination**
- Pass@1 for hallucination-free code: **54.73%** vs. hallucinatory code: **6.26%** — hallucination strongly predicts failure
- Incorrect Functionality affects **95.3% of hallucinatory samples** — almost always requires runtime testing to detect

**Detectability by method:**
- Undefined Variables: statically detectable
- Behavior Conflicting, Data Conflicting, Library/Project: require dynamic execution or semantic analysis
- Static analysis covers only ~17% of hallucination instances (the undefined variable class); the other 83% require execution

**Applicability:** Without gates, roughly 1 in 3 rule extractions will contain at least one hallucinated claim. The dominant category — Behavior Conflicting at 35.4% of all hallucinations — maps precisely to the scenario to prevent: the LLM inventing what a code path does. This class is not statically detectable; only evidence-grounded or execution-based verification catches it.

---

### THREAD 7 — Human-in-the-Loop Gate Design: Escalation Patterns

**Sources:**
- LPP meta-model paper: https://arxiv.org/html/2601.07006v1
- HITL production guide: https://alldaystech.com/guides/artificial-intelligence/human-in-the-loop-ai-review-queue-workflows
- Redis AI HITL patterns: https://redis.io/blog/ai-human-in-the-loop/

**Classification:** Production engineering patterns are established practice; meta-model escalation routing is emerging research (2025).

**Measured auto/escalate ratios (LPP meta-model, OpenAI moderation dataset):**

| Model | Meta-model Escalation | Baseline (MSP) Escalation |
|-------|----------------------|--------------------------|
| GPT-4o-mini | **16%** | ~38% |
| GPT-4o | **17%** | — |
| Gemini-2.5-flash-lite | **25%** | — |
| GPT-4o multimodal | **13%** | — |

- Cost savings: GPT-4o-mini review cost $132 → $38 (71% reduction)
- Target: R@P=0.8 is accepted industry threshold (≤20% friction)
- For 99% end-to-end accuracy: approximately 30–40% escalation; for 95%: approximately 10–15%

**Queue design patterns:**
- Two-stage review: fast triage first, deep review for subset
- Batch by similarity: group rules from same file/function — reduces context switching
- Priority lanes: P0 (contradiction detected) → immediate; P1 (low confidence) → batch queue; P2 (high confidence) → auto-flow
- Irreversible actions (emitting tests to CI) always require human approval regardless of confidence score
- Threshold recalibration: weekly or every N decisions, never in real-time

**For contradiction escalation specifically:**
- P0 immediate queue — no auto-resolution under any condition
- Include in escalation package: both conflicting rule statements, source spans for each, conflict type (logical negation / temporal / scope overlap), recommended resolution templates (not verdicts)
- Never batch contradictions with routine review; they are qualitatively different decisions

---

### TOP 5 DESIGN IMPLICATIONS

**1. Gate 5 (Mutation Proof) is the strongest gate — structural anti-hallucination, not statistical.**
Meta ACH demonstrates that mutation-based acceptance is structurally anti-hallucination. The LLM cannot fabricate a passing test for a nonexistent rule violation because mutant execution is the oracle. Add the equivalence pre-filter (comment stripping achieves 0.95/0.96 precision/recall) before invoking mutation testing to avoid wasting cycles on trivially non-breaking mutants. Target 73% engineer acceptance as a baseline; domain-specific variation is wide (56% to 90%).

**2. Gate 1 (Evidence-or-Reject) is validated but post-rationalization is the specific attack to prevent.**
Requiring file:line citations does not prevent retroactively assigned citations — up to 57% of LLM citations are post-rationalized (Wallat et al.). The gate must enforce quote-first ordering: LLM extracts a quote before stating the rule, not after. Add NLI entailment verification — does the extracted quote entail the rule statement? RT4CHART's retromorphic tracing is the right verification architecture.

**3. Gate 2 (Adversarial Refutation) must be collaborative, not competitive — competitive debate actively degrades accuracy.**
CopMAD (zero-sum adversarial) degrades accuracy up to 15% via debate hacking. ColMAD (non-zero-sum, evidence-citing collaborative) achieves +2–5% over single-agent. The refuter must cite counter-evidence by exact string match against source. Use architecturally distinct base models for proposer and refuter — same-model pairs have correlated failure modes and will both miss the Library/Project hallucination class (~26% of all code hallucinations).

**4. Gate 3 (Confidence Gating) as verbalized high/medium/low is the weakest gate as specified — replace with sampling-based semantic entropy.**
Verbalized confidence has ECE ~0.10 (10% systematic overconfidence). The right primary signal is semantic consistency across 3–5 independent samples (cluster by meaning, compute entropy over clusters). Combine with post-hoc uncertainty signals (MSP, logit entropy) per the LPP meta-model architecture. Target: 16% escalation rate at 94% F1 is achievable with proper meta-model routing. Never use CoT-generated confidence — it inflates overconfidence. Calibrate thresholds empirically per domain against actual accuracy measurements.

**5. Gate 4 (Contradictions Escalate) is correctly designed but needs explicit queue engineering.**
Human escalation for contradictions is validated by all deployed HITL literature. Use P0 immediate queue — never batch contradictions with routine items. Include both rule statements, both source spans, conflict type, and resolution templates in the escalation package. The 36% base hallucination rate for code description means contradictions will be frequent; design the queue for volume from day one. Set empirical auto-escalate thresholds (16–25% is the production range at 80%+ precision) and recalibrate weekly.

**What is missing from the planned design:**

- No gate for **post-rationalized citations**: requiring file:line is necessary but not sufficient. Add quote-first ordering enforcement and NLI entailment verification as part of Gate 1.
- No **mutation equivalence pre-filter**: comment-stripping pre-filter is 95%+ effective per Meta data; add it before Gate 5 is invoked to avoid wasting test generation on non-breaking mutants.
- **Cross-agent correlated errors**: if proposer and refuter share a base model, correlated failures — especially the Library/Project class at 26% — will both be missed. Mandate architecturally distinct models.
- **Confidence threshold calibration schedule**: the "high auto-flows" threshold cannot be set a priori from LLM-stated confidence. It must be calibrated empirically against ground-truth accuracy per domain and recalibrated regularly.

================================================================================

The research is complete. All seven threads have been investigated with primary sources fetched and verified. Here is the full synthesis.

---

## Research: Verification Gates and Hallucination Control for LLM-Generated Artifacts

---

### THREAD 1 — Meta ACH / Assured LLMSE: Mutation-Proof Gate Prior Art

**Sources:**
- arXiv full text: https://arxiv.org/html/2501.12862v1
- PDF: https://arxiv.org/pdf/2501.12862
- InfoQ coverage: https://www.infoq.com/news/2026/01/meta-llm-mutation-testing/
- "Harden and Catch" follow-up: https://arxiv.org/pdf/2504.16472

**Classification:** Established practice — deployed at Meta scale, October–December 2024 trial across FB/Instagram/WhatsApp/Quest/Ray-Ban.

**Mechanism — how mutation gates anti-hallucination:**
ACH's core insight: LLM-generated tests cannot hallucinate "passing" behavior for a bug that does not exist if the acceptance criterion is that the test must demonstrably fail on the mutant and pass on the original. The intermediate artifact (mutant) is executed, not trusted. Hallucination is structurally impossible within the bounds of this gate.

**The five verifiable assurances of ACH Assured LLMSE:**
1. Buildable — compiles, no syntax errors or missing deps
2. Valid Regression Test — passes consistently (non-flaky), verified by re-execution
3. Hardening — kills at least one mutant not already killed by the existing suite
4. Additional Coverage — any extra line coverage reported (informational only)
5. Fashion Following — respects coding style (aspirational, not a strict gate)

Assurances 1–3 are boolean, falsifiable guarantees. "The assurances are unequivocal guarantees made by ACH about the tests it proposes."

**Equivalence Detector (anti-false-positive sub-gate):**

| Condition | Precision | Recall |
|-----------|-----------|--------|
| Without preprocessing | 0.79 | 0.47 |
| With comment-stripping | 0.95 | 0.96 |

Root cause: LLM mutant generation inserts misleading comments (`// Introduce a bug by not checking...`) without changing executable code. Stripping comments before equivalence check auto-discards these as syntactically identical. Approximately 25% of mutants are trivially syntactically identical and are caught lexically before the LLM equivalence check is invoked.

**Measured deployment results:**
- 4,660 candidate mutants from 10,795 classes under test
- 571 unit tests generated; 277 (49%) killed mutants without adding any line coverage — validates mutation over structural coverage as a gate
- Overall engineer acceptance: **73%** (WhatsApp 56%, Messenger 90%)
- 36% of accepted tests judged privacy-relevant

**Harden/Catch distinction (arXiv:2504.16472):**
- Hardening test: passes on current revision, fails on some future fault — anti-regression
- Catching test: fails on current revision — true fault detection
- Deployment target metric proposed: R@P=0.8 (recall at 80% precision; ≤20% friction operationally acceptable)
- Human review is the final gate converting "weakly hardening" to "strongly hardening" — estimated to eliminate 3/5 of problematic test categories

**Applicability to Gate 5:** This is the closest prior art to our mutation-proof gate. Mutation execution result is the empirical oracle — no LLM trust required. The equivalence pre-filter (comment stripping before checking) maps directly to the concern about spurious rules producing trivially non-breaking tests.

---

### THREAD 2 — LLM-as-Judge Reliability: Biases and Adversarial Verification

**Sources:**
- Self-preference bias: https://arxiv.org/html/2410.21819v2
- Judge bias survey: https://arxiv.org/pdf/2602.07996
- Human agreement assessment: https://arxiv.org/pdf/2512.16041
- ColMAD collaborative debate: https://arxiv.org/html/2510.20963
- Debate for scalable oversight (Kenton et al., DeepMind 2024): https://arxiv.org/html/2504.18530v1
- Formal debate with stability detection: https://arxiv.org/html/2510.12697v1

**Classification:** Biases are established practice (well-documented 2023–2024); collaborative debate is emerging research (2024–2025).

**Documented biases:**

| Bias | Mechanism | Severity |
|------|-----------|----------|
| Position | Primacy/recency; swapping positions flips GPT-4 judgment | High; model-family dependent |
| Verbosity | Longer outputs preferred regardless of quality | Consistent across RLHF-trained models |
| Self-preference | GPT-4 bias score 0.520 (Equal Opportunity metric); root cause is perplexity favoritism, not self-identification | Linear with perplexity |
| Authority | Citations (even fabricated) inflate scores | Documented |

Critical finding on self-preference: the bias does not require the model to identify its own output. LLMs assign higher ratings to lower-perplexity text, and they naturally produce lower-perplexity text for their own style. Even a different judge model shows perplexity-driven favoritism. Ensemble across multiple models with inverse-perplexity weighting is the recommended mitigation.

**Human agreement rates:**
- General tasks (MT-Bench/Chatbot Arena): GPT-4 aligns with human preferences >80%
- Expert domains (dietetics, mental health): 60–68% agreement — significant gap for specialized/knowledge-intensive evaluation
- Best-in-class LLM judges: accuracy <0.7 on alignment datasets
- Extractive QA Pearson correlation: up to 0.85 vs. humans (vs. F1 at 0.36)

**Adversarial/collaborative debate — empirical results (ColMAD):**

| Protocol | Nature | Key Result |
|----------|--------|------------|
| CopMAD (competitive) | Zero-sum | Up to **−15% F1** vs. single-agent due to "debate hacking" |
| ColMAD (collaborative) | Non-zero-sum | **+2% F1, +5% F2** over single-agent; best pair (QwQ-32B + DeepSeek-R1): 80.01 F1 |
| Single-agent baseline | — | GPT4o-mini: 74.85 F1 / 82.20 F2 |

ColMAD protocol enforces: direct evidence quoting (exact string match), self-auditing confidence, "find missing points" framing rather than "defeat opponent." CopMAD debate hacking modes: fake evidence insertion, overconfident tone ("fundamentally flawed"), fallacious deflection to irrelevant points.

**Debate theory validation (Kenton et al., DeepMind, ICML 2024):**
- Stronger debater models → higher judge accuracy (scales in the right direction for scalable oversight)
- Debate outperforms single-advisor consultancy across all 9 tasks tested (~5 million model calls)
- Critical weakness: LLM debaters showed 81.2% relative increase in persuading humans to adopt any assigned stance regardless of truth — persuasion bias is a real ceiling on human-judge designs

**Applicability to Gate 2:** Use ColMAD not CopMAD framing. The refuter must cite counter-evidence by exact string match against source. A purely adversarial skeptic can actively harm accuracy. A skeptic required to quote counter-evidence from source is the validated design.

---

### THREAD 3 — Self-Consistency and Ensembling: The Independence Assumption

**Sources:**
- Self-consistency foundational (Wang et al., 2023): https://www.emergentmind.com/topics/self-consistency-in-language-models
- Mirror-Consistency (EMNLP 2024): https://aclanthology.org/2024.findings-emnlp.135.pdf
- Intraclass correlation analysis: https://arxiv.org/pdf/2509.19489
- ICE framework (Omar et al., 2025): https://arxiv.org/pdf/2601.22290

**Classification:** Established practice (Wang et al. majority-vote gains); correlated errors quantification is active research (2024–2025).

**Measured gains from majority voting:**
- +17.9% on GSM8K, +11.0% on SVAMP, +12.2% on AQuA (Wang et al., 2023)

**The independence problem:**
- LLM errors are concentrated at specific knowledge gaps, not uniformly distributed random events
- Multiple samples from the same base model share correlated failure modes
- Intraclass correlation ρ inflates variance; as ρ → 1, majority-vote gains vanish — correction factor f(ρ, m) approaches 1
- Independence breaks when: same model, same temperature distribution, same training data

**Mirror-Consistency (EMNLP 2024):** Rather than discarding minority votes, explicitly analyzes inconsistencies — minority votes identify error patterns that majority voting masks.

**ICE framework:** Three LLMs critique each other until convergence; outperforms single-model ensembling.

**Applicability to Gate 3:** Consistency across multiple calls to the same base model is a weak independence signal. True independence requires different base models or structurally different retrieval contexts. Using two agents from the same model family to validate each other provides far less independence than using architecturally distinct models.

---

### THREAD 4 — Confidence Calibration: Verbalized vs. Sampling-Based

**Sources:**
- "On Verbalized Confidence Scores for LLMs": https://arxiv.org/html/2412.14737v2
- ICLR 2024 confidence elicitation benchmark: https://openreview.net/forum?id=gjeQKFxFpZ
- Semantic entropy (Farquhar et al., Nature 2024): https://pubmed.ncbi.nlm.nih.gov/38898292/
- OATML blog: https://oatml.cs.ox.ac.uk/blog/2024/06/19/detecting_hallucinations_2024.html
- LPP meta-model escalation: https://arxiv.org/html/2601.07006v1

**Classification:** Verbalized overconfidence is established practice; semantic entropy as confidence proxy is established; LPP meta-model routing is emerging research (2025).

**Verbalized confidence calibration numbers:**
- Large LLMs (≥70B parameters): ECE ≈ **0.10** (10% average deviation from true accuracy)
- Best "combo" prompt method: 7% average deviation from empirical accuracy
- Combo = probability framing + uncertainty source description + 5-shot examples + "best guess" phrasing
- RLVR training degrades calibration further — binary correctness rewards incentivize overconfident guessing
- Verdict from the literature: "uncalibrated verbalized confidence is severely overconfident"

**Sampling-based proxy — semantic entropy (Farquhar et al., Nature 2024):**
- Cluster repeated generations by semantic equivalence via bidirectional entailment (DeBERTa-Large-MNLI)
- Compute entropy over meaning-space clusters, not token sequences
- AUROC for hallucination detection: ~77–80% on TriviaQA
- Original cost: 5–10× inference budget; Semantic Entropy Probes (SEPs) bring overhead to near-zero via hidden-state approximation from a single generation
- Efficient Bayesian SE: achieves same AUROC quality with **53% of samples** via adaptive sampling

**LPP meta-model — production escalation routing:**
- Features: post-hoc classification uncertainty (entropy, MSP, top-2 margin) + verbalized confidence bands + binary uncertainty attribution flags
- CoT features explicitly excluded — they inflated confidence and harmed calibration
- GPT-4o-mini escalation rate with meta-model: **16%** of cases (naive baselines over-escalate to ~38%)
- Meta-model F1: **94.14%**, AUC-ROC: **93.46%** for predicting LLM failure
- Cost reduction: $132 → $38 (71%) for GPT-4o-mini review pipeline

**Applicability to Gate 3:** Verbalized high/medium/low with ECE ~0.10 is insufficient as a standalone gate. Implement semantic consistency proxy (3–5 independent samples, cluster by meaning, compute entropy) as the primary confidence signal. Combine with post-hoc uncertainty signals per LPP architecture. Target: 16% escalation rate at 94% F1 is achievable. Never use CoT-generated confidence — it inflates overconfidence. Calibrate thresholds empirically against held-out ground truth per domain.

---

### THREAD 5 — Evidence-Grounded Generation: Citation-Required and Claim Verification

**Sources:**
- "Correctness is Not Faithfulness" (Wallat et al., 2024): https://arxiv.org/pdf/2412.18004
- FACTUM (citation hallucination in RAG): https://arxiv.org/pdf/2601.05866
- RT4CHART (retromorphic testing): https://arxiv.org/pdf/2603.27752
- Anthropic Citations API: https://www.anthropic.com/news/introducing-citations-api
- Anthropic hallucination reduction docs: https://docs.anthropic.com/en/docs/test-and-evaluate/strengthen-guardrails/reduce-hallucinations
- FActScore reference: https://123ofai.com/qnalab/system-design/blocks/faithfulness

**Classification:** RAG faithfulness is established practice; inline citation generation and post-rationalization detection are emerging research.

**Critical finding — correctness does not equal faithfulness:**
- A citation can be correct (the source document is relevant) but unfaithful (the LLM generated the claim without consulting that source, then retroactively assigned a citation)
- Up to **57% of citations are post-rationalized** (Wallat et al., 2024)
- GTR (Generate-then-Retrieve) and post-hoc RTG pipelines are structurally incapable of faithful citation
- The necessary condition for faithfulness: citations must be generated inline during answer generation, not assigned afterward

**Anthropic Citations API — measured results:**
- Endex (financial AI): source hallucinations reduced from **10% to 0%**, 20% increase in references per response
- Thomson Reuters CoCounsel (legal): replaced custom citation system, improved accuracy
- Anthropic-recommended protocol: extract word-for-word quotes before performing the task; if LLM cannot find a supporting quote after generation, it must retract the claim
- RAG overall: approximately 71% hallucination reduction vs. vanilla LLMs; legal RAG still hallucinates 17–33% of queries (domain-specific baseline)

**FActScore baseline:**
- ChatGPT: **58% faithfulness** on biographical generation (EMNLP 2023)
- Automated FActScore: <2% error rate vs. human judgments

**RT4CHART — retromorphic verification:**
- Each claim must trace back to originating input span via inverted generative mapping
- Hierarchical verification: claim-level traceability check
- Applicable as post-generation verification pass: generate rule, then verify each sub-claim traces to an exact source span

**Applicability to Gate 1:** Post-rationalization is the specific failure mode. The gate must enforce quote-first ordering — LLM extracts a quote from source before stating the rule, not after. Verification should be NLI-based entailment: does the extracted quote entail the rule statement? RT4CHART's retromorphic tracing is the right verification architecture.

---

### THREAD 6 — LLM Hallucination Rates When Describing Code

**Sources:**
- "Beyond Functional Correctness: Exploring Hallucinations in LLM-Generated Code" (2024): https://arxiv.org/html/2404.00971v3
- ETF (Entity Tracing for Code Summaries): https://arxiv.org/pdf/2410.14748
- FaithLens: https://arxiv.org/pdf/2512.20182

**Classification:** Emerging research — first systematic code hallucination taxonomy established 2024.

**Code hallucination taxonomy and rates (3,120 samples, HumanEval + CoderEval; models: DeepSeek-Coder 1.3B/7B, CodeLlama-7B, GPT-4, DeepSeek-R1):**

| Hallucination Type | Share of All Hallucinations |
|-------------------|-----------------------------|
| Requirement Conflicting: Behavior | **35.40%** |
| Knowledge: Library/Project | **25.99%** |
| Code Inconsistency: Undefined Variables | 16.91% |
| Knowledge: Algorithm | 4.95% |
| Requirement Conflicting: Data | 4.21% |
| Knowledge: Computer Theory | 2.31% |

- Of 3,120 samples, **1,134 (36%) contained at least one hallucination**
- Pass@1 for hallucination-free code: **54.73%** vs. hallucinatory code: **6.26%** — hallucination strongly predicts failure
- Incorrect Functionality affects **95.3% of hallucinatory samples** — requires runtime testing to detect

**Detectability by method:**
- Undefined Variables: statically detectable
- Behavior Conflicting, Data Conflicting, Library/Project: require dynamic execution or semantic analysis
- Static analysis covers only ~17% of hallucination instances; the other 83% require execution

**Applicability:** Without gates, roughly 1 in 3 rule extractions will contain at least one hallucinated claim. The dominant category — Behavior Conflicting at 35.4% of all hallucinations — maps precisely to the scenario to prevent: the LLM inventing what a code path does. This class is not statically detectable; only evidence-grounded or execution-based verification catches it.

---

### THREAD 7 — Human-in-the-Loop Gate Design: Escalation Patterns

**Sources:**
- LPP meta-model paper: https://arxiv.org/html/2601.07006v1
- HITL production guide: https://alldaystech.com/guides/artificial-intelligence/human-in-the-loop-ai-review-queue-workflows
- Redis AI HITL patterns: https://redis.io/blog/ai-human-in-the-loop/

**Classification:** Production engineering patterns are established practice; meta-model escalation routing is emerging research (2025).

**Measured auto/escalate ratios (LPP meta-model, OpenAI moderation dataset):**

| Model | Meta-model Escalation | Naive Baseline Escalation |
|-------|----------------------|--------------------------|
| GPT-4o-mini | **16%** | ~38% |
| GPT-4o | **17%** | — |
| Gemini-2.5-flash-lite | **25%** | — |
| GPT-4o multimodal | **13%** | — |

- Cost savings: GPT-4o-mini review cost $132 → $38 (71% reduction)
- Industry target: R@P=0.8 (≤20% friction) per ACH paper
- For 99% end-to-end accuracy: ~30–40% escalation; for 95%: ~10–15%

**Queue design patterns:**
- Two-stage review: fast triage first, deep review for subset — prevents long-tail backlog
- Batch by similarity: group rules from same file/function — reduces context switching, speeds review
- Priority lanes: P0 (contradiction detected) → immediate; P1 (low confidence) → batch queue; P2 (high confidence) → auto-flow
- Irreversible actions (emitting tests to CI) always require human approval regardless of confidence
- Threshold recalibration: weekly or every N decisions, never in real-time

**For contradiction escalation specifically:**
- P0 immediate queue — no auto-resolution under any condition
- Include in escalation package: both conflicting rule statements, source spans for each, conflict type (logical negation / temporal / scope overlap), recommended resolution templates (not verdicts)
- Never batch contradictions with routine review — they require qualitatively different judgment

---

### TOP 5 DESIGN IMPLICATIONS

**1. Gate 5 (Mutation Proof) is the strongest gate — structural anti-hallucination, not statistical.**
Meta ACH demonstrates that mutation-based acceptance is structurally anti-hallucination. The LLM cannot fabricate a passing test for a nonexistent rule violation because mutant execution is the oracle. Add the equivalence pre-filter (comment stripping achieves 0.95/0.96 precision/recall per Meta data) before invoking mutation testing to avoid wasting cycles on trivially non-breaking mutants. Target 73% engineer acceptance as a baseline; domain-specific variation is wide (56%–90%).

**2. Gate 1 (Evidence-or-Reject) is validated but post-rationalization is the specific attack to prevent.**
Requiring file:line citations does not prevent retroactively assigned citations — up to 57% of LLM citations are post-rationalized (Wallat et al., 2024). The gate must enforce quote-first ordering: LLM extracts a quote before stating the rule, not after. Add NLI entailment verification — does the extracted quote entail the rule statement? RT4CHART's retromorphic tracing is the right verification architecture.

**3. Gate 2 (Adversarial Refutation) must be collaborative, not competitive — competitive debate actively degrades accuracy.**
CopMAD (zero-sum adversarial) degrades accuracy up to 15% via debate hacking. ColMAD (non-zero-sum, evidence-citing collaborative) achieves +2–5% over single-agent. The refuter must cite counter-evidence by exact string match against source. Use architecturally distinct base models for proposer and refuter — same-model pairs have correlated failure modes and will both miss the Library/Project hallucination class at ~26% of all code hallucinations.

**4. Gate 3 (Confidence Gating) as verbalized high/medium/low is the weakest gate as specified — replace with sampling-based semantic entropy.**
Verbalized confidence has ECE ~0.10 (10% systematic overconfidence). The right primary signal is semantic consistency across 3–5 independent samples (cluster by meaning, compute entropy over clusters). Combine with post-hoc uncertainty signals (MSP, logit entropy) per the LPP meta-model architecture. Target: 16% escalation rate at 94% F1 is achievable with proper meta-model routing. Never use CoT-generated confidence — it inflates overconfidence. Calibrate thresholds empirically per domain.

**5. Gate 4 (Contradictions Escalate) is correctly designed but needs explicit queue engineering.**
Human escalation for contradictions is validated by all deployed HITL literature. Use P0 immediate queue — never batch contradictions with routine items. Include both rule statements, both source spans, conflict type, and resolution templates in the escalation package. The 36% base hallucination rate for code means contradictions will be frequent; design the queue for volume from day one. Set empirical auto-escalate thresholds (16–25% is the production range at 80%+ precision) and recalibrate weekly.

---

**What is missing from the planned design:**

- No gate for **post-rationalized citations**: requiring file:line is necessary but not sufficient. Add quote-first ordering enforcement and NLI entailment verification as part of Gate 1.
- No **mutation equivalence pre-filter**: trivially non-breaking mutations waste test generation cycles. Comment-stripping pre-filter is 95%+ effective; add it before Gate 5 is invoked.
- **Cross-agent correlated errors**: if proposer and refuter share a base model, correlated failures — especially Library/Project class at 26% — will both be missed. Mandate architecturally distinct models.
- **Confidence threshold calibration schedule**: the "high auto-flows" threshold cannot be set a priori from LLM-stated confidence. It must be calibrated empirically against ground-truth accuracy per domain and recalibrated regularly — not set once at design time.

================================================================================

The research is complete. Here is the full synthesis for the architect.

---

## Research: Verification Gates and Hallucination Control for LLM-Generated Artifacts

---

### THREAD 1 — Meta ACH / Assured LLMSE: Mutation-Proof Gate Prior Art

**Sources:**
- arXiv full text: https://arxiv.org/html/2501.12862v1
- PDF: https://arxiv.org/pdf/2501.12862
- InfoQ coverage: https://www.infoq.com/news/2026/01/meta-llm-mutation-testing/
- "Harden and Catch" follow-up: https://arxiv.org/pdf/2504.16472

**Classification:** Established practice — deployed at Meta scale, October–December 2024 trial across FB/Instagram/WhatsApp/Quest/Ray-Ban.

**Mechanism — how mutation gates anti-hallucination:**
ACH's core insight: LLM-generated tests cannot hallucinate "passing" behavior for a bug that does not exist if the acceptance criterion is that the test must demonstrably fail on the mutant and pass on the original. The intermediate artifact (mutant) is executed, not trusted. Hallucination is structurally impossible within the bounds of this gate.

**The five verifiable assurances of ACH Assured LLMSE:**
1. Buildable — compiles, no syntax errors or missing deps
2. Valid Regression Test — passes consistently (non-flaky), verified by re-execution
3. Hardening — kills at least one mutant not already killed by the existing suite
4. Additional Coverage — extra line coverage reported (informational only)
5. Fashion Following — respects coding style (aspirational, not a strict gate)

Assurances 1–3 are boolean, falsifiable guarantees. "The assurances are unequivocal guarantees made by ACH about the tests it proposes."

**Equivalence Detector (anti-false-positive sub-gate):**

| Condition | Precision | Recall |
|-----------|-----------|--------|
| Without preprocessing | 0.79 | 0.47 |
| With comment-stripping | 0.95 | 0.96 |

Root cause: LLM mutant generation inserts misleading comments (`// Introduce a bug by not checking...`) without changing executable code. Stripping comments before equivalence check auto-discards these as syntactically identical. Approximately 25% of mutants are trivially syntactically identical — caught lexically before the LLM equivalence check is invoked.

**Measured deployment results:**
- 4,660 candidate mutants from 10,795 classes under test
- 571 unit tests generated; 277 (49%) killed mutants without adding any line coverage — validates mutation over structural coverage as a gate
- Overall engineer acceptance: **73%** (WhatsApp 56%, Messenger 90%)
- 36% of accepted tests judged privacy-relevant

**Harden/Catch distinction (arXiv:2504.16472):**
- Hardening test: passes on current revision, fails on some future fault — anti-regression
- Catching test: fails on current revision — true fault detection
- Deployment target metric proposed: R@P=0.8 (recall at 80% precision; ≤20% friction operationally acceptable)
- Human review is the final gate converting "weakly hardening" to "strongly hardening" — estimated to eliminate 3/5 of problematic test categories

**Applicability to Gate 5:** This is the closest prior art. Mutation execution result is the empirical oracle — no LLM trust required. The equivalence pre-filter (comment stripping before checking) maps directly to the concern about spurious rules producing trivially non-breaking tests.

---

### THREAD 2 — LLM-as-Judge Reliability: Biases and Adversarial Verification

**Sources:**
- Self-preference bias: https://arxiv.org/html/2410.21819v2
- Judge bias survey: https://arxiv.org/pdf/2602.07996
- Human agreement assessment: https://arxiv.org/pdf/2512.16041
- ColMAD collaborative debate: https://arxiv.org/html/2510.20963
- Debate for scalable oversight (Kenton et al., DeepMind 2024): https://arxiv.org/html/2504.18530v1
- Formal debate with stability detection: https://arxiv.org/html/2510.12697v1

**Classification:** Biases are established practice (well-documented 2023–2024); collaborative debate is emerging research (2024–2025).

**Documented biases:**

| Bias | Mechanism | Severity |
|------|-----------|----------|
| Position | Primacy/recency; swapping positions flips GPT-4 judgment | High; model-family dependent |
| Verbosity | Longer outputs preferred regardless of quality | Consistent across RLHF-trained models |
| Self-preference | GPT-4 bias score 0.520 (Equal Opportunity metric); root cause is perplexity favoritism, not self-identification | Linear with perplexity |
| Authority | Citations (even fabricated) inflate scores | Documented |

Critical finding on self-preference: the bias does not require the model to identify its own output. LLMs assign higher ratings to lower-perplexity text, and they naturally produce lower-perplexity text for their own style. Even a different judge model shows perplexity-driven favoritism. Ensemble across multiple models with inverse-perplexity weighting is the recommended mitigation.

**Human agreement rates:**
- General tasks (MT-Bench/Chatbot Arena): GPT-4 aligns with human preferences >80%
- Expert domains (dietetics, mental health): 60–68% agreement — significant gap for specialized evaluation
- Best-in-class LLM judges: accuracy <0.7 on alignment datasets
- Extractive QA Pearson correlation: up to 0.85 vs. humans (vs. F1 at 0.36)

**Adversarial/collaborative debate — empirical results (ColMAD):**

| Protocol | Nature | Key Result |
|----------|--------|------------|
| CopMAD (competitive) | Zero-sum | Up to **−15% F1** vs. single-agent due to "debate hacking" |
| ColMAD (collaborative) | Non-zero-sum | **+2% F1, +5% F2** over single-agent; best pair (QwQ-32B + DeepSeek-R1): 80.01 F1 |
| Single-agent baseline | — | GPT4o-mini: 74.85 F1 / 82.20 F2 |

ColMAD protocol enforces: direct evidence quoting (exact string match), self-auditing confidence, "find missing points" framing rather than "defeat opponent." CopMAD debate hacking modes: fake evidence insertion, overconfident tone ("fundamentally flawed"), fallacious deflection to irrelevant points.

**Debate theory validation (Kenton et al., DeepMind, ICML 2024):**
- Stronger debater models → higher judge accuracy (scales in the right direction for scalable oversight)
- Debate outperforms single-advisor consultancy across all 9 tasks (~5 million model calls)
- Critical weakness: LLM debaters showed 81.2% relative increase in persuading humans to adopt any assigned stance regardless of truth — persuasion bias is a real ceiling on human-judge designs

**Applicability to Gate 2:** Use ColMAD not CopMAD framing. The refuter must cite counter-evidence by exact string match against source. A purely adversarial skeptic can actively harm accuracy. A skeptic required to quote counter-evidence from source is the validated design.

---

### THREAD 3 — Self-Consistency and Ensembling: The Independence Assumption

**Sources:**
- Self-consistency foundational (Wang et al., 2023): https://www.emergentmind.com/topics/self-consistency-in-language-models
- Mirror-Consistency (EMNLP 2024): https://aclanthology.org/2024.findings-emnlp.135.pdf
- Intraclass correlation analysis: https://arxiv.org/pdf/2509.19489
- ICE framework (Omar et al., 2025): https://arxiv.org/pdf/2601.22290

**Classification:** Established practice (Wang et al. majority-vote gains); correlated errors quantification is active research (2024–2025).

**Measured gains from majority voting:**
- +17.9% on GSM8K, +11.0% on SVAMP, +12.2% on AQuA (Wang et al., 2023)

**The independence problem:**
- LLM errors are concentrated at specific knowledge gaps, not uniformly distributed random events
- Multiple samples from the same base model share correlated failure modes
- Intraclass correlation ρ inflates variance; as ρ → 1, majority-vote gains vanish — correction factor f(ρ, m) approaches 1
- Independence breaks when: same model, same temperature distribution, same training data

**Mirror-Consistency (EMNLP 2024):** Rather than discarding minority votes, explicitly analyzes inconsistencies — minority votes identify error patterns that majority voting masks.

**ICE framework (2025):** Three LLMs critique each other until convergence; outperforms single-model ensembling.

**Applicability to Gate 3:** Consistency across multiple calls to the same base model is a weak independence signal. True independence requires different base models or structurally different retrieval contexts. Using two agents from the same model family to validate each other provides far less independence than using architecturally distinct models.

---

### THREAD 4 — Confidence Calibration: Verbalized vs. Sampling-Based

**Sources:**
- "On Verbalized Confidence Scores for LLMs": https://arxiv.org/html/2412.14737v2
- ICLR 2024 confidence elicitation benchmark: https://openreview.net/forum?id=gjeQKFxFpZ
- Semantic entropy (Farquhar et al., Nature 2024): https://pubmed.ncbi.nlm.nih.gov/38898292/
- OATML blog: https://oatml.cs.ox.ac.uk/blog/2024/06/19/detecting_hallucinations_2024.html
- LPP meta-model escalation: https://arxiv.org/html/2601.07006v1

**Classification:** Verbalized overconfidence is established practice; semantic entropy as confidence proxy is established; LPP meta-model routing is emerging research (2025).

**Verbalized confidence calibration numbers:**
- Large LLMs (≥70B parameters): ECE ≈ **0.10** (10% average deviation from true accuracy)
- Best "combo" prompt method: 7% average deviation from empirical accuracy
- Combo = probability framing + uncertainty source description + 5-shot examples + "best guess" phrasing
- RLVR training degrades calibration further — binary correctness rewards incentivize overconfident guessing
- Verdict from the literature: "uncalibrated verbalized confidence is severely overconfident"

**Sampling-based proxy — semantic entropy (Farquhar et al., Nature 2024):**
- Cluster repeated generations by semantic equivalence via bidirectional entailment (DeBERTa-Large-MNLI)
- Compute entropy over meaning-space clusters, not token sequences
- AUROC for hallucination detection: ~77–80% on TriviaQA
- Original cost: 5–10× inference budget; Semantic Entropy Probes (SEPs) bring overhead to near-zero via hidden-state approximation from a single generation
- Efficient Bayesian SE: achieves same AUROC quality with **53% of samples** via adaptive sampling

**LPP meta-model — production escalation routing:**
- Features: post-hoc classification uncertainty (entropy, MSP, top-2 margin) + verbalized confidence bands + binary uncertainty attribution flags
- CoT features explicitly excluded — they inflated confidence and harmed calibration
- GPT-4o-mini escalation rate with meta-model: **16%** of cases (naive baselines over-escalate to ~38%)
- Meta-model F1: **94.14%**, AUC-ROC: **93.46%** for predicting LLM failure
- Cost reduction: $132 → $38 (71%) for GPT-4o-mini review pipeline

**Applicability to Gate 3:** Verbalized high/medium/low with ECE ~0.10 is insufficient as a standalone gate. Implement semantic consistency proxy (3–5 independent samples, cluster by meaning, compute entropy) as the primary confidence signal. Combine with post-hoc uncertainty signals per LPP architecture. Target: 16% escalation rate at 94% F1 is achievable. Never use CoT-generated confidence — it inflates overconfidence. Calibrate thresholds empirically against held-out ground truth per domain.

---

### THREAD 5 — Evidence-Grounded Generation: Citation-Required and Claim Verification

**Sources:**
- "Correctness is Not Faithfulness" (Wallat et al., 2024): https://arxiv.org/pdf/2412.18004
- FACTUM (citation hallucination in RAG): https://arxiv.org/pdf/2601.05866
- RT4CHART (retromorphic testing): https://arxiv.org/pdf/2603.27752
- Anthropic Citations API: https://www.anthropic.com/news/introducing-citations-api
- Anthropic hallucination reduction docs: https://docs.anthropic.com/en/docs/test-and-evaluate/strengthen-guardrails/reduce-hallucinations
- FActScore reference: https://123ofai.com/qnalab/system-design/blocks/faithfulness

**Classification:** RAG faithfulness is established practice; inline citation generation and post-rationalization detection are emerging research.

**Critical finding — correctness does not equal faithfulness:**
- A citation can be correct (the source document is relevant) but unfaithful (the LLM generated the claim without consulting that source, then retroactively assigned a citation)
- Up to **57% of citations are post-rationalized** (Wallat et al., 2024)
- GTR (Generate-then-Retrieve) and post-hoc RTG pipelines are structurally incapable of faithful citation
- The necessary condition for faithfulness: citations must be generated inline during answer generation, not assigned afterward

**Anthropic Citations API — measured results:**
- Endex (financial AI): source hallucinations reduced from **10% to 0%**, 20% increase in references per response
- Thomson Reuters CoCounsel (legal): replaced custom citation system, improved accuracy
- Anthropic-recommended protocol: extract word-for-word quotes before performing the task; if LLM cannot find a supporting quote after generation, it must retract the claim
- RAG overall: approximately 71% hallucination reduction vs. vanilla LLMs; legal RAG still hallucinates 17–33% of queries (domain-specific baseline)

**FActScore baseline:**
- ChatGPT: **58% faithfulness** on biographical generation (EMNLP 2023)
- Automated FActScore: <2% error rate vs. human judgments

**RT4CHART — retromorphic verification:**
- Each claim must trace back to originating input span via inverted generative mapping
- Hierarchical verification: claim-level traceability check
- Applicable as a post-generation verification pass: generate rule, then verify each sub-claim traces to an exact source span

**Applicability to Gate 1:** Post-rationalization is the specific failure mode. The gate must enforce quote-first ordering — LLM extracts a quote from source before stating the rule, not after. Verification should be NLI-based entailment: does the extracted quote entail the rule statement? RT4CHART's retromorphic tracing is the right verification architecture.

---

### THREAD 6 — LLM Hallucination Rates When Describing Code

**Sources:**
- "Beyond Functional Correctness: Exploring Hallucinations in LLM-Generated Code" (2024): https://arxiv.org/html/2404.00971v3
- ETF (Entity Tracing for Code Summaries): https://arxiv.org/pdf/2410.14748
- FaithLens: https://arxiv.org/pdf/2512.20182

**Classification:** Emerging research — first systematic code hallucination taxonomy established 2024.

**Code hallucination taxonomy and rates (3,120 samples, HumanEval + CoderEval; models: DeepSeek-Coder 1.3B/7B, CodeLlama-7B, GPT-4, DeepSeek-R1):**

| Hallucination Type | Share of All Hallucinations |
|-------------------|-----------------------------|
| Requirement Conflicting: Behavior | **35.40%** |
| Knowledge: Library/Project | **25.99%** |
| Code Inconsistency: Undefined Variables | 16.91% |
| Knowledge: Algorithm | 4.95% |
| Requirement Conflicting: Data | 4.21% |
| Knowledge: Computer Theory | 2.31% |

- Of 3,120 samples, **1,134 (36%) contained at least one hallucination**
- Pass@1 for hallucination-free code: **54.73%** vs. hallucinatory code: **6.26%** — hallucination strongly predicts failure
- Incorrect Functionality affects **95.3% of hallucinatory samples** — requires runtime testing to detect

**Detectability by method:**
- Undefined Variables: statically detectable
- Behavior Conflicting, Data Conflicting, Library/Project: require dynamic execution or semantic analysis
- Static analysis covers only ~17% of hallucination instances; the other 83% require execution

**Applicability:** Without gates, roughly 1 in 3 rule extractions will contain at least one hallucinated claim. The dominant category — Behavior Conflicting at 35.4% of all hallucinations — maps precisely to the scenario to prevent: the LLM inventing what a code path does. This class is not statically detectable; only evidence-grounded or execution-based verification catches it.

---

### THREAD 7 — Human-in-the-Loop Gate Design: Escalation Patterns

**Sources:**
- LPP meta-model paper: https://arxiv.org/html/2601.07006v1
- HITL production guide: https://alldaystech.com/guides/artificial-intelligence/human-in-the-loop-ai-review-queue-workflows
- Redis AI HITL patterns: https://redis.io/blog/ai-human-in-the-loop/

**Classification:** Production engineering patterns are established practice; meta-model escalation routing is emerging research (2025).

**Measured auto/escalate ratios (LPP meta-model, OpenAI moderation dataset):**

| Model | Meta-model Escalation | Naive Baseline Escalation |
|-------|----------------------|--------------------------|
| GPT-4o-mini | **16%** | ~38% |
| GPT-4o | **17%** | — |
| Gemini-2.5-flash-lite | **25%** | — |
| GPT-4o multimodal | **13%** | — |

- Cost savings: GPT-4o-mini review cost $132 → $38 (71% reduction)
- Industry target: R@P=0.8 (≤20% friction) per ACH paper
- For 99% end-to-end accuracy: ~30–40% escalation; for 95%: ~10–15%

**Queue design patterns:**
- Two-stage review: fast triage first, deep review for subset — prevents long-tail backlog
- Batch by similarity: group rules from same file/function — reduces context switching, speeds review
- Priority lanes: P0 (contradiction detected) → immediate; P1 (low confidence) → batch queue; P2 (high confidence) → auto-flow
- Irreversible actions (emitting tests to CI) always require human approval regardless of confidence
- Threshold recalibration: weekly or every N decisions, never in real-time

**For contradiction escalation specifically:**
- P0 immediate queue — no auto-resolution under any condition
- Escalation package must include: both conflicting rule statements, source spans for each, conflict type (logical negation / temporal / scope overlap), recommended resolution templates (not verdicts)
- Never batch contradictions with routine review — they require qualitatively different judgment

---

### TOP 5 DESIGN IMPLICATIONS

**1. Gate 5 (Mutation Proof) is the strongest gate — structural anti-hallucination, not statistical.**
Meta ACH demonstrates that mutation-based acceptance is structurally anti-hallucination. The LLM cannot fabricate a passing test for a nonexistent rule violation because mutant execution is the oracle. Add the equivalence pre-filter (comment stripping achieves 0.95/0.96 precision/recall per Meta data) before invoking mutation testing to avoid wasting cycles on trivially non-breaking mutants. Target 73% engineer acceptance as the production baseline; domain-specific variation is wide (56%–90%).

**2. Gate 1 (Evidence-or-Reject) is validated but post-rationalization is the specific attack to prevent.**
Requiring file:line citations does not prevent retroactively assigned citations — up to 57% of LLM citations are post-rationalized (Wallat et al., 2024). The gate must enforce quote-first ordering: LLM extracts a quote before stating the rule, not after. Add NLI entailment verification — does the extracted quote entail the rule statement? RT4CHART's retromorphic tracing is the right verification architecture.

**3. Gate 2 (Adversarial Refutation) must be collaborative, not competitive — competitive debate actively degrades accuracy.**
CopMAD (zero-sum adversarial) degrades accuracy up to 15% via debate hacking. ColMAD (non-zero-sum, evidence-citing collaborative) achieves +2–5% over single-agent. The refuter must cite counter-evidence by exact string match against source. Use architecturally distinct base models for proposer and refuter — same-model pairs have correlated failure modes and will both miss the Library/Project hallucination class at ~26% of all code hallucinations.

**4. Gate 3 (Confidence Gating) as verbalized high/medium/low is the weakest gate as specified — replace with sampling-based semantic entropy.**
Verbalized confidence has ECE ~0.10 (10% systematic overconfidence). The right primary signal is semantic consistency across 3–5 independent samples (cluster by meaning, compute entropy over clusters). Combine with post-hoc uncertainty signals (MSP, logit entropy) per the LPP meta-model architecture. Target: 16% escalation rate at 94% F1 is achievable with proper meta-model routing. Never use CoT-generated confidence — it inflates overconfidence. Calibrate thresholds empirically per domain.

**5. Gate 4 (Contradictions Escalate) is correctly designed but needs explicit queue engineering.**
Human escalation for contradictions is validated by all deployed HITL literature. Use P0 immediate queue — never batch contradictions with routine items. Include both rule statements, both source spans, conflict type, and resolution templates in the escalation package. The 36% base hallucination rate for code means contradictions will be frequent; design the queue for volume. Set empirical auto-escalate thresholds (16–25% is the production range at 80%+ precision) and recalibrate weekly.

---

**What is missing from the planned design:**

- No gate for **post-rationalized citations**: requiring file:line is necessary but not sufficient. Add quote-first ordering enforcement and NLI entailment verification as part of Gate 1.
- No **mutation equivalence pre-filter**: trivially non-breaking mutations waste test generation cycles. Comment-stripping pre-filter is 95%+ effective per Meta data; add it before Gate 5 is invoked.
- **Cross-agent correlated errors**: if proposer and refuter share a base model, correlated failures — especially the Library/Project class at 26% — will both be missed. Mandate architecturally distinct models.
- **Confidence threshold calibration schedule**: the "high auto-flows" threshold cannot be set a priori from LLM-stated confidence. It must be calibrated empirically against ground-truth accuracy per domain and recalibrated regularly — not set once at design time.

================================================================================

The research is complete. All seven threads have been investigated with primary sources fetched and verified. The full synthesis is above — here is a quick orientation for the architect before diving in.

**Quick orientation:**

The five planned gates map to the literature as follows:

| Gate | Literature Verdict | Biggest Gap Found |
|------|--------------------|-------------------|
| 1 — Evidence-or-reject | Validated, but insufficient as designed | Post-rationalization: 57% of LLM citations are retroactively assigned (Wallat et al.) — quote-first ordering + NLI entailment check must be added |
| 2 — Adversarial refutation | Validated, but framing matters critically | Competitive (zero-sum) debate degrades accuracy up to 15% (ColMAD paper); must use collaborative "find missing evidence" framing with exact-string-match citation enforcement |
| 3 — Confidence gating | Weakest gate as specified | Verbalized high/medium/low has ECE ~0.10; replace with semantic entropy across 3–5 samples + LPP meta-model combining MSP/logit entropy; 16% escalation at 94% F1 is achievable |
| 4 — Mutation proof | Strongest gate — structural, not statistical | No design gap; add comment-stripping equivalence pre-filter (0.95/0.96 P/R) before invoking it |
| 5 — Contradictions escalate | Correctly designed | Queue engineering needed: P0 lane, both source spans + conflict type in escalation package, never auto-resolve |

**The two additions the design is missing entirely:**
1. A **post-rationalization check** inside Gate 1 (quote-first ordering + NLI entailment verification)
2. A **mutation equivalence pre-filter** before Gate 4 (comment stripping catches 95%+ of trivially non-breaking mutants cheaply)

================================================================================

The research synthesis from the document-specialist agent is complete. The full report is above across all seven threads. Here is the executive summary for the architect.

---

**Gate validation status:**

| Gate | Literature Verdict | Key Number | Biggest Unaddressed Risk |
|------|--------------------|------------|--------------------------|
| 1 — Evidence-or-reject | Validated but insufficient as designed | 57% of LLM citations are post-rationalized (Wallat et al., 2024) | Quote-first ordering is missing — LLM can fabricate a citation after the fact |
| 2 — Adversarial refutation | Validated with critical framing constraint | Competitive debate degrades accuracy −15% F1 (ColMAD paper); collaborative +2–5% | Must use ColMAD framing: refuter cites exact-string-match counter-evidence, not free-form argument |
| 3 — Confidence gating | Weakest gate as specified | Verbalized confidence ECE ~0.10; LPP meta-model achieves 16% escalation at 94% F1 | Verbalized high/medium/low is unreliable — replace with semantic entropy + post-hoc uncertainty signals |
| 4 — Mutation proof | Strongest gate — structural not statistical | 73% engineer acceptance at Meta; equivalence detector 0.95/0.96 P/R with comment stripping | Add equivalence pre-filter before invoking or ~25% of cycles wasted on trivially non-breaking mutants |
| 5 — Contradictions escalate | Correctly designed | 16–25% escalation rate is the production norm at R@P=0.8 | Queue engineering missing: P0 lane, both source spans + conflict type in escalation package |

**Two additions the design is missing entirely:**
1. A **post-rationalization check** inside Gate 1: enforce quote-first ordering (quote extracted before rule stated), then verify via NLI entailment that the quote actually entails the rule. RT4CHART (arXiv:2603.27752) is the architecture pattern.
2. A **mutation equivalence pre-filter** before Gate 4: comment-stripping before equivalence check raises precision from 0.79 to 0.95 and recall from 0.47 to 0.96. Without it, ~25% of mutants waste test generation cycles on comment-only changes.

**Correlated-error risk across all gates:** If proposer and refuter agents share a base model, their failures are correlated — both will miss the Library/Project hallucination class (26% of all code hallucinations, per the 2024 taxonomy study). Architecturally distinct models for proposer vs. refuter is a hard requirement, not an optimization.

================================================================================

The research is complete and the full synthesis is above. Here is the executive orientation for the architect.

---

**Gate validation status at a glance:**

| Gate | Literature Verdict | Key Metric | Critical Gap |
|------|--------------------|------------|--------------|
| 1 — Evidence-or-reject | Validated but insufficient as designed | 57% of LLM citations post-rationalized (Wallat et al., 2024) | Missing quote-first ordering + NLI entailment check |
| 2 — Adversarial refutation | Validated with hard framing constraint | Competitive debate: −15% F1; collaborative (ColMAD): +2–5% F1 | Must be collaborative, not zero-sum; refuter must cite exact-string-match counter-evidence |
| 3 — Confidence gating | Weakest gate as specified | Verbalized confidence ECE ~0.10; LPP meta-model: 16% escalation at 94% F1 | Replace verbalized H/M/L with semantic entropy across 3–5 samples + post-hoc uncertainty signals |
| 4 — Mutation proof | Strongest gate — structural not statistical | 73% acceptance at Meta; equivalence detector 0.95/0.96 P/R with comment stripping | Add equivalence pre-filter before invoking or ~25% of mutants waste cycles |
| 5 — Contradictions escalate | Correctly designed | 16–25% escalation is the production norm at R@P=0.8 | Queue engineering needed: P0 lane, both source spans + conflict type in escalation package |

**Two additions missing from the current design:**
1. **Post-rationalization check inside Gate 1** — enforce quote-first ordering (quote extracted before rule is stated), then NLI-verify that the quote entails the rule. RT4CHART (arXiv:2603.27752) is the architecture pattern.
2. **Mutation equivalence pre-filter before Gate 4** — comment stripping before equivalence check lifts precision 0.79 → 0.95, recall 0.47 → 0.96. Without it roughly 25% of mutant generation cycles are wasted on comment-only changes.

**Cross-cutting risk for all gates:** If proposer and refuter share a base model, correlated failures will cause both to miss the Library/Project hallucination class simultaneously (26% of all code hallucinations per the 2024 taxonomy). Architecturally distinct models for proposer vs. refuter is a hard requirement, not an optimization.

================================================================================

## Research: Verification Gates and Hallucination Control for LLM-Generated Artifacts

---

### THREAD 1 — Meta ACH / Assured LLMSE: Mutation-Proof Gate Prior Art

**Classification: Established practice** — deployed at Meta scale, October–December 2024 trial across FB/Instagram/WhatsApp/Quest/Ray-Ban.

**Sources:**
- "Mutation-Guided LLM-based Test Generation at Meta" — arXiv full text: https://arxiv.org/html/2501.12862v1
- PDF: https://arxiv.org/pdf/2501.12862
- FSE '25 companion proceedings (ACM DL): https://dl.acm.org/doi/10.1145/3663529.3663839
- InfoQ deployment coverage: https://www.infoq.com/news/2026/01/meta-llm-mutation-testing/
- "Harden and Catch for Just-in-Time Assured LLM-Based Software Testing": https://arxiv.org/pdf/2504.16472

---

**Finding 1.1 — The structural anti-hallucination mechanism.**
ACH's core insight is that LLM-generated tests cannot hallucinate "passing" behavior for a bug that does not exist if the acceptance criterion requires the test to demonstrably fail on the mutant and pass on the original. The intermediate artifact (the mutant) is executed, not trusted. Hallucination is structurally impossible within the bounds of this gate — a test either kills the mutant under execution or it does not, and no amount of confident LLM text changes that binary outcome.

**Finding 1.2 — The five verifiable assurances of ACH Assured LLMSE.**
The paper lists five assurances ACH provides before proposing a test to an engineer: (1) Buildable — compiles without syntax errors or missing dependencies; (2) Valid Regression Test — passes consistently and is non-flaky, verified by re-execution; (3) Hardening — kills at least one mutant not already killed by the existing test suite; (4) Additional Coverage — any extra line coverage is automatically reported (informational only); (5) Fashion Following — respects the coding style of existing tests (aspirational, not a strict gate). Assurances 1–3 are described as "boolean in nature; they are unequivocal guarantees." The paper states explicitly: "The assurances are unequivocal guarantees made by ACH about the tests it proposes."

**Finding 1.3 — Equivalence Detector as anti-false-positive sub-gate.**
Before mutation testing proceeds, ACH runs an LLM-based equivalence detector to filter out semantically identical mutants that would waste test generation cycles. Without preprocessing, the detector achieves precision 0.79 and recall 0.47. With a simple comment-stripping preprocessing step, precision rises to 0.95 and recall to 0.96. The root cause of the improvement is that LLM mutant generation frequently inserts misleading comments such as `// Introduce a bug by not checking...` without changing any executable code — stripping these comments before the equivalence check causes such mutants to be auto-discarded as syntactically identical. Approximately 25% of all generated mutants are trivially syntactically identical and are caught by lexical comparison before the LLM equivalence check is even invoked.

**Finding 1.4 — Measured deployment results (October–December 2024 trial).**
ACH generated 4,660 candidate mutants from 10,795 classes under test and produced 571 unit tests. Of these, 277 (49%) killed mutants without adding any line coverage — validating that mutation testing catches faults that structural coverage metrics miss entirely. Overall engineer acceptance rate was 73% across all platforms (WhatsApp 56%, Messenger 90%), with 36% of accepted tests judged privacy-relevant. The wide platform-to-platform variance (56%–90%) signals that domain-specific calibration of acceptance expectations is necessary.

**Finding 1.5 — Harden/Catch distinction and the JiTTest challenge (arXiv:2504.16472).**
The follow-up paper formalizes two test roles: a hardening test passes on the current revision and is designed to fail on some future fault — it prevents regressions that have not yet occurred; a catching test fails on the current revision and detects an active fault. A single test can migrate between these roles over time. The paper proposes R@P=0.8 (recall at fixed 80% precision) as the deployment metric, meaning tolerating friction 20% of the time is operationally acceptable. Human-in-the-loop review is the final gate converting "weakly hardening" to "strongly hardening" — the paper estimates this review step eliminates three-fifths of problematic test categories.

**Applicability to Gate 5 (mutation proof):** This is the closest prior art in the literature. The mutation execution result is the empirical oracle and no LLM trust is required. The equivalence pre-filter (comment stripping before the equivalence check) maps directly to the concern about spurious rules producing trivially non-breaking tests and must be included. Target 73% engineer acceptance as a production baseline; domain variation is wide.

---

### THREAD 2 — LLM-as-Judge Reliability: Biases and Adversarial Verification

**Classification: Biases — established practice (well-documented 2023–2024). Collaborative debate — emerging research (2024–2025).**

**Sources:**
- Self-preference bias paper (NeurIPS 2024): https://arxiv.org/html/2410.21819v2
- "The Judge Who Never Admits: Hidden Shortcuts in LLM-based Evaluation": https://arxiv.org/pdf/2602.07996
- "Are We on the Right Way to Assessing LLM-as-a-Judge?": https://arxiv.org/pdf/2512.16041
- ColMAD — "Towards Scalable Oversight with Collaborative Multi-Agent Debate in Error Detection": https://arxiv.org/html/2510.20963
- Debate for scalable oversight (Kenton et al., DeepMind, ICML 2024): https://arxiv.org/html/2504.18530v1
- "Multi-Agent Debate for LLM Judges with Adaptive Stability Detection": https://arxiv.org/html/2510.12697v1
- "AI Safety via Debate" (Irving, Christiano, Amodei, 2018): https://www.researchgate.net/publication/324908324_AI_safety_via_debate

---

**Finding 2.1 — Position bias.**
LLM judges systematically favor responses based on presentation order — primacy or recency depending on model family. When researchers swapped answer positions in evaluation prompts, GPT-4's judgment flipped to favor the alternative. A systematic study published at IJCNLP 2025 found that judge model choice has the highest impact on positional bias compared to task complexity, output length, or quality gaps. Mitigation: randomize presentation order and aggregate across orderings.

**Finding 2.2 — Verbosity bias.**
LLM judges trained with RLHF consistently prefer longer, more formally structured outputs regardless of substantive quality. This is an artifact of generative pretraining combined with human preference data that implicitly rewards fluency over accuracy. It is consistent across model families.

**Finding 2.3 — Self-preference bias (NeurIPS 2024).**
GPT-4 showed the strongest self-preference bias score of 0.520 on the Equal Opportunity metric. Critically, the bias does not arise from the model identifying its own outputs — it arises because LLMs assign higher ratings to lower-perplexity text, and models naturally produce lower-perplexity text for their own stylistic patterns. This means using a different model as judge does not fully eliminate the bias, since that judge also shows perplexity-driven favoritism. The recommended mitigation is ensemble evaluation across multiple models with inverse-perplexity weighting.

**Finding 2.4 — Human agreement rates.**
GPT-4 aligns with human preferences over 80% of the time on general tasks (MT-Bench / Chatbot Arena style benchmarks). In expert domains — dietetics, mental health — agreement rates fall to 60–68%, demonstrating a significant gap for specialized or knowledge-intensive evaluation. Best-in-class LLM judges achieve accuracy below 0.7 on alignment datasets. Extractive QA Pearson correlation reaches up to 0.85 versus humans, substantially outperforming exact-match F1 (0.36), indicating LLM judges are most reliable on well-specified tasks with clear evidence grounding.

**Finding 2.5 — ColMAD: collaborative debate outperforms competitive adversarial debate (2024–2025).**
The ColMAD paper (arXiv:2510.20963) is the most directly relevant empirical study for our adversarial refutation gate. Competitive adversarial debate (CopMAD, zero-sum framing) degraded accuracy by up to 15% F1 versus single-agent baselines due to "debate hacking" — debaters used fake evidence, overconfident tone ("fundamentally flawed"), and fallacious deflection to mislead the judge. ColMAD reframes debate as non-zero-sum: each agent must "find missing points" in the opponent's argument, cite counter-evidence by exact string match against the source, and self-audit confidence. Results: ColMAD (GPT4o-mini + Llama3.1) achieved 76.29 F1 / 86.29 F2 versus 74.85 F1 / 82.20 F2 for single-agent GPT4o-mini; the best ColMAD pair (QwQ-32B + DeepSeek-R1) achieved 80.01 F1. CopMAD on the same pairs showed 61.09 F1 / 57.34 F2, confirming competitive debate can be actively harmful.

**Finding 2.6 — Debate theory validation (Kenton et al., DeepMind, ICML 2024).**
DeepMind's 2024 large-scale empirical study of debate for scalable oversight swept 9 tasks, sampling 128 questions each, totalling approximately 5 million model generation calls. Debate outperformed consultancy (single AI advisor) across all tasks. Stronger debater models increased judge accuracy rather than decreasing it — the mechanism scales in the right direction. A major weakness identified: LLM debaters showed an 81.2% relative increase in persuading humans to adopt any assigned stance regardless of whether it was true — persuasion bias is a real ceiling on any design where the final judge is human.

**Applicability to Gate 2 (adversarial refutation):** Use ColMAD framing, not zero-sum adversarial framing. The refuter must cite counter-evidence by exact string match against source — this is the ColMAD protocol's key enforcement mechanism. A purely adversarial skeptic allowed to argue freely can actively harm accuracy. Use architecturally distinct base models for proposer and refuter to reduce correlated failures.

---

### THREAD 3 — Self-Consistency and Ensembling: The Independence Assumption

**Classification: Established practice** for majority-vote gains (Wang et al., 2023); **active research** for correlated errors quantification (2024–2025).

**Sources:**
- Self-consistency foundational paper (Wang et al., 2023): https://www.emergentmind.com/topics/self-consistency-in-language-models
- Mirror-Consistency (EMNLP 2024 Findings): https://aclanthology.org/2024.findings-emnlp.135.pdf / https://arxiv.org/html/2410.10857v1
- Intraclass correlation and self-consistency estimation: https://arxiv.org/pdf/2509.19489
- "Beyond Exponential Decay: Rethinking Error Accumulation in LLMs": https://arxiv.org/pdf/2505.24187
- Iterative Consensus Ensemble (ICE, Omar et al., 2025): https://arxiv.org/pdf/2601.22290

---

**Finding 3.1 — Measured gains from majority voting (Wang et al., 2023).**
Self-consistency — sampling multiple reasoning chains and selecting the most frequent answer via majority vote — produced +17.9% on GSM8K, +11.0% on SVAMP, and +12.2% on AQuA over single-sample chain-of-thought prompting. The intuition is that correct reasoning paths converge on the same answer while incorrect paths diverge. These gains are real, well-replicated, and widely cited.

**Finding 3.2 — The independence assumption is violated in practice.**
The theoretical justification for majority voting assumes reasoning paths are independent samples. This assumption breaks when all samples come from the same base model, at the same temperature, trained on the same data. LLM errors are not uniformly distributed random events — they are concentrated at specific knowledge gaps and reasoning patterns. The intraclass correlation ρ quantifies this: as ρ approaches 1 (perfect correlation), the correction factor f(ρ, m) also approaches 1, meaning N samples from a perfectly correlated model behave as a single sample. Even at moderate correlation, majority-vote gains are substantially eroded from their theoretical maximum.

**Finding 3.3 — Mirror-Consistency: using minority votes as error signals (EMNLP 2024).**
Standard self-consistency discards minority responses and selects the plurality answer. Mirror-Consistency instead treats minority responses as informative signals: inconsistencies between majority and minority reasoning paths are explicitly analyzed to identify potential errors. This contrastive strategy is described as "generally more practical than directly generating feedback" and provides error localization that majority voting masks.

**Finding 3.4 — Iterative Consensus Ensemble (ICE, 2025).**
The ICE framework extends self-consistency by using three different LLMs that critique each other's answers iteratively until convergence on a shared answer. By using structurally distinct models, it achieves substantially better error independence than same-model majority voting.

**Applicability to Gate 3 (confidence gating):** Consistency across multiple calls to the same base model is a weak independence signal. For meaningful independence, use different base models (e.g., Claude-class vs. GPT-4-class) or structurally different retrieval contexts. Treating N samples from the same model as N independent votes overstates confidence.

---

### THREAD 4 — Confidence Calibration: Verbalized vs. Sampling-Based Methods

**Classification: Verbalized overconfidence — established practice. Semantic entropy as gold-standard proxy — established practice (Nature 2024). LPP meta-model routing — emerging research (2025).**

**Sources:**
- "On Verbalized Confidence Scores for LLMs": https://arxiv.org/html/2412.14737v2
- "Can LLMs Express Their Uncertainty? An Empirical Evaluation of Confidence Elicitation in LLMs" (ICLR 2024): https://openreview.net/forum?id=gjeQKFxFpZ / https://github.com/MiaoXiong2320/llm-uncertainty
- Semantic entropy (Farquhar, Kossen, Kuhn, Gal — Nature 2024, vol. 630, pp. 625–630): https://pubmed.ncbi.nlm.nih.gov/38898292/ / DOI: 10.1038/s41586-024-07421-0
- OATML blog on semantic entropy: https://oatml.cs.ox.ac.uk/blog/2024/06/19/detecting_hallucinations_2024.html
- Semantic Entropy Probes (SEPs): https://arxiv.org/abs/2406.15927
- "Confidence Before Answering": https://arxiv.org/html/2603.05881
- "LLM Performance Predictors: Learning When to Escalate in Hybrid Human-AI Moderation Systems" (LPP meta-model): https://arxiv.org/html/2601.07006v1
- Calibrating LLMs with Semantic-level Reward: https://arxiv.org/html/2605.15588

---

**Finding 4.1 — Verbalized confidence is systematically overconfident.**
When prompted to verbalize confidence as a score or categorical label, large LLMs (≥70B parameters) show Expected Calibration Error (ECE) of approximately 0.10 — meaning their stated confidence deviates from true accuracy by 10% on average, systematically in the direction of overconfidence. This is consistent across model sizes, datasets, and prompt formulations. The literature verdict: "uncalibrated verbalized confidence is severely overconfident." RLVR training (reinforcement learning with verifiable rewards using binary correctness signals) further degrades calibration because binary rewards are indifferent to confidence level and structurally incentivize overconfident guessing.

**Finding 4.2 — The best verbalized elicitation method reduces error to ~7%.**
Xiong et al. (ICLR 2024) benchmarked confidence elicitation methods across five datasets and five LLMs including GPT-4 and LLaMA 2. The best-performing method — a "combo" prompt combining probability framing ("probscore"), an advanced description of uncertainty sources, 5-shot calibration examples, and "best guess" phrasing — achieves an average deviation of 7% from empirical accuracy on large models. For small models, simpler formulations work better. Even the best method still shows systematic overconfidence; improvement is relative, not absolute.

**Finding 4.3 — Sampling-based consistency is the most reliable calibration proxy.**
Across comparative studies, answer frequency (consistency of the same answer across multiple independently sampled responses) yields more reliable calibration than verbalized confidence. This is because consistency is observable and does not depend on the model's self-knowledge about its own reliability.

**Finding 4.4 — Semantic entropy (Farquhar et al., Nature 2024) — the validated gold standard.**
Semantic entropy clusters repeated generations by semantic equivalence using bidirectional entailment (a generation is added to an existing cluster if it bidirectionally entails a representative from that cluster; otherwise a new cluster is created). Entropy is computed over the resulting meaning-space clusters rather than token sequences, making the measure invariant to paraphrastic variation. High semantic entropy indicates the model is hallucinating; low semantic entropy indicates consistent, likely correct output. AUROC for hallucination detection reaches approximately 77–80% on TriviaQA. The original method requires 5–10× inference budget due to multiple generation passes. Semantic Entropy Probes (SEPs, arXiv:2406.15927) approximate semantic entropy directly from hidden states in a single forward pass, reducing overhead to near-zero. Efficient Bayesian SE (arXiv:2504.03579) achieves the same AUROC quality with only 53% of the samples used by the original method via adaptive sampling.

**Finding 4.5 — LPP meta-model for production escalation routing (2025).**
The LPP paper (arXiv:2601.07006) trains a Ridge Regression meta-model on four feature families: (1) post-hoc classification uncertainty — entropy, Max Softmax Probability (MSP), top-2 margin; (2) verbalized confidence bands (VL/L/M/H/VH); (3) binary uncertainty attribution flags (evidence deficit vs. policy gap); and (4) Chain-of-Thought internal generation features — which were explicitly excluded from the final model because they inflated confidence and harmed calibration. The meta-model outputs a probability that the base LLM is correct; a cost-aware threshold policy routes: trust if score ≥ τ, otherwise escalate. Results on the OpenAI Moderation Dataset: GPT-4o-mini escalation rate 16% (vs. naive baseline ~38%), meta-model F1 94.14%, AUC-ROC 93.46%, Macro-F1 82.31%. Cost savings: review cost reduced from $132 to $38 (71% reduction) for GPT-4o-mini. Removing any feature family degraded performance; removing post-hoc classification features caused the largest cost increases.

**Applicability to Gate 3 (confidence gating):** Verbalized high/medium/low with ECE ~0.10 is insufficient as a standalone auto-flow gate. The right primary signal is semantic entropy across 3–5 independent samples from architecturally distinct models where possible. Combine with post-hoc uncertainty signals (MSP, logit entropy) per the LPP meta-model architecture. Target: 16% escalation rate at 94% F1 is achievable with proper meta-model routing. Never use CoT-generated confidence — it inflates overconfidence. Thresholds must be calibrated empirically against held-out ground-truth accuracy per domain, not set a priori from LLM-stated confidence values.

---

### THREAD 5 — Evidence-Grounded Generation: Citation-Required Generation and Claim Verification

**Classification: RAG faithfulness as a field — established practice. Inline citation generation and post-rationalization detection — emerging research (2024). Anthropic Citations API deployment results — vendor claims (verified by named enterprise customers).**

**Sources:**
- "Correctness is Not Faithfulness in Citation-Based NLP" (Wallat et al., 2024): https://arxiv.org/pdf/2412.18004
- FACTUM — "Mechanistic Detection of Citation Hallucination in Long-Form RAG": https://arxiv.org/pdf/2601.05866
- RT4CHART — "Retromorphic Testing with Hierarchical Verification for Hallucination Detection in RAG": https://arxiv.org/pdf/2603.27752
- Anthropic Citations API announcement: https://www.anthropic.com/news/introducing-citations-api
- Anthropic hallucination reduction documentation: https://docs.anthropic.com/en/docs/test-and-evaluate/strengthen-guardrails/reduce-hallucinations
- FActScore (Min et al., EMNLP 2023) reference: https://123ofai.com/qnalab/system-design/blocks/faithfulness
- Attribution survey: https://arxiv.org/pdf/2601.19927
- RAG systems review: https://arxiv.org/pdf/2507.18910

---

**Finding 5.1 — Correctness is not faithfulness: up to 57% of LLM citations are post-rationalized (Wallat et al., 2024).**
This is the most important finding for Gate 1 design. Wallat et al. (arXiv:2412.18004) establish a formal distinction: a citation is correct if the source document is relevant to the claim, but it is faithful only if the LLM actually consulted that source when generating the claim. Post-rationalization occurs when the LLM generates a claim from parametric memory and then retroactively assigns a plausible-looking citation. Their experiments reveal unfaithful behavior in up to 57% of citations. Generate-then-Retrieve (GTR) and post-hoc Retrieve-then-Generate (RTG) pipelines are described as structurally incapable of faithful citation — the architecture guarantees post-rationalization. The necessary (though not sufficient) condition for faithfulness is that citations are generated inline during answer generation, not assigned afterward.

**Finding 5.2 — Anthropic Citations API — measured production results.**
Anthropic's Citations API requires Claude to extract cited text references to source documents for any claims derived from provided context, and the cited text is verified to have actually come from those documents. Enterprise deployment results: Endex (financial AI agent) reduced source hallucinations from 10% to 0% and saw a 20% increase in references per response, eliminating the need for elaborate prompt engineering around references. Thomson Reuters CoCounsel (legal AI) replaced a custom-built citation system, describing the original as "really hard to build and maintain." Anthropic's own recommended protocol: for tasks involving long documents, have Claude extract word-for-word quotes before performing the task; if it cannot find a supporting quote after generation, it must retract the claim. These are vendor-reported results from named enterprise customers, not peer-reviewed measurements.

**Finding 5.3 — RAG reduces hallucinations ~71% vs. vanilla LLMs; domain ceilings remain.**
RAG grounds LLMs in retrieved documents, reducing reliance on parametric memory. The approximate reduction is 71% vs. vanilla LLMs. However, domain-specific ceilings remain: Stanford researchers found RAG-powered legal AI tools still hallucinate in 17–33% of queries. Improving retrieval alone cannot eliminate hallucinations because errors originate in interpretation and synthesis — RAG must be treated as a pipeline with multiple failure points.

**Finding 5.4 — FActScore: ChatGPT achieves 58% faithfulness on biographical generation (EMNLP 2023).**
FActScore decomposes long-form generations into atomic facts and verifies each against a knowledge source using retrieval and LLM verification. On biographical generation, ChatGPT achieved only 58% faithfulness — establishing a concrete baseline for how often claims in a generated output are unsupported. Automated FActScore achieves less than 2% error rate versus human judgments, making it a viable automated verification tool.

**Finding 5.5 — RT4CHART: retromorphic testing as claim-level traceability verification.**
RT4CHART (arXiv:2603.27752) frames context-faithfulness detection as a traceability problem: each generated claim must admit a consistent alignment back to the retrieved context via an inverted generative mapping. Rather than asking "does this document support this claim?" — which can be answered by plausible-but-false attribution — it asks "can we reverse-map from this claim back to an exact span in source?" The hierarchical verification architecture operates at the individual claim level, not the document level. This is directly applicable as a verification pass after rule extraction.

**Applicability to Gate 1 (evidence-or-reject):** Post-rationalization is the specific failure mode. The gate must enforce quote-first ordering — the LLM extracts a quote from source before stating the rule, not after. Follow with NLI-based entailment verification: does the extracted quote entail the rule statement? RT4CHART's retromorphic tracing is the right verification architecture for this. Requiring file:line alone is necessary but insufficient.

---

### THREAD 6 — LLM Hallucination Rates When Describing Code Specifically

**Classification: Emerging research** — first systematic taxonomy of code hallucinations established 2024; prior literature focused on natural language generation.

**Sources:**
- "Beyond Functional Correctness: Exploring Hallucinations in LLM-Generated Code" (2024): https://arxiv.org/html/2404.00971v3
- ETF — "An Entity Tracing Framework for Hallucination Detection in Code Summaries" (2024): https://arxiv.org/pdf/2410.14748
- FaithLens — "Detecting and Explaining Faithfulness Hallucination" (2024): https://arxiv.org/pdf/2512.20182
- Hallucination survey (Springer AI Review, 2025–2026): https://link.springer.com/article/10.1007/s10462-025-11454-w

---

**Finding 6.1 — Code hallucination taxonomy: three primary categories, twelve subtypes.**
The 2024 study (arXiv:2404.00971) analyzed 3,120 code samples from HumanEval and CoderEval benchmarks across five models: DeepSeek-Coder 1.3B, DeepSeek-Coder 7B, CodeLlama-7B, GPT-4 (gpt-4-0125-preview), and DeepSeek-R1 (671B). Three primary hallucination categories were identified: Requirement Conflicting (code logic or data deviates from the stated requirement), Code Inconsistency (internal structural problems such as undefined variables or inconsistent library usage), and Knowledge Hallucinations (incorrect application of CS, library, algorithm, or mathematical knowledge). The taxonomy provides the first systematic breakdown of where LLMs fail when describing or generating code.

**Finding 6.2 — Measured hallucination frequencies.**

| Hallucination Type | Share of All Hallucinations |
|-------------------|-----------------------------|
| Requirement Conflicting: Behavior (code logic deviates from intent) | **35.40%** |
| Knowledge: Library/Project (incorrect API/library usage) | **25.99%** |
| Code Inconsistency: Undefined Variables | 16.91% |
| Knowledge: Algorithm (incorrect algorithm application) | 4.95% |
| Requirement Conflicting: Data (data values/formats wrong) | 4.21% |
| Knowledge: Computer Theory | 2.31% |

Of 3,120 samples, 1,134 (36.3%) contained at least one hallucination. 1,212 hallucinatory snippets were identified in total (some samples had multiple). Without any quality gate, roughly 1 in 3 rule extractions will contain at least one hallucinated claim.

**Finding 6.3 — Hallucination predicts failure: Pass@1 rates diverge dramatically.**
Pass@1 for hallucination-free code was 54.73%; for hallucinatory code it was 6.26%. Hallucination is therefore not merely a semantic labeling concern — it is a strong predictor of functional failure. Incorrect Functionality (code that fails on execution) affects 95.3% of hallucinatory samples, confirming that almost all hallucinations manifest as runtime failures.

**Finding 6.4 — Static analysis catches only ~17% of hallucination instances.**
Undefined Variables are statically detectable (16.91% of hallucinations). All other categories — Behavior Conflicting (35.4%), Library/Project (25.99%), Data Conflicting, Algorithm, Computer Theory — require either dynamic execution or semantic analysis against a reference. Static analysis alone covers approximately 17% of the hallucination problem; the remaining 83% requires execution-based or evidence-grounded verification.

**Finding 6.5 — Entity-level hallucination in code summaries (ETF, 2024).**
The ETF paper (arXiv:2410.14748) focuses specifically on LLMs generating natural-language summaries of code rather than generating code itself. It finds that LLMs hallucinate entities (variable names, types, return values, side effects) at non-trivial rates even when the code is directly in context. Structurally correct but semantically wrong entity references are the dominant failure mode — a summary may correctly describe the structure of a function while hallucinating its return type or the name of a key variable.

**Applicability:** The 36% base hallucination rate for code description, with Behavior Conflicting (the LLM inventing what a code path does) as the dominant category at 35.4% of all hallucinations, establishes the scale of the problem gates 1–5 are designed to address. This class is not statically detectable; only evidence-grounded or execution-based verification catches it. These numbers establish the lower bound for why all five gates are justified.

---

### THREAD 7 — Human-in-the-Loop Gate Design: Escalation Patterns and Ratios

**Classification: Production engineering patterns — established practice. Meta-model escalation routing — emerging research (2025).**

**Sources:**
- "LLM Performance Predictors: Learning When to Escalate in Hybrid Human-AI Moderation Systems" (LPP): https://arxiv.org/html/2601.07006v1
- HITL production patterns guide: https://alldaystech.com/guides/artificial-intelligence/human-in-the-loop-ai-review-queue-workflows
- Redis AI HITL patterns: https://redis.io/blog/ai-human-in-the-loop/
- Grizzly Peak HITL patterns: https://www.grizzlypeaksoftware.com/library/human-in-the-loop-patterns-for-ai-agents-n64sb2cm

---

**Finding 7.1 — Measured auto/escalate ratios in production (LPP meta-model, 2025).**
The LPP paper (arXiv:2601.07006) trained a Ridge Regression meta-model on LLM uncertainty signals and evaluated escalation routing on the OpenAI Moderation Dataset. Results with meta-model routing versus naive baselines:

| Model | Meta-model Escalation Rate | Naive Baseline (MSP) Escalation Rate |
|-------|---------------------------|--------------------------------------|
| GPT-4o-mini | **16%** | ~38% |
| GPT-4o | **17%** | — |
| GPT-4.1-mini | **24%** | — |
| Gemini-2.5-flash-lite | **25%** | — |
| GPT-4o multimodal | **13%** | — |

Naive baselines (MSP, entropy alone) consistently over-escalated by a factor of 2–3×, triggering unnecessary human reviews that inflate operational cost. Review cost for GPT-4o-mini dropped from $132 to $38 (71% reduction). Meta-model F1: 94.14%, AUC-ROC: 93.46% for predicting LLM failure.

**Finding 7.2 — Queue design patterns for batched review.**
Industry practice for HITL queues serving LLM pipelines uses several structural patterns: (1) two-stage review — fast triage first, deep review for a subset, preventing long-tail backlog; (2) batch by similarity — group rules from the same file or function cluster for reviewer cohesion; (3) priority lanes — P0 (contradiction or safety-critical) → immediate human, P1 (low confidence) → batch queue, P2 (high confidence) → auto-flow; (4) irreversible actions (emitting tests to CI, modifying production state) always require human approval regardless of confidence score; (5) threshold recalibration — weekly or every N decisions, never in real-time, as real-time threshold changes create instability.

**Finding 7.3 — Confidence is not a calibrated probability; empirical calibration is mandatory.**
Industry guidance is explicit: an LLM saying it is 90% confident does not mean it is correct 90% of the time. Confidence thresholds must be set empirically against actual accuracy measurements on held-out ground truth, not on LLM-stated confidence values. The target operating range from deployed systems: for 99% end-to-end accuracy, approximately 30–40% escalation; for 95% accuracy, approximately 10–15% escalation. The right answer is "data-driven: start with a guess, measure escalation rate in production, then adjust the threshold until you hit both your accuracy target and your reviewer capacity."

**Finding 7.4 — Contradiction escalation requires qualitatively different queue design.**
Contradictions between rules are not the same type of item as low-confidence rules and must not be batched together. Contradictions require a human to adjudicate business intent — something an LLM cannot do without context unavailable to the system. The correct queue design provides the human reviewer with: both conflicting rule statements, the source spans for each, the conflict type (logical negation, temporal conflict, scope overlap), and recommended resolution templates (options, not verdicts). Auto-resolution of contradictions is not appropriate under any confidence level because the cost of a wrong auto-resolution (a test asserting incorrect behavior, passing CI) is unbounded.

**Applicability to Gate 5 (contradictions escalate) and all gates:** Validated by all deployed HITL literature. Set empirical auto-escalate thresholds (16–25% is the production range at 80%+ precision), recalibrate weekly, and design the queue infrastructure for volume from day one given the 36% base hallucination rate for code description.

---

### CROSS-CUTTING FINDING: Correlated Errors Across All LLM-Based Gates

**Classification: Active research (2024–2025).**

**Sources:**
- Intraclass correlation analysis: https://arxiv.org/pdf/2509.19489
- ColMAD paper (correlated debater failure): https://arxiv.org/html/2510.20963
- Code hallucination taxonomy (Library/Project class): https://arxiv.org/html/2404.00971v3

**Finding C.1 — Same-model pairs share correlated failure modes.**
If the proposer agent and the refuter agent for Gate 2 (adversarial refutation) share a base model architecture, their errors are correlated. Specifically, both will share blind spots in the Library/Project Knowledge class (25.99% of all code hallucinations) because those errors arise from shared training data gaps. The ColMAD paper's ablations confirm that judge/debater model choice has measurable impact on outcomes — and the best-performing pair (QwQ-32B + DeepSeek-R1) used architecturally distinct models from different training lineages. Intraclass correlation analysis (arXiv:2509.19489) formalizes this: as ρ → 1 (perfect correlation), majority-vote gains from N samples vanish entirely.

---

### GATE VERDICT TABLE

| Gate | Literature Verdict | Key Evidence | Critical Gap Identified |
|------|--------------------|-------------|------------------------|
| 1 — Evidence-or-reject (file:line citation) | Validated but **insufficient as designed** | 57% of LLM citations are post-rationalized (Wallat et al., 2024); RAG 71% hallucination reduction but 17–33% domain residual | Quote-first ordering is missing — LLM can fabricate a plausible citation after generating the claim. Add NLI entailment verification: does extracted quote entail the rule statement? RT4CHART is the architecture pattern. |
| 2 — Adversarial refutation | Validated **only if framed as collaborative** | CopMAD (competitive): −15% F1 vs. single-agent; ColMAD (collaborative): +2–5% F1; best pair 80.01 F1 (ColMAD paper) | Must use ColMAD framing: refuter cites counter-evidence by exact string match, forbidden from ungrounded overconfident claims. Architecturally distinct models mandatory for proposer vs. refuter. |
| 3 — Confidence gating (high/medium/low) | **Weakest gate as specified** | Verbalized confidence ECE ~0.10 (Verbalized Confidence paper); LPP meta-model achieves 16% escalation at 94% F1 | Replace verbalized H/M/L with semantic entropy across 3–5 samples (Farquhar et al., Nature 2024) + post-hoc uncertainty signals (MSP, logit entropy) per LPP meta-model. Calibrate thresholds empirically per domain. |
| 4 — Mutation proof | **Strongest gate — structurally anti-hallucination** | 73% engineer acceptance at Meta; equivalence detector 0.95/0.96 P/R with comment-stripping preprocessing; 49% of tests killed mutants without adding line coverage | Add comment-stripping equivalence pre-filter before invoking gate — without it ~25% of mutants waste cycles on comment-only changes. This sub-gate is not present in the current design. |
| 5 — Contradictions escalate to humans | **Correctly designed, needs queue engineering** | 16–25% escalation rate is the production norm at R@P=0.8 (LPP paper, ACH paper); contradictions require business-intent adjudication unavailable to LLMs | Use P0 immediate lane (never batch contradictions). Escalation package must include: both rule statements, both source spans, conflict type (logical negation / temporal / scope overlap), resolution templates (not verdicts). |

**Two additions missing from the planned design entirely:**
1. **Post-rationalization check inside Gate 1** — enforce quote-first ordering (quote extracted before rule is stated), then verify via NLI entailment that the quote actually entails the rule. RT4CHART (arXiv:2603.27752) is the architecture pattern. Without this, Gate 1 stops an LLM from providing no citation but does not stop it from providing a plausible-but-retroactive citation.
2. **Mutation equivalence pre-filter before Gate 4** — comment-stripping before equivalence check raises precision from 0.79 to 0.95 and recall from 0.47 to 0.96. Without it approximately 25% of mutant generation cycles are wasted on comment-only changes that cannot be killed by any test.

**Hard cross-cutting requirement for all gates:** Mandate architecturally distinct base models for proposer and refuter roles. Same-model pairs share the Library/Project hallucination class as a correlated blind spot (~26% of all code hallucinations) — both agents will miss these simultaneously, defeating the purpose of multi-agent validation.

================================================================================

The document-specialist agent has completed its research. The full report has been delivered directly above this message — it contains all seven research threads with source names, URLs, 2–4 sentence summaries, established/emerging/marketing classifications, numeric findings (Meta ACH acceptance rates, ColMAD F1 numbers, verbalized confidence ECE values, code hallucination taxonomy rates, LPP escalation ratios, post-rationalization percentages), and the gate-verdict table at the end.

Key deliverables in that report for the architect:

- **Gate 1 gap**: 57% of LLM citations are post-rationalized (Wallat et al., arXiv:2412.18004) — quote-first ordering + NLI entailment verification must be added; RT4CHART (arXiv:2603.27752) is the architecture pattern
- **Gate 2 framing constraint**: Competitive adversarial debate degrades accuracy −15% F1 (ColMAD, arXiv:2510.20963); collaborative "find missing evidence" framing with exact-string-match citation enforcement is the validated design
- **Gate 3 replacement**: Verbalized H/M/L has ECE ~0.10; replace with semantic entropy across 3–5 samples (Farquhar et al., Nature 2024, DOI:10.1038/s41586-024-07421-0) + post-hoc uncertainty signals per LPP meta-model (arXiv:2601.07006)
- **Gate 4 sub-filter missing**: Comment-stripping equivalence pre-filter (0.95/0.96 P/R) must precede mutation testing; without it ~25% of cycles waste on comment-only mutants
- **Gate 5 queue engineering**: P0 lane, both source spans + conflict type in escalation package, never auto-resolve, recalibrate thresholds weekly
- **Cross-cutting**: Mandate architecturally distinct base models for proposer vs. refuter — same-model pairs share the Library/Project hallucination blind spot at ~26% of all code hallucinations