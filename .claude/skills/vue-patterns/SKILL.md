---
name: vue-patterns
description: Vue 3 Composition API patterns — script setup macros, reactivity, composables, lifecycle, built-in components. Loaded when writing or reviewing Vue SFC files. MUST be used for Vue.js tasks.
user-invocable: true
---

# Vue Patterns

Vue 3.5+ with `<script setup lang="ts">`. Always Composition API. Never Options API.

## Principles

- TypeScript first — all props, emits, and composable returns fully typed
- `shallowRef` over `ref` for large objects (only `.value` assignment triggers)
- No Reactive Props Destructure — use `defineProps` + `withDefaults`
- Composables use `use` prefix and return refs (not reactive objects)
- One composable = one lifecycle concern

## Script Setup Macros

See `references/script-setup.md` for full patterns.

Summary:
- **defineProps** — type-based: `defineProps<{ title: string }>()`. Defaults via `withDefaults`
- **defineEmits** — named tuple: `defineEmits<{ update: [value: string] }>()`
- **defineModel** — two-way binding (Vue 3.4+): `const model = defineModel<string>()`
- **defineExpose** — explicitly expose to parent via template refs
- **defineOptions** — `inheritAttrs: false`, custom `name`
- **defineSlots** — typed slot props

## Reactivity

See `references/reactivity.md` for full patterns.

Summary:
- `ref` for primitives and small objects. `shallowRef` for large data.
- `computed` for derived state. Writable computed for two-way transforms.
- `watch` with `deep: 2` depth limit (3.5+), `once: true` (3.4+)
- `watchEffect` auto-tracks deps. Use `onWatcherCleanup` (3.5+) for teardown.
- `toValue()` (3.3+) to unwrap `MaybeRefOrGetter<T>` in composables.

## Composable Pattern

```typescript
// composables/useFeature.ts
import { ref, onMounted, onUnmounted } from 'vue'

export function useFeature(options: FeatureOptions) {
  const data = ref<Data | null>(null)
  const loading = ref(false)

  async function load() {
    loading.value = true
    try {
      data.value = await fetchData(options)
    } finally {
      loading.value = false
    }
  }

  onMounted(load)

  return { data, loading, reload: load } // return refs, not reactive
}
```

**Module-level refs** — for shared state across call sites, declare refs outside the function. Per-call state goes inside the function. Pinia stores are preferred for shared business state.

## Built-in Components

- **Transition** — `<Transition name="fade">` with CSS classes `.fade-enter-active`, `.fade-leave-active`
- **TransitionGroup** — list animations, `key` required on children
- **Teleport** — `<Teleport to="body">` for modals/toasts. `:disabled` to render in-place.
- **Suspense** — wraps async components (top-level `await` in `<script setup>`)
- **KeepAlive** — `:include`/`:exclude` by component name, `:max` cache limit

## URL-State Sync

Views that persist selection state in the URL follow a two-part pattern: seed from URL on mount, watch store to update URL.

```vue
<script setup lang="ts">
import { onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'

const route = useRoute()
const router = useRouter()
const store = useFeatureStore()

onMounted(async () => {
  // 1. Seed store from URL before initialize
  const param = route.query.itemId
  if (param && !isNaN(Number(param))) {
    store.selectItem(Number(param))  // use store action, not direct mutation
  }
  await store.initialize()
})

// 2. Keep URL in sync when selection changes
watch(
  () => store.selectedItemId,
  (id) => {
    if (id !== null) {
      router.replace({ query: { ...route.query, itemId: String(id) } })
    }
  }
)
</script>
```

**Rules:**
- Use `router.replace` (not `push`) to avoid polluting browser history with filter changes.
- Seed from URL **before** `initialize()` so the first fetch uses the URL value.
- Use store actions for mutations, not direct property assignment.

## Recommended Packages

See `references/recommended-packages.md` for the approved ecosystem stack.

## Project Conventions

- Types live in `client/src/types/index.ts`
- API modules in `client/src/api/{resource}.ts` using `apiFetch`
- Stores in `client/src/stores/{name}Store.ts`
- Components in `client/src/components/`
- Composables in `client/src/composables/`
- Views in `client/src/views/{Name}View.vue`
- `RouterLink` and `RouterView` are globally registered — no import needed
