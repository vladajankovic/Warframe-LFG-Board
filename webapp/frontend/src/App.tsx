import { BrowserRouter, Routes, Route } from 'react-router-dom'
import HomePage from './pages/HomePage'
import RelicsPage from './pages/RelicsPage'
import ResourcesPage from './pages/ResourcesPage'
import RelicDetailPage from './pages/RelicDetailPage'
import ResourceDetailPage from './pages/ResourceDetailPage'

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/relics" element={<RelicsPage />} />
        <Route path="/relics/:name" element={<RelicDetailPage />} />
        <Route path="/resources" element={<ResourcesPage />} />
        <Route path="/resources/:name" element={<ResourceDetailPage />} />
      </Routes>
    </BrowserRouter>
  )
}
