<script setup lang="ts">
import { computed } from 'vue'
import type { SprintTrendEntry } from '../../types'

const props = defineProps<{
  sprintTrends: SprintTrendEntry[]
}>()

const xLabels = computed(() => props.sprintTrends.map(t => t.sprintName))

const series = computed(() => [
  {
    name: 'Feature SP',
    type: 'bar',
    data: props.sprintTrends.map(t => parseFloat(t.featureSp.toFixed(1)))
  },
  {
    name: 'Bug SP',
    type: 'bar',
    data: props.sprintTrends.map(t => parseFloat(t.bugSp.toFixed(1)))
  },
  {
    name: 'Completion % (all types)',
    type: 'line',
    data: props.sprintTrends.map(t => parseFloat(t.completionPercent.toFixed(1)))
  },
  {
    name: 'Rolling Avg SP',
    type: 'line',
    data: props.sprintTrends.map(t => t.rollingAverageSp !== null ? parseFloat(t.rollingAverageSp.toFixed(1)) : null)
  }
])

const chartOptions = computed(() => ({
  chart: {
    type: 'bar',
    stacked: true,
    background: 'transparent',
    toolbar: { show: false },
    animations: { enabled: false },
    zoom: { enabled: false }
  },
  plotOptions: {
    bar: { columnWidth: '60%' }
  },
  stroke: {
    width: [0, 0, 2, 2],
    dashArray: [0, 0, 0, 4],
    curve: 'straight'
  },
  colors: ['#3b82f6', '#ef4444', '#9ca3af', '#f59e0b'],
  fill: {
    type: ['solid', 'solid', 'solid', 'solid'],
    opacity: [1, 1, 1, 1]
  },
  xaxis: {
    categories: xLabels.value,
    labels: { style: { colors: '#9ca3af', fontSize: '11px' }, rotate: -30 },
    axisBorder: { show: false },
    axisTicks: { show: false }
  },
  yaxis: [
    {
      seriesName: 'Feature SP',
      title: { text: 'Story Points', style: { color: '#9ca3af' } },
      labels: { style: { colors: '#9ca3af', fontSize: '10px' } }
    },
    {
      seriesName: 'Bug SP',
      show: false
    },
    {
      seriesName: 'Completion % (all types)',
      opposite: true,
      min: 0,
      max: 100,
      title: { text: 'Completion %', style: { color: '#9ca3af' } },
      labels: {
        style: { colors: '#9ca3af', fontSize: '10px' },
        formatter: (v: number) => `${v.toFixed(0)}%`
      }
    },
    {
      seriesName: 'Rolling Avg SP',
      show: false
    }
  ],
  grid: { borderColor: '#374151', strokeDashArray: 3 },
  legend: {
    show: true,
    labels: { colors: '#9ca3af' }
  },
  theme: { mode: 'dark' },
  tooltip: {
    theme: 'dark',
    shared: true,
    intersect: false,
    custom: ({ dataPointIndex }: { seriesIndex: number; dataPointIndex: number }) => {
      const t = props.sprintTrends[dataPointIndex]
      if (!t) return ''
      return `<div style="background:#1f2937;border:1px solid #374151;border-radius:6px;padding:8px 12px;font-size:12px">
        <div style="color:#9ca3af;font-weight:500;margin-bottom:4px">${t.sprintName}</div>
        <div style="color:#3b82f6">Feature SP: <strong>${t.featureSp.toFixed(1)}</strong></div>
        <div style="color:#ef4444">Bug SP: <strong>${t.bugSp.toFixed(1)}</strong></div>
        <div style="color:#f9fafb">Total SP: <strong>${t.totalSp.toFixed(1)}</strong></div>
        <div style="color:#d1d5db">Completion: <strong>${t.completionPercent.toFixed(1)}%</strong> (all types)</div>
        <div style="color:#d1d5db">Capacity: <strong>${t.capacityPercent}%</strong></div>
        ${t.rollingAverageSp !== null ? `<div style="color:#f59e0b">Rolling Avg: <strong>${t.rollingAverageSp.toFixed(1)} SP</strong></div>` : ''}
      </div>`
    }
  }
}))
</script>

<template>
  <div>
    <h3 class="text-sm font-medium text-text-secondary mb-3">Sprint Trends</h3>
    <apexchart
      v-if="sprintTrends.length > 0"
      type="bar"
      height="300"
      :options="chartOptions"
      :series="series"
    />
    <div v-else class="text-sm text-text-muted">No sprint data available.</div>
  </div>
</template>
