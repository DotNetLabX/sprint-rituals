# Critic Review — Plan Review (Mode 2): adhoc-Pass5-EventCorrectness

**Reviewed:** `docs/specs/adhoc-Pass5-EventCorrectness/delivery/plan.md`
**Requirement sources cross-referenced:** `questions.md` (Q1–Q5 locked answers), `docs/proposals/refactor-triage.md` (B1/B2/B6/B7), `docs/architecture/decisions.md` (ADR register). `.claude/rules/engineering-discipline.md` named as binding but **absent from the repo** (see F1).
**Reviewer:** Critic (nexus)
**Date:** 2026-06-09

---

## Verdict

**REQUEST CHANGES** — The plan correctly covers all five locked decisions with no scope creep and a correct B6 migration. However it contains one **High** correctness defect: the entire B7 justification and its acceptance/regression check rest on a false premise about who calls `RepositoryBase.UpsertAsync`. Code on disk proves **no production path calls the base method** — every Fokus repository hides it with `new` and copies scalars explicitly. The B7 fix is harmless but its stated regression check tests a path the change never touches, and the "hardens the shared base" rationale is weaker than the plan claims. Fix the B7 framing (Steps 4–5) before implementation; the rest of the plan is sound.

---

## Decision-coverage matrix (every locked decision → plan step)

| Decision | Locked answer | Plan coverage | Correct? |
|----------|---------------|---------------|----------|
| Q1 / B1 | Retract + note; PersonCreated already dispatches; no code change; accept two-save workaround | Context (L21), Scope L26, Locked L42, "Cross-Service Changes" L146, Testing L157 — documented, **no code step** | Yes |
| Q2 / B2 | Keep `UserCreated` as documented placeholder; annotate; no delete, no wire, no behavior change | Step 1 (L80–87) — XML doc comment only, explicit "do NOT raise/delete/change shape" | Yes |
| Q3 / B6 | Remove `HasDefaultValue("Manager")`; factory single source; include EF migration | Step 2 (config removal L89–97) + Step 3 (migration L99–111) | Yes |
| Q4 / B7 | Fix now: upsert must copy private backing fields; guardrail/regression note | Step 4 (L113–130) + Step 5 (guardrail note L132–140) | Partially — fix scoped correctly, but **regression check rests on a false premise** (F2) |
| Q5 | Scope held; no User→aggregate promotion | Scope "out of scope" L32, Domain Model Changes L70, mirrors Q2(c) | Yes |

All five decisions are addressed. No decision is silently dropped. The two no-code decisions (Q1, Q5) are correctly handled as documentation/scope statements rather than invented code steps.

---

## Findings

### F1 — [Medium] Binding rule file `.claude/rules/engineering-discipline.md` does not exist
- **Location:** Plan L3 (front-matter "binding constraints ... `.claude/rules/engineering-discipline.md`"); also named as binding in the review brief and in ADR-003 (decisions.md L20: "Enforced by `.claude/rules/engineering-discipline.md`").
- **Issue:** The file is cited as an authoritative binding constraint by the plan, the ADR register, and the review brief, but it is **not present in the repo**. `.claude/` contains only `settings.json`, `settings.local.json`, `.current-agent`, `.personas.json`, `.pipeline-state` — no `rules/` directory anywhere in the tree (verified by repo-wide search for `engineering-discipline*` and any `rules/` dir: zero hits). The plan therefore claims conformance to a constraint it cannot have verified, and a reviewer/developer cannot check the plan against it.
- **Severity rationale:** Medium not High — the plan's *content* does not appear to violate any obvious discipline rule, and ADR-001..015 (the real binding register) is present and honored. But a binding source being a dangling reference is a real gap: it means "every finding is a problem class / fix root cause" (the rule ADR-003 says this file enforces) is unenforceable, and the plan's "Anti-pattern check" (L64) asserts conformance to a rubric whose authoritative text is missing.
- **Suggested resolution:** Either (a) the architect restores/locates `engineering-discipline.md` and the plan cites the real path, or (b) the plan drops the dangling citation and points at the actual enforcement source. Flag to whoever owns the ADR register that ADR-003's "Enforced by" pointer is also dangling. Not a blocker for the B6/B7 code, but should be reconciled.

