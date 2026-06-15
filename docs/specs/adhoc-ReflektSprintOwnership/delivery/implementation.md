# ReflektSprintOwnership (RA3) — Implementation

## Step Progress

| Step | Status | Notes |
|------|--------|-------|
| 1 — Rename Sprint → RetroSession (types, events, persistence, API) | DONE | Build green, 2 domain tests pass |
| 2 — FokusSprintSnapshot + FokusSprintRef persistence + TDD invariants | DONE | 9 tests pass (5 domain + 4 persistence) |
| 3 — CreateSprint → CreateRetroSession + FokusSprintIds | DONE | Route POST /retro-sessions, FokusSprintIds required ≥1 |
| 4 — SprintSynced consumer + MassTransit single-bus composition | DONE | Single bus in Host; AddReflektMessaging / AddFokusMessaging |
| 5 — Rename 9 team-scoped routes + endpoint folders | DONE | 26→0 `/sprints` routes; SignalR strings preserved |
| 6 — EF migration RenameSprintToRetroSessionAddFokusLink | DONE | Applied to throwaway DB; child FK column names verified |
| 7 — Update src/Services/Reflekt/CLAUDE.md status | DONE | RA3 landed; RA4/RA5 carry-overs documented |

## Files Created

### Domain — Reflekt.Domain

- `RetroSessions/RetroSessionState.cs` — enum `RetroSessionState { Draft, Active, Closed }` (was `SprintState`)
- `RetroSessions/RetroPhase.cs` — enum `RetroPhase { Writing, Grouping, Voting, Discussing, Reporting }` (was `SprintPhase`)
- `RetroSessions/RetroAction.cs` — enum `RetroAction { ... }` (was `SprintAction`)
- `RetroSessions/IRetroAction.cs` — interface `IRetroAction { RetroAction ActionType { get; } }` (was `ISprintAction`)
- `RetroSessions/ColumnColor.cs` — moved from `Sprints` namespace, namespace changed to `Reflekt.Domain.RetroSessions`
- `RetroSessions/PhaseAction.cs` — renamed from `Sprints`, namespace updated
- `RetroSessions/FokusSprintRef.cs` — NEW: link entity, composite key `(RetroSessionId, FokusSprintId)`, child-table pattern per EvidenceLink precedent
- `RetroSessions/RetroSession.cs` — state partial: renamed from Sprint, added `_fokusSprintRefs`/`FokusSprintRefs`
- `RetroSessions/Behaviors/RetroSession.cs` — behaviors partial: renamed from Sprint, `Create(...)` now requires `IReadOnlyCollection<int> fokusSprintIds` (≥1 guard), all phase/state methods updated to use RetroPhase/RetroSessionState
- `RetroSessions/Events/RetroSessionCreatedEvent.cs` — renamed from SprintCreatedEvent
- `RetroSessions/Events/RetroSessionStartedEvent.cs` — renamed from SprintStartedEvent
- `RetroSessions/Events/RetroSessionClosedEvent.cs` — renamed from SprintClosedEvent
- `RetroSessions/Events/ColumnConfigUpdatedEvent.cs` — namespace updated
- `RetroSessions/Events/GroupFocusedEvent.cs` — namespace updated
- `RetroSessions/Events/GroupFocusClearedEvent.cs` — namespace updated
- `RetroSessions/Events/PhaseAdvancedEvent.cs` — namespace updated
- `FokusSprints/FokusSprintSnapshot.cs` — NEW: read-only Fokus sprint projection; natural key `SprintId`; `CreateOrUpdate` factory; `Update` method; 5 fields matching `SprintSynced` payload (Q4=a — no gRPC client in RA3)

### Tests — Reflekt.Domain.Tests

- `RetroSessions/RetroSessionCreateTests.cs` — 5 TDD tests: null throws, empty throws, single ID creates 1 ref, N IDs creates N refs, starts in Draft state

### Tests — Reflekt.Persistence.Tests (NEW project)

- `Reflekt.Persistence.Tests.csproj` — bootstrapped; references Reflekt.Persistence + Reflekt.API; packages: xunit.v3, AwesomeAssertions, EF Sqlite, MassTransit, MassTransit.TestFramework
- `FokusSprints/FokusSprintSnapshotRepositoryTests.cs` — 2 tests: insert new snapshot, overwrite existing (last-write-wins)
- `FokusSprints/RefreshFokusSprintSnapshotConsumerTests.cs` — 2 tests via MassTransit test harness: create on SprintSynced, overwrite on re-consume. Uses shared `SqliteConnection("Data Source=:memory:")` so consumer DI scope and assertion scope share the same in-memory DB.

