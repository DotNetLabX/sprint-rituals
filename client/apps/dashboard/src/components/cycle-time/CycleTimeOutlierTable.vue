<script setup lang="ts">
import BaseCard from '../BaseCard.vue'
import type { CycleTimeOutlierEntry } from '../../types'

defineProps<{
  entries: CycleTimeOutlierEntry[]
}>()
</script>

<template>
  <!-- Outlier Table tooltip: Tickets that took 2x+ the sprint median. Review these for process improvement insights. -->
  <BaseCard v-if="entries.length > 0">
    <div
      class="text-sm font-medium text-text-primary mb-1 cursor-help"
      title="Tickets that took 2x+ the sprint median. Review these for process improvement insights."
    >
      Outlier Tickets
    </div>
    <p class="text-xs text-text-muted mb-3">Tickets exceeding 2x the sprint median cycle time. Sorted by cycle time descending.</p>
    <table class="w-full text-sm">
      <thead>
        <tr class="text-text-muted text-left border-b border-border-default">
          <th class="pb-2 pr-4 font-medium">Ticket</th>
          <th class="pb-2 pr-4 font-medium">Summary</th>
          <th class="pb-2 pr-4 font-medium">Type</th>
          <th class="pb-2 pr-4 font-medium text-right">Cycle Time</th>
          <th class="pb-2 pr-4 font-medium">Stage Breakdown</th>
          <th class="pb-2 font-medium text-right">Rework</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="entry in entries"
          :key="entry.ticketKey"
          class="border-b border-border-default last:border-0"
        >
          <td class="py-2 pr-4 font-mono text-text-primary text-xs">{{ entry.ticketKey }}</td>
          <td class="py-2 pr-4 text-text-primary max-w-xs truncate">{{ entry.ticketSummary }}</td>
          <td class="py-2 pr-4 text-text-secondary">{{ entry.issueType }}</td>
          <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ entry.cycleTimeDays.toFixed(1) }}d</td>
          <td class="py-2 pr-4 text-xs text-text-muted">
            <span
              v-for="(stage, i) in entry.stageBreakdown"
              :key="stage.stageName"
            >{{ stage.stageName }}: {{ stage.durationDays.toFixed(1) }}d<span v-if="i < entry.stageBreakdown.length - 1"> · </span></span>
          </td>
          <td class="py-2 text-right tabular-nums">
            <!-- Rework Badge tooltip: This ticket re-entered a workflow stage it had already passed through. -->
            <span
              v-if="entry.reworkCount > 0"
              class="bg-status-warning/10 text-status-warning text-xs rounded px-1.5 py-0.5 cursor-help"
              title="This ticket re-entered a workflow stage it had already passed through. The number shows re-entry count."
            >
              {{ entry.reworkCount }}
            </span>
            <span v-else class="text-text-muted">—</span>
          </td>
        </tr>
      </tbody>
    </table>
  </BaseCard>
</template>
