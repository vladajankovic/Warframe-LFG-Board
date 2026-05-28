import { useEffect, useState } from 'react'
import { useNavigate, useParams, Link } from 'react-router-dom'
import { fetchResourceByName, type Resource } from '../api/itemsApi'
import {
  fetchGroupRequestsByItem,
  fetchMyGroupRequests,
  createGroupRequest,
  deleteMyGroupRequest,
  type GroupRequest,
} from '../api/groupRequestsApi'
import { notifyError, notifySuccess } from '../api/apiUtils'

function GroupRequestSection({ itemName }: { itemName: string }) {
  const email = localStorage.getItem('user_email')
  const ign = localStorage.getItem('user_ign') ?? ''

  const [requests, setRequests] = useState<GroupRequest[]>([])
  const [hasOwn, setHasOwn] = useState(false)
  const [loadingList, setLoadingList] = useState(true)
  const [acting, setActing] = useState(false)

  async function load() {
    setLoadingList(true)
    try {
      const [all, mine] = await Promise.all([
        fetchGroupRequestsByItem(itemName),
        email ? fetchMyGroupRequests() : Promise.resolve([]),
      ])
      setRequests(all)
      setHasOwn(mine.some(r => r.itemName === itemName))
    } catch {
      // silently ignore — list simply stays empty
    } finally {
      setLoadingList(false)
    }
  }

  useEffect(() => { load() }, [itemName])

  async function handleCreate() {
    setActing(true)
    try {
      await createGroupRequest(itemName, ign)
      notifySuccess('Group request created.')
      load()
    } catch (err) {
      notifyError(err instanceof Error ? err.message : 'Something went wrong.')
    } finally {
      setActing(false)
    }
  }

  async function handleDelete() {
    setActing(true)
    try {
      await deleteMyGroupRequest(itemName)
      notifySuccess('Group request removed.')
      load()
    } catch (err) {
      notifyError(err instanceof Error ? err.message : 'Something went wrong.')
    } finally {
      setActing(false)
    }
  }

  return (
    <div className="gr-section">
      <h2 className="gr-section__title">Group Requests</h2>

      {loadingList ? (
        <p className="gr-notice">Loading…</p>
      ) : requests.length === 0 ? (
        <p className="gr-notice">No group requests yet.</p>
      ) : (
        <ul className="gr-list">
          {requests.map((r, i) => (
            <li key={i} className="gr-item">
              <span className="gr-item__icon">◉</span>
              <span className="gr-item__name">{r.inGameName}</span>
            </li>
          ))}
        </ul>
      )}

      <div className="gr-action">
        {!email ? (
          <p className="gr-notice">
            <Link to="/login" className="gr-link">Log in</Link> to request a group.
          </p>
        ) : !ign ? (
          <p className="gr-notice">
            Set your <Link to="/settings" className="gr-link">in-game name</Link> in Settings to request a group.
          </p>
        ) : hasOwn ? (
          <div className="gr-active">
            <span className="gr-badge">✓ Your request is active</span>
            <button className="gr-btn gr-btn--remove" onClick={handleDelete} disabled={acting}>
              {acting ? 'Removing…' : 'Remove'}
            </button>
          </div>
        ) : (
          <button className="gr-btn gr-btn--create" onClick={handleCreate} disabled={acting}>
            {acting ? 'Requesting…' : '⚑ Request Group'}
          </button>
        )}
      </div>
    </div>
  )
}

export default function ResourceDetailPage() {
  const { name } = useParams<{ name: string }>()
  const [resource, setResource] = useState<Resource | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const navigate = useNavigate()

  useEffect(() => {
    if (!name) return
    fetchResourceByName(name)
      .then(setResource)
      .catch((err: Error) => setError(err.message))
      .finally(() => setLoading(false))
  }, [name])

  return (
    <div className="detail-page">
      <button className="back-btn" onClick={() => navigate('/resources')}>← Back to Resources</button>

      {loading && <p className="status-msg">Loading…</p>}
      {error && <p className="status-msg status-msg--error">Error: {error}</p>}

      {resource && (
        <>
          <div className="detail-card">
            <img
              className="detail-card__img"
              src={`/images/resources/${resource.name.replace(/\s+/g, '')}.png`}
              alt={resource.name}
              onError={(e) => { (e.currentTarget as HTMLImageElement).style.display = 'none' }}
            />
            <h1 className="detail-card__name">{resource.name}</h1>
            <p className="detail-card__sub">
              {[resource.region, resource.category, resource.group]
                .filter(Boolean)
                .join(' · ')}
            </p>
            {resource.description && (
              <p className="detail-card__desc">{resource.description}</p>
            )}
          </div>

          <GroupRequestSection itemName={resource.name} />
        </>
      )}
    </div>
  )
}
