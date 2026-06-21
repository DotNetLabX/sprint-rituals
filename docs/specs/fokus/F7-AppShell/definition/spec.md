# App Shell & Navigation

**Traces to:** `docs/product/v1.md` §6 (UI Structure), §6.1 (Navigation), §6.2 (Design Direction), §6.3 (Visual References)
**Covers:** F7 (App Shell & Navigation)
**Dependencies:** F1 (Project Scaffolding)
**Status:** Done
**Plan:** `docs/plans/AppShell/plan.md`

---

## Purpose

Every analytics feature (F8-F14) needs a navigation frame, a consistent visual language, and shared layout conventions before it can render content. This feature establishes the app shell — the persistent chrome that wraps every page — and the design foundation that all subsequent features build on. Without it, each feature would reinvent its own layout, sidebar, and theme handling.

## Entities

This feature introduces no domain entities. It is a purely visual/structural feature.

## User Flows

```
Flow 1: Navigate Between Sections
1. User opens the app
2. Sidebar displays five navigation items: Dashboard, Developers, Sprints, Epics, Settings
3. The current section is visually highlighted in the sidebar
4. User clicks a different section
5. The main content area updates to show that section's page
6. The sidebar highlight moves to the selected section
7. The URL updates to reflect the current section
```

```
Flow 2: Toggle Theme
1. User clicks the theme toggle in the app header
2. The entire UI switches between dark and light themes
3. The preference persists across browser sessions
4. On next visit, the app loads with the last-selected theme
5. Default for first-time visitors: dark theme
```

```
Flow 3: Sidebar Collapse (Narrow Viewport)
1. User resizes the browser window or uses split-screen
2. When the viewport narrows below the collapse threshold, the sidebar automatically collapses to show icons only
3. When the viewport widens back, the sidebar expands to show icons and labels
4. No manual toggle — this is purely automatic based on available space
```

```
Flow 4: View Empty Section
1. User navigates to a section with no data yet (Dashboard, Developers, Sprints, or Epics before any syncs)
2. Instead of a blank page, the section displays a contextual empty state:
   - A relevant icon
   - A message explaining why there's no data
   - A call-to-action directing the user to the next step (e.g., "Sync a sprint to see data here")
3. The empty state matches the app's visual language (theme-aware, consistent styling)
```

## API Surface

None. This feature introduces no new endpoints. The sidebar, header, theme, and layout are entirely client-side.

## Business Rules

1. **Five navigation sections.** Dashboard (landing page), Developers, Sprints, Epics, Settings. This set is fixed in v1 — no dynamic menu items, no user-customizable nav.

2. **Dashboard is the landing page.** Opening the app navigates to Dashboard by default. There is no separate home page or splash screen.

3. **Dark theme default.** First-time visitors see the dark theme. The toggle switches between dark and light. The choice persists in the browser across sessions.

4. **Sidebar always visible.** The sidebar is always present — it never fully hides. At full width it shows icons and labels. Below the collapse threshold it shows icons only. There is no hamburger menu, no off-screen drawer.

5. **Design token layer.** The app defines a shared set of color tokens for surfaces, borders, text hierarchy, accent, and status colors (success, warning, danger). All pages use these tokens instead of raw color values. This ensures visual consistency across features built by different people at different times.

6. **Card-based layout.** Analytics pages use a card-based layout — each metric or content group lives in its own card with appropriate spacing. This is a design principle, not a per-feature choice. All analytics features (F8-F14) follow this convention. Cards provide visual separation, breathing room, and a consistent unit of composition across all dashboard screens.

7. **Page layout convention.** Analytics pages (Dashboard, Developers, Sprints, Epics) share a consistent structure: a title area at the top, an optional toolbar area for page-level filters (used by F8+ when they add the sprint selector and sub-team filter), and a content area for cards and charts. Settings does not follow this convention — it has its own form-based layout.

