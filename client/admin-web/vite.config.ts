import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { fileURLToPath, URL } from 'node:url'

const identity = 'http://localhost:5001'
const school = 'http://localhost:5003'
const driver = 'http://localhost:5007'
const student = 'http://localhost:5009'

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
      '/api': { target: identity, changeOrigin: true },
    },
  },
})