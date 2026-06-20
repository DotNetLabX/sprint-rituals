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
  sprintId: number
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
    async fetchGroups(sprintId: number): Promise<void> {
      const guestStore = useGuestStore()
      const headers: Record<string, string> = {}
      if (guestStore.token) headers['X-Guest-Token'] = guestStore.token
      const res = await fetch(`/api/sprints/${sprintId}/card-groups`, {
        headers,
        credentials: 'include',
      })
      if (!res.ok) return
      this.groups = (await res.json()) as CardGroup[]
      const voteStore = useVoteStore()
      voteStore.initVoteCounts(this.groups.map((g) => ({ groupId: g.id, voteCount: g.voteCount })))
    },

    async createGroup(sprintId: number, cardIds: number[], title?: string): Promise<CardGroup> {
      const guestStore = useGuestStore()
      const headers: Record<string, string> = { 'Content-Type': 'application/json' }
      if (guestStore.token) headers['X-Guest-Token'] = guestStore.token
      const res = await fetch(`/api/sprints/${sprintId}/card-groups`, {
        method: 'POST',
        headers,
        credentials: 'include',
        body: JSON.stringify({ cardIds, title }),
      })
      if (!res.ok) throw { status: res.status, body: await res.json().catch(() => ({})) }
      const group = (await res.json()) as CardGroup
      await this.fetchGroups(sprintId)
      return group
    },

    async addCardToGroup(sprintId: number, groupId: number, cardId: number): Promise<void> {
      const guestStore = useGuestStore()
      const headers: Record<string, string> = { 'Content-Type': 'application/json' }
      if (guestStore.token) headers['X-Guest-Token'] = guestStore.token
      const res = await fetch(`/api/sprints/${sprintId}/card-groups/${groupId}/cards`, {
        method: 'POST',
        headers,
        credentials: 'include',
        body: JSON.stringify({ cardId }),
      })
      if (!res.ok) throw { status: res.status, body: await res.json().catch(() => ({})) }
      await this.fetchGroups(sprintId)
    },

    async removeCardFromGroup(sprintId: number, groupId: number, cardId: number): Promise<void> {
      const guestStore = useGuestStore()
      const headers: Record<string, string> = {}
      if (guestStore.token) headers['X-Guest-Token'] = guestStore.token
      const res = await fetch(`/api/sprints/${sprintId}/card-groups/${groupId}/cards/${cardId}`, {
        method: 'DELETE',
        headers,
        credentials: 'include',
      })
      if (!res.ok) throw { status: res.status, body: await res.json().catch(() => ({})) }
      await this.fetchGroups(sprintId)
    },

    async dissolveGroup(sprintId: number, groupId: number): Promise<void> {
      const guestStore = useGuestStore()
      const headers: Record<string, string> = {}
      if (guestStore.token) headers['X-Guest-Token'] = guestStore.token
      const res = await fetch(`/api/sprints/${sprintId}/card-groups/${groupId}`, {
        method: 'DELETE',
        headers,
        credentials: 'include',
      })
      if (!res.ok) throw { status: res.status, body: await res.json().catch(() => ({})) }
      await this.fetchGroups(sprintId)
    },

    async focusGroup(sprintId: number, groupId: number): Promise<void> {
      const guestStore = useGuestStore()
      const headers: Record<string, string> = { 'Content-Type': 'application/json' }
      if (guestStore.token) headers['X-Guest-Token'] = guestStore.token
      const res = await fetch(`/api/sprints/${sprintId}/card-groups/${groupId}/focus`, {
        method: 'POST',
        headers,
        credentials: 'include',
        body: '{}',
      })
      if (!res.ok) throw { status: res.status, body: await res.json().catch(() => ({})) }
    },

    async clearFocus(sprintId: number): Promise<void> {
      const guestStore = useGuestStore()
      const headers: Record<string, string> = { 'Content-Type': 'application/json' }
      if (guestStore.token) headers['X-Guest-Token'] = guestStore.token
      const res = await fetch(`/api/sprints/${sprintId}/focus`, {
        method: 'DELETE',
        headers,
        credentials: 'include',
        body: '{}',
      })
      if (!res.ok) throw { status: res.status, body: await res.json().catch(() => ({})) }
    },

    async toggleDiscussed(sprintId: number, groupId: number): Promise<void> {
      const guestStore = useGuestStore()
      const headers: Record<string, string> = { 'Content-Type': 'application/json' }
      if (guestStore.token) headers['X-Guest-Token'] = guestStore.token
      const res = await fetch(`/api/sprints/${sprintId}/card-groups/${groupId}/toggle-discussed`, {
        method: 'POST',
        headers,
        credentials: 'include',
        body: '{}',
      })
      if (!res.ok) throw { status: res.status, body: await res.json().catch(() => ({})) }
    },

    async updateGroupTitle(sprintId: number, groupId: number, title: string | null): Promise<void> {
      const guestStore = useGuestStore()
      const headers: Record<string, string> = { 'Content-Type': 'application/json' }
      if (guestStore.token) headers['X-Guest-Token'] = guestStore.token
      const res = await fetch(`/api/sprints/${sprintId}/card-groups/${groupId}`, {
        method: 'PATCH',
        headers,
        credentials: 'include',
        body: JSON.stringify({ title }),
      })
      if (!res.ok) throw { status: res.status, body: await res.json().catch(() => ({})) }
      await this.fetchGroups(sprintId)
    },
  },
})
