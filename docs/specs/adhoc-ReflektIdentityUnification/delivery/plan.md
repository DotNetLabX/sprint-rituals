# Implementation Plan — adhoc-ReflektIdentityUnification (RA2)

**Status:** Updated (critic round-1 findings fixed: CRITICAL `GetSprintSummary` bypass + 2 HIGH + 2 MEDIUM; grep-acceptance re-derived exhaustively) — awaiting team-lead verification against `review-critic.md`
**Pass:** RA2 — Reflekt Integration Program (`docs/backlog.md`)
**Binding input:** `docs/architecture/decisions.md` ADR-016 / **ADR-021** / **ADR-022** + the RA2 backlog row + `questions.md` (Q1–Q5 all Resolved). No `spec.md` (adhoc).
**Grounding (re-grounded against committed RA1, HEAD `4d93287`; corrected after critic round 1):** RA1 **kept** Reflekt's `User` aggregate, `UserRepository`, `UserConfiguration`, `Users` DbSet, the `SprintHub`/`GetSprintSummary` cookie fallbacks, and the `DbInitializer` seed-`User` — those are Step-1/Step-3 targets. **But RA1 already deleted** the cookie/Google registration and the Google ticket handler: `AuthenticationConfiguration.cs` is **already guest-scheme-only** and is **not** edited by RA2 (corrected — was a stale deletion target). The identity-value-space + FK fallout below is live. **Owner-ratified adjudications (2026-06-14, see `questions.md` Q3–Q5):** Q3 = Person id everywhere — **three** helper-bypass sites repointed (`SprintHub.cs:204`, `GetSprintSummaryEndpoint.cs:25` owner comparisons + two card presence-checks), no backfill; Q4 = mechanism **(A) assembly-of-declaring-endpoint** — scheme-based **rejected**, not a fallback; Q5 = drop seed-`User`, own seed Team by a sentinel Person-id constant.

## What RA2 is — and is not

**Is:** the identity *plumbing* — remove Reflekt's own `User`/auth, make a retro participant's identity the Identity **`Person`** (for authenticated participants only), and scope Identity's global user-gate off Reflekt. Green build; guests and existing suite members both work.

**Is not:** the **SM waiting room** (ADR-021) — that is a separate later feature (`adhoc-RetroWaitingRoom`). RA2 leaves the approval step as an **unbuilt seam**. No guest `Person`, no IP/cross-session matching (ADR-022). The `Sprint`→`RetroSession` rename and Fokus linkage are RA3.

## Grounded facts (read at plan time)

- Identity's cookie (`AuthConfiguration.SignInAsync:287-293`) carries `NameIdentifier = user.Id` (**Identity User id**), `Email`, `Name`, `Role`, `avatar_url` — **not** the Person id. Every Identity `User` has a `PersonId` (1:1).
- `ActiveUserPreProcessor` (global `IGlobalPreProcessor`) loads the `User` by the int `NameIdentifier` and **401s if missing/inactive**; it early-returns (allows) only when the principal is unauthenticated or `NameIdentifier` isn't an int.
- Reflekt `Participant.UserId int?` is the FK to Reflekt's `User`; `ClaimsPrincipalExtensions.GetUserId()` parses `NameIdentifier`. Reflekt policies are scheme-based (Cookie / GuestToken), no `Role` claim.
- **gRPC not needed in RA2:** with a `person_id` claim on the cookie, Reflekt reads the Person id directly — no `IPersonService` round-trip and no client registration. ADR-016's "Reflekt becomes a 2nd `IPersonService` client" is owner-ratified as landing with `adhoc-RetroWaitingRoom` (when Reflekt must *mint* Persons), not RA2 — timing note added to ADR-016 (2026-06-14). RA2 only *reads*.
- **Ownership keys on Identity Person id, not User id (Q3, grep-confirmed):** 31 `GetUserId()` call sites, all flowing `Team.OwnerId != User.GetUserId()` / `Team.Create(name, GetUserId())` through the single helper. **Three** non-helper sites read `ClaimTypes.NameIdentifier` directly and must be repointed explicitly: `SprintHub.cs:204` and `GetSprintSummaryEndpoint.cs:25` (both **owner comparisons** — silent SM-status corruption if missed) and `EditCard`/`DeleteCard` presence-checks; plus the `JoinSprintResponse.UserId` wire DTO. All enumerated in Step 3's grep-acceptance table.

