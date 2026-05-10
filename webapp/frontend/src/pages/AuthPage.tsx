import { useState, type FormEvent } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { login, register, type AuthCredentials } from '../api/usersApi'

type Tab = 'login' | 'register'

export default function AuthPage() {
  const [tab, setTab] = useState<Tab>('login')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const navigate = useNavigate()

  function switchTab(next: Tab) {
    setTab(next)
    setError(null)
    setEmail('')
    setPassword('')
    setConfirmPassword('')
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)

    if (tab === 'register' && password !== confirmPassword) {
      setError('Passwords do not match.')
      return
    }

    const credentials: AuthCredentials = { email, password }
    setLoading(true)
    try {
      if (tab === 'login') {
        const response = await login(credentials)
        localStorage.setItem('auth_token', response.token)
        localStorage.setItem('user_email', response.email)
        localStorage.setItem('user_role', response.role)
      } else {
        await register(credentials)
        // auto-login after registration
        const response = await login(credentials)
        localStorage.setItem('auth_token', response.token)
        localStorage.setItem('user_email', response.email)
        localStorage.setItem('user_role', response.role)
      }
      window.dispatchEvent(new Event('user_auth_changed'))
      navigate('/')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Something went wrong.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="auth-page">
      <div className="auth-card">
        <Link to="/" className="auth-back">← Back</Link>

        <h1 className="auth-title">Warframe Items</h1>

        <div className="auth-tabs">
          <button
            className={`auth-tab${tab === 'login' ? ' auth-tab--active' : ''}`}
            onClick={() => switchTab('login')}
            type="button"
          >
            Login
          </button>
          <button
            className={`auth-tab${tab === 'register' ? ' auth-tab--active' : ''}`}
            onClick={() => switchTab('register')}
            type="button"
          >
            Register
          </button>
        </div>

        <form className="auth-form" onSubmit={handleSubmit} noValidate>
          <div className="auth-field">
            <label className="auth-label" htmlFor="auth-email">Email</label>
            <input
              id="auth-email"
              className="auth-input"
              type="email"
              placeholder="operator@tenno.net"
              value={email}
              onChange={e => setEmail(e.target.value)}
              required
              autoComplete="email"
            />
          </div>

          <div className="auth-field">
            <label className="auth-label" htmlFor="auth-password">Password</label>
            <input
              id="auth-password"
              className="auth-input"
              type="password"
              placeholder="••••••••"
              value={password}
              onChange={e => setPassword(e.target.value)}
              required
              autoComplete={tab === 'login' ? 'current-password' : 'new-password'}
            />
          </div>

          {tab === 'register' && (
            <div className="auth-field">
              <label className="auth-label" htmlFor="auth-confirm">Confirm Password</label>
              <input
                id="auth-confirm"
                className="auth-input"
                type="password"
                placeholder="••••••••"
                value={confirmPassword}
                onChange={e => setConfirmPassword(e.target.value)}
                required
                autoComplete="new-password"
              />
            </div>
          )}

          {error && <p className="auth-error">{error}</p>}

          <button className="auth-submit" type="submit" disabled={loading}>
            {loading ? 'Please wait…' : tab === 'login' ? 'Login' : 'Create Account'}
          </button>
        </form>
      </div>
    </div>
  )
}
