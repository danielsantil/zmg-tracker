/// <reference types="vitest/config" />
import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Dev server proxies /api to the ASP.NET Core app; prod build lands in the API's
// wwwroot so one `dotnet run` serves the SPA (build-plan.md section 4).
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    proxy: {
      '/api': 'http://localhost:5274',
    },
  },
  build: {
    outDir: '../Zmg.Api/wwwroot',
    emptyOutDir: true,
  },
  test: {
    // Pure-module tests only (no Testing Library — component tests are out of scope this round,
    // M24.2). The `node` environment plus a small localStorage shim (src/test/setup.ts) avoids
    // jsdom, whose ESM-only CSS dep breaks under Node < 22.12.
    environment: 'node',
    setupFiles: ['./src/test/setup.ts'],
    include: ['src/**/*.test.ts'],
  },
})
