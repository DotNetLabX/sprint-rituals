# RA5-A — Frontend Merge & Routing Shell: Review

## Step 1 — Done-Check

**Pre-commitment predictions (before reading implementation.md):** given the plan's risk profile, the three most likely gaps were (1) the C1 hub-path fix silently missed in the copy-in, (2) one of the four H1 absolute redirects left unprefixed, and (3) the Host fallback registered before the endpoint maps (swallowing `/api`, `/auth`, `/hubs`). All three were checked specifically — none materialized.

**Verify gate note:** the platform verify gate skipped this run (custom developer agent name `dev-ra5a`), so build/conformance claims were re-confirmed by re-running `dotnet build` and `npm run build` and grepping every cited file:line — not assumed from implementation.md.

| Step | Disposition | Notes |
|------|-------------|-------|
| 1 — Establish client workspace; move Fokus, bring Reflekt in | Implemented | `client/` is an npm workspace (`workspaces: ["apps/*","packages/*"]`); `apps/dashboard`, `apps/retro`, `packages/shared/src/{auth.ts,index.ts}` all present on disk. Shared deps hoisted to root; app-unique deps (`apexcharts`+`vue-router@4` dashboard, `lucide`+`vue-router@5` retro) kept per-app. `<reflekt-repo>\web\reflekt-web` SPA source confirmed unmodified (`git status` clean for that path). Two documented deviations (build-script `run-p`→`vue-tsc --build && vite build`; `@tsconfig/node24`→inline `compilerOptions`) — both valid (root-unavailable deps, steered by team lead). |
| 2 — Two Vite builds, path-prefixed bases → `wwwroot/{dashboard,retro}` | Implemented | `base:'/dashboard/'` + `outDir:'../../../src/Host/wwwroot/dashboard'` + scoped `emptyOutDir` confirmed in dashboard `vite.config.ts`; retro `base:'/retro/'` + matching `outDir`. Dashboard router `createWebHistory('/dashboard/')`. All four H1 redirects fixed at source: `client.ts:15`→`/dashboard/login`, `authStore.ts:36`→`/dashboard/login`, `auth.ts:26`→`/retro/`, `RetroJoinForm.vue:88` returnUrl→`/retro/sprint/${id}`. Root `build` script chains both workspace builds. |
| — retro router base | Deviated (valid) | Plan said `createWebHistory('/retro/')`; impl kept retro's existing `createWebHistory(import.meta.env.BASE_URL)`, which resolves to `/retro/` from Vite's `base`. Reason documented; functionally equivalent and less invasive. |
| 3 — Shared build-time package: auth-state + thin UI kit | Implemented | `packages/shared/src/auth.ts` exports `fetchMe()` wrapping **only** `GET /auth/me` with explicit `credentials:'same-origin'`; returns `null` on non-ok (no over-extraction of the fetch wrapper — M1 honored). Both apps delegate via `@sprint-rituals/shared` (alias + tsconfig `paths` + dep entry). Guest auth (X-Guest-Token, localStorage, `accessTokenFactory`) confirmed untouched and retro-local. No shared UI kit invented (no genuine duplicate) — plan explicitly permits this. |
| 4 — Host serves both SPAs under prefixes + thin `/` entry + C1 fix | Implemented | `Program.cs`: unscoped `UseStaticFiles` replaced by two `RequestPath`-scoped providers (`/dashboard`→`wwwroot/dashboard`, `/retro`→`wwwroot/retro`); bare `MapFallbackToFile("index.html")` removed; two prefixed fallbacks added **after** `MapFokus/ReflektEndpoints` and Identity maps (`:94-95`); thin `/` `MapGet` redirect (signed-in→`/dashboard/`, else→`/dashboard/login`). **C1 verified**: `useSprintHub.ts:32` now `.withUrl('/hubs/retro', …)` — collision with Fokus hub removed. Open question (cold `/` for unauth user) resolved to dashboard login per plan default. |
| 5 — End-to-end verification | Implemented (build-verified; manual smoke deferred) | `dotnet build SprintRituals.slnx` re-run by architect: **green, 0 errors** (6 pre-existing NU1903 warnings; impl said 9 — count is not load-bearing and unrelated to this feature). `npm run build` re-run: **green** — both apps emit `index.html`+assets under their own prefixes; `vue-tsc --build` type-check passed for both; asset paths confirmed prefix-scoped (no collision). Manual browser smoke (cross-prefix auth, live SignalR, `/` redirect in browser) explicitly deferred and documented as a reviewer/operator item — acceptable: the plan's Step 5 acceptance is `dotnet build` green + `npm run build` green + documented results, all met; the four browser bullets are manual-verification items with no automated frontend suite by project decision. |

