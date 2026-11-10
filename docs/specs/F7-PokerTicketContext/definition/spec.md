# Poker Ticket Context

**Traces to:** `docs/specs/F6-PlanningPoker/definition/spec.md` (the Poker room this extends) · `docs/architecture/poker/v1.md` §8.1 (planning room main view)
**Source:** Scratch — owner feedback on the running Poker room, 2026-10-05 (shaping record and owner rulings: `definition/ideas.md`)
**Dependencies:** F6-PlanningPoker (implemented), Fokus (Jira ticket data and story-point write-back), Identity (Admin role)
**Status:** Ready
**Model:** claude-opus-5-5
**Plan:** None
Rule list: `docs/specs/F7-PokerTicketContext/definition/spec-rules.md` (stamped)

---

## Purpose

The Poker room shows only a ticket's key and title, so the team estimates half-blind. Participants cannot look at any ticket but the current one, the ticket list is unreadable on a laptop or split screen, and every untouched ticket is labelled "Voting", so the list feels endless. This feature gives the room **just enough context to orient** — type, labels, a short story preview, subtasks, the AI estimate — and **sends people to Jira for everything else**. It also lets the team filter out work it does not estimate (bugs, subtasks, tests), lets the facilitator move through the list faster, and lets the facilitator correct or redo an estimate.

**Guiding principle (owner, 2026-10-05):** Poker shows enough to know *what* is being estimated; the real detail lives in Jira, and the room encourages opening it.

**Terms.** *Facilitator* = the session's Scrum Master: its creator, who holds every Scrum-Master action (F6 Rules 1, 2, 22). The screen may keep labelling these controls "SM". *Participant* = anyone who joined the room, facilitator included. *Joined participant* = a signed-in person or a guest who joined (F6 Flow 2); someone who only opens the link without joining is not one.

## Entities

Extends F6's entities; introduces one new one.

### PlanningSession (extended)
- **Current ticket** — at most one item the whole room is on. The same for everyone, kept across reloads, rejoins and disconnects; set only by the facilitator; votes apply only to it. None until the facilitator first selects a ticket.
- **Session filter** (sprint mode only) — the issue types hidden and the labels selected for this session. Starts as a copy of the team default; changing it never changes the team default.

### EstimationItem (extended)
- **Status** — gains a new initial value, **Not started**: Not started | Voting | Revealed | Decided | Skipped. Transitions are in § Status transitions.
- **Voting round** — each time voting starts on an item it starts a new round; a reset starts another. Only the item's latest round is live.
- **Ticket context** (sprint mode only) — taken from Jira when items are pulled or refreshed:
  - **Issue type** — e.g. Story, Task, Bug.
  - **Labels** — zero or more Jira labels.
  - **Description preview** — the Jira description flattened to plain text (lists become lines, images dropped, links shown as their text).
  - **Subtasks** — zero or more, each with its Jira key, title and assigned developer ("Unassigned" when none).
  - **Jira story points** — the points the ticket carries in Jira, if any. 0 counts as having points.
  - **AI estimate** — the value of the team's AI-estimate Jira field. Valid only if it is a number ≥ 0; anything else (empty, text, negative) counts as **no AI estimate**. Any valid number, not limited to the card scale.
  - **Context state** — loaded | could not load (the last fetch from Jira failed) | no longer in sprint (the ticket left the sprint; F6 Rule 19 keeps the item).
- **Round AI value** — the AI estimate as it stood when the current round started; fixed for that round (see Rule 20).
- **Final estimate** — a typed number ≥ 0 in whole or half steps (e.g. 0, 2.5, 4), or the round AI value exactly as it is. Null until decided.
- **Estimate source** — Team | AI. AI only when the round AI value was adopted unchanged; Team otherwise.

