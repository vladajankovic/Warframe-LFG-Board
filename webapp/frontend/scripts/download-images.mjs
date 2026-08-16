import { mkdirSync, writeFileSync, statSync } from 'fs'
import { join, dirname } from 'path'
import { fileURLToPath } from 'url'

const __dirname = dirname(fileURLToPath(import.meta.url))
const PUBLIC = join(__dirname, '..', 'public', 'images')
const WIKI_BASE = 'https://wiki.warframe.com/images'
const BACKEND = 'http://localhost:8002/v1/itemstore'

async function downloadImage(url, dest) {
  const response = await fetch(url)
  if (!response.ok) throw new Error(`HTTP ${response.status}`)
  const arrayBuffer = await response.arrayBuffer()
  writeFileSync(dest, Buffer.from(arrayBuffer))
  const info = statSync(dest)
  if (info.size < 1000) throw new Error(`Too small (${info.size} bytes)`)
  return info.size
}

async function downloadAll(items, destDir, getName) {
  mkdirSync(destDir, { recursive: true })
  let ok = 0, fail = 0
  for (const item of items) {
    const name = getName(item)
    const url = `${WIKI_BASE}/${name}.png`
    const dest = join(destDir, `${name}.png`)
    try {
      const size = await downloadImage(url, dest)
      console.log(`  ✓ ${name} (${(size / 1024).toFixed(0)} KB)`)
      ok++
    } catch (e) {
      console.warn(`  ✗ ${name}: ${e.message}`)
      fail++
    }
  }
  return { ok, fail }
}

function blueprintImageName(itemName) {
  return itemName
    .trim()
    .replace(/\s+Blueprint$/i, '')
    .replace(/\s+/g, '')
}

async function fetchUniqueRelicDrops() {
  const relicsResponse = await fetch(`${BACKEND}/relics`)
  if (!relicsResponse.ok) {
    throw new Error(`Could not fetch relics (HTTP ${relicsResponse.status})`)
  }

  const relics = await relicsResponse.json()
  const uniqueDrops = new Map()

  for (const relic of relics) {
    const detailResponse = await fetch(
      `${BACKEND}/relics/relic/${encodeURIComponent(relic.name)}`,
    )
    if (!detailResponse.ok) {
      throw new Error(
        `Could not fetch drops for ${relic.name} (HTTP ${detailResponse.status})`,
      )
    }

    const relicDetail = await detailResponse.json()
    for (const drop of relicDetail.drops ?? []) {
      const itemName = drop.itemName?.trim()
      if (!itemName) continue
      uniqueDrops.set(itemName.toLocaleLowerCase('en-US'), { itemName })
    }
  }

  return [...uniqueDrops.values()].sort((left, right) =>
    left.itemName.localeCompare(right.itemName),
  )
}

// ── Relic images ──────────────────────────────────────────────────────────────
const RELIC_TYPES = ['Lith', 'Meso', 'Neo', 'Axi', 'Requiem']
const RELIC_QUALITIES = ['Intact', 'Exceptional', 'Flawless', 'Radiant']
const relicImages = RELIC_TYPES.flatMap(t => RELIC_QUALITIES.map(q => `${t}Relic${q}`))

// ── Resource images ───────────────────────────────────────────────────────────
console.log('\nFetching resource list from backend…')
let resources
try {
  const res = await fetch(`${BACKEND}/resources`)
  if (!res.ok) throw new Error(`HTTP ${res.status}`)
  resources = await res.json()
  console.log(`  Found ${resources.length} resources`)
} catch {
  console.error('  Could not reach backend at', BACKEND)
  console.error('  Start the backend first, then re-run this script.')
  process.exit(1)
}

console.log('\nFetching unique relic drops from backend…')
let relicDrops
try {
  relicDrops = await fetchUniqueRelicDrops()
  console.log(`  Found ${relicDrops.length} unique relic drops`)
} catch (error) {
  console.error(`  ${error.message}`)
  console.error('  Start the backend and seed relic drops before re-running this script.')
  process.exit(1)
}

// ── Download ──────────────────────────────────────────────────────────────────
console.log('\nDownloading relic images…')
const relicResult = await downloadAll(
  relicImages,
  join(PUBLIC, 'relics'),
  name => name,
)

console.log('\nDownloading resource images…')
const resourceResult = await downloadAll(
  resources,
  join(PUBLIC, 'resources'),
  r => r.name.replace(/\s+/g, ''),
)

console.log('\nDownloading relic drop blueprint images…')
const relicDropResult = await downloadAll(
  relicDrops,
  join(PUBLIC, 'drops'),
  drop => blueprintImageName(drop.itemName),
)

console.log('\n── Summary ──────────────────────────────────────')
console.log(`Relics   : ${relicResult.ok} OK  ${relicResult.fail} failed`)
console.log(`Resources: ${resourceResult.ok} OK  ${resourceResult.fail} failed`)
console.log(`Drops    : ${relicDropResult.ok} OK  ${relicDropResult.fail} failed`)
