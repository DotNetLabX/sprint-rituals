<template>
  <div class="w-full max-w-sm">
    <BaseCard padding="lg">
      <div class="flex flex-col gap-4">
        <div class="flex items-center justify-center gap-2 text-primary-600">
          <RefreshCw :size="24" />
          <span class="text-xl font-semibold">Reflekt</span>
        </div>

        <h1 class="text-lg font-semibold text-text-primary text-center">{{ sprintName }}</h1>

        <div v-if="participants.length > 0" class="flex flex-col gap-2">
          <p class="text-xs font-semibold text-text-muted uppercase tracking-wide">Already in this retro:</p>
          <div class="flex flex-wrap gap-2">
            <div
              v-for="p in participants"
              :key="p.displayName"
              class="w-8 h-8 rounded-full bg-primary-100 text-primary-700 font-semibold text-sm flex items-center justify-center"
              :title="p.displayName"
            >
              {{ p.displayName.charAt(0).toUpperCase() }}
            </div>
          </div>
        </div>

        <p v-if="errorMessage" class="text-sm text-danger">{{ errorMessage }}</p>

        <template v-if="showGoogleSignIn">
          <a
            :href="googleLoginUrl"
            class="flex items-center justify-center gap-2 w-full px-4 py-2 border border-border rounded-[--radius-sm] text-sm font-medium text-text-secondary bg-surface hover:bg-surface-raised transition-colors"
          >
            Sign in with Google
          </a>

          <div class="flex items-center gap-3">
            <div class="flex-1 h-px bg-border" />
            <span class="text-xs text-text-muted">or</span>
            <div class="flex-1 h-px bg-border" />
          </div>
        </template>

        <form class="flex flex-col gap-3" @submit.prevent="$emit('join', displayName)">
          <BaseInput
            v-model="displayName"
            :maxlength="50"
            placeholder="Your display name"
            :disabled="busy"
          />
          <BaseButton
            type="submit"
            variant="primary"
            size="lg"
            class="w-full"
            :disabled="busy || !displayName.trim()"
            :loading="busy"
          >
            Join as Guest
          </BaseButton>
        </form>
      </div>
    </BaseCard>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { RefreshCw } from 'lucide-vue-next'
import BaseCard from '../ui/BaseCard.vue'
import BaseInput from '../ui/BaseInput.vue'
import BaseButton from '../ui/BaseButton.vue'

const props = defineProps<{
  sprintName: string
  sprintId: number
  participants: { displayName: string }[]
  errorMessage: string
  busy: boolean
  showGoogleSignIn: boolean
}>()

defineEmits<{
  join: [displayName: string]
}>()

const displayName = ref('')

const googleLoginUrl = computed(() => `/auth/login?returnUrl=/retro/sprint/${props.sprintId}`)
</script>

