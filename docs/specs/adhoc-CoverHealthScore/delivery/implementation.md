# Implementation — adhoc-CoverHealthScore

## Mutation Gate Result

**File:** `src/Services/Fokus/Fokus.Domain/Analytics/HealthScoreCalculator.cs`
**Stryker version:** 4.14.2
**Run date:** 2026-06-13
**Mutants tested:** 70
**Killed:** 70
**Survived:** 0
**Ignored (Stryker disable comments):** 2 (dead-code range==0 guards — L18/L35 of the pre-comment source)
**Mutation score:** 100.00%
**Break threshold reached:** 75 (then tightened — final run at 100%)

## What Was Done

### 1. Dead-code guard exclusions (HealthScoreCalculator.cs)

Added `// Stryker disable once all : <reason>` on the two provably unreachable guards:
- `ScoreHigherIsBetter` — `if (range == 0) return 50` (reaching it requires `value>=amber AND value<green AND green==amber` — contradiction)
- `ScoreLowerIsBetter` — `if (range == 0) return 50` (reaching it requires `value>green AND value<=amber AND amber==green` — contradiction)

Both reference `docs/kb/health-score.md Edge Cases` in the reason string. Production calculator is otherwise unchanged.

### 2. stryker-config.json

- Added `"mutate": ["**/Analytics/HealthScoreCalculator.cs"]` to scope mutation to the single target file (smoke run had mutated all of Fokus.Domain — noise).
- Added `"thresholds": { "break": 75 }` (gate threshold).
- Retained: `test-runner: mtp`, `coverage-analysis: off`, no `global.json`.

### 3. HealthScoreCalculatorTests.cs — new/expanded tests (62 total, up from 18)

**BR6/BR7 — MetricRag (previously zero tests):**
- BR6 (higher-is-better): green-exact, above-green, amber-exact, between-amber-and-green, below-amber, zero, just-below-amber (→red), just-below-green (→amber, not green).
- BR7 (lower-is-better): green-exact, below-green, amber-exact, between-green-and-amber, above-amber, high-value, just-above-amber (→red), just-above-green (→amber, not green), degenerate green==amber case.
- Direction-discriminator test: same value + same thresholds + opposite `higherIsBetter` → different outcomes.

**BR2/BR4 amber discontinuity pins:**
- `ScoreHigherIsBetter`: `value==amber` → exactly 50; `value==amber-ε` → <50.
- `ScoreLowerIsBetter`: `value==amber` → exactly 50; `value==amber+ε` → <50.

**Characterization test — negative-amber pathology:**
- `Characterization_NegativeAmber_ProducesAbove49_HigherIsBetter`: `amber=-10, value=-20 → 98`. Tagged as candidate bug; asserts ACTUAL value; NOT gated as intended behavior.

**Additional mutant-killing tests for BR1–BR4:**
- Exact linear interpolation values for amber band and below-amber band (higher-is-better).
- Exact linear interpolation values for green-amber band and above-amber band (lower-is-better).
- `value==0 → 0`, negative value with positive amber → clamped to 0.
- `value` just approaching green from below (→ <100 in amber band).
- `value` just above green (lower-is-better) → approaches 99 from below.
- `value > 2×amber` → clamped to 0.
- `amber==0` in above-amber branch (lower-is-better) → cap==0, guard fires → 0 (kills cap<=amber → cap<amber mutant).

## Surviving Mutants Triaged

None. All 70 reachable mutants killed.

The 2 Stryker-disabled mutants are equivalent/unreachable by black-box tests (the `range==0` guards are dead code per KB Edge Cases — verified in Mine→Verify pass).

## MetricRag Coverage

BR6 and BR7 are now covered. All four RAG outcomes for each direction are exercised with boundary pins on both the green and amber thresholds (inclusive edges verified). The `higherIsBetter` flag discriminator is tested.

## Negative-Amber Backlog Note

Filed in `docs/backlog.md` under "Candidate Bug — Negative-Amber Pathology in ScoreHigherIsBetter". Low priority — production invariant prevents negative thresholds today. Three resolution options documented.

## Final Break Threshold

Gate cleared at **100%** (target was 75, tightened to 80, reached 100 after killing the cap<=amber surviving mutant with the `amber==0` test).
