# Dark / Light Theme

**Feature Spec:** `docs/specs/F31-DarkLightTheme/definition/spec.md`

## Context

Reflekt's UI is built entirely on hardcoded Tailwind color classes (`bg-white`, `text-gray-700`, `border-gray-200`, etc.) with no theme awareness. There are 202 hardcoded gray/white references across 37 `.vue` files and 48 semantic color references (badge/status) across 21 files. Three surface CSS variables exist (`--color-surface`, `--color-surface-raised`, `--color-background`) but are defined as fixed light-mode values with no dark-mode counterparts.

This feature establishes a two-layer CSS token system (raw vars per theme class + `@theme` aliases), adds a `useTheme` composable and toggle button, prevents flash-of-wrong-theme on load, and migrates every component from hardcoded colors to theme-aware tokens. Dark mode becomes the default.

**Service impacted:** Frontend only (`web/reflekt-web/`). No backend changes.

## Scope

**In scope:**
- Two-layer CSS variable system with dark/light theme definitions
- Theme-aware column color `-light` tokens (low-opacity in dark mode)
- Theme-aware semantic badge/status tokens (6 variants)
- `useTheme` composable (localStorage persistence, `<html>` class toggle)
- Blocking inline script in `index.html` for flash prevention
- Theme toggle button in AppHeader (sun/moon icon)
- Dark-mode scrollbar styling
- Focus ring offset token
- Card shadow suppression in dark mode (border-only depth)
- Migration of all 37 `.vue` files from hardcoded colors to tokens
- WCAG AA contrast compliance in both themes

**Out of scope (per spec):**
- Per-user server-side theme preference
- System preference detection (`prefers-color-scheme`)
- Theme transition animation
- Per-component/per-page theme override
- Custom accent color selection
- High contrast mode

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | tailwind-theme | Follow | Dark/light palette hex values from spec, two-layer pattern, column-light token overrides, semantic badge tokens, scrollbar/shadow/focus tokens | |
| 2 | vue-patterns | Follow | `useTheme` composable — localStorage key `reflekt:theme`, default `dark`, `<html>` class toggle | |
| 3 | (none) | — | Blocking inline `<script>` in index.html — reads localStorage, sets class on `<html>` before first paint | Flash prevention pattern could be a skill |
| 4 | vue-patterns | Follow | ThemeToggle — icon button component, Sun/Moon icons from lucide-vue-next | |
| 5 | tailwind-theme | Follow | Base UI components token migration — 8 components | |
| 6 | tailwind-theme | Follow | Layout + teams components token migration — 7 components | |
| 7 | tailwind-theme | Follow | Sprints components token migration — 3 components | |
| 8 | tailwind-theme | Follow | Retro components token migration — 13 components | |
| 9 | tailwind-theme | Follow | Views token migration — 6 views | |
| 10 | (none) | — | Build verification + visual spot-check in both themes | |

## Domain Model Changes

None.

## Data Model Changes

None.

## Implementation Steps

### Step 1: Establish the CSS token system

**Skill:** Follow `tailwind-theme`

**File:** `web/reflekt-web/src/assets/main.css`

Restructure the existing `@theme` block and add `:root` (dark default) and `.light` theme class blocks using the two-layer pattern from the skill.

**Token definitions (spec values):**

Surface/text/border raw vars (set in `:root` for dark, overridden in `.light`):

| Raw var | Dark value | Light value |
|---------|-----------|-------------|
| `--surface-page` | `#0a0a0f` | `#f4f4f8` |
| `--surface-card` | `#13131a` | `#ffffff` |
| `--surface-elevated` | `#1c1c26` | `#eaeaf0` |
| `--border-default` | `#2a2a38` | `#e2e2ea` |
| `--border-subtle` | `#1e1e2a` | `#ededf3` |
| `--text-primary` | `#f0f0f5` | `#1a1a2e` |
| `--text-secondary` | `#a0a0b8` | `#6b6b80` |
| `--text-muted` | `#60607a` | `#9b9bab` |
| `--scrollbar-thumb` | `#2a2a38` | (not set — browser default) |
| `--focus-offset` | `#0a0a0f` | `#f4f4f8` |

