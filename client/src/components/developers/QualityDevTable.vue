<script setup lang="ts">
import { ref, computed } from 'vue'
import type { DeveloperQualityEntry, DeveloperQualitySprintInfo } from '../../types'
import { useQualityAverages } from '../../composables/useQualityAverages'
import BaseCard from '../BaseCard.vue'
import QualityDevTableRow from './QualityDevTableRow.vue'

const props = defineProps<{
  developers: DeveloperQualityEntry[]
  sprints: DeveloperQualitySprintInfo[]
  isSingleSprint: boolean
}>()

const { avgCoverage, avgPassRate, avgStories, avgCovered, avgUntested, avgBugsFound } = useQualityAverages()

// --- Sorting ---
const sortColumn = ref<string>('coveragePercent')
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

// --- Single-sprint breakdown map ---
const singleBreakdownMap = computed(() => {
  const map = new Map<string, import('../../types').DeveloperQualitySprintBreakdown>()
  if (props.sprints.length === 0) return map
  const sprintId = props.sprints[0].id
  for (const dev of props.developers) {
    const bd = dev.sprintBreakdowns.find(b => b.sprintId === sprintId)
    if (bd) map.set(dev.accountId, bd)
  }
  return map
})

// --- Sort value ---
function sortValue(dev: DeveloperQualityEntry): number {
  if (props.isSingleSprint) {
    const bd = singleBreakdownMap.value.get(dev.accountId)
    if (!bd) return 0
    if (sortColumn.value === 'coveragePercent') return bd.coveragePercent
    if (sortColumn.value === 'passRatePercent') return bd.passRatePercent
    if (sortColumn.value === 'stories') return bd.stories
    if (sortColumn.value === 'covered') return bd.covered
    if (sortColumn.value === 'untested') return bd.untested
    if (sortColumn.value === 'bugsFound') return bd.bugsFound
  } else {
    if (sortColumn.value === 'coveragePercent') return avgCoverage(dev)
    if (sortColumn.value === 'passRatePercent') return avgPassRate(dev)
    if (sortColumn.value === 'stories') return avgStories(dev)
    if (sortColumn.value === 'covered') return avgCovered(dev)
    if (sortColumn.value === 'untested') return avgUntested(dev)
    if (sortColumn.value === 'bugsFound') return avgBugsFound(dev)
  }
  return 0
}

const sortedDevelopers = computed(() => {
  const devs = [...props.developers]
  const dir = sortDirection.value
  return devs.sort((a, b) => {
    const diff = sortValue(a) - sortValue(b)
    return dir === 'asc' ? diff : -diff
  })
})
</script>

<template>
  <BaseCard>
    <div class="flex items-center gap-1 mb-4">
      <div class="text-sm font-medium text-text-primary">Developer Story Quality</div>
    </div>
    <div class="overflow-x-auto">
      <table class="w-full text-sm">
        <thead>
          <tr class="text-text-muted text-left border-b border-border-default">
            <th class="pb-2 pr-4 font-medium">Developer</th>
            <th class="pb-2 pr-4 font-medium">Sub-Team</th>
            <th
              class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary"
              :class="{ 'text-text-primary': sortColumn === 'stories' }"
              title="Total feature stories assigned to this developer in the sprint scope."
              @click="toggleSort('stories')"
            >Stories{{ sortIcon('stories') }}</th>
            <th
              class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary"
              :class="{ 'text-text-primary': sortColumn === 'covered' }"
              title="Stories with at least one linked test execution."
              @click="toggleSort('covered')"
            >Covered{{ sortIcon('covered') }}</th>
            <th
              class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary"
              :class="{ 'text-text-primary': sortColumn === 'coveragePercent' }"
              title="Percentage of this developer's stories that have linked test executions."
              @click="toggleSort('coveragePercent')"
            >Coverage %{{ sortIcon('coveragePercent') }}</th>
            <th
              class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary"
              :class="{ 'text-text-primary': sortColumn === 'passRatePercent' }"
              title="Percentage of test runs that passed across all test executions on this developer's stories."
              @click="toggleSort('passRatePercent')"
            >Pass Rate %{{ sortIcon('passRatePercent') }}</th>
            <th
              class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary"
              :class="{ 'text-text-primary': sortColumn === 'untested' }"
              title="Stories with no linked test executions — the coverage gap for this developer."
              @click="toggleSort('untested')"
            >Untested{{ sortIcon('untested') }}</th>
            <th
              class="pb-2 font-medium text-right cursor-pointer select-none hover:text-text-primary"
              :class="{ 'text-text-primary': sortColumn === 'bugsFound' }"
              title="Bugs discovered during testing of this developer's stories. Delta is neutral — more is not inherently good or bad."
              @click="toggleSort('bugsFound')"
            >Bugs Found{{ sortIcon('bugsFound') }}</th>
          </tr>
        </thead>
        <tbody>
          <QualityDevTableRow
            v-for="dev in sortedDevelopers"
            :key="dev.accountId"
            :dev="dev"
            :is-single-sprint="isSingleSprint"
            :breakdown="singleBreakdownMap.get(dev.accountId)"
          />
        </tbody>
      </table>
    </div>
  </BaseCard>
</template>
