import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import { fetchAuditLogs, type AuditLog } from '../api/auditApi'
import { notifyError } from '../api/apiUtils'
import { AdminTable, type ColumnDef } from '../components/AdminTable'

const COLUMNS: ColumnDef<AuditLog>[] = [
  { header: 'Date', cell: l => new Date(l.dateTime).toLocaleString(), sortKey: l => l.dateTime },
  { header: 'Service', cell: l => l.service, sortKey: l => l.service },
  { header: 'Action', cell: l => l.action, sortKey: l => l.action },
  { header: 'User', cell: l => l.user, sortKey: l => l.user },
  { header: 'Details', cell: l => l.details ?? '—', className: 'admin-table__desc' },
]

export default function LogsAdminPage() {
  const [logs, setLogs] = useState<AuditLog[]>([])
  const [loading, setLoading] = useState(true)
  const [search, setSearch] = useState('')

  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [service, setService] = useState('')
  const [action, setAction] = useState('')
  const [user, setUser] = useState('')

  async function load() {
    setLoading(true)
    try {
      const fromParam = from ? new Date(from).toISOString() : undefined
      let toParam: string | undefined = undefined
      if (to) {
        const d = new Date(to)
        d.setHours(23, 59, 59, 999)
        toParam = d.toISOString()
      }
      const data = await fetchAuditLogs(fromParam, toParam, service || undefined, action || undefined, user || undefined)
      setLogs(data)
    } catch (err) {
      notifyError(err instanceof Error ? err.message : 'Failed to load logs')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load() }, [])

  const filtered = logs.filter(l =>
    search === '' ||
    l.service.toLowerCase().includes(search.toLowerCase()) ||
    l.action.toLowerCase().includes(search.toLowerCase()) ||
    l.user.toLowerCase().includes(search.toLowerCase()) ||
    l.details.toLowerCase().includes(search.toLowerCase())
  )

  function handleFilter(e: React.FormEvent) {
    e.preventDefault()
    load()
  }

  function clearFilters() {
    setFrom('')
    setTo('')
    setService('')
    setAction('')
    setUser('')
    load()
  }

  return (
    <div className="admin-subpage">
      <div className="admin-subpage__header">
        <Link to="/admin" className="admin-subpage__back">← Admin Panel</Link>
        <h1 className="admin-subpage__title">Audit Logs</h1>
      </div>

      <form className="admin-gr-filters" onSubmit={handleFilter}>
        <label className="admin-filters__label">
          From
          <input className="admin-search" type="date" value={from} onChange={e => setFrom(e.target.value)} />
        </label>
        <label className="admin-filters__label">
          To
          <input className="admin-search" type="date" value={to} onChange={e => setTo(e.target.value)} />
        </label>
        <label className="admin-filters__label">
          Service
          <input className="admin-search" value={service} onChange={e => setService(e.target.value)} placeholder="Service name" />
        </label>
        <label className="admin-filters__label">
          Action
          <input className="admin-search" value={action} onChange={e => setAction(e.target.value)} placeholder="Action" />
        </label>
        <label className="admin-filters__label">
          Username
          <input className="admin-search" value={user} onChange={e => setUser(e.target.value)} placeholder="Username" />
        </label>
        <div className="admin-filters__actions">
          <button className="admin-add-btn" type="submit">Filter</button>
          <button className="admin-add-btn" type="button" onClick={clearFilters}>Clear</button>
        </div>
      </form>

      <AdminTable
        columns={COLUMNS}
        rows={filtered}
        loading={loading}
        search={search}
        onSearchChange={setSearch}
        searchPlaceholder="Search logs"
        getRowKey={l => l.dateTime + l.service + l.action + l.user}
        emptyMessage="No logs found."
      />
    </div>
  )
}
