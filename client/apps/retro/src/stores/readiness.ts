import { defineStore } from 'pinia'

interface ReadinessSnapshot {
  readyParticipantIds: number[]
  readyCount: number
  totalCount: number
}

interface ReadinessChangedPayload {
  participantId: number
  displayName: string | null
  isReady: boolean
  readyCount: number
  totalCount: number
}

export const useReadinessStore = defineStore('readiness', {
  state: () => ({
    readyParticipantIds: [] as number[],
    readyCount: 0,
    totalCount: 0,
  }),
  getters: {
    isReady: (state) => (participantId: number) => state.readyParticipantIds.includes(participantId),
  },
  actions: {
    setFromSnapshot(payload: ReadinessSnapshot) {
      this.readyParticipantIds = payload.readyParticipantIds
      this.readyCount = payload.readyCount
      this.totalCount = payload.totalCount
    },
    applyChange(payload: ReadinessChangedPayload) {
      if (payload.isReady) {
        if (!this.readyParticipantIds.includes(payload.participantId)) {
          this.readyParticipantIds.push(payload.participantId)
        }
      } else {
        this.readyParticipantIds = this.readyParticipantIds.filter((id) => id !== payload.participantId)
      }
      this.readyCount = payload.readyCount
      this.totalCount = payload.totalCount
    },
    reset() {
      this.readyParticipantIds = []
      this.readyCount = 0
      this.totalCount = 0
    },
  },
})
