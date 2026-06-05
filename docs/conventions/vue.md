# Vue Conventions

Programming conventions for Vue 3 + TypeScript specific to this codebase. Referenced by developer and reviewer agents. Complements the skills in `.claude/skills/` (vue-patterns, vue-component-architecture, pinia-patterns, tailwind-theme).

## Reactivity Gotchas

- When a composable is called in multiple places, declare shared reactive refs at **module level** (outside the exported function). Function-scoped refs give each call site an isolated copy.
- Guard against empty-state flickering by checking all three conditions: `data.length === 0 && result === null && !loading`. Without the `!loading` guard, the empty-state message flashes during async calls.
- When multiple code paths write to the same form field (e.g., auto-detect on mount vs manual Re-detect), apply the **same guard conditions** on both paths. Guard parity prevents silent data loss on empty results.

## Component Patterns

- `RouterLink` is globally registered by Vue Router — no import needed in `<script setup>`.
- Use `withDefaults(defineProps<T>(), { ... })` for components with optional props that have defaults.
- `<select>` elements need `appearance-none bg-transparent` Tailwind classes to override native browser styling.

## Store / View Boundary

- Store actions only mutate store-owned state. View handlers mutate form/UI state. Never cross the boundary.
- Error state from the store (`error` ref) must be surfaced in views — not silently swallowed.
- Empty state must be rendered when data or filter produces zero results. Don't let `v-if="list.length > 0"` silently hide valid empty results.

## White Flash Prevention

- In `index.html`, use inline `style` on `<body>` with the literal hex value of the dark theme background. Tailwind CSS custom property tokens are not available until the CSS file loads.

## TypeScript

- `erasableSyntaxOnly` (Vite default) forbids `enum` declarations. Use type unions: `type SprintState = 'Active' | 'Closed'`.
