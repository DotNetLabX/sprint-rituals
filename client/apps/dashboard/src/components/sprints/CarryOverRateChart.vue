<script setup lang="ts">
import { computed } from 'vue'
import BaseCard from '../BaseCard.vue'
import type { CarryOverSprintInfo, CarryOverPerSprintData } from '../../types'

const props = defineProps<{
  sprints: CarryOverSprintInfo[]
  perSprintData: CarryOverPerSprintData[]
}>()

const emit = defineEmits<{
  'sprint-click': [sprintId: number]
}>()

const sprintNames = computed(() => props.sprints.map(s => s.name))

const series = computed(() => [
  {
    name: 'Carry-Over Rate %',
    data: props.perSprintData.map(d => Math.round(d.carryOverRate * 10) / 10)
  }
])

const options = computed(() => ({
  chart: {
    type: 'line',
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
  stroke: { curve: 'smooth', width: 2 },
  colors: ['#f97316'],
  xaxis: {
    categories: sprintNames.value,
    labels: { style: { colors: '#9ca3af', fontSize: '12px' } }
  },
  yaxis: {
    labels: {
      style: { colors: '#9ca3af', fontSize: '12px' },
      formatter: (val: number) => `${val.toFixed(1)}%`
    },
    title: { text: 'Carry-Over Rate %', style: { color: '#9ca3af' } }
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
    <div class="text-sm font-medium text-text-primary mb-4 cursor-help" title="How carry-over rate changes across sprints — a rising line signals growing delivery problems.">Carry-Over Rate Trend</div>
    <p class="text-xs text-text-muted mb-3">Click a point to drill into a single sprint.</p>
    <apexchart
      type="line"
      height="220"
      :options="options"
      :series="series"
    />
  </BaseCard>
</template>
