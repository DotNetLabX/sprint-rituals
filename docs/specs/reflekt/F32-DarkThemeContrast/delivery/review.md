# Dark Theme Contrast & Visual Hierarchy — Review

## Reviewed By
reviewer (Sonnet agent)

## Verdict: APPROVE

## Pre-commitment Predictions

1. **Contrast math discrepancy** — Implementation audit table might report slightly different ratios than independent calculation. FOUND (minor): claimed 4.65:1 for `#8585a0` on `#13131a`; actual is 5.16:1. Still passes. Also claimed 5.90:1 for `#818cf8` on `#13131a`; actual is 6.20:1. Compliance unaffected — both well above 4.5:1.
2. **`text-badge-primary-text` Tailwind safelist** — Token used in PhaseIndicator.vue must be in `@source inline` or scanned by Tailwind; otherwise the class would be purged. NOT a problem: `text-badge-primary-text` is present in the `@source inline(...)` directive in `main.css` (line 3). Build passes clean.
3. **`isDark` ternary timing / SSR** — `useTheme` reads `localStorage` at module init; if the value is stale between toggle and render the button could flash. NOT a problem: `isDark` is a `computed()` off a module-level `ref` — all consumers share the same reactive state. Correct per vue.md.
4. **Tooltip wiring omitted** — `help.tooltips.md` exists for this feature; the `help-tooltips.md` rule requires architect to include tooltip wiring in plan steps and developer to wire tooltips. FOUND: Neither plan nor implementation includes tooltip wiring for "I'm Done", the phase stepper, or any of the eight tooltip entries defined in `definition/help.tooltips.md`.
5. **Light mode regression from `border border-border` on `RetroCardItem`** — Plan acknowledges this adds a `#e2e2ea` border in light mode too. Confirmed in code: the border is static, not conditionally applied. Plan explicitly accepted this as cosmetically negligible. NOT a blocker.

---

## Findings

### [LOW] Implementation audit table reports incorrect contrast ratios

**File:** `docs/specs/F32-DarkThemeContrast/delivery/implementation.md` (Contrast Audit table)
**Issue:** The audit table claims 4.65:1 for `#8585a0` on `#13131a` and 5.90:1 for `#818cf8` on `#13131a`. Independent WCAG calculation yields 5.16:1 and 6.20:1 respectively. All values pass the 4.5:1 threshold regardless — this is a documentation accuracy issue only. The developer lesson file also notes the `bg-emerald-600/80` composited contrast row was omitted; independently computed as 5.34:1 (white on `#067a57`), which passes.
**Fix:** Update the audit table with the correct ratios and add the missing emerald row. Not a code defect.

### [LOW] Tooltip wiring not implemented — plan gap

**File:** `docs/specs/F32-DarkThemeContrast/definition/help.tooltips.md` (exists, 8 entries)
**Issue:** The `help-tooltips.md` rule states the architect must include tooltip wiring in plan steps for relevant UI components, and the developer must wire them. `help.tooltips.md` defines tooltips for "I'm Done Button", "Phase Stepper", and all five phase names. None of the four plan steps include tooltip wiring, and the implementation does not add any `title` attributes or tooltip components to the button or stepper elements. The developer correctly followed the plan as written — this is a plan omission, not a developer error.
**Fix:** Architecture decision needed: either the architect adds a follow-up step to a future plan, or this feature's plan is retroactively updated to include tooltip wiring. Not a blocking code defect for this cycle.

---

## Positive Observations

- All four plan steps were executed exactly as specified with zero deviations. The diff is surgically minimal.
- The `border-transparent` trick for cancelling the base `border` class in the dark not-ready button state is correct and idiomatic for Tailwind v4 class binding arrays. The developer documented the reasoning in lessons.md.
- `useTheme` import and destructuring in `RetroBoardPanel.vue` follows the module-level composable pattern correctly per vue.md — `isDark` is a shared computed ref, not an isolated copy.
- The `text-badge-primary-text` token reuse for phase labels is semantically sound: the token provides "primary color readable on current surface" in both modes, which is precisely what the label needs. Light mode appearance is identical to before (`#4338ca`).
- Pre-existing TypeScript errors were correctly identified as pre-existing via `git stash` rather than treated as regressions, and `npm run build-only` was used to confirm Vite bundle compilation passes independently.
- The `@source inline` safelist already contained `text-badge-primary-text` from F31, so no safelist update was needed and the class is not at risk of purge.

---

## Gaps

- The `bg-emerald-600/80` composited contrast was not verified in-browser (plan Step 5 explicitly required DevTools inspection). The developer omitted this row from the audit table. The independent calculation (5.34:1) confirms compliance, but the traceability gap was noted in architect lessons.md.

---

## Open Questions

- None. All pre-commitment predictions resolved.

---

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Build | PASS | `npm run build-only` (from `web/reflekt-web/`) | `✓ built in 800ms`, 0 errors |
| `--text-muted` dark value | `#8585a0` | git diff + file read | Changed from `#60607a`; `.light` block `#9b9bab` untouched |
| New CSS custom properties | 0 | File read of `main.css` | Only `--text-muted` value changed in `:root` |
| Contrast: `#8585a0` on `#13131a` | 5.16:1 — PASS >= 4.5:1 | Node.js WCAG calculation | Luminance L1=0.2430, L2=0.00679 |
| Contrast: `#8585a0` on `#0a0a0f` | 5.51:1 — PASS >= 4.5:1 | Node.js WCAG calculation | |
| Contrast: `#818cf8` on `#13131a` | 6.20:1 — PASS >= 4.5:1 | Node.js WCAG calculation | |
| Contrast: white on `bg-emerald-600/80` composited | 5.34:1 — PASS >= 4.5:1 | Node.js calculation (alpha composite over `#0a0a0f`) | Effective hex `#067a57` |
| `text-badge-primary-text` in safelist | Present | Node.js `@source inline` parse | Found in `main.css` line 3 |
| `border border-border` on card wrapper | Present | File read `RetroCardItem.vue:2` | Static class on root `<div>` |
| `isDark` conditional on button | Present | File read `RetroBoardPanel.vue:10-12` | Ternary in class binding array |
| `useTheme` import | Present | git diff `RetroBoardPanel.vue` | Line 196 import, line 223 destructure |
| `text-badge-primary-text` on phase labels | Present | File read `PhaseIndicator.vue:15` | Replaces `text-primary-700` |
| `.light` block unchanged | Confirmed | git diff `main.css` | No diff in `.light` block |
| Light mode `text-badge-primary-text` value | `#4338ca` | File read `main.css:87` | Same as old `text-primary-700` — no light mode regression |
