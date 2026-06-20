import { defineStore } from 'pinia';

export interface TeamSummary {
  id: number;
  name: string;
  createdAt: string;
  retroSessionCount: number;
  lastRetroSessionDate: string | null;
  hasActiveRetroSession: boolean;
}

export const useTeamsStore = defineStore('teams', {
  state: () => ({
    teams: [] as TeamSummary[],
    loaded: false,
  }),
  getters: {
    hasTeams: (s) => s.teams.length > 0,
    isAtLimit: (s) => s.teams.length >= 10,
    teamById: (s) => (id: number) => s.teams.find((t) => t.id === id) ?? null,
  },
  actions: {
    async fetchMyTeams() {
      const res = await fetch('/api/teams', { credentials: 'include' });
      this.teams = res.ok ? ((await res.json()) as TeamSummary[]) : [];
      this.loaded = true;
    },
    async createTeam(name: string): Promise<TeamSummary> {
      const res = await fetch('/api/teams', {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name }),
      });
      if (!res.ok) {
        const err = await res.json().catch(() => ({}));
        throw err;
      }
      const team = (await res.json()) as TeamSummary;
      this.teams.push(team);
      return team;
    },
    async deleteTeam(teamId: number, confirmationName: string): Promise<void> {
      const res = await fetch(`/api/teams/${teamId}`, {
        method: 'DELETE',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ confirmationName }),
      });
      if (!res.ok) {
        const err = await res.json().catch(() => ({}));
        throw err;
      }
      this.teams = this.teams.filter((t) => t.id !== teamId);
    },
  },
});
