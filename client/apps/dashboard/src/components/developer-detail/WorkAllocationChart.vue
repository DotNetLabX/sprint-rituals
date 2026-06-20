<script setup lang="ts">
import { computed } from 'vue'
import type { SprintTrendEntry, WorkAllocationSummary } from '../../types'

const props = defineProps<{
  sprintTrends: SprintTrendEntry[]
  workAllocation: WorkAllocationSummary
  bugRatioTarget: number
}>()

const xLabels = computed(() => props.sprintTrends.map(t => t.sprintName))

const series = computed(() => [
  {
    name: 'Bug %',
    data: props.sprintTrends.map(t => parseFloat(t.bugPercent.toFixed(1)))
  }
])

const chartOptions = computed(() => ({
  chart: {
    type: 'line',
    background: 'transparent',
    toolbar: { show: false },
    animations: { enabled: false },
    zoom: { enabled: false }
  },
  stroke: {
    curve: 'straight',
    width: 2
  },
  colors: ['#f59e0b'],
  markers: {
    size: 4,
    colors: props.sprintTrends.map(t =>
      t.bugPercent > props.bugRatioTarget ? '#ef4444' : '#f59e0b'
    ),
    strokeColors: props.sprintTrends.map(t =>
      t.bugPercent > props.bugRatioTarget ? '#ef4444' : '#f59e0b'
    )
  },
  xaxis: {
    categories: xLabels.value,
    labels: { style: { colors: '#9ca3af', fontSize: '11px' }, rotate: -30 },
    axisBorder: { show: false },
    axisTicks: { show: false }
  },
  yaxis: {
    min: 0,
    max: 100,
    labels: {
      style: { colors: '#9ca3af', fontSize: '10px' },
      formatter: (v: number) => `${v.toFixed(0)}%`
    }
  },
  annotations: {
    yaxis: [
      {
        y: props.bugRatioTarget,
        borderColor: '#ef4444',
        strokeDashArray: 4,
        label: {
          text: `Target ${props.bugRatioTarget}%`,
          style: { color: '#ef4444', background: 'transparent', fontSize: '11px' }
        }
      }
    ]
  },
  grid: { borderColor: '#374151', strokeDashArray: 3 },
  legend: { show: false },
  theme: { mode: 'dark' },
  tooltip: {
    theme: 'dark',
    custom: ({ dataPointIndex }: { seriesIndex: number; dataPointIndex: number }) => {
      const t = props.sprintTrends[dataPointIndex]
      if (!t) return ''
      const aboveTarget = t.bugPercent > props.bugRatioTarget
      return `<div style="background:#1f2937;border:1px solid #374151;border-radius:6px;padding:8px 12px;font-size:12px">
        <div style="color:#9ca3af;font-weight:500;margin-bottom:4px">${t.sprintName}</div>
        <div style="color:#3b82f6">Feature SP: <strong>${t.featureSp.toFixed(1)}</strong></div>
        <div style="color:#ef4444">Bug SP: <strong>${t.bugSp.toFixed(1)}</strong></div>
        <div style="color:${aboveTarget ? '#ef4444' : '#f59e0b'}">Bug %: <strong>${t.bugPercent.toFixed(1)}%</strong>${aboveTarget ? ' ▲ above target' : ''}</div>
        <div style="color:#9ca3af">Target: <strong>${props.bugRatioTarget}%</strong></div>
      </div>`
    }
  }
}))
</script>

<template>
  <div>
    <h3 class="text-sm font-medium text-text-secondary mb-3">Work Allocation — Bug %</h3>
    <apexchart
      v-if="sprintTrends.length > 0"
      type="line"
      height="220"
      :options="chartOptions"
      :series="series"
    />
    <div v-else class="text-sm text-text-muted">No sprint data available.</div>
    <!-- Summary line -->
    <div class="mt-2 text-xs text-text-muted">
      Average bug %: <span class="text-text-primary font-medium">{{ workAllocation.averageBugPercent.toFixed(1) }}%</span>
      &nbsp;&middot;&nbsp;
      <span :class="workAllocation.sprintsAboveTarget > 0 ? 'text-status-danger' : 'text-text-muted'">
        {{ workAllocation.sprintsAboveTarget }} of {{ workAllocation.totalSprints }} sprints above target
      </span>
    </div>
  </div>
</template>
