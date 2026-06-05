<script setup lang="ts">
import { computed, ref } from 'vue'
import type { DeveloperThroughputResponse, DeveloperThroughputEntry, SprintBreakdown } from '../../types'
import { useDeltaDisplay } from '../../composables/useDeltaDisplay'
import BaseCard from '../BaseCard.vue'
import InfoTooltip from '../InfoTooltip.vue'

const { deltaIcon, deltaClass } = useDeltaDisplay()

const props = defineProps<{
  throughput: DeveloperThroughputResponse | null
  sprintMode: 'single' | 'multi'
  selectedLast: number | null
  loading: boolean
}>()

const emit = defineEmits<{
  capacityChange: [accountId: string, sprintId: number, value: number]
}>()

const isMultiSprint = computed(() => props.sprintMode === 'multi')

const singleSprintId = computed(() => {
  if (!isMultiSprint.value && props.throughput?.sprints.length === 1) {
    return props.throughput.sprints[0].id
  }
  return null
})

interface SingleSprintRow {
  dev: DeveloperThroughputEntry
  bd: SprintBreakdown | undefined
}

const singleSprintRows = computed<SingleSprintRow[]>(() => {
  if (!props.throughput || singleSprintId.value === null) return []
  return props.throughput.developers.map(dev => ({
    dev,
    bd: dev.sprintBreakdowns.find(b => b.sprintId === singleSprintId.value!)
  }))
})

function avgSpAssigned(dev: DeveloperThroughputEntry): string {
  const vals = dev.sprintBreakdowns.map(b => b.spAssigned)
  if (vals.length === 0) return '0'
  return (vals.reduce((a, b) => a + b, 0) / vals.length).toFixed(1)
}

function avgSpCompleted(dev: DeveloperThroughputEntry): string {
  const vals = dev.sprintBreakdowns.map(b => b.spCompleted)
  if (vals.length === 0) return '0'
  return (vals.reduce((a, b) => a + b, 0) / vals.length).toFixed(1)
}

function avgCompletionPercent(dev: DeveloperThroughputEntry): string {
  const vals = dev.sprintBreakdowns.map(b => b.completionPercent)
  if (vals.length === 0) return '0'
  return (vals.reduce((a, b) => a + b, 0) / vals.length).toFixed(1)
}

function avgTicketsDone(dev: DeveloperThroughputEntry): string {
  const vals = dev.sprintBreakdowns.map(b => b.ticketsDone)
  if (vals.length === 0) return '0'
  return (vals.reduce((a, b) => a + b, 0) / vals.length).toFixed(1)
}

function avgTicketsCarriedOver(dev: DeveloperThroughputEntry): string {
  const vals = dev.sprintBreakdowns.map(b => b.ticketsCarriedOver)
  if (vals.length === 0) return '0'
  return (vals.reduce((a, b) => a + b, 0) / vals.length).toFixed(1)
}

function latestCapacity(dev: DeveloperThroughputEntry): number {
  if (dev.sprintBreakdowns.length === 0) return 100
  return dev.sprintBreakdowns[dev.sprintBreakdowns.length - 1].capacityPercent
}

function normalizedSp(spCompleted: number, capacityPercent: number): number | null {
  if (capacityPercent >= 100 || capacityPercent <= 0) return null
  return Math.round(spCompleted / (capacityPercent / 100))
}

function avgNormalizedSpCompleted(dev: DeveloperThroughputEntry): number | null {
  const anyReduced = dev.sprintBreakdowns.some(b => b.capacityPercent < 100 && b.capacityPercent > 0)
  if (!anyReduced) return null
  const perSprintNormalized = dev.sprintBreakdowns.map(b => {
    if (b.capacityPercent >= 100 || b.capacityPercent <= 0) return b.spCompleted
    return b.spCompleted / (b.capacityPercent / 100)
  })
  const avg = perSprintNormalized.reduce((a, b) => a + b, 0) / perSprintNormalized.length
  return Math.round(avg)
}

const chartSeries = computed(() => {
  if (!props.throughput) return []
  return props.throughput.developers.map(dev => ({
    name: dev.displayName,
    data: dev.sprintBreakdowns.map(b => b.rollingAverageSpCompleted ?? null)
  }))
})

