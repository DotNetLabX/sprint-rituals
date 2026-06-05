<script setup lang="ts">
import { computed } from 'vue'
import BaseCard from '../BaseCard.vue'
import type { CarryOverDestination } from '../../types'

const props = defineProps<{
  destination: CarryOverDestination | null
}>()

const hasData = computed(() =>
  props.destination !== null && props.destination.priorCarryOverCount > 0
)

const buckets = computed(() => {
  if (!props.destination || !hasData.value) return []
  const d = props.destination
  const total = d.priorCarryOverCount
  return [
    { label: 'Completed', count: d.completed.count, sp: d.completed.sp, color: 'bg-status-success' },
    { label: 'Carried Again', count: d.carriedAgain.count, sp: d.carriedAgain.sp, color: 'bg-status-danger' },
    { label: 'Removed', count: d.removed.count, sp: d.removed.sp, color: 'bg-status-warning' },
    { label: 'Dropped', count: d.dropped.count, sp: d.dropped.sp, color: 'bg-surface-elevated' }
  ].map(b => ({ ...b, pct: total > 0 ? (b.count / total) * 100 : 0 }))
})
</script>

<template>
  <BaseCard v-if="destination !== null">
    <div class="text-sm font-medium text-text-primary mb-1 cursor-help" title="What happened to the prior sprint's unfinished tickets — completed, carried again, removed, or dropped.">Prior Sprint Carry-Over</div>
    <div class="text-xs text-text-muted mb-4">
      Outcomes for {{ destination.priorCarryOverCount }} carry-over ticket(s) from {{ destination.priorSprintName }}
    </div>

    <div v-if="destination.priorCarryOverCount === 0" class="text-sm text-text-muted">
      No carry-over from prior sprint.
    </div>

    <template v-else>
      <!-- Horizontal stacked bar -->
      <div class="flex h-4 rounded overflow-hidden mb-4">
        <div
          v-for="b in buckets"
          :key="b.label"
          :class="[b.color, 'transition-all']"
          :style="{ width: `${b.pct}%` }"
          :title="`${b.label}: ${b.count}`"
        />
      </div>

      <!-- Bucket details -->
      <div class="grid grid-cols-4 gap-3 text-sm">
        <div v-for="b in buckets" :key="b.label" class="flex flex-col gap-0.5">
          <div class="flex items-center gap-1.5">
            <span :class="[b.color, 'inline-block w-2 h-2 rounded-sm flex-shrink-0']" />
            <span class="text-xs text-text-muted">{{ b.label }}</span>
          </div>
          <div class="text-text-primary font-semibold tabular-nums">{{ b.count }}</div>
          <div class="text-text-secondary text-xs tabular-nums">{{ b.sp.toFixed(1) }} SP</div>
        </div>
      </div>
    </template>
  </BaseCard>
</template>
