import { parseApiError } from './apiUtils'

const BASE_URL = '/v1/directory'
const WARFRAME_PROFILE_URL = 'https://api.warframestat.us/profile'

interface WarframeUserData {
  user_id?: string
}

interface WarframeProfileData {
  accountId?: string
  displayName?: string
}

export interface User {
  email: string
  role: string
  inGameName: string
  createdAt: string
  lastUpdatedAt: string
  isVerified: boolean
}

export interface UpdateUserPayload {
  email: string
  password?: string
  role?: string
  inGameName?: string
  isVerified?: boolean
}

function authHeaders(): HeadersInit {
  const token = localStorage.getItem('auth_token')
  return {
    'Content-Type': 'application/json',
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  }
}

export async function fetchAllUsers(): Promise<User[]> {
  const res = await fetch(`${BASE_URL}/users`, { headers: authHeaders() })
  if (!res.ok) throw new Error('Failed to fetch users.')
  return res.json() as Promise<User[]>
}

export async function updateUser(payload: UpdateUserPayload): Promise<User> {
  const res = await fetch(`${BASE_URL}/user`, {
    method: 'PATCH',
    headers: authHeaders(),
    body: JSON.stringify(payload),
  })
  if (!res.ok) throw new Error(await parseApiError(res, 'Failed to update user.'))
  return res.json() as Promise<User>
}

export async function changePassword(email: string, oldPassword: string, newPassword: string): Promise<void> {
  const res = await fetch(`${BASE_URL}/user/me/update`, {
    method: 'PATCH',
    headers: authHeaders(),
    body: JSON.stringify({ email, oldPassword, newPassword }),
  })
  if (!res.ok) throw new Error(await parseApiError(res, 'Failed to change password.'))
}

export async function updateInGameName(email: string, inGameName: string): Promise<User> {
  const res = await fetch(`${BASE_URL}/user/me/ingamename`, {
    method: 'PATCH',
    headers: authHeaders(),
    body: JSON.stringify({ email, inGameName }),
  })
  if (!res.ok) throw new Error(await parseApiError(res, 'Failed to update in-game name.'))
  return res.json() as Promise<User>
}

/** Extracts a player ID from pasted Warframe user data and resolves its profile. */
export async function fetchWarframeDisplayName(userDataJson: string): Promise<string> {
  let userData: WarframeUserData
  try {
    userData = JSON.parse(userDataJson) as WarframeUserData
  } catch {
    throw new Error('The pasted Warframe response is not valid JSON.')
  }

  if (!userData.user_id) {
    throw new Error('The response does not contain a user_id. Make sure you are logged in on the Warframe website.')
  }

  let profileResponse: Response
  try {
    profileResponse = await fetch(
      `${WARFRAME_PROFILE_URL}/${encodeURIComponent(userData.user_id)}`,
    )
  } catch {
    throw new Error('Your browser blocked access to the Warframe profile response.')
  }
  if (!profileResponse.ok) {
    throw new Error('Warframe could not load your player profile. Please try again.')
  }

  const profile = await profileResponse.json() as WarframeProfileData
  const displayName = profile.displayName?.trim()
  if (!displayName) {
    throw new Error('Your Warframe profile did not contain a display name.')
  }

  return displayName
}

export async function deleteUser(email: string): Promise<void> {
  const res = await fetch(`${BASE_URL}/user?email=${encodeURIComponent(email)}`, {
    method: 'DELETE',
    headers: authHeaders(),
  })
  if (!res.ok) throw new Error('Failed to delete user.')
}

export async function deleteOwnAccount(email: string, password: string): Promise<void> {
  const res = await fetch(`${BASE_URL}/user/me/delete`, {
    method: 'DELETE',
    headers: authHeaders(),
    body: JSON.stringify({ email, password }),
  })
  if (!res.ok) throw new Error(await parseApiError(res, 'Failed to delete account.'))
}

export interface AuthCredentials {
  email: string
  password: string
}

export interface LoginResponse {
  userId: string
  token: string
  email: string
  role: string
  inGameName: string
  subscriptionStatus: 'Active' | 'Inactive'
  subscriptionExpiryDate: string | null
}

export interface SubscriptionInfo {
  subscriptionStatus: 'Active' | 'Inactive'
  subscriptionExpiryDate: string | null
  token?: string
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
  if (!res.ok) throw new Error(await parseApiError(res, 'Invalid credentials.'))
  return res.json() as Promise<LoginResponse>
}

export async function register(credentials: AuthCredentials): Promise<void> {
  const res = await fetch(`${BASE_URL}/register`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(credentials),
  })
  if (!res.ok) throw new Error(await parseApiError(res, 'Registration failed.'))
}

export async function getSubscription(): Promise<SubscriptionInfo> {
  const res = await fetch(`${BASE_URL}/user/me/subscription`, { headers: authHeaders() })
  if (!res.ok) throw new Error(await parseApiError(res, 'Failed to fetch subscription.'))
  return res.json() as Promise<SubscriptionInfo>
}

export async function activateSubscription(): Promise<SubscriptionInfo> {
  const res = await fetch(`${BASE_URL}/user/me/subscription/activate`, {
    method: 'POST',
    headers: authHeaders(),
  })
  if (!res.ok) throw new Error(await parseApiError(res, 'Failed to activate subscription.'))
  return res.json() as Promise<SubscriptionInfo>
}

export async function cancelSubscription(): Promise<SubscriptionInfo> {
  const res = await fetch(`${BASE_URL}/user/me/subscription/cancel`, {
    method: 'DELETE',
    headers: authHeaders(),
  })
  if (!res.ok) throw new Error(await parseApiError(res, 'Failed to cancel subscription.'))
  return res.json() as Promise<SubscriptionInfo>
}
