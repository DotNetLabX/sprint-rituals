# Daily Progress Enhancements

**Traces to:** `docs/product/v1.md` §5.1 (Developer Throughput — per-developer analytics). Extends F32 (Daily Developer Progress) with work-type visibility, chart usability fixes, and actionable alerting.
**Source:** Scratch
**Dependencies:** F32 (Daily Developer Progress — the tab, cards, burnup charts, alert banner, and stall detection this feature enhances)
**Status:** Done
**Plan:** `docs/specs/F33-DailyProgressEnhancements/delivery/plan.md`

---

## Purpose

F32 introduced per-developer burnup cards for the active sprint. In practice, three problems reduce the tab's usefulness: (1) the "Behind Pace" alert banner lists everyone when everyone is behind — providing noise instead of signal, (2) progress cards show total SP but don't distinguish bugs from features, so a Scrum Master can't tell whether a developer is behind because of bug work or slow feature delivery, and (3) the mini burnup charts are too small and have rendering defects (inverted Y-axis, unpredictable zoom) that make them unreadable. This feature addresses all three.

## Entities

No new entities. This feature modifies the response shape of the existing developer progress endpoint and the frontend components that consume it.

**Response extensions (developer progress endpoint):**

Per developer entry — two new fields:
- `featureCompletedSp` (decimal) — cumulative SP completed for non-bug ticket types
- `bugCompletedSp` (decimal) — cumulative SP completed for bug ticket types

Per alert entry — two new fields:
- `gapDelta` (decimal) — change in SP gap since the previous day. Positive = gap grew (developer fell further behind). Negative = gap shrank (developer made progress). Zero or null on day 1.
- `direction` (string) — one of: `worsening`, `improving`, `stable`, `new-stall`, `stall-resolved`. Derived from gapDelta and stall state changes.

Top-level alerts list changes:
- Rename conceptually from "behind pace" to "pace changes" — the list now includes developers whose pace changed in either direction, not just those who are behind.
- Add developers who are improving (gap decreased since yesterday) alongside those who are worsening.
- Add developers with newly stalled tickets (`new-stall`) or resolved stalls (`stall-resolved`).

## User Flows

```
Flow 1: View Bug/Feature Breakdown on Cards
1. User opens the Daily Progress tab
2. Each developer card shows the total SP line as before: "26.0 / 50.0 SP (52%) (all types)"
3. Below the total, a split line shows: "18.0 features / 8.0 bugs"
4. If a developer has completed only features or only bugs, the split line shows the single category
5. If a developer has completed 0 SP, the split line is hidden
```

```
Flow 2: Read the Pace Changes Banner
1. User opens the Daily Progress tab after the grace period
2. The alert banner title reads "Pace Changes" instead of "Behind Pace"
3. The banner groups entries into two sections:
   a. Worsening / Stalled — developers whose gap increased or who have newly stalled tickets. Shown with a warning indicator and sorted by gap delta (largest increase first).
   b. Improving / Recovered — developers whose gap decreased or whose stalls resolved. Shown with a positive indicator and sorted by gap delta (largest improvement first).
4. Each entry shows: developer name, current gap (SP behind or ahead), and the delta ("gap grew by 3.2 SP" or "gap shrank by 5.0 SP" or "1 ticket newly stalled" or "stall resolved")
5. If no developer's pace changed since yesterday (all stable), the banner is hidden
6. The banner is hidden during the grace period (first 2 days), same as F32
7. On day 3 (first day after grace period), all behind-pace developers are shown as "worsening" since there is no prior-day comparison — this is the initial baseline
```

```
Flow 3: Readable Burnup Charts
1. User opens the Daily Progress tab
2. Each developer card's burnup chart renders at a taller height than before — enough vertical space to distinguish the actual line from the expected line
3. Y-axis is always oriented correctly: 0 at the bottom, max SP at the top
4. Zoom is disabled on the card-level mini charts — the small chart is an overview, not an interactive analysis tool
5. Hovering a day point still shows the ticket completion tooltip (unchanged from F32)
```

