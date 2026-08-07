import { BrowserRouter, Routes, Route, Outlet, Navigate, useLocation } from 'react-router-dom'
import { useEffect } from 'react'

function ScrollToTop() {
  const { pathname } = useLocation()
  useEffect(() => { window.scrollTo({ top: 0 }) }, [pathname])
  return null
}
import NavBar from './components/NavBar'
import HomePage from './pages/HomePage'
import RelicsPage from './pages/RelicsPage'
import ResourcesPage from './pages/ResourcesPage'
import RelicDetailPage from './pages/RelicDetailPage'
import ResourceDetailPage from './pages/ResourceDetailPage'
import AuthPage from './pages/AuthPage'
import AdminPage from './pages/AdminPage'
import AdminLayout from './pages/AdminLayout'
import RelicsAdminPage from './pages/RelicsAdminPage'
import ResourcesAdminPage from './pages/ResourcesAdminPage'
import UsersAdminPage from './pages/UsersAdminPage'
import GroupRequestsAdminPage from './pages/GroupRequestsAdminPage'
import LogsAdminPage from './pages/LogsAdminPage'
import SettingsPage from './pages/SettingsPage'
import MyGroupRequestsPage from './pages/MyGroupRequestsPage'
import FavoritesPage from './pages/FavoritesPage'
import FavoritesAdminPage from './pages/FavoritesAdminPage'

function RequireAuth({ role }: { role?: string }) {
  const token = localStorage.getItem('auth_token')
  const userRole = localStorage.getItem('user_role')
  if (!token) return <Navigate to="/" replace />
  if (role && userRole !== role) return <Navigate to="/" replace />
  return <Outlet />
}

function Layout() {
  return (
    <>
      <NavBar />
      <Outlet />
      <footer className="home-footer">
        <a href="https://www.warframe.com" target="_blank" rel="noopener noreferrer" className="home-footer__link">
          <img src="/images/warframe-logo.png" alt="Warframe" className="home-footer__logo" />
          <span className="home-footer__label">warframe.com</span>
          <span className="home-footer__sublabel">Official game site</span>
        </a>
        <a href="https://warframe.market/" target="_blank" rel="noopener noreferrer" className="home-footer__link">
          <img src="/images/wm-logo.png" alt="Warframe Market" className="home-footer__logo" />
          <span className="home-footer__label">warframe.market</span>
          <span className="home-footer__sublabel">Buy &amp; sell items</span>
        </a>
        <a href="https://github.com/vladajankovic" target="_blank" rel="noopener noreferrer" className="home-footer__link">
          <img src="/images/github-logo.png" alt="GitHub" className="home-footer__logo" />
          <span className="home-footer__label">vladajankovic</span>
          <span className="home-footer__sublabel">GitHub profile</span>
        </a>
        <div className="home-footer__disclaimer">
          <strong>Disclaimer</strong>
          <p>
            Digital Extremes Ltd, Warframe and the logo Warframe are registered trademarks. All rights are reserved worldwide. This site has no official link with Digital Extremes Ltd or Warframe. All artwork, screenshots, characters or other recognizable features of the intellectual property relating to these trademarks are likewise the intellectual property of Digital Extremes Ltd.
          </p>
        </div>
      </footer>
    </>
  )
}

export default function App() {
  return (
    <BrowserRouter>
      <ScrollToTop />
      <Routes>
        <Route element={<Layout />}>
          <Route path="/" element={<HomePage />} />
          <Route path="/login" element={<AuthPage />} />
          <Route path="/relics" element={<RelicsPage />} />
          <Route path="/relics/:name" element={<RelicDetailPage />} />
          <Route path="/resources" element={<ResourcesPage />} />
          <Route path="/resources/:name" element={<ResourceDetailPage />} />
          <Route element={<RequireAuth />}>
            <Route path="/settings" element={<SettingsPage />} />
            <Route path="/my-requests" element={<MyGroupRequestsPage />} />
            <Route path="/favorites" element={<FavoritesPage />} />
          </Route>
          <Route element={<RequireAuth role="Admin" />}>
            <Route path="/admin" element={<AdminLayout />}>
              <Route index element={<AdminPage />} />
              <Route path="relics" element={<RelicsAdminPage />} />
              <Route path="resources" element={<ResourcesAdminPage />} />
              <Route path="users" element={<UsersAdminPage />} />
              <Route path="group-requests" element={<GroupRequestsAdminPage />} />
              <Route path="favorites" element={<FavoritesAdminPage />} />
              <Route path="logs" element={<LogsAdminPage />} />
            </Route>
          </Route>
        </Route>
      </Routes>
    </BrowserRouter>
  )
}
