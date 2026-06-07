# Pass 3b — AppSettings VOs + Identity Encapsulation — Step 1 Done Check

**By:** architect
**Date:** 2026-06-07
**Inputs:** `plan.md` (7 steps, post-critic-cycle-1), `implementation.md`. Spot-verified against the live files for the two highest-risk areas (repository write funnel, migration).

## Verdict: PASS

Every plan step is Implemented or Deviated-with-valid-reason. No step Missing. Build recorded green. Both recorded deviations ruled VALID. Cleared to reviewer for Step 2.

## Pre-commitment Predictions (made before reading implementation.md)
1. **Repository write-funnel (HIGH-1 fix) most likely skipped** — it was a late critic-cycle-1 addition. → **Not skipped.** `AppSettingsRepository.cs:35-38` carries all 4 `existing.{VO} = settings.{VO}` re-assignments with the explanatory comment. Verified against the live file.
2. **A read-site left flat** (esp. the `settings2.` variant or a Settings endpoint). → **None left flat.** implementation.md enumerates all ~40 sites incl. `settings2.Xray.Enabled` (`SprintIssueSyncService.cs`); Verification 1 grep returned zero flat AppSettings reads.
3. **Migration emitted drop+add (data-loss on the seeded singleton).** → **Not realized.** `20260607075717_GroupAppSettingsScalarsIntoVOs.cs` uses `RenameColumn` for all 11 columns (verified against the live file), with a clean `Down` reversal. Singleton values (incl. `Xray_ClientSecret`) preserved.

All three predicted risks were handled correctly by the developer.

## Per-Step Disposition

| Step | Plan requirement | Disposition | Evidence |
|------|------------------|-------------|----------|
| 1 | 4 config VOs + AppSettings regroup + CreateDefault | **Implemented** | 4 VO files created; AppSettings.cs 11 scalars removed, 4 VO members `= new()`, CreateDefault updated (impl §Files Created, §Step 1) |
| 2 | ComplexProperty config + rename migration | **Implemented** | AppSettingsConfiguration ComplexProperty ×4, OwnsOne ToJson ×4 untouched, HasMaxLength(512); migration all-RenameColumn (verified live) |
| 3 | ~40 read/write sites → grouped paths | **Implemented** | Full file list (impl §Step 3); grep verified zero flat AppSettings reads (Verification 1) |
| 4 | Boundary mapping + repository write funnel | **Implemented** (+ valid deviation B) | GetSettings + 5 Save endpoints repointed; `SaveAsync` extended with 4 VO re-assignments (verified live `AppSettingsRepository.cs:35-38`) |
| 5 | Person/User encapsulation | **Deviated** (valid — deviation A) | Both files all `private set`; `private Person() {}` ctor; methods compile (verified live `Person.cs`, `Person/Behaviors/Person.cs`) |
| 6 | Fokus/CLAUDE.md VO list | **Implemented** | CLAUDE.md "Value objects" + "AppSettings singleton" updated to all 8 VOs + wire-flat note (verified live) |
| 7 | Verification sweep + build | **Implemented** | All 7 checks recorded with results; `dotnet build SprintRituals.slnx` green, 0 errors |

**No step Missing. No unexpected files / scope creep** — every modified file maps to a plan step; `client/` untouched (Verification 3); Fokus sync aggregates untouched (out-of-scope honored).

## Deviation Rulings

### Deviation A — `required` dropped from `Person.FirstName/LastName/Email` (the deviation flagged for ruling)
**Recorded reason:** C# CS9032 forbids `required` + `private set`; invariant preserved via `private Person() {}` ctor + `private set`.
**Ruling: VALID — PASS.**
- The compile constraint is real (CS9032 — a `required` member's setter must be at least as accessible as the property; `private set` on a `public required` property does not compile). The plan's "keep `required`" instruction rested on **my incorrect assumption** (in the plan) that `required` + `private set` would compile. The infeasibility is a plan error, not a developer error.
- The **encapsulation invariant ADR-007 cares about is preserved**: with `private Person()` + all `private set`, external code can neither `new Person { ... }` nor assign any property. The sole construction path is the `CreateFromGoogle` factory. `required` was a redundant compile-time guard on top of an already-private construction surface — not the encapsulation mechanism. Dropping it does not weaken the invariant.
- Default empty-string initializers (`= string.Empty`) keep the non-null contract for the required-ish members; the factory always sets real values. No behavior change.
- This does NOT contradict any user decision: Q3 said "private set/init + EF ctor; mutation only via factory/behavior methods." The implementation satisfies Q3 exactly (private set + EF ctor + factory-only mutation). `required` was never a user-stated requirement — it was a plan implementation note.

### Deviation B — explicit assignment instead of `IRegister` + `req.Adapt(existing)` for the 3 `Adapt` Save endpoints
**Recorded reason:** Mapster adapting onto a tracked EF entity with `ComplexProperty` members risks silent change-tracking bypass; explicit assignment is structurally equivalent and matches the existing pattern.
**Ruling: VALID — PASS.** Step 4 explicitly offered "a Mapster `IRegister` config **(or per-endpoint `TypeAdapterConfig`)**" and the step goal is "map flat→grouped at the boundary, wire stays flat." Explicit assignment achieves the identical result, avoids the same change-tracking hazard class as critic HIGH-1, and is consistent with the two Save endpoints that already used explicit assignment. Within the step's latitude — reasonable engineering judgment, not a scope or contract change.

## Scope Compliance
- **Wire flat (Q1=A):** `client/` diff empty; `GetSettingsResponse` + 5 Save command/response records unchanged. ✓
- **VO names (Q2):** `BugRatioConfig`/`XrayConfig`/`CycleTimeConfig`/`SyncConfig`, `DefaultSpPerBug` in `BugRatio`. ✓
- **Identity in 3b (Q3):** Person/User encapsulated, factory/behavior-only mutation. ✓
- **ComplexProperty (Q4):** 4 new VOs `ComplexProperty`; 4 existing JSON-owned untouched. ✓
- **Fokus sync aggregates anemic (Pass-3 Q3):** untouched — no encapsulation added. ✓

## Handoff
Step 1 passed → reviewer for Step 2 (code review). The reviewer should independently re-run the build (the done check does not build — it reads the recorded green result) and pay attention to: (1) behavioral parity of the ~40 rewritten read sites (only member path should change), (2) the detached-`settings` round-trip behavior of `SaveAsync` (HIGH-1 acceptance), (3) the `ComplexProperty` migration column defaults vs the VO initializers per `ef-core.md`.
