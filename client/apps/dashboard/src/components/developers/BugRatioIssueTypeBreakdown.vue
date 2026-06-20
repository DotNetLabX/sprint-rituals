<script setup lang="ts">
import type { BugRatioIssueTypeEntry } from '../../types'
import BaseCard from '../BaseCard.vue'

defineProps<{
  issueTypeBreakdown: BugRatioIssueTypeEntry[]
}>()
</script>

<template>
  <BaseCard v-if="issueTypeBreakdown.length > 0">
    <div class="text-sm font-medium text-text-primary mb-4 cursor-help" title="Completed ticket counts grouped by raw Jira issue type. See composition beyond the bug/non-bug split.">Completed Tickets by Issue Type</div>
    <table class="w-full text-sm">
      <thead>
        <tr class="text-text-muted text-left border-b border-border-default">
          <th class="pb-2 pr-4 font-medium">Issue Type</th>
          <th class="pb-2 pr-4 font-medium text-right">Tickets</th>
          <th class="pb-2 font-medium text-right">SP Total</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="entry in issueTypeBreakdown"
          :key="entry.issueType"
          class="border-b border-border-default last:border-0"
        >
          <td class="py-2 pr-4 text-text-primary">{{ entry.issueType }}</td>
          <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ entry.ticketCount }}</td>
          <td class="py-2 text-right tabular-nums text-text-primary">{{ entry.spTotal.toFixed(1) }}</td>
        </tr>
      </tbody>
    </table>
  </BaseCard>
</template>
