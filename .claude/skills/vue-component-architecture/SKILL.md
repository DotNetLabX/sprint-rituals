---
name: vue-component-architecture
description: Vue 3 component architecture — level hierarchy, split decisions, composable lifecycle, store boundaries, props resolution. Loaded when designing component structure or splitting large components.
user-invocable: true
---

# Vue Component Architecture

Opinionated rules for structuring Vue 3 components in this project. Based on proven patterns from KIMJINWOO4/vue-skills and alexanderop/claude-skill-vue-development.

## Component Levels

Every component has a level. The level determines what it may access.

| Level | Name | Max Lines | Store Access | Composables | Children |
|-------|------|-----------|-------------|-------------|----------|
| L0 | Page (View) | < 100 | Read-only | OK | L1 |
| L1 | Feature | < 200 | Full CRUD | OK | L2, L3 |
| L2 | Controller | < 150 | via inject | OK | L3, L4 |
| L3 | Humble | < 100 | Forbidden | Forbidden | L4 |
| L4 | Atomic | < 50 | Forbidden | Forbidden | None |

**Decision tree:**
- Route target → L0 (`*View.vue` in `views/`)
- Independent feature area → L1 (e.g., `SprintSummaryCard.vue`)
- Fetches data or wires composables → L2
- Props-only, no logic → L3
- Single UI primitive → L4

**This project's mapping:**
- `client/src/views/*View.vue` → L0
- `client/src/components/App*.vue`, `PageLayout.vue`, `PageToolbar.vue` → L1/L2
- Feature-specific sub-components → L3
- Design tokens, icons → L4

## When to Split

**MUST split** when a component exceeds 200 lines.

| Signal | Extract To |
|--------|-----------|
| 3+ `v-if` branches, each > 30 lines | Conditional components |
| `v-for` body > 20 lines | Item component |
| 2-3 collapsible sections, each > 50 lines | Section components |
| Script fetches 2+ independent data sources | Composable per source |
| 5+ props with 3+ handlers | provide/inject |

**Do NOT split when:**
- Component < 150 lines of mostly static markup
- Splitting adds files without improving comprehension
- Only 1 occurrence (no reuse benefit)

## Inline vs External Composables

**Start inline. Extract when reused in 2+ components.**

```typescript
// INLINE — component-specific logic stays in the component
<script setup lang="ts">
function useFormState() {
  const form = ref({ name: '', email: '' })
  const errors = ref<Record<string, string>>({})
  return { form, errors }
}

const { form, errors } = useFormState()
</script>

// EXTERNAL — only when reused across components
// client/src/composables/useTheme.ts
export function useTheme() { /* ... */ }
```

**Composable rules:**
- Must use Vue lifecycle APIs (ref, watch, onMounted) — otherwise it's a plain function, drop `use` prefix
- One composable = one lifecycle concern
- Setup and cleanup stay together (`onMounted` + `onUnmounted`)
- Return refs in a named object, never an array
- No UI side effects (toasts, alerts) — expose error state, component handles UI

## Store Boundary

**In store:** Shared business state, business actions, computed getters.

**NOT in store:** UI-only state (local ref), form draft state (view-local reactive), API call logic (composable/service), transient UI state (drag, hover).

**Project convention:** Store actions only mutate store-owned state. View handlers mutate form/UI state. Never cross the boundary.

Store > 300 lines → split into domain-focused stores.

## Props Resolution

| Props Count | Action |
|------------|--------|
| 1-3 | Fine as-is |
| 4-5 | Consider "Preserve Whole Object" (pass entity, not fields) |
| 6+ | Must use provide/inject or split component |
| 3+ handler props | Must use provide/inject action object |

**No props drilling beyond 1 level.** Use provide/inject with typed `InjectionKey` symbols for deeper passing.

## Anti-Patterns — STOP and Fix

| If you catch yourself... | Do this instead |
|-------------------------|-----------------|
| `const props = defineProps()` without using props in script | Drop the `const props =` |
| Manual `modelValue` + `update:modelValue` | Use `defineModel<T>()` |
| `defineEmits(['event'])` array syntax | Use typed `defineEmits<{ event: [args] }>()` |
| External composable used in only 1 component | Move to inline composable |
| Composable calling `showToast()` or `alert()` | Expose error state, handle UI in component |
| Store mutating form/UI state | Keep form state in view, store state in store |
