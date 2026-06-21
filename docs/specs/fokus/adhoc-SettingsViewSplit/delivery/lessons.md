# SettingsView Split — Lessons

## Developer Lessons

- **L0 line count targets underestimate provide/inject overhead.** A view with 6+ injected keys needs ~10 `provide()` calls + import statements for all injection keys + tab component imports. This alone adds 25-30 lines before any business logic. Plan targets for L0 refactors should account for this overhead.
- **`form` provided as reactive object, not Ref.** When providing a reactive object (created with `reactive()`), tabs inject it as `AppSettings` (not `Ref<AppSettings>`) and mutate it directly. Vue's reactivity system keeps the reference connected — no `.value` needed in tabs. This differs from providing a `ref()` where `.value` access is required.
- **`ReturnType<typeof useStore>` for injection key typing.** When providing a Pinia store via InjectionKey, use `type StoreType = ReturnType<typeof useSettingsStore>` in injectionKeys.ts — import the store function and use TypeScript's built-in `ReturnType` utility. Do not import `ReturnType` from the store module.
- **Per-tab `onMounted` fetches work correctly with `v-if` tab switching.** Because tabs are rendered with `v-if` (not `v-show`), each tab's `onMounted` fires when the tab is first shown. JiraTab fetches excluded statuses, WorkflowTab fetches cycle time boundaries, UsersTab loads user management — all on first activation rather than all at once on page load.
- **Template markup line count vs functional line count.** Dense Tailwind class strings make Vue template lines much longer than functional line estimates. A component described as "~145 lines" by content becomes 400+ lines when Tailwind class strings are preserved verbatim. Plan estimates for UI-heavy components should use "sections" or "panels" as the unit rather than lines.

## Skill Gaps

- **Missing skill: `extract-view-to-tabs`.** A skill covering the full pattern of splitting a large settings/dashboard view into tab components via provide/inject would be valuable. Should cover: injection key file structure, what to provide vs import directly (stores are singletons — importing is fine), how to move per-tab onMounted fetches, and realistic line count expectations given Tailwind density.
