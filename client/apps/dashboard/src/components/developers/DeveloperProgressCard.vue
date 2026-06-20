<script setup lang="ts">
import { computed, ref } from 'vue'
import type { DeveloperProgressEntry, DayBreakdownEntry } from '../../types'

const props = defineProps<{
  developer: DeveloperProgressEntry
  isGracePeriod: boolean
}>()

// ---- Stall badge toggle ----
const stallExpanded = ref(false)

// ---- Chart series ----
const xLabels = computed(() =>
  props.developer.dailyBreakdown.map(d => `Day ${d.day}`)
)

const dailyCumulatives = computed(() => {
  let featureSp = 0
  let bugSp = 0
  return props.developer.dailyBreakdown.map((d: DayBreakdownEntry) => {
    for (const t of d.completedTickets) {
      const sp = t.storyPoints ?? 0
      if (t.issueType === 'Bug') bugSp += sp
      else featureSp += sp
    }
    return { featureSp: parseFloat(featureSp.toFixed(2)), bugSp: parseFloat(bugSp.toFixed(2)) }
  })
})

const series = computed(() => [
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
    data: props.developer.dailyBreakdown.map(d => parseFloat(d.expectedCumulativeSp.toFixed(2)))
  }
])

// Y-axis max: assignedSp * capacityPercent / 100, min 1 to avoid empty chart
const yMax = computed(() => {
  const paceTarget = props.developer.assignedSp * (props.developer.capacityPercent / 100)
  return Math.max(paceTarget, 0.1)
})

const chartOptions = computed(() => ({
  chart: {
    type: 'line',
    background: 'transparent',
    toolbar: { show: false },
    sparkline: { enabled: false },
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
    categories: xLabels.value,
    labels: { style: { colors: '#9ca3af', fontSize: '10px' } },
    axisBorder: { show: false },
    axisTicks: { show: false }
  },
  yaxis: {
    min: 0,
    max: yMax.value,
    reversed: false,
    labels: { style: { colors: '#9ca3af', fontSize: '10px' }, formatter: (v: number) => v.toFixed(1) }
  },
  grid: { borderColor: '#374151', strokeDashArray: 3 },
  legend: { show: false },
  theme: { mode: 'dark' },
  tooltip: {
    theme: 'dark',
    custom: ({ dataPointIndex }: { seriesIndex: number; dataPointIndex: number }) => {
      const d: DayBreakdownEntry | undefined = props.developer.dailyBreakdown[dataPointIndex]
      const cum = dailyCumulatives.value[dataPointIndex]
      if (!d || !cum) return ''
      const date = new Date(d.date).toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' })
      const tickets = d.completedTickets.length > 0
        ? d.completedTickets.map(t =>
            `<div style="padding:2px 0;color:#d1d5db">
              <span style="font-weight:600;color:#f9fafb">${t.key}</span>
              ${t.storyPoints != null ? `<span style="color:#9ca3af"> · ${t.storyPoints} SP</span>` : ''}
              <span style="color:${t.issueType === 'Bug' ? '#ef4444' : '#3b82f6'};font-size:11px"> [${t.issueType}]</span>
              <div style="color:#9ca3af;font-size:11px">${t.summary}</div>
            </div>`
          ).join('')
        : '<div style="color:#6b7280;font-size:11px">No completions</div>'
      return `<div style="background:#1f2937;border:1px solid #374151;border-radius:6px;padding:8px 12px;font-size:12px;max-width:240px">
        <div style="color:#9ca3af;margin-bottom:4px;font-weight:500">Day ${d.day} — ${date}</div>
        <div style="color:#3b82f6;margin-bottom:2px">Feature: <strong>${cum.featureSp.toFixed(1)} SP</strong></div>
        <div style="color:#ef4444;margin-bottom:4px">Bug: <strong>${cum.bugSp.toFixed(1)} SP</strong></div>
        ${tickets}
      </div>`
    }
  }
}))

// ---- Behind-pace border ----
const cardBorderClass = computed(() =>
  props.developer.isBehindPace && !props.isGracePeriod
    ? 'border-status-warning'
    : 'border-border-default'
)
</script>

