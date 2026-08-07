import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { fileURLToPath, URL } from 'node:url'

const identity = process.env.VITE_IDENTITY_API ?? 'http://localhost:5001'
const school = process.env.VITE_SCHOOL_API ?? 'http://localhost:5003'
const driver = process.env.VITE_DRIVER_API ?? 'http://localhost:5007'
const student = process.env.VITE_STUDENT_API ?? 'http://localhost:5009'

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