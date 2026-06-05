<script setup lang="ts">
import BaseCard from '../BaseCard.vue'
import type { CarryOverZombieSummary } from '../../types'

defineProps<{
  zombies: CarryOverZombieSummary[]
}>()
</script>

<template>
  <BaseCard v-if="zombies.length > 0">
    <div class="text-sm font-medium text-text-primary mb-4 cursor-help" title="Tickets that have lived through 3 or more sprints without being completed.">Zombie Tickets</div>
    <p class="text-xs text-text-muted mb-3">Tickets appearing in 3 or more sprints.</p>
    <table class="w-full text-sm">
      <thead>
        <tr class="text-text-muted text-left border-b border-border-default">
          <th class="pb-2 pr-4 font-medium">Ticket</th>
          <th class="pb-2 pr-4 font-medium">Summary</th>
          <th class="pb-2 pr-4 font-medium">Type</th>
          <th class="pb-2 pr-4 font-medium">Status</th>
          <th class="pb-2 pr-4 font-medium text-right">SP</th>
          <th class="pb-2 font-medium text-right">Sprints</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="z in zombies"
          :key="z.ticketKey"
          class="border-b border-border-default last:border-0"
        >
          <td class="py-2 pr-4 font-mono text-text-primary text-xs">{{ z.ticketKey }}</td>
          <td class="py-2 pr-4 text-text-primary max-w-xs truncate">{{ z.summary }}</td>
          <td class="py-2 pr-4 text-text-secondary">{{ z.issueType }}</td>
          <td class="py-2 pr-4 text-text-secondary">{{ z.currentStatus }}</td>
          <td class="py-2 pr-4 text-right tabular-nums text-text-secondary">
            {{ z.storyPoints !== null ? z.storyPoints : '—' }}
          </td>
          <td class="py-2 text-right tabular-nums">
            <span class="bg-status-warning/10 text-status-warning text-xs rounded px-1.5 py-0.5">
              {{ z.sprintCount }}
            </span>
          </td>
        </tr>
      </tbody>
    </table>
  </BaseCard>
</template>
