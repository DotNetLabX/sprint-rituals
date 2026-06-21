// ---- Poker API types ----

export type SessionMode = 'SprintBound' | 'Free'
export type SessionStatus = 'Active' | 'Closed'
export type ItemStatus = 'Voting' | 'Revealed' | 'Decided' | 'Skipped'

export const FIBONACCI_SCALE = ['1', '2', '3', '5', '8', '13', '21', '?'] as const
export type FibonacciValue = (typeof FIBONACCI_SCALE)[number]

export interface ParticipantResponse {
  id: string
  displayName: string
  isVoter: boolean
  personId: number | null
  // guestToken intentionally omitted — server strips it from list responses (security: Fix 3)
}

export interface VoteResponse {
  participantId: string
  value: string
}

export interface EstimationItemResponse {
  id: string
  label: string
  ticketKey: string | null
  status: ItemStatus
  finalEstimate: string | null
  order: number
  votes: VoteResponse[]
}

export interface SessionResponse {
  id: string
  mode: SessionMode
  status: SessionStatus
  sprintId: number | null
  sprintName: string | null
  title: string | null
  createdByPersonId: number
  createdAt: string
  closedAt: string | null
  participants: ParticipantResponse[]
  items: EstimationItemResponse[]
}

export interface SessionSummaryResponse {
  id: string
  mode: SessionMode
  sprintName: string | null
  title: string | null
  itemsEstimated: number
  itemsSkipped: number
  totalStoryPoints: number
  closedAt: string | null
  items: EstimationItemResponse[]
}

export interface PokerSprintInfo {
  sprintId: number
  name: string
  startDate: string | null
  endDate: string | null
  state: string
}

export interface PokerSprintsResponse {
  sprints: PokerSprintInfo[]
  defaultSprintId: number | null
}

export interface PokerTicket {
  key: string
  summary: string
  storyPoints: number | null
}

export interface CreateSessionRequest {
  mode: SessionMode
  sprintId?: number | null
  sprintName?: string | null
  title?: string | null
}

export interface JoinSessionRequest {
  displayName: string
}

export interface JoinSessionResponse {
  participantId: string
  guestToken: string
  displayName: string
  sessionId: string
}

export interface AddSessionItemRequest {
  label: string
}

// ---- SignalR broadcast message shapes ----

export interface ParticipantJoinedPayload {
  participantId: string
  displayName: string
}

export interface ParticipantLeftPayload {
  participantId: string
}

export interface VoteReceivedPayload {
  participantId: string
  itemId: string
}

export interface ItemSelectedPayload {
  sessionId: string
  itemId: string
  label: string
}

export interface VotesRevealedPayload {
  sessionId: string
  itemId: string
  votes: Array<{ participantId: string; displayName: string; value: string }>
}

export interface EstimateDecidedPayload {
  sessionId: string
  itemId: string
  finalEstimate: string
}

export interface ItemSkippedPayload {
  sessionId: string
  itemId: string
}

export interface SessionClosedPayload {
  sessionId: string
  itemsEstimated: number
  itemsSkipped: number
  totalStoryPoints: number
}

export interface WriteBackFailedPayload {
  itemId: string
  reason: string
}
