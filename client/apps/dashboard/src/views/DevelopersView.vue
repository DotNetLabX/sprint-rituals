<script setup lang="ts">
import { onMounted, onUnmounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useDevelopersStore } from '../stores/developersStore'
import { useSettingsStore } from '../stores/settingsStore'
import { useDailyProgressStore } from '../stores/dailyProgressStore'
import PageLayout from '../components/PageLayout.vue'
import PageToolbar from '../components/PageToolbar.vue'
import EmptyState from '../components/EmptyState.vue'
import ThroughputTab from '../components/developers/ThroughputTab.vue'
import BugRatioTab from '../components/developers/BugRatioTab.vue'
import LeaderboardTab from '../components/developers/LeaderboardTab.vue'
import QualityTab from '../components/developers/QualityTab.vue'
import QaWorkloadTab from '../components/developers/QaWorkloadTab.vue'
import DailyProgressTab from '../components/developers/DailyProgressTab.vue'

const route = useRoute()
const router = useRouter()
const store = useDevelopersStore()
const settingsStore = useSettingsStore()
const dailyProgressStore = useDailyProgressStore()

onMounted(async () => {
  // Seed state from URL before initialize
  const sprintParam = route.query.sprint
  const lastParam = route.query.last
  const tabParam = route.query.tab

  if (lastParam) {
    const n = lastParam === 'all' ? null : Number(lastParam)
    store.sprintMode = 'multi'
    store.selectedLast = isNaN(n as number) ? null : n
  } else if (sprintParam && !isNaN(Number(sprintParam))) {
    store.sprintMode = 'single'
    store.selectedSprintId = Number(sprintParam)
  }

  if (tabParam === 'bug-ratio' || tabParam === 'bugRatio') {
    store.activeTab = 'bugRatio'
  } else if (tabParam === 'leaderboard') {
    store.activeTab = 'leaderboard'
  } else if (tabParam === 'quality') {
    store.activeTab = 'quality'
  } else if (tabParam === 'qa-workload') {
    store.activeTab = 'qaWorkload'
  } else if (tabParam === 'daily-progress') {
    store.activeTab = 'dailyProgress'
  }

  await Promise.all([store.initialize(), settingsStore.fetchSettings()])

  // If bug-ratio tab was requested and store initialized, load bug ratio data
  if (store.activeTab === 'bugRatio' && store.bugRatio === null) {
    await store.fetchBugRatio()
  }

  // If leaderboard tab was requested and store initialized, load leaderboard data
  if (store.activeTab === 'leaderboard' && store.leaderboard === null) {
    await store.fetchLeaderboard()
  }

  // If quality tab was requested and store initialized, load quality data
  if (store.activeTab === 'quality' && store.quality === null) {
    await store.fetchQuality()
  }

  // If qa-workload tab was requested and store initialized, load QA workload data
  if (store.activeTab === 'qaWorkload' && store.qaWorkload === null) {
    await store.fetchQaWorkload()
  }

  // If daily-progress tab was requested, fetch progress and start SignalR
  if (store.activeTab === 'dailyProgress' && dailyProgressStore.data === null) {
    await dailyProgressStore.fetchProgress(store.selectedSubTeam)
    await dailyProgressStore.startSignalR()
  }
})

onUnmounted(() => {
  if (store.activeTab === 'dailyProgress') {
    dailyProgressStore.stopSignalR()
  }
})

