<script setup lang="ts">
import { onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useEpicsStore } from '../stores/epicsStore'
import PageLayout from '../components/PageLayout.vue'
import PageToolbar from '../components/PageToolbar.vue'
import EmptyState from '../components/EmptyState.vue'
import EpicSummaryCards from '../components/epics/EpicSummaryCards.vue'
import EpicTable from '../components/epics/EpicTable.vue'
import EpicActiveCompletedToggle from '../components/epics/EpicActiveCompletedToggle.vue'
import EpicColumnToggle from '../components/epics/EpicColumnToggle.vue'
import type { ColumnDef } from '../components/epics/EpicColumnToggle.vue'

const route = useRoute()
const router = useRouter()
const store = useEpicsStore()

const COLUMN_DEFS: ColumnDef[] = [
  { id: 'progress', label: 'Progress' },
  { id: 'spDoneTotal', label: 'SP Done / Total' },
  { id: 'tickets', label: 'Tickets' },
  { id: 'velocity', label: 'Velocity' },
  { id: 'projected', label: 'Projected' },
  { id: 'startedDate', label: 'Started' },
  { id: 'lastWorkDate', label: 'Last Work' },
  { id: 'coverageRate', label: 'Coverage %' },
  { id: 'passRate', label: 'Pass Rate %' },
  { id: 'bugsFound', label: 'Bugs Found' },
]

onMounted(async () => {
  // Seed state from URL before initialize
  const subTeamParam = route.query.subTeam
  const filterParam = route.query.filter

  if (subTeamParam && typeof subTeamParam === 'string') {
    store.selectedSubTeam = subTeamParam
  }

  if (filterParam === 'completed') {
    store.activeFilter = 'completed'
  }

  await store.initialize()
})

// URL sync
watch(
  () => [store.selectedSubTeam, store.activeFilter] as const,
  ([subTeam, filter]) => {
    const query: Record<string, string> = {}
    if (subTeam) query.subTeam = subTeam
    if (filter === 'completed') query.filter = 'completed'
    router.replace({ query })
  }
)

function onSubTeamChange(subTeam: string | null) {
  store.selectSubTeam(subTeam)
}
</script>

<template>
  <PageLayout title="Epics">
    <template #toolbar>
      <PageToolbar
        :sub-teams="store.subTeams"
        :selected-sub-team="store.selectedSubTeam"
        :show-sub-team-filter="true"
        :show-sprint-selector="false"
        @update:selected-sub-team="onSubTeamChange"
      />
    </template>

    <!-- Initializing -->
    <div v-if="store.initializing" class="flex items-center justify-center h-64">
      <div class="text-text-muted text-sm">Loading...</div>
    </div>

    <!-- Empty state: no epics in system -->
    <template v-else-if="!store.hasEpics">
      <EmptyState
        title="No epic data yet"
        description="Sync a sprint to see epic progress here."
      >
        <template #icon>
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-12 h-12">
            <path d="M7 3a1 1 0 000 2h6a1 1 0 100-2H7zM4 7a1 1 0 011-1h10a1 1 0 110 2H5a1 1 0 01-1-1zM2 11a2 2 0 012-2h12a2 2 0 012 2v4a2 2 0 01-2 2H4a2 2 0 01-2-2v-4z" />
          </svg>
        </template>
      </EmptyState>
    </template>

    <!-- Content -->
    <template v-else>
      <div class="flex flex-col gap-6">
        <div v-if="store.loading" class="text-xs text-text-muted">Updating...</div>

        <!-- Summary cards — receive search-filtered values (BR5) except Unlinked Work -->
        <EpicSummaryCards
          v-if="store.epicProgress"
          :active-epic-count="store.searchSummaryMetrics.activeEpicCount"
          :average-completion="store.searchSummaryMetrics.averageCompletion"
          :average-test-coverage="store.searchSummaryMetrics.averageTestCoverage"
          :unlinked-work="store.epicProgress.unlinkedWork"
          :active-filter="store.activeFilter"
          :has-qa-data="store.epicProgress.hasQaData"
        />

        <!-- Toolbar: active/completed toggle + search + column toggle -->
        <div class="flex flex-wrap items-center gap-3">
          <EpicActiveCompletedToggle
            :active-filter="store.activeFilter"
            @update:active-filter="store.setActiveFilter"
          />

          <div class="flex-1 min-w-0">
            <input
              type="text"
              :value="store.searchQuery"
              placeholder="Search epics..."
              class="w-full max-w-xs px-3 py-1.5 text-sm bg-surface-elevated border border-border-default rounded text-text-primary placeholder-text-muted focus:outline-none focus:ring-1 focus:ring-accent-default"
              title="Filter epics by name or Jira key. Combines with the sub-team filter."
              @input="store.setSearchQuery(($event.target as HTMLInputElement).value)"
            />
          </div>

          <EpicColumnToggle
            :columns="COLUMN_DEFS"
            :hidden-columns="store.hiddenColumns"
            :has-qa-data="store.epicProgress?.hasQaData ?? false"
            @toggle="store.toggleColumnVisibility"
          />
        </div>

        <!-- Epic table -->
        <EpicTable
          :epics="store.sortedEpics"
          :expanded-epic-keys="store.expandedEpicKeys"
          :has-qa-data="store.epicProgress?.hasQaData ?? false"
          :sort-column="store.sortColumn"
          :sort-direction="store.sortDirection"
          :is-column-visible="store.isColumnVisible"
          @toggle-expand="store.toggleEpicExpanded"
          @sort="store.toggleSort"
        >
          <template #empty>
            <template v-if="store.searchQuery">
              No epics match your search.
            </template>
            <template v-else-if="store.activeFilter === 'active'">
              All epics are complete — switch to <button class="text-accent-default underline" @click="store.setActiveFilter('completed')">Completed</button> to view them.
            </template>
            <template v-else>
              No completed epics yet.
            </template>
          </template>
        </EpicTable>
      </div>
    </template>
  </PageLayout>
</template>
