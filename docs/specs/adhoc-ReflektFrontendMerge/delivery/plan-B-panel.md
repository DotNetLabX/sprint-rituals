# RA5-B — Sprint-Context Panel UI (RA4-deferred)

**Parent:** `plan.md` · **Depends on:** RA5-A (retro app in-repo at `client/apps/retro`). Soft on RA5-C (the sprint-context endpoint is *already* retro-named — `/api/retro-sessions/{retroSessionId}/sprint-context` — so B doesn't block on C; build B on whichever lands first). · **Type:** Scoped feature (frontend-only; no backend change)

Build the Vue slide-out **sprint-context panel** RA4 deferred — the visible integration payoff. The backend is **done** (RA4): the panel only *reads* `GET /api/retro-sessions/{retroSessionId}/sprint-context` and renders it. **No backend, no gRPC, no new persisted state** in this sub-plan.

**Binding design source:** the RA4 spec `docs/specs/adhoc-SprintContextPanel/definition/spec.md` (Flows 1–4, Business Rules 1–10, Acceptance Criteria) and `help.tooltips.md` (tooltip copy). These ACs are RA5-B's acceptance.

**Shipped contract the panel binds to** (`Reflekt.API/.../GetSprintContext/GetSprintContextQuery.cs`):
```
SprintContextResponse { Sprints: SprintContextSection[] }   // already ordered earliest-start-first; cached/unavailable last
SprintContextSection {
  sprintId:int, sprintName:string?, startDate:DateTime?, endDate:DateTime?,
  freshness: "live" | "cached" | "unavailable",
  completionRate/Rag, disruptionRate/Rag, carryOverRate/Rag, healthScore/healthRag,  // metrics null unless freshness=="live"
  addedMidSprint: { ticketKey, summary, storyPoints }[] | null,                       // present only when disruption amber-or-worse (Rule 6)
  addedMidSprintOverflow: int?                                                        // "+N more"
}
```
> `addedMidSprint` ordering is currently SP-based and `overflow` is "+N among top-5-by-SP" (RA4 deviation). Rule 7's "most-recently-added" ordering + true total is a **backend** change in **RA5-D** — transparent to this panel. RA5-B renders the list + overflow exactly as the contract returns them.

**Privacy invariant (Rule 4):** `ScopeAdditionDto` has no assignee field by design — the panel *cannot* leak a person. Never add a field or a lookup that would.

---

## Step 1 — Types + API module for the sprint-context endpoint

In `client/apps/retro/src/`: add the TS types mirroring `SprintContextResponse` / `SprintContextSection` / `ScopeAdditionDto`, and an API module function `getSprintContext(retroSessionId): Promise<SprintContextResponse>` calling `GET /api/retro-sessions/${retroSessionId}/sprint-context` (**reuse the retro app's existing guest-aware fetch helper** — `credentials: 'include'` + `X-Guest-Token` when a guest token exists, per `report.ts`; the endpoint is `AllowAnonymous`/open-participation so the header is harmless, but reusing the helper keeps the convention uniform — critic M1).

**Accept:** types compile and match the wire shape field-for-field (camelCase); the API module returns a typed `SprintContextResponse`; a 404 surfaces as a typed not-found the store can branch on.
**Skill:** Follow `create-vue-feature` (types + api-module parts) · **TDD:** no (no frontend suite — verified at Step 4) · **Confidence:** high

## Step 2 — Pinia store: session-cached sprint context

Add a `sprintContext` store (`client/apps/retro/src/stores/`): fetch the context **at most once per retro session** (Rule 8) and cache it for the session, keyed by `retroSessionId`. Expose `sections`, `loading`, and a `loaded` flag; reopening the panel reads cache, never re-fetches. Trigger the fetch from `useRetroRoom.enterRoom()` (the existing hydration orchestrator — the `Promise.all([...])` that fetches the other stores on room entry), **after** the guest join completes; the store itself is **cache-only** (a `fetch` action + a `loaded` guard — no self-`onMounted`), matching the established composable-orchestrates / store-holds split (critic H2). A gRPC/Fokus failure is **not** an error here — the endpoint returns 200 with `cached`/`unavailable` sections, so the store treats any 200 as success and only sets an error on a genuine transport/404 failure (Flow 3).

**Accept:** opening → closing → reopening the panel triggers exactly one network call per session (Rule 8 / AC "reopening does not trigger a second pull"); a `cached`/`unavailable` payload populates `sections` without setting the error state; loading is observable while the first fetch is in flight.
**Skill:** Follow `pinia-patterns` · **TDD:** no · **Confidence:** high

## Step 3 — Panel components + top-bar button

Build the UI under `client/apps/retro/src/components/` following the retro app's existing component/layout conventions:
- **Top-bar button** in the retro room's **persistent header — `RetroPhaseHeader.vue`** (the header `RetroRoomView.vue` renders in every phase; its row-1 action cluster is the natural anchor), **not** `PageToolbar` (which the retro room never mounts — critic H1). Present in **every phase** for **every participant** incl. guests (Rule 1, Flow 4) — no phase/role gate. Tooltip: help.tooltips "Sprint Context button".
- **`SprintContextPanel`** — a slide-out drawer; opens/closes from the button; renders one **`SprintContextSection`** per `Sprints[]` entry in array order (Rule 2 — backend already ordered). Multi-sprint header per help.tooltips "Panel header (multi-sprint)" when `Sprints.length > 1`.
- **`SprintContextSection`** — sprint name + dates; the four metrics (completion / disruption / carry-over rates + health score) each shown with its RAG color from the `*Rag` string (map `"green"/"amber"/"red"` → the retro app's Tailwind tokens via a **retro-local** RAG badge — keep it in `apps/retro`, **not** `packages/shared`: the apps are decoupled by the parent's locked decision, so don't manufacture a shared UI component — critic open-Q resolved). Health band per `docs/kb/health-score.md` (display only — never recompute, Rule 3).
- **Added-mid-sprint block** — rendered only when `addedMidSprint` is present (Rule 6, already gated server-side); lists up to 3 `{ticketKey, summary}` (+ optional storyPoints), with "+N more" from `addedMidSprintOverflow` (Rule 7 display). Tooltip: "Added mid-sprint block" / "+N more".
- **States:** `cached` → last-known badge (tooltip "Cached / last-known badge"); `unavailable` → name+dates, no metrics, "Unavailable section" copy; loading → loading state (Flow 3). Metric fields are null unless `freshness=="live"` — guard rendering accordingly.

**Accept:** matches RA4 Flows 1–4 and tooltip copy; no developer name/avatar/assignee renderable anywhere (Rule 4 — structurally guaranteed by the DTO); the block appears iff `addedMidSprint != null`.
**Skill:** Follow `vue-component-architecture` (panel/section split) + `tailwind-theme` (RAG tokens) + `vue-patterns` (SFC composition) · **TDD:** no · **Confidence:** medium (slide-out + multi-state rendering is adaptation, not a direct precedent)

## Step 4 — Integration + verification against the RA4 ACs

Wire the store + panel into the retro room; verify against the RA4 spec's Acceptance Criteria (manual smoke — no automated frontend suite):
- single-sprint + multi-sprint sections render with correct values vs Fokus; button present in every phase + as guest; amber-or-worse → added-mid-sprint block (≤3 + "+N more"), below-band → absent; Fokus-down → cached/unavailable, panel still renders, no blank; reopen → no second fetch; no person/assignee anywhere.

**Accept:** all RA4 ACs pass in manual smoke against the merged retro app (RA5-A shell); `client/` builds; documented in implementation.md with the AC checklist results.
**Skill:** None (manual verification) · **TDD:** N/A · **Confidence:** high

---

## Skill Mapping

| Step | Disposition | TDD | Notes |
|------|-------------|-----|-------|
| 1 | Follow `create-vue-feature` | no | Types + API module mirroring the shipped DTOs |
| 2 | Follow `pinia-patterns` | no | Session-cached, fetch-once store (Rule 8) |
| 3 | Follow `vue-component-architecture` + `tailwind-theme` + `vue-patterns` | no | Slide-out panel + section + top-bar button; RAG colors; states |
| 4 | None | N/A | Manual smoke vs RA4 ACs (no frontend test suite) |

**TDD all `no`:** the project runs no automated frontend test suite (RA4/RA5-A decision); frontend behavior is verified by manual smoke against the RA4 ACs. Steps 1–3 carry non-`None` skills — the done-check expects those skill invocations in the log. Each step `Satisfies:` an RA4 spec AC (the panel's binding acceptance), so this sub-plan's done-check maps steps ↔ RA4 ACs, not ADRs.

## Review mode

**Critic — Mode 2 vs the RA4 spec** (this sub-plan *does* have a spec to diff: the RA4 definition). Cross-reference each panel step against Flows 1–4 / Rules 1–10 / the 8 ACs, and confirm the contract-binding (TS types vs `GetSprintContextQuery.cs`) is field-accurate and the Rule-4 privacy invariant is structurally held.

---

## Plan Review (critic, Mode 2 vs RA4 spec — code-grounded)

**Verdict: ACCEPT-WITH-CHANGES → all findings folded above.** Critic agent `a5fc7ac5d49359298`. Coverage: all 8 ACs + Flows 1–4 + Rules 1–10 map to steps; contract mirror field-accurate vs `GetSprintContextQuery.cs`; Rule-4 privacy holds structurally; camelCase wire assumption confirmed sound. Findings were integration-point inaccuracies against the real retro source, not spec-coverage gaps.

| # | Severity | Finding | Disposition |
|---|----------|---------|-------------|
| H1 | HIGH | Button host `PageToolbar` is never mounted in the retro room (it renders `RetroPhaseHeader`) → button would never appear, AC2 fails | Step 3 re-pointed to `RetroPhaseHeader.vue` (persistent, every phase) |
| H2 | HIGH | Hydration is `useRetroRoom.enterRoom()` (`Promise.all`), not store-level → risk of mis-timed/duplicate fetch (Rule 8/AC8) | Step 2 names `enterRoom()` as trigger; store is cache-only |
| M1 | MEDIUM | Step 1 understated guest auth (`X-Guest-Token` + credentials, per `report.ts`) | Step 1 reworded to reuse the guest-aware fetch helper |
| open-Q | — | `RagBadge` in `packages/shared` would contradict the no-shared-UI decision | Resolved → retro-local badge |

**Confirmed sound (no change):** camelCase JSON (no PascalCase config in Host; FastEndpoints + middleware default to camelCase); `ScopeAdditionDto` has no assignee field (Rule 4 structurally enforced); Rule 8 cache-keying correct; Rule 7 ordering correctly deferred to RA5-D (not a gap).

**Status: Approved — ready for developer** (all critic findings folded; spec coverage complete; no open CRITICAL/HIGH).
