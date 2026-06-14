# Health Score

Sprint health metric interpolation and RAG (Red/Amber/Green) classification rules.
Used by `SprintSummaryCalculator` and `QaMetricsCalculator`.

Rules below are **verified** via the Mine→Verify pass (clean-room extraction from source +
independent Codex refutation, 2026-06-13). They correct the earlier hand-authored draft —
notably the `range==0` guards, which are dead code, not clamps.

## Rules

- BR1: `ScoreHigherIsBetter` — `value >= green` → 100 (green inclusive); `value == amber` → exactly 50; `value == 0` → 0.
- BR2: `ScoreHigherIsBetter` — `[amber, green)` interpolates linearly 50→99 (never 100; green itself returns via BR1); `[0, amber)` with `amber > 0` interpolates linearly 0→49, negatives floored to 0 by `Math.Max`.
- BR3: `ScoreLowerIsBetter` — `value <= green` → 100 (green inclusive); `value == amber` → exactly 50; `value == 2×amber` → 0 (hard floor).
- BR4: `ScoreLowerIsBetter` — `(green, amber]` interpolates linearly 99→50; `(amber, 2×amber]` interpolates linearly 49→0; values above 2×amber clamped to 0.
- BR5: `CompositeRag` — `score >= 75` → "green"; `40 <= score < 75` → "amber"; `score < 40` → "red".
- BR6: `MetricRag` (higher-is-better) — `value >= green` → "green"; `value >= amber` → "amber"; else "red". Inclusive on the better side.
- BR7: `MetricRag` (lower-is-better) — `value <= green` → "green"; `value <= amber` → "amber"; else "red". Compared on the raw value, inclusive on the better side.

## Key Files

- `src/Services/Fokus/Fokus.Domain/Analytics/HealthScoreCalculator.cs` — production implementation
- `src/Services/Fokus/Fokus.Domain.Tests/Analytics/HealthScoreCalculatorTests.cs` — example-based tests; currently cover BR1–BR5. **BR6/BR7 (`MetricRag`) are untested** — first target for Step 3 (Cover).

## Edge Cases

- **Dead code, NOT clamps (corrected):** the `range==0` (green==amber) guards returning 50 — `ScoreHigherIsBetter` L18 and `ScoreLowerIsBetter` L35 — are **unreachable for all inputs**. Reaching either needs `value≥amber ∧ value<green ∧ green==amber` (a contradiction); the equality case returns 100 at the green check. They are defensive dead code: their Stryker mutants are not killable by black-box tests and are **excluded from the mutation scope**.
- **Boundary discontinuity at `value==amber`:** `ScoreLowerIsBetter` returns 50 at `value==amber` (amber band) while the above-amber formula → 49 as `value→amber⁺`; `ScoreHigherIsBetter` has the symmetric jump (below-amber branch → 49 as `value→amber⁻` vs the band's 50). The limit gap is 1; any concrete off-boundary value gives a larger gap.
- **`amber == 0` guard (`ScoreHigherIsBetter` L22):** returns 0; only reachable for `value < 0` (non-negative values route into the amber band first).
- **`cap <= amber` guard (`ScoreLowerIsBetter` L40):** returns 0 when `amber <= 0` (non-positive cap).
- **Negative-`amber` pathology (unintended — candidate bug, not an intended rule):** with `amber < 0`, the below-amber branch of `ScoreHigherIsBetter` (`value/amber*49`) exceeds 49 (e.g. `amber=-10, value=-20 → 98`). Unguarded; production never passes negative thresholds. Tracked for backlog, not gated.
- **Inverted thresholds:** `green<amber` (higher-is-better) or `green>amber` (lower-is-better) make the respective amber band dead code — all inputs short-circuit at the green check.

## Relationships

- `HealthScoreCalculator` → `SprintSummaryCalculator`: provides per-metric scores that are weighted into a composite score.
- `HealthScoreCalculator` → `QaMetricsCalculator`: same scoring logic applied to QA-specific metrics.
- `CompositeRag` → frontend RAG badge: the string values "green"/"amber"/"red" are wire literals consumed by the Vue client.

## Source

Original Fokus `fokus-spec.md` §Health Score; ported implementation in `SprintSummaryCalculator` (step 3c-A).
Rule verification: Mine→Verify pilot (Pass 2), clean-room miner + Codex refutation, 2026-06-13.

---
<!-- status: mutation-gated — all reachable mutants killed (100%, 70/70), Step 3 (Cover) complete 2026-06-13 -->
<!-- last-stryker-run: 2026-06-13 — per-file scoped run (HealthScoreCalculator.cs only); 70 mutants tested, 70 killed, 2 excluded (dead-code range==0 guards), score 100% -->