## API Surface

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|-------------|
| GET | /api/analytics/developer-progress | Authenticated | — | DeveloperProgressResponse (extended) | 200 |

No new endpoints. The existing endpoint is extended with additional fields. Query parameters unchanged (`?subTeam` optional).

**Response extensions:**

Per developer entry adds:
- `featureCompletedSp` — sum of effective SP for completed non-bug tickets
- `bugCompletedSp` — sum of effective SP for completed bug tickets
- Invariant: `featureCompletedSp + bugCompletedSp = completedSp`

Per alert entry adds:
- `gapDelta` — decimal, change in pace gap since previous day
- `direction` — string enum: `worsening` | `improving` | `stable` | `new-stall` | `stall-resolved`

Alerts list changes:
- Now includes improving developers (not just behind-pace). A developer appears in the alerts list when their `direction` is anything other than `stable`.
- Developers who are on pace and whose pace did not change are excluded (no alert entry).

**Error conditions:** Unchanged from F32.

## Business Rules

### Bug/Feature Breakdown

1. **Issue type classification.** A ticket is a "bug" when its issue type equals "Bug" (exact case-sensitive match). All other issue types (Story, Task, Sub-task, etc.) are classified as "features" for the purpose of the SP split. This matches the classification used by the Leaderboard (F19), Bug Ratio (F13), and Throughput (F9) features — all use case-sensitive comparison.

2. **Split covers completed SP only.** The feature/bug split applies to `completedSp`, not `assignedSp`. The split tells you what work was done, not what was planned.

3. **SP attribution follows existing rules.** Same effective SP computation as F32: ticket SP if present, configured default SP per bug for unestimated bugs, null for unestimated non-bugs. Null-SP tickets are excluded from both `featureCompletedSp` and `bugCompletedSp` sums.

### Delta-Based Alerting

4. **Gap delta computation.** For each developer on day N (where N > 1): `gapDelta = gapSp(day N) - gapSp(day N-1)`. Where `gapSp = expectedCumulativeSp - cumulativeSp`. A positive gapDelta means the developer fell further behind; a negative gapDelta means they caught up. The delta is computed from the daily breakdown array (which contains both `cumulativeSp` and `expectedCumulativeSp` for each day), not from stored state. Weekend days increase expected pace without corresponding completions, so Monday deltas will typically be positive for all developers — this is expected behavior consistent with the calendar-day pace model from F32. Round gapDelta to 1 decimal place, consistent with F32's paceGapSp rounding.

5. **Day 1 has no delta.** On the first day of the sprint, gapDelta is null for all developers. No pace change alerts are shown (also covered by the grace period).

6. **Direction derivation.** Computed from gapDelta and stall state changes:
   - `worsening` — gapDelta > 0 (gap grew) and no stall change
   - `improving` — gapDelta < 0 (gap shrank) and no stall change
   - `stable` — gapDelta = 0 and no stall change
   - `new-stall` — developer has at least one stalled ticket today that was not stalled yesterday
   - `stall-resolved` — developer had stalled tickets yesterday but has none today

   When both a gap change and a stall change occur simultaneously, the stall signal takes priority (`new-stall` or `stall-resolved`). The gap delta is still included in the alert entry for context. A `stall-resolved` developer appears in the Improving/Recovered group even if their gap increased, because stall resolution is the stronger signal. Conversely, a `new-stall` developer appears in Worsening/Stalled even if their gap decreased. The stall count for display text ("1 ticket newly stalled") is derived from the developer's `stalledTickets` list in the existing response — no new field needed.

7. **Alert inclusion rule.** A developer appears in the alerts list when their direction is anything other than `stable`. Developers who are behind pace but whose gap did not change are not listed — they were already behind yesterday, and the alert is about *change*, not position.

