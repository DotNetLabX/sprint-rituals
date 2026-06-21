# App Shell & Navigation

**Feature Spec:** `docs/features/AppShell/spec.md`

## Context

Every analytics feature (F8-F14) needs a navigation frame, a consistent visual language, and shared layout conventions before it can render content. The current frontend has five route-level views (Dashboard, Developers, Sprints, Epics, Settings) but no persistent shell — each view renders full-screen with no sidebar, header, or shared chrome. Settings has a complete form layout; the other four are placeholder stubs.

This feature builds the app shell: sidebar navigation, header with theme toggle, design token layer, page layout convention with toolbar area, and contextual empty states. It is purely frontend — no new API endpoints or domain entities.

**Service impacted:** `client/` (Vue 3 SPA, builds to `src/Services/Fokus/Fokus.API/wwwroot/`).

## Scope

**In scope:**
- Sidebar with five navigation items (Dashboard, Developers, Sprints, Epics, Settings), active-route highlighting, icons
- Automatic sidebar collapse (icons-only) below a viewport threshold (~1024px)
- Header area with theme toggle (dark/light)
- Dark theme default, localStorage persistence across sessions
- Design token layer via Tailwind v4 CSS custom properties (`@theme` in `main.css`)
- Page layout component for analytics pages (title area, toolbar area, content area)
- Toolbar area with static sprint selector and sub-team filter dropdowns (visual only, not data-wired)
- Contextual empty states for Dashboard, Developers, Sprints, Epics
- Settings page integrated into the shell without regression (retains existing form layout)
- Card component foundation for analytics pages

**Out of scope (per spec):**
- Functional sprint selector / sub-team filter (data-wiring deferred to F8)
- Mobile/tablet layouts (minimum 1366px)
- Manual sidebar collapse toggle
- Breadcrumbs or nested navigation
- User avatar / profile menu
- Notification indicators
- Animated page transitions
- Health indicator convention (F8)
- ApexCharts animation conventions (F8+)
- Metrics density guideline (F8-F14)

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | (none) | — | Design tokens via Tailwind v4 @theme directive | Frontend patterns gap |
| 2 | (none) | — | Vue 3 composable for theme state + localStorage | Frontend patterns gap |
| 3 | (none) | — | Vue 3 layout components (sidebar, header) | Frontend patterns gap |
| 4 | (none) | — | Vue 3 sidebar with vue-router active matching | Frontend patterns gap |
| 5 | (none) | — | Vue 3 page layout + toolbar components | Frontend patterns gap |
| 6 | (none) | — | Vue 3 empty state component | Frontend patterns gap |
| 7 | (none) | — | Refactor existing views to use shell layout | Frontend patterns gap |
| 8 | (none) | — | Dark/light class toggle on html element | Frontend patterns gap |

All steps have disposition "None" because no frontend skill exists. This is a known gap documented in `docs/architecture/v2.md` under Gaps > Frontend patterns. Logged to lessons.md.

## Domain Model Changes

None. This is a purely visual/structural feature.

## Data Model Changes

None.

## Implementation Steps

### Step 1: Design token layer

**What:** Define the color token system as CSS custom properties using Tailwind v4's `@theme` directive. This establishes the shared visual language that all features build on. Tokens cover: surface colors (page background, card background, elevated surface), border colors, text hierarchy (primary, secondary, muted), accent color, and status colors (success, warning, danger). Define tokens for both dark and light themes.

**Files to create/modify:**
- `client/src/assets/main.css` — add `@theme` block with CSS custom properties for colors; add `.dark` and `:root` (light) scoped token definitions

**Pattern:** Tailwind v4 uses `@theme` for extending the design system. Color tokens are defined as CSS custom properties and referenced in Tailwind classes via `--color-*`. The dark/light variants use CSS scoping (`.dark` class on `<html>`, already present in `index.html`).

**Token categories to define:**
- `--color-surface-{page,card,elevated}` — background layers
- `--color-border-{default,subtle}` — borders
- `--color-text-{primary,secondary,muted}` — text hierarchy
- `--color-accent-{default,hover}` — interactive accent
- `--color-status-{success,warning,danger}` — RAG status colors (used by F8+ for health indicators)
- `--color-sidebar-{bg,item-hover,item-active}` — sidebar-specific tokens

**Dependencies:** None (first step).

---

### Step 2: Theme composable

