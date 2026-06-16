# Sprint Context Panel — Fokus Metrics in the Retro Room

**Traces to:** `docs/proposals/suite-integration-architecture.md` §9 (Sprint Context Panel), §2 (gRPC), §5 (data flow); `docs/architecture/decisions.md` ADR-019 (one-directional anonymized Fokus→Reflekt flow), ADR-018 (RetroSession spans 1..N Fokus sprints), ADR-017 (Fokus owns sprint data); `docs/backlog.md` RA4.
**Source:** Scratch (Reflekt Integration Program, RA4)
**Dependencies:** RA3 `adhoc-ReflektSprintOwnership` (Done) — `RetroSession` references 1..N Fokus sprints and stores the `FokusSprintSnapshot` projection. Fokus sprint analytics (Pass 3c, Done) — completion / disruption / carry-over / health-score computation. The `ISprintAnalyticsService` gRPC contract and its Fokus server implementation already exist; this feature adds the Reflekt-side client and the panel.
**Status:** Ready
**Plan:** None

---

## Purpose

The retro room currently has no view of the sprint it reflects on — participants debate "did we have a lot of scope change?" from memory. This feature surfaces Fokus's already-computed sprint metrics inside Reflekt as a persistent, slide-out **sprint context panel**: completion rate, disruption rate, carry-over rate, the health score, and — when a sprint's disruption ran high — the tickets added mid-sprint. The data is **anonymized aggregate only** (no developer ever appears), pulled from Fokus over gRPC. It is a reference tool available in every retro phase to every participant, delivering the integration payoff named in proposal §9 and ADR-019. See `docs/kb/health-score.md` for the health-score definition; the panel **displays** Fokus's values and never recomputes them.

## Entities

This feature introduces no new aggregate and owns no new persisted state. It reads two things that already exist and renders them.

### Referenced Fokus sprints (read, owned by RetroSession — RA3)
- A `RetroSession` already references **1..N Fokus sprints** (ADR-018) and holds a thin `FokusSprintSnapshot` per referenced sprint (sprint id, name, dates, completion rate, disruption rate, carry-over rate, health score), refreshed out-of-band by the `SprintSynced` integration event.
- The panel renders **one section per referenced sprint**, in the order the sprints occurred (earliest sprint start first). A single-sprint retro — the common case — renders one section.

### Anonymized sprint summary (read, owned by Fokus)
- The cross-module read contract is `ISprintAnalyticsService` / `AnonymizedSprintSummary` (proposal §2, ADR-019), defined in `Blocks.Contracts`, Fokus implementing, Reflekt consuming. The contract is the privacy boundary: only anonymized aggregates cross it.
- Per referenced sprint, the panel needs: **completion rate**, **disruption rate**, **carry-over rate**, the **health score with its color band** (`docs/kb/health-score.md`), each rate shown with its Fokus-defined indicator color where Fokus exposes one, and — gated on disruption (Rule 6) — a **list of mid-sprint scope additions**: each entry the ticket key and title only, **never an assignee or any person**.
- *(Architecture, delegated to the planner: whether `AnonymizedSprintSummary` already carries the scope-addition list, the per-rate indicator bands, and the disruption band that Rule 6 gates the whole scope-additions block on — or needs enriching to expose them — and whether the contract is called once per sprint (Reflekt looping over N) or gains a batch operation. Note: per-rate / disruption banding is flagged **untested** in `docs/kb/health-score.md` — confirm Fokus actually surfaces it before Rule 6 relies on it. The panel's data needs are fixed here; the contract shape is the plan's call.)*

### Sprint context (display read-model, assembled by Reflekt)
- A per-retro, per-sprint view assembled from the gRPC pull, with the `FokusSprintSnapshot` as fallback. Each per-sprint entry carries a **freshness state**: `live` (from the gRPC pull), `cached` (gRPC unavailable, served from the snapshot), or `unavailable` (neither available — e.g. a sprint never synced).
- Held for the lifetime of the retro session (Rule 7); not persisted as new domain state.

## User Flows

