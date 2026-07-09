import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { fetchAdminFavorites } from '../api/favoritesApi'
import { notifyError } from '../api/apiUtils'

interface AdminFavoriteRow {
  userId: string
  itemName: string
}

export default function FavoritesAdminPage() {
  const [filterUserId, setFilterUserId] = useState('')
  const [filterItemName, setFilterItemName] = useState('')
  const [appliedUserId, setAppliedUserId] = useState('')
  const [appliedItemName, setAppliedItemName] = useState('')
  const [favorites, setFavorites] = useState<AdminFavoriteRow[]>([])
  const [loading, setLoading] = useState(true)

  async function load() {
    setLoading(true)
    try {
      const result = await fetchAdminFavorites()
      setFavorites(result.flatMap(userFavorites =>
        userFavorites.favoriteItems.map(itemName => ({
          userId: userFavorites.userId,
          itemName,
        })),
      ))
    } catch (err) {
      notifyError(err instanceof Error ? err.message : 'Failed to load favorites.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load() }, [])

  const filteredFavorites = useMemo(() => {
    const userNeedle = appliedUserId.trim().toLowerCase()
    const itemNeedle = appliedItemName.trim().toLowerCase()

    return favorites.filter(favorite =>
      (!userNeedle || favorite.userId.toLowerCase().includes(userNeedle)) &&
      (!itemNeedle || favorite.itemName.toLowerCase().includes(itemNeedle)),
    )
  }, [favorites, appliedUserId, appliedItemName])

  function handleFilter(e: FormEvent) {
    e.preventDefault()
    setAppliedUserId(filterUserId)
    setAppliedItemName(filterItemName)
  }

  function handleClear() {
    setFilterUserId('')
    setFilterItemName('')
    setAppliedUserId('')
    setAppliedItemName('')
  }

  return (
    <div className="admin-subpage">
      <div className="admin-subpage__header">
        <Link to="/admin" className="admin-subpage__back">← Admin Panel</Link>
        <h1 className="admin-subpage__title">Favorites</h1>
      </div>

      <div className="admin-section">
        <form className="admin-gr-filters" onSubmit={handleFilter}>
          <input
            className="admin-search"
            placeholder="Filter by User ID..."
            value={filterUserId}
            onChange={e => setFilterUserId(e.target.value)}
          />
          <input
            className="admin-search"
            placeholder="Filter by Item Name..."
            value={filterItemName}
            onChange={e => setFilterItemName(e.target.value)}
          />
          <button type="submit" className="admin-add-btn">Filter</button>
          {(appliedUserId || appliedItemName || filterUserId || filterItemName) && (
            <button type="button" className="admin-add-btn admin-add-btn--secondary" onClick={handleClear}>
              Clear
            </button>
          )}
        </form>

        <div className="admin-table-wrapper">
          <table className="admin-table">
            <thead>
              <tr>
                <th>User ID</th>
                <th>Item</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr><td colSpan={2} className="admin-table__empty">Loading...</td></tr>
              ) : filteredFavorites.length === 0 ? (
                <tr><td colSpan={2} className="admin-table__empty">No favorites found.</td></tr>
              ) : (
                filteredFavorites.map(favorite => (
                  <tr key={`${favorite.userId}-${favorite.itemName}`}>
                    <td className="admin-gr__userid" title={favorite.userId}>{favorite.userId}</td>
                    <td>{favorite.itemName}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
