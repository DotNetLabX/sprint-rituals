<template>
  <div class="flex items-center gap-2">
    <!-- Timer area — clickable for SM to open controls -->
    <div
      class="relative flex items-center gap-1.5 rounded px-1.5 py-0.5 transition-colors"
      :class="isScrumMaster ? 'cursor-pointer hover:bg-surface-raised' : ''"
      @click="isScrumMaster ? (showControls = !showControls) : undefined"
    >
      <!-- Timer text -->
      <span
        class="font-mono text-sm font-semibold tabular-nums select-none"
        :class="[
          timerStatus === 'expired'
            ? 'text-red-600 animate-blink'
            : timerStatus === 'running'
              ? 'text-text-primary'
              : 'text-text-muted',
        ]"
      >
        {{ timerStatus === 'stopped' ? phaseDefaultFormatted : formattedTime }}
      </span>
      <!-- SM dropdown indicator -->
      <svg v-if="isScrumMaster" class="w-3 h-3 text-text-muted" viewBox="0 0 12 12" fill="currentColor">
        <path d="M2 4l4 4 4-4" />
      </svg>

      <!-- Mute toggle (visible to all when timer is not stopped) -->
      <button
        v-if="timerStatus !== 'stopped'"
        class="text-text-muted hover:text-text-secondary transition-colors cursor-pointer"
        :title="isMuted ? 'Unmute timer' : 'Mute timer'"
        @click.stop="toggleMute"
      >
        <svg
          v-if="isMuted"
          xmlns="http://www.w3.org/2000/svg"
          class="w-4 h-4"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
          stroke-linecap="round"
          stroke-linejoin="round"
        >
          <path d="M11 5L6 9H2v6h4l5 4V5z" />
          <line x1="23" y1="9" x2="17" y2="15" />
          <line x1="17" y1="9" x2="23" y2="15" />
        </svg>
        <svg
          v-else
          xmlns="http://www.w3.org/2000/svg"
          class="w-4 h-4"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
          stroke-linecap="round"
          stroke-linejoin="round"
        >
          <polygon points="11 5 6 9 2 9 2 15 6 15 11 19 11 5" />
          <path d="M19.07 4.93a10 10 0 0 1 0 14.14" />
          <path d="M15.54 8.46a5 5 0 0 1 0 7.07" />
        </svg>
      </button>

      <!-- SM controls popover -->
      <TimerControls
        v-if="isScrumMaster && showControls"
        :sprint-id="sprintId"
        :timer-status="timerStatus"
        @close="showControls = false"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useTimer } from '../../composables/useTimer'
import TimerControls from './TimerControls.vue'

const props = defineProps<{
  sprintId: number
  isScrumMaster: boolean
  currentPhase: string
  writingTimerDefault: number
  votingTimerDefault: number
  discussingTimerDefault: number
}>()

const showControls = ref(false)

const { timerStatus, formattedTime, isMuted, toggleMute } = useTimer()

const phaseDefaultSeconds = computed(() => {
  switch (props.currentPhase) {
    case 'Writing':
      return props.writingTimerDefault * 60
    case 'Voting':
      return props.votingTimerDefault * 60
    case 'Discussing':
      return props.discussingTimerDefault * 60
    default:
      return 0
  }
})

const phaseDefaultFormatted = computed(() => {
  const total = phaseDefaultSeconds.value
  const mins = Math.floor(total / 60)
  const secs = total % 60
  return `${String(mins).padStart(2, '0')}:${String(secs).padStart(2, '0')}`
})
</script>
