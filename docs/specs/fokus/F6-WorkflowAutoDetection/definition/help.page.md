# Workflow Auto-Detection — Guide Page Help

Sibling of `docs/features/WorkflowAutoDetection/spec.md`. Each section provides a guide page paragraph explaining interpretation and recommended actions.

---

## Workflow Stages

Workflow stages define the pipeline that cycle time and carry-over metrics measure against. Each stage is a Jira status name (e.g., "To Do," "In Progress," "Code Review," "Done") arranged in the order your team typically progresses work. You can configure stages manually or let the system detect them from your sync history. The order matters — it determines how forward progress and bottlenecks are calculated across other features.

---

## Auto-Detection

When you visit Settings with no workflow stages configured and at least one sprint synced, the system automatically analyzes all status transitions from your synced tickets and proposes an ordered list of stages. The detection builds a frequency-weighted graph of every status-to-status transition and identifies the dominant forward path your team follows. This saves you from needing to know your Jira workflow by heart or having Jira admin access — the detected pipeline reflects your team's actual behavior, not Jira's configured workflow definition.

---

## Confidence Summary

When a detection result is displayed, a summary line shows three numbers: the total status transitions analyzed, the number of distinct tickets those transitions came from, and how many sprints they span. More data means a more reliable proposal. A result based on 500 transitions across 40 tickets and 5 sprints is highly trustworthy. A result from 20 transitions across 3 tickets in 1 sprint still works but deserves closer review before saving. The system never blocks detection due to low data — the confidence summary lets you judge reliability yourself.

---

## Sidelined Statuses ("Other Statuses")

Not every Jira status belongs in the main workflow pipeline. Statuses like "Blocked," "On Hold," or rarely used custom statuses appear in the "Other statuses" area below the main list. These were found in your transition data but excluded because they had predominantly bidirectional transitions (tickets move in and out equally) or appeared too infrequently to place confidently in the forward path. Review this list after detection — if a status belongs in your pipeline (e.g., a legitimate phase your team uses), drag or add it into the main ordered list. If it's truly a side state, leave it out.

---

## Re-Detect Button

Click "Re-detect" to re-analyze your transition history and generate a fresh workflow proposal. This is useful after syncing additional sprints, which gives the detection more data to work with. The button replaces whatever is currently shown in the stage editor with the new proposal — but it does not overwrite your saved stages. If you don't like the new result, simply navigate away and your previously saved configuration remains intact. The button is disabled when no transition data exists (no sprints have been synced yet).

---

## Bug Exclusion

The detection algorithm deliberately excludes transitions from tickets with issue type "Bug." Bug tickets tend to follow non-linear, chaotic status paths — bouncing between states, skipping phases, or following entirely different routes than feature work. Including them would distort the detected pipeline with noise. Stories, tasks, epics, and sub-tasks are all included because they typically follow the team's standard workflow. If your team uses custom issue types in Jira, those are also included unless they are typed as "Bug."

---

## Forward-Flow Scoring

The detection algorithm scores each status by comparing how many of its transitions move toward a done status versus how many move away. Statuses with high forward-flow scores form the main pipeline, placed in order from earliest to latest. This approach handles a common reality of agile workflows: tickets don't always move strictly forward. Code review tickets go back to "In Progress" for rework, tested tickets return to development. The algorithm uses the dominant direction — the one with more transitions — to determine ordering, treating the reverse direction as a rework loop rather than a pipeline step.

---

## Done Status Anchoring

The detection uses your configured done statuses (from the Done Statuses setting) as anchor points — known terminal statuses that mark the end of the workflow. These always appear at the tail of the detected stage list, giving downstream features like cycle time a complete pipeline from the first status to a terminal status. If your done statuses are not configured before running detection, the algorithm has no anchor points and the ordering may be less accurate. Configure done statuses first for best results.

---

## Manual Entry

You don't have to use auto-detection. If you know your team's workflow stages, type them directly into the ordered list editor. Manual entry works at any time — before syncing sprints, after syncing, or alongside a detection result. You can also start from a detected proposal and manually adjust it: reorder stages, remove ones that don't apply, or add stages the detection missed. The save behavior is the same regardless of how the stages were entered.

---

## Unsaved Changes

Detection results and any edits you make to the stage list are held in memory until you click Save. If you navigate away from the Settings page without saving, everything reverts to your last saved configuration. This applies to both auto-detected proposals and manual re-detect results. There is no draft or auto-save — the workflow stages setting only changes when you explicitly save.
