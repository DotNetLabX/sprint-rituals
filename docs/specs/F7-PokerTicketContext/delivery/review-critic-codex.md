# Codex critic review — F7-PokerTicketContext spec

**Round:** 1 · **Reader:** Codex (primary, read-only) · **Date:** 2026-10-05 · **Job:** task-muuwavnh-983b91 (completed)
**Verdict:** NO-GO (helper reported no parsed verdict; the verdict line is the first line of the output below)
**Persisted by:** PO via the Relay Contract, verbatim from the job output.

---

**NO-GO — Round 1.** Found **9 HIGH, 7 MEDIUM and 5 LOW** issues. No CRITICAL findings. No files were modified.

Reviewed the [F7 spec](/docs/specs/F7-PokerTicketContext/definition/spec.md), [F6 spec](/docs/specs/F6-PlanningPoker/definition/spec.md), [Poker architecture](/docs/architecture/poker/v1.md), [owner rulings](/docs/specs/F7-PokerTicketContext/definition/ideas.md:83), and [tooltips](/docs/specs/F7-PokerTicketContext/definition/help.tooltips.md).

1. **HIGH — Unsupported restriction on final estimates.**  
   **File + section:** F7 spec, Entities → EstimationItem; Flows 7–8; Business Rules 19–21; Acceptance Criteria. Owner ruling 10.  
   **Issue:** The ruling permits “ANY number ≥ 0 (half points allowed).” F7 restricts manual decisions and edits to multiples of 0.5 and expressly rejects 2.3. That restriction is absent from the ruling. F7 also omits the ruling’s explicit supersession of F6’s “Arbitrary final value” out-of-scope exclusion.  
   **Suggested fix:** Remove the restriction or record an owner ruling authorizing it. Align validation, acceptance criteria and tooltips, and explicitly retire the conflicting F6 exclusion.

2. **HIGH — Facilitator excluded from an owner-approved private look.**  
   **File + section:** F7 spec, Flow 2; Business Rule 5; Out of Scope → Facilitator private look. Owner ruling 5; F6 Rule 22.  
   **Issue:** The owner permits private looks for “any participant.” F6 expressly makes the facilitator a participant, but F7 excludes them and declares the capability unnecessary without a corresponding ruling. Authority to move the room does not require every facilitator inspection to move it.  
   **Suggested fix:** Provide distinct private-view and shared-selection actions for the facilitator, or record an explicit owner-approved exception.

3. **HIGH — Selection can reopen terminal states without reset.**  
   **File + section:** F7 spec, Entities → Status; Business Rules 4, 14, 21, 23; Acceptance Criteria.  
   **Issue:** “Selecting an item makes it Voting” applies without qualification to Revealed, Decided and Skipped items. Selecting a Decided ticket could therefore remove its eligibility for editing, while retaining its final estimate and old votes. Selecting a Revealed ticket could reopen voting without the prescribed reset.  
   **Suggested fix:** Define the complete state/action matrix. Normally, only Not started transitions to Voting on selection; other states remain intact, and Reset alone starts a fresh round.

4. **HIGH — AI adoption bypasses the nonnegative requirement.**  
   **File + section:** F7 spec, Entities → AI estimate / Final estimate; Business Rules 19–20. Owner rulings 10–11.  
   **Issue:** AI estimates accept “any number,” and their exact value is an alternative to the normal final-estimate constraints. This permits adopting a negative AI value, contrary to the owner’s nonnegative final-estimate requirement. Invalid or unrepresentable AI values also lack defined handling.  
   **Suggested fix:** Require a valid, finite, nonnegative AI value. Limit the adoption exception to scale/decimal-step restrictions. Explicitly treat zero as present and define handling of invalid field values.

5. **HIGH — Pending Jira writes can race reset, edit or close.**  
   **File + section:** F7 spec, Flows 7–9; Business Rules 21–25; Error responses. F6 Rule 14.  
   **Issue:** The item retains its previous state while Jira responds, so another facilitator tab can reset it, submit another estimate or close the session. A late confirmation could restore a cleared decision, overwrite a newer edit, or change a supposedly immutable closed summary.  
   **Suggested fix:** Define permitted actions while a write is pending. Prevent conflicting writes and reset, and settle pending writes before closure or define cancellation behavior that prevents late mutation. Specify reconciliation after an uncertain timeout.

6. **HIGH — Reset lacks protection against actions from the previous round.**  
   **File + section:** F7 spec, Flow 9; Business Rule 23; Real-time Events. F6 Rule 21.  
   **Issue:** Reset introduces multiple rounds on the same item, but no rule distinguishes their actions. A delayed old vote, reveal or decision retry could affect the fresh round after its votes were cleared.  
   **Suggested fix:** Establish round identity as a product invariant: actions belonging to an earlier round cannot affect the current round. Add acceptance cases for delayed actions and reconnects following reset.

