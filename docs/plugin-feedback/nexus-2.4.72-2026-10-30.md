# Nexus plugin feedback — v2.4.72 — 2026-10-30

Portable feedback file (ADR-1). Send to the plugin maintainer; entries apply in the plugin source
repo (`<plugin-repo>`).

Source project: **sprint-rituals**. Origin: **declare-verify-roles onboarding** (solo session),
declaring a `unit` role that chains three `dotnet test` invocations.

**Status:** READY for maintainer. Not applied locally (ADR-1). Worked around locally by running
the composer once per leg and joining the legs with `&&`.

---

## 1. Composer `join` / `join-stop` touch only the first `dotnet test` of a chained command — DEFECT, MEDIUM

- **Suggested target:** `hooks/scripts/lib/compose-test-command.js` (`join`, `join-stop`), and the
  `declare-verify-roles` SKILL.md step 2 wording ("the clause on the test invocation of a chained
  `unit`").
- **Action:** update + regression test.
- **Evidence:** input
  `dotnet test A.csproj && dotnet test B.csproj && dotnet test C.csproj`;
  `join --runner vstest` printed `--filter "Speed!=Slow"` on `A` only, and `join-stop --runner vstest
  --seconds 30` added `--blame-hang-timeout 30s --blame-hang-dump-type none` to `A` only. Both exit 0,
  so the partial result looks complete.
- **Lesson:** in vstest mode `dotnet test` takes one project per call, so a `unit` scoped to several
  test projects is naturally a chain of test legs, not `build && test`. Either apply the clause to
  every test leg, or exit 2 naming the reason when a command holds more than one test invocation —
  never a silent partial join. The skill text should then name the multi-leg case.
