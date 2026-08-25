import { useState, useEffect, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import {
  fetchAllUsers,
  updateUser,
  deleteUser,
  type User,
  type UpdateUserPayload,
} from '../api/usersApi'
import { notifyError } from '../api/apiUtils'
import { AdminTable, type ColumnDef } from '../components/AdminTable'
import { DeleteModal } from '../components/DeleteModal'

const ROLES = ['User', 'Admin']

function formatSubscriptionExpiry(value: string | null): string {
  if (!value) return '—'
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : date.toLocaleDateString()
}

// ── Edit modal ────────────────────────────────────────────────────────────────

interface UserModalProps {
  initial: User
  onClose: () => void
  onSaved: () => void
}

function UserModal({ initial, onClose, onSaved }: UserModalProps) {
  const [role, setRole] = useState(initial.role)
  const [inGameName, setInGameName] = useState(initial.inGameName)
  const [password, setPassword] = useState('')
  const [saving, setSaving] = useState(false)

  const subscriptionExpiry = formatSubscriptionExpiry(initial.subscriptionExpiryDate)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    const payload: UpdateUserPayload = {
      email: initial.email,
      role,
      inGameName,
      ...(password ? { password } : {}),
    }
    try {
      await updateUser(payload)
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
        <h2 className="modal-title">Edit User</h2>
        <form className="modal-form" onSubmit={handleSubmit}>
          <label className="modal-label">
            User ID
            <input className="modal-input" value={initial.id} disabled />
          </label>
          <label className="modal-label">
            Email
            <input className="modal-input" value={initial.email} disabled />
          </label>
          <label className="modal-label">
            In-Game Name
            <input
              className="modal-input"
              value={inGameName}
              onChange={e => setInGameName(e.target.value)}
              placeholder="In-game name"
            />
          </label>
          <label className="modal-label">
            Role
            <select
              className="modal-input"
              value={role}
              onChange={e => setRole(e.target.value)}
            >
              {ROLES.map(r => (
                <option key={r} value={r}>{r}</option>
              ))}
            </select>
          </label>
          <label className="modal-label">
            Subscription Status
            <input className="modal-input" value={initial.subscriptionStatus} disabled />
          </label>
          <label className="modal-label">
            Subscription Expiry Date
            <input className="modal-input" value={subscriptionExpiry} disabled />
          </label>
          <label className="modal-label">
            New Password
            <span className="modal-label__hint"> (leave blank to keep current)</span>
            <input
              className="modal-input"
              type="password"
              value={password}
              onChange={e => setPassword(e.target.value)}
              placeholder="••••••••"
              autoComplete="new-password"
            />
          </label>
          <div className="modal-actions">
            <button type="button" className="modal-btn modal-btn--cancel" onClick={onClose}>
              Cancel
            </button>
            <button type="submit" className="modal-btn modal-btn--save" disabled={saving}>
              {saving ? 'Saving…' : 'Save'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

// ── Main page ─────────────────────────────────────────────────────────────────

const COLUMNS: ColumnDef<User>[] = [
  {
    header: 'ID',
    cell: u => u.id,
    sortKey: u => u.id,
    className: 'admin-table__id',
  },
  { header: 'Email', cell: u => u.email, sortKey: u => u.email },
  { header: 'In-Game Name', cell: u => u.inGameName, sortKey: u => u.inGameName },
  { header: 'Role', cell: u => u.role, sortKey: u => u.role },
  {
    header: 'Subscription',
    cell: u => (
      <span className={`settings-subscription-badge settings-subscription-badge--${u.subscriptionStatus.toLowerCase()}`}>
        {u.subscriptionStatus}
      </span>
    ),
    sortKey: u => u.subscriptionStatus,
  },
  {
    header: 'Expiry Date',
    cell: u => formatSubscriptionExpiry(u.subscriptionExpiryDate),
    sortKey: u => u.subscriptionExpiryDate ?? '',
  },
  {
    header: 'Created',
    cell: u => new Date(u.createdAt).toLocaleDateString(),
    sortKey: u => u.createdAt,
  },
]

export default function UsersAdminPage() {
  const [users, setUsers] = useState<User[]>([])
  const [loading, setLoading] = useState(true)
  const [search, setSearch] = useState('')
  const [editTarget, setEditTarget] = useState<User | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<User | null>(null)

  async function load() {
    setLoading(true)
    try {
      const data = await fetchAllUsers()
      setUsers(data)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load() }, [])

  const filtered = users.filter(u =>
    u.id.toLowerCase().includes(search.toLowerCase()) ||
    u.email.toLowerCase().includes(search.toLowerCase()) ||
    u.inGameName.toLowerCase().includes(search.toLowerCase()) ||
    u.role.toLowerCase().includes(search.toLowerCase()) ||
    u.subscriptionStatus.toLowerCase().includes(search.toLowerCase())
  )

  async function handleDelete() {
    if (!deleteTarget) return
    await deleteUser(deleteTarget.email)
    setDeleteTarget(null)
    load()
  }

  return (
    <div className="admin-subpage admin-subpage--users">
      <div className="admin-subpage__header">
        <Link to="/admin" className="admin-subpage__back">← Admin Panel</Link>
        <h1 className="admin-subpage__title">Users</h1>
      </div>

      <AdminTable
        columns={COLUMNS}
        rows={filtered}
        loading={loading}
        search={search}
        onSearchChange={setSearch}
        searchPlaceholder="Search by ID, email, in-game name, role or subscription…"
        getRowKey={u => u.id}
        emptyMessage="No users found."
        onEdit={u => setEditTarget(u)}
        onDelete={u => setDeleteTarget(u)}
      />

      {editTarget && (
        <UserModal
          initial={editTarget}
          onClose={() => setEditTarget(null)}
          onSaved={() => { setEditTarget(null); load() }}
        />
      )}

      {deleteTarget && (
        <DeleteModal
          itemName={deleteTarget.email}
          onClose={() => setDeleteTarget(null)}
          onConfirm={handleDelete}
        />
      )}
    </div>
  )
}
