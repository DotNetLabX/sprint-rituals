<template>
  <nav class="flex items-center justify-center gap-0" aria-label="Retro phase">
    <template v-for="(phase, index) in phases" :key="phase.name">
      <div v-if="index > 0" class="h-0.5 w-6 sm:w-10" :class="index <= currentIndex ? 'bg-primary-500' : 'bg-border'" />
      <div class="flex flex-col items-center gap-1">
        <div
          class="flex items-center justify-center rounded-full transition-all duration-300"
          :class="stepCircleClasses(index)"
        >
          <Check v-if="index < currentIndex" :size="iconSize" />
          <component v-else :is="phase.icon" :size="iconSize" />
        </div>
        <span
          class="text-[10px] sm:text-xs font-medium transition-colors duration-300"
          :class="index <= currentIndex ? 'text-badge-primary-text' : 'text-text-muted'"
        >
          <span class="hidden sm:inline">{{ phase.name }}</span>
          <span class="sm:hidden">{{ phase.short }}</span>
        </span>
      </div>
    </template>
  </nav>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { PenLine, Layers, ThumbsUp, MessageSquare, FileText, Check } from 'lucide-vue-next'

const props = defineProps<{
  currentPhase: string
}>()

const phases = [
  { name: 'Writing', short: 'Write', icon: PenLine },
  { name: 'Grouping', short: 'Group', icon: Layers },
  { name: 'Voting', short: 'Vote', icon: ThumbsUp },
  { name: 'Discussing', short: 'Discuss', icon: MessageSquare },
  { name: 'Reporting', short: 'Report', icon: FileText },
]

const currentIndex = computed(() => {
  const idx = phases.findIndex((p) => p.name === props.currentPhase)
  return idx === -1 ? 0 : idx
})

const iconSize = 16

function stepCircleClasses(index: number) {
  if (index < currentIndex.value) {
    return 'w-8 h-8 bg-primary-500 text-white'
  }
  if (index === currentIndex.value) {
    return 'w-9 h-9 bg-primary-600 text-white ring-4 ring-primary-100'
  }
  return 'w-8 h-8 bg-surface text-text-muted border-2 border-border'
}
</script>