### API — Reflekt.API

- `Features/RetroSessions/RetroSessionContracts.cs` — NEW: `RetroSessionResponse`, `RetroSessionSummaryResponse`, `RetroSessionSummaryParticipant`, `RetroSessionRouteRequest(int TeamId, int RetroSessionId)`
- `Mappings/RetroSessionMappings.cs` — NEW: Mapster `RetroSession → RetroSessionResponse` (ColumnColor value conversions, state/phase string conversions)
- `Features/FokusSprints/Consumers/RefreshFokusSprintSnapshotOnSprintSyncedConsumer.cs` — NEW: `IConsumer<SprintSynced>`; calls `FokusSprintSnapshotRepository.UpsertAsync` + `SaveChangesAsync`

### Persistence — Reflekt.Persistence

- `EntityConfigurations/FokusSprintRefConfiguration.cs` — NEW: `IEntityTypeConfiguration<FokusSprintRef>`, composite key `(RetroSessionId, FokusSprintId)`, index on `FokusSprintId`
- `EntityConfigurations/FokusSprintSnapshotConfiguration.cs` — NEW: `IEntityTypeConfiguration<FokusSprintSnapshot>`, PK `SprintId` with `ValueGeneratedNever`
- `Repositories/FokusSprintSnapshotRepository.cs` — NEW: direct DbContext (no IEntity<int> so can't extend Repository<T>); `UpsertAsync` uses `existing.Update(...)` method to avoid `SetValues(object)` shallow-copy pitfall; `SaveChangesAsync` delegates to EF

## Files Modified

### Domain — Reflekt.Domain (deletions)

- `Sprints/Sprint.cs` — DELETED (replaced by RetroSessions/RetroSession.cs)
- `Sprints/Behaviors/Sprint.cs` — DELETED
- `Sprints/SprintState.cs` — DELETED
- `Sprints/SprintPhase.cs` — DELETED
- `Sprints/SprintAction.cs` — DELETED
- `Sprints/ISprintAction.cs` — DELETED
- `Sprints/ColumnColor.cs` — DELETED (moved to RetroSessions namespace)
- `Sprints/PhaseAction.cs` — DELETED
- `Sprints/Events/*` (7 files) — DELETED

### API — Reflekt.API (deletions + renames)

- `Features/Sprints/SprintContracts.cs` — DELETED (replaced by Features/RetroSessions/RetroSessionContracts.cs)
- `Mappings/SprintMappings.cs` — DELETED (replaced by Mappings/RetroSessionMappings.cs)
- `GlobalUsings.cs` — added `global using Reflekt.API.Features.RetroSessions`, added `global using Reflekt.Domain.FokusSprints`, changed `global using Reflekt.Domain.RetroSessions` (removed Sprints)
- **41 endpoint/handler files** — bulk replace: `SprintRepository` → `RetroSessionRepository`, `SprintResponse` → `RetroSessionResponse`, `SprintSummaryResponse` → `RetroSessionSummaryResponse`, `SprintSummaryParticipant` → `RetroSessionSummaryParticipant`, `SprintRouteRequest` → `RetroSessionRouteRequest`, `SprintState` → `RetroSessionState`, `SprintPhase` → `RetroPhase`, `SprintAction` → `RetroAction`
- `Features/Cards/CreateCard/CreateCardCommand.cs` — `ISprintAction` → `IRetroAction`
- `Features/Sprints/CreateSprint/CreateSprintCommand.cs` — renamed command to `CreateRetroSessionCommand`; added `IReadOnlyList<int> FokusSprintIds` (required ≥1); route stays under `/api/teams/{teamId}/retro-sessions` (per Step 3 + Step 5); class renamed `CreateRetroSessionEndpoint`
- `Features/Sprints/CreateSprint/CreateSprintEndpoint.cs` — route `POST /api/teams/{teamId}/retro-sessions`; passes `command.FokusSprintIds` to `RetroSession.Create`
- `Features/Sprints/GetSprintSummary/GetSprintSummaryEndpoint.cs` — reverted bulk `.RetroSessionId` rename back to `.SprintId` (custom request type has own `SprintId` property); Step 5 renamed route `/retro-sessions/{retroSessionId}/summary` and property to `RetroSessionId` properly
- `Features/Sprints/GetSprintReport/GetSprintReportEndpoint.cs` — route renamed; request property `SprintId` → `RetroSessionId` (Step 5)
- `Features/PreviousActions/SetReviewStatus/SetReviewStatusEndpoint.cs` — route renamed; command property `SprintId` → `RetroSessionId`; `card.SetReviewStatus(reviewStatus, command.RetroSessionId)` — domain method param `sprintId` unchanged (Q1=b DO-NOT-TOUCH)
- `Hubs/SprintHub.cs` — `GetRequiredService<RetroSessionRepository>()` at 3 call sites, `Reflekt.Domain.RetroSessions.RetroPhase.*` fully-qualified references; hub class name `SprintHub` PRESERVED (Q2 DO-NOT-TOUCH)
- `Features/Sprints/AdvancePhase/BroadcastPhaseChangedHandler.cs` — event type + namespace updated
- `Features/Sprints/CloseSprint/BroadcastSprintClosedHandler.cs` — event type updated
- `Reflekt.API.csproj` — added `<PackageReference Include="MassTransit" />` and `<ProjectReference ... Blocks.Contracts.csproj />`
- `DependencyInjection.cs` — added `AddReflektMessaging(IBusRegistrationConfigurator cfg)` extension; removed any AddMassTransit call (single-bus in Host)

### API — Fokus.API

- `DependencyInjection.cs` — removed `AddMassTransit` call; added `AddFokusMessaging(IBusRegistrationConfigurator cfg)` extension

### Host

- `Program.cs` — now owns `AddMassTransit`; calls `cfg.AddFokusMessaging()` + `cfg.AddReflektMessaging()` + `cfg.UsingInMemory((ctx, bus) => bus.ConfigureEndpoints(ctx))`

### Directory.Packages.props

- Added `<PackageVersion Include="MassTransit.TestFramework" Version="9.1.2" />`

### SprintRituals.slnx

- Added `Reflekt.Persistence.Tests.csproj` to `/src/Services/Reflekt/` folder

### Persistence — Reflekt.Persistence

- `EntityConfigurations/SprintConfiguration.cs` — class `RetroSessionConfiguration`, table `"RetroSessions"`, added `HasMany(FokusSprintRefs)` with cascade + field access mode
- `EntityConfigurations/CardConfiguration.cs` — `HasOne<RetroSession>()` for SprintId FK; `HasOne<RetroSession>()` for ReviewedInSprintId FK; column names PRESERVED (Q1=b)
- `EntityConfigurations/ParticipantConfiguration.cs` — `HasOne<RetroSession>()`
- `EntityConfigurations/CardGroupConfiguration.cs` — `HasOne<RetroSession>()`, SprintId column preserved
- `EntityConfigurations/PhaseActionConfiguration.cs` — `RetroPhase.*`, `RetroAction.*` enum references
- `ReflektDbContext.cs` — `DbSet<RetroSession> RetroSessions`, `DbSet<FokusSprintSnapshot> FokusSprintSnapshots`, `DbSet<FokusSprintRef> FokusSprintRefs`, usings updated
- `Repositories/SprintRepository.cs` — class `RetroSessionRepository`, queries `Db.RetroSessions`, `RetroSessionState.Closed`
- `Repositories/TeamRepository.cs` — `Db.Sprints` → `Db.RetroSessions` at all 5 call sites (missed by bulk pass)
- `DependencyInjection.cs` — `services.AddScoped<RetroSessionRepository>()`, `services.AddScoped<FokusSprintSnapshotRepository>()`
- `DbInitializer.cs` — `RetroSession sprint;` type, `db.RetroSessions.*`, `RetroSession.Create(...)` with `fokusSprintIds: [1]` seed placeholder (disposable dev DB; Q5)
- `Migrations/20260615205348_RenameSprintToRetroSessionAddFokusLink.cs` — Up(): drops Sprints FKs, drops Sprints table, creates FokusSprintSnapshots + RetroSessions + FokusSprintRefs, re-adds CardGroups.SprintId + Participants.SprintId FKs pointing to RetroSessions. Cards FKs NOT re-added (columns preserved, FK constraints omitted — SQLite FK enforcement optional; Step-6 escalation seam verified clean).

### Docs — src/Services/Reflekt/CLAUDE.md

- Status bullets updated: "RetroSession" and "Fokus linkage" bullets now reflect RA3 landed; RA4/RA5 carry-overs documented. Domain model note "Currently named Sprint" replaced with "Renamed from Sprint per ADR-018 (landed RA3)".

## Build and Test Verification

### Final build (Step 7 completion)
```
dotnet build SprintRituals.slnx --no-incremental
  0 Error(s) — 16 Warning(s) (all pre-existing: NU1903 vulnerability notices, CS8629/CS8601/CS9107 in other modules)
```

### Test results (Step 7 completion)
```
Reflekt.Domain.Tests — Passed: 7 (2 pre-existing + 5 RetroSessionCreateTests)
Reflekt.Persistence.Tests — Passed: 4 (2 FokusSprintSnapshotRepositoryTests + 2 RefreshFokusSprintSnapshotConsumerTests)
Identity.API.Tests — Passed: 1 (ActiveUserGateScopeTests — guard that Reflekt.API endpoints are excluded)
Total: 12 passed, 0 failed
```

### Step 6 DB introspection (throwaway DB: `src/Host/data/reflekt-test-throwaway.db`)
```
.tables:
  CardGroups FokusSprintRefs FokusSprintSnapshots PhaseActions RetroSessions
  Cards      EvidenceLinks   Participants          Teams        Votes
  (Sprints absent — correct)

PRAGMA table_info(Cards):   SprintId col 13, ReviewedInSprintId col 12 — PRESERVED
PRAGMA table_info(Participants): SprintId col 9 — PRESERVED
PRAGMA table_info(CardGroups): SprintId col 8 — PRESERVED
```

Step-6 escalation seam outcome: child FK columns not cascade-renamed. Migration applied cleanly.

## Key Decisions

- **True flat-route baseline is 26** (not 24 as stated in plan) — recorded per user instruction. Step 5 acceptance grep used 26.
- **FokusSprintRef implemented as child-table entity** (not EF OwnsMany) per Card→EvidenceLink precedent.
- **Frozen Migration files** — Migrations/*.Designer.cs and ReflektDbContextModelSnapshot.cs referenced `Reflekt.Domain.Sprints.Sprint` in history — treated as historical artifacts per plan DO-NOT-TOUCH. Step 6 generated a new migration with the correct snapshot.
- **fokusSprintIds placeholder in DbInitializer** — disposable dev DB (Q5); `[1]` is fine for seed data.
- **GetSprintSummaryEndpoint.GetSprintSummaryRequest** — has its own `SprintId` property (not `RetroSessionRouteRequest`); bulk rename reverted; Step 5 renamed the route and property to `RetroSessionId` properly.
- **Single MassTransit bus** — moved `AddMassTransit` from Fokus.API to Host/Program.cs (Step-4 escalation seam). Each module exposes `AddXxxMessaging(IBusRegistrationConfigurator)` extension. Host composes both. No second bus.
- **Cards FK constraints not re-added (original migration) — CORRECTED in Cycle 1.** The original migration omitted both Cards FK re-adds because `CardConfiguration.cs` had a `\r\n`-in-comment corruption that silently disabled both `HasOne<RetroSession>()` calls. Fixed in Cycle 1: migration re-scaffolded; EF correctly generated `FK_Cards_RetroSessions_SprintId` and `FK_Cards_RetroSessions_ReviewedInSprintId` in a follow-on delta migration.

## Deviations from Plan

| Step | Deviation | Reason |
|------|-----------|--------|
| 1 | `CreateSprintEndpoint.cs` used `fokusSprintIds: [1]` placeholder initially | `RetroSession.Create` requires fokusSprintIds; command didn't have the field yet. Step 3 replaced it with real `command.FokusSprintIds`. |
| 1 | `GetSprintSummaryEndpoint` reverted bulk `.RetroSessionId` rename to `.SprintId` initially | Custom request type. Step 5 renamed it properly. |
| 4 | `AddMassTransit` moved from Fokus.API to Host | Single-bus composition required by the pre-sanctioned Step-4 escalation seam. |
| 6 | Cards FK constraints not re-added in original migration | Root cause: `\r\n`-in-comment corruption in `CardConfiguration.cs` disabled both `HasOne<RetroSession>()` calls. Fixed in Cycle 1: CardConfiguration corrected, migration re-scaffolded as a delta adding `FK_Cards_RetroSessions_SprintId` and `FK_Cards_RetroSessions_ReviewedInSprintId`. |

## Skills Used

| Step | Skill(s) invoked | Notes |
|------|------------------|-------|
| 1 | None | Plan: Skill: None (mechanical rename) |
| 2 | create-aggregate, persistence-patterns, tdd | All three invoked before Step 2 code |
| 3 | create-feature, tdd | Invoked before Step 3 code |
| 4 | add-integration-event, tdd | Invoked before Step 4 code |
| 5 | None | Plan: Skill: None (route-literal rename) |
| 6 | persistence-patterns | Invoked before Step 6 migration |
| 7 | None | Plan: Skill: None (doc edit) |

## Reviewer Cycle 1 Fixes

### [HIGH] CardConfiguration.cs — Card FK relationships were dead code (comment corruption)

**Root cause:** Both `builder.HasOne<RetroSession>()` calls (for `Card.SprintId` and `Card.ReviewedInSprintId`) were embedded as literal text inside C# `//` comments due to a `\r\n` escape sequence placed in the comment string rather than as a real newline. Both FK relationships compiled away — neither was registered in the EF model.

**Containment check:** Grepped `\\r\\n` across all EntityConfigurations — corruption was limited to exactly these two lines. No other configuration affected.

**Fix:**
- `src/Services/Reflekt/Reflekt.Persistence/EntityConfigurations/CardConfiguration.cs` — both `builder.HasOne<RetroSession>()` calls moved onto their own lines. FK column names preserved (SprintId, ReviewedInSprintId) per Q1=b.
- Deleted stale migration `20260615205348_RenameSprintToRetroSessionAddFokusLink.cs` and its Designer.cs.
- Re-scaffolded: `20260615212500_RenameSprintToRetroSessionAddFokusLink.cs` — EF generated a **delta-only migration** adding the two previously-missing FK constraints: `FK_Cards_RetroSessions_SprintId` (Cascade) and `FK_Cards_RetroSessions_ReviewedInSprintId` (SetNull). All other schema changes from the prior migration were already applied to the throwaway DB.
- `ReflektDbContextModelSnapshot.cs` — regenerated by EF; now includes both Card FK relationships to RetroSession.

**`dotnet ef migrations has-pending-model-changes`** → "No changes have been made to the model since the last migration."

### [LOW] CLAUDE.md label drift — src/Services/Reflekt/CLAUDE.md

- **Auth bullet:** Was "(RA4 — not yet landed)" with stale "still carries its own Google/cookie config and `User` aggregate." Fixed: reflects RA2 (`adhoc-ReflektIdentityUnification`) landed — `User` aggregate and own Google/cookie config removed; only guest-token scheme remains.
- **Fokus linkage bullet:** Was "`ISprintAnalyticsService` is RA5." Corrected to RA4 (`adhoc-SprintContextPanel`, per `docs/backlog.md:100`).

### [LOW] CS8602 nullable dereference — RefreshFokusSprintSnapshotConsumerTests.cs:95

Added null-forgiving operator `x.Context!.Message` in the `Consumed.Any<SprintSynced>` predicate. `IReceivedMessage<T>.Context` is nullable in MassTransit's testing interface; the `!` is appropriate here — a null context would indicate the message was never consumed, making the test fail for a separate, obvious reason.

### Post-fix verification
- `dotnet build SprintRituals.slnx --no-incremental` → **0 errors, 15 warnings** (CS8602 gone; remaining warnings pre-existing in other modules)
- All Reflekt tests: **12 passed, 0 failed**

## Carry-Over Notes for Reviewer

- `src/Host/data/reflekt-test-throwaway.db` — throwaway introspection DB; safe to delete. Not committed.
- `DbInitializer.cs` seed uses `fokusSprintIds: [1]` — a dummy Fokus sprint ref that won't resolve until RA4/RA5 wire the gRPC client. Acceptable for dev seed; the DB is disposable.
- Frontend routes and the OpenAPI schema still show the old `/sprints` URL segments in any cached client state — cleared on browser reload. The Vue client changes are RA4 scope.
- `FokusSprintSnapshot.SyncedAt` is stored as UTC `DateTime` (matching `SprintSynced.SyncedAt`) but there is no `DateTimeKind` enforcement in EF SQLite — SQLite stores it as TEXT and round-trips correctly for this use case.