### F2 — [High] B7 regression check (Step 4 Accept #4) tests a path the change cannot reach — false premise
- **Location:** Step 4, acceptance criterion #4 (plan L129); Testing Strategy item 2 (L156); Step 4 "Problem" framing (L116); Q4 context in questions.md L88.
- **Issue:** The plan instructs the developer to validate the `RepositoryBase.UpsertAsync` change by re-syncing a sprint/ticket twice and confirming the second sync updates existing rows. **This regression check does not exercise the changed code.** On disk:
  - `RepositoryBase.UpsertAsync` (`Blocks.EntityFrameworkCore/Repositories/RepositoryBase.cs:22`) is the method being changed.
  - `SprintRepository` (`SprintRepository.cs:46`), `TicketRepository` (`TicketRepository.cs:39`), `DeveloperRepository` (`DeveloperRepository.cs:28`), and `TestExecutionRepository` (`TestExecutionRepository.cs:29`) **each declare `public new async Task UpsertAsync(...)`** — they *hide* the base method via `new` and perform their own explicit scalar property copy (e.g. `SprintRepository` lines 55–63 hand-assign `Name`, `StartDate`, … ; `TicketRepository` lines 49–58 likewise). They never call `base.UpsertAsync` / `RepositoryBase.UpsertAsync`.
  - The sync path resolves the override, not the base: `SprintIssueSyncService.cs:59` calls `sprintRepository.UpsertAsync(...)` where the variable's static type is `SprintRepository`, so the **`new` override** runs — never the base method.
  - Repo-wide search confirms `RepositoryBase.UpsertAsync` has **zero callers** in the entire solution; the only `UpsertAsync` invocation sites (`SprintIssueSyncService`, `XrayIssueSyncService`) all target the hiding overrides.
  - Therefore the re-sync regression check at L129/L156 can pass or fail **independently** of the Step 4 edit; it gives the developer false confidence that the base change is "verified."
- **Why this matters for the decision:** The user's Q4 answer (b) "Fix now" presumes the base method is a live shared-infra hazard. In reality it is *dead code today* (no caller). That does not contradict the user's decision to harden it proactively for "when the domain thickens" (Step 4 Problem L116 is honest that it is *latent*), but the plan must not present a regression check that doesn't test the change. As written, a developer following Step 4 Accept #4 / Testing #2 will "verify" the wrong method.
- **Suggested resolution:** Rewrite Step 4 Accept #4 and Testing #2 to reflect reality:
  1. State explicitly that `RepositoryBase.UpsertAsync` currently has **no production caller** (all four Fokus repos override it with `new`), so the change is to dormant shared infra and carries **no runtime regression risk to the sync path**.
  2. Replace the re-sync "regression check" with the honest verification that actually exercises the base method — e.g. a throwaway/manual call to a repository instance that does *not* override `UpsertAsync` (there is none today, which is itself the point), or an EF in-memory probe constructing the base behavior directly. If no such probe is feasible without an automated suite, say so and downgrade the claim from "regression check" to "compile-green + manual reasoning," rather than asserting a sync re-run validates it.
  3. Keep the re-sync smoke test if desired, but label it as a *general sync sanity check unrelated to the B7 edit*, not as verification of Step 4.

