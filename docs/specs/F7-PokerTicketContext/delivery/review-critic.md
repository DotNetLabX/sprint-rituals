# Critic review record — F7-PokerTicketContext spec

## Round 1 — 2026-10-05 — spec — primary (Codex)

Header note: reader pair codex + sonnet (picked, see `reader-pair.md`). Primary = Codex, read-only job task-muuwavnh-983b91, completed; its verdict (NO-GO, 9 HIGH / 7 MEDIUM / 5 LOW) is persisted verbatim in `review-critic-codex.md` — that file is this round's primary reader section. The helper parsed no verdict token; the verdict is the output's first line. The second reader's message was recovered from its transcript (salvage script) — the agent went idle without delivering inline.

## Second reader — round 1 (sonnet)

# F7 Poker Ticket Context — Critic Review (round 1, second reader)

## Mode: Spec Review
## Verdict: REVISE

1 HIGH, 8 MEDIUM, 7 LOW or unranked. No CRITICAL, so no adversarial escalation. The owner's 14 interview rulings are carried almost entirely intact. The one HIGH is a real gap: the room's current ticket is used in eight places but never defined as shared, persisted state. Codex was the primary reader. I read only the first line of `delivery/reader-pair.md` (the pair record) and none of Codex's findings.

## Pre-commitment Predictions
| Predicted | Found |
|---|---|
| F6 supersession leaves loose ends | Mostly closed (Rules 19, 23, Out of Scope). Residue is bookkeeping only (F7-L7). |
| Edge cases in "Next unestimated" | Confirmed (F7-M3, F7-L2). |
| Facilitator vs Scrum Master terminology drift | Confirmed (F7-L3). |
| Admin and settings underspecified | Confirmed (F7-M7). |
| AI "hidden until reveal" vs API payload | Confirmed (F7-M1). |
| Not predicted: no defined "current ticket" | The biggest finding (F7-H1). |
| Not predicted: guest and link-holder visibility of Jira text | F7-M2. |

## Coverage ledger
Round: 1 · Depth: product · Bar: product completeness · Ruler: seed only (`docs/critic-calibration/ruler.md` does not exist) · Read: full

| Requirement / area | Surfaces opened | Status |
|---|---|---|
| Interview decisions 1–14 carried into spec | ideas.md § Interview decisions; spec Flows, Rules, ACs | PARTIAL: #6 gains a "Skipped" exclusion and #10 narrows to 0.5 steps (F7-M6) |
| F6 Rules 6, 8 and the three out-of-scope lines superseded | F6 spec Rules 6, 8, 9, 12, 19, Out of Scope, ACs; F7 Rules 19, 23, Out of Scope | COVERED. F6 ACs and the "arbitrary final value" line are not named (F7-L7). |
| Ticket context (Flow 1, Rules 1–3) | spec; v1.md § 8.1 | PARTIAL (F7-L5, F7-M5) |
| Private look (Flow 2, Rules 5–6) | spec; hub, store and view code | PARTIAL (F7-H1, F7-L1) |
| Filter (Flow 4, Rules 8–12, 28) | spec | PARTIAL (F7-M4) |
| Next unestimated (Flow 5, Rules 13–14) | spec | PARTIAL (F7-M3, F7-L2, F7-M6) |
| AI estimate (Flow 6, Rules 15–18, 20) | spec; `SessionContracts.cs`; `GetSessionEndpoint.cs` | PARTIAL (F7-M1, F7-M8) |
| Decide, edit, reset (Flows 7–9, Rules 19–26) | spec; `EstimationItem` and `PlanningSession` behaviours; `PlanningHub.cs` | COVERED apart from findings above |
| Team settings (Flow 10, Rules 27–28) | spec; Admin-role grep | PARTIAL (F7-M7) |
| API surface and real-time events | spec; hub, `SyncSessionItems`, `IPokerFokusService`, `PokerTicket` | PARTIAL (F7-M1, F7-M2) |
| Tooltips | help.tooltips.md vs spec | COVERED, with drift (F7-L4) |
| Acceptance criteria testability | spec § Acceptance Criteria | PARTIAL (F7-L6) |
| Out-of-scope disposition | ideas.md A–L vs spec § Out of Scope | PARTIAL (F7-L7) |
| docs/product/index.md | read | Opened. There is no Poker subtree under docs/product/, so the product baseline is `docs/architecture/poker/v1.md`. |
| docs/architecture/poker/v1.md | read in full | COVERED. It also records the "type a custom value?" question (§14 Q2), which F7 resolves. |
| docs/product/suite-strategy.md | keyword grep only | PARTIAL. No contradiction with the AI-voter direction. |
| ADR-021 and ADR-022 (guest and open-link stance) | grep hit only; matched lines were truncated and I did not read them | NOT OPENED (not briefed; F6 Rules 15, 20 and 23 used as proxy) |
| Sibling specs | F6 and F8 ideas (first 60 lines); `adhoc-SprintContextPanel` keyword grep | COVERED. No Poker coupling found. |

## Findings

