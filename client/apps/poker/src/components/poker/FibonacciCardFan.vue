<template>
  <!-- Card fan for voting — L3 display + voter interaction -->
  <div class="flex flex-wrap gap-2 justify-center">
    <button
      v-for="value in FIBONACCI_SCALE"
      :key="value"
      :disabled="!canVote"
      :class="[
        'w-12 h-16 flex items-center justify-center text-lg font-bold rounded-[--radius-md] border-2 transition-all cursor-pointer select-none',
        selectedValue === value
          ? 'bg-primary-600 border-primary-500 text-white scale-110 shadow-card-hover'
          : 'bg-surface border-border text-text-primary hover:border-primary-400 hover:scale-105',
        !canVote ? 'opacity-50 cursor-not-allowed pointer-events-none' : '',
      ]"
      @click="emit('vote', value)"
    >
      {{ value }}
    </button>
  </div>
</template>

<script setup lang="ts">
import { FIBONACCI_SCALE } from '../../types'
import type { FibonacciValue } from '../../types'

defineProps<{
  selectedValue: string | null
  canVote: boolean
}>()

const emit = defineEmits<{ vote: [value: FibonacciValue] }>()
</script>
