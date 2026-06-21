# Dark / Light Theme — Implementation

## Files Created
- `web/reflekt-web/src/composables/useTheme.ts` — module-level `theme` ref, `isDark` computed, `toggleTheme` sets `document.documentElement.className` and persists to `localStorage` under key `reflekt:theme`

## Files Modified
- `web/reflekt-web/index.html` — added blocking inline script before `</head>` to apply `.light` class on `<html>` before paint; added `style="background-color: #0a0a0f;"` on `<body>` for flash prevention
- `web/reflekt-web/src/assets/main.css` — complete rewrite: added `:root` (dark default) and `.light` raw var blocks; restructured `@theme` to alias-only mappings; added badge tokens (6 variants × bg/text), shadow tokens (`shadow-card`, `shadow-modal`, `shadow-card-hover`), scrollbar tokens, focus-offset token; updated `@source inline(...)` to include all dynamic badge/column token classes
- `web/reflekt-web/src/components/AppHeader.vue` — added Sun/Moon theme toggle button (visible to all users); migrated all gray/blue classes to token classes
- `web/reflekt-web/src/components/PhaseIndicator.vue` — connector `bg-border`, label `text-text-muted`, inactive step `bg-surface text-text-muted border-border`
- `web/reflekt-web/src/components/layout/PageLayout.vue` — fallback texts `text-text-muted`
- `web/reflekt-web/src/components/ui/BaseBadge.vue` — all 6 variants use `bg-badge-{variant}-bg text-badge-{variant}-text` tokens
- `web/reflekt-web/src/components/ui/BaseButton.vue` — secondary/ghost variants use surface/border tokens; added `ring-offset-focus-offset`
- `web/reflekt-web/src/components/ui/BaseCard.vue` — `border-border`; hoverable adds `hover:-translate-y-0.5`
- `web/reflekt-web/src/components/ui/BaseInput.vue` — all states use surface/border/text tokens; added `ring-offset-focus-offset`
- `web/reflekt-web/src/components/ui/BaseModal.vue` — `border-border`, text/close button tokens
- `web/reflekt-web/src/components/ui/EmptyState.vue` — icon/title/description use text tokens
- `web/reflekt-web/src/components/ui/LoadingSpinner.vue` — `text-text-muted`
- `web/reflekt-web/src/components/ui/ToastContainer.vue` — `border-border`, text/close button tokens
- `web/reflekt-web/src/components/teams/CreateTeamForm.vue` — all gray/white → surface/border/text tokens
- `web/reflekt-web/src/components/teams/DeleteTeamDialog.vue` — `bg-surface shadow-modal`, all tokens
- `web/reflekt-web/src/components/teams/TeamCard.vue` — surface/border/text tokens; menu and tooltip use `bg-surface-raised border-border`
- `web/reflekt-web/src/components/teams/TeamSelector.vue` — `text-text-primary`; `bg-primary-600 hover:bg-primary-700`
- `web/reflekt-web/src/components/sprints/CreateSprintForm.vue` — all inputs/labels/buttons use tokens
- `web/reflekt-web/src/components/sprints/SprintDetailPanel.vue` — surface/border tokens; `stateBadgeClass` uses badge tokens
- `web/reflekt-web/src/components/sprints/SprintList.vue` — `bg-surface border-border shadow-card`; `stateBadge` uses badge tokens
- `web/reflekt-web/src/components/retro/RetroBoardPanel.vue` — "I'm done" inactive state `bg-surface border-border text-text-secondary hover:bg-surface-raised`; gray text refs replaced with token classes
- `web/reflekt-web/src/components/retro/RetroCardItem.vue` — `cardBgClass` fallback `bg-surface`; all edit controls use tokens
- `web/reflekt-web/src/components/retro/RetroColumnPanel.vue` — `border-border`; inline edit uses surface/border tokens
- `web/reflekt-web/src/components/retro/RetroGroupDiscussion.vue` — toggle button `text-text-muted hover:text-text-secondary`
- `web/reflekt-web/src/components/retro/RetroGroupItem.vue` — grip icons/text/border/vote buttons all use tokens; inputs use `bg-surface border-border text-text-primary`
- `web/reflekt-web/src/components/retro/RetroJoinForm.vue` — sprint name/participants/sign-in button/divider use tokens
- `web/reflekt-web/src/components/retro/RetroParticipantList.vue` — header `text-text-muted`
- `web/reflekt-web/src/components/retro/RetroPhaseHeader.vue` — sprint name/tooltip/not-ready text use tokens
- `web/reflekt-web/src/components/retro/RetroReportPanel.vue` — all loading/error/section texts use tokens
- `web/reflekt-web/src/components/retro/PreviousActionsPanel.vue` — header uses badge-warning tokens; "Pending" badge uses badge-default tokens; `statusBadgeClass`/`statusButtonClass` use token classes
- `web/reflekt-web/src/components/retro/TimerControls.vue` — popup `bg-surface-raised border-border`; `@apply` styles use surface-raised/border/text tokens
- `web/reflekt-web/src/components/retro/TimerDisplay.vue` — hover `bg-surface-raised`; timer/mute button use text tokens
- `web/reflekt-web/src/components/reports/ReportSection.vue` — `bg-surface border-border shadow-card`; all text uses tokens
- `web/reflekt-web/src/components/reports/SprintReportPanel.vue` — all three sections (header, action items, previous actions review) use surface/border/badge tokens; `reviewStatusBadgeClass` uses badge tokens
- `web/reflekt-web/src/views/ReportView.vue` — loading/empty state use text-muted/danger tokens; empty wrapper `bg-surface border-border shadow-card`
- `web/reflekt-web/src/views/RetroRoomView.vue` — kicked message `text-text-secondary`; closed banner uses badge-warning tokens; view toggle inactive state uses surface-raised/border tokens; dialog body texts use `text-text-secondary`
- `web/reflekt-web/src/views/SprintView.vue` — `bg-background`; loading text `text-text-muted`
- `web/reflekt-web/src/views/TeamDashboardView.vue` — headings `text-text-primary`; buttons `bg-primary-600 hover:bg-primary-700`; tooltips `bg-surface-raised text-text-primary border-border`
- `web/reflekt-web/src/views/WelcomeView.vue` — heading `text-text-primary`; button `bg-primary-600 hover:bg-primary-700`

## Key Decisions
- Dark mode is the default: `:root` block holds dark values; `.light` class overrides. No `.dark` class is set — the absence of `.light` means dark mode.
- `document.documentElement.className` is set to `'light'` for light mode and `''` for dark mode (not `'dark'`), matching the CSS selector structure.
- Badge border color in the closed-retro banner uses `border-badge-warning-text` rather than a dedicated border token — acceptable since the warning text color provides sufficient contrast as a border.
- Pre-existing TypeScript errors in `TeamSelector.vue` and `previousActions.ts` were confirmed on the baseline before F31 — not introduced by this feature.

## Review Fixes (Cycle 1)
- `RetroPhaseHeader.vue:32` — replaced `text-emerald-300` with `text-badge-success-text` on the "Done" participants label inside the readiness tooltip
- `RetroGroupItem.vue:126-127` — fixed `groupBgClass` to use `bg-col-${props.columnColor}-light` (added `col-` prefix, removed `/30` opacity modifier — dark token is pre-mixed at 10% opacity so the modifier was incorrect)

## Deviations from Plan
- None. All 10 steps completed as specified.