`@theme` aliases (map raw vars to Tailwind utility names):
- `--color-background: var(--surface-page)`
- `--color-surface: var(--surface-card)`
- `--color-surface-raised: var(--surface-elevated)`
- `--color-border: var(--border-default)` (new)
- `--color-border-subtle: var(--border-subtle)` (new)
- `--color-text-primary: var(--text-primary)` (new)
- `--color-text-secondary: var(--text-secondary)` (new)
- `--color-text-muted: var(--text-muted)` (new)
- `--color-scrollbar-thumb: var(--scrollbar-thumb)` (new)
- `--color-focus-offset: var(--focus-offset)` (new)

Existing `--color-primary-*` tokens remain unchanged (theme-independent).

**Column color `-light` tokens** — move from `@theme` to theme-class blocks:
- In `:root` (dark): each `--col-{name}-light` = the saturated color at ~10% opacity over dark surface. Use `color-mix(in srgb, var(--color-col-{name}) 10%, #13131a)` or pre-computed hex values.
- In `.light`: each `--col-{name}-light` = the current hardcoded pastel values (e.g., green-light = `#d1fae5`).
- In `@theme`: `--color-col-{name}-light: var(--col-{name}-light)` — the alias references the raw var.
- Same treatment for `--color-action-items-light`.

**Semantic badge tokens** (new, 6 variants x 2 properties = 12 raw vars):

| Variant | bg dark | text dark | bg light | text light |
|---------|---------|-----------|----------|------------|
| success | `color-mix(in srgb, #10b981 10%, #13131a)` | `#34d399` | `#d1fae5` | `#065f46` |
| danger | `color-mix(in srgb, #ef4444 10%, #13131a)` | `#f87171` | `#fee2e2` | `#991b1b` |
| warning | `color-mix(in srgb, #f59e0b 10%, #13131a)` | `#fbbf24` | `#fef3c7` | `#92400e` |
| info | `color-mix(in srgb, #3b82f6 10%, #13131a)` | `#60a5fa` | `#dbeafe` | `#1e40af` |
| default | `color-mix(in srgb, #64748b 10%, #13131a)` | `#94a3b8` | `#f1f5f9` | `#475569` |
| primary | `color-mix(in srgb, #6366f1 10%, #13131a)` | `#818cf8` | `#eef2ff` | `#4338ca` |

`@theme` aliases: `--color-badge-{variant}-bg` and `--color-badge-{variant}-text` for each variant.

**Shadow tokens** — add to `:root` and `.light`:
- `:root` (dark): `--shadow-card: none`, `--shadow-card-hover: none`, `--shadow-modal: 0 20px 60px rgba(0,0,0,0.4)`
- `.light`: `--shadow-card`, `--shadow-card-hover`, `--shadow-modal` keep current values.

**Scrollbar styles** — add after the theme blocks:

```css
:root:not(.light) {
  scrollbar-width: thin;
  scrollbar-color: var(--scrollbar-thumb) transparent;
}
:root:not(.light) ::-webkit-scrollbar { width: 6px; }
:root:not(.light) ::-webkit-scrollbar-track { background: transparent; }
:root:not(.light) ::-webkit-scrollbar-thumb { background: var(--scrollbar-thumb); border-radius: 3px; }
```

**Accept:**
- `@theme` block contains only aliases to raw vars (plus the theme-independent primary palette and saturated column colors)
- `:root` sets dark values; `.light` overrides for light values
- Column `-light`, badge, shadow, scrollbar tokens all resolve correctly per theme class
- Existing `body { background-color: var(--color-background); }` continues to work
- `npm run build` passes (from `web/reflekt-web/`)

### Step 2: Create the `useTheme` composable

**Skill:** Follow `vue-patterns`

