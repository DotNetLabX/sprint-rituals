import { defineStore } from 'pinia'
import { useGuestStore } from './guest'
import { useVoteStore } from './vote'

export interface CardGroupCard {
  id: number
  text: string
  createdAt: string
  evidenceLinks: { id: number; url: string; source: string; title: string | null }[]
}

export interface CardGroup {
  id: number
  retroSessionId: number
  columnType: string
  title: string | null
  cards: CardGroupCard[]
  voteCount: number
  isDiscussed: boolean
  isFocused: boolean
}

export const useCardGroupStore = defineStore('cardGroup', {
  state: () => ({
    groups: [] as CardGroup[],
  }),
  getters: {
    column1Groups: (state): CardGroup[] =>
      state.groups.filter((g) => g.columnType === 'Column1'),
    column2Groups: (state): CardGroup[] =>
      state.groups.filter((g) => g.columnType === 'Column2'),
    column3Groups: (state): CardGroup[] =>
      state.groups.filter((g) => g.columnType === 'Column3'),
    focusedGroup: (state): CardGroup | undefined =>
      state.groups.find((g) => g.isFocused),
  },
  actions: {
    async fetchGroups(retroSessionId: number): Promise<void> {
      const guestStore = useGuestStore()
      const headers: Record<string, string> = {}
      if (guestStore.token) headers['X-Guest-Token'] = guestStore.token
      const res = await fetch(`/api/retro-sessions/${retroSessionId}/card-groups`, {
        headers,
        credentials: 'include',
      })
      if (!res.ok) return
      this.groups = (await res.json()) as CardGroup[]
      const voteStore = useVoteStore()
      voteStore.initVoteCounts(this.groups.map((g) => ({ groupId: g.id, voteCount: g.voteCount })))
    },

    async createGroup(retroSessionId: number, cardIds: number[], title?: string): Promise<CardGroup> {
      const guestStore = useGuestStore()
      const headers: Record<string, string> = { 'Content-Type': 'application/json' }
      if (guestStore.token) headers['X-Guest-Token'] = guestStore.token
      const res = await fetch(`/api/retro-sessions/${retroSessionId}/card-groups`, {
        method: 'POST',
        headers,
        credentials: 'include',
        body: JSON.stringify({ cardIds, title }),
      })
      if (!res.ok) throw { status: res.status, body: await res.json().catch(() => ({})) }
      const group = (await res.json()) as CardGroup
      await this.fetchGroups(retroSessionId)
      return group
    },

    async addCardToGroup(retroSessionId: number, groupId: number, cardId: number): Promise<void> {
      const guestStore = useGuestStore()
      const headers: Record<string, string> = { 'Content-Type': 'application/json' }
      if (guestStore.token) headers['X-Guest-Token'] = guestStore.token
      const res = await fetch(`/api/retro-sessions/${retroSessionId}/card-groups/${groupId}/cards`, {
        method: 'POST',
        headers,
        credentials: 'include',
        body: JSON.stringify({ cardId }),
      })
      if (!res.ok) throw { status: res.status, body: await res.json().catch(() => ({})) }
      await this.fetchGroups(retroSessionId)
    },

    async removeCardFromGroup(retroSessionId: number, groupId: number, cardId: number): Promise<void> {
      const guestStore = useGuestStore()
      const headers: Record<string, string> = {}
      if (guestStore.token) headers['X-Guest-Token'] = guestStore.token
      const res = await fetch(`/api/retro-sessions/${retroSessionId}/card-groups/${groupId}/cards/${cardId}`, {
        method: 'DELETE',
        headers,
        credentials: 'include',
      })
      if (!res.ok) throw { status: res.status, body: await res.json().catch(() => ({})) }
      await this.fetchGroups(retroSessionId)
    },

    async dissolveGroup(retroSessionId: number, groupId: number): Promise<void> {
      const guestStore = useGuestStore()
      const headers: Record<string, string> = {}
      if (guestStore.token) headers['X-Guest-Token'] = guestStore.token
      const res = await fetch(`/api/retro-sessions/${retroSessionId}/card-groups/${groupId}`, {
        method: 'DELETE',
        headers,
        credentials: 'include',
      })
      if (!res.ok) throw { status: res.status, body: await res.json().catch(() => ({})) }
      await this.fetchGroups(retroSessionId)
    },

    async focusGroup(retroSessionId: number, groupId: number): Promise<void> {
      const guestStore = useGuestStore()
      const headers: Record<string, string> = { 'Content-Type': 'application/json' }
      if (guestStore.token) headers['X-Guest-Token'] = guestStore.token
      const res = await fetch(`/api/retro-sessions/${retroSessionId}/card-groups/${groupId}/focus`, {
        method: 'POST',
        headers,
        credentials: 'include',
        body: '{}',
      })
      if (!res.ok) throw { status: res.status, body: await res.json().catch(() => ({})) }
    },

    async clearFocus(retroSessionId: number): Promise<void> {
      const guestStore = useGuestStore()
      const headers: Record<string, string> = { 'Content-Type': 'application/json' }
      if (guestStore.token) headers['X-Guest-Token'] = guestStore.token
      const res = await fetch(`/api/retro-sessions/${retroSessionId}/focus`, {
        method: 'DELETE',
        headers,
        credentials: 'include',
        body: '{}',
      })
      if (!res.ok) throw { status: res.status, body: await res.json().catch(() => ({})) }
    },

    async toggleDiscussed(retroSessionId: number, groupId: number): Promise<void> {
      const guestStore = useGuestStore()
      const headers: Record<string, string> = { 'Content-Type': 'application/json' }
      if (guestStore.token) headers['X-Guest-Token'] = guestStore.token
      const res = await fetch(`/api/retro-sessions/${retroSessionId}/card-groups/${groupId}/toggle-discussed`, {
        method: 'POST',
        headers,
        credentials: 'include',
        body: '{}',
      })
      if (!res.ok) throw { status: res.status, body: await res.json().catch(() => ({})) }
    },

    async updateGroupTitle(retroSessionId: number, groupId: number, title: string | null): Promise<void> {
      const guestStore = useGuestStore()
      const headers: Record<string, string> = { 'Content-Type': 'application/json' }
      if (guestStore.token) headers['X-Guest-Token'] = guestStore.token
      const res = await fetch(`/api/retro-sessions/${retroSessionId}/card-groups/${groupId}`, {
        method: 'PATCH',
        headers,
        credentials: 'include',
        body: JSON.stringify({ title }),
      })
      if (!res.ok) throw { status: res.status, body: await res.json().catch(() => ({})) }
      await this.fetchGroups(retroSessionId)
    },
  },
})
