# Normalized Capacity Indicator — Lessons

## PO Lessons
- External research confirmed no mainstream tool normalizes throughput by capacity — all use it as a planning guardrail only. This validated that normalization is a differentiator, not a gap.
- The user's challenge ("what's the purpose of capacity if we don't use it?") uncovered that the capacity percentage was functioning as a binary toggle. The real product question was whether to simplify or extend — not whether the feature was broken.
- Starting with "what problem are you actually solving?" (vacation vs TL cases) narrowed scope faster than debating normalization in the abstract.

## Developer Lessons
- When a plan defers tooltip wiring to a late step but the same HTML line is already being modified in an earlier step, do both edits together in the earlier step. Document the merge in implementation.md to avoid the architect flagging a missing step.
- PowerShell paths with trailing backslash fail in bash quoting (`"<fokus-repo>\client\"` is parsed as escaped quote). Always use paths without trailing backslash in bash, or use forward slashes.
- TypeScript type-check (`npx tsc --noEmit`) producing no output = zero errors — successful outcome.
- Capacity lookup logic inlined in `ComputeLeaderboard` (private static method) rather than extracted to a helper — the method already has all parameters in scope. Extracting adds indirection without value in a self-contained private method.

## Architect Lessons
- Plan step 9 (tooltip wiring) was correctly designed as a verification/finalization step, but the developer reasonably merged the actual `title` attribute writes into steps 6-8 since the same HTML lines were being touched. Future plans should acknowledge this by noting "tooltip text should be wired during display steps; step N verifies consistency and adds column-header InfoTooltips" rather than implying the tooltip text is first written in the verification step.
- The plan's approach of passing `allDevelopers` (unfiltered) alongside `activeDevelopers`/`filteredDevelopers` for capacity default fallback was necessary and correctly specified. This dual-list pattern (filtered for iteration, unfiltered for lookups) recurs across analytics services and should be noted as a cross-cutting pattern.
- Inline capacity resolution in `ComputeLeaderboard` (vs extracting a `GetCapacity` helper) was a valid deviation the developer documented. The plan specified "same logic as GetCapacity helper" but didn't mandate extraction -- the developer's judgment to inline in a private static method was sound. Plans should avoid prescribing extraction vs inlining for private methods.

## Reviewer Lessons
- KB maintenance rule mandates HIGH severity for missing KB entries, but the rule is easy to miss when `implementation.md` only lists frontend-map.md and not the analytics KB file. When a feature adds new business rules (formula, display threshold, multi-sprint behaviour) to an existing analytics area, the analytics KB file for that area is always in scope — check it independently of what implementation.md lists.
- For features that are purely additive field-plumbing + frontend display (no new domain objects, no new endpoints), the most likely review gap is KB staleness, not logic errors. Front-load the KB check.

## Skill Gaps
- **Missing skill:** No skill covers "add a field to C# positional records and update all construction sites". A skill would cover: field position conventions, updating record constructors, grepping construction sites, updating TypeScript counterparts. Reference files: `LeaderboardService.cs`, `SprintSummaryService.cs`, `types/index.ts`.