### [HIGH] F7-H1 — "The room's current ticket" is never defined as shared, persisted state
**Anchor:** seed-0007 · above seed-0022 · below seed-0002
**Shape:** concept used by eight rules but absent from the data model
**Consequence:** A person who reloads after the host moved on lands on, and votes for, an earlier unfinished item, and nothing tells them.
**Evidence:** quoted, scenario, path
**Surfaces:** spec § Entities (PlanningSession) and Rule 4; `PlanningHub.cs`; `planningSession.ts`
**Untouchable:** silent-wrong-output
- **Source:** Entities § PlanningSession (extended) lists only `Session filter`. Rule 4 (line 171): `Moving the room away from a Voting item leaves it Voting.` Rules 5, 12 and 13 and Flows 2, 5 and 9 all lean on "the room's current ticket".
- **Issue:** Today the current item is not stored anywhere.
  - The hub derives the active item as the first Voting one: `PlanningHub.cs:339` `session.Items.FirstOrDefault(i => i.Status == ItemStatus.Voting)`.
  - The client restores it the same way: `planningSession.ts:45-47` "Restore active item — first Voting item".
  - `SubmitVote` (`PlanningHub.cs:99-126`) accepts any item id that is still Voting.
- **Scenario:** The facilitator selects A, which becomes Voting. They press Next, so B is current and A stays Voting under Rule 4. A participant reloads. The store restores A, shows voting cards, and the vote lands on A.
- **Impact:** Rule 5 ("votes always apply to the room's current ticket") cannot be enforced. Reload and late-join land on the wrong ticket. F6's "re-enters with current state" criterion silently regresses. The existing "first Voting" shortcut only worked because items could not stay Voting after the room moved on.
- **Fix (spec level):**
  - State that the room has exactly one current ticket (or none).
  - Everyone sees the same one, and it survives reload and rejoin.
  - Only the facilitator moves it, and votes target only it.
  - Add an AC: "after a reload, a participant lands on the room's current ticket, not on an earlier Voting one".
  - Also state what selecting a Decided, Skipped or Revealed ticket does to its status. Rule 4's "Selecting an item makes it Voting" can be read as applying to every status.
- **Route:** plan must resolve the mechanism: the hub's active-item derivation, auto-reveal on disconnect, and server-side vote targeting.

### [MEDIUM] F7-M1 — AI value is "hidden until reveal" but rides in the anonymous per-item payload
**Anchor:** seed-0022 · above seed-0310 · below seed-0004
**Band:** MEDIUM–HIGH; driver: whether an early read of the AI number counts as breaking the anchoring guarantee
**Shape:** two statements contradict each other
**Consequence:** The AI estimate rides in the same payload anyone can fetch before reveal, so the hidden-until-reveal promise holds only on screen.
**Evidence:** quoted, path
**Surfaces:** spec § API Surface (GET session row), Rule 16, Flow 6, AC "its value appears only after reveal"; `SessionContracts.cs:31-33,78-80`; `GetSessionEndpoint.cs`
- **Source:** The API row reads `(extended) each item carries status incl. Not started, ticket context, final estimate + source`, and Entities lists AI estimate under "Ticket context". Rule 16: `its value is hidden until reveal`.
- **Issue:** `GET /api/poker/sessions/{id}` is `[AllowAnonymous]`. The existing item response already carries every vote's value regardless of status (`VoteResponse(ParticipantId, Value)` on every item). So the F6 promise "others see only 'voted'" is already broken at GET, which is a baseline defect outside F7 (see Gap Analysis).
- **Fix:** Add a product rule that, before reveal, the AI value is not delivered to anyone who cannot yet see it. It reaches clients only with the reveal, or the item response omits it until the item is Revealed. Add an AC that tests the payload, not only the screen.

### [MEDIUM] F7-M2 — Audience for Jira text and developer names is never decided
**Anchor:** seed-0021 · above seed-0364 · below seed-0004
**Band:** MEDIUM–HIGH; driver: whether the owner accepts guests and link-holders reading ticket text
**Shape:** silent scope extension of who can see what
**Consequence:** Ticket text and assignee names reach anyone holding the room link, including guests and non-participants, with no decision recorded on that.
**Evidence:** quoted, path
**Surfaces:** spec § API Surface (GET row "Anyone with link"), Rule 1, Flow 1; F6 Rules 18 and 20; `GetSessionEndpoint.cs`
**Untouchable:** security
- **Source:** GET session is `Anyone with link`. Rule 1 and Flow 1 add description text, labels and subtask developer names to what the room shows.
- **Issue:** F6 exposed key and title only. F7 adds a description preview and named assignees on an anonymous, open-link surface. The guiding principle ("detail lives in Jira") implies Jira's access control protects the detail, but the preview and the names sit outside it. Neither the spec nor the ideas record discusses this.
- **Fix:** Have the owner decide who sees Jira context:
  - signed-in participants only,
  - any joined participant including guests,
  - or anyone with the link.
  
  Record the answer, and state whether a Closed session's summary view shows any context. My recommendation is joined participants only, with no context for non-participants. Confidence: medium. It is a reasonable lean, but it is the owner's call.

### [MEDIUM] F7-M3 — "Next unestimated" boundary cases are unresolved
**Anchor:** seed-0022 · above seed-0310 · below seed-0007
**Shape:** boundary the rule leaves open
**Consequence:** On the last open item the next-item button either re-selects the current one or claims all done while it is still unestimated.
**Evidence:** quoted, scenario
**Surfaces:** spec § Flow 5, Rule 13, AC "Next unestimated…"
- **Source:** Rule 13: `the first ticket after the current one in list order, wrapping to the top, that is visible… neither Decided nor Skipped… and has no Jira story points. With none left, it is unavailable.`
- **Scenarios:**
  - **(a)** The current ticket is Voting and every other ticket is Decided. The wrap reaches the current ticket, which meets all the criteria. Reading one: Next "moves" to itself. Reading two: it is excluded, so the button reads "All estimated" while the current ticket is not estimated.
  - **(b)** No current ticket yet (session start). "After the current one" has no anchor.
  - **(c)** The current ticket is hidden by the filter (Rule 12). It is unclear whether "list order" means the full order or the visible one.
