<script setup lang="ts">
import { computed } from 'vue'
import type { DeveloperQualityResponse } from '../../types'
import QualityDevTable from './QualityDevTable.vue'
import QualityTrendChart from './QualityTrendChart.vue'

const props = defineProps<{
  data: DeveloperQualityResponse
  sprintMode: 'single' | 'multi'
}>()

const isSingleSprint = computed(() => props.sprintMode === 'single')
</script>

<template>
  <div class="flex flex-col gap-6">
    <!-- Single-sprint mode -->
    <template v-if="isSingleSprint">
      <QualityDevTable
        :developers="data.developers"
        :sprints="data.sprints"
        :is-single-sprint="true"
      />
    </template>

    <!-- Multi-sprint mode -->
    <template v-else>
      <QualityDevTable
        :developers="data.developers"
        :sprints="data.sprints"
        :is-single-sprint="false"
      />
      <QualityTrendChart
        v-if="data.developers.length > 0 && data.sprints.length > 0"
        :developers="data.developers"
        :sprints="data.sprints"
      />
    </template>
  </div>
</template>
