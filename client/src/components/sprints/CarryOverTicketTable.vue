<script setup lang="ts">
import { computed } from 'vue'
import BaseCard from '../BaseCard.vue'
import type { CarryOverTicketEntry } from '../../types'

const props = defineProps<{
  tickets: CarryOverTicketEntry[]
}>()

interface StageGroup {
  stage: string
  tickets: CarryOverTicketEntry[]
  ticketCount: number
  spTotal: number
}

// BR20: when all tickets are under "Other" (no workflow stages configured), show flat table
const isFlat = computed(() =>
  props.tickets.length > 0 && props.tickets.every(t => t.workflowStage === 'Other')
)

const groupedByStage = computed((): StageGroup[] => {
  const map = new Map<string, CarryOverTicketEntry[]>()
  for (const t of props.tickets) {
    const list = map.get(t.workflowStage) ?? []
    list.push(t)
    map.set(t.workflowStage, list)
  }
  return Array.from(map.entries()).map(([stage, tickets]) => ({
    stage,
    tickets,
    ticketCount: tickets.length,
    spTotal: tickets.filter(t => t.storyPoints !== null).reduce((sum, t) => sum + (t.storyPoints ?? 0), 0)
  }))
})
</script>

<template>
  <BaseCard>
    <div class="text-sm font-medium text-text-primary mb-4">Carry-Over Tickets</div>
    <div v-if="tickets.length === 0" class="text-sm text-text-muted">No carry-over tickets in this sprint.</div>

    <!-- BR20: flat table when no workflow stages configured (all under "Other") -->
    <div v-else-if="isFlat" class="overflow-x-auto">
      <table class="w-full text-sm">
        <thead>
          <tr class="text-text-muted text-left border-b border-border-default">
            <th class="pb-2 pr-4 font-medium">Ticket</th>
            <th class="pb-2 pr-4 font-medium">Summary</th>
            <th class="pb-2 pr-4 font-medium">Type</th>
            <th class="pb-2 pr-4 font-medium text-right">SP</th>
            <th class="pb-2 pr-4 font-medium">Status</th>
            <th class="pb-2 font-medium text-right">Sprints</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="ticket in tickets"
            :key="ticket.ticketKey"
            :class="[
              'border-b border-border-default last:border-0',
              ticket.isZombie ? 'bg-status-warning/5' : '',
              ticket.isExcluded ? 'opacity-50' : ''
            ]"
          >
            <td class="py-2 pr-4 font-mono text-text-primary text-xs">{{ ticket.ticketKey }}</td>
            <td class="py-2 pr-4 text-text-primary max-w-xs truncate">
              {{ ticket.summary }}
              <span v-if="ticket.isExcluded" class="ml-1 text-xs bg-surface-elevated text-text-muted rounded px-1 py-0.5">excluded</span>
            </td>
            <td class="py-2 pr-4 text-text-secondary">{{ ticket.issueType }}</td>
            <td class="py-2 pr-4 text-right tabular-nums text-text-secondary">
              {{ ticket.storyPoints !== null ? ticket.storyPoints : '—' }}
            </td>
            <td class="py-2 pr-4 text-text-secondary">{{ ticket.finalStatus }}</td>
            <td class="py-2 text-right tabular-nums">
              <span
                v-if="ticket.isZombie"
                class="bg-status-warning/10 text-status-warning text-xs rounded px-1.5 py-0.5"
              >
                {{ ticket.sprintCount }} Zombie
              </span>
              <span v-else class="text-text-muted text-xs">{{ ticket.sprintCount }}</span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Grouped by workflow stage -->
    <div v-else>
      <div v-for="group in groupedByStage" :key="group.stage" class="mb-6 last:mb-0">
        <!-- Stage header -->
        <div class="flex items-center justify-between py-2 border-b border-border-default mb-2">
          <span class="text-sm font-medium text-text-primary">{{ group.stage }}</span>
          <span class="text-xs text-text-muted tabular-nums">
            {{ group.ticketCount }} ticket{{ group.ticketCount !== 1 ? 's' : '' }} · {{ group.spTotal.toFixed(1) }} SP
          </span>
        </div>
        <table class="w-full text-sm">
          <thead>
            <tr class="text-text-muted text-left">
              <th class="pb-2 pr-4 font-medium">Ticket</th>
              <th class="pb-2 pr-4 font-medium">Summary</th>
              <th class="pb-2 pr-4 font-medium">Type</th>
              <th class="pb-2 pr-4 font-medium text-right">SP</th>
              <th class="pb-2 pr-4 font-medium">Status</th>
              <th class="pb-2 pr-4 font-medium">Stage</th>
              <th class="pb-2 font-medium text-right">Sprints</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="ticket in group.tickets"
              :key="ticket.ticketKey"
              :class="[
                'border-b border-border-default last:border-0',
                ticket.isZombie ? 'bg-status-warning/5' : '',
                ticket.isExcluded ? 'opacity-50' : ''
              ]"
            >
              <td class="py-2 pr-4 font-mono text-text-primary text-xs">{{ ticket.ticketKey }}</td>
              <td class="py-2 pr-4 text-text-primary max-w-xs truncate">
                {{ ticket.summary }}
                <span v-if="ticket.isExcluded" class="ml-1 text-xs bg-surface-elevated text-text-muted rounded px-1 py-0.5">excluded</span>
              </td>
              <td class="py-2 pr-4 text-text-secondary">{{ ticket.issueType }}</td>
              <td class="py-2 pr-4 text-right tabular-nums text-text-secondary">
                {{ ticket.storyPoints !== null ? ticket.storyPoints : '—' }}
              </td>
              <td class="py-2 pr-4 text-text-secondary">{{ ticket.finalStatus }}</td>
              <td class="py-2 pr-4 text-text-secondary text-xs">{{ ticket.workflowStage }}</td>
              <td class="py-2 text-right tabular-nums">
                <span
                  v-if="ticket.isZombie"
                  class="bg-status-warning/10 text-status-warning text-xs rounded px-1.5 py-0.5"
                >
                  {{ ticket.sprintCount }} Zombie
                </span>
                <span v-else class="text-text-muted text-xs">{{ ticket.sprintCount }}</span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </BaseCard>
</template>
