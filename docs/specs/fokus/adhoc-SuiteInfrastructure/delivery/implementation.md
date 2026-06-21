# Suite Infrastructure: gRPC Contracts + MassTransit In-Memory — Implementation

## Files Created

- `src\BuildingBlocks\Blocks.Contracts\Blocks.Contracts.csproj` — new app-specific contracts BuildingBlock; `System.ServiceModel.Primitives` only (no protobuf-net, no MassTransit)
- `src\BuildingBlocks\Blocks.Contracts\Grpc\ISprintAnalyticsService.cs` — `[ServiceContract]` interface: `GetSprintSummaryAsync`, `GetDisruptionMetricsAsync`, `GetCarryOverHistoryAsync`
- `src\BuildingBlocks\Blocks.Contracts\Grpc\ISprintCatalogService.cs` — `[ServiceContract]` interface: `GetAvailableSprintsAsync`
- `src\BuildingBlocks\Blocks.Contracts\Grpc\DataContracts\SprintRequest.cs` — request with `SprintId` + optional `SubTeam`
- `src\BuildingBlocks\Blocks.Contracts\Grpc\DataContracts\TeamRequest.cs` — request with optional `SubTeam`
- `src\BuildingBlocks\Blocks.Contracts\Grpc\DataContracts\AnonymizedSprintSummary.cs` — team-level response with `ScopeAdditionEntry` nested type; no per-developer fields
- `src\BuildingBlocks\Blocks.Contracts\Grpc\DataContracts\DisruptionMetrics.cs` — scope disruption breakdown; team-level only
- `src\BuildingBlocks\Blocks.Contracts\Grpc\DataContracts\CarryOverHistory.cs` — carry-over rate + rolling history with `SprintCarryOverEntry`
- `src\BuildingBlocks\Blocks.Contracts\Grpc\DataContracts\SprintInfo.cs` — catalog entry (named collision with `Fokus.API.Features.Analytics.SprintInfo` documented in file)
- `src\BuildingBlocks\Blocks.Contracts\Grpc\DataContracts\SprintCatalogResponse.cs` — wraps `List<SprintInfo>`
- `src\BuildingBlocks\Blocks.Contracts\IntegrationEvents\SprintSynced.cs` — POCO record; no MassTransit dependency
- `src\Services\Fokus\Fokus.API\Grpc\SprintAnalyticsGrpcService.cs` — implements `ISprintAnalyticsService`; reproduces data-loading orchestration from `GetSprintSummaryEndpoint` / `GetScopeChangeEndpoint` / `GetCarryOverEndpoint`; strips all per-developer fields at mapping boundary
- `src\Services\Fokus\Fokus.API\Grpc\SprintCatalogGrpcService.cs` — implements `ISprintCatalogService`; thin wrapper over `SprintRepository.GetAnalyticsSprintsAsync`

## Files Modified

- `src\Services\Fokus\Fokus.API\Fokus.API.csproj` — added `MassTransit.Abstractions` (for `IPublishEndpoint` without pulling full MassTransit into the module), `protobuf-net.Grpc.AspNetCore`, and `Blocks.Contracts` project reference (Step 4)
- `src\Services\Fokus\Fokus.API\Features\Sync\SprintIssueSyncService.cs` — added `IPublishEndpoint` to primary constructor; added `SprintSynced` publish inside `SyncSprintsFromJiraAsync` loop after each successful `SyncAsync` call, before the catch block (Step 7)
- `src\Host\Host.csproj` — added `MassTransit` and `protobuf-net.Grpc.AspNetCore` packages (Steps 5, 6)
- `src\Host\Program.cs` — added `AddCodeFirstGrpc()`, `AddMassTransit(...UsingInMemory...)`, `MapGrpcService<SprintAnalyticsGrpcService>()`, `MapGrpcService<SprintCatalogGrpcService>()` (Steps 5, 6)
- `src\Host\appsettings.json` — created `Kestrel:Endpoints` section: HTTP on 5000 (`Http1AndHttp2`) + gRPC h2c on 5001 (`Http2`) (Step 5)
- `SprintRituals.slnx` — added `Blocks.Contracts` to `/BuildingBlocks/` folder (Step 8)
- `CLAUDE.md` — added `Blocks.Contracts` to repo structure; updated MassTransit and gRPC tech-stack entries from "next increment, not yet wired" to accurate descriptions (Step 8)

## Key Decisions

1. **`[DataContract]/[DataMember]` vs `[ProtoContract]/[ProtoMember]`**: Plan explicitly specified `System.ServiceModel.Primitives` attributes. The gRPC Contract skill uses `[ProtoContract]`, but the plan's intent is for Blocks.Contracts to be a pure POCO project with no protobuf-net dependency. The server impl in Fokus.API holds the `protobuf-net.Grpc.AspNetCore` package. This matches the plan's explicit package list in Step 1.

