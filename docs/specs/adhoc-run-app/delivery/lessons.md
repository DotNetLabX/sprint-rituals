# Lessons — adhoc-run-app

## Solo Lessons

- **Host crashed on a clean checkout with no built frontend.** `Program.cs` passed
  `builder.Environment.WebRootPath` straight into `Path.Combine`; `WebRootPath` is
  **null** until `src/Host/wwwroot/` physically exists, and it doesn't until a Vite
  build runs. Result: `ArgumentNullException` at startup before the server bound.
  - Fix: resolve web root as `WebRootPath ?? ContentRootPath/wwwroot`, then
    `Directory.CreateDirectory` each SPA subfolder before wiring its
    `PhysicalFileProvider` (the provider also throws if its root folder is absent).
  - Collapsed the three duplicated `UseStaticFiles` blocks (dashboard/retro/poker)
    into one loop in the same pass (boy-scout, in-file).
  - Takeaway: the API must run against the Vite dev server without a prior frontend
    build. Don't assume `wwwroot` exists in dev.

- **Login always dumped every user on `/dashboard/` — return-to-origin was half-built.**
  `SignInAsync` hardcoded `Response.Redirect("/")` → `/dashboard/`, ignoring the `returnUrl`
  query param that `RetroJoinForm.vue` was *already* passing. So no one chose a "shared
  dashboard"; the intended per-app return was just never finished server-side.
  - Fix (return-to-origin): `/auth/login` now captures + validates `returnUrl` into
    `AuthenticationProperties.Items` (round-trips through the OAuth state, same as `invite_token`);
    `SignInAsync` redirects there, `/` only as fallback. Open-redirect guard allows same-origin
    relative paths or absolute URLs whose origin is in `Auth:AllowedReturnOrigins` (dev config lists
    the Vite ports 5171/5172/5173 + 5100). Each SPA now sends its own absolute page URL so dev
    logins return to the Vite origin, not the backend's built copy on 5100.
  - Scope note: this was ~7 files across Identity auth + a security validator + 3 FE apps —
    **past solo's 1-3 file lane**; done as an extended solo change at explicit owner direction.
    The right default for auth/security-surface work is the team pipeline (plan + review).

- **Retro creation is blocked by the unbuilt RA5 sprint-picker.** `CreateRetroSessionCommand`
  validator requires `FokusSprintIds` NotEmpty, but the FE form never sends the field — so every
  create 400s. **Correction (2026-10-04):** the domain also enforces ≥1 — `RetroSessionCreateTests`
  `Should_Throw_When_FokusSprintIdsIsEmpty` — matching RA3's Q3=b ruling, so relaxing the validator
  is not a stopgap. This is the documented RA5 carry-over (`Reflekt/CLAUDE.md`).
  - Data source already exists: `ISprintCatalogService.GetAvailableSprintsAsync(teamId)` →
    `SprintInfo { SprintId, Name, StartDate, EndDate, State∈Active/Closed/Future }`. Reflekt never
    calls it — no `available-sprints` endpoint, no picker.
  - Owner intent (2026-07-19): retro covers 1–2 sprints; picker defaults to last-closed sprint,
    auto-suggests name ("Sprint 31" / "Sprint 31+32"), name editable.
  - Routed OUT of solo: cross-module + multi-file + product ACs → RA5 feature (PO spec → pipeline).
    Not carried on the adhoc-run-app slug.
  - **Correction + resolution (2026-10-04):** the "team-mapping rule" blocker was wrong — Fokus's
    `SprintCatalogGrpcService` ignores `TeamQuery` (single Jira project). With that fact the picker
    fit one service, no domain change, so the owner had it built in this solo pass (~6 FE/BE source
    files — over the 1-3 rule of thumb, inside the blast-radius criterion): Reflekt
    `ISprintCatalogService` client, `GET /api/teams/{teamId}/available-sprints`, form picker
    (1–2 sprints, default newest Closed, auto-name "Sprint 39" / "Sprint 39+40", editable).
  - The form also swallowed the 400: it `emit`ted fire-and-forget and the parent's `await` rejected
    unhandled. The form now owns the store call (`teamId` prop), shows server errors, and keeps the
    name on failure. Lesson: a create form that emits and forgets cannot show a server error —
    `CreateTeamForm` already had the await-in-form shape; follow it.
  - Takeaway: "the catalog takes a team param" is not "the catalog filters by team" — read the
    server impl before routing work out on an assumed product question.

- **Test projects landed in phantom `/src/Services/...` solution folders.** `dotnet sln add` without
  `--solution-folder` derives the folder from the path, so VS showed a duplicate `src` tree next to
  `Services`. Fixed by hand in `SprintRituals.slnx`. Takeaway: add projects with
  `dotnet sln add <csproj> --solution-folder Services/<Module>`.

