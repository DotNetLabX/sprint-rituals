<script setup lang="ts">
import BaseCard from '../BaseCard.vue'
import type { CycleTimeMetricCard } from '../../types'

defineProps<{
  metricCards: CycleTimeMetricCard[] | null
  selectedPercentile?: number
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

function cardTooltip(name: string): string {
  if (name === 'Median Cycle Time') return 'Half of completed tickets finish faster than this. Lower means the team is moving quicker.'
  if (name === 'P85 Cycle Time') return '85% of tickets complete within this many days. Use this number for stakeholder commitments.'
  if (name === 'Throughput') return 'Number of tickets that completed the full workflow cycle this sprint.'
  if (name === 'Outliers') return 'Tickets taking more than 2x the sprint median. Investigate root causes in retrospectives.'
  return ''
}
</script>

<template>
  <div v-if="metricCards && metricCards.length > 0" class="grid grid-cols-4 gap-4">
    <BaseCard v-for="card in metricCards" :key="card.name">
      <div class="text-xs text-text-muted mb-1 cursor-help" :title="cardTooltip(card.name)">{{ card.name }}</div>
      <div class="text-xl font-semibold text-text-primary tabular-nums">{{ card.displayValue }}</div>
      <div v-if="card.delta !== null" :class="['mt-1 text-xs', deltaClass(card.deltaPolarity, card.deltaDirection)]">
        {{ deltaIcon(card.deltaDirection) }} {{ Math.abs(card.delta).toFixed(1) }}
      </div>
    </BaseCard>
  </div>
</template>
