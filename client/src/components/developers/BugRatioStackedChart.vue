<script setup lang="ts">
import { computed } from 'vue'
import type { BugRatioMultiSprintResponse } from '../../types'
import BaseCard from '../BaseCard.vue'

const props = defineProps<{
  multi: BugRatioMultiSprintResponse
}>()

const series = computed(() => {
  const developers = props.multi.developers
  return [
    {
      name: 'Bug SP',
      data: developers.map(dev => {
        const bySprintId: Record<number, number> = {}
        dev.sprintBreakdowns.forEach(b => { bySprintId[b.sprintId] = b.bugSp })
        return props.multi.sprints.map(s => bySprintId[s.id] ?? 0)
      }).reduce((acc: number[], devData) => {
        devData.forEach((v, i) => { acc[i] = (acc[i] ?? 0) + v })
        return acc
      }, [])
    },
    {
      name: 'Non-Bug SP',
      data: developers.map(dev => {
        const bySprintId: Record<number, number> = {}
        dev.sprintBreakdowns.forEach(b => { bySprintId[b.sprintId] = b.nonBugSp })
        return props.multi.sprints.map(s => bySprintId[s.id] ?? 0)
      }).reduce((acc: number[], devData) => {
        devData.forEach((v, i) => { acc[i] = (acc[i] ?? 0) + v })
        return acc
      }, [])
    }
  ]
})

const options = computed(() => ({
  chart: {
    type: 'bar',
    stacked: true,
    background: 'transparent',
    toolbar: { show: false },
    animations: { enabled: false }
  },
  colors: ['#ef4444', '#3b82f6'],
  xaxis: {
    categories: props.multi.sprints.map(s => s.name),
    labels: { style: { colors: '#9ca3af', fontSize: '12px' } }
  },
  yaxis: {
    labels: {
      style: { colors: '#9ca3af', fontSize: '12px' },
      formatter: (val: number) => val.toFixed(1)
    },
    title: { text: 'Story Points', style: { color: '#9ca3af' } }
  },
  tooltip: {
    theme: 'dark',
    y: { formatter: (val: number) => val.toFixed(1) }
  },
  legend: { labels: { colors: '#d1d5db' } },
  grid: { borderColor: '#374151' },
  theme: { mode: 'dark' },
  plotOptions: { bar: { columnWidth: '60%' } }
}))
</script>

<template>
  <BaseCard v-if="multi.sprints.length > 0">
    <div class="text-sm font-medium text-text-primary mb-4 cursor-help" title="Bug SP (red) vs. Non-Bug SP (blue) per sprint for each developer. Compare allocation patterns.">Bug SP vs Non-Bug SP per Sprint</div>
    <apexchart type="bar" height="260" :options="options" :series="series" />
  </BaseCard>
</template>
