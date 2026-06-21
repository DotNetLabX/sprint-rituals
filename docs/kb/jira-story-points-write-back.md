# Jira Story Points Write-Back

How Fokus writes an estimated story-points value back to a Jira issue after the SM decides an estimate in a Planning Poker session.

## Rules

- BR1: The Jira issue-edit endpoint is `PUT /rest/api/3/issue/{issueIdOrKey}` with a JSON body `{ "fields": { "customfield_10016": <decimal> } }`. The field name is `customfield_10016` (the canonical id for story points), **not** `story_points`. v1 pins this id; per-project field variance is out of scope.
- BR2: The API returns `204 No Content` on success. No response body is deserialized; the caller receives void.
- BR3: The `DecideEstimate` hub method calls the gRPC `SetStoryPoints` operation **first**, before calling `session.DecideEstimate()` on the domain. The item is only marked `Decided` if `SetStoryPointsResponse.Success == true`.
- BR4: On failure (`Success == false`), the hub broadcasts `WriteBackFailed` with a reason string. The item remains `Voting` or `Revealed`; no vote data is lost. The SM may retry.
- BR5: Free-mode sessions perform no write-back. `DecideEstimate` goes directly to the domain without a gRPC call (items have no `TicketKey`).
- BR6: Non-numeric values (e.g. `"?"`) must never reach the write-back path. The domain's `Decide()` enforces Rule 6 (no `?` as final estimate) before the hub calls `SetStoryPoints`.

## Key Files

- `src/Modules/Jira/Jira.Contracts/UpdateIssueFieldsCommand.cs` — `IssueFields.StoryPoints` serialized as `customfield_10016`
- `src/Modules/Jira/Jira.Contracts/IJiraClient.cs` — `SetStoryPointsAsync(issueKey, storyPoints, ct)` contract
- `src/Modules/Jira/Jira.RestApi/IJiraApi.cs` — `[Put] UpdateIssueFieldsAsync` — returns `IApiResponse` (no body form)
- `src/Modules/Jira/Jira.RestApi/RestApiJiraClient.cs` — `SetStoryPointsAsync` impl; `RequestAsync(Func<Task<IApiResponse>>, ct)` void overload (204 NPE guard)
- `src/Services/Fokus/Fokus.API/Grpc/PokerFokusGrpcService.cs` — gRPC server; bridges `IPokerFokusService.SetStoryPointsAsync → IJiraClient.SetStoryPointsAsync`
- `src/Services/Poker/Poker.API/Hubs/PlanningHub.cs` — `DecideEstimate` hub method enforces write-first sequencing

## Edge Cases

- **204 NPE**: `IApiResponse` (non-generic) is used for the PUT so that `response.Content` is never accessed. The generic `IApiResponse<T>` form would NPE on a 204; the void overload avoids this.
- **`customfield_10016` is project-type-specific**: Classic Jira projects use this id. Next-gen / team-managed projects may differ. v1 assumes `customfield_10016`; broadening is a backlog item.
- **Retry is SM-driven**: there is no automatic retry. If `WriteBackFailed` fires, the SM must re-invoke `DecideEstimate`. Votes are preserved.
- **Sprint-bound only**: `item.TicketKey` is non-null only for Sprint-bound items. `TrySetStoryPointsAsync` returns false (not an error) if the value cannot be parsed as decimal — but domain enforces numeric values, so this is defense-in-depth.

## Relationships

- `PlanningHub.DecideEstimate` → `IPokerFokusService.SetStoryPointsAsync`: gRPC call from Poker to Fokus
- `PokerFokusGrpcService` → `IJiraClient.SetStoryPointsAsync`: Fokus delegates to the Jira module
- `IJiraClient.SetStoryPointsAsync` → `PUT /rest/api/3/issue/{key}`: Refit HTTP PUT via `IJiraApi`
- `EstimationItem.Decide()` → enforces Rule 6 (no `?` final value) before hub calls write-back

## Source

F6-PlanningPoker spec Rule 10 (write-back-on-decide, Sprint-bound only), spec §API Surface (DecideEstimate endpoint contract), `docs/specs/F6-PlanningPoker/definition/spec.md`.
