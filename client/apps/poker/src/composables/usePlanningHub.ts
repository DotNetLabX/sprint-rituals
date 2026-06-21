import * as signalR from '@microsoft/signalr'
import { useGuestStore } from '../stores/guest'
import { usePlanningSessionStore } from '../stores/planningSession'
import type {
  ItemSelectedPayload,
  VotesRevealedPayload,
  EstimateDecidedPayload,
  ItemSkippedPayload,
  SessionClosedPayload,
  WriteBackFailedPayload,
} from '../types'

let connection: signalR.HubConnection | null = null

export function getHubConnection(): signalR.HubConnection | null {
  return connection
}

export function usePlanningHub(onSessionClosed?: (payload: SessionClosedPayload) => void) {
  const guestStore = useGuestStore()
  const sessionStore = usePlanningSessionStore()

  function build(sessionId: string) {
    connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/poker', {
        // Guest token sent as bearer for the PokerGuestToken scheme
        accessTokenFactory: () => guestStore.token ?? '',
      })
      .withAutomaticReconnect()
      .build()

    connection.on('ParticipantJoined', (payload: { participantId: string; displayName: string }) => {
      sessionStore.applyParticipantJoined(payload.participantId, payload.displayName)
    })

    connection.on('ParticipantLeft', (payload: { participantId: string }) => {
      sessionStore.applyParticipantLeft(payload.participantId)
    })

    connection.on('ItemSelected', (payload: ItemSelectedPayload) => {
      sessionStore.applyItemSelected(payload.itemId)
    })

    connection.on('VoteReceived', (payload: { participantId: string; itemId: string }) => {
      sessionStore.applyVoteReceived(payload.participantId, payload.itemId)
    })

    connection.on('VotesRevealed', (payload: VotesRevealedPayload) => {
      sessionStore.applyVotesRevealed(payload)
    })

    connection.on('EstimateDecided', (payload: EstimateDecidedPayload) => {
      sessionStore.applyEstimateDecided(payload.itemId, payload.finalEstimate)
    })

    connection.on('ItemSkipped', (payload: ItemSkippedPayload) => {
      sessionStore.applyItemSkipped(payload.itemId)
    })

    connection.on('SessionClosed', (payload: SessionClosedPayload) => {
      sessionStore.applySessionClosed(payload)
      onSessionClosed?.(payload)
    })

    connection.on('WriteBackFailed', (payload: WriteBackFailedPayload) => {
      sessionStore.applyWriteBackFailed(payload)
    })
  }

  async function connect(sessionId: string) {
    if (connection) await disconnect()
    build(sessionId)
    await connection!.start()
    await connection!.invoke('JoinSession', sessionId)
  }

  async function disconnect() {
    if (connection) {
      await connection.stop()
      connection = null
    }
  }

  // ---- SM hub method helpers ----
  // These invoke server methods; the resulting broadcasts update store state.

  async function selectItem(sessionId: string, itemId: string) {
    await connection?.invoke('SelectItem', sessionId, itemId)
  }

  async function revealVotes(sessionId: string, itemId: string) {
    await connection?.invoke('RevealVotes', sessionId, itemId)
  }

  async function decideEstimate(sessionId: string, itemId: string, value: string) {
    await connection?.invoke('DecideEstimate', sessionId, itemId, value)
  }

  async function skipItem(sessionId: string, itemId: string) {
    await connection?.invoke('SkipItem', sessionId, itemId)
  }

  async function closeSession(sessionId: string) {
    await connection?.invoke('CloseSession', sessionId)
  }

  // ---- Voter hub method helpers ----

  async function submitVote(sessionId: string, itemId: string, value: string) {
    await connection?.invoke('SubmitVote', sessionId, itemId, value)
  }

  async function changeVote(sessionId: string, itemId: string, value: string) {
    await connection?.invoke('ChangeVote', sessionId, itemId, value)
  }

  return {
    connect,
    disconnect,
    selectItem,
    revealVotes,
    decideEstimate,
    skipItem,
    closeSession,
    submitVote,
    changeVote,
  }
}
