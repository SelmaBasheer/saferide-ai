import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'path'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    // "@" resolves to /src for clean absolute imports (required by shadcn/ui)
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 5173,
    proxy: {
      // Proxy API calls to the Identity service to avoid cross-origin requests in dev
      '/api': {
        target: 'http://localhost:5001',
        changeOrigin: true,
      },
    },
  },
})