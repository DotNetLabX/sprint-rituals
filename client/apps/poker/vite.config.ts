import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  base: '/poker/',
  plugins: [
    vue(),
    tailwindcss(),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
      '@sprint-rituals/shared': fileURLToPath(new URL('../../packages/shared/src/index.ts', import.meta.url)),
    },
  },
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      '/api': 'http://localhost:5100',
      '/auth': 'http://localhost:5100',
      '/signin-google': 'http://localhost:5100',
      '/hubs': {
        target: 'http://localhost:5100',
        ws: true,
      },
    },
  },
  build: {
    // Output to wwwroot/poker — scoped to this subfolder
    outDir: '../../../src/Host/wwwroot/poker',
    emptyOutDir: true,
  },
})
