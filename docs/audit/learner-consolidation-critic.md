# Critic Review — Learner Consolidation into Nexus Plugin Source

**Verdict: GO-WITH-FIXES**

Code-grounded review (every target file + all 7 source lessons read live). The consolidation is **largely faithful and technically accurate** — the high-risk Pass-2 Mapster erratum is encoded **correctly** (the corrected whitelist/no-op semantics, not the wrong original). Routing is correct (.NET → nexus-dotnet, flow → nexus). Version/CHANGELOG hygiene is clean and matches the PATCH-default policy. The fixes below are 1 MEDIUM routing/completeness gap, plus LOWs; none block the consolidation, but two are worth applying before close.

Scope reviewed: 6 nexus-dotnet edits + 3 nexus edits + 2 CHANGELOGs + 2 plugin.json bumps, cross-referenced against `adhoc-Pass0…Pass3c-B` lessons.

---

## CRITICAL

None.

## HIGH

None. (Explicitly: the item the brief flagged as HIGH-RISK — the Pass-2 `IgnoreNonMapped` erratum — is **clean**. See "Clean promotions" below.)

## MEDIUM

### M1 — Pass-3b lesson directed the `required`+`private set` rule to `domain-patterns`; it landed only in `conventions/csharp.md`. The named encapsulation home got nothing.
**Where:** `nexus-dotnet/skills/domain-patterns/SKILL.md` (`## Entity`, line 25; `### When to Use a Value Object vs a Flat Scalar`, line 57) — rule absent. Rule is at `nexus-dotnet/conventions/csharp.md:23`.
**What's wrong:** The Pass-3b lesson is explicit (`adhoc-Pass3b-AppSettingsVOs/delivery/lessons.md:24`): *"Fold this into the `domain-patterns` encapsulation guidance."* The Developer Lesson (line 32) frames it as an encapsulation rule (`Person.UpdateFromGoogle`, factory-only construction, private ctor). The Learner promoted it **only** to `csharp.md` Type Conventions. A grep of `domain-patterns/SKILL.md` for `required|private set|CS9032|init|encapsulat` returns zero hits, even though that skill has an `## Entity` section and an ADR-007 VO section — exactly the "encapsulation guidance" the lesson pointed at. The csharp.md text is correct and well-written, so this is not a distortion; it is an **incomplete routing**: the agent who reaches for `domain-patterns` while encapsulating an aggregate (the actual trigger context) won't see the CS9032 trap.
**Severity rationale:** MEDIUM not LOW because the rule is most needed precisely when someone is following `domain-patterns` to tighten setters on a behavior-bearing entity — the exact scenario that produced the lesson. csharp.md is read by developer/reviewer/solo per its header, so it isn't invisible; that's why it isn't HIGH.
**Fix:** Add a one-line cross-reference in `domain-patterns/SKILL.md` `## Entity` section: "When tightening an entity's setters to enforce factory-only construction, do not write `required` + `private set` — that is CS9032 (see `conventions/csharp.md` → Type Conventions). Use `init`, or drop `required` + add a private ctor." Keep the full rule in csharp.md; just point to it from the encapsulation context the lesson named.

## LOW

### L1 — `domain-service` envelope note and `analytics-computation-service` envelope note are near-duplicates; confirm intent, not accidental divergence.
**Where:** `nexus-dotnet/skills/domain-service/SKILL.md:134` and `nexus-dotnet/skills/analytics-computation-service/SKILL.md:111`.
**What's wrong:** Both encode the `{Mode, Multi?, Single?}` `*Response`-is-wire-only rule. They do **not** contradict each other (good — I checked: same semantics, same "endpoint assembles / never mirror into domain / never in a domain rename table"). The analytics one correctly cross-references `domain-service → Output Boundary`. This is intentional reinforcement, defensible because analytics-computation-service is a specialization read on its own. No contradiction — but the two phrasings drift slightly ("Many analytics features wrap…" vs "Each analytics feature wraps…"). Pure polish.
**Fix (optional):** None required. If you want single-source-of-truth, demote the analytics one to a bare pointer ("envelope is wire-only — see `domain-service` → Output Boundary") and drop the restated body. Not necessary; the duplication is coherent.

