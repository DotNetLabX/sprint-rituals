# App Shell & Navigation — Guide Page Help

Sibling of `docs/features/AppShell/spec.md`. Each section provides the Long variant (guide page paragraph explaining interpretation and recommended actions).

---

## Sidebar Navigation

The sidebar is always visible on every page. At full desktop width it shows icons alongside text labels. When you resize the browser window or use split-screen, the sidebar automatically collapses to show icons only — and expands again when space is available. There is no manual toggle; the behavior is entirely automatic based on your viewport width. The currently active section is visually highlighted so you always know where you are.

---

## Dashboard

Dashboard is the default section when you open Fokus. It serves as the entry point for all analytics — sprint health, developer throughput, and delivery trends. If no sprint data has been synced yet, Dashboard shows a guided empty state telling you how to get started. Once data is available, the page displays analytics cards following the shared card-based layout used across all analytics sections.

---

## Theme Toggle

The theme toggle sits in the app header. Clicking it switches every element in the UI between dark and light mode. Your preference is saved in the browser and persists across page reloads and future visits. First-time visitors see the dark theme by default. The toggle affects all pages, cards, charts, and empty states — everything respects the active theme.

---

## Empty States

When you navigate to Dashboard, Developers, Sprints, or Epics before any sprint data has been synced, you see a contextual empty state instead of a blank page. Each empty state includes an icon, a message explaining why there is no data, and a call-to-action directing you to the next step — typically syncing your first sprint in Settings. Empty states are theme-aware, so they look correct in both dark and light modes. Once data is available, the empty state is replaced by the section's analytics content automatically.

---

## Card-Based Layout

All analytics pages (Dashboard, Developers, Sprints, Epics) use a card-based layout where each metric, chart, or content group occupies its own card. Cards provide visual separation and consistent spacing, making it easier to scan multiple data points at a glance. This layout is a design convention shared across every analytics feature — you will see the same card structure regardless of which section you are viewing.

---

## Page Toolbar

Each analytics page has a toolbar area below the page title that holds page-level controls. The toolbar contains two dropdowns: the sprint selector and the sub-team filter. These controls scope all content on the current page to the selected sprint and team. The toolbar belongs to the page, not the app header — changing filters on one page does not affect other pages unless they share the same selection state.

---

## Sprint Selector

The sprint selector dropdown offers two modes. You can pick a specific sprint to see detailed metrics for that sprint, or you can pick an aggregate window — Last 3, Last 5, or All — to see trends and averages across multiple sprints. The aggregate options change how all page content is computed: charts show trend lines, summary cards show averages, and tables show cross-sprint comparisons. Use single-sprint mode for retrospective deep-dives and multi-sprint mode for spotting patterns over time.

---

## Sub-Team Filter

The sub-team filter dropdown defaults to "All," which shows data for every developer. Selecting a specific sub-team name restricts all metrics, charts, and tables on the current page to only the developers belonging to that sub-team. This is useful for comparing performance across sub-teams or focusing a retrospective on a specific group. The filter applies to all content on the page — there is no way to mix sub-teams in a single view.

---

## Design Tokens

Fokus uses a design token layer — a set of named color values for surfaces, borders, text hierarchy, accents, and status indicators (success, warning, danger). Every page and component references these tokens rather than raw color values. This means the entire app maintains visual consistency regardless of which feature you are viewing, and theme switching (dark/light) works uniformly because both themes define the same token names with different values.

---

## Responsive Sidebar Collapse

The app is designed for desktop monitors (1440px and above) and laptops (1366px minimum). When you resize your browser or use split-screen mode, the sidebar detects available space and collapses to show only icons — no text labels. When the window widens again, the sidebar restores the full icon-and-label view. This happens automatically with no manual control needed. The sidebar never disappears entirely — icons are always visible so you can navigate between sections at any viewport width.