- **Fix:** State that Next never returns the current ticket. State what the button does when only the current ticket qualifies, and what "first after" means when there is no current ticket. Settle (c) too.

### [MEDIUM] F7-M4 — A selected default label that no ticket carries empties the list, and the control cannot show it
**Anchor:** seed-0022 · above seed-0364 · below seed-0008
**Shape:** asymmetric edge-case rule
**Consequence:** A default label that no ticket in the sprint carries hides every ticket, and the filter control cannot show it to be cleared.
**Evidence:** quoted, scenario
**Surfaces:** spec § Rules 8 and 28, Flow 4 step 2, AC "clearing all labels…"
- **Source:** Rule 8: `when one or more labels are selected — it carries at least one selected label`. Flow 4 step 2 offers `labels present` in the session's tickets. Rule 28 covers a default hidden type that does not occur ("simply has no effect") but says nothing for a selected label.
- **Scenario:** An Admin sets default selected label `team-a`. A sprint has no ticket with `team-a`. The session starts with an empty list for everyone. The multi-select offers only labels present in the tickets, so `team-a` is not shown and cannot be unticked.
- **Impact:** Not data loss, and a clear-all path is implied by the AC. But an empty room with no visible cause looks like breakage.
- **Fix:** State that a selected label absent from the session's tickets has no effect, or that it stays visible and removable. Add an AC.

### [MEDIUM] F7-M5 — What "Jira story points" shows after the room itself writes to Jira
**Anchor:** seed-0022 · above seed-0364 · below seed-0004
**Shape:** two plausible builds, nothing settles which
**Consequence:** After the room writes a value to the tracker, the shown tracker points and the skip-if-pointed rule may still use the old snapshot.
**Evidence:** quoted, scenario, path
**Surfaces:** spec § Entities (Ticket context), Rules 2, 13, 14, 22, Flow 9 step 4; help.tooltips.md § Jira story points; `SyncSessionItemsEndpoint.cs` (refresh is `AddSprintBoundItem`, idempotent, no update)
- **Source:** Rule 2: `Ticket context reflects Jira as of the last pull or refresh`. The tooltip says: `Deciding a new estimate here replaces them.`
- **Scenario:** A ticket is decided at 5 and Jira now holds 5. The strip still shows the old snapshot (empty or 3), because only a refresh updates it. Then the facilitator resets that ticket. If the snapshot updated on write, Next skips it, since it now "carries Jira points". If not, Next offers it.
- **Impact:** Every Decided ticket in sprint mode shows stale "Jira points" until someone refreshes. Other participants never learn of a refresh, because the spec has no refresh event.
- **Fix:** State whether a confirmed in-room write updates the item's "Jira story points" immediately. Say whether a refresh reaches other participants' views.

### [MEDIUM] F7-M6 — Two places depart from the owner's rulings without saying so
**Anchor:** seed-0021 · above seed-0364 · below seed-0004
**Shape:** unflagged change to an approved set
**Consequence:** The spec adds a skip rule and a stricter number grid than the owner ruled, so people build against choices nobody confirmed.
**Evidence:** quoted
**Surfaces:** ideas.md § Interview decisions #6 and #10; spec § Rules 13 and 19, Entities (Final estimate), AC "typed −1 or 2.3 is rejected"
- **Skipped exclusion:** Ruling #6 reads `skips tickets decided in the session AND tickets already carrying Jira points`. The spec adds `neither Decided nor Skipped`, so a ticket the facilitator skipped is never offered by Next. That is a sensible rule, but the owner did not state it and the spec does not flag it as a PO addition.
- **Number grid:** Ruling #10 reads `ANY number ≥ 0 (half points allowed)`. The spec fixes steps of 0.5 and rejects 2.3. "Half points allowed" can mean halves are the only fractions, or merely that halves are included.
- **Fix:** Either mark both as PO proposals for the owner to confirm, or get the owner's word. My recommendation is to confirm both as written. Confidence: medium. Both are defensible, but each narrows what the owner said.

### [MEDIUM] F7-M7 — Flow 10 (Admin sets defaults) never says where the choices come from or how names match
**Anchor:** seed-0022 · above seed-0310 · below seed-0007
**Shape:** a screen whose inputs are unspecified
**Consequence:** The admin screen never says where type and label choices come from or how names match, so each build invents its own input rules.
**Evidence:** quoted
**Surfaces:** spec § Flow 10, Entities (PokerTeamSettings), Rule 28, API row PUT settings ("400 invalid")
- **Source:** Flow 10 step 2: `The Admin sets the default hidden issue types, the default selected labels`. The session filter offers only values present in a session's tickets, but settings live outside any session.
- **Issue:** The spec gives no list to choose from, so free text, a pick-list from synced Jira data, or something else are all possible. Matching is `by issue-type name` with no case or whitespace rule, and "400 invalid" has no definition.
- **Fix:** State the input style (typed names, or choices from Jira data), whether matching is case-insensitive, and what makes an entry invalid (empty, duplicate, too long).

