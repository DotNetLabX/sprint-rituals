<template>
  <div class="flex flex-col gap-4 p-8 bg-surface border border-border rounded shadow-card w-full max-w-md">
    <h2 class="text-xl font-bold text-text-primary">{{ teamName }}</h2>

    <div v-if="sprints.length === 0" class="text-text-muted text-sm">
      No sprints yet. Create your first sprint.
    </div>
    <ul v-else class="flex flex-col gap-2">
      <li
        v-for="sprint in sprints"
        :key="sprint.id"
        class="flex items-center justify-between px-3 py-2 border border-border rounded hover:bg-surface-raised cursor-pointer"
        @click="$emit('select', sprint.id)"
      >
        <span class="text-sm font-medium text-text-primary">{{ sprint.name }}</span>
        <div class="flex items-center gap-2 text-xs">
          <span class="text-text-muted">{{ sprint.currentPhase }}</span>
          <span :class="stateBadge(sprint.state)">{{ sprint.state }}</span>
        </div>
      </li>
    </ul>

    <CreateSprintForm @create="$emit('create-sprint', $event)" />
  </div>
</template>

<script setup lang="ts">
import CreateSprintForm from './CreateSprintForm.vue'

defineProps<{
  teamName: string
  sprints: { id: number; name: string; currentPhase: string; state: string }[]
}>()

defineEmits<{
  select: [sprintId: number]
  'create-sprint': [payload: {
    name: string
    votesPerParticipant: number | undefined
    column1Name: string
    column1Color: string
    column2Name: string
    column2Color: string
    column3Name: string
    column3Color: string
  }]
}>()

function stateBadge(state: string) {
  const base = 'px-2 py-0.5 rounded font-semibold'
  if (state === 'Draft') return `${base} bg-badge-default-bg text-badge-default-text`
  if (state === 'Active') return `${base} bg-badge-success-bg text-badge-success-text`
  return `${base} bg-badge-danger-bg text-badge-danger-text`
}
</script>
