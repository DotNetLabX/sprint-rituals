<script setup lang="ts">
import BaseCard from '../BaseCard.vue'
import type { CycleTimeSprintSummaryEntry } from '../../types'

defineProps<{
  summaries: CycleTimeSprintSummaryEntry[]
}>()

const emit = defineEmits<{
  'sprint-click': [sprintId: number]
}>()
</script>

<template>
  <!-- Sprint Summary Table tooltip: One row per sprint with key cycle time metrics. Click a row to see full detail. -->
  <BaseCard v-if="summaries.length > 0">
    <div
      class="text-sm font-medium text-text-primary mb-1 cursor-help"
      title="One row per sprint with key cycle time metrics. Click a row to see full detail."
    >
      Sprint Summary
    </div>
    <p class="text-xs text-text-muted mb-3">Click a row to drill into a single sprint.</p>
    <table class="w-full text-sm">
      <thead>
        <tr class="text-text-muted text-left border-b border-border-default">
          <th class="pb-2 pr-4 font-medium">Sprint</th>
          <th class="pb-2 pr-4 font-medium text-right">Tickets</th>
          <th class="pb-2 pr-4 font-medium text-right">Median (days)</th>
          <th class="pb-2 pr-4 font-medium text-right">P85 (days)</th>
          <th class="pb-2 font-medium text-right">Outliers</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="summary in summaries"
          :key="summary.sprintId"
          class="border-b border-border-default last:border-0 cursor-pointer hover:bg-sidebar-item-hover transition-colors"
          @click="emit('sprint-click', summary.sprintId)"
        >
          <td class="py-2 pr-4 text-text-primary">{{ summary.sprintName }}</td>
          <td class="py-2 pr-4 text-right tabular-nums text-text-secondary">{{ summary.ticketsCompleted }}</td>
          <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ summary.medianCycleTime.toFixed(1) }}</td>
          <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ summary.p85CycleTime.toFixed(1) }}</td>
          <td class="py-2 text-right tabular-nums">
            <span
              v-if="summary.outlierCount > 0"
              class="bg-status-danger/10 text-status-danger text-xs rounded px-1.5 py-0.5"
            >{{ summary.outlierCount }}</span>
            <span v-else class="text-text-muted">0</span>
          </td>
        </tr>
      </tbody>
    </table>
  </BaseCard>
</template>