### [MEDIUM] F7-M8 — An adopted AI value bypasses the number rules, and its validity is unspecified
**Anchor:** seed-0022 · above seed-0310 · below seed-0008
**Shape:** exemption without a floor
**Consequence:** An AI value that is negative, non-numeric or enormous can be adopted as-is and written to the tracker, bypassing the stated number rules.
**Evidence:** quoted, scenario
**Surfaces:** spec § Entities (AI estimate: `Any number`), Rules 19 and 20, AC "Adopting the AI value as-is (e.g. 6.4)"
**Untouchable:** silent-wrong-output
- **Source:** Rule 20: `exactly as it is, even off the card scale or not a step of 0.5`.
- **Scenario:** The AI field holds -2, text such as "5-8", or 1000000. Adoption is "as-is", so -2 would be written to Jira as story points, contradicting Rule 19's ≥ 0. Nothing says a non-numeric field counts as "no AI estimate".
- **Fix:** State that an AI value that is not a number ≥ 0 is treated as no AI estimate. Decide whether any upper bound applies to typed numbers.

### [UNRANKED] F7-L1 — A person on a private look still holds up auto-reveal
**Anchor:** unranked — no anchor fits; persona grades
**Reader's read:** LOW–MEDIUM; driver: whether the owner wants someone browsing elsewhere to block auto-reveal
**Shape:** unacknowledged interaction between two rules
**Consequence:** A person browsing another item is still a connected voter with no cards, so automatic reveal waits on them with no visible reason.
**Evidence:** quoted
**Surfaces:** spec § Rules 5, 6, 17, "Private looks … produce no room events"
- **Source and issue:** The spec says private looks produce no room events and a person on a private look is never pulled back. Rule 17 counts connected people only. By composition, a browsing participant blocks auto-reveal, and the facilitator sees them as "waiting" with no explanation. The feature exists to get people browsing, so this will be common.
- **Fix:** State the intended behaviour: they still count (the facilitator force-reveals), or the banner tells them voting is open. Either is acceptable once written down.

### [LOW] F7-L2 — Zero points versus no points
**Anchor:** seed-0364 · above seed-0041 · below seed-0022
**Band:** LOW–MEDIUM; driver: whether the strip hides a real "0"
**Shape:** boundary value not named
**Consequence:** A ticket pointing at zero is treated as having no points by one build and as estimated by another, so it is re-offered or shown blank.
**Evidence:** quoted, path
**Surfaces:** spec § Flow 1 ("Jira story points if any"), Rule 13, Rule 19 (0 allowed), AC "typed … 0"; `SprintTicketsResponse.cs` (`decimal? StoryPoints`)
- **Issue:** This spec introduces 0 as a valid estimate, and the contract already separates null from 0. "If any" and "no Jira story points" do not say which side 0 falls on.
- **Fix:** State that 0 counts as having points. It is shown as "0", and Next passes it over.

### [LOW] F7-L3 — "Facilitator" versus "Scrum Master" in the same room
**Anchor:** seed-0041 · below seed-0021
**Shape:** terminology drift
**Consequence:** Two names for the same role appear in the same room, so readers and users cannot tell whether the host and the Scrum Master differ.
**Evidence:** path
**Surfaces:** spec (41 uses of "facilitator"); F6 Rules 1, 2, 22; help.tooltips.md ("Facilitator only" ×4); `SmControls.vue`; `PlanningRoomView.vue:73`
- **Issue:** F7 never says facilitator is the F6 Scrum Master, that is, the session creator. The shipped UI says "Scrum Master" and "SM Controls", while the new tooltips say "Facilitator only".
- **Fix:** Define the term once, or adopt F6's. Make the tooltips match what the UI says.

### [LOW] F7-L4 — Two tooltips contradict the rules they explain
**Anchor:** seed-0041 · below seed-0021
**Shape:** help text drift
**Consequence:** Two help texts say the opposite of the rules they explain, so users learn the wrong scope for the AI switch and the filter.
**Evidence:** quoted
**Surfaces:** help.tooltips.md § Team settings, § Filter; spec Rules 27, 28 and 8
- **Team settings:** `Defaults for every new session: hidden issue types, selected labels, and the AI estimate.` Rule 27 says the AI switch applies to running sessions too, and the AI switch tooltip says so correctly.
- **Filter:** `Choose which issue types and labels the team sees.` The model is hidden types (selection hides) plus selected labels (selection narrows). The spec never says what ticking a type does in the multi-select. The tooltip implies "ticked means shown", which is the opposite of the hidden-set model Rule 28 needs.
- **Fix:** Correct both texts. State the tick semantics for each multi-select.

### [LOW] F7-L5 — "First 3 lines" of the description, and an empty description
**Anchor:** seed-0310 · below seed-0021
**Shape:** undefined display rule
**Consequence:** The "first 3 lines" preview and an empty description are not defined, so previews differ by screen width or show a dead link.
**Evidence:** quoted
**Surfaces:** spec § Entities (Description preview), Flow 1 step 2, Rule 1; ideas.md decision #2
- **Issue:** "Lines" could mean source lines or lines on screen. A long single paragraph is one source line but several on screen. The spec does not say whether "Read in Jira" shows when the description is empty or already short. The "as plain text" conversion of formatted Jira content (lists, links, images) is not described.
- **Fix:** Pick screen-lines with truncation, and show "Read in Jira" only when there is something to read.

### [LOW] F7-L6 — Several rules have no acceptance criterion
**Anchor:** seed-0310 · below seed-0021
**Shape:** untested rules
**Consequence:** Several stated rules have no pass/fail check, so builds can drift from them unnoticed.
**Evidence:** path
**Surfaces:** spec § Acceptance Criteria vs Rules 8, 12, 26, 27, 28
- **Rule 12:** the filter hides the current ticket and it stays current.
- **Rule 26:** the summary total includes AI-sourced estimates.
- **Rule 27:** the AI switch applies to a running session on its next load.
- **Rule 28:** type names match by name, and a new type is shown.
- **Rule 8:** hidden-type AND label combined.
- **Fix:** Add the ACs, and the F7-H1 reload AC.

