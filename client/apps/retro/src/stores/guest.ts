import { defineStore } from 'pinia'

// localStorage key format locked at Foundation: reflekt:retro-session:{retroSessionId}:token
export const guestTokenKey = (retroSessionId: number) => `reflekt:retro-session:${retroSessionId}:token`

export const useGuestStore = defineStore('guest', {
  state: () => ({
    token: null as string | null,
    displayName: null as string | null,
  }),
  actions: {
    loadToken(retroSessionId: number) {
      this.token = localStorage.getItem(guestTokenKey(retroSessionId))
    },
    saveToken(retroSessionId: number, token: string, displayName: string) {
      localStorage.setItem(guestTokenKey(retroSessionId), token)
      this.token = token
      this.displayName = displayName
    },
    clearToken(retroSessionId: number) {
      localStorage.removeItem(guestTokenKey(retroSessionId))
      this.token = null
      this.displayName = null
    },
  },
})
