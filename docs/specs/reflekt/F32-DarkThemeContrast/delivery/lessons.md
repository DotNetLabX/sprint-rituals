# Dark Theme Contrast & Visual Hierarchy — Lessons

## Developer Lessons
- Pre-existing TypeScript type errors (unrelated to the feature) cause `vue-tsc` to exit non-zero, making `npm run build` fail even when the Vite production bundle compiles clean. Verify pre-existence via `git stash` before investigating. Use `npm run build-only` to confirm the Vite bundle passes independently.
- When a button has a base `border` class in a static list and a conditional branch needs no border, add `border-transparent` explicitly in that branch — the base class cannot be removed conditionally in Tailwind v4 class binding arrays.
- The `useTheme` composable exposes `isDark` as a computed ref. It is safe to destructure directly: `const { isDark } = useTheme()` — module-level state means all consumers share the same reactive value.

## Reviewer Lessons
- When `help.tooltips.md` exists for a feature, verify the plan includes a tooltip wiring step. If the plan omits it, flag it as a plan gap (LOW severity) rather than a developer miss — the developer is not expected to add tooltips not in the plan.
- For contrast-only frontend features, independently recalculate all claimed ratios using the WCAG formula (not just accept the developer's audit table). The calculation is cheap (a few lines of Node.js) and catches numeric errors even when compliance is not affected.
- The `@source inline` directive in `main.css` is the Tailwind v4 safelist for dynamically-constructed class names. Always verify that new token utility classes used in templates are present there, especially when using `text-{token}` patterns for semantic tokens — the build can succeed even if a class is missing from the safelist (the class simply has no CSS output).

## Architect Lessons
- When F31's spec committed to WCAG AA compliance (BR6), the specific token values should have been validated against contrast ratios before shipping. A post-launch contrast audit would have caught the muted text failure. Future theme specs should include a contrast ratio validation step in the acceptance criteria — not just "meets WCAG AA" but specific ratio calculations per token against each surface it appears on.
- Theme-aware badge tokens (`--badge-{variant}-text`) are useful beyond badges — they provide "readable color X on current surface" values. When phase labels or other non-badge elements need the same dual-value behavior, reusing badge tokens is a pragmatic BR9-compliant approach. This pattern should be documented if it recurs.
- The `claude --print` background command approach for spawning a critic review did not produce output. Need to investigate whether this is a Windows/PowerShell path issue or a CLI limitation. For now, self-review was effective for a scoped 5-step plan.
- When a plan step asks for browser DevTools verification (e.g., composited opacity contrast), the implementation audit table should include that specific check with the method used (DevTools computed style, manual calculation, or "verified visually"). The developer omitted the `bg-emerald-600/80` composited contrast row — minor since the math works out, but the audit should be complete for traceability.
