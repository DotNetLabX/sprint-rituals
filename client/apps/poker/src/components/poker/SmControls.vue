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
          <p v-if="!castOptions.length" class="text-xs text-text-muted">No numeric votes cast — skip this item.</p>
          <button
            v-for="{ value, count } in castOptions"
            :key="value"
            :class="[
              'px-2.5 py-1 text-xs font-semibold rounded-[--radius-sm] border transition-colors cursor-pointer',
              decidingValue === value
                ? 'bg-primary-600 border-primary-500 text-white'
                : 'bg-surface border-border text-text-secondary hover:border-primary-400',
            ]"
            @click="decidingValue = value"
          >
            {{ value }} <span class="font-normal opacity-70">×{{ count }}</span>
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
import { ref, computed, watch } from 'vue'
import BaseButton from '../ui/BaseButton.vue'
import { FIBONACCI_SCALE } from '../../types'
import type { FibonacciValue, ItemStatus, VoteResponse, WriteBackFailedPayload } from '../../types'

const props = defineProps<{
  itemStatus: ItemStatus | null
  itemId: string | null
  votes: VoteResponse[]
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
// Rule 8: the estimate must be a value actually cast; '?' is never a final estimate (Rule 6).
const castOptions = computed(() => {
  const counts = new Map<FibonacciValue, number>()
  for (const v of props.votes) {
    if (v.value === '?' || !FIBONACCI_SCALE.includes(v.value as FibonacciValue)) continue
    counts.set(v.value as FibonacciValue, (counts.get(v.value as FibonacciValue) ?? 0) + 1)
  }
  return [...counts]
    .map(([value, count]) => ({ value, count }))
    .sort((a, b) => FIBONACCI_SCALE.indexOf(a.value) - FIBONACCI_SCALE.indexOf(b.value))
})

// Preselect the most-voted value (ties → the higher estimate) whenever a new item is revealed.
watch(
  () => [props.itemId, props.itemStatus, castOptions.value] as const,
  () => {
    const best = [...castOptions.value].sort((a, b) => b.count - a.count || FIBONACCI_SCALE.indexOf(b.value) - FIBONACCI_SCALE.indexOf(a.value))[0]
    decidingValue.value = canDecide.value && best ? best.value : null
  },
  { immediate: true },
)

const canSkip = computed(() => props.itemStatus === 'Voting' || props.itemStatus === 'Revealed')
</script>
