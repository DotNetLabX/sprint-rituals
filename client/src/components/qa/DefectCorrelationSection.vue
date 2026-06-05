<script setup lang="ts">
import { computed } from 'vue'
import type { DefectCorrelationResult } from '../../types'
import InfoTooltip from '../InfoTooltip.vue'

const props = defineProps<{
  defectCorrelation: DefectCorrelationResult | null
  hasQaData: boolean
}>()

// Coverage panel: all data points in order
const coverageSeries = computed(() => [
  {
    name: 'Coverage Rate',
    data: (props.defectCorrelation?.dataPoints ?? []).map(dp => dp.coverageRate)
  }
])

const coverageCategories = computed(() =>
  (props.defectCorrelation?.dataPoints ?? []).map(dp => dp.sprintName)
)

// Bug ratio panel: only points with a next-sprint bug ratio value
// X-axis uses nextSprintName to show the N+1 offset
const bugRatioPairs = computed(() =>
  (props.defectCorrelation?.dataPoints ?? []).filter(dp => dp.nextSprintBugRatio !== null)
)

const bugRatioSeries = computed(() => [
  {
    name: 'Bug Ratio (next sprint)',
    data: bugRatioPairs.value.map(dp => dp.nextSprintBugRatio as number)
  }
])

const bugRatioCategories = computed(() =>
  bugRatioPairs.value.map(dp => dp.nextSprintName ?? dp.sprintName)
)

const coverageOptions = computed(() => ({
  chart: {
    id: 'coverage-panel',
    type: 'line',
    background: 'transparent',
    toolbar: { show: false },
    animations: { enabled: false }
  },
  colors: ['#3b82f6'],
  stroke: { curve: 'smooth', width: 2 },
  markers: { size: 4 },
  xaxis: {
    categories: coverageCategories.value,
    labels: { style: { colors: '#9ca3af', fontSize: '11px' } }
  },
  yaxis: {
    min: 0,
    max: 100,
    labels: {
      style: { colors: '#9ca3af', fontSize: '11px' },
      formatter: (val: number) => `${val.toFixed(0)}%`
    },
    title: { text: 'Coverage %', style: { color: '#9ca3af', fontSize: '11px' } }
  },
  tooltip: {
    theme: 'dark',
    y: { formatter: (val: number) => `${val.toFixed(1)}%` }
  },
  legend: { show: false },
  grid: { borderColor: '#374151' },
  theme: { mode: 'dark' }
}))

const bugRatioOptions = computed(() => ({
  chart: {
    id: 'bug-ratio-panel',
    type: 'line',
    background: 'transparent',
    toolbar: { show: false },
    animations: { enabled: false }
  },
  colors: ['#ef4444'],
  stroke: { curve: 'smooth', width: 2 },
  markers: { size: 4 },
  xaxis: {
    categories: bugRatioCategories.value,
    labels: { style: { colors: '#9ca3af', fontSize: '11px' } }
  },
  yaxis: {
    min: 0,
    labels: {
      style: { colors: '#9ca3af', fontSize: '11px' },
      formatter: (val: number) => `${val.toFixed(1)}%`
    },
    title: { text: 'Bug Ratio %', style: { color: '#9ca3af', fontSize: '11px' } }
  },
  tooltip: {
    theme: 'dark',
    y: { formatter: (val: number) => `${val.toFixed(1)}%` }
  },
  legend: { show: false },
  grid: { borderColor: '#374151' },
  theme: { mode: 'dark' }
}))

function pearsonRBgClass(r: number): string {
  if (r < -0.3) return 'bg-status-success text-white'
  if (r <= 0.3) return 'bg-status-warning text-white'
  return 'bg-status-danger text-white'
}

const emptyMessage = computed(() => {
  if (!props.defectCorrelation || props.defectCorrelation.dataPointCount === 0) {
    return 'No bug ratio data available for correlation'
  }
  if (props.defectCorrelation.pearsonR === null && props.defectCorrelation.dataPointCount > 0 && props.defectCorrelation.dataPointCount < 6) {
    return 'Need 6+ sprints with both QA and bug data for correlation analysis'
  }
  return null
})
</script>

<template>
  <div>
    <!-- Section header -->
    <div class="flex items-center gap-2 text-sm font-medium text-text-primary mb-4">
      <span>Defect Correlation</span>
      <InfoTooltip text="Compares test coverage in one sprint against the bug ratio in the next sprint (N+1 lag). Lower coverage often predicts more bugs." />

      <!-- Pearson r badge -->
      <template v-if="defectCorrelation?.pearsonR !== null && defectCorrelation?.pearsonR !== undefined">
        <span
          :class="['ml-2 inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold cursor-help', pearsonRBgClass(defectCorrelation.pearsonR)]"
          :title="'Correlation strength between coverage and next-sprint bugs. Negative = higher coverage predicts fewer bugs (healthy).'"
        >
          r = {{ defectCorrelation.pearsonR.toFixed(2) }}
        </span>
        <InfoTooltip text="Correlation strength between coverage and next-sprint bugs. Negative = higher coverage predicts fewer bugs (healthy)." />
      </template>
    </div>

    <!-- Empty states -->
    <template v-if="emptyMessage">
      <div class="text-sm text-text-muted py-4">{{ emptyMessage }}</div>
    </template>

    <!-- Dual panel charts -->
    <template v-else-if="defectCorrelation && defectCorrelation.dataPoints.length > 0">
      <div class="text-xs text-text-muted mb-2 italic">
        Coverage (sprint N) paired with Bug Ratio (sprint N+1) — dashed alignment shows the lag
      </div>

      <!-- Coverage panel -->
      <div class="mb-1">
        <div class="text-xs text-text-secondary mb-1">Coverage Rate (sprint N)</div>
        <apexchart type="line" height="180" :options="coverageOptions" :series="coverageSeries" />
      </div>

      <!-- Visual connector: dashed divider with arrow hint -->
      <div class="flex items-center gap-2 my-2 px-1">
        <div class="flex-1 border-t border-dashed border-border-default" />
        <span class="text-xs text-text-muted shrink-0">N+1 lag ↓</span>
        <div class="flex-1 border-t border-dashed border-border-default" />
      </div>

      <!-- Bug ratio panel -->
      <div>
        <div class="text-xs text-text-secondary mb-1">Bug Ratio (sprint N+1)</div>
        <apexchart type="line" height="180" :options="bugRatioOptions" :series="bugRatioSeries" />
      </div>
    </template>
  </div>
</template>
