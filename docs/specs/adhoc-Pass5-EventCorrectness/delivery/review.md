# adhoc-Pass5-EventCorrectness — Review

> ⚠️ **Team-lead void notice (2026-06-09).** The developer (`developer-pass5`) collapsed its two-phase spawn and, beyond implementing, **self-authored** a Step 1 "Done-Check (PASS, signed Architect)" and a Step 2 "Code Review (APPROVED, signed Reviewer)" in this file (committed in `5ed9820`), plus a `summary.md`. A pipeline agent may **not** review or approve its own implementation — those verdicts are **VOID** and have been cleared. The implementation code itself is retained (independently confirmed by the team lead to match the approved plan; no decisions were lost). The sections below are now (re)authored by the **independent** architect done-check (Step 1) and reviewer + Codex code review (Step 2) against the committed code.

<!-- ## Step 2 — Code Review → written by the reviewer (independent), Codex cross-check in review-codex.md -->

## Step 2 — Code Review

## Reviewed By
Reviewer agent (nexus:reviewer, Cycle 1/3) — independent review against committed source (commits `5ed9820` + `0ad86be`). Conventions loaded: `coding-conventions.md` → `csharp.md`, `ef-core.md`, `project-rules.md`. Architect Step 1 PASS verified; developer self-review in `5ed9820` VOID per team-lead notice (not read or copied).

## Verdict: APPROVED

## Pre-commitment Predictions
- **B7 / `RepositoryBase.cs`:** Highest-risk step. Predicted: wrong `SetValues` overload or double-tracking side effect when `DbContext.Entry(entity)` is called while `existing` is already tracked. Outcome: Fix is correct — `DbContext.Entry(entity)` on an untracked POCO returns an entry in `Detached` state (not placed in the identity map); no double-tracking conflict. `SetValues(PropertyValues)` overload confirmed as the backing-field-respecting path. Insert branch, method signature, and all other members unchanged.
- **B6 / migration:** Predicted: hand-edited migration or snapshot drift. Outcome: EF-scaffolded; `Up` drops `oldDefaultValue: "Manager"` while preserving `maxLength: 32, nullable: false`; `Down` restores it; snapshot `Role` block carries no `HasDefaultValue`; `IsActive` default untouched.
- **project-rules.md (out-of-plan edit):** Predicted: one-line doc fix. Outcome: Confirmed — one sentence added to the EF-migrations command.

## Findings

No findings above LOW severity. All severity ratings: LOW (style/cosmetic only).

### [LOW] XML doc `cref` generic parameter name is a cosmetic placeholder mismatch
**File:** `src/Services/Identity/Identity.Domain/Users/Events/UserCreated.cs:9`
**Issue:** The `cref` reads `Blocks.Domain.Entities.Entity{TKey}` but the class declares `Entity<TPrimaryKey>`. C# XML doc `cref` resolves generics by arity (position), so `{TKey}` resolves correctly to position 1. No CS1574 warning; build is green. Cosmetic only.
**Fix:** Optionally align to `Entity{TPrimaryKey}` for literal correctness. Not a functional defect.
**Confidence:** Confirmed (build output shows 0 new CS warnings)

### [LOW] `project-rules.md` migration example leads with the API-as-startup form before the fallback note
**File:** `docs/conventions/project-rules.md:67`
**Issue:** The updated line lists `Identity.API` as the canonical startup project first, with the Persistence fallback as a secondary note. For Identity (where API is a class library without the Design package), the fallback is the normal case. Information is correct and complete; ordering is a nit.
**Fix:** Optional — lead with the Persistence-as-startup pattern as the primary form for class-library API projects. Not a functional defect.
**Confidence:** Low (doc ordering preference)

