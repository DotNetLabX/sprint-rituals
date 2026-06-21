<template>
  <!-- SM estimation loop controls: reveal, decide, skip, close -->
  <div class="flex flex-col gap-3">
    <h3 class="text-xs font-semibold text-text-muted uppercase tracking-wide">SM Controls</h3>

    <div class="flex flex-wrap gap-2">
      <BaseButton
        v-if="canReveal"
        size="sm"
        variant="secondary"
        @click="emit('reveal')"
      >
        Reveal Votes
      </BaseButton>

      <template v-if="canDecide">
        <div class="flex flex-wrap gap-1">
          <button
            v-for="value in FIBONACCI_SCALE"
            :key="value"
            :class="[
              'px-2.5 py-1 text-xs font-semibold rounded-[--radius-sm] border transition-colors cursor-pointer',
              decidingValue === value
                ? 'bg-primary-600 border-primary-500 text-white'
                : 'bg-surface border-border text-text-secondary hover:border-primary-400',
            ]"
            @click="decidingValue = value"
          >
            {{ value }}
          </button>
        </div>
        <BaseButton
          size="sm"
          variant="primary"
          :disabled="!decidingValue"
          @click="decidingValue && emit('decide', decidingValue)"
        >
          Set Estimate
        </BaseButton>
      </template>

      <BaseButton
        v-if="canSkip"
        size="sm"
        variant="ghost"
        @click="emit('skip')"
      >
        Skip
      </BaseButton>
    </div>

    <div v-if="writeBackError" class="px-3 py-2 bg-badge-danger-bg border border-badge-danger-text/30 rounded-[--radius-sm] text-sm text-badge-danger-text flex items-center justify-between gap-2">
      <span>Jira write-back failed: {{ writeBackError.reason }}</span>
      <button class="text-badge-danger-text/70 hover:text-badge-danger-text cursor-pointer" @click="emit('clear-write-back-error')">✕</button>
    </div>

    <div class="border-t border-border pt-3 mt-1">
      <BaseButton
        v-if="!sessionClosed"
        size="sm"
        variant="danger"
        @click="emit('close-session')"
      >
        Close Session
      </BaseButton>
      <p v-else class="text-xs text-text-muted">Session is closed.</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import BaseButton from '../ui/BaseButton.vue'
import { FIBONACCI_SCALE } from '../../types'
import type { FibonacciValue, ItemStatus, WriteBackFailedPayload } from '../../types'

const props = defineProps<{
  itemStatus: ItemStatus | null
  sessionClosed: boolean
  writeBackError: WriteBackFailedPayload | null
}>()

const emit = defineEmits<{
  reveal: []
  decide: [value: FibonacciValue]
  skip: []
  'close-session': []
  'clear-write-back-error': []
}>()

const decidingValue = ref<FibonacciValue | null>(null)

const canReveal = computed(() => props.itemStatus === 'Voting')
const canDecide = computed(() => props.itemStatus === 'Revealed')
const canSkip = computed(() => props.itemStatus === 'Voting' || props.itemStatus === 'Revealed')
</script>
