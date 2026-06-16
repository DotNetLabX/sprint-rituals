# Lessons — adhoc-Pass6-AnalyticsSuffixAlign

## Architect Lessons

### Rename reference sweeps must grep ALL textual occurrences, not just compile-significant references
The Explore reference sweep enumerated code references (DI registration, constructor injection, the static call, one doc comment in `MetricCardData.cs`) but missed **comment-only** mentions of the old names: `MetricCardBuilder.cs:6`, `QaMetricsResult.cs:27`, `TestTimelineResult.cs:90`, two code comments in `GetQaTrendsEndpoint.cs`, and a cross-reference comment inside a renamed file (`TestTimelineCalculator.cs:67`). The code-grounded critic caught all six.

**Why it matters:** the plan's own Step 3 grep-guard requires *zero textual hits* of the old names in `src/`. A reference table built only from compile-significant sites is therefore guaranteed to under-count what the gate enforces — Step 2 (work list) can't satisfy Step 3 (acceptance). The failure is silent in a feature plan but loud in a rename plan because the grep-guard is the gate.

**How to apply next time:** when dispatching the reference-sweep for a rename/type-move, ask the helper for **every textual occurrence** (a plain `grep oldname src/`), then bucket into code references vs comment/doc occurrences — both go in the plan. Better: derive Step 2's list from the *exact* grep query used in Step 3's acceptance, so the work list and the gate are the same query by construction (the `create-implementation-plan` refactor rule: "derive the file list from the exact grep used in its acceptance criterion"). [[subagent-findings-stranding]] is adjacent — Explore returned solid findings here, but scoped to the wrong predicate (references, not occurrences).

### Code-grounded critic was the right call for a live-file rename
The user chose critic over my recommended self-review. A doc-only or in-context critic would not have found the 6 missed sites — only a fresh grep of the live tree did. Confirms the role's guidance: for any pass whose correctness depends on the current state of live files, the load-bearing gate is a code-grounded review. I recommended self-review on the grounds that "the developer's build catches it"; that was wrong — a comment miss does **not** break the build, so the build would have stayed green while the grep-guard failed. Lesson: weight self-review lower whenever the acceptance gate is a *textual* grep rather than the compiler.

### Owner suffix decisions cleanly separated from architect scoping
The 7→Analyzer classification and the two carve-outs (Leaderboard stays Calculator; HealthScoreCalculator untouched) were owner calls surfaced via AskUserQuestion; the reference enumeration, wire-stability, and KB/coordination handling stayed architect-owned. The split worked — no re-litigation of the owner's naming in review.

## Developer Lessons

### `git mv` before `Edit` — read the new path after rename
When renaming files with `git mv`, the Edit tool requires a Read of the new path before it will accept edits. The old path is gone; the tool's file-context tracking doesn't follow renames. Pattern: `git mv old new` → `Read new` → `Edit new`. Batching all 9 git renames first, then reading all 9 new paths in a single parallel call, then editing, is efficient.

### All-None rename plans: no skill invocations means no skill log — and that's expected
The plan's Skill Mapping is all-None with an explicit exemption documented in the plan itself. The done-check should not flag an empty `skill-invocations.log` as a gap for this pass. Record this in implementation.md's `## Skills Used` section with the plan citation so the architect sees it at a glance.

### Local variable names in FastEndpoints ctor parameters are independent of type names
When a FastEndpoints primary constructor injects `SprintSummaryCalculator sprintSummaryCalculator`, renaming the type to `SprintSummaryAnalyzer` leaves the local name `sprintSummaryCalculator` valid C# (parameter name ≠ type name). The plan's "type only — local var name unchanged" note is correct — no cascading rename of call sites using the local variable is needed. This keeps the diff minimal and the intent clear.

## Reviewer Lessons

### Pre-commit prediction + targeted grep beats passive reading for rename reviews
For pure-rename passes, passive reading misses nothing the compiler catches but misses everything textual (comments, doc strings, KB references). The right review shape is: (1) predict the tricky sites from the plan (dual-occurrence files, cross-ref comments, static calls, pending test file names in docs); (2) run targeted grep checks for each prediction; (3) run the plan's own grep-guard commands to produce fresh evidence rather than trusting implementation.md claims. This pass hit all 5 pre-committed predictions as confirmed-correct rather than findings — which is the best outcome: the plan's tricky sites were correctly handled, and the review produced independent evidence.

### Wire-stability check via `git diff --name-only` scope scan is fast and sufficient
For a rename-only pass, the full wire-stability check reduces to: enumerate changed files, classify each (renamed class, reference site, comment-only, doc) — if nothing outside those categories appears, the diff is wire-clean. No need to grep every route string or contract file individually; the file classification is the gate.

## Skill Gaps

None — this was an all-None plan by design. No skill for mechanical renames; `analytics-computation-service` provides the naming authority, not a structural pattern to follow.
