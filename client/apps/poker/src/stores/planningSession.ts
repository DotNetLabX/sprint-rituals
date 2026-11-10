import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { sessionsApi, sprintsApi } from '../api/sessions'
import type {
  SessionResponse,
  CreateSessionRequest,
  EstimationItemResponse,
  ParticipantResponse,
  PokerSprintInfo,
  PokerTicket,
  VotesRevealedPayload,
  WriteBackFailedPayload,
  SessionClosedPayload,
} from '../types'

export const usePlanningSessionStore = defineStore('planningSession', () => {
  // ---- State ----
  const session = ref<SessionResponse | null>(null)
  const sprints = ref<PokerSprintInfo[]>([])
  const defaultSprintId = ref<number | null>(null)
  const tickets = ref<PokerTicket[]>([])
  const activeItemId = ref<string | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)
  const writeBackError = ref<WriteBackFailedPayload | null>(null)

  // ---- Getters ----
  const activeItem = computed<EstimationItemResponse | null>(
    () => session.value?.items.find((i) => i.id === activeItemId.value) ?? null,
  )

  const participants = computed<ParticipantResponse[]>(() => session.value?.participants ?? [])

  const isSprintBound = computed(() => session.value?.mode === 'SprintBound')

  const isClosed = computed(() => session.value?.status === 'Closed')

  // ---- Actions ----

  async function fetchSession(sessionId: string) {
    loading.value = true
    error.value = null
    try {
      session.value = await sessionsApi.get(sessionId)
      // Restore active item — first Voting item, if any
      const votingItem = session.value.items.find((i) => i.status === 'Voting')
      if (votingItem) activeItemId.value = votingItem.id
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to load session'
    } finally {
      loading.value = false
    }
  }

  async function fetchSprints() {
    try {
      const data = await sprintsApi.getSprints()
      sprints.value = data.sprints
      defaultSprintId.value = data.defaultSprintId
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to load sprints'
    }
  }

  async function createSession(payload: CreateSessionRequest): Promise<string> {
    const { sessionId } = await sessionsApi.create(payload)
    return sessionId
  }

  async function syncTicketItems(sessionId: string) {
    session.value = await sessionsApi.syncTicketItems(sessionId)
  }

  async function fetchTickets(sessionId: string) {
    try {
      tickets.value = await sprintsApi.getTickets(sessionId)
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to load tickets'
    }
  }

  // ---- SignalR state reconciliation ----
  // Each hub handler calls these to keep store in sync with server broadcasts.

  function applyParticipantJoined(participantId: string, displayName: string) {
    if (!session.value) return
    if (session.value.participants.some((p) => p.id === participantId)) return
    session.value.participants.push({
      id: participantId,
      displayName,
      isVoter: true,
      personId: null,
    })
  }

  function applyParticipantLeft(participantId: string) {
    if (!session.value) return
    session.value.participants = session.value.participants.filter((p) => p.id !== participantId)
  }

  async function applyItemSelected(itemId: string) {
    // Items added after this client loaded (ticket sync / free add) are not broadcast; reload to learn them.
    if (session.value && !session.value.items.some((i) => i.id === itemId)) await fetchSession(session.value.id)
    activeItemId.value = itemId
  }

  function applyVoteReceived(participantId: string, itemId: string) {
    // Record that this participant voted (value hidden until reveal)
    if (!session.value) return
    const item = session.value.items.find((i) => i.id === itemId)
    if (!item) return
    // Mark as voted — value is placeholder; real values arrive in VotesRevealed
    const existing = item.votes.find((v) => v.participantId === participantId)
    if (!existing) {
      item.votes.push({ participantId, value: '?' })
    }
  }

  function applyVotesRevealed(payload: VotesRevealedPayload) {
    if (!session.value) return
    const item = session.value.items.find((i) => i.id === payload.itemId)
    if (!item) return
    item.status = 'Revealed'
    // Replace placeholder votes with actual values from the reveal payload
    item.votes = payload.votes.map((v) => ({ participantId: v.participantId, value: v.value }))
  }

  function applyEstimateDecided(itemId: string, finalEstimate: string) {
    if (!session.value) return
    const item = session.value.items.find((i) => i.id === itemId)
    if (!item) return
    item.status = 'Decided'
    item.finalEstimate = finalEstimate
  }

  function applyItemSkipped(itemId: string) {
    if (!session.value) return
    const item = session.value.items.find((i) => i.id === itemId)
    if (!item) return
    item.status = 'Skipped'
  }

  function applySessionClosed(payload: SessionClosedPayload) {
    if (!session.value) return
    session.value.status = 'Closed'
    session.value.closedAt = new Date().toISOString()
    // Stats are in the summary endpoint — trigger a refetch if needed by caller
    void payload // suppress unused-param warning; caller may use for toast
  }

  function applyWriteBackFailed(payload: WriteBackFailedPayload) {
    writeBackError.value = payload
  }

  function clearWriteBackError() {
    writeBackError.value = null
  }

  return {
    session,
    sprints,
    defaultSprintId,
    tickets,
    activeItemId,
    activeItem,
    participants,
    isSprintBound,
    isClosed,
    loading,
    error,
    writeBackError,
    fetchSession,
    fetchSprints,
    createSession,
    syncTicketItems,
    fetchTickets,
    applyParticipantJoined,
    applyParticipantLeft,
    applyItemSelected,
    applyVoteReceived,
    applyVotesRevealed,
    applyEstimateDecided,
    applyItemSkipped,
    applySessionClosed,
    applyWriteBackFailed,
    clearWriteBackError,
  }
})
