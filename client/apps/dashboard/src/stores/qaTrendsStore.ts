import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { SprintItem, QaTrendsResponse } from '../types'
import { getSprints, getSubTeams, getQaTrends } from '../api/analytics'

export const useQaTrendsStore = defineStore('qaTrends', () => {
  const sprints = ref<SprintItem[]>([])
  const subTeams = ref<string[]>([])
  const selectedLast = ref<number>(10)
  const selectedSubTeam = ref<string | null>(null)
  const qaTrends = ref<QaTrendsResponse | null>(null)
  const loading = ref(false)
  const initializing = ref(false)
  const error = ref<string | null>(null)

  async function initialize() {
    initializing.value = true
    error.value = null
    try {
      const [sprintList, teams] = await Promise.all([getSprints(), getSubTeams()])
      sprints.value = sprintList
      subTeams.value = teams

      await fetchTrends()
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to initialize QA trends page'
    } finally {
      initializing.value = false
    }
  }

  async function selectLastN(n: number | null) {
    selectedLast.value = n === null ? 0 : n
    await fetchTrends()
  }

  async function selectSubTeam(subTeam: string | null) {
    selectedSubTeam.value = subTeam
    await fetchTrends()
  }

  async function fetchTrends() {
    loading.value = true
    error.value = null
    try {
      // selectedLast 0 = "all" (omit last param or send 0)
      const last = selectedLast.value === 0 ? 0 : selectedLast.value
      const subTeam = selectedSubTeam.value ?? undefined
      qaTrends.value = await getQaTrends(last, subTeam)
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to load QA trends data'
    } finally {
      loading.value = false
    }
  }

  return {
    sprints,
    subTeams,
    selectedLast,
    selectedSubTeam,
    qaTrends,
    loading,
    initializing,
    error,
    initialize,
    selectLastN,
    selectSubTeam,
    fetchTrends
  }
})
