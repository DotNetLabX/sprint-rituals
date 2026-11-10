# TeamManagedSync — Lessons

## Developer Lessons

- `Microsoft.Extensions.Logging.Abstractions` is available transitively in `Jira.RestApi` via `Refit.HttpClientFactory` — no explicit `PackageReference` needed for `ILogger<T>`.
- When a primary constructor parameter is promoted to a `protected` property for subclass access, all internal usages in the base class must also be updated to use the property (not the constructor parameter directly). The compiler accepts both, but consistency requires the property.
- The solution file is at `src/Fokus.slnx`, not at the repo root. Use `dotnet build "<fokus-repo>/src/Fokus.slnx"` for full solution builds.
- `RestApiJiraClient.RequestAsync` maps all non-2xx Jira responses to `BadGatewayException` (or `UnauthorizedException` for 401). There is no typed JQL parse error — catching `BadGatewayException` is the correct approach for the epic JQL fallback pattern.

## Architect Lessons

- For refactoring plans with multiple sub-items per step (Step 3 had 6 sub-tasks), the implementation.md tracked all sub-items explicitly. This validates that numbering sub-items in the plan produces thorough implementation reporting.
- Open Questions with inline recommendations (e.g., "Recommendation: try X first, fall back to Y") were adopted directly by the developer without needing a separate questions.md round-trip. Including recommendations in Open Questions reduces handoff friction.
- This plan had zero deviations reported. The Key Decisions section captured 3 implementation choices that were within plan boundaries but worth documenting (logger injection, exception type for catch, constructor parameter promotion). The distinction between "deviation" and "key decision" was handled correctly — deviations change what the plan said, key decisions fill gaps the plan intentionally left open.
- First-cycle approval validates the plan quality: 6 steps, no deviations, no questions.md round-trips. For module-internal refactoring with no domain/persistence/endpoint changes, a scoped plan with explicit sub-items per step produces clean execution.
- Open Questions with inline recommendations (used in this plan for epic JQL and Agile API deprecation) let the developer proceed without blocking. The reviewer correctly evaluated the recommendation's implementation as MEDIUM rather than escalating. Include recommendations in Open Questions for non-blocking decisions; omit them when the decision genuinely requires human input.

## Reviewer Lessons

- **`GetFullChangelogAsync` uses `IsLast` on the changelog endpoint.** Jira Cloud's `/rest/api/3/issue/{key}/changelog` does return `isLast` in its paginated response — the same `JiraPagedResult<T>` pattern used for boards and sprints applies. No special handling needed.
- **"Fall back on empty results OR exception" is a common heuristic in Jira client code** when a JQL field may or may not be supported. The `GetEpicIssuesAsync` pattern (try Epic Link, fall back to parent on both exception and zero results) is intentional per the plan's Open Question 1 recommendation. Flag as MEDIUM (unnecessary double query) rather than HIGH (logic error).
- **Behavioral parity verification via `git show <commit>:<path>`** is reliable when the plan cites a specific commit (here `1987027`). Always do this for refactoring reviews — do not rely on implementation.md description alone.
- **The `EnrichChangelogsAsync` sequential pattern** (foreach + await) is correct for correctness but suboptimal for large sprints. Flag as LOW for future optimization, not a blocker.

## Skill Gaps

- **Missing skill:** No skill for "inheritance refactoring of an existing class" — pattern of making methods virtual, adding protected helpers, and creating a subclass. Suggested name: `refactor-for-inheritance`. Coverage: virtual method promotion, protected helper extraction, subclass with override pattern, DI factory registration.