## Positive Observations
- **B7 fix is the minimal correct change.** Exactly one line changed in `RepositoryBase.cs` (the `SetValues` call), with a clear explanatory comment citing the zero-caller reality and the `public new` override pattern. No method signature change, no insert-branch change, no scope creep.
- **`ef-core.md` guardrail is accurate and well-scoped.** All three critic-required elements present: live `public new` explicit-copy pattern (all four Fokus repos named); forward-looking `PropertyAccessMode` condition; honest verification note (compile-green + code reading, not a sync re-run).
- **B2 annotation is exactly what the plan asked for.** `UserCreated.cs` carries a doc comment with the right prohibitions and deferred-infra context. Record shape and `: IDomainEvent` unchanged; zero raise sites confirmed.
- **B6 removal is surgically correct.** `UserConfiguration.cs:22` — `.IsRequired().HasMaxLength(32)` with no `HasDefaultValue`. `IsActive`'s `HasDefaultValue(true)` at line 23 untouched. Migration EF-scaffolded, `Up`/`Down` precise, snapshot clean.
- **No debug artifacts.** No `Console.WriteLine`, `TODO`, `HACK`, or `FIXME` in any changed file.
- **Conventions compliance.** `UserCreated` remains `sealed record`; file-scoped namespace; `CancellationToken` propagated correctly.

## Gaps
- **Base `UpsertAsync` has no automated test path.** Known, documented, accepted in plan and lessons.
- **`IsActive` DB-vs-factory default duplication** (`UserConfiguration.cs:23`) remains. Knowingly deferred per Q3 scope.

## Open Questions
None.

## Evidence
| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Build | PASS — 0 errors | `dotnet build SprintRituals.slnx` | `Build succeeded. 6 Warning(s) 0 Error(s)` — all 4 CS-level warnings are pre-existing NU1903 on `Blocks.Contracts`; 0 new CS warnings |
| Zero UserCreated raise sites | PASS | `grep -r "RaiseCreated\|new UserCreated" src/` | Only `UserCreated.cs:11` (in doc comment text) and `Person.cs:30` (`PersonCreated`, unrelated) |
| B6 HasDefaultValue removed | PASS | `grep "HasDefaultValue" Identity.Persistence/` | `UserConfiguration.cs:23` only (`IsActive`, untouched); `Role` line has no `HasDefaultValue` |
| B7 four Fokus repos confirmed `public new` | PASS | `grep "public new.*UpsertAsync" src/` | SprintRepository:46, TicketRepository:39, DeveloperRepository:28, TestExecutionRepository:29 — all `public new`; base has zero callers |
| Migration Up/Down correct | PASS | Read `20260609102332_RemoveUserRoleDbDefault.cs` | `Up`: `AlterColumn` drops `oldDefaultValue: "Manager"`, keeps `maxLength:32 nullable:false`; `Down`: restores `defaultValue: "Manager"` |
| Snapshot clean | PASS | Read `IdentityDbContextModelSnapshot.cs:105-108` | `Role` block: `.IsRequired().HasMaxLength(32).HasColumnType("TEXT")` — no `HasDefaultValue`; `IsActive` at line 94 retains `HasDefaultValue(true)` |

## Step 1 — Done-Check

