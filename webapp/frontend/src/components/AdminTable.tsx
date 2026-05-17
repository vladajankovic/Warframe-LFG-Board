import { useState } from 'react'
import type { ReactNode } from 'react'

export interface ColumnDef<T> {
  header: string
  cell: (row: T) => ReactNode
  /** If provided, this column is sortable using this key extractor */
  sortKey?: (row: T) => string | number
  className?: string
}

interface AdminTableProps<T> {
  columns: ColumnDef<T>[]
  rows: T[]
  loading?: boolean
  search: string
  onSearchChange: (v: string) => void
  searchPlaceholder?: string
  onAdd: () => void
  addLabel?: string
  onEdit: (row: T) => void
  onDelete: (row: T) => void
  getRowKey: (row: T) => string | number
  emptyMessage?: string
}

export function AdminTable<T extends object>({
  columns,
  rows,
  loading,
  search,
  onSearchChange,
  searchPlaceholder = 'Search…',
  onAdd,
  addLabel = '+ Add',
  onEdit,
  onDelete,
  getRowKey,
  emptyMessage = 'No items found.',
}: AdminTableProps<T>) {
  const [sortCol, setSortCol] = useState<string | null>(null)
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('asc')

  function handleSortClick(col: ColumnDef<T>) {
    if (!col.sortKey) return
    if (sortCol === col.header) {
      setSortDir(d => d === 'asc' ? 'desc' : 'asc')
    } else {
      setSortCol(col.header)
      setSortDir('asc')
    }
  }

  const sortedRows = (() => {
    const col = columns.find(c => c.header === sortCol && c.sortKey)
    if (!col?.sortKey) return rows
    return [...rows].sort((a, b) => {
      const av = col.sortKey!(a)
      const bv = col.sortKey!(b)
      const cmp = av < bv ? -1 : av > bv ? 1 : 0
      return sortDir === 'asc' ? cmp : -cmp
    })
  })()

  return (
    <div className="admin-section">
      <div className="admin-section-header">
        <input
          className="admin-search"
          placeholder={searchPlaceholder}
          value={search}
          onChange={e => onSearchChange(e.target.value)}
        />
        <button className="admin-add-btn" onClick={onAdd} type="button">
          {addLabel}
        </button>
      </div>

      {loading ? (
        <p className="admin-loading">Loading…</p>
      ) : (
        <div className="admin-table-wrapper">
          <table className="admin-table">
            <thead>
              <tr>
                {columns.map(col => (
                  <th
                    key={col.header}
                    className={col.sortKey ? 'admin-table__th--sortable' : undefined}
                    onClick={() => handleSortClick(col)}
                  >
                    {col.header}
                    {col.sortKey && (
                      <span className="admin-table__sort-indicator">
                        {sortCol === col.header ? (sortDir === 'asc' ? ' ▲' : ' ▼') : ' ⇅'}
                      </span>
                    )}
                  </th>
                ))}
                <th className="admin-table__actions-col">Actions</th>
              </tr>
            </thead>
            <tbody>
              {sortedRows.length === 0 ? (
                <tr>
                  <td colSpan={columns.length + 1} className="admin-table__empty">
                    {emptyMessage}
                  </td>
                </tr>
              ) : (
                sortedRows.map(row => (
                  <tr key={getRowKey(row)}>
                    {columns.map(col => (
                      <td key={col.header} className={col.className}>
                        {col.cell(row)}
                      </td>
                    ))}
                    <td className="admin-table__actions">
                      <button
                        className="admin-action-btn admin-action-btn--edit"
                        onClick={() => onEdit(row)}
                        type="button"
                      >
                        Edit
                      </button>
                      <button
                        className="admin-action-btn admin-action-btn--delete"
                        onClick={() => onDelete(row)}
                        type="button"
                      >
                        Delete
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
