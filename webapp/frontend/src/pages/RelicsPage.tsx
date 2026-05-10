import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { fetchRelics, type Relic } from '../api/itemsApi'

const RELIC_TYPES = ['ALL', 'LITH', 'MESO', 'NEO', 'AXI', 'REQUIEM', 'VANGUARD'] as const
type RelicTypeFilter = (typeof RELIC_TYPES)[number]

export default function RelicsPage() {
  const [relics, setRelics] = useState<Relic[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [activeType, setActiveType] = useState<RelicTypeFilter>('ALL')
  const [search, setSearch] = useState('')
  const navigate = useNavigate()

  useEffect(() => {
    fetchRelics()
      .then(setRelics)
      .catch((err: Error) => setError(err.message))
      .finally(() => setLoading(false))
  }, [])

  const grouped = useMemo<Record<string, Relic[]>>(() => {
    const types = activeType === 'ALL' ? (RELIC_TYPES.slice(1) as string[]) : [activeType]
    const result: Record<string, Relic[]> = {}

    for (const type of types) {
      const matches = relics
        .filter((r) => r.relicType?.toUpperCase() === type)
        .filter((r) =>
          search === '' || r.name.toLowerCase().includes(search.toLowerCase()),
        )
        .sort((a, b) => a.name.localeCompare(b.name))

      if (matches.length > 0) result[type] = matches
    }

    return result
  }, [relics, activeType, search])

  return (
    <div className="list-page">
      <header className="list-header">
        <button className="back-btn" onClick={() => navigate('/')}>← Back</button>
        <h1>Relics</h1>
        <input
          type="text"
          className="search-bar"
          placeholder="Search relics…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </header>

      <div className="filter-bar">
        {RELIC_TYPES.map((type) => (
          <button
            key={type}
            className={`filter-btn ${activeType === type ? 'filter-btn--active' : ''}`}
            onClick={() => setActiveType(type)}
            type="button"
          >
            {type}
          </button>
        ))}
      </div>

      {loading && <p className="status-msg">Loading relics…</p>}
      {error && <p className="status-msg status-msg--error">Error: {error}</p>}

      {!loading && !error && Object.keys(grouped).length === 0 && (
        <p className="status-msg">No relics found.</p>
      )}

      {!loading && !error && Object.entries(grouped).map(([type, items]) => (
        <section key={type} className="relic-group">
          <h2 className="relic-group__title">{type}</h2>
          <ul className="item-list">
            {items.map((relic) => {
              const typeKey = relic.relicType.toUpperCase() === 'VANGUARD' ? 'Axi' : relic.relicType.charAt(0).toUpperCase() + relic.relicType.slice(1).toLowerCase()
              const imgSrc = `/images/relics/${typeKey}RelicIntact.png`
              return (
                <li
                  key={relic.id}
                  className="item-card item-card--relic item-card--clickable"
                  onClick={() => navigate(`/relics/${encodeURIComponent(relic.name)}`)}
                >
                  <img
                    className="item-card__relic-img"
                    src={imgSrc}
                    alt=""
                    onError={(e) => { (e.currentTarget as HTMLImageElement).style.display = 'none' }}
                  />
                  <span className="item-card__name">{relic.name}</span>
                </li>
              )
            })}
          </ul>
        </section>
      ))}
    </div>
  )
}
