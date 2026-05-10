import { useState, useEffect } from 'react'
import { Link, useNavigate } from 'react-router-dom'

export default function NavBar() {
  const [email, setEmail] = useState<string | null>(() => localStorage.getItem('user_email'))
  const navigate = useNavigate()

  useEffect(() => {
    function onStorage(e: StorageEvent) {
      if (e.key === 'user_email') {
        setEmail(e.newValue)
      }
    }
    function onAuthChanged() {
      setEmail(localStorage.getItem('user_email'))
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
    window.dispatchEvent(new Event('user_auth_changed'))
    navigate('/')
  }

  return (
    <nav className="navbar">
      <Link to="/" className="navbar__brand">Warframe Items</Link>

      <div className="navbar__right">
        {email ? (
          <>
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
