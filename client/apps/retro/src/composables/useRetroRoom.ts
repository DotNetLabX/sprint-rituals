import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useGuestStore } from '../stores/guest'
import { useParticipantStore } from '../stores/participant'
import { useCardStore } from '../stores/card'
import { useAuthStore } from '../stores/auth'
import { useSprintHub } from './useSprintHub'
import { useVoteStore } from '../stores/vote'
import { useSprintStore } from '../stores/sprint'
import { usePolicyStore } from '../stores/policy'
import { useCardGroupStore } from '../stores/cardGroup'
import { useReportStore } from '../stores/report'
import { useReadinessStore } from '../stores/readiness'
import { usePreviousActionsStore } from '../stores/previousActions'

export function useRetroRoom(sprintId: number) {
  const guestStore = useGuestStore()
  const participantStore = useParticipantStore()
  const cardStore = useCardStore()
  const authStore = useAuthStore()
  const voteStore = useVoteStore()
  const sprintStore = useSprintStore()
  const policyStore = usePolicyStore()
  const cardGroupStore = useCardGroupStore()
  const reportStore = useReportStore()
  const readinessStore = useReadinessStore()

  const previousActionsStore = usePreviousActionsStore()

  const kicked = ref(false)
  const busy = ref(false)
  const errorMessage = ref('')
  const joined = ref(false)
  const loading = ref(true)

  const { connect, disconnect } = useSprintHub(() => {
    kicked.value = true
    guestStore.clearToken(sprintId)
    disconnect()
  })

  const sprintName = computed(() => sprintStore.currentSummary?.name ?? '')
  const currentPhase = computed(() => sprintStore.currentSummary?.currentPhase ?? '')
  const summaryParticipants = computed(() => sprintStore.currentSummary?.participants ?? [])
  const isReportingPhase = computed(() => currentPhase.value === 'Reporting')
  const isClosed = computed(() => sprintStore.currentSummary?.state === 'Closed')
  const teamId = computed(() => sprintStore.currentSummary?.teamId ?? null)
  const isScrumMaster = computed(() => sprintStore.currentSummary?.isScrumMaster ?? false)

  async function enterRoom() {
    await Promise.all([
      participantStore.fetchParticipants(sprintId),
      cardStore.fetchCards(sprintId),
      cardGroupStore.fetchGroups(sprintId),
      sprintStore.fetchSprintSummary(sprintId),
      previousActionsStore.fetchPreviousActions(sprintId),
    ])
    if (sprintStore.currentSummary) voteStore.votesPerParticipant = sprintStore.currentSummary.votesPerParticipant
    await voteStore.fetchMyVotes(sprintId)
    readinessStore.reset()
    await connect(sprintId)
    joined.value = true
    loading.value = false
    if (isReportingPhase.value) reportStore.fetchParticipantReport(sprintId)
  }

  async function handleJoin(displayName: string): Promise<void> {
    busy.value = true
    errorMessage.value = ''
    try {
      const response = await participantStore.join(sprintId, displayName.trim())
      guestStore.saveToken(sprintId, response.guestToken, response.displayName)
      await enterRoom()
    } catch (err: unknown) {
      const status = (err as { status?: number })?.status
      if (status === 409) errorMessage.value = 'Name already taken. Choose another.'
      else if (status === 404) errorMessage.value = 'Sprint not found.'
      else errorMessage.value = 'Failed to join. Please try again.'
    } finally {
      busy.value = false
    }
  }

  onMounted(async () => {
    await policyStore.fetch()
    await sprintStore.fetchSprintSummary(sprintId)

    if (!authStore.loaded) await authStore.fetchMe()
    if (authStore.isSignedIn) {
      try {
        const response = await participantStore.joinWithGoogle(sprintId)
        guestStore.saveToken(sprintId, response.guestToken, response.displayName)
        await enterRoom(); return
      } catch (err: unknown) {
        console.error('[joinWithGoogle] failed:', err)
        const status = (err as { status?: number })?.status
        if (status === 409) errorMessage.value = 'Sprint is not accepting participants.'
        else if (status === 404) errorMessage.value = 'Sprint not found.'
        else errorMessage.value = `Could not join with Google (${status ?? 'network error'}). You can join as a guest instead.`
      }
      loading.value = false
      return
    }

    guestStore.loadToken(sprintId)
    if (guestStore.token) {
      try { await participantStore.fetchMe(sprintId); await enterRoom(); return }
      catch (err: unknown) { if ((err as { status?: number })?.status === 401) guestStore.clearToken(sprintId) }
    }
    loading.value = false
  })

  onUnmounted(() => disconnect())

  const showGoogleOption = computed(() => !authStore.isSignedIn)

  return {
    kicked,
    busy,
    errorMessage,
    joined,
    loading,
    sprintName,
    currentPhase,
    summaryParticipants,
    isReportingPhase,
    isClosed,
    teamId,
    isScrumMaster,
    handleJoin,
    reportStore,
    showGoogleOption,
  }
}
