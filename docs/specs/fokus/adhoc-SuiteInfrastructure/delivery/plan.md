# Suite Infrastructure: gRPC Contracts + MassTransit In-Memory

**Status:** Approved
**Feature Spec:** None (ad-hoc infrastructure, driven by `docs/proposals/suite-integration-architecture.md`)

## Refresh Notes (vs as-built V2)

This plan was originally written against an earlier, discarded scaffold attempt. It has been re-validated against the **real** SprintRituals V2 scaffold at `this repo` (built by `adhoc-SuiteScaffoldingV2`, reviewer-approved). Every stale reference below was corrected against the actual code:

| # | Stale claim (old plan) | Reality (as-built V2) | Fix applied |
|---|------------------------|------------------------|-------------|
| 1 | "11 → 12 projects" | Scaffold has **17** projects | Total after adding Blocks.Contracts is **18**; corrected in Step 8 + Testing. |
| 2 | Solution at `src/SprintRituals.slnx` | Solution is at repo **root**: `SprintRituals.slnx` | Corrected path everywhere (Steps 8, Testing, Cross-Repo Notes). |
| 3 | Step 4 registers gRPC services in `AddApiServices` | The Fokus DI method is `AddFokusModule(this IServiceCollection, IConfiguration)`. gRPC impls are resolved by the gRPC middleware and **do not need explicit `AddScoped`** registration. | Step 4 no longer adds DI registrations for the impls; only the `Blocks.Contracts` reference + package are added to `Fokus.API.csproj`. |
| 4 | Step 4 injects only 5 deps (`SprintSummaryService`, `ScopeChangeService`, `CarryOverService`, `SprintRepository`, `AppSettingsRepository`) | All three analytics services are **synchronous compute services that take pre-loaded entities** (they load nothing themselves). To call `ComputeSummary` the impl must reproduce the entire data-loading orchestration of `GetSprintSummaryEndpoint` — which also needs `DeveloperRepository`, `TicketRepository`, and (for QA) `TestExecutionRepository` + `QaMetricsService`. | Step 4 expanded to list the real dependency set and point at the endpoint as the data-loading pattern reference. Confidence lowered to **medium**. |
| 5 | Step 5 edits an existing Kestrel section; "5000 HTTP + 5001 gRPC" in both appsettings.json and appsettings.Development.json | `appsettings.json` has **no Kestrel section**; the HTTP:5000 binding comes from `Properties/launchSettings.json`, not appsettings. There is no gRPC port in any config. | Step 5 now **creates** the Kestrel `Endpoints` section in appsettings.json (HTTP stays 5000, adds h2c gRPC port), and drops the false appsettings.Development.json gRPC-port edit. |
| 6 | Step 1 mandates `GlobalUsings.cs` | **No** BuildingBlocks package has a GlobalUsings.cs — they all rely on `ImplicitUsings enable`. | Step 1 makes GlobalUsings.cs optional (only if shared imports emerge). |
| 7 | Step 7 framed as "publisher portion (domain event handler)" | `SprintSynced` is an **integration** event published **directly** via `IPublishEndpoint` from the sync service. The scaffold's domain-event path (raise on aggregate → `DispatchDomainEventsInterceptor`) is **separate** and not used here. `SprintIssueSyncService` is a primary-constructor class; its batch method `SyncSprintsFromJiraAsync` saves **per-sprint inside a loop** (no single terminal SaveChanges). | Step 7 reframed as a direct integration-event publish, with the real publish point identified (per-sprint, after that sprint's memberships are saved). Not a domain-event handler. |
| 8 | `SprintSynced(... string State)` | `Sprint.State` is the **`SprintState` enum**, `Sprint.BoardId` is `int`, `Goal` is `string?`. | Step 3 keeps `string State` in the contract but notes the publish maps `State.ToString()`. `BoardId` int matches. |
| 9 | `SprintInfo` contract name | `Fokus.API.Features.Analytics` already defines a `SprintInfo` record (and `ScopeChangeService` defines `ScopeChangeSprintInfo`, etc.). | Step 2 flags the name collision: the contract `SprintInfo` lives in a different namespace, so it compiles, but the gRPC impl must fully-qualify when both are in scope. |

Scope, out-of-scope, privacy boundary, and the 8-step structure are unchanged. Step count remains **8**.

## Context

