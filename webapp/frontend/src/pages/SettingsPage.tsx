import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { changePassword, deleteOwnAccount, updateInGameName, activateSubscription, cancelSubscription, fetchWarframeDisplayName } from '../api/usersApi'
import { notifyError, notifySuccess } from '../api/apiUtils'
import { DeleteModal } from '../components/DeleteModal'

export default function SettingsPage() {
  const email = localStorage.getItem('user_email') ?? ''
  const [currentIgn, setCurrentIgn] = useState(localStorage.getItem('user_ign') ?? '')
  const navigate = useNavigate()

  // ── Subscription ────────────────────────────────────────
  const [subscriptionStatus, setSubscriptionStatus] = useState(
    (localStorage.getItem('user_subscription_status') ?? 'Inactive') as 'Active' | 'Inactive'
  )
  const [subscriptionExpiry, setSubscriptionExpiry] = useState(
    localStorage.getItem('user_subscription_expiry') ?? ''
  )
  const [subLoading, setSubLoading] = useState(false)

  // ── Change password ─────────────────────────────────────
  const [oldPassword, setOldPassword] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [pwSaving, setPwSaving] = useState(false)

  // ── Change in-game name ─────────────────────────────────
  const [inGameName, setInGameName] = useState(currentIgn)
  const [warframeUserData, setWarframeUserData] = useState('')
  const [ignSaving, setIgnSaving] = useState(false)

  async function handleFetchInGameName() {
    setIgnSaving(true)
    try {
      const displayName = await fetchWarframeDisplayName(warframeUserData)
      await updateInGameName(email, displayName)
      localStorage.setItem('user_ign', displayName)
      setInGameName(displayName)
      setCurrentIgn(displayName)
      setWarframeUserData('')
      window.dispatchEvent(new Event('user_auth_changed'))
      notifySuccess(`Warframe display name set to ${displayName}.`)
    } catch (err) {
      notifyError(err instanceof Error ? err.message : 'Failed to fetch your Warframe display name.')
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

  async function handleActivateSubscription() {
    setSubLoading(true)
    try {
      const info = await activateSubscription()
      setSubscriptionStatus(info.subscriptionStatus)
      setSubscriptionExpiry(info.subscriptionExpiryDate ?? '')
      localStorage.setItem('user_subscription_status', info.subscriptionStatus)
      localStorage.setItem('user_subscription_expiry', info.subscriptionExpiryDate ?? '')
      if (info.token) localStorage.setItem('auth_token', info.token)
      window.dispatchEvent(new Event('user_auth_changed'))
      notifySuccess('Subscription activated successfully.')
    } catch (err) {
      notifyError(err instanceof Error ? err.message : 'Failed to activate subscription.')
    } finally {
      setSubLoading(false)
    }
  }

  async function handleCancelSubscription() {
    setSubLoading(true)
    try {
      const info = await cancelSubscription()
      setSubscriptionStatus(info.subscriptionStatus)
      setSubscriptionExpiry(info.subscriptionExpiryDate ?? '')
      localStorage.setItem('user_subscription_status', info.subscriptionStatus)
      localStorage.setItem('user_subscription_expiry', info.subscriptionExpiryDate ?? '')
      if (info.token) localStorage.setItem('auth_token', info.token)
      window.dispatchEvent(new Event('user_auth_changed'))
      notifySuccess('Subscription cancelled.')
    } catch (err) {
      notifyError(err instanceof Error ? err.message : 'Failed to cancel subscription.')
    } finally {
      setSubLoading(false)
    }
  }

  async function handleDeleteAccount(password?: string) {
    await deleteOwnAccount(email, password!)
    localStorage.removeItem('auth_token')
    localStorage.removeItem('user_email')
    localStorage.removeItem('user_role')
    localStorage.removeItem('user_subscription_status')
    localStorage.removeItem('user_subscription_expiry')
    window.dispatchEvent(new Event('user_auth_changed'))
    navigate('/')
  }

  return (
    <div className="settings-page">
      <div className="settings-card">
        <h1 className="settings-title">Account Settings</h1>

        <div className="settings-info">
          <h2 className="settings-section__title">User Info</h2>
          <div className="settings-info__row">
            <span className="settings-info__label">Email</span>
            <span className="settings-info__value">{email}</span>
          </div>
          <div className="settings-info__row">
            <span className="settings-info__label">In-Game Name</span>
            <span className="settings-info__value">{currentIgn || <em className="settings-info__empty">Not set</em>}</span>
          </div>
        </div>

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
          <div className="settings-form">
            <p className="settings-section__desc">
              1. Log in to the official Warframe website in this browser.
            </p>
            <a
              className="settings-btn settings-btn--save settings-btn--link"
              href="https://www.warframe.com/login"
              target="_blank"
              rel="noopener noreferrer"
            >
              Log in to Warframe
            </a>
            <p className="settings-section__desc">
              2. Open your Warframe user data, copy the entire JSON response, and return here.
            </p>
            <a
              className="settings-btn settings-btn--save settings-btn--link"
              href="https://www.warframe.com/api/user-data"
              target="_blank"
              rel="noopener noreferrer"
            >
              Open Warframe User Data
            </a>
            <label className="settings-label">
              Warframe User Data JSON
              <textarea
                className="settings-input settings-input--json"
                value={warframeUserData}
                onChange={e => setWarframeUserData(e.target.value)}
                placeholder={'{"country_code":"...","user_id":"..."}'}
                rows={5}
                spellCheck={false}
              />
            </label>
            <p className="settings-section__desc">
              3. Fetch and save the display name associated with that user ID.
            </p>
            <label className="settings-label">
              Warframe Display Name
              <input
                className="settings-input"
                type="text"
                value={inGameName}
                readOnly
                placeholder="Fetched display name"
              />
            </label>
            <button
              type="button"
              className="settings-btn settings-btn--save"
              onClick={handleFetchInGameName}
              disabled={ignSaving || !warframeUserData.trim()}
            >
              {ignSaving ? 'Fetching and saving...' : 'Fetch Display Name'}
            </button>
          </div>
        </section>

        {/* ── Subscription ── */}
        <section className="settings-section">
          <h2 className="settings-section__title">Subscription</h2>
          <span></span>
          <div className="settings-info">
            <div className="settings-info__row">
              <span className="settings-info__label">Status</span>
              <span className={`settings-subscription-badge settings-subscription-badge--${subscriptionStatus.toLowerCase()}`}>
                {subscriptionStatus}
              </span>
            </div>
            {subscriptionExpiry && (
              <div className="settings-info__row">
                <span className="settings-info__label">
                  {subscriptionStatus === 'Active' ? 'Renews on' : 'Expired on'}
                </span>
                <span className="settings-info__value">
                  {new Date(subscriptionExpiry).toLocaleDateString()}
                </span>
              </div>
            )}
          </div>
          {subscriptionStatus === 'Inactive' ? (
            <button
              className="settings-btn settings-btn--save"
              onClick={handleActivateSubscription}
              disabled={subLoading}
              type="button"
            >
              {subLoading ? 'Activating…' : 'Activate Subscription'}
            </button>
          ) : (
            <button
              className="settings-btn settings-btn--delete"
              onClick={handleCancelSubscription}
              disabled={subLoading}
              type="button"
            >
              {subLoading ? 'Cancelling…' : 'Cancel Subscription'}
            </button>
          )}
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
