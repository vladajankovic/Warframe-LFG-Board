import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import CategoryCard from '../components/CategoryCard'
import { fetchRelics, fetchResources, type Relic, type Resource } from '../api/itemsApi'

type ItemResult =
  | { kind: 'relic'; data: Relic }
  | { kind: 'resource'; data: Resource }

const RELIC_FAN_IMAGES = [
  { key: 'lith',    src: '/images/relics/LithRelicRadiant.png',    cls: 'relic-fan__img--lith'    },
  { key: 'meso',    src: '/images/relics/MesoRelicRadiant.png',    cls: 'relic-fan__img--meso'    },
  { key: 'axi',     src: '/images/relics/AxiRelicRadiant.png',     cls: 'relic-fan__img--axi'     },
  { key: 'neo',     src: '/images/relics/NeoRelicRadiant.png',     cls: 'relic-fan__img--neo'     },
  { key: 'requiem', src: '/images/relics/RequiemRelicRadiant.png', cls: 'relic-fan__img--requiem' },
]

function RelicFan() {
  return (
    <div className="relic-fan">
      {RELIC_FAN_IMAGES.map(({ key, src, cls }) => (
        <img key={key} className={`relic-fan__img ${cls}`} src={src} alt="" />
      ))}
    </div>
  )
}

const RESOURCE_FADE_IMAGES = [
  '/images/resources/OrokinCell.png',
  '/images/resources/Credits.png',
  '/images/resources/Endo.png',
]

function ResourceFade() {
  return (
    <div className="resource-fade">
      {RESOURCE_FADE_IMAGES.map((src) => (
        <img key={src} className="resource-fade__img" src={src} alt="" />
      ))}
    </div>
  )
}

const CATEGORIES = [
  {
    id: 'relics',
    label: 'Void Relics',
    description: 'Lith, Meso, Neo, Axi, Requiem & Vanguard',
    icon: '/images/relic-icon.png',
    path: '/relics',
  },
  {
    id: 'resources',
    label: 'Resources',
    description: 'Crafting materials and miscellaneous resources',
    icon: '/images/resource-icon.png',
    path: '/resources',
  },
]

function topMatches(
  relics: Relic[],
  resources: Resource[],
  query: string,
  limit = 5,
): ItemResult[] {
  const q = query.trim().toLowerCase()
  if (!q) return []

  const all: ItemResult[] = [
    ...relics.map((d): ItemResult => ({ kind: 'relic', data: d })),
    ...resources.map((d): ItemResult => ({ kind: 'resource', data: d })),
  ]

  const startsWith: ItemResult[] = []
  const contains: ItemResult[] = []

  for (const item of all) {
    const name = item.data.name.toLowerCase()
    if (name.startsWith(q)) startsWith.push(item)
    else if (name.includes(q)) contains.push(item)
    if (startsWith.length + contains.length >= limit * 4) break
  }

  return [...startsWith, ...contains].slice(0, limit)
}

export default function HomePage() {
  const [search, setSearch] = useState('')
  const [relics, setRelics] = useState<Relic[]>([])
  const [resources, setResources] = useState<Resource[]>([])
  const [open, setOpen] = useState(false)
  const navigate = useNavigate()
  const wrapperRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    fetchRelics().then(setRelics).catch(() => {})
    fetchResources().then(setResources).catch(() => {})
  }, [])

  useEffect(() => {
    function onPointerDown(e: PointerEvent) {
      if (wrapperRef.current && !wrapperRef.current.contains(e.target as Node)) {
        setOpen(false)
      }
    }
    document.addEventListener('pointerdown', onPointerDown)
    return () => document.removeEventListener('pointerdown', onPointerDown)
  }, [])

  const suggestions = topMatches(relics, resources, search)

  function handleSelect(item: ItemResult) {
    setSearch('')
    setOpen(false)
    if (item.kind === 'relic') {
      navigate(`/relics/${encodeURIComponent(item.data.name)}`)
    } else {
      navigate(`/resources/${encodeURIComponent(item.data.name)}`)
    }
  }

  return (
    <div className="home-page">
      <header className="home-header">
        <h1 className="site-title">Warframe LFG Board</h1>
        <p className="site-subtitle">Find a squad — select a category or search for an item</p>

        <div className="search-bar-wrapper" ref={wrapperRef}>
          <input
            type="text"
            className="search-bar"
            placeholder="Search items…"
            value={search}
            onChange={(e) => { setSearch(e.target.value); setOpen(true) }}
            onFocus={() => search && setOpen(true)}
            autoComplete="off"
          />
          {open && suggestions.length > 0 && (
            <ul className="search-dropdown">
              {suggestions.map((item) => (
                <li
                  key={`${item.kind}-${item.data.id}`}
                  className="search-dropdown__item"
                  onPointerDown={(e) => { e.preventDefault(); handleSelect(item) }}
                >
                  <span className="search-dropdown__name">{item.data.name}</span>
                  <span className="search-dropdown__badge">
                    {item.kind === 'relic'
                      ? `Relic · ${(item.data as Relic).relicType} ${(item.data as Relic).relicCode}`
                      : 'Resource'}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </div>
      </header>

      <main className="category-grid">
        {CATEGORIES.map((cat) => (
          <CategoryCard
            key={cat.id}
            label={cat.label}
            description={cat.description}
            icon={cat.icon}
            onClick={() => navigate(cat.path)}
            iconContent={cat.id === 'relics' ? <RelicFan /> : cat.id === 'resources' ? <ResourceFade /> : undefined}
          />
        ))}
      </main>
    </div>
  )
}
