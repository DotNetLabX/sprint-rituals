<template>
  <div class="flex flex-col gap-3 p-8 bg-surface border border-border rounded shadow-card w-72">
    <h2 class="text-lg font-semibold text-text-primary">Create your first team</h2>
    <div class="relative group">
      <input
        v-model="teamName"
        type="text"
        placeholder="Team name"
        maxlength="100"
        class="w-full border border-border rounded px-3 py-2 text-sm text-text-primary bg-surface focus:outline-none focus:ring-2 focus:ring-primary-500"
      />
      <div class="absolute left-0 bottom-10 w-64 bg-surface-raised text-text-primary border border-border text-xs rounded px-2 py-1 opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none z-10 whitespace-normal">
        Choose a name for your team. Must be unique among your teams. Max 100 characters.
      </div>
    </div>
    <p v-if="errorMessage" class="text-red-600 text-sm">{{ errorMessage }}</p>
    <div class="flex gap-2">
      <button
        class="flex-1 px-4 py-2 bg-primary-600 text-white rounded hover:bg-primary-700 text-sm disabled:opacity-50"
        :disabled="busy"
        @click="handleCreate"
      >
        {{ busy ? 'Creating…' : 'Create Team' }}
      </button>
      <button
        v-if="showCancel"
        class="px-4 py-2 text-sm text-text-secondary border border-border rounded hover:bg-surface-raised"
        @click="$emit('cancel')"
      >
        Cancel
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useTeamsStore, type TeamSummary } from '../../stores/teams'

defineProps<{ showCancel?: boolean }>()

const emit = defineEmits<{
  created: [team: TeamSummary]
  cancel: []
}>()

const teamsStore = useTeamsStore()
const teamName = ref('')
const errorMessage = ref('')
const busy = ref(false)

async function handleCreate() {
  errorMessage.value = ''
  const name = teamName.value.trim()
  if (!name) { errorMessage.value = 'Team name is required.'; return }
  busy.value = true
  try {
    const team = await teamsStore.createTeam(name)
    teamName.value = ''
    emit('created', team)
  } catch (err: unknown) {
    const errors = (err as Record<string, unknown>)?.errors as Record<string, string[]> | undefined
    const general = errors?.generalErrors?.[0] ?? ''
    if (general.startsWith('duplicate_name:')) {
      errorMessage.value = general.replace('duplicate_name: ', '')
    } else if (general.startsWith('limit_reached:')) {
      errorMessage.value = general.replace('limit_reached: ', '')
    } else {
      errorMessage.value = general || 'Failed to create team.'
    }
  } finally {
    busy.value = false
  }
}
</script>
