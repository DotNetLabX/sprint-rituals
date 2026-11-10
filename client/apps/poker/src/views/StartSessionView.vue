<template>
  <div class="flex items-center justify-center min-h-[calc(100vh-4rem)] p-4">
    <LoadingSpinner v-if="!authStore.loaded" />

    <!-- Signed out: only an authenticated user can create a session -->
    <div
      v-else-if="!authStore.isSignedIn"
      class="w-full max-w-sm p-8 bg-surface rounded-[--radius-lg] border border-border shadow-card flex flex-col gap-6 text-center"
    >
      <div>
        <h1 class="text-xl font-bold text-text-primary">Planning Poker</h1>
        <p class="text-sm text-text-secondary mt-1">Sign in to start a planning session.</p>
      </div>
      <BaseButton class="w-full" @click="authStore.signIn()">Sign in with Google</BaseButton>
    </div>

    <form
      v-else
      class="w-full max-w-sm p-8 bg-surface rounded-[--radius-lg] border border-border shadow-card flex flex-col gap-5"
      @submit.prevent="handleCreate"
    >
      <h1 class="text-xl font-bold text-text-primary text-center">New Planning Session</h1>

      <div class="grid grid-cols-2 gap-1 p-1 bg-surface-raised rounded-[--radius-md]">
        <button
          v-for="option in modeOptions"
          :key="option.value"
          type="button"
          class="py-1.5 text-sm rounded-[--radius-sm] transition-colors cursor-pointer"
          :class="mode === option.value ? 'bg-primary-600 text-white' : 'text-text-secondary hover:text-text-primary'"
          @click="mode = option.value"
        >
          {{ option.label }}
        </button>
      </div>

      <div v-if="mode === 'SprintBound'" class="flex flex-col gap-1">
        <label class="text-xs font-medium text-text-secondary" for="sprint">Sprint</label>
        <p v-if="!sessionStore.sprints.length" class="text-sm text-text-muted">
          {{ sessionStore.error ?? 'No sprints available. Run a sprint sync in Fokus first.' }}
        </p>
        <select
          v-else
          id="sprint"
          v-model="sprintId"
          class="px-3 py-2 text-sm bg-surface-raised border border-border rounded-[--radius-md] text-text-primary focus:outline-none focus:ring-2 focus:ring-primary-500"
        >
          <option v-for="sprint in sessionStore.sprints" :key="sprint.sprintId" :value="sprint.sprintId">
            {{ sprint.name }} ({{ sprint.state }})
          </option>
        </select>
      </div>

      <div v-else class="flex flex-col gap-1">
        <label class="text-xs font-medium text-text-secondary" for="title">Title</label>
        <input
          id="title"
          v-model="title"
          type="text"
          maxlength="200"
          placeholder="e.g. Backlog refinement"
          class="px-3 py-2 text-sm bg-surface-raised border border-border rounded-[--radius-md] text-text-primary placeholder:text-text-muted focus:outline-none focus:ring-2 focus:ring-primary-500"
        />
      </div>

      <p v-if="errorMessage" class="text-sm text-danger">{{ errorMessage }}</p>

      <BaseButton type="submit" :loading="busy" :disabled="!canCreate" class="w-full">Create Session</BaseButton>
    </form>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import BaseButton from '../components/ui/BaseButton.vue'
import LoadingSpinner from '../components/ui/LoadingSpinner.vue'
import { useAuthStore } from '../stores/auth'
import { usePlanningSessionStore } from '../stores/planningSession'
import type { SessionMode } from '../types'

const router = useRouter()
const authStore = useAuthStore()
const sessionStore = usePlanningSessionStore()

const modeOptions: { value: SessionMode; label: string }[] = [
  { value: 'SprintBound', label: 'Sprint' },
  { value: 'Free', label: 'Free' },
]

const mode = ref<SessionMode>('SprintBound')
const sprintId = ref<number | null>(null)
const title = ref('')
const busy = ref(false)
const errorMessage = ref('')

const selectedSprint = computed(() => sessionStore.sprints.find((s) => s.sprintId === sprintId.value) ?? null)
const canCreate = computed(() => (mode.value === 'SprintBound' ? selectedSprint.value !== null : title.value.trim() !== ''))

onMounted(async () => {
  if (!authStore.loaded) await authStore.fetchMe()
  if (authStore.isSignedIn && !sessionStore.sprints.length) {
    await sessionStore.fetchSprints()
    sprintId.value = sessionStore.defaultSprintId ?? sessionStore.sprints[0]?.sprintId ?? null
  }
})

async function handleCreate() {
  if (!canCreate.value) return
  busy.value = true
  errorMessage.value = ''
  try {
    const id = await sessionStore.createSession(
      mode.value === 'SprintBound'
        ? { mode: 'SprintBound', sprintId: selectedSprint.value!.sprintId, sprintName: selectedSprint.value!.name }
        : { mode: 'Free', title: title.value.trim() },
    )
    await router.push({ name: 'planning-room', params: { id } })
  } catch {
    errorMessage.value = 'Failed to create the session. Please try again.'
  } finally {
    busy.value = false
  }
}
</script>
