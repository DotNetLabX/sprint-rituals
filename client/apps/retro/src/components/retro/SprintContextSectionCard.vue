<template>
  <div class="bg-surface-raised rounded-[--radius-md] border border-border p-4 flex flex-col gap-3">
    <!-- Sprint name + dates -->
    <div class="flex flex-col gap-0.5">
      <p class="font-semibold text-text-primary text-sm">
        {{ section.sprintName ?? `Sprint #${section.sprintId}` }}
      </p>
      <p v-if="section.startDate || section.endDate" class="text-xs text-text-secondary">
        {{ formatDate(section.startDate) }} – {{ formatDate(section.endDate) }}
      </p>
    </div>

    <!-- Cached badge -->
    <div
      v-if="section.freshness === 'cached'"
      class="flex items-center gap-1.5"
      :title="cachedTooltip"
    >
      <BaseBadge variant="warning">Last-known data</BaseBadge>
    </div>

    <!-- Unavailable state -->
    <div v-if="section.freshness === 'unavailable'" class="text-sm text-text-secondary italic">
      {{ unavailableText }}
    </div>

    <!-- Metrics (live or cached) -->
    <template v-if="section.freshness !== 'unavailable'">
      <div class="grid grid-cols-2 gap-x-4 gap-y-2 text-sm">
        <!-- Completion rate -->
        <div class="flex flex-col gap-0.5">
          <span class="text-xs text-text-secondary" :title="completionTooltip">Completion</span>
          <div class="flex items-center gap-1">
            <span v-if="section.completionRate != null" class="text-text-primary font-medium">
              {{ formatRate(section.completionRate) }}
            </span>
            <span v-else class="text-text-muted">—</span>
            <RagBadge v-if="section.completionRag" :rag="section.completionRag" />
          </div>
        </div>

        <!-- Disruption rate -->
        <div class="flex flex-col gap-0.5">
          <span class="text-xs text-text-secondary" :title="disruptionTooltip">Disruption</span>
          <div class="flex items-center gap-1">
            <span v-if="section.disruptionRate != null" class="text-text-primary font-medium">
              {{ formatRate(section.disruptionRate) }}
            </span>
            <span v-else class="text-text-muted">—</span>
            <RagBadge v-if="section.disruptionRag" :rag="section.disruptionRag" />
          </div>
        </div>

        <!-- Carry-over rate -->
        <div class="flex flex-col gap-0.5">
          <span class="text-xs text-text-secondary" :title="carryOverTooltip">Carry-over</span>
          <div class="flex items-center gap-1">
            <span v-if="section.carryOverRate != null" class="text-text-primary font-medium">
              {{ formatRate(section.carryOverRate) }}
            </span>
            <span v-else class="text-text-muted">—</span>
            <RagBadge v-if="section.carryOverRag" :rag="section.carryOverRag" />
          </div>
        </div>

        <!-- Health score -->
        <div class="flex flex-col gap-0.5">
          <span class="text-xs text-text-secondary" :title="healthTooltip">Health</span>
          <div class="flex items-center gap-1">
            <span v-if="section.healthScore != null" class="text-text-primary font-medium">
              {{ section.healthScore }}
            </span>
            <span v-else class="text-text-muted">—</span>
            <RagBadge v-if="section.healthRag" :rag="section.healthRag" />
          </div>
        </div>
      </div>

      <!-- Added mid-sprint block -->
      <div
        v-if="section.addedMidSprint != null"
        class="flex flex-col gap-1.5 pt-1 border-t border-border"
        :title="addedMidSprintTooltip"
      >
        <p class="text-xs font-medium text-text-secondary">Added mid-sprint</p>
        <ul class="flex flex-col gap-1">
          <li
            v-for="item in visibleAdditions"
            :key="item.ticketKey"
            class="text-xs text-text-primary"
          >
            <span class="font-mono text-text-secondary">{{ item.ticketKey }}</span>
            {{ item.summary }}
            <span v-if="item.storyPoints != null" class="text-text-muted">({{ item.storyPoints }} pts)</span>
          </li>
        </ul>
        <p
          v-if="section.addedMidSprintOverflow && section.addedMidSprintOverflow > 0"
          class="text-xs text-text-muted"
          :title="overflowTooltip"
        >
          +{{ section.addedMidSprintOverflow }} more
        </p>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import BaseBadge from '../ui/BaseBadge.vue'
import RagBadge from './RagBadge.vue'
import type { SprintContextSection } from '../../types/sprintContext'

const props = defineProps<{
  section: SprintContextSection
}>()

const cachedTooltip = "Live Fokus data wasn't reachable, so this shows the last synced values for the sprint."
const unavailableText = "This sprint hasn't been synced from Fokus yet, so there are no metrics to show."
const completionTooltip = "Share of planned work the team finished in the sprint, as Fokus measured it."
const disruptionTooltip = "How much work was added or changed mid-sprint. Higher means the plan shifted more after the sprint started."
const carryOverTooltip = "Share of work that wasn't finished and rolled into the next sprint."
const healthTooltip = "Fokus's overall sprint health, combining the rates above. Color shows the band: green, amber, or red."
const addedMidSprintTooltip = "Tickets added after the sprint started — shown when disruption was high. Keys and titles only, never who they were assigned to."
const overflowTooltip = "More tickets were added than shown here. Open Fokus for the full list."

const visibleAdditions = computed(() => {
  if (!props.section.addedMidSprint) return []
  return props.section.addedMidSprint.slice(0, 3)
})

function formatDate(dateStr: string | null | undefined): string {
  if (!dateStr) return '?'
  const d = new Date(dateStr)
  return d.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' })
}

function formatRate(rate: number): string {
  return `${Math.round(rate * 100)}%`
}
</script>