**Pre-existing-bug fixes (in-scope, documented):** `TeamSelector.vue` (`hasTeam`→`hasTeams`, `currentTeam`→`teams[0]`, `createSprint` arity) and `previousActions.ts:52-53` (undefined guard under `noUncheckedIndexedAccess`). These were required for retro's stricter type-check to build green; fixing them at copy-in is correct (a broken build is not a valid hand-off). Confirmed by the green `vue-tsc --build`.

**Skill conformance:** All-`None` plan (every step `Skill: None`, `TDD: no`). The all-None exemption applies — no pattern-skill log entries are expected. Scoped audit log (`agent ∈ {developer, main, dev-ra5a}` for the `developer:implement` token window of this run) shows the developer-role lifecycle skill `nexus:developer` only — consistent with an all-None infra pass. `## Skills Used` section present in implementation.md and matches. No fabricated invocations. No conformance gap.

**Carry-overs noted (not blockers):** legacy `wwwroot/index.html` + `wwwroot/assets/` orphaned by the move (not served by any route now; safe to delete — left for reviewer confirmation); `lucide-vue-next@1.0.0` deprecation warning (follow-up); manual browser smoke of the four Step-5 bullets (operator/reviewer).

**Verdict: PASS**

*Status: COMPLETE — architect, 2026-06-20*

---

## Step 2 — Code Review

## Reviewed By
Reviewer agent (nexus:reviewer), 2026-06-20. Cycle 2/3 re-review. All five Cycle 1 fixes verified against actual source files + backend endpoint attributes. Fresh builds run independently.

## Verdict: APPROVED

## Cycle 1 Fixes — Verified

| Fix # | Severity | Claim | Verified against | Result |
|-------|----------|-------|-----------------|--------|
| 1 | BLOCKER | `shared/src/auth.ts:13` → `fetch('/api/auth/me', …)` | `GetMeEndpoint.cs:5` `[HttpGet("/api/auth/me")]` | ✓ Match |
| 2 | BLOCKER | `retro/stores/auth.ts:21` → `window.location.href = '/auth/login'` | `LoginEndpoint.cs:13` `app.MapGet("/auth/login", …)` | ✓ Match |
| 3 | BLOCKER | `RetroJoinForm.vue:88` computed → `` `/auth/login?returnUrl=/retro/sprint/${props.sprintId}` `` | Same `LoginEndpoint.cs:13` | ✓ Match |
| 4 | BLOCKER | `retro/stores/auth.ts:24` → `fetch('/api/auth/logout', …)` | `LogoutEndpoint.cs:6` `[HttpPost("/api/auth/logout")]` | ✓ Match |
| 5 | MAJOR | `givenName?: string` (optional) in shared + retro local `User` interface | `packages/shared/src/auth.ts:8`; `retro/stores/auth.ts:4` | ✓ Fixed |

**Adjacent-area check:** dashboard `api/auth.ts` still calls `apiFetch('/auth/me')` and `apiFetch('/auth/logout')`. These go through `client.ts` which prepends `BASE_URL = '/api'`, reaching `/api/auth/me` and `/api/auth/logout` — both correct server routes. The guard at `client.ts:14` (`path !== '/auth/me'`) checks the pre-prefix path, which remains correct. No regression introduced.

## Findings

### [LOW] `getMe()` in `dashboard/src/api/auth.ts:4` is dead code
**File:** `client/apps/dashboard/src/api/auth.ts:4`
**Origin:** implementation
**Issue:** `getMe()` was the pre-shared-package auth fetch; `authStore.ts` now delegates to `sharedFetchMe()`. `getMe()` is exported but no file imports it — zero call sites. Harmless but adds noise.
**Fix:** Remove `getMe()` from `auth.ts` in a follow-up cleanup pass (RA5-B or boy-scout). No immediate action required.
**Confidence:** 92/100

