import { defineStore } from 'pinia'

interface RetroSession {
  id: number
  teamId: number
  name: string
  state: string
  currentPhase: string
  votesPerParticipant: number
  createdAt: string
  closedAt: string | null
  column1Name: string
  column1Color: string
  column2Name: string
  column2Color: string
  column3Name: string
  column3Color: string
  writingTimerDefault: number
  votingTimerDefault: number
  discussingTimerDefault: number
}

interface RetroSessionSummary {
  id: number
  teamId: number
  name: string
  state: string
  currentPhase: string
  votesPerParticipant: number
  isScrumMaster: boolean
  participants: { displayName: string }[]
  column1Name: string
  column1Color: string
  column2Name: string
  column2Color: string
  column3Name: string
  column3Color: string
  writingTimerDefault: number
  votingTimerDefault: number
  discussingTimerDefault: number
}

export interface AdvancePhaseCheckResponse {
  currentPhase: string
  nonVotingParticipantCount?: number
  totalParticipantCount?: number
  discussedGroupCount?: number
  focusedGroupId?: number | null
  unreviewedActionCount?: number
  totalPreviousActionCount?: number
}

interface ColumnConfigPayload {
  column1Name: string
  column1Color: string
  column2Name: string
  column2Color: string
  column3Name: string
  column3Color: string
}

interface CreateRetroSessionPayload {
  name: string
  votesPerParticipant?: number
  column1Name?: string
  column1Color?: string
  column2Name?: string
  column2Color?: string
  column3Name?: string
  column3Color?: string
  writingTimerDefault?: number
  votingTimerDefault?: number
  discussingTimerDefault?: number
}

export const useRetroSessionStore = defineStore('retroSession', {
  state: () => ({
    retroSessions: [] as RetroSession[],
    currentRetroSession: null as RetroSession | null,
    currentSummary: null as RetroSessionSummary | null,
    loaded: false,
  }),
  actions: {
    async fetchTeamRetroSessions(teamId: number) {
      const res = await fetch(`/api/teams/${teamId}/retro-sessions`, { credentials: 'include' })
      this.retroSessions = res.ok ? ((await res.json()) as RetroSession[]) : []
      this.loaded = true
    },
    async fetchRetroSession(teamId: number, retroSessionId: number) {
      const res = await fetch(`/api/teams/${teamId}/retro-sessions/${retroSessionId}`, { credentials: 'include' })
      if (res.ok) {
        this.currentRetroSession = (await res.json()) as RetroSession
      }
    },
    async fetchRetroSessionSummary(retroSessionId: number) {
      const res = await fetch(`/api/retro-sessions/${retroSessionId}/summary`)
      if (res.ok) {
        this.currentSummary = (await res.json()) as RetroSessionSummary
      }
    },
    async createRetroSession(teamId: number, payload: CreateRetroSessionPayload): Promise<RetroSession> {
      const res = await fetch(`/api/teams/${teamId}/retro-sessions`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      })
      if (!res.ok) throw await res.json().catch(() => ({}))
      const retroSession = (await res.json()) as RetroSession
      this.retroSessions.unshift(retroSession)
      return retroSession
    },
    async updateColumns(retroSessionId: number, payload: ColumnConfigPayload): Promise<RetroSession> {
      const res = await fetch(`/api/retro-sessions/${retroSessionId}/columns`, {
        method: 'PUT',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      })
      if (!res.ok) throw await res.json().catch(() => ({}))
      const retroSession = (await res.json()) as RetroSession
      this._replaceRetroSession(retroSession)
      if (this.currentSummary?.id === retroSession.id) {
        this.currentSummary.column1Name = retroSession.column1Name
        this.currentSummary.column1Color = retroSession.column1Color
        this.currentSummary.column2Name = retroSession.column2Name
        this.currentSummary.column2Color = retroSession.column2Color
        this.currentSummary.column3Name = retroSession.column3Name
        this.currentSummary.column3Color = retroSession.column3Color
      }
      return retroSession
    },
    async startRetroSession(teamId: number, retroSessionId: number) {
      const res = await fetch(`/api/teams/${teamId}/retro-sessions/${retroSessionId}/start`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: '{}',
      })
      if (!res.ok) throw await res.json().catch(() => ({}))
      const retroSession = (await res.json()) as RetroSession
      this._replaceRetroSession(retroSession)
    },
    async advancePhaseCheck(teamId: number, retroSessionId: number): Promise<AdvancePhaseCheckResponse> {
      const res = await fetch(`/api/teams/${teamId}/retro-sessions/${retroSessionId}/advance-phase/check`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: '{}',
      })
      if (!res.ok) throw await res.json().catch(() => ({}))
      return (await res.json()) as AdvancePhaseCheckResponse
    },
    async advancePhase(teamId: number, retroSessionId: number) {
      const res = await fetch(`/api/teams/${teamId}/retro-sessions/${retroSessionId}/advance-phase`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: '{}',
      })
      if (!res.ok) throw await res.json().catch(() => ({}))
      const retroSession = (await res.json()) as RetroSession
      this._replaceRetroSession(retroSession)
    },
    async closeRetroSession(teamId: number, retroSessionId: number) {
      const res = await fetch(`/api/teams/${teamId}/retro-sessions/${retroSessionId}/close`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: '{}',
      })
      if (!res.ok) throw await res.json().catch(() => ({}))
      const retroSession = (await res.json()) as RetroSession
      this._replaceRetroSession(retroSession)
    },
    _replaceRetroSession(retroSession: RetroSession) {
      const idx = this.retroSessions.findIndex((s) => s.id === retroSession.id)
      if (idx !== -1) this.retroSessions[idx] = retroSession
      if (this.currentRetroSession?.id === retroSession.id) this.currentRetroSession = retroSession
    },
  },
})
