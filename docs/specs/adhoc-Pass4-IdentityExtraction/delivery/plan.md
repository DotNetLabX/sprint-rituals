# Identity Extraction — Finish the Fokus → Identity Cut (Pass 4)

**Feature Spec:** `docs/specs/adhoc-Pass4-IdentityExtraction/definition/spec.md`
**Slug:** adhoc-Pass4-IdentityExtraction
**Plan:** docs/specs/adhoc-Pass4-IdentityExtraction/delivery/plan.md
**Intent:** Complex (multi-module: 2 new gRPC contracts in opposite directions, 1 integration event, a domain-model promotion, and a cross-module reconciliation consumer)

---

## Context

Pass 4 completes the in-flight Identity extraction (ADR-015) by delivering the three deferred seams the cut stubbed out: **invitation redemption at login**, **company-domain auto-seed**, and the **cross-module Person service**. The invitation concept stays in Fokus (locked L1), so redemption must bridge Identity (login) ↔ Fokus (invitation) across the module boundary using only sanctioned mechanisms — never a project reference (ADR-001).

**Services impacted:** Identity (API + Domain + Persistence), Fokus (API + Domain), `Blocks.Contracts` (two new gRPC contracts + one integration event DTO), `Blocks.AspNetCore` (new gRPC client helper), `Directory.Packages.props` (gRPC client packages), Host (gRPC server mapping for Identity, gRPC `Grpc` URL config).

**Verified seam facts (Phase 1, source-on-disk):**
- Redemption stub: `Identity.API/Auth/AuthConfiguration.cs:88-95` redirects any token to `/invite/error?reason=not-implemented`. The Google challenge already carries `invite_token` (`LoginEndpoint.cs`), so the seam is entirely inside `HandleTicketReceivedAsync`.
- Bootstrap branch (`AuthConfiguration.cs:99-119`) already knows "first user" (user count == 0) and creates the Admin; the company-domain seed is a comment-only stub at `:116`.
- `UserCreated(int UserId)` exists as an **unraised placeholder**; `User : Entity<int>` (no `DomainEvents` collection) — promotion is the hard prerequisite for dispatch (Q11).
- `Person : AggregateRoot<int>` is the in-repo pattern to mirror for the promotion (state/behavior partial split + `RaiseCreated()` after save, `Person.cs` / `Behaviors/Person.cs`).
- Dispatch path: `DispatchDomainEventsInterceptor` (post-`SavedChangesAsync`) reads `ChangeTracker.Entries<IAggregateRoot>()`, clears events, publishes via `IDomainEventPublisher` → FastEndpoints `IEvent` bus. There are **zero** existing `IEventHandler<>` implementations — the `UserCreated` handler is the first.
- gRPC today is **server-only** in Fokus (`AddCodeFirstGrpc` + `MapGrpcService` in `Fokus.API/DependencyInjection.cs`); **no gRPC client** infra exists (CPM lacks the client packages; `Blocks.AspNetCore` has no client helper). Single shared h2c endpoint: `Grpc` → `http://localhost:5001`.
- MassTransit: exactly **one** `AddMassTransit` (Fokus, in-memory). Identity has no MassTransit package and no bus. **No** `AddConsumers` anywhere — the seed consumer is the first consumer in the repo. One process = one bus.
- Company-domain gate extraction (the byte-pattern the seed MUST match): `CreateInvitationEndpoint.cs:21` → `email.Split('@')[1]`, compared case-insensitively against `settings.CompanyDomain`.

## Scope

**In scope (locked answers Q1–Q15, all Answered):**
- Invitation redemption at login (Flow 1) — synchronous, via a **new** gRPC `IInvitationService` (Fokus server / Identity client) doing server-side validate + email-match + `Invitation.Accept(now)` in one atomic round trip, returning success+role or a specific failure reason (Q13 = Option A + A1).
- Company-domain auto-seed (Flow 2) — `IEventHandler<UserCreated>` publishes a seed integration event gated on bootstrap predicate `Role == "Admin" AND active-admin-count == 1`, deriving the domain via the same `email.Split('@')[1]` extraction, skipping known public providers; Fokus consumes and seeds `AppSettings.CompanyDomain` (Q6/Q7/Q8/Q14).
- Cross-module Person reconciliation (Flow 3) — **new** gRPC `IPersonService` (Identity server / Fokus client); the full upsert+lookup surface is defined, only the Jira-developer-sync upsert consumer is wired (Q9/Q10). **Jira-sourced Persons are keyed on `ExternalId` (=Jira AccountId), NOT email** — Jira Cloud hides developer email by default (H1 resolution).
- **Jira email best-effort fetch + `Person.Email` made optional** (H1 resolution, user-decided): add a Jira `GET /rest/api/3/user?accountId=` fetch (new `IJiraClient`/`IJiraApi` method + nullable `EmailAddress` on the `JiraUser` DTO), propagate email best-effort through `Developer` + `JiraDomainMapper`; make `Person.Email` **nullable** with the unique index scoped to non-null values (a **second** Identity migration). Google-sourced Persons still match by email; Jira-sourced match/create by `ExternalId` + `Source=Jira`. The PO is patching the spec's AC-8 + Person-entity-email-optional in parallel; this plan is written against that resolution.
- Promote `User` `Entity<int>` → `AggregateRoot<int>` so `UserCreated` dispatches (Q11). This **adds 4 mapped audit columns** (`CreatedById`/`CreatedOn`/`LastModifiedById`/`LastModifiedOn`) → a **mandatory** Identity migration (M1).
- Build the missing gRPC **client** infrastructure (CPM packages + `Blocks.AspNetCore` helper + `Grpc` URL config), reusing the single `localhost:5001` h2c endpoint for both contracts (Q15).
- Remove the `not-implemented` redemption stub (BR 14).

**Explicitly out of scope:**
- Relocating/promoting the Invitation entity (stays `Entity<int>` in Fokus — L1).
- Multi-tenancy / multiple company domains; non-Google IdPs; RabbitMQ transport; invitation email delivery.
- Frontend invite landing/error pages — API/redirect-only this pass (Q12). The server still redirects to `/invite/...` and `/` so the follow-up supplies views.
- Wiring the unused gRPC operations (Person lookup; any non-upsert Person op) — defined but unconsumed (Q10).

---

## Locked decisions (from questions.md — all Answered)

- **Q1** link → validate → Google carries token → callback redeems. **Q2** enforce exact case-insensitive email match. **Q3** redeemed role from invitation; bootstrap still Admin. **Q4** reuse existing Person, leave its source; create only if none. **Q5** dedicated `/invite/error?reason=…`; already-registered active user → ignore token, normal login.
- **Q6** seed at first-admin bootstrap only, no later reconcile. **Q7** derive from admin email domain; skip known public providers. **Q8** seed via integration event over the in-process bus.
- **Q9** Person gRPC direction Fokus→Identity upsert, source=Jira. **Q10** define full Person contract, wire only the Jira-sync upsert consumer.
- **Q11** promote User to AggregateRoot this pass.
- **Q12** API/redirect-only; client pages deferred.
- **Q13 = Option A + A1:** new gRPC `IInvitationService`, **Fokus server / Identity client** (opposite of `IPersonService`). Fokus does server-side validate + case-insensitive email-match + `Invitation.Accept(now)` and returns outcome (success+role | specific failure reason) in **one atomic round trip**. Two opposite-direction gRPC contracts confirmed in scope. Redemption NOT deferred.
- **Q14 = Option (a):** `IEventHandler<UserCreated>` publishes the seed integration event, gated on `Role == "Admin" AND active-admin-count == 1`; derive domain via `email.Split('@')[1]`; skip known public providers.
- **Q15 = yes, in scope:** build gRPC client infra (`Blocks.AspNetCore` client helper; `Grpc` URL config); reuse `localhost:5001` for both contracts. *(The answer named `Grpc.Net.ClientFactory`, but critic M4 corrected the CPM package to `protobuf-net.Grpc` + **`Grpc.Net.Client`** — the packages the ported `AddCodeFirstGrpcClient<T>` helper actually uses; see Step 3.)*