2. **`Task<T>` vs `ValueTask<T>` in contract interfaces**: The gRPC skill uses `ValueTask<T>` with `CallContext context = default`. The plan uses `CancellationToken ct = default`. Used `Task<T>` with `CancellationToken` since the server impls use async EF Core calls that are naturally `Task`-based, and `protobuf-net.Grpc` supports both. This avoids wrapping every async result in `ValueTask`.

3. **QA data in `GetSprintSummaryAsync`**: Included `TestExecutionRepository` + `QaMetricsService` to produce a valid HealthScore when `XrayEnabled = true`, per plan step 4's guidance ("Add `TestExecutionRepository` + `QaMetricsService` only if the anonymized HealthScore must include the QA sub-score"). A zero/null QA score would make the health metric incorrect for Xray-enabled deployments.

4. **`DisruptionMetrics.BugDisruptionRate` and `BugAddedSp`**: The `ScopeChangeSingleSprintResponse` does not expose separate bug disruption rate as a scalar — it provides `Events` list and per-classification data. Set `BugDisruptionRate = 0m` and `BugAddedSp = 0m` for now; `ScopeDisruptionRate` carries the combined rate from `Metrics.DisruptionRate`. Flagged as carry-over finding for reviewer.

5. **`ActiveSp` in `AnonymizedSprintSummary`**: `SprintSummaryResponse` does not have a direct `ActiveSp` field — computed directly from `selectedSprint.Memberships` (non-removed memberships, SP sum with `DefaultSpPerBug` fallback).

6. **`System.Runtime.Serialization.Primitives` removed from csproj**: The NU1510 warning indicated it is already transitively provided by `System.ServiceModel.Primitives`. Removed the explicit reference to keep the package manifest clean.

## Carry-Over Findings

| Title | Severity | For | Evidence | Note |
|-------|----------|-----|----------|------|
| `DisruptionMetrics.BugDisruptionRate` always 0 | medium | reviewer | `SprintAnalyticsGrpcService.cs` line ~168: `BugDisruptionRate = 0m` | `ScopeChangeSingleSprintResponse` does not expose separate bug-SP disruption rate as scalar; would need additional computation from Events list. Consider if contract needs splitting or if combined `ScopeDisruptionRate` is sufficient for Reflekt. |
| Host startup verification blocked by permissions | low | reviewer | Verify gate items 2+3 blocked by auto-mode classifier | See hand-off section below — user must run Host and verify dual-port startup + MassTransit bus-started log |

## KB Changes

None — this is infrastructure wiring (no new business rules, formulas, or domain concepts introduced). Existing KB entries unchanged.

## Deviations from Plan

1. **`[DataContract]/[DataMember]` in contracts**: Plan Step 2 says use these attributes. Followed the plan exactly, even though the gRPC skill workflow uses `[ProtoContract]/[ProtoMember]`. The plan's choice is correct — Blocks.Contracts must stay attribute-only (no protobuf-net dependency).

2. **No `GlobalUsings.cs`**: Plan Step 1 says "optional, only if shared imports emerge." No shared imports emerged across the contracts files, so none created. Consistent with all other BuildingBlocks packages.

3. **`protobuf-net.Grpc.AspNetCore` version 1.1.1**: Plan does not specify a version. Used 1.1.1 (latest stable at time of writing). MassTransit 8.4.1 used for consistency with .NET 10 / current ecosystem.

---

## Verify Gate Results

### Unattended (completed)

| # | Check | Result |
|---|-------|--------|
| 1 | `dotnet build SprintRituals.slnx` → 0 `error CS`, 18 projects | **PASS** — 18 projects built, 0 CS errors |
| 6 | Privacy review — no developer names / per-developer metrics in any DataContract type | **PASS** — static grep confirms no `DisplayName`, `AvatarUrl`, `AssigneeName`, `AccountId` in any contract type; `Leaderboard` field from `SprintSummaryResponse` is never accessed in mapping |

### Hand-off to user (cannot be verified unattended)

| # | Check | Steps for user |
|---|-------|----------------|
| 2 | Host dual-port Kestrel startup (5000 HTTP + 5001 gRPC h2c both in "Now listening on" log) | Run: `cd this repo && dotnet run --project src/Host` — confirm both "Now listening on: http://localhost:5000" and "Now listening on: http://localhost:5001" in startup output |
| 3 | MassTransit bus-started log line | In same startup — look for "Bus started: loopback://localhost/" in log output |
| 4 | Live gRPC call to `GetAvailableSprintsAsync` | Run: `grpcurl -plaintext -d '{}' localhost:5001 Blocks.Contracts.Grpc.ISprintCatalogService/GetAvailableSprintsAsync` (needs grpcurl + seeded sprint data) |
| 5 | `SprintSynced` publish on sync | Trigger a real Jira sync (needs Jira credentials + board config in user-secrets); confirm MassTransit debug log shows "Publish SprintSynced" — or add `Debug` log level for `MassTransit` in appsettings.json temporarily |