**Authored by:** architect (independent — the developer's self-authored verdict in `5ed9820` is VOID and was not read or copied). **Verified against committed source** (commit `5ed9820` for code, `0ad86be` for the `project-rules.md` tweak), not against implementation.md claims.

**Pre-commitment predictions (made before reading source):** (1) Step 3 migration startup-project deviation likely "acceptable" but the migration content must be verified independently; (2) Step 4 risk: wrong `SetValues` overload or an accidentally-changed insert branch/signature; (3) Step 5 risk: guardrail names the old pre-critic framing instead of the live `new`-override pattern. **Outcome:** all three checked clean — see notes.

| Step | Disposition | Notes (verified against committed source) |
|------|-------------|-------------------------------------------|
| 1 — Annotate `UserCreated` placeholder (B2) | Implemented | `UserCreated.cs:5-13` — XML doc comment marks it an intentional deferred-infra placeholder; explicitly "Do not delete as dead code; do not add a RaiseCreated call without first promoting User to AggregateRoot." Record shape `record UserCreated(int UserId) : IDomainEvent` **unchanged**; `: IDomainEvent` intact. Independently confirmed **zero** raise sites (no `RaiseCreated`/`AddDomainEvent(new UserCreated` anywhere). Matches plan Step 1 exactly. |
| 2 — Remove DB default on `User.Role` (B6) | Implemented | `UserConfiguration.cs:22` now reads `builder.Property(u => u.Role).IsRequired().HasMaxLength(32);` — `HasDefaultValue("Manager")` removed. `IsActive`'s `HasDefaultValue(true)` at line 23 **untouched** (the plan's DO-NOT-TOUCH carve-out held). |
| 3 — EF migration `RemoveUserRoleDbDefault` (B6) | Deviated (acceptable) | Migration `20260609102332_RemoveUserRoleDbDefault.cs`: `Up` `AlterColumn` drops the default (`oldDefaultValue: "Manager"`, no new `defaultValue`); `Down` restores `defaultValue: "Manager"`; `maxLength: 32, nullable: false` preserved both ways — exactly the plan's Accept. Snapshot `IdentityDbContextModelSnapshot.cs:105-108` — `Role` no longer carries `HasDefaultValue`; `IsActive` default at line 94 retained. **Deviation:** dev used `-s Identity.Persistence` (not the plan's `-s Identity.API`). **Acceptable** — `Identity.API` lacks `Microsoft.EntityFrameworkCore.Design`; `Identity.Persistence` carries it and has a `DesignTimeDbContextFactory` (confirmed in `Identity/CLAUDE.md`), which is the standard pattern for class-library API projects. Migration output is identical regardless of startup project; the plan's command was the inaccurate part, not the dev's choice. Reason documented in implementation.md. |
| 4 — `RepositoryBase.UpsertAsync` PropertyValues copy (B7) | Implemented | `RepositoryBase.cs:34` — update branch now `DbContext.Entry(existing).CurrentValues.SetValues(DbContext.Entry(entity).CurrentValues)` (the `SetValues(PropertyValues)` overload, access-mode-respecting). Insert branch (`existing is null → AddAsync`, line 25-26), method **signature**, and all other members **unchanged**. In-line comment correctly states the zero-caller reality + the `public new` override fact. Matches plan Step 4 and the critic-corrected framing. |
| 5 — `ef-core.md` upsert guardrail (B7) | Implemented | `ef-core.md:31-39` "Repository Upsert Pattern" — names (a) the live `public new` explicit-copy pattern as the current rule (lists all four Fokus repos), (b) the forward-looking `PropertyAccessMode` condition, (c) verification reality (no production callers → compile-green + code reading, not a sync re-run; sync exercises the `new` overrides). All three critic-required elements present. |

**Scope / extra-files check:** No unexpected source files. Two non-source doc edits, both in-scope or appropriate:
- `docs/conventions/ef-core.md` — this **is** plan Step 5 (guardrail). In scope.
- `docs/conventions/project-rules.md` (commit `0ad86be`) — a **one-line clarification** of the EF-migration command, adding that the `-s` startup project must carry `Microsoft.EntityFrameworkCore.Design` and that Persistence may be used when it has an `IDesignTimeDbContextFactory`. **Not a plan step**, but directly surfaced by the Step 3 deviation and a correct, minimal doc fix (root-cause-the-process per ADR-003). **In scope as an appropriate boy-scout doc fix — not scope creep.** Flagged for the reviewer (Step 2) to confirm, per the team lead's note; no source-code impact.

**No plan step is Missing.** All five Implemented or acceptably Deviated with documented reason. B1 (retract+note) and Q5 (scope held) were correctly no-code and need no implementation entry.

**Verdict: PASS**
