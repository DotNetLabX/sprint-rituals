# adhoc-Pass5-EventCorrectness — Review

## Step 1 — Done-Check

**Verdict: PASS**

**Reviewer:** Architect
**Date:** 2026-06-09

### Step Disposition Table

| Step | Description | Disposition | Evidence |
|------|-------------|-------------|----------|
| 1 | Annotate `UserCreated` as deferred-infra placeholder (B2) | Implemented | XML doc comment added; `sealed record UserCreated(int UserId) : IDomainEvent` shape unchanged; grep confirms 0 raise sites across `src/` |
| 2 | Remove `HasDefaultValue("Manager")` from `UserConfiguration.cs` (B6) | Implemented | Line 22: `builder.Property(u => u.Role).IsRequired().HasMaxLength(32)` — no `HasDefaultValue`; line 23 `IsActive` default untouched |
| 3 | EF migration `RemoveUserRoleDbDefault` (B6) | Deviated — valid | Migration `Up` drops `defaultValue: "Manager"`, `Down` restores, snapshot updated. Deviation: `-s Identity.Persistence` used instead of plan's `-s Identity.API` — `Identity.API` lacks `Microsoft.EntityFrameworkCore.Design`; `Identity.Persistence` has both the Design package and `DesignTimeDbContextFactory`, which is the correct workaround for class-library API projects. Migration output is identical. |
| 4 | Fix `RepositoryBase.UpsertAsync` backing-field copy (B7) | Implemented | `SetValues(DbContext.Entry(entity).CurrentValues)` — EF `PropertyValues` overload respects `PropertyAccessMode.Field`; insert branch, method signature, and all other members unchanged; in-line comment names zero-caller reality and rationale; compile-green + code-reading verification (correct — no production callers exist) |
| 5 | B7 guardrail in `docs/conventions/ef-core.md` | Implemented | "Repository Upsert Pattern" section covers all three required elements: (a) live `public new` explicit-copy pattern as current rule (names all four repos), (b) forward-looking `PropertyAccessMode` condition, (c) verification reality (compile + code reading, not a sync re-run) |

### Notes

- **Step 3 deviation** is architecturally sound. The `DesignTimeDbContextFactory` exists precisely for this pattern — the plan's migration command example in `project-rules.md` assumes the startup project holds `Microsoft.EntityFrameworkCore.Design`, which is true for Fokus (class-library API with Design in Persistence, but Fokus.API may behave differently) but not for Identity. The developer correctly flagged this as a carry-over finding (low severity) for the reviewer to note the `project-rules.md` example may need a clarifying note.
- **Step 4 verification** is honest and correct per the plan's Accept criterion #4: build green + code reading, explicitly not a sync re-run (which cannot reach the base method). No positive runtime evidence is claimed.
- **B1 (PersonCreated dispatch correct)** — handled as context/scope documentation in the plan, not a numbered step. Confirmed no code change was made to the dispatch path; correct per Q1 locked decision.

---

## Step 2 — Code Review

**Verdict: APPROVED**
**Reviewer:** Reviewer
**Date:** 2026-06-09
**Cycle:** 1/3

### Evidence Table

| # | Severity | File:Line | Finding | Resolution |
|---|----------|-----------|---------|------------|
| — | — | — | No CRITICAL or HIGH findings | — |

### Dimension Review

**1. Plan conformance**
All 5 steps match the plan exactly. The Step 3 deviation (`-s Identity.Persistence` instead of `-s Identity.API`) is correctly documented and architecturally sound — `Identity.API` lacks `Microsoft.EntityFrameworkCore.Design`; `DesignTimeDbContextFactory` in Persistence is the proper workaround. Migration output is correct.

**2. Correctness**

- **Step 1 (UserCreated annotation):** Doc comment content is accurate. Record shape `sealed record UserCreated(int UserId) : IDomainEvent` is unchanged. The `see cref` to `Blocks.Domain.Entities.Entity{TKey}` is correct. Zero raise sites confirmed. No behavior change.

- **Step 2 (UserConfiguration):** `HasDefaultValue("Manager")` correctly removed from `Role` at line 22. `IsActive`'s `HasDefaultValue(true)` at line 23 is untouched per the explicit carve-out. `User.CreateFromPerson` factory default is now the single source of truth for Role.

- **Step 3 (migration):** `Up` drops `oldDefaultValue: "Manager"` with `maxLength: 32, nullable: false` preserved. `Down` restores `defaultValue: "Manager"`. Snapshot `Role` entry at lines 105–108 shows `IsRequired().HasMaxLength(32)` with no `HasDefaultValue` — consistent with the config change. Only `Role` is touched; `IsActive` snapshot at line 94 is untouched. Migration is a pure column-default drop; existing rows unaffected.

- **Step 4 (RepositoryBase.UpsertAsync):** `DbContext.Entry(entity).CurrentValues` is called on a detached incoming entity. EF's `Entry()` does not attach/track the entity — it wraps it and returns an `EntityEntry` with `State = Detached`. Reading `CurrentValues` on a detached entry reads scalar property values through EF's configured property access mode (including `PropertyAccessMode.Field` for backing-field-mapped properties) without modifying tracking state. `SetValues(PropertyValues source)` then copies those values into `existing`'s `CurrentValues` — `existing` is already tracked (returned by `FindByIdAsync`). No duplicate-tracking conflict can arise because `Entry()` does not change `entity`'s tracking state. Insert branch (`entity.Id` is non-default when reaching the update branch; any default-Id entity takes the insert path via `FindByIdAsync` returning null), method signature, and all other members are unchanged. Comment is accurate and names the zero-caller reality.

- **Step 5 (ef-core.md guardrail):** All three required elements present: (a) live `public new` explicit-copy pattern naming all four repos, (b) forward-looking `PropertyAccessMode` condition, (c) verification reality. The `SetValues(entity)` anti-pattern is explicitly called out. Content is accurate and actionable.

**3. Security** — No security-relevant changes. No auth, no secrets, no injection surface.

**4. Performance** — `DbContext.Entry(entity).CurrentValues` on a detached POCO is a synchronous, in-memory read — no extra DB query introduced. No performance concern.

**5. Conventions** — All files follow project conventions: XML doc comment uses `/// <summary>` form; C# `sealed record` shape unchanged; in-line comments in RepositoryBase use accurate terminology; ef-core.md follows the existing section-heading style.

**6. Tests** — No automated suite for this app (accepted). Build is green. Verification is compile-green + code reading per the plan's accepted testing strategy.

### Follow-up Items (non-blocking)

| # | Severity | Item |
|---|----------|------|
| F1 | LOW | `docs/conventions/project-rules.md` line 67: migration command `-s Services/{Svc}/{Svc}.API` should note that the startup project must carry `Microsoft.EntityFrameworkCore.Design`, or that `{Svc}.Persistence` can be used as startup when it has `IDesignTimeDbContextFactory`. The Identity deviation exposes this gap. Recommend a one-line note added in a future cleanup pass. |