---

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | `domain-patterns` + `create-aggregate` (EF config shape) | Follow | Promote `User` `Entity<int>`→`AggregateRoot<int>`; mirror in-repo `Person` aggregate; raise `UserCreated` after save via `RaiseCreated()`; **mandatory** migration for 4 audit columns (M1) | — |
| 2 | `create-grpc-contract` (Contract workflow) | Follow | Two contracts in `Blocks.Contracts/Grpc`: `IInvitationService` (redeem) + `IPersonService` (upsert keyed on ExternalId+Source, nullable Email + lookup). **`[DataContract]`/`[DataMember(Order=n)]` + `Task<T>` + `CancellationToken ct = default`** — mirror `ISprintCatalogService`/`SprintInfo` (M2) | — |
| 3 | `create-grpc-contract` (Client workflow) | **Build** | gRPC client infra absent: add CPM packages (`protobuf-net.Grpc` + `Grpc.Net.Client` — M4) + `Blocks.AspNetCore` client helper + `Grpc:Services:<name>:Url` config (L4), THEN follow Client workflow | Log to lessons.md → `create-grpc-contract` Client prereqs |
| 4 | `create-grpc-contract` (Server workflow) | Follow | `IPersonService` server impl in `Identity.API/Grpc/`; **committed** `AddCodeFirstGrpc` + `MapGrpcService` reachable from `Program.cs` (M3); upsert keyed by `ExternalId`+`Source=Jira` (Jira) / email (Google) | — |
| 5 | `create-grpc-contract` (Server workflow) | Follow | `IInvitationService` server impl in `Fokus.API/Grpc/`; reuses `InvitationRepository` + `Invitation.Accept(now)` (check-then-accept, no throw across boundary — L1); explicit `EmailMismatch` outcome (L2) | — |
| 6 | (none) | None | Redemption seam in `AuthConfiguration.HandleTicketReceivedAsync` — replace stub branch; call `IInvitationService`; provision User w/ invitation role; reuse-or-create Person; redirect outcomes | Log to lessons.md (auth-callback orchestration) |
| 7 | `add-integration-event` (EventContract workflow) | Follow | Integration event DTO `FirstAdministratorRegistered(string CompanyDomain)` in `Blocks.Contracts/IntegrationEvents` | — |
| 8 | `create-domain-event-handler` + `add-integration-event` (Publisher workflow, FastEndpoints variant) | Follow | `IEventHandler<UserCreated>` in Identity that publishes the seed event (POST-save raise so count==1 holds — L3); bootstrap predicate + public-provider skip + `email.Split('@')[1]` extraction | — |
| 9 | `add-integration-event` (Consumer workflow) | **Build** | Seed consumer in Fokus mutates the **loaded** `AppSettings` via the repo (not `new AppSettings{}` — L5); requires adding `AddConsumers(...)` to the existing single `AddMassTransit` + Identity MassTransit publish wiring | Log to lessons.md → MassTransit consumer registration |
| 10 | (none) | None | **Jira email best-effort fetch + `Person.Email` nullable** (H1): new `IJiraClient`/`IJiraApi` user-fetch + nullable `JiraUser.EmailAddress`; propagate via `Developer`/`JiraDomainMapper`; make `Person.Email` nullable + scoped unique index (2nd Identity migration) | Log to lessons.md (Jira user fetch + optional-email Person) |
| 11 | (none) | None | Wire the Fokus Jira-developer sync path to call `IPersonService` upsert (single in-scope consumer); reconcile each developer as Person `ExternalId=AccountId`, `Source=Jira`, email best-effort | Log to lessons.md (sync→gRPC consumer) |
| 12 | (none — documentation) | None | Two opposite-direction gRPC contracts + redemption bridge; update `Identity/CLAUDE.md` **and `Fokus/CLAUDE.md`** (M5) + root deferred list | — |

**Disposition rules:** Follow = apply the skill pattern, SQLite/in-repo specifics may differ. Build = skill references infra absent locally (gRPC client helper; MassTransit consumer registration) — build it first, then follow. None = no skill covers it (auth-callback orchestration; sync→gRPC call site; doc).

