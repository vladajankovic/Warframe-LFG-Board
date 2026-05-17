import { useEffect } from 'react'
import { Outlet, useNavigate } from 'react-router-dom'

export default function AdminLayout() {
  const navigate = useNavigate()

  useEffect(() => {
    if (localStorage.getItem('user_role') !== 'Admin') {
      navigate('/', { replace: true })
    }
  }, [navigate])

  return <Outlet />
}