**File:** `web/reflekt-web/src/composables/useTheme.ts`

- localStorage key: `reflekt:theme`
- Default (no saved preference): `dark`
- Exports: `theme` (ref), `isDark` (computed), `toggleTheme` (function)
- `toggleTheme` sets `document.documentElement.className` to the new theme value (or empty string for dark, `light` for light — matching the CSS `:root` / `.light` selectors) and persists to localStorage
- Module-level refs so all consumers share state

**Accept:**
- Toggling updates the `<html>` element's class and localStorage
- Multiple components importing `useTheme` see the same reactive state
- `npm run build` passes

### Step 3: Add flash-prevention script to `index.html`

**Skill:** None

**File:** `web/reflekt-web/index.html`

Add a blocking inline `<script>` in `<head>` (after `<meta>` tags, before `<title>`) that:
1. Reads `localStorage.getItem('reflekt:theme')`
2. If value is `'light'`, adds class `light` to `document.documentElement`
3. If value is `null` or `'dark'`, does nothing (`:root` defaults to dark)

Also add `style="background-color: #0a0a0f;"` on the `<body>` tag to prevent white flash before CSS loads.

**Accept:**
- Page renders with correct theme class on `<html>` before Vue mounts
- No visible flash of wrong theme on refresh in either theme
- Script is minimal (no framework dependencies, no async)

### Step 4: Add theme toggle to AppHeader

**Skill:** Follow `vue-patterns`

**Files:**
- `web/reflekt-web/src/components/AppHeader.vue`

Add a theme toggle button in the header's right-side area, before the user avatar button. The button:
- Imports `useTheme` composable and `Sun`, `Moon` icons from `lucide-vue-next`
- Shows `Sun` icon when `isDark` is true (action: switch to light)
- Shows `Moon` icon when `isDark` is false (action: switch to dark)
- Calls `toggleTheme()` on click
- Styling: small icon button, uses token classes for text/hover colors
- Visible on all pages where AppHeader appears (which is all authenticated pages)
- The toggle is visible even when the user is not signed in (guest/participant pages) — place it outside the `v-if="authStore.isSignedIn"` block

**Accept:**
- Toggle button visible in header on all pages
- Clicking switches theme instantly without reload
- Icon reflects current theme (sun in dark, moon in light)

### Step 5: Migrate base UI components to theme tokens

**Skill:** Follow `tailwind-theme`

Replace hardcoded Tailwind color classes with theme token classes. Pattern: `bg-white` -> `bg-surface`, `border-gray-200` -> `border-border`, `text-gray-900` -> `text-text-primary`, `text-gray-700` -> `text-text-secondary`, `text-gray-500`/`text-gray-400` -> `text-text-muted`, `bg-gray-50` -> `bg-surface-raised`, `hover:bg-gray-50` -> `hover:bg-surface-raised`.

**Files and specific changes:**

1. **`web/reflekt-web/src/components/ui/BaseCard.vue`**
   - `bg-surface` stays; `border-gray-200` -> `border-border` (3 occurrences: card, header divider, footer divider)
   - Card hover: add `hover:-translate-y-0.5` when `hoverable`

2. **`web/reflekt-web/src/components/ui/BaseButton.vue`**
   - `secondary` variant: `border-gray-300 bg-white text-gray-700 hover:bg-gray-50` -> `border-border bg-surface text-text-secondary hover:bg-surface-raised`
   - `ghost` variant: `text-gray-700 hover:bg-gray-100` -> `text-text-secondary hover:bg-surface-raised`
   - `focus-visible:ring-offset-2` -> `focus-visible:ring-offset-2 ring-offset-focus-offset` (use the focus offset token)

3. **`web/reflekt-web/src/components/ui/BaseInput.vue`**
   - Normal state: `border-gray-300 text-gray-900 bg-white` -> `border-border text-text-primary bg-surface`
   - Disabled: `bg-gray-50 text-gray-400` -> `bg-surface-raised text-text-muted`
   - Error: `text-gray-900` -> `text-text-primary`
   - Focus ring offset: add `ring-offset-focus-offset`

