# Review — adhoc-Pass4-IdentityExtraction

## Step 1 — Done-Check

**Method:** Each plan step verified **present in source on disk** (not just claimed in implementation.md). Read `implementation.md` once; grepped/read the actual source for each step's primary artifact + the code-grounded resolutions baked into the plan (H1, M2, M3, L1, L2, L3, L5). Build reported green by developer at commit `6fcd54f`; this done-check verifies conformance/presence, not a re-build.

**Pre-commitment predictions (made before reading source):** (1) M3 `MapGrpcService` reachable from `Program.cs` likely skipped; (2) H1 SQLite filtered unique index likely unfiltered/missing; (3) Step 12 Fokus/CLAUDE.md half likely skipped. **All three checked out as present** — none was the predicted gap.

| Step | Disposition | Notes (evidence) |
|------|-------------|------------------|
| 1 — Promote `User` to AggregateRoot + raise `UserCreated` | Implemented | `User.cs:5` `: AggregateRoot<int>`; `Behaviors/User.cs:42` `RaiseCreated() => AddDomainEvent(new UserCreated(Id))`; migration `20260609192647_PromoteUserToAggregate` on disk adds the 4 audit columns (M1 — mandatory migration present). |
| 2 — Define both gRPC contracts (M2) | Implemented | `IInvitationService.cs` + `IPersonService.cs` use `[ServiceContract]`/`[OperationContract]`, **`Task<T>` + `CancellationToken ct = default`**; DTOs use **`[DataContract]`/`[DataMember(Order=n)]`**; `InvitationRedeemOutcome` enum has all 6 values incl. `EmailMismatch=5`. `grep ProtoContract\|ValueTask\|CallContext Blocks.Contracts` → **zero** (M2 satisfied). |
| 3 — Build gRPC client infra + register clients (M4/L4) | Implemented | CPM adds `protobuf-net.Grpc` + `Grpc.Net.Client` (NOT `Grpc.Net.ClientFactory` — M4); `Blocks.AspNetCore/Grpc/GrpcClientRegistrationExtensions.cs` uses `GrpcChannel.ForAddress(url).CreateGrpcService<T>()`; config under `Grpc:Services:{Invitation,Person}:Url` (L4). |
| 4 — `IPersonService` server in Identity (M3) | Implemented | `Identity.API/Grpc/PersonGrpcService.cs`: Jira match by `ExternalId+Source`, Google by email; `CreateFromJira`/`CreateFromGoogle`; `RaiseCreated()` post-save. **M3:** `DependencyInjection.cs:25` `AddCodeFirstGrpc()` (idempotent comment), `:43` `MapGrpcService<PersonGrpcService>()`, reached from `Program.cs:40` `app.MapIdentityGrpcServices()`. |
| 5 — `IInvitationService` server in Fokus (L1/L2) | Implemented | `Fokus.API/Grpc/InvitationGrpcService.cs`: all failure outcomes pre-checked (NotFound/Used/Revoked/Expired/EmailMismatch) **before** `Invitation.Accept(now)` — no `DomainException` across the boundary (L1); `EmailMismatch` is the redeem-only outcome (L2); mapped via `MapGrpcService<InvitationGrpcService>` (`Fokus DI:113`). |
| 6 — Replace redemption stub in auth callback | Implemented | `grep "not-implemented" Services/Identity` → **zero** (stub gone); `AuthConfiguration.cs` decomposed into `HandleInviteRedemptionAsync`/`HandleBootstrapAsync`/etc.; existing-active-user fast-path ignores token (Q5). |
| 7 — Company-domain seed integration event | Implemented | `Blocks.Contracts/IntegrationEvents/FirstAdministratorRegistered.cs` — plain record `(string CompanyDomain)`, mirrors `SprintSynced`. |
| 8 — `IEventHandler<UserCreated>` publishes seed (L3) | Implemented | `AnnounceCompanyDomainOnUserCreatedHandler.cs`: gate `Role=="Admin" && CountActiveAdminsAsync()==1`; domain via `email.Split('@')`; public-provider skip; null-guards `person?.Email is null` (line 50) before split. **L3:** raise is post-save in both User-creation paths (`AuthConfiguration.cs:146-147` redemption, `:179-180` bootstrap). |
| 9 — Fokus consumer + bus topology (L5) | Implemented | `SeedCompanyDomainOnFirstAdministratorRegisteredConsumer.cs` — `sealed IConsumer<FirstAdministratorRegistered>`, idempotent (seeds only if null), **mutates the loaded `AppSettings` via Get/SaveAsync, no `new AppSettings{}`** (L5); `AddConsumers(...)` added to the single existing `AddMassTransit` (`Fokus DI:36-38`), no second bus. |
| 10 — Jira email best-effort fetch + `Person.Email` optional (H1) | Implemented | `Person.cs:15` `Email` → `string?`; `PersonConfiguration.cs:20-24` `IsRequired()` removed + **filtered** unique index `HasFilter("[Email] IS NOT NULL")` (H1 scoped index present); `CreateFromJira`/`UpdateFromJira` in `Behaviors/Person.cs`; `GetByExternalIdAndSourceAsync` in `PersonRepository`; `JiraUser.EmailAddress` + `IJiraClient.GetUserAsync` + Refit `GET /rest/api/3/user`; `Developer.Email`; migrations `MakePersonEmailOptional` + `AddDeveloperEmail` on disk. |
| 11 — Wire Fokus Jira-sync → `IPersonService` upsert | Implemented | `SprintIssueSyncService.cs:34` injects `IPersonService`; `UpsertPersonBestEffortAsync` (`:210`) sends `ExternalId=developer.Id`, `Source="Jira"`, best-effort email; called from both `SyncAsync` (`:170`) and `SyncEpicDiscoveryAsync` (`:130`); gRPC failure caught/logged, doesn't abort the batch. |
| 12 — Documentation: two-contract topology + clear deferred lists (M5) | Implemented | `Identity/CLAUDE.md` updated (domain model, three login paths, gRPC server/client, published event); **`Fokus/CLAUDE.md` updated** — gRPC clients (`IPersonService` client), gRPC server (`IInvitationService` server), consumed `FirstAdministratorRegistered`, contract topology (M5 satisfied); root `CLAUDE.md` deferred bullet removed. |

