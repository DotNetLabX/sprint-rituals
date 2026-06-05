<script setup lang="ts">
import { computed } from 'vue'
import BaseCard from '../BaseCard.vue'
import type { CycleTimeScatterPoint } from '../../types'

const props = defineProps<{
  dataPoints: CycleTimeScatterPoint[]
  selectedPercentile?: number
}>()

const issueTypeColors: Record<string, string> = {
  'Story': '#3b82f6',
  'Bug': '#ef4444',
  'Task': '#8b5cf6',
  'Subtask': '#f59e0b',
  'Epic': '#10b981',
}

function colorForType(type: string): string {
  return issueTypeColors[type] ?? '#6b7280'
}

const p50 = computed(() => {
  if (props.dataPoints.length === 0) return 0
  const sorted = [...props.dataPoints].map(p => p.cycleTimeDays).sort((a, b) => a - b)
  return percentile(sorted, 0.5)
})

const pSelected = computed(() => {
  if (props.dataPoints.length === 0) return 0
  const sorted = [...props.dataPoints].map(p => p.cycleTimeDays).sort((a, b) => a - b)
  const p = (props.selectedPercentile ?? 85) / 100
  return percentile(sorted, p)
})

function percentile(sorted: number[], p: number): number {
  if (sorted.length === 1) return sorted[0]
  const index = p * (sorted.length - 1)
  const lower = Math.floor(index)
  const upper = Math.ceil(index)
  if (lower === upper) return sorted[lower]
  return sorted[lower] + (index - lower) * (sorted[upper] - sorted[lower])
}

// Build series: one series per issue type for color grouping
const seriesData = computed(() => {
  const byType = new Map<string, CycleTimeScatterPoint[]>()
  for (const pt of props.dataPoints) {
    const list = byType.get(pt.issueType) ?? []
    list.push(pt)
    byType.set(pt.issueType, list)
  }

  return Array.from(byType.entries()).map(([type, points]) => ({
    name: type,
    data: points.map(pt => ({
      x: new Date(pt.completionDate).getTime(),
      y: pt.cycleTimeDays,
      meta: pt
    }))
  }))
})

const colors = computed(() =>
  seriesData.value.map(s => colorForType(s.name))
)

const options = computed(() => ({
  chart: {
    type: 'scatter',
    background: 'transparent',
    toolbar: { show: false },
    animations: { enabled: false },
    zoom: { enabled: false }
  },
  colors: colors.value,
  xaxis: {
    type: 'datetime',
    labels: { style: { colors: '#9ca3af', fontSize: '11px' }, datetimeUTC: false },
    title: { text: 'Completion Date', style: { color: '#9ca3af' } }
  },
  yaxis: {
    labels: {
      style: { colors: '#9ca3af', fontSize: '11px' },
      formatter: (val: number) => `${val.toFixed(1)}d`
    },
    title: { text: 'Cycle Time (days)', style: { color: '#9ca3af' } }
  },
  annotations: {
    yaxis: [
      {
        y: p50.value,
        borderColor: '#9ca3af',
        borderWidth: 1,
        strokeDashArray: 4,
        label: { text: 'P50', style: { color: '#9ca3af', background: 'transparent', fontSize: '11px' } }
      },
      {
        y: pSelected.value,
        borderColor: '#f97316',
        borderWidth: 2,
        strokeDashArray: 0,
        label: {
          text: `P${props.selectedPercentile ?? 85}`,
          style: { color: '#f97316', background: 'transparent', fontSize: '11px' }
        }
      }
    ]
  },
  tooltip: {
    theme: 'dark',
    custom: ({ seriesIndex, dataPointIndex }: { seriesIndex: number; dataPointIndex: number }) => {
      const pt: CycleTimeScatterPoint = seriesData.value[seriesIndex]?.data[dataPointIndex]?.meta
      if (!pt) return ''
      const breakdown = pt.stageBreakdown
        .map(s => `<div>${s.stageName}: ${s.durationDays.toFixed(1)}d</div>`)
        .join('')
      const rework = pt.reworkCount > 0
        ? `<div class="text-amber-400">Rework: ${pt.reworkCount} re-entr${pt.reworkCount === 1 ? 'y' : 'ies'}</div>`
        : ''
      return `<div style="padding:8px;font-size:12px;max-width:240px">
        <div class="font-mono font-bold">${pt.ticketKey}</div>
        <div style="margin-top:2px">${pt.ticketSummary}</div>
        <div style="margin-top:4px;color:#9ca3af">${pt.issueType} · ${pt.cycleTimeDays.toFixed(1)}d</div>
        ${rework}
        <div style="margin-top:6px;border-top:1px solid #374151;padding-top:6px">${breakdown}</div>
      </div>`
    }
  },
  markers: { size: 6, hover: { size: 8 } },
  legend: { labels: { colors: '#d1d5db' } },
  grid: { borderColor: '#374151' },
  theme: { mode: 'dark' }
}))
</script>

<template>
  <!-- Scatter Plot tooltip: Each dot is one completed ticket. Higher dots took longer. Color indicates issue type. -->
  <BaseCard v-if="dataPoints.length > 0">
    <div class="text-sm font-medium text-text-primary mb-1 cursor-help" title="Each dot is one completed ticket. Higher dots took longer. Color indicates issue type.">
      Scatter Plot
    </div>
    <p class="text-xs text-text-muted mb-3">
      <!-- P50 Reference Line tooltip -->
      <span title="Median cycle time. Half of tickets finish below this line, half above.">Dashed line = P50 (median).</span>
      <!-- P85 Reference Line tooltip -->
      <span class="ml-1" :title="`${selectedPercentile ?? 85}th percentile. ${selectedPercentile ?? 85}% of tickets finish below this line. Your predictability benchmark.`">Solid line = P{{ selectedPercentile ?? 85 }}.</span>
      <!-- Rework Badge tooltip: shown inline on dots via tooltip -->
    </p>
    <apexchart
      type="scatter"
      height="280"
      :options="options"
      :series="seriesData"
    />
  </BaseCard>
</template>
