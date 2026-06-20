# adhoc-ReflektFrontendMerge (RA5) — Summary

**Status:** ✅ Complete — all 4 sub-plans implemented, reviewed, and committed.
**Branch:** main · **Team mode:** Standard+Codex · **Commits:** per-sub-plan (5 total)
**Completed:** 2026-06-20

## What shipped

RA5 merged the Reflekt frontend into the monorepo under one routing shell, completed the deferred backend rename, built the RA4-deferred sprint-context panel, and closed two analytics carry-overs.

| Sub-plan | Scope | Review outcome | Commit |
|----------|-------|----------------|--------|
| **RA5-A** Merge & shell | Two Vite builds (`/dashboard/`, `/retro/`) under one Host routing shell; shared `/api/auth/me` helper; SignalR hub `/hubs/sprint`→`/hubs/retro` | Codex round-1 caught 3 client/server auth-route breaks the build hid → fixed; reviewer APPROVED cycle 2 | `3070445` |
| **RA5-C** Backend rename | Complete sprint→RetroSession rename (DB FK columns via RenameColumn migration, 26 routes + 9 folded-in team-scoped frontend calls, SignalR wire strings, JoinSprint*→JoinRetro*) + (user decision) full local-identifier cleanup incl. `Features/Sprints`→`Features/RetroSessions` | Codex round-1 caught `ColumnConfigUpdated` payload-key wire break → fixed; reviewer APPROVED cycle 2 | `4a3bc75` |
| **RA5-B** Panel UI | Sprint-context slide-out panel + button on RetroPhaseHeader; guest-aware fetch; RAG badges; added-mid-sprint overflow | Reviewer + Codex both caught a stale cross-retro cache bug (bare `loaded` not keyed by retroSessionId) → fixed; reviewer APPROVED cycle 2 | `d59da31` |
| **RA5-D** Rule 7 + true total | Most-recently-added ordering (AddedAt, never serialized); additive `ScopeAdditionTotal` (Order=16) over the same predicate; `MidSprintAdditionCount` case-bug fix; PC3 no-op | Reviewer APPROVED + Codex GO (all 6 analytics/privacy checks PASS), round 1 — no fixes needed | `669c7cf` |

## Acceptance / invariants verified
- **Carve-out held** end-to-end: Fokus-sprint semantics (`FokusSprint*`, `GetSprintContext*`/`sprint-context`, gRPC `ISprintAnalyticsService`, `SprintSynced`) never renamed.
- **Privacy (RA5-D):** `AddedAt` is an ordering key only — zero references in `Blocks.Contracts`; scope-addition DTOs stay 3-field (no timestamp/person).
- **Builds + tests green** at every sub-plan close: dotnet 0 errors; Fokus.API 6/6, Reflekt.Persistence 15/15, Fokus.Domain 62/62, Reflekt.Domain 7/7; both SPAs build clean.

## Value of the Codex cross-check
Codex caught **real runtime breaks the build + single-pass review missed** on RA5-A (3 auth-route mismatches) and RA5-C (the `ColumnConfigUpdated` payload-key break) — all confirmed against source before routing. On RA5-B it independently confirmed the reviewer's cache-keying bug; on RA5-D it agreed GO. The Standard+Codex mode paid for itself on the full-stack/analytics surfaces exactly as predicted.

## Follow-ups (tracked, out of RA5 scope)
1. **Google-guest-join 400** (RA5-C) — `joinWithGoogle()` repointed to `/join`, but `/join` requires a `DisplayName` the Google flow doesn't supply → 400. Pre-existing functional gap (authenticated-join wiring). Needs a proper authenticated-join path.
2. **RA5-D double-cap (LOW)** — Fokus caps the scope-additions list at 5, the assembler re-trims to 3 → 2 unused entries on the wire. Harmless; optional efficiency trim.
3. **RA5-A LOWs** — dashboard `authStore` cast fragility + dead `getMe()` orphan (post shared-extraction). Cleanup candidates.
4. **CLAUDE.md narrative** (Reflekt) — line ~16 "sprint" product prose is stale post-rename; doc-polish follow-up.

## Runtime / tooling issues (see communication-log.md Runtime Issues Log)
- Verify gate skipped on named subagents; ran (vacuously, 0 commands) on unnamed — done-checks re-ran builds throughout.
- Boundary-detector false-positives on custom-named agents (ownership writes) — distinguished by base role, not voided.
- **Codex file-write strands (all 3 runs)** — read-only sandbox blocks the write; verdicts recovered verbatim from the runtime job logs, never re-run.
- nexus:* subagent types went unavailable for new spawns mid-run — completed by reusing the live agents via SendMessage.
