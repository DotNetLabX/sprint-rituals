import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import tailwindcss from '@tailwindcss/vite'

// vite-plugin-vue-devtools intentionally omitted — not a workspace-root dep;
// drop from the build path. Re-add as a workspace devDep if needed for local dev.

export default defineConfig({
  base: '/retro/',
  plugins: [
    vue(),
    tailwindcss(),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
      // Resolve workspace package to its source (build-time; no publish step needed)
      '@sprint-rituals/shared': fileURLToPath(new URL('../../packages/shared/src/index.ts', import.meta.url)),
    },
  },
  server: {
    proxy: {
      '/api': 'http://localhost:5000',
      '/auth': 'http://localhost:5000',
      '/signin-google': 'http://localhost:5000',
      '/hubs': {
        target: 'http://localhost:5000',
        ws: true,
      },
    },
  },
  build: {
    // Output to wwwroot/retro — never the wwwroot root (emptyOutDir is scoped to this subfolder)
    outDir: '../../../src/Host/wwwroot/retro',
    emptyOutDir: true,
  },
})
