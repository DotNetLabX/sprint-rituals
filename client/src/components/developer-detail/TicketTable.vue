<script setup lang="ts">
import type { TicketDetailEntry } from '../../types'

const props = defineProps<{
  tickets: TicketDetailEntry[]
  jiraInstanceUrl: string
}>()

const stateLabel: Record<string, string> = {
  'stalled': 'Stalled',
  'in-progress': 'In Progress',
  'not-started': 'Not Started',
  'done': 'Done'
}

const stateOrder = ['stalled', 'in-progress', 'not-started', 'done']

function ticketsByState(state: string): TicketDetailEntry[] {
  return props.tickets.filter(t => t.state === state)
}

function jiraUrl(key: string): string {
  return `${props.jiraInstanceUrl}/browse/${key}`
}
</script>

<template>
  <div>
    <h3 class="text-sm font-medium text-text-secondary mb-3">Tickets</h3>

    <div v-if="tickets.length === 0" class="text-sm text-text-muted">No tickets assigned.</div>

    <template v-for="state in stateOrder" :key="state">
      <template v-if="ticketsByState(state).length > 0">
        <!-- State group header -->
        <div
          class="flex items-center gap-2 px-2 py-1 mb-1 mt-3 first:mt-0 rounded text-xs font-semibold"
          :class="{
            'bg-status-danger/10 text-status-danger': state === 'stalled',
            'bg-accent-default/10 text-accent-default': state === 'in-progress',
            'bg-surface-elevated text-text-muted': state === 'not-started',
            'bg-status-success/10 text-status-success': state === 'done'
          }"
        >
          {{ stateLabel[state] }}
          <span class="font-normal opacity-70">({{ ticketsByState(state).length }})</span>
        </div>

        <!-- Tickets table for this group -->
        <div class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead>
              <tr class="text-xs text-text-muted border-b border-border-default">
                <th class="text-left py-1 px-2 font-medium">Key</th>
                <th class="text-left py-1 px-2 font-medium">Summary</th>
                <th class="text-left py-1 px-2 font-medium">Status</th>
                <th class="text-right py-1 px-2 font-medium">SP</th>
                <th class="text-left py-1 px-2 font-medium">Type</th>
                <th class="text-right py-1 px-2 font-medium">Days</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="ticket in ticketsByState(state)"
                :key="ticket.key"
                class="border-b border-border-default/50 hover:bg-surface-elevated/50 transition-colors"
              >
                <td
                  class="py-1.5 px-2 shrink-0"
                  :class="ticket.isStalled ? 'border-l-2 border-l-status-danger pl-1.5' : 'border-l-2 border-l-transparent'"
                >
                  <a
                    :href="jiraUrl(ticket.key)"
                    target="_blank"
                    rel="noopener noreferrer"
                    class="text-accent-default hover:underline font-medium text-xs whitespace-nowrap"
                  >
                    {{ ticket.key }}
                  </a>
                </td>
                <td class="py-1.5 px-2 text-text-secondary max-w-xs truncate" :title="ticket.summary">
                  {{ ticket.summary }}
                </td>
                <td class="py-1.5 px-2 text-text-muted text-xs whitespace-nowrap">
                  {{ ticket.currentStatus }}
                </td>
                <td class="py-1.5 px-2 text-right text-text-primary font-medium text-xs whitespace-nowrap">
                  {{ ticket.storyPoints !== null ? ticket.storyPoints : '—' }}
                </td>
                <td class="py-1.5 px-2 text-text-muted text-xs whitespace-nowrap">
                  {{ ticket.issueType }}
                </td>
                <td
                  class="py-1.5 px-2 text-right text-xs whitespace-nowrap font-medium"
                  :class="ticket.isStalled ? 'text-status-danger' : 'text-text-muted'"
                  :title="ticket.isStalled ? `Stalled for ${ticket.daysInCurrentStatus} business days` : `${ticket.daysInCurrentStatus} business days in current status`"
                >
                  {{ ticket.daysInCurrentStatus }}d
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </template>
    </template>
  </div>
</template>
