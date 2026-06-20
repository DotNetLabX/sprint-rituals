# RA5-A Review (Codex)

> Recovered by the team lead from the Codex runtime job log (`task-mqm1ntv0-tt5kqd.log`) —
> Codex computed the verdict but its file write was blocked by the read-only session sandbox
> (known strand). Content below is verbatim from the run. Team lead independently verified F1/F2
> against source before routing (see communication-log.md).

Reviewed in full before code inspection:
- `docs/specs/adhoc-ReflektFrontendMerge/delivery/plan-A-merge.md`
- `docs/specs/adhoc-ReflektFrontendMerge/delivery/implementation.md`

## Focus Summary

| Area | Status | Evidence |
|---|---|---|
| 1. Two prefixed Vite builds | PASS | `client/apps/dashboard/vite.config.ts:7,21-24`; `client/apps/retro/vite.config.ts:11,34-37` |
| 2. Host static-file serving for both outputs | PASS | `src/Host/Program.cs:54-65` |
| 3. Per-prefix fallback ordering vs endpoints | PASS | `src/Host/Program.cs:71-95` |
| 4. Root shell redirect at `/` without shadowing `/api`, `/auth`, hubs, or gRPC | PASS | `src/Host/Program.cs:94-103` |
| 5. SignalR hub path fix | PASS | `client/apps/retro/src/composables/useSprintHub.ts:31-34`; `src/Services/Reflekt/Reflekt.API/DependencyInjection.cs:72-74` |
| 6. Four absolute-prefix redirect fixes | PASS | `client.ts:14-16`; `authStore.ts:29-36`; retro `auth.ts:23-26`; `RetroJoinForm.vue:88` |
| 7. Shared package wiring / auth boundary | FAIL | Findings F1-F3 |
| Step 5 acceptance evidence in implementation doc | FAIL (MISSING) | Finding F4 |

## Findings

| ID | Severity | Finding |
|---|---|---|
| F1 | BLOCKER | Shared auth-state endpoint path is wrong. Both apps delegate to the shared helper, which calls `GET /auth/me` (`client/packages/shared/src/auth.ts:13`); mounted endpoint is `GET /api/auth/me` (`Identity.API/Features/Auth/GetMe/GetMeEndpoint.cs:5`). No server alias. Signed-in cookie state resolves to `null` in both SPAs. **TL-verified:** dashboard's `apiFetch('/auth/me')` prepends `/api`; the extracted shared helper dropped that prefix → regression. |
| F2 | BLOCKER | Retro auth entry points don't match mounted Identity routes. Retro links `/auth/google/login` in `signIn()` (`retro/src/stores/auth.ts:21`) and the JoinForm CTA (`RetroJoinForm.vue:88`); server exposes `/auth/login` (`Login/LoginEndpoint.cs:13`; `Auth/AuthConfiguration.cs:30`). Retro sign-out POSTs `/auth/logout` (`retro/src/stores/auth.ts:24`); server is `/api/auth/logout` (`Logout/LogoutEndpoint.cs:6`). Retro sign-in CTAs 404; sign-out misses. **TL-verified against source.** |
| F3 | MAJOR | Shared auth contract shape inconsistent with mounted response. Shared `AuthUser` types `givenName` as required (`packages/shared/src/auth.ts`); `/api/auth/me` (`GetMe/GetMeQuery.cs`) returns `id/email/displayName/avatarUrl/role`, no `givenName`. UI falls back to `displayName`, but the type contract is wrong. (Same item the nexus reviewer rated MEDIUM.) |
| F4 | MAJOR | Step 5 acceptance evidence missing from `implementation.md`. Plan requires documented Host-backed manual smoke (`plan-A-merge.md:72-79`); implementation marks it pending while `Status: Complete`. Completion unsupported by its own acceptance criteria. |

## Verdict

**NO-GO** — build-shell mechanics are correct (Vite bases/outDirs, Host prefix serving, fallback ordering, exact-path root redirect, `/hubs/retro` fix), but the shared auth boundary is broken (`/auth/me` vs `/api/auth/me`), retro sign-in/sign-out target unmounted routes, and Step 5 smoke evidence is missing.
