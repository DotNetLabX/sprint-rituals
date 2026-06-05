<script setup lang="ts">
import BaseCard from '../BaseCard.vue'
import type { CycleTimeDeveloperEntry } from '../../types'

defineProps<{
  entries: CycleTimeDeveloperEntry[]
}>()
</script>

<template>
  <!-- Per-Developer Breakdown tooltip: Cycle time patterns by developer. For identifying coaching opportunities, not ranking. -->
  <BaseCard v-if="entries.length > 0">
    <div
      class="text-sm font-medium text-text-primary mb-1 cursor-help"
      title="Cycle time patterns by developer. For identifying coaching opportunities, not ranking."
    >
      Per-Developer Breakdown
    </div>
    <p class="text-xs text-text-muted mb-3">Sorted alphabetically. Not a ranking.</p>
    <table class="w-full text-sm">
      <thead>
        <tr class="text-text-muted text-left border-b border-border-default">
          <th class="pb-2 pr-4 font-medium">Developer</th>
          <th class="pb-2 pr-4 font-medium text-right">Tickets</th>
          <th class="pb-2 pr-4 font-medium text-right">Median (days)</th>
          <th class="pb-2 pr-4 font-medium text-right">P85 (days)</th>
          <th class="pb-2 font-medium">
            <!-- Dominant Stage tooltip -->
            <span
              class="cursor-help"
              title="The workflow stage where this developer's tickets spend the most time on average."
            >Dominant Stage</span>
          </th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="entry in entries"
          :key="entry.displayName"
          class="border-b border-border-default last:border-0"
        >
          <td class="py-2 pr-4 text-text-primary">
            <div class="flex items-center gap-2">
              <img
                v-if="entry.avatarUrl"
                :src="entry.avatarUrl"
                :alt="entry.displayName"
                class="w-6 h-6 rounded-full shrink-0"
              />
              <span>{{ entry.displayName }}</span>
            </div>
          </td>
          <td class="py-2 pr-4 text-right tabular-nums text-text-secondary">{{ entry.ticketsCompleted }}</td>
          <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ entry.medianCycleTime.toFixed(1) }}</td>
          <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ entry.p85CycleTime.toFixed(1) }}</td>
          <td class="py-2 text-text-secondary text-xs">{{ entry.dominantStage }}</td>
        </tr>
      </tbody>
    </table>
  </BaseCard>
</template>
