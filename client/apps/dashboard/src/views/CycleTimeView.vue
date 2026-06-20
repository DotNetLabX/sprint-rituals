<script setup lang="ts">
import { onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useCycleTimeStore } from '../stores/cycleTimeStore'
import PageLayout from '../components/PageLayout.vue'
import PageToolbar from '../components/PageToolbar.vue'
import EmptyState from '../components/EmptyState.vue'
import CycleTimeMetricCards from '../components/cycle-time/CycleTimeMetricCards.vue'
import PercentileToggle from '../components/cycle-time/PercentileToggle.vue'
import CycleTimeScatterPlot from '../components/cycle-time/CycleTimeScatterPlot.vue'
import StageFunnel from '../components/cycle-time/StageFunnel.vue'
import CycleTimeIssueTypeTable from '../components/cycle-time/CycleTimeIssueTypeTable.vue'
import CycleTimeDeveloperTable from '../components/cycle-time/CycleTimeDeveloperTable.vue'
import CycleTimeOutlierTable from '../components/cycle-time/CycleTimeOutlierTable.vue'
import CycleTimeTrendChart from '../components/cycle-time/CycleTimeTrendChart.vue'
import CycleTimeSprintSummaryTable from '../components/cycle-time/CycleTimeSprintSummaryTable.vue'

const route = useRoute()
const router = useRouter()
const store = useCycleTimeStore()

onMounted(async () => {
  // Seed state from URL before initialize
  const sprintParam = route.query.sprint
  const lastParam = route.query.last

  if (lastParam) {
    const n = lastParam === 'all' ? null : Number(lastParam)
    store.sprintMode = 'multi'
    store.selectedLast = isNaN(n as number) ? null : n
  } else if (sprintParam && !isNaN(Number(sprintParam))) {
    store.sprintMode = 'single'
    store.selectedSprintId = Number(sprintParam)
  }

  await store.initialize()
})

// URL sync
watch(
  () => [store.sprintMode, store.selectedSprintId, store.selectedLast] as const,
  ([mode, sprintId, last]) => {
    if (mode === 'single' && sprintId !== null) {
      router.replace({ query: { sprint: String(sprintId) } })
    } else if (mode === 'multi') {
      if (last === null) {
        router.replace({ query: { last: 'all' } })
      } else {
        router.replace({ query: { last: String(last) } })
      }
    }
  }
)

function onSprintModeChange(payload: { mode: 'single'; sprintId: number } | { mode: 'multi'; last: number | null }) {
  if (payload.mode === 'single') {
    store.selectSprint(payload.sprintId)
  } else {
    store.selectLastN(payload.last)
  }
}

function onSubTeamChange(subTeam: string | null) {
  store.selectSubTeam(subTeam)
}

function onSprintBarClick(sprintId: number) {
  store.selectSprint(sprintId)
}
</script>

<template>
  <PageLayout title="Cycle Time">
    <template #toolbar>
      <PageToolbar
        :sprints="store.sprints"
        :selected-sprint-id="store.selectedSprintId"
        :sub-teams="store.subTeams"
        :selected-sub-team="store.selectedSubTeam"
        :show-sub-team-filter="true"
        :show-aggregate-options="true"
        :sprint-mode="store.sprintMode"
        :selected-last="store.selectedLast"
        @update:selected-sprint-id="(id) => store.selectSprint(id)"
        @update:selected-sub-team="onSubTeamChange"
        @update:sprint-mode="onSprintModeChange"
      />
    </template>

    <!-- Initializing -->
    <div v-if="store.initializing" class="flex items-center justify-center h-64">
      <div class="text-text-muted text-sm">Loading...</div>
    </div>

    <!-- Empty: no closed sprints -->
    <template v-else-if="store.sprints.length === 0">
      <EmptyState
        title="No sprint data yet"
        description="Sync a sprint to see cycle time analytics here."
      >
        <template #icon>
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-12 h-12">
            <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-12a1 1 0 10-2 0v4a1 1 0 00.293.707l2.828 2.829a1 1 0 101.415-1.415L11 9.586V6z" clip-rule="evenodd" />
          </svg>
        </template>
      </EmptyState>
    </template>

    <!-- Empty: no workflow stages configured (BR13) -->
    <template v-else-if="!store.hasWorkflowStages">
      <EmptyState
        title="No workflow stages configured"
        description="Configure workflow stages in Settings to enable cycle time measurement."
      >
        <template #icon>
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-12 h-12">
            <path fill-rule="evenodd" d="M11.49 3.17c-.38-1.56-2.6-1.56-2.98 0a1.532 1.532 0 01-2.286.948c-1.372-.836-2.942.734-2.106 2.106.54.886.061 2.042-.947 2.287-1.561.379-1.561 2.6 0 2.978a1.532 1.532 0 01.947 2.287c-.836 1.372.734 2.942 2.106 2.106a1.532 1.532 0 012.287.947c.379 1.561 2.6 1.561 2.978 0a1.533 1.533 0 012.287-.947c1.372.836 2.942-.734 2.106-2.106a1.533 1.533 0 01.947-2.287c1.561-.379 1.561-2.6 0-2.978a1.532 1.532 0 01-.947-2.287c.836-1.372-.734-2.942-2.106-2.106a1.532 1.532 0 01-2.287-.947zM10 13a3 3 0 100-6 3 3 0 000 6z" clip-rule="evenodd" />
          </svg>
        </template>
      </EmptyState>
    </template>

    <!-- Content -->
    <template v-else>
      <div class="flex flex-col gap-6">
        <div v-if="store.loading" class="text-xs text-text-muted">Updating...</div>

        <!-- Single-sprint mode -->
        <template v-if="store.cycleTime?.mode === 'single' && store.cycleTime.singleSprint">
          <CycleTimeMetricCards
            :metric-cards="store.cycleTime.singleSprint.metricCards"
            :selected-percentile="store.selectedPercentile"
          />

          <!-- Percentile toggle (BR22: single-sprint only) -->
          <PercentileToggle
            :model-value="store.selectedPercentile"
            @update:model-value="store.selectPercentile"
          />

          <CycleTimeScatterPlot
            :data-points="store.cycleTime.singleSprint.scatterPlot"
            :selected-percentile="store.selectedPercentile"
          />

          <StageFunnel
            :stages="store.cycleTime.singleSprint.stageFunnel"
          />

          <CycleTimeIssueTypeTable
            :entries="store.cycleTime.singleSprint.issueTypeBreakdown"
          />

          <CycleTimeDeveloperTable
            :entries="store.cycleTime.singleSprint.developerBreakdown"
          />

          <CycleTimeOutlierTable
            :entries="store.cycleTime.singleSprint.outliers"
          />
        </template>

        <!-- Multi-sprint mode -->
        <template v-else-if="store.cycleTime?.mode === 'multi' && store.cycleTime.multiSprint">
          <CycleTimeMetricCards
            :metric-cards="store.cycleTime.multiSprint.metricCards"
          />

          <CycleTimeTrendChart
            :trend="store.cycleTime.multiSprint.trend"
          />

          <StageFunnel
            :stages="store.cycleTime.multiSprint.stageFunnel"
          />

          <CycleTimeSprintSummaryTable
            :summaries="store.cycleTime.multiSprint.sprintSummaries"
            @sprint-click="onSprintBarClick"
          />
        </template>
      </div>
    </template>
  </PageLayout>
</template>
