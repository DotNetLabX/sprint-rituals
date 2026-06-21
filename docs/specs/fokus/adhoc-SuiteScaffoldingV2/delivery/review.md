# Suite Scaffolding V2 -- Critic Review

## Mode: Plan Review
## Verdict: REVISE

Two HIGH findings require fixes before developer handoff. No CRITICAL findings. Plan is structurally sound and comprehensively carries forward all lessons from the discarded run.

---

## Pre-commitment Predictions

| Predicted Problem Area | Actual Finding |
|---|---|
| 1. Plan might still carry ASP.NET Core Identity traces | **Not found** -- plan is clean; every reference is plain DDD |
| 2. Identity field mapping gaps (AppUser fields lost in Person/User split) | **Found (MEDIUM)** -- `DisplayName` mapping is implicit but resolvable |
| 3. CompanyDomain auto-seeding on bootstrap might be missed | **Found (HIGH)** -- plan's bootstrap path omits this Fokus behavior |
| 4. CreateInvitation "existing user" check silently dropped | **Already documented** -- plan acknowledges the drop |
| 5. Sibling command/query files not listed for move to Identity | **Found (MEDIUM)** -- plan lists endpoints but not companion DTOs |

---

## Cross-reference Matrix

| Proposal Requirement (scaffold scope) | Plan Coverage | Status |
|---|---|---|
| Modular monolith Host composition (S1) | Steps 3, 9 | COVERED |
| Identity owns Person + User (S3) | Steps 5, 6, 7 | COVERED |
| Three-DB topology: identity.db + fokus.db (S7) | Steps 3, 6, 9 | COVERED (two DBs; reflekt.db correctly excluded) |
| Fokus-as-module -- all features ported (S1) | Step 8 | COVERED |
| Google OAuth + cookie auth (S3) | Step 7 | COVERED |
| Person as universal anchor (S3) | Step 5 domain model | COVERED |
| No cross-module DB FK (S7) | Step 8 (InvitedByUserId) | COVERED |
| Invitation stays in Fokus (locked decision 6) | Step 8 | COVERED |
| Frontend clean copy (S8) | Step 9 | COVERED |
| BuildingBlocks + Modules copy (S1) | Step 2 | COVERED |
| Fresh repo, no git history (S13) | Step 1 context | COVERED |

---

## Findings

### [HIGH] F1: Bootstrap path missing CompanyDomain auto-seeding (Step 7)

**Source:** Fokus `AuthConfiguration.cs` lines 108-113

**Issue:** The current Fokus bootstrap flow auto-sets `AppSettings.CompanyDomain` from the first admin's email domain when `CompanyDomain` is null. Step 7 describes the bootstrap path as "first User (count 0) -> create Person + User with Role = Admin" but does not mention reading `AppSettingsRepository` or seeding `CompanyDomain`. Since this runs in Identity's `OnTicketReceived` and `AppSettings` lives in `fokus.db` (a different DbContext), Identity's auth handler would need cross-module access to replicate this.

**Evidence -- current code** (`<fokus-repo>\src\Services\Fokus\Fokus.API\Auth\AuthConfiguration.cs`):
```csharp
var settings = await appSettingsRepository.GetAsync();
var domain = email.Contains('@') ? email.Split('@')[1] : null;
if (domain is not null && settings.CompanyDomain is null)
{
    settings.CompanyDomain = domain;
    await appSettingsRepository.SaveAsync(settings);
}
```

**Evidence -- plan Step 7:** "Bootstrap -- first User (count 0) -> create Person + User with `Role = "Admin"`." No mention of CompanyDomain.

**Impact:** First-time setup loses the auto-domain feature. The admin would have to manually configure CompanyDomain in settings -- a silent UX regression.

**Confidence:** HIGH. The plan's explicit deferred-items list does not mention CompanyDomain, so this is not an intentional omission -- it is a gap.

**Suggestion:** Either (a) explicitly defer CompanyDomain auto-seeding with a `// DEFERRED` comment in Step 7 (like the invite-token path), documenting it as a known gap so it is not silently lost, or (b) add a step note that the Host bootstrap path should resolve `AppSettingsRepository` from Fokus DI to seed the domain. Option (a) is cleaner for scaffold scope.

---

### [HIGH] F2: UserRepository missing `GetAllAsync` for GetUsers endpoint (Step 6 vs Step 7)

**Source:** Plan Step 6 repository spec vs. Step 7 endpoint list

**Issue:** Step 6 defines `UserRepository` with: `GetByIdAsync`, `GetByPersonIdAsync`, `CountAsync`, `CountActiveAdminsAsync`. Step 7 moves the `GetUsers` endpoint (`GET /api/users`) to Identity, which currently calls `AppUserRepository.GetAllAsync()`. No equivalent method exists in the planned repository.

**Evidence -- current GetUsersEndpoint** (`<fokus-repo>\src\Services\Fokus\Fokus.API\Features\Auth\GetUsers\GetUsersEndpoint.cs` line 11):
```csharp
var users = await appUserRepository.GetAllAsync(ct);
```

