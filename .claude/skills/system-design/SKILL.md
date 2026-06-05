---
name: system-design
description: Architect-only skill — system decomposition, domain lifecycle design, service boundary definition, variant axis selection. Loaded by the Architect agent when designing new apps or services.
user-invocable: true
---

# System Design (Architect Reference)

This skill is for the **Architect agent only**. It provides templates and checklists for designing new applications or decomposing domains into services. The Developer never loads this skill — they receive the Architect's decisions via plan files.

## 1. Variant Axes

The Architect picks these 3 axes first. Everything else cascades:

| Axis | Options | Cascading Effects |
|------|---------|-------------------|
| **Endpoint framework** | FastEndpoints / Carter / Minimal APIs | Validator base, user ID assignment, swagger setup, endpoint file structure |
| **CQRS/Dispatch** | MediatR / FastEndpoints events | Domain event handler interface, pipeline behaviors, DomainEventPublisher implementation |
| **Persistence** | EF Core + SQL Server / EF Core + PostgreSQL / Redis | Repository base, seed approach, interceptors, DbContext type, folder structure |

### Axis Selection Guide

**Endpoint framework:**
- **FastEndpoints** — when handler logic is simple (no separate handler class needed), endpoint + command in one class. Good for: simple CRUD services, services without complex pipelines.
- **Carter + MediatR** — when you want clean endpoint-to-handler separation, pipeline behaviors (validation, logging, user assignment). Good for: domain-heavy services with complex handlers.
- **Minimal APIs + MediatR** — same benefits as Carter but with static method endpoints and explicit registration. Good for: when you prefer explicit route wiring over auto-discovery.

**CQRS/Dispatch:**
- **MediatR** — when you need pipeline behaviors, `INotificationHandler` for domain events, or the service has complex cross-cutting concerns. Most services use this.
- **FastEndpoints events** — when the service is simple and doesn't need pipeline behaviors.

**Persistence:**
- **EF Core + SQL Server** — default. Complex domain, relational data, ACID transactions, reporting queries.
- **EF Core + PostgreSQL** — same as SQL Server but for read-heavy aggregate views (read-model pattern).
- **Redis** — read-heavy, low-complexity domain, shallow entity graph, data fits in memory. 1-2 services per system max.

### Cascading Effects Matrix

| Concept | FastEndpoints + FE Events | Carter + MediatR | Minimal APIs + MediatR |
|---------|---------------------------|-------------------|------------------------|
| User ID assignment | FE PreProcessor | MediatR Behavior | MediatR Behavior (or EndpointFilter) |
| Validator | `Validator<T>` (auto-discovered) | `AbstractValidator<T>` + `ValidationBehavior` | `AbstractValidator<T>` + `ValidationBehavior` |
| Domain event handler | `IEventHandler<T>.HandleAsync()` | `INotificationHandler<T>.Handle()` | `INotificationHandler<T>.Handle()` |
| DomainEventPublisher | `Blocks.FastEndpoints/DomainEventPublisher` | `Blocks.MediatR/DomainEventPublisher` | `Blocks.MediatR/DomainEventPublisher` |
| Endpoint discovery | Auto (FastEndpoints scans assembly) | Auto (`AddCarter()` scans assembly) | Manual (`MapAllEndpoints()`) |
| Application layer | Optional (logic can live in endpoint) | Yes (handlers in Application project) | Yes (handlers in Application project) |

## 2. Domain Lifecycle Template

When designing a new domain, answer these questions:

### States and Transitions
- What are the core entity states? (e.g., Draft → Submitted → Reviewed → Published)
- What triggers each transition? (user action, event from another service, timer)
- Which transitions are reversible?
- Are transition rules static or configurable (DB-stored)?

### Service Ownership
- Which service owns which states?
- Where do handoffs happen between services?
- What data does each service need to own vs receive?

### Event Flow
- Which state transitions produce domain events?
- Which domain events bridge to integration events?
- Which services consume which integration events?
- What does each consumer create locally? (factory method pattern)

### State Machine Implementation
If the domain has a state machine:
- Store transition rules in DB (`{Entity}StageTransition` table seeded via master data JSON)
- `{Entity}StateMachineFactory` registered as delegate factory
- Aggregate behavior methods validate transitions before mutation
- Per-service action enum defines allowed actions

## 3. Service Boundary Checklist

When decomposing into services:

### Bounded Context Rules
- Each service owns its data — no shared databases
- Each service defines its own representation of cross-cutting entities
- Communication: gRPC for synchronous queries, integration events for async state propagation
- Default to module first; extract to microservice only when deployment/scaling/ownership demands it

### Per-Service Decisions
For each service, the Architect specifies in the plan:
1. **Endpoint framework** (axis 1)
2. **CQRS/dispatch approach** (axis 2)
3. **Persistence technology** (axis 3)
4. **Folder structure variant:** 4-project (complex) / 3-project (simple) / merged (read-only)
5. **Port assignment** (4400-4499 range, HTTP/HTTPS pairs)
6. **gRPC contracts** it exposes and consumes
7. **Integration events** it publishes and subscribes to

### Capturing decisions into CLAUDE.md

After making the decisions above, capture them into the target file by running:

- **For a new service:** `create-service-claude-md` → writes `src/Services/{Name}/CLAUDE.md`
- **For a new module:** `create-module-claude-md` → writes `src/Modules/{Name}/CLAUDE.md`

Both skills are Architect-only (`user-invocable: false`). They walk the Architect through the full axis checklist — pre-filling from the current conversation — and confirm with the user before writing.

Once the CLAUDE.md exists, the Developer runs the scaffold skill in a Developer session:

- **For a new service:** `create-service {Name}` → reads the CLAUDE.md and scaffolds the empty skeleton (folders, csprojs, DI stubs, Dockerfile, docker-compose entry, solution registration)
- **For a new module:** `create-module {Name}` → same, branching on archetype (Component vs Domain)

After scaffolding, the Developer continues with `create-aggregate` and `create-feature` for the business work.

### Folder Structure Variants

| Variant | Layout | When |
|---------|--------|------|
| 4-project | API / Application / Domain / Persistence | Complex domain, CQRS, separate handler layer |
| 3-project | API / Domain / Persistence | Simple CRUD, handler logic in endpoints |
| Merged | API+Application / Domain / Persistence | Read-only aggregation, no domain logic |

## 4. Communication Pattern Guide

| Pattern | When | Mechanism |
|---------|------|-----------|
| **gRPC (sync)** | Service needs data from another service to process a request | Code-first contracts (`[ServiceContract]`), `AddCodeFirstGrpcClient<T>()` |
| **Integration event (async)** | Service needs to react to state changes in another service | MassTransit + RabbitMQ, record types in shared contracts |
| **Event → local aggregate creation** | Receiving service needs its own copy of data | Consumer calls factory method (e.g., `{Entity}.From{Source}()`) |
| **API Gateway** | External clients need unified entry point | YARP reverse proxy, route-based forwarding |

### Decision Rules
- If the caller **blocks** until it gets a response → gRPC
- If the caller **doesn't care** about the response timing → integration event
- If the data is needed for **read-only display** → gRPC query
- If the data triggers **local state creation** → integration event + consumer + factory method
- **Never** share a database between services
