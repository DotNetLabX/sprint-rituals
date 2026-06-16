# adhoc-SprintContextPanel — Implementation

## Step 1 decision: test home for `SprintAnalyticsGrpcService`

The new RA4 fields are in `Fokus.API/Grpc/SprintAnalyticsGrpcService.cs`. No `Fokus.API.Tests` project existed; the correct home is a new project mirroring the `Reflekt.Persistence.Tests` pattern (real SQLite in-memory + real repositories + real pure-domain analyzer instances — no mocking). Created `src/Services/Fokus/Fokus.API.Tests/`.

Approach: test through `GetSprintSummaryAsync` (the only public entry point) rather than the private `MapToAnonymizedSummary`. All 9 constructor parameters are pure domain logic except the 5 repositories; real instances are used throughout.

## Files changed

### Step 1 — gRPC contract extension + Fokus mapping + tests

| File | Change |
|------|--------|
| `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/AnonymizedSprintSummary.cs` | 7 new `[DataMember]` fields (Orders 9–15): `SprintId`, `SprintName`, `StartDate`, `EndDate`, `CompletionRag`, `DisruptionRag`, `CarryOverRag`. Additive — existing Orders 1–8 unchanged. |
| `src/Services/Fokus/Fokus.API/Grpc/SprintAnalyticsGrpcService.cs` | Added mapping of new fields in `MapToAnonymizedSummary`. `DisruptionRag` mapped from `health?.DisruptionRag` — authoritative analyzer band (bands on combined `DisruptionRate` = scope + bug), never recomputed here (Rule 3). |
| `src/Services/Fokus/Fokus.API.Tests/Fokus.API.Tests.csproj` | New project. References `Fokus.API` + `Fokus.Persistence`. |
| `src/Services/Fokus/Fokus.API.Tests/_Fixtures/TestData.cs` | Static factories: `ClosedSprint`, `FeatureTicket`, `CommittedMembership`, `DoneTransition`. |
| `src/Services/Fokus/Fokus.API.Tests/Grpc/SprintAnalyticsGrpcServiceTests.cs` | 3 tests: sprint identity fields, per-rate RAG fields, green-path health band. All passed (3/3). |
| `SprintRituals.slnx` | Added `Fokus.API.Tests` to `/Services/Fokus/` folder. |

### Step 2 — Reflekt gRPC client registration

| File | Change |
|------|--------|
| `src/Services/Reflekt/Reflekt.API/DependencyInjection.cs` | Added `AddCodeFirstGrpcClient<ISprintAnalyticsService>(grpcOptions, "SprintAnalytics")`. Config key: `Grpc:Services:SprintAnalytics:Url`. Mirrors the `IPersonService` registration in `Fokus.API`. |
| `src/Host/appsettings.json` | Added `"SprintAnalytics": { "Url": "http://localhost:5001" }` to `Grpc:Services`. |

### Step 3 — `GetWithFokusSprintsAsync`

| File | Change |
|------|--------|
| `src/Services/Reflekt/Reflekt.Persistence/Repositories/SprintRepository.cs` | Added `GetWithFokusSprintsAsync(int retroSessionId, CancellationToken ct)` to `RetroSessionRepository` — eager-loads `FokusSprintRefs` via `Include`. |

### Step 4 — endpoint + assembler + query types + tests

| File | Change |
|------|--------|
| `src/Services/Reflekt/Reflekt.API/Features/RetroSessions/GetSprintContext/GetSprintContextQuery.cs` | Request + response + DTOs: `SprintContextRequest`, `SprintContextResponse`, `SprintContextSection`, `ScopeAdditionDto`. `ScopeAdditionDto` has exactly 3 fields (TicketKey, Summary, StoryPoints) — no assignee data (Rule 4 / ADR-019). |
| `src/Services/Reflekt/Reflekt.API/Features/RetroSessions/GetSprintContext/GetSprintContextAssembler.cs` | Assembly logic (extracted from endpoint for testability): per-sprint gRPC pull, Rule 6 scope-addition gate, overflow cap, snapshot fallback, section ordering. |
| `src/Services/Reflekt/Reflekt.API/Features/RetroSessions/GetSprintContext/GetSprintContextEndpoint.cs` | Thin FastEndpoints endpoint: `[AllowAnonymous]`, `GET /api/retro-sessions/{retroSessionId}/sprint-context`, 404 on unknown retro, delegates assembly to `GetSprintContextAssembler`. |
| `src/Services/Reflekt/Reflekt.API/DependencyInjection.cs` | Registered `GetSprintContextAssembler` as scoped. |
| `src/Services/Reflekt/Reflekt.Persistence.Tests/SprintContext/GetSprintContextAssemblerTests.cs` | 10 tests: live section, 2-ref ordering, gRPC-throws+snapshot (cached), gRPC-throws+no-snapshot (unavailable), Rule 6 gate ON (amber), Rule 6 gate OFF (green), overflow cap (3+2), structural no-assignee assertion, per-sprint isolation, nulls-last ordering. All passed (10/10). |

### Step 5 — `src/Services/Reflekt/CLAUDE.md` updated

Marked `ISprintAnalyticsService` gRPC client and `GET /api/retro-sessions/{id}/sprint-context` as landed in RA4. Updated carry-overs from RA4 → RA5.

## Design decisions made during implementation

**Assembler extraction.** `GetSprintContextAssembler` was extracted from the endpoint to keep the assembly logic testable without a FastEndpoints HTTP pipeline. The endpoint becomes a thin shell that reads the retro and delegates to the assembler. This is a minimal structural addition — no new abstraction layer, no interface.

**`DisruptionRag` provenance.** `DisruptionRate` on the contract is `ScopeDisruptionRate + BugDisruptionRate` (summed). `DisruptionRag` is Fokus's `health.DisruptionRag`, which also bands on that same combined `DisruptionRate` — there is no asymmetry between the two fields (verified: `SprintSummaryAnalyzer.cs:497` calls `MetricRag(metrics.DisruptionRate, ...)`). The RAG is mapped straight from the analyzer — never recomputed here (Rule 3).

**Overflow semantics.** `AddedMidSprintOverflow` counts how many of the top-N-by-SP entries from `TopScopeAdditions` are not displayed (i.e. `max(0, TopScopeAdditions.Count - 3)`). Fokus truncates `TopScopeAdditions` server-side at 5 entries, so the displayed "+N more" is bounded at 2 — it is not the true total of all mid-sprint additions. This limitation is visible to the frontend and noted in the assembler comment.

**No-date ordering.** Sections in "cached" or "unavailable" state have no `StartDate` (the snapshot does not store dates — PC3). They sort after all live sections (which have `StartDate`), then by `SprintId` ascending for determinism.

**Per-sprint gRPC isolation.** Each sprint's gRPC call is wrapped independently. A failure on sprint N logs a warning and falls back to snapshot/unavailable; the other sprints in the same response are unaffected. A non-200 is never returned due to a single sprint's Fokus outage (ADR-019 / Rule 9).

## RA5 carry-overs

- Vue sprint-context panel component (`/retro/{id}/sprint-context` view).
- Wire `FokusSprintIds` into `CreateRetroSession` client UI.
- Wire authenticated `Participant.PersonId` into sprint-context (once Identity auth is fully wired — RA2 carry-over).
- `FokusSprintSnapshot` dates absent (RA5 revisit if UI needs offline sprint dates in cached mode).
