import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { SprintItem, DeveloperThroughputResponse, BugRatioResponse, LeaderboardResponse, DeveloperQualityResponse, QaWorkloadResponse } from '../types'
import { getDeveloperThroughput, getBugRatio, getLeaderboard, getSprints, getSubTeams, getDeveloperQuality, getQaWorkload } from '../api/analytics'
import { setDeveloperCapacity } from '../api/developers'
import { useDailyProgressStore } from './dailyProgressStore'

export const useDevelopersStore = defineStore('developers', () => {
  const sprints = ref<SprintItem[]>([])
  const subTeams = ref<string[]>([])
  const selectedSprintId = ref<number | null>(null)
  const selectedLast = ref<number | null>(null)
  const sprintMode = ref<'single' | 'multi'>('single')
  const selectedSubTeam = ref<string | null>(null)
  const throughput = ref<DeveloperThroughputResponse | null>(null)
  const loading = ref(false)
  const initializing = ref(false)
  const error = ref<string | null>(null)

  const activeTab = ref<'throughput' | 'bugRatio' | 'leaderboard' | 'quality' | 'qaWorkload' | 'dailyProgress'>('throughput')
  const bugRatio = ref<BugRatioResponse | null>(null)
  const bugRatioLoading = ref(false)
  const bugRatioError = ref<string | null>(null)
  const leaderboard = ref<LeaderboardResponse | null>(null)
  const leaderboardLoading = ref(false)
  const leaderboardError = ref<string | null>(null)
  const quality = ref<DeveloperQualityResponse | null>(null)
  const qualityLoading = ref(false)
  const qualityError = ref<string | null>(null)
  const qaWorkload = ref<QaWorkloadResponse | null>(null)
  const qaWorkloadLoading = ref(false)
  const qaWorkloadError = ref<string | null>(null)

  async function initialize() {
    initializing.value = true
    error.value = null
    try {
      const [sprintList, teams] = await Promise.all([getSprints(), getSubTeams()])
      sprints.value = sprintList
      subTeams.value = teams

      if (sprintList.length > 0 && selectedSprintId.value === null && sprintMode.value === 'single') {
        selectedSprintId.value = sprintList[0].id
      }

      await fetchThroughput()
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to initialize developers page'
    } finally {
      initializing.value = false
    }
  }

  async function selectSprint(sprintId: number) {
    if (activeTab.value === 'dailyProgress') return
    sprintMode.value = 'single'
    selectedSprintId.value = sprintId
    selectedLast.value = null
    await fetchThroughput()
    if (activeTab.value === 'bugRatio') {
      await fetchBugRatio()
    }
    if (activeTab.value === 'leaderboard') {
      await fetchLeaderboard()
    }
    if (activeTab.value === 'quality') {
      await fetchQuality()
    }
    if (activeTab.value === 'qaWorkload') {
      await fetchQaWorkload()
    }
  }

  async function selectLastN(n: number | null) {
    if (activeTab.value === 'dailyProgress') return
    sprintMode.value = 'multi'
    selectedLast.value = n
    selectedSprintId.value = null
    await fetchThroughput()
    if (activeTab.value === 'bugRatio') {
      await fetchBugRatio()
    }
    if (activeTab.value === 'leaderboard') {
      await fetchLeaderboard()
    }
    if (activeTab.value === 'quality') {
      await fetchQuality()
    }
    if (activeTab.value === 'qaWorkload') {
      await fetchQaWorkload()
    }
  }

  async function selectSubTeam(subTeam: string | null) {
    selectedSubTeam.value = subTeam
    await fetchThroughput()
    if (activeTab.value === 'bugRatio') {
      await fetchBugRatio()
    }
    if (activeTab.value === 'leaderboard') {
      await fetchLeaderboard()
    }
    if (activeTab.value === 'quality') {
      await fetchQuality()
    }
    if (activeTab.value === 'qaWorkload') {
      await fetchQaWorkload()
    }
    if (activeTab.value === 'dailyProgress') {
      const dailyProgressStore = useDailyProgressStore()
      await dailyProgressStore.fetchProgress(subTeam)
    }
  }

  async function switchTab(tab: 'throughput' | 'bugRatio' | 'leaderboard' | 'quality' | 'qaWorkload' | 'dailyProgress') {
    // Stop SignalR when leaving dailyProgress tab
    if (activeTab.value === 'dailyProgress' && tab !== 'dailyProgress') {
      const dailyProgressStore = useDailyProgressStore()
      await dailyProgressStore.stopSignalR()
    }
    activeTab.value = tab
    if (tab === 'bugRatio') {
      await fetchBugRatio()
    }
    if (tab === 'leaderboard') {
      await fetchLeaderboard()
    }
    if (tab === 'quality') {
      await fetchQuality()
    }
    if (tab === 'qaWorkload') {
      await fetchQaWorkload()
    }
    if (tab === 'dailyProgress') {
      const dailyProgressStore = useDailyProgressStore()
      await dailyProgressStore.fetchProgress(selectedSubTeam.value)
      await dailyProgressStore.startSignalR()
    }
  }

  async function fetchThroughput() {
    loading.value = true
    error.value = null
    try {
      if (sprintMode.value === 'single') {
        throughput.value = await getDeveloperThroughput(
          selectedSprintId.value ?? undefined,
          undefined,
          selectedSubTeam.value ?? undefined
        )
      } else {
        throughput.value = await getDeveloperThroughput(
          undefined,
          selectedLast.value ?? undefined,
          selectedSubTeam.value ?? undefined
        )
      }
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to load developer throughput'
    } finally {
      loading.value = false
    }
  }

  async function fetchBugRatio() {
    bugRatioLoading.value = true
    bugRatioError.value = null
    try {
      if (sprintMode.value === 'single') {
        bugRatio.value = await getBugRatio(
          selectedSprintId.value ?? undefined,
          undefined,
          selectedSubTeam.value ?? undefined
        )
      } else {
        bugRatio.value = await getBugRatio(
          undefined,
          selectedLast.value ?? undefined,
          selectedSubTeam.value ?? undefined
        )
      }
    } catch (e) {
      bugRatioError.value = e instanceof Error ? e.message : 'Failed to load bug ratio data'
    } finally {
      bugRatioLoading.value = false
    }
  }

  async function fetchQuality() {
    qualityLoading.value = true
    qualityError.value = null
    try {
      if (sprintMode.value === 'single') {
        quality.value = await getDeveloperQuality(
          selectedSprintId.value ?? undefined,
          undefined,
          selectedSubTeam.value ?? undefined
        )
      } else {
        quality.value = await getDeveloperQuality(
          undefined,
          selectedLast.value ?? undefined,
          selectedSubTeam.value ?? undefined
        )
      }
    } catch (e) {
      qualityError.value = e instanceof Error ? e.message : 'Failed to load developer quality data'
    } finally {
      qualityLoading.value = false
    }
  }

  async function fetchQaWorkload() {
    qaWorkloadLoading.value = true
    qaWorkloadError.value = null
    try {
      if (sprintMode.value === 'single') {
        qaWorkload.value = await getQaWorkload(
          selectedSprintId.value ?? undefined,
          undefined,
          selectedSubTeam.value ?? undefined
        )
      } else {
        qaWorkload.value = await getQaWorkload(
          undefined,
          selectedLast.value ?? undefined,
          selectedSubTeam.value ?? undefined
        )
      }
    } catch (e) {
      qaWorkloadError.value = e instanceof Error ? e.message : 'Failed to load QA workload data'
    } finally {
      qaWorkloadLoading.value = false
    }
  }

  async function fetchLeaderboard() {
    leaderboardLoading.value = true
    leaderboardError.value = null
    try {
      if (sprintMode.value === 'single') {
        leaderboard.value = await getLeaderboard(
          selectedSprintId.value ?? undefined,
          undefined,
          selectedSubTeam.value ?? undefined
        )
      } else {
        leaderboard.value = await getLeaderboard(
          undefined,
          selectedLast.value ?? undefined,
          selectedSubTeam.value ?? undefined
        )
      }
    } catch (e) {
      leaderboardError.value = e instanceof Error ? e.message : 'Failed to load leaderboard data'
    } finally {
      leaderboardLoading.value = false
    }
  }

  async function updateCapacity(accountId: string, sprintId: number, capacityPercent: number) {
    if (throughput.value) {
      const devEntry = throughput.value.developers.find(d => d.accountId === accountId)
      const breakdown = devEntry?.sprintBreakdowns.find(b => b.sprintId === sprintId)
      const previousCapacity = breakdown?.capacityPercent ?? 100

      if (breakdown) {
        breakdown.capacityPercent = capacityPercent
      }

      try {
        await setDeveloperCapacity(accountId, sprintId, capacityPercent)
        await fetchThroughput()
      } catch (e) {
        if (breakdown) {
          breakdown.capacityPercent = previousCapacity
        }
        error.value = e instanceof Error ? e.message : 'Failed to update capacity'
      }
    } else {
      try {
        await setDeveloperCapacity(accountId, sprintId, capacityPercent)
        await fetchThroughput()
      } catch (e) {
        error.value = e instanceof Error ? e.message : 'Failed to update capacity'
      }
    }
  }

  return {
    sprints,
    subTeams,
    selectedSprintId,
    selectedLast,
    sprintMode,
    selectedSubTeam,
    throughput,
    loading,
    initializing,
    error,
    activeTab,
    bugRatio,
    bugRatioLoading,
    bugRatioError,
    leaderboard,
    leaderboardLoading,
    leaderboardError,
    quality,
    qualityLoading,
    qualityError,
    qaWorkload,
    qaWorkloadLoading,
    qaWorkloadError,
    initialize,
    selectSprint,
    selectLastN,
    selectSubTeam,
    switchTab,
    fetchThroughput,
    fetchBugRatio,
    fetchLeaderboard,
    fetchQuality,
    fetchQaWorkload,
    updateCapacity
  }
})
