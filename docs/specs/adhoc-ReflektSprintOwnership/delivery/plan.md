# Implementation Plan — adhoc-ReflektSprintOwnership (RA3)

**Type:** ad-hoc refactor + integration (Reflekt Integration Program, RA3). No `spec.md` — binding inputs are the **ADR register** (ADR-016…022, esp. **ADR-017/018/019**) + the backlog RA3 row + RA1/RA2 carry-overs.
**Grounded against:** committed `main`, HEAD `ca1ecd6`. Every count/route/symbol below comes from a grep run at plan time (queries pasted into the acceptance criteria), not from memory.
**Review mode:** critic (code-grounded — reads live Reflekt source + grep).
**DB stance (Q5):** dev `reflekt.db` is disposable; rename migration is `Up()`-meaningful; `Down()` is schema-reversible, not data-reversible (inherits RA2).

---

## Goal

Land ADR-017/018/019 on the Reflekt module:
1. Rename the retro aggregate `Sprint` → `RetroSession` and its retro-state types (Q1=b).
2. Give a `RetroSession` a **1..N Fokus-sprint reference** and a **`FokusSprintSnapshot`** read-projection (ADR-018/019).
3. **Consume `SprintSynced`** (Fokus → Reflekt) to refresh the snapshot (ADR-019), event-only/forward-only (Q4=a).
4. Make retro creation **require ≥1 Fokus sprint** (Q3=b) — "drop standalone creation" = drop *un-linked* creation, not the endpoint.
5. Rename Reflekt's **HTTP lifecycle routes + JSON shapes** (Q2), **preserving** the SignalR + FK-column wire layer for RA5.

Bisect discipline: this is the *integrate* half of lift-then-integrate. Keep the rename (mechanical) and the Fokus-link (semantic) legible as separate steps.

---

## Binding decisions (from user-confirmed Q1–Q5 — see `questions.md`)

| # | Decision | Binding consequence |
|---|----------|---------------------|
| Q1=b | Rename aggregate **+ retro-state types**; **keep** child FK columns named `SprintId`. | Rename table below is the *complete* set. `SprintId` columns/params are a **DO-NOT-TOUCH** list. |
| Q2 | Rename **HTTP lifecycle routes + JSON**; **preserve** SignalR method/group/claim strings. | `JoinSprint`/`LeaveSprint`, `"sprint:{id}"`, `"sprintId"`/`participantId`/`displayName` claims are **DO-NOT-TOUCH**. |
| Q3=b | Keep creation, rename `CreateRetroSession*`, require **1..N `FokusSprintIds`**. | New required request field; ≥1 validated. |
| Q4=a | `FokusSprintSnapshot` holds **exactly the `SprintSynced` fields**; **no gRPC client** in RA3. | Rich metrics + on-demand pull = RA4 carry-over. |
| Q5 | Disposable dev DB. | Migration `Up()`-only meaningful. |

### Sub-decision resolved in-plan (flagged, not re-asked): the flat `/api/sprints/{sprintId}/*` child-route family STAYS
A grep of `[Http*("/api/sprints/...")]` returns **24 child-resource routes** (cards, votes, card-groups, participants, evidence-links, previous-actions) keyed on the `{sprintId}` route param. That param **is** the `Participant.SprintId`/`Card.SprintId` FK value — which Q1(b) keeps named `SprintId` and Q2 preserves at the claim layer. Renaming these routes to `/api/retro-sessions/{sprintId}` while the param stays `sprintId` is incoherent, and renaming the param contradicts Q1(b). **Resolution (deterministic from the confirmed answers, not a new decision): the flat `/api/sprints/{sprintId}/*` family is DO-NOT-TOUCH in RA3**, deferred to RA5 with the rest of the FK-column/wire rename. Only the **lifecycle** routes already in resource-noun form rename (Step 5). If the user wants the flat family renamed *now*, that reopens Q2 — flagged here, default is keep.

---

## Rename Table (byte-checkable — the done-check reads this field-by-field)

**Domain types (Q1=b) — `src/Services/Reflekt/Reflekt.Domain/Sprints/` → `…/RetroSessions/`:**

| Old | New |
|-----|-----|
| `Sprint` (aggregate, both partials) | `RetroSession` |
| `SprintState` (Draft/Active/Closed) | `RetroSessionState` |
| `SprintPhase` (Writing/Grouping/Voting/Discussing/Reporting) | `RetroPhase` |
| `SprintAction` (enum) | `RetroAction` |
| `ISprintAction` | `IRetroAction` |
| `SprintCreatedEvent` / `SprintStartedEvent` / `SprintClosedEvent` | `RetroSessionCreatedEvent` / `…StartedEvent` / `…ClosedEvent` |
| namespace `Reflekt.Domain.Sprints` | `Reflekt.Domain.RetroSessions` |

**Persistence / API types:**