**Evidence -- plan Step 6 repository methods:** "Concrete ... `UserRepository` (`GetByIdAsync`, `GetByPersonIdAsync`, `CountAsync`, `CountActiveAdminsAsync`)." No `GetAllAsync`.

**Evidence -- current AppUserRepository** (`<fokus-repo>\src\Services\Fokus\Fokus.Persistence\Repositories\AppUserRepository.cs`):
```csharp
public async Task<List<AppUser>> GetAllAsync(CancellationToken ct = default) =>
    await db.AppUsers.OrderBy(u => u.DisplayName).ToListAsync(ct);
```

**Impact:** The developer will hit a compilation error when implementing `GetUsers`, which is trivially fixable. The deeper issue: `GetAllAsync` on the old model returned flat `AppUser` rows with `DisplayName`, `AvatarUrl`, `Email`, `LastLoginAt` all on one entity. In the new model, these fields are split across Person (Email, name, AvatarUrl) and User (Role, IsActive, LastLoginAt). The repository query or endpoint logic needs a join or eager load. The plan does not acknowledge this join requirement.

**Confidence:** HIGH. The method is objectively missing, and the Person+User join complexity is real.

**Suggestion:** Add `GetAllWithPersonAsync` to the `UserRepository` spec in Step 6. Note that the query must Include/join Person to populate display name, email, and avatar URL in the `UserResponse` DTO.

---

### [MEDIUM] F3: Person model lacks DisplayName -- field mapping gap (Step 5)

**Source:** Plan Step 5 domain model vs. Fokus `AppUser.cs`

**Issue:** `AppUser` has a mutable `DisplayName` field updated on each login via `RecordLogin(displayName, avatarUrl)`. The plan's Person model has `FirstName`, `LastName`, `FullName` (computed) but no `DisplayName`. Google OAuth returns `ClaimTypes.Name` as a single display name string, not first/last. The plan's `UpdateFromGoogle` behavior needs to either split the Google display name into First/Last, or store it as-is in a single field -- but the plan does not specify which.

**Evidence:** `AppUser.cs` line 7: `public required string DisplayName { get; set; }`. Plan domain model: "FirstName : string, LastName : string, FullName : string (computed)".

**Impact:** Not a blocker -- the developer can split or store -- but two developers could make different choices. The `GetMe` response currently returns `DisplayName`; the Identity version must produce the same shape for frontend compatibility.

**Suggestion:** Add a one-liner to Step 5 or Step 7 noting how Google's single `ClaimTypes.Name` maps to `FirstName`/`LastName` (e.g., "split on first space; remainder is LastName; if no space, store full value as FirstName with empty LastName").

---

### [MEDIUM] F4: Sibling DTO/validator files not listed in Step 7 move list

**Source:** Plan Step 7 vs. Fokus `Features/Auth/` file tree

**Issue:** Step 7 names the endpoints to move (GetUsers, UpdateRole, UpdateStatus) but each has a companion file containing DTOs and validators:
- `GetUsersQuery.cs` -- contains `UserResponse` DTO (used by GetUsers, UpdateRole, UpdateStatus)
- `UpdateRoleCommand.cs` -- contains `UpdateRoleRequest` + `UpdateRoleRequestValidator`
- `UpdateStatusCommand.cs` -- contains `UpdateStatusRequest` + `UpdateStatusRequestValidator`
- `GetMeQuery.cs` -- contains `MeResponse` DTO

The plan does not mention these 4 companion files (6+ types total).

**Evidence:** Grep of `<fokus-repo>\src\Services\Fokus\Fokus.API\Features\Auth` confirms all four companion files exist alongside their endpoint files.

**Impact:** Low risk in practice (build fails immediately without them), but unnecessary ambiguity.

**Suggestion:** Add a note to Step 7: "each endpoint includes its request/response DTOs and validator, following the Fokus vertical-slice pattern (port the companion command/query files)."

---

### [MEDIUM] F5: Verify gate missing second-login (regular path) test (Step 9)

**Source:** Plan Step 9, gate item 3

**Issue:** Gate item 3 tests only bootstrap: "Google OAuth login (first user) -> Person + User created in identity.db, User Role = Admin, cookie set." No gate item tests a **second** login (regular path) that exercises: finding Person by ExternalId/email, navigating Person -> User, calling `RecordLogin`, building claims from joined Person+User data. The regular login path is higher-risk than bootstrap because it requires the ExternalId lookup and Person-User navigation to work correctly.

**Impact:** A broken regular-login path passes the verify gate but fails on first real use after bootstrap.

**Suggestion:** Add gate item 3b: "Second login with same Google account -> `LastLogin` updated in `identity.db`, cookie claims match Person+User data (FullName, Email, Role, avatar_url)."

---

### [LOW] F6: ActiveUserPreProcessor status code convention unspecified (Step 7)

**Source:** Plan Step 7 vs. lessons.md `[TRACKED]` item

**Issue:** Current Fokus preprocessor returns 401 for both "not found" and "deactivated." The discarded run's review changed the deactivated case to 403. The plan mentions "401 if missing/inactive" without specifying whether deactivated should be 401 or 403.

