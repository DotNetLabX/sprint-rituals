# Planning Poker

**Traces to:** `docs/architecture/poker-v1.md` (product draft this spec graduates)
**Source:** Scratch
**Dependencies:** Fokus (sprint catalog + Jira ticket write-back), Identity (Google auth + suite Person), guest participation (session-scoped, no Person — same identity stance as Reflekt's ADR-022), unified frontend routing shell (RA5)
**Status:** Implemented (2026-06-21)
**Plan:** `docs/specs/F6-PlanningPoker/delivery/plan.md`

---

## Purpose

A real-time planning poker tool for collaborative sprint estimation, integrated with the suite rather than an external app. A Scrum Master runs an estimation ceremony; the team votes on items with a Fibonacci scale; agreed story points are written back to Jira through Fokus. It runs in two modes — **sprint-bound** (items pulled from a Fokus sprint, estimates written back to Jira) and **free** (items typed in manually, no write-back) — so a team can also estimate ad-hoc work the way a standalone poker app would. It is the suite's third sprint ritual alongside Fokus (analytics) and Reflekt (retro).

## Entities

Own data store. Four concepts.

### PlanningSession
- **Mode** — Sprint-bound | Free (set at creation, immutable).
- **Sprint reference** — the Fokus sprint being estimated. Required in Sprint-bound mode; absent in Free mode.
- **Sprint name** — snapshot of the sprint name at creation (Sprint-bound only).
- **Title** — a human label for the ceremony. Required in Free mode (no sprint to name it); optional in Sprint-bound.
- **Created by** — the authenticated user who created it; this person is the session's Scrum Master. Required, never a guest.
- **Status** — Active | Closed.
- **Created at / Closed at** — timestamps (Closed at set when the SM closes).
- One session per ceremony. The SM creates it and shares the room link.

### SessionParticipant
- **Session** — the session joined.
- **Person link** — the suite identity, for Google-authenticated participants only; absent for guests.
- **Display name** — shown in the room.
- **Is voter** — all participants are voters in v1.
- **Joined at** — timestamp.
- A guest is a session-scoped participant with no identity link and no cross-session matching.

### EstimationItem
- **Session** — the owning session.
- **Label** — what is being estimated: in Sprint-bound mode this is the Jira ticket (key + summary, snapshotted from Fokus); in Free mode it is a title the SM types.
- **Ticket reference** — the Jira ticket key, present in Sprint-bound mode only (drives write-back); absent in Free mode.
- **Status** — Voting | Revealed | Decided | Skipped.
- **Final estimate** — the agreed value; set when the SM decides; null until then.
- **Order** — sequence within the session.
- **Decided at** — timestamp (set on decide).
- One item per ticket per session (Sprint-bound); items are identified by their ticket so a refresh is idempotent — see Rule 19.

### Vote
- **Item** — the estimation item.
- **Participant** — who cast it.
- **Value** — one of the Fibonacci values including "?".
- **Submitted at** — timestamp.

Reference: `docs/architecture/poker-v1.md` §4 for the originating product model.

## Estimation Scale

Fixed Fibonacci: **1, 2, 3, 5, 8, 13, 21, ?**. "?" means "not enough information to estimate" — it is an abstain, never a final estimate, and never triggers an automatic skip. Configurable scales (T-shirt, custom) are out of scope.

## User Flows

```
Flow 1: Create session (SM, authenticated)
1. SM opens Poker in the suite (must be signed in — guests cannot create).
2. SM chooses a mode:
   - Sprint-bound: selects a sprint. Default selection is the first future sprint
     (the one after the current active sprint); the selector also offers recent sprints.
   - Free: enters a title for the ceremony; no sprint.
3. System creates the session and generates a room link (/poker/{sessionId}).
4. SM shares the link with the team.
```

```
Flow 2: Join (anyone with the link)
1. Participant opens the room link.
2. Participant signs in with Google or joins as a guest (picks a display name).
3. Participant enters the room — sees the participant list, item list, and estimation area.
```

```
Flow 3: Populate items
- Sprint-bound: the item list is the current tickets in the selected sprint, pulled from Fokus.
  The SM can refresh to pick up tickets moved in/out of the sprint in Jira during the session.
- Free: the SM adds items manually (one title each). Guests cannot add items.
```

```
Flow 4: Estimate an item
1. SM selects an item to estimate.
2. Voting phase: every participant picks a card; choices are hidden, others see only "voted".
3. Reveal: automatic once all present participants have voted, or the SM force-reveals early.
4. All votes are shown with participant names.
5. SM picks the winner from the values actually cast (e.g. "3× voted 5, 2× voted 8").
6. Sprint-bound: the winning value is written to Jira via Fokus; the item is marked Decided
   only after the write-back is confirmed. Free: the item is marked Decided locally.
7. SM moves to the next item or closes the session.
```

```
Flow 5: Skip an item (SM only)
1. SM chooses to skip the current item.
2. Item status becomes Skipped — no estimate, no write-back.
   (Skip is always a manual choice, including when every vote is "?".)
```

```
Flow 6: Close session (SM only)
1. SM closes the session.
2. Status becomes Closed; the room becomes a read-only summary and cannot be reopened.
3. Summary shows: items estimated (count), items skipped (count), total story points
   assigned, and each item's final estimate.
```

```
Flow 7: Leave and rejoin
1. Any participant may close their tab and rejoin later via the room URL while the session is Active.
2. The session stays Active as long as at least one participant remains, and is never deleted or
   deactivated by disconnects — including the SM disconnecting.
3. After the SM closes the session, the URL shows the read-only summary.
```

## API Surface

**Indicative contract, non-binding** — routes, status codes, and field shapes are product-level intent; the plan owns the final shapes.

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|-------------|
| POST | `/api/poker/sessions` | Authenticated (SM) | mode, sprintId (sprint-bound), title (free) | session id + room URL | 201; 400 sprint-bound without a sprint; 400 free without a title; 401 unauthenticated |
| GET | `/api/poker/sessions/{id}` | Anyone with link | — | session state: mode, status, items, participants | 200; 404 unknown session |
| POST | `/api/poker/sessions/{id}/participants` | Anyone (Google or guest) | display name (guest) | participant id + role | 200; 404; 409 session closed |
| GET | `/api/poker/sprints` | Authenticated (SM) | — | available sprints including future state | 200; 401 |
| GET | `/api/poker/sessions/{id}/tickets` | SM | — | current sprint tickets (key, summary, current story points) | 200; 403 non-SM; 409 free-mode session |
| POST | `/api/poker/sessions/{id}/items` | SM | item title | item id | 201; 403 non-SM; 409 sprint-bound session |
| POST | `/api/poker/sessions/{id}/close` | SM | — | session summary | 200; 403 non-SM; 409 already closed |
| GET | `/api/poker/sessions/{id}/summary` | Anyone with link | — | summary stats + per-item final estimates | 200; 404 |

The estimation loop (select, vote, reveal, decide, skip) runs over the real-time channel, not REST. Read endpoints (`GET session`, `GET summary`) are reachable by anyone holding the room link, including a non-participant viewing a closed session's summary (Rule 18).

**Sprint availability (`GET /api/poker/sprints`) is a small lift, not new sync work.** `SprintState.Future` already exists (`Fokus.Domain/Sprint/SprintState.cs`, `Jira.Contracts/SprintState.cs`) and future sprints already sync into Fokus via `POST /api/sync/backlog`. The build is a **new Poker-scoped sprint query** that includes Future state. Do **not** relax `SprintRepository.GetAnalyticsSprintsAsync` — it deliberately filters to `Closed || Active` (line 19) and is shared by ~20 analytics endpoints; widening it would leak Future sprints into all of them. Add a sibling query (e.g. `GetPokerSprintsAsync` returning Active + Future + recent Closed) instead. The product draft (poker-v1.md §9.1) overstated this as "Add `Future` to the enum + extend sync"; the enum and sync already exist.

**Error responses:** writing a story point back to Jira can fail (Jira unavailable, value rejected, timeout). On failure the item is **not** marked Decided, the SM sees a clear error, and the action can be retried; no vote data is lost.

## Real-time Events

One room channel per session. **Indicative contract, non-binding** — event names and payloads are product-level intent; the plan owns the final shapes.

| Direction | Event | Payload | Trigger |
|-----------|-------|---------|---------|
| Server → Client | ParticipantJoined | display name, participant id | someone joins |
| Server → Client | ParticipantLeft | participant id | someone disconnects |
| Server → Client | ItemSelected | item id, label | SM selects an item to estimate |
| Server → Client | VoteReceived | participant id (no value) | a vote is in, or a voter changes their vote pre-reveal — show "voted", not the value |
| Server → Client | VotesRevealed | list of {participant id, display name, value} | all votes visible |
| Server → Client | EstimateDecided | item id, final estimate | SM picked the winner (and, sprint-bound, write-back confirmed) |
| Server → Client | WriteBackFailed | item id, reason | sprint-bound write-back to Jira failed — item stays not-Decided, SM may retry (Rule 10) |
| Server → Client | ItemSkipped | item id | SM skipped the item |
| Server → Client | SessionClosed | summary stats | SM closed the session |
| Client → Server | SelectItem | item id | SM only |
| Client → Server | SubmitVote | value | any voter |
| Client → Server | ChangeVote | value | any voter, before reveal only |
| Client → Server | RevealVotes | — | SM only |
| Client → Server | DecideEstimate | value (from cast votes) | SM only |
| Client → Server | SkipItem | — | SM only |
| Client → Server | CloseSession | — | SM only |

## Business Rules

1. Only an authenticated (Google) user can create a session; the creator is that session's Scrum Master. Guests cannot create sessions.
2. SM-only actions: select an item, reveal votes, decide the estimate, skip an item, add a manual item (free mode), and close the session. The system enforces these against the session creator's identity.
3. Guests may join and vote, but cannot add items or perform any SM action.
4. Every participant is a voter in v1; there is no observer (watch-only) role.
5. The estimation scale is fixed Fibonacci 1, 2, 3, 5, 8, 13, 21, ?.
6. "?" is an abstain: it can never be selected as the final estimate, and a round needs at least one numeric vote to be decided.
7. Votes are hidden until reveal. Auto-reveal fires when every currently-connected voter has a vote on the active item — including the case where a disconnect leaves all remaining connected voters already voted. A voter rejoining before reveal does not discard votes already cast. The SM may also force an early reveal at any time.
8. The final estimate must be one of the values actually cast in that round (no arbitrary value entry in v1).
9. Skip is a manual SM action only — there is no automatic skip, including when all votes are "?". A skipped item receives no estimate and no write-back.
10. **Sprint-bound mode:** the agreed estimate is written back to the item's Jira ticket through Fokus. The item is marked Decided only after the write-back is confirmed; on failure the SM is shown an error and may retry, and no vote data is lost.
11. **Free mode:** there is no sprint and no Jira write-back; items are added manually by the SM and estimates are recorded only within the session.
12. In sprint-bound mode the SM can refresh the item list during the session to reflect tickets moved in or out of the sprint in Jira since the session started.
13. A session stays Active while at least one participant is present. Participant disconnects — including the SM's — never delete or deactivate it. Anyone with the room URL can rejoin an Active session.
14. Only the SM can close a session. A Closed session is a read-only summary and cannot be reopened.
15. Authenticated participants are linked to their suite Person; guests have no Person and are session-scoped, with no cross-session or cross-tool matching. Poker defines its **own** guest-token scheme (see Rule 23).
16. Sprint-bound default selection is the first future sprint after the active one; the selector also lists recent sprints.
17. The session summary reports items estimated, items skipped, total story points assigned, and each item's final estimate.
18. A Closed session's summary remains readable via the room URL by anyone holding the link, including a viewer who never joined; only mutating actions are rejected once a session is Closed.
19. In Sprint-bound mode there is exactly one item per ticket per session. A refresh (Rule 12) adds items for newly-present tickets and never deletes items already in the session — a ticket pulled out of the sprint keeps its item (and any votes/decision); re-adding a previously-removed ticket reuses its existing item and votes rather than creating a duplicate.
20. Poker access is open-link join: anyone with the room URL joins immediately (Google or guest), with no Scrum-Master approval step. Poker does not use a waiting room — this is intentionally distinct from Reflekt's SM-gated access (ADR-021).
21. A participant has at most one vote per item; changing a vote replaces the existing one (no duplicate vote rows), so the revealed tally counts one value per participant.
22. The Scrum Master is also a participant and votes like anyone else; SM authority is derived from being the session creator, not from a stored participant role.
23. **Guest-token scheme is Poker-local — a conscious duplication for v1.** ADR-016 kept guest-token authentication module-local to Reflekt ("Reflekt retains its guest-token authentication for anonymous retro participants"). Poker is a separate module and defines its **own** session-scoped guest-token scheme rather than sharing Reflekt's. This is a deliberate "yes, duplicate for v1" decision, not accidental drift: extracting a shared guest-auth building block is premature with two consumers and would couple the modules. Revisit if a third tool needs anonymous join. (Architect to confirm at plan time whether a shared block is warranted; default is duplicate.)

## Acceptance Criteria

- [ ] An unauthenticated user cannot create a session (`POST /api/poker/sessions` → 401); a guest who joined cannot create one.
- [ ] Creating a sprint-bound session with a valid sprint returns 201 with a room URL; the item list matches the sprint's current Jira tickets.
- [ ] Creating a free-mode session requires a title and starts with an empty item list; the SM can add items and a guest cannot (`POST .../items` by a guest → 403).
- [ ] A second user opening the room URL can join as Google or guest and appears in the participant list for everyone in real time.
- [ ] During voting, other participants see only that someone has voted, not the value, until reveal.
- [ ] Reveal happens automatically when all present participants have voted, and the SM can force reveal before that.
- [ ] The SM can only pick a final estimate from the values actually cast; "?" cannot be chosen as the final estimate.
- [ ] In sprint-bound mode, deciding an estimate writes the value to the Jira ticket; if the write-back fails, the item is not marked Decided and the SM sees an error with a retry path.
- [ ] In free mode, deciding an estimate records it in the session with no Jira call.
- [ ] Skipping an item marks it Skipped with no estimate and no write-back, even when all votes were "?".
- [ ] When the SM disconnects, the session stays Active and any participant can still act; the session is not deleted while ≥1 participant remains.
- [ ] A participant who reloads or rejoins via the URL re-enters the Active session with current state.
- [ ] Closing a session (SM only) makes the room a read-only summary showing items estimated, items skipped, total story points, and per-item estimates; a non-SM close attempt → 403.
- [ ] A non-participant holding the room link can read a Closed session's summary (`GET .../summary` → 200) but cannot perform any mutating action.
- [ ] Creating a sprint-bound session without a sprint → 400; creating a free-mode session without a title → 400.
- [ ] The sprint selector defaults to the first future sprint after the active one.
- [ ] Refreshing a sprint-bound session picks up tickets newly added in Jira and preserves existing items and their votes (including Decided items whose ticket was removed).
- [ ] A round with no numeric votes cast (all "?", or none) cannot be decided — only skipped.
- [ ] A participant who votes twice on the same item ends with a single vote (the latest value); the revealed tally counts one value per participant.
- [ ] Auto-reveal fires when the last connected voter votes, and also when a disconnect leaves all remaining connected voters already voted.

## Out of Scope

- **Observer (watch-only) role** — all participants vote in v1; PO/stakeholder watching is v2.
- **Configurable scales** — T-shirt sizes, custom sequences; Fibonacci covers the need.
- **Re-vote rounds** — single round per item; votes cluster enough that the SM can decide.
- **Arbitrary final value** — selecting a value not in the cast votes.
- **Per-item comments / discussion thread** — only voting is a confirmed participant action in v1; richer per-item interaction is deferred.
- **Estimation accuracy analytics** — comparing estimates vs actuals via Fokus throughput; v3+.
- **Ticket detail panel** — Jira description/acceptance/comments inside Poker; keep Jira open instead.
- **Session history / browsing past sessions** — no current need.
- **Backlog browsing** — sprint-bound items come from the sprint as already arranged in Jira.
- **Per-item timer** — optional voting countdown; not needed for current team dynamics.
- **Cross-tool integration event to Reflekt** — no concrete consumer yet.
- **Cross-session guest identity matching** — guests are session-scoped by design.

## Architecture Decisions & Open Questions

Feasibility/architecture items for the architect — not product-shape gaps. The first three were resolved by Phase-1 analysis against source (recorded as decisions); the fourth remains open.

- [x] **Jira story-points field — RESOLVED: `customfield_10016`.** Fokus already reads and deserializes story points from `customfield_10016` (`Jira.Contracts/JiraIssue.cs` `JiraIssueFields.StoryPoints`, in the `IssueFields` fetch list). The write-back reuses the same field id — no new field discovery needed.
- [x] **Fokus Jira write capability — RESOLVED: write-back is a NET-NEW path, and the feature's highest build risk.** Fokus's *only* Jira write today is `UpdateSprintAsync` → `POST /rest/agile/1.0/sprint/{id}` (the **Agile API**, sprint metadata). Writing story points requires `PUT /rest/api/3/issue/{key}` with `{fields:{customfield_10016}}` — a **different REST API** and a **net-new `IJiraClient` + `IJiraApi` method** (e.g. `SetStoryPoints`). This is not an extension of existing write capability; it is the riskiest build item and the plan must scope it explicitly (auth, error mapping, value serialization).
- [x] **Ticket freshness mechanism — RATIFIED: Option A, live Jira pass-through.** `GetSprintTickets` hits Jira live via Fokus on every call; the Poker UI exposes a refresh button (Rule 12). Live accuracy during a ceremony outweighs the round-trip cost, and `IJiraClient.GetSprintIssuesAsync(sprintId)` already exists, so Option A is cheap to wire. Option B (cached read from the Fokus sync snapshot) is rejected: it goes stale when tickets move mid-session. **This decision sets the shape of the `GetSprintTickets` gRPC contract** (live pass-through, no caching on this path). (Ratified by PO+architect consensus from product intent — accuracy in the live ceremony is the product guarantee behind Rule 12.)
- [ ] **Person for a not-yet-known Google participant** — Rule 15 links authenticated participants to their suite Person and gives guests none (ADR-022). For a Google user who authenticates but is not yet a Person in Identity, does Poker mint a Person via the cross-module Person service (as Reflekt does at its waiting-room timing, ADR-016) or defer the link and treat them as session-scoped? Poker has no waiting room (Rule 20), so the minting trigger differs from Reflekt's. (Architect to decide against the Identity contract.)
