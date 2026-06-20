import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { SprintItem, ScopeChangeResponse, CarryOverResponse, TestTimelineResponse } from '../types'
import { getScopeChange, getCarryOver, getSprints, getSubTeams, getTestTimeline } from '../api/analytics'

export const useSprintsStore = defineStore('sprints', () => {
  const sprints = ref<SprintItem[]>([])
  const subTeams = ref<string[]>([])
  const selectedSprintId = ref<number | null>(null)
  const selectedLast = ref<number | null>(5)
  const sprintMode = ref<'single' | 'multi'>('multi')
  const selectedSubTeam = ref<string | null>(null)
  const scopeChange = ref<ScopeChangeResponse | null>(null)
  const carryOver = ref<CarryOverResponse | null>(null)
  const testTimeline = ref<TestTimelineResponse | null>(null)
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

      await fetchAllData()
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to initialize sprints page'
    } finally {
      initializing.value = false
    }
  }

  async function selectSprint(sprintId: number) {
    sprintMode.value = 'single'
    selectedSprintId.value = sprintId
    selectedLast.value = null
    await fetchAllData()
  }

  async function selectLastN(n: number | null) {
    sprintMode.value = 'multi'
    selectedLast.value = n
    selectedSprintId.value = null
    await fetchAllData()
  }

  async function selectSubTeam(subTeam: string | null) {
    selectedSubTeam.value = subTeam
    await fetchAllData()
  }

  async function refreshSprints() {
    const sprintList = await getSprints()
    sprints.value = sprintList
  }

  async function fetchAllData() {
    loading.value = true
    error.value = null
    try {
      const sprintId = sprintMode.value === 'single' ? selectedSprintId.value ?? undefined : undefined
      const last = sprintMode.value === 'single' ? undefined : (selectedLast.value === null ? 0 : selectedLast.value)
      const subTeam = selectedSubTeam.value ?? undefined

      if (sprintMode.value === 'single' && sprintId !== undefined) {
        const [scopeChangeResult, carryOverResult, testTimelineResult] = await Promise.all([
          getScopeChange(sprintId, last, subTeam),
          getCarryOver(sprintId, last, subTeam),
          getTestTimeline(sprintId, subTeam)
        ])
        scopeChange.value = scopeChangeResult
        carryOver.value = carryOverResult
        testTimeline.value = testTimelineResult
      } else {
        testTimeline.value = null
        const [scopeChangeResult, carryOverResult] = await Promise.all([
          getScopeChange(sprintId, last, subTeam),
          getCarryOver(sprintId, last, subTeam)
        ])
        scopeChange.value = scopeChangeResult
        carryOver.value = carryOverResult
      }
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to load analytics data'
    } finally {
      loading.value = false
    }
  }

  return {
    sprints,
    subTeams,
    selectedSprintId,
    selectedLast,
    sprintMode,
    selectedSubTeam,
    scopeChange,
    carryOver,
    testTimeline,
    loading,
    initializing,
    error,
    initialize,
    selectSprint,
    selectLastN,
    selectSubTeam,
    refreshSprints,
    fetchAllData
  }
})