### Deviations validated (developer-reported)

1. **EF migration startup project (`-s Identity.Persistence`, not `-s Identity.API`; `Microsoft.EntityFrameworkCore.Design` added to `Fokus.Persistence.csproj`).** **VALID — convention-compliant, not a violation.** `docs/conventions/project-rules.md:67` (clarified in commit `0ad86be`) states: *"If the API project is a class library that does not include [Design], use the Persistence project as startup instead (`-s …Persistence`), provided it has an `IDesignTimeDbContextFactory`."* `Identity.Persistence` has a `DesignTimeDbContextFactory`; using it as startup is the sanctioned path. The plan itself anticipated "the Persistence-as-startup variant per `project-rules.md`" (Migration Notes, Step 10). Adding Design to `Fokus.Persistence` (PrivateAssets=all) so Fokus migrations follow the same supported pattern is consistent with the convention. Disposition: **Deviated (with valid, convention-backed reason) → pass.**

2. **Step 10 executed before Steps 4/5 (order 1→2→3→10→4→5→…).** **VALID — honors the plan's declared dependency graph.** The plan's Step 4 listed *"Dependency: Steps 2, 3, 10"* and Step 10 stated *"the Person.Email-nullable migration must land before Step 4's server stores email-optional Persons."* PersonGrpcService (Step 4) consumes `CreateFromJira` + nullable `Person.Email` (both produced by Step 10) — verified present, nothing skipped by the reorder. Disposition: **Deviated (sanctioned reorder, no skip) → pass.**

