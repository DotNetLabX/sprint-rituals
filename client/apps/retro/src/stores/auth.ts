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
      // Come back to the exact page we left from (absolute URL keeps us on this origin —
      // e.g. the Vite dev server on :5172 — instead of the backend's default module).
      const returnUrl = encodeURIComponent(window.location.href);
      window.location.href = `/auth/login?returnUrl=${returnUrl}`;
    },
    async signOut() {
      await fetch('/api/auth/logout', { method: 'POST', credentials: 'same-origin' });
      this.user = null;
      window.location.href = '/retro/';
    },
  },
});

