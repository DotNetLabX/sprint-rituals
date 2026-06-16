# Critic Review (Mode 2: Plan vs Spec) — adhoc-SprintContextPanel

**Reviewer:** critic (critic-scp), persisted verbatim by team-lead (critic writes no file by design).
**Recovery note:** critic output stranded behind an idle notification; `TaskOutput` returned "no task found"; recovered via salvage-transcript.js from `subagents/agent-aa08a78d8f01e4029.jsonl`.
**Verdict:** ACCEPT (no blockers; 2 Low items to fold into implementation.md).

---

## Coverage matrix — spec requirement → plan step

| Spec item | Covered by | Status |
|---|---|---|
| Rule 1 (top-bar button, every phase, every participant) | — | **Deferred to RA5** (frontend) — out of backend scope by user decision |
| Rule 2 (one section per sprint, earliest-start-first) | Step 1 (identity fields) + Step 4 (ordering) | Covered |
| Rule 3 (completion/disruption/carry-over + health, never recompute; per-rate indicator color) | Step 1 (per-rate RAG mapping) + Step 4 (passthrough) | Covered |
| Rule 4 (anonymized only; scope entries key+title, no assignee) | Step 4 (`ScopeAdditionEntry` carries none) | Covered |
| Rule 5 (data over `ISprintAnalyticsService` gRPC; per-dev never crosses) | Step 2 (client) + Step 4 (endpoint pull) | Covered |
| Rule 6 (added-mid-sprint gate on amber-or-worse disruption band) | Step 1 (`DisruptionRag` exposed) + Step 4 (`!= "green"` gate) | Covered |
| Rule 7 (up to 3, most-recently-added first, "+N more") | Step 4 (cap 3 + overflow; **order divergence flagged**) | Partial |
| Rule 8 (fetch once, session-cached) | Step 4 (stateless; cache locus = RA5 client) | Covered (locus deferral spec-sanctioned) |
| Rule 9 (snapshot fallback `cached`/`unavailable`; one failure never blanks) | Step 4 (per-sprint try/catch + snapshot lookup) | Partial — PC3 |
| Rule 10 (no project reference; gRPC + local snapshot only) | Steps 2/4 (contract in `Blocks.Contracts`) | Covered |
| API: `GET …/sprint-context` 200/404, ordered array, gRPC failure → still 200 | Step 4 | Covered |
| Real-time: none | (n/a) | Covered |
| ACs 1–4, guest AC | — | **Deferred to RA5** (frontend ACs) |
| ACs 6,7 (endpoint 200/404; reopen no second pull) | Step 4 | Covered (AC7 reopen = client cache, RA5) |
| ACs 5,8 (no per-dev field; no project ref) | Step 4 structural assertion + Step 2 | Covered |

Every backend-relevant requirement maps to a concrete step with verified code anchors. Nothing in backend scope is silently dropped.

## Assessment of the two flagged divergences

**PC3 — `cached` shows identity only, no last-known metric numbers (diverges from Rule 9's "last-known values").** Acceptable within backend-only scope. The stored `FokusSprintSnapshot` contains no metrics (verified: `SprintId/SprintName/BoardId/SyncedAt/State`), and ADR-019 deliberately excludes them ("no rich metrics — those are RA4/gRPC", `FokusSprintSnapshot.cs:5`). Surfacing last-known numbers would require enriching the projection — a speculative metric-cache write path with no consumer (the UI is RA5). "No blank panel" guarantee holds. **Severity: Low (documented limitation, not a gap).** Required: recorded as explicit RA5 carry-over — Step 5 does exactly that.

**Rule 7 ordering — scope-additions by story-points, not most-recently-added.** A real partial miss but acceptable in additive scope. `ScopeAdditionEntry` carries `TicketKey/Summary/StoryPoints` — no added-date — and `BuildTopScopeAdditions` orders `OrderByDescending(StoryPoints).Take(5)` (`SprintAnalyticsGrpcService.cs:231`). "Most-recently-added first" is unsatisfiable from the wire today; closing it needs a Fokus-side ordering change + a new `AddedAt` contract field — outside this pass's additive-only envelope. Plan's resolution (render existing order, flag the gap) is the right backend call. **Severity: Low-Medium.**

- **Overflow under-count (Low):** `BuildTopScopeAdditions` does `.Take(5)`, so `TopScopeAdditions.Count` ≤ 5 — `AddedMidSprintOverflow = max(0, Count - 3)` reports at most "+2 more" even if 30 tickets were added mid-sprint. Pre-existing Fokus truncation, not introduced here, but the plan's "+N more" under-counts. State it as "+N more among the top-5-by-SP", or accept it as another facet of the same Fokus-side limitation. Flag in implementation.md alongside the ordering note.

## Other observations

- **Stale line citations (Trivial).** A few `file:line` anchors slightly off (snapshot at `Reflekt.Domain/FokusSprints/FokusSprintSnapshot.cs`); none point at wrong code — substance resolves.
- **Step 2 framing (Trivial).** Reflekt's `DependencyInjection.cs` registers zero gRPC clients today — this is the first. The `GrpcServicesOptions` read + gRPC `using`s are net-new; the registration *line* mirrors Fokus 42-43; project refs (Blocks.AspNetCore + Blocks.Contracts) verified present → unblocked.
- **DisruptionRate vs DisruptionRag provenance** — asymmetry real and verified (`DisruptionRate` summed at line 213; `DisruptionRag` from analyzer field at 497). Mapping straight from `health.DisruptionRag` is Rule-3-compliant. Well handled.
- **gRPC failure semantics** — Step 4 per-sprint catch → 200; existing endpoint uses `Send.OkAsync`/`Send.NotFoundAsync`. `IPersonService` best-effort catch+continue is the right model to copy.

## Verdict

**ACCEPT.** Plan covers every in-scope (backend) requirement with a concrete step anchored to verified code. PC1–PC3 sound; enrichment genuinely additive (max Order 8 confirmed, Orders 9–15 free); every cited symbol/repository method/config key/server mapping exists. Both consciously-flagged divergences acceptable within backend-only scope and correctly routed to RA5 (Step 5).

Two Low-severity items to fold into implementation.md (not blockers):
1. **`AddedMidSprintOverflow` under-counts** — `.Take(5)` cap means "+N more" is at most "+2" and is "among the top-5-by-SP", not the true remainder. Document with the Rule 7 ordering divergence (same root cause).
2. **Step 2 is Reflekt's first gRPC client** — `Grpc` config read into `GrpcServicesOptions` and gRPC `using`s are net-new to `Reflekt.API/DependencyInjection.cs`; registration line mirrors Fokus 42-43 but no existing Reflekt client to copy wholesale. Project refs in place (verified) → unblocked.
