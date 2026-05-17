import { useState, useEffect } from 'react'
import { Link, useNavigate } from 'react-router-dom'

export default function NavBar() {
  const [email, setEmail] = useState<string | null>(() => localStorage.getItem('user_email'))
  const [role, setRole] = useState<string | null>(() => localStorage.getItem('user_role'))
  const navigate = useNavigate()

  useEffect(() => {
    function onStorage(e: StorageEvent) {
      if (e.key === 'user_email') {
        setEmail(e.newValue)
      }
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
    window.dispatchEvent(new Event('user_auth_changed'))
    navigate('/')
  }

  return (
    <nav className="navbar">
      <Link to="/" className="navbar__brand">Warframe Items</Link>

      <div className="navbar__right">
        {email ? (
          <>
            {role === 'Admin' && (
              <Link to="/admin" className="navbar__admin-link">Admin Panel</Link>
            )}
            <span className="navbar__user">{email}</span>
            <button className="navbar__logout" onClick={handleLogout} type="button">
              Logout
            </button>
          </>
        ) : (
          <Link to="/login" className="navbar__login-btn">
            Login / Register
          </Link>
        )}
      </div>
    </nav>
  )
}
