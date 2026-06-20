import { defineStore } from 'pinia'

interface PhaseActionsEntry {
  phase: string
  actions: string[]
}

export const usePolicyStore = defineStore('policy', {
  state: () => ({
    matrix: {} as Record<string, string[]>,
  }),
  actions: {
    async fetch() {
      const res = await fetch('/api/phase-actions')
      if (res.ok) {
        const entries = (await res.json()) as PhaseActionsEntry[]
        const matrix: Record<string, string[]> = {}
        for (const entry of entries) {
          matrix[entry.phase] = entry.actions
        }
        this.matrix = matrix
      }
    },
  },
  getters: {
    isAllowed: (state) => (phase: string, action: string) =>
      state.matrix[phase]?.includes(action) ?? false,
  },
})
