<script setup lang="ts">
import { onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useDeveloperDetailStore } from '../stores/developerDetailStore'
import PageLayout from '../components/PageLayout.vue'
import SprintTrendsChart from '../components/developer-detail/SprintTrendsChart.vue'
import WorkAllocationChart from '../components/developer-detail/WorkAllocationChart.vue'
import CurrentSprintSection from '../components/developer-detail/CurrentSprintSection.vue'

const route = useRoute()
const router = useRouter()
const store = useDeveloperDetailStore()

const accountId = computed(() => route.params.accountId as string)

onMounted(async () => {
  store.reset()
  await store.fetchDetail(accountId.value)
})

async function onSprintRangeChange(last: number) {
  await store.setSprintRange(accountId.value, last)
}

function goBack() {
  router.push({ name: 'developers', query: { tab: 'daily-progress' } })
}
</script>

<template>
  <PageLayout :title="store.data ? store.data.developer.displayName : 'Developer Detail'">
    <!-- Loading state -->
    <div v-if="store.loading" class="flex items-center justify-center h-64">
      <div class="text-text-muted text-sm">Loading...</div>
    </div>

    <!-- Error state -->
    <div v-else-if="store.error" class="flex flex-col items-center justify-center h-64 gap-4">
      <div class="text-status-danger text-sm">{{ store.error }}</div>
      <button
        class="text-sm text-accent-default hover:underline"
        @click="goBack"
      >
        Back to Developers
      </button>
    </div>

    <!-- Developer not found (404) -->
    <div v-else-if="!store.data" class="flex flex-col items-center justify-center h-64 gap-4">
      <div class="text-text-muted text-sm">Developer not found.</div>
      <button
        class="text-sm text-accent-default hover:underline"
        @click="goBack"
      >
        Back to Developers
      </button>
    </div>

    <template v-else>
      <!-- Breadcrumb -->
      <nav class="flex items-center gap-2 text-sm text-text-secondary mb-6">
        <router-link
          :to="{ name: 'developers', query: { tab: 'daily-progress' } }"
          class="hover:text-text-primary hover:underline"
        >
          Developers
        </router-link>
        <span class="text-text-muted">&rsaquo;</span>
        <span class="text-text-primary font-medium">{{ store.data.developer.displayName }}</span>
      </nav>

      <!-- Developer header -->
      <div class="flex items-center gap-3 mb-6">
        <img
          v-if="store.data.developer.avatarUrl"
          :src="store.data.developer.avatarUrl"
          :alt="store.data.developer.displayName"
          class="w-12 h-12 rounded-full shrink-0"
        />
        <div
          v-else
          class="w-12 h-12 rounded-full bg-surface-elevated flex items-center justify-center text-sm font-medium text-text-secondary shrink-0"
        >
          {{ store.data.developer.displayName.charAt(0).toUpperCase() }}
        </div>
        <div>
          <div class="text-lg font-semibold text-text-primary">{{ store.data.developer.displayName }}</div>
          <div class="text-sm text-text-muted">
            {{ store.data.developer.role }}
            <span v-if="store.data.developer.subTeam"> &middot; {{ store.data.developer.subTeam }}</span>
          </div>
        </div>
      </div>

      <!-- Sprint range selector -->
      <div class="flex items-center gap-2 mb-6">
        <span class="text-sm text-text-secondary">Sprint range:</span>
        <button
          v-for="option in [{ label: 'Last 5', value: 5 }, { label: 'Last 10', value: 10 }, { label: 'All', value: 0 }]"
          :key="option.value"
          :class="[
            'px-3 py-1 text-sm rounded border transition-colors',
            store.selectedLast === option.value
              ? 'border-accent-default text-accent-default bg-accent-default/10'
              : 'border-border-default text-text-secondary hover:text-text-primary hover:border-border-hover'
          ]"
          @click="onSprintRangeChange(option.value)"
        >
          {{ option.label }}
        </button>
      </div>

      <!-- Charts and sections -->
      <div class="space-y-8">
        <SprintTrendsChart :sprint-trends="store.data.sprintTrends" />
        <WorkAllocationChart
          :sprint-trends="store.data.sprintTrends"
          :work-allocation="store.data.workAllocation"
          :bug-ratio-target="store.data.bugRatioTarget"
        />
        <CurrentSprintSection
          :current-sprint="store.data.currentSprint"
          :jira-instance-url="store.data.jiraInstanceUrl"
        />
      </div>
    </template>
  </PageLayout>
</template>
