# Poker Sprint Tickets Freshness Contract

How `GetSprintTickets` (the live ticket pass-through) fetches Jira data and what freshness guarantee it makes.

## Rules

- BR1: `GetSprintTickets` is a **live pass-through** — it calls Jira on every invocation. No caching layer is involved. The response reflects Jira's state at the moment of the call.
- BR2: The endpoint is SM-gated (resource gate: `session.CreatedByPersonId == callerPersonId`). Only the session's Scrum Master can fetch tickets.
- BR3: `GetSprintTickets` is only valid for Sprint-bound sessions (`Mode == SprintBound`). Calling it on a Free-mode session returns 409 Conflict (Rule 12).
- BR4: The gRPC method is `IPokerFokusService.GetSprintTicketsAsync(SprintTicketsQuery { SprintId })`. Fokus forwards directly to `IJiraClient.GetSprintIssuesAsync`; there is no persistence layer between the call and Jira.
- BR5: Tickets include `StoryPoints decimal?` from `JiraIssueFields.StoryPoints` (field `customfield_10016`). This is the same field the write-back sets; a freshly-estimated ticket will show its current story points on the next load.

## Key Files

- `src/Services/Poker/Poker.API/Features/Tickets/GetSessionTickets/GetSessionTicketsEndpoint.cs` — REST endpoint; SM gate + 409 for Free mode
- `src/Services/Fokus/Fokus.API/Grpc/PokerFokusGrpcService.cs` — `GetSprintTicketsAsync` impl; calls `IJiraClient.GetSprintIssuesAsync`
- `src/BuildingBlocks/Blocks.Contracts/Grpc/IPokerFokusService.cs` — gRPC contract (`GetSprintTicketsAsync`)
- `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/SprintTicketsResponse.cs` — `PokerTicket` DTO (Key, Summary, StoryPoints)

## Edge Cases

- **No offline fallback**: if Jira is unreachable, the endpoint propagates the Jira error upstream. There is no cached ticket list to fall back to. The SM can reload; the session items already in `EstimationItems` are unaffected.
- **Stale data between sessions**: tickets loaded at session-create time are snapshotted as `EstimationItem.Label`/`TicketKey`. A live reload reflects any Jira changes (re-estimation, rename) but only if the SM explicitly triggers it. Existing items in the session are not automatically refreshed.
- **Rule 19 idempotency**: `AddSprintBoundItem` is idempotent by `TicketKey`. If a ticket is re-added after a label rename, the existing item's label is refreshed; votes are preserved.
- **`GetAnalyticsSprintsAsync` is unchanged**: the Poker sprint query is a separate sibling method (`GetPokerSprintsAsync`) that includes `Future` state sprints. The analytics query deliberately excludes `Future`; the two must never be merged.

## Relationships

- `GetSessionTicketsEndpoint` → `IPokerFokusService.GetSprintTicketsAsync`: Poker REST → Fokus gRPC
- `PokerFokusGrpcService.GetSprintTicketsAsync` → `IJiraClient.GetSprintIssuesAsync`: Fokus → Jira module (live)
- `JiraIssueFields.StoryPoints` (`customfield_10016`) → `PokerTicket.StoryPoints`: same field as write-back

## Source

F6-PlanningPoker spec Rule 12 (no tickets for Free-mode), spec §3.2.1 Option A ratification (live pass-through, no caching), `docs/specs/F6-PlanningPoker/definition/spec.md`.
