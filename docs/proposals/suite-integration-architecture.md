# Agentic Rituals Suite — Integration Architecture

**Status:** Decided  
**Date:** May 16, 2026  
**Context:** Reflekt v2 nearly complete, Fokus v1 shipped with Jira integration. This document captures the architectural decisions for integrating them into a unified suite, how each decision was reached, and what was explicitly deferred.

---

## 1. Monorepo — Modular Monolith

Reflekt and Fokus were two separate repositories sharing the same stack (.NET 10, Vue 3, TypeScript, Pinia, EF Core, SQLite, SignalR, Tailwind), the same architecture (DDD, Vertical Slice, CQRS), the same skills, the same Claude Code setup, the same building blocks, and the same Google OAuth. Maintaining two repos meant paying duplication tax on every infrastructure change.

The decision: merge into a single monorepo structured as a modular monolith. Each module (Identity, Reflekt, Fokus) retains its own API, Domain, and Persistence projects — the same separation as microservices. The only difference is a shared Host project that composes all modules into a single deployable.

```
src/
  Host/                          # Single deployable, composes all modules
    Program.cs
  BuildingBlocks/                # Shared auth middleware, gRPC contracts, base types
  Identity/
    Identity.API/                # Endpoints + service registration, no self-hosting
    Identity.Domain/
    Identity.Persistence/
  Reflekt/
    Reflekt.API/
    Reflekt.Domain/
    Reflekt.Persistence/
  Fokus/
    Fokus.API/
    Fokus.Domain/
    Fokus.Persistence/
  client/                        # Vue SPA
```

The Host's `Program.cs` composes the modules:

```csharp
builder.Services.AddIdentityModule();
builder.Services.AddReflektModule();
builder.Services.AddFokusModule();

app.MapIdentityEndpoints();
app.MapReflektEndpoints();
app.MapFokusEndpoints();
```

Why modular monolith over true microservices: the application is too small to benefit from three separate deployed services. The team is 10-15 people. One Azure App Service instance is sufficient. The modular structure means splitting to independent services later requires only extracting the Host — the project boundaries, database separation, and gRPC contracts are already in place.

---

## 2. gRPC via protobuf-net.Grpc

Inter-module communication uses gRPC. The decision to use gRPC over simpler alternatives (shared interfaces via DI, direct database reads) was made early and deliberately — gRPC enforces the privacy boundary at the protocol level, it matches the existing skill set and Claude Code skills, and it's the standard Laurentiu uses across all applications.

