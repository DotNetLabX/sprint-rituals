# Sprint Context Panel — Fokus Metrics in the Retro Room (RA4)

**Feature Spec:** `docs/specs/adhoc-SprintContextPanel/definition/spec.md`
**Status:** Approved — Ready for dev (critic ACCEPT, no blockers; 2 Low items folded — see `review-critic.md` + the critic-Low notes on Steps 2 and 4).
**Scope resolution (Q1, user-confirmed — see `questions.md`):** **Option A — backend-only RA4.** The Vue slide-out panel, the top-bar button, Flows 1–4 frontend ACs, and `help.tooltips.md` are **deferred to RA5** (`adhoc-ReflektFrontendMerge`) — the Reflekt SPA is not in this repository (RA1 lifted the backend only). This plan builds the **server half**: the `AnonymizedSprintSummary` contract enrichment, the Reflekt-side gRPC client, and the `GET …/sprint-context` browser-facing endpoint that the RA5 panel will consume.
**Grounded against:** committed `main`, HEAD `327d21d`. Every count/route/symbol/field below comes from a read or grep run at plan time, not from memory.
**Review mode:** critic (code-grounded — reads live Reflekt + Fokus source + the contract files).

---

## Context

The retro room has no view of the sprint it reflects on. Fokus already computes the metrics (completion / disruption / carry-over / health score + per-rate RAG bands + mid-sprint scope additions); RA3 gave `RetroSession` its 1..N Fokus-sprint references and a thin `FokusSprintSnapshot` projection. This pass adds the cross-module **read path**: Reflekt pulls anonymized per-sprint summaries from Fokus over the existing `ISprintAnalyticsService` gRPC contract and exposes them on one browser-facing endpoint, applying the snapshot fallback when Fokus is unreachable. No new domain state, no per-developer data crosses the boundary (ADR-019).

**Services impacted:**
- `Blocks.Contracts` — enrich `AnonymizedSprintSummary` (additive DataMembers: sprint identity + per-rate RAGs).
- `Fokus.API` — map the new fields in the existing `SprintAnalyticsGrpcService` (data already loaded server-side, pure additive mapping).
- `Reflekt.API` / `Reflekt.Persistence` — register the gRPC client, add a refs-eager-loading repo query, build the `GET …/sprint-context` endpoint with the snapshot-fallback assembly.
- `Host` — one `appsettings.json` config block for the new gRPC client target.

---

## Scope

**In scope (backend, RA4):**
- Enrich `AnonymizedSprintSummary` with sprint identity (id, name, start/end dates) + per-rate RAG bands (completion / disruption / carry-over).
- Map those fields in `SprintAnalyticsGrpcService.GetSprintSummaryAsync` (already loads the `Sprint` aggregate + per-metric RAGs).
- Register the `ISprintAnalyticsService` gRPC client in Reflekt (config + DI).
- A `GET /api/retro-sessions/{retroSessionId}/sprint-context` endpoint that loops the retro's `FokusSprintRefs`, pulls each sprint's summary, applies the snapshot fallback, and returns the ordered per-sprint array with a `live`/`cached`/`unavailable` freshness state per section.

**Explicitly out of scope:**
- **All frontend** — the Vue panel, top-bar button, Flows 1–4 frontend ACs, `help.tooltips.md` → **RA5** (no Reflekt SPA in this repo). The endpoint is the RA5 panel's contract; this plan delivers and tests it server-side.
- **SignalR / live refresh** — the endpoint is request/response, session-cached client-side in RA5 (spec Real-time Events: none).
- **Per-developer drill-down / any individual metrics** — permanently out (ADR-019).
- **Reflekt → Fokus writeback** — deferred (ADR-019).
- **Renaming the flat `/api/sprints/{sprintId}/*` route family or FK columns** — RA5 carry-over (RA3 Q1=b/Q2). This plan's new route uses the resource-noun lifecycle family (`/api/retro-sessions/…`), consistent with RA3's renamed lifecycle routes — see Step 4 binding note.

---

