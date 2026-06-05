<script setup lang="ts">
import BaseCard from '../BaseCard.vue'
import type { BugTimeInProgress } from '../../types'

defineProps<{
  bugs: BugTimeInProgress[]
}>()
</script>

<template>
  <BaseCard v-if="bugs.length > 0">
    <div class="text-sm font-medium text-text-primary mb-4 cursor-help" title="Calendar days each mid-sprint bug spent in active work statuses — measures capacity consumed.">Bug Time in Progress</div>
    <table class="w-full text-sm">
      <thead>
        <tr class="text-text-muted text-left border-b border-border-default">
          <th class="pb-2 pr-4 font-medium">Ticket</th>
          <th class="pb-2 pr-4 font-medium">Summary</th>
          <th class="pb-2 pr-4 font-medium text-right">Active Days</th>
          <th class="pb-2 font-medium">Current Status</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="bug in bugs"
          :key="bug.ticketKey"
          class="border-b border-border-default last:border-0"
        >
          <td class="py-2 pr-4 font-mono text-text-primary text-xs">{{ bug.ticketKey }}</td>
          <td class="py-2 pr-4 text-text-primary max-w-xs truncate">{{ bug.ticketSummary }}</td>
          <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ bug.timeInActiveDays.toFixed(2) }}d</td>
          <td class="py-2 text-text-secondary">{{ bug.currentStatus }}</td>
        </tr>
      </tbody>
    </table>
  </BaseCard>
</template>
