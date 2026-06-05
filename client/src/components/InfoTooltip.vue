<script setup lang="ts">
defineProps<{
  text: string
}>()
</script>

<template>
  <span class="relative group/tip inline-flex text-text-muted cursor-help">
    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3.5 h-3.5">
      <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
    </svg>
    <span class="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 px-3 py-2 text-sm text-text-primary bg-surface-elevated border border-border-default rounded-lg shadow-lg opacity-0 group-hover/tip:opacity-100 pointer-events-none transition-opacity duration-150 whitespace-normal w-64 text-center z-50">
      {{ text }}
    </span>
  </span>
</template>