The library is **protobuf-net.Grpc** (Marc Gravell's code-first gRPC). This eliminates `.proto` files and protoc code generation entirely. Service contracts are C# interfaces with `[ServiceContract]` attributes, data contracts are POCOs with `[DataContract]` attributes. The contracts live in BuildingBlocks as shared project references.

```csharp
[ServiceContract]
public interface ISprintAnalyticsService
{
    ValueTask<AnonymizedSprintSummary> GetSprintSummaryAsync(SprintRequest request);
    ValueTask<DisruptionMetrics> GetDisruptionMetricsAsync(SprintRequest request);
    ValueTask<CarryOverHistory> GetCarryOverHistoryAsync(TeamRequest request);
}
```

Fokus implements the interface. Reflekt consumes it via `CreateGrpcService<T>()`. The privacy boundary (per-developer data never exposed to Reflekt) is enforced by the `AnonymizedSprintSummary` message type — the contract itself prevents leaking individual developer metrics.

The serialization overhead of gRPC in a single-process modular monolith (localhost HTTP/2) was considered and accepted. The calls are infrequent (once per retro session, roughly every two weeks), the payloads are small, and the architectural consistency with Laurentiu's other applications and existing skills outweighs the minor overhead.

---

## 3. Identity Model — Person as the Universal Anchor

The hardest design decision. Three services need to know about people, but each has a different concept of who a person is:

- **Fokus** knows about Jira developers (synced from Jira REST API, may never log into the app)
- **Reflekt** knows about retro participants (anonymous guests or Google-authenticated users)
- **Both** need Google OAuth for the Scrum Master

The solution separates three concepts:

| Entity | Owner | Description |
|--------|-------|-------------|
| **Person** | Identity service | The universal record — anyone who appears in the system from any source |
| **User** | Identity service | A Person who has authenticated via Google OAuth |
| **Developer** | Fokus | A Person who appears in Jira sprint data. Has domain-specific fields (SubTeam, IsActive, JiraAccountId). Optional FK to Person. |
| **Participant** | Reflekt | A Person participating in a retro session. Has domain-specific fields (GuestToken, role in the retro). Optional FK to Person. |

The Identity service owns Person and User. It exposes a **Person gRPC service** that other modules use to create, query, and link Person records. When Fokus syncs developers from Jira, it calls the Identity service to upsert Persons (matched by email). When Reflekt onboards a participant, it calls the Identity service to create or find a Person.

The linking path is: `Participant → Person ← Developer`, connected through the Person record in Identity. Reflekt never knows about Developer. Fokus never knows about Participant. Each service queries only its own side.

**How we reached this:** The initial proposal was three separate entity models with no shared ownership. This was rejected because it created duplication — the same human would exist as unlinked records across services with no correlation path. A shared `User` entity in BuildingBlocks was considered next, but rejected because it created a shared database dependency that defeats the microservice boundary. The Person-in-Identity approach gives a single source of truth without coupling the domain models.

**Anonymous-to-authenticated linking:** When a guest participant later signs in with Google, the system can link their guest Person record to their User record. The mechanism options discussed were: same-browser-session detection (guest token in localStorage + Google login in same browser), SM-initiated merge, or name-based prompting ("continue as [name] or start fresh?"). The exact mechanism is deferred to implementation — the important thing is that the Person model supports the link.

---

## 4. Sprint Ownership — Fokus Is the Source of Truth

Fokus owns sprint data. It syncs sprints from Jira, stores sprint metadata (name, dates, state, tickets, metrics), and computes all analytics.

Reflekt does not create sprints. When the SM starts a retro, they select a sprint from a list provided by Fokus via gRPC (e.g., `GetAvailableSprints`). Reflekt creates a **retro session** attached to the selected Fokus sprint ID. All sprint metadata displayed in Reflekt comes from Fokus.

This eliminates the sprint correlation problem entirely — there is no mapping to figure out because Reflekt always references a Fokus sprint by ID.

**Implication:** Reflekt is no longer standalone. It depends on Fokus for sprint data. This is acceptable because both modules live in the same Host — Fokus is always available. This is a deliberate departure from v1 and v2, where Reflekt was fully independent. The tradeoff is justified by the elimination of duplicate sprint management and the guaranteed data consistency.

---

## 5. Data Flow — Fokus → Reflekt, Reverse Deferred

The primary data flow is one-directional: Fokus provides sprint analytics to Reflekt.

**Fokus → Reflekt (active):**

- **gRPC on demand:** Reflekt calls Fokus for anonymized sprint metrics when the SM or a participant opens the sprint context panel. Cached for the session.
- **Integration events:** `SprintSynced` event notifies Reflekt when new sprint data is available. Reflekt stores a local `FokusSprintSnapshot` projection (sprint ID, name, dates, completion rate, disruption rate, carry-over rate, health score). Read-only, overwritten on re-sync.

**Reflekt → Fokus (deferred):**

This direction was evaluated and found to lack concrete value. The question asked: what would Fokus do with retro data? Most retro outcomes (action items, discussed themes) are behavioral and not measurable by Fokus. The only attributable signal — action items linked to Jira tickets — is a small subset that doesn't justify the integration plumbing.

The decision: don't build Reflekt → Fokus events until there is a concrete metric Fokus can compute from retro data. gRPC makes adding new service endpoints trivial when the need arises.

---

## 6. Integration Events

For async notifications between modules, MassTransit with in-memory transport. No RabbitMQ — overkill for an internal tool in a single process.

**Events defined:**

| Event | Source | Consumer | Purpose |
|-------|--------|----------|---------|
| `SprintSynced` | Fokus | Reflekt | Sprint data available, triggers local projection update |
| `HealthScoreCalculated` | Fokus | Reflekt (v3) | Could trigger agent-authored retro cards when health drops to red |

Event contracts live in BuildingBlocks alongside gRPC contracts. Events carry all the data the consumer needs to build its local projection — the consumer never calls back to the source for additional data.

In-memory transport means events are lost on process restart. Acceptable because `SprintSynced` is idempotent — re-syncing regenerates the event.

---

## 7. Database Topology

Three SQLite files, one per module. Each module has its own `DbContext` pointing to its own `.db` file:

- `identity.db` — Person, User tables
- `fokus.db` — Sprint, Developer, Ticket, SprintMembership, StatusTransition tables
- `reflekt.db` — RetroSession, Participant, CardGroup, Card, Vote, EvidenceLink tables, plus FokusSprintSnapshot projection

No cross-database joins. All cross-module data access goes through gRPC or integration events. This maintains the module boundary discipline and makes future service extraction straightforward.

---

## 8. Frontend Architecture

Two logically separate Vue applications sharing a routing shell. The backend's modular monolith guides the frontend structure — each module's pages are independent, sharing no components or state.

- `/retro/*` routes → Reflekt Vue app
- `/dashboard/*` routes → Fokus Vue app
- `/settings/*` routes → shared (auth, Identity management)

Each app has its own Pinia stores, its own components, its own views. The shared layer is minimal: routing configuration, Google OAuth state (read from the shared cookie), and possibly a thin UI kit (buttons, modals, toasts) to maintain visual consistency.

This structure allows splitting to separate SPAs later if the backend is ever decomposed into independent services.

---

## 9. Sprint Context Panel — Fokus Data in the Retro Room

The integration's most visible feature: a persistent button in Reflekt's top bar (available in all phases, to all participants) that opens a slide-out panel showing anonymized sprint metrics from Fokus.

**Contents:** completion rate, disruption rate, carry-over rate, health score indicators, top scope additions if disruption was high.

**Why a persistent button, not a one-time card:** A card shown once at the start of Writing means latecomers miss it and participants forget the numbers by Discussion. A persistent button means anyone can reference the data at any point — during discussion when someone says "I feel like we had a lot of scope change," they open the panel and see the actual number.

**Data flow:** One gRPC call to Fokus on retro session load, result cached for the session, rendered on demand when the button is pressed. The panel shows the same data regardless of phase — it's a reference tool, not a phase-specific feature.

**Privacy:** The panel shows only anonymized aggregate data (the `AnonymizedSprintSummary` message type). No per-developer metrics, no individual ticket assignments. The gRPC contract enforces this at the API level.

---

## 10. Anonymous Guest Participation — A Tryout Ramp

Anonymous/guest participation exists for people evaluating the tool. Real teams will use Google auth — a team of 10-15 people knows who everyone is, and anonymity in a small retro is cosmetic.

The guest flow (pick a display name, get a localStorage token, join the retro) is preserved from v1/v2 for zero-friction onboarding. If a returning guest enters the same name, the system prompts "continue as [name] or start fresh?" to reduce orphaned Person records. No IP tracking, no complex identity matching — not worth the engineering for a feature that covers the first 1-2 trial sprints before adoption or abandonment.

The real identity investment goes into the Google auth → Person → User path, which handles cross-sprint recognition, cross-service correlation, and the anonymous-to-authenticated upgrade.

---

## 11. Meeting-to-Spec — Data Producer, Not a Service

Meeting-to-spec is a Claude Code workflow (agents + skills + file-based handoffs) that produces structured artifacts: YAML-tagged meeting notes, specs, and Jira tickets. It is not a web application and is not promoted to a suite service.

Its artifacts are consumable by the suite tools:

- Meeting notes with domain tags and decision classifications can be referenced as evidence links in Reflekt cards
- Specs produced by the pipeline feed into Jira tickets, which Fokus tracks
- The YAML schema (tags, domain, participants, actions, confidence scores) is stable enough for future ingestion if a memory layer is ever built

No integration plumbing is built for meeting-to-spec now. The connection is through existing artifacts (Jira tickets, URLs) that both Reflekt and Fokus already handle.

---

## 12. What Was Explicitly Deferred

| Item | Reason |
|------|--------|
| Reflekt → Fokus data flow | No concrete metric Fokus can compute from retro data yet |
| Memory layer as a separate service | Each tool querying its own history is sufficient; cross-tool correlation is premature |
| Spec + Delivery Tracker | Depends on memory layer; v3+ at earliest |
| Agent-authored retro cards from Fokus events | Requires Reflekt v3 agent infrastructure |
| Scheduled Fokus sync (webhook/cron) | Manual sync is sufficient for v1 |
| Multi-board Fokus support | Single board covers current team |
| Standalone Reflekt operation without Fokus | Accepted tradeoff of monorepo integration |

---

## 13. Migration

Fresh repo. Existing Reflekt and Fokus repos are not merged with git history preservation — not worth the complexity. The new monorepo is published to Laurentiu's personal GitHub (`ldumit`) first (private), establishing independent authorship provenance before any company fork, consistent with the IP strategy from the suite strategy document.

