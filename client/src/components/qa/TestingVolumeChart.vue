<script setup lang="ts">
import { computed } from 'vue'
import type { TestingVolumeEntry } from '../../types'
import InfoTooltip from '../InfoTooltip.vue'

const props = defineProps<{
  testingVolume: TestingVolumeEntry[]
}>()

const series = computed(() => [
  {
    name: 'TE Count',
    data: props.testingVolume.map(e => e.teCount)
  },
  {
    name: 'Bugs Found',
    data: props.testingVolume.map(e => e.bugsFound)
  }
])

const options = computed(() => ({
  chart: {
    type: 'bar',
    background: 'transparent',
    toolbar: { show: false },
    animations: { enabled: false }
  },
  colors: ['#3b82f6', '#ef4444'],
  plotOptions: {
    bar: {
      columnWidth: '60%',
      grouped: true
    }
  },
  xaxis: {
    categories: props.testingVolume.map(e => e.sprintName),
    labels: { style: { colors: '#9ca3af', fontSize: '12px' } }
  },
  yaxis: {
    labels: {
      style: { colors: '#9ca3af', fontSize: '12px' },
      formatter: (val: number) => String(Math.round(val))
    }
  },
  tooltip: {
    theme: 'dark',
    y: { formatter: (val: number) => String(Math.round(val)) }
  },
  legend: { labels: { colors: '#d1d5db' } },
  grid: { borderColor: '#374151' },
  theme: { mode: 'dark' }
}))
</script>

<template>
  <div v-if="testingVolume.length > 0">
    <div class="flex items-center gap-2 text-sm font-medium text-text-primary mb-4">
      <span>Testing Volume</span>
      <InfoTooltip text="Total test executions and bugs found per sprint. Shows whether testing effort is growing or shrinking." />
    </div>
    <apexchart type="bar" height="260" :options="options" :series="series" />
  </div>
</template>
