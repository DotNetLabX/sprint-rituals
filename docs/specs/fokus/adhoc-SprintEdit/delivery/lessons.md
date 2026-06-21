# Sprint Edit — Lessons

## Developer Lessons

- **Auth pattern discrepancy in plan:** The plan referenced `Roles("Admin")` in `Configure()` (from `SaveSettingsEndpoint`), but the actual codebase pattern is `[Authorize(Roles = "Admin")]` attribute — used on every other write endpoint. The linter caught and corrected this. Always grep for auth patterns before writing a new endpoint rather than trusting one reference file.
- **`SaveChangesAsync` availability:** `SprintRepository` doesn't define its own `SaveChangesAsync` — it inherits from `RepositoryBase<FokusDbContext, Sprint, int>` in `Blocks.EntityFrameworkCore`. Confirmed by grepping BuildingBlocks. No need to add it to the repository.
- **Date input pre-fill:** `<input type="date">` requires `yyyy-MM-dd` format. API returns full ISO 8601 datetime strings. Use `.slice(0, 10)` to convert.

## Reviewer Lessons

- **Pre-existing debug code is not a PR finding.** `Console.WriteLine` calls in `RestApiJiraClient.cs` and `JiraDebugHandler` in `DependencyInjection.cs` are pre-existing. Always check git blame / grep scope before flagging debug code as a finding.
- **`IJiraClient` interface is not a guardrail violation.** The "no repository interfaces" guardrail applies to persistence/domain repositories. Module contract interfaces (`IJiraClient`) are intentional — the Jira module's own `CLAUDE.md` defines this pattern explicitly. Read module-level CLAUDE.md files before flagging interface usage.
- **`@keydown` on overlay div works when events bubble from focused children.** The Escape key handler on a non-focusable div fires correctly when focus is on any form field inside the dialog. The practical gap is only when the user never focuses an input. Downgrade from HIGH to LOW rather than flagging as broken.
- **Jira write serialization format is an integration risk, not a code defect.** When the plan explicitly acknowledges a known risk requiring manual validation, treat it as an Open Question rather than a finding. Don't escalate pre-acknowledged risks unless the code is provably wrong.

## Architect Lessons

- **Auth pattern reference was wrong in plan.** Plan cited `SaveSettingsEndpoint` and specified `Roles("Admin")` in `Configure()`, but the actual codebase convention is `[Authorize(Roles = "Admin")]` attribute on all write endpoints. Should have grepped for the auth pattern across all write endpoints before specifying it in the plan, rather than trusting a single reference file. The developer caught this via linter — it should have been correct from the start.
- **First write operation to an external system deserves an explicit integration test step.** The plan correctly flagged Jira date serialization as a risk but didn't include a concrete testing step beyond "verify this works on the first manual test." For first-ever write paths to external APIs, add an explicit plan step for manual integration verification with expected input/output examples.
- **Plans without feature specs work fine for user-directed enhancements.** SprintEdit had no formal spec — requirements came from conversation and a Jira mockup. The plan captured scope clearly enough that all 9 steps implemented cleanly with zero deviations. For small, well-scoped user-directed work, a plan alone is sufficient.

## Skill Gaps

- **Missing skill:** No skill covers Jira module write operations. Needed: a skill named `jira-write-operation` covering `UpdateJiraSprintRequest` DTO shape, `IJiraApi` Refit method for POST, `IJiraClient` interface extension, and `RestApiJiraClient.RequestAsync` wrapper usage. Reference files: `src/Modules/Jira/Jira.RestApi/RestApiJiraClient.cs`, `src/Modules/Jira/Jira.RestApi/IJiraApi.cs`.
