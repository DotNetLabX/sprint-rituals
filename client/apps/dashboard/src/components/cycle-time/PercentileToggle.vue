<script setup lang="ts">
const props = defineProps<{
  modelValue: number
}>()

const emit = defineEmits<{
  'update:modelValue': [value: number]
}>()

const options = [50, 75, 85, 90]

function select(p: number) {
  emit('update:modelValue', p)
}
</script>

<template>
  <!-- Percentile Toggle tooltip: Switch which percentile is highlighted (single-sprint view only). P85 is recommended for commitments. -->
  <div class="flex items-center gap-2">
    <span
      class="text-xs text-text-muted cursor-help"
      title="Switch which percentile is highlighted (single-sprint view only). P85 is recommended for commitments."
    >Percentile:</span>
    <div class="flex rounded-md border border-border-default overflow-hidden text-xs">
      <button
        v-for="p in options"
        :key="p"
        class="px-3 py-1 font-medium transition-colors"
        :class="modelValue === p
          ? 'bg-accent-default text-white'
          : 'bg-surface-card text-text-secondary hover:bg-sidebar-item-hover hover:text-text-primary'"
        @click="select(p)"
      >
        P{{ p }}
      </button>
    </div>
  </div>
</template>