### PokerTeamSettings (new — one per install)
The suite runs one team per install, so these settings are app-wide.
- **Default hidden issue types** — a set of issue-type names. Seed value: Bug, Sub-task, Test.
- **Default selected labels** — a set of label names. Seed value: empty.
- **AI estimate enabled** — yes/no. Seed value: no.
- Readable by any signed-in user; changeable only by users with the Admin role.

### Status transitions

| Action (facilitator) | Not started | Voting | Revealed | Decided | Skipped |
|---|---|---|---|---|---|
| Select (room moves to it) | → Voting, new round | stays Voting, round continues | stays Revealed | stays Decided | stays Skipped |
| Reveal | — | → Revealed | — | — | — |
| Decide | — | — | → Decided | — | — |
| Edit estimate | — | — | — | stays Decided, new value | — |
| Skip | — | → Skipped | → Skipped | — | — |
| Reset | — | new round (current: → Voting; else → Not started) | same | same | same |

"—" = not allowed. Selecting never reopens a Revealed, Decided or Skipped item; only Reset starts voting again.

## User Flows

```
Flow 1: Read a ticket's context (joined participants, sprint mode)
1. The ticket shows its key and title, plus: issue type, labels, the Jira story points if any,
   and an "Open in Jira" link that opens the ticket in a new tab.
2. Below that: the description preview, cut to 3 lines as displayed on that person's screen,
   followed by "Read in Jira" when there is more. An empty description shows "No description"
   and no "Read in Jira".
3. Below that: every subtask on one line — key (opens Jira in a new tab), title, developer.
4. If Jira could not be read, the ticket shows "Couldn't load details from Jira — refresh to retry"
   and keeps any context loaded earlier. A ticket that left the sprint shows "No longer in sprint"
   with its last known context.
```

```
Flow 2: Look at another ticket privately (any participant, facilitator included)
1. A participant opens a private look at a listed ticket — by clicking it (non-facilitator), or with
   the "Look" (eye) button on the row (facilitator; the facilitator's plain click still moves the room).
2. Their own view shows that ticket: its context (Flow 1), status, final estimate if decided, and its
   votes if they were revealed. The room's current ticket does not change for anyone.
3. A banner names the room's current ticket, with "Back to current". When the room's ticket is
   Voting and this person has not voted, the banner says "Voting is open".
4. No voting cards are shown during a private look; votes go only to the room's current ticket.
5. If the facilitator moves the room meanwhile, the banner updates; the person is not pulled back.
6. "Back to current" returns them to the room's current ticket in its actual phase — voting cards
   only if it is Voting.
```

```
Flow 3: Use the ticket list on a small screen (anyone)
1. Each person can collapse the list to a thin strip with an always-visible expand control,
   and expand it again.
2. Each person can drag the list wider or narrower within limits that always leave the estimation
   area usable; a saved width that no longer fits a smaller window is reduced to fit.
3. Both choices are remembered in that browser on that device, for any room.
4. Each row shows the ticket key, as much of the title as fits (full title on hover), its status,
   and its final estimate once decided.
```

```
Flow 4: Filter the sprint's tickets (facilitator, sprint mode)
1. A session starts with the team's default filter (seed: Bug, Sub-task and Test hidden).
2. The facilitator opens the filter: a multi-select of the issue types present in the session's
   tickets (ticked = shown) and a multi-select of the labels present (ticked = only tickets with a
   ticked label are shown; none ticked = no label filtering).
3. The facilitator changes the selection; everyone's list updates at once to the same result.
4. Everyone sees how many tickets the filter hides (e.g. "4 hidden by filter").
5. The change applies to this session only.
```

```
Flow 5: Move to the next ticket (facilitator)
1. The facilitator presses "Next unestimated".
2. The room moves to the next eligible ticket below the current one in the visible list, wrapping to
   the top; with no current ticket, the first eligible ticket from the top. The current ticket is
   never the target.
3. If none is eligible, the button is disabled and reads "Nothing left to estimate".
   (Eligibility: Rule 34.)
```