8. **Page toolbar is a shared convention, not a shell element.** The sprint selector and sub-team filter are page-level controls, not part of the persistent header. They appear within each analytics page's toolbar area. This matches industry patterns (Grafana, Datadog, LinearB) where filters scope to the active dashboard, not the app globally. The toolbar visual is built in this feature; data-wiring happens in F8 when the first analytics page needs it. The toolbar contains two controls:
   - **Sprint selector** — a dropdown with two modes: pick a specific sprint, or pick an aggregate window (Last 3, Last 5, All). This is not a simple single-value picker — the aggregate options affect how all page content is computed.
   - **Sub-team filter** — a dropdown with options: All (default, shows everyone) or a specific sub-team name. Filters all page content to developers in the selected sub-team.

9. **Responsive floor: 1366px.** The app is designed for desktop monitors (1440px primary) and laptops (1366px minimum). Split-screen at 1440px (~720px half) is supported via sidebar collapse. There is no tablet or mobile layout.

10. **Empty state consistency.** Every section that depends on synced data shows a themed empty state when no data exists. The empty state always tells the user what to do next — it never shows a blank page or a generic "no data" message.

## Acceptance Criteria

- [ ] Opening the app shows the Dashboard section with the sidebar visible
- [ ] All five navigation items are present in the sidebar: Dashboard, Developers, Sprints, Epics, Settings
- [ ] Clicking a sidebar item navigates to the corresponding section and updates the URL
- [ ] The currently active section is visually distinguished in the sidebar
- [ ] A theme toggle is visible in the app header area
- [ ] Clicking the theme toggle switches the entire UI between dark and light themes
- [ ] The selected theme persists across page reloads and new browser sessions
- [ ] First visit defaults to dark theme
- [ ] At full desktop width (1440px+), the sidebar shows icons and text labels
- [ ] When the viewport narrows below the collapse threshold, the sidebar collapses to icons only automatically
- [ ] When the viewport widens back, the sidebar expands automatically
- [ ] The sidebar never fully disappears — icons are always visible
- [ ] Dashboard, Developers, Sprints, and Epics sections show a contextual empty state when no sprint data has been synced
- [ ] Each empty state includes an icon, an explanatory message, and a call-to-action
- [ ] Empty states respect the current theme (dark and light)
- [ ] Settings page renders its existing form layout within the shell without regression
- [ ] The app defines a color token layer used consistently across the shell and all page content
- [ ] Analytics pages follow the shared layout convention (title area, optional toolbar area, content area)
- [ ] The page toolbar area is present but unpopulated — it will be activated by F8
- [ ] The toolbar visually contains a sprint selector dropdown and a sub-team filter dropdown (placeholder/static, not data-wired)

## Visual References

These inform the overall design direction for Fokus. They are most relevant to analytics features (F8-F14) but established here as the design foundation:

- **Hatica Sprint Performance Dashboard** — signal cards + per-developer breakdown + cycle time chart. Closest overall layout reference.
- **LinearB** — individual contributor sparklines, clean developer comparison tables.
- **Harness SEI** — ratio-based health signals with thresholds, flow chart diagnostics.

## Out of Scope

- **Functional sprint selector and sub-team filter** — the toolbar area and dropdown visuals are part of this feature; data-fetching and state management for filters are deferred to F8 (Sprint Summary Card), which is the first consumer.
- **Mobile and tablet layouts** — not targeted. The minimum supported width is 1366px.
- **Manual sidebar collapse toggle** — collapse is automatic based on viewport width. A user-controlled pin/unpin toggle is unnecessary complexity for v1.
- **Breadcrumbs or nested navigation** — all five sections are top-level. No drill-down navigation in v1 shell; analytics features handle their own detail views.
- **User avatar or profile menu** — no authentication UI in the shell. Auth is deferred.
- **Notification indicators** — no badge counts, no sync status in the sidebar.
- **Animated page transitions** — standard browser navigation. No slide/fade between sections.
- **Health indicator convention (green/amber/red badges)** — defined by F8 (Sprint Summary Card) when the first cards with status data are built. The design token layer establishes the status colors; F8 establishes how they're applied to cards.
- **ApexCharts animation conventions** — chart-specific, deferred to whichever feature first uses charts (F8).
- **Metrics density guideline (5-7 per screen)** — a per-page layout concern, deferred to individual analytics feature specs (F8-F14).
