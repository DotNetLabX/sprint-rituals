<script setup lang="ts">
import type { MetricCard } from '../../types'
import { computed } from 'vue'
import InfoTooltip from '../InfoTooltip.vue'

const props = defineProps<{
  metric: MetricCard
  annotation?: string
}>()

function metricTooltip(name: string): string {
  if (name === 'SP Completed') return 'Feature story points completed this sprint. Bug SP shown separately below.'
  if (name === 'Completion %') return 'Percentage of committed story points completed. Higher is better.'
  if (name === 'Scope Disruption Rate') return 'Non-bug work added mid-sprint as % of committed SP. Lower is better.'
  if (name === 'Bug Disruption Rate') return 'Bug work added mid-sprint as % of committed SP. Lower is better.'
  if (name === 'Carry-Over Rate') return 'Percentage of total sprint scope (committed + added) not completed. Lower is better.'
  if (name === 'Coverage Rate') return 'Percentage of feature tickets with at least one linked test execution. Higher means broader test protection.'
  if (name === 'Execution Rate') return 'Of covered tickets, percentage where tests have been run to completion (PASS or FAIL verdict).'
  if (name === 'Pass Rate') return 'Percentage of executed test runs that passed, across all test executions in this sprint.'
  return ''
}

const deltaArrow = computed(() => {
  switch (props.metric.deltaDirection) {
    case 'up': return '↑'
    case 'down': return '↓'
    default: return '—'
  }
})

const deltaClass = computed(() => {
  switch (props.metric.deltaPolarity) {
    case 'positive': return 'text-status-success'
    case 'negative': return 'text-status-danger'
    default: return 'text-text-secondary'
  }
})

const sparklineOptions = computed(() => ({
  chart: {
    type: 'line',
    sparkline: { enabled: true },
    animations: { enabled: false }
  },
  stroke: { curve: 'smooth', width: 2 },
  colors: ['#4f7cff'],
  tooltip: { enabled: false }
}))

const sparklineSeries = computed(() => [
  {
    name: props.metric.name,
    data: props.metric.sparkline.map(p => p.value)
  }
])
</script>

<template>
  <div
    :class="[
      'bg-surface-card border rounded-lg p-4 flex flex-col gap-3',
      metric.rag === 'green' ? 'border-status-success' :
      metric.rag === 'amber' ? 'border-status-warning' :
      metric.rag === 'red'   ? 'border-status-danger' :
      'border-border-default'
    ]"
  >
    <div class="flex items-center gap-1 text-xs text-text-muted uppercase tracking-wide">
      {{ metric.name }}
      <InfoTooltip v-if="metricTooltip(metric.name)" :text="metricTooltip(metric.name)" />
    </div>
    <div
      :class="[
        'text-2xl font-bold tabular-nums',
        metric.rag === 'green' ? 'text-status-success' :
        metric.rag === 'amber' ? 'text-status-warning' :
        metric.rag === 'red'   ? 'text-status-danger' :
        'text-text-primary'
      ]"
    >{{ metric.displayValue }}</div>
    <div v-if="annotation" class="text-xs text-text-muted">{{ annotation }}</div>
    <div class="flex items-center justify-between gap-2">
      <div v-if="metric.delta !== null" :class="['flex items-center gap-1 text-sm font-medium', deltaClass]">
        {{ deltaArrow }} {{ Math.abs(metric.delta) }}
        <InfoTooltip text="Change versus the prior closed sprint. Arrow direction and color show whether the metric improved." />
      </div>
      <div v-else class="text-sm text-text-muted">—</div>
      <div v-if="metric.sparkline.length > 1" class="flex items-center gap-1">
        <div class="w-24 h-8">
          <apexchart
            type="line"
            height="32"
            :options="sparklineOptions"
            :series="sparklineSeries"
          />
        </div>
        <InfoTooltip text="Trend of the last 4 sprints ending at the selected sprint." />
      </div>
    </div>
  </div>
</template>
