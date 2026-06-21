<template>
  <!-- Join gate — shown before JoinSession succeeds -->
  <div class="w-full max-w-sm p-8 bg-surface rounded-[--radius-lg] border border-border shadow-card flex flex-col gap-6">
    <div class="text-center">
      <h1 class="text-xl font-bold text-text-primary">Join Planning Session</h1>
      <p class="text-sm text-text-secondary mt-1">{{ sessionTitle }}</p>
    </div>

    <div v-if="participants.length > 0" class="flex flex-wrap gap-2">
      <span
        v-for="p in participants"
        :key="p.id"
        class="px-2 py-0.5 bg-surface-raised text-xs text-text-secondary rounded-[--radius-full]"
      >
        {{ p.displayName }}
      </span>
    </div>

    <form class="flex flex-col gap-4" @submit.prevent="handleSubmit">
      <div v-if="!isSignedIn" class="flex flex-col gap-1">
        <label class="text-xs font-medium text-text-secondary" for="displayName">Your name</label>
        <input
          id="displayName"
          v-model="displayName"
          type="text"
          placeholder="Enter your display name"
          required
          class="px-3 py-2 text-sm bg-surface-raised border border-border rounded-[--radius-md] text-text-primary placeholder:text-text-muted focus:outline-none focus:ring-2 focus:ring-primary-500"
        />
      </div>

      <p v-if="errorMessage" class="text-sm text-danger">{{ errorMessage }}</p>

      <BaseButton type="submit" :loading="busy" class="w-full">
        {{ isSignedIn ? 'Join Session' : 'Join as Guest' }}
      </BaseButton>

      <button
        v-if="showGoogleSignIn"
        type="button"
        class="text-xs text-text-muted hover:text-text-secondary text-center transition-colors cursor-pointer"
        @click="emit('sign-in')"
      >
        Sign in with Google instead
      </button>
    </form>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import BaseButton from '../ui/BaseButton.vue'
import type { ParticipantResponse } from '../../types'

const props = defineProps<{
  sessionTitle: string
  participants: ParticipantResponse[]
  errorMessage: string | null
  busy: boolean
  isSignedIn: boolean
  showGoogleSignIn: boolean
}>()

const emit = defineEmits<{
  join: [displayName: string]
  'sign-in': []
}>()

const displayName = ref('')

function handleSubmit() {
  emit('join', props.isSignedIn ? '' : displayName.value.trim())
}
</script>
