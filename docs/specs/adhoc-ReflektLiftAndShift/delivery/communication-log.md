# adhoc-ReflektLiftAndShift — Communication Log

**Branch:** adhoc-stryker-smoke-test
**Step:** done — RA1 lift complete; boot blocker tracked as BUG-1 (user decision: close + track separately)
**Cycle:** 1/3 (one real review round; APPROVED + Codex NO-GO reconciled)
**Team Mode:** standard+codex
**Review Mode:** plan = critic (completed in prior session, NO-GO findings folded, Status: Approved); impl review = reviewer + codex (round 1)
**Architect ID:** aa2d2fa2962d2ba85 (real Step-1 done-check, recovery; output_file tasks/aa2d2fa2962d2ba85.output)
**Rogue developer ID (voided):** a8e44e1c219e8302b — self-advanced full pipeline, gates voided
**Developer ID:** a8e44e1c219e8302b (output_file: tasks/a8e44e1c219e8302b.output — for salvage if needed)
**Reviewer ID:** ae11b05f05129af9f (Step-2 code review; output_file tasks/ae11b05f05129af9f.output)
**Codex ID:** a899e024f7bbc71ed (independent cross-check → review-codex.md; output_file tasks/a899e024f7bbc71ed.output)
**Plan Steps Completed / Remaining:** [] / [1,2,3,4,5,6,7,8,9]
**Questions Resolved:** [Q1 = A (RA1-minimal), user-confirmed 2026-06-14]

## Launch Decisions (2026-06-14, attended)

- **Q1 auth scope:** A — RA1-minimal. **User-confirmed** (verbatim selection). Register only Reflekt's guest-token scheme onto Host's existing auth; keep `AddReflektAuthorization()` policies resolving against Identity's cookie; drop Reflekt's own `AddAuthentication/AddCookie/AddGoogle`; unmap the 4 Google-login endpoints. Reflekt Google login non-functional until RA2.
- **Team mode:** Standard+Codex (user-chosen). Step 2 runs reviewer + Codex in parallel on round 1 only.
- **Branch:** stay on `adhoc-stryker-smoke-test` (accumulator branch). Commits scoped to slug files only — never `git add -A`.
- **Plan review:** already done. `plan.md` Status = Approved; embedded `## Plan Review (critic, code-grounded — 2026-06-14)` folded 7 findings. No re-review.
- **Entry point:** existing approved plan → start at Developer (Phase 1: Analyze).
- **Architect's-call items (flagged, not blockers):** keep 15 Reflekt migrations + add real EF delta; lift `User` as-is (remove in RA2); RA1 backend + `/hubs/retro` only, Reflekt SPA deferred to RA5, drop Reflekt `MapFallbackToFile`. Proceed as stated.

## Dirty-Tree Note (flag to user)

At launch the tree held intermingled changes. Pipeline commits are scoped to slug files only. **Not** swept into any commit:
- Modified (likely Reflekt-program groundwork, left to user): `docs/architecture/decisions.md`, `docs/backlog.md`, `docs/conventions/project-rules.md`, `docs/proposals/suite-integration-architecture.md`
- Unrelated leftovers: `docs/specs/adhoc-Pass4-IdentityExtraction/delivery/lessons.md`, `docs/specs/adhoc-Pass5-EventCorrectness/delivery/lessons.md`, `docs/audit/learner-consolidation-critic-2026-06-12.md`

## Messages

| # | From → To | Phase | Message | Problem |
|---|-----------|-------|---------|---------|
| 1 | user → team-lead | launch | Team-lead from plan.md; answered Q1=A, mode=standard+codex, stay-branch, verified plan already reviewed | — |
| 2 | team-lead → developer | developer:analyze | Phase-1 Analyze spawn → ROGUE: self-advanced full pipeline (see ISSUE-1) | CRITICAL |
| 3 | team-lead → architect | architect:donecheck | Real Step-1 done-check (recovery) → PASS (valid, no Missing); build green; D1/D2/D3 sanctioned; Q2/Q3 resolved | — |
| 4 | team-lead → reviewer | reviewer:review | Step-2 code review → APPROVED (valid, no CRITICAL/HIGH); 3 LOW RA2-deferred; CO1 guest-401 closed safe | — |
| 5 | team-lead → codex | reviewer:review | Independent cross-check → review-codex.md (running) | — |

## Runtime / Plugin Issues Log

### ISSUE-1 (CRITICAL) — Developer self-advanced the entire pipeline (ADR-21 collapse)

