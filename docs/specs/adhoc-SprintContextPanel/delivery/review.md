# adhoc-SprintContextPanel — Review

## Step 2 — Code Review

**Reviewer:** reviewer-scp · **Date:** 2026-06-16 · **Verdict: APPROVED**

### Evidence

| Check | Command | Result |
|-------|---------|--------|
| Build | `dotnet build SprintRituals.slnx` | 0 errors, 12 warnings (all pre-existing `NU1903` — none introduced by this feature) |
| Fokus.API.Tests | `dotnet test Fokus.API.Tests.csproj --no-build` | 3/3 passed |
| Reflekt.Persistence.Tests | `dotnet test Reflekt.Persistence.Tests.csproj --no-build` | 14/14 passed (10 new assembler + 4 pre-existing RA3) |

### Pre-commitment Predictions vs Findings

Predicted: (1) `catch (Exception)` swallowing cancellation, (2) `AsNoTracking` missing on the repo read, (3) `DisruptionRag` provenance drift, (4) nulls-last ordering correctness, (5) overflow semantics.

Results: (1) **confirmed — MEDIUM** (below); (2) pre-existing pattern, not introduced here; (3) correctly handled; (4) correct; (5) correctly documented and capped.

### Carry-Over Findings from implementation.md

**CF1 — DisruptionRag provenance (rate-vs-RAG asymmetry)**
Confirmed honored. `SprintAnalyticsGrpcService.cs:231-236` maps `DisruptionRag` straight from `health?.DisruptionRag` — never recomputed. The contract comment at `AnonymizedSprintSummary.cs:57-61` documents the asymmetry. The assembler comment at `GetSprintContextAssembler.cs:83-84` echoes this. Plan Step 1 binding note satisfied (Rule 3).

**CF2 — Overflow semantics ("top-5-by-SP not true total")**
Correctly implemented. `BuildScopeAdditions` cap at 3 with `Math.Max(0, top.Count - 3)` (`:100`). The bounding by Fokus's server-side `.Take(5)` is documented in the assembler comment and in implementation.md. Critic Low #1 addressed.

