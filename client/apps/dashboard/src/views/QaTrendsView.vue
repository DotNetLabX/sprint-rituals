<script setup lang="ts">
import { onMounted, computed } from 'vue'
import { useQaTrendsStore } from '../stores/qaTrendsStore'
import PageLayout from '../components/PageLayout.vue'
import BaseCard from '../components/BaseCard.vue'
import BaseSelect from '../components/BaseSelect.vue'
import EmptyState from '../components/EmptyState.vue'
import QualityTrendsChart from '../components/qa/QualityTrendsChart.vue'
import TestingVolumeChart from '../components/qa/TestingVolumeChart.vue'
import DefectCorrelationSection from '../components/qa/DefectCorrelationSection.vue'
import type { SelectOption } from '../components/BaseSelect.vue'

const store = useQaTrendsStore()

onMounted(async () => {
  await store.initialize()
})

const lastOptions: SelectOption[] = [
  { label: 'Last 3', value: '3' },
  { label: 'Last 5', value: '5' },
  { label: 'Last 10', value: '10' },
  { label: 'All', value: '0' }
]

const selectedLastValue = computed({
  get: () => String(store.selectedLast),
  set: (val: string) => {
    store.selectLastN(Number(val))
  }
})

const subTeamOptions = computed<SelectOption[]>(() => [
  { label: 'All Teams', value: '' },
  ...store.subTeams.map(t => ({ label: t, value: t }))
])

const selectedSubTeamValue = computed({
  get: () => store.selectedSubTeam ?? '',
  set: (val: string) => {
    store.selectSubTeam(val === '' ? null : val)
  }
})
</script>

<template>
  <PageLayout title="QA Trends">
    <template #toolbar>
      <div class="flex items-center gap-3 flex-wrap">
        <!-- Sprint range selector -->
        <BaseSelect
          v-model="selectedLastValue"
          :options="lastOptions"
        />

        <!-- Sub-team filter -->
        <BaseSelect
          v-if="store.subTeams.length > 0"
          v-model="selectedSubTeamValue"
          :options="subTeamOptions"
        />
      </div>
    </template>

    <!-- Initializing -->
    <div v-if="store.initializing" class="flex items-center justify-center h-64">
      <div class="text-text-muted text-sm">Loading...</div>
    </div>

    <!-- No QA data -->
    <template v-else-if="store.qaTrends?.hasQaData === false">
      <EmptyState
        title="Not enough data to show trends"
        description="Sync at least 2 sprints with QA data."
      >
        <template #icon>
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-12 h-12">
            <path fill-rule="evenodd" d="M6 2a1 1 0 00-1 1v1H4a2 2 0 00-2 2v10a2 2 0 002 2h12a2 2 0 002-2V6a2 2 0 00-2-2h-1V3a1 1 0 10-2 0v1H7V3a1 1 0 00-1-1zm0 5a1 1 0 000 2h8a1 1 0 100-2H6z" clip-rule="evenodd" />
          </svg>
        </template>
      </EmptyState>
    </template>

    <!-- Content -->
    <template v-else-if="store.qaTrends?.hasQaData">
      <div class="flex flex-col gap-6">
        <!-- Updating indicator -->
        <div v-if="store.loading" class="text-xs text-text-muted">Updating...</div>

        <!-- Quality Trends chart -->
        <BaseCard>
          <QualityTrendsChart :quality-trends="store.qaTrends.qualityTrends" />
        </BaseCard>

        <!-- Testing Volume chart -->
        <BaseCard>
          <TestingVolumeChart :testing-volume="store.qaTrends.testingVolume" />
        </BaseCard>

        <!-- Defect Correlation section -->
        <BaseCard>
          <DefectCorrelationSection
            :defect-correlation="store.qaTrends.defectCorrelation"
            :has-qa-data="store.qaTrends.hasQaData"
          />
        </BaseCard>
      </div>
    </template>
  </PageLayout>
</template>
