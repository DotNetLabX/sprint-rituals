<script setup lang="ts">
import { onMounted, watch, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useSprintsStore } from '../stores/sprintsStore'
import PageLayout from '../components/PageLayout.vue'
import PageToolbar from '../components/PageToolbar.vue'
import EmptyState from '../components/EmptyState.vue'
import ScopeMetricCards from '../components/sprints/ScopeMetricCards.vue'
import ClassificationTable from '../components/sprints/ClassificationTable.vue'
import ScopeChangeChart from '../components/sprints/ScopeChangeChart.vue'
import BurnupChart from '../components/sprints/BurnupChart.vue'
import EventTable from '../components/sprints/EventTable.vue'
import BugTimeTable from '../components/sprints/BugTimeTable.vue'
import CarryOverMetricCards from '../components/sprints/CarryOverMetricCards.vue'
import CarryOverRateChart from '../components/sprints/CarryOverRateChart.vue'
import CarryOverStackedChart from '../components/sprints/CarryOverStackedChart.vue'
import IssueTypeBreakdown from '../components/sprints/IssueTypeBreakdown.vue'
import ZombieSummaryTable from '../components/sprints/ZombieSummaryTable.vue'
import StatusDistributionChart from '../components/sprints/StatusDistributionChart.vue'
import CarryOverDestinationSection from '../components/sprints/CarryOverDestination.vue'
import CarryOverTicketTable from '../components/sprints/CarryOverTicketTable.vue'
import ZombieTrajectorySection from '../components/sprints/ZombieTrajectorySection.vue'
import TestExecutionBurnupChart from '../components/sprints/TestExecutionBurnupChart.vue'
import TestingCrunchSection from '../components/sprints/TestingCrunchSection.vue'
import PostSprintTestingSection from '../components/sprints/PostSprintTestingSection.vue'
import UntestedAtCloseSection from '../components/sprints/UntestedAtCloseSection.vue'
import DevToTestGapSection from '../components/sprints/DevToTestGapSection.vue'

const route = useRoute()
const router = useRouter()
const store = useSprintsStore()

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

async function onSprintUpdated() {
  await store.refreshSprints()
  await store.fetchAllData()
}

const sprintEndDayNumber = computed(() => {
  const tl = store.testTimeline
  if (!tl) return 0
  const start = new Date(tl.sprintStartDate)
  const end = new Date(tl.sprintEndDate)
  return Math.round((end.getTime() - start.getTime()) / (1000 * 60 * 60 * 24)) + 1
})
</script>

