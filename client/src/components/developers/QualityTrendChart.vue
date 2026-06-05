<script setup lang="ts">
import { computed } from 'vue'
import type { DeveloperQualityEntry, DeveloperQualitySprintInfo } from '../../types'
import BaseCard from '../BaseCard.vue'

const props = defineProps<{
  developers: DeveloperQualityEntry[]
  sprints: DeveloperQualitySprintInfo[]
}>()

const series = computed(() =>
  props.developers.map(dev => ({
    name: dev.displayName,
    data: props.sprints.map(sprint => {
      const bd = dev.sprintBreakdowns.find(b => b.sprintId === sprint.id)
      return bd ? bd.coveragePercent : null
    })
  }))
)

const options = computed(() => ({
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
    min: 0,
    max: 100,
    labels: {
      style: { colors: '#9ca3af', fontSize: '12px' },
      formatter: (val: number) => `${val.toFixed(0)}%`
    },
    title: { text: 'Coverage %', style: { color: '#9ca3af' } }
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
  <BaseCard v-if="sprints.length > 0 && developers.length > 0">
    <div
      class="text-sm font-medium text-text-primary mb-4 cursor-help"
      title="Coverage % per developer across sprints. Shows whether story test coverage is improving."
    >Coverage % Trend</div>
    <apexchart type="line" height="300" :options="options" :series="series" />
  </BaseCard>
</template>
