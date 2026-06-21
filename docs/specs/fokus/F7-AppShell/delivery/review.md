# App Shell & Navigation — Review

## Reviewed By
`reviewer` (Sonnet agent, claude-sonnet-4-6). No Codex cross-validation requested.

## Verdict: COMMENT

No CRITICAL or HIGH issues found. Two MEDIUM findings (one plan deviation, one accessibility gap) and one LOW finding.

---

## Pre-commitment Predictions

| Prediction | Actual |
|---|---|
| Theme persistence: module-level ref could cause sync issues between `main.ts` and `AppHeader` | Not an issue — `isDark` is correctly declared at module level, all call sites share the same ref |
| Responsive listener: `resize` handler may not clean up on `onUnmounted` | Not an issue — cleanup is present at `AppSidebar.vue:17` |
| Static toolbar: might have accidental interactivity or missing aria | Confirmed static (no handlers), minor aria gap (LOW) |
| CSS tokens: `:root` / `.dark` co-definition may not correctly cover light theme path | Not an issue — `.light` specificity (class) beats `:root` (element) as expected |
| Active route: manual `useRoute()` comparison may have edge cases | Logic is correct — Dashboard uses `exact: true` to prevent `/` matching all routes |

All five predictions checked. Four were unfounded; one (toolbar aria) partially confirmed at LOW severity.

---

## Findings

### [MEDIUM] PageLayout uses `title` prop instead of `title` slot

**File:** `client/src/components/PageLayout.vue:1-24`
**Issue:** The plan explicitly specifies "slot-based layout: `title` slot, `toolbar` slot (optional), default slot for content area" (Step 5). The implementation uses a `title` string prop (`defineProps<{ title: string }>()`) rendered as `{{ title }}`, not a named slot. The other two slots (`toolbar`, default) are correctly implemented as named/default slots.
**Impact:** Minor — a prop works equally well for the current use case where all callers pass a plain string. However it deviates from the plan contract and restricts future callers from passing rich markup (e.g., a title with a badge) into the title area without a component change.
**Fix:** Either (a) change `title` to a named slot and update the four view callers to use `<template #title>`, or (b) accept the prop as a deliberate simplification and note it as a deviation in implementation.md. If keeping the prop, architect should acknowledge the plan deviation.

---

### [MEDIUM] Active sidebar link missing `aria-current="page"`

**File:** `client/src/components/AppSidebar.vue:92-105`
**Issue:** The active navigation item is visually distinguished via CSS class (`bg-sidebar-item-active text-accent-default`) but has no `aria-current="page"` attribute on the active `RouterLink`. Screen readers and assistive technologies rely on `aria-current` to identify the current page in a navigation list — visual-only active state is invisible to them.
**Fix:** Bind `:aria-current="isActive(item) ? 'page' : undefined"` on the `RouterLink` element (line 92). `undefined` causes Vue to omit the attribute entirely for inactive items, which is the correct behavior.

---

### [LOW] Theme toggle button relies on `title` attribute instead of `aria-label`

**File:** `client/src/components/AppHeader.vue:10-23`
**Issue:** The theme toggle `<button>` uses `:title="isDark ? 'Switch to light theme' : 'Switch to dark theme'"` for its accessible name. The `title` attribute is not reliably announced by all screen readers (particularly on mobile or when keyboard-focused). `aria-label` is the correct attribute for providing an accessible name on an icon-only button.
**Fix:** Replace `:title` with `:aria-label` (or add both). The current `title` value is correct in content — just needs to be expressed via `aria-label`.

---

## Positive Observations

- **Theme system architecture is clean.** Module-level `isDark` ref in `useTheme.ts` correctly solves the shared-state problem across multiple call sites. The two-layer CSS pattern (`@theme` aliases + `:root`/`.dark`/`.light` value blocks) is the right approach for Tailwind v4 theme switching.
- **FOUC prevention is thorough.** Three layers work together: `class="dark"` on `<html>` in `index.html`, inline `style` on `<body>` with literal hex values, and `initTheme()` called before `app.mount()`. This is the correct defense-in-depth approach.
- **Responsive sidebar cleanup is correct.** `window.removeEventListener('resize', checkWidth)` in `onUnmounted` uses the same function reference — this will actually remove the listener (a common bug is using an inline arrow function which creates a new reference and fails to remove).
- **Dashboard exact-match guard is correct.** `exact: true` on the Dashboard route prevents `/` from matching every path via `startsWith`. The other four routes use `exact: false` which is safe because their paths are unique prefixes.
- **Toolbar is genuinely non-interactive.** Both dropdown placeholders are `<div>` elements with `cursor-default` and no event handlers — they cannot be accidentally activated. The visual appearance (border, chevron icon) is appropriate for a placeholder.
- **Static toolbar uses real design tokens throughout.** `bg-surface-card`, `border-border-default`, `text-text-secondary`, `text-text-muted` — no hardcoded colors leaked in.
- **`BaseCard` and `EmptyState` are minimal and reusable.** No over-engineering, token-based styling throughout.
- **Build output is clean.** Zero TypeScript errors, zero type-check warnings. The chunk size warning (ApexCharts 508 kB) is pre-existing and unrelated to this feature.
- **Settings regression is avoided.** Outer page wrapper removed correctly; shell's `p-6` on `<main>` provides equivalent spacing. Inner form content preserved intact.

---

## Gaps

- **No keyboard navigation test for collapsed sidebar.** When the sidebar is icon-only (collapsed), nav items show only `title` tooltip text. Keyboard users navigating by Tab will land on each link with no visible label. The `title` attribute provides tooltip context on hover but not on focus. This is acceptable for the stated minimum viewport of 1366px (where the sidebar stays expanded), but worth noting for future accessibility work.
- **`v-html` for icon rendering.** `AppSidebar.vue:103` uses `v-html="item.icon"` to render inline SVG strings from a static constant array. This is safe (developer-controlled, not user input) but is a pattern that becomes a risk if icon sources ever become dynamic. Consider migrating to slot-based or component-based icons in a follow-up.
- **Light theme flash on cold load.** The `<html class="dark">` default in `index.html` and the inline body style prevent the dark→light flash direction, but a user whose saved preference is `light` will briefly see the dark background before `initTheme()` runs. This is a known hard problem with CSS-class-based themes in SSR-less SPAs and is acceptable at this stage — worth documenting for future improvement.

---

## Open Questions

None. All CRITICAL/HIGH self-audit items were downgraded or confirmed absent.

---

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Build | PASS | `npm run build` (from `client/`) | 65 modules transformed, 0 errors, 0 type errors. Output to `wwwroot/`. Chunk size warning for ApexCharts is pre-existing. |
| TypeScript | PASS | `vue-tsc -b` (part of build) | No type errors reported |
| Files match plan | PASS | Manual cross-reference | All 8 plan steps have corresponding implementation entries |
| Token categories | PASS | Read `main.css` | All 6 token categories defined: surface, border, text, accent, status, sidebar |
| Toolbar non-interactive | PASS | Read `PageToolbar.vue` | No event handlers, `cursor-default`, pure `<div>` elements |
| Listener cleanup | PASS | Read `AppSidebar.vue:17` | `onUnmounted` removes same `checkWidth` reference |
| Settings regression | PASS | Read `SettingsView.vue:89` | Outer wrapper removed, inner `max-w-3xl` content intact |
