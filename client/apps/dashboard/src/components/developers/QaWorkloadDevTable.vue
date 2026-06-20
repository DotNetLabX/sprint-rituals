<script setup lang="ts">
import { ref, computed } from 'vue'
import type { QaWorkloadEntry, QaWorkloadSingleEntry } from '../../types'
import { useDeltaDisplay } from '../../composables/useDeltaDisplay'
import BaseCard from '../BaseCard.vue'

const props = defineProps<{
  developers: QaWorkloadEntry[] | QaWorkloadSingleEntry[]
  mode: 'multi' | 'single'
}>()

const { deltaIcon, deltaClass } = useDeltaDisplay()

const PASS_RATE_GREEN = 90
const PASS_RATE_AMBER = 70

function passRateRagClass(rate: number): string {
  if (rate >= PASS_RATE_GREEN) return 'text-status-success'
  if (rate >= PASS_RATE_AMBER) return 'text-status-warning'
  return 'text-status-danger'
}

const sortColumn = ref<string | null>(null)
const sortDirection = ref<'asc' | 'desc'>('asc')

function toggleSort(column: string) {
  if (sortColumn.value === column) {
    sortDirection.value = sortDirection.value === 'asc' ? 'desc' : 'asc'
  } else {
    sortColumn.value = column
    sortDirection.value = 'asc'
  }
}

function sortIcon(column: string): string {
  if (sortColumn.value !== column) return ' ↕'
  return sortDirection.value === 'asc' ? ' ↑' : ' ↓'
}

function getVal(entry: QaWorkloadEntry | QaWorkloadSingleEntry, col: string): number {
  if (col === 'tesOwned') return entry.tesOwned
  if (col === 'runsCompleted') return entry.runsCompleted
  if (col === 'passCount') return entry.passCount
  if (col === 'failCount') return entry.failCount
  if (col === 'passRate') return entry.passRate
  if (col === 'storiesCovered') return entry.storiesCovered
  if (col === 'bugsFound') return entry.bugsFound
  return 0
}

const sortedDevelopers = computed(() => {
  const devs = [...props.developers] as (QaWorkloadEntry | QaWorkloadSingleEntry)[]
  const col = sortColumn.value
  const dir = sortDirection.value
  if (!col) return devs
  return devs.sort((a, b) => {
    if (a.accountId === null) return 1
    if (b.accountId === null) return -1
    const diff = getVal(a, col) - getVal(b, col)
    return dir === 'asc' ? diff : -diff
  })
})
</script>