const chartOptions = computed(() => ({
  chart: {
    type: 'line',
    background: 'transparent',
    toolbar: { show: false },
    animations: { enabled: false }
  },
  stroke: { curve: 'smooth', width: 2 },
  xaxis: {
    categories: props.throughput?.sprints.map(s => s.name) ?? [],
    labels: { style: { colors: '#9ca3af', fontSize: '12px' } }
  },
  yaxis: {
    labels: { style: { colors: '#9ca3af', fontSize: '12px' } },
    title: { text: 'SP Completed', style: { color: '#9ca3af' } }
  },
  tooltip: {
    theme: 'dark',
    y: { formatter: (val: number | null) => val !== null ? val.toFixed(1) : 'N/A' }
  },
  legend: { labels: { colors: '#d1d5db' } },
  grid: { borderColor: '#374151' },
  theme: { mode: 'dark' }
}))

function onCapacityChange(dev: DeveloperThroughputEntry, sprintId: number, event: Event) {
  const value = parseInt((event.target as HTMLInputElement).value, 10)
  if (!isNaN(value) && value >= 0 && value <= 100) {
    emit('capacityChange', dev.accountId, sprintId, value)
  }
}

// Throughput table sorting
const throughputSortColumn = ref<string | null>(null)
const throughputSortDirection = ref<'asc' | 'desc'>('asc')

function toggleThroughputSort(column: string) {
  if (throughputSortColumn.value === column) {
    throughputSortDirection.value = throughputSortDirection.value === 'asc' ? 'desc' : 'asc'
  } else {
    throughputSortColumn.value = column
    throughputSortDirection.value = 'asc'
  }
}

function throughputSortIcon(column: string): string {
  if (throughputSortColumn.value !== column) return ' ↕'
  return throughputSortDirection.value === 'asc' ? ' ↑' : ' ↓'
}

const sortedSingleSprintRows = computed<SingleSprintRow[]>(() => {
  const rows = [...singleSprintRows.value]
  const col = throughputSortColumn.value
  const dir = throughputSortDirection.value
  if (!col) return rows
  return rows.sort((a, b) => {
    let aVal = 0
    let bVal = 0
    if (col === 'spAssigned') { aVal = a.bd?.spAssigned ?? 0; bVal = b.bd?.spAssigned ?? 0 }
    else if (col === 'spCompleted') { aVal = a.bd?.spCompleted ?? 0; bVal = b.bd?.spCompleted ?? 0 }
    else if (col === 'completionPercent') { aVal = a.bd?.completionPercent ?? 0; bVal = b.bd?.completionPercent ?? 0 }
    else if (col === 'ticketsDone') { aVal = a.bd?.ticketsDone ?? 0; bVal = b.bd?.ticketsDone ?? 0 }
    else if (col === 'ticketsCarriedOver') { aVal = a.bd?.ticketsCarriedOver ?? 0; bVal = b.bd?.ticketsCarriedOver ?? 0 }
    else if (col === 'capacityPercent') { aVal = a.bd?.capacityPercent ?? 100; bVal = b.bd?.capacityPercent ?? 100 }
    return dir === 'asc' ? aVal - bVal : bVal - aVal
  })
})

const sortedMultiSprintDevelopers = computed<DeveloperThroughputEntry[]>(() => {
  if (!props.throughput) return []
  const devs = [...props.throughput.developers]
  const col = throughputSortColumn.value
  const dir = throughputSortDirection.value
  if (!col) return devs
  return devs.sort((a, b) => {
    let aVal = 0
    let bVal = 0
    if (col === 'spAssigned') { aVal = parseFloat(avgSpAssigned(a)); bVal = parseFloat(avgSpAssigned(b)) }
    else if (col === 'spCompleted') { aVal = parseFloat(avgSpCompleted(a)); bVal = parseFloat(avgSpCompleted(b)) }
    else if (col === 'completionPercent') { aVal = parseFloat(avgCompletionPercent(a)); bVal = parseFloat(avgCompletionPercent(b)) }
    else if (col === 'ticketsDone') { aVal = parseFloat(avgTicketsDone(a)); bVal = parseFloat(avgTicketsDone(b)) }
    else if (col === 'ticketsCarriedOver') { aVal = parseFloat(avgTicketsCarriedOver(a)); bVal = parseFloat(avgTicketsCarriedOver(b)) }
    else if (col === 'capacityPercent') { aVal = latestCapacity(a); bVal = latestCapacity(b) }
    return dir === 'asc' ? aVal - bVal : bVal - aVal
  })
})
</script>

