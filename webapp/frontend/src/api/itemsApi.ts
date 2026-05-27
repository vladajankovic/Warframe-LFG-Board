import { getAuthToken } from './usersApi'
import { parseApiError } from './apiUtils'

export interface Relic {
  id: number
  name: string
  relicType: string
  relicCode: string
}

export interface Resource {
  id: number
  name: string
  description?: string
  region?: string
  category?: string
  group?: string
}

const BASE_URL = '/v1/itemstore'

export function authHeaders(): HeadersInit {
  const token = getAuthToken()
  return token ? { Authorization: `Bearer ${token}` } : {}
}

export async function fetchRelics(): Promise<Relic[]> {
  const res = await fetch(`${BASE_URL}/relics`)
  if (!res.ok) throw new Error('Failed to fetch relics')
  return res.json() as Promise<Relic[]>
}

export async function fetchResources(): Promise<Resource[]> {
  const res = await fetch(`${BASE_URL}/resources`)
  if (!res.ok) throw new Error('Failed to fetch resources')
  return res.json() as Promise<Resource[]>
}

export async function fetchRelicByName(name: string): Promise<Relic> {
  const res = await fetch(`${BASE_URL}/relics/relic/${encodeURIComponent(name)}`)
  if (!res.ok) throw new Error('Relic not found')
  return res.json() as Promise<Relic>
}

export async function fetchResourceByName(name: string): Promise<Resource> {
  const res = await fetch(`${BASE_URL}/resources/resource/${encodeURIComponent(name)}`)
  if (!res.ok) throw new Error('Resource not found')
  return res.json() as Promise<Resource>
}

// ── Admin CRUD ────────────────────────────────────────────

export interface AddRelicPayload {
  name: string
  type: number
  code: string
}

export interface AddResourcePayload {
  name: string
  description: string
  region: string
  category: string
  group: string
}

export async function createRelic(payload: AddRelicPayload): Promise<Relic> {
  const res = await fetch(`${BASE_URL}/relics`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify(payload),
  })
  if (!res.ok) throw new Error(await parseApiError(res))
  return res.json() as Promise<Relic>
}

export async function updateRelic(payload: AddRelicPayload): Promise<Relic> {
  const res = await fetch(`${BASE_URL}/relics`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify(payload),
  })
  if (!res.ok) throw new Error(await parseApiError(res))
  return res.json() as Promise<Relic>
}

export async function deleteRelic(name: string): Promise<void> {
  const res = await fetch(`${BASE_URL}/relics/relic/${encodeURIComponent(name)}`, {
    method: 'DELETE',
    headers: { ...authHeaders() },
  })
  if (!res.ok) throw new Error(await parseApiError(res))
}

export async function createResource(payload: AddResourcePayload): Promise<Resource> {
  const res = await fetch(`${BASE_URL}/resources`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify(payload),
  })
  if (!res.ok) throw new Error(await parseApiError(res))
  return res.json() as Promise<Resource>
}

export async function updateResource(payload: AddResourcePayload): Promise<Resource> {
  const res = await fetch(`${BASE_URL}/resources`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify(payload),
  })
  if (!res.ok) throw new Error(await parseApiError(res))
  return res.json() as Promise<Resource>
}

export async function deleteResource(name: string): Promise<void> {
  const res = await fetch(`${BASE_URL}/resources/resource/${encodeURIComponent(name)}`, {
    method: 'DELETE',
    headers: { ...authHeaders() },
  })
  if (!res.ok) throw new Error(await parseApiError(res))
}
