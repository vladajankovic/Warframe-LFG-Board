import { useState, useEffect, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { fetchAdminGroupRequests, adminDeleteGroupRequest, type GroupRequestDTO } from '../api/groupRequestsApi'
import { notifyError, notifySuccess } from '../api/apiUtils'
import { DeleteModal } from '../components/DeleteModal'

export default function GroupRequestsAdminPage() {
  const [filterUserId, setFilterUserId] = useState('')
  const [filterItemName, setFilterItemName] = useState('')
  const [requests, setRequests] = useState<GroupRequestDTO[]>([])
  const [loading, setLoading] = useState(true)
  const [deleteTarget, setDeleteTarget] = useState<GroupRequestDTO | null>(null)

  async function load(userId?: string, itemName?: string) {
    setLoading(true)
    try {
      setRequests(await fetchAdminGroupRequests(userId || undefined, itemName || undefined))
    } catch (err) {
      notifyError(err instanceof Error ? err.message : 'Failed to load group requests.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load() }, [])

  function handleFilter(e: FormEvent) {
    e.preventDefault()
    load(filterUserId, filterItemName)
  }

  function handleClear() {
    setFilterUserId('')
    setFilterItemName('')
    load()
  }

  return (
    <div className="admin-subpage">
      <div className="admin-subpage__header">
        <Link to="/admin" className="admin-subpage__back">← Admin Panel</Link>
        <h1 className="admin-subpage__title">Group Requests</h1>
      </div>

      <div className="admin-section">
        <form className="admin-gr-filters" onSubmit={handleFilter}>
          <input
            className="admin-search"
            placeholder="Filter by User ID…"
            value={filterUserId}
            onChange={e => setFilterUserId(e.target.value)}
          />
          <input
            className="admin-search"
            placeholder="Filter by Item Name…"
            value={filterItemName}
            onChange={e => setFilterItemName(e.target.value)}
          />
          <button type="submit" className="admin-add-btn">Filter</button>
          {(filterUserId || filterItemName) && (
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
                <th>In-Game Name</th>
                <th>Item</th>
                <th className="admin-table__actions-col"></th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr><td colSpan={4} className="admin-table__empty">Loading…</td></tr>
              ) : requests.length === 0 ? (
                <tr><td colSpan={4} className="admin-table__empty">No group requests found.</td></tr>
              ) : (
                requests.map(r => (
                  <tr key={`${r.userId}-${r.itemName}`}>
                    <td className="admin-gr__userid" title={r.userId}>{r.userId}</td>
                    <td>{r.inGameName}</td>
                    <td>{r.itemName}</td>
                    <td className="admin-table__actions">
                      <button
                        className="admin-action-btn admin-action-btn--delete"
                        onClick={() => setDeleteTarget(r)}
                        type="button"
                      >
                        Delete
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {deleteTarget && (
        <DeleteModal
          itemName={`${deleteTarget.inGameName}'s request for "${deleteTarget.itemName}"`}
          onClose={() => setDeleteTarget(null)}
          onConfirm={async () => {
            await adminDeleteGroupRequest(deleteTarget.userId, deleteTarget.itemName)
            notifySuccess('Group request deleted.')
            setDeleteTarget(null)
            load(filterUserId || undefined, filterItemName || undefined)
          }}
        />
      )}
    </div>
  )
}
