<script setup lang="ts">
import { ref, computed } from 'vue'
import BaseCard from '../BaseCard.vue'
import InfoTooltip from '../InfoTooltip.vue'
import type { DevToTestGapResult } from '../../types'

const props = defineProps<{
  gap: DevToTestGapResult
}>()

const expanded = ref(false)

function deltaIcon(direction: string | null | undefined): string {
  if (direction === 'up') return '▲'
  if (direction === 'down') return '▼'
  return '—'
}

function deltaClass(direction: string | null | undefined): string {
  // For gap: down = gap shrinking = good (green), up = gap growing = bad (red)
  if (direction === 'down') return 'text-status-success'
  if (direction === 'up') return 'text-status-danger'
  return 'text-text-secondary'
}

const hasDelta = computed(() => props.gap.medianGapDelta !== null && props.gap.medianGapDirection !== null)
</script>

<template>
  <BaseCard>
    <div class="flex items-start justify-between mb-4">
      <div class="flex items-center gap-2 text-sm font-medium text-text-primary">
        Dev-to-Test Gap
        <InfoTooltip text="Median calendar days between dev completion and first test result. Lower is better — less QA queue time." />
      </div>
    </div>

    <div v-if="gap.medianGapDays !== null" class="flex items-end gap-4 mb-4">
      <div class="text-3xl font-bold tabular-nums text-text-primary">
        {{ gap.medianGapDays.toFixed(1) }}
        <span class="text-base font-normal text-text-muted ml-1">days</span>
      </div>
      <div v-if="hasDelta" :class="['flex items-center gap-1 text-sm font-medium mb-1', deltaClass(gap.medianGapDirection)]">
        {{ deltaIcon(gap.medianGapDirection) }} {{ Math.abs(gap.medianGapDelta!).toFixed(1) }}
        <InfoTooltip text="Change vs prior sprint. Green down arrow = gap shrinking (faster testing). Red up arrow = gap growing." />
      </div>
    </div>
    <div v-else class="flex items-center h-12 text-text-muted text-sm mb-4">
      No dev-to-test gap data for this sprint.
    </div>

    <div v-if="gap.gapTickets.length > 0">
      <div class="flex items-start justify-between mb-2">
        <div class="flex items-center gap-2 text-xs text-text-muted">
          Per-ticket breakdown
          <InfoTooltip text="Per-ticket breakdown: when dev finished, when first test ran, and the gap between them. Longest waits first." />
        </div>
        <button
          class="text-xs text-text-muted hover:text-text-primary transition-colors shrink-0 ml-4"
          @click="expanded = !expanded"
        >
          {{ expanded ? 'Hide' : 'Show' }} tickets
        </button>
      </div>

      <div v-if="expanded" class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="text-left text-xs text-text-muted border-b border-border-default">
              <th class="pb-2 pr-4 font-medium">Ticket</th>
              <th class="pb-2 pr-4 font-medium">Summary</th>
              <th class="pb-2 pr-4 font-medium">Assignee</th>
              <th class="pb-2 pr-4 font-medium text-right">Dev Done</th>
              <th class="pb-2 pr-4 font-medium text-right">First Test</th>
              <th class="pb-2 font-medium text-right">Gap (days)</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="t in gap.gapTickets"
              :key="t.ticketKey"
              class="border-b border-border-default/50 last:border-0"
            >
              <td class="py-2 pr-4 font-mono text-xs text-text-primary whitespace-nowrap">{{ t.ticketKey }}</td>
              <td class="py-2 pr-4 text-text-secondary max-w-xs truncate">{{ t.summary }}</td>
              <td class="py-2 pr-4 text-text-muted text-xs">{{ t.assigneeName ?? '—' }}</td>
              <td class="py-2 pr-4 text-text-secondary tabular-nums text-right text-xs whitespace-nowrap">
                {{ new Date(t.devDoneDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric' }) }}
              </td>
              <td class="py-2 pr-4 text-text-secondary tabular-nums text-right text-xs whitespace-nowrap">
                {{ new Date(t.firstTestDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric' }) }}
              </td>
              <td class="py-2 tabular-nums text-right font-medium" :class="t.gapDays > 0 ? 'text-text-primary' : 'text-status-success'">
                {{ t.gapDays.toFixed(1) }}
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </BaseCard>
</template>
