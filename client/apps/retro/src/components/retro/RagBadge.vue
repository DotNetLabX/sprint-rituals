<template>
  <BaseBadge :variant="variant">
    <slot />
  </BaseBadge>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import BaseBadge from '../ui/BaseBadge.vue'

const props = defineProps<{
  rag: string | null | undefined
}>()

const variant = computed(() => {
  if (props.rag === 'green') return 'success'
  if (props.rag === 'amber') return 'warning'
  if (props.rag === 'red') return 'danger'
  return 'default'
})
</script>
