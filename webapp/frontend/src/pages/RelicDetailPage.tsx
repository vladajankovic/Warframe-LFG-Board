import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { fetchRelicByName, type Relic } from '../api/itemsApi'

function relicImgSrc(relicType: string, quality: 'Intact' | 'Radiant') {
  const typeKey =
    relicType.toUpperCase() === 'VANGUARD'
      ? 'Axi'
      : relicType.charAt(0).toUpperCase() + relicType.slice(1).toLowerCase()
  return `/images/relics/${typeKey}Relic${quality}.png`
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
      )}
    </div>
  )
}