```
Flow 6: Estimate with the AI as a voter (sprint mode, AI estimate enabled)
1. When a round starts on a ticket with an AI estimate, the vote area shows an "AI Estimator" tile
   beside the people, already marked "voted".
2. Its value stays hidden from everyone until reveal.
3. Auto-reveal waits for the connected people only; the AI tile never holds it up.
4. After reveal, the AI value is shown next to — not inside — the people's tally.
5. On a ticket without an AI estimate, or in a round started while the switch was off, no AI tile appears.
```

```
Flow 7: Decide the final estimate (facilitator, after reveal)
1. The facilitator picks a value the people cast, the AI value (if shown), or types a number
   ≥ 0 in whole or half steps.
2. Sprint mode: the number is written to Jira first; the ticket is Decided only once Jira confirms,
   and its Jira story points then show the new value. Free mode: recorded in the session.
3. If the AI value was adopted unchanged, the estimate is AI-sourced.
```

```
Flow 8: Change a decided estimate (facilitator)
1. On a Decided ticket — the current one or one in the facilitator's private look — the facilitator
   edits the number (≥ 0, whole or half steps).
2. Sprint mode: written to Jira first; on failure the old value stays, an error shows, and the
   facilitator can retry. Free mode: recorded in the session.
3. An edited estimate is Team-sourced.
```

```
Flow 9: Reset a reshaped ticket (facilitator)
1. On a ticket that is Voting, Revealed, Decided or Skipped — the current one or one in the
   facilitator's private look — the facilitator presses Reset.
2. Its votes, final estimate and source in this session are cleared, and a new round begins:
   the current ticket returns to Voting at once; any other ticket becomes Not started.
3. Jira keeps the points it had; they change only when a new estimate is decided.
```

```
Flow 10: Set the team defaults (Admin)
1. An Admin opens Poker settings.
2. The Admin picks hidden issue types and selected labels from the names seen in the team's Jira
   data, or types a name, and turns the AI estimate on or off.
3. New sessions use the defaults. The AI switch applies as in Rule 21.
```

## API Surface

**Indicative contract, non-binding** — routes, status codes and field shapes are product-level intent; the plan owns the final shapes and the split between requests and real-time messages. Changes to F6's surface only.

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|-------------|
| GET | `/api/poker/sessions/{id}` | Anyone with link | — | (extended) current ticket, session filter, each item's status (incl. Not started), final estimate + source; ticket context only for joined participants; no unrevealed vote or AI value | 200; 404 |
| GET | `/api/poker/settings` | Authenticated | — | team defaults + AI switch | 200; 401 |
| PUT | `/api/poker/settings` | Admin | hidden types, selected labels, AI switch | updated settings | 200; 400 empty or duplicate name; 401; 403 non-Admin |
| PUT | `/api/poker/sessions/{id}/filter` | Facilitator | hidden types, selected labels | the session filter | 200; 403 non-facilitator; 409 free-mode or closed session |
| PUT | `/api/poker/sessions/{id}/items/{itemId}/estimate` | Facilitator | number | the item | 200; 400 negative or not a whole/half step; 403; 409 item not Decided, a Jira write pending, or session closed; 502 Jira write failed |
| POST | `/api/poker/sessions/{id}/items/{itemId}/reset` | Facilitator | — | the item | 200; 403; 409 item Not started, a Jira write pending, or session closed |
| POST | `/api/poker/sessions/{id}/close` | Facilitator | — | (F6, extended) | adds 409 while a Jira write is pending |

**Error responses:** a Jira write that fails or times out (on decide or on edit) leaves the item's previous state untouched, shows the facilitator a clear error, and can be retried; no vote is lost.

## Real-time Events

One room channel per session, extending F6's events. **Indicative, non-binding.**

