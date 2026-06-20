import { defineStore } from 'pinia'

// localStorage key format locked at Foundation: reflekt:sprint:{sprintId}:token
export const guestTokenKey = (sprintId: number) => `reflekt:sprint:${sprintId}:token`

export const useGuestStore = defineStore('guest', {
  state: () => ({
    token: null as string | null,
    displayName: null as string | null,
  }),
  actions: {
    loadToken(sprintId: number) {
      this.token = localStorage.getItem(guestTokenKey(sprintId))
    },
    saveToken(sprintId: number, token: string, displayName: string) {
      localStorage.setItem(guestTokenKey(sprintId), token)
      this.token = token
      this.displayName = displayName
    },
    clearToken(sprintId: number) {
      localStorage.removeItem(guestTokenKey(sprintId))
      this.token = null
      this.displayName = null
    },
  },
})