## Skill Mapping

| # | Step | Skill | Disposition | TDD |
|---|------|-------|-------------|-----|
| 1 | Remove Reflekt `User` (aggregate/repo/config/DbSet) + seed-`User` block + 2 `HasOne<User>()` FK configs + 4 stubbed `_ra1/unmapped` endpoints + dead `JoinWithGoogle`/`GetByUserAndSprintAsync` (`AuthenticationConfiguration.cs` kept as-is) | — | None (removal) | no |
| 2 | Add `person_id` claim to Identity's cookie | — | None (1 claim) | no |
| 3 | Repoint identity read: `Participant.UserId`→`PersonId`, `GetUserId`→`GetPersonId`, **3 helper-bypass repoints** (`SprintHub:204`, `GetSprintSummary:25`, `EditCard`/`DeleteCard`), wire-stable `JoinSprintResponse.UserId` | — | None (rename/repoint) | no |
| 4 | Scope `ActiveUserPreProcessor` off Reflekt via assembly-of-declaring-endpoint (Q4-A) | — | None (auth-scoping) | **yes** |
| 5 | Keep Reflekt authorization policies on the shared cookie + guest scheme | — | None (verify) | no |
| 6 | Join flow: extend single `JoinSprintEndpoint` + `Participant.Join(…, personId?)` to set `PersonId` from optional `person_id` claim; leave the gate seam | — | None (endpoint/domain) | **yes** |
| 7 | Seed-owner sentinel constant + migration (`Users` drop, FK drops, `Participant` column+index rename) + green build | `persistence-patterns` | Follow | no |

---

## Step 1 — Remove Reflekt's `User` aggregate and its own auth registration