| Old | New |
|-----|-----|
| `Sprints` DbSet | `RetroSessions` |
| table `"Sprints"` | `"RetroSessions"` |
| `SprintConfiguration` | `RetroSessionConfiguration` |
| `SprintRepository` | `RetroSessionRepository` |
| `SprintMappings` | `RetroSessionMappings` |
| `SprintResponse` / `SprintSummaryResponse` / `SprintSummaryParticipant` / `SprintRouteRequest` | `RetroSessionResponse` / `RetroSessionSummaryResponse` / `RetroSessionSummaryParticipant` / `RetroSessionRouteRequest` |
| `CreateSprintCommand` / `CreateSprintEndpoint` (+ validator) | `CreateRetroSessionCommand` / `CreateRetroSessionEndpoint` |
| `Features/Sprints/` folder | `Features/RetroSessions/` |

**JSON property names (Q2 — these change; no in-repo consumer, RA5 adapts):** the renamed response records carry the same field set; serialized names follow the C# property names (e.g. `state`, `currentPhase`). No `[JsonPropertyName]` pinning needed (unlike RA2's `UserId` — that had a kept name; here we rename freely).

**`Report*` types are intentionally OUT of the rename set (critic LOW #4).** `ReportResponse`, `ReportSprintInfo`, and the other `Report*` records in `Reflekt.API/Features/Reports/ReportContracts.cs` (incl. the `ReportSprintInfo Sprint` property and `ReportPreviousActionsSection.PreviousSprintName`) describe the **retro report** payload — they are retained as-is in RA3. The `GetSprintReport`/`GetParticipantReport` *endpoint folders* and their flat `/api/sprints/{sprintId}/report` route are in the flat DO-NOT-TOUCH family; the team-scoped `GetSprintReport` *route* renames (table above) but its **response type stays `ReportResponse`**. These report-DTO names are a wire surface deferred to RA5 with the rest; do not rename them in RA3.

### DO-NOT-TOUCH (binding carve-out — homonyms & preserved wire; the done-check FAILS if any of these changed)

| Item | Where (grep) | Why preserved |
|------|--------------|---------------|
| `Participant.SprintId`, `Card.SprintId`, `CardGroup.SprintId` properties + columns | `*Configuration.cs`, aggregate props | Q1=b keeps FK columns; rename = RA5 |
| **`Card.ReviewedInSprintId`** — the **4th** FK to the `Sprints`/`RetroSessions` table (`HasForeignKey(c => c.ReviewedInSprintId).OnDelete(SetNull)`) + its index `IX_Cards_ReviewedInSprintId` + the constraint `FK_Cards_Sprints_ReviewedInSprintId` | `CardConfiguration.cs:32-33` | **critic HIGH #2.** Q1=b keeps the column `SprintId`-style → guard against EF's principal-rename cascade renaming it to `ReviewedInRetroSessionId`. Property, column, index all stay; rename = RA5. (The FK *constraint name* may follow the table rename to `FK_Cards_RetroSessions_ReviewedInSprintId` — that is a constraint-name change, NOT a column rename, and is acceptable. The **column** `ReviewedInSprintId` must not change.) |
| `{SprintId, PersonId}` filtered unique index; `HasIndex(p => p.SprintId)` | `ParticipantConfiguration.cs:24-25` | same |
| The 24 flat routes `[Http*("/api/sprints/{sprintId}/...")]` | `Cards/`, `Votes/`, `CardGroups/`, `Participants/`, `EvidenceLinks/`, `PreviousActions/`, `JoinSprint/`, `GetSprintSummary/`, `UpdateColumns/` | keyed on `{sprintId}` FK param (sub-decision above) |
| SignalR hub class `SprintHub`, route `/hubs/retro`, methods `JoinSprint`/`LeaveSprint`, group `"sprint:{id}"`, claims `"sprintId"`/`participantId`/`displayName` | `SprintHub.cs`, `MapReflektEndpoints` | Q2 preserves SignalR layer for RA5 |
| `PhaseAction` / `IRetroAction`-consumers under `Features/PhaseActions/` that key on `SprintId` param | `PhaseActionCache.cs`, `PhaseActionConfiguration.cs` | flat-route family |
| Migration Designer/Snapshot `.cs` under `Migrations/` (historical) | `Migrations/*.Designer.cs`, `*ModelSnapshot.cs` | frozen history — only the NEW migration is authored |

> **Note on `SprintHub` + `SprintRepository` collision:** the hub stays `SprintHub` (Q2 DO-NOT-TOUCH) but the repo becomes `RetroSessionRepository`. `SprintHub` injects the repo via `IServiceScopeFactory.GetRequiredService<…>()` (lines 64, 190, 229) — those resolve calls update to `RetroSessionRepository`; the hub *class name* does not change. This is the one place a preserved name and a renamed name sit in the same file — called out so it's not flagged as inconsistent.

---

## Grounded facts (verified at plan time)

- **`Sprint` token:** 153 occ / 59 files. Of those, retro-state-type files = 39 (grep `SprintState|SprintPhase|SprintAction|ISprintAction`); contract files = 14; the rest are aggregate/persistence/migration. Migration `.Designer.cs`/snapshot files are DO-NOT-TOUCH (frozen).
- **`SprintSynced`** (`Blocks.Contracts/IntegrationEvents/SprintSynced.cs`): `record SprintSynced(int SprintId, string SprintName, int BoardId, DateTime SyncedAt, string State)`. Published in `Fokus.API/Features/Sync/SprintIssueSyncService.cs:80` via `IPublishEndpoint`. This is the **exact** field set the `FokusSprintSnapshot` stores (Q4=a). The snapshot's natural key is the Fokus `SprintId`.
- **MassTransit bus** is registered in `Fokus.API/DependencyInjection.cs:36` (`AddMassTransit` → `AddConsumers(Fokus assembly)` → `UsingInMemory`). **Reflekt has no MassTransit registration.** RA3 must add a Reflekt-side registration (see Step 4 — this is the Build/infra part). Consumer pattern reference (shape, not structure): `SeedCompanyDomainOnFirstAdministratorRegisteredConsumer` (`IConsumer<T>`, mutate-via-repository, idempotent).
- **`AnonymizedSprintSummary`** (rich metrics) is a **gRPC** contract (`ISprintAnalyticsService`) — its client is **RA4**, NOT RA3. Do not wire it here.
- **Reflekt module DI:** `AddReflektModule` (`Reflekt.API/DependencyInjection.cs`) composes persistence + Mapster + auth + feature services. Persistence DI (`Reflekt.Persistence/DependencyInjection.cs`) registers repos + `ReflektDbContext` (conn-string key `ReflektDatabase`). Host `Program.cs` calls `AddReflektModule` (Add) + `app.Migrate<ReflektDbContext>()` (Init) + `MapReflektEndpoints` (Use).
- **Creation today:** `CreateSprintEndpoint` (`POST /api/teams/{teamId}/sprints`) — SM-only, column-config fallback to most-recent sprint, no Fokus link. `Sprint.Create(...)` factory in `Behaviors/Sprint.cs:11`.
- **Tests:** `Reflekt.Domain.Tests` exists (`ParticipantJoinTests.cs`). No `SprintTests` present — the aggregate has no current unit test; new behavior (creation requires Fokus sprints; snapshot upsert) is testable.

---

## Steps

### Step 1 — Rename the retro aggregate + retro-state types (`Sprint` → `RetroSession`, Q1=b)
**Satisfies:** ADR-018 (aggregate rename)
**Skill:** None (mechanical rename/type-move — no skill teaches a project-wide rename) · **TDD:** no (pure rename; behavior unchanged — existing tests must still pass)

Apply the **Rename Table** (domain-types + persistence/API-types sections) across the Reflekt module. This is a rename only — **no behavior, no state-machine, no validation change.** Folder `Reflekt.Domain/Sprints/` → `Reflekt.Domain/RetroSessions/`; `Features/Sprints/` → `Features/RetroSessions/`.

**Binding constraints:**
- Apply **only** the Rename Table. Every item on the **DO-NOT-TOUCH** list stays byte-identical. In particular: do **not** rename `Participant.SprintId`/`Card.SprintId`/`CardGroup.SprintId`, the flat `/api/sprints/{sprintId}/*` routes, or any SignalR string.
- `SprintHub` class name stays (DO-NOT-TOUCH) but its `GetRequiredService<SprintRepository>()` resolves (`SprintHub.cs:64,190,229`) become `RetroSessionRepository`. The `Reflekt.Domain.Sprints.SprintPhase.*` fully-qualified refs in `SprintHub.cs:66-69,234-236` become `Reflekt.Domain.RetroSessions.RetroPhase.*`.
- The `GetMostRecentClosedByTeamIdAsync` / `GetByTeamIdAsync` repo methods keep their names (not in the table).
- `DbInitializer.cs` seed references (`db.Sprints`, `Sprint.Create`, local `sprint` var) update to the new type/DbSet; the seed **stays** un-linked for now — Step 3 decides whether the seed gets a Fokus-sprint link (it does not need one in dev; note as a seed exception).

**Acceptance:**
- `grep -rn "\bSprint\b" src/Services/Reflekt --include=*.cs` (excluding `Migrations/*.Designer.cs`, `*ModelSnapshot.cs`, and the DO-NOT-TOUCH SignalR/`SprintHub`/`SprintId`/flat-route lines) returns **only**: the preserved `SprintHub` class + its members, `SprintId` FK properties/columns/indexes/params, and the flat `/api/sprints/{sprintId}` route literals. Every aggregate/state-type/contract/event reference is now `RetroSession*`/`Retro*`.
- `grep -rn "SprintState\|SprintPhase\|SprintAction\|ISprintAction" src/Services/Reflekt --include=*.cs` (excluding `Migrations/`) → **0 hits**.
- `grep -rn "namespace Reflekt.Domain.Sprints\|Reflekt.Domain.Sprints" src/Services/Reflekt --include=*.cs` (excluding `Migrations/`) → **0 hits**.
- `dotnet build SprintRituals.slnx` green; `dotnet test src/Services/Reflekt/Reflekt.Domain.Tests` green (existing tests unchanged — they reference `Participant`, not `Sprint`).

### Step 2 — Add the Fokus-sprint reference (1..N) + `FokusSprintSnapshot` projection to the domain & persistence
**Satisfies:** ADR-018 (1..N reference), ADR-019 (snapshot projection)
**Skill:** Follow `create-aggregate` (the `FokusSprintSnapshot` is a small read-projection entity + the link is an owned/child collection on `RetroSession`) · **TDD:** yes (the "references ≥1 Fokus sprint" invariant + link-add behavior are domain logic)

Add to the `RetroSession` aggregate a **1..N reference** to Fokus sprint IDs, and a **`FokusSprintSnapshot`** read-projection (ADR-019: one row per referenced Fokus sprint, overwritten on re-sync).

**Feature-specific inputs:**
- **Link model:** a `RetroSession` references 1..N Fokus sprints. Model as a child collection of a small owned type `FokusSprintRef { int FokusSprintId }` on `RetroSession` (the link), keyed `(RetroSessionId, FokusSprintId)`. The `RetroSession.Create` factory takes `IReadOnlyCollection<int> fokusSprintIds` and **guards ≥1** (throws if empty — this is the ADR-018 invariant). Add a behavior method to add a reference (for the skipped-retro "next covers two" case).
- **`FokusSprintSnapshot`** — a **separate** projection entity (NOT owned by `RetroSession`; it is refreshed by the `SprintSynced` consumer independently of any retro). Fields = **exactly** the `SprintSynced` payload: `int SprintId` (PK — the Fokus sprint id, natural key, `ValueGeneratedNever`), `string SprintName`, `int BoardId`, `DateTime SyncedAt`, `string State`. Read-only projection: no behavior beyond a create/overwrite upsert helper. New folder `Reflekt.Domain/FokusSprints/` (keep it out of `RetroSessions/` — it is a Fokus-owned projection, not retro state).
- **Persistence (Follow `persistence-patterns`):** `FokusSprintSnapshotConfiguration` (table `"FokusSprintSnapshots"`, PK `SprintId` `ValueGeneratedNever`), the `FokusSprintRef` link table config (composite key), `DbSet<FokusSprintSnapshot>` + the link DbSet on `ReflektDbContext`, and a `FokusSprintSnapshotRepository` with an `UpsertAsync(snapshot)` (overwrite-on-re-sync) + `GetByIdAsync`. Register repos in `Reflekt.Persistence/DependencyInjection.cs`.

**Acceptance:**
- `RetroSession.Create(name, teamId, fokusSprintIds, …)` throws when `fokusSprintIds` is empty; persists one link row per id; TDD red→green for both (empty→throw, N ids→N links).
- `FokusSprintSnapshot` table + `FokusSprintRefs` (or chosen link-table name) table exist after migration (Step 6); snapshot PK is the Fokus `SprintId`.
- `FokusSprintSnapshotRepository.UpsertAsync` overwrites an existing row with the same `SprintId` (no duplicate; last-write-wins) — TDD covers re-upsert.
- Snapshot carries **only** the 5 `SprintSynced` fields — no `CompletionRate`/`HealthScore`/etc. (those are RA4's gRPC `AnonymizedSprintSummary`). Confidence: a reviewer can diff the snapshot fields against `SprintSynced.cs` 1:1.

**Confidence: medium** — `create-aggregate` covers the entity scaffold, but the "owned link collection vs separate link entity" shape is an adaptation the developer should settle by reading the skill + the existing `Card`→`EvidenceLink` owned-collection precedent in `Reflekt.Domain/Cards/`. Explore that precedent before implementing.

### Step 3 — Require ≥1 Fokus sprint at retro creation (`CreateRetroSession`, Q3=b)
**Satisfies:** ADR-017/018 (no un-linked retro)
**Skill:** Follow `create-feature` (FastEndpoints slice — feature-specific inputs only) · **TDD:** yes (request validation + 1..N link persistence are endpoint behavior)

Rename the creation slice to `CreateRetroSession*` (per Rename Table) and make a Fokus-sprint reference **mandatory**.

**Feature-specific inputs:**
- `CreateRetroSessionCommand` gains `required IReadOnlyList<int> FokusSprintIds`. Validator: `FokusSprintIds` **NotEmpty** + every id `> 0`. Keep all existing fields (name, column config, timers).
- Route renames to `POST /api/teams/{teamId}/retro-sessions` (Q2; old `/sprints` literal gone). Response is `RetroSessionResponse`.
- Handler: existing SM-ownership check + column-config fallback preserved; pass `FokusSprintIds` into `RetroSession.Create(...)` (Step 2 factory guards ≥1 as defense-in-depth). **Snapshot handling for unknown ids (sub-decision):** **accept ids whose `FokusSprintSnapshot` row does not yet exist** — do NOT 400 on a missing snapshot. The forward-only `SprintSynced` consumer (Step 4) backfills the snapshot on the next Fokus sync; until then the reference exists with a thin/absent snapshot (Q4=a accepts this). Do **not** call Fokus to validate the id (no gRPC client in RA3). This keeps creation decoupled from Fokus availability.
- Update `DbInitializer` seed (Step 1 left it un-linked): give the seed retro a single placeholder `FokusSprintIds: [1]` so the dev board satisfies the ≥1 invariant; note it as a dev-only seed value (no real Fokus sprint 1 guaranteed — same spirit as the RA2 `SeedOwnerPersonId` sentinel).

**Acceptance:**
- `POST /api/teams/{teamId}/retro-sessions` with empty `fokusSprintIds` → 400 (validator). With `[N]` → 201 `RetroSessionResponse`, N link rows persisted.
- A referenced id with no snapshot row still → 201 (no Fokus call, no 400). TDD covers create-with-unknown-id.
- `grep -rn "/api/teams/{teamId}/sprints\"\|CreateSprintCommand\|CreateSprintEndpoint" src/Services/Reflekt` → 0 hits (renamed).

### Step 4 — Consume `SprintSynced` to refresh the snapshot (ADR-019, event-only/forward-only)
**Satisfies:** ADR-019 (integration-event channel)
**Skill:** Build (Reflekt has no MassTransit registration) **then** Follow `add-integration-event` (consumer side) · **TDD:** yes (the consumer's idempotent upsert is testable logic)

Wire Reflekt as a **consumer** of `SprintSynced` (already published by Fokus) and refresh `FokusSprintSnapshot` on each event.

**Build (infra that doesn't exist locally):** Reflekt currently has **no** `AddMassTransit`. The single in-memory bus lives in `Fokus.API/DependencyInjection.cs:36` (`x.AddConsumers(Fokus assembly)`). Two independent `AddMassTransit` calls in one Host container is a MassTransit footgun. **Resolve by registering the Reflekt consumer onto the single shared bus** — NOT a second bus.

**Guardrail-narrowed options (critic MEDIUM #3 — the no-cross-module-reference rule in `CLAUDE.md` rules out putting Reflekt's consumer at the Fokus site):**
- **REJECTED:** adding `x.AddConsumers(typeof(Reflekt…).Assembly)` *inside* `Fokus.API/DependencyInjection.cs`. That requires `Fokus.API` to reference the Reflekt assembly → a **Fokus→Reflekt project reference**, violating the CLAUDE.md "No cross-module project references" guardrail. Do not do this.
- **VALID (preferred):** a Reflekt-owned `AddReflektMessaging` (in `Reflekt.API`) that registers the Reflekt consumer, composed into a **single Host-level `AddMassTransit`**; OR move the bus composition to the **Host** (which already references all modules — `Program.cs:9-10`) and call both modules' consumer-registration there. Either keeps the bus single and each module's consumers in its own assembly. The developer reads `add-integration-event` + the Fokus registration to pick between the two Host-composable shapes.
- **Escalation seam:** if neither Host-composable shape can host a second module's consumers on one bus without a second `AddMassTransit`, flag the architect rather than creating a second bus.

**Feature-specific inputs (consumer):**
- `RefreshFokusSprintSnapshotOnSprintSyncedConsumer : IConsumer<SprintSynced>` in `Reflekt.API/Features/FokusSprints/Consumers/` (mirror the Fokus consumer-folder convention). Maps the 5 `SprintSynced` fields → `FokusSprintSnapshot`, calls `FokusSprintSnapshotRepository.UpsertAsync` (overwrite-on-re-sync per ADR-019). **Idempotent + forward-only:** upsert by `SprintId`; an event for a sprint no retro references is still stored (cheap, harmless) OR skipped — developer's call, document which. No call back to Fokus (ADR-019: event carries all needed data).
- Reference (shape only, not structure): `SeedCompanyDomainOnFirstAdministratorRegisteredConsumer` — `IConsumer<T>`, repository-mutation, idempotent.

**Acceptance:**
- Consuming `SprintSynced(SprintId=42, …)` creates/overwrites the `FokusSprintSnapshot` row with id 42 carrying the event's 5 fields — TDD red→green (new snapshot; then re-consume with changed `State` → row overwritten, not duplicated).
- Solution builds with exactly **one** `AddMassTransit` in the container (`grep -rn "AddMassTransit" src` → 1 hit). Reflekt consumer is discovered/registered.
- No gRPC client added (`grep -rn "ISprintAnalyticsService\|AnonymizedSprintSummary" src/Services/Reflekt` → 0 hits — that's RA4).

### Step 5 — Rename the retro **lifecycle** HTTP routes + JSON shapes (Q2)
**Satisfies:** ADR-018 (visible ownership rename)
**Skill:** None (route-literal + DTO rename; conformance is grep-checkable) · **TDD:** no (no behavior change — same handlers, renamed surface)

Rename only the **lifecycle** routes already in resource-noun form, plus their response records (per Rename Table). The flat `/api/sprints/{sprintId}/*` child family is DO-NOT-TOUCH (sub-decision).

**Exact route renames — ALL 9 team-scoped `/api/teams/{teamId}/sprints…` routes (derived from the plan-time grep `[Http*("…/api/teams/{teamId}/sprints…")]` re-run after the critic CRITICAL; the 7-row count in the prior revision was off by one — `SetReviewStatus` is the 9th endpoint, 8 rows because AdvancePhase+Check share a row):**

| File | Old route | New route |
|------|-----------|-----------|
| `CreateRetroSession/…Endpoint.cs` (Step 3) | `POST /api/teams/{teamId}/sprints` | `POST /api/teams/{teamId}/retro-sessions` |
| `GetTeamSprints/…Endpoint.cs` → `GetTeamRetroSessions/` | `GET /api/teams/{teamId}/sprints` | `GET /api/teams/{teamId}/retro-sessions` |
| `GetSprint/…Endpoint.cs` → `GetRetroSession/` | `GET /api/teams/{teamId}/sprints/{sprintId}` | `GET /api/teams/{teamId}/retro-sessions/{retroSessionId}` |
| `StartSprint/…` → `StartRetroSession/` | `POST /api/teams/{teamId}/sprints/{sprintId}/start` | `POST /api/teams/{teamId}/retro-sessions/{retroSessionId}/start` |
| `CloseSprint/…` → `CloseRetroSession/` | `POST /api/teams/{teamId}/sprints/{sprintId}/close` | `POST /api/teams/{teamId}/retro-sessions/{retroSessionId}/close` |
| `AdvancePhase/…Endpoint.cs` + `AdvancePhaseCheckEndpoint.cs` | `POST /api/teams/{teamId}/sprints/{sprintId}/advance-phase[/check]` | `POST /api/teams/{teamId}/retro-sessions/{retroSessionId}/advance-phase[/check]` |
| `Reports/GetSprintReport/…` | `GET /api/teams/{teamId}/sprints/{sprintId}/report` | `GET /api/teams/{teamId}/retro-sessions/{retroSessionId}/report` |
| `PreviousActions/SetReviewStatus/SetReviewStatusEndpoint.cs` **(critic CRITICAL — the missed 9th route)** | `PATCH /api/teams/{teamId}/sprints/{sprintId}/previous-actions/{cardId}` | `PATCH /api/teams/{teamId}/retro-sessions/{retroSessionId}/previous-actions/{cardId}` |

**`SetReviewStatus` — binding pins (critic CRITICAL #1):** this is a **team-scoped** route (renames with the lifecycle group), NOT one of the 24 flat `/api/sprints/{sprintId}` routes. Two constraints:
- **Command rebind:** `SetReviewStatusCommand(int TeamId, int SprintId, int CardId, string ReviewStatus)` → `SetReviewStatusCommand(int TeamId, int RetroSessionId, int CardId, string ReviewStatus)`. Route param `{sprintId}`→`{retroSessionId}`; bound property renames with it. Update the in-handler reads at `SetReviewStatusEndpoint.cs:49` (`sprintRepository.GetByIdAsync(command.SprintId…)`) and `:50` (`sprint.TeamId != command.TeamId`) to `command.RetroSessionId`.
- **CRITICAL — `Card.ReviewedInSprintId` STAYS `SprintId`-named (Q1=b DO-NOT-TOUCH, see HIGH #2 below).** At `SetReviewStatusEndpoint.cs:75`, `card.SetReviewStatus(reviewStatus, command.SprintId)` feeds the command value into the **kept** `Card.ReviewedInSprintId` FK column. After the rebind this becomes `card.SetReviewStatus(reviewStatus, command.RetroSessionId)` — the **request-surface parameter** renames, but the **`Card.SetReviewStatus(…, int sprintId)` domain-method signature, the `Card.ReviewedInSprintId` property, and the FK column do NOT rename** (they stay `SprintId`-style, deferred to RA5). The value's *semantics* (which Fokus-linked retro session a previous-action card was reviewed in) are unchanged — this is a renamed route feeding a kept FK. **Do NOT** rename `SetReviewStatus`'s `int sprintId` parameter to `retroSessionId` — it lands in `ReviewedInSprintId`.

**Binding carve-out (DO-NOT-TOUCH — these flat routes stay on `/api/sprints/{sprintId}`):** every route from the plan-time grep that begins `/api/sprints/{sprintId}/` — i.e. cards, card-groups, votes, participants (incl. `JoinSprint` `POST /api/sprints/{sprintId}/join`, `KickParticipant`), evidence-links, previous-actions, `GetSprintSummary` (`/api/sprints/{sprintId}/summary`), `UpdateColumns` (`/api/sprints/{sprintId}/columns`), `ClearFocus`/`FocusGroup`/etc. The route param stays `{sprintId}` (= the FK column kept by Q1=b). **24 flat routes — none change.**

**Note on `RetroSessionRouteRequest`:** the lifecycle endpoints that took `SprintRouteRequest(int TeamId, int SprintId)` now bind `RetroSessionRouteRequest(int TeamId, int RetroSessionId)` — the route param renames from `{sprintId}` to `{retroSessionId}` **only on the 6 lifecycle routes above** (they are team-scoped, not the FK-keyed flat family). FastEndpoints auto-binds the renamed param to the matching property.

**Acceptance:**
- `grep -rn "\"/api/teams/{teamId}/sprints" src/Services/Reflekt` → **0 hits** (all **9** team-scoped lifecycle routes renamed — incl. `SetReviewStatus`). This grep already covered the missed 9th route; the gap was the rename *table*, now complete.
- `grep -rn "\"/api/sprints/{sprintId}/" src/Services/Reflekt | wc -l` → **24** (flat family unchanged — `SetReviewStatus` is NOT in this set; it is team-scoped).
- `grep -rn "command.SprintId" src/Services/Reflekt/Reflekt.API/Features/PreviousActions` → **0 hits** (rebound to `command.RetroSessionId`); `grep -rn "ReviewedInSprintId" src/Services/Reflekt` → **unchanged** (the `Card` property + FK column stay — see HIGH #2). Confirms the route/command rebind did NOT bleed into the kept FK.
- `grep -rn "SprintResponse\|SprintSummaryResponse\|SprintRouteRequest\|SprintSummaryParticipant" src/Services/Reflekt` → 0 hits (renamed per table). `GetSprintSummary` *endpoint folder/route* stays (flat family) but its **response type** is `RetroSessionSummaryResponse` (renamed) — call this out so the kept route + renamed response isn't flagged inconsistent.
- `dotnet build SprintRituals.slnx` green.

### Step 6 — EF migration (rename `Sprints`→`RetroSessions` + new snapshot/link tables)
**Satisfies:** ADR-018/019 (schema)
**Skill:** Follow `persistence-patterns` (migration mechanism) · **TDD:** no (migration — verified by apply+introspect, not unit test)

Scaffold one migration for: table rename `Sprints` → `RetroSessions`; new `FokusSprintSnapshots` table (PK `SprintId`); new link table for the 1..N `FokusSprintRef`s.

**Binding constraints:**
- **Child FK columns stay `SprintId`** (Q1=b) — the rename of the parent table must **not** rename `Participants.SprintId`, `Cards.SprintId`, `CardGroups.SprintId`, **`Cards.ReviewedInSprintId`** (the 4th FK — critic HIGH #2), or the `{SprintId, PersonId}` filtered unique index. EF may try to rename the FK relationship when the principal type renames — verify the generated `Up()` renames the **table** and the FK *constraint names* only, never the child *column* names. (RA2 lesson: a parent rename can cascade column renames into children — read the generated `Up()` and assert.) The four FK columns + their indexes (`IX_Cards_SprintId`, `IX_CardGroups_SprintId`, `IX_Cards_ReviewedInSprintId`, `IX_Participants_SprintId`, the `{SprintId,PersonId}` filtered unique) all survive byte-identical.
- SQLite renames = table rebuilds; the `ColumnColor` `HasConversion` + defaults + filtered indexes on the retro table must survive — verify post-apply.
- Migration name: `RenameSprintToRetroSessionAddFokusLink` (developer may adjust).
- **Q5 stance:** `Down()` need not be data-reversible; schema-reversible is enough. Document in implementation.md.

**Acceptance (apply + introspect a throwaway DB — RA2's proven method):**
- All Reflekt migrations apply clean to a temp `Data Source`; `sqlite_master`/`PRAGMA` shows: table `RetroSessions` exists, `Sprints` gone; `FokusSprintSnapshots` exists with PK `SprintId`; link table exists; `Participants.SprintId`/`Cards.SprintId`/`CardGroups.SprintId`/**`Cards.ReviewedInSprintId`** columns **unchanged** (HIGH #2 — the 4th FK column must NOT become `ReviewedInRetroSessionId`); the `{SprintId,PersonId}` filtered unique index + `IX_Cards_ReviewedInSprintId` intact.
- `dotnet ef migrations list` shows the new migration after `InitialReflektSchema` + RA2's `RemoveReflektUserUnifyIdentity`.
- Scaffold → **build** → `database update --no-build` (RA2 lesson: scaffold needs a rebuild before apply or you get a spurious `PendingModelChangesWarning`).

### Step 7 — Update the Reflekt module CLAUDE.md status + KB/ADR cross-references
**Satisfies:** ADR-018/019 (doc currency)
**Skill:** None (doc edit) · **TDD:** no

Update `src/Services/Reflekt/CLAUDE.md` to reflect RA3 landed: the "Status — in-flight port" RetroSession + Fokus-linkage bullets move from future to present; the domain-model section already names `RetroSession`/`FokusSprintSnapshot` (written ahead) — confirm it now matches code. Note the **carry-overs** explicitly in the module doc and/or a one-line backlog touch is the team lead's (do not edit `docs/backlog.md` yourself — owner-managed per RA2 close note).

**Carry-overs to RA4/RA5 (record in implementation.md + module CLAUDE.md):**
- **RA4:** `FokusSprintSnapshot` is forward-only/thin — a referenced sprint never re-synced after RA3 shows an absent/stale snapshot until RA4's gRPC `ISprintAnalyticsService` pull lands. Rich metrics (`AnonymizedSprintSummary`) are entirely RA4.
- **RA5:** the FK columns `SprintId`, the 24 flat `/api/sprints/{sprintId}/*` routes, and the SignalR `JoinSprint`/`"sprint:{id}"`/`"sprintId"` wire layer are deliberately un-renamed (Q1=b, Q2) — RA5 owns them with the frontend merge.

**Acceptance:** `src/Services/Reflekt/CLAUDE.md` describes `RetroSession`/`FokusSprintSnapshot`/`SprintSynced`-consumer as present (not "arrives in later passes"); carry-overs documented.

---

## Skill Mapping

| Step | Disposition | Skill | TDD | Notes / Gaps |
|------|-------------|-------|-----|--------------|
| 1 — Rename aggregate + retro-state types | None | — | no | Mechanical project-wide rename; no skill teaches it. Conformance = rename table + greps. |
| 2 — Fokus-sprint ref + snapshot | Follow | `create-aggregate` + `persistence-patterns` | yes | Owned-link-collection vs separate-entity is an adaptation — explore `Card`→`EvidenceLink` precedent. |
| 3 — Require ≥1 Fokus sprint at create | Follow | `create-feature` | yes | Feature-specific: required `FokusSprintIds`, accept-unknown-id rule. |
| 4 — Consume `SprintSynced` | Build then Follow | `add-integration-event` (consumer) | yes | **Build:** Reflekt has no MassTransit reg — resolve onto the single shared bus, not a 2nd bus. Flag if shared-bus can't host a 2nd module's consumers. |
| 5 — Rename lifecycle routes + JSON | None | — | no | 6 route renames (table); 24 flat routes DO-NOT-TOUCH. Conformance = greps with exact counts. |
| 6 — EF migration | Follow | `persistence-patterns` | no | Verify child `SprintId` columns NOT renamed; apply+introspect throwaway DB. |
| 7 — Doc currency + carry-overs | None | — | no | Module CLAUDE.md; backlog left to owner/team-lead. |

**Skill-gap note for the critic/learner:** Step 4's `add-integration-event` skill is publisher-oriented in its examples; its **consumer-side + "second module joins an existing single in-memory bus"** coverage should be verified at implement time. If the skill doesn't cover the shared-bus registration topology, that's a skill gap to log (the Build half of Step 4 exists precisely because the local infra/skill coverage is thin here).

---

## Risks & ordering

- **Step order is dependency-driven:** 1 (rename) → 2 (domain/persistence additions on the renamed type) → 3 (create requires the Step-2 link) → 4 (consumer refreshes the Step-2 snapshot) → 5 (route rename; independent of 2-4 but after 1) → 6 (migration captures 1+2's model) → 7 (docs). Steps 1 and 5 are both renames but split deliberately (domain vs HTTP surface) for bisectability; 6 must come after the model is final (1+2+3).
- **Biggest risk = the EF parent-rename cascading into child `SprintId` columns** (Step 6). The acceptance pins this with a throwaway-DB introspection, not a `Up()` read alone.
- **Second risk = the MassTransit shared-bus topology** (Step 4 Build half). Pre-sanctioned escalation: if a second module's consumers cannot register onto the single bus without a second `AddMassTransit`, the developer flags the architect rather than creating a second bus.
- **No operator-owed fallback, no prompt-only LLM obligation, no live-credential step** in this plan — none of those rule-classes apply (pure in-process refactor + in-memory event).

---

## Out of scope (explicit)

- gRPC `ISprintAnalyticsService` client + `AnonymizedSprintSummary` rich metrics + the sprint-context panel → **RA4** (`adhoc-SprintContextPanel`).
- Renaming child FK columns `SprintId`→`RetroSessionId`, the 24 flat `/api/sprints/{sprintId}/*` routes, the SignalR `JoinSprint`/`"sprint:{id}"`/claims → **RA5** (`adhoc-ReflektFrontendMerge`), with the frontend.
- Any frontend change — the Reflekt SPA is not in this repo yet (RA5).
- SM waiting room / approval gate → **RW** (`adhoc-RetroWaitingRoom`).
- `Reflekt → Fokus` events → deferred (ADR-019, no concrete metric yet).

---

## Plan Review (critic, code-grounded — REJECT → all findings folded)

Critic ran Mode 2 against the ADR register (no spec); full findings persisted to `review-critic.md`. All four re-verified against code by the architect before folding (the critic is advisory):

| # | Sev | Finding | Resolution (folded into) |
|---|-----|---------|--------------------------|
| 1 | CRITICAL | `SetReviewStatusEndpoint.cs:28` is a 9th team-scoped `/sprints` route, absent from the Step-5 table → its acceptance grep was unsatisfiable. And `:75` `command.SprintId` flows into the kept `Card.ReviewedInSprintId` FK. | **Verified** (`:28`, `:75`). Added the 9th route to the Step-5 table; added the command rebind + the CRITICAL `ReviewedInSprintId`-stays pin; fixed the count framing (9 endpoints / 8 rows) + acceptance greps. |
| 2 | HIGH | `Card.ReviewedInSprintId` is a 4th FK to `Sprints` (`CardConfiguration.cs:32-33`), missing from DO-NOT-TOUCH + Step-6 introspection → EF principal-rename could cascade-rename it. | **Verified** (`:32-33`). Added to DO-NOT-TOUCH (column + `IX_Cards_ReviewedInSprintId` + constraint) and to the Step-6 binding constraint + acceptance (assert column unchanged; constraint-name-follows-table is OK). |
| 3 | MEDIUM | Step-4 option (a) (`AddConsumers(Reflekt)` at the Fokus site) would create a Fokus→Reflekt project reference, violating the no-cross-module-ref guardrail. | **Verified** (guardrail in `CLAUDE.md`). Narrowed Step 4: option (a) REJECTED; valid placements are Host-composed or a Reflekt-owned `AddReflektMessaging` into a single bus. |
| 4 | LOW | Confirm `Report*` types (`ReportResponse`, `ReportSprintInfo`) are intentionally out of the rename set. | **Verified** (`ReportContracts.cs`). Added an explicit carve-out line — retained as-is in RA3, deferred to RA5. |

**Disposition:** all four folded; no finding required reopening a user-confirmed answer (each is a completeness/consistency fix *within* the locked Q1/Q2 scope — #1's "rename now" is consistent with Q2's "rename team-scoped routes freely," and the user directed it explicitly). Re-grounded the touched steps (5, 6, 4) against code per the revision rule. Plan auto-approved (critic gate run, no open user questions).
