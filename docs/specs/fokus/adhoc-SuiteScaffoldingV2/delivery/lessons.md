# Suite Scaffolding V2 — Lessons

## Architect Lessons

### Done Check (Step 1) — 2026-06-05

**Verdict: PASS.** All 9 steps Implemented / Deviated-with-valid-reason; 4 locked decisions verified honored in built code.

1. **Baking a past failure into a structural decision works.** The discarded run's biggest pain (ASP.NET Core Identity → 3 review cycles) was converted from a "review will catch it" hope into Step 5's structural lock (plain `Entity<int>` + string Role). Result: grep for `IdentityUser`/`UserManager`/`IdentityRole`/`Microsoft.AspNetCore.Identity` across the whole new repo returned **zero** source hits. When a prior run failed on a class of mistake, encode the avoidance as a plan-level structural decision, not a checklist item — it eliminates the failure mode instead of catching it late.

2. **Verify locked decisions in code, not in implementation.md.** The done check confirmed every lock by grep + file read on `this repo`, not by trusting the status table. Two of the three pre-commitment predictions (identity leakage, AppUser split incompleteness) were disproven *because the verification was run* — and the one real artifact (stray fokus.db) was caught and correctly judged non-blocking. Done checks must spot-check the built tree; a claim of "Done" is not evidence.

3. **A clean model snapshot is the real proof a "remove X" migration is complete.** `RemoveAppUsers` could have existed while `FokusDbContextModelSnapshot.cs` still carried the `AppUser` entity (a half-done drop → `PendingModelChangesWarning` at startup). Checking the *current snapshot* (zero `AppUser` matches), not just the migration file, is what proved the removal complete. For any "remove a type/table" step, the done check should grep the model snapshot, not only the migration.

4. **Gitignored working-tree artifacts are not commit risks.** The stray `Fokus.API/fokus.db` looked like a clean-copy violation until `git ls-files | grep '\.db$'` showed it untracked under the repo's `.gitignore`. Distinguish "exists in working tree" from "will be committed" before escalating an artifact as a blocker. The commit boundary, not the filesystem, is what the no-secrets / clean-copy locks actually govern.

5. **Auth-gated verify items are a legitimate plan-sanctioned handoff.** Gate items needing real Google OAuth creds + a browser (3/3b/4/6/7/9) cannot pass an unattended team run. The plan pre-declared this (Cross-Repo Execution Note 4), so "PASS (partial) + user handoff" is conformant, not a skipped step. When a plan's own constraints make some acceptance criteria un-automatable, the plan must say so up front — and the done check honors that boundary rather than failing the step.

### Skill gap (for the learner)

- **`compose-host`** — multi-module Host `Program.cs` orchestration (multi-assembly FastEndpoints registration via `o.Assemblies = [...]`, multi-DbContext startup migration ordering, data-dir creation for relative SQLite paths, AppSettings seed). The plan inlined this in Steps 3 & 9 because no skill covers it. Recurs in any modular-monolith Host. Candidate for a new skill.
