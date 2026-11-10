# Plan-Critic Findings — adhoc-Pass4-IdentityExtraction (Mode 2, code-grounded)

**Reviewed:** `docs/specs/adhoc-Pass4-IdentityExtraction/delivery/plan.md`
against `definition/spec.md` + `delivery/questions.md` (Q1–Q15) + real source on disk + the reference codebase.
**Reviewer:** critic (nexus). Relay fallback file — NOT review.md, NOT a verdict. Folded by the architect into plan.md `## Plan Review`.

Cross-referenced all 14 Business Rules, all 9 Acceptance Criteria, all 3 Flows, all 15 answered questions, and all 11 plan steps. **11 findings: 1 HIGH (blocking), 5 MEDIUM, 5 LOW.** No locked decision is contradicted by the plan (L1, Q13 two-direction gRPC, Q14 bootstrap-gated seed, ADR-001 no project refs all honored). The decomposition (11 steps unsplit) is NOT re-raised — accepted per instruction; no correctness problem found with the single chain.

---

## HIGH (blocking — must resolve before/within the step)

### H1 — Step 10 / Step 4 / BR 8 / Q4 / AC 8: Jira developers have NO email, but the Person upsert matches & requires email
**Where:** Step 10 (`SprintIssueSyncService` → `IPersonService.UpsertPersonAsync`), Step 4 (Identity upsert match-by-email), Step 2 (`UpsertPersonAsync({ Email, ... })`).
**Code evidence:**
- `Jira.Contracts/JiraIssue.cs:78-83` — `JiraUser` (the assignee) carries **only** `AccountId`, `DisplayName`, `AvatarUrls`. **No email field.**
- `Fokus.API/Features/Sync/JiraDomainMapper.cs:67-81` — `MapDeveloper` maps only `Id=AccountId`, `DisplayName`, `AvatarUrl`.
- `Fokus.Domain/Developer/Developer.cs` — `Developer : Entity<string>` has `DisplayName`, `AvatarUrl`, `SubTeam`, `IsActive`, `Role`, `DefaultCapacityPercent`. **No email.**
- `Identity.Persistence/Configurations/PersonConfiguration.cs:18` — `Person.Email` is `IsRequired()` with a unique NOCASE index; BR 8 / Q4 / Step 4 match an existing Person **by email**.
**Problem:** The single in-scope Person-upsert consumer (Step 10) cannot supply `Email`, yet the contract field is present (Step 2), the server matches on it (Step 4), and `Person.Email` is required. As written, Step 10 either passes empty/placeholder email (violates the unique NOCASE Email index the moment two emailless developers are reconciled, and breaks the redemption-reuse-by-email path in Flow 1 / AC 5) or cannot create the Person at all. **AC 8 ("synced developer appears as a Person row with source=Jira") is not achievable** with the current Jira contract + Developer shape.
**Needs:** An explicit decision the plan does not make — either (a) extend the Jira fetch/contract to pull assignee email (Jira `emailAddress` is often restricted/absent via API — not guaranteed), (b) key the Jira-sourced Person on `ExternalId=AccountId` instead of email and relax the email-required/unique constraints for `Source=Jira` Persons (a Person model + migration change NOT in scope), or (c) re-scope Step 10 so the upsert request shape and match key are AccountId-based, not email-based. This is a genuine spec/plan gap, not an implementation detail — flag to PO/architect.

---

## MEDIUM

### M1 — Step 1 / Data Model Changes: migration is DEFINITELY required, not conditional
**Where:** Step 1 Accept #3, "Data Model Changes", "Migration Notes" — all hedge ("may be required", "iff", "confirm whether the base maps these").
**Code evidence:**
- `Blocks.Domain/Entities/AggregateRoot.cs:11-14` — base declares four **public get/set** audit props: `CreatedById`, `CreatedOn`, `LastModifiedById`, `LastModifiedOn` (mapped by EF convention; nothing ignores them).
- `Identity.Persistence/Migrations/IdentityDbContextModelSnapshot.cs:29-54` — **Person** (already an AggregateRoot) **maps all four columns** (`CreatedById`, `CreatedOn`, `LastModifiedById`, `LastModifiedOn`). `PersonConfiguration.cs` does NOT ignore them.
- Same snapshot lines 85-117 — **User** (current `Entity<int>`) has **none** of these columns.
- `ApplicationDbContext.cs` is a bare `DbContext` passthrough — no global Ignore of audit fields; `UserConfiguration.cs` does not ignore them either.
**Problem:** Promoting `User : Entity<int>` → `AggregateRoot<int>` WILL add four new mapped columns to the `Users` table by convention (Person is the on-disk proof). **A migration is mandatory**, not "maybe". The plan's repeated conditional framing risks the developer "diffing and finding no change" and skipping it.
**Needs:** Reword Step 1 to state a migration is required (add `PromoteUserToAggregate` with the Identity startup-project command already given), unless the developer deliberately `Ignore()`s the four audit props in `UserConfiguration` to match a "no new columns" choice — but note Person does NOT ignore them, so ignoring on User diverges from the very precedent Step 1 says to mirror. Either way the "no-migration" branch is misleading as written.

