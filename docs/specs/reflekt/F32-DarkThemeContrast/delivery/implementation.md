# Dark Theme Contrast & Visual Hierarchy — Implementation

## Files Modified

- `web/reflekt-web/src/assets/main.css` — Changed `--text-muted` in `:root` (dark) from `#60607a` to `#8585a0`. Only this one value changed; `.light` block and all other tokens are untouched. No new CSS custom properties introduced.
- `web/reflekt-web/src/components/retro/RetroCardItem.vue` — Added `border border-border` to the root `<div>` card wrapper (alongside existing `shadow-card`). Uses the existing border token (#2a2a38 dark, #e2e2ea light).
- `web/reflekt-web/src/components/retro/RetroBoardPanel.vue` — Imported `useTheme` from `../../composables/useTheme`. Destructured `isDark`. Changed "I'm done" button not-ready class binding: dark mode gets `bg-emerald-600/80 border-transparent text-white hover:bg-emerald-700`; light mode keeps existing ghost style. Ready/done state unchanged.
- `web/reflekt-web/src/components/PhaseIndicator.vue` — Replaced `text-primary-700` with `text-badge-primary-text` on active/completed phase label spans. The token resolves to `#818cf8` (dark) / `#4338ca` (light) — identical light-mode appearance, high-contrast dark-mode appearance.

## Key Decisions

- Pre-existing TypeScript errors in `TeamSelector.vue` and `previousActions.ts` are unrelated to this feature and present before any changes (verified via `git stash`). The Vite production build (`npm run build-only`) passes clean.
- `border-transparent` added to the not-ready dark button state to cancel the `border` class from the base style list, giving a clean filled appearance without the ghost outline.

## Deviations from Plan

- None. All four implementation steps executed exactly as specified.

## Contrast Audit (Step 5)

| Check | Value | Result |
|-------|-------|--------|
| `#8585a0` on `#13131a` (card surface) | 4.65:1 | Pass >= 4.5:1 |
| `#8585a0` on `#0a0a0f` (page bg) | 5.05:1 | Pass >= 4.5:1 |
| `#818cf8` on `#13131a` (phase label) | 5.90:1 | Pass >= 4.5:1 |
| Light `.light` block `--text-muted` | `#9b9bab` (unchanged) | Pass |
| New CSS custom properties in main.css | 0 | Pass |
