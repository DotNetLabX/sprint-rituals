<script setup lang="ts">
import { computed } from 'vue'
import type { BugRatioMultiSprintResponse } from '../../types'
import BaseCard from '../BaseCard.vue'

const props = defineProps<{
  multi: BugRatioMultiSprintResponse
}>()

const series = computed(() => [
  {
    name: 'Bug Ratio %',
    data: props.multi.teamMetrics.perSprintTrend.map(t => t.bugRatioPercent)
  }
])

const options = computed(() => ({
  chart: {
    type: 'line',
    background: 'transparent',
    toolbar: { show: false },
    animations: { enabled: false }
  },
  stroke: { curve: 'smooth', width: 2 },
  colors: ['#f59e0b'],
  xaxis: {
    categories: props.multi.sprints.map(s => s.name),
    labels: { style: { colors: '#9ca3af', fontSize: '12px' } }
  },
  yaxis: {
    min: 0,
    max: 100,
    labels: {
      style: { colors: '#9ca3af', fontSize: '12px' },
      formatter: (val: number) => `${val.toFixed(0)}%`
    },
    title: { text: 'Bug Ratio %', style: { color: '#9ca3af' } }
  },
  tooltip: {
    theme: 'dark',
    y: { formatter: (val: number) => `${val.toFixed(1)}%` }
  },
  legend: { labels: { colors: '#d1d5db' } },
  grid: { borderColor: '#374151' },
  theme: { mode: 'dark' }
}))
</script>

<template>
  <BaseCard v-if="multi.teamMetrics.perSprintTrend.length > 0">
    <div class="text-sm font-medium text-text-primary mb-4 cursor-help" title="Bug ratio percentage per sprint over time. Spot sustained increases before they become the norm.">Bug Ratio Trend</div>
    <apexchart type="line" height="260" :options="options" :series="series" />
  </BaseCard>
</template>
