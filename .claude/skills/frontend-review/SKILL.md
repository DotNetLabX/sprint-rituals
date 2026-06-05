---
name: frontend-review
description: Frontend code review checklist — accessibility, reactive state, Tailwind tokens, component architecture, error/empty states. Loaded by reviewer agent for Vue/TypeScript features.
user-invocable: true
---

# Frontend Review Checklist

Review checklist for Vue 3 + TypeScript + Tailwind CSS v4 code. Supplements the standard reviewer checklist with frontend-specific checks.

## Stage 1: Plan Conformance (same as backend)

- Every plan step has corresponding implementation
- Named identifiers match plan (function names, component APIs, store actions)
- File paths match plan specification
- Deviations documented

## Stage 2: Component Architecture

- [ ] Components respect level hierarchy (L0 < 100 lines, L1 < 200, etc.)
- [ ] Store state vs view state boundary respected (store owns business data, view owns form/UI)
- [ ] `storeToRefs()` used for reactive destructuring (not bare destructure)
- [ ] Actions destructured directly from store (not via storeToRefs)
- [ ] `defineModel<T>()` used instead of manual `modelValue` + emit
- [ ] Props typed with `defineProps<T>()` generics (not runtime validation)
- [ ] Emits typed with `defineEmits<T>()` (not array syntax)

## Stage 3: Accessibility

- [ ] `aria-current="page"` on active navigation links (not just CSS class)
- [ ] `aria-label` on icon-only buttons (not just `title` attribute)
- [ ] `aria-invalid` on form inputs with validation errors
- [ ] Semantic HTML elements (`<nav>`, `<main>`, `<button>`, `<select>`) not `<div>` with click
- [ ] Keyboard navigation works (Tab order, Enter/Space on buttons)

## Stage 4: Reactive State

- [ ] Composables that share state across call sites use module-level refs
- [ ] Per-instance composables use function-scoped refs
- [ ] No composables calling UI side effects (toasts, alerts) — expose error state
- [ ] `watch`/`watchEffect` cleanup for subscriptions and timers
- [ ] Loading guards prevent flickering (check `loading && !result && !data`)
- [ ] Error state surfaced in views (not silently swallowed in store)
- [ ] Empty state rendered when data/filter produces zero results

## Stage 5: Tailwind & Styling

- [ ] Design tokens used (not hardcoded hex colors)
- [ ] No `dark:` prefix — use CSS custom property tokens
- [ ] `<select>` elements have `appearance-none bg-transparent`
- [ ] Responsive: mobile-first, breakpoints layered (`sm:`, `md:`, `lg:`)
- [ ] Consistent spacing and typography across feature

## Stage 6: TypeScript

- [ ] Types defined in `client/src/types/index.ts` (not inline)
- [ ] `import type` used for type-only imports
- [ ] No `any` types (use `unknown` if truly needed)
- [ ] TypeScript `erasableSyntaxOnly`: no `enum` declarations (use type unions)
- [ ] API response types match backend endpoint contracts

## Stage 7: Build Evidence

- [ ] `npm run build` from `client/` passes (runs `vue-tsc -b` + Vite)
- [ ] Build output provided (not assumed or claimed)
- [ ] No TypeScript errors in build output

## Stage 8: Gap Analysis

- [ ] Error paths: what happens on API failure?
- [ ] Empty states: what shows when list/filter returns zero items?
- [ ] Loading states: is there feedback during async operations?
- [ ] Sub-team/filter edge cases: what shows when filter cross-product is empty?
- [ ] v-if visibility guards: do they silently hide valid empty results?

## Severity Mapping

| Finding | Severity |
|---------|----------|
| Missing `aria-label` on interactive element | MEDIUM |
| Store mutating view/form state | HIGH |
| Hardcoded colors instead of tokens | MEDIUM |
| Missing error state in view | MEDIUM |
| `any` type in API contract | HIGH |
| Build fails | CRITICAL |
| Dead-code function with no UI trigger | HIGH |
| Unnamed deviation from plan identifiers | HIGH |
| Missing loading guard (flickering) | LOW |
| Inconsistent spacing | LOW |
