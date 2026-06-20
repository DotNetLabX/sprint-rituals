<script setup lang="ts">
import BaseCard from '../BaseCard.vue'
import type { ScopeChangeEvent } from '../../types'

defineProps<{
  events: ScopeChangeEvent[]
}>()

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('en-GB', { month: 'short', day: 'numeric' })
}
</script>

<template>
  <BaseCard>
    <div class="text-sm font-medium text-text-primary mb-4 cursor-help" title="Chronological log of every ticket added to or removed from the sprint after it started.">Scope Change Events</div>
    <div v-if="events.length === 0" class="text-sm text-text-muted">No scope change events in this sprint.</div>
    <div v-else class="overflow-x-auto">
      <table class="w-full text-sm">
        <thead>
          <tr class="text-text-muted text-left border-b border-border-default">
            <th class="pb-2 pr-4 font-medium">Date</th>
            <th class="pb-2 pr-4 font-medium">Day</th>
            <th class="pb-2 pr-4 font-medium">Ticket</th>
            <th class="pb-2 pr-4 font-medium">Summary</th>
            <th class="pb-2 pr-4 font-medium text-right">SP</th>
            <th class="pb-2 pr-4 font-medium">Type</th>
            <th class="pb-2 pr-4 font-medium">Action</th>
            <th class="pb-2 font-medium">Category</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="(event, idx) in events"
            :key="idx"
            :class="['border-b border-border-default last:border-0', event.isExcluded ? 'opacity-50' : '']"
          >
            <td class="py-2 pr-4 text-text-secondary tabular-nums">{{ formatDate(event.date) }}</td>
            <td class="py-2 pr-4 text-text-secondary tabular-nums">{{ event.sprintDayNumber }}</td>
            <td class="py-2 pr-4 font-mono text-text-primary text-xs">{{ event.ticketKey }}</td>
            <td class="py-2 pr-4 text-text-primary max-w-xs truncate">
              {{ event.ticketSummary }}
              <span v-if="event.isExcluded" class="ml-1 text-xs bg-surface-elevated text-text-muted rounded px-1 py-0.5">excluded</span>
            </td>
            <td class="py-2 pr-4 text-right tabular-nums text-text-secondary">
              {{ event.storyPoints !== null ? event.storyPoints : '—' }}
            </td>
            <td class="py-2 pr-4 text-text-secondary">{{ event.issueType }}</td>
            <td class="py-2 pr-4">
              <span :class="['text-xs font-medium rounded px-1.5 py-0.5', event.action === 'added' ? 'bg-status-success/10 text-status-success' : 'bg-status-danger/10 text-status-danger']">
                {{ event.action }}
              </span>
            </td>
            <td class="py-2 text-text-secondary text-xs">{{ event.category ?? '—' }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </BaseCard>
</template>
