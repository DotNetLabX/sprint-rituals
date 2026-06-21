// Shared auth-state helper — wraps GET /api/auth/me against the Identity cookie.
// Both apps delegate here for the signed-in user object.
// Guest auth (X-Guest-Token, per-sprint localStorage tokens) is NOT shared — stays in apps/retro.

export interface AuthUser {
  /** Person id from Identity — matches SessionResponse.createdByPersonId for SM check. */
  id: number
  email: string
  displayName: string
  givenName?: string
  role?: string
}

export async function fetchMe(): Promise<AuthUser | null> {
  const res = await fetch('/api/auth/me', {
    // Explicit same-origin so behavior is consistent across both apps regardless of
    // their individual fetch defaults (dashboard omits credentials; retro sends 'include').
    credentials: 'same-origin',
  })
  if (!res.ok) return null
  return res.json() as Promise<AuthUser>
}
