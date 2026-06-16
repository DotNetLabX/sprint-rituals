# Reflekt Module

**Endpoint framework:** FastEndpoints
**Database:** SQLite (reflekt.db)
**Port:** TBD (service index not yet assigned)

## Purpose

Reflekt is a real-time collaborative sprint-retrospective tool. A team runs a retro as a phased session (Writing → Grouping → Voting → Discussion) over a board of cards: participants add cards, group them, vote, and discuss. Reflekt consumes anonymized sprint analytics from Fokus to ground the retro in real sprint data. Cross-module communication uses gRPC (synchronous) and the in-process MassTransit bus (integration events).

## Status — in-flight port

Reflekt is being ported into the monolith in sequenced passes (lift-and-shift first, then refactor/integrate — see the Reflekt program in `docs/backlog.md`). The binding end-state is captured in `docs/architecture/decisions.md` ADR-016…020. Until the later passes land, the lifted code still differs from the target:

- **Auth (ADR-016):** target is Identity-owned (Google + cookie via the Identity module); Reflekt keeps only guest-token auth. **Landed in RA2 (adhoc-ReflektIdentityUnification):** Reflekt's own `User` aggregate and parallel Google/cookie config removed; only the guest-token scheme remains (chained onto Identity's auth builder). RA5 carry-over: wire authenticated `Participant.PersonId` into the sprint-context panel.
- **RetroSession (ADR-018):** `Sprint` → `RetroSession` rename and Fokus-sprint linkage (1..N `FokusSprintRef` child rows, `FokusSprintSnapshot` projection, `SprintSynced` consumer, `CreateRetroSession` route). **Landed in RA3 (adhoc-ReflektSprintOwnership).** Child FK columns kept `SprintId`-named (Q1=b). SignalR method/group/claim strings kept `sprint*`-named (Q2). HTTP lifecycle routes renamed to `/retro-sessions`. RA5 carry-overs: wire `FokusSprintIds` field into client UI and OpenAPI schema.
- **Fokus linkage (ADR-017/019):** `SprintSynced` consumer and `FokusSprintSnapshot` projection **landed in RA3**. `ISprintAnalyticsService` gRPC client and `GET /api/retro-sessions/{id}/sprint-context` endpoint **landed in RA4 (adhoc-SprintContextPanel)**. RA5 carry-overs: Vue sprint-context panel component; wire `FokusSprintIds` into CreateRetroSession client UI.

## Architecture

DDD, Vertical Slice, CQRS, Clean Architecture, Event Driven Design — same as the rest of the monolith. Module is **API + Domain + Persistence** only (no MediatR, no Application layer — ADR-004). FastEndpoints endpoint classes are the handlers. **No cross-module project references** — cross-module data arrives via gRPC and integration events.

## Domain model

- **Aggregates:**
  - `RetroSession` — the retro board (phases, columns, focus). Renamed from `Sprint` per ADR-018 (landed RA3). References 1..N Fokus sprints via `FokusSprintRef` child table.
  - `Team` — owns retros; membership.
  - `Card` — a retro note; owns its `EvidenceLink`s.
  - `CardGroup` — clusters cards during Grouping; carries discussed/focus state.
  - `Vote` — a participant's vote on a card/group.
- **Key entities:** `Participant` (a person in a retro — guest token or Google-authed; optional FK to Identity `Person`, ADR-016), `EvidenceLink` (URL attached to a card, OpenGraph-fetched).
- **Projections:** `FokusSprintSnapshot` — read-only Fokus sprint metrics, one row per referenced sprint, overwritten on re-sync (ADR-019).

## Endpoint pattern

Two files per feature (ADR-008): `{Feature}Endpoint.cs` (endpoint + handler) and `{Feature}Command.cs` / `{Feature}Query.cs` (request + response + DTOs + validator). Reflekt already follows the `Command`/`Query` naming (ADR-009). Validators are sibling `Validator<T>` classes, auto-discovered by FastEndpoints.

## Domain event infrastructure

- **Bus:** FastEndpoints `IEvent` in-process pub/sub. Aggregates raise events onto a `DomainEvents` list; a `SaveChangesInterceptor` in Persistence publishes them after a successful save.
- **Handlers:** `IEventHandler<T>` implementations live with their feature slices — the `Broadcast*Handler` classes translate domain events into SignalR pushes (e.g. `BroadcastCardCreatedHandler`, `BroadcastVoteCountOnVoteCastHandler`).

## Real-time (SignalR)

A retro hub pushes card / group / vote / participant / phase changes to the retro's connection group so every participant stays in sync. Optimistic UI on the client, reconciled by the broadcast. (Hub class name confirmed at lift.)

## Key patterns

- **Phased state machine** — `RetroSession` advances Writing → Grouping → Voting → Discussion; phase-gated actions enforced server-side.
- **OpenGraph fetch** — `OpenGraphBackgroundService` fetches link previews for evidence links off the request path.
- **Phase timer** — `TimerService` drives phase countdowns.
- **Guest participation** — anonymous participants join via a localStorage guest token (`GuestTokenAuthenticationHandler`); retained per ADR-016.

## Cross-module

- **gRPC client — `IPersonService`** (Identity server, ADR-016): upsert/link `Participant` → `Person`. Best-effort.
- **gRPC client — `ISprintAnalyticsService`** (Fokus server, ADR-019): anonymized sprint metrics for the sprint-context panel; cached per session.
- **Integration event consumed — `SprintSynced`** (Fokus, ADR-019): refresh the `FokusSprintSnapshot` projection.

## Frontend

`/retro/*` routes (ADR-020). Vue 3 + TypeScript + Pinia + Tailwind. Module-isolated stores/components/views; shares only the routing shell, Google-auth cookie state, and a thin UI kit.
