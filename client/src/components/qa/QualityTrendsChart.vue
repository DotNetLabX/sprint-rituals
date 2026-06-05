<script setup lang="ts">
import { computed } from 'vue'
import type { QualityTrendEntry } from '../../types'
import InfoTooltip from '../InfoTooltip.vue'

const props = defineProps<{
  qualityTrends: QualityTrendEntry[]
}>()

function ragColor(rag: string): string {
  switch (rag) {
    case 'green': return '#22c55e'
    case 'amber': return '#f59e0b'
    case 'red': return '#ef4444'
    default: return '#6b7280'
  }
}

// Build discrete markers for each series and data point
const discreteMarkers = computed(() => {
  const markers: Array<{ seriesIndex: number; dataPointIndex: number; fillColor: string; strokeColor: string; size: number }> = []

  props.qualityTrends.forEach((entry, dataPointIndex) => {
    // Series 0: Coverage Rate
    markers.push({
      seriesIndex: 0,
      dataPointIndex,
      fillColor: ragColor(entry.coverageRag),
      strokeColor: ragColor(entry.coverageRag),
      size: 5
    })
    // Series 1: Pass Rate
    markers.push({
      seriesIndex: 1,
      dataPointIndex,
      fillColor: ragColor(entry.passRateRag),
      strokeColor: ragColor(entry.passRateRag),
      size: 5
    })
    // Series 2: Execution Rate
    markers.push({
      seriesIndex: 2,
      dataPointIndex,
      fillColor: ragColor(entry.executionRag),
      strokeColor: ragColor(entry.executionRag),
      size: 5
    })
  })

  return markers
})

const series = computed(() => [
  {
    name: 'Coverage Rate',
    data: props.qualityTrends.map(e => e.coverageRate)
  },
  {
    name: 'Pass Rate',
    data: props.qualityTrends.map(e => e.passRate)
  },
  {
    name: 'Execution Rate',
    data: props.qualityTrends.map(e => e.executionRate)
  }
])

const options = computed(() => ({
  chart: {
    type: 'line',
    background: 'transparent',
    toolbar: { show: false },
    animations: { enabled: false }
  },
  colors: ['#3b82f6', '#22c55e', '#6b7280'],
  stroke: { curve: 'smooth', width: 2 },
  markers: {
    size: 0,
    discrete: discreteMarkers.value
  },
  xaxis: {
    categories: props.qualityTrends.map(e => e.sprintName),
    labels: { style: { colors: '#9ca3af', fontSize: '12px' } }
  },
  yaxis: {
    min: 0,
    max: 100,
    labels: {
      style: { colors: '#9ca3af', fontSize: '12px' },
      formatter: (val: number) => `${val.toFixed(0)}%`
    }
  },
  tooltip: {
    theme: 'dark',
    y: { formatter: (val: number | null) => val !== null ? `${val.toFixed(1)}%` : 'N/A' }
  },
  legend: { labels: { colors: '#d1d5db' } },
  grid: { borderColor: '#374151' },
  theme: { mode: 'dark' }
}))
</script>

<template>
  <div v-if="qualityTrends.length > 0">
    <div class="flex items-center gap-2 text-sm font-medium text-text-primary mb-4">
      <span>Quality Trends</span>
      <InfoTooltip text="Shows coverage rate, pass rate, and execution rate across sprints. Reveals whether testing quality is improving over time." />
    </div>
    <apexchart type="line" height="300" :options="options" :series="series" />
  </div>
</template>