4. **`web/reflekt-web/src/components/ui/BaseBadge.vue`**
   - Replace all 6 variant classes with badge token classes:
     - `default`: `bg-badge-default-bg text-badge-default-text`
     - `success`: `bg-badge-success-bg text-badge-success-text`
     - `danger`: `bg-badge-danger-bg text-badge-danger-text`
     - `warning`: `bg-badge-warning-bg text-badge-warning-text`
     - `info`: `bg-badge-info-bg text-badge-info-text`
     - `primary`: `bg-badge-primary-bg text-badge-primary-text`

5. **`web/reflekt-web/src/components/ui/BaseModal.vue`**
   - `border-gray-200` -> `border-border` (3 occurrences)
   - `text-gray-900` -> `text-text-primary`
   - `text-gray-400 hover:text-gray-600` -> `text-text-muted hover:text-text-secondary`

6. **`web/reflekt-web/src/components/ui/EmptyState.vue`**
   - `text-gray-300` (icon) -> `text-text-muted`
   - `text-gray-500` -> `text-text-secondary`
   - `text-gray-400` -> `text-text-muted`

7. **`web/reflekt-web/src/components/ui/LoadingSpinner.vue`**
   - `text-gray-500` -> `text-text-muted`

8. **`web/reflekt-web/src/components/ui/ToastContainer.vue`**
   - `border-gray-200` -> `border-border`
   - `text-gray-800` -> `text-text-primary`
   - `text-gray-400 hover:text-gray-600` -> `text-text-muted hover:text-text-secondary`

**Accept:**
- All 8 base UI components use only token classes (no `gray-*`, no `white` as bg)
- Both themes render correctly for these components
- `npm run build` passes

### Step 6: Migrate layout and team components

**Skill:** Follow `tailwind-theme`

Same token replacement pattern as Step 5.

**Files:**

1. **`web/reflekt-web/src/components/layout/PageLayout.vue`**
   - `text-gray-500` -> `text-text-muted`
   - `text-gray-400` -> `text-text-muted`

2. **`web/reflekt-web/src/components/AppHeader.vue`** (continued from Step 4)
   - `bg-white border-b border-gray-200` -> `bg-surface border-b border-border`
   - `text-gray-300` (breadcrumb separator) -> `text-text-muted`
   - `text-gray-500 hover:text-gray-700` -> `text-text-muted hover:text-text-secondary`
   - `text-gray-700` -> `text-text-secondary`
   - Dropdown: `border-gray-200` -> `border-border`, `border-gray-100` -> `border-border-subtle`, `text-gray-500` -> `text-text-muted`, `text-gray-700 hover:bg-gray-50` -> `text-text-secondary hover:bg-surface-raised`
   - Avatar: `bg-primary-100 text-primary-700 hover:bg-primary-200` — keep as-is (accent, theme-independent)

3. **`web/reflekt-web/src/components/teams/TeamCard.vue`**
   - `bg-white border border-gray-200` -> `bg-surface border border-border`
   - `hover:bg-gray-100` -> `hover:bg-surface-raised`
   - `text-gray-800` -> `text-text-primary`
   - `text-gray-400/500` -> `text-text-muted`
   - Menu dropdown: `bg-white border-gray-200` -> `bg-surface-raised border-border`
   - `text-red-600 hover:bg-red-50` — keep (semantic danger, already works)
   - Tooltip: `bg-gray-800 text-white` -> `bg-surface-raised text-text-primary border border-border`

4. **`web/reflekt-web/src/components/teams/CreateTeamForm.vue`**
   - All `border-gray-*`, `text-gray-*`, `bg-white/bg-gray-*` -> token equivalents
   - Follow same mapping as Step 5