7. **HIGH — AI setting changes lack a shared effective point.**  
   **File + section:** F7 spec, Business Rules 15–17, 27; Flow 10.  
   **Issue:** Applying the switch on the “next room load or ticket refresh” does not establish whether one participant’s reload changes the whole session or only that participant’s view. Connected people could see different AI participation and decision options. Behavior when toggled during Voting or Revealed is unspecified.  
   **Suggested fix:** Define a session-wide application point and propagate the effective setting to everyone. Specify whether an existing round retains its AI participation or changes through an explicit round transition.

8. **HIGH — Refresh can change an AI proposal after it has voted or been revealed.**  
   **File + section:** F7 spec, Entities → Ticket context; Business Rules 2, 16, 20.  
   **Issue:** Refresh updates the AI field snapshot, while the tile is already considered voted. No rule determines what happens if the value changes during voting or after reveal. The facilitator could adopt a different value from the one participants saw, undermining the AI-source record.  
   **Suggested fix:** Freeze the AI proposal used for a round, or explicitly invalidate/restart the round when it changes. Adoption must reference the proposal actually revealed.

9. **HIGH — The room-read contract exposes unrevealed AI values.**  
   **File + section:** F7 spec, Entities → Ticket context; API Surface → GET session; Business Rules 15–16.  
   **Issue:** Each returned item carries ticket context, which includes the numeric AI estimate. No redaction exception reconciles that contract with hiding the value until reveal. Returning it before reveal would disclose it through room reads even if the tile conceals it.  
   **Suggested fix:** Explicitly prohibit disclosure of an item’s unrevealed AI number through any room read, private look or reconnect response. Availability can be disclosed before its value.

10. **MEDIUM — Retained tickets have undefined context freshness.**  
    **File + section:** F7 spec, Business Rule 2; Open Questions → Ticket context source. F6 Rule 19; architecture §3.2.1.  
    **Issue:** F6 retains items whose tickets leave the sprint. The ratified refresh retrieves current sprint contents, so those retained items will no longer be returned. F7 nevertheless says context reflects the last session refresh without defining whether retained items are fetched separately or retain older snapshots. Stale Jira points also affect navigation.  
    **Suggested fix:** Define retained-item refresh behavior. Either refresh their context separately or explicitly preserve and identify their last-known snapshot; preserve their votes and decisions.

11. **MEDIUM — Missing and failed context reads have no product behavior.**  
    **File + section:** F7 spec, Flow 1; Entities → Ticket context; Business Rules 1–2, 15; Open Questions → AI-estimate Jira field.  
    **Issue:** Empty descriptions, unassigned subtasks, unavailable fields, partial subtask results and an enabled but unusable AI field lack defined presentation and failure behavior. A failed fetch could be presented as “no subtasks” or “no AI estimate,” despite the requirement to show all available context.  
    **Suggested fix:** Distinguish empty values from retrieval failures. Define placeholders, incomplete-data indications and retry behavior; preserve previous context and estimation state on refresh failure. Define an actionable error for unavailable AI configuration.

12. **MEDIUM — “First 3 lines” is ambiguous and may show excessive detail.**  
    **File + section:** F7 spec, Flow 1; Business Rule 1; Acceptance Criteria; Out of Scope → Full ticket detail.  
    **Issue:** Three source-text lines and three rendered lines produce different results. A single long paragraph could display most of the description under the former interpretation, contradicting the light-preview intent. Rich description structures also lack a defined plain-text line interpretation.  
    **Suggested fix:** Define the line-counting and truncation rule, including paragraph/list normalization, long paragraphs and descriptions shorter than three lines.

13. **MEDIUM — Next unestimated has undefined starting and wrap boundaries.**  
    **File + section:** F7 spec, Flow 5; Business Rules 13, 24; Acceptance Criteria.  
    **Issue:** New rooms can have no current item, but navigation is defined only relative to one. It is also unclear whether wrapping may select the current item when it is the sole eligible candidate. “No Jira story points” does not explicitly distinguish null from the newly valid estimate of zero.  
    **Suggested fix:** Define behavior with no current item, empty lists and a single eligible item; specify whether the current item is excluded. Explicitly distinguish absent points from zero and cover both modes.

