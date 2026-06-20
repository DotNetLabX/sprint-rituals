<script setup lang="ts">
import { computed } from 'vue'
import BaseCard from '../BaseCard.vue'
import type { CarryOverStatusDistributionEntry } from '../../types'

const props = defineProps<{
  distribution: CarryOverStatusDistributionEntry[]
}>()

const labels = computed(() => props.distribution.map(e => e.stageName))
const series = computed(() => props.distribution.map(e => e.ticketCount))

const options = computed(() => ({
  chart: {
    type: 'donut',
    background: 'transparent',
    animations: { enabled: false }
  },
  labels: labels.value,
  tooltip: {
    theme: 'dark',
    y: {
      formatter: (val: number, opts: { seriesIndex: number }) => {
        const entry = props.distribution[opts.seriesIndex]
        return `${val} tickets · ${entry.spTotal.toFixed(1)} SP`
      }
    }
  },
  legend: {
    labels: { colors: '#d1d5db' },
    position: 'bottom'
  },
  dataLabels: {
    style: { colors: ['#fff'] }
  },
  theme: { mode: 'dark' }
}))
</script>

<template>
  <BaseCard>
    <div class="text-sm font-medium text-text-primary mb-4 cursor-help" title="Proportional breakdown of where carry-over tickets are stuck by workflow phase.">Status Distribution</div>
    <div v-if="distribution.length === 0" class="text-sm text-text-muted">No carry-over tickets.</div>
    <apexchart
      v-else
      type="donut"
      height="280"
      :options="options"
      :series="series"
    />
  </BaseCard>
</template>