5. **`web/reflekt-web/src/components/teams/DeleteTeamDialog.vue`**
   - `bg-white` -> `bg-surface`
   - `text-gray-800` -> `text-text-primary`
   - `text-gray-600` -> `text-text-secondary`
   - `text-gray-700` -> `text-text-secondary`
   - `border-gray-300` -> `border-border`
   - `bg-gray-100 text-gray-700 hover:bg-gray-200` -> `bg-surface-raised text-text-secondary hover:bg-border`
   - `text-gray-600 hover:bg-gray-100` -> `text-text-secondary hover:bg-surface-raised`
   - Tooltip: same pattern as TeamCard

6. **`web/reflekt-web/src/components/teams/TeamSelector.vue`**
   - All gray references -> tokens

7. **`web/reflekt-web/src/components/layout/PageToolbar.vue`**
   - No hardcoded colors (already clean) — verify and skip if clean

**Accept:**
- All layout and team components use token classes
- TeamCard dropdown, DeleteTeamDialog, tooltips render correctly in dark mode
- `npm run build` passes

### Step 7: Migrate sprint components

**Skill:** Follow `tailwind-theme`

**Files:**

1. **`web/reflekt-web/src/components/sprints/SprintList.vue`**
   - All gray references -> tokens
   - Status badge classes -> badge tokens (if not using BaseBadge)

2. **`web/reflekt-web/src/components/sprints\CreateSprintForm.vue`**
   - `border-gray-100` (top border) -> `border-border-subtle`
   - `text-gray-700` -> `text-text-secondary`
   - All input `border-gray-300` -> `border-border`
   - `text-gray-500 uppercase` labels -> `text-text-muted`
   - `text-gray-600` timer labels -> `text-text-secondary`
   - `focus:ring-blue-500` -> `focus:ring-primary-500` (normalize)
   - `bg-blue-600 text-white hover:bg-blue-700` submit button -> `bg-primary-600 text-white hover:bg-primary-700`
   - `text-red-600` error -> `text-danger`
   - Color swatch buttons: `bg-col-${color}` stays (spec: always full saturation). `border-gray-800` selected indicator -> `border-text-primary` (visible in both themes)

3. **`web/reflekt-web/src/components/sprints/SprintDetailPanel.vue`**
   - `bg-white` -> `bg-surface`
   - `text-gray-800` -> `text-text-primary`
   - `text-gray-600` -> `text-text-secondary`
   - State badge: inline `bg-gray-100 text-gray-700` / `bg-green-100 text-green-700` / `bg-red-100 text-red-700` -> use badge tokens (`badge-default`, `badge-success`, `badge-danger`)
   - `bg-blue-600` / `bg-green-600` / `bg-indigo-600` action buttons -> `bg-primary-600` (normalize to accent) or keep semantic where appropriate
   - `bg-gray-200 text-gray-800 hover:bg-gray-300` copy button -> `bg-surface-raised text-text-primary hover:bg-border`
   - Participants: `bg-gray-50` -> `bg-surface-raised`, `text-gray-800` -> `text-text-primary`
   - `text-red-600` error/kick -> keep (semantic danger)

**Accept:**
- Sprint list, creation form, and detail panel all render correctly in both themes
- Color swatch pickers remain full-saturation in both themes
- State badges use theme-aware colors
- `npm run build` passes

### Step 8: Migrate retro components

**Skill:** Follow `tailwind-theme`

**Files (13 components):**

1. **`web/reflekt-web/src/components/retro/RetroColumnPanel.vue`**
   - `bg-surface border border-gray-200` -> `bg-surface border border-border`
   - Inline edit input: `bg-white border border-gray-300` -> `bg-surface border border-border`
   - Color swatch selected: `border-gray-800` -> `border-text-primary`
   - Drag-over highlight: `bg-primary-50/30` -> keep (accent, works in both)