### L2 — `create-implementation-plan` rename-table rule embeds a Refit/JSON-key claim that is correct but states the property→key casing transform as if universal.
**Where:** `nexus/skills/create-implementation-plan/SKILL.md:98`.
**What's wrong:** The text says a C# rename `AlertThreshold → BugRatioAlertThreshold` "changes the JSON key to `bugRatioAlertThreshold`." That is correct **for this project** (System.Text.Json camelCase policy, which SprintRituals uses), and the Pass-2 lesson (`adhoc-Pass2`/line 20) is the faithful source. But the rule is being promoted into a **stack-agnostic** plan skill; a consuming project with default (PascalCase) JSON would see `BugRatioAlertThreshold`. The claim is not wrong for the evidence, just under-qualified for a shared skill.
**Fix:** Add three words: "…changes the JSON key (under a camelCase policy) to `bugRatioAlertThreshold`…". One-line guard; keeps the worked example.

### L3 — CHANGELOG line for nexus-dotnet says "proven (2+ occurrence) and critical lessons" but two promoted items are 1-occurrence.
**Where:** `nexus-dotnet/CHANGELOG.md:4`.
**What's wrong:** The header reads "proven (2+ occurrence) **and** critical lessons" — which is actually the *correct* framing (the "and critical" clause covers the two 1-occurrence immediate-promotes). This is fine. Flagging only to confirm I checked it: the wording does carry the carve-out. No fix needed; included for completeness so you know it was verified, not skipped.

---

## Detailed verification of the brief's 7 check areas

### 1. Faithfulness / distortion — PASS (including the HIGH-RISK erratum item)
- **Pass-2 `IgnoreNonMapped` erratum (the flagged high-risk item): CLEAN.** Source `adhoc-Pass2-SliceHygiene/delivery/lessons.md:25` carries the `⚠️ ERRATUM` correcting the WRONG original ("IgnoreNonMapped prevents nulling"). The promotion in `persistence-patterns/SKILL.md:105-108` encodes **only the corrected semantics**: whitelist-not-shield, zero `.Map()` = silent no-op, `.Adapt(existing)` never nulls unmatched → no guard needed, register nothing. A repo-wide grep for any residual "prevents nulling"/"protect unmatched" phrasing finds **zero** instances of the wrong original anywhere in either plugin. The distortion risk the brief worried about did not occur.
- Build-verification para (`csharp.md:37`) faithfully matches Pass-0/Pass-3c-B (`cd /d` cmd-flag, exit-1 masquerade).
- EF RenameColumn (`ef-core.md:19`) faithfully matches Pass-3b (`BugRatioTarget → BugRatio_Target`, seeded-singleton data loss).
- Mapster eligibility test (`Mappings.md:41-51`) faithful to Pass-2 (method-dispatch / multi-source / conditional carve-outs, all three named correctly).
- Prefix-flatten caveat (`Mappings.md:63-65`) faithful to Pass-3b (`settings2.X` member-name anchor included).
- Envelope rule (domain-service / analytics) faithful to Pass-3c-B Finding 3.
- Refactoring/type-move rules (`create-implementation-plan:86-100`) faithfully aggregate Pass-0/2/3/3c/3c-B (definition-line grep, enumerate-consumers, grep-before-delete, homonym carve-outs incl. the `"neutral"` literal, rename table, GlobalUsings grep). The `EndpointWithoutRequest<MeResponse>` undercount example is faithful to Pass-2.
- architect.md adhoc branch (115-126) + Phase-2 critic disclosure (165-166) faithful to Pass-0/1/3/3b.
- agents-workflow.md three failure modes (59-61) faithful to Pass-3b (Bash-printf), Pass-3c-B (mv, original-dispatch-verb), Pass-1/3b (foreign-repo blind spot).