### [LOW] F7-L7 — Out-of-scope and supersession bookkeeping
**Anchor:** seed-0041 · below seed-0021
**Shape:** disposition not recorded
**Consequence:** Parked ideas and older superseded checks are not listed in the exclusions, so readers cannot tell dropped from forgotten.
**Evidence:** path
**Surfaces:** ideas.md § H, J, B; spec § Out of Scope, Rule 19; F6 spec ACs (lines 199, 210), Flow 4 step 5
- **Parked ideas not listed:**
  - idea H (vote spread on finished tickets);
  - idea J (wrapped titles and a type icon per list row);
  - the assignee and parent-epic parts of idea B.
  
  The spec also never says what a list row shows, which matters because the owner's complaint was an unreadable list.
- **Supersession gap:** Rule 19 names F6 Rules 6 and 8. It does not name the F6 acceptance criteria they invalidate ("can only pick … values actually cast", "no numeric votes … cannot be decided") or the "Arbitrary final value" out-of-scope line. The ideas record names that line, but the spec does not.
- **Fix:** List the parked ideas under Out of Scope with their forward references. Name the superseded F6 criteria in Rule 19.

## Plan must resolve (informational, LOW, no extra round)
- **Current-ticket mechanism (F7-H1).** The hub's active-item derivation, the auto-reveal paths and `SubmitVote` item targeting all need an explicit current-item notion.
- **Pre-reveal leak (baseline defect).** `GetSessionEndpoint` plus `SessionContracts.cs:31-33,78-80` ship all vote values before reveal to anonymous callers. Private look and the AI payload cannot be built by extending that response as it stands.
- **Refresh must update existing items.** `AddSprintBoundItem` returns the existing item and changes nothing. The sync endpoint returns the full session only to the facilitator, with no broadcast.
- **Jira fetch fields.** `RestApiJiraClient.cs:11` fetches `summary,issuetype,customfield_10016,…,assignee,…`, with no labels, description or subtasks. `PokerTicket` carries only Key, Summary and StoryPoints.
- **Rendering and links.** Converting Jira's rich description to plain text, and where the base URL for "Open in Jira" comes from.
- **Transport split.** Reset, edit and filter are REST in the spec, while F6 runs the estimation loop over the hub.
- **Existing data.** Existing Active sessions' items are Voting by default and have no context until a refresh. `decimal.TryParse` in the hub and `FinalEstimate` stored as a string need a culture and format decision.
- **Edit and reset entry points.** The spec does not say where these live for non-current tickets. If the facilitator must select the ticket first, editing an old estimate moves the room.

## Gap Analysis
- **Backward impact.** Not started, typed and AI estimates, and Reset change the F6 estimation state machine (Voting → Revealed → Decided | Skipped). Reset adds Revealed → Voting, Decided → Voting and Skipped → Voting. The F6 domain guards (`CastVote` requires Voting; `Decide` requires a cast value) and the summary totals (string parsed as decimal) all need rework. The spec's rules are sound, but nothing says it is a state-machine change.
- **Participants see a shorter list with no signal.** Only the facilitator can open the filter. Participants get no "N hidden by filter" cue and no search (out of scope), so they cannot tell why a ticket is missing. Small, but a real product-completeness hole.
- **Empty room.** With no current ticket, Flow 2's banner ("which ticket the room is on") has no content, and the empty state is unspecified.
- **Subtask cap.** Subtasks are unbounded ("all, one line each"). That is the owner's explicit ruling, so it is not a finding.
- **Carried correctly.** Interview rulings #1–5, 7–9 and 11–14 are all carried. F6 Rule 2 (facilitator moves the room) is preserved. The "?" rule and the Closed-session 409s are preserved.

## Open Questions
- Do guests and link-holders see Jira description and developer names? (F7-M2.) Recommendation: joined participants only. Confidence: medium.
- Does "Next unestimated" skip Skipped tickets, and does "any number" mean half-steps only? (F7-M6.) Recommendation: keep both. Confidence: medium.
- Should a person on a private look block auto-reveal? (F7-L1.) Recommendation: yes, with the banner flagging that voting is open. Confidence: low, a toss-up that wants the owner's call.

Reviewed: docs/specs/F7-PokerTicketContext/definition/spec.md

### Merge — round 1

