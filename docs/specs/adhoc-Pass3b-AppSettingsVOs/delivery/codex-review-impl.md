# Pass 3b — Codex Implementation Review

## Review Scope

Independent review of Pass 3b (`adhoc-Pass3b-AppSettingsVOs`) against the current workspace diff plus `docs/specs/adhoc-Pass3b-AppSettingsVOs/delivery/plan.md` and `implementation.md`, limited to: behavioral parity of rewritten read sites, `AppSettingsRepository.SaveAsync` persistence for the 4 new VOs including the detached path, EF migration/schema parity for the new VO columns, and Identity encapsulation for `Person`/`User`.

## Findings

### [BLOCKING] `User` still has an external construction path — factory is not the sole creation path

- **File:** `src/Services/Identity/Identity.Domain/Users/User.cs` (`User` type); `src/Services/Identity/Identity.Domain/Users/Behaviors/User.cs` (`CreateFromPerson`)
- **Evidence:** `User.cs` tightens property setters but declares no constructor. In C#, that leaves an implicit public parameterless constructor. `CreateFromPerson` in `Users/Behaviors/User.cs` is therefore not the only public creation path.
- **Risk:** Breaks the pass's hard encapsulation invariant. External code can call `new User()` with `PersonId = 0` and `RegistrationDate = default`, then invoke public behavior methods on an invalid instance.
- **Recommendation:** Add `private User() { }` so EF can still materialize the entity while `CreateFromPerson` remains the sole public construction path.

---

### [WARNING] `Sync_PlanningWindowDays` store default remains `0` after the VO rename migration

- **File:** `src/Services/Fokus/Fokus.Persistence/Configurations/AppSettingsConfiguration.cs` (`Configure`); `src/Services/Fokus/Fokus.Persistence/Migrations/20260509203929_AddPlanningWindowDays.cs` (`Up`); `src/Services/Fokus/Fokus.Persistence/Migrations/20260607075717_GroupAppSettingsScalarsIntoVOs.cs` (`Up`)
- **Evidence:** `Configure` now sets `HasDefaultValue(2)` for `Sync.PlanningWindowDays`. The original column was created with `defaultValue: 0` in `AddPlanningWindowDays.cs`. The new VO migration only renames the column; it does not alter or rebuild the column default.
- **Risk:** Upgraded databases, and fresh databases created by replaying the migration chain, keep a store default of `0` while the domain model and VO initializer say `2`. Current singleton seeding writes the value explicitly so this is fragile rather than immediately breaking, but the schema contract is wrong.
- **Recommendation:** Alter or rebuild the column in the VO migration so `Sync_PlanningWindowDays` gets store default `2`, or remove `HasDefaultValue(2)` from the model if database defaults are intentionally not part of the contract.

---

## Summary Table

| # | Severity | Area | Title | Verdict |
|---|----------|------|-------|---------|
| 1 | BLOCKING | Identity encapsulation | `User` implicit public ctor bypasses factory | Must fix |
| 2 | WARNING | EF migration | `Sync_PlanningWindowDays` store default drift (0 vs 2) | Fix while pass is open |

No additional findings: read-site rewrites were behavior-preserving in all reviewed diffs; `AppSettingsRepository.SaveAsync` contains all 4 VO re-assignments including the detached path; `Person` encapsulation is correctly closed.

## Final Verdict

**NO-GO** — fix the `User` constructor gap before merging; the `PlanningWindowDays` store-default drift should also be corrected while this pass is open.

---

## Cycle 2 Verification

1. **BLOCKING fix — CONFIRMED.**
   `src/Services/Identity/Identity.Domain/Users/User.cs` now declares `private User() { }`, so `User` no longer has an implicit public parameterless constructor. The factory in `src/Services/Identity/Identity.Domain/Users/Behaviors/User.cs` remains the sole public construction path.

2. **WARNING fix — CONFIRMED.**
   `src/Services/Fokus/Fokus.Persistence/Configurations/AppSettingsConfiguration.cs` still declares `HasDefaultValue(2)` for `Sync.PlanningWindowDays`, and the VO migration now resolves the previous drift by altering the renamed column default to `2` in `src/Services/Fokus/Fokus.Persistence/Migrations/20260607075717_GroupAppSettingsScalarsIntoVOs.cs` with a matching down-path restore.

## Final Verdict

**GO** — both Cycle 2 verification items are fixed correctly. No further blocking issues.
