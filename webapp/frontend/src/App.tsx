import { BrowserRouter, Routes, Route, Outlet } from 'react-router-dom'
import NavBar from './components/NavBar'
import HomePage from './pages/HomePage'
import RelicsPage from './pages/RelicsPage'
import ResourcesPage from './pages/ResourcesPage'
import RelicDetailPage from './pages/RelicDetailPage'
import ResourceDetailPage from './pages/ResourceDetailPage'
import AuthPage from './pages/AuthPage'

function Layout() {
  return (
    <>
      <NavBar />
      <Outlet />
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
        </Route>
      </Routes>
    </BrowserRouter>
  )
}
