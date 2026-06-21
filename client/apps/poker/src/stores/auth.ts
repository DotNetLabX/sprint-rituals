import { defineStore } from 'pinia'
import { fetchMe as sharedFetchMe, type AuthUser } from '@sprint-rituals/shared'

// Re-use the shared AuthUser type — includes `id` (Identity Person id) needed for SM check.
type User = AuthUser

export const useAuthStore = defineStore('auth', {
  state: () => ({
    user: null as User | null,
    loaded: false,
  }),
  getters: {
    isSignedIn: (s) => s.user !== null,
  },
  actions: {
    async fetchMe() {
      this.user = (await sharedFetchMe()) as User | null
      this.loaded = true
    },
    signIn() {
      window.location.href = '/auth/login'
    },
    async signOut() {
      await fetch('/api/auth/logout', { method: 'POST', credentials: 'same-origin' })
      this.user = null
      window.location.href = '/poker/'
    },
  },
})
