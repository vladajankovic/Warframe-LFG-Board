import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import { fetchMyGroupRequests, deleteMyGroupRequest, type GroupRequestDTO } from '../api/groupRequestsApi'
import { notifyError, notifySuccess } from '../api/apiUtils'

export default function MyGroupRequestsPage() {
  const [requests, setRequests] = useState<GroupRequestDTO[]>([])
  const [loading, setLoading] = useState(true)
  const [deleting, setDeleting] = useState<string | null>(null)

  async function load() {
    setLoading(true)
    try {
      setRequests(await fetchMyGroupRequests())
    } catch (err) {
      notifyError(err instanceof Error ? err.message : 'Failed to load group requests.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load() }, [])

  async function handleDelete(itemName: string) {
    setDeleting(itemName)
    try {
      await deleteMyGroupRequest(itemName)
      notifySuccess(`Request for "${itemName}" removed.`)
      setRequests(prev => prev.filter(r => r.itemName !== itemName))
    } catch (err) {
      notifyError(err instanceof Error ? err.message : 'Failed to remove request.')
    } finally {
      setDeleting(null)
    }
  }

  return (
    <div className="settings-page">
      <div className="settings-card">
        <h1 className="settings-title">My Group Requests</h1>
        <p className="settings-email">Active group requests you have created.</p>

        {loading ? (
          <p className="gr-notice" style={{ marginTop: '1rem' }}>Loading…</p>
        ) : requests.length === 0 ? (
          <p className="gr-notice" style={{ marginTop: '1rem' }}>
            You have no active group requests.{' '}
            Browse <Link to="/resources" className="gr-link">resources</Link> or{' '}
            <Link to="/relics" className="gr-link">relics</Link> to create one.
          </p>
        ) : (
          <ul className="my-gr-list">
            {requests.map(r => (
              <li key={r.itemName} className="my-gr-item">
                <span className="my-gr-item__name">{r.itemName}</span>
                <button
                  className="gr-btn gr-btn--remove"
                  onClick={() => handleDelete(r.itemName)}
                  disabled={deleting === r.itemName}
                >
                  {deleting === r.itemName ? 'Removing…' : 'Remove'}
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  )
}
