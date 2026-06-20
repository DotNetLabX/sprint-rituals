import { defineStore } from 'pinia'

interface Sprint {
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

interface SprintSummary {
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

interface CreateSprintPayload {
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

export const useSprintStore = defineStore('sprint', {
  state: () => ({
    sprints: [] as Sprint[],
    currentSprint: null as Sprint | null,
    currentSummary: null as SprintSummary | null,
    loaded: false,
  }),
  actions: {
    async fetchTeamSprints(teamId: number) {
      const res = await fetch(`/api/teams/${teamId}/sprints`, { credentials: 'include' })
      this.sprints = res.ok ? ((await res.json()) as Sprint[]) : []
      this.loaded = true
    },
    async fetchSprint(teamId: number, sprintId: number) {
      const res = await fetch(`/api/teams/${teamId}/sprints/${sprintId}`, { credentials: 'include' })
      if (res.ok) {
        this.currentSprint = (await res.json()) as Sprint
      }
    },
    async fetchSprintSummary(sprintId: number) {
      const res = await fetch(`/api/sprints/${sprintId}/summary`)
      if (res.ok) {
        this.currentSummary = (await res.json()) as SprintSummary
      }
    },
    async createSprint(teamId: number, payload: CreateSprintPayload): Promise<Sprint> {
      const res = await fetch(`/api/teams/${teamId}/sprints`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      })
      if (!res.ok) throw await res.json().catch(() => ({}))
      const sprint = (await res.json()) as Sprint
      this.sprints.unshift(sprint)
      return sprint
    },
    async updateColumns(sprintId: number, payload: ColumnConfigPayload): Promise<Sprint> {
      const res = await fetch(`/api/sprints/${sprintId}/columns`, {
        method: 'PUT',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      })
      if (!res.ok) throw await res.json().catch(() => ({}))
      const sprint = (await res.json()) as Sprint
      this._replaceSprint(sprint)
      if (this.currentSummary?.id === sprint.id) {
        this.currentSummary.column1Name = sprint.column1Name
        this.currentSummary.column1Color = sprint.column1Color
        this.currentSummary.column2Name = sprint.column2Name
        this.currentSummary.column2Color = sprint.column2Color
        this.currentSummary.column3Name = sprint.column3Name
        this.currentSummary.column3Color = sprint.column3Color
      }
      return sprint
    },
    async startSprint(teamId: number, sprintId: number) {
      const res = await fetch(`/api/teams/${teamId}/sprints/${sprintId}/start`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: '{}',
      })
      if (!res.ok) throw await res.json().catch(() => ({}))
      const sprint = (await res.json()) as Sprint
      this._replaceSprint(sprint)
    },
    async advancePhaseCheck(teamId: number, sprintId: number): Promise<AdvancePhaseCheckResponse> {
      const res = await fetch(`/api/teams/${teamId}/sprints/${sprintId}/advance-phase/check`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: '{}',
      })
      if (!res.ok) throw await res.json().catch(() => ({}))
      return (await res.json()) as AdvancePhaseCheckResponse
    },
    async advancePhase(teamId: number, sprintId: number) {
      const res = await fetch(`/api/teams/${teamId}/sprints/${sprintId}/advance-phase`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: '{}',
      })
      if (!res.ok) throw await res.json().catch(() => ({}))
      const sprint = (await res.json()) as Sprint
      this._replaceSprint(sprint)
    },
    async closeSprint(teamId: number, sprintId: number) {
      const res = await fetch(`/api/teams/${teamId}/sprints/${sprintId}/close`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: '{}',
      })
      if (!res.ok) throw await res.json().catch(() => ({}))
      const sprint = (await res.json()) as Sprint
      this._replaceSprint(sprint)
    },
    _replaceSprint(sprint: Sprint) {
      const idx = this.sprints.findIndex((s) => s.id === sprint.id)
      if (idx !== -1) this.sprints[idx] = sprint
      if (this.currentSprint?.id === sprint.id) this.currentSprint = sprint
    },
  },
})
