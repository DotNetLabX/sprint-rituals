# Poker Module

**Endpoint framework:** FastEndpoints
**Database:** SQLite (poker.db)
**Port:** N/A — hosted inside the SprintRituals Host (class library)

## Purpose

Poker is a real-time planning poker tool for collaborative sprint estimation. A Scrum Master runs an estimation ceremony; the team votes on tickets with a Fibonacci scale; agreed story points are written back to Jira through Fokus. Two modes: Sprint-bound (items from a Fokus sprint, write-back to Jira) and Free (manual items, no write-back). Cross-module communication uses gRPC (synchronous Fokus surfaces: GetPokerSprints, GetSprintTickets, SetStoryPoints). No integration events in v1.

## Architecture

DDD, Vertical Slice, CQRS, Clean Architecture, Event Driven Design — same as Reflekt/Fokus. Module is **API + Domain + Persistence** only (no MediatR, no Application layer — ADR-004). FastEndpoints endpoint classes are the handlers. **No cross-module project references** — cross-module data arrives via gRPC.

## Module shape

Scaffolded from Reflekt (RA2 pattern). Same three-project structure:
- `Poker.Domain` — aggregates, events, value objects
- `Poker.Persistence` — PokerDbContext → poker.db, repositories, migrations
- `Poker.API` — FastEndpoints feature slices, auth, hub, DI

## Domain model

- `PlanningSession : AggregateRoot<Guid>` — the estimation ceremony
- `SessionParticipant : Entity<Guid>` — a participant (Google-authed or guest)
- `EstimationItem : Entity<Guid>` — one ticket/item being estimated
- `Vote : Entity<Guid>` — a participant's vote on an item

## Key patterns

- **Guest auth:** Poker-local `PokerGuestTokenAuthenticationHandler` with scheme `"PokerGuestToken"` — distinct from Reflekt's `"GuestToken"` scheme (Rule 23 / ADR-016 conscious duplication).
- **SM gate:** SM-only actions gated on `PlanningSession.CreatedByPersonId` vs. caller's `person_id` claim (session creator, not a stored role).
- **Real-time hub:** `PlanningHub` at `/hubs/poker` — connection group `poker:{sessionId}`.
- **Domain events → SignalR:** `IEventHandler<T>` broadcast handlers per Reflekt's `Broadcast*Handler` pattern.
- **No MassTransit consumers or publishers in v1.**

## DB

Connection string key: `PokerDatabase` → `poker.db` (under `data/` in Host working dir).

## gRPC client

Dials Fokus's `IPokerFokusService` (Blocks.Contracts) for:
- `GetPokerSprints` — Active + Future + recent Closed sprints
- `GetSprintTickets` — live Jira pass-through for sprint tickets
- `SetStoryPoints` — write agreed estimate back to Jira

Config key: `Grpc:Services:PokerFokus:Url`.

## Identity gate scope

`ActiveUserGateScope` in `Identity.API` excludes `"Poker.API"` assembly (Step 6 — mirrors RA2 exclusion for `"Reflekt.API"`).