### M2 — Step 2: DTO attribute guidance contradicts the in-repo precedent (`[ProtoContract]` vs `[DataContract]`) and won't compile in Blocks.Contracts
**Where:** Step 2 — "DTO rules per skill: `[ProtoContract]` classes ... `[ProtoMember(n)]` ... enums need `[ProtoContract]` + `[ProtoEnum]`"; also Step 2 claims the existing style is "`ValueTask<T>`, `CallContext context = default`".
**Code evidence:**
- `Blocks.Contracts/Grpc/DataContracts/SprintInfo.cs:9-12` + `TeamQuery.cs:5-12` — existing DTOs use **`[DataContract]` / `[DataMember(Order = n)]`** from `System.Runtime.Serialization`, NOT protobuf-net attributes.
- `Blocks.Contracts/Blocks.Contracts.csproj:10` — references **only** `System.ServiceModel.Primitives`. No `protobuf-net.Grpc` package → `[ProtoContract]`/`[ProtoMember]`/`[ProtoEnum]` **will not resolve/compile** here.
- `ISprintCatalogService.cs:9-10` — existing contract op is `Task<SprintCatalogResponse> GetAvailableSprintsAsync(TeamQuery request, CancellationToken ct = default)` — i.e. **`Task<T>` + `CancellationToken ct = default`**, NOT `ValueTask<T>` + `CallContext context = default`.
- `Fokus.API/Grpc/SprintCatalogGrpcService.cs:13` — server impl signature also uses `Task<T>` + `CancellationToken ct`.
**Problem:** Both Step 2's DTO-attribute rule AND its description of the existing signature style are wrong against the repo. Following the skill's generic `[ProtoContract]` guidance over the verified local `[DataContract]` pattern would either fail to compile (no protobuf-net pkg in Blocks.Contracts) or introduce an inconsistent attribute style across contracts. This is precisely the "Follow = in-repo specifics may differ" case the plan claims to honor.
**Needs:** Step 2 must direct the developer to mirror the **actual** in-repo precedent: `[DataContract]` / `[DataMember(Order=n)]`, `Task<T>` return, `CancellationToken ct = default` param (matching `ISprintCatalogService`). The `[ProtoMember]`/`CallContext` text should be struck.

### M3 — Steps 4 & 5: Identity hosting its first gRPC server requires a Program.cs change that is only hedged, not committed
**Where:** Step 4 "Modify: ... Host wiring **if** Identity needs its own MapGrpcService call"; Step 5 maps in `MapFokusEndpoints`.
**Code evidence:**
- `Host/Program.cs:39` calls only `app.MapIdentityEndpoints()`; `:50` calls `app.MapFokusEndpoints()`. `MapGrpcService<...>` lives **inside** each module's map method (`Fokus.API/DependencyInjection.cs:100-102`).
- `Identity.API/DependencyInjection.cs:21-26` — `MapIdentityEndpoints` maps only the Login minimal API; it maps **no** gRPC service and Identity registers **no** `AddCodeFirstGrpc`.
**Problem:** For Identity to serve `IPersonService` on `localhost:5001`, Identity needs (a) `AddCodeFirstGrpc` in `AddIdentityModule` and (b) a `MapGrpcService<PersonGrpcService>` call reached from `Program.cs`. Since `MapIdentityEndpoints` currently maps no gRPC, either it must be extended OR a new `MapIdentityGrpcServices` must be added AND called in `Program.cs`. The plan flags this only conditionally ("if Identity needs..."). It definitely does. Note also `AddCodeFirstGrpc()` will then be called twice (Fokus + Identity) — tolerated/idempotent for protobuf-net server registration, but the plan doesn't acknowledge it.
**Needs:** Make the `Program.cs` + Identity DI gRPC-server wiring an explicit, non-conditional sub-task of Step 4 (mapping reachable from Program.cs), and note the double `AddCodeFirstGrpc` is intentional/harmless.

