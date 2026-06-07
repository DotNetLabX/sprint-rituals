# Fokus Service

**Endpoint framework:** FastEndpoints
**Database:** SQLite (FokusDb)
**Port:** TBD (service index not yet assigned in docker-compose)

## Purpose

Fokus is a sprint analytics service that syncs data from Jira (sprints, tickets, developers, status transitions) and computes health metrics (completion rate, disruption rate, carry-over rate) for sprint retrospectives. It is a single-service system with no cross-service dependencies in v1.

## Domain model

- **Aggregates:** Sprint (int PK, owns SprintMemberships), Ticket (string PK, owns StatusTransitions)
- **Key entities:** Developer (Entity<string>), StatusTransition (Entity<int>), SprintMembership (composite key child, plain class)
- **Value objects:** `HealthThresholdConfig`, `HealthWeightConfig`, `QaHealthThresholdConfig`, `QualitySubScoreWeightConfig` (owned by AppSettings, stored as JSON columns via `OwnsOne().ToJson()`); `BugRatioConfig`, `XrayConfig`, `CycleTimeConfig`, `SyncConfig` (owned by AppSettings, stored as flat columns via `ComplexProperty`). Wire contract stays flat — the 4 new VOs are domain-internal, mapped to/from flat DTOs at the Settings endpoint boundary (ADR-007, Q1=A).
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

- **Sync-from-external:** All data originates from Jira API. Jira and Xray DTOs are mapped to domain types by boundary mappers at the API layer (`Features/Sync/JiraDomainMapper.cs` for Group-1 Jira→domain maps, `Features/Xray/XrayDomainMapper.cs` for Group-2 primitive construction). No DTO enters the Domain layer (ADR-006).
- **Natural keys:** Ticket.Id = Jira issue key (e.g. "FOK-123"), Developer.Id = Jira accountId. Sprint.Id = Jira sprint ID. These are never auto-generated (`ValueGeneratedNever`).
- **Computed metrics:** Analytics computation lives in domain calculators in `Fokus.Domain/Analytics/` (ADR-005). Calculators are pure domain logic — no `DbContext`, no HTTP dependencies. Endpoints load data, call calculators, and map domain result records to endpoint-owned response DTOs via Mapster (ADR-010). No computed metrics are stored in the DB.
- **Thin domain (v1):** Entities are data containers. No behavior methods in v1. Behaviors folder will be added when domain logic is needed.
- **AppSettings singleton:** Single row (Id=1), seeded on startup if missing. Holds board config, done-status list, workflow stages (`BoardId`, `DoneStatuses`, `WorkflowStages`, `ExcludedFromScopeStatuses`, `CompanyDomain` — top-level), health config VOs (`HealthThresholds`, `HealthWeights`, `QaHealthThresholds`, `QualitySubScoreWeights`, `QualityHealthWeight`), and 4 domain-internal config VOs (`BugRatio`, `Xray`, `CycleTime`, `Sync`) persisted as flat columns via `ComplexProperty`. API wire stays flat.
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
