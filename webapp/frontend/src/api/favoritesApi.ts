import { authHeaders } from './itemsApi'
import { parseApiError } from './apiUtils'

const BASE_URL = '/v1/user-notification/favorites'
export const FAVORITES_SUBSCRIPTION_REQUIRED_MESSAGE =
  'You need an active subscription to listen for notifications for favorite items.'

export interface UserFavorites {
  userId: string
  favoriteItems: string[]
}

export async function fetchMyFavorites(): Promise<UserFavorites> {
  const res = await fetch(`${BASE_URL}/me`, { headers: { ...authHeaders() } })
  if (!res.ok) throw new Error(await parseApiError(res, 'Failed to fetch favorites.'))
  return res.json() as Promise<UserFavorites>
}

export async function fetchAdminFavorites(): Promise<UserFavorites[]> {
  const res = await fetch(BASE_URL, { headers: { ...authHeaders() } })
  if (!res.ok) throw new Error(await parseApiError(res, 'Failed to fetch favorites.'))
  return res.json() as Promise<UserFavorites[]>
}

export async function addFavorite(itemName: string): Promise<void> {
  const res = await fetch(BASE_URL, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify({ itemName }),
  })

  if (res.status === 400) {
    throw new Error(FAVORITES_SUBSCRIPTION_REQUIRED_MESSAGE)
  }

  if (!res.ok) throw new Error(await parseApiError(res, 'Failed to add favorite.'))
}

export async function removeFavorite(itemName: string): Promise<void> {
  const res = await fetch(BASE_URL, {
    method: 'DELETE',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify({ itemName }),
  })

  if (!res.ok) throw new Error(await parseApiError(res, 'Failed to remove favorite.'))
}
