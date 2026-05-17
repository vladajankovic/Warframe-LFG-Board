import { useState, useEffect } from 'react'
import { Link, useNavigate } from 'react-router-dom'

export default function NavBar() {
  const [email, setEmail] = useState<string | null>(() => localStorage.getItem('user_email'))
  const [role, setRole] = useState<string | null>(() => localStorage.getItem('user_role'))
  const [open, setOpen] = useState(false)
  const navigate = useNavigate()

  useEffect(() => {
    function onStorage(e: StorageEvent) {
      if (e.key === 'user_email') setEmail(e.newValue)
    }
    function onAuthChanged() {
      setEmail(localStorage.getItem('user_email'))
      setRole(localStorage.getItem('user_role'))
    }
    window.addEventListener('storage', onStorage)
    window.addEventListener('user_auth_changed', onAuthChanged)
    return () => {
      window.removeEventListener('storage', onStorage)
      window.removeEventListener('user_auth_changed', onAuthChanged)
    }
  }, [])

  function handleLogout() {
    localStorage.removeItem('auth_token')
    localStorage.removeItem('user_email')
    localStorage.removeItem('user_role')
    setEmail(null)
    setRole(null)
    setOpen(false)
    window.dispatchEvent(new Event('user_auth_changed'))
    navigate('/')
  }

  return (
    <nav className="navbar">
      <Link to="/" className="navbar__brand">
        <img src="/images/lfg-logo1.png" alt="Warframe LFG Logo" className="navbar__logo" />
      </Link>

      <div className="navbar__right">
        {email ? (
          <div
            className="navbar__user-menu"
            onMouseEnter={() => setOpen(true)}
            onMouseLeave={() => setOpen(false)}
          >
            <span className="navbar__user-trigger">
              {email}
              <span className="navbar__user-caret">▾</span>
            </span>

            {open && (
              <div className="navbar__dropdown">
                <Link
                  to="/settings"
                  className="navbar__dropdown-item"
                  onClick={() => setOpen(false)}
                >
                  ⚙ Settings
                </Link>
                {role === 'Admin' && (
                  <Link
                    to="/admin"
                    className="navbar__dropdown-item"
                    onClick={() => setOpen(false)}
                  >
                    ◈ Admin Panel
                  </Link>
                )}
                <button
                  className="navbar__dropdown-item navbar__dropdown-item--danger"
                  onClick={handleLogout}
                  type="button"
                >
                  ⏻ Logout
                </button>
              </div>
            )}
          </div>
        ) : (
          <Link to="/login" className="navbar__login-btn">
            Login / Register
          </Link>
        )}
      </div>
    </nav>
  )
}
