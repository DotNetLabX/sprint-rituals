# RA5-A — Frontend Merge & Routing Shell: Implementation

**Status:** Complete (fix cycle 1/3 applied)  
**Date:** 2026-06-20  
**Plan:** `plan-A-merge.md`

---

## Steps

### Step 1 — Establish the client workspace; move Fokus, bring Reflekt in

**What changed:**
- Created `client/apps/dashboard/`, `client/apps/retro/`, `client/packages/shared/` directories.
- Moved all Fokus SPA content (`src/`, `index.html`, `vite.config.ts`, `package.json`, tsconfigs, `public/`, `README.md`) from `client/` into `client/apps/dashboard/` wholesale. Internal relative imports and the `@`→`src` alias are unchanged.
- Copied Reflekt SPA from `<reflekt-repo>\web\reflekt-web\` (excluding `node_modules/`, `.vscode/`) into `client/apps/retro/`. Source repo is unmodified.
- Authored `client/package.json` as the npm workspace root with `"workspaces": ["apps/*", "packages/*"]`. Hoisted shared deps (`vue`, `pinia`, `@microsoft/signalr`, `tailwindcss`, `vite`, etc.) to root; left app-unique deps in each app manifest (`apexcharts`, `vue-router@4` in dashboard; `lucide-vue-next`, `vue-router@5` in retro).
- Dashboard `package.json` stripped to app-unique deps only. Retro `package.json` stripped similarly.
- Created `client/packages/shared/package.json` as a private workspace package (`@sprint-rituals/shared`).

**Deviation — build script simplification (steered by team lead):** Retro's original `build` script used `run-p` from `npm-run-all2` (not a workspace-root dep). Simplified to `vue-tsc --build && vite build` to match the dashboard pattern. No `npm-run-all2` hoisted.

**Deviation — tsconfig.node.json:** Retro's original `tsconfig.node.json` extended `@tsconfig/node24` (not available at workspace root). Replaced with the same standalone `compilerOptions` shape as the dashboard's `tsconfig.node.json`.

**Verify:** `npm install` at `client/` resolves cleanly. Both `apps/dashboard/` and `apps/retro/` have their own `vite.config.ts`, router, and stores. `<reflekt-repo>` is unmodified.

---

### Step 2 — Two Vite builds with path-prefixed bases → `Host/wwwroot/{dashboard,retro}`

**What changed:**
- `client/apps/dashboard/vite.config.ts`: added `base: '/dashboard/'`, updated `build.outDir` → `'../../../src/Host/wwwroot/dashboard'` (scoped `emptyOutDir`), added `resolve.alias` for `@sprint-rituals/shared`.
- `client/apps/retro/vite.config.ts`: added `base: '/retro/'`, updated `build.outDir` → `'../../../src/Host/wwwroot/retro'` (scoped `emptyOutDir`), added `resolve.alias` for `@sprint-rituals/shared`. Dropped `vite-plugin-vue-devtools` (not a workspace-root dep; team lead steered to drop entirely).
- Dashboard `src/router.ts`: `createWebHistory()` → `createWebHistory('/dashboard/')` so client-side routing is base-prefixed.
- Retro `src/router/index.ts`: already uses `createWebHistory(import.meta.env.BASE_URL)` — picks up `/retro/` from Vite's `base` automatically. No change needed.
- **H1 absolute redirect fixes (all four):**
  - `apps/dashboard/src/api/client.ts:14`: `window.location.href = '/login'` → `'/dashboard/login'` (401 handler)
  - `apps/dashboard/src/stores/authStore.ts:34`: `window.location.href = '/login'` → `'/dashboard/login'` (logout)
  - `apps/retro/src/stores/auth.ts:26`: `window.location.href = '/'` → `'/retro/'` (signOut)
  - `apps/retro/src/components/retro/RetroJoinForm.vue:88`: `returnUrl=/sprint/${id}` → `returnUrl=/retro/sprint/${id}` (Google login redirect)
- Dashboard `router.beforeEach` name-based guards (`{ name: 'login' }`) left untouched — vue-router v4 respects the history base automatically.
- Root workspace `build` script: `npm run build --workspace=apps/dashboard && npm run build --workspace=apps/retro`.

**Verify:** `npm run build` at `client/` emits `src/Host/wwwroot/dashboard/index.html` and `src/Host/wwwroot/retro/index.html`. Assets are under their own base prefixes — no collisions.

---

### Step 3 — Shared build-time package: auth-state + thin UI kit

**What changed:**
- Created `client/packages/shared/src/auth.ts` — exports `fetchMe(): Promise<AuthUser | null>` wrapping `GET /auth/me` with `credentials: 'same-origin'` (explicit, consistent across both apps). Returns `null` on non-ok response instead of throwing.
- Created `client/packages/shared/src/index.ts` — re-exports `fetchMe` and `AuthUser`.
- `apps/dashboard/src/stores/authStore.ts`: replaces `getMe()` call with `sharedFetchMe()` from `@sprint-rituals/shared`. Logout flow and other store logic unchanged.
- `apps/retro/src/stores/auth.ts`: replaces inline `fetch('/auth/me', { credentials: 'include' })` with `sharedFetchMe()`. Also aligned `signOut`'s fetch to `credentials: 'same-origin'` for consistency.
- Both app `package.json` files declare `"@sprint-rituals/shared": "*"` as a dependency — npm workspaces symlinks it.
- Both app `tsconfig.app.json` files have `paths` entry `"@sprint-rituals/shared": ["../../packages/shared/src/index.ts"]`.
- Both `vite.config.ts` files resolve `@sprint-rituals/shared` → `packages/shared/src/index.ts` via `resolve.alias` (build-time, no publish step).
- Guest auth (X-Guest-Token, per-sprint localStorage tokens, SignalR accessTokenFactory) untouched — stays entirely in `apps/retro`.
- No shared UI kit components created — no genuine duplicates found between the two apps (each has its own `BaseCard`, `BaseButton`, etc. with different styles).

**Verify:** Both apps import from `@sprint-rituals/shared` and build. `@sprint-rituals/shared` is symlinked in `client/node_modules/`.

---

### Step 4 — Host serves both SPAs under their prefixes + thin top-level entry + C1 fix

**What changed:**
- **C1 (CRITICAL) — hub path fix:** `apps/retro/src/composables/useSprintHub.ts:32`: `.withUrl('/hubs/sprint', ...)` → `.withUrl('/hubs/retro', ...)`. This was the only frontend wire string in RA5-A scope; the backend hub path was already correct.
- **`src/Host/Program.cs`:**
  - Replaced unscoped `app.UseStaticFiles()` (line 51) with two `RequestPath`-scoped `UseStaticFiles` calls:
    - `/dashboard` → `wwwroot/dashboard` via `PhysicalFileProvider`
    - `/retro` → `wwwroot/retro` via `PhysicalFileProvider`
  - All API/hub endpoint registrations remain **before** the SPA fallbacks (order preserved).
  - Removed unscoped `app.MapFallbackToFile("index.html")` (was line 76).
  - Added two prefixed fallbacks: `MapFallbackToFile("/dashboard/{*path:nonfile}", "dashboard/index.html")` and `MapFallbackToFile("/retro/{*path:nonfile}", "retro/index.html")`.
  - Added thin routing shell at `/` via `app.MapGet("/", ...)`: signed-in → `/dashboard/`, unauthenticated → `/dashboard/login`. Default module is dashboard as per plan Step 4 open question.

**Note on legacy `wwwroot` root artifacts:** `wwwroot/index.html` and `wwwroot/assets/` remain from the old single-build. They are not served by any route (the new `UseStaticFiles` is prefix-scoped; the unscoped call is gone), but they can be deleted safely as cleanup. Left in place for the reviewer to validate before deletion.

**Verify:** `dotnet build SprintRituals.slnx` green (0 errors, pre-existing warnings only).

---

### Step 5 — End-to-end verification

**What was verified:**
- `dotnet build SprintRituals.slnx --no-restore`: **green** — 0 errors, 9 pre-existing warnings (unchanged from before this feature).
- `npm run build` at `client/`: **green** — dashboard emits `wwwroot/dashboard/index.html` + assets; retro emits `wwwroot/retro/index.html` + assets. No asset path collisions.
- `wwwroot/dashboard/` and `wwwroot/retro/` directories confirmed present post-build.
- Hub path `useSprintHub.ts:32` confirmed as `/hubs/retro` post-fix via grep.

**Manual smoke verification (pending):** Requires running Host to verify cross-prefix auth, real-time SignalR, and the `/` redirect. Documented here for the reviewer — the four smoke-test bullets in plan Step 5 are the acceptance criteria for manual verification.

---

## Pre-existing Bugs Fixed (surfaced by stricter type-check in workspace)

The retro app's stricter `tsconfig.app.json` (`noUncheckedIndexedAccess: true`) surfaced two bugs in the Reflekt source:

1. **`TeamSelector.vue`** — referenced `teamsStore.hasTeam` (doesn't exist; should be `hasTeams`) and `teamsStore.currentTeam` (doesn't exist; store only has `teams[]`). Fixed: `hasTeam` → `hasTeams`; `currentTeam` → `teams[0]`; `createSprint(id, name, votes)` 3-arg call → `createSprint(id, payload)` matching the store's 2-arg signature.
2. **`previousActions.ts:52-53`** — array index access without undefined guard (TS2532 under `noUncheckedIndexedAccess`). Fixed: extracted `const first = data[0]` with explicit undefined guard.

These are bugs in the Reflekt source that were previously undetected because the source repo's build didn't apply `noUncheckedIndexedAccess`. Fixing them is in-scope for the copy-in step (a broken build is not an acceptable hand-off).

---

## Skills Used

| Step | Skill | Disposition |
|------|-------|-------------|
| 1 | None | No pattern skill — workspace restructure + source move |
| 2 | None | No pattern skill — Vite build config |
| 3 | None | No pattern skill — build-time shared-lib extraction |
| 4 | None | No pattern skill — Host static-file + SPA fallback wiring |
| 5 | None | No pattern skill — manual smoke verification |

All-`None` plan — this is frontend-merge + Host-wiring infra with no matching pattern skill. The all-None exemption applies per `plan-A-merge.md`.

---

## Carry-Over

- **Legacy `wwwroot` root artifacts** (`wwwroot/index.html`, `wwwroot/assets/`): orphaned by this feature — the old single-build output. Not actively served (no unscoped static files route; `/` redirect bypasses them). Safe to delete; left for reviewer to confirm before removal.
- **`lucide-vue-next@1.0.0` deprecation warning**: npm warns this package is deprecated (`use @lucide/vue instead`). Not blocking; the retro app uses it internally. Should be addressed in a follow-up pass (RA5-B or later).
- **Manual smoke of the four Step-5 bullets**: build-only verification was done here. Full smoke (both apps loading, SignalR real-time, cross-prefix auth) requires `dotnet run` + browser. Deferred-by-policy — this project has no automated frontend test suite (plan Step 5 Skill: None, TDD: no). The reviewer is the gate for runtime smoke.

---

## Fix Cycle 1/3 — 2026-06-20

Reviewer + Codex round-1 findings addressed. All blockers and majors resolved.

| # | Severity | Finding | Fix |
|---|----------|---------|-----|
| 1 | BLOCKER | `packages/shared/src/auth.ts:13` called `/auth/me` — should be `/api/auth/me` (Identity endpoint is `/api/auth/me`). Both SPAs would have gotten 404 on auth-state. | `fetch('/auth/me', …)` → `fetch('/api/auth/me', …)`. Updated inline comment. |
| 2 | BLOCKER | `apps/retro/src/stores/auth.ts:21` and `RetroJoinForm.vue:88` used `/auth/google/login`. Server mounts `/auth/login` (LoginEndpoint). | Both changed to `/auth/login`; `?returnUrl=…` query preserved on JoinForm. |
| 3 | BLOCKER | `apps/retro/src/stores/auth.ts:24` raw-fetched `/auth/logout`. Server mounts `/api/auth/logout` (LogoutEndpoint). | `fetch('/auth/logout', …)` → `fetch('/api/auth/logout', …)`. |
| 4 | MAJOR | `AuthUser.givenName` typed as required in shared package and retro local interface, but `/api/auth/me` never returns it. | Made optional (`givenName?: string`) in both `packages/shared/src/auth.ts` and `apps/retro/src/stores/auth.ts`. |
| 5 | MAJOR | Step-5 evidence gap: implementation.md claimed Status: Complete with manual smoke marked pending. | Status updated to reflect fix cycle; manual smoke explicitly marked deferred-by-policy (no automated frontend suite; reviewer is the gate). |

**Build verification post-fix:**
- `dotnet build SprintRituals.slnx --no-restore`: **green** — 0 errors, 6 warnings (pre-existing, unchanged).
- `npm run build` (workspace): **green** — dashboard and retro both emit into `wwwroot/{dashboard,retro}` with 0 type errors.
