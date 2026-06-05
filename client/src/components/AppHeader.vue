<script setup lang="ts">
import { useTheme } from '../composables/useTheme'

const { isDark, toggleTheme } = useTheme()
</script>

<template>
  <header class="flex items-center justify-between h-14 px-6 border-b border-border-default bg-surface-card shrink-0">
    <span class="text-sm font-semibold tracking-wide text-text-secondary uppercase">Fokus</span>
    <button
      @click="toggleTheme"
      class="flex items-center justify-center w-8 h-8 rounded-md text-text-secondary hover:text-text-primary hover:bg-surface-elevated transition-colors"
      :title="isDark ? 'Switch to light theme' : 'Switch to dark theme'"
    >
      <!-- Sun icon (shown in dark mode to switch to light) -->
      <svg v-if="isDark" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-4 h-4">
        <path d="M10 2a1 1 0 011 1v1a1 1 0 11-2 0V3a1 1 0 011-1zm4.22 1.78a1 1 0 011.42 1.42l-.7.7a1 1 0 11-1.42-1.42l.7-.7zm-9.86 0l.7.7A1 1 0 115.64 5.9l-.7-.7a1 1 0 011.42-1.42zM10 6a4 4 0 100 8 4 4 0 000-8zm8 4a1 1 0 01-1 1h-1a1 1 0 110-2h1a1 1 0 011 1zM4 10a1 1 0 01-1 1H2a1 1 0 110-2h1a1 1 0 011 1zm11.36 4.64a1 1 0 010 1.42l-.7.7a1 1 0 11-1.42-1.42l.7-.7a1 1 0 011.42 0zM5.34 15.36l-.7.7a1 1 0 01-1.42-1.42l.7-.7a1 1 0 011.42 1.42zM10 17a1 1 0 011 1v1a1 1 0 11-2 0v-1a1 1 0 011-1z" />
      </svg>
      <!-- Moon icon (shown in light mode to switch to dark) -->
      <svg v-else xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-4 h-4">
        <path d="M17.293 13.293A8 8 0 016.707 2.707a8.001 8.001 0 1010.586 10.586z" />
      </svg>
    </button>
  </header>
</template>
