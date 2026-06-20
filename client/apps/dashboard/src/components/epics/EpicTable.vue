<script setup lang="ts">
import { computed } from 'vue'
import type { EpicProgressEntry } from '../../types'
import BaseCard from '../BaseCard.vue'
import EpicTicketTable from './EpicTicketTable.vue'

const props = defineProps<{
  epics: EpicProgressEntry[]
  expandedEpicKeys: Set<string>
  hasQaData: boolean
  sortColumn: string
  sortDirection: 'asc' | 'desc'
  isColumnVisible: (columnId: string) => boolean
}>()

const emit = defineEmits<{
  'toggle-expand': [epicKey: string]
  'sort': [columnId: string]
}>()

// Compute visible column count for expanded-row colspan
// Always-visible: chevron (1) + epic name (1) = 2
// Optional columns: each counts 1 when visible
const OPTIONAL_COLUMNS = [
  'progress', 'spDoneTotal', 'tickets', 'velocity', 'projected',
  'startedDate', 'lastWorkDate', 'coverageRate', 'passRate', 'bugsFound',
]
const QA_COLUMNS = new Set(['coverageRate', 'passRate', 'bugsFound'])

const visibleColspan = computed(() => {
  let count = 2 // chevron + epic name always visible
  for (const col of OPTIONAL_COLUMNS) {
    if (QA_COLUMNS.has(col) && !props.hasQaData) continue
    if (props.isColumnVisible(col)) count++
  }
  return count
})

function spDisplay(entry: EpicProgressEntry): string {
  if (entry.adjustedTotalSp === null) return '—'
  const base = `${entry.doneSp.toFixed(1)} / ${entry.adjustedTotalSp.toFixed(1)} SP`
  if (entry.imputedSp !== null && entry.imputedSp > 0) {
    return `${base} (+~${entry.imputedSp.toFixed(1)} est.)`
  }
  return base
}

function velocityDisplay(entry: EpicProgressEntry): string {
  if (entry.velocity === null) return '—'
  return `${entry.velocity.toFixed(1)} SP/sprint`
}

function projectionDisplay(entry: EpicProgressEntry): string {
  if (entry.projectedSprintsRemaining === null) return 'Insufficient data'
  return `${entry.projectedSprintsRemaining.toFixed(1)} sprints`
}

function completionBarWidth(entry: EpicProgressEntry): string {
  if (entry.spCompletionPercentage !== null) {
    return `${Math.min(entry.spCompletionPercentage, 100)}%`
  }
  return `${Math.min(entry.ticketCompletionPercentage, 100)}%`
}

function imputedBarWidth(entry: EpicProgressEntry): string {
  if (entry.adjustedTotalSp === null || entry.adjustedTotalSp === 0 || entry.imputedSp === null || entry.imputedSp === 0) {
    return '0%'
  }
  const donePct = entry.spCompletionPercentage ?? 0
  const imputedPct = (entry.imputedSp / entry.adjustedTotalSp) * 100
  const remainingImputedPct = Math.max(0, Math.min(imputedPct, 100 - donePct))
  return `${remainingImputedPct.toFixed(1)}%`
}

function completionLabel(entry: EpicProgressEntry): string {
  if (entry.spCompletionPercentage !== null) {
    return `${entry.spCompletionPercentage.toFixed(1)}%`
  }
  return `${entry.ticketCompletionPercentage.toFixed(1)}%`
}

function ragClass(rag: string | null | undefined): string {
  if (rag === 'green') return 'text-status-success'
  if (rag === 'amber') return 'text-status-warning'
  if (rag === 'red') return 'text-status-danger'
  return 'text-text-primary'
}

// Date formatting: relative within 30 days, absolute beyond (BR spec Flow 4 step 6)
function formatActivityDate(isoDate: string | null): string {
  if (!isoDate) return '—'
  // Parse as local date — new Date('YYYY-MM-DD') is treated as UTC midnight and shifts the
  // displayed day by one in negative-offset timezones. Splitting and constructing avoids this.
  const [y, m, d] = isoDate.split('-').map(Number)
  const date = new Date(y, m - 1, d)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24))

  if (diffDays < 0) {
    // Future date — show absolute
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
  }

  if (diffDays <= 30) {
    if (diffDays === 0) return 'Today'
    if (diffDays === 1) return '1d ago'
    if (diffDays < 7) return `${diffDays}d ago`
    const weeks = Math.floor(diffDays / 7)
    if (diffDays < 30) return `${weeks}w ago`
    return '4w ago'
  }

  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
}