<template>
  <div
    class="bg-surface-card border rounded-lg p-4 flex flex-col gap-3"
    :class="cardBorderClass"
    :title="developer.isBehindPace && !isGracePeriod ? 'This developer\'s completed SP is more than one full day behind their expected pace.' : undefined"
  >
    <!-- Header: avatar + name + sub-team -->
    <div class="flex items-center gap-2">
      <RouterLink :to="{ name: 'developer-detail', params: { accountId: developer.accountId } }" class="shrink-0">
        <img
          v-if="developer.avatarUrl"
          :src="developer.avatarUrl"
          :alt="developer.displayName"
          class="w-8 h-8 rounded-full hover:ring-2 hover:ring-accent-default transition-shadow"
        />
        <div
          v-else
          class="w-8 h-8 rounded-full bg-surface-elevated flex items-center justify-center text-xs font-medium text-text-secondary hover:ring-2 hover:ring-accent-default transition-shadow"
        >
          {{ developer.displayName.charAt(0).toUpperCase() }}
        </div>
      </RouterLink>
      <div class="flex-1 min-w-0">
        <RouterLink
          :to="{ name: 'developer-detail', params: { accountId: developer.accountId } }"
          class="text-sm font-medium text-text-primary truncate hover:text-accent-default hover:underline"
        >{{ developer.displayName }}</RouterLink>
        <div v-if="developer.subTeam" class="text-xs text-text-muted">{{ developer.subTeam }}</div>
      </div>
      <!-- Behind-pace indicator -->
      <div
        v-if="developer.isBehindPace && !isGracePeriod"
        class="shrink-0 flex items-center gap-1 text-status-warning text-xs font-medium"
        title="This developer's completed SP is more than one full day behind their expected pace."
      >
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3.5 h-3.5">
          <path fill-rule="evenodd" d="M8.485 2.495c.673-1.167 2.357-1.167 3.03 0l6.28 10.875c.673 1.167-.17 2.625-1.516 2.625H3.72c-1.347 0-2.189-1.458-1.515-2.625L8.485 2.495zM10 5a.75.75 0 01.75.75v3.5a.75.75 0 01-1.5 0v-3.5A.75.75 0 0110 5zm0 9a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd" />
        </svg>
        Behind
      </div>
    </div>

    <!-- SP progress line -->
    <div
      class="text-sm text-text-secondary"
      title="Total story points completed out of total assigned (all types: features, bugs, tasks)."
    >
      <span class="font-semibold text-text-primary">{{ developer.completedSp.toFixed(1) }}</span>
      <span class="text-text-muted"> / {{ developer.assignedSp.toFixed(1) }} SP</span>
      <span class="ml-1 text-xs text-text-muted">({{ developer.completionPercent.toFixed(0) }}%)</span>
      <span class="ml-1 text-xs text-text-muted italic">(all types)</span>
    </div>

    <!-- Bug/feature SP split line -->
    <div
      v-if="developer.completedSp > 0"
      class="text-xs text-text-muted"
      title="Completed SP split by ticket type: features (stories, tasks) and bugs."
    >
      <template v-if="developer.featureCompletedSp > 0 && developer.bugCompletedSp > 0">
        {{ developer.featureCompletedSp.toFixed(1) }} features / {{ developer.bugCompletedSp.toFixed(1) }} bugs
      </template>
      <template v-else-if="developer.featureCompletedSp > 0">
        {{ developer.featureCompletedSp.toFixed(1) }} features
      </template>
      <template v-else>
        {{ developer.bugCompletedSp.toFixed(1) }} bugs
      </template>
    </div>

    <!-- Mini burnup chart -->
    <div v-if="developer.dailyBreakdown.length > 0">
      <div class="flex items-center gap-2 mb-1 flex-wrap">
        <span class="text-xs text-blue-400">Feature</span>
        <span class="text-xs text-red-400">Bug</span>
        <span
          class="text-xs text-text-muted cursor-help"
          title="Straight diagonal based on assigned SP, capacity, and sprint length in calendar days."
        >— Expected</span>
      </div>
      <apexchart
        type="line"
        height="140"
        :options="chartOptions"
        :series="series"
      />
    </div>

    <!-- Stall badge -->
    <div v-if="developer.stalledTickets.length > 0" class="flex flex-col gap-2">
      <button
        type="button"
        class="flex items-center gap-1.5 text-xs font-medium text-status-danger hover:text-status-danger/80 transition-colors w-fit"
        title="Tickets in progress with no status change for 2+ business days (weekdays only)."
        @click="stallExpanded = !stallExpanded"
      >
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3.5 h-3.5">
          <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.28 7.22a.75.75 0 00-1.06 1.06L8.94 10l-1.72 1.72a.75.75 0 101.06 1.06L10 11.06l1.72 1.72a.75.75 0 101.06-1.06L11.06 10l1.72-1.72a.75.75 0 00-1.06-1.06L10 8.94 8.28 7.22z" clip-rule="evenodd" />
        </svg>
        {{ developer.stalledTickets.length }} stalled
        <svg
          xmlns="http://www.w3.org/2000/svg"
          viewBox="0 0 20 20"
          fill="currentColor"
          class="w-3 h-3 transition-transform"
          :class="stallExpanded ? 'rotate-180' : ''"
        >
          <path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clip-rule="evenodd" />
        </svg>
      </button>

      <!-- Stalled tickets list (expanded) -->
      <ul v-if="stallExpanded" class="flex flex-col gap-1.5 pl-1">
        <li
          v-for="ticket in developer.stalledTickets"
          :key="ticket.key"
          class="text-xs text-text-secondary border-l-2 border-status-danger/40 pl-2"
        >
          <div class="flex items-center gap-1">
            <span class="font-medium text-text-primary">{{ ticket.key }}</span>
            <span class="text-text-muted">·</span>
            <span class="text-text-muted">{{ ticket.currentStatus }}</span>
            <span
              class="ml-auto shrink-0 text-status-danger font-medium"
              title="Business days (Mon-Fri) since this ticket's last status change in Jira."
            >{{ ticket.daysSinceLastTransition }}d</span>
          </div>
          <div class="text-text-muted truncate">{{ ticket.summary }}</div>
        </li>
      </ul>
    </div>
  </div>
</template>
