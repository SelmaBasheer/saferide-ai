import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { fileURLToPath, URL } from 'node:url'

const identity = process.env.VITE_IDENTITY_API ?? 'http://localhost:5001'
const school = process.env.VITE_SCHOOL_API ?? 'http://localhost:5003'
const driver = process.env.VITE_DRIVER_API ?? 'http://localhost:5007'
const student = process.env.VITE_STUDENT_API ?? 'http://localhost:5009'
const bus = process.env.VITE_BUS_API ?? 'http://localhost:5011'
const route = process.env.VITE_ROUTE_API ?? 'http://localhost:5013'
const tracking = process.env.VITE_TRACKING_API ?? 'http://localhost:5015'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    port: 5173,
    proxy: {
      // order matters: more specific rule first, /api catches the rest
      '/api/students': { target: student, changeOrigin: true },
      '/api/drivers': { target: driver, changeOrigin: true },
      '/api/schools': { target: school, changeOrigin: true },
      '/api/buses': { target: bus, changeOrigin: true },
      '/api/routes': { target: route, changeOrigin: true },
      '/api/trips': { target: tracking, changeOrigin: true },
      '/hubs': { target: tracking, changeOrigin: true, ws: true },
      '/api': { target: identity, changeOrigin: true },
    },
  },
})