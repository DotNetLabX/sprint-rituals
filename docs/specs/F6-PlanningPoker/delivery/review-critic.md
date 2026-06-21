# Critic Plan Review (Mode 2: plan vs spec) — F6-PlanningPoker

**Recorded verbatim by team-lead** (the critic writes no file by design — ADR/relay contract). Source: critic-F6@session-d8f90939, 2026-06-21.

Plan: docs/specs/F6-PlanningPoker/delivery/plan.md vs spec.md.

**VERDICT: ACCEPT** (two minor pre-implementation fixes recommended; neither blocks)

The plan is strong: every spec AC traces to a step, all source-line claims verify against the codebase, and the high-risk Jira write path is correctly identified and scoped.

## Spec → Plan coverage (all 19 ACs traced)
Cross-referenced every Acceptance Criterion and Business Rule against the steps. Full coverage:
- Auth/SM gates (AC unauth-create, guest-cannot-create/add, non-SM-403) → Steps 5+6
- Mode validation 400s, closed 409, summary readability → Step 5
- Domain rules (Rule 6 "?"-not-final, 8 value-must-be-cast, 21 one-vote, 9 skip, 19 refresh-idempotency) → Step 2 with explicit TDD asserts
- Write-back path (Rule 10), live tickets (Rule 12), Future sprints (Rule 16) → Step 4
- Real-time loop incl. auto-reveal both cases (Rule 7), vote privacy, SM-disconnect liveness (Rule 13) → Step 7
- Broadcasts → Step 8; Host wiring → Step 9; Vue room → Step 10; KB → Step 11

## Verified claims (all correct)
- SprintRepository.cs:19 filter is exactly `Closed || Active` — confirmed; shared by ~20 analytics call sites (grep'd). The "don't widen, add sibling" instruction is sound.
- Jira's only write today is `UpdateSprintAsync` → `POST /rest/agile/1.0/sprint/{id}` (IJiraApi.cs:33, Agile API). Story-points PUT is genuinely net-new. Correct risk call.
- The 204-NPE concern is real: `RequestAsync<T>` returns `response.Content!` (RestApiJiraClient.cs:164) — a 204 has no content. Plan flags it.
- Reflekt guest-scheme chaining (AuthenticationConfiguration.cs), idempotent authenticated re-entry (JoinRetroEndpoint.cs:27-35), and assembly-name gate scoping (ActiveUserGateScope.cs) all match the plan's "mirror Reflekt" instructions.

## Two minor fixes (recommend before Step 4/6, not blocking)

1. **[Minor — Step 4a] Second IJiraClient impl not mentioned.** There are TWO `IJiraClient` implementations: `RestApiJiraClient` and `TeamManagedJiraClient : RestApiJiraClient` (overrides the issue-fetch methods for team-managed boards; DI picks one by `Jira:ProjectKey` at DependencyInjection.cs:47-51). Adding `SetStoryPointsAsync` to the `IJiraClient` interface is fine — `TeamManagedJiraClient` inherits the base impl, and the issue-edit PUT is board-type-agnostic — but the plan should name this so the developer (a) doesn't get surprised by the second class and (b) consciously confirms no override is needed there. One sentence in Step 4a.

2. **[Minor — Step 6] ActiveUserGateScope is hardcoded, not a list.** The plan says "confirm the existing mechanism keys on declaring assembly and just needs Poker's assembly added." Confirmed it keys on assembly — but `ActiveUserGateScope.IsExcludedModule` is `endpointAssemblyName == "Reflekt.API"` (single const equality, ActiveUserGateScope.cs:20-21), NOT a collection. Adding Poker means editing this Identity file to also match `"Poker.API"` (e.g. promote to a set), and there are existing tests (Identity.API.Tests/Auth/ActiveUserGateScopeTests.cs) that must be updated. The plan should state Step 6 edits an Identity file — minor but it's a cross-module touch the plan elsewhere claims doesn't happen ("Identity: no changes", Cross-Service Changes). That line is now slightly inaccurate; reconcile it.

## Non-blocking observations (no action needed)
- Product draft (poker-v1.md §4) names the item entity `EstimationRound`, status `Completed`, `CreatedByUserId`; the spec correctly supersedes with `EstimationItem`, `Closed`, `CreatedByPersonId`. Plan follows the spec. Good — just noting the draft is stale, expected since spec graduates it.
- Spec's one open question (Person for not-yet-known Google participant) is resolved in the plan's Open Questions (mirror Reflekt, never mint, join-as-guest). Resolution is consistent with RA2 and Rule 20. Sound.

Both fixes are one- to two-sentence plan edits. The plan is implementable as-is; these prevent a mid-Step surprise.
