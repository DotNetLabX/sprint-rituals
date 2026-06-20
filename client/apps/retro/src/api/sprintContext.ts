import { useGuestStore } from '../stores/guest'
import type { SprintContextResponse } from '../types/sprintContext'

export async function getSprintContext(retroSessionId: number): Promise<SprintContextResponse> {
  const guestStore = useGuestStore()
  const headers: Record<string, string> = {}
  if (guestStore.token) headers['X-Guest-Token'] = guestStore.token

  const res = await fetch(`/api/retro-sessions/${retroSessionId}/sprint-context`, {
    headers,
    credentials: 'include',
  })

  if (res.status === 404) {
    throw Object.assign(new Error('Sprint context not found'), { status: 404 })
  }
  if (!res.ok) {
    throw Object.assign(new Error('Failed to load sprint context'), { status: res.status })
  }
  return (await res.json()) as SprintContextResponse
}
