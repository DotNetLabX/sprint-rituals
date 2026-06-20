<script setup lang="ts">
import { computed } from 'vue'
import type { CurrentSprintDetail, DayBreakdownEntry } from '../../types'
import TicketTable from './TicketTable.vue'

const props = defineProps<{
  currentSprint: CurrentSprintDetail | null
  jiraInstanceUrl: string
}>()

const series = computed(() => {
  if (!props.currentSprint) return []
  return [
    {
      name: 'Feature SP',
      data: dailyCumulatives.value.map(c => c.featureSp)
    },
    {
      name: 'Bug SP',
      data: dailyCumulatives.value.map(c => c.bugSp)
    },
    {
      name: 'Expected Pace',
      data: props.currentSprint.dailyBreakdown.map((d: DayBreakdownEntry) =>
        parseFloat(d.expectedCumulativeSp.toFixed(2))
      )
    }
  ]
})

const yMax = computed(() => {
  if (!props.currentSprint) return 1
  const paceTarget = props.currentSprint.assignedSp * (100 / 100)
  return Math.max(paceTarget, 0.1)
})

const dailyCumulatives = computed(() => {
  if (!props.currentSprint) return []
  let featureSp = 0
  let bugSp = 0
  return props.currentSprint.dailyBreakdown.map((d: DayBreakdownEntry) => {
    for (const t of d.completedTickets) {
      const sp = t.storyPoints ?? 0
      if (t.issueType === 'Bug') bugSp += sp
      else featureSp += sp
    }
    return { featureSp: parseFloat(featureSp.toFixed(2)), bugSp: parseFloat(bugSp.toFixed(2)) }
  })
})

const chartOptions = computed(() => {
  if (!props.currentSprint) return {}
  const xLabels = props.currentSprint.dailyBreakdown.map((d: DayBreakdownEntry) => `Day ${d.day}`)
  return {
    chart: {
      type: 'line',
      background: 'transparent',
      toolbar: { show: false },
      animations: { enabled: false },
      zoom: { enabled: false }
    },
    stroke: {
      curve: 'straight',
      width: [2, 2, 1],
      dashArray: [0, 0, 4]
    },
    colors: ['#3b82f6', '#ef4444', '#6b7280'],
    fill: { type: ['solid', 'solid', 'solid'], opacity: [1, 1, 1] },
    xaxis: {
      categories: xLabels,
      labels: { style: { colors: '#9ca3af', fontSize: '11px' } },
      axisBorder: { show: false },
      axisTicks: { show: false }
    },
    yaxis: {
      min: 0,
      max: yMax.value,
      labels: {
        style: { colors: '#9ca3af', fontSize: '10px' },
        formatter: (v: number) => v.toFixed(1)
      }
    },
    grid: { borderColor: '#374151', strokeDashArray: 3 },
    legend: { show: true, labels: { colors: '#9ca3af' } },
    theme: { mode: 'dark' },
    tooltip: {
      theme: 'dark',
      custom: ({ dataPointIndex }: { seriesIndex: number; dataPointIndex: number }) => {
        const d = props.currentSprint?.dailyBreakdown[dataPointIndex]
        const cum = dailyCumulatives.value[dataPointIndex]
        if (!d || !cum) return ''
        const date = new Date(d.date).toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' })
        const ticketLines = d.completedTickets.length > 0
          ? d.completedTickets.map(t =>
              `<div style="padding:2px 0;color:#d1d5db">
                <span style="font-weight:600;color:#f9fafb">${t.key}</span>
                ${t.storyPoints != null ? `<span style="color:#9ca3af"> · ${t.storyPoints} SP</span>` : ''}
                <span style="color:${t.issueType === 'Bug' ? '#ef4444' : '#3b82f6'};font-size:11px"> [${t.issueType}]</span>
                <div style="color:#9ca3af;font-size:11px">${t.summary}</div>
              </div>`
            ).join('')
          : '<div style="color:#6b7280;font-size:11px">No completions</div>'
        return `<div style="background:#1f2937;border:1px solid #374151;border-radius:6px;padding:8px 12px;font-size:12px;max-width:280px">
          <div style="color:#9ca3af;margin-bottom:4px;font-weight:500">Day ${d.day} — ${date}</div>
          <div style="color:#3b82f6;margin-bottom:2px">Feature: <strong>${cum.featureSp.toFixed(1)} SP</strong></div>
          <div style="color:#ef4444;margin-bottom:4px">Bug: <strong>${cum.bugSp.toFixed(1)} SP</strong></div>
          ${ticketLines}
        </div>`
      }
    }
  }
})
</script>

<template>
  <div>
    <h3 class="text-sm font-medium text-text-secondary mb-3">Current Sprint</h3>

    <!-- No active sprint -->
    <div v-if="currentSprint === null" class="text-sm text-text-muted">No active sprint.</div>

    <template v-else>
      <!-- Sprint name -->
      <div class="text-xs text-text-muted mb-4">{{ currentSprint.sprint.name }}</div>

      <!-- Full-width burnup chart -->
      <div v-if="currentSprint.dailyBreakdown.length > 0" class="mb-4">
        <div class="flex items-center gap-2 mb-1">
          <span class="text-xs text-text-muted">Burnup</span>
          <span class="text-xs text-text-muted ml-2">— Expected Pace</span>
        </div>
        <apexchart
          type="line"
          height="300"
          :options="chartOptions"
          :series="series"
        />
      </div>

      <!-- SP summary -->
      <div
        class="text-sm text-text-secondary mb-4"
        title="Completed SP out of assigned SP across all ticket types (features + bugs)."
      >
        <span class="font-semibold text-text-primary">{{ currentSprint.completedSp.toFixed(1) }}</span>
        <span class="text-text-muted"> / {{ currentSprint.assignedSp.toFixed(1) }} SP</span>
        <span class="ml-1 text-xs text-text-muted">({{ currentSprint.completionPercent.toFixed(0) }}%)</span>
        <span class="ml-1 text-xs text-text-muted italic">(all types)</span>
        <span v-if="currentSprint.featureCompletedSp > 0 || currentSprint.bugCompletedSp > 0" class="ml-2 text-xs text-text-muted">
          — {{ currentSprint.featureCompletedSp.toFixed(1) }} features / {{ currentSprint.bugCompletedSp.toFixed(1) }} bugs
        </span>
      </div>

      <!-- Ticket table -->
      <TicketTable
        :tickets="currentSprint.tickets"
        :jira-instance-url="jiraInstanceUrl"
      />
    </template>
  </div>
</template>
