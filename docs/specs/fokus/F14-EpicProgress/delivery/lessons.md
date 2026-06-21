# Epic Progress — Lessons

## PO Lessons
- When offering clarifying questions and research, bundle all pre-write decisions in the same batch: verification method (cross-check vs critic) and help content (yes/no). Don't ask these after writing — it delays help content and feels like an afterthought.
- External research (competitor analysis) added significant value for this feature — Jira's "skip sprints with ≤2 SP" velocity behavior and Hatica's "Issues w/o Epics" hygiene signal both shaped the spec directly.
- Cross-cutting concerns (C1, C2, C3) must be explicitly addressed even when the feature justifiably omits them. The first critic caught C1/C3 as HIGH because they were silently omitted rather than explicitly justified in Out of Scope.
- When a feature's computation diverges from an existing feature's computation for the same concept (F14 epic completion vs F8 Top Epics), the spec must explicitly scope the alignment work — either in-scope with an AC, or out-of-scope with a backlog item. "Should be aligned" without a clear owner is ambiguous.
- Imputation concepts (inferring missing data from averages) need careful edge case analysis: which tickets contribute to the average, which receive imputed values, and how asymmetry affects user perception. The critic caught that done unestimated tickets vanishing from SP metrics creates unintuitive divergence between ticket % and SP %.

## Architect Lessons

- **Navigation includes must be explicit for every consumer.** When a plan specifies a repository query, list every navigation property needed by every downstream consumer — not just the primary use case. The `GetAllClosedSprintMembershipsAsync` query initially only mentioned `Ticket` navigation, but sub-team filtering also required `Ticket.Assignee`. The `GetTicketsWithoutEpicInSprintsAsync` query was described as "lightweight projection" but sub-team filtering on unlinked work needed `Assignee`. Both gaps were caught during critic review before reaching the developer, but the developer's projection approach still recreated the second gap — proving the plan instruction wasn't forceful enough.
- **When a plan says "include navigation X," add a negative instruction too.** The unlinked work query gap could have been prevented by adding: "Do NOT use a Select projection that omits Assignee — sub-team filtering requires the full navigation." Positive instructions ("include Assignee") get lost when the developer makes an equivalent-seeming implementation choice; negative instructions ("do not project away Assignee") are harder to accidentally violate.
- **Shared component modifications need their own explicit verification line in the plan.** The PageToolbar tooltip was the only tooltip target in a shared component (vs new epics-specific components). Shared component changes are at higher risk of being dropped because they feel like a side-effect rather than a primary deliverable. Future plans should call out shared component modifications as a separate sub-item, not buried inside a component creation step.
- **Skill mapping table must be regenerated after folding steps.** When tooltip wiring (originally a separate step) was folded into Step 9 during critic review, the skill mapping table was left with 11 rows for 10 steps. Always regenerate the mapping table after any step count change.
- **Pre-existing plan from a prior session needs full re-validation, not just a skim.** The plan for F14 already existed when I was invoked. I verified it against the spec and found it complete, but the critic review process still caught the navigation gaps — proving that even a well-structured plan benefits from the formal quality gate.

## Developer Lessons

- When a repository query needs ordering by a navigation property (e.g., Sprint.StartDate on SprintMembership), the Sprint navigation must be explicitly included in the EF Core query — it is not loaded automatically even when filtering on Sprint.State via a navigation predicate.
- EF Core correlated subquery `DbContext.Set.Any(...)` inside a `Where` clause is a clean way to filter entities by existence of a related record without loading the related data.
- GetTicketsWithoutEpicInSprintsAsync uses explicit property assignment in a `Select` projection rather than an anonymous type, because EF Core requires projected entities to be concrete types for tracking. This is acceptable for lightweight projections.
- The `showSprintSelector` prop on PageToolbar is a backward-compatible addition (default `true`) — all existing pages continue to work without change.
- Named slots in Vue components (e.g., `#empty`) let parent views supply context-sensitive messages without coupling child display components to store state. Prefer this pattern for empty states that vary by context.
- `Set<string>` (JavaScript) works well for expand/collapse state in Pinia stores — toggling membership is O(1) and reactive when the ref is replaced or mutated with `add`/`delete`.

## Skill Gaps

- **Missing skill:** `tooltip-wiring` — covers how to wire tooltip text from `help.tooltips.md` to UI elements using `title` attributes or tooltip components. Would replace ad-hoc inline guidance in plan Step 10. Reference files: `docs/features/EpicProgress/help.tooltips.md`, `client/src/components/epics/EpicSummaryCards.vue`.
- **Missing skill:** `metric-refactor` — covers updating an existing computation method in an analytics service to use a different data source (e.g., switching from sprint-membership-based to ticket-status-based completion). Would cover the signature change + caller update pattern. Reference files: `SprintSummaryService.cs` (ComputeTopEpics), `GetSprintSummaryEndpoint.cs`.

## Developer Lessons (Round 2)

- Bash heredoc (`<< 'EOF'`) fails when the content contains single quotes — the shell interprets the single quote as starting a new string context. For Vue/TS files with single-quoted imports, use the Write tool directly (for new files, touch them first via `mkdir -p` + bash then Read the empty file) or write via a language that avoids the shell quoting issue.
- The Write tool requires the file to have been read in the current session before overwriting — even for near-empty stub files. Use `bash touch` to create the file, then Read it, then Write.
- When a plan says steps were done in a previous round, always verify the actual file state rather than assuming — several steps were indeed complete, which saved significant time but required careful verification of correctness.
- EpicTable.vue using a proper HTML `<table>` layout gives better column alignment than a card-per-row approach — the table layout matches DevelopersView and makes metric columns scan-able across rows.

## Reviewer Lessons

- **EF Core projection + sub-team filtering mismatch is a silent failure pattern.** When a query uses `.Select(t => new Entity { ... })` projection and omits a navigation property (e.g., `Assignee`), any in-memory filter that reads that navigation (e.g., `t.Assignee?.SubTeam == subTeam`) silently returns nothing for filtered sub-teams. This class of bug passes the build, passes type checking, and only fails at runtime with real sub-team data. Pre-commitment prediction helped catch it — flag this pattern explicitly when the plan requires "include navigation for sub-team filtering" and the developer notes a "projection approach."
- **Tooltip coverage for shared components requires deliberate grep.** When a feature adds tooltips to new components AND requires modifying a shared component (PageToolbar), the shared component is at higher risk of being skipped. Always grep each distinct file individually — don't rely on "tooltip wiring complete" in implementation.md to imply all targets.
- **When the plan says "include navigation X" and the developer notes a deviation ("projection approach"), treat that combination as HIGH risk.** The deviation note itself is a signal worth investigating immediately in code review rather than treating as an equivalent implementation.
- **Cycle re-reviews are fast when fixes are surgical.** Both cycle 1 HIGH findings were resolved with minimal, targeted changes (one `.Include()` call replacing a projection; one tooltip wrapper added). Re-verification was straightforward: read the two specific files, grep for the exact attributes, run both builds. No regressions. Cycle 2 reviews can often be completed in a single pass when the developer follows the reviewer's specific fix suggestions exactly.