**Skill-verification notes (actions → frontmatter match):**
- **Step 1** changes a base type + raises a domain event → `domain-patterns` (aggregates, domain events, partial split) and `create-aggregate` (EF config shape) match. The Person aggregate is the concrete in-repo precedent. Follow, not None.
- **Steps 2/4/5** create gRPC contract + server impls → `create-grpc-contract` frontmatter ("contract, server implementation, client registration") matches directly. Follow.
- **Step 3** is Follow-pattern but the **client** infra (`protobuf-net.Grpc` client package, `Blocks.AspNetCore` helper) does not exist here → **Build** (the skill's Client workflow references `GrpcServicesOptions`/`AddCodeFirstGrpcClient` that must be ported in first).
- **Step 6** orchestrates the Google `OnTicketReceived` callback (sign-in, redirects, claims). No skill frontmatter covers auth-callback orchestration → None. (It *calls* the gRPC client from Step 3/5 but the call site itself is bespoke login plumbing.)
- **Step 7** creates a MassTransit integration event contract → `add-integration-event` EventContract workflow. Follow.
- **Step 8** is a domain-event handler that bridges to `IPublishEndpoint.Publish` → exactly the `add-integration-event` Publisher (FastEndpoints variant) + `create-domain-event-handler` (effect-named, co-located). Follow.
- **Step 9** is a MassTransit consumer → `add-integration-event` Consumer workflow; but consumer **registration** (`AddConsumers`) and Identity's publish wiring don't exist yet → **Build** (wire registration, then follow the Consumer pattern).
- **Step 10** extends the Jira adapter (a new Refit/`IJiraClient` method + DTO field) and changes the `Person` model (email nullable + scoped index + migration). No skill frontmatter covers "add a Jira REST method + relax a required domain property"; the Refit pattern is the existing `IJiraApi`. None.
- **Step 11** is a call-site edit in `SprintIssueSyncService` to invoke the gRPC client → no skill covers "modify an existing sync service to call a gRPC client." None.
- **Step 12** documentation → None.

**Anti-pattern check:** no "adapt" near any skill ref; no skill dismissed as "too simple"; Follow steps carry only feature-specific inputs (type names, file paths, DTO field shapes, the one extraction pattern), not restated gRPC/MassTransit boilerplate. Two Build steps justified by verified-absent infra, not by "no BuildingBlocks to reference." **Step 2 explicitly overrides the `create-grpc-contract` skill's generic `[ProtoContract]`/`ValueTask`/`CallContext` guidance with the verified in-repo `[DataContract]`/`Task<T>`/`CancellationToken` precedent** (`ISprintCatalogService`/`SprintInfo`) — `Blocks.Contracts` references only `System.ServiceModel.Primitives`, so protobuf-net attributes would not compile (M2). This is "Follow = in-repo specifics differ," not a skill skip.

---

## Domain Model Changes

- **`User` promoted** `Entity<int>` → `AggregateRoot<int>` (Identity). Gains `DomainEvents` collection + `RaiseCreated()` and the 4 base audit scalars. `UserCreated(int UserId)` becomes a **raised** event (drop the "unraised placeholder / do-not-raise" doc-comment; replace with a normal summary). No new domain-specific properties.
- **`UserCreated`** — now dispatched (the first dispatched `User` event). Carries only `UserId` (re-load for email per domain-event convention: events carry the aggregate reference, not individual properties — `project-rules.md`).
- **`Invitation`** — unchanged shape (stays `Entity<int>`, L1). Its existing `Accept(DateTime now)` behavior is now **consumed** (by the `IInvitationService` server impl) — previously unreachable.
- **`Person`** — `Email` becomes **nullable** (H1): Jira Cloud hides developer email, so a Jira-sourced Person may have no email. The match/create key is now **source-dependent**: Google-sourced Persons match by `Email` (lower-cased, as today); Jira-sourced Persons match by `ExternalId` (=Jira AccountId) + `Source=Jira`. A reconcile/upsert-from-Jira factory or method is added in `Behaviors/Person.cs` (developer decides; reuse leaves source unchanged, create sets source=Jira — BR 8 / Q4 / Q9). The unique Email index is scoped to non-null values.
- **`Developer` (Fokus)** — gains an optional `Email` (`string?`) populated best-effort from the Jira user fetch (H1); `JiraDomainMapper.MapDeveloper` + its Mapster `IRegister` config propagate it.
- **`JiraUser` (Jira.Contracts)** — gains a nullable `EmailAddress` field (H1).
- No new aggregates, no value objects.

## Data Model Changes

- **Identity migration #1 (Step 1) is MANDATORY (M1).** `AggregateRoot<int>` declares four **public get/set** audit props (`CreatedById`/`CreatedOn`/`LastModifiedById`/`LastModifiedOn`) that EF maps by convention — verified on disk: `Person` (already an `AggregateRoot<int>`) maps all four columns in `IdentityDbContextModelSnapshot.cs`, and nothing (`PersonConfiguration`, the bare `ApplicationDbContext`) ignores them. Promoting `User` therefore **adds four new columns to the `Users` table** — a migration is required, not conditional. (The only way to avoid it would be to `Ignore()` the four props on `User` — but that diverges from the Person precedent Step 1 mirrors, so do **not** do that; add the migration.)
- **Identity migration #2 (Step 10) is MANDATORY (H1).** Making `Person.Email` nullable + scoping its unique index to non-null values is a column-nullability + index change on the `Persons` table → its own migration.
- **No schema change to Fokus's `AppSettings`** — `CompanyDomain` already exists (nullable string); the seed only sets a value. `Developer` gains a nullable `Email` column → a **Fokus** migration (folded into Step 10).

---

## Implementation Steps

### Step 1 — Promote `User` to an aggregate root and raise `UserCreated` (Q11)
**Files (modify):** `src/Services/Identity/Identity.Domain/Users/User.cs`, `src/Services/Identity/Identity.Domain/Users/Behaviors/User.cs`, `src/Services/Identity/Identity.Domain/Users/Events/UserCreated.cs`. **Files (create):** a new Identity migration + updated `IdentityDbContextModelSnapshot.cs`.

Change `User : Entity<int>` → `User : AggregateRoot<int>`. Mirror the in-repo `Person` aggregate exactly: keep the state/behavior partial split; add a `RaiseCreated()` method in the behavior partial that does `AddDomainEvent(new UserCreated(Id))`, to be called **after** the User is persisted (so EF has assigned the PK), matching `Person.RaiseCreated()` (`Behaviors/Person.cs:30`). Rewrite the `UserCreated.cs` doc-comment from "unraised placeholder / do not raise" to a normal summary (it is now raised on provisioning).

**The migration is mandatory (M1):** the base `AggregateRoot<int>` adds four EF-mapped audit columns to `Users` (`CreatedById`, `CreatedOn`, `LastModifiedById`, `LastModifiedOn`) — verified: `Person` maps all four and nothing ignores them. Do **not** `Ignore()` them on `User` (that would diverge from the Person precedent). Generate the migration; do not hand-edit it.

- **Skill:** Follow `domain-patterns` (aggregate shape, domain events) + `create-aggregate` (EF config). Precedent: `Person` aggregate (`Identity.Domain/Persons/`) — `PersonConfiguration` does NOT ignore audit props, so `UserConfiguration` leaves them mapped too.
- **Feature inputs:** `UserCreated(int UserId)`; raise after save; reuse `Person`'s post-save raise pattern.
- **Dependency:** none (foundation for Steps 6 and 8).
- **Accept:**
  1. `User` declares `: AggregateRoot<int>`; `grep "class User" src/Services/Identity` shows the new base.
  2. `User.RaiseCreated()` exists and adds `UserCreated(Id)`; no raise is wired into the factory (raise happens post-save at the call site, Step 6).
  3. A migration `PromoteUserToAggregate` exists, scaffolded via `dotnet ef migrations add PromoteUserToAggregate -p src/Services/Identity/Identity.Persistence -s src/Services/Identity/Identity.API`; its `Up` **adds the four audit columns** to `Users` (and `Down` drops them); `IdentityDbContextModelSnapshot.cs` now shows the four columns on `User`, matching Person's treatment.
  4. `dotnet build SprintRituals.slnx` green.
- **Confidence:** high — the migration consequence is now confirmed (Person is the on-disk proof); the only judgment is mirroring Person's config, which is verified.

### Step 2 — Define both gRPC contracts in `Blocks.Contracts` (Q9/Q10/Q13)
**Files (create):** `src/BuildingBlocks/Blocks.Contracts/Grpc/IInvitationService.cs`, `src/BuildingBlocks/Blocks.Contracts/Grpc/IPersonService.cs`, plus DTOs under `src/BuildingBlocks/Blocks.Contracts/Grpc/DataContracts/`.

Define two code-first contracts (no `.proto`). **Mirror the verified in-repo precedent, NOT the skill's generic protobuf-net guidance (M2):** `ISprintCatalogService.cs` uses `[ServiceContract]` + `[OperationContract]` with **`Task<T>` return + `CancellationToken ct = default`** param (not `ValueTask`/`CallContext`); `SprintInfo.cs`/`TeamQuery.cs` DTOs use **`[DataContract]` + `[DataMember(Order = n)]`** from `System.Runtime.Serialization` (NOT `[ProtoContract]`/`[ProtoMember]`). `Blocks.Contracts` references only `System.ServiceModel.Primitives`, so protobuf-net attributes would not compile here.

- **`IInvitationService`** (Fokus = server, Identity = client) — **redeem** op: input `{ Token, SigningInEmail }`; output `{ Outcome, Role?, InvitedEmail? }`. `Outcome` is a `[DataContract]`/`[DataMember]` enum (or string) with values **Success | NotFound | Used | Revoked | Expired | EmailMismatch**. One atomic call: Fokus validates usability, enforces case-insensitive email match, calls `Invitation.Accept(now)` on success, returns the role. (Per Q13/A1.) Note `EmailMismatch` is a redeem-only outcome the validate endpoint never produces (L2).
- **`IPersonService`** (Identity = server, Fokus = client) — **full** upsert + lookup surface (Q10): `UpsertPersonAsync({ ExternalId, Source, Email?, FirstName, LastName, PictureUrl }) → { PersonId }` — **`Email` is nullable** and `ExternalId`+`Source` are the Jira match key (H1); and a lookup op (e.g. `FindPersonByEmailAsync({ Email }) → { Found, PersonId, … }`). Only Upsert is consumed this pass; lookup is defined-but-unconsumed.

DTO rules (in-repo): `[DataContract]` classes with parameterless ctors; `[DataMember(Order = n)]` unique/sequential per class; `= default!` for non-nullable refs; the `Outcome` enum is a `[DataContract]` with `[DataMember]` (or modeled as a `string` field if simpler — match the existing DTO style).

- **Skill:** Follow `create-grpc-contract` (Contract workflow) — **but its `[ProtoContract]`/`ValueTask`/`CallContext` text is overridden by the in-repo `[DataContract]`/`Task`/`CancellationToken` precedent above.** Precedent files: `Blocks.Contracts/Grpc/ISprintCatalogService.cs`, `DataContracts/SprintInfo.cs`, `DataContracts/TeamQuery.cs`.
- **Feature inputs:** the two interfaces + DTO field lists above; outcome values map 1:1 to the spec's failure reasons (not-found/used/revoked/expired/email-mismatch) so Step 6 translates them to `/invite/error?reason=…`.
- **Dependency:** none.
- **Accept:** both interfaces compile in `Blocks.Contracts` (`[DataContract]`/`[DataMember(Order=n)]`, `Task<T>`, `CancellationToken ct = default` — `grep "ProtoContract\|ValueTask\|CallContext" src/BuildingBlocks/Blocks.Contracts` returns **zero**); the `IInvitationService` redeem output exposes Role + the full 6-value outcome set incl. `EmailMismatch`; `IPersonService.UpsertPerson` request has nullable `Email` + `ExternalId` + `Source`. `dotnet build` green. No `Blocks.Contracts` reference to either module.
- **Confidence:** high.

### Step 3 — Build the gRPC client infrastructure, then register clients (Q15) [BUILD]
**Files (modify/create):** `src/Directory.Packages.props`, `src/BuildingBlocks/Blocks.AspNetCore/Grpc/GrpcClientRegistrationExtensions.cs` (create) + a `GrpcServicesOptions` options type (create), `src/Host/appsettings.json` (Grpc service URLs), and the consuming modules' `DependencyInjection.cs`.

**Build the missing infra first:**
1. Add to `Directory.Packages.props` (M4 — the packages the ported method actually uses): **`protobuf-net.Grpc`** (client, for `CreateGrpcService<T>()` from `ProtoBuf.Grpc.Client`; version-align with the existing `protobuf-net.Grpc.AspNetCore` 1.2.2) and **`Grpc.Net.Client`** (for `GrpcChannel.ForAddress(...)`). **Do NOT add `Grpc.Net.ClientFactory`** — the reference's `AddCodeFirstGrpcClient<T>` (the method being ported) uses the `GrpcChannel.ForAddress(...).CreateGrpcService<T>()` path, not the `ClientFactory`/Polly path. Bare `<PackageReference>` (no Version) in the consuming csproj per CPM (ADR-013).
2. Port the **`AddCodeFirstGrpcClient<T>`** helper (reference lines 62-89 of `<reference-repo>/src/BuildingBlocks/Blocks.AspNetCore/Grpc/GrpcClientRegistrationExtensions.cs`) into `Blocks.AspNetCore/Grpc/`: resolve the service URL from config, `GrpcChannel.ForAddress(url)`, return `channel.CreateGrpcService<TClient>()`. The endpoint is plain h2c `http://localhost:5001` — **no TLS handler needed** (skip the reference's `#if DEBUG` self-signed-cert handler; it's for https). Port the `GrpcServicesOptions` options type alongside.
3. Config (L4 — client-dial shape, not the server-bind key): add a **`Grpc:Services:<name>:Url`** section to `Host/appsettings.json` that the helper reads — e.g. `"Grpc": { "Services": { "Invitation": { "Url": "http://localhost:5001" }, "Person": { "Url": "http://localhost:5001" } } }`. Do **not** reuse the Kestrel server-bind key `Kestrel:Endpoints:Grpc:Url` as the client target (that conflates server-bind with client-dial config).

**Then follow the Client workflow:** register the `IInvitationService` client in **Identity** (`Identity.API/DependencyInjection.cs` or a new `AddIdentityGrpcClients`), and the `IPersonService` client in **Fokus** (`Fokus.API/DependencyInjection.cs`). Inject the interface; always pass the cancellation token.

- **Skill:** Build (client infra absent) then Follow `create-grpc-contract` (Client workflow). Reference: the named reference-repo `AddCodeFirstGrpcClient<T>` method specifically.
- **Dependency:** Step 2 (contracts exist).
- **Accept:**
  1. `Directory.Packages.props` declares `protobuf-net.Grpc` + `Grpc.Net.Client` (and **not** `Grpc.Net.ClientFactory`); consuming csproj use bare `PackageReference`.
  2. `Blocks.AspNetCore/Grpc/` has the `AddCodeFirstGrpcClient<T>` helper + `GrpcServicesOptions`; `grep "AddCodeFirstGrpcClient\|CreateGrpcService" src/BuildingBlocks/Blocks.AspNetCore` returns the new helper; no TLS/`#if DEBUG` cert handler present.
  3. Config uses `Grpc:Services:<name>:Url`; Identity registers the `IInvitationService` client, Fokus registers the `IPersonService` client; both resolve `http://localhost:5001`.
  4. `dotnet build SprintRituals.slnx` green.
- **Confidence:** medium — porting the one method + confirming the in-process h2c channel works for code-first clients is the adaptation; the reference is the anchor. Read reference lines 62-89 before settling.

### Step 4 — Implement the `IPersonService` gRPC server in Identity (Q9)
**Files (create):** `src/Services/Identity/Identity.API/Grpc/PersonGrpcService.cs`. **Modify (committed, not conditional — M3):** `Identity.API/DependencyInjection.cs` (add `AddCodeFirstGrpc()` in `AddIdentityModule` **and** a `MapIdentityGrpcServices(this WebApplication)` that maps `PersonGrpcService`), `src/Host/Program.cs` (call the new Identity gRPC-mapping method — see below), `PersonRepository.cs` (add a get-by-external-id-and-source query if not already present — `GetByExternalIdAsync` exists but is not source-scoped).

**Identity must host its first gRPC server (M3 — required, not "if needed"):** `MapIdentityEndpoints` today maps only the Login minimal API (`Identity.API/DependencyInjection.cs:21-26`) and Identity registers no `AddCodeFirstGrpc`. So this step MUST: (a) add `AddCodeFirstGrpc()` to `AddIdentityModule`, and (b) expose a `MapGrpcService<PersonGrpcService>()` call **reachable from `Program.cs`** — either fold it into `MapIdentityEndpoints` or add a new `MapIdentityGrpcServices` and call it in `Program.cs` next to `app.MapIdentityEndpoints()` (`:39`). Note `AddCodeFirstGrpc()` is then called twice (Fokus + Identity) — this is **intentional and idempotent** for protobuf-net server registration; do not try to dedupe it.

Implement `IPersonService` (Identity = server). **Upsert (H1 keying):** for `Source=Jira`, match an existing Person by **`ExternalId` (=AccountId) + `Source=Jira`**, NOT by email (Jira email may be null); for a Google-sourced match use email. If found, leave its source unchanged and update name/picture/email-if-now-known as appropriate; if not found, create a new Person with **source = Jira**, the provided `ExternalId`, and the **nullable** email (reuse/extend `Person` behavior — add a reconcile/upsert factory in `Behaviors/Person.cs`, raising `PersonCreated` post-save per the existing pattern). **Lookup:** implement the defined lookup op (defined-but-unconsumed; must compile and behave). Signatures match the contract (`Task<T>` + `CancellationToken ct = default` — M2).

- **Skill:** Follow `create-grpc-contract` (Server workflow). Precedent: `Fokus.API/Grpc/SprintCatalogGrpcService.cs` (impl signature + repo injection) and Fokus's `AddCodeFirstGrpc`/`MapGrpcService` wiring (`Fokus.API/DependencyInjection.cs:31,100-102`).
- **Feature inputs:** upsert key = `ExternalId`+`Source` for Jira / email for Google (BR 8 / Q4 / Q9 / H1); source=Jira on create; nullable email; reuse `PersonRepository`.
- **Dependency:** Steps 2, 3, 10 (nullable `Person.Email` must land before the server stores email-optional Persons — see ordering note in Step 10).
- **Accept:**
  1. `PersonGrpcService : IPersonService` exists in `Identity.API/Grpc/`; both ops implemented; `Task<T>`+`CancellationToken` signatures match the contract.
  2. Identity registers `AddCodeFirstGrpc()` and a `MapGrpcService<PersonGrpcService>()` **call reached from `Program.cs`** (`grep "MapGrpcService" src/Services/Identity src/Host` shows it); the double `AddCodeFirstGrpc` is noted as intentional.
  3. Upsert matches a Jira Person by `ExternalId`+`Source` (not email); creates source=Jira with nullable email otherwise; reuse leaves source unchanged; `PersonCreated` raised post-save on create.
  4. `dotnet build` green.
- **Confidence:** medium — Identity standing up its first gRPC server (registration + Program.cs mapping) is the new bit; the ExternalId-keyed upsert is straightforward against `PersonRepository`.

### Step 5 — Implement the `IInvitationService` gRPC server in Fokus (Q13/A1)
**Files (create):** `src/Services/Fokus/Fokus.API/Grpc/InvitationGrpcService.cs`. **Modify:** `Fokus.API/DependencyInjection.cs` `MapFokusEndpoints` (add `MapGrpcService<InvitationGrpcService>`).

Implement `IInvitationService` (Fokus = server). The redeem op, in one atomic call: load the invitation by token (`InvitationRepository.GetByTokenAsync`); **derive the failure outcome from explicit pre-checks** (mirroring `ValidateInvitationEndpoint.cs:13-35`): null→`NotFound`, `Status=="Accepted"`→`Used`, `Status=="Revoked"`→`Revoked`, `IsExpired`→`Expired`; then a **redeem-only** check the validate endpoint never does — case-insensitive compare of the signing-in email vs `invitation.Email`, mismatch→`EmailMismatch` (L2). Only when all pre-checks pass call `invitation.Accept(DateTime.UtcNow)`, save, and return `{ Success, Role = invitation.Role, InvitedEmail }`.

**L1 — no throw across the gRPC boundary:** `Invitation.Accept(now)` itself **throws `DomainException`** if status ≠ Pending or expired (`Behaviors/Invitation.cs:30-39`). The failure outcomes (Used/Revoked/Expired) MUST be resolved by the pre-checks above and returned as `Outcome` values — never by letting `Accept` throw and translating an exception to an outcome. By the time `Accept` is called, the pre-checks guarantee it will not throw. The accept + success return are in the same call so the token is consumed exactly once (BR 2 / AC "cannot be redeemed a second time").

- **Skill:** Follow `create-grpc-contract` (Server workflow). Precedent: `Fokus.API/Grpc/SprintCatalogGrpcService.cs` (impl signature + repo injection); reason/state logic precedent: `ValidateInvitationEndpoint.cs:11-37`.
- **Feature inputs:** outcome set maps to the Step-2 contract values incl. the extra `EmailMismatch`; email match case-insensitive; role from `invitation.Role`; signatures `Task<T>`+`CancellationToken` (M2).
- **Dependency:** Steps 2, 3.
- **Accept:**
  1. `InvitationGrpcService : IInvitationService` in `Fokus.API/Grpc/`; mapped via `MapGrpcService` in `MapFokusEndpoints`.
  2. All failure outcomes (incl. `EmailMismatch`) come from pre-checks; `Invitation.Accept(now)` is invoked **only** after the pre-checks pass (never throws across the boundary) and the invitation is saved Accepted.
  3. A second redeem of the same token returns `Used` (state is now Accepted).
  4. `dotnet build` green.
- **Confidence:** high — reuses existing repo + domain method + the validate endpoint's reason logic; the no-throw contract is explicit.

### Step 6 — Replace the redemption stub in the auth callback (Q1–Q5, Q13)
**File (modify):** `src/Services/Identity/Identity.API/Auth/AuthConfiguration.cs:88-95` (the `!string.IsNullOrEmpty(inviteToken)` branch) — and the surrounding provisioning so the redeemed User is created with the invitation role.

Replace the `not-implemented` stub. When an `invite_token` is present:
- If the signing-in account is already a registered active user (Person+User exist, `IsActive`), **ignore the token** and fall through to normal login (Q5).
- Otherwise call the injected `IInvitationService` client (Step 3/5) with `{ token, signing-in email }`. On a failure outcome, redirect to `/invite/error?reason={outcome}` (map the contract outcome to the spec's reason strings: not-found/used/revoked/expired/email-mismatch) and do **not** sign in (`context.HandleResponse()`), no cookie. On success: provision the User with `Role = returned role` (not the default), **reuse an existing Person** for that email (`PersonRepository.GetByEmailAsync`) linking a new User, or create a new Person if none (mirroring the bootstrap provisioning at `:102-114`); call `User.RaiseCreated()` post-save (Step 1); then build claims and sign in exactly as the existing success path (`:164-185`), redirecting to `/`.

This is **None** (auth-callback orchestration): describe operations + acceptance; the developer decides decomposition into private methods (`project-rules.md`: >5 sequential sub-ops → private methods; `csharp.md`: ~60-line method cap). Do **not** write a method body here.

- **Skill:** None. Pattern references: existing bootstrap/normal-login provisioning + claims-building in the same file (`:99-185`); error-redirect pattern (`:128`, `:92`).
- **Key domain constraints:** email lower-cased/compared case-insensitively throughout (`Person.Email` is stored lower-cased); role comes from the invitation; Person reuse leaves source unchanged; the `not-implemented` redirect must be **gone**.
- **Dependency:** Steps 1, 3, 5.
- **Accept:**
  1. `grep "not-implemented" src/Services/Identity` returns **zero** hits.
  2. Valid token + matching Google email → cookie set + 302 to `/`; User has the invitation's role; invitation now reports Used via the validate endpoint (AC 1, 3).
  3. Email mismatch → no cookie, 302 to `/invite/error?reason=email-mismatch` (AC 2).
  4. Expired/revoked/used/unknown → no cookie, 302 to `/invite/error?reason={specific}` (AC 4).
  5. Existing Person for the email → new User linked to it, one Person row (AC 5).
  6. Already-registered active user with a token → normal login, 302 to `/`, no invite-error (AC 6).
  7. `dotnet build` green.
- **Confidence:** medium — the branch interacts with the existing claims/sign-in tail; the developer should explore `:99-186` fully before editing and reuse the existing claims-building rather than duplicating it.

### Step 7 — Define the company-domain seed integration event (Q8)
**File (create):** `src/BuildingBlocks/Blocks.Contracts/IntegrationEvents/FirstAdministratorRegistered.cs` (name is the architect's; developer keeps it).

A `record` carrying the candidate company domain (e.g. `FirstAdministratorRegistered(string CompanyDomain)`), mirroring the existing `SprintSynced` record style (plain record in `Blocks.Contracts/IntegrationEvents`, no MassTransit attributes).

- **Skill:** Follow `add-integration-event` (EventContract workflow). Precedent: `Blocks.Contracts/IntegrationEvents/SprintSynced.cs`.
- **Dependency:** none.
- **Accept:** the record exists in `Blocks.Contracts/IntegrationEvents`; carries the company-domain string; `dotnet build` green.
- **Confidence:** high.

### Step 8 — Publish the seed event from an `IEventHandler<UserCreated>` (Q14, option a)
**Files (create):** an `IEventHandler<UserCreated>` co-located in Identity (e.g. `Identity.API/Features/Auth/.../AnnounceCompanyDomainOnUserCreatedHandler.cs` — effect-named per `create-domain-event-handler`). **Modify:** `Identity.API/DependencyInjection.cs` / csproj for the MassTransit publish dependency (see Step 9 for bus wiring) + the public-provider constant.

The handler reacts to `UserCreated`, re-loads the user + person (events carry only the id — `project-rules.md`), and **gates on the bootstrap predicate `user.Role == "Admin" AND active-admin-count == 1`** (`UserRepository.CountActiveAdminsAsync` already exists — `UserRepository.cs:14`). When the predicate holds: derive the domain from the person's email with the **identical extraction the Fokus gate uses** — `email.Split('@')[1]` (see `CreateInvitationEndpoint.cs:21`); if the domain is a **known public provider**, skip (no publish); otherwise `IPublishEndpoint.Publish(new FirstAdministratorRegistered(domain))`. The public-provider list is a **single constant in the Identity module** (non-exhaustive by design — gmail.com, outlook.com, …; spec §Company domain / L2).

**L3 — raise timing is load-bearing:** `UserCreated` dispatches in `DispatchDomainEventsInterceptor.SavedChangesAsync` (**post-save**). The bootstrap path must call `User.RaiseCreated()` **after** the User is committed (mirroring the Person pattern at `AuthConfiguration.cs:105-107`) so that when the handler runs `CountActiveAdminsAsync()` already counts the new Admin → `== 1` holds for the true bootstrap admin. Do **not** move the raise before the save (that would make the count 0 and the predicate never fire on the genuine first admin).

- **Skill:** Follow `create-domain-event-handler` (effect-named, co-located) + `add-integration-event` Publisher (FastEndpoints `IEventHandler<T>` variant). Precedent for the bus publish: `SprintIssueSyncService.cs:77` (`IPublishEndpoint.Publish`).
- **Feature inputs:** predicate `Role=="Admin" && active-admin-count==1`; extraction `email.Split('@')[1]`; public-provider constant in Identity; publish `FirstAdministratorRegistered`.
- **Dependency:** Steps 1 (User raises `UserCreated`), 7 (event contract), 9 (bus publish wiring + Identity MassTransit reference).
- **Accept:**
  1. An `IEventHandler<UserCreated>` exists in Identity, effect-named; it is the first such handler (`grep "IEventHandler<" src` shows it).
  2. It publishes `FirstAdministratorRegistered` **only** when `Role=="Admin" && active-admin-count==1` and the domain is not a public provider; otherwise no publish.
  3. The domain extraction is `email.Split('@')[1]` (byte-identical to `CreateInvitationEndpoint.cs:21`).
  4. The public-provider list is a single Identity constant; gmail.com/outlook.com present; non-exhaustive.
  5. `dotnet build` green.
- **Confidence:** medium — wiring an `IEventHandler` + `IPublishEndpoint` in Identity (which has neither today) is new; depends on Step 9's bus wiring.

### Step 9 — Consume the seed event in Fokus + wire bus topology (Q8) [BUILD]
**Files (create):** seed consumer in Fokus (e.g. `Fokus.API/Features/Settings/Consumers/SeedCompanyDomainOnFirstAdministratorRegisteredConsumer.cs`). **Modify:** `Fokus.API/DependencyInjection.cs:33` (the single `AddMassTransit`) to add `x.AddConsumers(typeof(DependencyInjection).Assembly)`; `Identity.API` csproj + DI for the MassTransit publish dependency (so `IPublishEndpoint` resolves in Identity).

**Build the missing registration first:** there is exactly one `AddMassTransit` (Fokus) and **no** `AddConsumers` anywhere — add `AddConsumers(...)` to the existing Fokus `AddMassTransit` so the seed consumer is discovered (one process = one bus; do **not** add a second `AddMassTransit`). Identity needs `IPublishEndpoint` available — add the `MassTransit` package reference to `Identity.API` and ensure the single bus (registered in Fokus, composed by the Host) provides the publish endpoint process-wide; if registration order in `Program.cs` matters (Fokus module added after Identity), confirm `IPublishEndpoint` resolves for the Identity handler.

**Then follow the Consumer pattern:** the consumer must be **idempotent** (seed only if `AppSettings.CompanyDomain` is still null — BR 9 "exactly once"). **L5 — mutate the LOADED instance, never construct a fresh one:** `GetAsync()` the singleton `AppSettings`, and if `CompanyDomain is null` set `CompanyDomain` on **that loaded instance** and persist via `AppSettingsRepository.SaveAsync(settings)`. Do **not** `new AppSettings { CompanyDomain = ... }` — `AppSettings` carries `ComplexProperty` VO columns (`BugRatio`/`Xray`/`CycleTime`/`Sync`, etc.) and a fresh instance would wipe them (`AppSettingsRepository.SaveAsync:27-38` re-assigns VOs from the passed object). Round-tripping the loaded instance preserves them. Identity never writes Fokus settings directly (BR 10).

- **Skill:** Build (consumer registration + Identity publish wiring absent) then Follow `add-integration-event` (Consumer workflow). Precedent for idempotent consume + repo save: the skill's Simple Consumer + `AppSettingsRepository`.
- **Feature inputs:** seed `AppSettings.CompanyDomain` only if currently null; mutate the loaded singleton (not `new AppSettings{}` — L5); idempotent; `sealed class`.
- **Dependency:** Steps 7, 8.
- **Accept:**
  1. A `sealed` consumer of `FirstAdministratorRegistered` exists in Fokus and sets `AppSettings.CompanyDomain` only when null.
  2. `AddConsumers(...)` is added to the existing single `AddMassTransit` in `Fokus.API/DependencyInjection.cs`; **no** second `AddMassTransit` exists (`grep -c "AddMassTransit" src` stays 1).
  3. `Identity.API` references MassTransit and the `IEventHandler` resolves `IPublishEndpoint` at runtime.
  4. End-to-end: fresh system, first admin signs in with a corporate email → `AppSettings.CompanyDomain` is seeded; with a gmail.com admin → stays null (AC 7).
  5. After seed, `POST /api/invitations` outside the domain → 400, inside → 201 (AC 8 — existing gate, now populated).
  6. `dotnet build` green.
- **Confidence:** medium — the single-bus topology across two modules (publisher in Identity, consumer in Fokus, one `AddMassTransit`) is the wiring risk; verify `IPublishEndpoint` resolution and consumer discovery at runtime.

### Step 10 — Jira email best-effort fetch + make `Person.Email` optional (H1)
**Files (modify):** `src/Modules/Jira/Jira.Contracts/JiraIssue.cs` (add nullable `EmailAddress` to `JiraUser`), `src/Modules/Jira/Jira.Contracts/IJiraClient.cs` (add a `GetUserAsync(string accountId, CancellationToken ct)` method), `src/Modules/Jira/Jira.RestApi/IJiraApi.cs` (add the Refit `[Get("/rest/api/3/user")]` with `[Query] string accountId`), `src/Modules/Jira/Jira.RestApi/RestApiJiraClient.cs` + `TeamManagedJiraClient.cs` (implement the new method), `src/Services/Fokus/Fokus.Domain/Developer/Developer.cs` (add `string? Email`), `src/Services/Fokus/Fokus.API/Features/Sync/JiraDomainMapper.cs` (propagate email in `MapDeveloper` + its `IRegister` config), `src/Services/Identity/Identity.Domain/Persons/Person.cs` (make `Email` `string?`), `src/Services/Identity/Identity.Persistence/Configurations/PersonConfiguration.cs:18-21` (drop `IsRequired()` on `Email`; scope the unique index to non-null), `src/Services/Identity/Identity.Domain/Persons/Behaviors/Person.cs` (Google factory still lower-cases email; reconcile path tolerates null). **Files (create):** an Identity migration (Person.Email nullable + scoped index) + a Fokus migration (`Developer.Email` column).

H1 resolution (user-decided): Jira Cloud has no developer email on the issue assignee and hides email by default, so the cross-module Person cannot be keyed/required on email.
- **Best-effort email fetch:** add a Jira `GET /rest/api/3/user?accountId={id}` call returning a `JiraUser` whose new nullable `EmailAddress` is populated **iff** Jira returns it (often restricted → null). Propagate best-effort into `Developer.Email` and onward to the Person upsert request.
- **`Person.Email` optional:** make it `string?`; scope the existing unique NOCASE index to non-null values only (SQLite filtered/partial unique index) so multiple emailless Jira Persons don't collide. Google-sourced Persons still carry (lower-cased) email; the match key for Google stays email, for Jira becomes `ExternalId`+`Source` (Step 4).

This is **None** — adding a Jira REST method + DTO field follows the existing Refit `IJiraApi` pattern; relaxing a required domain property + scoped index has no skill. Describe operations + acceptance; developer decides decomposition. Do **not** write method bodies.

- **Skill:** None. Pattern references: existing Refit methods in `IJiraApi.cs` (same `[Get]`/`[Query]`/`IApiResponse<T>` shape); `JiraDomainMapper.MapDeveloper:67-81` (the mapping site); `PersonConfiguration.cs:18-21` (the index to scope).
- **Key domain constraints:** email best-effort (may be null); Jira email lower-cased when present (consistency with Person email handling); the unique Email index must exclude nulls; `Source=Jira` Persons keyed by `ExternalId=AccountId`.
- **Dependency:** none for the Jira/Developer half; the `Person.Email`-nullable migration must land **before** Step 4's server stores email-optional Persons and **before** Step 11 runs a real sync. (Order Step 10 before Step 11; Step 4 depends on it.)
- **Accept:**
  1. `JiraUser` has a nullable `EmailAddress`; `IJiraClient`/`IJiraApi` expose a user-by-accountId fetch following the existing Refit pattern; both client impls implement it.
  2. `Developer` has `string? Email`; `MapDeveloper` + the Mapster `IRegister` config propagate it (null-safe).
  3. `Person.Email` is `string?`; `PersonConfiguration` drops `IsRequired()` and scopes the unique index to non-null; an Identity migration captures the nullability + index change; a Fokus migration adds `Developer.Email`.
  4. `dotnet build SprintRituals.slnx` green.
- **Confidence:** medium — the Refit method + DTO field are mechanical; the SQLite **filtered unique index** (`.HasFilter("[Email] IS NOT NULL")` or the EF `.IsUnique()` + filter form) is the one detail to get right against SQLite; the developer should verify the generated index DDL.

### Step 11 — Wire the Fokus Jira-developer sync path to upsert Persons via gRPC (Q9/Q10/H1)
**File (modify):** `src/Services/Fokus/Fokus.API/Features/Sync/SprintIssueSyncService.cs` (the developer-discovery points: `SyncAsync` at `:157-161` and `SyncEpicDiscoveryAsync` at `:123-125`, where `JiraDomainMapper.MapDeveloper` produces a developer).

Inject the `IPersonService` client (Step 3) into `SprintIssueSyncService`. For each discovered Jira developer, call `UpsertPersonAsync` to reconcile it into Identity as a Person — request keyed on **`ExternalId = AccountId`, `Source = Jira`, `Email = developer.Email` (nullable, best-effort from Step 10)**, name/picture from the developer. This is the **single in-scope consumer** of the Person contract (Q10). Pass the cancellation token. Do not block the sync on a gRPC failure beyond the existing per-sprint try/catch semantics (partial failures already don't abort the batch — `:84-87`).

This is **None** (modifying an existing sync service to call a gRPC client). Describe the operation + acceptance; the developer decides where in the discovery flow to call and how to batch. Do **not** write the loop body here.

- **Skill:** None. Pattern references: the existing developer upsert (`developerRepository.UpsertAsync`, `:160`) marks the discovery points; the gRPC client call pattern is the `create-grpc-contract` Client "Usage in Handlers" (inject interface, pass the token).
- **Key domain constraints:** `ExternalId=AccountId`; `Source=Jira`; email best-effort/nullable; reconciliation is **Fokus-initiated on Jira sync** — NOT driven by `UserCreated` (BR 11/12); one direction only.
- **Dependency:** Steps 2, 3, 4, 10.
- **Accept:**
  1. `SprintIssueSyncService` injects `IPersonService` and calls `UpsertPersonAsync` for discovered developers (`ExternalId=AccountId`, `Source=Jira`, nullable email), token propagated.
  2. After a Fokus Jira sync, the synced developer appears as a Person row with `Source=Jira` and `ExternalId=AccountId` in `identity.db` (email filled iff Jira returned it) — the patched AC-8 observable.
  3. No direct Identity↔Fokus project reference is introduced (`grep "Identity" Fokus.*.csproj` and vice-versa → zero; only `Blocks.Contracts` is shared).
  4. A gRPC failure for one developer does not abort the sprint batch beyond existing semantics.
  5. `dotnet build` green.
- **Confidence:** medium — choosing the call site within the discovery flow and the failure semantics needs the developer to read `SyncAsync`/`SyncEpicDiscoveryAsync` first.

### Step 12 — Documentation: record the two-contract topology and clear the deferred lists
**Files (modify):** `src/Services/Identity/CLAUDE.md` (remove `PersonGrpcService` / redemption / company-domain-seed from "Deferred"; record the new topology), **`src/Services/Fokus/CLAUDE.md`** (M5 — update "gRPC clients" None→`IPersonService` client, "gRPC server" None→`IInvitationService` server, "Integration events Consumed" None→`FirstAdministratorRegistered`; while there, the pre-existing "Published: None" is already stale given `SprintSynced` — correct it too), `docs/conventions/known-deviations.md` or `docs/architecture/decisions.md` cross-ref as appropriate (architect/team-lead owns ADR edits — propose, don't silently amend), the root `CLAUDE.md` "Deferred (infra increment)" line, and `src/Modules/Jira/CLAUDE.md` if the user-fetch method warrants a note.

Record: (a) two opposite-direction gRPC contracts now exist — Identity hosts `IPersonService` (server) + is the `IInvitationService` client; Fokus hosts `IInvitationService` (server) + is the `IPersonService` client; (b) the company-domain seed rides the in-process bus via `FirstAdministratorRegistered`; (c) the redemption seam is complete (no `not-implemented` stub); (d) `Person.Email` is now optional + Jira-sourced Persons key on `ExternalId` (H1). KB-equivalent step (no `docs/kb/` index — confirmed empty; nearest homes are the module CLAUDE.md + conventions). Numbered so the done-check catches it.

- **Skill:** None (documentation).
- **Dependency:** Steps 1–11.
- **Accept:** **both** `Identity/CLAUDE.md` and `Fokus/CLAUDE.md` reflect the post-Pass-4 gRPC client/server + integration-event topology (M5); `Identity/CLAUDE.md` no longer lists the three items as deferred; root `CLAUDE.md` "Deferred (infra increment)" line updated; no `not-implemented` reference remains in docs describing live behavior. (Doc artifacts — not source files.)
- **Confidence:** high.

---

## Cross-Service Changes

- **Two new gRPC contracts** (`Blocks.Contracts/Grpc`, `[DataContract]`/`Task<T>`/`CancellationToken` style — M2): `IInvitationService` (Fokus server / Identity client) and `IPersonService` (Identity server / Fokus client) — opposite directions, both over the single in-process `localhost:5001` h2c endpoint. **Identity hosts its first gRPC server** (committed `AddCodeFirstGrpc` + `MapGrpcService` reached from `Program.cs` — M3; double `AddCodeFirstGrpc` is intentional/idempotent).
- **One new integration event** (`Blocks.Contracts/IntegrationEvents`): `FirstAdministratorRegistered`, published by Identity's `IEventHandler<UserCreated>` (post-save — L3), consumed by a Fokus consumer that mutates the **loaded** `AppSettings` to seed `CompanyDomain` (L5).
- **New gRPC client infrastructure** in `Blocks.AspNetCore` + CPM `protobuf-net.Grpc` + `Grpc.Net.Client` (Step 3 — M4, NOT `Grpc.Net.ClientFactory`); client config under `Grpc:Services:<name>:Url` (L4). First client wiring in the repo.
- **Jira adapter extended** (Step 10): a user-by-accountId fetch + nullable `JiraUser.EmailAddress`; `Developer.Email` added; `Person.Email` made nullable (H1).
- **MassTransit topology:** single bus (Fokus's `AddMassTransit`) gains `AddConsumers`; Identity gains the publish dependency. No second bus.
- **No project reference** crosses the Identity↔Fokus boundary — only `Blocks.Contracts` is shared (ADR-001).

## Migration Notes

Three migrations, all **mandatory**:
- **Identity #1 (Step 1, M1):** `PromoteUserToAggregate` — adds the four audit columns to `Users` (Person is the on-disk proof EF maps them). `dotnet ef migrations add PromoteUserToAggregate -p src/Services/Identity/Identity.Persistence -s src/Services/Identity/Identity.API`.
- **Identity #2 (Step 10, H1):** `MakePersonEmailOptional` — `Persons.Email` nullable + unique index scoped to non-null. Same `-p`/`-s` projects.
- **Fokus (Step 10, H1):** `AddDeveloperEmail` — `Developer.Email` nullable column. `dotnet ef migrations add AddDeveloperEmail -p src/Services/Fokus/Fokus.Persistence -s src/Services/Fokus/Fokus.API` (or the Persistence-as-startup variant per `project-rules.md` if the API project lacks `Design`).
- No seed-data changes (`AppSettings.CompanyDomain` already exists; the seed only sets a value).

## Testing Strategy

Manual (no automated suite — accepted for this app). Each maps to an Acceptance Criterion:
1. **Redemption success (AC 1, 3):** create an invitation (Admin), open the link, sign in with the matching Google account → cookie set, 302 to `/`, User has the invitation role; re-validate token → `used`; second redeem → fails `used`.
2. **Email mismatch (AC 2):** redeem with a different Google email → no cookie, 302 `/invite/error?reason=email-mismatch`.
3. **Bad invitation (AC 4):** expired/revoked/used/unknown token → no cookie, 302 `/invite/error?reason={specific}`.
4. **Person reuse (AC 5):** pre-seed a Person (source=Jira, no User) for the invited email, redeem → one Person row, now linked to a User, source unchanged.
5. **Already-registered (AC 6):** active user clicks an invite link → normal login, 302 `/`, no invite-error.
6. **Auto-seed (AC 7):** fresh system, first admin with a corporate email → `CompanyDomain` seeded; fresh system, gmail.com admin → stays null.
7. **Gate in effect (AC 8):** after seed, `POST /api/invitations` outside domain → 400, inside → 201.
8. **Person reconciliation (patched AC 8/Flow 3):** run a Fokus Jira sync → discovered developer appears as a Person row with `Source=Jira` and `ExternalId=AccountId` in `identity.db` (email filled iff Jira returned it via the user fetch — `null` otherwise, which is now valid); a second sync of the same developer updates (matches by `ExternalId`) rather than duplicating; confirm no Identity↔Fokus project reference exists.
9. **Build (AC final):** `dotnet build SprintRituals.slnx` green (filter `error CS`); `not-implemented` stub gone; all three migrations apply cleanly to `identity.db`/`fokus.db`.

## KB Impact

No `docs/kb/` index exists in this repo (confirmed empty). The documentation lands in module CLAUDE.md + conventions, captured as numbered **Step 12** (not a trailing section). If a KB is introduced later, the redemption flow, the two-contract topology, the company-domain seed rule, and the Jira-sourced optional-email Person keying are the candidate entries.

## Open Questions

None — Q1–Q15 all Answered (see Locked decisions and `questions.md`).

---

## Plan Review

**Critic (Mode 2, code-grounded) verdict:** 1 HIGH + 5 MEDIUM + 5 LOW — no locked decision contradicted (L1, Q13 two-direction gRPC, Q14 bootstrap-gated seed, ADR-001 all honored). Full findings: `docs/specs/adhoc-Pass4-IdentityExtraction/delivery/plan-critic-findings.md`. **User resolutions folded in below.** The decomposition (now 12 steps unsplit) was accepted by the critic with no correctness problem in the chain.

| Finding | Sev | Disposition | Where fixed |
|---------|-----|-------------|-------------|
| H1 — Jira developers have no email, but the Person upsert matched/required email (AC-8 unachievable as written) | **HIGH** | **Resolved by user — best-effort email + `ExternalId` key.** New **Step 10** adds the Jira `GET /rest/api/3/user?accountId=` fetch (+ nullable `JiraUser.EmailAddress`), propagates email best-effort through `Developer`/`JiraDomainMapper`, makes `Person.Email` **nullable** with a non-null-scoped unique index (2nd Identity migration), and keys Jira-sourced Persons on `ExternalId`+`Source`. Step 2/4/11 + Scope + Domain/Data Model + AC-8 observable all updated. PO patching spec AC-8 + Person-email-optional in parallel. |
| M1 — Step 1 migration is mandatory (4 audit columns), not conditional | MED | Fixed | Step 1 reworded: migration mandatory (Person maps all 4 audit columns on disk → User will too); `PromoteUserToAggregate` is a create-file in the step; "may/iff/confirm" framing removed from Step 1, Data Model, Migration Notes; confidence raised to high. |
| M2 — contracts must use `[DataContract]`/`[DataMember(Order=n)]` + `Task<T>` + `CancellationToken`, not `[ProtoContract]`/`ValueTask`/`CallContext` (won't compile in `Blocks.Contracts`) | MED | Fixed | Step 2 rewritten to mirror `ISprintCatalogService`/`SprintInfo`; the protobuf-net/`ValueTask`/`CallContext` text struck; accept adds a `grep` that returns zero protobuf-net attrs; Steps 4/5 signatures aligned; anti-pattern note records the deliberate skill-guidance override. |
| M3 — Identity's first gRPC server needs a committed `Program.cs`-reachable `MapGrpcService`, not a hedge | MED | Fixed | Step 4 made `AddCodeFirstGrpc` + `MapGrpcService<PersonGrpcService>` (reached from `Program.cs`) a committed sub-task with file list incl. `Host/Program.cs`; the double `AddCodeFirstGrpc` is noted intentional/idempotent. |
| M4 — CPM packages must match the ported helper (`Grpc.Net.Client`, not `Grpc.Net.ClientFactory`) | MED | Fixed | Step 3 pins `protobuf-net.Grpc` + `Grpc.Net.Client`, explicitly excludes `Grpc.Net.ClientFactory`/Polly, names the exact reference method + lines (62-89), and drops the TLS/`#if DEBUG` handler (plain h2c). |
| M5 — `Fokus/CLAUDE.md` left stale (Fokus becomes IPersonService client, IInvitationService server, FirstAdministratorRegistered consumer) | MED | Fixed | Step 12 modify list now includes `Fokus/CLAUDE.md` (gRPC client/server + Integration events sections) alongside `Identity/CLAUDE.md`; the pre-existing stale "Published: None" vs `SprintSynced` also flagged for correction. |
| L1 — `Invitation.Accept(now)` throws; derive outcomes from pre-checks, never an exception across gRPC | LOW | Fixed | Step 5 makes check-then-accept explicit: all failure outcomes come from pre-checks; `Accept` is reached only when it cannot throw; no exception-to-outcome translation across the boundary. |
| L2 — Step 5 needs an explicit `EmailMismatch` outcome (validate endpoint never produces it) | LOW | Fixed | Step 2 + Step 5 call out `EmailMismatch` as a redeem-only outcome beyond the validate endpoint's four reasons. |
| L3 — keep the `UserCreated` raise POST-save so `CountActiveAdminsAsync()==1` holds | LOW | Fixed | Step 8 adds a load-bearing "raise after commit" note (mirrors `AuthConfiguration.cs:105-107`); do-not-move-before-save directive. |
| L4 — client config uses `Grpc:Services:<name>:Url`, not the Kestrel server-bind key | LOW | Fixed | Step 3 config bullet pins the `Grpc:Services:<name>:Url` client-dial shape and explicitly rejects reusing `Kestrel:Endpoints:Grpc:Url`. |
| L5 — consumer mutates the LOADED `AppSettings` via the repo (not `new AppSettings{}`) to avoid wiping ComplexProperty VOs | LOW | Fixed | Step 9 adds the mutate-loaded-instance directive with the `AppSettingsRepository.SaveAsync:27-38` VO-reassign evidence. |

**Note on size:** now **12 numbered steps** (H1 added Step 10). Per the >10-step rule, splitting was re-considered. Steps 1–6 remain one prerequisite chain (promotion → contracts → client infra → both servers → redemption seam); H1's Step 10 is a prerequisite of the Person-upsert consumer (Step 11) and of Step 4's email-optional storage. A seam-based split still yields sub-plans of <2 independent steps or cross-sub-plan dependencies. **Kept as one plan of 12 steps.** Flagged to the team lead.

**Post-fix status:** all 11 findings folded (H1 resolved per user decision; M1–M5 + L1–L5 fixed in the named steps). No second critic round per the team lead's instruction. Plan ready for approval.
