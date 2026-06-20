import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useGuestStore } from '../stores/guest'
import { useParticipantStore } from '../stores/participant'
import { useCardStore } from '../stores/card'
import { useAuthStore } from '../stores/auth'
import { useRetroHub } from './useRetroHub'
import { useVoteStore } from '../stores/vote'
import { useRetroSessionStore } from '../stores/retroSession'
import { usePolicyStore } from '../stores/policy'
import { useCardGroupStore } from '../stores/cardGroup'
import { useReportStore } from '../stores/report'
import { useReadinessStore } from '../stores/readiness'
import { usePreviousActionsStore } from '../stores/previousActions'
import { useSprintContextStore } from '../stores/sprintContext'

export function useRetroRoom(retroSessionId: number) {
  const guestStore = useGuestStore()
  const participantStore = useParticipantStore()
  const cardStore = useCardStore()
  const authStore = useAuthStore()
  const voteStore = useVoteStore()
  const retroSessionStore = useRetroSessionStore()
  const policyStore = usePolicyStore()
  const cardGroupStore = useCardGroupStore()
  const reportStore = useReportStore()
  const readinessStore = useReadinessStore()

  const previousActionsStore = usePreviousActionsStore()
  const sprintContextStore = useSprintContextStore()

  const kicked = ref(false)
  const busy = ref(false)
  const errorMessage = ref('')
  const joined = ref(false)
  const loading = ref(true)

  const { connect, disconnect } = useRetroHub(() => {
    kicked.value = true
    guestStore.clearToken(retroSessionId)
    disconnect()
  })

  const retroSessionName = computed(() => retroSessionStore.currentSummary?.name ?? '')
  const currentPhase = computed(() => retroSessionStore.currentSummary?.currentPhase ?? '')
  const summaryParticipants = computed(() => retroSessionStore.currentSummary?.participants ?? [])
  const isReportingPhase = computed(() => currentPhase.value === 'Reporting')
  const isClosed = computed(() => retroSessionStore.currentSummary?.state === 'Closed')
  const teamId = computed(() => retroSessionStore.currentSummary?.teamId ?? null)
  const isScrumMaster = computed(() => retroSessionStore.currentSummary?.isScrumMaster ?? false)

  async function enterRoom() {
    await Promise.all([
      participantStore.fetchParticipants(retroSessionId),
      cardStore.fetchCards(retroSessionId),
      cardGroupStore.fetchGroups(retroSessionId),
      retroSessionStore.fetchRetroSessionSummary(retroSessionId),
      previousActionsStore.fetchPreviousActions(retroSessionId),
      sprintContextStore.fetchSprintContext(retroSessionId),
    ])
    if (retroSessionStore.currentSummary) voteStore.votesPerParticipant = retroSessionStore.currentSummary.votesPerParticipant
    await voteStore.fetchMyVotes(retroSessionId)
    readinessStore.reset()
    await connect(retroSessionId)
    joined.value = true
    loading.value = false
    if (isReportingPhase.value) reportStore.fetchParticipantReport(retroSessionId)
  }

  async function handleJoin(displayName: string): Promise<void> {
    busy.value = true
    errorMessage.value = ''
    try {
      const response = await participantStore.join(retroSessionId, displayName.trim())
      guestStore.saveToken(retroSessionId, response.guestToken, response.displayName)
      await enterRoom()
    } catch (err: unknown) {
      const status = (err as { status?: number })?.status
      if (status === 409) errorMessage.value = 'Name already taken. Choose another.'
      else if (status === 404) errorMessage.value = 'Retro not found.'
      else errorMessage.value = 'Failed to join. Please try again.'
    } finally {
      busy.value = false
    }
  }

  onMounted(async () => {
    await policyStore.fetch()
    await retroSessionStore.fetchRetroSessionSummary(retroSessionId)

    if (!authStore.loaded) await authStore.fetchMe()
    if (authStore.isSignedIn) {
      try {
        const response = await participantStore.joinWithGoogle(retroSessionId)
        guestStore.saveToken(retroSessionId, response.guestToken, response.displayName)
        await enterRoom(); return
      } catch (err: unknown) {
        console.error('[joinWithGoogle] failed:', err)
        const status = (err as { status?: number })?.status
        if (status === 409) errorMessage.value = 'Retro is not accepting participants.'
        else if (status === 404) errorMessage.value = 'Retro not found.'
        else errorMessage.value = `Could not join with Google (${status ?? 'network error'}). You can join as a guest instead.`
      }
      loading.value = false
      return
    }

    guestStore.loadToken(retroSessionId)
    if (guestStore.token) {
      try { await participantStore.fetchMe(retroSessionId); await enterRoom(); return }
      catch (err: unknown) { if ((err as { status?: number })?.status === 401) guestStore.clearToken(retroSessionId) }
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
    retroSessionName,
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