### M4 — Step 3: named client packages don't match the reference helper the plan says to port verbatim
**Where:** Step 3 / Q15 — add `protobuf-net.Grpc` + `Grpc.Net.ClientFactory`; "Port ... modeled on the reference `GrpcClientRegistrationExtensions.AddCodeFirstGrpcClient<T>`".
**Code evidence (reference repo):**
- `dotnet-microservices/.../Grpc/GrpcClientRegistrationExtensions.cs` — `AddCodeFirstGrpcClient<T>` (lines 62-89) uses `Grpc.Net.Client`'s `GrpcChannel.ForAddress(...).CreateGrpcService<T>()` (from `ProtoBuf.Grpc.Client`), **not** `Grpc.Net.ClientFactory`. The `Grpc.Net.ClientFactory` path is a *different* method (`AddConfiguredGrpcClient1`, lines 12-47) that also pulls in **Polly** (`RpcException` retry) + `Grpc.Core`.
- `dotnet-microservices/src/Directory.Packages.props` declares `protobuf-net.Grpc` 1.2.2, `Grpc.Net.Client` 2.71.0, `Grpc.Net.ClientFactory` 2.71.0, and `Microsoft.Extensions.Http.Polly`.
**Problem:** If the developer ports `AddCodeFirstGrpcClient` (the method the plan names) verbatim, the required packages are `protobuf-net.Grpc` + **`Grpc.Net.Client`** (+ `Grpc.Core` for `RpcException` only if retry kept) — NOT `Grpc.Net.ClientFactory`. The plan's package list is mismatched to the cited helper. The reference also `#if DEBUG` accepts any server cert — fine for h2c loopback but worth a note (the in-repo endpoint is plain `http://localhost:5001`, so no TLS handler is even needed).
**Needs:** Reconcile Step 3's package set with the actual ported method: either port `AddCodeFirstGrpcClient` (then add `protobuf-net.Grpc` + `Grpc.Net.Client`, drop `Grpc.Net.ClientFactory`/Polly) or commit to the `AddGrpcClient`/`ClientFactory` path (then keep `Grpc.Net.ClientFactory` + Polly). State which, so CPM additions match the helper.

### M5 — Step 11 / Doc artifact drift: Fokus CLAUDE.md (and root CLAUDE.md) carry stale gRPC/integration-event "None" claims that Step 11 doesn't fix
**Where:** Step 11 modifies only `Identity/CLAUDE.md`, root `CLAUDE.md` "Deferred" line, and `known-deviations`/`decisions` cross-ref.
**Code evidence:**
- `Fokus/CLAUDE.md` states: "gRPC clients: None", "gRPC server: None", "Integration events Published: None / Consumed: None". After Pass 4, Fokus is the `IPersonService` **client**, the `IInvitationService` **server**, and **consumes** `FirstAdministratorRegistered`. (Also note Fokus already publishes `SprintSynced` today — that line is *already* stale, pre-existing.)
- `Identity/CLAUDE.md` "gRPC clients / server: None ... Integration events Published/Consumed: None" — after Pass 4, Identity is the `IPersonService` server, `IInvitationService` client, and publishes `FirstAdministratorRegistered`.
**Problem:** Step 11 updates the Identity CLAUDE.md but the symmetric Fokus CLAUDE.md sections (gRPC client/server + consumed/published integration events) are left stale — artifact drift. The two-contract topology touches both module docs.
**Needs:** Add `Fokus/CLAUDE.md` (gRPC clients/server + Integration events sections) to Step 11's modify list alongside `Identity/CLAUDE.md`.

---

## LOW

### L1 — Step 5: `Invitation.Accept(now)` throws `DomainException` on non-Pending/expired; the gRPC redeem must check-then-accept, not catch-the-throw
**Where:** Step 5 redeem op.
**Code evidence:** `Fokus.Domain/Auth/Behaviors/Invitation.cs:30-39` — `Accept` throws `DomainException` if `Status != "Pending"` or expired. The plan correctly says "determine usability using the same state checks ... on success call Accept" (check-first), so the design is sound — but it should explicitly note `Accept` itself throws, so the failure outcomes (Used/Revoked/Expired) MUST be resolved by the pre-checks (mirroring `ValidateInvitationEndpoint.cs:13-35`) and never by letting `Accept` throw across the gRPC boundary. Minor: make the no-throw contract explicit so the developer doesn't rely on exception-to-outcome translation.

### L2 — Step 5 reason mapping: ValidateInvitation has no "email-mismatch" reason; the gRPC contract introduces it
**Where:** Step 5 / Step 2 outcome enum (Success|NotFound|Used|Revoked|Expired|EmailMismatch).
**Code evidence:** `ValidateInvitationEndpoint.cs` emits only `not-found|used|revoked|expired` (no email-mismatch — validation is pre-sign-in, email unknown). The redeem op adds `EmailMismatch` (server-side match per Q2/Q13). Correct and intended, but the plan's "same state checks as ValidateInvitationEndpoint" phrasing understates that the redeem op has **one extra** outcome the validate endpoint never produces. Minor wording.

