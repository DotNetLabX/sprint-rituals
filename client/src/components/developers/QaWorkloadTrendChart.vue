<script setup lang="ts">
import { computed } from 'vue'
import type { QaWorkloadEntry, SprintSummaryItem } from '../../types'
import BaseCard from '../BaseCard.vue'

const props = defineProps<{
  sprints: SprintSummaryItem[]
  developers: QaWorkloadEntry[]
}>()

const trendSeries = computed(() =>
  props.developers.map(dev => ({
    name: dev.displayName,
    data: props.sprints.map(s => {
      const bd = dev.sprintBreakdowns.find(b => b.sprintId === s.id)
      return bd?.runsCompleted ?? 0
    })
  }))
)

const trendOptions = computed(() => ({
  chart: {
    type: 'line',
    background: 'transparent',
    toolbar: { show: false },
    animations: { enabled: false }
  },
  stroke: { curve: 'smooth', width: 2 },
  xaxis: {
    categories: props.sprints.map(s => s.name),
    labels: { style: { colors: '#9ca3af', fontSize: '12px' } }
  },
  yaxis: {
    labels: { style: { colors: '#9ca3af', fontSize: '12px' } },
    title: { text: 'Runs Completed', style: { color: '#9ca3af' } }
  },
  tooltip: { theme: 'dark' },
  legend: { labels: { colors: '#d1d5db' } },
  grid: { borderColor: '#374151' },
  theme: { mode: 'dark' }
}))
</script>

<template>
  <BaseCard>
    <div
      class="text-sm font-medium text-text-primary mb-4 cursor-help"
      title="Each person's completed test runs per sprint over time. Helps spot changes in testing capacity."
    >
      Execution Throughput Trend
    </div>
    <apexchart
      type="line"
      height="300"
      :options="trendOptions"
      :series="trendSeries"
    />
  </BaseCard>
</template>
