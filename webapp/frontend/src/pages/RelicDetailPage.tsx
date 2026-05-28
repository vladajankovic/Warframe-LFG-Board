import { useEffect, useState } from 'react'
import { useNavigate, useParams, Link } from 'react-router-dom'
import { fetchRelicByName, type Relic } from '../api/itemsApi'
import {
  fetchGroupRequestsByItem,
  fetchMyGroupRequests,
  createGroupRequest,
  deleteMyGroupRequest,
  type GroupRequest,
} from '../api/groupRequestsApi'
import { notifyError, notifySuccess } from '../api/apiUtils'

function relicImgSrc(relicType: string, quality: 'Intact' | 'Radiant') {
  const typeKey =
    relicType.toUpperCase() === 'VANGUARD'
      ? 'Axi'
      : relicType.charAt(0).toUpperCase() + relicType.slice(1).toLowerCase()
  return `/images/relics/${typeKey}Relic${quality}.png`
}

const REFINEMENTS = ['Intact', 'Flawless', 'Exceptional', 'Radiant'] as const
type Refinement = typeof REFINEMENTS[number]
type GrTab = 'All' | Refinement

function refinedName(base: string, ref: Refinement) {
  return `${base} (${ref})`
}

function extractRefinement(fullItemName: string): string {
  const match = fullItemName.match(/\((\w+)\)$/)
  return match ? match[1] : ''
}

function GroupRequestSection({ itemName }: { itemName: string }) {
  const email = localStorage.getItem('user_email')
  const ign = localStorage.getItem('user_ign') ?? ''

  const [activeTab, setActiveTab] = useState<GrTab>('All')
  const [requests, setRequests] = useState<GroupRequest[]>([])
  const [ownRequest, setOwnRequest] = useState<{ itemName: string } | null>(null)
  const [loadingList, setLoadingList] = useState(true)
  const [acting, setActing] = useState(false)

  async function load(tab: GrTab) {
    setLoadingList(true)
    try {
      const [tabRequests, mine] = await Promise.all([
        tab === 'All'
          ? Promise.all(REFINEMENTS.map(r => fetchGroupRequestsByItem(refinedName(itemName, r)))).then(results => results.flat())
          : fetchGroupRequestsByItem(refinedName(itemName, tab)),
        email ? fetchMyGroupRequests() : Promise.resolve([]),
      ])
      setRequests(tabRequests)
      const own = mine.find(r => REFINEMENTS.some(ref => r.itemName === refinedName(itemName, ref)))
      setOwnRequest(own ? { itemName: own.itemName } : null)
    } catch {
      // silently ignore — list simply stays empty
    } finally {
      setLoadingList(false)
    }
  }

  useEffect(() => { load(activeTab) }, [itemName, activeTab])

  async function handleCreate() {
    if (activeTab === 'All') return
    setActing(true)
    try {
      await createGroupRequest(refinedName(itemName, activeTab), ign)
      notifySuccess('Group request created.')
      load(activeTab)
    } catch (err) {
      notifyError(err instanceof Error ? err.message : 'Something went wrong.')
    } finally {
      setActing(false)
    }
  }

  async function handleDelete() {
    if (!ownRequest) return
    setActing(true)
    try {
      await deleteMyGroupRequest(ownRequest.itemName)
      notifySuccess('Group request removed.')
      load(activeTab)
    } catch (err) {
      notifyError(err instanceof Error ? err.message : 'Something went wrong.')
    } finally {
      setActing(false)
    }
  }

  const ownRefinement = ownRequest ? extractRefinement(ownRequest.itemName) : null

  return (
    <div className="gr-section">
      <h2 className="gr-section__title">Group Requests</h2>

      <div className="gr-tabs">
        {(['All', ...REFINEMENTS] as GrTab[]).map(t => (
          <button
            key={t}
            className={`gr-tab${activeTab === t ? ' gr-tab--active' : ''}`}
            onClick={() => setActiveTab(t)}
            type="button"
          >
            {t}
          </button>
        ))}
      </div>

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
              {activeTab === 'All' && (
                <span className="gr-item__refinement">{extractRefinement(r.itemName)}</span>
              )}
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
        ) : ownRequest ? (
          <div className="gr-active">
            <span className="gr-badge">✓ Your request is active{ownRefinement ? ` (${ownRefinement})` : ''}</span>
            <button className="gr-btn gr-btn--remove" onClick={handleDelete} disabled={acting}>
              {acting ? 'Removing…' : 'Remove'}
            </button>
          </div>
        ) : activeTab === 'All' ? (
          <p className="gr-notice">Select a refinement tab to create a group request.</p>
        ) : (
          <button className="gr-btn gr-btn--create" onClick={handleCreate} disabled={acting}>
            {acting ? 'Requesting…' : `⚑ Request Group (${activeTab})`}
          </button>
        )}
      </div>
    </div>
  )
}

export default function RelicDetailPage() {
  const { name } = useParams<{ name: string }>()
  const [relic, setRelic] = useState<Relic | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const navigate = useNavigate()

  useEffect(() => {
    if (!name) return
    fetchRelicByName(name)
      .then(setRelic)
      .catch((err: Error) => setError(err.message))
      .finally(() => setLoading(false))
  }, [name])

  return (
    <div className="detail-page">
      <button className="back-btn" onClick={() => navigate('/relics')}>← Back to Relics</button>

      {loading && <p className="status-msg">Loading…</p>}
      {error && <p className="status-msg status-msg--error">Error: {error}</p>}

      {relic && (
        <>
          <div className="detail-card">
            <img
              className="detail-card__img"
              src={relicImgSrc(relic.relicType, 'Radiant')}
              alt={relic.name}
              onError={(e) => { (e.currentTarget as HTMLImageElement).style.display = 'none' }}
            />
            <h1 className="detail-card__name">{relic.name}</h1>
            <p className="detail-card__sub">{relic.relicType} · {relic.relicCode}</p>
          </div>

          <GroupRequestSection itemName={relic.name} />
        </>
      )}
    </div>
  )
}
