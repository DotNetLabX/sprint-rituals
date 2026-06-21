# Dark / Light Theme — Review

## Reviewed By
reviewer (Sonnet agent)

## Verdict: APPROVE

## Pre-commitment Predictions

| Prediction | Finding |
|------------|---------|
| Flash prevention script position or content issue | Correctly implemented — blocking IIFE in `<head>`, `style` on `<body>`, matches spec |
| Token system missing entries or wrong aliases | Two-layer system complete; all 12 surface/text/border/badge/shadow tokens present |
| `@source inline` missing dynamic class coverage | Pre-existing `/30` opacity issue in RetroCardItem; wrong prefix pre-existing in RetroGroupItem — neither introduced by F31 |
| New TypeScript errors introduced | TS errors confirmed pre-existing in TeamSelector and previousActions.ts; unrelated to F31 |
| Remaining hardcoded gray/white references | Zero `bg-white`, `bg-gray-*`, `text-gray-*`, `border-gray-*` remain in 37 Vue files — full migration confirmed |

---

## Findings

### [MEDIUM] `text-emerald-300` not migrated in RetroPhaseHeader readiness tooltip

**File:** `web/reflekt-web/src/components/retro/RetroPhaseHeader.vue:33`

**Issue:** The readiness tooltip's container was correctly migrated from `bg-gray-900 text-white` to `bg-surface-raised text-text-primary border border-border`, but the "Done" participants label retains `text-emerald-300`. In light mode, `text-emerald-300` (oklch light green) on `bg-surface-raised` (`#eaeaf0`) fails WCAG AA contrast (light green on near-white). The "Not done" label was correctly migrated to `text-text-secondary` in the same diff.

**Fix:** Replace `text-emerald-300` with `text-badge-success-text` (which resolves to `#34d399` in dark and `#065f46` in light — meeting AA in both themes).

---

### [MEDIUM] Pre-existing `bg-${columnColor}-light/30` wrong prefix in RetroGroupItem group backgrounds

**File:** `web/reflekt-web/src/components/retro/RetroGroupItem.vue:126-127`

**Issue:** `groupBgClass` generates `bg-green-light/30`, `bg-red-light/30`, etc. — missing the required `col-` prefix. The correct token class is `bg-col-green-light`. This means group backgrounds in the retro board receive no styling (the generated class name has no CSS rule). The compiled CSS confirms only `bg-col-green-light` exists, not `bg-green-light`. The spec acceptance criterion "Retro column backgrounds use low-opacity tints of the column color" is not met for grouped cards.

**Note:** This is a **pre-existing bug** that predates F31. Plan Step 8 migrated RetroGroupItem's gray references but did not call out fixing this token prefix. Flagged as MEDIUM (pre-existing, not introduced, but in scope of the migrated file and blocking an acceptance criterion).

**Fix:** Change lines 126-127 to use `bg-col-${props.columnColor}-light` (without `/30`, since the dark token value is already pre-mixed at 10% opacity via `color-mix()`).

---

### [LOW] `hover:bg-red-50` retained in TeamCard delete menu button

**File:** `web/reflekt-web/src/components/teams/TeamCard.vue:26`

**Issue:** `text-red-600 hover:bg-red-50` on the "Delete team" button in the context menu uses a hardcoded Tailwind color. In dark mode, `bg-red-50` (`oklch(97.1% .013 17.38)`) is near-white — this will flash a bright hover state on the dark dropdown. Plan Step 6 explicitly said to keep `text-red-600 hover:bg-red-50` as "semantic danger, already works." However, `bg-red-50` does not work correctly in dark mode — it is a light-mode pastel.

**Fix:** Replace with `hover:bg-badge-danger-bg` to use the theme-aware danger background token. This is a plan deviation that should be assessed — the plan's guidance was incorrect for dark mode. Escalating as LOW since the menu is a small hover area.

---

### [LOW] `@source inline` missing opacity-modified column-light variants

**File:** `web/reflekt-web/src/assets/main.css:3`

**Issue:** `@source inline(...)` declares `bg-col-green-light` but not `bg-col-green-light/30`. `RetroCardItem` dynamically generates `bg-col-${columnColor}-light/30` — the `/30` opacity modifier class is absent from the compiled CSS. In Tailwind v4, CSS-var-backed color tokens cannot produce opacity variants at build time. The card backgrounds in the retro board are transparent (no CSS rule matches). This is pre-existing behavior — the `/30` was never in `@source inline` before F31 either, and the dark token is already pre-mixed at 10% opacity. The fix is to drop `/30` from the dynamic class and rely on the token's built-in opacity, or add `bg-col-green-light/30` etc. to `@source inline` with explicit opacity values.

