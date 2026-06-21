# Spec Review — Critic (Mode 1)

**Spec:** `docs/specs/F6-PlanningPoker/definition/spec.md`
**Reviewer:** critic (spawned, Mode 1) · **Date:** 2026-06-21
**Verdict:** REVISE
**Recovery note:** the critic stranded its findings (idle without returning a message); recovered verbatim from its transcript `subagents/agent-acritic-poker-35f75f2a5d985fa2.jsonl` line 15. Findings preserved below; dispositions tracked by the PO.

The spec faithfully captures the owner's resolved decisions and the three Open Questions are correctly scoped as architect-feasibility items (not Ready-blockers). The findings are internal-consistency / product-discipline defects plus one latent identity gap.

## HIGH
1. **Closed-session read path unspecified.** Flow 7 implies a closed session's summary is readable via URL, but the API table gives no non-participant read path. → Add rule + acceptance: closed summary readable by anyone with the link; only mutations rejected. *Disposition: FIXED (Rule 18, AC, API auth set to "Anyone with link" for reads).*
2. **Item↔ticket invariant + refresh/removed-ticket behavior missing.** "No re-vote / single round" depends on a 1:1 item↔ticket invariant the spec never states; removed-then-re-added ticket (votes preserved or lost?) undefined. → State invariant + refresh behavior. *Disposition: FIXED (Rule 19; default = idempotent by ticket key, refresh never deletes existing/Decided items; flagged to owner for veto).*
3. **Identity model for Google-not-yet-Person + "no waiting room" silence.** Spec said "Reflekt guest-auth pattern (reused)", dragging in ADR-021 (waiting room) Poker is NOT adopting; and the Google-authed-but-not-yet-a-Person case is unspecified vs ADR-016/021/022. → State open-link join (no waiting room); route Person-minting to architect. *Disposition: FIXED (Dependencies reworded; Rule 20 open-link join; new Open Question for architect on Person minting).*

## MEDIUM
4. **No real-time write-back-failure event.** Decide runs over the real-time channel; Rule 10 requires SM sees error + retry, but no failure event exists. → Add `WriteBackFailed` server→client event. *Disposition: FIXED.*
5. **Auto-reveal racy under disconnect/rejoin.** "All present participants have voted" undefined at the disconnect boundary. → Precise rule. *Disposition: FIXED (Rule 7 refined; AC added; default flagged to owner).*
6. **Vote uniqueness not stated.** Nothing says one-vote-per-participant-per-item; append-only reading would corrupt the tally. → Rule + acceptance. *Disposition: FIXED (Rule 21).*
7. **API/event tables leak plan-level contract** (status codes, payload fields). → Label both tables indicative/non-binding consistently. *Disposition: FIXED (labels added).*
8. **Rule↔acceptance coverage gaps** — Rules 6, 12, 16 untested. → Add acceptance. *Disposition: FIXED.*

## LOW
9. **Help file vocab drift** ("open" vs Active); no tooltip for write-back-failure state. → Align + add tooltip. *Disposition: FIXED.*
10. **SM-is-also-a-participant unstated.** → One line: SM votes like anyone; authority derived from creator, not a stored role. *Disposition: FIXED (Rule 22).*
11. **Mode-conditional 400 blurred.** sprint-bound-missing-sprint vs free-missing-title. → Split validation + acceptance. *Disposition: FIXED.*
