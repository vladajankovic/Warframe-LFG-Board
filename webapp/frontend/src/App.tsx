import { BrowserRouter, Routes, Route, Outlet } from 'react-router-dom'
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
      </footer>
    </>
  )
}

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<Layout />}>
          <Route path="/" element={<HomePage />} />
          <Route path="/login" element={<AuthPage />} />
          <Route path="/relics" element={<RelicsPage />} />
          <Route path="/relics/:name" element={<RelicDetailPage />} />
          <Route path="/resources" element={<ResourcesPage />} />
          <Route path="/resources/:name" element={<ResourceDetailPage />} />
          <Route path="/admin" element={<AdminLayout />}>
            <Route index element={<AdminPage />} />
            <Route path="relics" element={<RelicsAdminPage />} />
            <Route path="resources" element={<ResourcesAdminPage />} />
          </Route>
        </Route>
      </Routes>
    </BrowserRouter>
  )
}
