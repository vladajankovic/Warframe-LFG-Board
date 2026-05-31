import { authHeaders } from './itemsApi'
import { parseApiError } from './apiUtils'

const BASE_URL = '/v1/grouprequests'

export interface GroupRequest {
  inGameName: string
  itemName: string
}

export interface GroupRequestDTO {
  userId: string
  inGameName: string
  itemName: string
}

export async function fetchAllGroupRequests(): Promise<GroupRequest[]> {
  const res = await fetch(`${BASE_URL}`)
  if (!res.ok) throw new Error(await parseApiError(res))
  return res.json() as Promise<GroupRequest[]>
}

export async function fetchGroupRequestsByItem(itemName: string): Promise<GroupRequest[]> {
  const res = await fetch(`${BASE_URL}/item/${encodeURIComponent(itemName)}`)
  if (res.status === 404) return []
  if (!res.ok) throw new Error(await parseApiError(res))
  return res.json() as Promise<GroupRequest[]>
}

export async function fetchMyGroupRequests(): Promise<GroupRequestDTO[]> {
  const res = await fetch(`${BASE_URL}/me`, { headers: { ...authHeaders() } })
  if (!res.ok) throw new Error(await parseApiError(res))
  return res.json() as Promise<GroupRequestDTO[]>
}

export async function fetchAdminGroupRequests(
  userId?: string,
  itemName?: string,
): Promise<GroupRequestDTO[]> {
  const params = new URLSearchParams()
  if (userId) params.set('userId', userId)
  if (itemName) params.set('itemName', itemName)
  const query = params.toString() ? `?${params.toString()}` : ''
  const res = await fetch(`${BASE_URL}/admin${query}`, { headers: { ...authHeaders() } })
  if (!res.ok) throw new Error(await parseApiError(res))
  return res.json() as Promise<GroupRequestDTO[]>
}

export async function createGroupRequest(itemName: string, inGameName: string): Promise<GroupRequestDTO> {
  const userId = localStorage.getItem('user_id') ?? ''
  const res = await fetch(`${BASE_URL}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify({ userId, itemName, inGameName }),
  })
  if (!res.ok) throw new Error(await parseApiError(res))
  return res.json() as Promise<GroupRequestDTO>
}

export async function deleteMyGroupRequest(itemName: string): Promise<void> {
  const res = await fetch(`${BASE_URL}/${encodeURIComponent(itemName)}`, {
    method: 'DELETE',
    headers: { ...authHeaders() },
  })
  if (!res.ok) throw new Error(await parseApiError(res))
}

export async function adminDeleteGroupRequest(userId: string, itemName: string): Promise<void> {
  const params = new URLSearchParams({ userId, itemName })
  const res = await fetch(`${BASE_URL}/admin?${params.toString()}`, {
    method: 'DELETE',
    headers: { ...authHeaders() },
  })
  if (!res.ok) throw new Error(await parseApiError(res))
}
