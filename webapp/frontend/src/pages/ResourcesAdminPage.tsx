import { useState, useEffect, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import {
  fetchResources,
  createResource,
  updateResource,
  deleteResource,
  type Resource,
  type AddResourcePayload,
} from '../api/itemsApi'
import { notifyError } from '../api/apiUtils'
import { AdminTable, type ColumnDef } from '../components/AdminTable'
import { DeleteModal } from '../components/DeleteModal'

// ── Resource modal ────────────────────────────────────────────────────────────

interface ResourceModalProps {
  initial?: Resource | null
  onClose: () => void
  onSaved: () => void
}

function ResourceModal({ initial, onClose, onSaved }: ResourceModalProps) {
  const isEdit = !!initial
  const [name, setName] = useState(initial?.name ?? '')
  const [description, setDescription] = useState(initial?.description ?? '')
  const [region, setRegion] = useState(initial?.region ?? '')
  const [category, setCategory] = useState(initial?.category ?? '')
  const [group, setGroup] = useState(initial?.group ?? '')
  const [saving, setSaving] = useState(false)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    const payload: AddResourcePayload = { name, description, region, category, group }
    try {
      if (isEdit) {
        await updateResource(payload)
      } else {
        await createResource(payload)
      }
      onSaved()
    } catch (err) {
      notifyError(err instanceof Error ? err.message : 'Something went wrong.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="modal-overlay">
      <div className="modal-card" onClick={e => e.stopPropagation()}>
        <h2 className="modal-title">{isEdit ? 'Edit Resource' : 'Add Resource'}</h2>
        <form className="modal-form" onSubmit={handleSubmit}>
          <label className="modal-label">
            Name
            <input
              className="modal-input"
              value={name}
              onChange={e => setName(e.target.value)}
              required
              disabled={isEdit}
              placeholder="e.g. Ferrite"
            />
          </label>
          <label className="modal-label">
            Description
            <textarea
              className="modal-input modal-textarea"
              value={description}
              onChange={e => setDescription(e.target.value)}
              rows={3}
              placeholder="Short description…"
            />
          </label>
          <label className="modal-label">
            Region
            <input
              className="modal-input"
              value={region}
              onChange={e => setRegion(e.target.value)}
              placeholder="e.g. Plains of Eidolon"
            />
          </label>
          <label className="modal-label">
            Category
            <input
              className="modal-input"
              value={category}
              onChange={e => setCategory(e.target.value)}
              placeholder="e.g. Minerals"
            />
          </label>
          <label className="modal-label">
            Group
            <input
              className="modal-input"
              value={group}
              onChange={e => setGroup(e.target.value)}
              placeholder="e.g. Common"
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
    </div>
  )
}

// ── Columns ───────────────────────────────────────────────────────────────────

const COLUMNS: ColumnDef<Resource>[] = [
  { header: 'Name', cell: r => r.name, sortKey: r => r.name },
  { header: 'Region', cell: r => r.region ?? '—' },
  { header: 'Category', cell: r => r.category ?? '—' },
  { header: 'Group', cell: r => r.group ?? '—' },
  { header: 'Description', cell: r => r.description ?? '—', className: 'admin-table__desc' },
]

// ── Page ──────────────────────────────────────────────────────────────────────

export default function ResourcesAdminPage() {
  const [resources, setResources] = useState<Resource[]>([])
  const [loading, setLoading] = useState(true)
  const [search, setSearch] = useState('')

  const [modal, setModal] = useState<{ mode: 'add' | 'edit'; resource?: Resource } | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<Resource | null>(null)

  async function load() {
    setLoading(true)
    try {
      setResources(await fetchResources())
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load() }, [])

  const filtered = resources.filter(r =>
    r.name.toLowerCase().includes(search.toLowerCase())
  )

  return (
    <div className="admin-subpage">
      <div className="admin-subpage__header">
        <Link to="/admin" className="admin-subpage__back">← Admin Panel</Link>
        <h1 className="admin-subpage__title">Resources</h1>
      </div>

      <AdminTable
        columns={COLUMNS}
        rows={filtered}
        loading={loading}
        search={search}
        onSearchChange={setSearch}
        searchPlaceholder="Search resources…"
        onAdd={() => setModal({ mode: 'add' })}
        addLabel="+ Add Resource"
        onEdit={resource => setModal({ mode: 'edit', resource })}
        onDelete={resource => setDeleteTarget(resource)}
        getRowKey={r => r.id}
        emptyMessage="No resources found."
      />

      {modal && (
        <ResourceModal
          initial={modal.mode === 'edit' ? modal.resource : null}
          onClose={() => setModal(null)}
          onSaved={() => { setModal(null); load() }}
        />
      )}

      {deleteTarget && (
        <DeleteModal
          itemName={deleteTarget.name}
          onClose={() => setDeleteTarget(null)}
          onConfirm={async () => {
            await deleteResource(deleteTarget.name)
            setDeleteTarget(null)
            load()
          }}
        />
      )}
    </div>
  )
}
