import { authHeaders } from './itemsApi'
import { parseApiError } from './apiUtils'

const BASE_URL = '/v1/user-notification/favorites'

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
  const res = await fetch(`${BASE_URL}/${encodeURIComponent(itemName)}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...authHeaders() }
  })

  if (res.status === 400) {
    const errorText = await res.text()
    throw new Error(errorText)
  }

  if (!res.ok) throw new Error(await parseApiError(res, 'Failed to add favorite.'))
}

export async function removeFavorite(itemName: string): Promise<void> {
  const res = await fetch(`${BASE_URL}/${encodeURIComponent(itemName)}`, {
    method: 'DELETE',
    headers: { 'Content-Type': 'application/json', ...authHeaders() }
  })

  if (!res.ok) throw new Error(await parseApiError(res, 'Failed to remove favorite.'))
}
