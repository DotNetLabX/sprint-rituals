<script setup lang="ts">
import { useRoute } from 'vue-router'

const route = useRoute()

const errorMessages: Record<string, string> = {
  'no-account': 'Access denied. You need an invitation to use Fokus.',
  'deactivated': 'Your account has been deactivated. Contact your administrator.'
}

const error = route.query.error as string | undefined
const errorMessage = error ? (errorMessages[error] ?? 'An error occurred. Please try again.') : null
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
        <h1 class="text-lg font-semibold text-text-primary">Sign in to Fokus</h1>
        <p class="text-sm text-text-secondary mt-1">Sprint analytics for engineering teams</p>
      </div>

      <!-- Error message -->
      <div
        v-if="errorMessage"
        class="w-full rounded-md bg-red-500/10 border border-red-500/30 px-4 py-3 text-sm text-red-400 text-center"
      >
        {{ errorMessage }}
      </div>

      <!-- Sign in button -->
      <a
        href="/auth/login"
        class="flex items-center justify-center gap-3 w-full px-4 py-2.5 rounded-md bg-accent-default hover:bg-accent-hover text-white font-medium text-sm transition-colors"
      >
        <svg viewBox="0 0 24 24" class="w-5 h-5" fill="currentColor">
          <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4"/>
          <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853"/>
          <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z" fill="#FBBC05"/>
          <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335"/>
        </svg>
        Sign in with Google
      </a>
    </div>
  </div>
</template>
