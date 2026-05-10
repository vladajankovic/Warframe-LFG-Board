const BASE_URL = '/v1/directory'

export interface AuthCredentials {
  email: string
  password: string
}

export interface LoginResponse {
  token: string
  email: string
  role: string
}

export function getAuthToken(): string | null {
  return localStorage.getItem('auth_token')
}

export async function login(credentials: AuthCredentials): Promise<LoginResponse> {
  const res = await fetch(`${BASE_URL}/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(credentials),
  })
  if (!res.ok) {
    const msg = await res.text()
    throw new Error(msg || 'Invalid credentials.')
  }
  return res.json() as Promise<LoginResponse>
}

export async function register(credentials: AuthCredentials): Promise<void> {
  const res = await fetch(`${BASE_URL}/register`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(credentials),
  })
  if (!res.ok) {
    const msg = await res.text()
    throw new Error(msg || 'Registration failed.')
  }
}
