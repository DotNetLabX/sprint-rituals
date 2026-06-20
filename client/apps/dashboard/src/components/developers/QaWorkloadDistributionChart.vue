<script setup lang="ts">
import { computed } from 'vue'
import type { QaWorkloadEntry, QaWorkloadSingleEntry } from '../../types'
import BaseCard from '../BaseCard.vue'

const props = defineProps<{
  developers: (QaWorkloadEntry | QaWorkloadSingleEntry)[]
}>()

const sortedDevs = computed(() =>
  [...props.developers].sort((a, b) => {
    if (a.accountId === null) return 1
    if (b.accountId === null) return -1
    return b.runsCompleted - a.runsCompleted
  })
)

const distributionSeries = computed(() => {
  if (sortedDevs.value.length === 0) return []
  return [
    { name: 'Pass', data: sortedDevs.value.map(d => d.passCount) },
    { name: 'Fail', data: sortedDevs.value.map(d => d.failCount) }
  ]
})

const distributionCategories = computed(() => sortedDevs.value.map(d => d.displayName))

const distributionOptions = computed(() => ({
  chart: {
    type: 'bar',
    stacked: true,
    background: 'transparent',
    toolbar: { show: false },
    animations: { enabled: false }
  },
  plotOptions: { bar: { horizontal: true } },
  colors: ['#22c55e', '#ef4444'],
  xaxis: {
    labels: { style: { colors: '#9ca3af', fontSize: '12px' } },
    title: { text: 'Run Count', style: { color: '#9ca3af' } }
  },
  yaxis: {
    categories: distributionCategories.value,
    labels: { style: { colors: '#9ca3af', fontSize: '12px' } }
  },
  tooltip: { theme: 'dark' },
  legend: { labels: { colors: '#d1d5db' } },
  grid: { borderColor: '#374151' },
  theme: { mode: 'dark' }
}))
</script>

<template>
  <BaseCard>
    <div
      class="text-sm font-medium text-text-primary mb-4 cursor-help"
      title="Shows how test execution volume is split across team members, broken down by pass and fail results."
    >
      Workload Distribution
    </div>
    <apexchart
      type="bar"
      height="300"
      :options="distributionOptions"
      :series="distributionSeries"
    />
  </BaseCard>
</template>