**Suggestion:** Pick one in Step 7. Recommend 401 for both, matching current Fokus production behavior.

---

## Dimensions Summary

### Coverage
**Sound.** Every in-scope proposal element maps to a plan step. The cross-reference matrix shows full coverage across all 11 requirements. Out-of-scope items (gRPC, MassTransit, Reflekt, frontend routing split, invite-redemption-at-login) are correctly excluded and documented.

### Identity Extraction Completeness
**Two gaps found.** Missing `GetAllAsync` repo method (HIGH F2) and CompanyDomain auto-seeding (HIGH F1). All other AppUser/AppUserRepository consumers are correctly identified: auth features that move (GetMe, GetUsers, UpdateRole, UpdateStatus, Login, Logout) are listed in Step 7; consumers that stay (CreateInvitation with inviterId from claims) are rewired in Step 8; InvitedByUserId FK is correctly dropped. The Step 8 verification sub-step (grep for `AppUser` + diff `[Authorize]` attributes) is a strong safety net.

### Plain-DDD Soundness
**Clean.** Zero references to `IdentityUser`, `IdentityDbContext`, `UserManager`, `IdentityRole`, `AddIdentityCore` anywhere in the plan. Person is `AggregateRoot<int>`, User is `Entity<int>`. Role is a string claim. Domain events carry primitive ids (`int PersonId` / `int UserId`). The "Identity Reference" table explicitly lists what NOT to adopt. This fully reverses the discarded run's structural mistake.

### Lessons Regression-Proofing
**Complete.** All 11 `[TRACKED]` lessons from the discarded run are addressed:

| Lesson | Plan Step | Status |
|---|---|---|
| FrameworkReference for class library | Step 4 | Baked in |
| `[Tags]`/`ILogger` global usings | Steps 4, 8 | Baked in |
| FastEndpoints v6 multi-assembly | Step 9 | Baked in |
| No forward-ref GlobalUsings | Step 4 | Baked in |
| Hardcoded design-time conn str | Step 6 | Baked in |
| NOCASE email collation | Step 6 | Baked in |
| IsActive default true | Step 6 | Baked in |
| Pre-processor return-after-write | Step 7 | Baked in |
| Cross-service FK removal | Step 8 | Baked in |
| CLAUDE.md prerequisite | Step 4 | Baked in |
| Invite-token round-trip | Deferred, documented | Correct |

The biggest lesson -- avoid ASP.NET Core Identity -- is now a structural decision (locked decision 5), not a review fix.

### Verify Gate Adequacy
**Strong but incomplete.** The 9-point gate covers: build, migrations, bootstrap auth, frontend, Fokus API, authorization enforcement, preprocessor, Scalar multi-module, and invitations. One gap: no test for the regular login path (MEDIUM F5). Adding a second-login gate item would close the last untested integration seam.

### Ordering / Dependency Hazards
**Sound.** Identity CLAUDE.md prerequisite is called out before `create-service` (Step 4). Identity migrations run before Fokus (Step 9, Migration Notes). Host shell (Step 3) precedes module wiring (Step 9). No circular dependencies in step ordering. The plan correctly sequences: skeleton (1-3) -> domain (5) -> persistence (6) -> auth+endpoints (7) -> Fokus port (8) -> integration (9).

### Over/Under-Specification
**Sound.** No method-body pseudo-code or skill-internals restated in any step. The domain model section provides appropriate detail (it is the spec, not skill duplication). Every step has acceptance criteria. Minor under-specification on companion DTO files (MEDIUM F4) -- the plan names endpoints but not their sibling request/response types. No step is missing acceptance criteria entirely.

---

## Multi-perspective Notes

- **Developer perspective:** Steps are implementable without guessing for the most part. The main friction point is the Person+User join needed for `GetUsers`/`GetMe` responses (HIGH F2). Step 7's auth flow description is detailed enough to port from. The source pattern references (`<fokus-repo>\...`) are precise and helpful.
- **Reviewer perspective:** Acceptance criteria are verifiable. The 9-point verify gate is the strongest part of this plan versus the discarded run. The Step 8 grep/diff verification sub-step is excellent practice.
- **Skeptic perspective:** The highest-risk area is Step 7 (auth flow) -- correctly flagged as "medium confidence." The plan mitigates this with "port carefully, diff against Fokus original." The CompanyDomain auto-seeding gap (HIGH F1) is the kind of behavior that silently disappears in a port -- it is cross-module and the plan's scope boundary makes it easy to overlook.

---

## Action Items for Architect

1. **(Required)** Address F1: Add a `// DEFERRED` note for CompanyDomain auto-seeding to Step 7's bootstrap path, or specify how to handle the cross-module `AppSettingsRepository` access.
2. **(Required)** Address F2: Add `GetAllWithPersonAsync` (or equivalent) to the `UserRepository` method list in Step 6, noting the Person join/include requirement.
3. **(Recommended)** Address F5: Add verify gate item 3b for second-login regular path.
4. **(Recommended)** Address F3: Add a one-liner to Step 5 on DisplayName -> FirstName/LastName mapping from Google claims.
5. **(Optional)** Address F4: Note companion DTO/validator files in Step 7.
6. **(Optional)** Address F6: Specify 401 vs 403 for deactivated users in Step 7.

