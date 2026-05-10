import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { fetchResources, type Resource } from '../api/itemsApi'

const CATEGORIES = [
  'ALL',
  'Star Chart',
  'Plains of Eidolon',
  'Orb Vallis',
  'Cambion Drift',
  'Duviri',
  'Höllvania',
  'Dark Refractory',
  'Miscellaneous',
] as const
type CategoryFilter = (typeof CATEGORIES)[number]

/** Two-level tree: wikiCategory (or '') → group → resources */
type GroupTree = Record<string, Record<string, Resource[]>>

function buildTree(resources: Resource[]): GroupTree {
  const tree: GroupTree = {}
  for (const r of resources) {
    const wiki  = r.wikiCategory ?? ''
    const group = r.group ?? 'Other'
    if (!tree[wiki])        tree[wiki] = {}
    if (!tree[wiki][group]) tree[wiki][group] = []
    tree[wiki][group].push(r)
  }
  return tree
}

export default function ResourcesPage() {
  const [resources, setResources] = useState<Resource[]>([])
  const [loading, setLoading]     = useState(true)
  const [error, setError]         = useState<string | null>(null)
  const [activeCategory, setActiveCategory] = useState<CategoryFilter>('ALL')
  const [search, setSearch]       = useState('')
  const navigate = useNavigate()

  useEffect(() => {
    fetchResources()
      .then(setResources)
      .catch((err: Error) => setError(err.message))
      .finally(() => setLoading(false))
  }, [])

  /** Categories that actually contain data */
  const presentCategories = useMemo(
    () => new Set(resources.map((r) => r.category ?? '')),
    [resources],
  )

  /** Per-category two-level tree, filtered by search + active category */
  const grouped = useMemo<Record<string, GroupTree>>(() => {
    const categories =
      activeCategory === 'ALL'
        ? CATEGORIES.slice(1).filter((c) => presentCategories.has(c))
        : [activeCategory]

    const result: Record<string, GroupTree> = {}
    for (const cat of categories) {
      const matches = resources.filter(
        (r) =>
          r.category === cat &&
          (search === '' || r.name.toLowerCase().includes(search.toLowerCase())),
      )
      if (matches.length > 0) result[cat] = buildTree(matches)
    }
    return result
  }, [resources, activeCategory, search, presentCategories])

  return (
    <div className="list-page">
      <header className="list-header">
        <button className="back-btn" onClick={() => navigate('/')}>← Back</button>
        <h1>Resources</h1>
        <input
          type="text"
          className="search-bar"
          placeholder="Search resources…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </header>

      <div className="filter-bar">
        {CATEGORIES.filter((c) => c === 'ALL' || presentCategories.has(c)).map((cat) => (
          <button
            key={cat}
            className={`filter-btn ${activeCategory === cat ? 'filter-btn--active' : ''}`}
            onClick={() => setActiveCategory(cat)}
            type="button"
          >
            {cat}
          </button>
        ))}
      </div>

      {loading && <p className="status-msg">Loading resources…</p>}
      {error   && <p className="status-msg status-msg--error">Error: {error}</p>}

      {!loading && !error && Object.keys(grouped).length === 0 && (
        <p className="status-msg">No resources found.</p>
      )}

      {!loading && !error && Object.entries(grouped).map(([cat, tree]) => (
        <section key={cat} className="relic-group">
          <h2 className="relic-group__title">{cat}</h2>

          {Object.entries(tree).map(([wiki, groups]) => (
            <div key={wiki} className="resource-wiki-section">
              {wiki && <h3 className="resource-wiki-title">{wiki}</h3>}

              {Object.entries(groups).map(([group, items]) => (
                <div key={group} className="resource-group">
                  <h4 className="resource-group__title">{group}</h4>
                  <ul className="item-list item-list--flat">
                    {items.map((resource) => (
                      <li
                        key={resource.id}
                        className="item-card item-card--resource item-card--clickable"
                        onClick={() => navigate(`/resources/${encodeURIComponent(resource.name)}`)}
                      >
                        <img
                          className="item-card__img"
                          src={`/images/resources/${resource.name.replace(/\s+/g, '')}.png`}
                          alt={resource.name}
                          onError={(e) => { (e.currentTarget as HTMLImageElement).style.display = 'none' }}
                        />
                        <span className="item-card__name">{resource.name}</span>
                      </li>
                    ))}
                  </ul>
                </div>
              ))}
            </div>
          ))}
        </section>
      ))}
    </div>
  )
}