The SprintRituals monorepo (`this repo\`) composes Identity + Fokus in a single Host (`src/Host/Program.cs`) but has no inter-module communication layer. The proposal mandates gRPC (protobuf-net.Grpc, code-first) for synchronous cross-module calls (§2) and MassTransit in-memory for integration events (§6). This plan delivers that infrastructure — contracts, server implementation, and event publishing — so Reflekt can consume them when ported.

## Scope

**In scope:**
- New `Blocks.Contracts` BuildingBlock — gRPC service interfaces + integration event DTOs
- gRPC server implementation in Fokus.API (anonymized sprint data)
- Kestrel h2c endpoint for gRPC
- MassTransit in-memory transport registration in the Host
- `SprintSynced` integration event published after sync

**Out of scope:**
- Reflekt port, gRPC client registration (comes with Reflekt)
- `HealthScoreCalculated` event (v3 per proposal §6)
- `FokusSprintSnapshot` projection (Reflekt consumer responsibility, §5)
- Frontend changes

## Domain Model Changes

None — no new aggregates or entities. gRPC exposes existing data through an anonymization boundary.

## Data Model Changes

None — no new tables or migrations.

## Skill Mapping

| Step | Skill | Disposition |
|------|-------|-------------|
| 1 — Blocks.Contracts project | `create-building-blocks-package` | Follow — scaffold csproj (+ GlobalUsings only if needed) |
| 2 — gRPC service contracts | `create-grpc-contract` | Follow — Contract.md (interfaces + data contracts) |
| 3 — SprintSynced event | `add-integration-event` | Follow — EventContract.md only (no consumer) |
| 4 — gRPC server implementation | `create-grpc-contract` | Follow — Server.md; data-loading mirrors `GetSprintSummaryEndpoint` |
| 5 — Host gRPC wiring | None | Host composition; no skill covers Kestrel h2c + MapGrpcService |
| 6 — MassTransit in-memory | None | Host-level transport registration; no skill covers bus setup |
| 7 — Publish SprintSynced | `add-integration-event` | Follow — Publisher.md (direct `IPublishEndpoint` publish, not a domain-event handler) |
| 8 — Solution + docs update | None | Manual |

## Implementation Steps

### Step 1: Create Blocks.Contracts project

Create a new BuildingBlock for app-specific cross-module contracts (gRPC interfaces + integration events). Per the proposal §2/§6 and `project-rules.md` (BuildingBlocks "app-specific" tier), this is where gRPC contracts and integration-event contracts belong.

**Files to create:**
- `src/BuildingBlocks/Blocks.Contracts/Blocks.Contracts.csproj`
- (optional) `src/BuildingBlocks/Blocks.Contracts/GlobalUsings.cs` — only if shared imports emerge; sibling Blocks packages do NOT have one (they use `ImplicitUsings enable`).

**csproj:** match the sibling pattern (read `Blocks.Domain.csproj` for the exact shape): `Microsoft.NET.Sdk`, `<TargetFramework>net10.0</TargetFramework>`, `ImplicitUsings enable`, `Nullable enable`. Add packages: `System.ServiceModel.Primitives` (for `[ServiceContract]`/`[OperationContract]`) and `System.Runtime.Serialization.Primitives` (for `[DataContract]`/`[DataMember]`). No `protobuf-net.Grpc` dependency here — contracts are attribute-only POCOs.

**Skill:** `create-building-blocks-package`

**Acceptance:** Project builds. No protobuf-net or MassTransit dependency in contracts (attributes/POCOs only).

**Confidence:** high

---

### Step 2: Define gRPC service contracts and data contracts

Define the two gRPC service interfaces and their request/response data contracts per the proposal's privacy boundary (§2, §9) — team-level metrics only, no per-developer data.

**Files to create:**
- `src/BuildingBlocks/Blocks.Contracts/Grpc/ISprintAnalyticsService.cs`
- `src/BuildingBlocks/Blocks.Contracts/Grpc/ISprintCatalogService.cs`
- `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/SprintRequest.cs`
- `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/TeamRequest.cs`
- `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/AnonymizedSprintSummary.cs`
- `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/DisruptionMetrics.cs`
- `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/CarryOverHistory.cs`
- `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/SprintInfo.cs`
- `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/SprintCatalogResponse.cs`

**Contract surface (matches proposal §2):**

```
ISprintAnalyticsService [ServiceContract]:
  GetSprintSummaryAsync(SprintRequest) → AnonymizedSprintSummary
  GetDisruptionMetricsAsync(SprintRequest) → DisruptionMetrics
  GetCarryOverHistoryAsync(TeamRequest) → CarryOverHistory

ISprintCatalogService [ServiceContract]:
  GetAvailableSprintsAsync(TeamRequest) → SprintCatalogResponse
```

**AnonymizedSprintSummary:** CompletionRate, DisruptionRate (combined scope+bug), CarryOverRate, HealthScore (composite 0-100), HealthRag (string), SpCompleted, ActiveSp, TopScopeAdditions (list: TicketKey, Summary, StoryPoints).

**DisruptionMetrics:** ScopeDisruptionRate, BugDisruptionRate, AddedSp, BugAddedSp, MidSprintAdditionCount.

**CarryOverHistory:** CarryOverRate (current sprint), CarryOverSp, ZombieTicketCount, SprintHistory (list: SprintName, CarryOverRate — last 4 sprints).

**SprintInfo (contract):** SprintId, Name, StartDate, EndDate, State, Goal.

**SprintCatalogResponse:** List of SprintInfo.

**Name collision note:** `Fokus.API.Features.Analytics` already defines a `SprintInfo` record. The contract `SprintInfo` lives in `Blocks.Contracts.Grpc.DataContracts`, so it compiles — but the Step 4 impl will have both namespaces in scope and must fully-qualify references. Do **not** rename the contract type (binding public surface per proposal); resolve the ambiguity by qualification in the impl.

**Privacy rule (§9):** No developer names, no per-developer metrics, no assignee data crosses this boundary. The contract type IS the enforcement mechanism.

**Skill:** `create-grpc-contract` — Contract.md workflow.

**Acceptance:** Contracts compile. All data types use `[DataContract]`/`[DataMember]`; service interfaces use `[ServiceContract]`/`[OperationContract]`. No per-developer field on any type.

**Confidence:** high

---

### Step 3: Define SprintSynced integration event

**Files to create:**
- `src/BuildingBlocks/Blocks.Contracts/IntegrationEvents/SprintSynced.cs`

**Shape:** `record SprintSynced(int SprintId, string SprintName, int BoardId, DateTime SyncedAt, string State)`

Plain record — no attributes. MassTransit uses convention-based serialization. Per proposal §6, the event carries all data the consumer needs to build its local `FokusSprintSnapshot` projection without calling back.

**Source-type note:** `Sprint.State` is the `SprintState` enum and `Sprint.BoardId` is `int` (confirmed in `Fokus.Domain/Sprint/Sprint.cs`). The publish in Step 7 maps `sprint.State.ToString()` into the event's `string State` field; `BoardId` maps directly.

**Skill:** `add-integration-event` — EventContract.md only (publisher is Step 7; no consumer in scope).

**Acceptance:** Record compiles. No MassTransit package dependency in the contracts project (POCO only).

**Confidence:** high

---

### Step 4: Implement gRPC servers in Fokus.API

Implement both gRPC service interfaces, loading data via the existing Fokus repositories, calling the existing synchronous analytics compute services, and mapping the rich results down to the anonymized contracts.

**Files to create:**
- `src/Services/Fokus/Fokus.API/Grpc/SprintAnalyticsGrpcService.cs`
- `src/Services/Fokus/Fokus.API/Grpc/SprintCatalogGrpcService.cs`

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Fokus.API.csproj` — add `protobuf-net.Grpc.AspNetCore` package + `Blocks.Contracts` project reference. (No DI registration changes — the gRPC middleware resolves the impl's constructor dependencies from the container; the impls do NOT need `AddScoped`.)

**Dependency reality:** the analytics services (`SprintSummaryService.ComputeSummary`, `ScopeChangeService.ComputeSingleSprint/ComputeMultiSprint`, `CarryOverService.ComputeSingleSprint/ComputeMultiSprint`) are **synchronous and take fully pre-loaded entities** — they load nothing. The gRPC impl must reproduce the data-loading orchestration that the HTTP endpoints already perform.

- **`SprintAnalyticsGrpcService`** injects: `SprintRepository`, `AppSettingsRepository`, `DeveloperRepository`, `TicketRepository`, `SprintSummaryService`, `ScopeChangeService`, `CarryOverService`. (Add `TestExecutionRepository` + `QaMetricsService` only if the anonymized HealthScore must include the QA sub-score; otherwise pass `hasQaData: false` and omit them.)
- **`SprintCatalogGrpcService`** injects: `SprintRepository`.

**Pattern reference (load-then-compute):** `src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryEndpoint.cs` is the canonical orchestration to mirror for `GetSprintSummaryAsync` — analytics sprints → window resolution → memberships → developers → settings → capacity lookup → status transitions → excluded-developer filter → epic tickets → `ComputeSummary`. For disruption use the `GetScopeChange` endpoint's loading; for carry-over use the `GetCarryOver` endpoint's loading. Do NOT re-derive the metric math — reuse the existing compute services.

**Mapping:** map the rich internal response records → slim anonymized data contracts. Strip every developer/assignee field (leaderboard, AssigneeName, per-developer summaries). Keep only team-level aggregates. `TopScopeAdditions` derives from the scope-change classification/event data with ticket key + summary + SP only.

The impl decomposition (private load/map helpers) is the developer's decision — describe operations, not a single monolithic method.

**Skill:** `create-grpc-contract` — Server.md workflow.

**Acceptance:** Both services implement their interface. No per-developer data in any mapped response. Build passes. Endpoint orchestration is reused, not re-derived.

**Confidence:** medium

---

### Step 5: Wire gRPC in the Host (Kestrel h2c + MapGrpcService)

Add code-first gRPC to the Host and expose it over an h2c (HTTP/2 cleartext) Kestrel endpoint on a second port, keeping the existing HTTP:5000 binding.

**Files to modify:**
- `src/Host/Host.csproj` — add `protobuf-net.Grpc.AspNetCore` package.
- `src/Host/Program.cs` — add `builder.Services.AddCodeFirstGrpc();` alongside the other `builder.Services` registrations (before `builder.Build()`); after `app.UseFastEndpoints(...)` (and before `MapFallbackToFile`), add `app.MapGrpcService<SprintAnalyticsGrpcService>();` and `app.MapGrpcService<SprintCatalogGrpcService>();`.
- `src/Host/appsettings.json` — **create** a `Kestrel:Endpoints` section (none exists today). Keep HTTP on 5000; add a dedicated h2c endpoint for gRPC.

**Reality:** today the Host binds 5000 via `Properties/launchSettings.json` (`applicationUrl`), and `appsettings.json` has **no** `Kestrel` section. Adding an explicit `Kestrel:Endpoints` block makes both ports declarative. Per the root CLAUDE.md Port Convention, gRPC is h2c on its own port (service index TBD — pick the next free local port, e.g. 5001, and record it).

**Kestrel config to add to appsettings.json:**
```json
"Kestrel": {
  "Endpoints": {
    "Http": { "Url": "http://localhost:5000", "Protocols": "Http1AndHttp2" },
    "Grpc": { "Url": "http://localhost:5001", "Protocols": "Http2" }
  }
}
```

When an explicit `Kestrel:Endpoints` section is present, it overrides `launchSettings.json` `applicationUrl`; confirm the SPA proxy (client `vite.config.ts`, currently → localhost:5000) still targets the HTTP port.

**Skill:** None — Host composition. No skill covers Kestrel h2c configuration or `MapGrpcService` wiring; this is the inline justification.

**Acceptance:** Host builds. Kestrel binds **both** ports on startup (both URLs visible in the "Now listening on:" startup log lines).

**Confidence:** medium

---

### Step 6: Wire MassTransit in-memory in the Host

Add MassTransit with the in-memory transport (proposal §6 — no RabbitMQ). No consumers yet — bus infrastructure + publish endpoint only.

**Files to modify:**
- `src/Host/Host.csproj` — add `MassTransit` package.
- `src/Host/Program.cs` — add `builder.Services.AddMassTransit(x => x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context)));` with the other `builder.Services` registrations (before `builder.Build()`).
- `src/Services/Fokus/Fokus.API/Fokus.API.csproj` — add `MassTransit.Abstractions` package (provides `IPublishEndpoint` for Step 7 without pulling the full MassTransit package into the module).

**Skill:** None — Host-level transport registration; no skill covers MassTransit bus setup. Inline justification.

**Acceptance:** Host builds. MassTransit bus-started log line visible at startup ("Bus started: loopback://localhost/").

**Confidence:** medium

---

### Step 7: Publish SprintSynced from Fokus sync operations

After a sprint's data is persisted during sync, publish the `SprintSynced` **integration** event via `IPublishEndpoint`. This is a direct publish — NOT a domain-event handler and NOT routed through the `DispatchDomainEventsInterceptor` (which handles in-boundary domain events only).

**Files to modify:**
- `src/Services/Fokus/Fokus.API/Features/Sync/SprintIssueSyncService.cs` — add `IPublishEndpoint` to the primary constructor; publish `SprintSynced` after each sprint's memberships are saved.

**Publish point (real):** `SyncSprintsFromJiraAsync` loops over `jiraSprints`; for each it calls `SyncAsync`, which ends with `await sprintRepository.SaveChangesAsync(ct)` (after `UpsertMembershipsAsync`). There is **no single terminal SaveChanges** for the batch. Publish **one event per successfully-synced sprint**, inside the loop, after that sprint's `SyncAsync` returns (within the existing `try`, before the `catch`) — so a failed sprint (added to `failures`) does not emit an event. Map `sprint.State.ToString()` for the event's `State` field; `sprint.Id`, `sprint.Name`, `sprint.BoardId`, `sprint.SyncedAt` map directly.

**Skill:** `add-integration-event` — Publisher.md (direct publish variant; ignore the domain-event-handler framing — that variant does not apply here).

**Acceptance:** Build passes. Each synced sprint publishes one `SprintSynced` with correct SprintId, SprintName, BoardId, SyncedAt, and State string. Failed sprints publish nothing. Observable via MassTransit Debug-level logging.

**Confidence:** medium

---

### Step 8: Register in solution + update CLAUDE.md

**Files to modify:**
- `SprintRituals.slnx` (repo **root**) — add the `Blocks.Contracts` project (`dotnet sln SprintRituals.slnx add src/BuildingBlocks/Blocks.Contracts/Blocks.Contracts.csproj`, run from the repo root).
- `CLAUDE.md` — add `Blocks.Contracts` to the BuildingBlocks list; update the tech-stack notes so gRPC (protobuf-net.Grpc) and MassTransit in-memory are described as wired, not "future use."

**Acceptance:** `dotnet build SprintRituals.slnx` — zero `error CS`, **18 projects** (was 17).

**Confidence:** high

## Testing Strategy

Build/runtime verification runs against `this repo` (absolute paths; Bash cwd resets — see Cross-Repo Execution Notes).

| # | Check | Mode |
|---|-------|------|
| 1 | `dotnet build SprintRituals.slnx` → 0 `error CS`, 18 projects | **Unattended** (filter MSB file-lock errors per project-rules.md) |
| 2 | Start Host (`run_in_background`); both Kestrel ports bind (5000 HTTP + h2c gRPC port both in "Now listening on" log) | **Unattended** (read startup log) |
| 3 | MassTransit bus-started log line present at startup | **Unattended** (read startup log) |
| 4 | Live gRPC call (`grpcurl` or a code-first test client) to `GetAvailableSprintsAsync` returns a response | **Hand-off** — needs a live gRPC client + seeded sprint data |
| 5 | `SprintSynced` actually publishes on sync | **Hand-off** — requires a real Jira sync (Jira creds/board) → user; not unattended-checkable |
| 6 | Privacy boundary — review every DataContract type: no developer names / per-developer metrics | **Unattended** (static review of contract source) |

**Unattended-checkable:** items 1, 2, 3, 6 (build clean; Host dual-port startup; MassTransit bus-started; contract privacy review).
**Needs hand-off:** items 4 (live gRPC call via grpcurl — needs a client + seeded data) and 5 (`SprintSynced` publish — needs a real Jira sync, which needs Jira credentials/board → user).

## Cross-Repo Execution Notes

This plan is authored in the Fokus repo (`<fokus-repo>`) but **all implementation happens in `this repo`** (mirrors the SuiteScaffoldingV2 plan).

- **Absolute paths only.** The Bash tool's cwd **resets to `<fokus-repo>` after every call** — never rely on a prior `cd`. Reference every source file as `src\...` and run solution/build commands with the absolute slnx path (`dotnet build SprintRituals.slnx`).
- **Solution is at the repo root:** `SprintRituals.slnx` (NOT under `src/`).
- **Run the Host with `run_in_background`.** `dotnet run` (or `dotnet bin\...\Host.dll`) from `src\Host` is long-running — start it detached and read the startup log; do not block. Kill the background Host (`Stop-Process`) before re-running a full `dotnet build`, or the file lock yields MSB3026/MSB3027 copy errors (not compile errors — filter with `error CS`).
- **No commits.** Do not commit or push in either repo. Implementation lands as working-tree changes for review.
- **No secrets.** Jira/Google/Xray credentials live in user-secrets, never in any committed file or artifact. The live-sync test (Testing item 5) hands off to the user — do not embed credentials to force it.

## Open Questions

None — all decisions resolved by the proposal and validated against the as-built scaffold.
