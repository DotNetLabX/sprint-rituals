import type { SprintItem, SprintSummaryResponse, DeveloperThroughputResponse, ScopeChangeResponse, CarryOverResponse, BugRatioResponse, EpicProgressResponse, CycleTimeResponse, LeaderboardResponse, QaMetricsResponse, UntestedTicketsResponse, FailingTicketsResponse, DeveloperQualityResponse, QaWorkloadResponse, TestTimelineResponse, QaTrendsResponse, DeveloperProgressResponse, DeveloperDetailResponse } from '../types'
import { apiFetch } from './client'

export interface UpdateSprintRequest {
  name: string
  startDate: string
  endDate: string
  goal?: string
}

export interface UpdateSprintResponse {
  id: number
  name: string
  startDate: string
  endDate: string
  goal?: string
  state: string
}

export function getSprintSummary(sprintId?: number, subTeam?: string): Promise<SprintSummaryResponse> {
  const params = new URLSearchParams()
  if (sprintId !== undefined) params.set('sprintId', String(sprintId))
  if (subTeam) params.set('subTeam', subTeam)
  const query = params.toString()
  return apiFetch<SprintSummaryResponse>(`/analytics/sprint-summary${query ? `?${query}` : ''}`)
}

export function getSprints(): Promise<SprintItem[]> {
  return apiFetch<SprintItem[]>('/sprints')
}

export function getSubTeams(): Promise<string[]> {
  return apiFetch<string[]>('/developers/sub-teams')
}

export function getDeveloperThroughput(sprintId?: number, last?: number, subTeam?: string): Promise<DeveloperThroughputResponse> {
  const params = new URLSearchParams()
  if (sprintId !== undefined) params.set('sprintId', String(sprintId))
  if (last !== undefined) params.set('last', String(last))
  if (subTeam) params.set('subTeam', subTeam)
  const query = params.toString()
  return apiFetch<DeveloperThroughputResponse>(`/analytics/developer-throughput${query ? `?${query}` : ''}`)
}

export function getScopeChange(sprintId?: number, last?: number, subTeam?: string): Promise<ScopeChangeResponse> {
  const params = new URLSearchParams()
  if (sprintId !== undefined) params.set('sprintId', String(sprintId))
  if (last !== undefined) params.set('last', String(last))
  if (subTeam) params.set('subTeam', subTeam)
  const query = params.toString()
  return apiFetch<ScopeChangeResponse>(`/analytics/scope-change${query ? `?${query}` : ''}`)
}

export function getCarryOver(sprintId?: number, last?: number, subTeam?: string): Promise<CarryOverResponse> {
  const params = new URLSearchParams()
  if (sprintId !== undefined) params.set('sprintId', String(sprintId))
  if (last !== undefined) params.set('last', String(last))
  if (subTeam) params.set('subTeam', subTeam)
  const query = params.toString()
  return apiFetch<CarryOverResponse>(`/analytics/carry-over${query ? `?${query}` : ''}`)
}

export function getBugRatio(sprintId?: number, last?: number, subTeam?: string): Promise<BugRatioResponse> {
  const params = new URLSearchParams()
  if (sprintId !== undefined) params.set('sprintId', String(sprintId))
  if (last !== undefined) params.set('last', String(last))
  if (subTeam) params.set('subTeam', subTeam)
  const query = params.toString()
  return apiFetch<BugRatioResponse>(`/analytics/bug-ratio${query ? `?${query}` : ''}`)
}

export function getEpicProgress(subTeam?: string): Promise<EpicProgressResponse> {
  const params = new URLSearchParams()
  if (subTeam) params.set('subTeam', subTeam)
  const query = params.toString()
  return apiFetch<EpicProgressResponse>(`/analytics/epic-progress${query ? `?${query}` : ''}`)
}

export function updateSprint(sprintId: number, req: UpdateSprintRequest): Promise<UpdateSprintResponse> {
  return apiFetch<UpdateSprintResponse>(`/sprints/${sprintId}`, {
    method: 'PUT',
    body: JSON.stringify(req)
  })
}

export function getCycleTime(sprintId?: number, last?: number, subTeam?: string): Promise<CycleTimeResponse> {
  const params = new URLSearchParams()
  if (sprintId !== undefined) params.set('sprintId', String(sprintId))
  if (last !== undefined) params.set('last', String(last))
  if (subTeam) params.set('subTeam', subTeam)
  const query = params.toString()
  return apiFetch<CycleTimeResponse>(`/analytics/cycle-time${query ? `?${query}` : ''}`)
}

