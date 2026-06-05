<script setup lang="ts">
import BaseCard from '../BaseCard.vue'
import type { ZombieTrajectoryEntry } from '../../types'

defineProps<{
  trajectories: ZombieTrajectoryEntry[]
}>()
</script>

<template>
  <BaseCard v-if="trajectories.length > 0">
    <div class="text-sm font-medium text-text-primary mb-4 cursor-help" title="Sprint-by-sprint status history showing how a zombie ticket moved (or didn't) through workflow stages.">Zombie Trajectories</div>
    <p class="text-xs text-text-muted mb-4">Tickets persisting across 3+ sprints, ordered by longevity.</p>
    <div class="flex flex-col gap-6">
      <div v-for="z in trajectories" :key="z.ticketKey">
        <div class="flex items-start justify-between mb-2">
          <div>
            <span class="font-mono text-xs text-text-primary mr-2">{{ z.ticketKey }}</span>
            <span class="text-sm text-text-primary">{{ z.summary }}</span>
          </div>
          <div class="flex items-center gap-2 flex-shrink-0 ml-4">
            <span class="text-xs text-text-muted">{{ z.issueType }}</span>
            <span v-if="z.storyPoints !== null" class="text-xs text-text-secondary tabular-nums">{{ z.storyPoints }} SP</span>
            <span class="bg-status-warning/10 text-status-warning text-xs rounded px-1.5 py-0.5">
              {{ z.sprintCount }} sprints
            </span>
          </div>
        </div>
        <!-- Sprint trajectory -->
        <div class="flex flex-wrap gap-2">
          <div
            v-for="(entry, idx) in z.sprints"
            :key="entry.sprintId"
            class="flex items-center gap-1"
          >
            <div class="bg-surface-elevated border border-border-default rounded px-2 py-1 text-xs">
              <div class="text-text-muted text-xs">{{ entry.sprintName }}</div>
              <div class="text-text-secondary">{{ entry.finalStatus }}</div>
            </div>
            <span v-if="idx < z.sprints.length - 1" class="text-text-muted text-xs">→</span>
          </div>
        </div>
      </div>
    </div>
  </BaseCard>
</template>
