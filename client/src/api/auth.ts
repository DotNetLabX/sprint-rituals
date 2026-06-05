import type { AuthUser, UserEntry, InvitationEntry, CreateInvitationRequest, CreateInvitationResponse, InviteValidation } from '../types'
import { apiFetch } from './client'

export function getMe(): Promise<AuthUser> {
  return apiFetch<AuthUser>('/auth/me')
}

export function logout(): Promise<void> {
  return apiFetch<void>('/auth/logout', { method: 'POST' })
}

export function getUsers(): Promise<UserEntry[]> {
  return apiFetch<UserEntry[]>('/users')
}

export function updateUserRole(id: number, role: string): Promise<UserEntry> {
  return apiFetch<UserEntry>(`/users/${id}/role`, {
    method: 'PUT',
    body: JSON.stringify({ role })
  })
}

export function updateUserStatus(id: number, isActive: boolean): Promise<UserEntry> {
  return apiFetch<UserEntry>(`/users/${id}/status`, {
    method: 'PUT',
    body: JSON.stringify({ isActive })
  })
}

export function createInvitation(req: CreateInvitationRequest): Promise<CreateInvitationResponse> {
  return apiFetch<CreateInvitationResponse>('/invitations', {
    method: 'POST',
    body: JSON.stringify(req)
  })
}

export function getInvitations(): Promise<InvitationEntry[]> {
  return apiFetch<InvitationEntry[]>('/invitations')
}

export function revokeInvitation(id: number): Promise<void> {
  return apiFetch<void>(`/invitations/${id}`, { method: 'DELETE' })
}

export function validateInvitation(token: string): Promise<InviteValidation> {
  return apiFetch<InviteValidation>(`/invitations/${encodeURIComponent(token)}/validate`)
}