### F3 — [Medium] B7 "hardens the shared base" rationale overstates current benefit; verify the proactive-fix justification against the discipline rule
- **Location:** Step 4 Problem (plan L116 "the fix hardens the shared base for when the domain thickens"); Skill-verification note L62; questions.md Q4 verdict ("real (latent), low").
- **Issue:** The triage rates B7 **low / latent** and the architect's own Q4 recommendation was **(a) Defer** ("fixing a latent bug with no current trigger and no reproduction risks a speculative change to shared infrastructure"). The user overrode that with **(b) Fix now**, which is a legitimate locked decision the plan must honor — and it does. *However*, the plan's framing claims the fix "hardens the shared base" without noting that the base is currently **unreachable** (every repo hides it). So the proactive fix hardens a method that nothing calls, and any future aggregate that wants the hardened behavior would *also* have to stop using a `new` override to benefit. The plan should make this dependency explicit so the hardening isn't mistaken for an active improvement.
- **Severity rationale:** Medium — this is a framing/accuracy gap, not a decision violation. The user decided "fix now"; the critic does not relitigate that. But the plan presents the value proposition inaccurately, which could lead a reviewer to wave it through as "improves live infra" when it does not.
- **Suggested resolution:** Add one sentence to Step 4 (and the Step 5 guardrail) noting that (a) the base method is currently bypassed by `new` overrides in all four Fokus repos, so the hardening is purely forward-looking, and (b) the guardrail in Step 5 should therefore *also* state the real architectural rule the codebase relies on today: repositories with non-trivial copy semantics override `UpsertAsync` with explicit assignment (the `SprintRepository`/`TicketRepository` pattern), and the base method is the fallback for simple flat entities. Without that, Step 5's guardrail ("aggregates persisted via `RepositoryBase.UpsertAsync` must keep their EF-mapped state copyable…") names a path no aggregate currently takes.

### F4 — [Low] Step 2 references an `IsActive` default on "line 23" as a DO-NOT-TOUCH carve-out — confirm it is intended to remain
- **Location:** Step 2 Accept (plan L96): "`grep "HasDefaultValue" src/Services/Identity` returns only the unrelated `IsActive` default on line 23 (DO-NOT-TOUCH …)".
- **Issue:** Verified accurate against `UserConfiguration.cs:23` — `builder.Property(u => u.IsActive).IsRequired().HasDefaultValue(true);` does exist and is out of B6's scope. The carve-out is correct. The only note: `IsActive` has the *exact same* duplication smell B6 is removing for `Role` (the factory `User.CreateFromPerson` also sets `IsActive = true` at `User.cs:11`). The plan correctly scopes B6 to `Role` only per Q3, but should briefly acknowledge that the identical `IsActive` duplication is **knowingly left** (consistent with "fix the cited finding's scope," but a `boy-scout`/problem-class reader per ADR-003 might expect it flagged). This is informational — do not expand scope; Q3 was answered for `Role` only.
- **Suggested resolution:** One line in Step 2 or Data Model Changes: "`IsActive`'s analogous DB-vs-factory default duplication is out of Q3 scope and intentionally left for a future pass." Prevents a reviewer from reading the `IsActive` carve-out as an oversight.

### F5 — [Low] Step 3 migration acceptance is correct; confirm the column-drop target matches the existing migration
- **Location:** Step 3 (plan L99–111); existing migration `20260605102711_InitialCreate.cs`.
- **Issue:** Verified the InitialCreate migration emits `Role = table.Column<string>(... maxLength: 32, nullable: false, defaultValue: "Manager")` (line 45). So the new `RemoveUserRoleDbDefault` migration's `Up` must `AlterColumn` `Users.Role` to drop `defaultValue: "Manager"` while keeping `maxLength: 32, nullable: false` — exactly what Step 3 Accept L110 requires. The acceptance criteria (`Up` drops default, `Down` restores, snapshot no longer shows the default, do-not-hand-edit) are correct and precise. **No defect** — recording as a positive confirmation so the developer/reviewer can rely on it.
- **Suggested resolution:** None. Migration step is correct as written.