8. **Day 3 baseline.** On the first day after the grace period (day 3), gapDelta is computed normally using the actual day 2 gap: `gapDelta = gapSp(day 3) - gapSp(day 2)`. The underlying gap data exists during the grace period — the grace period only suppresses alert display, not computation. All developers who are behind pace (using F32's threshold: gap > dailyPace) and have a non-zero gapDelta appear in the banner. This establishes the first visible baseline. From day 4 onward, normal delta comparison continues.

9. **Alert sorting.** The banner renders two groups in order: Worsening/Stalled first, then Improving/Recovered. Within the "worsening" group: sort by gapDelta descending (largest increase first). Within the "improving" group: sort by gapDelta ascending (largest improvement first). Stall entries (`new-stall`, `stall-resolved`) appear at the top of their respective group.

10. **Empty banner.** If all developers have direction = `stable`, the banner is hidden entirely. No "all stable" message — silence is the signal.

### Chart Fixes

11. **Y-axis orientation.** The burnup chart Y-axis must always render with 0 at the bottom and the maximum value at the top. The maximum value remains `assignedSp x capacityPercent / 100` (unchanged from F32).

12. **Chart height.** The card burnup chart renders at a minimum height that makes the gap between actual and expected lines visually distinguishable. The exact pixel value is a design decision, but it must be significantly taller than the current rendering.

13. **Zoom disabled on cards.** Mini burnup charts in the card grid do not support zoom, pan, or scroll-wheel interactions. Both the chart toolbar and programmatic zoom must be explicitly disabled — hiding the toolbar alone is insufficient since scroll/pinch gestures can still trigger zoom. They are static overview charts. The Developer Detail Page (F34) will provide interactive charts.

## Acceptance Criteria

### Bug/Feature Breakdown

- [ ] Each developer card shows a "features / bugs" SP split line below the total SP line
- [ ] Split line hidden when completed SP is 0
- [ ] `featureCompletedSp + bugCompletedSp` equals `completedSp` for every developer
- [ ] Bug classification matches Leaderboard and Bug Ratio (issue type == "Bug", case-sensitive exact match)

### Pace Changes Banner

- [ ] Banner title is "Pace Changes" (not "Behind Pace")
- [ ] Banner shows two groups: "Worsening / Stalled" and "Improving / Recovered"
- [ ] Each entry shows: developer name, current gap, and delta description
- [ ] Worsening group sorted by gap delta descending; improving group sorted by gap delta ascending
- [ ] Stall entries appear at the top of their respective group
- [ ] Banner hidden when all developers are stable (no pace changes)
- [ ] Banner hidden during grace period (first 2 days)
- [ ] Day 3 shows all behind-pace developers as worsening (baseline)
- [ ] From day 4 onward, only developers with actual pace changes appear
- [ ] Banner respects sub-team filter

### Chart Fixes

- [ ] Y-axis always oriented correctly (0 at bottom, max at top) for all developers
- [ ] Chart height is visually increased — actual vs expected lines are distinguishable
- [ ] Zoom and pan interactions disabled on card-level mini charts
- [ ] Day hover tooltip still works (unchanged from F32)

### API

- [ ] GET /api/analytics/developer-progress response includes `featureCompletedSp` and `bugCompletedSp` per developer
- [ ] Response alert entries include `gapDelta` and `direction` fields
- [ ] Alerts list includes improving developers, not just behind-pace
- [ ] Existing developer entry fields unchanged. Alert list shape extended (new fields added, existing fields retained). Alert list now includes improving developers — frontend update required for the banner component

## Out of Scope

- **Stacked burnup chart (bugs vs features as areas)** — The card chart stays as a single-line burnup. A stacked view would be more useful on the Developer Detail Page (F34) where chart space is larger.
- **Configurable stall threshold** — Remains fixed at 2 business days (deferred in F32).
- **Historical delta (compare with same sprint day in prior sprints)** — "You were at 40% on day 8 last sprint, now you're at 25%" is interesting but adds complexity. Deferred to F34 cross-sprint trends.
- **Per-card delta badge** — An alternative to the banner where each card shows its own delta indicator. The banner was chosen because it provides a scannable summary without requiring visual inspection of every card. Could be added later as a complement.

## Open Questions

None — all resolved during discussion.