14. **MEDIUM — Private-view return behavior ignores the current phase.**  
    **File + section:** F7 spec, Flow 2; Acceptance Criteria → Back to current. Architecture §8.1; F6 Rule 7.  
    **Issue:** Returning unconditionally “restores the cards,” although the current ticket may have become Revealed, Decided or Skipped. The spec also does not establish whether the participant/vote panel represents the privately viewed item or the shared current item.  
    **Suggested fix:** Return to the current item’s actual phase, enabling voting only during Voting. Define which item each vote display represents and how private views update after reset or decision.

15. **MEDIUM — Persisted list widths lack recovery guardrails.**  
    **File + section:** F7 spec, Flow 3; Business Rule 7; Acceptance Criteria → Collapse/resize.  
    **Issue:** No bounds or viewport-change behavior prevent a saved wide list from obscuring the estimation area on a later smaller window. The requirement can pass while voting controls or the expand handle become inaccessible.  
    **Suggested fix:** Define supported viewport behavior, usable resizing bounds, clamping of saved widths and an always-accessible expand/recovery control.

16. **MEDIUM — Existing F6 rooms lack a transition policy.**  
    **File + section:** F7 spec, Entities; Business Rules 4, 10, 27; Acceptance Criteria.  
    **Issue:** F6 is implemented, but F7 specifies initial values mainly for new items and sessions. Existing untouched items already marked Voting, existing sessions without filters and previous decisions without an estimate source have no defined treatment. The owner’s “untouched tickets labelled Not started” requirement may remain unmet in existing rooms.  
    **Suggested fix:** Specify behavior for existing Active and Closed rooms: status mapping, initial filters and legacy estimate sources. Preserve votes, decisions and current selection, and maintain closed-room immutability.

17. **LOW — Navigation completion text and tooltip overstate eligibility.**  
    **File + section:** F7 spec, Flow 5 / Acceptance Criteria; tooltips → Next unestimated.  
    **Issue:** “All estimated” can appear when unfinished tickets are filtered out or every remaining ticket is Skipped. The tooltip promises the next ticket with no estimate but omits the filter and skipped-ticket exclusions.  
    **Suggested fix:** Use text such as “No eligible tickets” and explain that navigation considers visible, non-skipped tickets without a session decision or Jira points.

18. **LOW — Not started tooltip becomes false after reset.**  
    **File + section:** Tooltips → Ticket status: Not started; F7 spec, Business Rule 23.  
    **Issue:** “Nobody has started estimating this ticket in this session yet” is false when a previously Voting, Revealed, Decided or Skipped non-current ticket is reset to Not started.  
    **Suggested fix:** Describe current readiness, for example: “Ready to start a voting round; no current votes or decision.”

19. **LOW — Settings tooltips blur default and live-setting scopes.**  
    **File + section:** Tooltips → Team settings / AI estimate switch; F7 spec, Business Rule 27 and PokerTeamSettings.  
    **Issue:** The settings tooltip groups AI with defaults for new sessions, although AI also affects running sessions. The switch tooltip omits application on ticket refresh. “Admins only” also fails to distinguish Admin editing from signed-in read access.  
    **Suggested fix:** Separate new-session filter defaults from the live AI switch, state both application triggers, and distinguish viewing from editing permissions.

20. **LOW — The context whitelist contradicts the AI requirement.**  
    **File + section:** F7 spec, Business Rules 1, 15–18.  
    **Issue:** Rule 1 lists permitted Jira context, excludes AI, then says “Nothing else from Jira is shown.” Later rules require showing the Jira AI-estimate field.  
    **Suggested fix:** Add the AI estimate as an explicitly governed exception, or narrow the exclusion to additional ticket-detail fields.

21. **LOW — Per-person preference persistence is undefined for guests.**  
    **File + section:** F7 spec, Flow 3 / Business Rule 7; F6 Rules 15, 23.  
    **Issue:** Remembering preferences “per person per device” for the next visit lacks a scope for session-bound guests. Sharing a browser between guests and carrying preferences into another room have unspecified outcomes; persistent guest identity matching would conflict with F6.  
    **Suggested fix:** Define preference scopes for authenticated users and guests without introducing cross-session guest identity matching.

Codex session ID: 01a10ad8-94f8-7702-a738-0b38176c17e3
Resume in Codex: codex resume 01a10ad8-94f8-7702-a738-0b38176c17e3


---

# Round 2 — 2026-10-05 — Codex (primary, read-only, delta read)

**Job:** task-muux7678-m97s92 (completed) · **Verdict:** NO-GO (first line of output; helper parsed no verdict token)

**NO-GO — one HIGH and two MEDIUM findings.**

References: **S** = [F7 spec.md](/docs/specs/F7-PokerTicketContext/definition/spec.md); **T** = [help.tooltips.md](/docs/specs/F7-PokerTicketContext/definition/help.tooltips.md). Numbers below are file lines.

