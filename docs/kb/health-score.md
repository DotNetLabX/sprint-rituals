# Health Score

Sprint health metric interpolation and RAG (Red/Amber/Green) classification rules.
Used by `SprintSummaryCalculator` and `QaMetricsCalculator`.

## Rules

- BR1: `ScoreHigherIsBetter` — value >= green threshold yields 100; value at amber yields exactly 50; value = 0 yields 0.
- BR2: `ScoreHigherIsBetter` — [amber, green) interpolates linearly between 50 and 99 (not 100); [0, amber) interpolates linearly between 0 and 49.
- BR3: `ScoreLowerIsBetter` — value <= green threshold yields 100; value at amber yields a value in (50, 99]; value at 2×amber yields 0 (hard floor).
- BR4: `ScoreLowerIsBetter` — (green, amber] interpolates linearly between 99 and 50; (amber, 2×amber] interpolates linearly between 49 and 0; values above 2×amber are clamped to 0.
- BR5: `CompositeRag` — score >= 75 → "green"; 40 <= score < 75 → "amber"; score < 40 → "red".
- BR6: `MetricRag` — for higher-is-better: value >= green → "green", value >= amber → "amber", else "red". For lower-is-better: thresholds are reversed.

## Key Files

- `src/Services/Fokus/Fokus.Domain/Analytics/HealthScoreCalculator.cs` — production implementation
- `src/Services/Fokus/Fokus.Domain.Tests/Analytics/HealthScoreCalculatorTests.cs` — example-based tests covering BR1–BR5

## Edge Cases

- `amber == 0` in `ScoreHigherIsBetter`: returns 0 immediately (avoid divide-by-zero).
- `green == amber` in `ScoreHigherIsBetter`: range = 0 branch returns 50 (clamp).
- `green == amber` in `ScoreLowerIsBetter`: range = 0 branch returns 50 (clamp).
- `cap <= amber` in `ScoreLowerIsBetter`: returns 0 immediately (degenerate config guard).
- Values beyond the hard caps are clamped via `Math.Max(0, ...)` — no negative scores produced.

## Relationships

- `HealthScoreCalculator` → `SprintSummaryCalculator`: provides per-metric scores that are weighted into a composite score.
- `HealthScoreCalculator` → `QaMetricsCalculator`: same scoring logic applied to QA-specific metrics.
- `CompositeRag` → frontend RAG badge: the string values "green"/"amber"/"red" are wire literals consumed by the Vue client.

## Source

Original Fokus `fokus-spec.md` §Health Score; ported implementation in `SprintSummaryCalculator` (step 3c-A).

---
<!-- mutation-gated: true -->
<!-- last-stryker-run: 2026-06-13 — killed: >0 / total mutants (smoke test; full count in Step 2b) -->
