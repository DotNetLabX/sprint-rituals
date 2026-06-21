# App Shell & Navigation — Implementation

## Files Created

- `client/src/composables/useTheme.ts` — Theme composable: reads `fokus-theme` from localStorage, defaults to dark, toggles `.dark`/`.light` on `document.documentElement`, exposes `isDark` ref and `toggleTheme` function. Module-level `isDark` ref so state is shared across all composable call sites.
- `client/src/components/AppHeader.vue` — Header bar: app name on left, theme toggle button on right. Consumes `useTheme`. Sun icon shown in dark mode, moon icon in light mode.
- `client/src/components/AppShell.vue` — Root layout: flex row with `AppSidebar` on left, flex column (header + scrollable main) on right. `<slot>` receives `<router-view>` content.
- `client/src/components/AppSidebar.vue` — Sidebar with five nav items (Dashboard, Developers, Sprints, Epics, Settings). Uses `window.resize` listener to auto-collapse below 1024px (icons-only). Active state via `useRoute()` comparison. Inline SVG icons, no icon library dependency.
- `client/src/components/PageLayout.vue` — Slot-based page layout: `title` prop for heading, named `toolbar` slot (conditional), default slot for content area.
- `client/src/components/PageToolbar.vue` — Visual-only toolbar with sprint selector and sub-team filter dropdowns. Non-interactive (no data binding, no open/close). Styled with design tokens.
- `client/src/components/BaseCard.vue` — Minimal card wrapper: `bg-surface-card border border-border-default rounded-lg p-4` with a default slot.
- `client/src/components/EmptyState.vue` — Reusable empty state: `title`, `description`, optional `actionText`+`actionRoute` props. Named `icon` slot. `RouterLink` CTA rendered only when both action props provided.

## Files Modified

- `client/src/assets/main.css` — Added Tailwind v4 `@theme` block mapping CSS custom property aliases to the token variables, plus `:root`/`.dark` (dark theme defaults) and `.light` (light theme overrides). Token categories: surface, border, text, accent, status, sidebar.
- `client/src/main.ts` — Added `useTheme` import and `initTheme()` call before `app.mount()` to apply the correct theme class before first paint, preventing flash of wrong theme.
- `client/src/App.vue` — Replaced bare `<router-view />` with `<AppShell><router-view /></AppShell>`.
- `client/src/views/DashboardView.vue` — Replaced placeholder with `PageLayout` + `PageToolbar` + `EmptyState` (grid icon, "No sprint data yet", link to Settings).
- `client/src/views/DevelopersView.vue` — Replaced placeholder with `PageLayout` + `PageToolbar` + `EmptyState` (users icon, "No developer data yet").
- `client/src/views/SprintsView.vue` — Replaced placeholder with `PageLayout` + `PageToolbar` + `EmptyState` (refresh icon, "No sprint data yet").
- `client/src/views/EpicsView.vue` — Replaced placeholder with `PageLayout` + `PageToolbar` + `EmptyState` (layers icon, "No epic data yet").
- `client/src/views/SettingsView.vue` — Removed outer `<div class="min-h-screen bg-gray-950 p-8">` wrapper; shell now provides background and padding. Inner `max-w-3xl mx-auto space-y-6` content preserved intact. Internal form styles (gray-900, gray-800, etc.) retained as-is per plan note that full token migration is deferred.
- `client/index.html` — Changed body `class="bg-gray-950 text-gray-100"` to inline `style="background-color:#0a0a0f;color:#f0f0f5"` matching dark theme token values, to prevent background flash before CSS custom properties are applied.

## Key Decisions

- **Module-level `isDark` ref in `useTheme`:** Declared outside the exported function so all call sites (AppHeader, main.ts) share the same reactive ref. Without this, toggling in the header would not reflect in main.ts's `initTheme` reference.
- **`:root` and `.dark` share the same dark token values:** `index.html` already ships with `class="dark"` on `<html>`, so `:root` and `.dark` rules are co-defined. This ensures tokens work even before `initTheme()` runs on first load.
- **`window.resize` for sidebar collapse instead of `ResizeObserver`:** Both approaches are valid per the plan. `matchMedia` / `resize` listener on `window` is simpler and sufficient for the threshold-based requirement. Cleanup in `onUnmounted` prevents memory leaks.
- **`RouterLink` in `EmptyState` without explicit import:** Vue Router registers `RouterLink` globally when `app.use(router)` is called. No import needed in `<script setup>`. Build confirmed zero errors.
- **Inline SVG icons:** No icon library added — all icons are inline SVG path data as specified in the plan.

## Deviations from Plan

- None. All 8 plan steps executed as specified.