### L3 — Step 8 predicate vs `CountActiveAdminsAsync` timing (event fires post-save)
**Where:** Step 8 gate `Role=="Admin" && active-admin-count==1`; `UserRepository.cs:14` `CountActiveAdminsAsync`.
**Code evidence:** `UserCreated` dispatches in `DispatchDomainEventsInterceptor.SavedChangesAsync` (post-save), and the bootstrap path saves the User before `RaiseCreated()` (mirroring `AuthConfiguration.cs:109-114` Person pattern). So when the handler runs, the new Admin is already persisted → `CountActiveAdminsAsync()==1` holds for the true bootstrap admin. Logic is sound; just confirm the handler counts AFTER the user is committed (it will, given the post-save dispatch). No change needed — noting the timing is load-bearing and correct, so the developer shouldn't move the raise before save.

### L4 — Step 3 transport: plan offers "Grpc services section OR reuse Kestrel:Endpoints:Grpc:Url" — pick one; reference options shape differs
**Where:** Step 3 config bullet 3.
**Code evidence:** `Host/appsettings.json:5` exposes `Kestrel:Endpoints:Grpc:Url = http://localhost:5001` (a server-bind setting). The reference helper reads a **separate** `GrpcServicesOptions { Services: { <key>: { Url } } }` block (`GrpcServicesOptions.cs`). Reusing the Kestrel bind URL as the client target conflates server-bind config with client-dial config; the reference pattern keeps them separate. Minor: choose the reference's `Grpc:Services:<name>:Url` shape (cleaner, matches the ported helper) rather than reading the Kestrel bind key.

### L5 — Step 9: idempotency phrasing vs AppSettings singleton seed-on-read
**Where:** Step 9 consumer "seed only if CompanyDomain is still null".
**Code evidence:** `AppSettingsRepository.GetAsync` (lines 5-15) creates+saves a default row (CompanyDomain=null) if missing; `AppSettings` is a singleton (Id=1). The consumer should `GetAsync` → if `CompanyDomain is null` set + `SaveAsync`. Correct, but note `SaveAsync` (line 17-41) re-assigns ComplexProperty VOs; setting only `CompanyDomain` then `SaveAsync(settings)` is fine since it round-trips the loaded instance. Minor: the consumer should mutate the **loaded** settings instance and persist via the repo (not construct a fresh AppSettings), to avoid wiping the VO columns. Worth one sentence in Step 9 so the developer doesn't `new AppSettings{CompanyDomain=...}`.

---

## Confirmed sound (positive cross-checks — no action)

- **Step 1 dispatch path:** `Identity.Persistence/DependencyInjection.cs:15-21` registers `IDomainEventPublisher` + `DispatchDomainEventsInterceptor` on `IdentityDbContext`; `DispatchDomainEventsInterceptor` reads `Entries<IAggregateRoot>()` post-save. Once User is an AggregateRoot and `RaiseCreated()` is called post-save (Person pattern, `Behaviors/Person.cs:30` + `AuthConfiguration.cs:105-107`), `UserCreated` WILL dispatch. Q11/BR 12 satisfied.
- **Step 3 client infra genuinely absent:** `Directory.Packages.props` has only `protobuf-net.Grpc.AspNetCore` 1.2.2 (server). `Blocks.AspNetCore` has only `Middlewares/` — no `Grpc/` dir, no client helper. Build disposition justified.
- **Step 9 single-bus fact:** exactly one `AddMassTransit` (`Fokus.API/DependencyInjection.cs:33`, `UsingInMemory`), zero `AddConsumers`, no consumers anywhere. Topology change (add `AddConsumers` to the one bus, no second bus) is correct for the in-memory wiring.
- **Two opposite-direction gRPC servers on one endpoint:** both map onto the single h2c `localhost:5001` (`appsettings.json:5`); Http endpoint is `Http1AndHttp2`. Nothing in the current single-`AddCodeFirstGrpc` setup blocks a second service or the opposite-direction client. Q13/Q15 sound.
- **No locked-decision contradictions:** L1 (Invitation stays Fokus — plan keeps it `Entity<int>`, only consumes `Accept`), Q13 (two opposite-direction contracts), Q14 (bootstrap-gated `IEventHandler<UserCreated>`), ADR-001 (no cross-module project refs — Step 10 Accept #3 greps for the absence) all honored.
- **Step 6 stub removal:** `AuthConfiguration.cs:88-95` is the exact `not-implemented` branch; `:99-119` bootstrap and `:120-185` normal-login/claims tail exist as the plan describes; `invite_token` is carried by `LoginEndpoint.cs:20-21`. Seam is correctly localized.

---

**Reviewed:** `docs/specs/adhoc-Pass4-IdentityExtraction/delivery/plan.md`