1. **R2-01 — HIGH — fold; round-1 miss: jira-write**  
   **File:** [S:244](/docs/specs/F7-PokerTicketContext/definition/spec.md:244), also S:58,137,320.  
   The pending-write guard excludes **Skip**. A pending decision leaves the item Revealed until Jira confirms; the transition table still permits Revealed → Skipped. A subsequent Jira confirmation can overwrite Skipped with Decided, and the Jira write already contradicts the unchanged [F6 Rule 9](/docs/specs/F6-PlanningPoker/definition/spec.md:175) guarantee that skipping produces no estimate or write-back. Round protection does not resolve this: Skip does not change the round.

2. **R2-02 — MEDIUM — fold**  
   **File:** [S:251](/docs/specs/F7-PokerTicketContext/definition/spec.md:251), also S:253,300,303; T:54.  
   The new reset exception makes a visible, non-current ticket eligible for Next despite existing Jira points. Rule 36, AC303 and the Next tooltip still say tickets with Jira points are passed over. For example, resetting a previously estimated ticket from a private look makes it eligible under Rule 34 but ineligible under those other clauses. The fold leaves two conflicting navigation contracts.

3. **R2-03 — MEDIUM — fold**  
   **File:** [S:310](/docs/specs/F7-PokerTicketContext/definition/spec.md:310), against S:39,130,233,234,311.  
   The AI-absence criterion uses the current switch and field validity without limiting them to round start. If a round starts with AI enabled and a valid value, then the switch turns off—or a refresh changes the field to text/negative—AC310 says no AI tile appears. Rules 20–21 instead retain that round’s AI participation and frozen value. This leaves the effective timing and refresh guarantees internally inconsistent.

The inherited F6 guarantee break is R2-01. R2-02 and R2-03 are contradictions within F7.

Closure assesses the original finding; collateral records additional defects exposed by the fold. Each clause in the **none** column has collateral value **none**.

| Round-1 id | Verdict | Touched clauses: collateral none | Touched clauses: collateral finding |
|---|---|---|---|
| R1-01 | closed | S:25,206,277,278 | — |
| R1-02 | closed | S:54–57,59,61,207,276 | S:58 → R2-01 |
| R1-03 | closed | S:40,240 | S:251 → R2-02 |
| R1-04 | closed | S:80–81,220,246,282; Out of Scope S:332–343, removed exclusion; T:36 | — |
| R1-05 | closed | S:37,233 | S:310 → R2-03 |
| R1-06 | closed | API S:178–180; S:244 second sentence | S:244 pending-action guard,320 → R2-01 |
| R1-07 | closed | S:30,209,244 second sentence,190,194,197,198,321 | — |
| R1-08 | partial | S:130,233,234,312; T:78 | Conflicting AC S:310 → R2-03 |
| R1-09 | partial | S:39,233,311 | Conflicting AC S:310 → R2-03 |
| R1-10 | closed | S:174,190,235,307 | — |
| R1-11 | closed | S:38,74–75,216,270 | — |
| R1-12 | closed | S:137–138,196,215,271,318 | S:251 → R2-02 |
| R1-13 | closed | S:73,216,269 | — |
| R1-14 | closed | S:34,69–71,214,266 | — |
| R1-15 | closed | S:117–120,252,301,302 | S:251,300,303 → R2-02 |
| R1-16 | closed | S:88–89,222,285 | — |
| R1-17 | closed | S:94–97,223,287 | — |
| R1-18 | closed | S:260,330 | — |
| R1-19 | closed | S:18,174,213,268 | — |
| R1-20 | closed | S:230,293 | — |
| R1-21 | closed | S:163–164,176,230,257,327,328 | — |
| R1-22 | closed | S:84–85,221,286; T:42 | — |
| R1-23 | partial | T:15,30,48,75,78 | T:54 → R2-02 |
| R1-24 | closed | S:212 | — |
| R1-25 | closed | S:98,223 | — |
| R1-26 | closed | S:18 | — |
| R1-27 | closed | S:292,297,312,317,328 | — |
| R1-28 | closed | S:240,334,339,340 | — |
| R1-29 | closed | S:201,324 | — |

R1-08 and R1-09 remain partial because AC310 conflicts with their new round guarantees. R1-23 remains partial because Next’s tooltip still misstates eligibility. R1-06’s originally named reset/edit/close races are closed; the additional Skip race is R2-01.

Codex session ID: 01a10aef-9ba5-79d1-a6ca-28d51c3b898e
Resume in Codex: codex resume 01a10aef-9ba5-79d1-a6ca-28d51c3b898e
