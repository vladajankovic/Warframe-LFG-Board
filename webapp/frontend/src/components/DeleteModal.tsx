import { useState } from 'react'

interface DeleteModalProps {
  itemName: string
  onClose: () => void
  onConfirm: () => Promise<void>
}

export function DeleteModal({ itemName, onClose, onConfirm }: DeleteModalProps) {
  const [deleting, setDeleting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleConfirm() {
    setDeleting(true)
    setError(null)
    try {
      await onConfirm()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Delete failed.')
      setDeleting(false)
    }
  }

  return (
    <div className="modal-overlay">
      <div className="modal-card modal-card--sm" onClick={e => e.stopPropagation()}>
        <h2 className="modal-title">Delete "{itemName}"?</h2>
        <p className="modal-body-text">This action cannot be undone.</p>
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
    </div>
  )
}
