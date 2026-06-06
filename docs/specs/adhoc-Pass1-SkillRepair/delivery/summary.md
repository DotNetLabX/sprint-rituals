# Pass 1 — Skill Repair — Pipeline Summary

**Slug:** `adhoc-Pass1-SkillRepair`
**Status:** Done — reviewer APPROVED, Codex GO (Cycle 2), Step 1 done-check PASS.
**Team mode:** Standard + Codex (Codex = independent cross-model second opinion on both plan and implementation).
**Deliverable repo:** `<plugin-repo>` (plugin source — separate git target). **Artifact repo:** `this repo` (this folder).

## What shipped (5 core skills, plugin repo)

| # | Skill | Action | Core encoded rule |
|---|-------|--------|-------------------|
| 1 | `analytics-computation-service` | **Repaired** (whole-skill reframe, +165/−40) | Computation in Domain (ADR-005); endpoint owns response DTO + Mapster map (D-B/ADR-010); dropped the API/Service framing |
| 2 | `domain-patterns` | **Repaired** (+57) | VO-vs-flat-scalar (ADR-007); MediatR/Application kept as a *labelled full-stack variant* beside the light-stack `IEvent` variant — annotated, not scrubbed |
| 3 | `domain-service` | **Created** | Pure-domain service; no Application / `DbContext` / concrete-repo dependency (ADR-004/005/006); handler loads aggregates and passes them in |
| 4 | `central-package-management` | **Created** | ADR-013; three-form version grep (`Version=`, `<Version>` child, `VersionOverride=`) |
| 5 | `framework-currency` | **Created** | ADR-011/012; two-stage `Send.*` detection; verify-against-installed (FastEndpoints 8.1.0); HttpClient + SignalR false-positives with live `file:line` |

Plus housekeeping (C-2): `docs/skill-backlog.md` (new), two `CHANGELOG.md` (analytics, domain-patterns).

## Key decisions (locked this pass)

- **D-A — General / multi-variant skills.** SprintRituals ADRs are documented as *one* variant; skills stay reusable across stacks. Light-stack (FastEndpoints `IEvent`) and full-stack (MediatR/Application) both shown.
- **D-B — Endpoint always owns a response DTO + Mapster map**, even 1:1 (supersedes the earlier "no DTO twins"). Encoded in `analytics-computation-service` + ADR-010.
- **Repo stays out of the domain.** The endpoint handler needs the repo anyway and does the loading; the domain service receives loaded aggregates — keeps the domain pure. (User's design call on the `domain-service` shape.)
- **Mapster ADOPTED (ADR-010) but NOT yet in the stack.** Skills label it target-state. "Add Mapster to `Directory.Packages.props` (CPM)" is a **Pass 2 prerequisite**.
- **Plain DDD identity is locked** (no ASP.NET Core Identity) — reflected in examples.

## Pipeline trace

| Phase | Outcome |
|-------|---------|
| Architect — plan | GO after 3 review rounds (Codex 9 findings + critic 3 + 2 touch-ups, all fixed). Plan committed `a515e60`. |
| Developer — implement | All 7 steps; implementation.md written. |
| Architect — Step 1 done check | **PASS** — all steps present, grep-accepts independently re-run against the real plugin files. |
| Reviewer — Step 2 code review | 7 findings (RV-1..7) → **APPROVED** after Cycle 1 fixes. |
| Codex — Step 2 impl review | NO-GO (7H/4M/1L, CX-1..16/X1/X2) → Cycle 1 17/18 → **Cycle 2 GO** (CX-13 resolved). |

Verdict cross-check (Verdict Validation): reviewer APPROVED with **zero** open CRITICAL/HIGH; done-check PASS with **no** step `Missing`. Both valid.

## Excluded from the commit (surfaced, not touched)

- `docs/architecture/README.md` (+30, ADR-10 "spawn mode") — **pre-existing WIP**, mtime precedes the developer run; no Pass 1 skill references it. Left for the user.
- `.claude/`, `.omc/`, `docs/audit/tool-calls.log` (plugin repo) — agent/hook runtime pollution. Not committed.

## Process lessons (see lessons.md)

- **Cross-repo gate blind spot (PR-2):** the `pipeline-gate` hook watches the sprint-rituals tree, but skills land in the plugin repo — so the developer's two-phase spawn collapsed (analyze+implement in one run). No checkpoint was actually skipped (0 questions), but the gate can't enforce phases for plugin-repo work. Future plugin-editing passes need a gate that watches the plugin tree.
- **Codex relay truncation:** `codex-rescue` returns terse acks ("Done.", "Ready."). Mitigated by having Codex write full reports to delivery files and reading the verdict via Grep, never trusting the returned message.
- **In-context critic (PR-1):** the architect ran the critic inside its own context once (subagent spawn unavailable) — author≠reviewer violation; a fresh critic was spawned for the real gate.

## Next

- **Pass 2 — SliceHygiene** is now **Ready** (Pass 1 dependency cleared). First task in Pass 2: add Mapster to CPM (the prerequisite above), then endpoint split + naming using these skills.
