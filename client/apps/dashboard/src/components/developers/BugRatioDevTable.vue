<script setup lang="ts">
import { ref, computed } from 'vue'
import type { BugRatioMultiSprintResponse, BugRatioSingleSprintResponse } from '../../types'
import BaseCard from '../BaseCard.vue'

const props = defineProps<{
  mode: 'multi' | 'single'
  multi?: BugRatioMultiSprintResponse
  single?: BugRatioSingleSprintResponse
}>()

function deltaIcon(direction: string | null | undefined): string {
  if (direction === 'up') return '▲'
  if (direction === 'down') return '▼'
  return '—'
}

function deltaClass(polarity: string | null | undefined, direction: string | null | undefined): string {
  if (direction === 'flat' || !direction) return 'text-text-secondary'
  if (polarity === 'positive') return 'text-status-success'
  if (polarity === 'negative') return 'text-status-danger'
  return 'text-text-secondary'
}

// Bug ratio table sorting
const bugRatioSortColumn = ref<string | null>(null)
const bugRatioSortDirection = ref<'asc' | 'desc'>('asc')

function toggleBugRatioSort(column: string) {
  if (bugRatioSortColumn.value === column) {
    bugRatioSortDirection.value = bugRatioSortDirection.value === 'asc' ? 'desc' : 'asc'
  } else {
    bugRatioSortColumn.value = column
    bugRatioSortDirection.value = 'asc'
  }
}

function bugRatioSortIcon(column: string): string {
  if (bugRatioSortColumn.value !== column) return ' ↕'
  return bugRatioSortDirection.value === 'asc' ? ' ↑' : ' ↓'
}

function bugRatioSortValue(dev: any, col: string): number {
  if (col === 'bugSp') return dev.bugSp ?? 0
  if (col === 'nonBugSp') return dev.nonBugSp ?? 0
  if (col === 'bugRatioPercent') return dev.bugRatioPercent ?? 0
  if (col === 'bugTicketCount') return dev.bugTicketCount ?? 0
  if (col === 'nonBugTicketCount') return dev.nonBugTicketCount ?? 0
  return 0
}

const sortedMultiDevelopers = computed(() => {
  if (!props.multi) return []
  const devs = [...props.multi.developers]
  const col = bugRatioSortColumn.value
  const dir = bugRatioSortDirection.value
  if (!col) return devs
  return devs.sort((a, b) => {
    const diff = bugRatioSortValue(a, col) - bugRatioSortValue(b, col)
    return dir === 'asc' ? diff : -diff
  })
})

const sortedSingleDevelopers = computed(() => {
  if (!props.single) return []
  const devs = [...props.single.developers]
  const col = bugRatioSortColumn.value
  const dir = bugRatioSortDirection.value
  if (!col) return devs
  return devs.sort((a, b) => {
    const diff = bugRatioSortValue(a, col) - bugRatioSortValue(b, col)
    return dir === 'asc' ? diff : -diff
  })
})
</script>