### F6 — [Low] Artifact/format completeness — lessons feedback target named but no lessons.md interaction described for Step 1/2/3
- **Location:** Skill Mapping (plan L57, L123) routes the Step 4 skill gap to `lessons.md → persistence-patterns`.
- **Issue:** Plan correctly identifies the Step 4 skill gap and routes it to `lessons.md` as a `persistence-patterns` improvement (good, matches the `lessons-format` expectation). No issue with Steps 1–3 (they're `Follow`/None and need no lesson). This is a confirmation, not a defect: the single skill-gap feedback loop is present and correctly targeted. Note for the developer: when logging the Step 4 lesson, capture the *real* nuance surfaced here — that the base `UpsertAsync` is bypassed by `new` overrides — so the `persistence-patterns` skill improvement reflects the actual codebase pattern, not just the backing-field copy mechanics.
- **Suggested resolution:** None required; optional enrichment of the lesson per the note above.

---

## Scope-creep audit (explicit per brief)

| Risk | Verdict | Evidence |
|------|---------|----------|
| Aggregate-boundary change via B2 (User→aggregate) | **No creep** | Step 1 is doc-comment only; explicitly "do NOT add RaiseCreated, do NOT change `: IDomainEvent`" (L83). Q5 scope-held mirrored in L32/L70. |
| Aggregate-boundary change via B7 | **No creep** | Step 4 changes only `RepositoryBase.UpsertAsync` value-copy mechanism; signature/Add-branch frozen (L120); owned/complex-type copy explicitly carved out (L121). No aggregate or VO touched. |
| Over-engineering shared infra (B7) | **Borderline — see F2/F3** | The fix itself is minimal and correctly scoped (scalar/backing-field copy only). The *risk* is not over-engineering the change but over-stating its value and mis-verifying it. Mechanically in-bounds. |
| B6 expanding beyond `Role` | **No creep** | Step 2 removes only `Role`'s default; `IsActive` explicitly carved out (L96, and see F4). One migration only (L150). |
| New domain-event handlers / wiring | **No creep** | Explicitly out of scope (L33), "Cross-Service Changes: None" (L146). |
| SprintSynced / SignalR / MassTransit | **No creep** | Out of scope (L35); correctly excluded as integration-event path not domain-event path. |

No scope creep detected. The plan is, if anything, conservative — its defect is over-claiming verification (F2), not over-reaching.

---

## What is correct (positive confirmations)

- All five locked decisions are covered; the two no-code decisions (Q1, Q5) are handled as documentation/scope, not invented work.
- B6 (Steps 2–3) is fully correct: config removal scoped to `Role`, migration command and project paths match `project-rules.md` conventions, `Up`/`Down`/snapshot acceptance is precise and matches the on-disk InitialCreate migration (F5).
- B2 (Step 1) annotation is correctly bounded — no behavior, no shape change, no wire.
- Skill mapping is honest: `persistence-patterns` Follow for Steps 2–3; `None` + skill-gap feedback for Step 4 with a correct frontmatter-coverage analysis.
- No aggregate-boundary or encapsulation change leaks in via B2 or B7 (Q5 boundary held).

---

## Required actions before implementation

1. **F2 (High):** Rewrite Step 4 Accept #4 and Testing #2 — the base `UpsertAsync` has no production caller (all four Fokus repos hide it with `new`), so the re-sync check does not verify the change. State the no-caller reality; replace or relabel the verification.
2. **F3 (Medium):** Correct the "hardens the shared base" framing and make Step 5's guardrail name the *actual* live pattern (explicit-copy `new` overrides), not a path no aggregate uses today.
3. **F1 (Medium):** Reconcile the dangling `.claude/rules/engineering-discipline.md` citation (and the matching ADR-003 pointer) — restore the file or fix the reference.
4. **F4/F5/F6 (Low):** Optional polish — acknowledge the parallel `IsActive` duplication is knowingly out of scope; no change needed to the (correct) migration step; enrich the Step 4 lesson with the `new`-override nuance.

---

**Reviewed:** docs/specs/adhoc-Pass5-EventCorrectness/delivery/plan.md