| Direction | Event | Payload | Trigger |
|-----------|-------|---------|---------|
| Server → Client | ItemSelected (extended) | item id, round, whether an AI tile is present (never its value) | facilitator moves the room |
| Server → Client | VotesRevealed (extended) | people's votes + AI value (separate), if any | reveal |
| Server → Client | EstimateDecided (extended) | item id, final estimate, source | facilitator decides (sprint mode: after Jira confirms) |
| Server → Client | EstimateChanged | item id, new estimate | facilitator edits (sprint mode: after Jira confirms) |
| Server → Client | ItemReset | item id, new status, new round | facilitator resets |
| Server → Client | FilterChanged | hidden types, selected labels | facilitator changes the session filter |
| Server → Client | ContextRefreshed | items whose context changed | tickets pulled or refreshed from Jira |
| Client → Server | DecideEstimate (extended) | round, a cast value, the AI value, or a typed number | facilitator, after reveal |
| Client → Server | SubmitVote (extended) | round, value | any participant, current ticket only |
| Client → Server | NextUnestimated | — | facilitator |

Private looks (Flow 2) and list size or collapse (Flow 3) are per-person and produce no room events. A room action refused (wrong role, closed session, write pending, stale round) returns an error to the sender only.

## Business Rules

**Room and status**
1. The room has at most one current ticket, the same for everyone and kept across reloads and rejoins. Only the facilitator changes it. Votes apply only to the current ticket.
2. Item status follows § Status transitions. Selecting an item starts voting only when it is Not started; selecting a Revealed, Decided or Skipped item shows it without changing it.
3. **Not started** is the initial status of every item.
4. Every vote, reveal, decision and retry belongs to one voting round. An action for an earlier round of the same item has no effect.

**Ticket context**
5. **Context is light and links to Jira.** The room shows issue type, labels, Jira story points, the description preview, every subtask (key, title, developer) and, when enabled, the AI estimate. Nothing else from Jira is shown; every ticket and subtask key opens Jira in a new tab.
6. Ticket context is shown only to joined participants. Someone holding the link without joining, and any viewer of a Closed session's summary, sees key, title and final estimate only.
7. The description preview is cut at 3 displayed lines; "Read in Jira" shows only when text is cut. An empty description shows "No description".
8. Ticket context reflects Jira as of the last pull or refresh (F6 Rule 12), which updates every participant's view. A confirmed write from this room updates the item's Jira story points at once.
9. A ticket that left the sprint keeps its last known context, marked "No longer in sprint". A failed read from Jira keeps any earlier context and marks the ticket "Couldn't load details from Jira"; it never shows as "no subtasks" or "no AI estimate".
10. Ticket context, the filter, the AI estimate and "Open in Jira" apply to sprint mode only. Free-mode items have none of them.

**Private look and list**
11. Any participant may privately look at any listed ticket without changing the room's current ticket. The facilitator does so with a separate Look action; the facilitator's plain selection still moves the room.
12. A person in a private look sees no voting cards, is never moved back automatically, and still counts as a connected voter for auto-reveal; the banner tells them when voting is open on the room's ticket.
13. "Back to current" shows the current ticket in its actual phase.
14. The ticket list can be collapsed and resized; width stays within limits that keep the estimation area usable and is reduced to fit a smaller window; the collapsed list always shows an expand control. The choice is remembered per browser on the device, for any room, and is tied to no identity.

**Filter**
15. Issue types and labels are each multi-select. A ticket is listed when its type is not hidden **and** — when one or more labels are selected — it carries at least one selected label.
16. The filter is shared: everyone sees the same filtered list, and how many tickets it hides. Only the facilitator changes it, for this session only.
17. The filter hides tickets; it never deletes them. Hidden tickets keep their votes and decisions, and decided or skipped hidden tickets still count in the session summary (F6 Rule 17).
18. If the filter hides the room's current ticket, it stays current until the facilitator moves on.
19. Default type and label names match case-insensitively, ignoring surrounding spaces. A default type absent from a sprint has no effect; a type new to Jira is shown unless an Admin hides it. A default selected label that no ticket in the session carries is ignored, so it can never empty the list by itself.

