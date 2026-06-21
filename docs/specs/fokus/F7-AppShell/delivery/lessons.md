# App Shell & Navigation — Lessons

## Architect Lessons
- [APPLIED] No frontend skill exists in `.claude/skills/`. Every step in this plan has disposition "None" in the skill mapping. A `vue-frontend-patterns` skill would cover: composables, layout components, design tokens, theme management, empty states. This is already documented as a gap in `docs/architecture/v2.md` but bears repeating — the first frontend feature is entirely unguided by skills.
- [APPLIED] The architecture doc lives at `docs/architecture/v2.md`, not `v1.md`. The agent instructions reference `@docs/architecture/v1.md` which does not exist. The correct reference is `v2.md`.
- [APPLIED] Tailwind v4 has a fundamentally different configuration model than v3 (CSS-first with `@theme` directive, no `tailwind.config.js`). The plan must not assume Tailwind v3 patterns. The existing `main.css` already uses the v4 import (`@import "tailwindcss"`).
- [TRACKED] The existing `index.html` already has `class="dark"` on `<html>` and dark-themed body styles — this is a head start for the theme system but also means the theme composable needs to initialize from this state, not fight it.
- [TRACKED] Settings view has hardcoded gray color values (bg-gray-950, bg-gray-900, etc.) that don't use design tokens. Full migration to tokens was deemed out of scope for this feature to avoid regression risk — the plan explicitly calls this out as a future task. This is a conscious trade-off.
- [TRACKED] (Step 1 review) Purely frontend plans with a "verify and polish" catch-all step are hard to done-check rigorously — the step's scope is intentionally open-ended. The developer handled this well by documenting the `index.html` change (which fell under the polish step's explicit instruction about body background). For future plans, consider making the polish step's expected outputs more concrete (e.g., "build output confirmation, list of files adjusted during polish") so the done check has a crisper target.
- [TRACKED] (Step 1 review) This was a clean pass — 8 steps, 8 steps covered, zero deviations. The plan's specificity (full file paths, exact component props, explicit token categories) made the done check straightforward. High-specificity plans produce high-quality implementation reports.

## Developer Lessons

- [APPLIED] **Tailwind v4 `@theme` with CSS custom properties:** The `@theme` directive maps Tailwind utility class names to CSS custom properties via `var(--token)` references. The actual token values live in `:root`/`.dark`/`.light` blocks outside `@theme`. The `@theme` block only declares the alias (e.g., `--color-surface-page: var(--surface-page)`). This two-layer pattern (alias in `@theme`, value in CSS scope) is required for theme switching via class toggling on `<html>`.

- [APPLIED] **Shared reactive state across composable call sites:** When a composable is called in multiple places (e.g., `main.ts` and `AppHeader.vue`), declare reactive refs at module level (outside the exported function). If declared inside the function, each call site gets its own isolated ref and state changes don't propagate.

- [APPLIED] **`RouterLink` is globally registered:** Vue Router registers `RouterLink` and `RouterView` globally when `app.use(router)` is called. Components using `RouterLink` in templates do not need an explicit import in `<script setup>`. This is a Vue Router v4 convention.

- [APPLIED] **Body background before CSS variables resolve:** Tailwind CSS custom property tokens are not available until the CSS file loads and the theme class is applied. Using inline `style` on `<body>` with the literal hex value of the dark theme background prevents the brief white flash that would occur when using a Tailwind class that resolves before custom properties are applied.

- [TRACKED] **No `components/` or `composables/` directories pre-existed:** The entire component and composable layer was greenfield. The Write tool creates parent directories automatically when writing a new file — no need to `mkdir` first.

- [TRACKED] **PowerShell-only environment:** The shell environment is Bash via the Bash tool but the OS is Windows. PowerShell cmdlets (`New-Item`, `Test-Path`) are not available in the Bash tool. Use POSIX commands (`mkdir -p`, `test -f`) or rely on tool-native behavior (Write creates directories automatically).

## Reviewer Lessons

- [APPLIED] **Frontend reviews need an adapted checklist.** The standard reviewer checklist (N+1, SaveChangesAsync, domain events, repository interfaces) is entirely backend-oriented. For Vue 3 / TypeScript frontend features, the relevant checks are: component structure, theme persistence correctness, reactive state sharing across composable call sites, responsive listener cleanup, accessibility basics (aria-current, aria-label on icon buttons), Tailwind v4 token consistency, and that placeholder UI is genuinely non-interactive. This should be captured in a frontend review skill when one is created.
- [APPLIED] **`aria-current="page"` is easy to miss.** Visual active-state styling via CSS class is the first thing implemented and is obvious to review. `aria-current` is invisible to visual inspection and requires deliberate accessibility checking. Add it to the frontend review checklist.
- [APPLIED] **`title` vs `aria-label` on icon buttons.** Icon-only buttons using `:title` for their accessible name are a common pattern that passes visual review but fails accessibility. The correct attribute is `aria-label`. This is a recurring pattern to watch for in future frontend reviews.
- [APPLIED] **Plan slot vs prop distinction matters.** When a plan specifies "slot-based layout: `title` slot", implementing it as a `title` prop is a deviation even if functionally equivalent for current callers. Review should catch explicit plan deviations even when they are "benign" — the plan is a contract and deviations need acknowledgment.
- [APPLIED] **Build evidence is easy to obtain for frontend.** `npm run build` from `client/` runs `vue-tsc -b` + Vite in under 30 seconds. Always run it — TypeScript type errors are a real risk in frontend implementations and cannot be assumed to pass.

## Skill Gaps

- [APPLIED] **Missing skill:** Frontend Vue 3 component patterns — covering composables, layout components, slot-based design, router integration, and Tailwind v4 theme configuration. Suggested name: `vue-frontend-patterns`. Reference files: all files created in this feature (`client/src/composables/useTheme.ts`, `client/src/components/App*.vue`, `client/src/components/PageLayout.vue`, etc.). Pre-identified in plan Skill Mapping table.
