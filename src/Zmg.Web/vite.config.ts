import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Dev server proxies /api to the ASP.NET Core app; prod build lands in the API's
// wwwroot so one `dotnet run` serves the SPA (build-plan.md section 4).
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': 'http://localhost:5274',
    },
  },
  build: {
    outDir: '../Zmg.Api/wwwroot',
    emptyOutDir: true,
  },
})