---

## Step 1 Done Check (Architect)

**Date:** 2026-06-05
**Slug:** adhoc-SuiteScaffoldingV2
**Verdict: PASS** — all 9 plan steps Implemented / Deviated-with-valid-reason. No Missing steps. All 4 locked decisions verified honored in built code (not just claimed). Routes to reviewer for Step 2.

This check verifies `implementation.md` against `plan.md` and spot-checks the built code in `this repo`. It is a conformance/done check, not a code review.

### Pre-commitment Predictions (made before reading implementation.md)

| # | Predicted most-likely gap | Held? | Evidence |
|---|---|---|---|
| 1 | Identity leakage — scaffolded auth glue pulls in `IdentityUser`/`UserManager`/`IdentityDbContext<>` base | **Did NOT materialize** | Grep across `sprint-rituals/src` for `IdentityUser\|UserManager\|RoleManager\|IdentityRole\|AddIdentityCore\|Microsoft.AspNetCore.Identity` → zero hits in source. Every `IdentityDbContext` hit is the project's own plain class inheriting `ApplicationDbContext<IdentityDbContext>` (`IdentityDbContext.cs:6`), not ASP.NET's generic base. `User : Entity<int>` confirmed (`Users/User.cs:5`). |
| 2 | AppUser→Person/User split incomplete — dangling `AppUser` ref or stale model snapshot | **Did NOT materialize** | `AppUser` survives ONLY in immutable Fokus EF migration history (`*.Designer.cs`, the historical `AddAuth`) and the new `RemoveAppUsers` drop migration. **`FokusDbContextModelSnapshot.cs` has zero `AppUser` matches** → current model is clean, RemoveAppUsers is complete. No active source references AppUser. |
| 3 | Clean-copy contamination / committed artifacts/secrets (stray fokus.db flagged) | **Partially materialized (non-blocking)** | Stray `Fokus.API/fokus.db` exists in working tree but is **gitignored & untracked** (`git ls-files \| grep .db` → empty). `appsettings.json` ClientId/ClientSecret are empty strings — no secrets. Fokus source repo `src/` is clean. |

Two of three predictions did not materialize — the structural decision to bake plain-DDD into Step 5 (rather than fix it in review) worked exactly as the plan intended. Prediction 3 surfaced the artifact the team lead flagged, judged non-blocking below.

### Locked Decision Verification (code-level, not claim-level)

| Lock | Decision | Verified | Evidence |
|------|----------|----------|----------|
| a | Plain DDD — NO ASP.NET Core Identity anywhere | **HONORED** | Grep zero hits for IdentityUser/UserManager/IdentityRole/RoleManager/AddIdentityCore/Microsoft.AspNetCore.Identity in source. `User`/`Person` are plain `Entity<int>`/`AggregateRoot<int>`. Role = string claim emitted as `ClaimTypes.Role` (`AuthConfiguration.cs:167`). DbContext is plain (`ApplicationDbContext<IdentityDbContext>`, not `IdentityDbContext<>`). |
| b | Invitation stays in Fokus (not moved to Identity) | **HONORED** | `Invitation` aggregate, `InvitationConfiguration`, `InvitationRepository`, and all 4 endpoints (Create/Get/Revoke/Validate) live under `src/Services/Fokus/`. Only Identity reference is a deferred invite-token stub in `AuthConfiguration.cs:88-95` that does NOT read the Fokus aggregate (cross-module plumbing correctly deferred). |
| c | Clean copy — Fokus source repo `<fokus-repo>` never modified | **HONORED** | `git status` in fokus shows zero `src/` changes — only `.claude/hooks`, `settings.local.json`, `docs/backlog.md`, proposals, and the pipeline artifacts under `docs/specs/adhoc-SuiteScaffoldingV2/`. No application source touched. |
| d | No secrets committed | **HONORED** | `appsettings.json` Google/Jira sections hold empty-string placeholders. Credentials handed off via user-secrets (`UserSecretsId` in Host.csproj, documented in implementation.md handoff). No `.db`, `.env`, or credential file tracked by git (repo still at single "Initial commit"). |

### Per-Step Conformance Dispositions

