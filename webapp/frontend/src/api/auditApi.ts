import { authHeaders } from './itemsApi'
import { parseApiError } from './apiUtils'

const BASE_URL = '/v1/audit'

export interface AuditLog {
  dateTime: string
  service: string
  action: string
  user: string
  details: string
}

export async function fetchAuditLogs(
  from?: string,
  to?: string,
  service?: string,
  action?: string,
  user?: string,
): Promise<AuditLog[]> {
  const params = new URLSearchParams()
  if (from) params.set('from', from)
  if (to) params.set('to', to)
  if (service) params.set('service', service)
  if (action) params.set('action', action)
  if (user) params.set('user', user)
  const query = params.toString() ? `?${params.toString()}` : ''
  const res = await fetch(`${BASE_URL}${query}`, { headers: { ...authHeaders() } })
  if (!res.ok) throw new Error(await parseApiError(res))
  return res.json() as Promise<AuditLog[]>
}
