<script setup lang="ts">
import { computed, ref } from 'vue'
import type { SprintItem } from '../types'
import BaseSelect from './BaseSelect.vue'
import InfoTooltip from './InfoTooltip.vue'
import type { SelectOption } from './BaseSelect.vue'
import SprintEditDialog from './sprints/SprintEditDialog.vue'
import type { UpdateSprintResponse } from '../api/analytics'

const props = withDefaults(defineProps<{
  sprints?: SprintItem[]
  selectedSprintId?: number | null
  subTeams?: string[]
  selectedSubTeam?: string | null
  showSubTeamFilter?: boolean
  showAggregateOptions?: boolean
  showSprintSelector?: boolean
  showSprintInfo?: boolean
  sprintMode?: 'single' | 'multi'
  selectedLast?: number | null
  showEditButton?: boolean
  disabled?: boolean
}>(), {
  sprints: () => [],
  selectedSprintId: null,
  subTeams: () => [],
  selectedSubTeam: null,
  showSubTeamFilter: true,
  showAggregateOptions: false,
  showSprintSelector: true,
  showSprintInfo: true,
  sprintMode: 'single',
  selectedLast: null,
  showEditButton: false,
  disabled: false
})

const emit = defineEmits<{
  'update:selectedSprintId': [id: number]
  'update:selectedSubTeam': [subTeam: string | null]
  'update:sprintMode': [payload: { mode: 'single'; sprintId: number } | { mode: 'multi'; last: number | null }]
  'sprintUpdated': [sprint: UpdateSprintResponse]
}>()

const editDialogOpen = ref(false)

function onSprintSaved(sprint: UpdateSprintResponse) {
  editDialogOpen.value = false
  emit('sprintUpdated', sprint)
}

const sprintOptions = computed<SelectOption[]>(() => {
  const opts: SelectOption[] = props.sprints.map(s => ({
    label: s.state === 'Active' ? `${s.name} (In Progress)` : s.name,
    value: String(s.id)
  }))
  if (props.showAggregateOptions) {
    opts.push(
      { label: '', value: '', separator: true },
      { label: 'Last 3 Sprints', value: 'last-3' },
      { label: 'Last 5 Sprints', value: 'last-5' },
      { label: 'All Sprints', value: 'all' }
    )
  }
  return opts
})

const sprintValue = computed(() => {
  if (props.sprintMode === 'multi') {
    if (props.selectedLast === 3) return 'last-3'
    if (props.selectedLast === 5) return 'last-5'
    return 'all'
  }
  return String(props.selectedSprintId ?? props.sprints?.[0]?.id ?? '')
})

const subTeamOptions = computed<SelectOption[]>(() => [
  { label: 'All', value: '' },
  ...props.subTeams.map(t => ({ label: t, value: t }))
])

function onSprintChange(value: string) {
  if (value === 'last-3') {
    emit('update:sprintMode', { mode: 'multi', last: 3 })
  } else if (value === 'last-5') {
    emit('update:sprintMode', { mode: 'multi', last: 5 })
  } else if (value === 'all') {
    emit('update:sprintMode', { mode: 'multi', last: null })
  } else {
    const id = Number(value)
    emit('update:selectedSprintId', id)
    emit('update:sprintMode', { mode: 'single', sprintId: id })
  }
}

function onSubTeamChange(value: string) {
  emit('update:selectedSubTeam', value === '' ? null : value)
}

const selectedSprint = computed(() => {
  if (props.sprintMode !== 'single' || !props.selectedSprintId) return null
  return props.sprints.find(s => s.id === props.selectedSprintId) ?? null
})

const sprintDurationDays = computed(() => {
  if (!selectedSprint.value) return null
  const start = new Date(selectedSprint.value.startDate)
  const end = new Date(selectedSprint.value.endDate)
  return Math.round((end.getTime() - start.getTime()) / (1000 * 60 * 60 * 24))
})

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' })
}
</script>

