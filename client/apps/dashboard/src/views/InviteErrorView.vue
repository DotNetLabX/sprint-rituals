<script setup lang="ts">
import { useRoute } from 'vue-router'

const route = useRoute()

const errorMessages: Record<string, string> = {
  'expired': 'This invitation has expired.',
  'revoked': 'This invitation has been revoked.',
  'used': 'This invitation has already been used.',
  'email-mismatch': 'Sign in with the email this invitation was sent to.',
  'invalid': 'Invalid invitation link.'
}

const reason = route.query.reason as string | undefined
const message = errorMessages[reason ?? ''] ?? 'Invalid invitation link.'
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-app-bg">
    <div class="flex flex-col items-center gap-6 p-10 rounded-xl bg-panel-bg border border-border-default shadow-lg w-full max-w-sm">
      <!-- Logo -->
      <div class="flex items-center gap-2">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-7 h-7 text-accent-default">
          <path d="M10 12a2 2 0 100-4 2 2 0 000 4z" />
          <path fill-rule="evenodd" d="M.458 10C1.732 5.943 5.522 3 10 3s8.268 2.943 9.542 7c-1.274 4.057-5.064 7-9.542 7S1.732 14.057.458 10zM14 10a4 4 0 11-8 0 4 4 0 018 0z" clip-rule="evenodd" />
        </svg>
        <span class="text-xl font-bold text-text-primary tracking-wide">Fokus</span>
      </div>

      <div class="text-center">
        <h1 class="text-lg font-semibold text-text-primary">Invitation Error</h1>
        <p class="text-sm text-text-secondary mt-2">{{ message }}</p>
      </div>

      <a
        href="/login"
        class="text-sm text-accent-default hover:underline"
      >
        Go to login
      </a>
    </div>
  </div>
</template>
