# Dashboard (Fokus)

The Fokus sprint-analytics app of Sprint Rituals: Vue 3, TypeScript, Pinia and Tailwind CSS, built with Vite.

- **Dev server:** from `client/`, run `npm run dev:dashboard` — it serves the app at `http://localhost:5171/dashboard/` and proxies API calls to the host on `http://localhost:5100`, which must be running.
- **Build:** from `client/`, `npm run build` builds all three apps; this one goes to `src/Host/wwwroot/dashboard/`, where the host serves it at `/dashboard/`.

Setup, secrets and the rest of the suite: see the [root README](../../../README.md).
