# Fokus Service

**Endpoint framework:** FastEndpoints
**Database:** SQLite (FokusDb)
**Port:** TBD (service index not yet assigned in docker-compose)

## Purpose

Fokus is a sprint analytics service that syncs data from Jira (sprints, tickets, developers, status transitions) and computes health metrics (completion rate, disruption rate, carry-over rate) for sprint retrospectives. It is a single-service system with no cross-service dependencies in v1.

## Domain model

- **Aggregates:** Sprint (int PK, owns SprintMemberships), Ticket (string PK, owns StatusTransitions)
- **Key entities:** Developer (Entity<string>), StatusTransition (Entity<int>), SprintMembership (composite key child, plain class)
- **Value objects:** HealthThresholdConfig, HealthWeightConfig (owned by AppSettings, stored as JSON columns)
- **Application layer:** None — handler logic in endpoints

## Endpoint pattern

Class extending `Endpoint<TRequest, TResponse>` or `EndpointWithoutRequest<TResponse>`. Handler logic lives directly in `HandleAsync`. Validators extend `Validator<T>` as sibling classes in the same feature folder. Endpoints are auto-discovered by `AddFastEndpoints()`.

## MediatR pipeline behaviors

N/A — FastEndpoints native, no MediatR.

## Domain event infrastructure

- **Publisher:** `Blocks.FastEndpoints/DomainEventPublisher` — dispatches via `IEvent.PublishAsync(Mode.WaitForAll)`
- **Interceptor:** `DispatchDomainEventsInterceptor` (standard, non-transactional) — registered as `ISaveChangesInterceptor`
- **v1 status:** Interceptor is registered and harmless. No domain events are raised in v1. Ready for future use.
- **Handlers:** `IEventHandler<T>` implementations live alongside their feature slices in `Fokus.API/Features/`

## Key patterns

- **Sync-from-external:** All data originates from Jira API. `JiraMapper` maps Jira DTOs → domain entities.
- **Natural keys:** Ticket.Id = Jira issue key (e.g. "FOK-123"), Developer.Id = Jira accountId. Sprint.Id = Jira sprint ID. These are never auto-generated (`ValueGeneratedNever`).
- **Computed metrics:** Health scores are computed on-the-fly in endpoints — never stored in the DB.
- **Thin domain (v1):** Entities are data containers. No behavior methods in v1. Behaviors folder will be added when domain logic is needed.
- **AppSettings singleton:** Single row (Id=1), seeded on startup if missing. Holds board config, done-status list, workflow stages, health thresholds, health weights.
- **Focused operation services:** When handler logic is shared across endpoints in the same feature area, extract into a service class named after the operation (not the entity). Lives in the feature area root (e.g., `Features/Sync/SprintIssueSyncService.cs`). Must stay single-purpose — if it grows beyond one operation, split it.

## Existing features

- `GET /api/settings` — retrieve current AppSettings
- `POST /api/settings` — save AppSettings
- `GET /api/sync/boards` — list Jira boards
- `GET /api/sync/sprints` — list Jira sprints for a board
- `POST /api/sync/sprints` — sync sprint range from Jira
- `POST /api/sync/backlog` — sync future sprints and epic tickets from Jira

## gRPC clients

None

## gRPC server

None

## Integration events

**Published:** None
**Consumed:** None

## Jira integration

- `JiraClient` — HTTP client, reads from `appsettings.json` `Jira` section (`InstanceUrl`, `Email`, `ApiToken`, `DefaultProjectKey`). Throws `UnauthorizedException` for 401 and `BadGatewayException` for other Jira errors — handled by `GlobalExceptionMiddleware`, not by endpoints.
- `JiraOptions` — bound from `appsettings.json`, validated on start
