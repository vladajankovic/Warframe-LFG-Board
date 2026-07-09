import { useEffect, useState } from 'react'
import { addFavorite, fetchMyFavorites, removeFavorite } from '../api/favoritesApi'
import { notifyError, notifySuccess } from '../api/apiUtils'

interface FavoriteButtonProps {
  itemName: string | null
  disabledMessage?: string
}

export function FavoriteButton({ itemName, disabledMessage }: FavoriteButtonProps) {
  const [favoriteItems, setFavoriteItems] = useState<string[]>([])
  const [loading, setLoading] = useState(true)
  const [acting, setActing] = useState(false)

  useEffect(() => {
    let disposed = false

    async function load() {
      setLoading(true)
      try {
        const result = await fetchMyFavorites()
        if (!disposed) {
          setFavoriteItems(result.favoriteItems)
        }
      } catch {
        if (!disposed) {
          setFavoriteItems([])
        }
      } finally {
        if (!disposed) {
          setLoading(false)
        }
      }
    }

    load()
    return () => {
      disposed = true
    }
  }, [])

  async function handleClick() {
    if (!itemName) {
      notifyError(disabledMessage ?? 'Select an item before adding it to favorites.')
      return
    }

    setActing(true)
    try {
      if (favoriteItems.includes(itemName)) {
        await removeFavorite(itemName)
        setFavoriteItems(current => current.filter(favorite => favorite !== itemName))
        notifySuccess(`Removed "${itemName}" from favorites.`)
      } else {
        await addFavorite(itemName)
        setFavoriteItems(current => current.includes(itemName) ? current : [...current, itemName])
        notifySuccess(`Added "${itemName}" to favorites.`)
      }
    } catch (err) {
      notifyError(err instanceof Error ? err.message : 'Failed to update favorite.')
    } finally {
      setActing(false)
    }
  }

  const isFavorite = itemName ? favoriteItems.includes(itemName) : false

  return (
    <button
      className={`favorite-btn${isFavorite ? ' favorite-btn--active' : ''}`}
      type="button"
      onClick={handleClick}
      disabled={loading || acting}
      aria-label={isFavorite ? `Remove ${itemName} from favorites` : `Add ${itemName ?? 'item'} to favorites`}
      title={isFavorite ? 'Remove from favorites' : 'Add to favorites'}
    >
      <span aria-hidden="true">{isFavorite ? '★' : '☆'}</span>
      <span>{acting ? 'Saving...' : isFavorite ? 'Favorited' : 'Favorite'}</span>
    </button>
  )
}
