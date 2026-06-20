# adhoc-ReflektFrontendMerge (RA5) — Implementation Plan (parent)

**Status:** In progress — sub-plan A written; C/B/D to follow
**Type:** adhoc / Complex (multi-front: frontend merge + deferred backend renames + RA4 carry-overs)
**Spec:** None (adhoc — binding input is the ADR register + RA3/RA4 deferred carry-overs, not a spec)
**Binding ADRs:** ADR-016 (Reflekt as module), ADR-019 (one-directional, anonymized Fokus→Reflekt), ADR-020 (one routing shell, module-isolated SPAs; build topology deferred to *this* pass), ADR-021/022 (guest auth, no Person for guests)

---

## Locked decisions (owner, this session — 2026-06-19)

- **Build topology: two Vite builds under one thin routing shell** (NOT a single merged Vite app).
  Rationale: the two SPAs share *infrastructure* (identical stack, the same signed-in Identity cookie, the same Vite→wwwroot build model) but **no application code, UI, views, or runtime state**, and they are **not coupled** (a user may use only one). The monorepo already delivers build-time code sharing; a single runtime app would force-couple release and load both bundles on every visit for no runtime payoff — the RA4 integration is *data-in-place* (Reflekt renders Fokus metrics from its own backend), not navigate-across, so the no-reload benefit barely applies. Two-builds' usual prize (independent deploy) is moot under one Host deployable, but independent build + isolation is the correct default for decoupled apps. Resolves the ADR-020-deferred topology fork.
- **Scope: full RA5 in one plan, auto-split into sequential sub-plans** (each ≥2 steps), since the total exceeds 10 steps.
- **RA5-C rename endgame: COMPLETE, not partial.** Eliminate every `sprint`-named identifier meaning *the retro board* (DB columns, C# properties, routes, SignalR/claim wire strings → `RetroSession`/`retroSessionId`); `sprint` survives only where it means a *Fokus* sprint (the 1..N referenced sprints — `FokusSprintSnapshot`, gRPC, `GetSprintContext`). Driven by the multi-sprint RetroSession model (RA3): board-meaning `SprintId` is now a semantic lie, and a partial rename perpetuates the ambiguity (already biting). The in-code "Q2 DO-NOT-TOUCH" note was a deferral to this pass; frontend is in-repo so all renames are lockstep.

## Sub-plan breakdown & sequence

| Sub-plan | Title | Depends on | File |
|----------|-------|-----------|------|
| RA5-A | Frontend merge & routing shell | — | `plan-A-merge.md` ✅ written · critic ACCEPT-WITH-CHANGES → folded → **Approved** |
| RA5-C | Backend rename completion (FK columns + flat routes + SignalR strings + frontend callers) | A | `plan-C-renames.md` ✅ written · critic ACCEPT-WITH-CHANGES → folded → **Approved** |
| RA5-B | Sprint-context panel UI (RA4 deferred) | A | `plan-B-panel.md` ✅ written · critic ACCEPT-WITH-CHANGES → folded → **Approved** |
| RA5-D | Rule 7 ordering + true total (PC3 closed: thin) | B (soft), C (soft) | `plan-D-rule7-pc3.md` ✅ written · critic REVISE → folded → **Approved** |

**Recommended order: A → C → B → D.** Merge first (mechanical, bisectable); finish the deferred renames while frontend+backend are both being touched and before new feature code piles on; build the panel on a clean renamed base; do the two refinements last. **B may swap ahead of C** if the visible panel payoff is wanted sooner — no hard dependency between B and C.

## Open items — both RESOLVED at RA5-D planning (2026-06-20)

- **RA5-D / Rule 7 — FEASIBLE (resolved).** `SprintMembership.AddedAt` exists (`Fokus.Domain/Sprint/SprintMembership.cs:8`) and is already used by `ScopeChangeAnalyzer` — most-recently-added ordering is a one-line swap in `BuildTopScopeAdditions` + an additive true-total contract field. No Jira-sync work. Planned in RA5-D Steps 1–3. (Also surfaced a real bug: `MidSprintAdditionCount` always returns 0 — `"Added"` vs `"added"` case mismatch — folded into RA5-D Step 1.)
- **RA5-D / PC3 — RESOLVED: keep cached thin.** Owner decision (2026-06-20): `cached` sections stay identity+state only (no last-known metric numbers); `FokusSprintSnapshot` stays thin. Ratifies the ADR-019 thin-snapshot design; **no code change**. Privacy was never at risk (cached numbers would be anonymized) — only the thin-snapshot choice, which is upheld.

## Shared context (all sub-plans)

- **Reflekt frontend source to bring in:** `<reflekt-repo>\web\reflekt-web\` (Vue ^3.5, Pinia ^3, Tailwind 4, **vue-router 5**, SignalR 10, lucide icons). Read-only reference repo — copy in, never modify the source.
- **Fokus frontend (in-repo):** `client\` (vue-router 4, apexcharts; builds to `src/Host/wwwroot`).
- **Reflekt backend (in-repo since RA1):** `src/Services/Reflekt/`.
- **RA4 panel endpoint (already shipped):** `GET /api/retro-sessions/{retroSessionId}/sprint-context` (Reflekt.API) — RA5-B's data source.
- **Host** is the only Web SDK deployable; Reflekt.API is a class library (its `wwwroot` is vestigial). All built client output lands under `src/Host/wwwroot`.

## Review mode

Recommend **critic** review (Mode 2, plan vs ADR register) per sub-plan — there is no spec to diff, so the critic cross-references plan steps against ADR-019/020/021 acceptance and the RA3/RA4 carry-over list. RA5-A is frontend-infra (skill-light); RA5-C is a rename/type-move pass (the refactoring plan rules apply: old→new tables, grep-before-rename, DO-NOT-TOUCH carve-outs).
