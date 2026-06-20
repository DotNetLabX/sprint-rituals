<script setup lang="ts">
import type { BugRatioMultiSprintResponse, BugRatioSingleSprintResponse } from '../../types'
import { useDeltaDisplay } from '../../composables/useDeltaDisplay'
import BaseCard from '../BaseCard.vue'

defineProps<{
  mode: 'multi' | 'single'
  multi?: BugRatioMultiSprintResponse
  single?: BugRatioSingleSprintResponse
}>()

const { deltaIcon, deltaClass } = useDeltaDisplay()
</script>

<template>
  <!-- Multi-sprint: show totals without deltas -->
  <div v-if="mode === 'multi' && multi" class="grid grid-cols-3 gap-4">
    <BaseCard>
      <div class="flex items-center gap-1 mb-1">
        <div class="text-xs text-text-muted">Team Bug Ratio</div>
        <span
          class="text-text-muted cursor-help"
          title="Percentage of completed story points spent on bug fixes vs. all completed work across the selected sprints."
        >
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3.5 h-3.5">
            <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
          </svg>
        </span>
      </div>
      <div class="text-2xl font-bold text-text-primary tabular-nums">{{ multi.teamMetrics.bugRatioPercent.toFixed(1) }}%</div>
      <div class="text-xs text-text-muted mt-1">of completed SP</div>
    </BaseCard>
    <BaseCard>
      <div class="flex items-center gap-1 mb-1">
        <div class="text-xs text-text-muted">Total Bug SP</div>
        <span
          class="text-text-muted cursor-help"
          title="Story points completed on bug-type tickets vs. all other ticket types (stories, tasks, sub-tasks)."
        >
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3.5 h-3.5">
            <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
          </svg>
        </span>
      </div>
      <div class="text-2xl font-bold text-status-danger tabular-nums">{{ multi.teamMetrics.totalBugSp.toFixed(1) }}</div>
    </BaseCard>
    <BaseCard>
      <div class="flex items-center gap-1 mb-1">
        <div class="text-xs text-text-muted">Total Non-Bug SP</div>
        <span
          class="text-text-muted cursor-help"
          title="Story points completed on bug-type tickets vs. all other ticket types (stories, tasks, sub-tasks)."
        >
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3.5 h-3.5">
            <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
          </svg>
        </span>
      </div>
      <div class="text-2xl font-bold text-status-success tabular-nums">{{ multi.teamMetrics.totalNonBugSp.toFixed(1) }}</div>
    </BaseCard>
  </div>

  <!-- Single-sprint: metric cards with deltas -->
  <div v-else-if="mode === 'single' && single" class="grid grid-cols-3 gap-4">
    <BaseCard>
      <div class="text-xs text-text-muted mb-1 cursor-help" title="Percentage of completed story points spent on bug fixes vs. all completed work across the selected sprints.">{{ single.teamMetrics.bugRatioPercent.name }}</div>
      <div class="text-2xl font-bold text-text-primary tabular-nums">{{ single.teamMetrics.bugRatioPercent.displayValue }}</div>
      <div v-if="single.teamMetrics.bugRatioPercent.delta !== null" :class="['text-xs mt-1', deltaClass(single.teamMetrics.bugRatioPercent.deltaPolarity, single.teamMetrics.bugRatioPercent.deltaDirection)]">
        {{ deltaIcon(single.teamMetrics.bugRatioPercent.deltaDirection) }} {{ Math.abs(single.teamMetrics.bugRatioPercent.delta).toFixed(1) }}% vs prior
      </div>
    </BaseCard>
    <BaseCard>
      <div class="text-xs text-text-muted mb-1 cursor-help" title="Story points completed on bug-type tickets vs. all other ticket types (stories, tasks, sub-tasks).">{{ single.teamMetrics.bugSp.name }}</div>
      <div class="text-2xl font-bold text-status-danger tabular-nums">{{ single.teamMetrics.bugSp.displayValue }}</div>
      <div v-if="single.teamMetrics.bugSp.delta !== null" :class="['text-xs mt-1', deltaClass(single.teamMetrics.bugSp.deltaPolarity, single.teamMetrics.bugSp.deltaDirection)]">
        {{ deltaIcon(single.teamMetrics.bugSp.deltaDirection) }} {{ Math.abs(single.teamMetrics.bugSp.delta).toFixed(1) }} vs prior
      </div>
    </BaseCard>
    <BaseCard>
      <div class="text-xs text-text-muted mb-1 cursor-help" title="Story points completed on bug-type tickets vs. all other ticket types (stories, tasks, sub-tasks).">{{ single.teamMetrics.nonBugSp.name }}</div>
      <div class="text-2xl font-bold text-status-success tabular-nums">{{ single.teamMetrics.nonBugSp.displayValue }}</div>
      <div v-if="single.teamMetrics.nonBugSp.delta !== null" :class="['text-xs mt-1', deltaClass(single.teamMetrics.nonBugSp.deltaPolarity, single.teamMetrics.nonBugSp.deltaDirection)]">
        {{ deltaIcon(single.teamMetrics.nonBugSp.deltaDirection) }} {{ Math.abs(single.teamMetrics.nonBugSp.delta).toFixed(1) }} vs prior
      </div>
    </BaseCard>
  </div>
</template>
