# Lessons — F6-PlanningPoker

> **Learner consolidation — 2026-06-29** (→ `docs/plugin-feedback/nexus-1.18.6-2026-06-29.md`)
> - `[ROUTED-TO-PLUGIN]` verify-gate / boundary-detector custom-spawn-suffix mismatch (the Improvement Proposal below) — promoted at recurrence 5x (F6, ReflektFrontendMerge, ReflektComputationToDomain, ReflektValueObjects, SprintContextPanel). Routed as a deterministic hook fix; verified still-present in live nexus 1.18.6.

## Architect Lessons

### Plan acceptance cited a doc table that doesn't exist (stale-pointer trap)
Plan Step 1 acceptance said "add the row to `docs/conventions/project-rules.md` (the `{AA}XY` table)" — but that port/DB table was never authored in that file (the project CLAUDE.md *references* "See project-rules.md for full table," which is what I cribbed from at plan time). The developer correctly couldn't add a row to a non-existent table, surfacing as a done-check gap whose true origin is **design** (my plan pointed at a stale artifact), not implementation. Lesson: when a plan step's acceptance points at an existing doc artifact, **grep that artifact at plan time to confirm the target (table/section) actually exists** — a hedge-free "add the row to X" must be verified the same way a code reference is. The plan-grounding rule ("a hedge in a plan is a deferred read") extends to doc targets, not just code.

### Done-check must re-run everything when the verify-gate is known-skipped
The SubagentStop verify-gate recorded `verdict:"skipped", reason:"unrecognized agent_type: developer-f6"` on every developer stop — `blocking_failed:false` here means **not run**, not pass. With no automated verify signal, the architect done-check is the *only* gate that actually executes the build/tests. I re-ran solution build + all 6 test projects + the 3-SPA client build firsthand (177 tests, 0 fail; all green) rather than trusting implementation.md's counts. Lesson: a custom-named spawn silently disables the verify-gate; the done-check owner must treat the implementation.md build/test claims as *unverified* and reproduce them, every time the gate is skipped.

### Verifying 27 discrete artifact claims is well-suited to a parallel Explore fan-out
Dispositioning 11 steps against on-disk code meant confirming ~27 specific file/line claims (does `customfield_10016` actually appear? is the analytics filter actually unchanged? does write-back actually precede Decided?). Running build/tests in the background while an Explore agent verified the artifact claims in parallel kept the done-check fast without sacrificing firsthand confirmation of the high-stakes items.

### Improvement Proposal (optional, for systemic issues) `[ROUTED-TO-PLUGIN]` (2026-06-29, nexus-1.18.6 feedback)
**Target:** the verify-gate's agent_type recognition (plugin-bound — `subagentStop` hook / `verify-verdict.json` producer)
**Change:** the verify-gate should strip a custom spawn suffix (e.g. `developer-f6` → `developer`) before matching `agent_type`, so a named spawn doesn't silently skip the gate and emit `blocking_failed:false` (which reads as "passed" but means "not run").
**Evidence:** F6-PlanningPoker — gate skipped on *every* developer stop (`unrecognized agent_type: developer-f6`); same class as the boundary-detector false-positive on named agents already logged in this run's runtime issues. Done-check had to fully substitute for the missing automated verify.
**Priority:** medium

## Developer Lessons

### FastEndpoints Policies() method conflict
When a module has a `Policies` class in its auth namespace, adding `global using Module.API.Auth;` causes ambiguity in `[Authorize(Policy = Policies.X)]` attributes — FastEndpoints endpoint base class exposes a `Policies()` method with the same name. Fix: omit the global using; add `using AuthPolicies = Module.API.Auth.Policies;` per endpoint file and use `[Authorize(Policy = AuthPolicies.X)]`.

### ILogger<T> not available without explicit using in class-library hubs
`Microsoft.Extensions.Logging.ILogger<T>` requires `using Microsoft.Extensions.Logging;` explicitly in class library hubs — it is NOT covered by global usings in API class-library projects. Same for `Microsoft.AspNetCore.Authentication` (for `AuthenticateAsync`). Always add explicit usings for these in Hub classes.

### SignalR Hub `AuthenticateAsync` needs separate using
`HttpContext.AuthenticateAsync` (from `Microsoft.AspNetCore.Authentication`) is an extension method that requires `using Microsoft.AspNetCore.Authentication;` — it does not come from the SignalR or Auth packages automatically.

