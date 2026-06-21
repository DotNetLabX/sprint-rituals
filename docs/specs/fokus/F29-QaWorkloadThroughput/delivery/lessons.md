# QA Workload & Throughput — Lessons

## Developer Lessons

- `string?` as Dictionary key with a custom `IEqualityComparer<string?>` works at runtime but produces CS8714 warnings from the compiler. These are informational warnings only and do not affect correctness. The pattern is needed when null is a valid business key (Unassigned row).
- Reference type records (`public record Foo(...)`) use `is not null` for nullable checks, not `.HasValue`/`.Value` (those are for `Nullable<T>` value types). Confusing these causes CS1061 errors.
- When a plan says "inline delta with polarity", verify whether the backend record actually returns a polarity field. `QaWorkloadSingleEntry` stores only `*Delta` and `*Direction` — no separate `*Polarity` field. If colored delta indicators are needed client-side, a polarity field must be added to both the backend record and DTO, or polarity must be inferred client-side from the column's known direction convention.
- The ApexCharts horizontal bar chart requires `plotOptions.bar.horizontal: true` inside the options object (not a top-level chart type change). The `type: 'bar'` stays the same.

## Architect Lessons

- When a plan specifies delta polarities per column (neutral, higher-is-better, lower-is-better), explicitly require polarity fields on the backend record — not just direction fields. The plan said "Delta polarities per spec" and listed them, but did not make it a binding named field requirement. The developer followed the record shape literally (direction only) and skipped polarity. Next time: name the fields explicitly in the record definition (e.g., `PassCountPolarity (string?)`) the same way direction fields are named, and reference the BugRatio pattern which includes both.
- The plan's Step 1 record definitions listed "delta pairs for each numeric column: tesOwnedDelta/tesOwnedDirection" — this naming convention implied direction-only. It should have been "tesOwnedDelta/tesOwnedDirection/tesOwnedPolarity" to match the BugRatio pattern. Named identifiers in plans are binding contracts per the architect agent rules.
- Step 1 done check caught the polarity gap in Cycle 0. Fix was clean and scoped (7 fields added across 5 files). Pre-commitment prediction strategy worked — predicting "delta polarities might not match spec" led directly to the finding. Worth continuing to predict the 2-3 most likely gaps before reading implementation.md.

## Reviewer Lessons

- When a feature uses a reference pattern for error handling (e.g., GetBugRatioEndpoint), check the reference before flagging a deviation from the plan text. The plan said "404" but the reference endpoint uses 400 — the code correctly follows the reference. Flagging this as HIGH would have been wrong; MEDIUM is appropriate since it represents a plan-text deviation even when justified by the codebase pattern.
- For ApexCharts horizontal bar charts, `xaxis.categories` provides the bar labels (rendered on the Y axis visually). Placing categories in `yaxis` is non-standard. No existing horizontal bar chart in this codebase to confirm; note confidence limitation in the finding.
- The `sealed record` convention in csharp.md does not match the codebase practice for analytics service records. Before flagging a LOW convention issue, verify whether the entire feature area already deviates from it — if yes, the developer followed the right pattern and the convention file needs updating, not the code.
- Independent sort computeds for chart series and categories (distributionSeries / distributionCategories) are a subtle data-alignment risk when two items share the same sort key. Flag as a gap rather than a finding when the risk is theoretical with real data.

## Skill Gaps

- **Missing skill:** No skill for frontend developer tab integration (adding a tab button to DevelopersView, URL sync extension, store wiring). All five F2x features follow the same pattern. Suggested name: `add-developers-tab`. Coverage: store extension checklist, DevelopersView tab button template, onMounted URL seed pattern, watch extension.
