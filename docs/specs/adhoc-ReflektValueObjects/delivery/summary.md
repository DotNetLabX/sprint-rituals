# adhoc-ReflektValueObjects — Summary

**Pass:** RR-B (Reflekt Refactor Program) · **Status:** Complete · **Date:** 2026-06-20
**Branch:** main · **Team mode:** Standard+Codex · **Plan review:** critic
**Commits:** `960813c` (plan) · `<impl>` (implementation)

## What shipped
ADR-007 value-object pass on the Reflekt module. The flat cohesive scalar clusters on the `RetroSession` aggregate were grouped into value objects, persisted via EF `ComplexProperty`; the wire contract stays flat (mirrors Fokus `GroupAppSettingsScalarsIntoVOs` / ADR-007 Q1=A).
- **`RetroColumn`** (Name + `ColumnColor`) ×3 — replaces `Column{1,2,3}Name` / `Column{1,2,3}Color`.
- **`TimerDefaults`** (Writing/Voting/Discussing) — replaces the 3 flat timer ints; absorbs `ValidateTimerRange` + `GetForPhase`.
- **Migration** `20260620174702_GroupRetroSessionColumnsAndTimersIntoVOs` — 9 data-preserving `RenameColumn` (Up/Down symmetric, no drop+add).
- **6 read sites** repointed; SignalR `ColumnConfigUpdated` payload keys (`column1Name`…`column3Color`) byte-identical — only value sources moved.
- **RR-E N1:** deleted the empty `Sprints/{Behaviors,Events}` leftover dirs (RA3-rename residue).

## Problem-class coverage (ADR-003)
All Reflekt aggregates scanned. Only `RetroSession` carried VO clusters; Card, CardGroup, Participant, Vote, Team, EvidenceLink, FokusSprintSnapshot are thin (no clusters) — recorded in `src/Services/Reflekt/CLAUDE.md`.

## Review trail
- **Plan critic (Mode 2):** REVISE → C1 (read-site census 3→6, incl. the SignalR broadcast handler — caught a false "wire untouched" claim), C2 (default-row round-trip test), C3 — all resolved before approval.
- **Step 1 done-check (architect):** PASS — all 8 steps verified against on-disk code (architect ran the authoritative grep itself).
- **Step 2 (reviewer):** APPROVED — no CRITICAL/HIGH.
- **Codex cross-check:** GO.
- **Post-review fix attempt:** the converged MEDIUM (`HasDefaultValue` store-side string) was attempted under a revert guard and **REVERTED** — EF rejects a string default on a `ColumnColor`-typed property when a `HasConversion` is present; `HasDefaultValueSql` would couple to SQLite and is worse. `ColumnColor.Create(...)` is the correct EF pattern. MEDIUM accepted as an EF-API constraint, not a defect. Config is byte-for-byte the reviewed/approved state.

## Verification
`dotnet build SprintRituals.slnx` → 0 errors. Domain.Tests **43/43**, Persistence.Tests **19/19** (incl. custom-values + default-row persistence round-trips).

## Deviations from plan
None. Step 6 dir-delete was performed by the team-lead (the subagent was denied the recursive force-delete; user-approved; dirs verified empty first).

## Follow-ups
- `HasDefaultValue` + `HasConversion` store-side-string form is not applicable here — documented, no action.
- Lessons (lessons.md): architect read-site census should come from one grep, not opened files (the C1 miss); a camelCase SignalR payload is a wire contract a PascalCase grep silently skips; recurring `group-scalars-into-value-object` skill gap (now seen in Fokus Pass-3b + Reflekt RR-B) with an improvement proposal. **Unprocessed** unless the learner is run.
- `docs/proposals/reflekt-refactor-triage.md` (RR0 deliverable) remains uncommitted — out of RR-B scope; left for separate handling.

## Files
**Created:** `RetroColumn.cs`, `TimerDefaults.cs`, `RetroColumnTests.cs`, `TimerDefaultsTests.cs`, `RetroSessionColumnTimerPersistenceTests.cs`, migration (`.cs` + `.Designer.cs`).
**Modified:** `RetroSession.cs`, `Behaviors/RetroSession.cs`, `RetroSessionConfiguration.cs`, `ReflektDbContextModelSnapshot.cs`, `RetroSessionMappings.cs`, `GetRetroSummaryEndpoint.cs`, `CreateRetroEndpoint.cs`, `GetRetroReportEndpoint.cs`, `GetParticipantReportEndpoint.cs`, `BroadcastColumnConfigUpdatedHandler.cs`, `RetroSessionCreateTests.cs`, `src/Services/Reflekt/CLAUDE.md`.
**Deleted:** `Reflekt.Domain/RetroSessions/../Sprints/{Behaviors,Events}` dirs.

## Runtime notes
Several benign tooling events handled (full detail in `communication-log.md` Runtime Issues Log): one transient API 529 on the architect's plan-write turn (re-dispatched, no loss); the verify-gate skipped (didn't recognize the custom spawn name — real verification came from done-check/reviewer/Codex); recurring boundary-detector false-positives from custom agent names editing their own owned artifacts (none voided).
