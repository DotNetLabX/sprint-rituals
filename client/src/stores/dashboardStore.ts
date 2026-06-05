import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { SprintItem, SprintSummaryResponse, QaMetricsResponse, UntestedTicket, FailingTicket } from '../types'
import { getSprintSummary, getSprints, getSubTeams, getQaMetrics, getUntestedTickets, getFailingTickets } from '../api/analytics'

export const useDashboardStore = defineStore('dashboard', () => {
  const sprints = ref<SprintItem[]>([])
  const subTeams = ref<string[]>([])
  const selectedSprintId = ref<number | null>(null)
  const selectedSubTeam = ref<string | null>(null)
  const summary = ref<SprintSummaryResponse | null>(null)
  const loading = ref(false)
  const initializing = ref(false)
  const error = ref<string | null>(null)

  const qaMetrics = ref<QaMetricsResponse | null>(null)
  const untestedTickets = ref<UntestedTicket[]>([])
  const failingTickets = ref<FailingTicket[]>([])
  const qaLoading = ref(false)

  async function initialize() {
    initializing.value = true
    error.value = null
    try {
      const [sprintList, teams] = await Promise.all([getSprints(), getSubTeams()])
      sprints.value = sprintList
      subTeams.value = teams

      if (sprintList.length > 0 && selectedSprintId.value === null) {
        selectedSprintId.value = sprintList[0].id
      }

      await fetchSummary()
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to initialize dashboard'
    } finally {
      initializing.value = false
    }
  }

  async function selectSprint(sprintId: number) {
    selectedSprintId.value = sprintId
    clearQaState()
    await fetchSummary()
  }

  async function selectSubTeam(subTeam: string | null) {
    selectedSubTeam.value = subTeam
    clearQaState()
    await fetchSummary()
  }

  async function fetchSummary() {
    loading.value = true
    error.value = null
    try {
      summary.value = await getSprintSummary(
        selectedSprintId.value ?? undefined,
        selectedSubTeam.value ?? undefined
      )
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to load sprint summary'
    } finally {
      loading.value = false
    }
  }

  function clearQaState() {
    qaMetrics.value = null
    untestedTickets.value = []
    failingTickets.value = []
  }

  async function fetchQaMetrics() {
    if (selectedSprintId.value === null) return
    qaLoading.value = true
    try {
      qaMetrics.value = await getQaMetrics(selectedSprintId.value, selectedSubTeam.value ?? undefined)
    } catch {
      // non-fatal — QA section will remain hidden
    } finally {
      qaLoading.value = false
    }
  }

  async function fetchUntestedTickets() {
    if (selectedSprintId.value === null) return
    try {
      const result = await getUntestedTickets(selectedSprintId.value, selectedSubTeam.value ?? undefined)
      untestedTickets.value = result.tickets
    } catch {
      // non-fatal
    }
  }

  async function fetchFailingTickets() {
    if (selectedSprintId.value === null) return
    try {
      const result = await getFailingTickets(selectedSprintId.value, selectedSubTeam.value ?? undefined)
      failingTickets.value = result.tickets
    } catch {
      // non-fatal
    }
  }

  return {
    sprints,
    subTeams,
    selectedSprintId,
    selectedSubTeam,
    summary,
    loading,
    initializing,
    error,
    qaMetrics,
    untestedTickets,
    failingTickets,
    qaLoading,
    initialize,
    selectSprint,
    selectSubTeam,
    fetchSummary,
    fetchQaMetrics,
    fetchUntestedTickets,
    fetchFailingTickets
  }
})
