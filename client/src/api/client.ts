const BASE_URL = '/api'

export async function apiFetch<T>(path: string, options?: RequestInit): Promise<T> {
  const headers: Record<string, string> = { ...options?.headers as Record<string, string> }
  if (options?.body) {
    headers['Content-Type'] = headers['Content-Type'] ?? 'application/json'
  }

  const response = await fetch(`${BASE_URL}${path}`, {
    ...options,
    headers
  })

  if (response.status === 401 && path !== '/auth/me') {
    window.location.href = '/login'
    return undefined as unknown as T
  }

  if (!response.ok) {
    const errorText = await response.text()
    throw new Error(`API error ${response.status}: ${errorText}`)
  }

  if (response.status === 204) {
    return undefined as unknown as T
  }

  return response.json()
}
