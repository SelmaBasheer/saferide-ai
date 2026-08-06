import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { fileURLToPath, URL } from 'node:url'

// Local `dotnet run` ports by default; override via env vars for container mode:
//   $env:VITE_IDENTITY_URL="http://localhost:5001"; $env:VITE_SCHOOL_URL="http://localhost:5003"; npm run dev
const identity = process.env.VITE_IDENTITY_URL ?? 'http://localhost:5000'
const school = process.env.VITE_SCHOOL_URL ?? 'http://localhost:5002'
const driver = process.env.VITE_IDENTITY_URL ?? 'http://localhost:5007'
const student = process.env.VITE_SCHOOL_URL ?? 'http://localhost:5009'

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