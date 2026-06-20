import { ref, computed, watch, onUnmounted } from 'vue'
import { useTimerStore } from '../stores/timer'

export type TimerStatus = 'running' | 'paused' | 'expired' | 'stopped'

export interface TimerPayload {
  status: TimerStatus
  remainingSeconds: number
  endTimeUtc: string | null
}

const MUTE_KEY = 'reflekt:timer:muted'

let chimeAudio: HTMLAudioElement | null = null

function getChimeAudio(): HTMLAudioElement {
  if (!chimeAudio) {
    // Minimal base64-encoded single-beep WAV: 44-byte header + 1600 samples of 440 Hz sine at 8kHz 8-bit
    // Generated offline; provides audible chime without external file dependency
    const BASE64_CHIME =
      'UklGRlQGAABXQVZFZm10IBAAAAABAAEARKwAAESsAAABAAgAZGF0YTAG' +
      'AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA' +
      'AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA'
    chimeAudio = new Audio(`data:audio/wav;base64,${BASE64_CHIME}`)
    chimeAudio.volume = 0.5
  }
  return chimeAudio
}

export function useTimer() {
  const timerStore = useTimerStore()

  // Local countdown display — computed from store's endTimeUtc while running
  const displayRemainingSeconds = ref(timerStore.remainingSeconds)
  const isFlashing = ref(false)

  let intervalId: ReturnType<typeof setInterval> | null = null
  let flashTimeout: ReturnType<typeof setTimeout> | null = null

  const isMuted = computed(() => timerStore.isMuted)

  const formattedTime = computed<string>(() => {
    const total = Math.max(0, displayRemainingSeconds.value)
    const mins = Math.floor(total / 60)
    const secs = total % 60
    return `${String(mins).padStart(2, '0')}:${String(secs).padStart(2, '0')}`
  })

  function startInterval() {
    stopInterval()
    intervalId = setInterval(() => {
      const endTime = timerStore.endTimeUtc
      if (!endTime || timerStore.timerStatus !== 'running') {
        stopInterval()
        return
      }
      const diff = Math.floor((new Date(endTime).getTime() - Date.now()) / 1000)
      displayRemainingSeconds.value = Math.max(0, diff)
    }, 1000)
  }

  function stopInterval() {
    if (intervalId !== null) {
      clearInterval(intervalId)
      intervalId = null
    }
  }

  function triggerExpiry() {
    isFlashing.value = true
    if (!timerStore.isMuted) {
      try {
        getChimeAudio().play().catch(() => {
          // Ignore — browser may block autoplay
        })
      } catch {
        // Ignore audio errors
      }
    }
    if (flashTimeout !== null) clearTimeout(flashTimeout)
    flashTimeout = setTimeout(() => {
      isFlashing.value = false
      flashTimeout = null
    }, 3000)
  }

  // Watch store status changes to start/stop interval and trigger effects
  watch(
    () => timerStore.timerStatus,
    (status, prevStatus) => {
      if (status === 'running') {
        displayRemainingSeconds.value = timerStore.remainingSeconds
        startInterval()
      } else {
        stopInterval()
        displayRemainingSeconds.value = timerStore.remainingSeconds
      }

      if (status === 'expired' && prevStatus !== 'expired') {
        displayRemainingSeconds.value = 0
        triggerExpiry()
      }

      if (status === 'stopped') {
        isFlashing.value = false
      }
    },
    { immediate: true },
  )

  // Keep displayRemainingSeconds in sync when store updates while paused/stopped
  watch(
    () => timerStore.remainingSeconds,
    (val) => {
      if (timerStore.timerStatus !== 'running') {
        displayRemainingSeconds.value = val
      }
    },
  )

  function toggleMute() {
    timerStore.toggleMute()
  }

  onUnmounted(() => {
    stopInterval()
    if (flashTimeout !== null) clearTimeout(flashTimeout)
  })

  return {
    timerStatus: computed(() => timerStore.timerStatus),
    displayRemainingSeconds,
    isFlashing,
    isMuted,
    formattedTime,
    isRunning: computed(() => timerStore.isRunning),
    isExpired: computed(() => timerStore.isExpired),
    toggleMute,
  }
}
