<script setup lang="ts">
import { computed } from 'vue'
import BaseCard from '../BaseCard.vue'
import type { ScopeChangePerSprintData, ScopeChangeSprintInfo } from '../../types'

const props = defineProps<{
  sprints: ScopeChangeSprintInfo[]
  perSprintData: ScopeChangePerSprintData[]
}>()

const emit = defineEmits<{
  'sprint-click': [sprintId: number]
}>()

const sprintNames = computed(() => props.sprints.map(s => s.name))

const barSeries = computed(() => [
  {
    name: 'Committed SP',
    data: props.perSprintData.map(d => d.committedSpActive)
  },
  {
    name: 'Added SP',
    data: props.perSprintData.map(d => d.addedSp)
  },
  {
    name: 'Removed SP',
    data: props.perSprintData.map(d => d.removedSp)
  },
  {
    name: 'Completed SP',
    data: props.perSprintData.map(d => d.completedSp)
  }
])

const lineSeries = computed(() => [
  {
    name: 'Disruption Rate %',
    type: 'line',
    data: props.perSprintData.map(d => Math.round(d.disruptionRate * 10) / 10)
  }
])

const barOptions = computed(() => ({
  chart: {
    type: 'bar',
    background: 'transparent',
    toolbar: { show: false },
    animations: { enabled: false },
    events: {
      dataPointSelection: (_e: Event, _chart: unknown, config: { dataPointIndex: number }) => {
        const sprint = props.sprints[config.dataPointIndex]
        if (sprint) emit('sprint-click', sprint.id)
      }
    }
  },
  plotOptions: {
    bar: { grouped: true, columnWidth: '60%' }
  },
  colors: ['#6b7280', '#f97316', '#ef4444', '#22c55e'],
  xaxis: {
    categories: sprintNames.value,
    labels: { style: { colors: '#9ca3af', fontSize: '12px' } }
  },
  yaxis: {
    labels: { style: { colors: '#9ca3af', fontSize: '12px' } },
    title: { text: 'Story Points', style: { color: '#9ca3af' } }
  },
  tooltip: {
    theme: 'dark'
  },
  legend: { labels: { colors: '#d1d5db' } },
  grid: { borderColor: '#374151' },
  theme: { mode: 'dark' }
}))

const lineOptions = computed(() => ({
  chart: {
    type: 'line',
    background: 'transparent',
    toolbar: { show: false },
    animations: { enabled: false }
  },
  stroke: { curve: 'smooth', width: 2 },
  colors: ['#a78bfa'],
  xaxis: {
    categories: sprintNames.value,
    labels: { style: { colors: '#9ca3af', fontSize: '12px' } }
  },
  yaxis: {
    labels: {
      style: { colors: '#9ca3af', fontSize: '12px' },
      formatter: (val: number) => `${val.toFixed(1)}%`
    },
    title: { text: 'Disruption Rate %', style: { color: '#9ca3af' } }
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
  <BaseCard>
    <div class="text-sm font-medium text-text-primary mb-4 cursor-help" title="Per-sprint bars showing committed, added, removed, and completed SP side by side.">Scope Change by Sprint</div>
    <p class="text-xs text-text-muted mb-3">Click a bar to drill into a single sprint.</p>
    <apexchart
      type="bar"
      height="280"
      :options="barOptions"
      :series="barSeries"
    />
    <div class="text-sm font-medium text-text-primary mt-6 mb-4 cursor-help" title="Disruption rate percentage per sprint over time. Spot sustained increases early.">Disruption Rate Trend</div>
    <apexchart
      type="line"
      height="180"
      :options="lineOptions"
      :series="lineSeries"
    />
  </BaseCard>
</template>
