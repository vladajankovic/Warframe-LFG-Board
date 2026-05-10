import { mkdirSync, writeFileSync, statSync } from 'fs'
import { join, dirname } from 'path'
import { fileURLToPath } from 'url'

const __dirname = dirname(fileURLToPath(import.meta.url))
const PUBLIC = join(__dirname, '..', 'public', 'images')
const WIKI_BASE = 'https://wiki.warframe.com/images'
const BACKEND = 'http://localhost:8080/v1/itemstore'

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

// ── Relic images ──────────────────────────────────────────────────────────────
const RELIC_TYPES = ['Lith', 'Meso', 'Neo', 'Axi', 'Requiem']
const RELIC_QUALITIES = ['Intact', 'Exceptional', 'Flawless', 'Radiant']
const relicImages = RELIC_TYPES.flatMap(t => RELIC_QUALITIES.map(q => `${t}Relic${q}`))

// ── Resource images ───────────────────────────────────────────────────────────
console.log('\nFetching resource list from backend…')
let resources
try {
  const res = await fetch(`${BACKEND}/resources`)
  resources = await res.json()
  console.log(`  Found ${resources.length} resources`)
} catch {
  console.error('  Could not reach backend at', BACKEND)
  console.error('  Start the backend first, then re-run this script.')
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

console.log('\n── Summary ──────────────────────────────────────')
console.log(`Relics   : ${relicResult.ok} OK  ${relicResult.fail} failed`)
console.log(`Resources: ${resourceResult.ok} OK  ${resourceResult.fail} failed`)
