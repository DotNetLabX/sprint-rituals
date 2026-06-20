import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  base: '/dashboard/',
  plugins: [vue(), tailwindcss()],
  resolve: {
    alias: {
      // Resolve workspace package to its source (build-time; no publish step needed)
      '@sprint-rituals/shared': fileURLToPath(new URL('../../packages/shared/src/index.ts', import.meta.url)),
    },
  },
  server: {
    proxy: {
      '/api': 'http://localhost:5000',
      '/auth': 'http://localhost:5000'
    }
  },
  build: {
    // Output to wwwroot/dashboard — never the wwwroot root (emptyOutDir is scoped to this subfolder)
    outDir: '../../../src/Host/wwwroot/dashboard',
    emptyOutDir: true
  }
})