**What:** Create a composable that manages dark/light theme state. It reads the initial theme from `localStorage` (key: `fokus-theme`), defaults to `dark` on first visit, toggles the `dark` class on `document.documentElement`, and persists changes to `localStorage`. Expose a reactive `isDark` ref and a `toggleTheme` function.

**Files to create:**
- `client/src/composables/useTheme.ts`

**Files to modify:**
- `client/src/main.ts` — import and initialize the theme composable at app startup (before mount) so the correct class is applied before first paint. This prevents a flash of wrong theme.

**Dependencies:** Step 1 (tokens must exist so the theme class has meaning).

---

### Step 3: Shell layout — AppShell component

**What:** Create the persistent app shell component that wraps all pages. It provides a sidebar on the left and a main content area on the right (with a header row at the top of the content area). The sidebar and header are always visible. The `<router-view>` renders inside the main content area.

**Files to create:**
- `client/src/components/AppShell.vue` — the root layout. Uses CSS grid or flexbox: sidebar (fixed width left) + main area (flex-1 right). Main area has header row at top, scrollable content below.
- `client/src/components/AppHeader.vue` — header bar inside the main content area. Contains the app name/logo area on the left and the theme toggle button on the right. Consumes the `useTheme` composable for the toggle.

**Files to modify:**
- `client/src/App.vue` — replace bare `<router-view />` with `<AppShell>` wrapping `<router-view />`.

**Layout structure:**
```
┌──────────┬──────────────────────────────────┐
│          │  AppHeader (theme toggle)         │
│ Sidebar  ├──────────────────────────────────┤
│          │  <router-view /> (scrollable)    │
│          │                                  │
└──────────┴──────────────────────────────────┘
```

**Dependencies:** Step 2 (theme composable for header toggle).

---

### Step 4: Sidebar navigation

**What:** Create the sidebar component with five navigation items: Dashboard (`/`), Developers (`/developers`), Sprints (`/sprints`), Epics (`/epics`), Settings (`/settings`). Each item has an icon and a text label. The currently active route is visually highlighted. The sidebar uses `vue-router`'s `RouterLink` with active class matching.

Implement automatic collapse: use a `matchMedia` listener (or `ResizeObserver` on the shell) that detects when viewport width drops below 1024px. When collapsed, the sidebar shows icons only (no labels). When expanded, it shows icons + labels. No manual toggle — purely automatic. Use a reactive ref so the template conditionally renders labels.

**Files to create:**
- `client/src/components/AppSidebar.vue` — sidebar with nav items, collapse logic, active route styling.

**Files to modify:**
- `client/src/components/AppShell.vue` — integrate `AppSidebar` into the shell layout.

**Icon approach:** Use inline SVG icons (no icon library dependency). Five simple icons: grid/dashboard, users/developers, iterations/sprints, layers/epics, gear/settings. Define them as small SVG components or inline in the sidebar template.

**Dependencies:** Step 3 (shell layout exists to host the sidebar).

---

### Step 5: Analytics page layout and toolbar

**What:** Create a shared page layout component for analytics pages (Dashboard, Developers, Sprints, Epics). It provides the consistent structure specified in the spec: title area at top, optional toolbar area below the title, and content area for cards. Settings does NOT use this layout — it has its own form layout.

Create the toolbar component with two static dropdown controls: a sprint selector (with placeholder text "Select sprint...") and a sub-team filter (with placeholder text "All"). These are visual-only — they render as styled dropdown-looking elements but are not functional (no data binding, no options list, no open/close behavior). F8 will replace them with real interactive dropdowns.

Create a base card component that provides the standard card styling (background, border, padding, border-radius) used by all analytics features.

**Files to create:**
- `client/src/components/PageLayout.vue` — slot-based layout: `title` slot, `toolbar` slot (optional), default slot for content area.
- `client/src/components/PageToolbar.vue` — contains the two static dropdown placeholders (sprint selector, sub-team filter). Styled to look like dropdown controls but non-interactive.
- `client/src/components/BaseCard.vue` — simple card wrapper with consistent styling using design tokens.

**Dependencies:** Step 1 (design tokens for card and toolbar styling).

---

### Step 6: Empty state component

**What:** Create a reusable empty state component used by analytics pages when no sprint data exists. It accepts props for: icon (slot or named icon), title text, description text, and call-to-action text. It renders centered within its container, uses the design tokens for theme-aware styling.

