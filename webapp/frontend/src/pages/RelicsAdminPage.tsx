import { useState, useEffect, type FormEvent } from 'react'
import { createPortal } from 'react-dom'
import { Link } from 'react-router-dom'
import {
  fetchRelics,
  createRelic,
  updateRelic,
  deleteRelic,
  type Relic,
  type AddRelicPayload,
} from '../api/itemsApi'
import { notifyError } from '../api/apiUtils'
import { AdminTable, type ColumnDef } from '../components/AdminTable'
import { DeleteModal } from '../components/DeleteModal'

const RELIC_TYPES: { value: number; label: string }[] = [
  { value: 0, label: 'Unknown' },
  { value: 1, label: 'Lith' },
  { value: 2, label: 'Meso' },
  { value: 3, label: 'Neo' },
  { value: 4, label: 'Axi' },
  { value: 5, label: 'Requiem' },
  { value: 6, label: 'Vanguard' },
]

function relicTypeName(type: number | string): string {
  if (typeof type === 'string') {
    return RELIC_TYPES.find(t => t.label.toLowerCase() === type.toLowerCase())?.label ?? type
  }
  return RELIC_TYPES.find(t => t.value === type)?.label ?? String(type)
}

// ── Relic modal ───────────────────────────────────────────────────────────────

interface RelicModalProps {
  initial?: Relic | null
  onClose: () => void
  onSaved: () => void
}

function RelicModal({ initial, onClose, onSaved }: RelicModalProps) {
  const isEdit = !!initial
  const [name, setName] = useState(initial?.name ?? '')
  const [type, setType] = useState<number>(() => {
    if (!initial) return 1
    if (typeof initial.relicType === 'string') {
      return RELIC_TYPES.find(t => t.label.toLowerCase() === initial.relicType.toLowerCase())?.value ?? 1
    }
    return initial.relicType as unknown as number
  })
  const [code, setCode] = useState(initial?.relicCode ?? '')
  const [saving, setSaving] = useState(false)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    const payload: AddRelicPayload = { name, type, code }
    try {
      if (isEdit) {
        await updateRelic(payload)
      } else {
        await createRelic(payload)
      }
      onSaved()
    } catch (err) {
      notifyError(err instanceof Error ? err.message : 'Something went wrong.')
    } finally {
      setSaving(false)
    }
  }

  return createPortal(
    <div className="modal-overlay" role="dialog" aria-modal="true" aria-labelledby="relic-modal-title">
      <div className="modal-card" onClick={e => e.stopPropagation()}>
        <h2 id="relic-modal-title" className="modal-title">{isEdit ? 'Edit Relic' : 'Add Relic'}</h2>
        <form className="modal-form" onSubmit={handleSubmit}>
          <label className="modal-label">
            Name
            <input
              className="modal-input"
              value={name}
              onChange={e => setName(e.target.value)}
              required
              disabled={isEdit}
              placeholder="e.g. Lith A1 Intact"
            />
          </label>
          <label className="modal-label">
            Type
            <select
              className="modal-input"
              value={type}
              onChange={e => setType(Number(e.target.value))}
              required
            >
              {RELIC_TYPES.filter(t => t.value !== 0).map(t => (
                <option key={t.value} value={t.value}>{t.label}</option>
              ))}
            </select>
          </label>
          <label className="modal-label">
            Code
            <input
              className="modal-input"
              value={code}
              onChange={e => setCode(e.target.value)}
              required
              placeholder="e.g. A1"
            />
          </label>
          <div className="modal-actions">
            <button type="button" className="modal-btn modal-btn--cancel" onClick={onClose}>Cancel</button>
            <button type="submit" className="modal-btn modal-btn--save" disabled={saving}>
              {saving ? 'Saving…' : 'Save'}
            </button>
          </div>
        </form>
      </div>
    </div>,
    document.body,
  )
}

// ── Columns ───────────────────────────────────────────────────────────────────

const COLUMNS: ColumnDef<Relic>[] = [
  { header: 'Name', cell: r => r.name, sortKey: r => r.name },
  { header: 'Type', cell: r => relicTypeName(r.relicType) },
  { header: 'Code', cell: r => r.relicCode },
]

// ── Page ──────────────────────────────────────────────────────────────────────

export default function RelicsAdminPage() {
  const [relics, setRelics] = useState<Relic[]>([])
  const [loading, setLoading] = useState(true)
  const [search, setSearch] = useState('')

  const [modal, setModal] = useState<{ mode: 'add' | 'edit'; relic?: Relic } | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<Relic | null>(null)

  async function load() {
    setLoading(true)
    try {
      setRelics(await fetchRelics())
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load() }, [])

  const filtered = relics.filter(r =>
    r.name.toLowerCase().includes(search.toLowerCase()) ||
    r.relicCode.toLowerCase().includes(search.toLowerCase())
  )

  return (
    <div className="admin-subpage">
      <div className="admin-subpage__header">
        <Link to="/admin" className="admin-subpage__back">← Admin Panel</Link>
        <h1 className="admin-subpage__title">Relics</h1>
      </div>

      <AdminTable
        columns={COLUMNS}
        rows={filtered}
        loading={loading}
        search={search}
        onSearchChange={setSearch}
        searchPlaceholder="Search relics…"
        onAdd={() => setModal({ mode: 'add' })}
        addLabel="+ Add Relic"
        onEdit={relic => setModal({ mode: 'edit', relic })}
        onDelete={relic => setDeleteTarget(relic)}
        getRowKey={r => r.id}
        emptyMessage="No relics found."
      />

      {modal && (
        <RelicModal
          initial={modal.mode === 'edit' ? modal.relic : null}
          onClose={() => setModal(null)}
          onSaved={() => { setModal(null); load() }}
        />
      )}

      {deleteTarget && (
        <DeleteModal
          itemName={deleteTarget.name}
          onClose={() => setDeleteTarget(null)}
          onConfirm={async () => {
            await deleteRelic(deleteTarget.name)
            setDeleteTarget(null)
            load()
          }}
        />
      )}
    </div>
  )
}