**AI estimate**
20. The AI estimate takes part in a round only when the team switch was on and the ticket had a valid AI estimate **when that round started**. Its value is fixed for the round; a later refresh or switch change affects only later rounds.
21. Turning the AI switch on or off applies, for every Active session, to rounds that start after the change.
22. The AI Estimator appears as a voter tile marked "voted" from the start of the round. Its value is never delivered to anyone before that round's reveal — not on screen, not in a room read, not on reconnect, not in a private look. The same holds for people's votes (F6 Rule 7).
23. Auto-reveal (F6 Rule 7) counts connected people only; the AI tile is never waited for.
24. After reveal the AI value is shown apart from the people's tally; the tally ("3× voted 5") counts people only.

**Deciding, editing, resetting**
25. **Final estimate:** after reveal the facilitator may decide a value the people cast, the round's AI value, or a typed number ≥ 0 in whole or half steps. "?" is never a final estimate. A round may be decided by a typed number even with no numeric vote. *(Supersedes F6 Rule 6's "needs at least one numeric vote", F6 Rule 8, F6's acceptance criteria "the SM can only pick a final estimate from the values actually cast" and "a round with no numeric votes cast … cannot be decided — only skipped", and F6's "Arbitrary final value" out-of-scope line.)*
26. The AI value may be adopted exactly as it is, even off the card scale or not a half step. Only then is the estimate AI-sourced.
27. The facilitator may edit a Decided ticket's estimate (≥ 0, whole or half steps) while the session is Active. An edited estimate is Team-sourced.
28. Sprint mode: deciding and editing write to Jira first; the new value takes effect only once Jira confirms. A failure or timeout leaves the previous state, shows the facilitator an error, and allows retry (extends F6 Rule 10).
29. While a Jira write for an item is pending, that item accepts no decide, edit, skip or reset, and the session cannot be closed. A write's result never applies to a round that has since been reset. A skipped item is never written to Jira (F6 Rule 9 unchanged).
30. **Reset** clears a ticket's votes, final estimate and source in this session and starts a new round: the current ticket returns to Voting at once; any other ticket becomes Not started. A Not started ticket cannot be reset. Reset never changes Jira. *(Supersedes F6's "Re-vote rounds" out-of-scope line.)*
31. Edit and reset are available on the current ticket and on a ticket in the facilitator's private look.
32. Reset, typed decisions, editing and Next unestimated work in both sprint and free mode.
33. None of the facilitator actions above are possible once a session is Closed (F6 Rule 14 unchanged).

**Next unestimated**
34. A ticket is eligible when it is visible under the filter, is not the current ticket, has no session estimate and is not Skipped, and either has no Jira story points or was reset in this session. 0 counts as points. *(Passing over Skipped tickets is a PO addition: the facilitator already chose to move past them.)*
35. Next moves to the first eligible ticket after the current one in list order, wrapping to the top; the current ticket marks the starting position even when the filter hides it. With no current ticket, the first eligible from the top. With none eligible, it is unavailable.
36. A ticket with Jira story points shows them and is passed over by Next unless it was reset in this session; the facilitator can always select it by hand to re-estimate or reset it.

**Summary and settings**
37. The session summary marks AI-sourced estimates (e.g. "5 · AI"); total story points include them.
38. **Team settings** (default hidden types, default selected labels, AI switch) are changed by Admins only and read by any signed-in user. Names are picked from the team's Jira data or typed; an empty name (including spaces only) or a duplicate (the same name ignoring letter case and surrounding spaces) is refused. Changed defaults apply to new sessions only.

**Existing rooms (from F6)**
39. When this feature ships: in an Active session, an item that is Voting with no votes and is not the room's ticket becomes Not started; the room's ticket is the item it was on; the session takes the team default filter; existing decisions are Team-sourced. Votes, decisions and Closed sessions are otherwise unchanged.

