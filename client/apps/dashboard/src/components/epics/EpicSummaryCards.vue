<script setup lang="ts">
import BaseCard from '../BaseCard.vue'
import type { EpicProgressUnlinkedWork } from '../../types'

defineProps<{
  activeEpicCount: number
  averageCompletion: number
  averageTestCoverage: number | null
  unlinkedWork: EpicProgressUnlinkedWork
  activeFilter: 'active' | 'completed'
  hasQaData: boolean
}>()
</script>

<template>
  <div class="grid grid-cols-3 gap-4" :class="{ 'lg:grid-cols-4': hasQaData && averageTestCoverage !== null }">
    <!-- Active / Completed Epics count -->
    <BaseCard>
      <div class="flex items-center gap-1 mb-1">
        <div class="text-xs text-text-muted">
          {{ activeFilter === 'completed' ? 'Completed Epics' : 'Active Epics' }}
        </div>
        <span
          class="text-text-muted cursor-help"
          title="Number of epics with remaining work."
        >
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3.5 h-3.5">
            <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
          </svg>
        </span>
      </div>
      <div class="text-2xl font-semibold text-text-primary tabular-nums">
        {{ activeEpicCount }}
      </div>
    </BaseCard>

    <!-- Average Completion -->
    <BaseCard>
      <div class="flex items-center gap-1 mb-1">
        <div class="text-xs text-text-muted">Average Completion</div>
        <span
          class="text-text-muted cursor-help"
          title="Mean SP completion across all active epics."
        >
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3.5 h-3.5">
            <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
          </svg>
        </span>
      </div>
      <div class="text-2xl font-semibold text-text-primary tabular-nums">
        {{ averageCompletion.toFixed(1) }}%
      </div>
      <div class="mt-2 h-1.5 bg-surface-elevated rounded-full overflow-hidden">
        <div
          class="h-full bg-accent-default rounded-full transition-all"
          :style="{ width: `${Math.min(averageCompletion, 100)}%` }"
        />
      </div>
    </BaseCard>

    <!-- Unlinked Work — not affected by search (BR5) -->
    <BaseCard>
      <div class="flex items-center gap-1 mb-1">
        <div class="text-xs text-text-muted">Unlinked Work</div>
        <span
          class="text-text-muted cursor-help"
          title="Tickets in sprints that aren't tied to any epic."
        >
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3.5 h-3.5">
            <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
          </svg>
        </span>
      </div>
      <div class="text-2xl font-semibold text-text-primary tabular-nums">
        {{ unlinkedWork.ticketCount }}
      </div>
      <div class="text-xs text-text-muted mt-0.5">
        {{ unlinkedWork.totalSp > 0 ? `${unlinkedWork.totalSp.toFixed(1)} SP` : 'No SP' }}
      </div>
    </BaseCard>

    <!-- Average Test Coverage (only when hasQaData and at least one epic has non-null coverage) -->
    <BaseCard v-if="hasQaData && averageTestCoverage !== null">
      <div class="flex items-center gap-1 mb-1">
        <div class="text-xs text-text-muted">Average Test Coverage</div>
        <span
          class="text-text-muted cursor-help"
          title="Mean test coverage across visible epics. Epics with no feature tickets are excluded."
        >
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3.5 h-3.5">
            <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
          </svg>
        </span>
      </div>
      <div class="text-2xl font-semibold text-text-primary tabular-nums">
        {{ `${averageTestCoverage.toFixed(1)}%` }}
      </div>
      <div class="mt-2 h-1.5 bg-surface-elevated rounded-full overflow-hidden">
        <div
          class="h-full bg-emerald-500 rounded-full transition-all"
          :style="{ width: `${Math.min(averageTestCoverage, 100)}%` }"
        />
      </div>
    </BaseCard>
  </div>
</template>
