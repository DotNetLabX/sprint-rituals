export interface ScopeAdditionDto {
  ticketKey: string
  summary: string
  storyPoints?: number | null
}

export interface SprintContextSection {
  sprintId: number
  sprintName?: string | null
  startDate?: string | null
  endDate?: string | null
  freshness: 'live' | 'cached' | 'unavailable'
  completionRate?: number | null
  completionRag?: string | null
  disruptionRate?: number | null
  disruptionRag?: string | null
  carryOverRate?: number | null
  carryOverRag?: string | null
  healthScore?: number | null
  healthRag?: string | null
  addedMidSprint?: ScopeAdditionDto[] | null
  addedMidSprintOverflow?: number | null
}

export interface SprintContextResponse {
  sprints: SprintContextSection[]
}
