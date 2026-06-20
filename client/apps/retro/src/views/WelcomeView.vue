<template>
  <div class="flex items-center justify-center min-h-[calc(100vh-3.5rem)]">
    <div class="flex flex-col items-center gap-4">
      <h1 class="text-2xl font-semibold text-text-primary">Welcome to Reflekt</h1>
      <button
        class="px-4 py-2 bg-primary-600 text-white rounded hover:bg-primary-700"
        @click="authStore.signIn()"
      >
        Sign in with Google
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useAuthStore } from '../stores/auth'

const authStore = useAuthStore()
</script>
