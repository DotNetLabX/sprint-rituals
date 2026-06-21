# Bug Ratio — Lessons

## PO Lessons

- **Help file should be written in parallel with the spec, then sync-checked after critic fixes.** Writing the help file while the spec is fresh produces better content — the intent behind each section is clearest right after the discussion and research. Waiting until after critic review risks thinner content due to context distance. In practice, critic findings are mostly technical/consistency issues that rarely affect user-facing help text (this session: 4 findings, only 1 needed a help file patch — a single sentence). The real risk is forgetting to sync-check, not writing too early. PO agent workflow: write spec → write help file → critic review → fix spec findings → **sync-check help file** against revised spec → set Ready.

- **External research validated all three design decisions and prevented a bad default.** The competitor research (Hatica, LinearB, Harness SEI, Sleuth) confirmed: tabs over sections (layout), binary bug classification (simplest for v1), and completed-only scope (industry standard). Without research, the "assigned bugs as secondary column" idea would have added noise — LinearB explicitly states "only completed issues are included to ensure accuracy." Research also surfaced the "ratio of totals vs average of ratios" distinction, which became BR8.

- **Critic caught a real cross-feature consistency bug.** The case-sensitive vs case-insensitive "Bug" matching between ScopeChangeService and the BugRatio spec would have produced different bug counts on different pages. This is the kind of subtle divergence that only shows up when an independent reviewer cross-references implementation patterns across features. Worth always running the critic on specs that share computed concepts with existing features.

- **F9 excluded-from-scope gap is a backlog item.** F9 (Developer Throughput) was implemented before F10 introduced excluded-from-scope statuses. The Throughput tab and Bug Ratio tab on the same page would show different totals when exclusions are configured. This should be tracked as a separate fix, not bundled into F13.

## Developer Lessons
- `AppSettingsRepository.SaveAsync` uses `db.Entry(existing).CurrentValues.SetValues(settings)` which automatically copies all scalar int properties by name. Only list/complex properties with value converters need manual copy after `SetValues`. New scalar settings columns need no repository changes beyond domain model and endpoint mapping.
- The Bash tool cwd does not persist between calls. Always use explicit absolute paths (e.g. `cd "<fokus-repo>" && dotnet ef ...`) in every command.
- EF migration command requires paths relative to the solution root with the `src/` prefix: `-p src/Services/Fokus/Fokus.Persistence -s src/Services/Fokus/Fokus.API`.
- TypeScript strict mode catches unused imports in `.vue` `<script setup>` blocks — remove type imports that are not referenced in code.
- When adding fields to a TypeScript interface used as a `ref<T>` initial value, update all store initializations that use that interface.
- URL query params from `route.query` are `string | string[] | null` — never compare directly to a typed union like `'throughput' | 'bugRatio'`. Parse first, then assign the typed store property.

## Architect Lessons

- **Skill Mapping table must be numbered to match Implementation Steps, not an independent sequence.** The initial plan had 8 Skill Mapping rows for 10 Implementation Steps with misaligned numbering. The critic review caught this. Every implementation step needs a corresponding Skill Mapping row with the same step number.

- **"Per-developer stacked bar chart" is ambiguous — specify the stacking dimension explicitly.** The plan used the spec's wording ("per-developer stacked bar chart shows Bug SP vs Non-Bug SP per sprint for each developer") which the developer reasonably interpreted as team-level stacking. Plans should disambiguate: "X-axis = developers, stacked segments = Bug SP / Non-Bug SP" vs "X-axis = sprints, stacked segments = Bug SP / Non-Bug SP aggregated across team." The spec's AC was the ambiguous source, but the plan should have clarified.

- **Alert evaluation requiring all closed sprints (BR9) is the key performance decision for this feature.** The plan correctly identified that alert evaluation needs full sprint history and specified loading all closed sprints with memberships once. This is the right v1 approach. If performance becomes an issue, the optimization path is a targeted repository query that computes consecutive-above-threshold counts without loading full membership graphs.

- **Self-performed critic review is effective when structured as a matrix cross-check.** Systematically verifying every AC and BR against plan steps (34 + 19 = 53 items) with a tabular format caught the Skill Mapping numbering gap. The matrix approach forces completeness — freeform review would likely have missed the numbering mismatch.

## Skill Gaps
- **Missing skill:** No skill for "settings form extension" — adding scalar fields to AppSettings across domain, EF migration, GET/PUT endpoint mapping, repository analysis, frontend type/store/form. Suggested name: `extend-app-settings`.

## Reviewer Lessons

- **EF migration defaultValue must match domain defaults explicitly.** EF generates `defaultValue: 0` for int columns regardless of domain property defaults. Always check the migration output and set the correct defaultValue manually. Domain `= 50` / `= 2` defaults only apply to in-memory construction, not to the SQL column default for existing rows.

- **Lazy-load guard `=== null` breaks re-fetch after filter change.** When lazy-loading tab data with a null guard, any filter change while on another tab leaves stale data cached. The guard should be removed from `switchTab` so switching always fetches fresh data. This is a recurring risk in multi-tab stores with shared filters.

- **Codex cross-validation caught one HIGH issue (stale data) that Sonnet missed.** The tab-switching stale-data bug was not in the pre-commitment predictions. Running Codex in parallel is worth the cost for features with inter-component state coordination. Always predict 5+ areas before reviewing to activate deliberate search.

- **Alert consequence analysis must follow migration bug to its runtime effect.** Found the migration defaultValue=0 but initially rated it MEDIUM. The correct severity requires tracing through to `consecutiveAbove >= consecutiveCount` where 0 makes every developer alert. Always follow data bugs to their behavioral consequence before assigning severity.
