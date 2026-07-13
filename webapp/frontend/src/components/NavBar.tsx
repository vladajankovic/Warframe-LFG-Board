import { useEffect, useState } from 'react'
import type { HubConnection } from '@microsoft/signalr'
import { Link, useNavigate } from 'react-router-dom'
import { notifyError } from '../api/apiUtils'
import {
  NOTIFICATION_EVENT,
  createNotificationsConnection,
  dismissNotification,
  fetchPendingNotifications,
  resolveNotificationItemPath,
  type GroupRequestNotification,
} from '../api/notificationsApi'

function formatNotificationAge(createdAt: string) {
  const createdTime = new Date(createdAt).getTime()
  const diffMs = Date.now() - createdTime

  if (!Number.isFinite(createdTime) || diffMs < 60_000) {
    return 'now'
  }

  const minutes = Math.floor(diffMs / 60_000)
  return `${minutes} minute${minutes === 1 ? '' : 's'} ago`
}

function upsertNotification(
  notifications: GroupRequestNotification[],
  notification: GroupRequestNotification,
) {
  if (notifications.some(existing => existing.id === notification.id)) {
    return notifications
  }

  return [notification, ...notifications]
}

export default function NavBar() {
  const [email, setEmail] = useState<string | null>(() => localStorage.getItem('user_email'))
  const [ign, setIgn] = useState<string>(() => localStorage.getItem('user_ign') ?? '')
  const [role, setRole] = useState<string | null>(() => localStorage.getItem('user_role'))
  const [open, setOpen] = useState(false)
  const [notificationsOpen, setNotificationsOpen] = useState(false)
  const [notifications, setNotifications] = useState<GroupRequestNotification[]>([])
  const navigate = useNavigate()

  useEffect(() => {
    function onAuthChanged() {
      setEmail(localStorage.getItem('user_email'))
      setRole(localStorage.getItem('user_role'))
      setIgn(localStorage.getItem('user_ign') ?? '')
    }

    window.addEventListener('user_auth_changed', onAuthChanged)
    return () => window.removeEventListener('user_auth_changed', onAuthChanged)
  }, [])

  useEffect(() => {
    let connection: HubConnection | null = null
    let isDisposed = false

    async function loadNotifications() {
      try {
        const pending = await fetchPendingNotifications()
        if (!isDisposed) {
          setNotifications(pending)
        }
      } catch {
        if (!isDisposed) {
          setNotifications([])
        }
      }
    }

    async function startConnection() {
      connection = createNotificationsConnection()
      connection.on(NOTIFICATION_EVENT, (notification: GroupRequestNotification) => {
        setNotifications(current => upsertNotification(current, notification))
      })

      try {
        await connection.start()
      } catch {
        // The initial fetch still works; reconnect happens on next auth/page load.
      }
    }

    if (!email) {
      setNotifications([])
      setNotificationsOpen(false)
      return
    }

    loadNotifications()
    startConnection()

    return () => {
      isDisposed = true
      if (connection) {
        void connection.stop()
      }
    }
  }, [email])

  function handleLogout() {
    localStorage.removeItem('auth_token')
    localStorage.removeItem('user_id')
    localStorage.removeItem('user_email')
    localStorage.removeItem('user_role')
    localStorage.removeItem('user_ign')
    localStorage.removeItem('user_subscription_status')
    localStorage.removeItem('user_subscription_expiry')
    setEmail(null)
    setRole(null)
    setIgn('')
    setOpen(false)
    setNotifications([])
    setNotificationsOpen(false)
    window.dispatchEvent(new Event('user_auth_changed'))
    navigate('/')
  }

  async function handleDismiss(notificationId: string) {
    try {
      await dismissNotification(notificationId)
      setNotifications(current => current.filter(notification => notification.id !== notificationId))
    } catch (err) {
      notifyError(err instanceof Error ? err.message : 'Failed to dismiss notification.')
    }
  }

  async function handleDismissAll() {
    const currentNotifications = [...notifications]
    try {
      await Promise.all(currentNotifications.map(notification => dismissNotification(notification.id)))
      setNotifications([])
      setNotificationsOpen(false)
    } catch (err) {
      notifyError(err instanceof Error ? err.message : 'Failed to dismiss notifications.')
    }
  }

  async function handleNotificationClick(notification: GroupRequestNotification) {
    try {
      const path = await resolveNotificationItemPath(notification.itemName)
      setNotificationsOpen(false)
      navigate(path)
    } catch {
      notifyError(`Could not open item page for ${notification.itemName}.`)
    }
  }

  const displayName = ign || email

  return (
    <nav className="navbar">
      <Link to="/" className="navbar__brand">
        <img src="/images/lfg-logo1.png" alt="Warframe LFG Logo" className="navbar__logo" />
      </Link>

      <div className="navbar__right">
        {email ? (
          <>
            <div className="navbar__notifications">
              <button
                className="navbar__notification-btn"
                type="button"
                aria-label="Notifications"
                onClick={() => setNotificationsOpen(value => !value)}
              >
                <span aria-hidden="true" className="navbar__notification-icon">
                  &#128276;
                </span>
                {notifications.length > 0 && <span className="navbar__notification-dot" />}
              </button>

              {notificationsOpen && (
                <div className="navbar__notifications-dropdown">
                  <div className="navbar__notifications-head">
                    <span>Notifications</span>
                    {notifications.length > 0 && (
                      <button
                        className="navbar__notifications-clear"
                        type="button"
                        onClick={handleDismissAll}
                      >
                        Dismiss all
                      </button>
                    )}
                  </div>

                  {notifications.length === 0 ? (
                    <div className="navbar__notification-empty">No notifications</div>
                  ) : (
                    <ul className="navbar__notification-list">
                      {notifications.map(notification => (
                        <li key={notification.id} className="navbar__notification-row">
                          <button
                            className="navbar__notification-content"
                            type="button"
                            onClick={() => handleNotificationClick(notification)}
                          >
                            <span className="navbar__notification-text">
                              {notification.itemName} group hosted by {notification.inGameName}
                            </span>
                            <span className="navbar__notification-age">
                              {formatNotificationAge(notification.createdAt)}
                            </span>
                          </button>
                          <button
                            className="navbar__notification-dismiss"
                            type="button"
                            aria-label={`Dismiss ${notification.itemName} notification`}
                            onClick={() => handleDismiss(notification.id)}
                          >
                            x
                          </button>
                        </li>
                      ))}
                    </ul>
                  )}
                </div>
              )}
            </div>

            <div
              className="navbar__user-menu"
              onMouseEnter={() => setOpen(true)}
              onMouseLeave={() => setOpen(false)}
            >
              <span className="navbar__user-trigger">
                {displayName}
                <span className="navbar__user-caret">▾</span>
              </span>

              {open && (
                <div className="navbar__dropdown">
                  <Link
                    to="/my-requests"
                    className="navbar__dropdown-item"
                    onClick={() => setOpen(false)}
                  >
                  ⚑ My Requests
                  </Link>
                  <Link
                    to="/favorites"
                    className="navbar__dropdown-item"
                    onClick={() => setOpen(false)}
                  >
                  ★ Favorites
                  </Link>
                  <Link
                    to="/settings"
                    className="navbar__dropdown-item"
                    onClick={() => setOpen(false)}
                  >
                  ⚙ Settings
                  </Link>
                  {role === 'Admin' && (
                    <Link
                      to="/admin"
                      className="navbar__dropdown-item"
                      onClick={() => setOpen(false)}
                    >
                    ◈ Admin Panel
                    </Link>
                  )}
                  <button
                    className="navbar__dropdown-item navbar__dropdown-item--danger"
                    onClick={handleLogout}
                    type="button"
                  >
                  ⏻ Logout
                  </button>
                </div>
              )}
            </div>
          </>
        ) : (
          <Link to="/login" className="navbar__login-btn">
            Login / Register
          </Link>
        )}
      </div>
    </nav>
  )
}
