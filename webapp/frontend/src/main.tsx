import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import 'alertifyjs/build/css/alertify.css'
import 'alertifyjs/build/css/themes/default.css'
import './styles/main.css'
import App from './App'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
