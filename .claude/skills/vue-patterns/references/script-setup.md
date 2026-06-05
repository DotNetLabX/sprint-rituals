# Script Setup Macros

## defineProps

Type-based declaration (recommended):
```vue
<script setup lang="ts">
const props = defineProps<{
  title: string
  count?: number
  items: string[]
}>()
</script>
```

With defaults (Vue 3.5+ destructuring):
```vue
<script setup lang="ts">
const { title, count = 0, items = () => [] } = defineProps<{
  title: string
  count?: number
  items?: string[]
}>()
</script>
```

With defaults (Vue 3.4 and below — `withDefaults`):
```vue
<script setup lang="ts">
const props = withDefaults(defineProps<{
  title: string
  count?: number
  items?: string[]
}>(), {
  count: 0,
  items: () => [],
})
</script>
```

## defineEmits

Named tuple syntax:
```vue
<script setup lang="ts">
const emit = defineEmits<{
  change: [id: number]
  update: [value: string]
}>()

emit('change', 1)
emit('update', 'new value')
</script>
```

## defineModel

Two-way binding (Vue 3.4+):
```vue
<!-- Child -->
<script setup lang="ts">
const model = defineModel<string>()
// model.value syncs with parent's v-model
</script>

<!-- Parent -->
<MyInput v-model="username" />
```

Named models:
```vue
<script setup lang="ts">
const firstName = defineModel<string>('firstName')
const lastName = defineModel<string>('lastName')
</script>

<UserForm v-model:firstName="first" v-model:lastName="last" />
```

## defineExpose

Components are closed by default in `<script setup>`. Explicitly expose:
```vue
<script setup lang="ts">
const reset = () => { /* ... */ }
const validate = () => { /* ... */ }

defineExpose({ reset, validate })
</script>
```

## defineOptions

```vue
<script setup lang="ts">
defineOptions({
  inheritAttrs: false,
  name: 'CustomName',
})
</script>
```

## defineSlots

Typed slot props:
```vue
<script setup lang="ts" generic="T">
defineSlots<{
  default: (props: { item: T; index: number }) => any
  header: (props: { count: number }) => any
}>()
</script>
```

## Generic Components

```vue
<script setup lang="ts" generic="T extends string | number">
defineProps<{ items: T[]; selected: T }>()
defineEmits<{ select: [item: T] }>()
</script>
```
