<script setup lang="ts">
import { onMounted, watch, ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useDashboardStore } from '../stores/dashboardStore'
import { useSettingsStore } from '../stores/settingsStore'
import PageLayout from '../components/PageLayout.vue'
import PageToolbar from '../components/PageToolbar.vue'
import BaseCard from '../components/BaseCard.vue'
import EmptyState from '../components/EmptyState.vue'
import HealthScoreBadge from '../components/dashboard/HealthScoreBadge.vue'
import MetricCardComponent from '../components/dashboard/MetricCard.vue'
import SprintFlags from '../components/dashboard/SprintFlags.vue'
import InfoTooltip from '../components/InfoTooltip.vue'
import QaMetricsSection from '../components/dashboard/QaMetricsSection.vue'

const route = useRoute()
const router = useRouter()
const store = useDashboardStore()
const settingsStore = useSettingsStore()

const leaderboardMode = ref<'features' | 'bugs'>('features')

onMounted(async () => {
  // Seed selectedSprintId from URL before initialize so it's used on first fetch
  const sprintParam = route.query.sprint
  if (sprintParam && !isNaN(Number(sprintParam))) {
    store.selectedSprintId = Number(sprintParam)
  }
  leaderboardMode.value = 'features'
  await Promise.all([store.initialize(), settingsStore.fetchSettings()])
  if (settingsStore.settings.xrayEnabled && store.selectedSprintId !== null) {
    await store.fetchQaMetrics()
  }
})

// Keep URL in sync when sprint selection changes
watch(
  () => store.selectedSprintId,
  (id) => {
    if (id !== null) {
      router.replace({ query: { ...route.query, sprint: String(id) } })
    }
  }
)

async function onSprintChange(id: number) {
  await store.selectSprint(id)
  if (settingsStore.settings.xrayEnabled && store.selectedSprintId !== null) {
    await store.fetchQaMetrics()
  }
}

async function onSubTeamChange(subTeam: string | null) {
  await store.selectSubTeam(subTeam)
  if (settingsStore.settings.xrayEnabled && store.selectedSprintId !== null) {
    await store.fetchQaMetrics()
  }
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' })
}

const sortedLeaderboard = computed(() => {
  if (!store.summary?.leaderboard) return []
  const list = [...store.summary.leaderboard]
  if (leaderboardMode.value === 'features') {
    return list.sort((a, b) => b.featureSp - a.featureSp || a.displayName.localeCompare(b.displayName))
  } else {
    return list.sort((a, b) => b.bugSp - a.bugSp || a.displayName.localeCompare(b.displayName))
  }
})

function normalizedSp(spCompleted: number, capacityPercent: number): number | null {
  if (capacityPercent >= 100 || capacityPercent <= 0) return null
  return Math.round(spCompleted / (capacityPercent / 100))
}

const bugSpAnnotation = computed(() => {
  const bugSp = store.summary?.metrics?.bugSpCompleted
  if (!bugSp || bugSp === 0) return undefined
  return `(+${bugSp} bug SP)`
})
</script>