2. **`web/reflekt-web/src/components/retro/RetroCardItem.vue`**
   - `cardBgClass`: `bg-white` fallback -> `bg-surface`
   - `text-gray-800` -> `text-text-primary` (blurred text, display text)
   - Edit textarea/select: `border-gray-300` -> `border-border`
   - Evidence links: `text-gray-400` -> `text-text-muted`, `text-gray-600` -> `text-text-secondary`
   - `text-gray-300` (action icons) -> `text-text-muted`
   - Link input: `border-gray-300` -> `border-border`
   - `text-gray-400 hover:text-gray-600` -> `text-text-muted hover:text-text-secondary`

3. **`web/reflekt-web/src/components/retro/RetroGroupItem.vue`**
   - `text-gray-300` (grip icon) -> `text-text-muted`
   - `text-gray-800` -> `text-text-primary`
   - `text-gray-500` -> `text-text-muted`
   - `border-gray-200` (expanded child border) -> `border-border`
   - `text-gray-600` -> `text-text-secondary`
   - Vote button disabled: `border-gray-200 text-gray-300` -> `border-border text-text-muted`
   - Vote button normal: `border-gray-300 text-gray-500` -> `border-border text-text-muted`
   - `text-gray-500 hover:text-gray-700` (Rename/Dissolve) -> `text-text-muted hover:text-text-secondary`
   - Group title input: `border-gray-300` -> `border-border`
   - Focused group: `bg-primary-50 ring-2 ring-primary-500` — keep (accent)

4. **`web/reflekt-web/src/components/retro/RetroGroupDiscussion.vue`**
   - All gray references -> tokens

5. **`web/reflekt-web/src/components/retro/RetroBoardPanel.vue`**
   - All gray references -> tokens
   - Card input areas: `border-gray-300` -> `border-border`, `bg-white` -> `bg-surface`

6. **`web/reflekt-web/src/components/retro/RetroPhaseHeader.vue`**
   - All gray references -> tokens
   - Closed-sprint warning banner: `bg-amber-*` / `text-amber-*` -> use badge warning tokens or keep semantic amber (these are explicit UI elements, not badge components)

7. **`web/reflekt-web/src/components/retro/RetroJoinForm.vue`**
   - All gray references -> tokens

8. **`web/reflekt-web/src/components/retro/RetroParticipantList.vue`**
   - All gray references -> tokens

9. **`web/reflekt-web/src/components/retro/RetroMobileTabBar.vue`**
   - All gray references -> tokens

10. **`web/reflekt-web/src/components/retro/RetroReportPanel.vue`**
    - All gray references -> tokens

11. **`web/reflekt-web/src/components/retro/TimerControls.vue`**
    - All gray references -> tokens

12. **`web/reflekt-web/src/components/retro/TimerDisplay.vue`**
    - All gray references -> tokens

13. **`web/reflekt-web/src/components/retro/PreviousActionsPanel.vue`**
    - `bg-white border border-amber-200` -> `bg-surface border border-border`
    - Header: `bg-amber-50` -> `bg-badge-warning-bg`, `text-amber-800` -> `text-badge-warning-text`
    - Collapse text: `text-amber-600` -> `text-badge-warning-text`
    - `text-gray-600` -> `text-text-secondary`
    - `text-gray-800` -> `text-text-primary`
    - `text-gray-500` -> `text-text-muted`
    - `divide-gray-100` -> `divide-border-subtle`
    - Status button inactive: `bg-white border-gray-300 text-gray-600 hover:bg-gray-50` -> `bg-surface border-border text-text-secondary hover:bg-surface-raised`
    - Status button active Done: `bg-emerald-600` -> keep (semantic, high-contrast)
    - Status button active CarriedOver: `bg-amber-500` -> keep
    - Status button active Dropped: `bg-gray-500` -> `bg-text-muted` (grey button needs contrast in both themes)
    - Status badge read-only: `bg-emerald-100 text-emerald-700` -> `bg-badge-success-bg text-badge-success-text`, etc.
    - Pending badge: `bg-gray-100 text-gray-500` -> `bg-badge-default-bg text-badge-default-text`