### 2. Technical accuracy — PASS
- **CS9032:** Correct. `required` member must be at least as accessible as its setter; `required` + `private set` does not compile. csharp.md:23 states it precisely and the remedies (drop `required`+private ctor, or `init`) are both valid.
- **EF scaffolds drop+add, not rename:** Correct. EF Core's migration differ has no rename detection across a property regrouping; it emits `DropColumn`+`AddColumn`, which loses data. `RenameColumn` is the correct hand-edit. Accurate.
- **`IgnoreNonMapped(true)` disables name-convention matching:** Correct per Mapster semantics (verified against the Pass-2 erratum's Mapster 7.4.0 finding). It restricts to explicit `.Map()` resolvers; with none, it maps nothing. Accurate.
- **`.Adapt(onto existing)` never nulls unmatched destination members:** Correct — Mapster's adapt-onto-existing default does not touch unmatched destinations. Accurate.
- **protobuf-net keys wire on `[DataMember(Order)]`; Refit/JSON on property name:** Correct. protobuf-net.Grpc serializes by field Order, not C# name; Refit/System.Text.Json by property name (subject to naming policy — see L2). Accurate.
- **ComplexProperty/owned VOs need explicit assignment + `SetValues` doesn't deep-copy owned members:** Correct EF Core behavior. Accurate.
- No overstated or wrong technical claim found.

### 3. Routing — PASS (one completeness caveat, M1)
- All .NET/code rules (build verb, CS9032, EF RenameColumn, Mapster facts, envelope rule) → `nexus-dotnet`. Correct.
- All flow/process rules (refactoring plan rules, adhoc-pass architect branch, critic disclosure, pipeline-gate failure modes) → `nexus`. Correct.
- No misrouted item. The only routing issue is M1: a rule landed in the right plugin (`nexus-dotnet`) but a *different file* (`csharp.md`) than the lesson named (`domain-patterns`) — an intra-plugin completeness gap, not a cross-plugin misroute.

### 4. Coherence / contradiction — PASS
- **persistence-patterns tracked-entity section vs create-feature `command.Adapt<{Entity}>()` "Inline Mapping" line:** NO contradiction. The brief flagged this as a risk. Verified: `Mappings.md:58` (`var entity = command.Adapt<{Entity}>()`) is explicitly scoped "inbound: command → **NEW** entity (new object)", and the note at `Mappings.md:61` draws the exact distinction ("both create a **new** object; the dangerous case is the inbound partial update onto an already-tracked entity") and routes the onto-existing case to persistence-patterns. The two sections are complementary, not conflicting.
- **domain-service envelope note vs analytics-computation-service envelope note:** NO contradiction (see L1) — same rule, analytics cross-refs domain-service.
- **persistence-patterns new section vs its own existing "non-clobber" content:** Coherent. The new section (105-112) supersedes the old (erroneous) mental model cleanly; the erratum's wrong claim does not survive anywhere.
- **csharp.md CS9032 rule vs domain-patterns:** No contradiction; the gap is absence (M1), not conflict.

### 5. Recurrence threshold — PASS
- **`required`+`private set` (CS9032):** 1 occurrence (Pass-3b only). Justified as build-breaking critical-immediate-promote. **Justification holds** — it is a hard compile error; encountering it once and not recording it guarantees re-encountering it. Valid.
- **EF RenameColumn:** 1 occurrence (Pass-3b only). Justified as data-loss critical-immediate-promote. **Justification holds** — silent production data destruction on a seeded singleton is exactly the "critical regardless of count" class. Valid.
- **Other 1-occurrence items promoted WITHOUT the build-breaking/data-loss justification:** none found. I checked each promoted item against its source-lesson occurrence count: build verb (Pass-0 + Pass-3c-B = 2), Mapster eligibility (Pass-2, but the erratum/no-op family recurs Pass-2 + Pass-3b = 2), envelope rule (Pass-3c-B, with "will recur in 3c-C" noted — promoted at 1 but it's a flow-doc rule the lesson explicitly forecast recurring; borderline but defensible and not build-breaking-justified-falsely), refactoring rules (each recurs 2+: file-list Pass-2+3c+3c-B, grep-before-delete Pass-3c+3c-B, homonym Pass-0+1+3c-B, GlobalUsings Pass-3). The envelope rule at 1 occurrence is the only item promoted on a "will recur" forecast rather than 2 actual + critical; it's low-risk (a doc note, reversible) and the lesson's forecast is well-grounded, so not flagged as a violation. No item falsely claims build-breaking/data-loss to justify a 1-occurrence promote.

### 6. Completeness — PASS (one miss = M1; pipeline-gate deferral is the RIGHT call)
- **Scanned all 7 lessons for 2+-feature recurring patterns NOT promoted:** The encapsulation routing miss (M1) is the one substantive gap, and even there the *rule* was promoted (to csharp.md) — only its placement is incomplete. No fully-dropped 2+-occurrence pattern found. `[TRACKED]` items (behavior-preserving side-effect enumeration, static-method-on-injected-class CS9113 at 1 occurrence, shared-domain-type-vs-DRY tension) are correctly held below the bar — I agree with each: side-effect enumeration is folded into the refactoring rules implicitly; CS9113 is 1-occurrence and not build-breaking; the DRY tension is a per-pass design judgment, not a mechanical rule.
- **`pipeline-gate.js` deferral (document the workaround, don't auto-apply the code change):** **Correct call.** Three independent lessons (Pass-1 PR, Pass-3b, Pass-3c-B) all converge on the same point: the runtime gate change (two-repo-awareness / honor a phase marker) is a *behavior* change to an enforcement hook, flagged for owner approval. The Learner correctly documented the three failure modes + the human workaround (team lead advances the token / enforces the checkpoint manually) in `agents-workflow.md:59-61` and left `pipeline-gate.js` untouched with the improvement tracked. Auto-applying a gate behavior change from a consolidation pass would be exactly the kind of un-reviewed runtime change the no-self-advance rule warns against. The deferral is consistent with the plugin's own governance.

### 7. Version / CHANGELOG hygiene — PASS
- `nexus-dotnet/.claude-plugin/plugin.json`: `1.0.1` ✓ (was 1.0.0). PATCH bump.
- `nexus/.claude-plugin/plugin.json`: `1.2.3` ✓ (was 1.2.2). PATCH bump.
- **PATCH-default policy (release-plugin skill, ADR-9):** both bumps are PATCH. Correct — these are doc/convention additions (no new user-facing capability → not MINOR; no breaking change or behavior reversal → not MAJOR). Matches the skill's "Default is PATCH; owner escalates" rule.
- Both CHANGELOG entries dated 2026-06-08, accurately enumerate every changed file, and the descriptions match the actual edits (spot-checked each bullet against the file). nexus-dotnet:4 "proven (2+ occurrence) **and** critical" correctly carries the carve-out for the two 1-occurrence immediate-promotes (see L3).
- marketplace.json version (1.0.0) unchanged — correct; the marketplace manifest version is independent of per-plugin versions and the skill doesn't bump it on a content change.

---

## Clean promotions (called out explicitly per the brief's request)

These were checked adversarially and found **faithful, accurate, and coherent** — no invented problems:

1. **The Pass-2 Mapster erratum** — encodes the corrected semantics, the wrong original is nowhere in the repo. (The brief's designated highest-risk item: clean.)
2. **The command.Adapt inline-mapping coherence** — the new/onto-existing distinction is drawn explicitly; no contradiction with persistence-patterns. (The brief's designated coherence risk: clean.)
3. **The two envelope notes** — consistent, cross-referenced, no clash. (The brief's designated duplicate/clash risk: clean.)
4. **CS9032 and RenameColumn technical claims** — both technically correct.
5. **All version bumps + CHANGELOGs** — accurate and policy-conformant.
6. **The pipeline-gate.js deferral** — the right governance call.

---

## Recommended action before close

- **Apply M1** (one-line cross-ref from `domain-patterns` `## Entity` → csharp.md CS9032). Cheap; closes the only substantive gap, and honors the explicit Pass-3b lesson directive.
- **Optionally apply L2** (three-word camelCase qualifier in create-implementation-plan) — cheap correctness guard for a shared skill.
- L1 and L3 need no action.

Reviewed: <plugin-repo>\plugins (nexus 1.2.3 + nexus-dotnet 1.0.1) against docs/specs/adhoc-Pass{0,1,2,3,3b,3c,3c-B}/delivery/lessons.md
