<script setup lang="ts">
import BaseCard from '../BaseCard.vue'
import type { CarryOverSummaryMetrics, CarryOverSingleSprintMetrics, ScopeMetricCard } from '../../types'

const props = defineProps<{
  mode: 'multi' | 'single'
  summaryMetrics?: CarryOverSummaryMetrics | null
  singleMetrics?: CarryOverSingleSprintMetrics | null
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

function singleCards(metrics: CarryOverSingleSprintMetrics): ScopeMetricCard[] {
  return [
    metrics.carryOverRate,
    metrics.carryOverSp,
    metrics.carryOverTicketCount
  ]
}

function singleCardTooltip(name: string): string {
  if (name === 'Carry-Over Rate') return 'Percentage of total sprint work (committed + added) not completed by sprint end.'
  if (name === 'Carry-Over SP') return 'Total story points on tickets not completed by sprint end.'
  if (name === 'Carry-Over Ticket Count') return 'Number of tickets not completed by sprint end.'
  return ''
}
</script>

<template>
  <!-- Multi-sprint summary cards -->
  <div v-if="mode === 'multi' && summaryMetrics" class="grid grid-cols-3 gap-4">
    <BaseCard>
      <div class="flex items-center gap-1 mb-1">
        <div class="text-xs text-text-muted">Avg Carry-Over Rate</div>
        <span
          class="text-text-muted cursor-help"
          title="The mean carry-over rate across the selected sprints."
        >
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3.5 h-3.5">
            <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
          </svg>
        </span>
      </div>
      <div class="text-2xl font-semibold text-text-primary tabular-nums">
        {{ summaryMetrics.averageCarryOverRate.toFixed(1) }}%
      </div>
    </BaseCard>
    <BaseCard>
      <div class="flex items-center gap-1 mb-1">
        <div class="text-xs text-text-muted">Avg Carry-Over SP</div>
        <span
          class="text-text-muted cursor-help"
          title="Total story points on tickets not completed by sprint end."
        >
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3.5 h-3.5">
            <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
          </svg>
        </span>
      </div>
      <div class="text-2xl font-semibold text-text-primary tabular-nums">
        {{ summaryMetrics.averageCarryOverSp.toFixed(1) }}
      </div>
    </BaseCard>
    <BaseCard>
      <div class="flex items-center gap-1 mb-1">
        <div class="text-xs text-text-muted">Total Zombie Tickets</div>
        <span
          class="text-text-muted cursor-help"
          title="Count of tickets appearing in 3 or more sprints within the selected range."
        >
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3.5 h-3.5">
            <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
          </svg>
        </span>
      </div>
      <div class="text-2xl font-semibold text-text-primary tabular-nums">
        {{ summaryMetrics.totalZombieTickets }}
      </div>
    </BaseCard>
  </div>

  <!-- Single-sprint metric cards with deltas -->
  <div v-else-if="mode === 'single' && singleMetrics" class="grid grid-cols-3 gap-4">
    <BaseCard v-for="card in singleCards(singleMetrics)" :key="card.name">
      <div class="text-xs text-text-muted mb-1 cursor-help" :title="singleCardTooltip(card.name)">{{ card.name }}</div>
      <div class="text-xl font-semibold text-text-primary tabular-nums">{{ card.displayValue }}</div>
      <div v-if="card.delta !== null" :class="['mt-1 text-xs', deltaClass(card.deltaPolarity, card.deltaDirection)]">
        {{ deltaIcon(card.deltaDirection) }} {{ Math.abs(card.delta).toFixed(1) }}
      </div>
    </BaseCard>
  </div>
</template>