<template>
  <PageLayout title="Dashboard">
    <template #toolbar>
      <PageToolbar
        :sprints="store.sprints"
        :selected-sprint-id="store.selectedSprintId"
        :sub-teams="store.subTeams"
        :selected-sub-team="store.selectedSubTeam"
        :show-sub-team-filter="true"
        :show-aggregate-options="false"
        :show-sprint-info="false"
        @update:selected-sprint-id="onSprintChange"
        @update:selected-sub-team="onSubTeamChange"
      />
    </template>

    <!-- Initializing skeleton -->
    <div v-if="store.initializing" class="flex items-center justify-center h-64">
      <div class="text-text-muted text-sm">Loading...</div>
    </div>

    <!-- No closed sprints -->
    <template v-else-if="!store.summary?.sprint">
      <EmptyState
        title="No sprint data yet"
        description="Go to Settings to configure your Jira board, then sync a sprint."
        action-text="Go to Settings"
        action-route="/settings"
      >
        <template #icon>
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-12 h-12">
            <path d="M2 3a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1H3a1 1 0 01-1-1V3zm0 8a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1H3a1 1 0 01-1-1v-6zm8-8a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1h-6a1 1 0 01-1-1V3zm0 8a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1h-6a1 1 0 01-1-1v-6z" />
          </svg>
        </template>
      </EmptyState>
    </template>

    <!-- Summary content -->
    <template v-else>
      <div class="flex flex-col gap-6">

        <!-- Sprint header row -->
        <div class="flex items-center gap-3 flex-wrap">
          <h2 class="text-xl font-semibold text-text-primary">{{ store.summary.sprint.name }}</h2>
          <span class="text-text-secondary text-sm">
            {{ formatDate(store.summary.sprint.startDate) }} – {{ formatDate(store.summary.sprint.endDate) }}
          </span>
          <span class="text-xs bg-surface-elevated text-text-muted rounded px-2 py-0.5">
            {{ store.summary.sprint.durationDays }} days
          </span>
          <div v-if="store.loading" class="text-xs text-text-muted ml-2">Updating...</div>
        </div>

        <!-- Health score -->
        <BaseCard v-if="store.summary.healthScore">
          <HealthScoreBadge :health-score="store.summary.healthScore" />
        </BaseCard>

        <!-- Metric cards row -->
        <div v-if="store.summary.metrics" class="grid grid-cols-2 lg:grid-cols-5 gap-4">
          <MetricCardComponent :metric="store.summary.metrics.spCompleted" :annotation="bugSpAnnotation" />
          <MetricCardComponent :metric="store.summary.metrics.completionRate" />
          <MetricCardComponent :metric="store.summary.metrics.scopeDisruptionRate" />
          <MetricCardComponent :metric="store.summary.metrics.bugDisruptionRate" />
          <MetricCardComponent :metric="store.summary.metrics.carryOverRate" />
        </div>

        <!-- QA metrics section (Xray enabled + data available) -->
        <QaMetricsSection
          v-if="settingsStore.settings.xrayEnabled && store.qaMetrics !== null"
          :qa-metrics="store.qaMetrics!"
          :untested-tickets="store.untestedTickets"
          :failing-tickets="store.failingTickets"
          :qa-loading="store.qaLoading"
          @load-untested="store.fetchUntestedTickets()"
          @load-failing="store.fetchFailingTickets()"
        />

        <!-- Top epics -->
        <BaseCard v-if="store.summary.topEpics.length > 0">
          <div class="flex items-center gap-1 mb-3">
            <div class="text-sm font-medium text-text-primary">Top Epics</div>
            <InfoTooltip text="Up to 3 epics with the most story points completed in this sprint." />
          </div>
          <div class="flex flex-col gap-3">
            <div
              v-for="epic in store.summary.topEpics"
              :key="epic.epicName"
              class="flex flex-col gap-1"
            >
              <div class="flex items-center justify-between text-sm">
                <span class="text-text-primary truncate mr-4">{{ epic.epicName }}</span>
                <span class="text-text-secondary shrink-0">{{ epic.spCompletedThisSprint }} SP this sprint</span>
              </div>
              <div class="flex items-center gap-2">
                <div class="flex-1 h-1.5 bg-surface-elevated rounded-full overflow-hidden">
                  <div
                    class="h-full bg-accent-default rounded-full"
                    :style="{ width: `${Math.min(epic.completionPercentage, 100)}%` }"
                  />
                </div>
                <span class="text-xs text-text-muted shrink-0">
                  {{ epic.doneSp }}/{{ epic.totalSp }} SP ({{ epic.completionPercentage }}%)
                </span>
              </div>
            </div>
          </div>
        </BaseCard>

        <!-- Developer leaderboard -->
        <BaseCard v-if="store.summary.leaderboard.length > 0">
          <!-- Header row: title + info icon + toggle -->
          <div class="flex items-center justify-between mb-3">
            <div class="flex items-center gap-1">
              <div class="text-sm font-medium text-text-primary">
                {{ leaderboardMode === 'features' ? 'Developer Leaderboard — Features' : 'Developer Leaderboard — Bugs' }}
              </div>
              <InfoTooltip :text="leaderboardMode === 'features'
                  ? 'Active developers ranked by feature story points completed this sprint. Bug fixes excluded.'
                  : 'Active developers ranked by bug fix story points completed this sprint. Feature work excluded.'" />
            </div>
            <!-- Toggle group -->
            <div class="flex items-center gap-1">
              <InfoTooltip text="Switch between feature work and bug fix rankings. Features shows planned delivery; Bugs shows rework." />
              <div class="flex rounded overflow-hidden border border-border-default text-xs">
                <button
                  :class="[
                    'px-2.5 py-1 transition-colors',
                    leaderboardMode === 'features'
                      ? 'bg-accent-default text-white'
                      : 'text-text-secondary hover:text-text-primary'
                  ]"
                  @click="leaderboardMode = 'features'"
                >Features</button>
                <button
                  :class="[
                    'px-2.5 py-1 transition-colors',
                    leaderboardMode === 'bugs'
                      ? 'bg-accent-default text-white'
                      : 'text-text-secondary hover:text-text-primary'
                  ]"
                  @click="leaderboardMode = 'bugs'"
                >Bugs</button>
              </div>
            </div>
          </div>
          <!-- Column legend -->
          <div class="flex justify-end gap-3 text-xs text-text-muted mb-1">
            <span class="flex items-center gap-0.5">
              SP
              <InfoTooltip text="Story points completed for the selected work type. Numbers in parentheses (~N) show estimated SP at full capacity. Excludes unestimated tickets." />
            </span>
            <span class="flex items-center gap-0.5">
              t
              <InfoTooltip text="Number of done tickets for the selected work type. Includes tickets with no story points." />
            </span>
          </div>
          <!-- Leaderboard rows -->
          <div class="flex flex-col gap-2">
            <div
              v-for="(dev, index) in sortedLeaderboard"
              :key="dev.displayName"
              class="flex items-center gap-3 text-sm"
            >
              <span class="text-text-muted w-5 text-right tabular-nums shrink-0">{{ index + 1 }}</span>
              <img
                v-if="dev.avatarUrl"
                :src="dev.avatarUrl"
                :alt="dev.displayName"
                class="w-7 h-7 rounded-full shrink-0"
              />
              <div v-else class="w-7 h-7 rounded-full bg-surface-elevated shrink-0 flex items-center justify-center text-xs text-text-muted">
                {{ dev.displayName.charAt(0).toUpperCase() }}
              </div>
              <span class="flex-1 text-text-primary truncate">{{ dev.displayName }}</span>
              <span v-if="dev.subTeam" class="text-xs bg-surface-elevated text-text-muted rounded px-2 py-0.5 shrink-0">
                {{ dev.subTeam }}
              </span>
              <span class="text-text-secondary tabular-nums shrink-0 font-medium">
                {{ leaderboardMode === 'features' ? dev.featureSp : dev.bugSp }} SP
                <span
                  v-if="normalizedSp(leaderboardMode === 'features' ? dev.featureSp : dev.bugSp, dev.capacityPercent) !== null"
                  class="ml-1 text-xs text-text-muted font-normal"
                >(~{{ normalizedSp(leaderboardMode === 'features' ? dev.featureSp : dev.bugSp, dev.capacityPercent) }})</span>
              </span>
              <span class="text-accent-default/70 tabular-nums shrink-0">
                {{ leaderboardMode === 'features' ? dev.featureTickets : dev.bugTickets }}t
              </span>
            </div>
          </div>
        </BaseCard>

        <!-- Flags -->
        <BaseCard>
          <div class="text-sm font-medium text-text-primary mb-3">Flags</div>
          <SprintFlags :flags="store.summary.flags" />
        </BaseCard>

      </div>
    </template>
  </PageLayout>
</template>