## Planner calls (made explicit, with reasoning)

Three forks the Phase-1 analysis flagged as the planner's to settle. Resolved here, not deferred:

### PC1 — Contract enrichment: per-rate RAG bands (REQUIRED, additive)
**Decision:** add `CompletionRag`, `DisruptionRag`, `CarryOverRag` (`string`) to `AnonymizedSprintSummary`.
**Why:** Spec Rule 3 ("each rate shown with its Fokus-defined indicator color") and Rule 6 (the scope-additions gate keys on the **disruption** band) both need per-rate RAG. Fokus **already computes** all three in `SprintSummaryAnalyzer.cs:496-498` via `HealthScoreCalculator.MetricRag`, and they live on the `HealthScoreData` result the grpc service already holds — but `MapToAnonymizedSummary` (`SprintAnalyticsGrpcService.cs:199-224`) currently maps only the composite `HealthRag` and discards the three per-rate RAGs. This is pure additive mapping of in-hand data. (The KB's "untested" note on `MetricRag` BR6/BR7 is about *test coverage*, not wiring — the method is live in production paths.) Rule 6's gate then reduces to `DisruptionRag != "green"` (amber-or-worse), reusing Fokus's threshold — never recomputed in Reflekt (Rule 3/5).

### PC2 — Contract enrichment: sprint identity (id, name, dates) (REQUIRED, additive)
**Decision:** add `SprintId` (`int`), `SprintName` (`string`), `StartDate` (`DateTime`), `EndDate` (`DateTime`) to `AnonymizedSprintSummary`.
**Why:** Spec Rule 2 / Flow 2 / AC-3 require each section "identified by sprint name and dates" and the array "ordered earliest sprint start first." `AnonymizedSprintSummary` today carries **neither name nor dates**; the Reflekt-local `FokusSprintSnapshot` carries name but **no dates** (only `SyncedAt`). The dates *do* exist on the wire — but only on the **separate** `ISprintCatalogService`/`SprintInfo` contract, which is team-scoped (`TeamQuery`, not keyed by sprint id) and unregistered in Reflekt. Sourcing identity from the summary keeps it to **one** gRPC call per sprint that returns metrics + RAGs + identity together, makes the ordering rule satisfiable from the response alone, and avoids registering a second contract for a team-list lookup that doesn't even key by sprint id. The Fokus server already has the `Sprint` aggregate in hand (`selectedSprint`, which has `Name`/`StartDate`/`EndDate`) inside `GetSprintSummaryAsync` — pure additive mapping, no new load.

### PC3 — The `cached` fallback fork: keep `FokusSprintSnapshot` thin (do NOT add metrics to it)
**Decision:** the snapshot stays exactly the 5 RA3 `SprintSynced` fields. The `cached` freshness state surfaces **sprint identity only** (name + state from the snapshot; dates absent, see note), **no last-known metric numbers**. Distinguished from `unavailable` by: `cached` = a snapshot row exists (sprint was synced at least once) but the live gRPC pull failed; `unavailable` = no snapshot row and no live pull.
**Why:** Making the snapshot a metric cache would mean (a) persisting metrics that ADR-019 deliberately keeps out of the projection ("no rich metrics — those are RA4/gRPC", `FokusSprintSnapshot.cs:5`), and (b) writing them on every gRPC success — a write path with **no consumer yet**, since the UI is deferred to RA5 (Q1). The spec's Rule 9 wording ("falls back to the stored snapshot … marked last-known/cached") is satisfiable in spirit: a `cached` section still renders (name/state, graceful, no error, no blank panel — AC "no blank panel"), it just shows no metric numbers when live data is unreachable. If RA5's UX proves last-known *numbers* are needed offline, enriching the snapshot is a clean isolated follow-up (one projection + one consumer change) — cheaper to add then, against a real consumer, than to speculatively persist now. **This is the conservative call given the deferred UI**; flagged for the critic as the one place the plan diverges from a literal reading of spec Rule 9.
**Snapshot-dates note:** because the snapshot has no dates, a `cached` section reports name + state only (dates `null`). The endpoint response models dates as nullable so `cached`/`unavailable` sections omit them cleanly. The RA5 panel renders the section header from whatever identity is present.

---

## Domain Model Changes

**None.** This pass introduces no aggregate and persists no new state (spec Entities: "introduces no new aggregate and owns no new persisted state"). `RetroSession`, `FokusSprintRef`, and `FokusSprintSnapshot` are all RA3-landed and read as-is. The per-retro "sprint context" is an in-memory display read-model assembled in the endpoint, held only for the request.

## Data Model Changes

**None.** No new tables, columns, or migrations. (PC3 keeps `FokusSprintSnapshot` unchanged.)

---

## Skill Mapping

| Step | Skill | Disposition | TDD | Feature-Specific Inputs | Gap? |
|------|-------|-------------|-----|------------------------|------|
| 1 — Enrich `AnonymizedSprintSummary` + map in Fokus server | (none) | — | yes | 7 additive `[DataMember]` fields (next free Orders 9–15); map from `health.*Rag` + `selectedSprint.Name/StartDate/EndDate/Id` already in hand at `SprintAnalyticsGrpcService.cs:199` | Field-addition to an existing contract isn't taught by `create-grpc-contract` (it creates *new* contracts) — log |
| 2 — Register the `ISprintAnalyticsService` gRPC **client** in Reflekt | create-grpc-contract | Follow | no | Client only (contract + server exist). `AddCodeFirstGrpcClient<ISprintAnalyticsService>(grpcOptions, "SprintAnalytics")` in `Reflekt.API/DependencyInjection.cs`; `Grpc:Services:SprintAnalytics` block in `Host/appsettings.json` | |
| 3 — Add a refs-eager-loading query to `RetroSessionRepository` | persistence-patterns | Follow | no | `GetWithFokusSprintsAsync(int id)` — `Include(s => s.FokusSprintRefs)`, returns `RetroSession?` | |
| 4 — `GET …/sprint-context` endpoint (gRPC pull + snapshot fallback + assembly) | create-feature | Follow | yes | Route, freshness-state logic, ordering, Rule 6 gate, fallback rules — see step | |

**Disposition notes:**
- **Step 1 is `None`, not a `create-grpc-contract` Follow.** Verification: the step's actions are (a) add `[DataMember]` properties to an existing contract class, (b) map already-loaded values in an existing server method. `create-grpc-contract` creates a *new* contract + server + client triple; it does not teach field-addition to a live contract. Marked `None` with the constraint detail inline (exact field names, `[DataMember]` Order continuation, source values). It is still `TDD: yes` — the Fokus-side mapping (per-rate RAG + identity now populated) is asserted by a gRPC-service test.
- **Step 2 is the `create-grpc-contract` *Client* workflow only** — the contract (Step 1) and server already exist. Feature-specific inputs are the client registration + config; the skill's `workflows/Client.md` is the structural pattern. Cited in-repo surface (Fokus's `AddCodeFirstGrpcClient<IPersonService>(grpcOptions, "Person")` at `Fokus.API/DependencyInjection.cs:43`) is the **feature-specific registration line to mirror**, not the structural pattern.

