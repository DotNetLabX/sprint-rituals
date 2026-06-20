import { defineStore } from 'pinia'
import { useGuestStore } from './guest'

export interface EvidenceLink {
  id: number
  cardId: number
  url: string
  source: string
  title: string | null
  description: string | null
  iconUrl: string | null
  imageUrl: string | null
  externalKey: string | null
  fetchedAt: string | null
  fetchStatus: string
}

export interface Card {
  id: number
  sprintId: number
  participantId: number
  columnType: string
  cardType: string
  text: string
  assigneeName: string | null
  groupId: number | null
  createdAt: string
  updatedAt: string
  voteCount: number
  evidenceLinks: EvidenceLink[]
}

interface CreateCardPayload {
  text: string
  columnType: string
  cardType: string
  assigneeName?: string | null
}

interface EditCardPayload {
  text: string
  assigneeName?: string | null
}

export const useCardStore = defineStore('card', {
  state: () => ({
    cards: [] as Card[],
  }),
  getters: {
    column1Cards: (state) => state.cards.filter((c) => c.columnType === 'Column1'),
    column2Cards: (state) => state.cards.filter((c) => c.columnType === 'Column2'),
    column3Cards: (state) => state.cards.filter((c) => c.columnType === 'Column3'),
    actionItemCards: (state) => state.cards.filter((c) => c.columnType === 'ActionItems'),
  },
  actions: {
    async fetchCards(sprintId: number): Promise<void> {
      const guestStore = useGuestStore()
      const headers: Record<string, string> = {}
      if (guestStore.token) headers['X-Guest-Token'] = guestStore.token
      const res = await fetch(`/api/sprints/${sprintId}/cards`, { headers, credentials: 'include' })
      if (!res.ok) return
      const cards = (await res.json()) as Card[]
      this.cards = cards
    },

    async createCard(sprintId: number, payload: CreateCardPayload): Promise<Card> {
      const guestStore = useGuestStore()
      const res = await fetch(`/api/sprints/${sprintId}/cards`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(guestStore.token ? { 'X-Guest-Token': guestStore.token } : {}),
        },
        credentials: 'include',
        body: JSON.stringify(payload),
      })
      if (!res.ok) throw { status: res.status, body: await res.json().catch(() => ({})) }
      return (await res.json()) as Card
    },

    async editCard(sprintId: number, cardId: number, payload: EditCardPayload): Promise<Card> {
      const guestStore = useGuestStore()
      const res = await fetch(`/api/sprints/${sprintId}/cards/${cardId}`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          ...(guestStore.token ? { 'X-Guest-Token': guestStore.token } : {}),
        },
        credentials: 'include',
        body: JSON.stringify(payload),
      })
      if (!res.ok) throw { status: res.status, body: await res.json().catch(() => ({})) }
      const updated = (await res.json()) as Card
      const idx = this.cards.findIndex((c) => c.id === cardId)
      if (idx !== -1) this.cards[idx] = updated
      return updated
    },

    async deleteCard(sprintId: number, cardId: number): Promise<void> {
      const guestStore = useGuestStore()
      const res = await fetch(`/api/sprints/${sprintId}/cards/${cardId}`, {
        method: 'DELETE',
        headers: guestStore.token ? { 'X-Guest-Token': guestStore.token } : {},
        credentials: 'include',
      })
      if (!res.ok) throw { status: res.status }
      this.cards = this.cards.filter((c) => c.id !== cardId)
    },

    async attachEvidenceLink(sprintId: number, cardId: number, url: string): Promise<EvidenceLink> {
      const guestStore = useGuestStore()
      const res = await fetch(`/api/sprints/${sprintId}/cards/${cardId}/evidence-links`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(guestStore.token ? { 'X-Guest-Token': guestStore.token } : {}),
        },
        credentials: 'include',
        body: JSON.stringify({ url }),
      })
      if (!res.ok) throw { status: res.status, body: await res.json().catch(() => ({})) }
      return (await res.json()) as EvidenceLink
    },

    async detachEvidenceLink(sprintId: number, cardId: number, evidenceLinkId: number): Promise<void> {
      const guestStore = useGuestStore()
      const res = await fetch(`/api/sprints/${sprintId}/cards/${cardId}/evidence-links/${evidenceLinkId}`, {
        method: 'DELETE',
        headers: guestStore.token ? { 'X-Guest-Token': guestStore.token } : {},
        credentials: 'include',
      })
      if (!res.ok) throw { status: res.status }
    },
  },
})