**CF3 — First gRPC client in Reflekt (critic Low #2)**
Clean. `DependencyInjection.cs:31-32` mirrors the Fokus shape exactly. Config block present in `appsettings.json:12`.

### Findings

---

#### F1 — `catch (Exception ex)` swallows `OperationCanceledException`

**Severity:** MEDIUM · **Confidence:** HIGH
**File:** `src/Services/Reflekt/Reflekt.API/Features/RetroSessions/GetSprintContext/GetSprintContextAssembler.cs:48`

`BuildSectionAsync` catches `Exception` unconditionally. When the request is cancelled (client disconnects or server shutdown), `OperationCanceledException` is caught, logged as a warning ("Fokus gRPC pull failed"), and the code proceeds to query the snapshot repository and construct a `cached`/`unavailable` section — then the next iteration does the same. The cancellation is effectively swallowed for the duration of the sprint loop.

The established pattern in this codebase (same service): `OpenGraphFetcher.cs:40,61` uses `catch (Exception ex) when (ex is not OperationCanceledException)`.

**Suggested fix:**
```csharp
catch (Exception ex) when (ex is not OperationCanceledException)
```

This matches the csharp.md convention ("Don't catch `Exception` without a filter or re-throw") and the established local pattern.

---

#### F2 — `GetWithFokusSprintsAsync` missing `.AsNoTracking()`

**Severity:** LOW · **Confidence:** HIGH
**File:** `src/Services/Reflekt/Reflekt.Persistence/Repositories/SprintRepository.cs:25-28`

The new read-only query loads a `RetroSession` with `FokusSprintRefs` for display purposes only; nothing is written back. EF conventions from `csharp.md` require `.AsNoTracking()` on all read-only queries. The existing `GetByTeamIdAsync` and `GetMostRecentClosedByTeamIdAsync` methods in the same file also lack it — so this is a pre-existing gap, not introduced by this feature. Flag only for this method since it is the RA4 addition.

**Note:** This is LOW rather than MEDIUM because the query result is read-only in practice and no Fokus repositories use `.AsNoTracking()` either — it is a consistent (if non-ideal) project pattern across both services. The risk is cosmetic overhead.

---

### Privacy Boundary Assessment (Rule 4 / ADR-019)

`ScopeAdditionDto` has exactly 3 properties (`TicketKey`, `Summary`, `StoryPoints`). Confirmed by the structural assertion in `GetSprintContextAssemblerTests.cs:268-270`. `SprintContextSection` carries no assignee, developer name, or account ID. `AnonymizedSprintSummary` contract extension carries no per-developer data. **Privacy boundary holds.**

### Plan Conformance Summary

| Plan Requirement | Status |
|-----------------|--------|
| 7 additive DataMembers Orders 9–15; Orders 1–8 unchanged | Verified — exact Orders, byte-unchanged fields |
| Mapping from `selectedSprint` + `health.*Rag` — no recomputation | Verified — `SprintAnalyticsGrpcService.cs:225-236` |
| `AddCodeFirstGrpcClient<ISprintAnalyticsService>` in Reflekt DI | Verified — `DependencyInjection.cs:32` |
| `Grpc:Services:SprintAnalytics:Url` in appsettings | Verified — `appsettings.json:12` |
| `GetWithFokusSprintsAsync` eager-loads `FokusSprintRefs` | Verified — `SprintRepository.cs:25-28` |
| Route `GET /api/retro-sessions/{retroSessionId}/sprint-context` | Verified — `GetSprintContextEndpoint.cs:13` |
| `[AllowAnonymous]` | Verified — `GetSprintContextEndpoint.cs:12` |
| 404 on unknown retroSessionId | Verified — `GetSprintContextEndpoint.cs:23-26` |
| Rule 6 gate: `DisruptionRag != "green"` → show scope additions | Verified — `GetSprintContextAssembler.cs:90-91` |
| Cap at 3 + overflow `max(0, Count-3)` | Verified — `:94-100` |
| Per-sprint gRPC isolation; failure → cached/unavailable, not 5xx | Verified — `:39-56, :104-138` |
| Nulls-last ordering (live first, then cached/unavailable, then SprintId) | Verified — `:145-151` |
| Assembler registered as scoped | Verified — `DependencyInjection.cs:51` |
| `Reflekt/CLAUDE.md` updated with RA4 landed + RA5 carry-overs | Verified |

### Non-blocking Follow-ups

- **F1** (MEDIUM): Apply `when (ex is not OperationCanceledException)` to the catch filter in the next PR touching this file, or as a standalone cleanup.
- **F2** (LOW): Add `.AsNoTracking()` to `GetWithFokusSprintsAsync` when the file is next touched.

### Verdict

**APPROVED.** Both MEDIUM and LOW findings are non-blocking. The only MEDIUM (F1) is a missing cancellation filter — the correctness impact is limited to unnecessary snapshot lookups after request cancellation, with no data integrity or security consequence. Build is green, all 17 tests (3 Fokus + 14 Reflekt) pass, plan conformance is complete, privacy boundary holds, and all carry-over findings are addressed.

---

## Step 1 — Done-Check

Done-check by the architect against `plan.md` (Approved). Every step verified in code, not from the implementation report alone; build + the two affected test projects run fresh.

| Step | Disposition | Notes |
|------|-------------|-------|
| 1 — Enrich `AnonymizedSprintSummary` + map in Fokus server | Implemented | `AnonymizedSprintSummary.cs` carries 15 `[DataMember]`s; Orders 1–8 byte-unchanged (`TopScopeAdditions` still Order 8), 7 new fields at Orders 9–15 exactly as planned. Mapping present in `SprintAnalyticsGrpcService.cs:225-236` from `selectedSprint.{Id,Name,StartDate,EndDate}` and `health?.{Completion,Disruption,CarryOver}Rag ?? "red"`. The pinned `DisruptionRag` provenance (analyzer band, never recomputed against the summed rate) is honored and documented in the contract comment (`:57-61`). TDD: `Fokus.API.Tests/Grpc/SprintAnalyticsGrpcServiceTests.cs` — 3/3 green (identity fields, per-rate RAG, health band). New `Fokus.API.Tests` project added to `SprintRituals.slnx`. |
| 2 — Register `ISprintAnalyticsService` gRPC client in Reflekt | Implemented | `Reflekt.API/DependencyInjection.cs:31-32` — `GetSection("Grpc").Get<GrpcServicesOptions>()` then `AddCodeFirstGrpcClient<ISprintAnalyticsService>(grpcOptions, "SprintAnalytics")`, mirroring the Fokus shape. `Host/appsettings.json:12` adds the `SprintAnalytics` block at `:5001`. Reflekt's first gRPC client (critic Low #2) — net-new config read + usings landed cleanly; no server-mapping change needed (already mapped). |
| 3 — `GetWithFokusSprintsAsync` on the retro repo | Implemented | `SprintRepository.cs:25-27` — `RetroSessionRepository.GetWithFokusSprintsAsync` eager-loads `FokusSprintRefs` via `.Include`. Behavior covered transitively by Step 4's assembler tests. |
| 4 — `GET …/sprint-context` endpoint + assembler | Implemented | Route `/api/retro-sessions/{retroSessionId}/sprint-context`, `[AllowAnonymous]`, 404 on unknown retro (`GetSprintContextEndpoint.cs:13,23-27`). Response shape matches plan (nullable dates/metric block, `Freshness`, `AddedMidSprint` + `AddedMidSprintOverflow`). `ScopeAdditionDto` has exactly 3 fields, no assignee (`GetSprintContextQuery.cs:42`). Assembler: Rule 6 gate `DisruptionRag == "green" → null` (`:90-91`); cap-at-3 + overflow `max(0, Count-3)` with the critic-Low "+N among top-5-by-SP" semantics documented (`:94-100`); per-sprint try/catch isolation → cached/unavailable fallback (`:41-55,104-138`); dated-first then `SprintId` ordering (`:148-150`). Assembler registered scoped (`DependencyInjection.cs:51`). TDD: `GetSprintContextAssemblerTests.cs` — 10/10 green, including the structural no-assignee assertion via `typeof(ScopeAdditionDto).GetProperties()` (`:268-270`). |
| 5 — Module docs + RA5 carry-overs | Implemented | `Reflekt/CLAUDE.md` — gRPC client + sprint-context endpoint marked landed in RA4; carry-overs moved to RA5 (Vue panel, `FokusSprintIds` UI wiring, authenticated `Participant.PersonId`); the `DisruptionRate`-vs-`DisruptionRag` provenance recorded. `docs/backlog.md` left to the owner per convention. |

**Both critic Low items folded into the plan are reflected in the code:** the overflow "+N among top-5-by-SP" framing (Step 4 assembler comment + implementation.md) and the first-gRPC-client note (Step 2). Both flagged spec divergences (PC3 thin snapshot → `cached` shows identity only; Rule 7 by-SP ordering) are implemented as planned and documented in implementation.md — correctly routed to RA5, not silently dropped.

**Verification evidence:**
- Build: `dotnet build SprintRituals.slnx` → 0 errors, 12 warnings (all pre-existing `NU1903` transitive-dep advisory, not introduced here).
- Tests: `Fokus.API.Tests` 3/3; `Reflekt.Persistence.Tests` 14/14 (10 new assembler + 4 pre-existing RA3). Both projects green, run fresh `--no-build`.

No step missing, no silent skip, no scope creep (the new `Fokus.API.Tests` project is a sanctioned test home, noted in implementation.md). The assembler extraction is a minor, justified structural addition (testability without the HTTP pipeline), not an unplanned abstraction.

**Verdict: PASS**

*Status: COMPLETE — architect, 2026-06-16*
