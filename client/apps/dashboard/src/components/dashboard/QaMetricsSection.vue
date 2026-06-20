<script setup lang="ts">
import { ref } from 'vue'
import type { QaMetricsResponse, UntestedTicket, FailingTicket } from '../../types'
import MetricCardComponent from './MetricCard.vue'
import BaseCard from '../BaseCard.vue'
import InfoTooltip from '../InfoTooltip.vue'

const props = defineProps<{
  qaMetrics: QaMetricsResponse
  untestedTickets: UntestedTicket[]
  failingTickets: FailingTicket[]
  qaLoading: boolean
}>()

const emit = defineEmits<{
  loadUntested: []
  loadFailing: []
}>()

const untestedExpanded = ref(false)
const failingExpanded = ref(false)

function toggleUntested() {
  untestedExpanded.value = !untestedExpanded.value
  if (untestedExpanded.value && props.untestedTickets.length === 0) {
    emit('loadUntested')
  }
}

function toggleFailing() {
  failingExpanded.value = !failingExpanded.value
  if (failingExpanded.value && props.failingTickets.length === 0) {
    emit('loadFailing')
  }
}

</script>

<template>
  <BaseCard>
    <!-- Section header -->
    <div class="flex items-center gap-1 mb-4">
      <div class="text-sm font-medium text-text-primary">Test Quality</div>
      <InfoTooltip text="Weighted combination of Coverage Rate and Pass Rate scores. Weights are configurable in Settings." />
    </div>

    <!-- 3 metric cards -->
    <div class="grid grid-cols-3 gap-4 mb-4">
      <MetricCardComponent :metric="qaMetrics.coverageRate" />
      <MetricCardComponent :metric="qaMetrics.executionRate" />
      <MetricCardComponent :metric="qaMetrics.passRate" />
    </div>

    <!-- Bugs found (hidden when 0) -->
    <div v-if="qaMetrics.bugsFound > 0" class="flex items-center gap-2 text-sm text-text-secondary mb-4">
      <span class="flex items-center gap-1 font-medium text-text-primary">
        {{ qaMetrics.bugsFound }}
        <InfoTooltip text="Bugs discovered through test execution (linked via &quot;Blocks&quot; in Jira). Counts unique bugs only." />
      </span>
      <span>bugs found</span>
    </div>

    <!-- Untested tickets expandable -->
    <div class="border-t border-border-default pt-3">
      <button
        class="flex items-center gap-1 text-sm text-text-secondary hover:text-text-primary transition-colors w-full text-left"
        @click="toggleUntested"
      >
        <svg
          :class="['w-3.5 h-3.5 transition-transform', untestedExpanded ? 'rotate-90' : '']"
          xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor"
        >
          <path fill-rule="evenodd" d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z" clip-rule="evenodd" />
        </svg>
        <span>Untested Tickets</span>
        <span class="ml-1 text-xs bg-surface-elevated text-text-muted rounded px-1.5 py-0.5">{{ qaMetrics.untestedCount }}</span>
        <InfoTooltip text="Feature tickets with no linked test executions. Sorted by story points — highest risk first." />
      </button>

      <div v-if="untestedExpanded" class="mt-2">
        <div v-if="qaLoading" class="text-xs text-text-muted">Loading...</div>
        <div v-else-if="untestedTickets.length === 0" class="text-xs text-text-muted">No untested tickets.</div>
        <div v-else class="flex flex-col gap-1.5">
          <div
            v-for="ticket in untestedTickets"
            :key="ticket.ticketKey"
            class="flex items-center gap-2 text-xs"
          >
            <span class="text-text-muted font-mono shrink-0">{{ ticket.ticketKey }}</span>
            <span class="text-text-primary truncate flex-1">{{ ticket.summary }}</span>
            <span v-if="ticket.storyPoints !== null" class="text-text-muted shrink-0">{{ ticket.storyPoints }} SP</span>
            <span v-if="ticket.assigneeName" class="text-text-muted shrink-0">{{ ticket.assigneeName }}</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Failing tickets expandable -->
    <div class="border-t border-border-default pt-3 mt-3">
      <button
        class="flex items-center gap-1 text-sm text-text-secondary hover:text-text-primary transition-colors w-full text-left"
        @click="toggleFailing"
      >
        <svg
          :class="['w-3.5 h-3.5 transition-transform', failingExpanded ? 'rotate-90' : '']"
          xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor"
        >
          <path fill-rule="evenodd" d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z" clip-rule="evenodd" />
        </svg>
        <span>Failing Tickets</span>
        <span class="ml-1 text-xs bg-surface-elevated text-text-muted rounded px-1.5 py-0.5">{{ qaMetrics.failingCount }}</span>
        <InfoTooltip text="Feature tickets where at least one test execution has a failing test run. Shows fail/total counts." />
      </button>

      <div v-if="failingExpanded" class="mt-2">
        <div v-if="qaLoading" class="text-xs text-text-muted">Loading...</div>
        <div v-else-if="failingTickets.length === 0" class="text-xs text-text-muted">No failing tickets.</div>
        <div v-else class="flex flex-col gap-1.5">
          <div
            v-for="ticket in failingTickets"
            :key="ticket.ticketKey"
            class="flex items-center gap-2 text-xs"
          >
            <span class="text-text-muted font-mono shrink-0">{{ ticket.ticketKey }}</span>
            <span class="text-text-primary truncate flex-1">{{ ticket.summary }}</span>
            <span v-if="ticket.storyPoints !== null" class="text-text-muted shrink-0">{{ ticket.storyPoints }} SP</span>
            <span v-if="ticket.assigneeName" class="text-text-muted shrink-0">{{ ticket.assigneeName }}</span>
            <span class="text-status-danger shrink-0">{{ ticket.failedRunCount }}/{{ ticket.totalRunCount }}</span>
          </div>
        </div>
      </div>
    </div>
  </BaseCard>
</template>
