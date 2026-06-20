<script setup lang="ts">
import { computed } from 'vue'
import type { QaWorkloadResponse } from '../../types'
import QaWorkloadMetricCards from './QaWorkloadMetricCards.vue'
import QaWorkloadDistributionChart from './QaWorkloadDistributionChart.vue'
import QaWorkloadTrendChart from './QaWorkloadTrendChart.vue'
import QaWorkloadDevTable from './QaWorkloadDevTable.vue'
import BaseCard from '../BaseCard.vue'

const props = defineProps<{
  data: QaWorkloadResponse
  sprintMode: 'single' | 'multi'
}>()

const isMulti = computed(() => props.sprintMode === 'multi')
const multi = computed(() => props.data.multiSprint)
const single = computed(() => props.data.singleSprint)
const hasDevelopers = computed(() => {
  if (isMulti.value) return (multi.value?.developers.length ?? 0) > 0
  return (single.value?.developers.length ?? 0) > 0
})
</script>

<template>
  <div class="flex flex-col gap-6">
    <!-- Empty state -->
    <BaseCard v-if="!hasDevelopers">
      <div class="text-center py-6 text-text-muted">
        No developers with QA workload data in the selected sprint scope.
      </div>
    </BaseCard>

    <template v-else>
      <QaWorkloadMetricCards :mode="isMulti ? 'multi' : 'single'" :multi="multi ?? undefined" :single="single ?? undefined" />
      <QaWorkloadDistributionChart :developers="isMulti ? (multi?.developers ?? []) : (single?.developers ?? [])" />
      <QaWorkloadTrendChart
        v-if="isMulti && multi && multi.sprints.length > 1"
        :sprints="multi.sprints"
        :developers="multi.developers"
      />
      <QaWorkloadDevTable
        :developers="isMulti ? (multi?.developers ?? []) : (single?.developers ?? [])"
        :mode="isMulti ? 'multi' : 'single'"
      />
    </template>
  </div>
</template>
