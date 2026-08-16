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
import { FavoriteButton } from '../components/FavoriteButton'

function relicImgSrc(relicType: string, quality: string) {
  const typeKey =
    relicType.toUpperCase() === 'VANGUARD'
      ? 'Axi'
      : relicType.charAt(0).toUpperCase() + relicType.slice(1).toLowerCase()
  return `/images/relics/${typeKey}Relic${quality}.png`
}

const REFINEMENTS = ['Intact', 'Exceptional', 'Flawless', 'Radiant'] as const
type Refinement = typeof REFINEMENTS[number]
type GrTab = 'All' | Refinement
type DropRarity = 'common' | 'uncommon' | 'rare'

const DROP_RATES: Record<Refinement, {
  traces: number
  chances: Record<DropRarity, number>
}> = {
  Intact: {
    traces: 0,
    chances: {
      common: 76,
      uncommon: 22,
      rare: 2,
    },
  },
  Exceptional: {
    traces: 25,
    chances: {
      common: 70,
      uncommon: 26,
      rare: 4,
    },
  },
  Flawless: {
    traces: 50,
    chances: {
      common: 60,
      uncommon: 34,
      rare: 6,
    },
  },
  Radiant: {
    traces: 100,
    chances: {
      common: 50,
      uncommon: 40,
      rare: 10,
    },
  },
}

const RARITY_ORDER: Record<DropRarity, number> = {
  common: 0,
  uncommon: 1,
  rare: 2,
}
const DROP_RARITIES: DropRarity[] = ['common', 'uncommon', 'rare']

const DROP_PART_IMAGES = {
  Band: 'Grip.png',
  Barrel: 'Barrel.png',
  Blade: 'Blade.png',
  Blades: 'Blade.png',
  Boot: 'Guard.png',
  Buckle: 'Link.png',
  Carapace: 'Chassis.png',
  Cerebrum: 'Neuroptics.png',
  Chain: 'Stock.png',
  Chassis: 'Chassis.png',
  Disc: 'Blade.png',
  Gauntlet: 'Hilt.png',
  Guard: 'Guard.png',
  Grip: 'Grip.png',
  Handle: 'Hilt.png',
  Head: 'Blade.png',
  Hilt: 'Hilt.png',
  Link: 'Link.png',
  'Lower Limb': 'Blade.png',
  Neuroptics: 'Neuroptics.png',
  Ornament: 'Link.png',
  Pouch: 'Grip.png',
  Receiver: 'Receiver.png',
  Stars: 'Blade.png',
  Stock: 'Stock.png',
  String: 'Stock.png',
  Systems: 'Systems.png',
  'Upper Limb': 'Blade.png',
} as const

const DROP_SPECIAL_IMAGES: Record<string, string> = {
  '2x forma blueprint': 'Forma.png',
  '1200x kuva': 'Kuva.png',
  'odonata prime wings blueprint': 'ArchwingWings.png',
  'odonata prime systems blueprint': 'ArchwingSystems.png',
  'odonata prime harness blueprint': 'ArchwingHarness.png',
}

function normalizedRarity(rarity: string): DropRarity {
  const normalized = rarity.toLowerCase()
  return normalized === 'uncommon' || normalized === 'rare' ? normalized : 'common'
}

function dropImageSrc(itemName: string): string | null {
  const normalizedItemName = itemName.trim()
  const specialImage = DROP_SPECIAL_IMAGES[normalizedItemName.toLowerCase()]
  if (specialImage) return `/images/drops/${specialImage}`

  const part = Object.entries(DROP_PART_IMAGES).find(([partName]) =>
    new RegExp(`\\b${partName}(?:\\s+Blueprint)?$`, 'i').test(normalizedItemName),
  )
  if (part) return `/images/drops/${part[1]}`

  const blueprintImageName = normalizedItemName
    .replace(/\s+Blueprint$/i, '')
    .replace(/\s+/g, '')

  return blueprintImageName
    ? `/images/drops/${blueprintImageName}.png`
    : null
}

