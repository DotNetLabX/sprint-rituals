# Epic View Enhancements — Help Page

## Search Field

Use the search field above the table to quickly find an epic by its name or Jira key (e.g., "ABC-105" or "Configuration"). The search is case-insensitive and matches any part of the name or key. When you search, the summary cards at the top recalculate to reflect only the matching epics. Search works on both the Active and Completed tabs and combines with the sub-team filter — both narrow the results independently.

## Sort Indicator

Click any column header to sort the table by that column. The first click sorts ascending; clicking the same header again switches to descending. An arrow icon shows which column is active and in which direction. By default, the table sorts by "Last Work" (most recent activity first), so epics where work is happening appear at the top. Your sort preference is remembered across sessions.

## Column Visibility Toggle

Click the gear icon near the search field to open the column selector. Toggle columns on or off to focus on the metrics that matter to you. Your selection is saved in your browser and restored on your next visit. The epic name column is always visible and cannot be hidden. If you have Xray disabled, QA columns (Coverage %, Pass Rate %, Bugs Found) are automatically hidden regardless of your preference.

## Started

Shows when real work began on this epic — specifically, when the first ticket transitioned past the cycle time start boundary defined in your workflow settings. This is not when the epic was created in Jira, but when someone actually started working on it. If no ticket has passed the start boundary yet, the column shows "—". Dates within the last 30 days show in relative format (e.g., "3d ago"); older dates show as absolute (e.g., "Jan 15, 2025"). Use this to identify epics that have been planned but not yet started, or to see how long an epic has been in progress.

## Last Work

Shows the most recent date when any ticket in this epic had a meaningful status transition (past the cycle time start boundary). Use this to spot stale epics — if "Last Work" was weeks ago but the epic is far from complete, it may need attention or re-prioritization. Combined with the sort feature (default: last work descending), recently active epics appear at the top and stale ones sink to the bottom.

## Sticky Epic Column

When the table has more columns than fit on your screen, you can scroll horizontally to see columns on the right. The epic name column stays pinned on the left so you always know which epic each row belongs to. A subtle shadow marks the boundary between the pinned column and the scrolling area. To reduce the need for horizontal scrolling, use the column visibility toggle to hide columns you don't need.
