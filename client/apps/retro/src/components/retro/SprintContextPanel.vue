<template>
  <Transition
    enter-active-class="transition-transform duration-300 ease-out"
    enter-from-class="translate-x-full"
    enter-to-class="translate-x-0"
    leave-active-class="transition-transform duration-200 ease-in"
    leave-from-class="translate-x-0"
    leave-to-class="translate-x-full"
  >
    <aside
      v-if="open"
      class="fixed inset-y-0 right-0 w-80 sm:w-96 z-40 bg-surface shadow-xl border-l border-border flex flex-col overflow-hidden"
    >
      <!-- Panel header -->
      <div class="flex items-center justify-between px-4 py-3 border-b border-border flex-shrink-0">
        <h2 class="text-sm font-semibold text-text-primary">Sprint Context</h2>
        <button
          class="text-text-secondary hover:text-text-primary transition-colors cursor-pointer"
          aria-label="Close sprint context panel"
          @click="$emit('close')"
        >
          <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>

      <!-- Multi-sprint info header -->
      <div
        v-if="!sprintContextStore.loading && sprintContextStore.sections.length > 1"
        class="px-4 py-2 text-xs text-text-secondary border-b border-border bg-surface-raised"
        :title="multiSprintTooltip"
      >
        This retro covers more than one sprint. Each section below shows the metrics for one of them.
      </div>

      <!-- Loading state -->
      <div v-if="sprintContextStore.loading" class="flex-1 flex items-center justify-center">
        <LoadingSpinner>Loading sprint context...</LoadingSpinner>
      </div>

      <!-- Error state -->
      <div v-else-if="sprintContextStore.error" class="flex-1 flex flex-col items-center justify-center gap-2 px-6 text-center">
        <p class="text-sm text-danger">{{ sprintContextStore.error }}</p>
      </div>

      <!-- Empty state -->
      <div v-else-if="sprintContextStore.sections.length === 0" class="flex-1 flex items-center justify-center px-6">
        <p class="text-sm text-text-secondary text-center">No sprint context available for this retro.</p>
      </div>

      <!-- Section list -->
      <div v-else class="flex-1 overflow-y-auto px-4 py-4 flex flex-col gap-4">
        <SprintContextSectionCard
          v-for="section in sprintContextStore.sections"
          :key="section.sprintId"
          :section="section"
        />
      </div>
    </aside>
  </Transition>
</template>

<script setup lang="ts">
import { useSprintContextStore } from '../../stores/sprintContext'
import LoadingSpinner from '../ui/LoadingSpinner.vue'
import SprintContextSectionCard from './SprintContextSectionCard.vue'

defineProps<{
  open: boolean
}>()

defineEmits<{
  close: []
}>()

const sprintContextStore = useSprintContextStore()

const multiSprintTooltip = "This retro covers more than one sprint. Each section below shows the metrics for one of them."
</script>