| Step | Disposition | Basis |
|------|-------------|-------|
| 1 — Repo skeleton + `.claude`/`docs` seed | **Implemented** | `CLAUDE.md`, `SprintRituals.slnx` (17 projects), `.claude/`, `docs/{conventions,architecture,proposals}` present. Identity `CLAUDE.md` preserved (not overwritten — the Step-4 prereq). `.gitignore` not overwritten. |
| 2 — BuildingBlocks + Modules | **Implemented** | All 6 Blocks + 4 module projects copied; verify-gate item 1 = 0 CS errors across 17 projects. |
| 3 — Host skeleton | **Implemented** | `Host.csproj` (Web SDK net10), `Program.cs`, two connection strings (`IdentityDatabase`→identity.db, `Database`→fokus.db), launchSettings port 5000. (Built out fully in Step 9 per plan.) |
| 4 — Identity service skeleton | **Implemented (Deviated, valid)** | 3-project split with correct refs. Deviation: stub written then fully wired in 6–7 rather than as an isolated no-op pass — same end state, sequencing merged for efficiency. Valid. |
| 5 — Identity domain (plain DDD) | **Implemented** | Person aggregate (partial split + Behaviors: CreateFromGoogle/UpdateFromGoogle/LinkUser), plain User entity (CreateFromPerson/RecordLogin/Deactivate/Reactivate), `PersonSource` enum, events carry primitive ids. Matches the Identity Domain Model section. F3 Google name mapping present (`AuthConfiguration.cs:64-81`). |
| 6 — Identity persistence | **Implemented** | Plain `IdentityDbContext`, Person/User configs (NOCASE email, 1:1 Restrict FK, IsActive default true), repos incl. F2's `GetAllWithPersonAsync`, hardcoded design-time factory, `InitialCreate` migration generated. Deviation 3 (UseCollation on property not index) is a valid EF-Core-10 API constraint — documented, migration DDL confirmed. |
| 7 — Identity auth + endpoints | **Implemented** | `AuthConfiguration` bootstrap + regular-login paths both real (not stubbed) — verified `AuthConfiguration.cs:97-183`. `ActiveUserPreProcessor` (401 missing/inactive). Login/Logout/GetMe + GetUsers/UpdateRole/UpdateStatus with companion command/query files (F4). F1 CompanyDomain + invite-token correctly `// DEFERRED` stubbed. 401-for-both per F6. |
| 8 — Port Fokus as module | **Implemented** | Web SDK→class library, auth/AppUser removed, Invitation kept. CreateInvitation rewired to claims (`ClaimTypes.NameIdentifier`, `CreateInvitationEndpoint.cs:32`), `[Authorize(Roles="Admin")]` intact. InvitedByUserId FK dropped. `RemoveAppUsers` migration present; model snapshot clean. |
| 9 — Host composition + frontend + VERIFY GATE | **Implemented (partial-gate, valid handoff)** | Multi-assembly FastEndpoints (55 endpoints registered — both modules), multi-DbContext migration (Identity first), AppSettings seed, Scalar gated to Dev, Vite `outDir`→Host/wwwroot. Gate items 1/2/8 PASS with observed evidence; 3/3b/4/6/7/9 require live Google creds + browser → correctly handed to user per the plan's own Cross-Repo Execution Note 4 (unattended runs cannot pass auth-gated items). This is a documented, plan-sanctioned handoff, not a skipped step. |

No step is **Missing**. No step is unaccounted for.

### Developer Deviations — Validity Judgments (7 documented)

| # | Deviation | Valid? | Note |
|---|-----------|--------|------|
| 1 | Step 4 stub then wired in 6–7 | **Valid** | Same end state; pure sequencing. |
| 2 | `LoginEndpoint.MapLoginEndpoint(app)` called directly vs wrapper | **Valid** | Resolves CS0121 ambiguity; cleaner. Endpoint route unchanged (public surface preserved). |
| 3 | `UseCollation("NOCASE")` on property not index | **Valid** | EF-Core-10 API constraint, not a design change; NOCASE behavior preserved (the actual lesson requirement). |
| 4 | `InvitedByUserId` `.IsRequired()` removed | **Valid** | `int` non-nullable by default; fixed a real `PendingModelChangesWarning`. Snapshot now clean (verified). |
| 5 | `Path.IsPathRooted` guard added to startup data-dir logic | **Valid** | Defensive correctness for relative `data/fokus.db`; matches Fokus pattern + hardening. |
| 6 | `Fokus.Domain.Auth.Invitation.Create` fully qualified | **Valid** | Disambiguates from `System.IO.FileInfo.Create` (implicit usings). No behavior change. |
| 7 | `.claude/{hooks,commands}` + `settings.local.json` NOT copied | **Valid** | Machine-local/dev-env-specific; copying blanket-permission settings.local.json into a new repo would be wrong. Sound judgment; does not violate the Step-1 "seed `.claude`" intent (agents/rules/conventions/skills were copied). |

All 7 deviations are valid — none is a silent scope or requirement change. The `UserCreated`-not-raised item is documented as a Key Decision (User is `Entity`, no DomainEvents list) and logged as a carry-over for the architect; acceptable for scaffold scope.

### Carry-Over Findings — Classification Check (5)

| Finding | Sev | Routed to | Correctly classified? |
|---------|-----|-----------|----------------------|
| NU1903 Microsoft.Build.Tasks.Core advisory | low | reviewer | **Yes** — transitive, identical in Fokus source, non-actionable in scaffold. |
| CS9107 DeveloperRepository ctor warning | low | reviewer | **Yes** — pre-existing in Fokus source, not introduced here. |
| `UserCreated` event not raised | low | architect | **Yes** — architectural note for infra increment; valid for scaffold. |
| CompanyDomain auto-seed deferred | medium | architect | **Yes** — matches plan F1 deferral; `// DEFERRED` comment confirmed in code; CompanyDomain enforcement survives on the Fokus invitation side (`CreateInvitationEndpoint.cs:19-23`) exactly as planned. |
| Invite-token redemption deferred | medium | architect | **Yes** — matches plan; stub returns `/invite/error?reason=not-implemented` (verified `AuthConfiguration.cs:90-94`). |