14. **`web/reflekt-web/src/components/PhaseIndicator.vue`**
    - Connector line inactive: `bg-gray-200` -> `bg-border`
    - Phase label inactive: `text-gray-400` -> `text-text-muted`
    - Phase label active: `text-primary-700` -> keep (accent)
    - Inactive step circle: `bg-white text-gray-400 border-2 border-gray-200` -> `bg-surface text-text-muted border-2 border-border`
    - Active step ring: `ring-primary-100` -> keep (accent)

15. **`web/reflekt-web/src/components/reports/ReportSection.vue`**
    - All gray references -> tokens

16. **`web/reflekt-web/src/components/reports/SprintReportPanel.vue`**
    - All gray references -> tokens
    - Semantic badge colors -> badge tokens

**Accept:**
- All retro, report, and phase indicator components use token classes
- Column backgrounds show subtle tinted wash in dark mode via `-light` token
- Retro cards, groups, phase stepper, timer, participant list render correctly in dark mode
- Previous actions panel renders correctly in both themes
- `npm run build` passes

### Step 9: Migrate views

**Skill:** Follow `tailwind-theme`

**Files:**

1. **`web/reflekt-web/src/views/WelcomeView.vue`**
   - `text-gray-800` -> `text-text-primary`
   - `bg-blue-600 text-white hover:bg-blue-700` -> `bg-primary-600 text-white hover:bg-primary-700`

2. **`web/reflekt-web/src/views/TeamDashboardView.vue`**
   - All gray references -> tokens
   - Any semantic colors -> badge/status tokens

3. **`web/reflekt-web/src/views/SprintListView.vue`**
   - All gray references -> tokens

4. **`web/reflekt-web/src/views/SprintView.vue`**
   - All gray references -> tokens

5. **`web/reflekt-web/src/views/RetroRoomView.vue`**
   - All gray references -> tokens
   - Closed-sprint banner amber colors -> badge warning tokens

6. **`web/reflekt-web/src/views/ReportView.vue`**
   - All gray references -> tokens

**Accept:**
- All 6 views render correctly in both themes
- Welcome/login page renders correctly in dark mode
- `npm run build` passes

### Step 10: Build verification and final check

**Skill:** None

**Files:** None (verification only)

1. Run `npm run build` from `web/reflekt-web/` — must pass clean
2. Grep the entire `web/reflekt-web/src/` directory for remaining hardcoded color classes that should have been migrated:
   - `bg-white` (should be zero outside of color-swatch or semantic contexts)
   - `bg-gray-` (should be zero)
   - `text-gray-` (should be zero)
   - `border-gray-` (should be zero)
   - Any remaining `bg-emerald-50`, `bg-red-50`, `bg-amber-50`, `bg-blue-50` in badge contexts (should use badge tokens)
3. Verify the `@source inline(...)` directive in `main.css` includes any new dynamic token classes that might be used via template interpolation (e.g., `bg-badge-success-bg`, `bg-badge-danger-bg`, etc., if referenced via dynamic class binding)

**Accept:**
- Zero hardcoded gray/white references remain (except intentional: color swatches `bg-col-{name}` which are theme-independent, and a few semantic active-state buttons like `bg-emerald-600` for explicit high-contrast states)
- Build passes
- No regressions in `@source inline` — all dynamic classes are covered

## Cross-Service Changes

None.

## Migration Notes

None (no database changes).

## Testing Strategy

- **Manual visual verification** in both themes for each major view: Welcome, Team Dashboard, Sprint List, Sprint Detail, Retro Room (all phases), Report
- **Theme toggle:** verify instant switch, no flash on reload, localStorage persistence
- **Column colors:** verify all 8 palette colors + action items show correct tint in dark mode
- **Badges:** verify all 6 semantic variants readable in both themes
- **Scrollbars:** verify dark-mode custom scrollbar in retro column overflow
- **Focus rings:** tab through interactive elements in dark mode, verify no white gap
- **Color swatches:** verify full saturation in both themes in sprint creation form and column header edit

## KB Impact

None (purely frontend theming, no business rules or domain logic changes).

## Open Questions

None.