function RelicRewardImage({ itemName }: { itemName: string }) {
  const [imageFailed, setImageFailed] = useState(false)
  const imageSrc = dropImageSrc(itemName)

  return imageSrc && !imageFailed ? (
    <img
      className="relic-reward__image"
      src={imageSrc}
      alt=""
      onError={() => setImageFailed(true)}
    />
  ) : (
    <span className="relic-reward__fallback" aria-hidden="true">◇</span>
  )
}

function RelicRewards({
  drops,
  quality,
  onQualityChange,
}: {
  drops: Relic['drops']
  quality: Refinement
  onQualityChange: (quality: Refinement) => void
}) {
  const qualityRates = DROP_RATES[quality]
  const sortedDrops = [...drops].sort((left, right) => {
    const rarityDifference = RARITY_ORDER[normalizedRarity(left.rarity)]
      - RARITY_ORDER[normalizedRarity(right.rarity)]
    return rarityDifference || left.itemName.localeCompare(right.itemName)
  })
  const dropGroups = DROP_RARITIES.map(rarity => ({
    rarity,
    drops: sortedDrops.filter(drop => normalizedRarity(drop.rarity) === rarity),
  }))

  return (
    <section className="relic-rewards" aria-labelledby="relic-rewards-title">
      <div className="relic-rewards__header">
        <div>
          <h2 id="relic-rewards-title" className="relic-rewards__title">Possible rewards</h2>
          <p className="relic-rewards__subtitle">
            Select refinement
          </p>
        </div>
      </div>

      <div className="gr-tabs relic-rewards__refinements" aria-label="Relic refinement quality">
        {REFINEMENTS.map(refinement => (
          <button
            key={refinement}
            type="button"
            className={`gr-tab${quality === refinement ? ' gr-tab--active' : ''}`}
            onClick={() => onQualityChange(refinement)}
            aria-pressed={quality === refinement}
            title={`${DROP_RATES[refinement].traces} Void Traces`}
          >
            {refinement}
          </button>
        ))}
      </div>

      {sortedDrops.length === 0 ? (
        <p className="relic-rewards__empty">No reward data is available for this relic.</p>
      ) : (
        <div className="relic-rewards__groups">
          {dropGroups.map(({ rarity, drops: rarityDrops }) => {
            if (rarityDrops.length === 0) return null
            const totalChance = qualityRates.chances[rarity]

            return (
              <section key={rarity} className={`relic-reward-group relic-reward-group--${rarity}`}>
                <ul className="relic-reward-group__items">
                  {rarityDrops.map(drop => {
                    return (
                      <li key={drop.itemName} className={`relic-reward relic-reward--${rarity}`}>
                        <div className="relic-reward__visual">
                          <RelicRewardImage itemName={drop.itemName} />
                        </div>
                        <div className="relic-reward__body">
                          <span className="relic-reward__name">{drop.itemName}</span>
                        </div>
                      </li>
                    )
                  })}
                </ul>
                <div className="relic-reward-group__total">
                  <div className="relic-reward-group__label">
                    <span>{rarity} total</span>
                    <span>{totalChance}%</span>
                  </div>
                  <div
                    className="relic-reward-group__track"
                    role="progressbar"
                    aria-label={`${rarity} rewards total drop chance`}
                    aria-valuemin={0}
                    aria-valuemax={100}
                    aria-valuenow={totalChance}
                  >
                    <span
                      className={`relic-reward-group__fill relic-reward-group__fill--${rarity}`}
                      style={{ width: `${totalChance}%` }}
                    />
                  </div>
                </div>
              </section>
            )
          })}
        </div>
      )}
    </section>
  )
}

function refinedName(base: string, ref: Refinement) {
  return `${base} (${ref})`
}

function extractRefinement(fullItemName: string): string {
  const match = fullItemName.match(/\((\w+)\)$/)
  return match ? match[1] : ''
}