<template>
  <div class="flex flex-col gap-6">
    <div v-if="loading" class="text-xs text-text-muted">Updating...</div>

    <!-- Single-sprint throughput table -->
    <BaseCard v-if="!isMultiSprint && singleSprintId !== null && throughput">
      <div class="text-sm font-medium text-text-primary mb-4">
        {{ throughput.sprints[0]?.name }}
      </div>
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="text-text-muted text-left border-b border-border-default">
              <th class="pb-2 pr-4 font-medium">Developer</th>
              <th class="pb-2 pr-4 font-medium">Sub-Team</th>
              <th class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary" title="Total story points on non-removed tickets assigned to the developer in this sprint." :class="{ 'text-text-primary': throughputSortColumn === 'spAssigned' }" @click="toggleThroughputSort('spAssigned')">SP Assigned{{ throughputSortIcon('spAssigned') }}</th>
              <th class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary" title="Story points on tickets the developer finished — those whose final status is in the done statuses list." :class="{ 'text-text-primary': throughputSortColumn === 'spCompleted' }" @click="toggleThroughputSort('spCompleted')">SP Completed <InfoTooltip text="When a developer has reduced capacity, a bracketed (~X) value shows their estimated SP at full availability." />{{ throughputSortIcon('spCompleted') }}</th>
              <th class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary" title="Percentage of assigned story points the developer completed (SP Completed / SP Assigned)." :class="{ 'text-text-primary': throughputSortColumn === 'completionPercent' }" @click="toggleThroughputSort('completionPercent')">Completion %{{ throughputSortIcon('completionPercent') }}</th>
              <th class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary" title="Number of tickets the developer completed — those with a final status in the done statuses list." :class="{ 'text-text-primary': throughputSortColumn === 'ticketsDone' }" @click="toggleThroughputSort('ticketsDone')">Tickets Done{{ throughputSortIcon('ticketsDone') }}</th>
              <th class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary" title="Non-removed tickets assigned to the developer that were not completed by sprint end." :class="{ 'text-text-primary': throughputSortColumn === 'ticketsCarriedOver' }" @click="toggleThroughputSort('ticketsCarriedOver')">Carried Over{{ throughputSortIcon('ticketsCarriedOver') }}</th>
              <th class="pb-2 font-medium text-right cursor-pointer select-none hover:text-text-primary" title="Developer's availability for a sprint as a percentage (0-100%). Default is 100% (fully available)." :class="{ 'text-text-primary': throughputSortColumn === 'capacityPercent' }" @click="toggleThroughputSort('capacityPercent')">Capacity %{{ throughputSortIcon('capacityPercent') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="row in sortedSingleSprintRows"
              :key="row.dev.accountId"
              class="border-b border-border-default last:border-0"
            >
              <td class="py-2 pr-4">
                <div class="flex items-center gap-2">
                  <img
                    v-if="row.dev.avatarUrl"
                    :src="row.dev.avatarUrl"
                    :alt="row.dev.displayName"
                    class="w-6 h-6 rounded-full shrink-0"
                  />
                  <div v-else class="w-6 h-6 rounded-full bg-surface-elevated shrink-0 flex items-center justify-center text-xs text-text-muted">
                    {{ row.dev.displayName.charAt(0).toUpperCase() }}
                  </div>
                  <span class="text-text-primary truncate">{{ row.dev.displayName }}</span>
                </div>
              </td>
              <td class="py-2 pr-4 text-text-secondary">
                <span v-if="row.dev.subTeam" class="text-xs bg-surface-elevated rounded px-2 py-0.5">{{ row.dev.subTeam }}</span>
                <span v-else class="text-text-muted">—</span>
              </td>
              <template v-if="row.bd">
                <td class="py-2 pr-4 text-right tabular-nums text-text-primary">
                  <span>{{ row.bd.spAssigned }}</span>
                  <span v-if="row.bd.spAssignedDelta !== null" :class="['ml-1 text-xs', deltaClass(row.bd.spAssignedDeltaPolarity, row.bd.spAssignedDeltaDirection)]" title="Change from the prior sprint — green for improvement, red for regression, gray for neutral.">
                    {{ deltaIcon(row.bd.spAssignedDeltaDirection) }} {{ Math.abs(row.bd.spAssignedDelta).toFixed(1) }}
                  </span>
                </td>
                <td class="py-2 pr-4 text-right tabular-nums text-text-primary">
                  <span>{{ row.bd.spCompleted }}</span>
                  <span
                    v-if="normalizedSp(row.bd.spCompleted, row.bd.capacityPercent) !== null"
                    class="ml-1 text-xs text-text-muted"
                    title="Estimated SP at full capacity. Shows what this developer's output would look like at 100% availability."
                  >(~{{ normalizedSp(row.bd.spCompleted, row.bd.capacityPercent) }})</span>
                  <span v-if="row.bd.spCompletedDelta !== null" :class="['ml-1 text-xs', deltaClass(row.bd.spCompletedDeltaPolarity, row.bd.spCompletedDeltaDirection)]" title="Change from the prior sprint — green for improvement, red for regression, gray for neutral.">
                    {{ deltaIcon(row.bd.spCompletedDeltaDirection) }} {{ Math.abs(row.bd.spCompletedDelta).toFixed(1) }}
                  </span>
                </td>
                <td class="py-2 pr-4 text-right tabular-nums text-text-primary">
                  <span>{{ row.bd.completionPercent }}%</span>
                  <span v-if="row.bd.completionPercentDelta !== null" :class="['ml-1 text-xs', deltaClass(row.bd.completionPercentDeltaPolarity, row.bd.completionPercentDeltaDirection)]" title="Change from the prior sprint — green for improvement, red for regression, gray for neutral.">
                    {{ deltaIcon(row.bd.completionPercentDeltaDirection) }} {{ Math.abs(row.bd.completionPercentDelta).toFixed(1) }}
                  </span>
                </td>
                <td class="py-2 pr-4 text-right tabular-nums text-text-primary">
                  <span>{{ row.bd.ticketsDone }}</span>
                  <span v-if="row.bd.ticketsDoneDelta !== null" :class="['ml-1 text-xs', deltaClass(row.bd.ticketsDoneDeltaPolarity, row.bd.ticketsDoneDeltaDirection)]" title="Change from the prior sprint — green for improvement, red for regression, gray for neutral.">
                    {{ deltaIcon(row.bd.ticketsDoneDeltaDirection) }} {{ Math.abs(row.bd.ticketsDoneDelta) }}
                  </span>
                </td>
                <td class="py-2 pr-4 text-right tabular-nums text-text-primary">
                  <span>{{ row.bd.ticketsCarriedOver }}</span>
                  <span v-if="row.bd.ticketsCarriedOverDelta !== null" :class="['ml-1 text-xs', deltaClass(row.bd.ticketsCarriedOverDeltaPolarity, row.bd.ticketsCarriedOverDeltaDirection)]" title="Change from the prior sprint — green for improvement, red for regression, gray for neutral.">
                    {{ deltaIcon(row.bd.ticketsCarriedOverDeltaDirection) }} {{ Math.abs(row.bd.ticketsCarriedOverDelta) }}
                  </span>
                </td>
                <td class="py-2 text-right">
                  <input
                    type="number"
                    :value="row.bd.capacityPercent"
                    min="0"
                    max="100"
                    class="w-16 text-right bg-surface-elevated border border-border-default rounded px-1 py-0.5 tabular-nums outline-none focus:border-accent-default"
                    :class="row.bd.capacityPercent !== 100 ? 'text-status-warning' : 'text-text-primary'"
                    @change="onCapacityChange(row.dev, singleSprintId!, $event)"
                  />
                </td>
              </template>
              <template v-else>
                <td class="py-2 pr-4 text-right text-text-muted" colspan="6">—</td>
              </template>
            </tr>
          </tbody>
        </table>
      </div>
    </BaseCard>

    <!-- Multi-sprint throughput table -->
    <BaseCard v-if="isMultiSprint && throughput">
      <div class="text-sm font-medium text-text-primary mb-4">
        <span v-if="selectedLast">Last {{ selectedLast }} Sprints</span>
        <span v-else>All Sprints</span>
        — Averaged Values
      </div>
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="text-text-muted text-left border-b border-border-default">
              <th class="pb-2 pr-4 font-medium">Developer</th>
              <th class="pb-2 pr-4 font-medium">Sub-Team</th>
              <th class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary" title="Total story points on non-removed tickets assigned to the developer in this sprint." :class="{ 'text-text-primary': throughputSortColumn === 'spAssigned' }" @click="toggleThroughputSort('spAssigned')">Avg SP Assigned{{ throughputSortIcon('spAssigned') }}</th>
              <th class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary" title="Story points on tickets the developer finished — those whose final status is in the done statuses list." :class="{ 'text-text-primary': throughputSortColumn === 'spCompleted' }" @click="toggleThroughputSort('spCompleted')">Avg SP Completed <InfoTooltip text="When a developer has reduced capacity, a bracketed (~X) value shows their estimated SP at full availability." />{{ throughputSortIcon('spCompleted') }}</th>
              <th class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary" title="Percentage of assigned story points the developer completed (SP Completed / SP Assigned)." :class="{ 'text-text-primary': throughputSortColumn === 'completionPercent' }" @click="toggleThroughputSort('completionPercent')">Avg Completion %{{ throughputSortIcon('completionPercent') }}</th>
              <th class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary" title="Number of tickets the developer completed — those with a final status in the done statuses list." :class="{ 'text-text-primary': throughputSortColumn === 'ticketsDone' }" @click="toggleThroughputSort('ticketsDone')">Avg Tickets Done{{ throughputSortIcon('ticketsDone') }}</th>
              <th class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary" title="Non-removed tickets assigned to the developer that were not completed by sprint end." :class="{ 'text-text-primary': throughputSortColumn === 'ticketsCarriedOver' }" @click="toggleThroughputSort('ticketsCarriedOver')">Avg Carried Over{{ throughputSortIcon('ticketsCarriedOver') }}</th>
              <th class="pb-2 font-medium text-right cursor-pointer select-none hover:text-text-primary" title="Developer's availability for a sprint as a percentage (0-100%). Default is 100% (fully available)." :class="{ 'text-text-primary': throughputSortColumn === 'capacityPercent' }" @click="toggleThroughputSort('capacityPercent')">Capacity % (latest){{ throughputSortIcon('capacityPercent') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="dev in sortedMultiSprintDevelopers"
              :key="dev.accountId"
              class="border-b border-border-default last:border-0"
            >
              <td class="py-2 pr-4">
                <div class="flex items-center gap-2">
                  <img
                    v-if="dev.avatarUrl"
                    :src="dev.avatarUrl"
                    :alt="dev.displayName"
                    class="w-6 h-6 rounded-full shrink-0"
                  />
                  <div v-else class="w-6 h-6 rounded-full bg-surface-elevated shrink-0 flex items-center justify-center text-xs text-text-muted">
                    {{ dev.displayName.charAt(0).toUpperCase() }}
                  </div>
                  <span class="text-text-primary truncate">{{ dev.displayName }}</span>
                </div>
              </td>
              <td class="py-2 pr-4 text-text-secondary">
                <span v-if="dev.subTeam" class="text-xs bg-surface-elevated rounded px-2 py-0.5">{{ dev.subTeam }}</span>
                <span v-else class="text-text-muted">—</span>
              </td>
              <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ avgSpAssigned(dev) }}</td>
              <td class="py-2 pr-4 text-right tabular-nums text-text-primary">
                {{ avgSpCompleted(dev) }}
                <span
                  v-if="avgNormalizedSpCompleted(dev) !== null"
                  class="ml-1 text-xs text-text-muted"
                  title="Estimated SP at full capacity. Shows what this developer's output would look like at 100% availability."
                >(~{{ avgNormalizedSpCompleted(dev) }})</span>
              </td>
              <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ avgCompletionPercent(dev) }}%</td>
              <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ avgTicketsDone(dev) }}</td>
              <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ avgTicketsCarriedOver(dev) }}</td>
              <td class="py-2 text-right tabular-nums" :class="latestCapacity(dev) !== 100 ? 'text-status-warning' : 'text-text-secondary'">{{ latestCapacity(dev) }}%</td>
            </tr>
          </tbody>
        </table>
      </div>
    </BaseCard>

    <!-- Trend chart (multi-sprint mode only) -->
    <BaseCard v-if="isMultiSprint && throughput && throughput.developers.length > 0">
      <div class="flex items-center gap-1 mb-4">
        <div class="text-sm font-medium text-text-primary">SP Completed Trend (3-Sprint Rolling Avg)</div>
        <span
          class="text-text-muted cursor-help"
          title="Multi-line chart of SP completed per developer across sprints, using 3-sprint rolling averages."
        >
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3.5 h-3.5">
            <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
          </svg>
        </span>
      </div>
      <apexchart
        type="line"
        height="300"
        :options="chartOptions"
        :series="chartSeries"
      />
    </BaseCard>
  </div>
</template>