### customfield_10016 is the canonical Jira story-points field key
Jira's REST edit API does not accept field names like `story_points` — it requires custom field ids. For the standard story-points field the id is `customfield_10016` (the same field Fokus reads via `JiraIssueFields.StoryPoints`). v1 pins this id; per-project field variance is out of scope. Never use `story_points` as a JSON property name in the edit payload.

### Hub SM gate requires httpContext.AuthenticateAsync, not Context.User
In SignalR hubs, `Context.User` reflects the handshake-time authentication (could be guest token scheme). To reliably enforce an SM gate (cookie-scheme only), call `httpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme)` explicitly on the current HttpContext rather than trusting `Context.User.FindFirstValue(...)` which may be populated from the guest scheme.

### ConcurrentDictionary connection state — static field on hub class
SignalR hub instances are created per-connection. Connection state shared across connections must be a `static readonly ConcurrentDictionary<...>` on the hub class (not instance field). This matches RetroHub's pattern.

### Poker SPA is a full standalone app — scaffold all config files
New SPAs under `client/apps/` need the full retro scaffold: package.json, tsconfig.json, tsconfig.app.json, tsconfig.node.json, vite.config.ts, index.html, env.d.ts. Each differs only in app name, base URL, and outDir. Missing any one causes `vue-tsc --build` to fail or produce an incomplete build.

### Root workspace `build` script must include new apps
The root `client/package.json` build script chains `--workspace=apps/X` calls. New apps are invisible to CI and the completion gate until their workspace is added. Always update the root build script immediately when adding a new app.

### Guest token localStorage keys must be module-scoped
Each module uses a distinct key namespace (retro: `reflekt:retro-session:{id}:token`, poker: `poker:session:{id}:token`). This prevents collision when a user has both apps open in the same browser. This is Rule 23 / ADR-016 applied to localStorage.

### `computed` must be imported at the top of `<script setup>`
Vue composables (`ref`, `computed`, `watch`) should all be imported at the top of the `<script setup>` block. Placing a `computed()` call before its import compiles under Vite but is a clarity anti-pattern caught in review.

## Reviewer Lessons

### Authenticated-participant hub identity gap: check for the "who are you" problem in any hub with dual auth schemes
When a hub accepts both cookie-auth and a custom token scheme, each scheme populates different claims. Verify that every hub method that needs a participant Guid (not just the SM gate) has a fallback resolution path for each scheme. The SM gate (cookie-only, `person_id` claim) is the obvious one to check — but voter methods (`SubmitVote`, `ChangeVote`) also need an identity and they silently assume the guest-token claim is always present. In any dual-scheme hub: read `RequireParticipantId()` (or its equivalent) and trace each auth path through it.

### Don't include session credentials (guest tokens) in list-reads accessible without auth
A record that contains an auth credential (`GuestToken`) must never appear in an anonymous list read. The join endpoint returning the new participant's own token (once, at join time) is correct. Including it in `GET /sessions/{id}` (all participants, anonymous) is a token leak. Checklist: for every DTO that carries a credential field, grep all endpoint methods that include that DTO in a response and check their auth attribute.

### The `isScrumMaster` pattern in Vue: `!== undefined` on a non-nullable int is always true
When a DTO property is `int` (non-nullable), `?? undefined` type-narrowing checks are vacuous — the value is always truthy. Comparing it against the current user's identity requires the auth store to actually expose the user's matching id. Always verify that a role-guard computed property performs a real comparison, not just a presence check.

### When refactoring a response type, check every client fetch URL — not just the payload shape
Fix 1 correctly created `JoinSessionResponse` and updated the endpoint handler, but `sessions.ts:44` still called the old URL `/join` while the server route remained `/participants`. TypeScript validates the response shape, not URL strings — the client build was green while the join call was a 404 at runtime. Checklist on any endpoint response refactor: (1) confirm the server `[HttpPost(...)]` route, (2) grep all client fetch calls to that endpoint, (3) verify the URL string matches.

## Skill Gaps

None — all referenced skills were available and invocable. `nexus-dotnet:domain-patterns` was consumed via Read channel for the hub SM-gate pattern (skill doesn't cover SignalR hub methods directly; RetroHub.cs was the authoritative reference). `create-vue-feature` references an `apiFetch` wrapper that doesn't exist in this project's apps — apps use direct `fetch`; adapted to the existing pattern. Skill covers structural layout correctly; the wrapper discrepancy is project-specific, not a skill defect.
