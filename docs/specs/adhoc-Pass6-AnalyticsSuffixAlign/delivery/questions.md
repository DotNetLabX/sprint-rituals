# adhoc-Pass6-AnalyticsSuffixAlign — Questions

## Q1 — Are `DeveloperThroughputCalculator` and `DeveloperDetailCalculator` carve-outs or missed entries?

**Raised by:** developer (Phase 1 analyze)
**To:** owner (routed via team-lead → architect for classification → owner for scope decision)
**Status:** RESOLVED — owner accepted the sweep; plan amended (2026-06-16)

### Context
The plan's rename table excludes these two classes, which still carry `*Calculator`. The plan's Context claims 3c-C delivered "7 calculators with meaningful suffixes" but these two shipped without a semantic suffix. After Pass 6 (as written) the folder would still hold 4 `*Calculator` classes: `LeaderboardCalculator` (intended), `HealthScoreCalculator` (intended carve-out), and these two (unexplained).

### Architect classification (high confidence — line-grounded, source read in full)

| Class | What it computes | Correct suffix | Disposition |
|-------|------------------|----------------|-------------|
| `DeveloperThroughputCalculator` | Per-dev per-sprint SP metrics + prior-sprint deltas, direction, polarity, rolling average over 3-sprint window (delta/direction/polarity triplet via `MetricCardBuilder`; `ComputeRollingAverage`) | `Analyzer` | **Missed entry — sweep** |
| `DeveloperDetailCalculator` | Cross-sprint trends, rolling average, burnup/behind-pace, stall/state derivation; own comment says it "mirrors `DeveloperProgressAnalyzer`" (a 3c-C Analyzer exemplar) | `Analyzer` | **Missed entry — sweep** |

Neither has a raw-aggregation core to anchor a carve-out (contrast `LeaderboardCalculator`, whose dominant role is pure SP/ticket summation+sort). Both are analyzers mislabeled `Calculator`.

### Architect-recommended plan deltas (if owner accepts the sweep)
1. Rename table: add `DeveloperThroughputCalculator → DeveloperThroughputAnalyzer` and `DeveloperDetailCalculator → DeveloperDetailAnalyzer` (+ file renames + intra-file self-refs). `*Result`/`*Data` records co-located must NOT be renamed.
2. Reference-site sweep: re-grep `src/` for both old names; DI in `Fokus.API/DependencyInjection.cs` + consuming endpoints.
3. Step 3 grep-guard: add both old names to the zero-hit list.
4. KB/docs: grep `docs/` for both names; fold any hits into Step 5 with before/after.
5. Reconcile the plan's "8 earlier calculators" arithmetic (becomes 9 renamed + 1 kept).

### Resolution
**Accepted — swept into Pass 6.** Both classes added to the plan's rename table as rows 8–9 (`DeveloperThroughputCalculator → DeveloperThroughputAnalyzer`, `DeveloperDetailCalculator → DeveloperDetailAnalyzer`). All five architect-recommended deltas applied to `plan.md` (2026-06-16):
1. Rename table rows 8–9 + file renames + intra-file self-refs; co-located `*Result`/`*Data` records (`DeveloperThroughputResult`/`DeveloperDetailResult`) explicitly excluded.
2. Reference-site sweep folded into Step 2 — 4 sites: DI `DependencyInjection.cs:51`/`:62`, endpoint ctors `GetDeveloperThroughputEndpoint.cs:13` / `GetDeveloperDetailEndpoint.cs:15` (verified by solution-wide grep; no static call / second injection / full-name comment exists).
3. Step 3 grep-guard zero-hit list extended to 9 old names.
4. KB/docs: no `docs/kb/` entry names either class → no new Step 5 edit owed; the coverage-workstream ripple note (Open Questions) widened to name both classes and the now-stale GOLD-19/GOLD-20 source-path citations in `golden-set.md` + the coverage proposal (still out-of-scope, re-pointed by the Cover workstream).
5. Arithmetic reconciled across the whole plan: 11 `*Calculator` at plan time → 9 renamed (7 original + 2 here) + 2 carve-outs (`Leaderboard`, `HealthScore`).

Amendment self-reviewed (self mode) — verdict **PASS**; evidence table recorded in `plan.md` → Plan Review → "Amendment self-review".
