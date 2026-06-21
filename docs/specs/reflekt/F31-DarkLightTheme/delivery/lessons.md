# Dark / Light Theme — Lessons

## Architect Lessons

- The `tailwind-theme` skill's two-layer pattern (raw vars in `:root`/`.light` + `@theme` aliases) is the right approach for this codebase. The skill already documents flash prevention via `<body style>`, but the spec requires a stronger guarantee (blocking `<script>` that sets the class on `<html>`). The skill could be extended with a "theme initialization" section covering the blocking script pattern.
- Semantic badge tokens (success/danger/warning/info/default/primary) are a reusable pattern that should exist as first-class tokens in any themed app. The spec drove their creation but they will be reusable across future features. Consider documenting this in the tailwind-theme skill.
- When planning a large token migration (200+ references across 37 files), grouping by component layer (base UI -> layout -> feature -> views) gives the developer a natural order where each layer builds on the previous. Base UI components are the foundation -- once they use tokens, downstream components inherit correct behavior through composition.
- For pure-frontend features with high file counts, the plan should explicitly note which files are expected to be "already clean" (no hardcoded colors) so the implementation.md can skip them without appearing to have missed them. The plan listed `RetroMobileTabBar.vue` and `SprintListView.vue` as needing migration, but both had zero hardcoded colors. Adding a "verify and skip if clean" note (as was done for `PageToolbar.vue`) would have been clearer.

## Developer Lessons
- Grep for hardcoded color classes (`bg-white`, `bg-gray-\d`, `text-gray-\d`, `bg-amber-`, `text-amber-`, `bg-blue-6`, `hover:bg-blue-7`, `bg-emerald-1`, `text-emerald-7`) across all `.vue` files before declaring a token migration complete. Computed class strings inside TypeScript functions (e.g. `statusBadgeClass`, `voteBtnClass`) are not covered by template-level searches alone.
- Always verify pre-existing build errors before attributing failures to new changes. A `git stash` + type-check + `git stash pop` confirms baseline errors in under 30 seconds.
- When migrating status badge functions to use badge tokens, the `@source inline(...)` directive in `main.css` must include all dynamic class strings used in computed functions — classes that never appear literally in templates won't be picked up by Tailwind's content scanner otherwise.
- The "Pending" badge in `PreviousActionsPanel.vue` (shown when `reviewStatus === 'None'`) was missed in the initial pass because it uses a separate `v-else` branch not covered by `statusBadgeClass`. A full-file grep after migrating computed functions catches these stragglers.
- `document.documentElement.className = ''` (empty string) is the correct dark-mode reset — setting it to `'dark'` would add an unused class and could break future dark-specific selectors.

## Reviewer Lessons

- When reviewing Tailwind v4 token migrations, check the compiled CSS output (`wwwroot/assets/index-*.css`) directly rather than inferring from source. The `@theme` self-referential shadow pattern (`--shadow-card: var(--shadow-card)`) looks circular in source but resolves correctly in the compiled cascade — false alarm without the compiled output check.
- Tailwind v4 CSS-var-backed color tokens (`--color-col-green-light: var(--col-green-light)`) cannot generate opacity-modifier variants (`/30`) at build time. The `/30` modifier only works on tokens with statically-known color values. When reviewing theme token migrations, always check if dynamic classes use opacity modifiers on var-backed tokens and whether `@source inline` covers them.
- When a file is listed in a migration plan step, verify ALL hardcoded color references in the file were migrated — not just the ones the plan enumerated. `RetroPhaseHeader.vue` had `text-emerald-300` inside a block that was otherwise fully migrated. Template-level greps can miss colors that were pre-existing inside partially-updated code.
- Pre-existing bugs in migrated files are in scope for the reviewer to flag, even if the migration plan did not call them out. The plan's acceptance criteria (spec) take precedence over the plan's step descriptions. If an acceptance criterion requires correct behavior and a pre-existing bug blocks it, flag it.
