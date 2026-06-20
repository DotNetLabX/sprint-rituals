<template>
  <div class="w-full max-w-lg p-8 bg-surface border border-border rounded shadow-card flex flex-col gap-4">
    <router-link :to="{ name: 'welcome' }" class="text-sm text-text-muted hover:text-text-secondary self-start">
      ← Back to sprints
    </router-link>

    <div class="flex items-center justify-between">
      <h1 class="text-2xl font-bold text-text-primary">{{ sprint.name }}</h1>
      <span :class="stateBadgeClass">{{ sprint.state }}</span>
    </div>

    <div class="grid grid-cols-2 gap-2 text-sm text-text-secondary">
      <span class="font-medium">Phase</span><span>{{ sprint.currentPhase }}</span>
      <span class="font-medium">Votes per participant</span><span>{{ sprint.votesPerParticipant }}</span>
      <span class="font-medium">Created</span><span>{{ new Date(sprint.createdAt).toLocaleString() }}</span>
      <template v-if="sprint.closedAt">
        <span class="font-medium">Closed</span><span>{{ new Date(sprint.closedAt).toLocaleString() }}</span>
      </template>
    </div>

    <div class="flex flex-col gap-2 pt-2">
      <button v-if="sprint.state === 'Draft'" class="px-4 py-2 bg-primary-600 text-white rounded hover:bg-primary-700 text-sm disabled:opacity-50" :disabled="busy" @click="$emit('start')">
        Start Sprint
      </button>
      <router-link v-if="sprint.state === 'Active'" :to="{ name: 'retro-room', params: { sprintId: sprint.id } }" class="px-4 py-2 bg-primary-600 text-white rounded hover:bg-primary-700 text-sm text-center">
        Open Retro Room
      </router-link>
      <button v-if="sprint.state === 'Active'" class="px-4 py-2 bg-surface-raised text-text-primary rounded hover:bg-border text-sm" @click="$emit('copy-invite')">
        {{ copied ? 'Invite link copied!' : 'Copy invite link' }}
      </button>
      <router-link v-if="sprint.state === 'Closed'" :to="{ name: 'report', params: { teamId, sprintId: sprint.id } }" class="px-4 py-2 bg-primary-600 text-white rounded hover:bg-primary-700 text-sm text-center">
        View Report
      </router-link>
    </div>

    <!-- Participants -->
    <div v-if="sprint.state === 'Active' && participants.length > 0" class="flex flex-col gap-2 pt-2 border-t border-border">
      <h2 class="text-sm font-semibold text-text-secondary uppercase tracking-wide">Participants</h2>
      <div class="flex flex-col gap-1">
        <div v-for="p in participants" :key="p.id" class="flex items-center justify-between px-3 py-2 bg-surface-raised rounded text-sm">
          <span class="text-text-primary">{{ p.displayName }}</span>
          <button class="text-red-600 hover:text-red-800 text-xs font-medium disabled:opacity-50" :disabled="busy" @click="$emit('kick', p)">Kick</button>
        </div>
      </div>
    </div>

    <p v-if="errorMessage" class="text-red-600 text-sm">{{ errorMessage }}</p>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  sprint: {
    id: number
    name: string
    state: string
    currentPhase: string
    votesPerParticipant: number
    createdAt: string
    closedAt: string | null
  }
  teamId: number
  participants: { id: number; displayName: string }[]
  busy: boolean
  copied: boolean
  errorMessage: string
}>()

defineEmits<{
  start: []
  'copy-invite': []
  kick: [p: { id: number; displayName: string }]
}>()

const stateBadgeClass = computed(() => {
  const base = 'px-2 py-1 rounded text-xs font-semibold'
  switch (props.sprint.state) {
    case 'Draft': return `${base} bg-badge-default-bg text-badge-default-text`
    case 'Active': return `${base} bg-badge-success-bg text-badge-success-text`
    case 'Closed': return `${base} bg-badge-danger-bg text-badge-danger-text`
    default: return base
  }
})
</script>