<template>
  <!-- Multi-sprint developer table -->
  <BaseCard v-if="mode === 'multi' && multi">
    <div class="flex items-center gap-1 mb-4">
      <div class="text-sm font-medium text-text-primary">Developer Bug Ratio</div>
      <span
        class="text-text-muted cursor-help"
        title="Bug and non-bug SP, ticket counts, bug ratio %, and alert status for each active developer."
      >
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3.5 h-3.5">
          <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
        </svg>
      </span>
    </div>
    <div class="overflow-x-auto">
      <table class="w-full text-sm">
        <thead>
          <tr class="text-text-muted text-left border-b border-border-default">
            <th class="pb-2 pr-4 font-medium">Developer</th>
            <th class="pb-2 pr-4 font-medium">Sub-Team</th>
            <th class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary" :class="{ 'text-text-primary': bugRatioSortColumn === 'bugSp' }" @click="toggleBugRatioSort('bugSp')">Bug SP{{ bugRatioSortIcon('bugSp') }}</th>
            <th class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary" :class="{ 'text-text-primary': bugRatioSortColumn === 'nonBugSp' }" @click="toggleBugRatioSort('nonBugSp')">Non-Bug SP{{ bugRatioSortIcon('nonBugSp') }}</th>
            <th class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary" :class="{ 'text-text-primary': bugRatioSortColumn === 'bugRatioPercent' }" @click="toggleBugRatioSort('bugRatioPercent')">Bug Ratio %{{ bugRatioSortIcon('bugRatioPercent') }}</th>
            <th class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary" :class="{ 'text-text-primary': bugRatioSortColumn === 'bugTicketCount' }" @click="toggleBugRatioSort('bugTicketCount')">Bug Tickets{{ bugRatioSortIcon('bugTicketCount') }}</th>
            <th class="pb-2 font-medium text-right cursor-pointer select-none hover:text-text-primary" :class="{ 'text-text-primary': bugRatioSortColumn === 'nonBugTicketCount' }" @click="toggleBugRatioSort('nonBugTicketCount')">Non-Bug Tickets{{ bugRatioSortIcon('nonBugTicketCount') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="dev in sortedMultiDevelopers"
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
                <!-- Alert badge -->
                <span
                  v-if="dev.alert.isActive"
                  class="text-amber-400 text-xs"
                  title="Appears when a developer's bug ratio exceeds the threshold for consecutive sprints."
                >
                  &#9651;
                </span>
              </div>
            </td>
            <td class="py-2 pr-4 text-text-secondary">
              <span v-if="dev.subTeam" class="text-xs bg-surface-elevated rounded px-2 py-0.5">{{ dev.subTeam }}</span>
              <span v-else class="text-text-muted">—</span>
            </td>
            <td class="py-2 pr-4 text-right tabular-nums text-status-danger">{{ dev.bugSp.toFixed(1) }}</td>
            <td class="py-2 pr-4 text-right tabular-nums text-status-success">{{ dev.nonBugSp.toFixed(1) }}</td>
            <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ dev.bugRatioPercent.toFixed(1) }}%</td>
            <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ dev.bugTicketCount }}</td>
            <td class="py-2 text-right tabular-nums text-text-primary">{{ dev.nonBugTicketCount }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </BaseCard>

  <!-- Single-sprint developer table -->
  <BaseCard v-else-if="mode === 'single' && single">
    <div class="text-sm font-medium text-text-primary mb-4">Developer Bug Ratio — {{ single.sprint.name }}</div>
    <div class="overflow-x-auto">
      <table class="w-full text-sm">
        <thead>
          <tr class="text-text-muted text-left border-b border-border-default">
            <th class="pb-2 pr-4 font-medium">Developer</th>
            <th class="pb-2 pr-4 font-medium">Sub-Team</th>
            <th class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary" :class="{ 'text-text-primary': bugRatioSortColumn === 'bugSp' }" @click="toggleBugRatioSort('bugSp')">Bug SP{{ bugRatioSortIcon('bugSp') }}</th>
            <th class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary" :class="{ 'text-text-primary': bugRatioSortColumn === 'nonBugSp' }" @click="toggleBugRatioSort('nonBugSp')">Non-Bug SP{{ bugRatioSortIcon('nonBugSp') }}</th>
            <th class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary" :class="{ 'text-text-primary': bugRatioSortColumn === 'bugRatioPercent' }" @click="toggleBugRatioSort('bugRatioPercent')">Bug Ratio %{{ bugRatioSortIcon('bugRatioPercent') }}</th>
            <th class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary" :class="{ 'text-text-primary': bugRatioSortColumn === 'bugTicketCount' }" @click="toggleBugRatioSort('bugTicketCount')">Bug Tickets{{ bugRatioSortIcon('bugTicketCount') }}</th>
            <th class="pb-2 font-medium text-right cursor-pointer select-none hover:text-text-primary" :class="{ 'text-text-primary': bugRatioSortColumn === 'nonBugTicketCount' }" @click="toggleBugRatioSort('nonBugTicketCount')">Non-Bug Tickets{{ bugRatioSortIcon('nonBugTicketCount') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="dev in sortedSingleDevelopers"
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
                <span
                  v-if="dev.alert.isActive"
                  class="text-amber-400 text-xs"
                  title="Appears when a developer's bug ratio exceeds the threshold for consecutive sprints."
                >
                  &#9651;
                </span>
              </div>
            </td>
            <td class="py-2 pr-4 text-text-secondary">
              <span v-if="dev.subTeam" class="text-xs bg-surface-elevated rounded px-2 py-0.5">{{ dev.subTeam }}</span>
              <span v-else class="text-text-muted">—</span>
            </td>
            <td class="py-2 pr-4 text-right tabular-nums text-status-danger">
              <span>{{ dev.bugSp.toFixed(1) }}</span>
              <span v-if="dev.delta" :class="['ml-1 text-xs', deltaClass(dev.delta.bugSpDeltaPolarity, dev.delta.bugSpDeltaDirection)]">
                {{ deltaIcon(dev.delta.bugSpDeltaDirection) }} {{ Math.abs(dev.delta.bugSpDelta).toFixed(1) }}
              </span>
            </td>
            <td class="py-2 pr-4 text-right tabular-nums text-status-success">
              <span>{{ dev.nonBugSp.toFixed(1) }}</span>
              <span v-if="dev.delta" :class="['ml-1 text-xs', deltaClass(dev.delta.nonBugSpDeltaPolarity, dev.delta.nonBugSpDeltaDirection)]">
                {{ deltaIcon(dev.delta.nonBugSpDeltaDirection) }} {{ Math.abs(dev.delta.nonBugSpDelta).toFixed(1) }}
              </span>
            </td>
            <td class="py-2 pr-4 text-right tabular-nums text-text-primary">
              <span>{{ dev.bugRatioPercent.toFixed(1) }}%</span>
              <span v-if="dev.delta" :class="['ml-1 text-xs', deltaClass(dev.delta.bugRatioPercentDeltaPolarity, dev.delta.bugRatioPercentDeltaDirection)]">
                {{ deltaIcon(dev.delta.bugRatioPercentDeltaDirection) }} {{ Math.abs(dev.delta.bugRatioPercentDelta).toFixed(1) }}
              </span>
            </td>
            <td class="py-2 pr-4 text-right tabular-nums text-text-primary">
              <span>{{ dev.bugTicketCount }}</span>
              <span v-if="dev.delta" :class="['ml-1 text-xs', deltaClass(dev.delta.bugTicketCountDeltaPolarity, dev.delta.bugTicketCountDeltaDirection)]">
                {{ deltaIcon(dev.delta.bugTicketCountDeltaDirection) }} {{ Math.abs(dev.delta.bugTicketCountDelta) }}
              </span>
            </td>
            <td class="py-2 text-right tabular-nums text-text-primary">
              <span>{{ dev.nonBugTicketCount }}</span>
              <span v-if="dev.delta" :class="['ml-1 text-xs', deltaClass(dev.delta.nonBugTicketCountDeltaPolarity, dev.delta.nonBugTicketCountDeltaDirection)]">
                {{ deltaIcon(dev.delta.nonBugTicketCountDeltaDirection) }} {{ Math.abs(dev.delta.nonBugTicketCountDelta) }}
              </span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </BaseCard>
</template>
