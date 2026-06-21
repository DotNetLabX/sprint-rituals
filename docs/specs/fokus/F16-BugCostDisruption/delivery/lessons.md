# Bug Cost & Disruption Split — Lessons

## PO Lessons
- Cross-cutting features that modify SP calculations touch more sibling features than initially apparent. F9 (Throughput) and F14 (Epic Progress) were missed in the first draft because they weren't mentioned in the user's request — the critic caught both. Lesson: when a feature says "system-wide," enumerate every feature that touches SP and explicitly include or exclude each one.
- The existing MetricsResult is a named-property record, not a generic list. Spec language like "metrics list grows from 4 to 5" is misleading — always check the actual response shape before describing API changes in a spec.
- Excluded-from-scope statuses (introduced by F10) are a cross-cutting filter that must be mentioned in any spec that modifies SP calculations. Easy to forget because it's an F10 concept, not visible from the v1.md level.
- Epic Progress has its own imputation mechanism for unestimated tickets. Any feature that adds a different imputation (like default SP per bug) must specify precedence to avoid double-counting or conflicting values.

## Architect Lessons
- Analytics services in this codebase all follow the same SP access pattern (`m.StoryPoints.HasValue` / `m.StoryPoints!.Value`). A centralized `GetEffectiveSp` method on the domain entity is the right place for cross-cutting SP substitution -- it avoids duplicating the logic in 6 services.
- The EpicProgressService uses `Ticket` entities directly (not `SprintMembership`), so it needs a parallel helper method for Ticket-level SP substitution. The `GetEffectiveSp` on SprintMembership cannot serve both -- the domain boundary difference matters.
- No existing skill covers "add behavior method to an existing domain entity" -- the `create-aggregate` skill creates new aggregates, and `domain-patterns` covers aggregate patterns but not incremental behavior additions. Logged as a gap.
- The BugRatioService uses `m.StoryPoints ?? 0m` in places (defaulting null to 0) while other services use `m.StoryPoints.HasValue` guards. This inconsistency means the GetEffectiveSp adoption pattern differs slightly per service -- the plan must call this out explicitly or the developer will miss it.
- When planning endpoint caller updates (Step 13), check whether services already accept the full `AppSettings` object before proposing new parameters. All analytics services in this codebase receive `AppSettings` directly, so they can extract `DefaultSpPerBug` internally without any endpoint changes. The plan incorrectly assumed a new parameter was needed -- the deviation was valid and resulted in zero wasted work, but the plan step was unnecessary.
- Multi-phase methods like `ComputeTopEpics` operate on two different entity types (SprintMembership for selection, Ticket for completion). When a plan says "apply GetEffectiveSp," the done check must verify each data source independently. The reviewer caught that phase 2 (Ticket-based) was missed. Future plans should enumerate each data source explicitly when a method crosses entity boundaries.
- Verbatim spec text (tooltips, labels) should be treated as named identifiers in the plan. The Step 1 done check flagged the tooltip wording deviation as LOW, but the reviewer correctly required the fix. Plans should quote spec text verbatim and mark it as binding.

## Developer Lessons

- **EF migration defaultValue for int columns is always 0.** EF generates `defaultValue: 0` even when the domain property has `= 3`. Always manually correct the migration and add a SQL UPDATE seed for any existing rows.
- **DLL lock during migration with running dev server.** If the Debug build is locked by a running process, use `--configuration Release` for migrations and builds.
- **`SaveSettingsEndpoint` uses full replacement.** All `AppSettings` properties must be explicitly mapped in the `new AppSettings { ... }` initializer — missing a property silently resets it to the C# default. Any new setting added to the domain must also be added to both the command and the endpoint initializer.
- **Services extract settings internally — no endpoint changes needed.** All analytics services accept the full `AppSettings` object and extract what they need. Endpoints do not need updating when new settings properties are added.
- **Private helpers needing new parameters can use default parameter values.** `EvaluateAlert` and `BuildIssueTypeBreakdown` were given `int defaultSpPerBug = 0` defaults to keep the change minimal and backward-compatible.
- **BugRatioService uses `?? 0m` pattern, not `HasValue` guard.** The substitution is `GetEffectiveSp(defaultSpPerBug) ?? 0m` across the board — removing the `.HasValue` filter is also correct since `GetEffectiveSp` returns null for non-bugs with no SP.

## Reviewer Lessons

- **Multi-phase methods need full coverage.** `ComputeTopEpics` has two phases: (1) selecting sprint memberships using `GetEffectiveSp` (correct), (2) computing epic-level completion from Ticket entities using raw `StoryPoints` (missed). When a plan says "use GetEffectiveSp for top epics," check every distinct data source that feeds into the result — not just the first LINQ chain.
- **"No endpoint changes needed" deviations are legitimate when services own settings extraction.** If all analytics services accept `AppSettings` directly and extract `DefaultSpPerBug` internally, there is no need to thread the int through every endpoint. This is a better pattern than plan's proposed signature change. Verify intent rather than flagging automatically.
- **Build locking during review.** The running dev server locks the output DLLs. Use `-o <temp-dir>` to redirect build output, then delete the temp folder. Do not use `-no-incremental` alone when the process has the bin folder open.

- **Cycle 2:** When a fix adds a private static helper to a class, verify the helper signature and body match the canonical version in the codebase (here: `EpicProgressService.GetEffectiveTicketSp`). The developer replicated it correctly; confirming parity is a fast and sufficient check for this pattern.

## Skill Gaps

- **Missing skill:** Applying a system-wide SP fallback across multiple analytics services. A skill covering the "effective SP" pattern (domain method, per-service extraction, migration correction) would reduce exploration time. Suggested name: `apply-effective-sp`. Reference files: `SprintMembership.cs`, `BugRatioService.cs`, `CarryOverService.cs`.
