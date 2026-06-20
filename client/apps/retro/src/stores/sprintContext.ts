import { defineStore } from 'pinia'
import { getSprintContext } from '../api/sprintContext'
import type { SprintContextSection } from '../types/sprintContext'

export const useSprintContextStore = defineStore('sprintContext', {
  state: () => ({
    sections: [] as SprintContextSection[],
    loading: false,
    error: null as string | null,
    // null = nothing loaded yet; set to the id only on successful fetch so errors are retryable
    loadedFor: null as number | null,
  }),
  actions: {
    async fetchSprintContext(retroSessionId: number): Promise<void> {
      if (this.loadedFor === retroSessionId) return
      this.loading = true
      this.error = null
      this.sections = []
      try {
        const response = await getSprintContext(retroSessionId)
        this.sections = response.sprints
        this.loadedFor = retroSessionId
      } catch (e) {
        this.error = e instanceof Error ? e.message : 'Failed to load sprint context'
      } finally {
        this.loading = false
      }
    },
  },
})
