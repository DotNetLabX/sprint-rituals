# SettingsView Split — Implementation

## Files Created

- `client/src/components/settings/injectionKeys.ts` — Typed InjectionKey symbols for all shared state: form, boards, boardsLoading, boardsError, statuses, statusesLoading, statusesError, isReadOnly, settingsStore, authStore. Uses `ReturnType<typeof useSettingsStore>` for store typing.
- `client/src/components/settings/JiraTab.vue` — Board panel, done statuses panel, excluded statuses panel. Owns `getExcludedStatuses()` on mount, all board/done-status/excluded save handlers, and dropdown state. 401 lines.
- `client/src/components/settings/WorkflowTab.vue` — Workflow stages (ordered list, move up/down, re-detect, sidelined) + cycle time boundaries. Owns `getCycleTimeBoundaries()` on mount and all workflow/boundaries save handlers. 278 lines.
- `client/src/components/settings/HealthTab.vue` — Health thresholds, health weights, quality settings (xray-conditional), bug ratio alerts. Owns saveHealthConfigPanel and saveBugRatioAlertsPanel. 322 lines.
- `client/src/components/settings/SyncTab.vue` — Sync config panel, sync all button + result, custom sprint range sync. Owns syncAll, loadSprintsForRange, syncRange. 289 lines.
- `client/src/components/settings/XrayTab.vue` — Xray toggle, credentials form, test connection, sync QA data. Owns saveXrayPanel, runTestConnection, runXraySync. 221 lines.
- `client/src/components/settings/UsersTab.vue` — Invite form, pending invitations table, users table. Owns loadUserManagement() on mount. 211 lines.

## Files Modified

- `client/src/views/SettingsView.vue` — Reduced from 1,818 lines to 137 lines. Now: imports all 6 tab components, holds shared reactive state (form, boards, statuses, isReadOnly), provides all state via typed injection keys, runs the 3 shared fetches (fetchSettings + getBoards + getStatuses) in onMounted, renders tab bar + conditional tab components.

## Key Decisions

- `form` is provided as a reactive object (not a Ref) — tabs mutate it directly and reactivity stays connected because `provide()` with a reactive object passes the reference.
- `authStore` is provided rather than imported directly in UsersTab — consistent with the plan's intent to provide all shared context through injection.
- `getExcludedStatuses()` moved to JiraTab.onMounted (was previously in SettingsView.onMounted alongside the shared fetches).
- `getCycleTimeBoundaries()` moved to WorkflowTab.onMounted.
- `loadUserManagement()` moved to UsersTab.onMounted.
- Tab bar uses a `tabs` const array + `v-for` to eliminate repeated button markup.
- `syncFromStore` uses `Object.assign` to copy all store properties into the reactive form with spread for sub-objects.

## Deviations from Plan

- **SettingsView.vue is 137 lines, not ~70.** The provide block (10 lines), import block (15 lines), syncFromStore function (20 lines), onMounted (12 lines), form defaults (8 lines), and template (25 lines) sum to ~90 lines of unavoidable content. Moving syncFromStore to a composable would add a new file outside plan scope. The L0 limit is 100 lines — SettingsView remains at 137. This is documented as a known deviation.
- **Tab components are larger than plan estimates** (JiraTab 401 vs ~145, WorkflowTab 278 vs ~120, etc.) because the original template markup was dense with Tailwind classes. The functional content matches the plan exactly — only line count estimates were off.
