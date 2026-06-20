<script setup lang="ts">
import { ref } from 'vue'
import BaseCard from '../BaseCard.vue'
import InfoTooltip from '../InfoTooltip.vue'
import type { TestingCrunchResult } from '../../types'

const props = defineProps<{
  crunch: TestingCrunchResult
}>()

const expanded = ref(false)
</script>

<template>
  <BaseCard v-if="crunch.isCrunchFlagged">
    <div class="flex items-start justify-between">
      <div class="flex items-center gap-2 text-sm font-medium text-status-warning">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-4 h-4 shrink-0">
          <path fill-rule="evenodd" d="M8.485 2.495c.673-1.167 2.357-1.167 3.03 0l6.28 10.875c.673 1.167-.17 2.625-1.516 2.625H3.72c-1.347 0-2.189-1.458-1.515-2.625L8.485 2.495zM10 5a.75.75 0 01.75.75v3.5a.75.75 0 01-1.5 0v-3.5A.75.75 0 0110 5zm0 9a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd" />
        </svg>
        Testing Crunch: {{ crunch.crunchPercentage?.toFixed(1) }}% of test runs in the last 2 days
        ({{ crunch.crunchRunCount }} of {{ crunch.totalRunCount }} runs)
        <InfoTooltip text="Fires when >50% of test runs completed in the last 2 sprint days. Shows percentage and affected ticket count." />
      </div>
      <button
        v-if="crunch.crunchTickets.length > 0"
        class="text-xs text-text-muted hover:text-text-primary transition-colors shrink-0 ml-4"
        @click="expanded = !expanded"
      >
        {{ expanded ? 'Hide' : 'Show' }} tickets
      </button>
    </div>

    <div v-if="expanded && crunch.crunchTickets.length > 0" class="mt-4">
      <div class="flex items-center gap-2 text-xs text-text-muted mb-2">
        Tickets with late test runs
        <InfoTooltip text="Tickets whose test runs completed in the last 2 sprint days. Sorted by late run count — worst offenders first." />
      </div>
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="text-left text-xs text-text-muted border-b border-border-default">
              <th class="pb-2 pr-4 font-medium">Ticket</th>
              <th class="pb-2 pr-4 font-medium">Summary</th>
              <th class="pb-2 pr-4 font-medium">Assignee</th>
              <th class="pb-2 pr-4 font-medium text-right">SP</th>
              <th class="pb-2 font-medium text-right">Late Runs</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="t in crunch.crunchTickets"
              :key="t.ticketKey"
              class="border-b border-border-default/50 last:border-0"
            >
              <td class="py-2 pr-4 font-mono text-xs text-text-primary whitespace-nowrap">{{ t.ticketKey }}</td>
              <td class="py-2 pr-4 text-text-secondary max-w-xs truncate">{{ t.summary }}</td>
              <td class="py-2 pr-4 text-text-muted text-xs">{{ t.assigneeName ?? '—' }}</td>
              <td class="py-2 pr-4 text-text-secondary tabular-nums text-right text-xs">{{ t.storyPoints ?? '—' }}</td>
              <td class="py-2 text-status-warning tabular-nums text-right font-medium">{{ t.lateRunCount }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </BaseCard>
</template>
