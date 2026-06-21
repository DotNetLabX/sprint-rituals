# Dark Theme Contrast & Visual Hierarchy

**Feature Spec:** `docs/specs/F32-DarkThemeContrast/definition/spec.md`

## Context

F31 shipped a complete dark/light theme with a two-layer CSS token system. Post-launch visual testing revealed several WCAG AA contrast failures and visual hierarchy problems in dark mode: the muted text token fails 4.5:1 against both page and card surfaces, retro cards blend into column backgrounds (no border), the "I'm done" button is styled as a ghost control that disappears on dark backgrounds, and phase stepper active labels use a dark indigo shade (#4338ca) that achieves only ~2.3:1 contrast. All fixes are dark-mode-only, frontend-only, and reuse the existing F31 token system (no new CSS custom properties).

**Service impacted:** Frontend only (`web/reflekt-web/`). No backend changes.

## Scope

**In scope:**
- Adjust `--text-muted` dark value to meet WCAG AA 4.5:1 against card surface (#13131a)
- Add visible borders to retro cards in dark mode
- Restyle "I'm done" button from ghost to filled (emerald)
- Fix phase stepper label contrast (active and inactive) in dark mode
- Verify "Add a note..." input borders are visible (BaseInput already uses `border-border`)

**Out of scope (per spec):**
- Light mode adjustments (passes WCAG AA with current tokens)
- Phase stepper redesign (layout/icon changes)
- Role-based button visibility (functional, not visual)
- New animation or transition effects
- New CSS custom properties

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | tailwind-theme | Follow | Adjust `--text-muted` dark value in `:root` block of `main.css` | |
| 2 | tailwind-theme | Follow | Add `border border-border` to `RetroCardItem.vue` card wrapper | |
| 3 | (none) | -- | Restyle "I'm done" button in `RetroBoardPanel.vue` — emerald filled states | |
| 4 | (none) | -- | Fix phase stepper label contrast in `PhaseIndicator.vue` — dark-mode-aware classes | |
| 5 | (none) | -- | Build verification + contrast spot-check | |

## Domain Model Changes

None.

## Data Model Changes

None.

## Implementation Steps

### Step 1: Raise the muted text token for WCAG AA compliance

**Skill:** Follow `tailwind-theme`

**File:** `web/reflekt-web/src/assets/main.css`

In the `:root` block (dark theme values), adjust `--text-muted` from `#60607a` to a value that achieves >= 4.5:1 contrast ratio against the card surface `#13131a`.

**Contrast math:** Card surface `#13131a` has relative luminance ~0.012. For 4.5:1: `(L_text + 0.05) / (0.012 + 0.05) >= 4.5`, so `L_text >= 0.229`. Target value: `#8585a0` (relative luminance ~0.24, ratio ~4.7:1 against #13131a). Verify the chosen value also maintains >= 4.5:1 against page background `#0a0a0f` (luminance ~0.007 -- easier target).

**Constraint:** Only the dark value changes. The `.light` block `--text-muted: #9b9bab` is unchanged.

**Accept:**
- `--text-muted` in `:root` is a new hex value achieving >= 4.5:1 against `#13131a`
- `.light` value `#9b9bab` is untouched
- No new CSS custom properties introduced
- `npm run build` passes (from `web/reflekt-web/`)

### Step 2: Add visible borders to retro cards

**Skill:** Follow `tailwind-theme`

**File:** `web/reflekt-web/src/components/retro/RetroCardItem.vue`

The card wrapper root `<div>` (search for `shadow-card p-3`) currently has `shadow-card` and a computed `cardBgClass` but no border. Add `border border-border` to the static class list on this root `<div>`. This follows F31's "border-only depth in dark mode" design principle and uses the existing border token (`#2a2a38` dark, `#e2e2ea` light).

**BR4 verification:** The `BaseInput` component already applies `border-border` (verified in Phase 1 analysis), so "Add a note..." input fields already have visible borders. No changes needed to `BaseInput.vue` or `RetroColumnPanel.vue`.

**AC2 verification:** "No cards yet" empty state text is rendered in `RetroBoardPanel.vue` (search for `No cards yet` — appears in all four column `#empty` slots) using `text-text-muted`. Step 1's token adjustment fixes this automatically.

**BR7 verification:** "Add" buttons in retro column input areas use `BaseButton variant="primary"` (`bg-primary-600 text-white`) — already a filled button with the same visual weight as the facilitator's "Next" button. No adjustment needed for BR7.

**Light mode note:** This adds a `#e2e2ea` border to cards in light mode too. The border is nearly invisible on white card surfaces and consistent with F31's card design principle. This is a cosmetically negligible side effect, not a regression — light mode cards already have `shadow-card` (real box-shadow) for depth.

**Accept:**
- Every retro card displays a visible border in dark mode, visually separating it from the column background tint
- "No cards yet" text is readable in dark mode (uses `text-text-muted`, fixed by Step 1)
- "Add a note..." inputs at the bottom of columns have visible borders (already present via BaseInput)
- "Add" buttons are already filled primary buttons — no change needed for BR7
- `npm run build` passes

### Step 3: Restyle "I'm done" button to filled emerald (dark mode)

**Skill:** None (component-specific visual change, no pattern skill applies)

**File:** `web/reflekt-web/src/components/retro/RetroBoardPanel.vue`

The "I'm done" button (search for `"I'm done"` in template) has two visual states:
- **Not ready (current):** `bg-surface border-border text-text-secondary hover:bg-surface-raised` -- ghost/outline style that blends into dark backgrounds.
- **Ready/done (current):** `bg-emerald-600 border-emerald-600 text-white hover:bg-emerald-700` -- solid green, already good.

**Dark-mode-only change (BR8 compliance):** The not-ready state must change for dark mode only. Light mode keeps the current ghost style (it doesn't fail WCAG AA). Use the `useTheme` composable's `isDark` computed to conditionally apply the class set:

- **Not ready, dark mode:** `bg-emerald-600/80 text-white hover:bg-emerald-700` -- translucent emerald fill with white text.
- **Not ready, light mode:** Keep current `bg-surface border-border text-text-secondary hover:bg-surface-raised` (unchanged).
- **Ready/done (both modes):** Keep current `bg-emerald-600 border-emerald-600 text-white hover:bg-emerald-700` (unchanged).

Import `useTheme` from `../../composables/useTheme` and use `isDark` in the class binding for the not-ready branch. The ready/done branch stays unconditional since solid emerald works in both themes.

**User decision:** Option B confirmed -- emerald filled translucent for not-ready.

**Contrast note:** `bg-emerald-600/80` alpha-composites with the surface underneath. On `#0a0a0f` page background, the effective color will be slightly darker than pure `#059669`. Verify the actual composited contrast in browser DevTools (inspect the rendered element's computed background color) rather than computing against the raw emerald-600 hex.

**Accept:**
- "I'm done" not-ready state in dark mode is a filled green button with white text, clearly visible against dark backgrounds
- "I'm done" not-ready state in light mode is unchanged (ghost/outline style)
- "Done!" ready state remains solid emerald in both modes (unchanged)
- The "I'm done" button in dark mode is at least as visually prominent as the "Next: {Phase}" button in the header
- No new CSS custom properties introduced

### Step 4: Fix phase stepper label contrast

**Skill:** None (component-specific contrast fix, no pattern skill applies)

**File:** `web/reflekt-web/src/components/PhaseIndicator.vue`

**Problem:** Active/completed phase labels use `text-primary-700` (`#4338ca`) which achieves only ~2.3:1 against card surface `#13131a` in dark mode. Inactive labels use `text-text-muted` which is fixed by Step 1.

**Fix for active/completed labels:** Replace `text-primary-700` (search for `text-primary-700` in the label `<span>`) with `text-badge-primary-text`. This existing theme-aware token resolves to:
- Dark: `#818cf8` (bright indigo, ~12.5:1 against `#13131a` -- well above 4.5:1)
- Light: `#4338ca` (dark indigo, ~8.5:1 against `#ffffff` -- excellent)

This reuses an existing token (BR9 compliant). The `--badge-primary-text` variable already exists in both `:root` and `.light` blocks in `main.css`. The semantic stretch (badge token on a phase label) is acceptable because the underlying value is simply "primary color, readable on current surface" -- exactly what the phase label needs.

**Verify inactive labels:** After Step 1 raises `--text-muted`, inactive labels (`text-text-muted`) will inherit the new value. Confirm the new muted value achieves >= 3:1 against `#13131a` (required) and >= 4.5:1 (preferred). With the Step 1 target of ~4.7:1, both thresholds are met.

**Accept:**
- Active/completed phase labels achieve >= 4.5:1 contrast against card surface in dark mode
- Inactive phase labels achieve >= 4.5:1 contrast against card surface in dark mode (via Step 1 muted token fix)
- Light mode phase labels are unchanged in appearance (`#4338ca` dark indigo, same value the badge-primary-text token provides in light mode)
- No new CSS custom properties introduced

### Step 5: Build verification and contrast audit

**Skill:** None (verification only)

**Files:** None created or modified.

1. Run `npm run build` from `web/reflekt-web/` -- must pass clean.
2. Verify no new CSS custom properties were introduced in `main.css` -- the only change should be the `--text-muted` value in `:root`.
3. Contrast spot-check (manual or computed):
   - New `--text-muted` value against `#13131a` (card surface): >= 4.5:1
   - New `--text-muted` value against `#0a0a0f` (page background): >= 4.5:1
   - `#818cf8` (badge-primary-text, now used for active phase labels) against `#13131a`: >= 4.5:1
   - `bg-emerald-600/80` button text (white) against the composited emerald background: >= 4.5:1. Use browser DevTools to inspect the rendered element's computed background color (the 80% opacity alpha-composites with the dark surface underneath, so the effective color differs from raw `#059669`).
4. Light mode regression check: confirm `.light` block in `main.css` is untouched. Toggle to light mode and verify: retro board cards, "I'm done" button (should remain ghost/outline style), phase stepper labels (should remain dark indigo), muted text color (should remain `#9b9bab`).

**Accept:**
- Build passes
- All contrast ratios meet their WCAG AA thresholds
- Light mode is visually unchanged
- No new CSS custom properties exist in `main.css`

## Cross-Service Changes

None.

## Migration Notes

None (no database changes).

## Testing Strategy

- **Manual dark mode verification:** Open the retro board in Writing phase. Confirm cards have visible borders, "No cards yet" text is readable, "Add a note..." inputs have visible borders, "I'm done" button is a filled green button.
- **Phase stepper check:** Verify active phase label ("Writing") is bright indigo, inactive labels ("Grouping", "Voting", etc.) are readable mid-grey. Both should be legible at a glance.
- **Button hierarchy check:** With both "I'm done" (emerald) and "Next: Grouping" (indigo) visible, confirm neither overpowers the other -- both are filled buttons with white text, differentiated by color.
- **Light mode regression:** Toggle to light mode. Verify retro board, phase stepper, and "I'm done" button all render correctly. The "I'm done" button must remain ghost/outline style in light mode (not emerald filled). Muted text should be unchanged (#9b9bab). Phase labels should show dark indigo (#4338ca).
- **Theme toggle round-trip:** Switch dark -> light -> dark. Verify all fixes persist correctly through toggles.

## KB Impact

None (purely frontend contrast fixes, no business rules or domain logic changes).

## Open Questions

None.