All 5 carry-overs are correctly classified and correctly routed. None masks a Missing step.

### Team-Lead Caveats — Judgments

**(i) Verify-gate item 8 "PASS (partial)" — Scalar 400 without Google creds.**
**Accepted, non-blocking.** Both module assemblies are scanned and **55 endpoints register** (the load-bearing evidence that both modules compose into one Host). The 400 originates from Google OAuth middleware validating empty `ClientId` options on every request — an unavoidable consequence of the plan's own constraint (Cross-Repo Execution Note 4: auth-gated items cannot pass unattended). The Host process itself is healthy and bound. Full Scalar render is part of the same user-handoff bundle as items 3/3b/4/6/7. Does not block the done check.

**(ii) Stray `Fokus.API/fokus.db` outside `Host/data/`.**
**Acceptable-for-scaffold, non-blocking — with a cleanup note.** The file is a working-tree dev artifact (created when an EF/design-time command ran with the relative `Data Source` from the Fokus.API directory). It is **gitignored and untracked** — `git ls-files | grep '\.db$'` returns nothing; the repo is still at a single "Initial commit". It cannot reach a commit under the current `.gitignore`, so it does not violate locked decision (d) or the clean-copy intent. Recommend deleting it before the user's first real commit (cosmetic hygiene), but it is not a done-check blocker.

### Conclusion

**PASS.** Every plan step is Implemented or Deviated-with-valid-reason; zero Missing. All four locked decisions are honored in the built code, verified by grep + file reads rather than trusting implementation.md. The two team-lead caveats are both non-blocking. The auth-gated verify items (3/3b/4/6/7/9) remain a plan-sanctioned user handoff requiring real Google credentials. Routes to **reviewer** for Step 2 code review.

**For the reviewer (focus areas):** (1) Step 7 `AuthConfiguration.HandleTicketReceivedAsync` is the highest-risk code — review the regular-login ExternalId→email→User navigation and claims construction. (2) Confirm `GetAllWithPersonAsync` join shape and the `UserResponse` write-back `Id`=`User.Id` contract. (3) NU1903 + CS9107 carry-overs are pre-existing/transitive — verify, don't chase. (4) Sanity-check the stray `Fokus.API/fokus.db` is removed or remains gitignored.

---

## Step 2 Code Review (Reviewer)

**Date:** 2026-06-05
**Slug:** adhoc-SuiteScaffoldingV2
**Verdict: REQUEST CHANGES** — 1 HIGH finding. 2 MEDIUM, 3 LOW (non-blocking).

---

### Architect Focus Areas — Disposition

| Focus Area | Verdict | Notes |
|---|---|---|
| `AuthConfiguration.HandleTicketReceivedAsync` regular-login path | **Correct** | ExternalId→email fallback lookup is sound. Person→User navigation is correct (separate `GetByPersonIdAsync` call after person found). Missing/inactive rejection redirects match plan spec. Claims construction (`NameIdentifier = User.Id`, Email, Name, Role, avatar_url) is faithful. |
| `GetAllWithPersonAsync` join shape | **Correct with deviation** | Join is an inner join on `PersonId`, `.AsNoTracking()` present. `Id = p.User.Id` confirmed — `UserResponse.Id` maps to `User.Id`; UpdateRole/UpdateStatus consume it via `GetByIdAsync(req.Id)`. Write-back contract is sound. Ordering: plan says "by `Person.FullName`"; actual is `FirstName` then `LastName` — necessary and correct (FullName is EF-ignored computed property). Noted as LOW. |
| NU1903 + CS9107 carry-overs | **Confirmed pre-existing** | `NU1903 Microsoft.Build.Tasks.Core` advisory is identical in Fokus source (transitive from EF Design). `CS9107 DeveloperRepository` warning originates in Fokus source at `Fokus.Persistence/Repositories/DeveloperRepository.cs(6,57)` — not introduced by this work. No action required. |
| Stray `Fokus.API/fokus.db` | **Gitignored, untracked** | Confirmed via `git ls-files --others --ignored`: `src/Services/Fokus/Fokus.API/fokus.db` is in the ignored list; `git ls-files` returns no `.db` entries. Will never reach a commit. Cosmetic cleanup recommended before user's first commit (non-blocking). |

---

### Findings

#### [HIGH] H1 — `PersonCreated` event carries `PersonId = 0`

**File:** `src/Services/Identity/Identity.Domain/Persons/Behaviors/Person.cs:21`

**Evidence:**
```csharp
var person = new Person { ... };          // Id = 0 (not yet persisted)
person.AddDomainEvent(new PersonCreated(person.Id));  // PersonId = 0
return person;
```
`person.Id` is the EF auto-generated int PK. At the time `CreateFromGoogle` is called, the entity has not been saved — EF sets the generated Id only after `SaveChangesAsync` completes. The `PersonCreated` event object is constructed with `PersonId = 0` and stored in the aggregate's `DomainEvents` list.

