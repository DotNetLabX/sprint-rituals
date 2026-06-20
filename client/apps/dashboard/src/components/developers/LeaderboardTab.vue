<script setup lang="ts">
import { computed } from 'vue'
import type { LeaderboardResponse, LeaderboardMultiSprintResponse, LeaderboardSingleSprintResponse } from '../../types'
import BaseCard from '../BaseCard.vue'
import LeaderboardChart from './LeaderboardChart.vue'
import LeaderboardTable from './LeaderboardTable.vue'

const props = defineProps<{
  data: LeaderboardResponse
}>()

const isMulti = computed(() => props.data.mode === 'multi')
const multi = computed(() => props.data.multiSprint as LeaderboardMultiSprintResponse | null)
const single = computed(() => props.data.singleSprint as LeaderboardSingleSprintResponse | null)

const sortedMultiDevelopers = computed(() => {
  if (!multi.value) return []
  return [...multi.value.developers].sort((a, b) => b.totalSp - a.totalSp || a.displayName.localeCompare(b.displayName))
})

const sortedSingleDevelopers = computed(() => {
  if (!single.value) return []
  return [...single.value.developers].sort((a, b) => b.totalSp - a.totalSp || a.displayName.localeCompare(b.displayName))
})

const chartDevelopers = computed(() => {
  const devs = isMulti.value ? sortedMultiDevelopers.value : sortedSingleDevelopers.value
  return devs.map(d => ({
    displayName: d.displayName,
    featureSp: d.featureSp,
    bugSp: d.bugSp,
    totalSp: d.totalSp
  }))
})
</script>

<template>
  <div
    class="flex flex-col gap-6"
    title="Feature vs bug work composition per developer. Stacked bars show where sprint capacity went."
  >
    <!-- Multi-sprint mode -->
    <template v-if="isMulti && multi">
      <BaseCard>
        <div class="flex items-center gap-1 mb-4">
          <div class="text-sm font-medium text-text-primary">Work Composition</div>
          <span
            class="text-text-muted cursor-help"
            title="Feature vs bug work composition per developer. Stacked bars show where sprint capacity went."
          >
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3.5 h-3.5">
              <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
            </svg>
          </span>
        </div>
        <LeaderboardChart :developers="chartDevelopers" />
      </BaseCard>
      <LeaderboardTable :developers="sortedMultiDevelopers" mode="multi" />
    </template>

    <!-- Single-sprint mode -->
    <template v-else-if="!isMulti && single">
      <BaseCard>
        <div class="flex items-center gap-1 mb-4">
          <div class="text-sm font-medium text-text-primary">Work Composition — {{ single.sprint.name }}</div>
          <span
            class="text-text-muted cursor-help"
            title="Feature vs bug work composition per developer. Stacked bars show where sprint capacity went."
          >
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3.5 h-3.5">
              <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
            </svg>
          </span>
        </div>
        <LeaderboardChart :developers="chartDevelopers" />
      </BaseCard>
      <LeaderboardTable :developers="sortedSingleDevelopers" mode="single" />
    </template>
  </div>
</template>
