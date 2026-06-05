# Reactivity Patterns

## ref vs shallowRef

```typescript
// ref — deep reactivity, triggers on nested property changes
const user = ref({ name: 'Alice', scores: [1, 2, 3] })
user.value.name = 'Bob' // triggers

// shallowRef — only .value assignment triggers
const data = shallowRef<BigObject[]>([])
data.value[0].name = 'Bob' // does NOT trigger
data.value = [...data.value] // triggers (new reference)
```

Use `shallowRef` for: large arrays, API response data, objects you replace wholesale.

## computed

Read-only:
```typescript
const fullName = computed(() => `${first.value} ${last.value}`)
```

Writable:
```typescript
const fullName = computed({
  get: () => `${first.value} ${last.value}`,
  set: (val: string) => {
    const [f, l] = val.split(' ')
    first.value = f
    last.value = l ?? ''
  },
})
```

## watch

Single source:
```typescript
watch(count, (newVal, oldVal) => { /* ... */ })
```

Multiple sources:
```typescript
watch([firstName, lastName], ([newFirst, newLast], [oldFirst, oldLast]) => { /* ... */ })
```

Getter:
```typescript
watch(() => props.id, (newId) => { /* ... */ })
```

Options:
```typescript
watch(source, callback, {
  immediate: true,  // run on mount
  deep: true,       // deep watch (or deep: 2 for depth limit in 3.5+)
  once: true,       // auto-stop after first trigger (3.4+)
  flush: 'post',    // run after DOM update
})
```

## watchEffect

Auto-tracks dependencies:
```typescript
watchEffect(() => {
  console.log(count.value) // auto-tracked
  console.log(name.value)  // auto-tracked
})
```

Cleanup (3.5+):
```typescript
watchEffect(() => {
  const controller = new AbortController()
  fetch(url.value, { signal: controller.signal })

  onWatcherCleanup(() => controller.abort())
})
```

Pause/resume (3.5+):
```typescript
const { pause, resume, stop } = watchEffect(() => { /* ... */ })
```

## toValue / toRef / toRefs

```typescript
// In composables — accept flexible input
function useFeature(input: MaybeRefOrGetter<string>) {
  watch(() => toValue(input), (val) => { /* ... */ })
}

// Destructure reactive without losing reactivity
const { name, age } = toRefs(reactiveObj)

// Single property ref from reactive
const nameRef = toRef(reactiveObj, 'name')
```

## Effect Scope

Batch disposal of watchers/computeds:
```typescript
const scope = effectScope()

scope.run(() => {
  const count = ref(0)
  watch(count, () => { /* ... */ })
  watchEffect(() => { /* ... */ })
})

scope.stop() // disposes all effects created inside
```

## Lifecycle Hooks

```typescript
onBeforeMount(() => {})
onMounted(() => {})
onBeforeUpdate(() => {})
onUpdated(() => {})
onBeforeUnmount(() => {})
onUnmounted(() => {})
onActivated(() => {})       // KeepAlive
onDeactivated(() => {})     // KeepAlive
onErrorCaptured((err) => {})
```
