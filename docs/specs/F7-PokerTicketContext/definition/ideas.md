# Ideas — F7-PokerTicketContext

Source: owner feedback on the running Poker room (2026-10-05), screenshots of facilitator + participant views.

Owner principle (2026-10-05): Poker shows just enough to orient; real detail lives in Jira — encourage Jira navigation, never try to show everything.

## Topic: Ticket details (PO)

### A — Ticket strip: type icon, labels as chips, "Open in Jira", current points
- Problem: nobody knows what kind of ticket it is or how to reach Jira fast
- Status: parked
- Parked concerns: none
- Reason: topic closed in the ideas stage (type, labels and Jira link carried into the spec as the owner's own requirements from the source)

### B — Right detail panel: description, acceptance criteria folded, assignee, parent epic
- Problem: people estimate from the title alone
- Status: parked
- Parked concerns: competes for the right column with the facilitator controls; heavier than the owner's "encourage Jira" principle
- Reason: topic closed in the ideas stage

### C — Description preview under the title (~5 lines, "show more")
- Problem: a quick read without leaving the cards
- Status: parked
- Parked concerns: none
- Reason: topic closed in the ideas stage (PO recommends a 3-line preview + "Read in Jira" as a requirement — pending owner ruling)

### D — Subtask list on the ticket: key, title, developer; each key links to Jira (reshaped by owner from "hidden-scope counts")
- Problem: a short title hides a ticket that is really several pieces of work
- Status: picked
- Parked concerns: none
- Critique: new Jira data to fetch; long lists could swamp the view; subtasks may already be sprint items (hide Sub-task type by default) — owner kept it showing ALL subtasks, one line each (they are few)

## Topic: Moving around the tickets (PO)

### E — Private peek: anyone views another ticket without moving the shared current one; "Back to current"
- Problem: participants cannot look around
- Status: picked
- Parked concerns: none
- Critique: the owner's original complaint was otherwise uncovered; room stays in sync because only the facilitator moves it — owner revived and kept it (2026-10-05)

### F — Progress line on the list: "6 of 24 estimated · 31 SP"
- Problem: no sense of how much is left
- Status: parked
- Parked concerns: none
- Reason: topic closed in the ideas stage

### G — Group the list into Up next / Done / Skipped
- Problem: the list reads as one endless queue
- Status: dropped
- Reason: owner — the list must stay a single pick list; the team uses it to choose the next ticket and see which are finished

### H — Vote spread on finished tickets in the list
- Problem: lets people compare against tickets already estimated
- Status: parked
- Parked concerns: none
- Reason: topic closed in the ideas stage

### I — Ticket list collapsible and wider / resizable (owner)
- Problem: on laptops and split screens the list shows only the key — useless for choosing a ticket
- Status: picked
- Parked concerns: none
- Critique: cheap, screen-only; collapsed list needs another way to move on (L) and per-person memory — owner kept it collapsible + resizable, remembered per device, list stays on the left

### J — Two-line wrapped titles + type icon per row
- Problem: a cut-off title cannot be recognised
- Status: parked
- Parked concerns: none
- Reason: topic closed in the ideas stage

### K — Header search box to find a ticket by key or words when the list is collapsed
- Problem: the facilitator still needs to reach any ticket with the list closed
- Status: parked
- Parked concerns: none
- Reason: topic closed in the ideas stage

### L — "Next unestimated" button for the facilitator
- Problem: picking the next ticket means scanning the whole list
- Status: picked
- Parked concerns: none
- Critique: covers picking the next ticket while the list is collapsed — owner revived and kept it (2026-10-05)

## Interview decisions (PO, owner rulings 2026-10-05)

Carried into the spec; recorded here so nothing waits on the conversation.

| # | Decision |
|---|---|
| 1 | Ticket strip (sprint mode): issue type, Jira labels (labels only, not components), "Open in Jira" link, existing Jira story points |
| 2 | User story: first 3 lines of the description, then "Read in Jira" |
| 3 | Subtasks: ALL listed, one line each — key (links to Jira), title, developer |
| 4 | Ticket list stays on the left; collapsible + resizable; width/collapsed remembered per person per device; untouched tickets labelled "Not started" (not "Voting") |
| 5 | Private look: any participant opens another ticket without moving the room; "Back to current"; only the facilitator moves the room |
| 6 | "Next unestimated" button for the facilitator; skips tickets decided in the session AND tickets already carrying Jira points |
| 7 | Filter: issue type + label, both multi-select; facilitator-set, shared by everyone; session changes stay in the session |
| 8 | Team setting (Admin only): default filter (seed: hide Bug, Sub-task, Test) and AI-Estimate on/off |
| 9 | AI estimate: appears as a voter tile only on tickets that have a value; shows "voted" from the start; value hidden until reveal; never holds up auto-reveal; shown separately from the people's tally |
| 10 | Final estimate: facilitator may set ANY number ≥ 0 in whole or half steps (see #17), at decision time or later by editing a decided ticket; votes and the AI value are suggestions — supersedes F6 Rule 8 and F6's "arbitrary final value" out-of-scope line |
| 11 | AI value may be adopted as final even off the card scale; the summary marks it ("5 · AI") |
| 12 | Reset (reshaped ticket): clears Poker votes + decision only; Jira keeps its old points until the new decision is written — supersedes F6's "no re-vote rounds" out-of-scope line |
| 13 | Reset, typed number and Next unestimated work in both modes; Jira-only parts (type, labels, subtasks, AI, filter) are sprint mode only |
| 14 | Hover-help tooltips: produced (help.tooltips.md) |
| 15 | Facilitator also gets a private look via a separate Look action; plain click still moves the room (spec review round 1) |
| 16 | Ticket context (description, subtasks, developer names, type, labels, AI) only for joined participants (signed in or guest); link-only viewers and closed summaries see key, title, estimate |
| 17 | Typed estimates limited to whole and half steps (owner reconfirmed); the AI value is still adopted exactly as it is |