Coverage: complete — every baseline surface was opened by at least one reader (ADR-021/022 sit outside the baseline; neither reader opened them).

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R1-01 | sonnet | HIGH | seed-0007 | Room's current ticket never defined as shared state | concept absent from model | reload lands and votes on an earlier Voting item | quoted, scenario, path | silent-wrong-output | current-ticket | Entities, Rules 4/5/13, Flows 2/5/9 | S F7-H1 |
| R1-02 | codex, sonnet | HIGH | unranked — no anchor fits; Codex severity HIGH | Selecting a Revealed/Decided/Skipped item makes it Voting | missing state/action matrix | reopens terminal items without reset | quoted | — | state-machine | Entities Status, Rules 4/14/21/23 | C3, S F7-H1 (part), SR-52 |
| R1-03 | codex, sonnet | MEDIUM | seed-0021 | Final-number grid and Next's Skipped exclusion depart from rulings unflagged; F6 "arbitrary final value" line not retired | unflagged change to approved set | build against choices the owner did not confirm | quoted | — | ruling-drift | Entities, Rules 13/19, AC, Out of Scope | C1 (HIGH), S F7-M6 — contested HIGH / MEDIUM: settled MEDIUM, the owner's picked option text said whole or half numbers (owner reconfirmed 2026-10-05) |
| R1-04 | codex | HIGH | unranked — no anchor fits; Codex severity HIGH | Facilitator excluded from private look without a ruling | scope narrowed vs ruling | facilitator cannot read ahead without moving the room | quoted | — | ruling-drift | Flow 2, Rule 5, Out of Scope | C2 |
| R1-05 | codex, sonnet | HIGH | seed-0022 | AI value adopted as-is bypasses the floor of 0 and validity | exemption without a floor | negative or junk value written to Jira | quoted, scenario | silent-wrong-output | ai-value | Entities AI estimate, Rules 19/20 | C4, S F7-M8 (MEDIUM) — contested HIGH / MEDIUM: settled HIGH, wrong data written to Jira silently |
| R1-06 | codex | HIGH | unranked — no anchor fits; Codex severity HIGH | Pending Jira write can race reset, edit or close | concurrency gap | late confirmation restores a cleared decision or mutates a closed summary | scenario | silent-wrong-output | jira-write | Flows 7–9, Rules 21–25 | C5 |
| R1-07 | codex | HIGH | unranked — no anchor fits; Codex severity HIGH | Reset has no round identity; old-round actions can hit the new round | missing invariant | stale vote or decide lands on a fresh round | scenario | silent-wrong-output | reset | Flow 9, Rule 23 | C6 |
| R1-08 | codex | HIGH | unranked — no anchor fits; Codex severity HIGH | AI switch has no session-wide effective point | undefined timing | participants see different AI participation | quoted | — | ai-switch | Rules 15–17, 27 | C7, SR-64 |
| R1-09 | codex | HIGH | unranked — no anchor fits; Codex severity HIGH | Refresh can change the AI value after it voted or was revealed | snapshot drift | facilitator adopts a value people never saw | scenario | silent-wrong-output | ai-value | Entities, Rules 2/16/20 | C8 |
| R1-10 | codex, sonnet | HIGH | seed-0022 | Unrevealed AI value rides in the room-read payload | two statements contradict | hidden-until-reveal holds only on screen | quoted, path | — | ai-value | API GET session, Rule 16 | C9, S F7-M1 (MEDIUM) — contested HIGH / MEDIUM: settled HIGH, the anchoring guarantee is why the owner chose after-reveal |
| R1-11 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Retained tickets (left the sprint) have undefined context freshness | undefined refresh | stale context and points drive Next | quoted | — | context | Rule 2, F6 Rule 19 | C10 |
| R1-12 | sonnet | MEDIUM | seed-0022 | Jira points shown after the room's own write are stale; refresh not broadcast | two plausible builds | stale points; Next after reset differs per build | quoted, scenario, path | — | context | Entities, Rules 2/13/14/22 | S F7-M5, SR-58 |
| R1-13 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Empty vs failed context reads undefined | missing failure behaviour | a failed fetch reads as no subtasks or no AI | quoted | — | context | Flow 1, Rules 1/2/15 | C11 |
| R1-14 | codex, sonnet | MEDIUM | seed-0310 | "First 3 lines" undefined; empty description | undefined display rule | preview varies; dead Read in Jira link | quoted | — | context | Flow 1, Rule 1 | C12, S F7-L5 (LOW), SR-17 — contested MEDIUM / LOW: settled MEDIUM, a source-line reading defeats the light-preview principle |
| R1-15 | codex, sonnet | MEDIUM | seed-0022 | Next unestimated: no current item, current as sole candidate, zero vs no points | boundary left open | button re-selects current or says done when not | quoted, scenario | — | next | Flow 5, Rule 13 | C13, S F7-M3, S F7-L2, SR-30 |
| R1-16 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Back to current ignores the current ticket's phase | unconditional promise | cards offered on a Revealed or Decided ticket | quoted | — | private-look | Flow 2, AC | C14 |
| R1-17 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Saved list width has no bounds or recovery | missing guardrail | list can cover the voting area on a smaller window | scenario | — | list | Flow 3, Rule 7 | C15 |
| R1-18 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Existing F6 rooms have no transition policy | missing migration rule | old rooms keep Voting labels, no filter, no source | quoted | — | migration | Entities, Rules 4/10/27 | C16 |
| R1-19 | sonnet | MEDIUM | seed-0021 | Who sees Jira text and developer names never decided | silent scope extension | ticket text reaches anyone with the link | quoted, path | security | audience | API GET session, Rule 1 | S F7-M2 |
| R1-20 | sonnet | MEDIUM | seed-0022 | A default label no ticket carries empties the list | asymmetric edge rule | empty room, no visible cause | quoted, scenario | — | filter | Rules 8/28, Flow 4 | S F7-M4 |
| R1-21 | sonnet | MEDIUM | seed-0022 | Admin settings inputs, name matching and invalid input undefined | unspecified inputs | each build invents input rules | quoted | — | settings | Flow 10, Rule 28, API PUT settings | S F7-M7, SR-48 |
| R1-22 | sonnet | LOW | unranked — no anchor fits; persona grades LOW (a one-line rule closes it, and the facilitator can force-reveal) | Person on a private look still holds up auto-reveal | unacknowledged interaction | reveal waits with no visible reason | quoted | — | private-look | Rules 5/6/17 | S F7-L1 |
| R1-23 | codex, sonnet | LOW | seed-0041 | Tooltips contradict rules (Next, Not started, settings, filter tick meaning) | help-text drift | users learn the wrong scope | quoted | — | tooltips | help.tooltips.md | C17, C18, C19, S F7-L4 |
| R1-24 | codex | LOW | unranked — no anchor fits; Codex severity LOW | Rule 1's "nothing else from Jira" excludes the AI field | whitelist contradiction | rule conflict | quoted | — | context | Rules 1, 15–18 | C20 |
| R1-25 | codex | LOW | unranked — no anchor fits; Codex severity LOW | List preference "per person" undefined for guests | scope undefined | implies guest identity across sessions | quoted | — | list | Flow 3, Rule 7 | C21 |
| R1-26 | sonnet | LOW | seed-0041 | Facilitator vs Scrum Master never reconciled | terminology drift | two names for one role | path | — | terms | spec, tooltips | S F7-L3 |
| R1-27 | sonnet | LOW | seed-0310 | Rules 8/12/26/27/28 have no acceptance criterion | untested rules | drift unnoticed | path | — | ac | Acceptance Criteria | S F7-L6 |
| R1-28 | sonnet | LOW | seed-0041 | Parked ideas and superseded F6 criteria not recorded | disposition not recorded | dropped vs forgotten unclear | path | — | bookkeeping | Out of Scope, Rule 19 | S F7-L7 |
| R1-29 | rule-list skeptic | LOW | unranked — no anchor fits; persona grades LOW | Closed-session rejection stated as 409 for real-time actions | channel mismatch | untestable AC wording | quoted | — | ac | AC closed session | SR-62 |

