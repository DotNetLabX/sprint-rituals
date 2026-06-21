import { useGuestStore } from '../stores/guest'
import type {
  SessionResponse,
  SessionSummaryResponse,
  CreateSessionRequest,
  JoinSessionRequest,
  JoinSessionResponse,
  AddSessionItemRequest,
  EstimationItemResponse,
  PokerSprintsResponse,
  PokerTicket,
} from '../types'

// Builds auth headers for requests that may be made by a guest
function authHeaders(guestToken?: string | null): Record<string, string> {
  const headers: Record<string, string> = { 'Content-Type': 'application/json' }
  const token = guestToken ?? useGuestStore().token
  if (token) headers['X-Guest-Token'] = token
  return headers
}

export const sessionsApi = {
  async create(payload: CreateSessionRequest): Promise<{ id: string; roomUrl: string }> {
    const res = await fetch('/api/poker/sessions', {
      method: 'POST',
      credentials: 'include',
      headers: authHeaders(),
      body: JSON.stringify(payload),
    })
    if (!res.ok) throw Object.assign(new Error('Failed to create session'), { status: res.status })
    return res.json() as Promise<{ id: string; roomUrl: string }>
  },

  async get(sessionId: string): Promise<SessionResponse> {
    const res = await fetch(`/api/poker/sessions/${sessionId}`, {
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
    })
    if (!res.ok) throw Object.assign(new Error('Session not found'), { status: res.status })
    return res.json() as Promise<SessionResponse>
  },

  async join(sessionId: string, payload: JoinSessionRequest): Promise<JoinSessionResponse> {
    const res = await fetch(`/api/poker/sessions/${sessionId}/participants`, {
      method: 'POST',
      credentials: 'include',
      headers: authHeaders(),
      body: JSON.stringify(payload),
    })
    if (res.status === 409) throw Object.assign(new Error('Session is closed'), { status: 409 })
    if (!res.ok) throw Object.assign(new Error('Failed to join session'), { status: res.status })
    return res.json() as Promise<JoinSessionResponse>
  },

  async getSummary(sessionId: string): Promise<SessionSummaryResponse> {
    const res = await fetch(`/api/poker/sessions/${sessionId}/summary`, {
      credentials: 'include',
    })
    if (!res.ok) throw Object.assign(new Error('Summary not available'), { status: res.status })
    return res.json() as Promise<SessionSummaryResponse>
  },

  async close(sessionId: string): Promise<void> {
    const res = await fetch(`/api/poker/sessions/${sessionId}/close`, {
      method: 'POST',
      credentials: 'include',
      headers: authHeaders(),
      body: '{}',
    })
    if (!res.ok) throw Object.assign(new Error('Failed to close session'), { status: res.status })
  },

  async addItem(sessionId: string, payload: AddSessionItemRequest): Promise<EstimationItemResponse> {
    const res = await fetch(`/api/poker/sessions/${sessionId}/items`, {
      method: 'POST',
      credentials: 'include',
      headers: authHeaders(),
      body: JSON.stringify(payload),
    })
    if (!res.ok) throw Object.assign(new Error('Failed to add item'), { status: res.status })
    return res.json() as Promise<EstimationItemResponse>
  },
}

export const sprintsApi = {
  async getSprints(): Promise<PokerSprintsResponse> {
    const res = await fetch('/api/poker/sprints', { credentials: 'include' })
    if (!res.ok) throw Object.assign(new Error('Failed to load sprints'), { status: res.status })
    return res.json() as Promise<PokerSprintsResponse>
  },

  async getTickets(sessionId: string): Promise<PokerTicket[]> {
    const res = await fetch(`/api/poker/sessions/${sessionId}/tickets`, {
      credentials: 'include',
      headers: authHeaders(),
    })
    if (!res.ok) throw Object.assign(new Error('Failed to load tickets'), { status: res.status })
    return res.json() as Promise<PokerTicket[]>
  },
}
