<script setup lang="ts">
import type { DeveloperQualityEntry, DeveloperQualitySprintBreakdown } from '../../types'
import { useDeltaDisplay } from '../../composables/useDeltaDisplay'
import { useQualityAverages } from '../../composables/useQualityAverages'

const props = defineProps<{
  dev: DeveloperQualityEntry
  isSingleSprint: boolean
  breakdown?: DeveloperQualitySprintBreakdown
}>()

const { deltaIcon, deltaClass } = useDeltaDisplay()
const { avgCoverage, avgPassRate, avgStories, avgCovered, avgUntested, avgBugsFound, sparklinePath } = useQualityAverages()

function ragClass(rag: string | null | undefined): string {
  if (rag === 'green') return 'text-status-success'
  if (rag === 'amber') return 'text-status-warning'
  if (rag === 'red') return 'text-status-danger'
  return 'text-text-primary'
}
</script>

<template>
  <tr class="border-b border-border-default last:border-0">
    <!-- Developer identity -->
    <td class="py-2 pr-4">
      <div class="flex items-center gap-2">
        <img
          v-if="dev.avatarUrl"
          :src="dev.avatarUrl"
          :alt="dev.displayName"
          class="w-6 h-6 rounded-full shrink-0"
        />
        <div v-else class="w-6 h-6 rounded-full bg-surface-elevated shrink-0 flex items-center justify-center text-xs text-text-muted">
          {{ dev.displayName.charAt(0).toUpperCase() }}
        </div>
        <span class="text-text-primary truncate">{{ dev.displayName }}</span>
        <span
          v-if="isSingleSprint && dev.belowMedianStreak !== null && dev.belowMedianStreak !== undefined"
          class="text-amber-400 text-xs cursor-help"
          :title="`Coverage below team median for ${dev.belowMedianStreak} consecutive sprints. Review in retro.`"
        >&#9651;</span>
      </div>
    </td>

    <!-- Sub-team -->
    <td class="py-2 pr-4 text-text-secondary">
      <span v-if="dev.subTeam" class="text-xs bg-surface-elevated rounded px-2 py-0.5">{{ dev.subTeam }}</span>
      <span v-else class="text-text-muted">—</span>
    </td>

    <!-- Single-sprint mode -->
    <template v-if="isSingleSprint">
      <template v-if="breakdown">
        <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ breakdown.stories }}</td>
        <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ breakdown.covered }}</td>

        <!-- Coverage % with RAG, delta, sparkline -->
        <td class="py-2 pr-4 text-right tabular-nums">
          <div class="flex items-center justify-end gap-1">
            <svg
              v-if="breakdown.coverageSparkline && breakdown.coverageSparkline.length >= 2"
              width="48" height="20" class="shrink-0 opacity-60"
            >
              <path
                :d="sparklinePath(breakdown.coverageSparkline)"
                fill="none" stroke="currentColor" stroke-width="1.5"
                class="text-accent-default"
              />
            </svg>
            <span :class="ragClass(breakdown.coverageRag)">{{ breakdown.coveragePercent.toFixed(1) }}%</span>
            <span
              v-if="breakdown.coveragePercentDelta !== null"
              :class="['text-xs', deltaClass(breakdown.coveragePercentDeltaPolarity, breakdown.coveragePercentDeltaDirection)]"
            >{{ deltaIcon(breakdown.coveragePercentDeltaDirection) }} {{ Math.abs(breakdown.coveragePercentDelta!).toFixed(1) }}</span>
          </div>
        </td>

        <!-- Pass Rate % with RAG, delta, sparkline -->
        <td class="py-2 pr-4 text-right tabular-nums">
          <div class="flex items-center justify-end gap-1">
            <svg
              v-if="breakdown.passRateSparkline && breakdown.passRateSparkline.length >= 2"
              width="48" height="20" class="shrink-0 opacity-60"
            >
              <path
                :d="sparklinePath(breakdown.passRateSparkline)"
                fill="none" stroke="currentColor" stroke-width="1.5"
                class="text-accent-default"
              />
            </svg>
            <span :class="ragClass(breakdown.passRateRag)">{{ breakdown.passRatePercent.toFixed(1) }}%</span>
            <span
              v-if="breakdown.passRatePercentDelta !== null"
              :class="['text-xs', deltaClass(breakdown.passRatePercentDeltaPolarity, breakdown.passRatePercentDeltaDirection)]"
            >{{ deltaIcon(breakdown.passRatePercentDeltaDirection) }} {{ Math.abs(breakdown.passRatePercentDelta!).toFixed(1) }}</span>
          </div>
        </td>

        <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ breakdown.untested }}</td>

        <!-- Bugs Found with neutral delta -->
        <td class="py-2 text-right tabular-nums text-text-primary">
          <span>{{ breakdown.bugsFound }}</span>
          <span
            v-if="breakdown.bugsFoundDelta !== null"
            class="ml-1 text-xs text-text-secondary"
          >{{ deltaIcon(breakdown.bugsFoundDeltaDirection) }} {{ Math.abs(breakdown.bugsFoundDelta!) }}</span>
        </td>
      </template>
      <template v-else>
        <td class="py-2 pr-4 text-right text-text-muted" colspan="6">—</td>
      </template>
    </template>

    <!-- Multi-sprint mode: averaged values -->
    <template v-else>
      <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ avgStories(dev).toFixed(1) }}</td>
      <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ avgCovered(dev).toFixed(1) }}</td>
      <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ avgCoverage(dev).toFixed(1) }}%</td>
      <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ avgPassRate(dev).toFixed(1) }}%</td>
      <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ avgUntested(dev).toFixed(1) }}</td>
      <td class="py-2 text-right tabular-nums text-text-primary">{{ avgBugsFound(dev).toFixed(1) }}</td>
    </template>
  </tr>
</template>