<template>
  <BaseCard>
    <div class="overflow-x-auto">
      <table class="w-full text-sm">
        <thead>
          <tr class="text-text-muted text-left border-b border-border-default">
            <th class="pb-2 pr-4 font-medium">Person</th>
            <th class="pb-2 pr-4 font-medium">Sub-Team</th>
            <th
              class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary"
              :class="{ 'text-text-primary': sortColumn === 'tesOwned' }"
              title="Test executions assigned to this person. Counts ownership regardless of who ran the individual tests."
              @click="toggleSort('tesOwned')"
            >TEs Owned{{ sortIcon('tesOwned') }}</th>
            <th
              class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary"
              :class="{ 'text-text-primary': sortColumn === 'runsCompleted' }"
              title="Test runs this person executed with a final pass or fail result. Attributed by who ran the test."
              @click="toggleSort('runsCompleted')"
            >Runs Completed{{ sortIcon('runsCompleted') }}</th>
            <th
              class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary"
              :class="{ 'text-text-primary': sortColumn === 'passCount' }"
              title="Number of test runs this person executed that passed."
              @click="toggleSort('passCount')"
            >Pass{{ sortIcon('passCount') }}</th>
            <th
              class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary"
              :class="{ 'text-text-primary': sortColumn === 'failCount' }"
              title="Number of test runs this person executed that failed."
              @click="toggleSort('failCount')"
            >Fail{{ sortIcon('failCount') }}</th>
            <th
              class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary"
              :class="{ 'text-text-primary': sortColumn === 'passRate' }"
              title="Percentage of this person's completed runs that passed. Color indicates quality against thresholds."
              @click="toggleSort('passRate')"
            >Pass Rate{{ sortIcon('passRate') }}</th>
            <th
              class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary"
              :class="{ 'text-text-primary': sortColumn === 'storiesCovered' }"
              title="Unique sprint tickets linked to test executions where this person ran at least one test."
              @click="toggleSort('storiesCovered')"
            >Stories Covered{{ sortIcon('storiesCovered') }}</th>
            <th
              class="pb-2 font-medium text-right cursor-pointer select-none hover:text-text-primary"
              :class="{ 'text-text-primary': sortColumn === 'bugsFound' }"
              title="Bugs discovered through test executions this person owns. Credit goes to the TE owner."
              @click="toggleSort('bugsFound')"
            >Bugs Found{{ sortIcon('bugsFound') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="dev in sortedDevelopers"
            :key="dev.accountId ?? '__unassigned__'"
            class="border-b border-border-default last:border-0"
          >
            <!-- Person -->
            <td class="py-2 pr-4">
              <div class="flex items-center gap-2">
                <template v-if="dev.accountId !== null">
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
                </template>
                <template v-else>
                  <div class="w-6 h-6 rounded-full bg-surface-elevated shrink-0" />
                  <span class="text-text-muted italic">{{ dev.displayName }}</span>
                </template>
                <span
                  v-if="dev.workloadAlert.isActive"
                  class="text-status-warning cursor-help"
                  :title="`Handles >50% of test executions for ${dev.workloadAlert.consecutiveSprintCount} consecutive sprints.`"
                >
                  <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-4 h-4">
                    <path fill-rule="evenodd" d="M8.485 2.495c.673-1.167 2.357-1.167 3.03 0l6.28 10.875c.673 1.167-.17 2.625-1.516 2.625H3.72c-1.347 0-2.189-1.458-1.515-2.625L8.485 2.495zM10 5a.75.75 0 01.75.75v3.5a.75.75 0 01-1.5 0v-3.5A.75.75 0 0110 5zm0 9a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd" />
                  </svg>
                </span>
              </div>
            </td>
            <!-- Sub-Team -->
            <td class="py-2 pr-4">
              <span v-if="dev.subTeam" class="text-xs bg-surface-elevated rounded px-2 py-0.5 text-text-secondary">{{ dev.subTeam }}</span>
              <span v-else class="text-text-muted">—</span>
            </td>
            <!-- TEs Owned -->
            <td class="py-2 pr-4 text-right tabular-nums text-text-primary">
              <span>{{ dev.tesOwned }}</span>
              <span
                v-if="mode === 'single' && (dev as QaWorkloadSingleEntry).tesOwnedDelta !== null"
                :class="['ml-1 text-xs', deltaClass((dev as QaWorkloadSingleEntry).tesOwnedPolarity, (dev as QaWorkloadSingleEntry).tesOwnedDirection)]"
              >
                {{ deltaIcon((dev as QaWorkloadSingleEntry).tesOwnedDirection) }} {{ Math.abs((dev as QaWorkloadSingleEntry).tesOwnedDelta!) }}
              </span>
            </td>
            <!-- Runs Completed -->
            <td class="py-2 pr-4 text-right tabular-nums text-text-primary">
              <span>{{ dev.runsCompleted }}</span>
              <span
                v-if="mode === 'single' && (dev as QaWorkloadSingleEntry).runsCompletedDelta !== null"
                :class="['ml-1 text-xs', deltaClass((dev as QaWorkloadSingleEntry).runsCompletedPolarity, (dev as QaWorkloadSingleEntry).runsCompletedDirection)]"
              >
                {{ deltaIcon((dev as QaWorkloadSingleEntry).runsCompletedDirection) }} {{ Math.abs((dev as QaWorkloadSingleEntry).runsCompletedDelta!) }}
              </span>
            </td>
            <!-- Pass -->
            <td class="py-2 pr-4 text-right tabular-nums text-text-primary">
              <span>{{ dev.passCount }}</span>
              <span
                v-if="mode === 'single' && (dev as QaWorkloadSingleEntry).passCountDelta !== null"
                :class="['ml-1 text-xs', deltaClass((dev as QaWorkloadSingleEntry).passCountPolarity, (dev as QaWorkloadSingleEntry).passCountDirection)]"
              >
                {{ deltaIcon((dev as QaWorkloadSingleEntry).passCountDirection) }} {{ Math.abs((dev as QaWorkloadSingleEntry).passCountDelta!) }}
              </span>
            </td>
            <!-- Fail -->
            <td class="py-2 pr-4 text-right tabular-nums text-text-primary">
              <span>{{ dev.failCount }}</span>
              <span
                v-if="mode === 'single' && (dev as QaWorkloadSingleEntry).failCountDelta !== null"
                :class="['ml-1 text-xs', deltaClass((dev as QaWorkloadSingleEntry).failCountPolarity, (dev as QaWorkloadSingleEntry).failCountDirection)]"
              >
                {{ deltaIcon((dev as QaWorkloadSingleEntry).failCountDirection) }} {{ Math.abs((dev as QaWorkloadSingleEntry).failCountDelta!) }}
              </span>
            </td>
            <!-- Pass Rate -->
            <td class="py-2 pr-4 text-right tabular-nums" :class="passRateRagClass(dev.passRate)">
              <span>{{ dev.passRate.toFixed(1) }}%</span>
              <span
                v-if="mode === 'single' && (dev as QaWorkloadSingleEntry).passRateDelta !== null"
                :class="['ml-1 text-xs', deltaClass((dev as QaWorkloadSingleEntry).passRatePolarity, (dev as QaWorkloadSingleEntry).passRateDirection)]"
              >
                {{ deltaIcon((dev as QaWorkloadSingleEntry).passRateDirection) }} {{ Math.abs((dev as QaWorkloadSingleEntry).passRateDelta!).toFixed(1) }}
              </span>
            </td>
            <!-- Stories Covered -->
            <td class="py-2 pr-4 text-right tabular-nums text-text-primary">
              <span>{{ dev.storiesCovered }}</span>
              <span
                v-if="mode === 'single' && (dev as QaWorkloadSingleEntry).storiesCoveredDelta !== null"
                :class="['ml-1 text-xs', deltaClass((dev as QaWorkloadSingleEntry).storiesCoveredPolarity, (dev as QaWorkloadSingleEntry).storiesCoveredDirection)]"
              >
                {{ deltaIcon((dev as QaWorkloadSingleEntry).storiesCoveredDirection) }} {{ Math.abs((dev as QaWorkloadSingleEntry).storiesCoveredDelta!) }}
              </span>
            </td>
            <!-- Bugs Found -->
            <td class="py-2 text-right tabular-nums text-text-primary">
              <span>{{ dev.bugsFound }}</span>
              <span
                v-if="mode === 'single' && (dev as QaWorkloadSingleEntry).bugsFoundDelta !== null"
                :class="['ml-1 text-xs', deltaClass((dev as QaWorkloadSingleEntry).bugsFoundPolarity, (dev as QaWorkloadSingleEntry).bugsFoundDirection)]"
              >
                {{ deltaIcon((dev as QaWorkloadSingleEntry).bugsFoundDirection) }} {{ Math.abs((dev as QaWorkloadSingleEntry).bugsFoundDelta!) }}
              </span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </BaseCard>
</template>
