# Critic Review — RR-B Plan vs Requirements (Mode 2)

**Slug:** adhoc-ReflektValueObjects
**Reviewed:** docs/specs/adhoc-ReflektValueObjects/delivery/plan.md
**Date:** 2026-06-20
**Recovered from critic transcript via salvage (critic writes no file by design; persisted by team-lead per Relay Contract).**

**Verdict: REVISE**

The plan is well-researched and most load-bearing claims check out against code (Fokus migration uses `RenameColumn`; the L5/`SetValues` hazard genuinely doesn't apply because `RetroSessionRepository` has no `SetValues`/`CurrentValues` path; the `Sprints/` dirs are empty; the EF config, ColumnColor VO, and the 4 wire DTOs are as described). But it has one **Critical** factual error that makes Step 4 incomplete and self-contradicting, and it would fail its own done-gate.

## Findings

**[CRITICAL] C1 — Step 4 misses three of six aggregate read sites; the wire-untouched claim is false.**
The plan's Step 4 enumerates exactly three read sites (Mapster, `GetRetroSummary`, `CreateRetro`) and Step 4/Step 8's *binding* grep asserts "**zero** `RetroSession`-aggregate property accesses" remain. The actual grep over `src/Services/Reflekt` (migrations excluded) shows **six** aggregate read sites. Three are unaccounted for:
- `Features/Reports/GetRetroReport/GetRetroReportEndpoint.cs:75-77` — `session.Column1Name`, `session.Column1Color.Value`, ×3 columns.
- `Features/Reports/GetParticipantReport/GetParticipantReportEndpoint.cs:75-77` — same.
- `Features/RetroSessions/UpdateColumns/BroadcastColumnConfigUpdatedHandler.cs:15-20` — `ev.RetroSession.Column1Name`/`.Color.Value`, ×3 columns.

Consequences: (a) After Steps 1–2 these three files stop compiling — build-green after Step 4 is unachievable as written. (b) The Step 4/Step 8 grep gate would *fail* (these are real aggregate hits, not wire-DTO declarations), so a developer following the plan literally is stuck. (c) The `BroadcastColumnConfigUpdatedHandler` feeds the SignalR `ColumnConfigUpdated` payload — the plan's "SignalR untouched / no wire change" claim (Cross-Service Changes, Scope) is **false**; that read site must be repointed to keep the SignalR payload byte-identical. Fix: add all three to Step 4, and correct the Cross-Service/Scope statements to "SignalR payload preserved by repointing the broadcast handler's read."

**[MINOR] C2 — Step 5 round-trip test should assert the default-row path, not only custom values.**
The `ComplexProperty` defaults (`HasDefaultValue`) are the riskier half of the migration (EF complex-type defaults + the `ColumnColor` conversion-inside-complex-builder are the medium-confidence points the plan itself flags). The round-trip test as specified saves *non-default* values. Add a case that persists a default-constructed session and asserts the defaults survive — that's what actually exercises the `HasDefaultValue` wiring on the renamed columns.

**[NIT] C3 — `RetroSession.cs:6` already has `private RetroSession() {}`.**
Open Question / Step 3 frames the EF parameterless ctor as a "maybe add if materialization needs it" for the *VOs*. Fine as written (it's about `RetroColumn`/`TimerDefaults`, not the aggregate), but worth noting the aggregate's own ctor already exists so there's no confusion.

## What's correct (verified, not just internally consistent)
- VO problem-class coverage: the scan-and-clear-the-rest framing (ADR-003) is present and recorded as a Step 7 deliverable. Good.
- Migration RENAME-not-drop: matches the Fokus precedent exactly (`RenameColumn` ×11 there). SQLite caveat correctly called out.
- L5/`SetValues` hazard: correctly ruled out, not silently skipped — `RetroSessionRepository` has no `SetValues`/`CurrentValues`/`Adapt`-onto-existing path (confirmed). The Pass-3b hazard at `AppSettingsRepository.cs:27-32` genuinely doesn't transfer.
- RR-E N1 fold-in: present (Step 6); both `Sprints/{Behaviors,Events}` dirs confirmed empty.
- Test coverage: VO construction/validation/equality + persistence round-trip both present (modulo C2).

**Net:** one real blocker (C1) — the read-site census is wrong by half and breaks two of the plan's own gates plus its wire-stability claim. Everything else is sound. Send back to fix Step 4 (add the 2 Reports endpoints + the Broadcast handler), correct the SignalR/Cross-Service claims, and strengthen the round-trip test (C2).
