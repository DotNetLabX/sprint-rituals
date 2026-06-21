import { defineStore } from 'pinia'

// localStorage key format: poker:session:{sessionId}:token
// Mirrors retro's reflekt:retro-session:{retroSessionId}:token pattern — conscious per-module duplication (Rule 23)
export const guestTokenKey = (sessionId: string) => `poker:session:${sessionId}:token`

export const useGuestStore = defineStore('guest', {
  state: () => ({
    token: null as string | null,
    displayName: null as string | null,
    participantId: null as string | null,
  }),
  actions: {
    loadToken(sessionId: string) {
      this.token = localStorage.getItem(guestTokenKey(sessionId))
    },
    saveToken(sessionId: string, token: string, displayName: string, participantId: string) {
      localStorage.setItem(guestTokenKey(sessionId), token)
      this.token = token
      this.displayName = displayName
      this.participantId = participantId
    },
    clearToken(sessionId: string) {
      localStorage.removeItem(guestTokenKey(sessionId))
      this.token = null
      this.displayName = null
      this.participantId = null
    },
  },
})