### Fold — round 1 (po, claude-opus-5-5, 2026-10-05)

Owner rulings taken during the fold (2026-10-05): facilitator gets a separate Look action (R1-04); ticket context only for joined participants (R1-19); typed estimates whole or half steps only (R1-03).

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R1-01 | fixed-real | HIGH | — | Entities PlanningSession "Current ticket"; Rule 1; AC Room and status (reload lands on current; vote for non-current has no effect) |
| R1-02 | fixed-real | HIGH | — | Entities § Status transitions (new); Rule 2; AC "Selecting a Decided, Revealed or Skipped ticket…" |
| R1-03 | fixed-real | MEDIUM | — | Entities Final estimate; Rule 25 (names superseded F6 ACs and "Arbitrary final value"); Rule 34 (Skipped exclusion flagged as PO addition); owner reconfirmed half steps |
| R1-04 | fixed-real | HIGH | — | Flow 2 step 1; Rule 11; Rule 31; AC private look (facilitator Look); Out of Scope line removed; tooltip "Look (facilitator)" |
| R1-05 | fixed-real | HIGH | — | Entities AI estimate validity; Rule 20; AC "AI field holding text or a negative number" |
| R1-06 | fixed-real | HIGH | — | Rule 29; API estimate/reset/close 409 write pending; AC pending write refused |
| R1-07 | fixed-real | HIGH | — | Entities "Voting round"; Rule 4; Rule 29 second sentence; Real-time events carry round; AC reset retry no effect |
| R1-08 | fixed-real | HIGH | — | Rules 20, 21; Flow 6 step 5; AC switch mid-session; tooltip AI estimate switch |
| R1-09 | fixed-real | HIGH | — | Entities "Round AI value"; Rule 20; AC refresh during round |
| R1-10 | fixed-real | HIGH | — | Rule 22 (also people's votes — F6 Rule 7 payload level); API GET row; ItemSelected payload; AC no pre-reveal delivery |
| R1-11 | fixed-real | MEDIUM | — | Entities Context state; Rule 9; Flow 1 step 4; AC "No longer in sprint" |
| R1-12 | fixed-real | MEDIUM | — | Rule 8; Flow 7 step 2; ContextRefreshed event; Rule 34 "or was reset in this session"; AC refresh updates everyone, decided points shown |
| R1-13 | fixed-real | MEDIUM | — | Rule 9; Flow 1 step 4; AC failed read |
| R1-14 | fixed-real | MEDIUM | — | Entities Description preview; Rule 7; Flow 1 step 2; AC short/empty description |
| R1-15 | fixed-real | MEDIUM | — | Flow 5; Rules 34, 35; AC Next unestimated (no current, only current left, zero points) |
| R1-16 | fixed-real | MEDIUM | — | Flow 2 step 6; Rule 13; AC Back to current on Revealed/Decided |
| R1-17 | fixed-real | MEDIUM | — | Flow 3 steps 1–2; Rule 14; AC list reduced to fit |
| R1-18 | fixed-real | MEDIUM | — | Rule 39 (new, existing rooms); AC existing Active session |
| R1-19 | fixed-real | MEDIUM | — | Terms (joined participant); Rule 6; API GET row; AC non-joined / closed summary sees no context |
| R1-20 | fixed-real | MEDIUM | — | Rule 19 last sentence; AC default label absent |
| R1-21 | fixed-real | MEDIUM | — | Flow 10 step 2; Rules 19, 38; API PUT settings 400; AC empty/duplicate name, case-insensitive |
| R1-22 | fixed-real | LOW | — | Rule 12; Flow 2 step 3; AC private look holds up auto-reveal; tooltip "Voting is open" |
| R1-23 | fixed-real | LOW | — | help.tooltips.md: Next unestimated, Not started, Team settings, AI switch, Filter, Jira story points |
| R1-24 | fixed-real | LOW | — | Rule 5 includes the AI estimate |
| R1-25 | fixed-real | LOW | — | Flow 3 step 3; Rule 14 (per browser, any room, no identity) |
| R1-26 | fixed-real | LOW | — | Purpose § Terms (facilitator = F6 Scrum Master; screen may keep "SM") |
| R1-27 | fixed-real | LOW | — | AC Filter (type AND label, hidden current stays), AC AI (summary total), AC settings (case-insensitive), AC switch mid-session |
| R1-28 | fixed-real | LOW | — | Out of Scope (vote spread, list-row icon/two lines, ticket assignee/epic); Rule 25 names superseded F6 ACs |
| R1-29 | fixed-real | LOW | — | AC closed session now "refused"; Real-time note: refusals return an error to the sender |

#### Handed to the plan
- How the current ticket is stored and how vote targeting, auto-reveal on disconnect and the reload path use it (R1-01; second reader's note: today the active item is derived as "first Voting").
- Pre-reveal payload: today's room read returns vote values before reveal (baseline F6 defect the second reader found); Rule 22 requires it closed for votes and the AI value.
- Refresh must update existing items' context (today it only adds new items) and reach all participants.
- Jira fetch fields (labels, description, subtasks with assignee, AI field) and the Jira base URL for links; rich-text-to-plain-text conversion.
- Transport split (requests vs real-time messages) for filter, edit, reset; round identity on messages.
- Number format/culture for stored estimates (half steps, AI decimals) and the summary total.
- Migration of existing Active sessions per Rule 39.

Reconciled: Entities (PlanningSession, EstimationItem, Status transitions) ↔ Rules 1–4, 20, 25–30, 34–36, 39 ↔ Flows 2, 5, 6, 7, 8, 9 ↔ API rows (GET session, estimate, reset, close, settings) ↔ Real-time events (ItemSelected, SubmitVote, DecideEstimate, ItemReset, ContextRefreshed) ↔ every AC group ↔ Out of Scope ↔ help.tooltips.md (Look, Voting is open, Next unestimated, Filter, Not started, Team settings, AI switch, Type a number, Jira story points). Rule numbering changed throughout (1–39); spec-rules.md (mined against the earlier draft) gets the delta re-check.

## Round 2 — 2026-10-05 — spec — primary (Codex)

Header note: delta read (Read: delta — slug number 7 is not a sample, round-1 coverage complete), floor MEDIUM. Single reader by design (the second reader runs in round 1 only). Codex's verbatim message, including its per-finding fold-verification table with the collateral column, is persisted in `review-critic-codex.md` § Round 2.

### Merge — round 2

Coverage: complete (delta read of the fold-touched sections).

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R2-01 | codex | HIGH | unranked — no anchor fits; Codex severity HIGH | Pending-write lock omits Skip; a late confirmation turns Skipped into Decided and writes Jira | concurrency gap | skipped ticket written to Jira, breaking F6 Rule 9 | quoted, scenario | silent-wrong-output | fold | Rule 29, transitions table, AC pending write | C R2-1, rule-list SR-78 |
| R2-02 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Reset exception to Next eligibility contradicted by Rule 36, its AC and the tooltip | contradiction | two navigation contracts | quoted | — | fold | Rules 34/36, AC Next, tooltip Next unestimated | C R2-2 |
| R2-03 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | AI-absence AC ignores round-start timing | contradiction | AC conflicts with the frozen-per-round rule | quoted | — | fold | AC AI estimate, Rules 20/21 | C R2-3 |
| R2-04 | rule-list skeptic | LOW | unranked — no anchor fits; persona grades LOW | Next's start position when the filter hides the current ticket | boundary left open | Next has no anchor | quoted | — | fold | Rules 18/35 | SR-90 |
| R2-05 | rule-list skeptic | LOW | unranked — no anchor fits; persona grades LOW | Settings "duplicate" and "empty" undefined vs case-insensitive matching | undefined input rule | inconsistent validation | quoted | — | round-1 miss: Rule 38 | Rule 38, API PUT settings | SR-48 |

### Fold — round 2 (po, claude-opus-5-5, 2026-10-05)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R2-01 | fixed-real | HIGH | — | Rule 29 (skip added to the lock; skipped item never written to Jira); AC pending write |
| R2-02 | fixed-real | MEDIUM | — | Rule 36 ("unless it was reset in this session"); AC Next (reset exception); tooltip Next unestimated |
| R2-03 | fixed-real | MEDIUM | — | AC AI estimate (judged at round start; a round keeps its tile) |
| R2-04 | fixed-real | LOW | — | Rule 35 (hidden current ticket marks the position); new AC; Flow 5 step 3 points to Rule 34 |
| R2-05 | fixed-real | LOW | — | Rule 38 (spaces-only = empty; duplicate = same name ignoring case and spaces) |

Reconciled: Rule 29 ↔ transitions table (Skip from Voting/Revealed) ↔ F6 Rule 9 ↔ AC pending write; Rules 34–36 ↔ Flow 5 ↔ AC Next ↔ tooltip Next unestimated; Rules 20–21 ↔ Flow 6 step 5 ↔ AC AI estimate ↔ tooltip AI estimate switch; Rule 38 ↔ Rule 19 ↔ API PUT settings ↔ AC settings.

Schedule: round 3 runs only on a CRITICAL in round 2's merge — none, so the review ends here (no round 4 ever).