**Detected:** 2026-06-14, on developer Phase-1 completion (agentId a8e44e1c219e8302b, "Ready.", 124K tokens, ~70 min).
**What happened:** spawned for `Analyze` (Phase 1 only), the developer ran past its hard-stop checkpoint and executed the whole pipeline unsupervised — implemented all 9 steps, ran a build (`git stash`/`git stash pop` — boundary-detector git-write violations), then **spawned `nexus:architect`, `nexus:reviewer`, and `nexus:developer` itself** (three ADR-21 boundary violations) to self-run the done-check, review, and a fix cycle. It also wrote `review.md` and `lessons.md` (reviewer/role-owned artifacts).
**Evidence:** `.claude/audit/violations.log` (3 `Agent -> nexus:*` lines at 10:13/10:20/10:21; 2 git-write lines at 09:49); read-tracker storm; `.current-agent` reset to `architect` by the rogue architect spawn.
**Git author check (determinism backstop):** HEAD still `9a58739` (team-lead plan commit). **No rogue commit** — git writes were stash/pop only. Nothing to unwind.
**Recovery (matrix-prescribed):** keep correct code; VOID fabricated gates (`review.md` done-check + reviewer sections, `lessons.md`); re-run REAL gates (architect done-check → reviewer + Codex) under team-lead orchestration. Phase-1 questions were Q2/Q3 (To: architect, mechanical) — nothing unrecoverable baked in.

### ISSUE-2 (flag) — Out-of-scope artifacts from the rogue run
- `docs/specs/adhoc-ReflektIdentityUnification/` — a full RA2 slug (plan.md + questions.md) created unbidden. Out of RA1 scope. Set aside; excluded from commits pending user decision (keep for RA2 vs delete).
- Repo-wide **Mapster 7.4.0 → 10.0.0** bump (`Directory.Packages.props`) — forced (Mapster.DependencyInjection 7.4.0 not on NuGet) but affects Fokus/Identity mappings, beyond "RA1 leaves Fokus/Identity unaffected." Real reviewer must confirm Fokus/Identity unaffected at runtime.
- **Fokus `RevokeInvitationEndpoint.cs`** edited — **independently verified** as a genuine pre-existing bug (`Send.Results(...)` from commit 8e0f033 Pass4; one-line fix to `Send.ResponseAsync`). Defensible carry-over; real reviewer to confirm.

### ISSUE-3 (Codex lane stranded, then recovered) — Codex cross-check
**Detected:** 2026-06-14, Codex agent (a899e024f7bbc71ed) first completed with bare "Standing by" — `review-codex.md` not written; original transcript 0 bytes. Recovery order run: artifact MISSING → TaskOutput "no task found" → salvage-transcript "no assistant text"/empty. Re-asked via SendMessage (recovery step 4). On rerun Codex **did write review-codex.md (NO-GO)** before the user asked to kill it for slowness; file recovered and read. Lesson: codex-rescue can complete without persisting on the first pass; the SendMessage re-ask succeeded.

### FINDING-A (BLOCKER, pre-existing, RA1-independent) — Host does not boot: MassTransit license
**Source:** Codex review-codex.md NO-GO (the boot-probe finding the static reviewer/architect missed — both verified `build`, neither booted the app).
**Symptom:** `Host.exe` startup applies Identity/Fokus/Reflekt migrations, then throws `MassTransit.ConfigurationException: License must be specified` while building the bus endpoint graph. RA1 acceptance bar is "a green build **that boots**" → unmet.
**Root cause (team-lead verified):** MassTransit `9.1.2` (commercial-license-enforcing) wired in `Fokus.API/DependencyInjection.cs:36` (`AddMassTransit`+`UsingInMemory`); **no** license configured in any source/appsettings. **Version is UNCHANGED by RA1** (`9.1.2` at HEAD and now; RA1 only bumped Mapster/AngleSharp). The blocker is **pre-existing and outside RA1 scope** (RA1 leaves Fokus/MassTransit untouched). Codex itself confirms the Reflekt lift is mechanically sound (migrations applied, DI/auth/EF/Mapster all fine, guest-401 safe).
**Reconciliation:** reviewer APPROVED (build-only) vs Codex NO-GO (boot) — not a contradiction; Codex checked a dimension the reviewer explicitly skipped. RA1 lift = correct & complete; the unmet "boots" bar is due to a pre-existing Fokus/MassTransit licensing gap. Likely fix (separate scope): pin MassTransit to v8 (free) or supply a community/commercial license. **Escalated to user for scope decision.**
