<script setup lang="ts">
import { computed } from 'vue'
import type { BugRatioResponse, BugRatioMultiSprintResponse, BugRatioSingleSprintResponse } from '../../types'
import BugRatioMetricCards from './BugRatioMetricCards.vue'
import BugRatioTrendChart from './BugRatioTrendChart.vue'
import BugRatioStackedChart from './BugRatioStackedChart.vue'
import BugRatioDevTable from './BugRatioDevTable.vue'
import BugRatioIssueTypeBreakdown from './BugRatioIssueTypeBreakdown.vue'

const props = defineProps<{
  data: BugRatioResponse
}>()

const isMulti = computed(() => props.data.mode === 'multi')
const multi = computed(() => props.data.multiSprint as BugRatioMultiSprintResponse | null)
const single = computed(() => props.data.singleSprint as BugRatioSingleSprintResponse | null)
</script>

<template>
  <div class="flex flex-col gap-6">
    <!-- Multi-sprint mode -->
    <template v-if="isMulti && multi">
      <BugRatioMetricCards :mode="'multi'" :multi="multi" />
      <BugRatioTrendChart :multi="multi" />
      <BugRatioStackedChart :multi="multi" />
      <BugRatioDevTable :mode="'multi'" :multi="multi" />
      <BugRatioIssueTypeBreakdown :issue-type-breakdown="multi.issueTypeBreakdown" />
    </template>

    <!-- Single-sprint mode -->
    <template v-else-if="!isMulti && single">
      <BugRatioMetricCards :mode="'single'" :single="single" />
      <BugRatioDevTable :mode="'single'" :single="single" />
      <BugRatioIssueTypeBreakdown :issue-type-breakdown="single.issueTypeBreakdown" />
    </template>
  </div>
</template>
