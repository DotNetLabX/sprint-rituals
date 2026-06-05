import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { TeamDeveloperDto, UpdateTeamConfigRequest } from '../types'
import { getTeamRoster, updateTeamConfig } from '../api/team'

export const useTeamStore = defineStore('team', () => {
  const developers = ref<TeamDeveloperDto[]>([])
  const subTeams = ref<string[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  const groupedBySubTeam = computed(() => {
    const groups = new Map<string | null, TeamDeveloperDto[]>()
    for (const dev of developers.value) {
      const key = dev.subTeam ?? null
      if (!groups.has(key)) groups.set(key, [])
      groups.get(key)!.push(dev)
    }
    return groups
  })

  async function fetchTeam() {
    loading.value = true
    error.value = null
    try {
      const response = await getTeamRoster()
      developers.value = response.developers
      subTeams.value = response.subTeams
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to load team roster'
    } finally {
      loading.value = false
    }
  }

  async function updateConfig(accountId: string, req: UpdateTeamConfigRequest) {
    // Optimistic update
    const index = developers.value.findIndex(d => d.accountId === accountId)
    const previous = index !== -1 ? { ...developers.value[index] } : null

    if (index !== -1) {
      const dev = developers.value[index]
      if (req.role !== undefined && req.role !== null) dev.role = req.role
      if (req.defaultCapacityPercent !== undefined && req.defaultCapacityPercent !== null) dev.defaultCapacityPercent = req.defaultCapacityPercent
      if ('subTeam' in req) dev.subTeam = req.subTeam ?? null
      if (req.isActive !== undefined && req.isActive !== null) dev.isActive = req.isActive
    }

    try {
      const updated = await updateTeamConfig(accountId, req)
      if (index !== -1) {
        const oldSubTeam = previous?.subTeam ?? null
        const newSubTeam = updated.subTeam ?? null

        developers.value[index] = updated

        if (oldSubTeam !== newSubTeam) {
          // Add new sub-team to list if it's not already present
          if (newSubTeam !== null && !subTeams.value.includes(newSubTeam)) {
            subTeams.value = [...subTeams.value, newSubTeam].sort()
          }
          // Remove old sub-team from list if no other developer uses it
          if (oldSubTeam !== null) {
            const stillUsed = developers.value.some(d => d.subTeam === oldSubTeam)
            if (!stillUsed) {
              subTeams.value = subTeams.value.filter(st => st !== oldSubTeam)
            }
          }
        }
      }
    } catch (e) {
      // Revert optimistic update on failure
      if (index !== -1 && previous !== null) {
        developers.value[index] = previous
      }
      error.value = e instanceof Error ? e.message : 'Failed to update team config'
      throw e
    }
  }

  return {
    developers,
    subTeams,
    loading,
    error,
    groupedBySubTeam,
    fetchTeam,
    updateConfig
  }
})
