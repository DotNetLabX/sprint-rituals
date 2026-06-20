import { defineStore } from 'pinia'
import type { TimerPayload, TimerStatus } from '../composables/useTimer'

export const useTimerStore = defineStore('timer', {
  state: () => ({
    timerStatus: 'stopped' as TimerStatus,
    remainingSeconds: 0,
    endTimeUtc: null as string | null,
    isMuted: localStorage.getItem('reflekt:timer:muted') === 'true',
  }),
  getters: {
    isRunning: (state) => state.timerStatus === 'running',
    isExpired: (state) => state.timerStatus === 'expired',
    formattedTime: (state) => {
      const total = Math.max(0, state.remainingSeconds)
      const mins = Math.floor(total / 60)
      const secs = total % 60
      return `${String(mins).padStart(2, '0')}:${String(secs).padStart(2, '0')}`
    },
  },
  actions: {
    applyTimerUpdate(payload: TimerPayload) {
      this.timerStatus = payload.status
      this.remainingSeconds = payload.remainingSeconds
      this.endTimeUtc = payload.endTimeUtc
    },
    applyTimerSnapshot(payload: TimerPayload) {
      this.timerStatus = payload.status
      this.remainingSeconds = payload.remainingSeconds
      this.endTimeUtc = payload.endTimeUtc
    },
    clearTimer() {
      this.timerStatus = 'stopped'
      this.remainingSeconds = 0
      this.endTimeUtc = null
    },
    toggleMute() {
      this.isMuted = !this.isMuted
      localStorage.setItem('reflekt:timer:muted', String(this.isMuted))
    },
  },
})