```
Flow 1: Open the panel (single-sprint retro)
1. A participant is in a retro room (any phase). A persistent "Sprint Context" button sits in the top bar.
2. The participant clicks it; the panel slides out.
3. The panel shows one section for the retro's sprint: completion / disruption / carry-over rates, each
   with its indicator color, and the health score with its band.
4. If that sprint's disruption is high (Rule 6), a "Added mid-sprint" block lists the top added tickets
   (key + title, up to 3, "+N more" if more); otherwise the block is absent.
5. The participant closes the panel; reopening it shows the same data without re-fetching from Fokus.

Flow 2: Multi-sprint retro
1. The retro references more than one Fokus sprint (a skipped retro now covers two).
2. Opening the panel shows one section per referenced sprint, earliest sprint start first, each section
   identified by sprint name and dates and carrying its own metrics and (if applicable) scope-additions block.

Flow 3: Data unavailable or partial
1. On retro load the sprint context is fetched once. If the Fokus pull fails, each sprint falls back to its
   stored snapshot and that section is marked as cached/last-known.
2. If a referenced sprint has neither a live pull nor a snapshot (never synced), its section shows an
   "unavailable" state with the sprint name/dates and no metrics — the rest of the panel still renders.
3. While the initial fetch is in flight, the panel shows a loading state.

Flow 4: Guest participant
1. A guest (no account) is in the retro. The button and panel are available to them exactly as to
   authenticated participants — the data is anonymized, so there is no additional access gate.
```

## API Surface

The Vue panel reads from one new browser-facing endpoint on Reflekt; that endpoint performs the cross-module gRPC pull to Fokus (the browser never calls gRPC directly) and applies the snapshot fallback.

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|-------------|
| GET | `/api/retro-sessions/{retroSessionId}/sprint-context` | Any retro participant (open participation, ADR-021; authenticated or guest) | — | Array of per-sprint anonymized summaries, each with: sprint name + dates, completion / disruption / carry-over rate (+ indicator color), health score + band, freshness state (`live` / `cached` / `unavailable`), and — when disruption is high — a list of added tickets (key + title, no assignee) | 200, 404 |

Response conditions:
- **200** — always when the retro session exists, even if every section is `cached` or `unavailable`; the panel renders gracefully from whatever is available. Sections are ordered earliest sprint start first.
- **404** — the retro session id does not exist.
- A Fokus/gRPC failure is **not** surfaced as an error status; it degrades each affected section to `cached` (snapshot) or `unavailable` and still returns 200.

*(The browser→Reflekt route name above is the proposed surface; the planner may adjust it to fit the RA3 route family. The Reflekt→Fokus hop is the existing `ISprintAnalyticsService` gRPC contract, not a REST surface.)*

## Real-time Events

This feature produces no real-time (SignalR) events. The panel is request/response: fetched once on retro load, cached for the session, rendered on demand. It does not update live when the panel is open, and it does not push to other participants.

## Business Rules

