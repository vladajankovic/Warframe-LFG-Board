import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { changePassword, deleteOwnAccount } from '../api/usersApi'
import { DeleteModal } from '../components/DeleteModal'

export default function SettingsPage() {
  const email = localStorage.getItem('user_email') ?? ''
  const navigate = useNavigate()

  // ── Change password ─────────────────────────────────────
  const [oldPassword, setOldPassword] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [pwError, setPwError] = useState<string | null>(null)
  const [pwSuccess, setPwSuccess] = useState(false)
  const [pwSaving, setPwSaving] = useState(false)

  async function handlePasswordChange(e: FormEvent) {
    e.preventDefault()
    setPwError(null)
    setPwSuccess(false)
    if (password !== confirmPassword) {
      setPwError('Passwords do not match.')
      return
    }
    setPwSaving(true)
    try {
      await changePassword(email, oldPassword, password)
      setPwSuccess(true)
      setOldPassword('')
      setPassword('')
      setConfirmPassword('')
    } catch (err) {
      setPwError(err instanceof Error ? err.message : 'Failed to update password.')
    } finally {
      setPwSaving(false)
    }
  }

  // ── Delete account ──────────────────────────────────────
  const [showDelete, setShowDelete] = useState(false)

  async function handleDeleteAccount(password?: string) {
    await deleteOwnAccount(email, password!)
    localStorage.removeItem('auth_token')
    localStorage.removeItem('user_email')
    localStorage.removeItem('user_role')
    window.dispatchEvent(new Event('user_auth_changed'))
    navigate('/')
  }

  return (
    <div className="settings-page">
      <div className="settings-card">
        <h1 className="settings-title">Account Settings</h1>
        <p className="settings-email">{email}</p>

        {/* ── Change password ── */}
        <section className="settings-section">
          <h2 className="settings-section__title">Change Password</h2>
          {pwError && <p className="settings-msg settings-msg--error">{pwError}</p>}
          {pwSuccess && <p className="settings-msg settings-msg--success">Password updated successfully.</p>}
          <form className="settings-form" onSubmit={handlePasswordChange}>
            <label className="settings-label">
              Current Password
              <input
                className="settings-input"
                type="password"
                value={oldPassword}
                onChange={e => setOldPassword(e.target.value)}
                required
                autoComplete="current-password"
                placeholder="••••••••"
              />
            </label>
            <label className="settings-label">
              New Password
              <input
                className="settings-input"
                type="password"
                value={password}
                onChange={e => setPassword(e.target.value)}
                required
                autoComplete="new-password"
                placeholder="••••••••"
              />
            </label>
            <label className="settings-label">
              Confirm Password
              <input
                className="settings-input"
                type="password"
                value={confirmPassword}
                onChange={e => setConfirmPassword(e.target.value)}
                required
                autoComplete="new-password"
                placeholder="••••••••"
              />
            </label>
            <button
              type="submit"
              className="settings-btn settings-btn--save"
              disabled={pwSaving}
            >
              {pwSaving ? 'Saving…' : 'Update Password'}
            </button>
          </form>
        </section>

        {/* ── Danger zone ── */}
        <section className="settings-section settings-section--danger">
          <h2 className="settings-section__title settings-section__title--danger">Danger Zone</h2>
          <p className="settings-section__desc">
            Permanently delete your account and all associated data. This action cannot be undone.
          </p>
          <button
            className="settings-btn settings-btn--delete"
            onClick={() => setShowDelete(true)}
            type="button"
          >
            Delete Account
          </button>
        </section>
      </div>

      {showDelete && (
        <DeleteModal
          itemName="your account"
          onClose={() => setShowDelete(false)}
          onConfirm={handleDeleteAccount}
          requirePassword
        />
      )}
    </div>
  )
}