The `DispatchDomainEventsInterceptor` fires in `SavedChangesAsync` (post-save), so EF *has* updated `person.Id` on the entity by dispatch time — but the already-constructed event record is immutable (`sealed record`). Any handler receiving `PersonCreated` will see `PersonId = 0`, not the real database Id.

**Impact:** Harmless in the current scaffold (no `IEventHandler<PersonCreated>` is registered). However, the infra increment will wire SignalR broadcast or audit handlers against this event. When it does, every handler will operate on Id=0 — a silent data integrity bug that will be hard to trace back to this line.

**Suggested fix:** Move the `AddDomainEvent` call out of the factory method and into the handler, after `SaveChangesAsync` returns:
```csharp
// In AuthConfiguration.cs bootstrap path, after SaveChangesAsync:
await personRepository.AddAsync(person);
await personRepository.SaveChangesAsync();
person.AddDomainEvent(new PersonCreated(person.Id));  // Id is now set
```
Alternatively, the `DispatchDomainEventsInterceptor` could be enhanced to re-read `aggregate.Id` at dispatch time, but the simpler fix is to delay event construction until after save.

---

#### [MEDIUM] M1 — `User.Role` mutated via public setter, bypassing domain intent

**File:** `src/Services/Identity/Identity.API/Features/Auth/UpdateRole/UpdateRoleEndpoint.cs:28`
**Also:** `src/Services/Identity/Identity.Domain/Users/User.cs:8`

**Evidence:**
```csharp
// UpdateRoleEndpoint.cs:28
user.Role = req.Role;

// User.cs:8
public string Role { get; set; } = "Manager";  // public setter
```

The coding conventions state: "No public setters on entities. State changes go through intention-named methods." `User.Deactivate()` and `User.Reactivate()` follow this pattern correctly; `Role` does not. The public setter also violates the domain conventions section: "Collection navigation properties exposed as `IReadOnlyList<T>` — never the internal" (same spirit applies to scalar state).

**Impact:** No runtime defect in the scaffold. The risk is that `User.Role` becomes writable from anywhere — future handlers can bypass validation logic that may be added to a `ChangeRole(string role)` method.

**Suggested fix:** Add `public void ChangeRole(string role) { Role = role; }` to `Users/Behaviors/User.cs` and change the setter to `private set`. Call `user.ChangeRole(req.Role)` in the endpoint.

---

#### [MEDIUM] M2 — `GetAllWithPersonAsync` uses LINQ `Join` instead of `Include`, breaks if navigation is added later

**File:** `src/Services/Identity/Identity.Persistence/Repositories/UserRepository.cs:22-33`

**Evidence:**
```csharp
.Join(db.Persons,
    u => u.PersonId,
    p => p.Id,
    (u, p) => new { User = u, Person = p })
```

The plan spec (Step 6) says "Include(Person)" and the architect's focus area asks to confirm the join shape. The implementation uses a manual LINQ `Join` instead of `.Include()`. This is functionally correct for the current anonymous-projection query, but it deviates from EF Core conventions for two reasons: (1) it returns raw entity objects (`User`, `Person`) outside the DbContext's tracking graph — fine since `.AsNoTracking()` is present, but it means EF's navigation-property auto-population won't work if the return type ever changes to use navigations; (2) the inner join silently drops any User whose Person row is missing (unlikely given FK constraints, but worth flagging).

**Impact:** Functionally correct for the current use case. Would become a footgun if a `HasOne<Person>()` navigation property is later added to `User` and callers expect it to be populated.

**Suggested fix (optional for scaffold):** Either accept the manual join with a comment explaining why `Include` wasn't used (FullName is computed/ignored, so `Select` projection is needed anyway), or restructure to `db.Users.AsNoTracking().Include(u => u.Person)` once a navigation property is added. For now, add a comment: `// Manual join used because Person.FullName is a computed property ignored by EF; Include would still require a Select projection.`

---

#### [LOW] L1 — `ActiveUserPreProcessor` missing `return` after `CompleteAsync`

**File:** `src/Services/Identity/Identity.API/Auth/ActiveUserPreProcessor.cs:37`

**Evidence:**
```csharp
await httpContext.Response.CompleteAsync();
// method ends here — no explicit return
```

The plan Step 7 explicitly carries the lesson: "return immediately after every response write." The Fokus original and the plan's `[TRACKED]` lesson both mandate this. The method ends after `CompleteAsync`, so there is no double-write — but omitting `return` after `CompleteAsync` is inconsistent with the established pattern and with the explicit lesson the plan baked in.

**Suggested fix:** Add `return;` after `CompleteAsync()`.

---

#### [LOW] L2 — `GetAllWithPersonAsync` ordering deviation from plan (informational)

**File:** `src/Services/Identity/Identity.Persistence/Repositories/UserRepository.cs:29-30`