function GroupRequestSection({
  itemName,
  relicType,
  activeTab,
  onTabChange,
}: {
  itemName: string
  relicType: string
  activeTab: GrTab
  onTabChange: (tab: GrTab) => void
}) {
  const email = localStorage.getItem('user_email')
  const ign = localStorage.getItem('user_ign') ?? ''

  const [requests, setRequests] = useState<GroupRequest[]>([])
  const [ownRequests, setOwnRequests] = useState<GroupRequest[]>([])
  const [loadingList, setLoadingList] = useState(true)
  const [acting, setActing] = useState(false)
  const [contactedIndex, setContactedIndex] = useState<number | null>(null)

  async function load(tab: GrTab) {
    setLoadingList(true)
    setOwnRequests([])
    try {
      const [tabRequests, mine] = await Promise.all([
        tab === 'All'
          ? Promise.all(REFINEMENTS.map(r => fetchGroupRequestsByItem(refinedName(itemName, r)))).then(results => results.flat())
          : fetchGroupRequestsByItem(refinedName(itemName, tab)),
        email ? fetchMyGroupRequests().catch(() => []) : Promise.resolve([]),
      ])
      setRequests(tabRequests)
      const own = tab === 'All'
        ? mine.filter(r => REFINEMENTS.some(ref => r.itemName === refinedName(itemName, ref)))
        : mine.filter(r => r.itemName === refinedName(itemName, tab))
      setOwnRequests(own)
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

  async function handleDelete(requestItemName: string) {
    setActing(true)
    try {
      await deleteMyGroupRequest(requestItemName)
      notifySuccess('Group request removed.')
      load(activeTab)
    } catch (err) {
      notifyError(err instanceof Error ? err.message : 'Something went wrong.')
    } finally {
      setActing(false)
    }
  }

  const hasOwnForActiveTab = activeTab !== 'All' && ownRequests.length > 0
  const otherRequests = requests.filter(request =>
    !ownRequests.some(own =>
      own.itemName === request.itemName && request.inGameName === ign,
    ),
  )

  return (
    <div className="gr-section">
      <h2 className="gr-section__title">Group Requests</h2>

      <div className="gr-tabs">
        {(['All', ...REFINEMENTS] as GrTab[]).map(t => (
          <button
            key={t}
            className={`gr-tab${activeTab === t ? ' gr-tab--active' : ''}`}
            onClick={() => onTabChange(t)}
            type="button"
          >
            {t}
          </button>
        ))}
      </div>

      {!email ? (
        <p className="gr-notice">
          <Link to="/login" className="gr-link">Log in</Link> to request a group.
        </p>
      ) : !ign ? (
        <p className="gr-notice">
          Set your <Link to="/settings" className="gr-link">in-game name</Link> in Settings to request a group.
        </p>
      ) : activeTab === 'All' ? (
        <p className="gr-notice">Select a refinement tab to create a group request.</p>
      ) : !hasOwnForActiveTab ? (
        <button className="gr-btn gr-btn--create" onClick={handleCreate} disabled={acting || loadingList}>
          {acting ? 'Requesting…' : `⚑ Request Group (${activeTab})`}
        </button>
      ) : null}

      {loadingList && <p className="gr-notice">Loading…</p>}

      {!loadingList && email && ign && ownRequests.length > 0 && (
        <div className="gr-request-subsection">
          <h3 className="gr-request-subsection__title">{ign}&apos;s requests:</h3>
          <ul className="gr-list">
            {ownRequests.map(request => {
              const refinement = extractRefinement(request.itemName)

              return (
                <li key={request.itemName} className="gr-item gr-item--own">
                  <img
                    className="gr-item__img"
                    src={relicImgSrc(relicType, refinement)}
                    alt=""
                    onError={(e) => { (e.currentTarget as HTMLImageElement).style.display = 'none' }}
                  />
                  <span className="gr-item__name">{ign}</span>
                  <span className="gr-item__refinement">{refinement}</span>
                  <button
                    className="gr-btn gr-btn--remove gr-btn--inline"
                    onClick={() => handleDelete(request.itemName)}
                    disabled={acting}
                    type="button"
                  >
                    {acting ? 'Removing…' : 'Remove'}
                  </button>
                </li>
              )
            })}
          </ul>
        </div>
      )}

      {!loadingList && otherRequests.length > 0 && (
        <div className="gr-request-subsection">
          {email && ign && ownRequests.length > 0 && <div className="gr-divider" />}
          <h3 className="gr-request-subsection__title">Other requests:</h3>
          <ul className="gr-list">
            {otherRequests.map((r, i) => {
              const isContacted = contactedIndex === i
              const refinement = extractRefinement(r.itemName)
              const contactMsg = `/w ${r.inGameName} Hi, I want to join your group for [${itemName} Relic] ${refinement} (WarframeLFG)`
              return (
                <li key={i} className="gr-item">
                  {isContacted ? (
                    <input
                      className="gr-contact-input"
                      type="text"
                      defaultValue={contactMsg}
                      readOnly
                      autoFocus
                      onFocus={e => e.currentTarget.select()}
                    />
                  ) : (
                    <>
                      <img
                        className="gr-item__img"
                        src={relicImgSrc(relicType, refinement)}
                        alt=""
                        onError={(e) => { (e.currentTarget as HTMLImageElement).style.display = 'none' }}
                      />
                      <span className="gr-item__name">{r.inGameName}</span>
                      <span className="gr-item__refinement">{refinement}</span>
                    </>
                  )}
                  <button
                    className={`gr-btn gr-btn--inline${isContacted ? ' gr-btn--close' : ' gr-btn--contact'}`}
                    onClick={() => setContactedIndex(isContacted ? null : i)}
                    type="button"
                  >
                    {isContacted ? 'Close' : 'Contact'}
                  </button>
                </li>
              )
            })}
          </ul>
        </div>
      )}
    </div>
  )
}

export default function RelicDetailPage() {
  const { name } = useParams<{ name: string }>()
  const [relic, setRelic] = useState<Relic | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [activeTab, setActiveTab] = useState<GrTab>('All')
  const [refinementImageIndex, setRefinementImageIndex] = useState(0)
  const isLoggedIn = Boolean(localStorage.getItem('user_email'))
  const navigate = useNavigate()

  useEffect(() => {
    if (!name) return
    fetchRelicByName(name)
      .then(setRelic)
      .catch((err: Error) => setError(err.message))
      .finally(() => setLoading(false))
  }, [name])

  useEffect(() => {
    setRefinementImageIndex(0)
    if (activeTab !== 'All') return

    const intervalId = window.setInterval(() => {
      setRefinementImageIndex(current => (current + 1) % REFINEMENTS.length)
    }, 1800)

    return () => window.clearInterval(intervalId)
  }, [activeTab])

  const displayedRefinement: Refinement = activeTab === 'All'
    ? REFINEMENTS[refinementImageIndex]
    : activeTab
  const rewardQuality: Refinement = activeTab === 'All' ? 'Intact' : activeTab

  return (
    <div className="detail-page detail-page--relic">
      <button className="back-btn" onClick={() => navigate('/relics')}>← Back to Relics</button>

      {loading && <p className="status-msg">Loading…</p>}
      {error && <p className="status-msg status-msg--error">Error: {error}</p>}

      {relic && (
        <>
          <div className="relic-detail-layout">
            <div className="detail-card">
              <img
                className="detail-card__img"
                src={relicImgSrc(relic.relicType, displayedRefinement)}
                alt={`${relic.name} ${displayedRefinement}`}
                onError={(e) => { (e.currentTarget as HTMLImageElement).style.display = 'none' }}
              />
              <h1 className="detail-card__name">{relic.name}</h1>
              {isLoggedIn && (
                <FavoriteButton
                  itemName={activeTab === 'All' ? null : refinedName(relic.name, activeTab)}
                  disabledMessage="Select a refinement before adding this relic to favorites."
                />
              )}
              <p className="detail-card__desc">
                An artifact containing Orokin secrets. It can only be opened through the power of the Void.
              </p>
            </div>

            <RelicRewards
              drops={relic.drops ?? []}
              quality={rewardQuality}
              onQualityChange={setActiveTab}
            />
          </div>

          <GroupRequestSection
            itemName={relic.name}
            relicType={relic.relicType}
            activeTab={activeTab}
            onTabChange={setActiveTab}
          />
        </>
      )}
    </div>
  )
}
