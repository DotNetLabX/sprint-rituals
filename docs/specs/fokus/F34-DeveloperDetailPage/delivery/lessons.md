# F34-DeveloperDetailPage — Lessons

## Developer Lessons

- **`JiraOptions` is not in Fokus GlobalUsings — add explicit usings.** `JiraOptions` lives in `Jira.RestApi` namespace and `IOptions<T>` lives in `Microsoft.Extensions.Options`. Neither is in the service-level GlobalUsings. Pattern confirmed from `SyncXrayEndpoint.cs`. Always add explicit usings for these two when injecting `IOptions<JiraOptions>`.

- **Variable name collision in nested scopes with `out var`.** When a method has `var tickets = BuildTicketDetails(...)` in an outer scope and later calls `completionsByDay.TryGetValue(day, out var tickets)` in an inner loop, CS0136 fires. Fix: rename the `out var` to something more specific (e.g., `out var dayCompletions`).

- **Rolling average diverges between DeveloperDetail and Throughput.** Throughput computes rolling average over feature-only SP; DeveloperDetail computes it over all-types SP. Both use the capacity-aware skip pattern. When composing patterns across services, check which SP scope is intended — the pattern is the same but the input differs.

- **`AppSettingsConfiguration.cs` needs no changes for simple scalar decimal properties.** EF Core auto-maps `decimal` scalars. Only complex types (value objects, JSON columns, owned entities) require explicit configuration in the entity type config.

## Architect Lessons

- **Critic review justified on complex features.** The critic caught the "(all types)" label gap and the 204-vs-200 status code inconsistency before implementation. Both were addressed in the plan, and the developer implemented them correctly without confusion. Complex multi-area features benefit from critic mode over self-review.
- **Acknowledged-divergence pattern works well.** Noting codebase-pattern-over-spec in the plan (200 vs 204) with explicit reasoning gave the developer clear direction. The implementation correctly followed the codebase pattern and documented the same reasoning. No ambiguity at review time.
- **Medium-confidence step with line-number references succeeded.** Step 3 was flagged Medium confidence (composing 3 services). The pattern references with specific line numbers (LeaderboardService line 287, DeveloperProgressService line 367) gave the developer enough anchor points. All computation rules were implemented correctly.
## Reviewer Lessons

- **Rolling average look-back operates on the pre-exclusion list — subtle but inconsequential.** The service computes the rolling average before applying the sprint inclusion `continue` guard. In this case the `capacity == 0` skip inside `ComputeRollingAverage` covers the same condition, making the behavior correct. Future reviewers: when a service computes a secondary metric (like rolling average) inside a loop that later has a `continue`, check whether the secondary metric window can be distorted by sprints that are eventually excluded.

- [TRACKED] **`border-l-2` on `<tr>` is a Tailwind/CSS table trap.** CSS borders on `<tr>` elements are not reliably rendered across browsers when `border-collapse` is active (Tailwind's Preflight sets this). Prefer applying left-border indicators to the first `<td>` in the row or using a background color on the row instead.

- [TRACKED] **TakeLast before vs after exclusion guard is a recurring analytics pitfall.** When a service filters a list with `TakeLast(n)` and then applies a secondary exclusion guard inside the loop, the returned count can be less than `n`. Pattern to check on any analytics service with a `last` parameter: confirm the exclusion guard runs before `TakeLast`, not after. Fix pattern: accumulate into `allEntries`, apply `TakeLast` at the end.

## Skill Gaps