- **Dashboard Vite proxy lacked `/hubs`** while `dailyProgressStore` opens `/hubs/sprint` — SignalR
  failed under `dev:dashboard`. Added the ws proxy (retro/poker already had it). The `/signin-google`
  proxy entries in retro/poker are dead (callback path is `/auth/callback`, covered by `/auth`).

- **Dev OAuth now round-trips through each Vite origin** (Host header is preserved by the proxy), so
  Google's allowed redirect URIs must include `http://localhost:{5171,5172,5173}/auth/callback`.

- **No domain-event handler had ever run in this repo (all modules).** `Blocks.FastEndpoints.DomainEventPublisher`
  called `domainEvent.PublishAsync(...)` on a variable typed `IDomainEvent`; C# picks the generic
  `PublishAsync<TEvent>` with `TEvent = IDomainEvent`, and FastEndpoints dispatches by compile-time type,
  so no `IEventHandler<ConcreteEvent>` matched. Symptom: Reflekt cards saved but never appeared (no
  SignalR broadcast); same for all 21 Reflekt + 5 Poker broadcasts and Identity's
  `AnnounceCompanyDomainOnUserCreatedHandler`. Fix: cast to `IEvent` so the non-generic overload
  (runtime-type dispatch) wins. Verified with a scratch FastEndpoints 8.1 probe (0 runs → 1 run).
  - Attested: the original Reflekt (`<reflekt-repo>` `DomainEventPublishingInterceptor`) documented this
    exact trap and worked around it with reflection; the port to `Blocks.FastEndpoints` dropped it.
  - Takeaway: when porting shared infrastructure, carry over the "why" comments — they are the spec.
    No test covers event dispatch end-to-end; a Blocks-level test would have caught it at scaffold time.

- **Guests saw grouping controls the server refuses.** `canGroup`/`canUngroup` in
  `useRetroBoardActions.ts` checked only the phase policy, while every CardGroup endpoint is
  `ScrumMaster`-only and the original spec says "Only the Scrum Master can create, modify, or dissolve
  groups" (`<reflekt-repo>/docs/features/CardGrouping.md` rule 2). Guests could drag → 403 →
  "Failed to merge groups." Gated both on `isScrumMaster` (UI-only; server rule unchanged). The gap was
  inherited from the original Reflekt FE.

- **Owner decision (2026-10-04): the drop target names a merged group.** Original behaviour
  (CardGrouping.md rule 8) left merged groups untitled, labelled by the earliest card's text — which
  reads as random to the facilitator, and group-onto-group merges dropped both titles. Now
  `mergeGroups` sets the title to the target's title, or its displayed card text when untitled
  (≤100 chars). FE-only; server title rules unchanged. Deviation from the ported spec, by owner ruling.

- **Poker had no start page — spec Flow 1 (create session) was never built.** F6's plan step 10 scoped
  only the room (`/poker/:sessionId`); the server endpoints (`POST /api/poker/sessions`,
  `GET /api/poker/sprints`) and client calls (`sessionsApi.create`, store `fetchSprints`) existed but
  nothing called them, so `/poker/` rendered empty. Every F6 gate (done-check, reviewer, Codex) passed
  it. Added `StartSessionView` at `/` (sign-in gate; Sprint/Free; default sprint from the server's
  Rule 16 `defaultSprintId`). Takeaway: a done-check should walk each spec user flow to a reachable
  screen, not just verify built steps against the plan.

- **Poker room: four integration breaks no gate exercised (F6 was never run end to end).**
  1. Create: client read `{ id }`, server returns `sessionId` → room never opened (sessions still created).
  2. Get session / tickets: bodyless GETs sent `Content-Type: application/json` → FastEndpoints tried to
     bind an empty JSON body → 400 "input does not contain any JSON tokens"; curl (no header) got 200.
     Found via the browser's network log, not code reading.
  3. Sprint-bound items: `PlanningSession.AddSprintBoundItem` (Rule 19, unit-tested) had **no caller**;
     `GET .../tickets` only listed tickets. Added `POST /api/poker/sessions/{id}/tickets/sync` + SM
     "Refresh from Jira" button; SM auto-syncs on entering a sprint-bound room.
  4. Room decided SM/guest before `fetchMe` settled (App.vue fires it un-awaited); and `ItemSelected`
     carries only an id, so clients that loaded before items existed never saw the active item —
     store now reloads the session on an unknown item id.
  - Takeaway: a domain method with only test callers is a red flag for an unwired flow; and every
    client↔server call needs one real round-trip before a feature closes.

## Skill Gaps

- None found this pass.