## Acceptance Criteria

**Context**
- [ ] In a sprint-mode room a joined participant sees, on the current ticket: issue type, labels, Jira story points (when present, "0" when zero), "Open in Jira" (new tab), the description cut to 3 displayed lines with "Read in Jira", and every subtask with key, title and developer ("Unassigned" when none).
- [ ] A short description shows in full with no "Read in Jira"; an empty one shows "No description".
- [ ] A subtask key opens that subtask in Jira in a new tab.
- [ ] Someone holding the link who has not joined, and a viewer of a Closed session's summary, see no description, subtasks, developer names, labels, type or AI value.
- [ ] When Jira cannot be read, the ticket shows "Couldn't load details from Jira" and keeps earlier context; it does not show "no subtasks".
- [ ] A ticket moved out of the sprint keeps its last context and shows "No longer in sprint".
- [ ] A refresh updates context on every participant's screen without a reload.
- [ ] A free-mode room shows none of the Jira context, filter or AI elements.

**Room and status**
- [ ] Every newly pulled or added item shows "Not started"; selecting it shows "Voting".
- [ ] Selecting a Decided, Revealed or Skipped ticket moves the room to it without changing its status, votes or estimate.
- [ ] After a reload or rejoin, a participant lands on the room's current ticket — not on another ticket left in Voting.
- [ ] A vote sent for an item that is not the current ticket, or for an earlier round, has no effect.

**Private look and list**
- [ ] A participant clicking another ticket sees its context and status; everyone else still sees the room's current ticket.
- [ ] The facilitator's Look button opens a private look without moving the room; the facilitator's plain click moves the room.
- [ ] During a private look no voting cards show; the banner names the room's ticket and says "Voting is open" when it is Voting and the person has not voted.
- [ ] When the facilitator moves the room during a private look, the banner updates and the person's view does not jump.
- [ ] "Back to current" on a Revealed or Decided current ticket shows its result, not voting cards.
- [ ] A person in a private look who has not voted still holds up auto-reveal; the facilitator can force-reveal.
- [ ] The list collapses (with a visible expand control), expands and resizes; the state survives a reload in the same browser; a saved wide list is reduced to fit a smaller window and never hides the voting cards.
- [ ] Each list row shows key, title (full on hover), status and final estimate once decided.

**Filter**
- [ ] A new session starts with the team default; with the seed defaults, Bug, Sub-task and Test tickets are not listed, and everyone sees how many are hidden.
- [ ] Unticking a type hides it; ticking two labels lists only tickets with at least one of them; a hidden type stays hidden even if its tickets carry a ticked label.
- [ ] A default selected label that no ticket carries does not empty the list.
- [ ] A facilitator's filter change updates every participant's list in real time; a non-facilitator's attempt is refused.
- [ ] Changing a session's filter does not change the team default seen by the next new session.
- [ ] Filtered-out tickets that were decided still appear in the session summary.
- [ ] Hiding the current ticket with the filter leaves it current.

**Next unestimated**
- [ ] Next moves to the next visible ticket that has no session estimate, is not Skipped, and has no Jira points (or was reset in this session), wrapping to the top; it never stays on the current ticket.
- [ ] With no current ticket, Next moves to the first eligible ticket from the top.
- [ ] When only the current ticket is unestimated, Next is disabled and reads "Nothing left to estimate".
- [ ] A ticket with Jira points of 0 or more is passed over by Next unless it was reset in this session, and can always be selected by hand.
- [ ] With the current ticket hidden by the filter, Next moves to the first eligible visible ticket after its position in the list.