<template>
  <PageLayout title="Sprints">
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
        :show-edit-button="true"
        @update:selected-sprint-id="(id) => store.selectSprint(id)"
        @update:selected-sub-team="onSubTeamChange"
        @update:sprint-mode="onSprintModeChange"
        @sprint-updated="onSprintUpdated"
      />
    </template>

    <!-- Initializing -->
    <div v-if="store.initializing" class="flex items-center justify-center h-64">
      <div class="text-text-muted text-sm">Loading...</div>
    </div>

    <!-- No closed sprints -->
    <template v-else-if="store.sprints.length === 0">
      <EmptyState
        title="No sprint data yet"
        description="Sync a sprint to see sprint analytics here."
      >
        <template #icon>
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-12 h-12">
            <path fill-rule="evenodd" d="M4 2a1 1 0 011 1v2.101a7.002 7.002 0 0111.601 2.566 1 1 0 11-1.885.666A5.002 5.002 0 005.999 7H9a1 1 0 010 2H4a1 1 0 01-1-1V3a1 1 0 011-1zm.008 9.057a1 1 0 011.276.61A5.002 5.002 0 0014.001 13H11a1 1 0 110-2h5a1 1 0 011 1v5a1 1 0 11-2 0v-2.101a7.002 7.002 0 01-11.601-2.566 1 1 0 01.61-1.276z" clip-rule="evenodd" />
          </svg>
        </template>
      </EmptyState>
    </template>

    <!-- Scope change content -->
    <template v-else>
      <div class="flex flex-col gap-6">

        <!-- Loading indicator -->
        <div v-if="store.loading" class="text-xs text-text-muted">Updating...</div>

        <!-- Multi-sprint trend view -->
        <template v-if="store.scopeChange?.mode === 'multi' && store.scopeChange.multiSprint">
          <ScopeMetricCards
            mode="multi"
            :summary-metrics="store.scopeChange.multiSprint.summaryMetrics"
          />

          <ScopeChangeChart
            v-if="store.scopeChange.multiSprint.perSprintData.length > 0"
            :sprints="store.scopeChange.multiSprint.sprints"
            :per-sprint-data="store.scopeChange.multiSprint.perSprintData"
            @sprint-click="onSprintBarClick"
          />

          <ClassificationTable
            :entries="store.scopeChange.multiSprint.classificationBreakdown"
          />
        </template>

        <!-- Single-sprint detail view -->
        <template v-else-if="store.scopeChange?.mode === 'single' && store.scopeChange.singleSprint">
          <ScopeMetricCards
            mode="single"
            :single-metrics="store.scopeChange.singleSprint.metrics"
          />

          <BurnupChart
            v-if="store.scopeChange.singleSprint.burnupData.length > 0"
            :burnup-data="store.scopeChange.singleSprint.burnupData"
          />

          <ClassificationTable
            :entries="store.scopeChange.singleSprint.classificationBreakdown"
          />

          <EventTable
            :events="store.scopeChange.singleSprint.events"
          />

          <BugTimeTable
            :bugs="store.scopeChange.singleSprint.bugTimeInProgress"
          />
        </template>

        <!-- Carry-Over Analysis section separator -->
        <div v-if="store.carryOver" class="pt-2">
          <div class="text-base font-semibold text-text-primary mb-4 border-t border-border-default pt-6">
            Carry-Over Analysis
          </div>

          <!-- Multi-sprint carry-over -->
          <template v-if="store.carryOver.mode === 'multi' && store.carryOver.multiSprint">
            <div class="flex flex-col gap-6">
              <CarryOverMetricCards
                mode="multi"
                :summary-metrics="store.carryOver.multiSprint.summaryMetrics"
              />

              <CarryOverRateChart
                v-if="store.carryOver.multiSprint.perSprintData.length > 0"
                :sprints="store.carryOver.multiSprint.sprints"
                :per-sprint-data="store.carryOver.multiSprint.perSprintData"
                @sprint-click="onSprintBarClick"
              />

              <CarryOverStackedChart
                v-if="store.carryOver.multiSprint.perSprintData.length > 0"
                :sprints="store.carryOver.multiSprint.sprints"
                :per-sprint-data="store.carryOver.multiSprint.perSprintData"
                @sprint-click="onSprintBarClick"
              />

              <IssueTypeBreakdown
                :entries="store.carryOver.multiSprint.issueTypeBreakdown"
              />

              <ZombieSummaryTable
                :zombies="store.carryOver.multiSprint.zombieTickets"
              />
            </div>
          </template>

          <!-- Single-sprint carry-over -->
          <template v-else-if="store.carryOver.mode === 'single' && store.carryOver.singleSprint">
            <div class="flex flex-col gap-6">
              <CarryOverMetricCards
                mode="single"
                :single-metrics="store.carryOver.singleSprint.metrics"
              />

              <StatusDistributionChart
                :distribution="store.carryOver.singleSprint.statusDistribution"
              />

              <IssueTypeBreakdown
                :entries="store.carryOver.singleSprint.issueTypeBreakdown"
              />

              <CarryOverDestinationSection
                :destination="store.carryOver.singleSprint.carryOverDestination"
              />

              <CarryOverTicketTable
                :tickets="store.carryOver.singleSprint.tickets"
              />

              <ZombieTrajectorySection
                :trajectories="store.carryOver.singleSprint.zombieTrajectories"
              />
            </div>
          </template>
        </div>

        <!-- Test Execution Timeline section — single-sprint only, Xray disabled = hidden -->
        <div v-if="store.sprintMode === 'single' && store.testTimeline?.isXrayEnabled" class="pt-2">
          <div class="text-base font-semibold text-text-primary mb-4 border-t border-border-default pt-6">
            Test Execution Timeline
          </div>

          <!-- Xray enabled but sprint not yet synced with QA data -->
          <div v-if="!store.testTimeline.hasQaData" class="text-sm text-text-muted">
            Sync sprint to load QA data.
          </div>

          <!-- QA data available -->
          <div v-else class="flex flex-col gap-6">
            <TestExecutionBurnupChart
              :burnup-data="store.testTimeline.burnupData"
              :scope-change-overlay="store.testTimeline.scopeChangeOverlay"
              :planning-window-days="store.testTimeline.planningWindowDays"
              :sprint-end-day-number="sprintEndDayNumber"
            />
            <TestingCrunchSection :crunch="store.testTimeline.testingCrunch" />
            <PostSprintTestingSection :post-sprint="store.testTimeline.postSprintTesting" />
            <UntestedAtCloseSection :untested="store.testTimeline.untestedAtClose" />
            <DevToTestGapSection :gap="store.testTimeline.devToTestGap" />
          </div>
        </div>

      </div>
    </template>
  </PageLayout>
</template>
