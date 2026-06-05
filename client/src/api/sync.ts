import { apiFetch } from './client'
import type { JiraSprint, SyncSprintsResponse, SyncBacklogResponse } from '../types'

export function getJiraSprints(boardId: number): Promise<{ sprints: JiraSprint[] }> {
  return apiFetch<{ sprints: JiraSprint[] }>(`/jira/sprints?boardId=${boardId}`)
}

export function syncSprints(fromSprintId: number, toSprintId: number): Promise<SyncSprintsResponse> {
  return apiFetch<SyncSprintsResponse>('/sync/sprints', {
    method: 'POST',
    body: JSON.stringify({ fromSprintId, toSprintId })
  })
}

export function syncBacklog(): Promise<SyncBacklogResponse> {
  return apiFetch<SyncBacklogResponse>('/sync/backlog', {
    method: 'POST',
    body: JSON.stringify({})
  })
}
