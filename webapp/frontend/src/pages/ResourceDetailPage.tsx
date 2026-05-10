import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { fetchResourceByName, type Resource } from '../api/itemsApi'

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
        <div className="detail-card">
          <img
            className="detail-card__img"
            src={`/images/resources/${resource.name.replace(/\s+/g, '')}.png`}
            alt={resource.name}
            onError={(e) => { (e.currentTarget as HTMLImageElement).style.display = 'none' }}
          />
          <h1 className="detail-card__name">{resource.name}</h1>
          <p className="detail-card__sub">
            {[resource.category, resource.wikiCategory, resource.group]
              .filter(Boolean)
              .join(' · ')}
          </p>
          {resource.description && (
            <p className="detail-card__desc">{resource.description}</p>
          )}
        </div>
      )}
    </div>
  )
}