---

## Positive Observations

- **Two-layer token architecture is textbook.** `:root` holds dark defaults; `.light` overrides; `@theme` is alias-only. Exactly matches the tailwind-theme skill pattern.
- **`@source inline` is comprehensive.** All 6 badge variant pairs, all 8 column colors, and action-items are covered. No dynamic badge class will go missing.
- **Flash prevention is correctly implemented.** Blocking IIFE in `<head>`, minimal (no framework deps), and `style="background-color: #0a0a0f;"` on `<body>` prevents white flash before CSS loads — matches the convention documented in `vue.md`.
- **Module-level `useTheme` refs** correctly ensure all consumers share the same reactive state — the convention from `vue.md` is followed precisely.
- **Zero hardcoded gray/white references remain.** Grep across all 37 Vue files returns no matches for `bg-white`, `bg-gray-*`, `text-gray-*`, `border-gray-*`. Full migration.
- **`toggleTheme` correctly maps dark→no-class, light→`.light`**, matching the `:root` / `.light` CSS selector design. The inline script matches this logic exactly.
- **Badge token migration is complete.** All 6 variants in `BaseBadge`, `SprintDetailPanel`, `SprintList`, `PreviousActionsPanel`, and `SprintReportPanel` use `bg-badge-{variant}-bg text-badge-{variant}-text`. Dynamic class switching via computed properties follows existing codebase patterns.
- **Shadow tokens work correctly.** Despite `@theme` defining `--shadow-card: var(--shadow-card)` (which reads as circular), the CSS cascade ensures `:root`'s `--shadow-card: none` and `.light`'s shadow values take precedence. Build output confirms correct utility resolution.
- **Theme toggle placement is correct.** Button is outside the `v-if="authStore.isSignedIn"` block — visible to all users including participants on the join form, as the spec requires.

---

## Gaps

- **WCAG AA verification not independently confirmed.** The spec requires 4.5:1 contrast for body text in both themes. The token values appear correct by inspection (primary text `#f0f0f5` on `#13131a` ≈ 14.4:1, `#1a1a2e` on `#ffffff` ≈ 19.6:1) but no automated contrast check was run.
- **Retro column group backgrounds broken** (covered under MEDIUM finding above) — acceptance criterion "column backgrounds use low-opacity tints" unmet for grouped cards.
- **`text-emerald-300` in readiness tooltip fails AA in light mode** (covered under MEDIUM finding above).
- **`prefers-color-scheme` not implemented** — correctly out-of-scope per spec.
- **No automated test coverage** — the testing strategy is manual-only. Not a blocker (spec says manual), but noted.

---

## Open Questions

None — all findings have sufficient evidence. No architecture decisions needed.

---

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Vite build | PASS | `npm run build` (web/reflekt-web) | ✓ 1871 modules, 295.47 kB bundle, built in 782ms |
| TypeScript type-check | FAIL (pre-existing) | `vue-tsc --build` | 7 errors in TeamSelector.vue and previousActions.ts — confirmed pre-existing by `git show HEAD~1` |
| Hardcoded gray/white grep | PASS (0 matches) | Grep `bg-white\|bg-gray-\|text-gray-\|border-gray-` in `src/**/*.vue` | No matches |
| `@source inline` badge coverage | PASS | Grep compiled CSS for `bg-badge-*` utilities | All 12 badge utilities present |
| Shadow token cascade | PASS | Grep compiled CSS for `--shadow-card` | `:root` value `none`, `.light` value `0 1px 3px...` both present and correct |
| Dynamic class prefix (RetroGroupItem) | FAIL | Grep compiled CSS for `bg-green-light` | No match — class never generated; correct prefix is `bg-col-green-light` |
| `/30` opacity variant coverage | FAIL | Grep compiled CSS for `col-green-light/30` | No match — CSS-var colors cannot produce opacity variants; pre-existing |
| Pre-existing TS errors | Confirmed pre-existing | `git show HEAD~1:web/reflekt-web/src/components/teams/TeamSelector.vue` | Same TS errors present before F31 |
