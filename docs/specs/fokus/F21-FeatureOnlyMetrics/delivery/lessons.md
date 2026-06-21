# Feature-Only Delivery Metrics — Lessons

## PO Lessons

- **Research before committing to a design.** The user's initial instinct (exclude bugs everywhere) was strong but needed nuance. External research revealed the "investment distribution" pattern (show both, split clearly) which led to the annotation approach instead of pure exclusion. Always offer research before answering scope questions.

- **Scope expands through screenshots.** The initial request was about the burnup chart only. The second screenshot (Dashboard cards showing 199 SP) expanded scope to cards, completion %, and health score. A third question expanded to throughput. Each screenshot made the case stronger — the PO should ask "where else does this problem show up?" early.

- **Challenge your own recommendations.** I initially recommended keeping Throughput as total-SP. The user pushed back citing the existing Bug Ratio tab and Leaderboard tab as dedicated bug surfaces. They were right — my "fairness" argument didn't hold because bug visibility was already covered in two other tabs. When the user pushes back, re-examine the supporting evidence for your position rather than defending it reflexively.

- **Critic caught real implementation traps.** The dual committed SP issue (feature-committed vs total-committed for disruption rates) would have caused a formula divergence bug if the developer applied the feature filter globally. The scope-change response field delineation (burnup data points vs multi-sprint data) was another genuine gap. These are exactly the kind of issues that look fine in product language but break in code.

- **The "surfaces NOT affected" list is as important as the changes.** BR14 listing every unchanged surface prevented scope creep and gave the critic explicit material to verify against. For cross-cutting changes that touch multiple features, always write the negative list.

## Architect Lessons

- **Verification-only steps are valid plan steps.** Steps 2 and 7 were "confirm X works after Y" with no code changes expected. The developer correctly reported them as verification-only passes. Including them in the plan prevented silent assumption gaps (e.g., "did the health score actually pick up the new completion rate?").
- **The "two-committed-SP pattern" deserves first-class naming.** When a single metric (committed SP) feeds both feature-only and total-scope calculations, naming the pattern explicitly in the plan (Step 1 item 3-4) prevented the developer from accidentally applying one filter globally. Cross-cutting filters that apply selectively need enumeration of what stays unchanged, not just what changes.
- **KB updates as a plan step work well for cross-cutting changes.** Step 8 (KB updates) ensured the documentation stayed in sync. For changes that touch 4+ KB files, making it an explicit plan step with file list guarantees it isn't forgotten.

## Developer Lessons

- When a positional `record` (like `SprintMetrics`) needs new fields, every `new SprintMetrics(...)` call site must be updated simultaneously — check all usages before adding fields to avoid partial-update compile errors.
- File-lock build errors (MSB3027) from a running `dotnet run` process are not C# compile errors. Confirm with `grep "error CS"` on build output before treating a failed build as a code problem.
- The `ComputeSpCompleted` and `ComputeCompletionRate` helpers are used by both the metric card value path and the sparkline path — making them feature-only in one place fixes both automatically without touching sparkline wiring.
- For optional Vue props that control `v-if` visibility, prefer returning `undefined` (not empty string) — semantically cleaner and ensures the element is hidden when the value is not applicable.

## Reviewer Lessons

- **Dead fields in private records are a MEDIUM finding, not LOW.** A `SprintMetrics` record field (`SpCompleted`) that receives the same value as another field (`FeatureCompleted`) and is never read creates confusion for future maintainers about which field is "current." Even in private types, dead code misleads.
- **Check rounding consistency for all numeric response fields, not just MetricCard values.** Fields bypassing `BuildMetricCard` (like `BugSpCompleted` in `MetricsResult`) may skip `Math.Round` even when all sibling fields are rounded. Scan for bare `selected.FieldName` assignments in response construction.
- **File-lock build failures mask compilation success.** When `dotnet build` shows only MSB3027/MSB3021 copy errors (file locked by running process), use `--no-incremental -p:OutDir=<alternate>` to confirm zero compiler errors independently. Do not report a failed build without isolating the failure cause.