**AI estimate**
- [ ] With the switch on, a round on a ticket with a valid AI value shows an "AI Estimator" tile marked voted before anyone votes; its value appears only after reveal.
- [ ] Before reveal, no room read, reconnect or private look delivers the AI value or any person's vote value.
- [ ] Auto-reveal fires when the last connected person votes, regardless of the AI tile.
- [ ] After reveal, the people's tally excludes the AI value, which is shown separately.
- [ ] A round shows no AI tile when, at the moment it started, the switch was off, the ticket had no AI value, or the AI field held text or a negative number; a round that started with a tile keeps it until it ends.
- [ ] A refresh that changes a ticket's AI value during a round does not change the value revealed or adoptable in that round.
- [ ] Turning the switch on mid-session adds the AI tile only to rounds that start afterwards.

**Deciding, editing, resetting**
- [ ] The facilitator can decide a cast value, the AI value, or a typed 4, 2.5 or 0; a typed −1 or 2.3 is refused.
- [ ] An all-"?" round can be decided by a typed number.
- [ ] Adopting the AI value 6.4 as-is writes 6.4 to Jira; the summary shows it marked AI and counts it in the total.
- [ ] After a confirmed sprint-mode decision, the ticket's Jira story points show the new value.
- [ ] Editing a Decided ticket's estimate in sprint mode updates Jira; if Jira fails, the old value stays and the facilitator sees an error with retry. An edited AI-sourced estimate loses its AI mark.
- [ ] While a Jira write is pending, decide, edit, skip, reset and close are refused for that item or session; a skipped ticket never receives a Jira write.
- [ ] Resetting the current ticket clears its votes and estimate and starts a fresh round; resetting another ticket makes it Not started; Jira's points are unchanged after a reset; a decision retry from before the reset has no effect.
- [ ] The facilitator can edit or reset a ticket from a private look without moving the room.
- [ ] Reset, typed decide, edit and Next unestimated work in a free-mode session.
- [ ] After a session is Closed, decide, edit, reset, filter and Next unestimated are all refused.

**Settings and existing rooms**
- [ ] An Admin can change the team defaults and AI switch; a non-Admin attempt → 403; any signed-in user can read them; an empty or duplicate name → 400.
- [ ] Default names match regardless of letter case.
- [ ] Changing team defaults does not change the filter of a session already running.
- [ ] After this feature ships, an existing Active session shows untouched tickets as "Not started", keeps its current ticket, votes and decisions, and uses the team default filter.

## Out of Scope

- **Full ticket detail in Poker** (comments, attachments, acceptance criteria, history, full description, assignee and parent epic of the ticket itself) — the guiding principle: Jira holds the detail and the room links to it. Revises F6 / the product draft's "Ticket detail panel" exclusion only to the light context above.
- **Linked issues** (blocks / relates to) — not chosen; kept light.
- **Jira components** — the filter and context use labels only (owner, 2026-10-05).
- **Progress counter on the list** ("6 of 24 estimated") and **grouping the list** (up next / done) — not chosen; the Not started label carries the progress signal.
- **Search box for tickets** — not chosen; Next unestimated plus the resizable list cover navigation.
- **Vote spread on finished tickets in the list** — not chosen; a private look shows a ticket's revealed votes.
- **Type icon and two-line titles in list rows** — not chosen; rows show key, title (full on hover), status and estimate.
- **Per-session or per-sprint AI switch** — the AI switch is a team setting only.
- **Multiple teams per install** — the suite is one team per install; settings are app-wide.
- **Estimation accuracy analytics** (AI vs team vs actuals) — the AI mark in the summary is the only record for now; analysis stays F6's v3+ item.

## Open Questions

Architecture/feasibility items for the architect — not product gaps.

- [ ] **AI-estimate Jira field** — which Jira field holds the AI estimate and how its id is configured (the team setting holds only the on/off switch).
- [ ] **Ticket context source** — how Poker obtains issue type, labels, description, subtasks (with developer), Jira story points and the AI value, including for tickets that left the sprint, respecting F6's live pass-through decision.
- [ ] **Where team settings live** — Poker's own store vs an existing settings home, given the one-team-per-install model.