### [LOW] Dashboard `authStore.ts` casts shared `AuthUser` to richer local `AuthUser`
**File:** `client/apps/dashboard/src/stores/authStore.ts:20`
**Origin:** implementation
**Issue:** Local `AuthUser` has `id: number`, `avatarUrl: string | null` that the shared `AuthUser` type doesn't declare. The `as AuthUser | null` cast silences TS structural checking. Runtime is correct — `/api/auth/me` returns both fields. `AppSidebar.vue:159-160` accesses `user.avatarUrl` correctly. Fragile if the shared type or API changes.
**Fix:** Cleanup candidate when shared type stabilizes (RA5-B or later). No immediate action required.
**Confidence:** 90/100

## Carry-Over Findings (from implementation.md)

| Carry-Over Item | Resolution |
|----------------|-----------|
| Legacy `wwwroot/index.html` + `wwwroot/assets/` orphaned | Confirmed safe (both cycles): no unscoped `UseStaticFiles` handler; no unscoped fallback. Files are inert dead build artifacts — not served. Deletion safe; out of RA5-A scope. |
| `lucide-vue-next@1.0.0` deprecation warning | Acknowledged. Non-blocking. Follow-up in RA5-B or later. |
| Manual browser smoke (four Step-5 bullets) | Marked deferred-by-policy in implementation.md — confirmed correct disposition. No automated frontend suite by project decision. |

## Positive Observations
- All three Codex-caught BLOCKER route mismatches are now correct and verified against backend endpoint attributes (not assumed from comments or documentation).
- `givenName` made optional in both shared type and retro local `User` interface — type contract now accurately reflects the API.
- Dashboard `apiFetch` path routing through `BASE_URL = '/api'` is correct and unaffected by the shared-package changes.
- C1 hub-path, H1 redirects, two-build topology, `UseStaticFiles` registration order all held from Cycle 1.
- Both builds green post-fix (confirmed in this review session).

## Gaps
- `returnUrl` parameter in `RetroJoinForm.vue:88` is sent to `/auth/login` but `LoginEndpoint.cs` ignores it (hardcodes `RedirectUri = "/auth/callback"`). Pre-existing behavior from original Reflekt source — out of RA5-A scope (route/behavior renames are RA5-C). Follow-up.
- Manual browser smoke of the four Step-5 bullets remains deferred per project policy. Documented.

## Open Questions
None.

## Evidence
| Check | Result | Command | Output |
|-------|--------|---------|--------|
| .NET build (Cycle 2) | PASS | `dotnet build SprintRituals.slnx --no-restore` | `Build succeeded. 6 Warning(s) 0 Error(s)` (pre-existing NU1903 only) |
| Frontend build (Cycle 2) | PASS | `npm run build` (at `client/`) | Dashboard + retro both emit `index.html` + assets under their prefixes; both `vue-tsc --build` clean |
| Fix 1 — `/api/auth/me` | PASS | Read `packages/shared/src/auth.ts:13` | `fetch('/api/auth/me', …)` — matches `GetMeEndpoint.cs:5` |
| Fix 2 — `/auth/login` signIn | PASS | Read `retro/stores/auth.ts:21` | `window.location.href = '/auth/login'` — matches `LoginEndpoint.cs:13` |
| Fix 3 — `/auth/login` JoinForm | PASS | Read `RetroJoinForm.vue:88` | computed `` `/auth/login?returnUrl=…` `` — matches `LoginEndpoint.cs:13` |
| Fix 4 — `/api/auth/logout` | PASS | Read `retro/stores/auth.ts:24` | `fetch('/api/auth/logout', …)` — matches `LogoutEndpoint.cs:6` |
| Fix 5 — `givenName?` optional | PASS | Read `shared/src/auth.ts:8`; `retro/stores/auth.ts:4` | Both declare `givenName?: string` |
| Adjacent auth paths (dashboard) | PASS | Grep `auth/me\|auth/logout\|auth/google/login` in `client/` | Dashboard uses `apiFetch(…)` with `BASE_URL='/api'` — routes correctly; zero `/auth/google/login` matches |

*Status: COMPLETE — reviewer, 2026-06-20*
