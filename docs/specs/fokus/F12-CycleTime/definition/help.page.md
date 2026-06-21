# Cycle Time — Help Guide

## Median Cycle Time Card
The median (P50) is the midpoint of your team's cycle time distribution. Half of all completed tickets finished faster than this value, and half took longer. Use the median rather than the average because it is resistant to outliers — a single ticket that took 30 days won't distort the number. Watch the delta indicator to track whether your team is trending faster or slower compared to the prior sprint. A rising median across multiple sprints signals a systemic slowdown worth investigating.

## P85 Cycle Time Card
The P85 value tells you that 85% of tickets complete within this many days. This is your predictability number — when a stakeholder asks "how long will this take?", the P85 gives you a high-confidence answer. If your P85 is 7 days, you can say "there's an 85% chance this will be done within a week." The remaining 15% are outliers worth individual investigation but not worth building commitments around. Compare the P85 across sprints to measure whether your team's predictability is improving.

## Throughput Card
Throughput counts the number of tickets that completed the full workflow cycle during the sprint. Unlike story points, throughput is objective and not subject to estimation variance. Use it alongside cycle time to get a complete picture: a team can have low cycle time (fast per ticket) but low throughput (few tickets completed), or vice versa. Throughput trending downward while cycle time stays flat may indicate work-in-progress overload — too many things started, not enough finished.

## Outliers Card
An outlier is any ticket whose cycle time exceeds twice the sprint median. These tickets disproportionately affect team predictability and often reveal process issues: blocked dependencies, unclear requirements, scope creep within a single ticket, or review bottlenecks. Review the outlier table for common patterns. If the same developer or issue type repeatedly appears in outliers, that's a targeted coaching or process improvement opportunity. A healthy sprint typically has 0-2 outliers.

## Scatter Plot
The scatter plot shows every completed ticket as an individual dot, positioned by when it completed (X-axis) and how long it took (Y-axis). This visualization reveals patterns that summary statistics hide: clusters of fast tickets early in the sprint followed by slow ones at the end may indicate rushed starts or end-of-sprint complexity. Dots colored by issue type help identify whether bugs, stories, or tasks have different cycle time profiles. Look for dots well above the P85 line — those are your outliers, and hovering them shows the per-stage breakdown to identify where time was spent.

## P50 Reference Line
The P50 (median) reference line divides the scatter plot in half — roughly equal numbers of tickets above and below. It represents your team's "typical" performance. When the P50 line is close to the P85 line, your cycle times are tightly clustered (predictable). When there's a large gap between P50 and P85, your distribution has a long tail of slow tickets. Narrowing this gap — by reducing outliers — improves predictability more than reducing the median itself.

## P85 Reference Line
The P85 reference line is your primary benchmark. It tells you the cycle time within which 85% of work completes. Tickets below this line are performing within expectations. Tickets above it deserve investigation. As you improve your process, this line should trend downward across sprints. The P85 is more stable than the P90 (which is sensitive to individual extreme outliers) and more informative than the P50 (which ignores the tail entirely).

## Percentile Toggle
Available in single-sprint view only. Switch between P50, P75, P85, and P90 to examine different slices of your cycle time distribution. P50 (median) shows typical performance. P75 shows where three-quarters of work lands. P85 (default and recommended) balances signal and stability for commitment-setting. P90 captures almost everything except true anomalies. Use P75 or P85 for stakeholder conversations; use P90 when setting internal SLA targets where you want very high confidence. In multi-sprint view, the trend line always shows P85 — the toggle does not apply there.

## Rework Badge
The rework badge appears on tickets that moved backward through the workflow — for example, from code review back to development. The number indicates how many times the ticket re-entered a stage it had already passed through. Rework increases cycle time and indicates that work wasn't fully ready when it moved forward. Common causes include insufficient requirements, missing test coverage, or premature code review. Track which stages generate the most rework to identify where your "definition of done" for stage transitions needs strengthening.

## Stage Funnel
The stage funnel is a horizontal stacked bar chart showing how total cycle time breaks down across workflow stages. The widest segment is your bottleneck — the stage where tickets spend the most time. This is the highest-leverage improvement target: reducing time in the bottleneck stage directly reduces overall cycle time. Compare the funnel across sprints to see if bottlenecks shift. A persistent bottleneck in code review might indicate insufficient reviewer capacity. A bottleneck in testing might indicate test environment constraints or QA staffing.

## Issue Type Breakdown
This table compares cycle time across issue types (bugs, stories, tasks, etc.). Bugs typically have shorter cycle times than stories because they have clearer scope and fewer unknowns. If bugs are taking as long as stories, that may indicate insufficient triage or overly complex bug fixes. Large gaps between issue types help with estimation — you can set different cycle time expectations for different work types. The P85 column is especially useful for type-specific commitments.

## Per-Developer Breakdown
This table shows cycle time patterns per developer — not to rank people, but to spot coaching opportunities. A developer with a high median cycle time may be taking on more complex work, may be blocked by dependencies, or may benefit from pairing. The "dominant stage" column is the most actionable: if a developer's tickets consistently spend the most time in code review, the issue may be reviewer availability rather than the developer's speed. Use this data to start 1:1 conversations, not to draw conclusions from numbers alone.

## Dominant Stage
The dominant stage is the workflow stage where a developer's completed tickets spent the most time on average. This pinpoints where delays occur for that developer specifically. A developer whose dominant stage is "Code Review" may need help finding reviewers or may be submitting large PRs that take longer to review. A developer whose dominant stage is "In Progress" may be taking on work that is too large or too ambiguous. The dominant stage highlights where targeted intervention will have the most impact.

## Outlier Table
The outlier table lists every ticket that took more than twice the sprint median cycle time, sorted slowest first. For each ticket, you can see the full stage breakdown — which stage consumed the most time — and the rework count. Use this table in retrospectives to identify common patterns: are outliers concentrated in a particular issue type, developer, or stage? Are they rework-heavy? Do they share a common blocker? Even 2-3 outliers can significantly drag up the P85, so reducing outlier frequency is often the fastest path to improving team predictability.

## P85 Trend Line
The P85 trend line shows your team's cycle time predictability across sprints. Each point represents the P85 cycle time for one sprint. A downward trend means the team is becoming more predictable and faster. A flat line means stability. An upward trend is a warning — increasing complexity, growing WIP, or process degradation. Compare the trend line against changes you've made (new team members, process changes, tooling upgrades) to correlate improvements. This is the single best chart for measuring whether your process improvement efforts are working.

## Sprint Summary Table
The sprint summary table gives a bird's-eye view across multiple sprints. Each row summarizes one sprint's cycle time performance: how many tickets completed, the median and P85 cycle times, and how many outliers occurred. Use this to quickly spot the best and worst sprints, then click through to investigate what was different. Sprints with unusually high P85 or outlier counts are candidates for deeper retro analysis.

## Cycle Starts At (Settings)
This setting determines which workflow stage begins the cycle time measurement. By default, it is set to the second workflow stage (the first stage after the backlog), which typically represents "In Progress" or the equivalent in your team's workflow. Adjust this if your team's definition of "work started" differs — for example, if you have a "Refinement" stage that should not count toward active cycle time. Only stages from your configured workflow pipeline appear in the dropdown.

## Cycle Ends At (Settings)
This setting determines which workflow stage ends the cycle time measurement. By default, it is set to the first done status. Adjust this if you want to measure a subset of the pipeline — for example, measuring only "In Progress" to "Code Review" to isolate development time from review and testing time. The end stage is inclusive: time spent in the end stage counts toward cycle time. Only stages from your configured workflow pipeline and done statuses appear in the dropdown.