1. The panel is reachable from a persistent top-bar button available in **every retro phase** to **every participant**, authenticated or guest. There is no phase-specific or role-specific gating.
2. The panel renders **one section per Fokus sprint the retro references**, ordered by sprint start ascending (earliest first). A single-sprint retro renders exactly one section.
3. Each section displays the sprint's **completion rate, disruption rate, carry-over rate, and health score**, taken as Fokus computes them — the panel never recomputes a metric. The health score is shown with its color band per `docs/kb/health-score.md`; each rate is shown with its Fokus-defined indicator color where Fokus exposes one.
4. The panel shows **only anonymized aggregate data**. It never displays any developer name, avatar, assignee, or any per-developer metric. Scope-addition entries carry the ticket key and title only — never who it was assigned to.
5. The sprint context is obtained from Fokus over the `ISprintAnalyticsService` gRPC contract. Per-developer data does not cross the boundary — the `AnonymizedSprintSummary` contract enforces this at the protocol level (ADR-019).
6. A section shows an **"added mid-sprint"** block **only when that sprint's disruption sits in Fokus's amber-or-worse health band** — the panel reuses Fokus's existing disruption threshold and does not define its own. Below that band, the block is absent. The gate is evaluated per sprint section independently.
7. When shown, the scope-additions block lists **up to 3** added tickets (key + title), ordered by most-recently-added first; if more than 3 were added, it indicates the remaining count ("+N more").
8. The sprint context is **fetched once when the retro session is loaded and cached for the lifetime of that retro session**; reopening the panel does not trigger a new Fokus call. (Cache locus — client or server — is the planner's choice; the observable guarantee is at-most-once-per-session under normal operation.)
9. If the Fokus pull fails or is unavailable for a referenced sprint, that section **falls back to the stored `FokusSprintSnapshot`** and is marked as last-known/cached. If neither a live pull nor a snapshot exists for a sprint (e.g. never synced), that section shows an **unavailable** state with the sprint name and dates and no metrics. A failure of one sprint's data never blanks the whole panel.
10. No cross-module interaction is a direct project reference; the panel's data crosses the module boundary only over the `ISprintAnalyticsService` gRPC contract and (for fallback) the locally-stored snapshot (ADR-001, CLAUDE.md "No cross-module project references").

## Acceptance Criteria

- [ ] In a single-sprint retro, opening the panel via the top-bar button shows one section with the sprint's completion, disruption, and carry-over rates and its health score + color band, matching the values Fokus reports for that sprint.
- [ ] The top-bar button and panel are present and functional in every retro phase and for a guest participant (no account), not only for authenticated users.
- [ ] In a retro referencing two Fokus sprints, the panel shows two sections ordered earliest-sprint-start first, each with its own sprint name, dates, and metrics.
- [ ] For a sprint whose disruption is in the amber-or-worse band, the section shows an "added mid-sprint" block listing up to 3 added tickets by key and title (with "+N more" when more than 3 exist); for a sprint below that band, no such block appears.
- [ ] The panel never renders any developer name, avatar, or assignee, and no per-developer metric, in any section or in the scope-additions block — verified against a sprint that has per-developer data in Fokus.
- [ ] `GET /api/retro-sessions/{retroSessionId}/sprint-context` returns 200 with the ordered per-sprint array for an existing retro, and 404 for an unknown retro session id.
- [ ] When the Fokus data source is unavailable, the endpoint still returns 200 and each affected section is marked cached (served from the snapshot) or unavailable (no snapshot), with the rest of the panel rendering normally — no error status and no blank panel.
- [ ] Reopening the panel within the same retro session does not trigger a second Fokus pull (data is served from the session cache).
- [ ] The solution contains no direct Reflekt↔Fokus project reference; the panel's Fokus data flows only over the `ISprintAnalyticsService` gRPC contract.

## Out of Scope

- **Live refresh while the panel is open / mid-retro re-sync** — the panel is session-cached; if Fokus re-syncs a sprint during the retro, the panel keeps the data it loaded with. The out-of-band `SprintSynced` snapshot refresh continues, but the open panel does not re-pull. Deferred — retros are short and a live-updating reference panel adds complexity without clear value.
- **Manual "refresh" affordance** — no user-triggered re-fetch in v1, for the same reason.
- **Per-developer drill-down or any individual metrics** — permanently out by ADR-019; the panel is anonymized aggregate only.
- **Reflekt → Fokus data flow** — deferred by ADR-019; no retro outcome is written back to Fokus.
- **Metrics beyond the §9 set** — only completion / disruption / carry-over / health score / mid-sprint scope additions. Other Fokus analytics (cycle time, bug ratio, leaderboards, dev/epic breakdowns) are not surfaced in the panel.
- **Cross-sprint aggregate / blended metric** — in a multi-sprint retro each section stays independent; the panel never blends the referenced sprints into a single combined rate or health score. A blended figure would require recomputation, which Rule 3 forbids (the panel only displays Fokus's per-sprint values).
- **Frontend routing-shell merge** — the panel lives in the existing Reflekt SPA; unifying the two SPAs is RA5 (`adhoc-ReflektFrontendMerge`).
- **SM waiting-room / access gating** — retro access control is `adhoc-RetroWaitingRoom` (ADR-021); this feature assumes a participant already in the room.
