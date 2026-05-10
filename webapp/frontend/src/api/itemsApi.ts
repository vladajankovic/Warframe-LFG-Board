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
  category?: string
  wikiCategory?: string
  group?: string
}

const BASE_URL = '/v1/itemstore'

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
