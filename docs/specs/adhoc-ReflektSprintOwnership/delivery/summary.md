# adhoc-ReflektSprintOwnership (RA3) — Summary

**Status:** COMPLETE — 2026-06-15
**Branch:** main
**Team mode:** Standard + Codex · **Review mode:** critic (code-grounded)
**Commits:** `6c5957e` (plan) · `<impl>` (implementation) — 2-commit default

## What shipped

RA3 of the Reflekt Integration Program — Reflekt's retro board now references Fokus-owned sprints instead of standing alone, per ADR-017/018/019.

| Step | Delivered |
|------|-----------|
| 1 | `Sprint` → `RetroSession` rename across domain/persistence/API (41 files) + retro-state types (`SprintState`→`RetroSessionState`, `SprintPhase`→`RetroPhase`, `SprintAction`/`ISprintAction`→`RetroAction`/`IRetroAction`). `SprintHub` class name + child FK column names preserved. |
| 2 | `FokusSprintSnapshot` read-projection + `FokusSprintRef` child link entity (Card→EvidenceLink precedent, not EF OwnsMany) + `FokusSprintSnapshotRepository` (explicit `Update()` upsert to avoid `SetValues` shallow-copy) + TDD tests. |
| 3 | `CreateRetroSession` endpoint — `POST /api/teams/{teamId}/retro-sessions`; required `FokusSprintIds: IReadOnlyList<int>` (≥1, validator + domain guard). |
| 4 | `RefreshFokusSprintSnapshotOnSprintSyncedConsumer` on a single shared MassTransit bus — `AddMassTransit` moved to `Host/Program.cs`; each module exposes `AddXxxMessaging(IBusRegistrationConfigurator)`. No second bus, no cross-module project reference. |
| 5 | 8 team-scoped lifecycle routes renamed `/api/teams/{teamId}/retro-sessions/…` (incl. `SetReviewStatus`, the critic's CRITICAL catch); 26 flat `/api/sprints/{sprintId}/…` routes preserved; SignalR method/group/claim strings preserved (RA5 carve-out). |
| 6 | Migration `20260615212500_RenameSprintToRetroSessionAddFokusLink` — creates `RetroSessions`/`FokusSprintSnapshots`/`FokusSprintRefs`; child FK columns confirmed unchanged via PRAGMA introspection; FK constraints re-added (see fix below). |
| 7 | `Reflekt/CLAUDE.md` currency; RA4/RA5 carry-overs documented. |

## Locked decisions (all user-confirmed 2026-06-15)
- **Q1=b** — rename aggregate + retro-state types; keep child FK columns `SprintId`-named (FK-column rename deferred to RA5).
- **Q2** — rename HTTP routes/JSON freely; preserve SignalR strings (RA5 carve-out).
- **Q3=b** — keep creation, require ≥1 `FokusSprintId` ("standalone" = untied-to-a-Fokus-sprint is the dropped capability).
- **Q4=a** — event-only, forward-only snapshot (5 `SprintSynced` fields); no gRPC client (RA4's scope).
- **Q5** — disposable dev `reflekt.db`, inherit RA2 stance.

## Review outcome
- **Critic (code-grounded, plan):** REJECT → 1 CRITICAL (`SetReviewStatus` 9th route omitted) + 1 HIGH (`Card.ReviewedInSprintId` 4th FK missing from carve-out) + 1 MEDIUM + 1 LOW. All folded; plan re-approved.
- **Step-1 done-check (architect):** PASS, 7/7, 0 Missing. All three pre-commitment predictions clean.
- **Step-2 review:** Reviewer REQUEST CHANGES → APPROVE after 1 fix cycle. **Codex (independent) converged on the same defect** and escalated its framing.
- **The one implementation defect:** two `Card→RetroSession` FK configs in `CardConfiguration.cs` were swallowed into `//` comments by a literal `\r\n` (lines 18/32) — silently disabling both relationships and dropping the FK constraints in the migration. Caught independently by both reviewers; fixed (lines restored, migration re-scaffolded as a delta adding both constraints, `has-pending-model-changes` clean). Runtime impact today was nil; a latent correctness defect for RA5.

## Evidence
- Build: 0 errors, 15 warnings (all pre-existing). Tests: 77 passed, 0 failed.
- `dotnet ef migrations has-pending-model-changes` → "No changes" (model/snapshot agree, FKs present).

## Carry-overs
- **RA4** (now unblocked, needs PO spec): `ISprintAnalyticsService` gRPC client + rich `AnonymizedSprintSummary` metrics + the anonymized sprint-context panel. RA3 snapshots are thin (event-only) until RA4's gRPC pull lands — a referenced sprint never re-synced shows a thin/absent snapshot.
- **RA5** (now unblocked): FK-column renames (`SprintId`→`RetroSessionId`), the flat `/api/sprints/{sprintId}` route family, and SignalR method/group/claim string renames were all deliberately deferred here.
- **Skill gap (lessons):** `add-integration-event` lacks consumer-side / shared-bus composition coverage — surfaced by the Step-4 single-bus topology work.

## Runtime issue (recorded in communication-log.md)
The `codex:codex-rescue` round-1 cross-check completed its analysis but was cut off at the file-write step; `review-codex.md` never landed via the wrapper. Findings were salvaged from the Codex runtime job log (`task-mqfpg5w0-nmghg1.log`) and persisted with provenance — no re-run needed (analysis complete, corroborated the reviewer).
