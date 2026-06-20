import * as signalR from '@microsoft/signalr'
import { useGuestStore } from '../stores/guest'
import { useParticipantStore } from '../stores/participant'
import { useCardStore, type Card, type EvidenceLink } from '../stores/card'
import { useVoteStore } from '../stores/vote'
import { useSprintStore } from '../stores/sprint'
import { useCardGroupStore } from '../stores/cardGroup'
import { useReadinessStore } from '../stores/readiness'
import { useTimerStore } from '../stores/timer'
import { usePreviousActionsStore } from '../stores/previousActions'
import type { TimerPayload } from './useTimer'

let connection: signalR.HubConnection | null = null

export function getHubConnection(): signalR.HubConnection | null {
  return connection
}

export function useSprintHub(onKicked?: () => void) {
  const guestStore = useGuestStore()
  const participantStore = useParticipantStore()
  const cardStore = useCardStore()
  const voteStore = useVoteStore()
  const sprintStore = useSprintStore()
  const cardGroupStore = useCardGroupStore()
  const readinessStore = useReadinessStore()
  const timerStore = useTimerStore()
  const previousActionsStore = usePreviousActionsStore()

  function build(sprintId: number) {
    connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/retro', {
        accessTokenFactory: () => guestStore.token ?? '',
      })
      .withAutomaticReconnect()
      .build()

    connection.on('ParticipantJoined', (payload: { id: number; displayName: string }) => {
      const already = participantStore.participants.some((p) => p.id === payload.id)
      if (!already) {
        participantStore.participants.push({
          id: payload.id,
          sprintId,
          displayName: payload.displayName,
          joinedAt: new Date().toISOString(),
        })
      }
    })

    connection.on('ParticipantLeft', (_payload: { participantId: number }) => {})

    connection.on('ParticipantKicked', (payload: { participantId: number }) => {
      participantStore.participants = participantStore.participants.filter(
        (p) => p.id !== payload.participantId,
      )
      if (onKicked && participantStore.currentParticipant?.id === payload.participantId) {
        onKicked()
      }
    })

    connection.on('PhaseChanged', (phase: string) => {
      const wasWriting = sprintStore.currentSummary?.currentPhase === 'Writing'
      if (sprintStore.currentSummary) {
        sprintStore.currentSummary.currentPhase = phase
      }
      if (wasWriting && phase === 'Grouping') {
        cardStore.fetchCards(sprintId)
        cardGroupStore.fetchGroups(sprintId)
      } else if (phase !== 'Writing') {
        cardStore.fetchCards(sprintId)
        cardGroupStore.fetchGroups(sprintId)
      }
    })

    connection.on('CardCreated', (card: Card) => {
      if (!cardStore.cards.some((c) => c.id === card.id)) {
        cardStore.cards.unshift(card)
      }
    })

    connection.on('CardUpdated', (card: Card) => {
      const idx = cardStore.cards.findIndex((c) => c.id === card.id)
      if (idx !== -1) cardStore.cards[idx] = card
    })

    connection.on('CardDeleted', (payload: { id: number }) => {
      cardStore.cards = cardStore.cards.filter((c) => c.id !== payload.id)
    })

    connection.on('EvidenceLinkFetched', (link: EvidenceLink) => {
      const card = cardStore.cards.find((c) => c.id === link.cardId)
      if (card) {
        const idx = card.evidenceLinks.findIndex((e) => e.id === link.id)
        if (idx !== -1) card.evidenceLinks[idx] = link
        else card.evidenceLinks.push(link)
      }
    })

    connection.on('VoteCast', (payload: { groupId: number; newCount: number }) => {
      voteStore.setVoteCount(payload.groupId, payload.newCount)
    })

    connection.on('VoteRetracted', (payload: { groupId: number; newCount: number }) => {
      voteStore.setVoteCount(payload.groupId, payload.newCount)
    })

    connection.on('GroupCreated', () => {
      cardGroupStore.fetchGroups(sprintId)
    })

    connection.on('CardsGrouped', () => {
      cardGroupStore.fetchGroups(sprintId)
    })

    connection.on('CardUngrouped', () => {
      cardGroupStore.fetchGroups(sprintId)
    })

    connection.on('GroupDissolved', () => {
      cardGroupStore.fetchGroups(sprintId)
    })

    connection.on('GroupTitleChanged', (payload: { groupId: number; title: string | null }) => {
      const group = cardGroupStore.groups.find((g) => g.id === payload.groupId)
      if (group) group.title = payload.title
    })

    connection.on('GroupFocused', (payload: { groupId: number; previousGroupId: number | null }) => {
      cardGroupStore.groups.forEach((g) => (g.isFocused = g.id === payload.groupId))
    })

    connection.on('GroupFocusCleared', (_payload: { previousGroupId: number | null }) => {
      cardGroupStore.groups.forEach((g) => (g.isFocused = false))
    })

    connection.on(
      'GroupDiscussedChanged',
      (payload: { groupId: number; isDiscussed: boolean; discussedAt: string | null }) => {
        const group = cardGroupStore.groups.find((g) => g.id === payload.groupId)
        if (group) group.isDiscussed = payload.isDiscussed
      },
    )

    connection.on(
      'ColumnConfigUpdated',
      (payload: {
        sprintId: number
        column1Name: string
        column1Color: string
        column2Name: string
        column2Color: string
        column3Name: string
        column3Color: string
      }) => {
        if (sprintStore.currentSummary?.id === payload.sprintId) {
          sprintStore.currentSummary.column1Name = payload.column1Name
          sprintStore.currentSummary.column1Color = payload.column1Color
          sprintStore.currentSummary.column2Name = payload.column2Name
          sprintStore.currentSummary.column2Color = payload.column2Color
          sprintStore.currentSummary.column3Name = payload.column3Name
          sprintStore.currentSummary.column3Color = payload.column3Color
        }
      },
    )

    connection.on('SprintClosed', (_payload: { sprintId: number }) => {
      if (sprintStore.currentSummary) {
        sprintStore.currentSummary.state = 'Closed'
      }
    })

    connection.on(
      'ReadinessChanged',
      (payload: {
        participantId: number
        displayName: string | null
        isReady: boolean
        readyCount: number
        totalCount: number
      }) => {
        readinessStore.applyChange(payload)
      },
    )

    connection.on('ReadinessReset', (_payload: object) => {
      readinessStore.reset()
    })

    connection.on(
      'ReadinessSnapshot',
      (payload: { readyParticipantIds: number[]; readyCount: number; totalCount: number }) => {
        readinessStore.setFromSnapshot(payload)
      },
    )

    connection.on('TimerUpdated', (payload: TimerPayload) => {
      timerStore.applyTimerUpdate(payload)
    })

    connection.on('TimerSnapshot', (payload: TimerPayload) => {
      timerStore.applyTimerSnapshot(payload)
    })

    connection.on(
      'ReviewStatusChanged',
      (payload: { cardId: number; reviewStatus: string; reviewedCount: number; totalCount: number }) => {
        previousActionsStore.applyReviewStatusChanged(payload)
      },
    )
  }

  async function connect(sprintId: number) {
    if (connection) await disconnect()
    build(sprintId)
    await connection!.start()
    await connection!.invoke('JoinSprint', sprintId)
  }

  async function disconnect() {
    if (connection) {
      await connection.stop()
      connection = null
    }
  }

  return { connect, disconnect }
}