---

## Implementation Steps

### Step 1 — Enrich `AnonymizedSprintSummary` + map the new fields in the Fokus gRPC server
**Satisfies:** AC "completion/disruption/carry-over rates + health score + color band, matching Fokus" (per-rate RAG); AC "two sections ordered earliest-sprint-start first, each with sprint name, dates" (identity); spec Rule 3, Rule 6 gate input.
**Skill:** None (additive field-addition to an existing contract + additive mapping of already-loaded values — `create-grpc-contract` creates new contracts, doesn't teach this) · **TDD:** yes (the Fokus-side mapping is asserted)

Add 7 `[DataMember]` properties to `AnonymizedSprintSummary` (`src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/AnonymizedSprintSummary.cs`) and populate them in `MapToAnonymizedSummary` (`src/Services/Fokus/Fokus.API/Grpc/SprintAnalyticsGrpcService.cs`).

**Binding inputs:**
- **New fields (continue `[DataMember(Order = …)]` from the current max, which is `8` — `TopScopeAdditions`; use Orders 9–15). Additive only — do NOT renumber or reorder the existing 8, that is a wire-breaking change:**
  - `int SprintId` (Order 9)
  - `string SprintName` (Order 10)
  - `DateTime StartDate` (Order 11)
  - `DateTime EndDate` (Order 12)
  - `string CompletionRag` (Order 13)
  - `string DisruptionRag` (Order 14)
  - `string CarryOverRag` (Order 15)
- **Mapping source (all already in hand at `SprintAnalyticsGrpcService.cs:199-224` — no new load):**
  - `SprintId = selectedSprint.Id`, `SprintName = selectedSprint.Name`, `StartDate = selectedSprint.StartDate`, `EndDate = selectedSprint.EndDate` (the `Sprint` aggregate has all four — confirmed `Fokus.Domain/Sprint/Sprint.cs:5-7`).
  - `CompletionRag = health?.CompletionRag ?? "red"`, `DisruptionRag = health?.DisruptionRag ?? "red"`, `CarryOverRag = health?.CarryOverRag ?? "red"` — the `HealthScoreData` carries these three (computed at `SprintSummaryAnalyzer.cs:496-498`). Mirror the existing `HealthRag = health?.CompositeRag ?? "red"` null-fallback convention already on line 216.
- **DisruptionRag semantics for Rule 6 (pin so Reflekt's gate is correct):** `DisruptionRag` is the RAG of the **combined** disruption rate as the panel displays it. Note a known asymmetry to document, NOT to "fix" here: `AnonymizedSprintSummary.DisruptionRate` is `Scope + Bug` (line 213), whereas the `MetricRag` at `SprintSummaryAnalyzer.cs:497` is computed on `metrics.DisruptionRate` (the analyzer's own field). Map the RAG straight from `health.DisruptionRag` (the analyzer's value) — do not recompute it against the summed rate (Rule 3 forbids recomputation; the analyzer's RAG is Fokus's authoritative band). Document the rate-vs-RAG provenance in implementation.md so the reviewer doesn't read it as a mismatch.

**Acceptance:**
- `AnonymizedSprintSummary` has 15 `[DataMember]`s; Orders 1–8 byte-unchanged (grep the file: the existing 8 keep their Orders and names). New fields are Orders 9–15.
- TDD: a `SprintAnalyticsGrpcService.GetSprintSummaryAsync` test over a seeded closed sprint asserts the returned summary carries non-null `SprintName`, `StartDate`/`EndDate` matching the sprint, and `CompletionRag`/`DisruptionRag`/`CarryOverRag` equal to the `HealthScoreData` values (red→green path). Reuse/extend any existing grpc-service test fixture; if none exists for this service, the test is net-new (mark in implementation.md).
- `dotnet build SprintRituals.slnx` green.

### Step 2 — Register the `ISprintAnalyticsService` gRPC client in Reflekt
**Satisfies:** AC "the panel's Fokus data flows only over the `ISprintAnalyticsService` gRPC contract"; spec Rule 5/10.
**Skill:** Follow `create-grpc-contract` (the **Client** workflow only — contract + server already exist) · **TDD:** no (DI + config wiring)

Register the client so the endpoint (Step 4) can inject `ISprintAnalyticsService`. `Reflekt.API` already references `Blocks.AspNetCore` (the `AddCodeFirstGrpcClient` extension) and `Blocks.Contracts` (the contract) — no new project references, no cross-module-reference concern (the contract lives in shared `Blocks.Contracts`, ADR-001).

**Note (critic Low #2): this is Reflekt's *first* gRPC client** — there is no existing Reflekt client to copy. The `Grpc` config read into `GrpcServicesOptions` and the gRPC `using`s (`Blocks.AspNetCore.Grpc`, `Blocks.Contracts.Grpc`) are net-new to `Reflekt.API/DependencyInjection.cs`. Mirror the **Fokus** registration shape (`Fokus.API/DependencyInjection.cs:42-43` — `var grpcOptions = configuration.GetSection("Grpc").Get<GrpcServicesOptions>() ?? new GrpcServicesOptions();` then the `AddCodeFirstGrpcClient<…>` call). Project refs verified present → unblocked.

**Feature-specific inputs:**
- In `Reflekt.API/DependencyInjection.cs` `AddReflektModule` (mirror Fokus's registration line `Fokus.API/DependencyInjection.cs:43`): read the `Grpc` config section into `GrpcServicesOptions`, then `services.AddCodeFirstGrpcClient<ISprintAnalyticsService>(grpcOptions, "SprintAnalytics")`. The explicit `"SprintAnalytics"` key matches what the extension would auto-derive (`typeof(ISprintAnalyticsService).Name` strips "Service" → "SprintAnalytics") — pass it explicitly per the established Fokus/Identity convention.
- In `Host/appsettings.json`, add to the existing `Grpc:Services` block (currently `Invitation`, `Person`, both `http://localhost:5001`) a `"SprintAnalytics": { "Url": "http://localhost:5001" }` entry — same h2c Kestrel gRPC endpoint the Fokus server is mapped on (`Endpoints:Grpc`, port 5001). The Fokus `SprintAnalyticsGrpcService` is **already mapped server-side** (`Fokus.API/DependencyInjection.cs:115` — `app.MapGrpcService<SprintAnalyticsGrpcService>()`, verified at plan time), so no server-mapping change is needed — this step is client-side only.

**Acceptance:**
- `grep -rn "AddCodeFirstGrpcClient<ISprintAnalyticsService>" src/Services/Reflekt` → 1 hit.
- `Grpc:Services:SprintAnalytics:Url` present in `Host/appsettings.json`.
- `grep -rn "MapGrpcService<SprintAnalyticsGrpcService>" src/Services/Fokus` → 1 hit (already present at `DependencyInjection.cs:115` — verify unchanged, no new mapping).
- `dotnet build SprintRituals.slnx` green; the Host resolves `ISprintAnalyticsService` (no "Missing GrpcService config" at startup — the extension throws if the key is absent, so the config block is load-bearing).

### Step 3 — Add a Fokus-refs-eager-loading query to the retro repository
**Satisfies:** prerequisite for Step 4 (the endpoint needs the retro's `FokusSprintRefs`).
**Skill:** Follow `persistence-patterns` (repository query method) · **TDD:** no (thin EF query — covered transitively by Step 4's endpoint test)

The endpoint needs a `RetroSession` with its `FokusSprintRefs` loaded. The base `Repository<RetroSession>.GetByIdAsync` does **not** Include the refs (it calls `FindByIdAsync`, no eager load — confirmed `Repository.cs:16`), and `RetroSessionRepository` (class name; file is `Reflekt.Persistence/Repositories/SprintRepository.cs` — RA3 kept the filename, renamed the class) has only `GetByTeamIdAsync` / `GetMostRecentClosedByTeamIdAsync`.

**Feature-specific inputs:**
- Add `Task<RetroSession?> GetWithFokusSprintsAsync(int retroSessionId, CancellationToken ct)` — `Db.RetroSessions.Include(s => s.FokusSprintRefs).FirstOrDefaultAsync(s => s.Id == retroSessionId, ct)`. `FokusSprintRefs` is the `IReadOnlyList<FokusSprintRef>` navigation on `RetroSession` (`RetroSession.cs:28-29`).

**Acceptance:**
- Method returns the retro with `FokusSprintRefs` populated (non-lazy); returns `null` for an unknown id.
- `dotnet build` green. (Behavior verified via Step 4's endpoint test — no standalone repo test required.)

### Step 4 — `GET /api/retro-sessions/{retroSessionId}/sprint-context` endpoint
**Satisfies:** AC "returns 200 with the ordered per-sprint array for an existing retro, 404 for unknown id"; AC "Fokus unavailable → still 200, sections cached/unavailable, no blank panel"; spec Rules 2, 4, 5, 6, 7, 8, 9; Flows 1–4 (data half — the UI is RA5).
**Skill:** Follow `create-feature` (FastEndpoints slice) · **TDD:** yes (request/response, freshness states, ordering, Rule 6 gate, fallback all testable)

A browser-facing endpoint that assembles the per-retro sprint context. The browser never calls gRPC; this endpoint does (Rule 5/10).

**Feature-specific inputs:**
- **Route & auth:** `GET /api/retro-sessions/{retroSessionId}/sprint-context`, `[AllowAnonymous]` (open participation, ADR-021; guest or authenticated — data is anonymized so no resource gate; mirror `GetSprintSummaryEndpoint.cs:10`). Request `record SprintContextRequest(int RetroSessionId)`. Place under `Reflekt.API/Features/RetroSessions/GetSprintContext/` (two-file pattern per ADR-008: `GetSprintContextEndpoint.cs` + `GetSprintContextQuery.cs` for request/response/DTOs). **Route-family binding note:** the `/api/retro-sessions/{retroSessionId}` resource-noun form matches RA3's renamed *lifecycle* routes (e.g. `GET /api/teams/{teamId}/retro-sessions/{retroSessionId}`); it is NOT in the flat `/api/sprints/{sprintId}/*` DO-NOT-TOUCH family. This is a **new** route, so no rename/deferral tension — it simply adopts the target naming. (The spec proposed exactly this route; it's binding.)
- **Response shape:** `record SprintContextResponse(IReadOnlyList<SprintContextSection> Sprints)` where each `SprintContextSection` carries: `int SprintId`, `string SprintName`, `DateTime? StartDate`, `DateTime? EndDate` (nullable — absent on cached/unavailable, see PC3), `string Freshness` (`"live"`/`"cached"`/`"unavailable"`), and nullable metric block (`decimal? CompletionRate`, `CompletionRag`, `decimal? DisruptionRate`, `DisruptionRag`, `decimal? CarryOverRate`, `CarryOverRag`, `decimal? HealthScore`, `string? HealthRag`) plus `IReadOnlyList<ScopeAdditionDto>? AddedMidSprint` and `int? AddedMidSprintOverflow` (the "+N more" count). On `cached`/`unavailable` the metric block + scope additions are null; on `cached` `SprintName`/state come from the snapshot.
- **Assembly logic (what to accomplish — developer decides decomposition):**
  - Load the retro via Step 3's `GetWithFokusSprintsAsync`. **404** if null (Rule, AC).
  - For each `FokusSprintRef.FokusSprintId`: call `ISprintAnalyticsService.GetSprintSummaryAsync(new SprintQuery { SprintId = id })`. On success → `live` section from the summary (identity + metrics + RAGs from Step 1's enriched fields). On gRPC failure (catch the call, per-sprint — one sprint's failure never blanks the panel, Rule 9) → look up `FokusSprintSnapshotRepository.GetByIdAsync(id)`: snapshot present → `cached` (name/state, no metrics); absent → `unavailable` (id only, no metrics). **A gRPC failure is never surfaced as a non-200 status** (Rule 9, AC) — it degrades the section.
  - **Order sections earliest sprint start first** (Rule 2). `live` sections have `StartDate`; `cached`/`unavailable` have none. Ordering rule: order by `StartDate` ascending with null-dates (cached/unavailable) sorted last, tie-broken by `SprintId` ascending for determinism. Document this in implementation.md (the spec says "earliest sprint start first" but is silent on where date-less sections sort — this is the deterministic resolution).
  - **Rule 6 gate (scope additions):** include `AddedMidSprint` **only when** `DisruptionRag != "green"` (amber-or-worse — reuses Fokus's threshold, never recomputed). When included: take the summary's `TopScopeAdditions`, map **up to 3** (key + summary/title only — **never** an assignee; the contract's `ScopeAdditionEntry` already carries no assignee, Rule 4), ordered as Fokus returns them. **Resolution:** render in the contract's existing order and set `AddedMidSprintOverflow = max(0, TopScopeAdditions.Count - 3)` for the "+N more". (Below amber → block absent entirely.)
  - **Two known Fokus-side divergences to record in implementation.md (shared root cause — `BuildTopScopeAdditions`, `SprintAnalyticsGrpcService.cs:226-240`):**
    - **Rule 7 ordering** — `TopScopeAdditions` comes back ordered by **story-points descending** (`:231 OrderByDescending(StoryPoints)`), not "most-recently-added first"; the contract carries no added-date. Rendered in the contract's by-SP order. Closing it needs a Fokus-side ordering change + an added-date contract field — out of this pass's additive scope.
    - **`AddedMidSprintOverflow` under-counts (critic Low #1).** `BuildTopScopeAdditions` does `.Take(5)` (`:232`), so `TopScopeAdditions.Count` ≤ 5 and `AddedMidSprintOverflow = max(0, Count - 3)` caps at **"+2 more"** even when far more were added. Phrase the UI contract honestly as **"+N more among the top-5-by-SP"**, not "+N more of all additions". A true total-additions overflow needs Fokus to expose the full mid-sprint count (`DisruptionMetrics.MidSprintAdditionCount` already computes it, but on a different contract method) — RA5 follow-up if the panel needs a real total.
- **No session cache in this pass (Rule 8 is client-side, RA5):** Rule 8's "fetched once, cached for the session" is a client-side guarantee the RA5 panel implements (the spec leaves cache locus to the planner: "client or server"). With no UI yet, the endpoint is stateless — each call pulls fresh. Document that RA5 owns the session cache; the endpoint stays idempotent and side-effect-free so caching is purely a client concern. (Server-side per-session caching now would cache for a consumer that doesn't exist — deferred with the UI.)

**Acceptance:**
- TDD red→green: existing retro with 1 ref, Fokus reachable → 200, one `live` section with the rates + RAGs + health band + identity matching the stubbed `ISprintAnalyticsService` return. Unknown `retroSessionId` → 404.
- TDD: retro with 2 refs, both reachable → 200, two sections ordered by `StartDate` ascending.
- TDD: Fokus pull throws for a ref with a snapshot → that section is `cached` (name/state, null metrics), status still 200, other sections unaffected. Throws for a ref with no snapshot → `unavailable`, 200.
- TDD: a summary with `DisruptionRag = "amber"` and 5 `TopScopeAdditions` → section has `AddedMidSprint` (3 entries, no assignee field present) + `AddedMidSprintOverflow = 2`. A summary with `DisruptionRag = "green"` → `AddedMidSprint` null. Verify no assignee/developer field exists anywhere on the response type (Rule 4 — the response DTO must have no such property; assert structurally).
- `grep -rn "ISprintAnalyticsService" src/Services/Reflekt/Reflekt.API/Features` → the endpoint injects the contract; no `AnonymizedSprintSummary` field carrying per-developer data is read (none exists — the contract is the boundary).
- `dotnet build SprintRituals.slnx` green; `dotnet test` for Reflekt green.

### Step 5 — Update module docs + carry-over record
**Satisfies:** doc currency (ADR-019 cross-references).
**Skill:** None (doc edit) · **TDD:** no

Update `src/Services/Reflekt/CLAUDE.md`: the "Fokus linkage (ADR-017/019)" bullet's "`ISprintAnalyticsService` gRPC client … **RA4 — not yet landed**" moves to landed; the RA4 carry-over note ("add `FokusSprintSnapshot` data to the sprint-context panel via gRPC client") is now done server-side. Record the **RA5 carry-overs** explicitly:
- The Vue panel, top-bar button, `help.tooltips.md`, and the client-side session cache (Rule 8) are RA5 (the endpoint is their backend contract).
- PC3 deferral: `FokusSprintSnapshot` stays thin; `cached` sections show identity only, no last-known metric numbers — revisit if RA5 UX needs offline numbers.
- Rule 7 ordering gap: `AddedMidSprint` is ordered by story-points (Fokus's current order), not most-recently-added — closing it needs a Fokus-side ordering change + an added-date contract field.

Do **not** edit `docs/backlog.md` (owner-managed — the team lead touches it per the RA-pass close convention).

**Acceptance:** `src/Services/Reflekt/CLAUDE.md` describes the gRPC client + sprint-context endpoint as landed; the three RA5 carry-overs are recorded.

---

## Cross-Service Changes

- **Contract (`Blocks.Contracts`):** `AnonymizedSprintSummary` gains 7 additive `[DataMember]`s (Step 1). Additive-only — existing Orders 1–8 untouched, so no wire break for any current consumer (there is none yet besides the Fokus server that produces it). No `.proto` (code-first protobuf-net.Grpc).
- **Fokus server:** additive mapping in `SprintAnalyticsGrpcService` (Step 1); possibly one `MapGrpcService` line if `SprintAnalyticsGrpcService` isn't already mapped (Step 2).
- **Reflekt client:** new gRPC client registration (Step 2). No integration-event change (the `SprintSynced` consumer is RA3-landed and untouched).

## Migration Notes

None — no schema change (PC3).

## Testing Strategy

- **Step 1 (Fokus):** gRPC-service test asserts the enriched fields populate from a seeded closed sprint (identity + per-rate RAG).
- **Step 4 (Reflekt):** endpoint tests with a stubbed `ISprintAnalyticsService` cover the matrix: 1 ref live / 2 refs ordered / gRPC-throws-with-snapshot (cached) / gRPC-throws-no-snapshot (unavailable) / 404 unknown retro / Rule 6 gate on+off / scope-additions cap-at-3 + overflow / no-assignee-field structural assertion. Stub the gRPC client (inject a fake `ISprintAnalyticsService`) — no live Fokus needed.
- Privacy assertion (Rule 4 / AC) is structural: the response DTO type has no assignee/developer/per-developer property — assert against the type, and confirm the scope-addition DTO mirrors `ScopeAdditionEntry` (key + title + SP only).

## KB Impact

**None.** `docs/kb/health-score.md` already documents `MetricRag` (BR6/BR7) and `CompositeRag` (BR5) — this pass exposes existing computed values over the wire without changing the formulas, so no KB rule changes. (The BR6/BR7 "untested" note is a Fokus-side coverage item, not in this pass's scope — Step 1's test covers the *mapping*, not the `MetricRag` formula.)

## Open Questions

None open. Q1 (frontend home) resolved → backend-only RA4 (user-confirmed). PC1–PC3 are the planner calls, resolved above with reasoning; PC3 (the snapshot-cache fork) and the Rule 7 ordering divergence are flagged for the critic as the two places the plan consciously diverges from a literal spec reading, both justified by the deferred UI + additive-only scope.

---

## Risks & ordering

- **Step order is dependency-driven:** 1 (contract + server — the data the client consumes) → 2 (client registration) → 3 (repo query the endpoint needs) → 4 (endpoint, depends on 1+2+3) → 5 (docs). Steps 1 and 2 could be parallel, but 1-first keeps the contract real before the client registers against it.
- **Primary risk = the Rule 6 disruption-RAG provenance** (Step 1): mapping the RAG from the analyzer's `health.DisruptionRag` (authoritative) while the displayed `DisruptionRate` is the summed scope+bug rate. Pinned in Step 1 to map straight from the analyzer (never recompute) and documented so it's not read as a bug.
- **Second risk = the `cached`/`unavailable` fallback path** (Step 4): per-sprint exception isolation so one failed pull never blanks the panel (Rule 9). Pinned in the acceptance matrix.
- **No operator-owed fallback, no prompt-only LLM obligation, no live-credential step** — none of those rule-classes apply (in-process gRPC over a local h2c channel; tests stub the client).
- **Two conscious spec divergences, both flagged:** PC3 (`cached` shows no metric numbers) and Rule 7 (scope-additions ordered by SP not recency). Both stem from keeping scope additive + the UI deferred; both are cheap isolated follow-ups when RA5 lands a real consumer.
