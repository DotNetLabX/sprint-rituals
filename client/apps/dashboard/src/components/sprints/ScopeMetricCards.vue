<script setup lang="ts">
import BaseCard from '../BaseCard.vue'
import type { ScopeChangeSummaryMetrics, ScopeChangeSingleSprintMetrics, ScopeMetricCard } from '../../types'

const props = defineProps<{
  mode: 'multi' | 'single'
  summaryMetrics?: ScopeChangeSummaryMetrics | null
  singleMetrics?: ScopeChangeSingleSprintMetrics | null
}>()

function deltaIcon(direction: string | null | undefined): string {
  if (direction === 'up') return '▲'
  if (direction === 'down') return '▼'
  return '—'
}

function deltaClass(polarity: string | null | undefined, direction: string | null | undefined): string {
  if (direction === 'flat' || !direction) return 'text-text-secondary'
  if (polarity === 'positive') return 'text-status-success'
  if (polarity === 'negative') return 'text-status-danger'
  return 'text-text-secondary'
}

function singleCards(metrics: ScopeChangeSingleSprintMetrics): ScopeMetricCard[] {
  return [
    metrics.committedSpActive,
    metrics.committedSpTotal,
    metrics.addedSp,
    metrics.removedSp,
    metrics.netScopeChange,
    metrics.disruptionRate,
    metrics.bugCount
  ]
}

function singleCardTooltip(name: string): string {
  if (name === 'Committed SP (Active)') return 'Story points committed at sprint start. Active excludes tickets with excluded final statuses.'
  if (name === 'Committed SP (Total)') return 'Feature story points in the sprint when the planning window closed — what the team committed to going into execution.'
  if (name === 'Added SP') return 'Feature story points added after planning ended that entered the work cycle. Measures real disruption, not planning adjustments.'
  if (name === 'Removed SP') return 'Feature story points removed after planning ended that had entered the work cycle. Only counts started work that was pulled.'
  if (name === 'Net Scope Change') return 'Added SP minus Removed SP, both measured after planning ended. Positive means execution scope grew.'
  if (name === 'Disruption Rate') return 'Post-planning Added SP as a percentage of Active SP. Only counts additions that entered the cycle.'
  if (name === 'Bug Count') return 'Number of bug-type tickets added mid-sprint, regardless of story points.'
  return ''
}
</script>

<template>
  <!-- Multi-sprint summary cards -->
  <div v-if="mode === 'multi' && summaryMetrics" class="grid grid-cols-3 gap-4">
    <BaseCard>
      <div class="flex items-center gap-1 mb-1">
        <div class="text-xs text-text-muted">Avg Disruption Rate</div>
        <span
          class="text-text-muted cursor-help"
          title="Mean percentage of unplanned work added mid-sprint relative to committed scope, across selected sprints."
        >
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3.5 h-3.5">
            <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
          </svg>
        </span>
      </div>
      <div class="text-2xl font-semibold text-text-primary tabular-nums">
        {{ summaryMetrics.averageDisruptionRate.toFixed(1) }}%
      </div>
    </BaseCard>
    <BaseCard>
      <div class="flex items-center gap-1 mb-1">
        <div class="text-xs text-text-muted">Avg Net Scope Change</div>
        <span
          class="text-text-muted cursor-help"
          title="Mean difference between added and removed SP across selected sprints. Positive means sprints grew."
        >
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3.5 h-3.5">
            <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
          </svg>
        </span>
      </div>
      <div class="text-2xl font-semibold text-text-primary tabular-nums">
        {{ summaryMetrics.averageNetScopeChange >= 0 ? '+' : '' }}{{ summaryMetrics.averageNetScopeChange.toFixed(1) }} SP
      </div>
    </BaseCard>
    <BaseCard>
      <div class="flex items-center gap-1 mb-1">
        <div class="text-xs text-text-muted">Total Bugs Added</div>
        <span
          class="text-text-muted cursor-help"
          title="Count of bug-type tickets added mid-sprint across all selected sprints."
        >
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3.5 h-3.5">
            <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
          </svg>
        </span>
      </div>
      <div class="text-2xl font-semibold text-text-primary tabular-nums">
        {{ summaryMetrics.totalBugsAdded }}
      </div>
    </BaseCard>
  </div>

  <!-- Single-sprint metric cards -->
  <div v-else-if="mode === 'single' && singleMetrics" class="grid grid-cols-4 gap-4">
    <BaseCard v-for="card in singleCards(singleMetrics)" :key="card.name">
      <div class="text-xs text-text-muted mb-1 cursor-help" :title="singleCardTooltip(card.name)">{{ card.name }}</div>
      <div class="text-xl font-semibold text-text-primary tabular-nums">{{ card.displayValue }}</div>
      <div v-if="card.delta !== null" :class="['mt-1 text-xs', deltaClass(card.deltaPolarity, card.deltaDirection)]">
        {{ deltaIcon(card.deltaDirection) }} {{ Math.abs(card.delta).toFixed(1) }}
      </div>
    </BaseCard>
  </div>
</template>
