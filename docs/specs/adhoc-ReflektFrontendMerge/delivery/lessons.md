# RA5-A — Lessons

## Developer Lessons

1. **PowerShell `Set-Content -Encoding utf8` writes UTF-8 BOM** — Node.js JSON parser rejects BOM-prefixed JSON files with a cryptic "Unexpected token '﻿'" error. Use `[System.IO.File]::WriteAllText(path, content, New-Object System.Text.UTF8Encoding $false)` for any JSON file that will be read by Node tooling. The `Write` tool (Claude's built-in) writes UTF-8 no-BOM correctly; only `Set-Content` has this footgun.

2. **npm workspace workspace-level dep hoisting vs app-level tsconfig paths must stay in sync** — when a workspace package (`@sprint-rituals/shared`) is introduced, three places must all agree: `package.json` dependency entry, `vite.config.ts` resolve alias, and `tsconfig.app.json` paths. Missing any one causes a build-time "module not found" or a type-check miss while the other succeeds silently.

3. **Copying in a source repo may surface pre-existing type bugs** — the Reflekt source had bugs (`hasTeam` vs `hasTeams`, `currentTeam` getter, 3-arg `createSprint` call) that were invisible in the source repo because its tsconfig was less strict. Enabling `noUncheckedIndexedAccess` in the copied app's tsconfig surfaced one more (`previousActions.ts` array index). Fix these at copy-in time; a broken build is not an acceptable hand-off.

4. **`@tsconfig/node24` is not a workspace-universal package** — the Reflekt source's `tsconfig.node.json` extended `@tsconfig/node24` which was not in the workspace root. Replace with explicit `compilerOptions` matching the in-repo pattern at copy-in time.

5. **When extracting a fetch call into a shared helper, carry the path prefix with it** — `apiFetch('/auth/me')` prepends `/api` implicitly in its `BASE_URL` constant. Extracting `fetch('/auth/me', …)` into a shared helper drops that prefix and the call silently routes to a non-existent path. Always trace where the full URL is assembled before extracting. The pattern: search the original caller's fetch wrapper for `BASE_URL` or path-building logic before writing the helper.

6. **When porting a source repo, cross-check every hard-coded auth path against the in-repo server** — the Reflekt source used `/auth/google/login` and `/auth/logout` which were correct against the Reflekt backend. The in-repo backend (Identity module) mounts `/auth/login` and `/api/auth/logout`. These are invisible at type-check time; they only fail at runtime. Audit all raw `window.location.href`, `fetch('/auth/…')`, and `?returnUrl=…` strings at copy-in time against the host server's actual endpoints.

## Architect Lessons

1. **Prescriptive Host-wiring steps survive contact better than "mirror the existing pattern."** Step 4's C2 fix worked because the plan named the exact replacement (two `RequestPath`-scoped `UseStaticFiles` + two prefixed `MapFallbackToFile` + registration-order constraint, with `Program.cs:51/76` line anchors) rather than saying "follow the existing fallback." There *was* no per-prefix pattern to mirror, and the critic caught that the bare fallback would 404 after the outDir move. Lesson: when a refactor invalidates the only in-repo precedent, the plan must spell out the new shape — a "follow existing" instruction would have produced a non-working augment.

2. **An all-`None` frontend/infra plan is a legitimate, clean done-check — the all-None exemption did its job.** No pattern skill covers npm-workspace restructure, Vite multi-build config, or ASP.NET per-prefix SPA fallback. The done-check scored skill conformance against the audit log scoped to the `developer:implement` token and found only the lifecycle skill — exactly as expected. No false "missing skill" finding. Confirms the exemption is correctly load-bearing for infra passes.

3. **Custom developer agent names skip the verify gate — re-run the builds, don't trust the self-report.** The `dev-ra5a` agent name meant the platform verify gate didn't fire, so the PASS rested on the architect re-running `dotnet build` + `npm run build` and grepping all cited file:line edits. Both were genuinely green. Lesson: when the verify gate is skipped (custom agent name, boundary-detector edge), the done-check must independently confirm build/conformance claims, never assume them. Worth flagging to the learner: a custom-agent-name → verify-gate-skip is a recurring operational seam (boundary-detector false-positive memory note is the sibling case).

4. **Plan-deferred "open question with a default" resolves cleanly at done-check when the default is stated in the step.** Step 4's cold-`/`-unauthenticated question carried `default = dashboard login` in the plan; the developer took the default and documented it. No escalation, one-line disposition. Reinforces the create-implementation-plan rule: a genuine developer-judgment point with a stated default pre-resolves what would otherwise be a deviation.

## Reviewer Lessons

1. **Shared type declarations must reflect the actual API contract, not the caller's wish.** The shared `AuthUser.givenName: string` is declared as required but `/auth/me` never returns it. The graceful `||` fallback in the consumer masked the runtime impact, but the type is a false promise. When reviewing a shared helper that wraps an API endpoint, always cross-check the declared type against the actual response DTO — not just the calling code.

2. **Type-unsafe casts that work at runtime are LOW, not ignored.** `authStore.ts` casts `sharedFetchMe()` to a richer local `AuthUser` type. The cast silences structural TS checking but is correct at runtime because the API does return the extra fields. The pattern is fragile (a future change to the shared type or the API response would break silently). File it as LOW and track for cleanup rather than elevating to MEDIUM/HIGH.

3. **Orphaned wwwroot root artifacts: trace the serving chain before calling them a risk.** `wwwroot/index.html` and `wwwroot/assets/` are orphaned build artifacts. Before flagging them as a security risk or routing hazard, trace: (a) is there an unscoped `UseStaticFiles`? (b) is there an unscoped `MapFallbackToFile`? Neither exists here — both handlers are prefix-scoped. The files are inert dead weight, not a routing hazard. Correct disposition: SAFE, cleanup follow-up.

4. **Raw URL strings in frontend code are build-invisible — always verify against the backend endpoint attribute.** `vue-tsc --build` green tells you nothing about whether `fetch('/auth/me')` matches `[HttpGet("/api/auth/me")]`. On any review touching cross-origin paths, read the actual server endpoint files and trace each raw URL string against the HTTP verb + route attribute. This is especially important on fix-cycle re-review where the developer has corrected paths but adjacent call sites may have been missed.

5. **Adjacent call sites near a fix are the first check in a re-review.** When a shared fetch helper's path changes, find every other call site that invokes the same endpoint via a different path-building mechanism (e.g., `apiFetch()` with its own `BASE_URL = '/api'`). Confirm those adjacent callers still route correctly — they may use a different convention that was not broken by the fix but could be affected by any shared-package refactor.

## Skill Gaps

None — all steps were correctly mapped as `Skill: None` (no matching pattern skill for npm workspace restructure or Host SPA-fallback wiring).