### Code-grounded resolutions — all confirmed in source
H1 (filtered unique index + ExternalId key + nullable Email), M2 (`[DataContract]`/`Task`/`CancellationToken`, zero protobuf-net attrs), M3 (committed `AddCodeFirstGrpc` + `MapGrpcService` reached from `Program.cs`), L1 (pre-check, no throw across gRPC), L2 (`EmailMismatch` outcome), L3 (`RaiseCreated` post-save), L5 (mutate loaded `AppSettings`) — every one present.

**Verdict: PASS** — all 12 steps Implemented; 2 deviations validated as sanctioned (convention-backed migration startup + dependency-honoring reorder); no step Missing; all code-grounded plan resolutions present in source. Hand to reviewer for Step 2.

---

## Step 2 — Code Review (cycle 1 re-review)

## Reviewed By
Reviewer agent (nexus:reviewer), cycle 1/3 re-review. Verified every named fix in source; ran a fresh build.

## Verdict: APPROVED

All cycle-0 findings resolved. Both HIGHs from the independent cross-check (H1 atomic redemption, H2 inactive-user crash) confirmed fixed. No remaining CRITICAL or HIGH. Residual LOW noted below.

## Findings

### RESOLVED — H1: Atomic single-use redemption (concurrency token)

`Invitation.ConcurrencyToken` field added (`Fokus.Domain/Auth/Invitation.cs:18`); `IsConcurrencyToken()` configured in `InvitationConfiguration.cs:23`; migration `20260609212836_AddInvitationConcurrencyToken` adds the column with default `00000000-...`; `InvitationGrpcService.RedeemAsync` sets `invitation.ConcurrencyToken = Guid.NewGuid()` before save and catches `DbUpdateConcurrencyException` → returns `Used` outcome. The snapshot shows `IsConcurrencyToken()` on the Invitations entity. Migration default of all-zeros is correct — two concurrent requests on the same row both read zero GUID; one write wins, the other sees 0 rows affected and throws. **Verified resolved.**

### RESOLVED — H2: Inactive-user creates second User (duplicate PersonId crash)

`HandleInviteRedemptionAsync` now checks `existingUser is not null` before branching on `IsActive`. Inactive path at line 131 redirects to `/invite/error?reason=account-inactive` and calls `context.HandleResponse()` — no User created, no DB write. Active path at lines 119-126 logs in normally as before (Q5). The Person-exists-but-no-User case (lines 112-136 outer: `existingPerson is not null` but `existingUser is null`) falls through to the gRPC redemption path, where a new User will be created against the existing Person — correct per Q4/AC5. **Verified resolved.**

### RESOLVED — M (GetUserAsync dead code)

`SprintIssueSyncService` now calls `FetchJiraEmailBestEffortAsync` (lines 131, 177) when `developer.Email is null`, which calls `jiraClient.GetUserAsync(accountId, ct)` (line 263) and returns the lowercased email or null on failure. The method is no longer dead code. **Verified resolved.**

### RESOLVED — M (PersonRepository enum parse)

`GetByExternalIdAndSourceAsync` now parses with `Enum.TryParse<PersonSource>(source, ignoreCase: true, out var src)` and compares `p.Source == src` (line 18-20). Comment notes the reason. **Verified resolved.**

### RESOLVED — L (GrpcChannel per-scope, no disposal)

`AddCodeFirstGrpcClient<TClient>` changed from `AddScoped` to `AddSingleton`. Comment at line 29 explains `GrpcChannel` is thread-safe and designed for sharing. **Verified resolved.**

### RESOLVED — Codex M1 (catch RpcException in invite branch)

`HandleInviteRedemptionAsync` now wraps `invitationService.RedeemAsync(...)` in `try/catch (RpcException)` (lines 141-153), redirecting to `/invite/error?reason=error` on transport failure. `using Grpc.Core;` present at file line 4; resolves transitively via `protobuf-net.Grpc.AspNetCore`. Build green confirms resolution. **Verified resolved.**

### RESOLVED — Codex M4 (Person upsert after Fokus commit)

