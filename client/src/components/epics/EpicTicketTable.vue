<script setup lang="ts">
import type { EpicProgressTicketEntry } from '../../types'

defineProps<{
  tickets: EpicProgressTicketEntry[]
  hasQaData: boolean
}>()

function testStatusClass(status: string | null): string {
  if (status === 'Passed') return 'bg-status-success/10 text-status-success'
  if (status === 'Failed') return 'bg-status-danger/10 text-status-danger'
  if (status === 'InProgress') return 'bg-status-warning/10 text-status-warning'
  return 'bg-surface-elevated text-text-muted'
}

function testStatusLabel(status: string | null): string {
  if (status === 'Passed') return 'Passed'
  if (status === 'Failed') return 'Failed'
  if (status === 'InProgress') return 'In Progress'
  if (status === 'NoTests') return 'No Tests'
  return '—'
}
</script>

<template>
  <div class="overflow-x-auto mt-3 border-t border-border-default pt-3">
    <table class="w-full text-sm">
      <thead>
        <tr class="text-text-muted text-left border-b border-border-default">
          <th class="pb-2 pr-4 font-medium">
            <span title="Individual ticket details within the epic.">Key</span>
          </th>
          <th class="pb-2 pr-4 font-medium">Summary</th>
          <th class="pb-2 pr-4 font-medium">Type</th>
          <th class="pb-2 pr-4 font-medium text-right">SP</th>
          <th class="pb-2 pr-4 font-medium">Status</th>
          <th class="pb-2 font-medium" :class="{ 'pr-4': hasQaData }">Assignee</th>
          <th v-if="hasQaData" class="pb-2 pr-4 font-medium" title="Test status derived from all linked test execution runs.">Test Status</th>
          <th v-if="hasQaData" class="pb-2 pr-4 font-medium text-right" title="Percentage of PASS runs across this ticket's linked test executions.">Pass Rate</th>
          <th v-if="hasQaData" class="pb-2 font-medium text-right" title="Unique bugs linked via Blocks from this ticket's test executions.">Bugs Found</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="ticket in tickets"
          :key="ticket.ticketKey"
          :class="[
            'border-b border-border-default last:border-0',
            ticket.isDone ? 'opacity-60' : ''
          ]"
        >
          <td class="py-2 pr-4 text-text-secondary tabular-nums font-mono text-xs whitespace-nowrap">
            {{ ticket.ticketKey }}
          </td>
          <td class="py-2 pr-4 text-text-primary max-w-xs truncate">
            {{ ticket.summary }}
          </td>
          <td class="py-2 pr-4 text-text-secondary text-xs">
            {{ ticket.issueType }}
          </td>
          <td class="py-2 pr-4 text-right tabular-nums text-text-primary">
            {{ ticket.storyPoints !== null ? ticket.storyPoints : '—' }}
          </td>
          <td class="py-2 pr-4 text-xs">
            <span
              :class="[
                'inline-block rounded px-1.5 py-0.5',
                ticket.isDone
                  ? 'bg-status-success/10 text-status-success'
                  : 'bg-surface-elevated text-text-secondary'
              ]"
            >
              {{ ticket.currentStatus }}
            </span>
          </td>
          <td class="py-2 text-text-secondary text-xs" :class="{ 'pr-4': hasQaData }">
            {{ ticket.assigneeDisplayName ?? 'Unassigned' }}
          </td>

          <!-- Test Status badge (null for bug tickets) -->
          <td v-if="hasQaData" class="py-2 pr-4 text-xs">
            <span
              v-if="ticket.testStatus !== null"
              :class="['inline-block rounded px-1.5 py-0.5', testStatusClass(ticket.testStatus)]"
            >
              {{ testStatusLabel(ticket.testStatus) }}
            </span>
            <span v-else class="text-text-muted">—</span>
          </td>

          <!-- Pass Rate -->
          <td v-if="hasQaData" class="py-2 pr-4 text-right tabular-nums text-xs text-text-primary">
            {{ ticket.testPassRate !== null ? `${ticket.testPassRate.toFixed(1)}%` : '—' }}
          </td>

          <!-- Bugs Found -->
          <td v-if="hasQaData" class="py-2 text-right tabular-nums text-xs text-text-primary">
            {{ ticket.testBugsFound !== null ? ticket.testBugsFound : '—' }}
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
