import { useId, useState } from 'react'
import { createPortal } from 'react-dom'

interface DeleteModalProps {
  itemName: string
  onClose: () => void
  onConfirm: (password?: string) => Promise<void>
  requirePassword?: boolean
}

export function DeleteModal({ itemName, onClose, onConfirm, requirePassword }: DeleteModalProps) {
  const titleId = useId()
  const [deleting, setDeleting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [password, setPassword] = useState('')

  async function handleConfirm() {
    if (requirePassword && !password) {
      setError('Please enter your password to confirm.')
      return
    }
    setDeleting(true)
    setError(null)
    try {
      await onConfirm(requirePassword ? password : undefined)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Delete failed.')
      setDeleting(false)
    }
  }

  return createPortal(
    <div className="modal-overlay" role="dialog" aria-modal="true" aria-labelledby={titleId}>
      <div className="modal-card modal-card--sm" onClick={e => e.stopPropagation()}>
        <h2 id={titleId} className="modal-title">Delete "{itemName}"?</h2>
        <p className="modal-body-text">This action cannot be undone.</p>
        {requirePassword && (
          <label className="settings-label" style={{ marginTop: '0.75rem' }}>
            Confirm your password
            <input
              className="settings-input"
              type="password"
              value={password}
              onChange={e => setPassword(e.target.value)}
              autoComplete="current-password"
              placeholder="••••••••"
            />
          </label>
        )}
        {error && <p className="modal-error">{error}</p>}
        <div className="modal-actions">
          <button type="button" className="modal-btn modal-btn--cancel" onClick={onClose}>
            Cancel
          </button>
          <button
            type="button"
            className="modal-btn modal-btn--danger"
            disabled={deleting}
            onClick={handleConfirm}
          >
            {deleting ? 'Deleting…' : 'Delete'}
          </button>
        </div>
      </div>
    </div>,
    document.body,
  )
}
