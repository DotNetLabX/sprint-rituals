<script setup lang="ts">
import { computed } from 'vue'
import type { DeveloperProgressResponse, DeveloperProgressAlert } from '../../types'
import EmptyState from '../EmptyState.vue'
import DeveloperProgressCard from './DeveloperProgressCard.vue'

const props = defineProps<{
  data: DeveloperProgressResponse
}>()

// Split pre-sorted alerts into worsening/stalled and improving/recovered groups
const worseningAlerts = computed(() =>
  props.data.alerts.filter(a => a.direction === 'worsening' || a.direction === 'new-stall')
)

const improvingAlerts = computed(() =>
  props.data.alerts.filter(a => a.direction === 'improving' || a.direction === 'stall-resolved')
)

// Stall count for new-stall entries: look up stalledTickets from the matching developer entry
function stallCount(alert: DeveloperProgressAlert): number {
  const dev = props.data.developers.find(d => d.accountId === alert.accountId)
  return dev?.stalledTickets.length ?? 0
}

// Delta description per direction
function deltaDescription(alert: DeveloperProgressAlert): string {
  switch (alert.direction) {
    case 'worsening':
      return alert.gapDelta != null ? `gap grew by ${alert.gapDelta.toFixed(1)} SP` : 'gap growing'
    case 'improving':
      return alert.gapDelta != null ? `gap shrank by ${Math.abs(alert.gapDelta).toFixed(1)} SP` : 'gap shrinking'
    case 'new-stall': {
      const count = stallCount(alert)
      return `${count} ticket${count !== 1 ? 's' : ''} newly stalled`
    }
    case 'stall-resolved':
      return 'stall resolved'
    default:
      return ''
  }
}
</script>

<template>
  <!-- No active sprint -->
  <EmptyState
    v-if="!data.hasActiveSprint"
    title="No active sprint"
    description="No active sprint. Sync a sprint to get started."
  >
    <template #icon>
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-12 h-12">
        <path fill-rule="evenodd" d="M4 2a1 1 0 011 1v2.101a7.002 7.002 0 0111.601 2.566 1 1 0 11-1.885.666A5.002 5.002 0 005.999 7H9a1 1 0 010 2H4a1 1 0 01-1-1V3a1 1 0 011-1zm.008 9.057a1 1 0 011.276.61A5.002 5.002 0 0014.001 13H11a1 1 0 110-2h5a1 1 0 011 1v5a1 1 0 11-2 0v-2.101a7.002 7.002 0 01-11.601-2.566 1 1 0 01.61-1.276z" clip-rule="evenodd" />
      </svg>
    </template>
  </EmptyState>

  <!-- Active sprint with no developers -->
  <EmptyState
    v-else-if="data.hasActiveSprint && data.developers.length === 0"
    title="No assigned developers"
    description="No developers with assigned tickets in this sprint."
  >
    <template #icon>
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-12 h-12">
        <path d="M9 6a3 3 0 11-6 0 3 3 0 016 0zM17 6a3 3 0 11-6 0 3 3 0 016 0zM12.93 17c.046-.327.07-.66.07-1a6.97 6.97 0 00-1.5-4.33A5 5 0 0119 16v1h-6.07zM6 11a5 5 0 015 5v1H1v-1a5 5 0 015-5z" />
      </svg>
    </template>
  </EmptyState>

  <!-- Main content -->
  <div v-else class="flex flex-col gap-4">
    <!-- Grace period info -->
    <div
      v-if="data.isGracePeriod"
      class="flex items-center gap-2 px-4 py-2 rounded-md bg-surface-elevated border border-border-default text-sm text-text-secondary"
      title="No pace change alerts during the first 2 days of the sprint while work ramps up."
    >
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-4 h-4 shrink-0 text-text-muted">
        <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
      </svg>
      Grace period — no pace change alerts during the first 2 days of the sprint.
    </div>

    <!-- Pace Changes alert banner -->
    <div
      v-if="!data.isGracePeriod && data.alerts.length > 0"
      class="rounded-md border border-border-default bg-surface-elevated px-4 py-3 flex flex-col gap-3"
      title="Developers whose pace changed since the previous day — worsening, improving, or stall events."
    >
      <div class="flex items-center gap-2">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-4 h-4 shrink-0 text-text-secondary">
          <path fill-rule="evenodd" d="M10 2a8 8 0 100 16A8 8 0 0010 2zm.75 4.75a.75.75 0 00-1.5 0v3.5a.75.75 0 001.5 0v-3.5zm0 6.5a.75.75 0 00-1.5 0v.5a.75.75 0 001.5 0v-.5z" clip-rule="evenodd" />
        </svg>
        <span class="text-sm font-medium text-text-primary">Pace Changes</span>
      </div>

      <!-- Worsening / Stalled group -->
      <div v-if="worseningAlerts.length > 0" class="flex flex-col gap-1">
        <div class="text-xs font-medium text-status-warning uppercase tracking-wide mb-0.5">Worsening / Stalled</div>
        <ul class="flex flex-col gap-1">
          <li
            v-for="alert in worseningAlerts"
            :key="alert.accountId"
            class="flex items-center gap-2 text-sm"
          >
            <img
              v-if="alert.avatarUrl"
              :src="alert.avatarUrl"
              :alt="alert.displayName"
              class="w-5 h-5 rounded-full shrink-0"
            />
            <span class="font-medium text-text-primary">{{ alert.displayName }}</span>
            <span class="text-text-secondary">
              — {{ alert.gapSp.toFixed(1) }} SP gap
            </span>
            <span class="ml-auto shrink-0 text-xs text-status-warning font-medium">
              {{ deltaDescription(alert) }}
            </span>
          </li>
        </ul>
      </div>

      <!-- Improving / Recovered group -->
      <div v-if="improvingAlerts.length > 0" class="flex flex-col gap-1">
        <div class="text-xs font-medium text-status-success uppercase tracking-wide mb-0.5">Improving / Recovered</div>
        <ul class="flex flex-col gap-1">
          <li
            v-for="alert in improvingAlerts"
            :key="alert.accountId"
            class="flex items-center gap-2 text-sm"
          >
            <img
              v-if="alert.avatarUrl"
              :src="alert.avatarUrl"
              :alt="alert.displayName"
              class="w-5 h-5 rounded-full shrink-0"
            />
            <span class="font-medium text-text-primary">{{ alert.displayName }}</span>
            <span class="text-text-secondary">
              — {{ alert.gapSp.toFixed(1) }} SP gap
            </span>
            <span class="ml-auto shrink-0 text-xs text-status-success font-medium">
              {{ deltaDescription(alert) }}
            </span>
          </li>
        </ul>
      </div>
    </div>

    <!-- Sprint info line -->
    <div v-if="data.sprint" class="flex items-center gap-3 text-sm text-text-secondary">
      <span class="font-medium text-text-primary">{{ data.sprint.name }}</span>
      <span>Day {{ data.currentDay }} of {{ data.totalDays }}</span>
    </div>

    <!-- Developer card grid -->
    <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
      <DeveloperProgressCard
        v-for="developer in data.developers"
        :key="developer.accountId"
        :developer="developer"
        :is-grace-period="data.isGracePeriod"
      />
    </div>
  </div>
</template>
