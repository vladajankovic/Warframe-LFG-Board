import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    proxy: {
      '/v1/directory': {
        target: 'http://localhost:8001',
        changeOrigin: true,
      },
      '/v1/itemstore': {
        target: 'http://localhost:8002',
        changeOrigin: true,
      },
      '/v1/grouprequests': {
        target: 'http://localhost:8003',
        changeOrigin: true,
      },
      '/v1/audit': {
        target: 'http://localhost:8004',
        changeOrigin: true,
      },
      '/v1/user-notification': {
        target: 'http://localhost:8005',
        changeOrigin: true,
      },
      '/hubs/user-notifications': {
        target: 'http://localhost:8005',
        changeOrigin: true,
        ws: true,
      },
    },
  },
})
