import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { authHeaders, fetchRelicByName, fetchResourceByName } from './itemsApi'
import { parseApiError } from './apiUtils'

const BASE_URL = '/v1/user-notification/notifications'
export const NOTIFICATION_EVENT = 'GroupRequestCreated'

export interface GroupRequestNotification {
  id: string
  groupRequestUserId: string
  itemName: string
  inGameName: string
  createdAt: string
}

export async function fetchPendingNotifications(): Promise<GroupRequestNotification[]> {
  const res = await fetch(BASE_URL, { headers: { ...authHeaders() } })
  if (!res.ok) throw new Error(await parseApiError(res, 'Failed to fetch notifications.'))
  return res.json() as Promise<GroupRequestNotification[]>
}

export async function dismissNotification(notificationId: string): Promise<void> {
  const res = await fetch(`${BASE_URL}/${encodeURIComponent(notificationId)}/dismiss`, {
    method: 'POST',
    headers: { ...authHeaders() },
  })
  if (!res.ok) throw new Error(await parseApiError(res, 'Failed to dismiss notification.'))
}

export function createNotificationsConnection() {
  return new HubConnectionBuilder()
    .withUrl('/hubs/user-notifications', {
      accessTokenFactory: () => localStorage.getItem('auth_token') ?? '',
    })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build()
}

export function notificationBaseItemName(itemName: string): string {
  return itemName.replace(/\s+\((Intact|Exceptional|Flawless|Radiant)\)$/i, '')
}

export async function resolveNotificationItemPath(itemName: string): Promise<string> {
  const baseName = notificationBaseItemName(itemName)

  try {
    await fetchRelicByName(baseName)
    return `/relics/${encodeURIComponent(baseName)}`
  } catch {
    await fetchResourceByName(baseName)
    return `/resources/${encodeURIComponent(baseName)}`
  }
}
