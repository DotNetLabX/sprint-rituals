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

const series = computed(() => {
  // Collect all stage names across all sprints
  const stageNames = Array.from(
    new Set(
      props.perSprintData.flatMap(d => d.statusDistribution.map(e => e.stageName))
    )
  )

  return stageNames.map(stage => ({
    name: stage,
    data: props.perSprintData.map(d => {
      const entry = d.statusDistribution.find(e => e.stageName === stage)
      return entry ? Math.round(entry.spTotal * 10) / 10 : 0
    })
  }))
})

const options = computed(() => ({
  chart: {
    type: 'bar',
    stacked: true,
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
    bar: { columnWidth: '60%' }
  },
  xaxis: {
    categories: sprintNames.value,
    labels: { style: { colors: '#9ca3af', fontSize: '12px' } }
  },
  yaxis: {
    labels: { style: { colors: '#9ca3af', fontSize: '12px' } },
    title: { text: 'Carry-Over SP', style: { color: '#9ca3af' } }
  },
  tooltip: {
    theme: 'dark'
  },
  legend: { labels: { colors: '#d1d5db' } },
  grid: { borderColor: '#374151' },
  theme: { mode: 'dark' }
}))
</script>

<template>
  <BaseCard>
    <div class="text-sm font-medium text-text-primary mb-4 cursor-help" title="Where unfinished work is stuck — which workflow phase accumulates the most carry-over.">Carry-Over SP by Workflow Stage</div>
    <p class="text-xs text-text-muted mb-3">Click a bar to drill into a single sprint.</p>
    <apexchart
      type="bar"
      height="260"
      :options="options"
      :series="series"
    />
  </BaseCard>
</template>