`SyncAsync` now collects discovered developers into `discoveredDevelopers` list (line 165, 181), calls `ticketRepository.SaveChangesAsync` and `sprintRepository.SaveChangesAsync` first (lines 191-194), then iterates `discoveredDevelopers` calling `UpsertPersonBestEffortAsync` (lines 196-199). Same pattern in `SyncEpicDiscoveryAsync` (lines 138-142). Comment explicitly notes "M4". **Verified resolved.**

### RESOLVED — Codex M5 (MakePersonEmailOptional.Down irreversible)

`MakePersonEmailOptional.Down` now `throw new InvalidOperationException(...)` with a comment explaining why rollback is unsafe once null-email Persons exist (lines 40-44). **Verified resolved.**

### [LOW] HandleInviteRedemptionAsync — active-user path calls only personRepository.SaveChangesAsync (no explicit userRepository call)

**File:** `src/Services/Identity/Identity.API/Auth/AuthConfiguration.cs:124`
**Issue:** `existingUser.RecordLogin()` (line 123) is persisted via `personRepository.SaveChangesAsync()` (line 124) because both repos share `IdentityDbContext`. This works but is a silent cross-entity dependency. Unchanged from cycle 0.
**Fix:** Add `await userRepository.SaveChangesAsync()` after `RecordLogin()`, mirroring `HandleRegularLoginAsync:246`. Or add an explicit comment. No correctness impact.

## Positive Observations
- Concurrency fix is correctly layered: token at the domain level, `IsConcurrencyToken()` at persistence, catch at the service level — no exception crosses the gRPC boundary.
- `FetchJiraEmailBestEffortAsync` is a clean private helper: catches all exceptions, logs at Debug level, and returns null — does not interfere with the sync batch.
- M4 ordering is explicit in both `SyncAsync` and `SyncEpicDiscoveryAsync` with matching comments. The "collect then reconcile" pattern cleanly separates Fokus writes from Identity gRPC calls.
- `MakePersonEmailOptional.Down` throws a descriptive exception — explicitly non-reversible, consistent with the ef-core convention doc on migration defaults.
- All previously-passing checks (ADR-001 boundary, L1, L3, L5, filtered index, gRPC topology) remain unchanged and intact.

## Gaps
- No backend tests for the new concurrency path. Not introduced this cycle; consistent with project state.

## Open Questions
None.

## Evidence
| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Build (cycle 1) | PASS | dotnet build SprintRituals.slnx --no-incremental | Build succeeded. 0 Error(s) |
| H1: ConcurrencyToken in snapshot | PASS | Grep IsConcurrencyToken in FokusDbContextModelSnapshot.cs | Line 132: .IsConcurrencyToken() |
| H1: DbUpdateConcurrencyException catch | PASS | Read InvitationGrpcService.cs:48-56 | try/catch present, returns Used |
| H2: inactive-user redirect | PASS | Read AuthConfiguration.cs:128-134 | else branch redirects to /invite/error?reason=account-inactive |
| H2: no second User for existing PersonId | PASS | Read AuthConfiguration.cs:112-136 | existingUser != null guard covers both active/inactive before gRPC call |
| M1: RpcException catch | PASS | Read AuthConfiguration.cs:141-153 | try/catch (RpcException) wraps RedeemAsync |
| M GetUserAsync wired | PASS | Grep GetUserAsync across src/ | Called at SprintIssueSyncService.cs:263 |
| M PersonRepository enum parse | PASS | Read PersonRepository.cs:14-21 | Enum.TryParse + p.Source == src |
| L GrpcChannel singleton | PASS | Read GrpcClientRegistrationExtensions.cs:30 | AddSingleton |
| M4: Fokus commit before gRPC | PASS | Read SprintIssueSyncService.cs:191-199 | SaveChangesAsync called before UpsertPersonBestEffortAsync loop |
| M5: Down throws | PASS | Read MakePersonEmailOptional.cs:40-44 | throw new InvalidOperationException |
