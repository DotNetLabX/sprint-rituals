# Retro (Reflekt)

The Reflekt retrospective app of Sprint Rituals: Vue 3, TypeScript, Pinia and Tailwind CSS, built with Vite, with live updates over SignalR.

- **Dev server:** from `client/`, run `npm run dev:retro` — it serves the app at `http://localhost:5172/retro/` and proxies API and SignalR calls to the host on `http://localhost:5100`, which must be running.
- **Build:** from `client/`, `npm run build` builds all three apps; this one goes to `src/Host/wwwroot/retro/`, where the host serves it at `/retro/`.

Setup, secrets and the rest of the suite: see the [root README](../../../README.md).