Identity owns login (ADR-016). Delete Reflekt's parallel identity:
- `Reflekt.Domain/Users/User.cs`, `Reflekt.Persistence/Repositories/UserRepository.cs`, `Reflekt.Persistence/EntityConfigurations/UserConfiguration.cs`, the `Users` `DbSet` in `ReflektDbContext`.
- **`Reflekt.API/Auth/AuthenticationConfiguration.cs` — KEEP AS-IS, do NOT edit (re-grounding correction, was a stale deletion target).** Committed RA1 (`4d93287`) already reduced this file to a single `AddAuthentication().AddScheme<…, GuestTokenAuthenticationHandler>(GuestTokenDefaults.AuthenticationScheme, …)` — there is **no `AddCookie`, no `AddGoogle`, and no `OnGoogleTicketReceived` left to delete** (the file's own header comment records that RA1 removed the Google ticket handler). It is already exactly the RA2 target (guest scheme only). **Keep** `GuestTokenAuthenticationHandler`/`GuestTokenDefaults`.
- The auth-login feature slices RA1 left as stubbed `_ra1/unmapped` placeholders: `Features/Auth/{Login,Logout,Me}` and `Features/Participants/JoinSprintGoogle` — **delete** (login/logout/me flow through Identity's own endpoints + the shared cookie; the authed-join endpoint surface for RA2 is settled in Step 6). `Features/Participants/GetCurrentParticipant` stays (retro participant, not auth).
- **Seed-`User` block** in `Reflekt.Persistence/DbInitializer.cs:17-34` (the `seedEmail` lookup + `User.Create(...)` + `db.Users.Add`) — **remove** (the seed Team's new owner source is Step 7 / Q5's sentinel constant). Drop the `Reflekt.Domain.Users` import there.
- **Dead `User`-referencing domain/repo members (delete, Step-3 grep depends on it):** `Participant.JoinWithGoogle(string, int, int userId)` (`Reflekt.Domain/Participants/Behaviors/Participant.cs:20-34`) and `ParticipantRepository.GetByUserAndSprintAsync(int userId, …)` (`:22`) both reference the old `UserId` and have **zero live callers** (grep-confirmed). Delete both — they are RA1 lift residue from the deleted Google-join path. (Keeping them would leave `.UserId` hits the Step-3 grep-acceptance cannot clear.)

**EF FK fallout (the `User` type is also referenced by relationship configs — both must drop, not just the `User` aggregate):**
- `Reflekt.Persistence/EntityConfigurations/TeamConfiguration.cs:15` — `builder.HasOne<User>().WithMany().HasForeignKey(t => t.OwnerId)…` references the deleted `User`. Remove the navigation/FK config; `OwnerId` becomes a plain `int` column (a Person id, owned cross-module — no Reflekt-local FK). Keep `OwnerId` `IsRequired()` and the `{OwnerId, Name}` unique index.
- `Reflekt.Persistence/EntityConfigurations/ParticipantConfiguration.cs:24-25` — handled in Step 3/7 (the `HasOne<User>()` FK + the `{SprintId, UserId}` filtered-unique index → drop the FK, rename the index to `{SprintId, PersonId}`).

**Grep-before-delete (hard sub-step):** grep `UserRepository`, `Reflekt.Domain.Users`, `GetByEmailAsync`, `HasOne<User>`, `JoinWithGoogle`, `GetByUserAndSprintAsync`, and the deleted endpoint types across `src/Services/Reflekt`; list and update/remove every consumer. Zero references to the deleted `User` type / dead members required after.

**Accept:** Reflekt no longer declares a `User` type, a `Users` table, a `Users` `DbSet`, any `HasOne<User>()` config, or the dead `JoinWithGoogle`/`GetByUserAndSprintAsync` members; the four stubbed `_ra1/unmapped` endpoints and the seed-`User` block are gone; `AuthenticationConfiguration.cs` is unchanged (already guest-scheme-only) — only the guest scheme remains Reflekt-side. (RA2 adds **no** `AddCookie`/`AddGoogle` and removes none — RA1 already did.)

## Step 2 — Add a `person_id` claim to Identity's cookie

In `Identity.API/Auth/AuthConfiguration.SignInAsync` (the single sign-in point), add `new Claim("person_id", user.PersonId.ToString())` to the claims list (alongside the existing `NameIdentifier`/`Email`/`Name`/`Role`). This is the suite-wide Person anchor Reflekt reads without a cross-module query.

**Accept:** every Identity cookie carries `person_id`; existing claims unchanged (no wire break for Fokus/Identity consumers — additive only).

## Step 3 — Repoint Reflekt's identity read to Person (Q3: Person id everywhere)

**Ownership value space flips from Identity User id → Identity Person id.** Today `GetUserId()` reads `NameIdentifier` (User id) and is the *single* helper feeding both owner writes (`Team.Create(name, GetUserId())`) and owner comparisons (`team.OwnerId != GetUserId()`), so repointing the helper keeps all **31 grep-confirmed `GetUserId()` call sites** internally consistent in one edit. **But three sites bypass the helper and read `ClaimTypes.NameIdentifier` directly** — they do NOT flip with the helper and must be edited explicitly: `SprintHub.cs:204` and `GetSprintSummaryEndpoint.cs:25` are **owner comparisons** (compare to `team.OwnerId` — a value-space bug if missed, silently corrupting SM status), and `EditCard`/`DeleteCard` are presence-checks. Plus the wire DTO `JoinSprintResponse.UserId`. **All enumerated in the exhaustive grep-acceptance table below — the acceptance is derived from `grep ClaimTypes.NameIdentifier src/Services/Reflekt`, not from spot inspection.** **No data backfill** (Q3c): dev `reflekt.db` is disposable, Step 7 rebuilds; any stale pre-existing `OwnerId` row is an expected, documented break.

**Rename table (binding):**

| Old | New | Notes |
|-----|-----|-------|
| `Reflekt.Domain/Participants/Participant.cs` — `UserId int?` (property) | `PersonId int?` | still nullable — null for guests (ADR-022) |
| `ClaimsPrincipalExtensions.GetUserId()` (reads `NameIdentifier`) | `GetPersonId()` (reads **`person_id`** claim) | returns `int`; the 31 endpoint call sites flip with it |
| `Team.OwnerId` value space | Person id | column type unchanged (`int`); only the *meaning* of the value changes |
| `Participant {SprintId, UserId}` filtered-unique index | `{SprintId, PersonId}` | Step 7 migration renames the column + index |

**Edited files (the 3 the grep-acceptance won't otherwise force):**
- `Reflekt.Domain/Participants/Participant.cs` + `Behaviors/Participant.cs:30` (the `UserId = userId` init).
- `Reflekt.API/Auth/ClaimsPrincipalExtensions.cs` — `GetUserId()` → `GetPersonId()`, read the `person_id` claim, parse `int`. The 31 `User.GetUserId()` endpoint call sites become `User.GetPersonId()`.
- **`Reflekt.API/Hubs/SprintHub.cs:204` (Q3b — owner-comparison bypass, repoint explicitly):** the cookie fallback reads `cookieResult.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)` and compares the parsed value to `team.OwnerId` at `:219`. Repoint `:204` to read the **`person_id`** claim so the hub compares a Person id against `team.OwnerId` (now a Person id). Also rename the local `resolvedUserId` / `participant?.UserId` reads at `:195` to the `PersonId` property. Without this edit the hub silently 401s the legitimate owner.
- **`Reflekt.API/Features/Sprints/GetSprintSummary/GetSprintSummaryEndpoint.cs:25` (CRITICAL — second owner-comparison bypass, same bug class as SprintHub:204):** `:25` reads `User.FindFirstValue(ClaimTypes.NameIdentifier)` (User id) and `:29` computes `isScrumMaster = team.OwnerId == userId`. After Step 3, `team.OwnerId` is a Person id, so this compares Person-id ↔ User-id and silently sets `isScrumMaster = false` for the real owner (SM controls vanish on the summary screen, no error). **Repoint `:25` to read the `person_id` claim** so `:29` compares Person-id ↔ Person-id.
- **Card-endpoint presence-checks — `EditCardEndpoint.cs:61` and `DeleteCardEndpoint.cs:61` (HIGH-2, repoint for uniformity):** both are `if (User.FindFirstValue(ClaimTypes.NameIdentifier) is null) → Forbidden` gates ("is there a cookie-authenticated suite user?"), **not** owner comparisons — no value-space bug. But they are literal `ClaimTypes.NameIdentifier` hits that would defeat the zero-hit grep. **Repoint both to read the `person_id` claim** (any Identity cookie carries `person_id` after Step 2, so the gate is behavior-equivalent) — this lets the `NameIdentifier` grep reach a genuine zero rather than maintain an allow-list. (`CreateCardEndpoint.cs:59` is the sibling gate but reads `ClaimTypes.Email`, a *different* claim — see the grep table; leave it as-is, it is not a `NameIdentifier` hit.)
- `Reflekt.Persistence/Repositories/ParticipantRepository.cs` — the **only** `.UserId` reference (`p.UserId == userId` at `:24`) is the body of the dead `GetByUserAndSprintAsync(:22-24)`, which **Step 1 deletes whole**. So there is nothing to repoint here — the method's removal clears the hit. (Confirmed: no other `.UserId` filter exists in this repo.)

**Wire-stability carve-out (Q3 DTO, binding — do NOT change the serialized name):** `Reflekt.API/Features/Participants/ParticipantContracts.cs:3` declares `JoinSprintResponse(int Id, int SprintId, string DisplayName, string GuestToken, DateTime JoinedAt, int? UserId)` — `UserId` is a **wire-visible JSON field the frontend consumes** (frontend is out of RA2 scope). **Keep the serialized field name `UserId`** while the backing source property renames to `PersonId`: rename the record member but pin the JSON name with `[property: JsonPropertyName("UserId")]` (or keep the record member literally named `UserId` and map it from `Participant.PersonId`). Either way the response JSON is unchanged — non-breaking for the frontend. This is the one place the grep-acceptance's "zero `.UserId`" rule is **intentionally** not applied; call it out in implementation.md as a sanctioned exception.

**Grep-acceptance (exhaustive — derived from `grep -rn "ClaimTypes.NameIdentifier" src/Services/Reflekt` run at plan time, every hit dispositioned).** The pre-edit grep returns **6** claim-read sites (5 `NameIdentifier` + 1 `Email`); each has a disposition:

| Pre-edit site | Kind | Disposition | Post-edit |
|---|---|---|---|
| `ClaimsPrincipalExtensions.cs:9` | the helper | repoint → reads `person_id` | gone (`NameIdentifier` no longer read here) |
| `SprintHub.cs:204` | owner comparison | repoint → `person_id` (Q3b) | gone |
| `GetSprintSummaryEndpoint.cs:25` | owner comparison | repoint → `person_id` (CRITICAL) | gone |
| `EditCardEndpoint.cs:61` | presence-check | repoint → `person_id` (uniformity) | gone |
| `DeleteCardEndpoint.cs:61` | presence-check | repoint → `person_id` (uniformity) | gone |
| `CreateCardEndpoint.cs:59` | presence-check on **`ClaimTypes.Email`** | **leave as-is** (not a `NameIdentifier` hit; Email still on the cookie) | n/a — not a `NameIdentifier` match |

**Acceptance greps after the edits:**
- `grep -rn "ClaimTypes.NameIdentifier" src/Services/Reflekt` → **zero** hits (all five repointed). This is now achievable because every hit is enumerated and repointed, not asserted away.
- `grep -rn "GetUserId" src/Services/Reflekt` → **zero** (helper renamed to `GetPersonId`; 31 call sites flipped).
- `grep -rn "\.UserId" src/Services/Reflekt` → **zero in live code**, with exactly two sanctioned/cleared exceptions: (1) `JoinSprintResponse.UserId` — the **wire-stable JSON field** kept by name (carve-out above); (2) the dead `GetByUserAndSprintAsync` / `JoinWithGoogle` `UserId` references are **deleted in Step 1**, not renamed. Migration files under `Reflekt.Persistence/Migrations/` are not edited by hand (Step 7 adds a new migration; the old ones are historical).
- `grep -rn "ClaimTypes.Email" src/Services/Reflekt` → **one** expected hit (`CreateCardEndpoint.cs:59`, sanctioned presence-check).

## Step 4 — Scope `ActiveUserPreProcessor` off Reflekt

The global `ActiveUserPreProcessor` (Identity) 401s any authenticated principal whose int `NameIdentifier` isn't an active Identity `User`. A guest-scheme Reflekt request is authenticated but is **not** an Identity User → it must not be 401'd; and (ADR-021 forward-compat with the waiting room) an authenticated Person-without-User participant must also pass.

**Mechanism (A), ratified — assembly-of-declaring-endpoint (Q4):** in `ActiveUserPreProcessor.PreProcessAsync`, early-return when the matched endpoint's declaring type belongs to the **`Reflekt.API`** assembly. Resolve the endpoint from `context.HttpContext.GetEndpoint()` → its FastEndpoints `EndpointDefinition` → the declaring `EndpointType.Assembly`; compare to `typeof(<a Reflekt.API marker type>).Assembly`. This is the only discriminator that is simultaneously Reflekt-exact (no `/api/sprints/*` Fokus collision — both modules share that prefix) and forward-compatible with ADR-021's authenticated-Person-without-User case.

**Rejected (do NOT use): scheme-based skip** (skip non-cookie / guest-scheme principals). ADR-021 requires this gate to pass an *authenticated Person-without-User* participant, who carries the Identity cookie — a scheme skip fails exactly that case. Adopting it would manufacture the debt this pass exists to avoid. Not a fallback. Also rejected: route-prefix (no Reflekt-only prefix) and re-routing (RA5 scope).

**Implementation note (the one open handle):** the FastEndpoints API for reaching the declaring assembly from the pre-processor context is the part to prototype first. The guest mitigation (`GuestTokenAuthenticationHandler` sets no `NameIdentifier`, so guests already early-return at `ActiveUserPreProcessor.cs:18-19`) is a safety net, **not** the mechanism — scope explicitly via (A). If the assembly handle proves awkward, **do not fall to a scheme skip** — raise a new question to the architect; any alternative must still satisfy the ADR-021 forward-compat invariant (e.g. a marker-interface check on Reflekt endpoints).

**Accept:** a guest-authenticated Reflekt request is not 401'd by the global gate; an authenticated request to a Reflekt endpoint is not 401'd even if its `NameIdentifier` is not an active Identity `User`; Identity/Fokus endpoints still gate exactly as before (a deactivated suite user is still 401'd on a Fokus endpoint). **TDD:** (1) a guest request to a Reflekt endpoint returns its normal response, not 401; (2) a request to a Fokus/Identity endpoint with an inactive/unknown `NameIdentifier` still returns 401 (the skip is Reflekt-exact, not global).

## Step 5 — Keep Reflekt's authorization policies on the shared cookie

Reflekt's `AddReflektAuthorization` policies (`ScrumMaster`/`OwnerOrScrumMaster`/`GoogleUser` on the Cookie scheme; `Participant` on GuestToken) resolve against **Identity's** cookie scheme (registered in the Host). No policy rewrite — verify they bind to the Host's cookie and that cookie-gated Reflekt endpoints authenticate a suite member. Resource-level ownership checks now key on Person id (Step 3).

**Accept:** a suite member (Identity cookie) passes `ScrumMaster`/`OwnerOrScrumMaster`; a guest passes `Participant`; no policy references a Reflekt-local scheme.

## Step 6 — Join flow: set `PersonId` on authenticated join; leave the gate seam

**Endpoint surface (MEDIUM-5 — make it concrete):** committed RA1 has exactly **one** surviving join endpoint, `Features/Participants/JoinSprint/JoinSprintEndpoint.cs` — `[AllowAnonymous]`, `POST /api/sprints/{sprintId}/join`, always calling the guest factory `Participant.Join(displayName, sprintId)`. The authenticated-join endpoint (`JoinSprintGoogleEndpoint`) and its factory (`Participant.JoinWithGoogle`) are **both deleted in Step 1**. **RA2 sets `PersonId` by extending this single `JoinSprintEndpoint`, not by adding a new authed-join endpoint:**
- Keep `JoinSprintEndpoint` `[AllowAnonymous]` (a guest with no cookie must still join).
- In the handler, read the **optional** `person_id` claim from the cookie: `var personId = User.GetPersonIdOrNull();` (an `int?`-returning sibling to `GetPersonId()` — guests have no `person_id`, so it returns null). Add this nullable accessor to `ClaimsPrincipalExtensions` alongside `GetPersonId()`.
- Construct the participant via a single factory that takes the optional Person id: `Participant.Join(displayName, sprintId, personId)` — extend the existing `Join` factory to accept `int? personId = null` (default null = guest), setting `PersonId`. (Do **not** resurrect a separate `JoinWithGoogle`; one factory, one nullable parameter — this is also what keeps join a single interceptable step for the seam.)
- Guest (no cookie / no `person_id`) → `PersonId` null (ADR-022). Suite member (Identity cookie carries `person_id`) → `PersonId` set.
- **Seam (ADR-021):** join stays one discrete, interceptable creation point — a future SM-approval gate slots in *before* the `Participant.Join(...)` call without restructuring. Do not build the waiting room; just don't foreclose it.

**RA2 limitation (document in implementation.md):** with no waiting room yet and Identity's login invitation-gated, a **non-suite Google user cannot authenticate** into a retro in RA2 — they join as a guest until `adhoc-RetroWaitingRoom` ships. Authenticated participation in RA2 = existing suite members (who already have an Identity cookie with `person_id`). This is expected, not a defect.

**Accept:** guest join (no cookie) → `PersonId` null; suite-member join (Identity cookie) → `PersonId` set from the `person_id` claim; one `JoinSprintEndpoint`, one `Participant.Join(displayName, sprintId, personId?)` factory, join remains a single interceptable step. **TDD:** the two join paths (cookie present / absent) set `PersonId` correctly.

## Step 7 — Migration + seed-owner constant + green build

**Seed owner (Q5):** in `Reflekt.Persistence/DbInitializer.cs`, after Step 1 removed the seed-`User` block, own the seed Team by a **named sentinel Person-id constant**:
```
// Dev-only placeholder Person id. NOT guaranteed to match any real Identity Person
// (RA2 has no Identity↔Reflekt seed handshake). The seed Team exists only so the dev
// reflekt.db has a demo board; see questions.md Q5.
private const int SeedOwnerPersonId = 1;
```
`Team.Create("Dev Team", SeedOwnerPersonId)` replaces `Team.Create("Dev Team", user.Id)`. The seed Sprint/Participants/Cards hang off it unchanged.

**Migration (Follow `persistence-patterns`, seed-data + migration section; Persistence-as-startup per `project-rules.md`):** one migration that:
- **drops the `Users` table** (and its FK constraints from `Participants`/`Teams`);
- on `Participants`: **drops the `FK_Participants_Users_UserId` FK**, **renames column `UserId` → `PersonId`** (preserve `int?` nullability), and **renames the filtered-unique index** `IX_Participants_SprintId_UserId` → `IX_Participants_SprintId_PersonId` (preserve the `"PersonId" IS NOT NULL` filter) plus `IX_Participants_UserId` → `IX_Participants_PersonId`;
- on `Teams`: **drops the `FK_Teams_Users_OwnerId` FK** (the `HasOne<User>()` config is gone — `OwnerId` is now a plain Person id with no Reflekt-local FK); keep the `OwnerId` column and `IX_Teams_OwnerId_Name`.

(These FK/index drops mirror the EF config changes in Steps 1 and 3 — the migration must match the model snapshot or the build's pending-model check fails.)

**Accept (overall RA2):**
- `dotnet build SprintRituals.slnx` green (filter `error CS` — file-lock MSB errors are not compile errors, per `project-rules.md`).
- `reflekt.db` migrates: no `Users` table; no `FK_Participants_Users_*` / `FK_Teams_Users_*`; `Participant.PersonId` column + `IX_Participants_SprintId_PersonId` filtered index present.
- A guest joins a retro (`PersonId` null) and a card broadcast round-trips.
- A signed-in suite member joins a retro with `PersonId` set from the cookie's `person_id` claim, and is not 401'd by the global gate.
- `SprintHub` timer control works for the team owner (the `:204` repoint compares Person id ↔ Person id).
- `JoinSprintResponse` JSON still carries a `UserId` field (sanctioned wire-stability exception, Step 3).
- Identity + Fokus auth unaffected (the `person_id` claim is additive); Fokus's Person flow untouched.

---

## Risks & out-of-scope

- **Pre-processor handle (Step 4)** is the one implementation-time prototype: reaching the declaring assembly from the FastEndpoints pre-processor context. The **mechanism is fixed (assembly-of-declaring-endpoint)** — only the API handle is to be confirmed. A scheme-based skip is **rejected** (breaks ADR-021 forward-compat); if the assembly handle is awkward, raise a new question — do not substitute a scheme skip.
- **Value-space break with no backfill (Q3c):** any stale pre-existing `Team.OwnerId` row holds an old Reflekt-User id and will not match a Person id. Dev `reflekt.db` is disposable and Step 7 rebuilds; document this in implementation.md as expected, not a defect.
- **`IPersonService` gRPC client is NOT in RA2.** RA2 only *reads* the Person id from the new `person_id` cookie claim; it never *mints* a Person, so no gRPC client/round-trip is needed. ADR-016's "Reflekt becomes a 2nd `IPersonService` client" lands with `adhoc-RetroWaitingRoom` (when Reflekt must mint Persons) — owner-ratified timing note added to ADR-016 (2026-06-14).
- **Non-suite Google participation** is unavailable until `adhoc-RetroWaitingRoom` (Step 6 limitation) — by design.
- **Out of scope (do not touch):** the SM waiting room, guest `Person` creation, IP/cross-session matching, `IPersonService` client registration, `Sprint`→`RetroSession`, Fokus snapshot/context panel, frontend (incl. the `JoinSprintResponse.UserId` JSON field — kept stable).