// URL sync
watch(
  () => [store.sprintMode, store.selectedSprintId, store.selectedLast, store.activeTab] as const,
  ([mode, sprintId, last, tab]) => {
    const query: Record<string, string> = {}

    if (mode === 'single' && sprintId !== null) {
      query.sprint = String(sprintId)
    } else if (mode === 'multi') {
      query.last = last === null ? 'all' : String(last)
    }

    if (tab === 'bugRatio') {
      query.tab = 'bug-ratio'
    } else if (tab === 'leaderboard') {
      query.tab = 'leaderboard'
    } else if (tab === 'quality') {
      query.tab = 'quality'
    } else if (tab === 'qaWorkload') {
      query.tab = 'qa-workload'
    } else if (tab === 'dailyProgress') {
      query.tab = 'daily-progress'
    }

    router.replace({ query })
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

function onTabSwitch(tab: 'throughput' | 'bugRatio' | 'leaderboard' | 'quality' | 'qaWorkload' | 'dailyProgress') {
  store.switchTab(tab)
}

function onCapacityChange(accountId: string, sprintId: number, value: number) {
  store.updateCapacity(accountId, sprintId, value)
}
</script>

<template>
  <PageLayout title="Developers">
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
        :disabled="store.activeTab === 'dailyProgress'"
        @update:selected-sprint-id="(id) => store.selectSprint(id)"
        @update:selected-sub-team="onSubTeamChange"
        @update:sprint-mode="onSprintModeChange"
      />
    </template>

    <!-- Initializing skeleton -->
    <div v-if="store.initializing" class="flex items-center justify-center h-64">
      <div class="text-text-muted text-sm">Loading...</div>
    </div>

    <!-- No closed sprints -->
    <template v-else-if="store.sprints.length === 0">
      <EmptyState
        title="No developer data yet"
        description="Sync a sprint to see developer metrics here."
      >
        <template #icon>
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-12 h-12">
            <path d="M9 6a3 3 0 11-6 0 3 3 0 016 0zM17 6a3 3 0 11-6 0 3 3 0 016 0zM12.93 17c.046-.327.07-.66.07-1a6.97 6.97 0 00-1.5-4.33A5 5 0 0119 16v1h-6.07zM6 11a5 5 0 015 5v1H1v-1a5 5 0 015-5z" />
          </svg>
        </template>
      </EmptyState>
    </template>

    <template v-else>
      <!-- Tab bar -->
      <div class="flex gap-1 border-b border-border-default mb-6">
        <button
          :class="[
            'px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors',
            store.activeTab === 'throughput'
              ? 'border-accent-default text-accent-default'
              : 'border-transparent text-text-secondary hover:text-text-primary'
          ]"
          @click="onTabSwitch('throughput')"
        >
          Throughput
        </button>
        <button
          :class="[
            'px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors',
            store.activeTab === 'bugRatio'
              ? 'border-accent-default text-accent-default'
              : 'border-transparent text-text-secondary hover:text-text-primary'
          ]"
          @click="onTabSwitch('bugRatio')"
        >
          Bug Ratio
        </button>
        <button
          :class="[
            'px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors',
            store.activeTab === 'leaderboard'
              ? 'border-accent-default text-accent-default'
              : 'border-transparent text-text-secondary hover:text-text-primary'
          ]"
          @click="onTabSwitch('leaderboard')"
        >
          Leaderboard
        </button>
        <button
          v-if="settingsStore.settings.xrayEnabled"
          :class="[
            'px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors',
            store.activeTab === 'quality'
              ? 'border-accent-default text-accent-default'
              : 'border-transparent text-text-secondary hover:text-text-primary'
          ]"
          @click="onTabSwitch('quality')"
        >
          Quality
        </button>
        <button
          v-if="settingsStore.settings.xrayEnabled"
          :class="[
            'px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors',
            store.activeTab === 'qaWorkload'
              ? 'border-accent-default text-accent-default'
              : 'border-transparent text-text-secondary hover:text-text-primary'
          ]"
          @click="onTabSwitch('qaWorkload')"
        >
          QA Workload
        </button>
        <button
          :class="[
            'px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors',
            store.activeTab === 'dailyProgress'
              ? 'border-accent-default text-accent-default'
              : 'border-transparent text-text-secondary hover:text-text-primary'
          ]"
          @click="onTabSwitch('dailyProgress')"
        >
          Daily Progress
        </button>
      </div>

      <!-- Throughput tab -->
      <ThroughputTab
        v-if="store.activeTab === 'throughput'"
        :throughput="store.throughput"
        :sprint-mode="store.sprintMode"
        :selected-last="store.selectedLast"
        :loading="store.loading"
        @capacity-change="onCapacityChange"
      />

      <!-- Bug Ratio tab -->
      <template v-else-if="store.activeTab === 'bugRatio'">
        <div v-if="store.bugRatioLoading" class="text-xs text-text-muted">Updating...</div>
        <div v-if="store.bugRatioError" class="text-sm text-status-danger">{{ store.bugRatioError }}</div>
        <BugRatioTab v-if="store.bugRatio" :data="store.bugRatio" />
      </template>

      <!-- Leaderboard tab -->
      <template v-else-if="store.activeTab === 'leaderboard'">
        <div v-if="store.leaderboardLoading" class="text-xs text-text-muted">Loading leaderboard data...</div>
        <div v-else-if="store.leaderboardError" class="text-sm text-status-danger">{{ store.leaderboardError }}</div>
        <LeaderboardTab v-else-if="store.leaderboard" :data="store.leaderboard" />
      </template>

      <!-- Quality tab -->
      <template v-else-if="store.activeTab === 'quality'">
        <div v-if="store.qualityLoading" class="text-xs text-text-muted">Updating...</div>
        <div v-if="store.qualityError" class="text-sm text-status-danger">{{ store.qualityError }}</div>
        <EmptyState
          v-else-if="store.quality && !store.quality.hasQaData"
          title="No QA data available"
          description="Sync sprint to load QA data."
        >
          <template #icon>
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-12 h-12">
              <path fill-rule="evenodd" d="M16.704 4.153a.75.75 0 01.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z" clip-rule="evenodd" />
            </svg>
          </template>
        </EmptyState>
        <QualityTab
          v-else-if="store.quality && store.quality.hasQaData"
          :data="store.quality"
          :sprint-mode="store.sprintMode"
        />
      </template>

      <!-- QA Workload tab -->
      <template v-else-if="store.activeTab === 'qaWorkload'">
        <div v-if="store.qaWorkloadLoading" class="text-xs text-text-muted">Updating...</div>
        <div v-if="store.qaWorkloadError" class="text-sm text-status-danger">{{ store.qaWorkloadError }}</div>
        <EmptyState
          v-else-if="store.qaWorkload && !store.qaWorkload.hasQaData"
          title="No QA data available"
          description="Sync sprint to load QA data."
        >
          <template #icon>
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-12 h-12">
              <path fill-rule="evenodd" d="M16.704 4.153a.75.75 0 01.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z" clip-rule="evenodd" />
            </svg>
          </template>
        </EmptyState>
        <QaWorkloadTab
          v-else-if="store.qaWorkload && store.qaWorkload.hasQaData"
          :data="store.qaWorkload"
          :sprint-mode="store.sprintMode"
        />
      </template>

      <!-- Daily Progress tab -->
      <template v-else-if="store.activeTab === 'dailyProgress'">
        <div v-if="dailyProgressStore.loading" class="text-xs text-text-muted">Loading...</div>
        <div v-else-if="dailyProgressStore.error" class="text-sm text-status-danger">{{ dailyProgressStore.error }}</div>
        <DailyProgressTab v-else-if="dailyProgressStore.data" :data="dailyProgressStore.data" />
      </template>
    </template>
  </PageLayout>
</template>
