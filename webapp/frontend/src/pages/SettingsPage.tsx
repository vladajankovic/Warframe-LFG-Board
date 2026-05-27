import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { changePassword, deleteOwnAccount, updateInGameName } from '../api/usersApi'
import { notifyError, notifySuccess } from '../api/apiUtils'
import { DeleteModal } from '../components/DeleteModal'

export default function SettingsPage() {
  const email = localStorage.getItem('user_email') ?? ''
  const navigate = useNavigate()

  // ── Change password ─────────────────────────────────────
  const [oldPassword, setOldPassword] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [pwSaving, setPwSaving] = useState(false)

  // ── Change in-game name ─────────────────────────────────
  const [inGameName, setInGameName] = useState('')
  const [ignSaving, setIgnSaving] = useState(false)

  async function handleInGameNameChange(e: FormEvent) {
    e.preventDefault()
    setIgnSaving(true)
    try {
      await updateInGameName(email, inGameName)
      localStorage.setItem('user_ign', inGameName)
      window.dispatchEvent(new Event('user_auth_changed'))
      notifySuccess('In-game name updated successfully.')
      setInGameName('')
    } catch (err) {
      notifyError(err instanceof Error ? err.message : 'Failed to update in-game name.')
    } finally {
      setIgnSaving(false)
    }
  }

  async function handlePasswordChange(e: FormEvent) {
    e.preventDefault()
    if (password !== confirmPassword) {
      notifyError('Passwords do not match.')
      return
    }
    setPwSaving(true)
    try {
      await changePassword(email, oldPassword, password)
      notifySuccess('Password updated successfully.')
      setOldPassword('')
      setPassword('')
      setConfirmPassword('')
    } catch (err) {
      notifyError(err instanceof Error ? err.message : 'Failed to update password.')
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

        {/* ── Change in-game name ── */}
        <section className="settings-section">
          <h2 className="settings-section__title">In-Game Name</h2>
          <form className="settings-form" onSubmit={handleInGameNameChange}>
            <label className="settings-label">
              New In-Game Name
              <input
                className="settings-input"
                type="text"
                value={inGameName}
                onChange={e => setInGameName(e.target.value)}
                required
                maxLength={50}
                placeholder="Your in-game name"
                autoComplete="off"
              />
            </label>
            <button
              type="submit"
              className="settings-btn settings-btn--save"
              disabled={ignSaving}
            >
              {ignSaving ? 'Saving…' : 'Update In-Game Name'}
            </button>
          </form>
        </section>

        {/* ── Danger zone ── */}
        <section className="settings-section settings-section--danger">
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
