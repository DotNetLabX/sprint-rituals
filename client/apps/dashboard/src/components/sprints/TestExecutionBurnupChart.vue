<script setup lang="ts">
import { computed } from 'vue'
import BaseCard from '../BaseCard.vue'
import InfoTooltip from '../InfoTooltip.vue'
import type { BurnupDayEntry, ScopeChangeDayEntry } from '../../types'

const props = defineProps<{
  burnupData: BurnupDayEntry[]
  scopeChangeOverlay: ScopeChangeDayEntry[]
  planningWindowDays: number
  sprintEndDayNumber: number
}>()

const xLabels = computed(() =>
  props.burnupData.map(d => {
    const date = new Date(d.calendarDate)
    return `Day ${d.dayNumber} (${date.toLocaleDateString('en-US', { weekday: 'short' })})`
  })
)

const series = computed(() => [
  {
    name: 'PASS (cumulative)',
    data: props.burnupData.map(d => d.cumulativePass)
  },
  {
    name: 'FAIL (cumulative)',
    data: props.burnupData.map(d => d.cumulativeFail)
  },
  {
    name: 'Total (cumulative)',
    data: props.burnupData.map(d => d.cumulativeTotal)
  }
])

// Scope change markers — vertical xaxis annotations
const scopeChangeAnnotations = computed(() =>
  props.scopeChangeOverlay.map(sc => {
    const label = sc.netSp >= 0 ? `+${sc.netSp} SP` : `${sc.netSp} SP`
    return {
      x: xLabels.value[sc.dayNumber - 1] ?? `Day ${sc.dayNumber}`,
      strokeDashArray: 4,
      borderColor: '#f97316',
      label: {
        text: label,
        style: { color: '#f97316', background: 'transparent', fontSize: '10px' }
      }
    }
  })
)

// Post-sprint separator annotation
const postSprintAnnotation = computed(() => {
  const endLabel = xLabels.value[props.sprintEndDayNumber - 1]
  if (!endLabel) return null
  return {
    x: endLabel,
    strokeDashArray: 6,
    borderColor: '#6b7280',
    label: {
      text: 'Sprint end',
      style: { color: '#9ca3af', background: 'transparent', fontSize: '11px' }
    }
  }
})

const chartOptions = computed(() => {
  // Phase background shading via fillColor annotations
  const crunchStartDayNumber = props.sprintEndDayNumber - 1 // last 2 days: N-1 and N
  const planningEndLabel = xLabels.value[props.planningWindowDays - 1]
  const executionEndLabel = xLabels.value[crunchStartDayNumber - 2] // day before crunch
  const crunchStartLabel = xLabels.value[crunchStartDayNumber - 1]
  const sprintEndLabel = xLabels.value[props.sprintEndDayNumber - 1]

  const xAnnotations: object[] = []

  // Scope change markers
  xAnnotations.push(...scopeChangeAnnotations.value)

  // Post-sprint separator
  if (postSprintAnnotation.value) {
    xAnnotations.push(postSprintAnnotation.value)
  }

  // Phase shading — planning zone
  const backgroundAnnotations: object[] = []
  if (planningEndLabel && xLabels.value[0]) {
    backgroundAnnotations.push({
      x: xLabels.value[0],
      x2: planningEndLabel,
      fillColor: '#3b82f620',
      label: {
        text: 'Planning',
        style: { color: '#6b7280', background: 'transparent', fontSize: '10px' }
      }
    })
  }

  // Execution zone
  if (planningEndLabel && executionEndLabel && props.planningWindowDays < crunchStartDayNumber - 1) {
    const execStartLabel = xLabels.value[props.planningWindowDays]
    if (execStartLabel) {
      backgroundAnnotations.push({
        x: execStartLabel,
        x2: executionEndLabel,
        fillColor: '#22c55e10',
        label: {
          text: 'Execution',
          style: { color: '#6b7280', background: 'transparent', fontSize: '10px' }
        }
      })
    }
  }

  // Testing crunch zone
  if (crunchStartLabel && sprintEndLabel) {
    backgroundAnnotations.push({
      x: crunchStartLabel,
      x2: sprintEndLabel,
      fillColor: '#ef444430',
      label: {
        text: 'Crunch',
        style: { color: '#ef4444', background: 'transparent', fontSize: '10px' }
      }
    })
  }

  return {
    chart: {
      type: 'line',
      background: 'transparent',
      toolbar: { show: false },
      animations: { enabled: false }
    },
    stroke: { curve: 'smooth', width: [2, 2, 2] },
    colors: ['#22c55e', '#ef4444', '#9ca3af'],
    xaxis: {
      categories: xLabels.value,
      labels: { style: { colors: '#9ca3af', fontSize: '11px' }, rotate: -30 }
    },
    yaxis: {
      labels: { style: { colors: '#9ca3af', fontSize: '12px' } },
      title: { text: 'Cumulative Test Runs', style: { color: '#9ca3af' } },
      min: 0
    },
    tooltip: {
      theme: 'dark',
      custom: ({ dataPointIndex, w }: { seriesIndex: number; dataPointIndex: number; w: any }) => {
        const d = props.burnupData[dataPointIndex]
        if (!d) return ''
        const label = w.globals.categoryLabels[dataPointIndex] ?? xLabels.value[dataPointIndex] ?? ''
        const phase = d.isWithinSprint ? 'Within sprint' : 'Post-sprint'
        const rows = [
          { name: 'PASS', value: d.cumulativePass, daily: d.dailyPass, color: '#22c55e' },
          { name: 'FAIL', value: d.cumulativeFail, daily: d.dailyFail, color: '#ef4444' },
          { name: 'Total', value: d.cumulativeTotal, daily: d.dailyPass + d.dailyFail, color: '#9ca3af' }
        ]
        const rowsHtml = rows.map(r =>
          `<div style="display:flex;align-items:center;gap:6px;padding:2px 0">
            <span style="display:inline-block;width:10px;height:10px;border-radius:50%;background:${r.color}"></span>
            <span style="color:#d1d5db">${r.name}:</span>
            <span style="color:#f9fafb;font-weight:600">${r.value}</span>
            <span style="color:#6b7280;font-size:10px">(+${r.daily} today)</span>
          </div>`
        ).join('')
        return `<div style="background:#1f2937;border:1px solid #374151;border-radius:6px;padding:10px 14px;font-size:12px">
          <div style="color:#9ca3af;margin-bottom:6px;font-weight:500">${label} — ${phase}</div>
          ${rowsHtml}
        </div>`
      }
    },
    legend: { labels: { colors: '#d1d5db' } },
    grid: { borderColor: '#374151' },
    theme: { mode: 'dark' },
    annotations: {
      xaxis: xAnnotations,
      ...(backgroundAnnotations.length > 0 ? { xaxis: [...backgroundAnnotations, ...xAnnotations] } : {})
    }
  }
})
</script>

<template>
  <BaseCard>
    <div class="flex items-center gap-2 text-sm font-medium text-text-primary mb-1">
      Test Execution Burnup
      <InfoTooltip text="Cumulative test runs completed per sprint day. Green = PASS, Red = FAIL, Gray = Total. Shows when testing happened." />
    </div>
    <p class="text-xs text-text-muted mb-4">
      Background shading: planning zone (days 1-{{ planningWindowDays }}), execution zone, testing crunch zone (last 2 days).
    </p>
    <apexchart
      v-if="burnupData.length > 0"
      type="line"
      height="300"
      :options="chartOptions"
      :series="series"
    />
    <div v-else class="flex items-center justify-center h-24 text-text-muted text-sm">
      No test execution data for this sprint.
    </div>
  </BaseCard>
</template>