Plan spec says "ordered by `Person.FullName`". Implementation uses `.OrderBy(x => x.Person.FirstName).ThenBy(x => x.Person.LastName)`. This is the correct fix — `FullName` is a computed property marked `builder.Ignore(p => p.FullName)` in `PersonConfiguration`, so EF cannot translate it to SQL. The decomposed ordering is functionally identical. Noting as deviation per plan-conformance dimension; no fix required, but the Key Decision log in `implementation.md` could document this.

---

#### [LOW] L3 — `PersonCreated` event not raised for regular-login Person updates (informational)

**File:** `src/Services/Identity/Identity.Domain/Persons/Behaviors/Person.cs` — `UpdateFromGoogle`

`UpdateFromGoogle` mutates `FirstName`, `LastName`, `PictureUrl` but raises no domain event. This is acceptable for scaffold scope — no handlers exist. Noting for the infra increment if a `PersonUpdated` event is needed for cache invalidation or audit.

---

### Carry-Over Verification

| Finding | Pre-existing? | Verdict |
|---|---|---|
| NU1903 `Microsoft.Build.Tasks.Core` | Yes — identical advisory in `<fokus-repo>` source | Confirmed pre-existing. Do not chase. |
| CS9107 `DeveloperRepository` constructor | Yes — originates in Fokus source `Fokus.Persistence/Repositories/DeveloperRepository.cs:6` | Confirmed pre-existing. Do not chase. |

---

### Summary

| Severity | Count | Items |
|---|---|---|
| CRITICAL | 0 | — |
| HIGH | 1 | H1: PersonCreated event carries PersonId=0 |
| MEDIUM | 2 | M1: User.Role public setter; M2: LINQ Join vs Include note |
| LOW | 3 | L1: missing return after CompleteAsync; L2: ordering deviation; L3: UpdateFromGoogle no event |

**H1 must be fixed before the infra increment wires any `PersonCreated` handler.** It is latent — not observable in the scaffold — but will silently corrupt any handler that consumes `PersonCreated.PersonId`. M1 and M2 are code-quality issues; M1 (public setter) should be addressed before the domain stabilises further. L1 is a tracked-lesson violation that is a one-line fix.

---

## Step 2 Re-Review (Cycle 1)

**Date:** 2026-06-05
**Verdict: APPROVED**

All five findings resolved. No new CRITICAL or HIGH issues. One infra-increment note recorded below.

---

### Finding Dispositions

| Finding | Verdict | Evidence |
|---|---|---|
| H1 — PersonCreated carries PersonId=0 | **Resolved** | See detail below. |
| M1 — User.Role public setter | **Resolved** | `User.cs:8` — `private set` confirmed. `Behaviors/User.cs:16-19` — `ChangeRole(string role)` added. `UpdateRoleEndpoint.cs:28` — calls `user.ChangeRole(req.Role)`. Last-admin protection reads `user.Role` before the call (line 21) — still correct because the read precedes the mutation. |
| M2 — GetAllWithPersonAsync missing rationale comment | **Resolved** | `UserRepository.cs:17-22` — XML doc updated: "Manual join used because Person.FullName is a computed property ignored by EF; Include would still require a Select projection." Ordering description corrected to "FirstName then LastName". |
| L1 — missing return after CompleteAsync | **Resolved** | `ActiveUserPreProcessor.cs:38` — `return;` present immediately after `CompleteAsync()`. |
| L2 — ordering deviation undocumented | **Resolved** | Key Decision #8 added to `implementation.md` explaining the EF-ignored FullName constraint. |

---

### H1 Detail — Double-Save Approach

**Verified:** `Behaviors/Person.cs:30` — `RaiseCreated()` calls `AddDomainEvent(new PersonCreated(Id))`. `AuthConfiguration.cs:104-107` — bootstrap path calls `personRepository.SaveChangesAsync()` first (EF assigns PK), then `person.RaiseCreated()`, then a second `personRepository.SaveChangesAsync()` (interceptor dispatches the event post-save).

**Event carries real Id:** Confirmed — `person.Id` is non-zero by line 106 because EF has already executed the INSERT and populated the identity column via the first save on line 104.

**Interceptor dispatch:** `DispatchDomainEventsInterceptor.SavedChangesAsync` fires after the second save. The person entity is still tracked in the same DbContext scope (scoped DI, same request), so `ChangeTracker.Entries<IAggregateRoot>()` includes it with `DomainEvents.Count == 1`. Event dispatches correctly.

**Single creation site:** `CreateFromGoogle` is called in exactly one place — `AuthConfiguration.cs:102`. No other path creates a Person without the paired `RaiseCreated()` + save. Verified by grep (one definition, one call site).

**Double-save acceptability:** Acceptable for scaffold scope. The two saves are semantically distinct (INSERT person, then dispatch creation event) and the transaction boundary is implicit per-save. The approach is consistent with the pattern used later in the bootstrap path for `LinkUser` (a third save on line 114). Worth noting for the infra increment: if `PersonCreated` gains a handler that itself calls `SaveChangesAsync` on the same DbContext (e.g., a SignalR broadcaster does not, but an audit writer might), the nested save could cause unexpected interceptor re-entry. A comment in `RaiseCreated()` already guides callers; no action needed now.

---

### No New Issues Found
