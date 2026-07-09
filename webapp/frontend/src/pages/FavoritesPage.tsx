import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { fetchMyFavorites, removeFavorite } from '../api/favoritesApi'
import { notifyError, notifySuccess } from '../api/apiUtils'
import { notificationBaseItemName, resolveNotificationItemPath } from '../api/notificationsApi'

interface FavoriteRow {
  itemName: string
  path?: string
}

export default function FavoritesPage() {
  const [favorites, setFavorites] = useState<FavoriteRow[]>([])
  const [loading, setLoading] = useState(true)
  const [removing, setRemoving] = useState<string | null>(null)

  async function load() {
    setLoading(true)
    try {
      const result = await fetchMyFavorites()
      const rows = await Promise.all(result.favoriteItems.map(async itemName => {
        try {
          return { itemName, path: await resolveNotificationItemPath(itemName) }
        } catch {
          return { itemName }
        }
      }))
      setFavorites(rows)
    } catch (err) {
      notifyError(err instanceof Error ? err.message : 'Failed to load favorites.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load() }, [])

  async function handleRemove(itemName: string) {
    setRemoving(itemName)
    try {
      await removeFavorite(itemName)
      notifySuccess(`Removed "${itemName}" from favorites.`)
      setFavorites(current => current.filter(favorite => favorite.itemName !== itemName))
    } catch (err) {
      notifyError(err instanceof Error ? err.message : 'Failed to remove favorite.')
    } finally {
      setRemoving(null)
    }
  }

  return (
    <div className="settings-page">
      <div className="settings-card">
        <h1 className="settings-title">Favorites</h1>
        <p className="settings-email">Items you listen to for group request notifications.</p>

        {loading ? (
          <p className="gr-notice" style={{ marginTop: '1rem' }}>Loading...</p>
        ) : favorites.length === 0 ? (
          <p className="gr-notice" style={{ marginTop: '1rem' }}>
            You have no favorite items.{' '}
            Browse <Link to="/resources" className="gr-link">resources</Link> or{' '}
            <Link to="/relics" className="gr-link">relics</Link> to add one.
          </p>
        ) : (
          <ul className="my-gr-list">
            {favorites.map(favorite => (
              <li key={favorite.itemName} className="my-gr-item">
                <div className="favorite-row__text">
                  {favorite.path ? (
                    <Link to={favorite.path} className="favorite-row__link">
                      {favorite.itemName}
                    </Link>
                  ) : (
                    <span className="my-gr-item__name">{favorite.itemName}</span>
                  )}
                  {notificationBaseItemName(favorite.itemName) !== favorite.itemName && (
                    <span className="favorite-row__base">
                      {notificationBaseItemName(favorite.itemName)}
                    </span>
                  )}
                </div>
                <button
                  className="gr-btn gr-btn--remove"
                  type="button"
                  onClick={() => handleRemove(favorite.itemName)}
                  disabled={removing === favorite.itemName}
                >
                  {removing === favorite.itemName ? 'Removing...' : 'Remove'}
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  )
}
