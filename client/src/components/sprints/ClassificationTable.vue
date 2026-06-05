<script setup lang="ts">
import BaseCard from '../BaseCard.vue'
import type { ClassificationEntry } from '../../types'

defineProps<{
  entries: ClassificationEntry[]
}>()

function categoryTooltip(category: string): string {
  if (category === 'Unplanned Bug') return 'Bug-type tickets added after the planning window that entered the work cycle — reactive quality work.'
  if (category === 'Priority Escalation') return 'Pre-existing tickets (created before sprint start) pulled in after the planning window — reprioritized work.'
  if (category === 'Scope Injection') return 'New work added after the planning window that entered the cycle — truly unplanned scope.'
  return ''
}
</script>

<template>
  <BaseCard>
    <div class="flex items-center gap-1 mb-4">
      <div class="text-sm font-medium text-text-primary">Classification Breakdown</div>
      <span
        class="text-text-muted cursor-help"
        title="How post-planning additions that entered the work cycle are classified by type."
      >
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3.5 h-3.5">
          <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
        </svg>
      </span>
    </div>
    <div v-if="entries.length === 0" class="text-sm text-text-muted">No mid-sprint additions.</div>
    <table v-else class="w-full text-sm">
      <thead>
        <tr class="text-text-muted text-left border-b border-border-default">
          <th class="pb-2 pr-4 font-medium">Category</th>
          <th class="pb-2 pr-4 font-medium text-right">Tickets</th>
          <th class="pb-2 pr-4 font-medium text-right">SP Total</th>
          <th class="pb-2 font-medium text-right">% of Additions</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="entry in entries"
          :key="entry.category"
          class="border-b border-border-default last:border-0"
        >
          <td class="py-2 pr-4 text-text-primary cursor-help" :title="categoryTooltip(entry.category)">{{ entry.category }}</td>
          <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ entry.ticketCount }}</td>
          <td class="py-2 pr-4 text-right tabular-nums text-text-secondary">
            {{ entry.spTotal !== null ? entry.spTotal.toFixed(1) : '—' }}
          </td>
          <td class="py-2 text-right tabular-nums text-text-secondary">{{ entry.percentage.toFixed(1) }}%</td>
        </tr>
      </tbody>
    </table>
  </BaseCard>
</template>
