<script setup lang="ts">
import { computed } from 'vue'
import BaseCard from '../BaseCard.vue'
import type { CycleTimeTrendEntry } from '../../types'

const props = defineProps<{
  trend: CycleTimeTrendEntry[]
}>()

const sprintNames = computed(() => props.trend.map(t => t.sprintName))

const series = computed(() => [
  {
    name: 'P85 Cycle Time',
    data: props.trend.map(t => Math.round(t.p85CycleTime * 10) / 10)
  },
  {
    name: 'Median',
    data: props.trend.map(t => Math.round(t.medianCycleTime * 10) / 10)
  }
])

const options = computed(() => ({
  chart: {
    type: 'line',
    background: 'transparent',
    toolbar: { show: false },
    animations: { enabled: false }
  },
  stroke: { curve: 'smooth', width: [2, 1], dashArray: [0, 4] },
  colors: ['#f97316', '#9ca3af'],
  xaxis: {
    categories: sprintNames.value,
    labels: { style: { colors: '#9ca3af', fontSize: '11px' } }
  },
  yaxis: {
    labels: {
      style: { colors: '#9ca3af', fontSize: '11px' },
      formatter: (val: number) => `${val.toFixed(1)}d`
    },
    title: { text: 'Cycle Time (days)', style: { color: '#9ca3af' } }
  },
  tooltip: {
    theme: 'dark',
    y: { formatter: (val: number) => `${val.toFixed(1)} days` }
  },
  legend: { labels: { colors: '#d1d5db' } },
  grid: { borderColor: '#374151' },
  theme: { mode: 'dark' }
}))
</script>

<template>
  <!-- P85 Trend Line tooltip: Sprint-over-sprint P85 cycle time. A downward trend means the team is getting faster. -->
  <BaseCard v-if="trend.length > 0">
    <div
      class="text-sm font-medium text-text-primary mb-1 cursor-help"
      title="Sprint-over-sprint P85 cycle time. A downward trend means the team is getting faster."
    >
      P85 Cycle Time Trend
    </div>
    <p class="text-xs text-text-muted mb-3">Solid = P85, dashed = median. A downward trend means the team is getting faster.</p>
    <apexchart
      type="line"
      height="220"
      :options="options"
      :series="series"
    />
  </BaseCard>
</template>
