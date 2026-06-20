<script setup lang="ts">
import { computed } from 'vue'
import BaseCard from '../BaseCard.vue'
import type { StageFunnelEntry } from '../../types'

const props = defineProps<{
  stages: StageFunnelEntry[]
}>()

const bottleneckIndex = computed(() => {
  if (props.stages.length === 0) return -1
  let maxIdx = 0
  for (let i = 1; i < props.stages.length; i++) {
    if (props.stages[i].averageDurationDays > props.stages[maxIdx].averageDurationDays) {
      maxIdx = i
    }
  }
  return maxIdx
})

const segmentColors = [
  '#3b82f6', '#8b5cf6', '#f59e0b', '#10b981', '#f97316', '#ec4899', '#6366f1', '#14b8a6'
]

function segmentColor(index: number, isBottleneck: boolean): string {
  if (isBottleneck) return '#ef4444'
  return segmentColors[index % segmentColors.length]
}
</script>

<template>
  <!-- Stage Funnel tooltip: Average time spent in each workflow stage. The widest bar is the team's bottleneck. -->
  <BaseCard v-if="stages.length > 0">
    <div
      class="text-sm font-medium text-text-primary mb-1 cursor-help"
      title="Average time spent in each workflow stage. The widest bar is the team's bottleneck."
    >
      Stage Funnel
    </div>
    <p class="text-xs text-text-muted mb-4">Average days per stage. Red = bottleneck.</p>

    <div class="flex w-full rounded overflow-hidden h-8 mb-4">
      <div
        v-for="(stage, i) in stages"
        :key="stage.stageName"
        class="h-full flex items-center justify-center text-xs font-medium text-white overflow-hidden transition-all"
        :style="{
          width: `${stage.percentage}%`,
          backgroundColor: segmentColor(i, i === bottleneckIndex),
          minWidth: stage.percentage > 3 ? undefined : '2px'
        }"
        :title="`${stage.stageName}: ${stage.averageDurationDays.toFixed(1)} days (${stage.percentage.toFixed(1)}%)`"
      >
        <span v-if="stage.percentage > 8" class="px-1 truncate">{{ stage.stageName }}</span>
      </div>
    </div>

    <!-- Legend -->
    <div class="flex flex-wrap gap-x-4 gap-y-2">
      <div
        v-for="(stage, i) in stages"
        :key="stage.stageName"
        class="flex items-center gap-1.5 text-xs text-text-secondary"
      >
        <span
          class="inline-block w-3 h-3 rounded-sm shrink-0"
          :style="{ backgroundColor: segmentColor(i, i === bottleneckIndex) }"
        />
        <span>{{ stage.stageName }}</span>
        <span class="text-text-muted">{{ stage.averageDurationDays.toFixed(1) }}d</span>
        <span v-if="i === bottleneckIndex" class="text-status-danger font-medium">(bottleneck)</span>
      </div>
    </div>
  </BaseCard>
</template>
