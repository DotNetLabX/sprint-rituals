import { defineStore } from 'pinia';
import { fetchMe as sharedFetchMe } from '@sprint-rituals/shared';

interface User { email: string; displayName: string; givenName?: string; }

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
      // Shared helper — explicit credentials: 'same-origin', consistent with dashboard
      this.user = (await sharedFetchMe()) as User | null;
      this.loaded = true;
    },
    signIn() {
      window.location.href = '/auth/login';
    },
    async signOut() {
      await fetch('/api/auth/logout', { method: 'POST', credentials: 'same-origin' });
      this.user = null;
      window.location.href = '/retro/';
    },
  },
});

