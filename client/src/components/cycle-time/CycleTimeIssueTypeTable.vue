<script setup lang="ts">
import BaseCard from '../BaseCard.vue'
import type { CycleTimeIssueTypeEntry } from '../../types'

defineProps<{
  entries: CycleTimeIssueTypeEntry[]
}>()
</script>

<template>
  <!-- Issue Type Breakdown tooltip: Cycle time comparison across issue types. Bugs are typically faster than stories. -->
  <BaseCard v-if="entries.length > 0">
    <div
      class="text-sm font-medium text-text-primary mb-1 cursor-help"
      title="Cycle time comparison across issue types. Bugs are typically faster than stories."
    >
      Issue Type Breakdown
    </div>
    <p class="text-xs text-text-muted mb-3">Cycle time by issue type.</p>
    <table class="w-full text-sm">
      <thead>
        <tr class="text-text-muted text-left border-b border-border-default">
          <th class="pb-2 pr-4 font-medium">Issue Type</th>
          <th class="pb-2 pr-4 font-medium text-right">Tickets</th>
          <th class="pb-2 pr-4 font-medium text-right">Median (days)</th>
          <th class="pb-2 font-medium text-right">P85 (days)</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="entry in entries"
          :key="entry.issueType"
          class="border-b border-border-default last:border-0"
        >
          <td class="py-2 pr-4 text-text-primary">{{ entry.issueType }}</td>
          <td class="py-2 pr-4 text-right tabular-nums text-text-secondary">{{ entry.ticketCount }}</td>
          <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ entry.medianCycleTime.toFixed(1) }}</td>
          <td class="py-2 text-right tabular-nums text-text-primary">{{ entry.p85CycleTime.toFixed(1) }}</td>
        </tr>
      </tbody>
    </table>
  </BaseCard>
</template>
