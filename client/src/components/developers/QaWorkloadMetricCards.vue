<script setup lang="ts">
import type { QaWorkloadMultiSprintResponse, QaWorkloadSingleSprintResponse } from '../../types'
import { useDeltaDisplay } from '../../composables/useDeltaDisplay'
import BaseCard from '../BaseCard.vue'

defineProps<{
  mode: 'multi' | 'single'
  multi?: QaWorkloadMultiSprintResponse
  single?: QaWorkloadSingleSprintResponse
}>()

const { deltaIcon } = useDeltaDisplay()

const PASS_RATE_GREEN = 90
const PASS_RATE_AMBER = 70

function passRateRagClass(rate: number): string {
  if (rate >= PASS_RATE_GREEN) return 'text-status-success'
  if (rate >= PASS_RATE_AMBER) return 'text-status-warning'
  return 'text-status-danger'
}
</script>

<template>
  <!-- Multi-sprint team metrics (plain values) -->
  <div v-if="mode === 'multi' && multi" class="grid grid-cols-3 gap-4">
    <BaseCard>
      <div
        class="text-xs text-text-muted mb-1 cursor-help"
        title="Number of test executions linked to sprint tickets. Counts unique TEs, not individual test runs."
      >
        Total TEs
      </div>
      <div class="text-2xl font-semibold text-text-primary tabular-nums">{{ multi.teamMetrics.totalTes }}</div>
    </BaseCard>

    <BaseCard>
      <div
        class="text-xs text-text-muted mb-1 cursor-help"
        title="Total individual test runs with a final result (pass or fail) across all testers this sprint."
      >
        Total Runs Completed
      </div>
      <div class="text-2xl font-semibold text-text-primary tabular-nums">{{ multi.teamMetrics.totalRunsCompleted }}</div>
    </BaseCard>

    <BaseCard>
      <div
        class="text-xs text-text-muted mb-1 cursor-help"
        title="Percentage of completed test runs that passed. Only counts runs with a final pass or fail result."
      >
        Team Pass Rate
      </div>
      <div :class="['text-2xl font-semibold tabular-nums', passRateRagClass(multi.teamMetrics.teamPassRate)]">
        {{ multi.teamMetrics.teamPassRate.toFixed(1) }}%
      </div>
    </BaseCard>
  </div>

  <!-- Single-sprint team metrics (with deltas) -->
  <div v-else-if="mode === 'single' && single" class="grid grid-cols-3 gap-4">
    <BaseCard>
      <div
        class="text-xs text-text-muted mb-1 cursor-help"
        title="Number of test executions linked to sprint tickets. Counts unique TEs, not individual test runs."
      >
        {{ single.teamMetrics.totalTes.name }}
      </div>
      <div class="text-2xl font-semibold text-text-primary tabular-nums">{{ single.teamMetrics.totalTes.displayValue }}</div>
      <div v-if="single.teamMetrics.totalTes.delta !== null" class="text-xs text-text-secondary mt-1">
        {{ deltaIcon(single.teamMetrics.totalTes.deltaDirection) }}
        {{ Math.abs(single.teamMetrics.totalTes.delta) }}
      </div>
    </BaseCard>

    <BaseCard>
      <div
        class="text-xs text-text-muted mb-1 cursor-help"
        title="Total individual test runs with a final result (pass or fail) across all testers this sprint."
      >
        {{ single.teamMetrics.totalRunsCompleted.name }}
      </div>
      <div class="text-2xl font-semibold text-text-primary tabular-nums">{{ single.teamMetrics.totalRunsCompleted.displayValue }}</div>
      <div v-if="single.teamMetrics.totalRunsCompleted.delta !== null" class="text-xs text-text-secondary mt-1">
        {{ deltaIcon(single.teamMetrics.totalRunsCompleted.deltaDirection) }}
        {{ Math.abs(single.teamMetrics.totalRunsCompleted.delta) }}
      </div>
    </BaseCard>

    <BaseCard>
      <div
        class="text-xs text-text-muted mb-1 cursor-help"
        title="Percentage of completed test runs that passed. Only counts runs with a final pass or fail result."
      >
        {{ single.teamMetrics.teamPassRate.name }}
      </div>
      <div :class="['text-2xl font-semibold tabular-nums', single.teamMetrics.teamPassRate.rag === 'green' ? 'text-status-success' : single.teamMetrics.teamPassRate.rag === 'amber' ? 'text-status-warning' : 'text-status-danger']">
        {{ single.teamMetrics.teamPassRate.displayValue }}
      </div>
      <div v-if="single.teamMetrics.teamPassRate.delta !== null" class="text-xs text-text-secondary mt-1">
        {{ deltaIcon(single.teamMetrics.teamPassRate.deltaDirection) }}
        {{ Math.abs(single.teamMetrics.teamPassRate.delta).toFixed(1) }}%
      </div>
    </BaseCard>
  </div>
</template>
