import { defineStore } from 'pinia'
import { useGuestStore } from './guest'

interface Participant {
  id: number
  retroSessionId: number
  displayName: string
  joinedAt: string
}

interface JoinRetroResponse extends Participant {
  guestToken: string
  userId?: number | null
}

export const useParticipantStore = defineStore('participant', {
  state: () => ({
    currentParticipant: null as Participant | null,
    participants: [] as Participant[],
  }),
  actions: {
    async join(retroSessionId: number, displayName: string): Promise<JoinRetroResponse> {
      const res = await fetch(`/api/retro-sessions/${retroSessionId}/join`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ displayName }),
      })
      if (!res.ok) throw { status: res.status, body: await res.json().catch(() => ({})) }
      const data = (await res.json()) as JoinRetroResponse
      this.currentParticipant = data
      return data
    },
    async joinWithGoogle(retroSessionId: number): Promise<JoinRetroResponse> {
      const res = await fetch(`/api/retro-sessions/${retroSessionId}/join`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: '{}',
        credentials: 'include',
      })
      if (!res.ok) throw { status: res.status, body: await res.json().catch(() => ({})) }
      const data = (await res.json()) as JoinRetroResponse
      this.currentParticipant = data
      return data
    },
    async fetchMe(retroSessionId: number): Promise<void> {
      const guestStore = useGuestStore()
      const res = await fetch(`/api/retro-sessions/${retroSessionId}/participants/me`, {
        headers: guestStore.token ? { 'X-Guest-Token': guestStore.token } : {},
      })
      if (!res.ok) throw { status: res.status }
      this.currentParticipant = (await res.json()) as Participant
    },
    async fetchParticipants(retroSessionId: number): Promise<void> {
      const guestStore = useGuestStore()
      const headers: Record<string, string> = {}
      if (guestStore.token) headers['X-Guest-Token'] = guestStore.token
      const res = await fetch(`/api/retro-sessions/${retroSessionId}/participants`, { headers, credentials: 'include' })
      if (!res.ok) return
      this.participants = (await res.json()) as Participant[]
    },
    async kickParticipant(retroSessionId: number, participantId: number): Promise<void> {
      const res = await fetch(`/api/retro-sessions/${retroSessionId}/participants/${participantId}`, {
        method: 'DELETE',
        credentials: 'include',
      })
      if (!res.ok) throw { status: res.status, body: await res.json().catch(() => ({})) }
      this.participants = this.participants.filter((p) => p.id !== participantId)
    },
  },
})
