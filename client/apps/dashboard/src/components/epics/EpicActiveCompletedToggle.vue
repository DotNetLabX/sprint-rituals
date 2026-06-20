<script setup lang="ts">
defineProps<{
  activeFilter: 'active' | 'completed'
}>()

const emit = defineEmits<{
  'update:activeFilter': [filter: 'active' | 'completed']
}>()
</script>

<template>
  <div class="flex items-center gap-1">
    <span
      class="text-xs text-text-muted cursor-help mr-1"
      title="Switch between in-progress and finished epics."
    >
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3.5 h-3.5 inline">
        <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
      </svg>
    </span>
    <div class="flex gap-1 border-b border-border-default">
      <button
        :class="[
          'px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors',
          activeFilter === 'active'
            ? 'border-accent-default text-accent-default'
            : 'border-transparent text-text-secondary hover:text-text-primary'
        ]"
        @click="emit('update:activeFilter', 'active')"
      >
        Active
      </button>
      <button
        :class="[
          'px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors',
          activeFilter === 'completed'
            ? 'border-accent-default text-accent-default'
            : 'border-transparent text-text-secondary hover:text-text-primary'
        ]"
        @click="emit('update:activeFilter', 'completed')"
      >
        Completed
      </button>
    </div>
  </div>
</template>