export function getLeaderboard(sprintId?: number, last?: number, subTeam?: string): Promise<LeaderboardResponse> {
  const params = new URLSearchParams()
  if (sprintId !== undefined) params.set('sprintId', String(sprintId))
  if (last !== undefined) params.set('last', String(last))
  if (subTeam) params.set('subTeam', subTeam)
  const query = params.toString()
  return apiFetch<LeaderboardResponse>(`/analytics/leaderboard${query ? `?${query}` : ''}`)
}

export function getQaMetrics(sprintId: number, subTeam?: string): Promise<QaMetricsResponse> {
  const params = new URLSearchParams()
  if (subTeam) params.set('subTeam', subTeam)
  const query = params.toString()
  return apiFetch<QaMetricsResponse>(`/sprints/${sprintId}/qa-metrics${query ? `?${query}` : ''}`)
}

export function getUntestedTickets(sprintId: number, subTeam?: string): Promise<UntestedTicketsResponse> {
  const params = new URLSearchParams()
  if (subTeam) params.set('subTeam', subTeam)
  const query = params.toString()
  return apiFetch<UntestedTicketsResponse>(`/sprints/${sprintId}/qa-metrics/untested${query ? `?${query}` : ''}`)
}

export function getFailingTickets(sprintId: number, subTeam?: string): Promise<FailingTicketsResponse> {
  const params = new URLSearchParams()
  if (subTeam) params.set('subTeam', subTeam)
  const query = params.toString()
  return apiFetch<FailingTicketsResponse>(`/sprints/${sprintId}/qa-metrics/failing${query ? `?${query}` : ''}`)
}

export function getDeveloperQuality(sprintId?: number, last?: number, subTeam?: string): Promise<DeveloperQualityResponse> {
  const params = new URLSearchParams()
  if (sprintId !== undefined) params.set('sprintId', String(sprintId))
  if (last !== undefined) params.set('last', String(last))
  if (subTeam) params.set('subTeam', subTeam)
  const query = params.toString()
  return apiFetch<DeveloperQualityResponse>(`/analytics/developer-quality${query ? `?${query}` : ''}`)
}

export function getQaWorkload(sprintId?: number, last?: number, subTeam?: string): Promise<QaWorkloadResponse> {
  const params = new URLSearchParams()
  if (sprintId !== undefined) params.set('sprintId', String(sprintId))
  if (last !== undefined) params.set('last', String(last))
  if (subTeam) params.set('subTeam', subTeam)
  const query = params.toString()
  return apiFetch<QaWorkloadResponse>(`/analytics/qa-workload${query ? `?${query}` : ''}`)
}

export function getTestTimeline(sprintId: number, subTeam?: string): Promise<TestTimelineResponse> {
  const params = new URLSearchParams()
  if (subTeam) params.set('subTeam', subTeam)
  const query = params.toString()
  return apiFetch<TestTimelineResponse>(`/sprints/${sprintId}/test-timeline${query ? `?${query}` : ''}`)
}

export function getQaTrends(last?: number, subTeam?: string): Promise<QaTrendsResponse> {
  const params = new URLSearchParams()
  if (last !== undefined) params.set('last', String(last))
  if (subTeam) params.set('subTeam', subTeam)
  const query = params.toString()
  return apiFetch<QaTrendsResponse>(`/analytics/qa-trends${query ? `?${query}` : ''}`)
}

export function getDeveloperProgress(subTeam?: string): Promise<DeveloperProgressResponse> {
  const params = new URLSearchParams()
  if (subTeam) params.set('subTeam', subTeam)
  const query = params.toString()
  return apiFetch<DeveloperProgressResponse>(`/analytics/developer-progress${query ? `?${query}` : ''}`)
}

export function getDeveloperDetail(accountId: string, last?: number): Promise<DeveloperDetailResponse> {
  const params = new URLSearchParams()
  if (last !== undefined) params.set('last', String(last))
  const query = params.toString()
  return apiFetch<DeveloperDetailResponse>(`/analytics/developer-detail/${encodeURIComponent(accountId)}${query ? `?${query}` : ''}`)
}
