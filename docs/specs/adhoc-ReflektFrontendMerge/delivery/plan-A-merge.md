# RA5-A — Frontend Merge & Routing Shell

**Parent:** `plan.md` · **Depends on:** — (first sub-plan) · **Type:** Refactoring + infra (no behavior change to either app's features)

Bring the Reflekt Vue SPA into the monorepo and compose both SPAs as **two independent Vite builds under one thin routing shell**, served by Host. This is the ADR-020 build-topology realization. Mechanical-before-deep: this sub-plan moves and wires only — it does **not** rename backend routes/SignalR strings (RA5-C) or add the sprint-context panel (RA5-B).

**Binding (do not deviate):** two separate builds (not one bundle); both build outputs land under `src/Host/wwwroot/{dashboard,retro}`; Reflekt's guest-auth path stays entirely inside the retro app; vue-router versions are **not** unified (Fokus stays v4, Reflekt stays v5 — each app owns its router). Folder names below are the recommended shape; the developer may adjust exact names but the two-build + shared-package structure is binding.

**What must NOT change:** every existing Fokus route/view/store behavior; every existing Reflekt route/view/store/SignalR behavior; the Reflekt backend (`src/Services/Reflekt`) — untouched in this sub-plan; the `/hubs/retro` hub path; all current API call paths from both frontends (renames are RA5-C).

---

## Step 1 — Establish the client workspace; move Fokus, bring Reflekt in

Convert `client/` into an npm workspace hosting two apps + one shared package:

```
client/
  package.json            # workspaces: ["apps/*", "packages/*"]
  apps/
    dashboard/            # = current client/* (Fokus), moved wholesale
    retro/                # = <reflekt-repo>/web/reflekt-web/* (Reflekt), copied in
  packages/
    shared/               # build-time shared lib (Step 3)
```

- Move the existing Fokus SPA (`client/src`, `client/index.html`, `client/vite.config.ts`, `client/package.json`, tsconfig, etc.) into `client/apps/dashboard/`. Internal relative imports and the `@`→`src` alias are preserved by the wholesale move; only workspace-root and outDir-relative paths change (Step 2).
- Copy the Reflekt SPA from `<reflekt-repo>/web/reflekt-web/` into `client/apps/retro/` (source repo is read-only — copy, don't move). Preserve its own `vite.config.ts`, `package.json`, router, stores.
- Author `client/package.json` as the workspace root; reconcile the trivially-skewed shared dep versions (Vue 3.5, Pinia 3, Tailwind 4, Vite 8, SignalR 10) to a single version each at the root; leave app-unique deps (apexcharts in dashboard, lucide in retro; **vue-router 4 in dashboard, 5 in retro**) in their own app manifests.

**Accept:** `npm install` at `client/` resolves one hoisted dependency tree; both `apps/dashboard` and `apps/retro` retain their own `vite.config.ts`, router, and stores; no Reflekt source file references the original `reflekt-web` path; the original Reflekt project is unmodified.
**Skill:** None · **TDD:** no · **Confidence:** medium (workspace restructure; developer verifies hoist + per-app build before proceeding)

## Step 2 — Two Vite builds with path-prefixed bases → `Host/wwwroot/{dashboard,retro}`

- Dashboard `vite.config.ts`: `base: '/dashboard/'`, `build.outDir` → `../../../src/Host/wwwroot/dashboard` (emptyOutDir scoped to that folder).
- Retro `vite.config.ts`: `base: '/retro/'`, `build.outDir` → `../../../src/Host/wwwroot/retro`.
- Each app's router switches to `createWebHistory('/dashboard/')` / `createWebHistory('/retro/')` so client-side routing is scoped under its prefix.
- **Rewrite absolute-path redirects that bypass the router base** (broken by the prefix move): Fokus `client/.../api/client.ts:15` + `stores/authStore.ts:34` `window.location.href='/login'` → `/dashboard/login`; Reflekt `stores/auth.ts:25` sign-out redirect `/` → `/retro/` (or the shell); `RetroJoinForm.vue:88` `returnUrl=/sprint/${id}` → `/retro/sprint/${id}`. Prefer name-based `router.push` where a router is in scope. (Step 2's `createWebHistory` change only handles router-internal nav, not these raw `window.location`/string URLs.)
- **`emptyOutDir` guard:** each `outDir` points at its own `wwwroot/{dashboard,retro}` subfolder — never at `wwwroot` root, or one build wipes the other's output.
- Add root workspace scripts (`build` builds both apps into their wwwroot subfolders).

**Accept:** `npm run build` at `client/` emits `src/Host/wwwroot/dashboard/index.html` + assets and `src/Host/wwwroot/retro/index.html` + assets; no asset path collisions (each app's assets are under its own base); existing Fokus deep links resolve under `/dashboard/*`, Reflekt under `/retro/*`; the rewritten absolute redirects (`/login`, sign-out, retro `returnUrl`) land inside the correct prefix, not the root shell.
**Skill:** None · **TDD:** no · **Confidence:** medium

## Step 3 — Shared build-time package: auth-state + thin UI kit

Create `client/packages/shared` (the ADR-020 "shared layer = routing + Google-auth state + thin UI kit"), consumed by both apps at **build time** (not a runtime singleton):

- A small auth-state composable/helper wrapping `GET /auth/me` against the shared Identity cookie, returning the signed-in user (email/displayName/givenName; role optional — only dashboard reads it). Both apps' existing auth stores delegate the `/auth/me` fetch to this helper. The shared helper wraps **only** `GET /auth/me` (auth-state) — **not** the whole fetch wrapper; each app keeps its own API client and its own `401 →` redirect policy (Fokus's `apiFetch`, Reflekt's per-store fetch). Do not over-extract.
- **Reconcile the credentials inconsistency:** the shared fetch helper sends `credentials: 'same-origin'` explicitly (Fokus's `apiFetch` currently omits it and relies on the implicit default; Reflekt sends `include`). Under one Host origin this is benign today but must be explicit and consistent.
- **Guest auth is NOT shared** — Reflekt's guest-token path (`X-Guest-Token`, per-sprint `localStorage` tokens, SignalR `accessTokenFactory`) stays entirely in `apps/retro`. Only the signed-in `/auth/me` path moves to `packages/shared`.
- Optionally seed the thin UI kit with shared primitives only if a genuine duplicate exists; do not invent shared components. (No shared component exists today — keep the kit minimal/empty rather than manufacture coupling.)

**Accept:** both apps import the shared auth-state helper from `packages/shared` and build; the signed-in user object is fetched once per app via the shared helper; Reflekt guest flow is unchanged and remains retro-only; `credentials` is explicit in the shared helper.
**Skill:** None (build-time lib extraction; no matching pattern skill) · **TDD:** no · **Confidence:** medium

## Step 4 — Host serves both SPAs under their prefixes + thin top-level entry

Wire Host (`src/Host/Program.cs` / its module composition) to serve each built SPA as a static app under its prefix with a per-prefix SPA fallback, plus a thin root entry:

- **Replace — do not augment — the current unscoped serving.** Host today has a single `app.UseStaticFiles()` (`Program.cs:51`) and a single catch-all `app.MapFallbackToFile("index.html")` (`Program.cs:76`); there is **no** per-prefix pattern to mirror, and the bare fallback 404s once Step 2 moves output into `wwwroot/{dashboard,retro}` (no root `wwwroot/index.html` remains). Wire instead: two `RequestPath`-scoped `UseStaticFiles` (`/dashboard`→`wwwroot/dashboard`, `/retro`→`wwwroot/retro`) **plus** two prefixed fallbacks `MapFallbackToFile("dashboard/index.html", "/dashboard/{*path}")` and the `/retro/{*path}` equivalent. Line 76 is replaced.
- **Registration order is load-bearing.** Both prefixed fallbacks and the root `/` launcher must register **after** `MapFokusEndpoints`/`MapReflektEndpoints` (`Program.cs:67-68`) and the Identity endpoints (`:55`), so `/api/*`, `/auth/*`, and `/hubs/retro` keep matching before any `index.html` fallback.
- **Thin routing shell at `/`:** a minimal entry that reads signed-in auth state and redirects to the default module (`/dashboard` for signed-in users; unauthenticated lands on the module's own login/welcome as today). This is the ADR-020 "one routing shell" — keep it minimal (a redirect/launcher), not a third heavyweight app. **Open (developer call, do not assume):** a cold `/` hit from an *unauthenticated* user with no prior module — land on dashboard login, or a neutral launcher? Default to dashboard login unless decided otherwise.
- **Fix the one in-scope frontend wire string (CRITICAL — C1).** The copied-in Reflekt `apps/retro/src/composables/useSprintHub.ts:32` connects to `/hubs/sprint` (its source-repo path), which collides with **Fokus's** hub and silently breaks retro real-time. Change it to `/hubs/retro` — the in-repo backend's actual hub path (`Reflekt.API/DependencyInjection.cs:74`). This single frontend edit is in RA5-A scope because the hub path is a stated RA5-A invariant; it is distinct from the `/api/*` route renames deferred to RA5-C (the backend hub path is already correct — only the copied frontend caller is wrong).

**Accept:** hitting `/` redirects to the correct module by auth state; `/dashboard/...` and `/retro/...` deep links serve the right SPA on hard refresh; `/api/*`, `/auth/*`, `/hubs/retro` are unaffected; both apps authenticate via the shared Identity cookie.
**Skill:** None (Host static-file + SPA-fallback wiring; reference the existing `MapFallbackToFile`/`UseStaticFiles` setup in `Program.cs`) · **TDD:** no · **Confidence:** medium

## Step 5 — End-to-end verification of the merged shell

Build and run Host; verify both apps function under one origin with no regressions:

- Both SPAs load, navigate (client-side, under their prefixes), and authenticate via the shared cookie.
- Reflekt real-time still works: the retro app connects to `/hubs/retro` with its guest token and receives events.
- Fokus dashboard features (charts, SignalR live updates on developers/daily-progress) work under `/dashboard`.
- Crossing `/dashboard` ↔ `/retro` performs a full navigation (expected — decoupled, two builds) and re-establishes auth from the cookie without a re-login.

**Accept:** `dotnet build SprintRituals.slnx` green; `npm run build` green; manual smoke of both apps under Host passes the four bullets above; no console errors on load; documented in implementation.md with the verification results.
**Skill:** None · **TDD:** no (manual verification — no automated frontend suite by project decision) · **Confidence:** medium

---

## Skill Mapping

| Step | Disposition | TDD | Notes |
|------|-------------|-----|-------|
| 1 | None | no | Workspace restructure + source move — no pattern skill |
| 2 | None | no | Vite build config — no pattern skill |
| 3 | None | no | Build-time shared-lib extraction; guest auth stays retro-local |
| 4 | None | no | Host static-file + per-prefix SPA fallback; reference existing `Program.cs` |
| 5 | None | no | Manual smoke verification (no automated frontend suite) |

**All-`None` plan** — this is a frontend-merge + Host-wiring infra pass with no matching pattern skill; the done-check's all-None exemption applies (no skill-log entries expected). No step has a `Satisfies:` AC (adhoc, no spec); each step traces to ADR-020 (routing shell / module isolation) and the two-builds owner decision recorded in `plan.md`.

---

## Plan Review (critic, Mode 2 vs ADR register — code-grounded)

**Verdict: ACCEPT-WITH-CHANGES → all six findings folded into the steps above.** Critic agent `a0b22aa795314b0ff`. Coverage: RA5-A correctly realizes the ADR-020 two-build topology; the gaps were within-scope *merge-correctness* items (not deferred work), now actioned.

| # | Severity | Finding (evidence) | Disposition |
|---|----------|--------------------|-------------|
| C1 | CRITICAL | Copied-in `useSprintHub.ts:32` → `/hubs/sprint` collides with Fokus's hub; in-repo backend serves `/hubs/retro` (`Reflekt.API/DependencyInjection.cs:74`) | Folded → Step 4 (in-scope frontend wire fix; hub path is an RA5-A invariant) |
| C2 | CRITICAL | Host `MapFallbackToFile("index.html")` (`Program.cs:76`) unscoped; no per-prefix pattern to mirror; 404s after Step 2 | Folded → Step 4 rewritten prescriptive (per-prefix `RequestPath` UseStaticFiles + prefixed fallbacks; line 76 replaced; order after endpoint maps) |
| H1 | HIGH | Absolute redirects (`client.ts:15`, `authStore.ts:34` → `/login`; Reflekt `auth.ts:25` → `/`; `RetroJoinForm.vue:88` returnUrl) bypass router base, break under prefix move | Folded → Step 2 |
| M1 | MEDIUM | Shared-helper scope ambiguous (risk of over-extracting whole fetch wrapper) | Folded → Step 3 (`/auth/me` only; each app keeps its API client + 401 policy) |
| L1 | LOW | `emptyOutDir` could wipe sibling build if pointed at `wwwroot` root | Folded → Step 2 guard note |

**Checks that came back clean (no finding):** `@`→`src` alias survives the move (relative); dep-skew hoist realistic; guest vs signed-in stores are cleanly separable (the plan's split is correct).

**Open question flagged to developer (not assumed):** cold `/` hit from an unauthenticated user with no prior module → dashboard login vs neutral launcher (recorded in Step 4; default = dashboard login).

**Status: Approved — ready for developer** (all critic findings folded; no open CRITICAL/HIGH remaining in the plan).
