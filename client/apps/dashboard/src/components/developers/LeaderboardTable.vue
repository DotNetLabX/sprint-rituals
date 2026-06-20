<script setup lang="ts">
import type { LeaderboardDeveloperEntry, LeaderboardDeveloperSingleEntry } from '../../types'
import BaseCard from '../BaseCard.vue'

defineProps<{
  developers: LeaderboardDeveloperEntry[] | LeaderboardDeveloperSingleEntry[]
  mode: 'multi' | 'single'
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

function isSingleEntry(dev: LeaderboardDeveloperEntry | LeaderboardDeveloperSingleEntry): dev is LeaderboardDeveloperSingleEntry {
  return 'delta' in dev
}

function normalizedTotalSp(dev: LeaderboardDeveloperEntry | LeaderboardDeveloperSingleEntry): number | null {
  if ('sprintBreakdowns' in dev && Array.isArray(dev.sprintBreakdowns)) {
    // Multi-sprint: per-sprint normalize then average (BR6)
    const breakdowns = dev.sprintBreakdowns as Array<{ totalSp: number; capacityPercent: number }>
    const anyReduced = breakdowns.some(b => b.capacityPercent < 100 && b.capacityPercent > 0)
    if (!anyReduced) return null
    const perSprint = breakdowns.map(b => {
      if (b.capacityPercent >= 100 || b.capacityPercent <= 0) return b.totalSp
      return b.totalSp / (b.capacityPercent / 100)
    })
    return Math.round(perSprint.reduce((a, b) => a + b, 0) / perSprint.length)
  }
  // Single-sprint
  const cap = (dev as LeaderboardDeveloperSingleEntry).capacityPercent
  if (cap >= 100 || cap <= 0) return null
  return Math.round(dev.totalSp / (cap / 100))
}
</script>

<template>
  <BaseCard>
    <div class="overflow-x-auto">
      <table class="w-full text-sm">
        <thead>
          <tr class="text-text-muted text-left border-b border-border-default">
            <th class="pb-2 pr-4 font-medium">Developer</th>
            <th class="pb-2 pr-4 font-medium">Sub-Team</th>
            <th
              class="pb-2 pr-4 font-medium text-right"
              title="Story points completed on non-bug tickets across the selected sprints."
            >Feature SP</th>
            <th
              class="pb-2 pr-4 font-medium text-right"
              title="Story points completed on bug tickets across the selected sprints."
            >Bug SP</th>
            <th
              class="pb-2 pr-4 font-medium text-right"
              title="Combined feature and bug story points completed across the selected sprints."
            >Total SP</th>
            <th
              class="pb-2 pr-4 font-medium text-right"
              title="Number of completed non-bug tickets. Includes tickets with no story point estimate."
            >Feature Tickets</th>
            <th
              class="pb-2 pr-4 font-medium text-right"
              title="Number of completed bug tickets. Includes tickets with no story point estimate."
            >Bug Tickets</th>
            <th
              class="pb-2 font-medium text-right"
              title="Total completed tickets (features + bugs) across the selected sprints. Includes unestimated tickets."
            >Total Tickets</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="dev in developers"
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
            <!-- Feature SP -->
            <td class="py-2 pr-4 text-right tabular-nums text-status-success">
              <span>{{ dev.featureSp.toFixed(1) }}</span>
              <template v-if="mode === 'single' && isSingleEntry(dev) && dev.delta">
                <span
                  :class="['ml-1 text-xs', deltaClass(dev.delta.featureSpDeltaPolarity, dev.delta.featureSpDeltaDirection)]"
                  title="Change versus the prior sprint. Feature SP: higher is green. Bug SP: lower is green. Total SP: neutral."
                >
                  {{ deltaIcon(dev.delta.featureSpDeltaDirection) }} {{ Math.abs(dev.delta.featureSpDelta).toFixed(1) }}
                </span>
              </template>
            </td>
            <!-- Bug SP -->
            <td class="py-2 pr-4 text-right tabular-nums text-status-danger">
              <span>{{ dev.bugSp.toFixed(1) }}</span>
              <template v-if="mode === 'single' && isSingleEntry(dev) && dev.delta">
                <span
                  :class="['ml-1 text-xs', deltaClass(dev.delta.bugSpDeltaPolarity, dev.delta.bugSpDeltaDirection)]"
                  title="Change versus the prior sprint. Feature SP: higher is green. Bug SP: lower is green. Total SP: neutral."
                >
                  {{ deltaIcon(dev.delta.bugSpDeltaDirection) }} {{ Math.abs(dev.delta.bugSpDelta).toFixed(1) }}
                </span>
              </template>
            </td>
            <!-- Total SP -->
            <td class="py-2 pr-4 text-right tabular-nums text-text-primary">
              <span>{{ dev.totalSp.toFixed(1) }}</span>
              <span
                v-if="normalizedTotalSp(dev) !== null"
                class="ml-1 text-xs text-text-muted"
                title="Estimated SP at full capacity. Shows what this developer's output would look like at 100% availability."
              >(~{{ normalizedTotalSp(dev) }})</span>
              <template v-if="mode === 'single' && isSingleEntry(dev) && dev.delta">
                <span
                  :class="['ml-1 text-xs', deltaClass(dev.delta.totalSpDeltaPolarity, dev.delta.totalSpDeltaDirection)]"
                  title="Change versus the prior sprint. Feature SP: higher is green. Bug SP: lower is green. Total SP: neutral."
                >
                  {{ deltaIcon(dev.delta.totalSpDeltaDirection) }} {{ Math.abs(dev.delta.totalSpDelta).toFixed(1) }}
                </span>
              </template>
            </td>
            <!-- Feature Tickets -->
            <td class="py-2 pr-4 text-right tabular-nums text-text-primary">
              <span>{{ dev.featureTickets }}</span>
              <template v-if="mode === 'single' && isSingleEntry(dev) && dev.delta">
                <span
                  :class="['ml-1 text-xs', deltaClass(dev.delta.featureTicketsDeltaPolarity, dev.delta.featureTicketsDeltaDirection)]"
                  title="Change versus the prior sprint. Feature SP: higher is green. Bug SP: lower is green. Total SP: neutral."
                >
                  {{ deltaIcon(dev.delta.featureTicketsDeltaDirection) }} {{ Math.abs(dev.delta.featureTicketsDelta) }}
                </span>
              </template>
            </td>
            <!-- Bug Tickets -->
            <td class="py-2 pr-4 text-right tabular-nums text-text-primary">
              <span>{{ dev.bugTickets }}</span>
              <template v-if="mode === 'single' && isSingleEntry(dev) && dev.delta">
                <span
                  :class="['ml-1 text-xs', deltaClass(dev.delta.bugTicketsDeltaPolarity, dev.delta.bugTicketsDeltaDirection)]"
                  title="Change versus the prior sprint. Feature SP: higher is green. Bug SP: lower is green. Total SP: neutral."
                >
                  {{ deltaIcon(dev.delta.bugTicketsDeltaDirection) }} {{ Math.abs(dev.delta.bugTicketsDelta) }}
                </span>
              </template>
            </td>
            <!-- Total Tickets -->
            <td class="py-2 text-right tabular-nums text-text-primary">{{ dev.totalTickets }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </BaseCard>
</template>