function sortIcon(columnId: string): 'asc' | 'desc' | null {
  if (props.sortColumn !== columnId) return null
  return props.sortDirection
}
</script>

<template>
  <div v-if="epics.length === 0" class="text-center py-12 text-text-muted text-sm">
    <slot name="empty" />
  </div>

  <BaseCard v-else class="overflow-hidden">
    <div class="overflow-x-auto">
      <table class="w-full text-sm">
        <thead>
          <tr class="text-text-muted text-left border-b border-border-default">
            <!-- Chevron — always visible, sticky -->
            <th class="pb-3 pr-2 font-medium w-6 sticky left-0 z-10 bg-surface-default"></th>

            <!-- Epic name — always visible, sticky with right shadow separator -->
            <th
              class="pb-3 pr-4 font-medium sticky z-10 bg-surface-default cursor-pointer select-none whitespace-nowrap"
              style="left: 1.5rem; box-shadow: 2px 0 4px -1px rgba(0,0,0,0.15)"
              title="Sort by epic name"
              @click="emit('sort', 'epicName')"
            >
              <span class="flex items-center gap-1">
                Epic
                <svg v-if="sortIcon('epicName') === 'asc'" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3 h-3 text-accent-default"><path fill-rule="evenodd" d="M10 17a.75.75 0 01-.75-.75V5.612L5.29 9.77a.75.75 0 01-1.08-1.04l5.25-5.5a.75.75 0 011.08 0l5.25 5.5a.75.75 0 11-1.08 1.04l-3.96-4.158V16.25A.75.75 0 0110 17z" clip-rule="evenodd" /></svg>
                <svg v-else-if="sortIcon('epicName') === 'desc'" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3 h-3 text-accent-default"><path fill-rule="evenodd" d="M10 3a.75.75 0 01.75.75v10.638l3.96-4.158a.75.75 0 111.08 1.04l-5.25 5.5a.75.75 0 01-1.08 0l-5.25-5.5a.75.75 0 111.08-1.04l3.96 4.158V3.75A.75.75 0 0110 3z" clip-rule="evenodd" /></svg>
              </span>
            </th>

            <!-- Progress -->
            <th
              v-if="isColumnVisible('progress')"
              class="pb-3 pr-4 font-medium min-w-40 cursor-pointer select-none whitespace-nowrap"
              title="SP completion with estimated portion shown separately."
              @click="emit('sort', 'progress')"
            >
              <span class="flex items-center gap-1">
                Progress
                <svg v-if="sortIcon('progress') === 'asc'" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3 h-3 text-accent-default"><path fill-rule="evenodd" d="M10 17a.75.75 0 01-.75-.75V5.612L5.29 9.77a.75.75 0 01-1.08-1.04l5.25-5.5a.75.75 0 011.08 0l5.25 5.5a.75.75 0 11-1.08 1.04l-3.96-4.158V16.25A.75.75 0 0110 17z" clip-rule="evenodd" /></svg>
                <svg v-else-if="sortIcon('progress') === 'desc'" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3 h-3 text-accent-default"><path fill-rule="evenodd" d="M10 3a.75.75 0 01.75.75v10.638l3.96-4.158a.75.75 0 111.08 1.04l-5.25 5.5a.75.75 0 01-1.08 0l-5.25-5.5a.75.75 0 111.08-1.04l3.96 4.158V3.75A.75.75 0 0110 3z" clip-rule="evenodd" /></svg>
              </span>
            </th>

            <!-- SP Done / Total — not sortable -->
            <th
              v-if="isColumnVisible('spDoneTotal')"
              class="pb-3 pr-4 font-medium text-right whitespace-nowrap"
              title="Completed story points versus total scope including estimates."
            >
              SP Done / Total
            </th>

            <!-- Tickets -->
            <th
              v-if="isColumnVisible('tickets')"
              class="pb-3 pr-4 font-medium text-right whitespace-nowrap cursor-pointer select-none"
              title="Completed tickets versus total ticket count in the epic."
              @click="emit('sort', 'tickets')"
            >
              <span class="flex items-center justify-end gap-1">
                <svg v-if="sortIcon('tickets') === 'asc'" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3 h-3 text-accent-default"><path fill-rule="evenodd" d="M10 17a.75.75 0 01-.75-.75V5.612L5.29 9.77a.75.75 0 01-1.08-1.04l5.25-5.5a.75.75 0 011.08 0l5.25 5.5a.75.75 0 11-1.08 1.04l-3.96-4.158V16.25A.75.75 0 0110 17z" clip-rule="evenodd" /></svg>
                <svg v-else-if="sortIcon('tickets') === 'desc'" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3 h-3 text-accent-default"><path fill-rule="evenodd" d="M10 3a.75.75 0 01.75.75v10.638l3.96-4.158a.75.75 0 111.08 1.04l-5.25 5.5a.75.75 0 01-1.08 0l-5.25-5.5a.75.75 0 111.08-1.04l3.96 4.158V3.75A.75.75 0 0110 3z" clip-rule="evenodd" /></svg>
                Tickets
              </span>
            </th>

            <!-- Velocity -->
            <th
              v-if="isColumnVisible('velocity')"
              class="pb-3 pr-4 font-medium text-right whitespace-nowrap cursor-pointer select-none"
              title="Average SP completed per sprint (last 3 sprints with progress)."
              @click="emit('sort', 'velocity')"
            >
              <span class="flex items-center justify-end gap-1">
                <svg v-if="sortIcon('velocity') === 'asc'" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3 h-3 text-accent-default"><path fill-rule="evenodd" d="M10 17a.75.75 0 01-.75-.75V5.612L5.29 9.77a.75.75 0 01-1.08-1.04l5.25-5.5a.75.75 0 011.08 0l5.25 5.5a.75.75 0 11-1.08 1.04l-3.96-4.158V16.25A.75.75 0 0110 17z" clip-rule="evenodd" /></svg>
                <svg v-else-if="sortIcon('velocity') === 'desc'" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3 h-3 text-accent-default"><path fill-rule="evenodd" d="M10 3a.75.75 0 01.75.75v10.638l3.96-4.158a.75.75 0 111.08 1.04l-5.25 5.5a.75.75 0 01-1.08 0l-5.25-5.5a.75.75 0 111.08-1.04l3.96 4.158V3.75A.75.75 0 0110 3z" clip-rule="evenodd" /></svg>
                Velocity
              </span>
            </th>

            <!-- Projected -->
            <th
              v-if="isColumnVisible('projected')"
              class="pb-3 pr-4 font-medium text-right whitespace-nowrap cursor-pointer select-none"
              title="Estimated sprints to completion based on current velocity."
              @click="emit('sort', 'projected')"
            >
              <span class="flex items-center justify-end gap-1">
                <svg v-if="sortIcon('projected') === 'asc'" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3 h-3 text-accent-default"><path fill-rule="evenodd" d="M10 17a.75.75 0 01-.75-.75V5.612L5.29 9.77a.75.75 0 01-1.08-1.04l5.25-5.5a.75.75 0 011.08 0l5.25 5.5a.75.75 0 11-1.08 1.04l-3.96-4.158V16.25A.75.75 0 0110 17z" clip-rule="evenodd" /></svg>
                <svg v-else-if="sortIcon('projected') === 'desc'" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3 h-3 text-accent-default"><path fill-rule="evenodd" d="M10 3a.75.75 0 01.75.75v10.638l3.96-4.158a.75.75 0 111.08 1.04l-5.25 5.5a.75.75 0 01-1.08 0l-5.25-5.5a.75.75 0 111.08-1.04l3.96 4.158V3.75A.75.75 0 0110 3z" clip-rule="evenodd" /></svg>
                Projected
              </span>
            </th>

            <!-- Started (replaces Sprints) -->
            <th
              v-if="isColumnVisible('startedDate')"
              class="pb-3 pr-4 font-medium text-right whitespace-nowrap cursor-pointer select-none"
              title="Date when the first ticket in this epic began active work (passed the cycle time start boundary)."
              @click="emit('sort', 'startedDate')"
            >
              <span class="flex items-center justify-end gap-1">
                <svg v-if="sortIcon('startedDate') === 'asc'" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3 h-3 text-accent-default"><path fill-rule="evenodd" d="M10 17a.75.75 0 01-.75-.75V5.612L5.29 9.77a.75.75 0 01-1.08-1.04l5.25-5.5a.75.75 0 011.08 0l5.25 5.5a.75.75 0 11-1.08 1.04l-3.96-4.158V16.25A.75.75 0 0110 17z" clip-rule="evenodd" /></svg>
                <svg v-else-if="sortIcon('startedDate') === 'desc'" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3 h-3 text-accent-default"><path fill-rule="evenodd" d="M10 3a.75.75 0 01.75.75v10.638l3.96-4.158a.75.75 0 111.08 1.04l-5.25 5.5a.75.75 0 01-1.08 0l-5.25-5.5a.75.75 0 111.08-1.04l3.96 4.158V3.75A.75.75 0 0110 3z" clip-rule="evenodd" /></svg>
                Started
              </span>
            </th>

            <!-- Last Work (replaces Sprints) -->
            <th
              v-if="isColumnVisible('lastWorkDate')"
              class="pb-3 pr-4 font-medium text-right whitespace-nowrap cursor-pointer select-none"
              title="Date of the most recent active work on any ticket in this epic."
              @click="emit('sort', 'lastWorkDate')"
            >
              <span class="flex items-center justify-end gap-1">
                <svg v-if="sortIcon('lastWorkDate') === 'asc'" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3 h-3 text-accent-default"><path fill-rule="evenodd" d="M10 17a.75.75 0 01-.75-.75V5.612L5.29 9.77a.75.75 0 01-1.08-1.04l5.25-5.5a.75.75 0 011.08 0l5.25 5.5a.75.75 0 11-1.08 1.04l-3.96-4.158V16.25A.75.75 0 0110 17z" clip-rule="evenodd" /></svg>
                <svg v-else-if="sortIcon('lastWorkDate') === 'desc'" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3 h-3 text-accent-default"><path fill-rule="evenodd" d="M10 3a.75.75 0 01.75.75v10.638l3.96-4.158a.75.75 0 111.08 1.04l-5.25 5.5a.75.75 0 01-1.08 0l-5.25-5.5a.75.75 0 111.08-1.04l3.96 4.158V3.75A.75.75 0 0110 3z" clip-rule="evenodd" /></svg>
                Last Work
              </span>
            </th>

            <!-- Coverage % (QA) -->
            <th
              v-if="hasQaData && isColumnVisible('coverageRate')"
              class="pb-3 pr-4 font-medium text-right whitespace-nowrap cursor-pointer select-none"
              title="Percentage of feature tickets with at least one linked test execution."
              @click="emit('sort', 'coverageRate')"
            >
              <span class="flex items-center justify-end gap-1">
                <svg v-if="sortIcon('coverageRate') === 'asc'" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3 h-3 text-accent-default"><path fill-rule="evenodd" d="M10 17a.75.75 0 01-.75-.75V5.612L5.29 9.77a.75.75 0 01-1.08-1.04l5.25-5.5a.75.75 0 011.08 0l5.25 5.5a.75.75 0 11-1.08 1.04l-3.96-4.158V16.25A.75.75 0 0110 17z" clip-rule="evenodd" /></svg>
                <svg v-else-if="sortIcon('coverageRate') === 'desc'" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3 h-3 text-accent-default"><path fill-rule="evenodd" d="M10 3a.75.75 0 01.75.75v10.638l3.96-4.158a.75.75 0 111.08 1.04l-5.25 5.5a.75.75 0 01-1.08 0l-5.25-5.5a.75.75 0 111.08-1.04l3.96 4.158V3.75A.75.75 0 0110 3z" clip-rule="evenodd" /></svg>
                Coverage %
              </span>
            </th>

            <!-- Pass Rate % (QA) -->
            <th
              v-if="hasQaData && isColumnVisible('passRate')"
              class="pb-3 pr-4 font-medium text-right whitespace-nowrap cursor-pointer select-none"
              title="Percentage of PASS test runs across all non-cancelled test executions linked to feature tickets."
              @click="emit('sort', 'passRate')"
            >
              <span class="flex items-center justify-end gap-1">
                <svg v-if="sortIcon('passRate') === 'asc'" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3 h-3 text-accent-default"><path fill-rule="evenodd" d="M10 17a.75.75 0 01-.75-.75V5.612L5.29 9.77a.75.75 0 01-1.08-1.04l5.25-5.5a.75.75 0 011.08 0l5.25 5.5a.75.75 0 11-1.08 1.04l-3.96-4.158V16.25A.75.75 0 0110 17z" clip-rule="evenodd" /></svg>
                <svg v-else-if="sortIcon('passRate') === 'desc'" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3 h-3 text-accent-default"><path fill-rule="evenodd" d="M10 3a.75.75 0 01.75.75v10.638l3.96-4.158a.75.75 0 111.08 1.04l-5.25 5.5a.75.75 0 01-1.08 0l-5.25-5.5a.75.75 0 111.08-1.04l3.96 4.158V3.75A.75.75 0 0110 3z" clip-rule="evenodd" /></svg>
                Pass Rate %
              </span>
            </th>

            <!-- Bugs Found (QA) -->
            <th
              v-if="hasQaData && isColumnVisible('bugsFound')"
              class="pb-3 font-medium text-right whitespace-nowrap cursor-pointer select-none"
              title="Unique bug tickets linked via Blocks from test executions on this epic's feature tickets."
              @click="emit('sort', 'bugsFound')"
            >
              <span class="flex items-center justify-end gap-1">
                <svg v-if="sortIcon('bugsFound') === 'asc'" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3 h-3 text-accent-default"><path fill-rule="evenodd" d="M10 17a.75.75 0 01-.75-.75V5.612L5.29 9.77a.75.75 0 01-1.08-1.04l5.25-5.5a.75.75 0 011.08 0l5.25 5.5a.75.75 0 11-1.08 1.04l-3.96-4.158V16.25A.75.75 0 0110 17z" clip-rule="evenodd" /></svg>
                <svg v-else-if="sortIcon('bugsFound') === 'desc'" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3 h-3 text-accent-default"><path fill-rule="evenodd" d="M10 3a.75.75 0 01.75.75v10.638l3.96-4.158a.75.75 0 111.08 1.04l-5.25 5.5a.75.75 0 01-1.08 0l-5.25-5.5a.75.75 0 111.08-1.04l3.96 4.158V3.75A.75.75 0 0110 3z" clip-rule="evenodd" /></svg>
                Bugs Found
              </span>
            </th>
          </tr>
        </thead>
        <tbody>
          <template v-for="epic in epics" :key="epic.epicKey">
            <!-- Epic row -->
            <tr
              class="border-b border-border-default hover:bg-surface-elevated/40 cursor-pointer transition-colors"
              :class="{ 'last:border-0': !expandedEpicKeys.has(epic.epicKey) }"
              @click="emit('toggle-expand', epic.epicKey)"
            >
              <!-- Expand chevron — sticky -->
              <td class="py-3 pr-2 sticky left-0 z-10 bg-surface-default">
                <svg
                  xmlns="http://www.w3.org/2000/svg"
                  viewBox="0 0 20 20"
                  fill="currentColor"
                  class="w-4 h-4 text-text-muted transition-transform"
                  :class="{ 'rotate-90': expandedEpicKeys.has(epic.epicKey) }"
                >
                  <path fill-rule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clip-rule="evenodd" />
                </svg>
              </td>

              <!-- Epic name — sticky with right shadow separator -->
              <td
                class="py-3 pr-4 text-text-primary font-medium sticky z-10 bg-surface-default"
                style="left: 1.5rem; box-shadow: 2px 0 4px -1px rgba(0,0,0,0.15)"
              >
                <div class="flex flex-col gap-0.5">
                  <span>{{ epic.epicName }}</span>
                  <span class="text-xs text-text-muted font-normal font-mono">{{ epic.epicKey }}</span>
                </div>
              </td>

              <!-- Progress bar -->
              <td v-if="isColumnVisible('progress')" class="py-3 pr-4">
                <div class="flex items-center gap-2">
                  <div class="flex-1 flex flex-col gap-1">
                    <div class="h-2 rounded-full bg-surface-elevated overflow-hidden flex">
                      <div
                        class="h-full bg-accent-default rounded-l-full transition-all"
                        :style="{ width: completionBarWidth(epic) }"
                      />
                      <div
                        v-if="epic.imputedSp !== null && epic.imputedSp > 0"
                        class="h-full bg-accent-default/30 transition-all"
                        :style="{ width: imputedBarWidth(epic) }"
                      />
                    </div>
                    <div
                      v-if="hasQaData && epic.coverageRate !== null"
                      class="h-1 rounded-full bg-surface-elevated overflow-hidden"
                    >
                      <div
                        class="h-full bg-emerald-500 rounded-full transition-all"
                        :style="{ width: `${Math.min(epic.coverageRate, 100)}%` }"
                      />
                    </div>
                  </div>
                  <span class="text-xs text-text-secondary tabular-nums whitespace-nowrap w-12 text-right">
                    {{ completionLabel(epic) }}
                  </span>
                </div>
              </td>

              <!-- SP done / total -->
              <td v-if="isColumnVisible('spDoneTotal')" class="py-3 pr-4 text-right tabular-nums text-text-primary whitespace-nowrap">
                {{ spDisplay(epic) }}
              </td>

              <!-- Tickets -->
              <td v-if="isColumnVisible('tickets')" class="py-3 pr-4 text-right tabular-nums text-text-primary whitespace-nowrap">
                {{ epic.doneTickets }} / {{ epic.totalTickets }}
              </td>

              <!-- Velocity -->
              <td v-if="isColumnVisible('velocity')" class="py-3 pr-4 text-right tabular-nums text-text-secondary whitespace-nowrap">
                {{ velocityDisplay(epic) }}
              </td>

              <!-- Projected remaining -->
              <td v-if="isColumnVisible('projected')" class="py-3 pr-4 text-right tabular-nums whitespace-nowrap">
                <span :class="epic.projectedSprintsRemaining === null ? 'text-text-muted' : 'text-text-secondary'">
                  {{ projectionDisplay(epic) }}
                </span>
                <span
                  v-if="epic.projectionConfidence === 'low'"
                  class="ml-1 text-xs text-amber-400 bg-amber-400/10 px-1.5 py-0.5 rounded"
                >
                  Low confidence
                </span>
              </td>

              <!-- Started date -->
              <td v-if="isColumnVisible('startedDate')" class="py-3 pr-4 text-right tabular-nums text-text-secondary whitespace-nowrap">
                {{ formatActivityDate(epic.startedDate) }}
              </td>

              <!-- Last Work date -->
              <td v-if="isColumnVisible('lastWorkDate')" class="py-3 pr-4 text-right tabular-nums text-text-secondary whitespace-nowrap">
                {{ formatActivityDate(epic.lastWorkDate) }}
              </td>

              <!-- Coverage % (QA) -->
              <td v-if="hasQaData && isColumnVisible('coverageRate')" class="py-3 pr-4 text-right tabular-nums whitespace-nowrap">
                <span :class="epic.coverageRate !== null ? ragClass(epic.coverageRag) : 'text-text-muted'">
                  {{ epic.coverageRate !== null ? `${epic.coverageRate.toFixed(1)}%` : '—' }}
                </span>
              </td>

              <!-- Pass Rate % (QA) -->
              <td v-if="hasQaData && isColumnVisible('passRate')" class="py-3 pr-4 text-right tabular-nums whitespace-nowrap">
                <span :class="epic.passRate !== null ? ragClass(epic.passRateRag) : 'text-text-muted'">
                  {{ epic.passRate !== null ? `${epic.passRate.toFixed(1)}%` : '—' }}
                </span>
              </td>

              <!-- Bugs Found (QA) -->
              <td v-if="hasQaData && isColumnVisible('bugsFound')" class="py-3 text-right tabular-nums text-text-primary">
                {{ epic.featureTicketCount > 0 ? epic.bugsFound : '—' }}
              </td>
            </tr>

            <!-- Expanded ticket table -->
            <tr v-if="expandedEpicKeys.has(epic.epicKey)" :key="`${epic.epicKey}-tickets`">
              <td :colspan="visibleColspan" class="px-4 pb-4 bg-surface-elevated/20 border-b border-border-default">
                <div class="pt-3">
                  <EpicTicketTable :tickets="epic.tickets" :has-qa-data="hasQaData" />
                </div>
              </td>
            </tr>
          </template>
        </tbody>
      </table>
    </div>
  </BaseCard>
</template>
