import { defineStore } from 'pinia'
import { useGuestStore } from './guest'

export interface VoteDistribution {
  groupId: number
  count: number
}

export const useVoteStore = defineStore('vote', {
  state: () => ({
    myVotes: [] as VoteDistribution[],
    voteCounts: new Map<number, number>(),
    votesPerParticipant: 3,
  }),
  getters: {
    remainingBudget: (state): number => {
      const used = state.myVotes.reduce((sum, v) => sum + v.count, 0)
      return state.votesPerParticipant - used
    },
    myVotesOnGroup:
      (state) =>
      (groupId: number): number =>
        state.myVotes.find((v) => v.groupId === groupId)?.count ?? 0,
    voteCountForGroup:
      (state) =>
      (groupId: number): number =>
        state.voteCounts.get(groupId) ?? 0,
  },
  actions: {
    async fetchMyVotes(retroSessionId: number): Promise<void> {
      const guestStore = useGuestStore()
      if (!guestStore.token) return
      const res = await fetch(`/api/retro-sessions/${retroSessionId}/votes/mine`, {
        headers: { 'X-Guest-Token': guestStore.token },
      })
      if (!res.ok) return
      this.myVotes = (await res.json()) as VoteDistribution[]
    },

    async castVote(retroSessionId: number, groupId: number): Promise<void> {
      const guestStore = useGuestStore()
      if (!guestStore.token) return

      const prev = this.myVotes.find((v) => v.groupId === groupId)
      const prevCount = prev?.count ?? 0
      const prevTotal = this.voteCounts.get(groupId) ?? 0

      if (prev) {
        prev.count++
      } else {
        this.myVotes.push({ groupId, count: 1 })
      }
      this.voteCounts.set(groupId, prevTotal + 1)

      try {
        const res = await fetch(`/api/retro-sessions/${retroSessionId}/card-groups/${groupId}/votes`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json', 'X-Guest-Token': guestStore.token },
          body: JSON.stringify({}),
        })
        if (!res.ok) throw { status: res.status, body: await res.json().catch(() => ({})) }
        const data = await res.json()
        this.voteCounts.set(groupId, data.voteCount)
        const entry = this.myVotes.find((v) => v.groupId === groupId)
        if (entry) entry.count = data.myVoteCount
      } catch (err) {
        const entry = this.myVotes.find((v) => v.groupId === groupId)
        if (entry) {
          entry.count = prevCount
          if (entry.count === 0) this.myVotes = this.myVotes.filter((v) => v.groupId !== groupId)
        }
        this.voteCounts.set(groupId, prevTotal)
        throw err
      }
    },

    async retractVote(retroSessionId: number, groupId: number): Promise<void> {
      const guestStore = useGuestStore()
      if (!guestStore.token) return

      const prev = this.myVotes.find((v) => v.groupId === groupId)
      const prevCount = prev?.count ?? 0
      const prevTotal = this.voteCounts.get(groupId) ?? 0

      if (prev && prev.count > 1) {
        prev.count--
      } else {
        this.myVotes = this.myVotes.filter((v) => v.groupId !== groupId)
      }
      this.voteCounts.set(groupId, Math.max(0, prevTotal - 1))

      try {
        const res = await fetch(`/api/retro-sessions/${retroSessionId}/card-groups/${groupId}/votes`, {
          method: 'DELETE',
          headers: { 'X-Guest-Token': guestStore.token },
        })
        if (!res.ok) throw { status: res.status, body: await res.json().catch(() => ({})) }
        const data = await res.json()
        this.voteCounts.set(groupId, data.voteCount)
        const entry = this.myVotes.find((v) => v.groupId === groupId)
        if (entry) entry.count = data.myVoteCount
        else if (data.myVoteCount > 0) this.myVotes.push({ groupId, count: data.myVoteCount })
      } catch (err) {
        const entry = this.myVotes.find((v) => v.groupId === groupId)
        if (entry) {
          entry.count = prevCount
        } else if (prevCount > 0) {
          this.myVotes.push({ groupId, count: prevCount })
        }
        this.voteCounts.set(groupId, prevTotal)
        throw err
      }
    },

    setVoteCount(groupId: number, count: number): void {
      this.voteCounts.set(groupId, count)
    },

    initVoteCounts(counts: { groupId: number; voteCount: number }[]): void {
      for (const { groupId, voteCount } of counts) {
        this.voteCounts.set(groupId, voteCount)
      }
    },
  },
})