**Files to create:**
- `client/src/components/EmptyState.vue` — props: `title` (string), `description` (string), `actionText` (string, optional), `actionRoute` (string, optional). Icon via a named slot. Theme-aware (uses token colors).

**Dependencies:** Step 1 (design tokens).

---

### Step 7: Integrate views into the shell

**What:** Update all five view files to work inside the shell layout.

**Analytics views (Dashboard, Developers, Sprints, Epics):** Replace the current placeholder markup with `PageLayout` + `EmptyState`. Each view uses the `PageLayout` component, passes its title, includes `PageToolbar` in the toolbar slot, and renders `EmptyState` in the content area with a contextual message. Each empty state has:
- **Dashboard:** icon = chart/grid, message = "No sprint data yet", action = "Go to Settings to configure your Jira board, then sync a sprint."
- **Developers:** icon = users, message = "No developer data yet", action = "Sync a sprint to see developer metrics here."
- **Sprints:** icon = iterations, message = "No sprint data yet", action = "Sync a sprint to see sprint analytics here."
- **Epics:** icon = layers, message = "No epic data yet", action = "Sync a sprint to see epic progress here."

**Settings view:** Remove the outer `min-h-screen bg-gray-950 p-8` wrapper (the shell now provides the page background and padding). Keep the inner `max-w-3xl mx-auto space-y-6` content intact. The Settings view does NOT use `PageLayout` or `PageToolbar` — it renders its form directly within the shell's content area. Verify the existing form styling works with the design tokens (it currently uses `bg-gray-900`, `text-gray-100`, etc. — these should be updated to use token-based classes where possible, but functional parity is the priority; a full token migration of Settings internals can happen later).

**Files to modify:**
- `client/src/views/DashboardView.vue` — replace placeholder with PageLayout + EmptyState
- `client/src/views/DevelopersView.vue` — replace placeholder with PageLayout + EmptyState
- `client/src/views/SprintsView.vue` — replace placeholder with PageLayout + EmptyState
- `client/src/views/EpicsView.vue` — replace placeholder with PageLayout + EmptyState
- `client/src/views/SettingsView.vue` — remove outer page wrapper, keep form content

**Dependencies:** Steps 3, 4, 5, 6 (shell, sidebar, page layout, empty state all exist).

---

### Step 8: Verify and polish

**What:** Final verification pass.

- Run `npm run build` from `client/` to confirm TypeScript compilation and Vite build succeed.
- Visually verify in dev server (`npm run dev`):
  - All five routes render correctly inside the shell
  - Sidebar navigation works, active state highlights correctly
  - Theme toggle switches between dark and light, persists across reload
  - Sidebar collapses to icons-only when viewport narrows below 1024px
  - Empty states display on all four analytics pages
  - Toolbar area shows the two static dropdown placeholders
  - Settings form renders without regression (all sections visible, save button works)
- Fix any styling inconsistencies between dark and light themes.
- Ensure the `body` tag's background color in `index.html` matches the dark theme default so there's no flash before Vue mounts.

**Files potentially modified:** Any file from steps 1-7 that needs adjustment.

**Dependencies:** All previous steps.

## Cross-Service Changes

None. Purely frontend.

## Migration Notes

None. No database changes.

## Testing Strategy

**Manual verification (no automated test framework in place):**

1. **Navigation flow:** Click each of the five sidebar items. Verify URL changes, content updates, active highlight moves.
2. **Theme toggle:** Click toggle, verify entire UI switches. Reload page, verify theme persists. Clear localStorage, reload, verify dark default.
3. **Sidebar collapse:** Resize browser window. Below ~1024px, sidebar should collapse to icons only. Widen back, labels reappear. At 1366px (minimum supported), full layout should be comfortable.
4. **Empty states:** Visit Dashboard, Developers, Sprints, Epics. Each shows contextual empty state with icon, message, and CTA. Verify both dark and light themes.
5. **Settings regression:** Open Settings, verify all form sections render (Board ID, Done Statuses, Workflow Stages, Health Thresholds, Health Weights). Add/remove a status, reorder a stage, change a threshold, save. Verify the saved toast appears.
6. **Build verification:** `npm run build` succeeds with zero errors. Output lands in `src/Services/Fokus/Fokus.API/wwwroot/`.
7. **Token consistency:** Inspect any card or surface element — confirm it uses token-based CSS variables, not hardcoded gray-950/gray-900 values (except in Settings which may retain legacy colors).

## Open Questions

None.