<template>
  <div class="flex flex-col gap-2">
  <div class="flex items-center gap-3">
    <!-- Sprint selector -->
    <BaseSelect
      v-if="showSprintSelector && sprints.length > 0"
      :options="sprintOptions"
      :model-value="sprintValue"
      placeholder="No sprints"
      title="Choose a specific sprint to analyze, or select a range (Last 3, Last 5, All) for trend views."
      :class="disabled ? 'opacity-50 pointer-events-none' : ''"
      @update:model-value="onSprintChange"
    >
      <template #icon>
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-4 h-4 shrink-0 text-text-muted">
          <path fill-rule="evenodd" d="M4 2a1 1 0 011 1v2.101a7.002 7.002 0 0111.601 2.566 1 1 0 11-1.885.666A5.002 5.002 0 005.999 7H9a1 1 0 010 2H4a1 1 0 01-1-1V3a1 1 0 011-1zm.008 9.057a1 1 0 011.276.61A5.002 5.002 0 0014.001 13H11a1 1 0 110-2h5a1 1 0 011 1v5a1 1 0 11-2 0v-2.101a7.002 7.002 0 01-11.601-2.566 1 1 0 01.61-1.276z" clip-rule="evenodd" />
        </svg>
      </template>
    </BaseSelect>

    <!-- Empty state -->
    <div
      v-if="showSprintSelector && sprints.length === 0"
      class="flex items-center gap-2 h-9 px-3 rounded-md border border-border-default bg-surface-card text-text-muted text-sm min-w-44"
    >
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-4 h-4 shrink-0">
        <path fill-rule="evenodd" d="M4 2a1 1 0 011 1v2.101a7.002 7.002 0 0111.601 2.566 1 1 0 11-1.885.666A5.002 5.002 0 005.999 7H9a1 1 0 010 2H4a1 1 0 01-1-1V3a1 1 0 011-1zm.008 9.057a1 1 0 011.276.61A5.002 5.002 0 0014.001 13H11a1 1 0 110-2h5a1 1 0 011 1v5a1 1 0 11-2 0v-2.101a7.002 7.002 0 01-11.601-2.566 1 1 0 01.61-1.276z" clip-rule="evenodd" />
      </svg>
      <span class="flex-1 truncate">No sprints</span>
    </div>

    <!-- Sub-team filter -->
    <div v-if="showSubTeamFilter" class="flex items-center gap-1">
      <InfoTooltip text="Scope all metrics to one sub-team's contributions." />
      <BaseSelect
        :options="subTeamOptions"
        :model-value="selectedSubTeam ?? ''"
        @update:model-value="onSubTeamChange"
      >
        <template #icon>
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-4 h-4 shrink-0 text-text-muted">
            <path d="M9 6a3 3 0 11-6 0 3 3 0 016 0zM17 6a3 3 0 11-6 0 3 3 0 016 0zM12.93 17c.046-.327.07-.66.07-1a6.97 6.97 0 00-1.5-4.33A5 5 0 0119 16v1h-6.07zM6 11a5 5 0 015 5v1H1v-1a5 5 0 015-5z" />
          </svg>
        </template>
      </BaseSelect>
    </div>
  </div>

  <!-- Sprint info line -->
  <div v-if="showSprintInfo && showSprintSelector && selectedSprint" class="flex items-center gap-3 flex-wrap">
    <h2 class="text-xl font-semibold text-text-primary">{{ selectedSprint.name }}</h2>
    <span class="text-text-secondary text-sm">
      {{ formatDate(selectedSprint.startDate) }} – {{ formatDate(selectedSprint.endDate) }}
    </span>
    <span class="text-xs bg-surface-elevated text-text-muted rounded px-2 py-0.5">
      {{ sprintDurationDays }} days
    </span>
    <button
      v-if="showEditButton && selectedSprint"
      @click="editDialogOpen = true"
      class="text-text-muted hover:text-text-primary transition-colors"
      title="Edit sprint"
    >
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-4 h-4">
        <path d="M5.433 13.917l1.262-3.155A4 4 0 017.58 9.42l6.92-6.918a2.121 2.121 0 013 3l-6.92 6.918c-.383.383-.84.685-1.343.886l-3.154 1.262a.5.5 0 01-.65-.65z" />
        <path d="M3.5 5.75c0-.69.56-1.25 1.25-1.25H10A.75.75 0 0010 3H4.75A2.75 2.75 0 002 5.75v9.5A2.75 2.75 0 004.75 18h9.5A2.75 2.75 0 0017 15.25V10a.75.75 0 00-1.5 0v5.25c0 .69-.56 1.25-1.25 1.25h-9.5c-.69 0-1.25-.56-1.25-1.25v-9.5z" />
      </svg>
    </button>
  </div>

  <SprintEditDialog
    v-if="showEditButton && selectedSprint"
    :sprint="selectedSprint"
    :open="editDialogOpen"
    @close="editDialogOpen = false"
    @saved="onSprintSaved"
  />
  </div>
</template>
