import type { TeamRosterResponse, UpdateTeamConfigRequest, TeamDeveloperDto } from '../types'
import { apiFetch } from './client'

export function getTeamRoster(): Promise<TeamRosterResponse> {
  return apiFetch<TeamRosterResponse>('/team')
}

export function updateTeamConfig(accountId: string, req: UpdateTeamConfigRequest): Promise<TeamDeveloperDto> {
  return apiFetch<TeamDeveloperDto>(`/developers/${encodeURIComponent(accountId)}/team-config`, {
    method: 'PUT',
    body: JSON.stringify(req)
  })
}
