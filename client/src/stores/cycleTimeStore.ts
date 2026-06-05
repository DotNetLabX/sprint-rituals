import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { SprintItem, CycleTimeResponse, CycleTimeBoundariesResponse } from '../types'
import { getCycleTime, getSprints, getSubTeams } from '../api/analytics'
import { getCycleTimeBoundaries } from '../api/settings'

export const useCycleTimeStore = defineStore('cycleTime', () => {
  const sprints = ref<SprintItem[]>([])
  const subTeams = ref<string[]>([])
  const boundaries = ref<CycleTimeBoundariesResponse | null>(null)
  const selectedSprintId = ref<number | null>(null)
  const selectedLast = ref<number | null>(null)
  const sprintMode = ref<'single' | 'multi'>('single')
  const selectedSubTeam = ref<string | null>(null)
  const selectedPercentile = ref<number>(85)
  const cycleTime = ref<CycleTimeResponse | null>(null)
  const loading = ref(false)
  const initializing = ref(false)
  const error = ref<string | null>(null)

  // BR13: no workflow stages configured means empty state
  // Check workflowStageCount directly — availableStages includes done statuses too
  const hasWorkflowStages = computed<boolean>(() =>
    boundaries.value !== null && boundaries.value.workflowStageCount > 0
  )

  async function initialize() {
    initializing.value = true
    error.value = null
    try {
      const [sprintList, teams, boundaryResult] = await Promise.all([
        getSprints(),
        getSubTeams(),
        getCycleTimeBoundaries()
      ])
      sprints.value = sprintList
      subTeams.value = teams
      boundaries.value = boundaryResult

      // Default to most recent sprint (sprints returned descending by startDate)
      if (sprintMode.value === 'single' && selectedSprintId.value === null && sprintList.length > 0) {
        selectedSprintId.value = sprintList[0].id
      }

      await fetchData()
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to initialize cycle time'
    } finally {
      initializing.value = false
    }
  }

  async function selectSprint(sprintId: number) {
    sprintMode.value = 'single'
    selectedSprintId.value = sprintId
    selectedLast.value = null
    await fetchData()
  }

  async function selectLastN(n: number | null) {
    sprintMode.value = 'multi'
    selectedLast.value = n
    selectedSprintId.value = null
    await fetchData()
  }

  async function selectSubTeam(subTeam: string | null) {
    selectedSubTeam.value = subTeam
    await fetchData()
  }

  function selectPercentile(p: number) {
    selectedPercentile.value = p
    // No re-fetch — purely UI state
  }

  async function fetchData() {
    loading.value = true
    error.value = null
    try {
      const subTeam = selectedSubTeam.value ?? undefined

      if (sprintMode.value === 'single') {
        cycleTime.value = await getCycleTime(selectedSprintId.value ?? undefined, undefined, subTeam)
      } else {
        // selectedLast === null means "all sprints" → send last=0
        const last = selectedLast.value === null ? 0 : selectedLast.value
        cycleTime.value = await getCycleTime(undefined, last, subTeam)
      }
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to load cycle time data'
    } finally {
      loading.value = false
    }
  }

  return {
    sprints,
    subTeams,
    boundaries,
    selectedSprintId,
    selectedLast,
    sprintMode,
    selectedSubTeam,
    selectedPercentile,
    cycleTime,
    loading,
    initializing,
    error,
    hasWorkflowStages,
    initialize,
    selectSprint,
    selectLastN,
    selectSubTeam,
    selectPercentile,
    fetchData
  }
})
