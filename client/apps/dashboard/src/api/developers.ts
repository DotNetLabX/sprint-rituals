import type { CapacityEntry, Developer, SetCapacityResponse } from '../types'
import { apiFetch } from './client'

export function getDevelopers(): Promise<Developer[]> {
  return apiFetch<Developer[]>('/developers')
}

export function setDeveloperCapacity(accountId: string, sprintId: number, capacityPercent: number): Promise<SetCapacityResponse> {
  return apiFetch<SetCapacityResponse>(`/developers/${encodeURIComponent(accountId)}/capacity`, {
    method: 'PUT',
    body: JSON.stringify({ sprintId, capacityPercent })
  })
}

export function getDeveloperCapacity(accountId: string, sprintId?: number): Promise<CapacityEntry[]> {
  const params = new URLSearchParams()
  if (sprintId !== undefined) params.set('sprintId', String(sprintId))
  const query = params.toString()
  return apiFetch<CapacityEntry[]>(`/developers/${encodeURIComponent(accountId)}/capacity${query ? `?${query}` : ''}`)
}
