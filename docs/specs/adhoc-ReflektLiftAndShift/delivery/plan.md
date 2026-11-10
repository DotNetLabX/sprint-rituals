# Implementation Plan — adhoc-ReflektLiftAndShift (RA1)

**Status:** Approved (revised after code-grounded critic — NO-GO findings folded)
**Pass:** RA1 — Reflekt Integration Program (`docs/backlog.md`)
**Binding input:** `docs/architecture/decisions.md` ADR-001/004/011/013/014/016 + the RA1 backlog row. No `spec.md` (adhoc).
**Source module:** `<reflekt-repo>/src/Services/Reflekt/{Reflekt.API,Reflekt.Domain,Reflekt.Persistence}` (read-only reference; copy, don't move-from).

## What RA1 is — and is not

**Is:** a lift of the Reflekt module into the monolith to a **green build that boots**, on the target's shared BuildingBlocks, CPM, thin composition root, and ADR-011 currency. Reflekt's domain (cards/groups/votes/phases) and SignalR behavior are unchanged.

**Is not:** semantic Identity/sprint/frontend change. The `Sprint`→`RetroSession` rename, `User` removal, `Participant`→`Person` link, Fokus snapshot/context panel, and frontend merge are RA2/RA3/RA4/RA5. RA1 keeps Reflekt's `User` + Google-login endpoints compiled but unmapped (Q1=A).

**Verified compatibility:** domain events are identical (`IDomainEvent : IEvent`); `AggregateRoot<TId>` has the same `DomainEvents`/`AddDomainEvent` shape (target's `AddDomainEvent` is `public`, Reflekt's `protected` — target is more permissive, OK). The base differs in three ways the steps below handle: the target `ApplicationDbContext<T>` is **generic**, the target `Entity.Id` setter is `public` (Reflekt `protected`) and drops the `notnull` constraint, and the target `AggregateRoot` carries audit *properties* (mapped only by `AuditedEntityConfiguration`, which Reflekt does not use).

## Plan Review (critic, code-grounded — 2026-06-14)

NO-GO on the first draft; all findings re-verified against source and folded in:

| # | Sev | Finding | Resolution |
|---|-----|---------|-----------|
| F1 | HIGH | Draft claimed "all `Send*Async` are SignalR, no currency work." False — **43 endpoint files** carry FastEndpoints `Send` calls (the first grep wrongly required a leading `.`). | New **Step 5** — framework-currency migration to `Send.*` (ADR-011). |
| F2 | HIGH | `/hubs/sprint` **already taken** by Fokus's `SprintHub` (`Fokus.API/DependencyInjection.cs:104`), and Fokus already calls `AddSignalR()` (`:42`). Reflekt's hub collides on the route. | Step 6 — map Reflekt's hub at **`/hubs/retro`**; do **not** re-register `AddSignalR`. Frontend realigns in RA5. |
| F3 | HIGH | Auth scope undercounted. `AddReflektAuthorization()` registers policies (`ScrumMaster`, `OwnerOrScrumMaster`, `GoogleUser`) bound to the **Cookie** scheme; ~25 endpoints depend on them — not 4. | Step 7 — **keep** `AddReflektAuthorization()`; its cookie policies resolve against **Identity's** cookie scheme (present in Host). Drop only Reflekt's own `AddAuthentication/AddCookie/AddGoogle`; add the guest scheme. |
| F4 | MED | Target `ApplicationDbContext<TDbContext>` is generic; `ReflektDbContext : ApplicationDbContext(options)` won't compile against it. | Step 2 — `ReflektDbContext : ApplicationDbContext<ReflektDbContext>(options)`. |
| F5 | MED | `DbInitializer` is `internal` — the Host (separate assembly) can't call it. | Step 8 — expose a `public` seed extension (or `InternalsVisibleTo` Host). |
| F6 | MED | CPM version skew between Reflekt's and the target's `Directory.Packages.props`; `Mapster.DependencyInjection`/`AngleSharp` unpinned. | Step 4 — re-baseline against `reflekt/Directory.Packages.props`; pin both at concrete versions; target wins on overlaps, verify no breaking downgrade. |
| F7 | LOW | Audit-columns migration premise is wrong — `EntityConfiguration<T>` (what Reflekt uses) does not map the audit props; only `AuditedEntityConfiguration` does. | Step 9 — add **whatever delta `migrations add` actually generates** after the base swap (possibly none), not an assumed audit migration. |

## Skill Mapping

| # | Step | Skill | Disposition | TDD |
|---|------|-------|-------------|-----|
| 1 | Move projects + solution registration | — | None (mechanical) | no |
| 2 | Repoint BuildingBlocks; generic DbContext base; delete Reflekt's `Blocks.*` | — | None (mechanical repoint) | no |
| 3 | Swap domain-event dispatch to target interceptor | `persistence-patterns` | Follow | no |
| 4 | CPM — add + re-baseline package versions | `central-package-management` | Follow | no |
| 5 | FastEndpoints currency — migrate `Send*Async` → `Send.*` | `framework-currency` | Follow | no |
| 6 | Module DI `AddReflektModule`/`MapReflektEndpoints`; hub route | `service-registration` | Follow | no |
| 7 | Auth — guest scheme; keep authorization policies on Identity cookie | — | None (lift-specific) | no |
| 8 | Host composition + `ReflektDbContext` wiring + design-time factory + public seed | `service-registration` + `persistence-patterns` | Follow | no |
| 9 | Migration (real EF delta) + green-build verification | `persistence-patterns` | Follow | no |

All steps are wiring/mechanical — TDD `no` throughout (the skill-log done-check applies only to the Follow steps).

---

## Step 1 — Move projects + register in the solution

Copy `Reflekt.API`, `Reflekt.Domain`, `Reflekt.Persistence` into `src/Services/Reflekt/`. Add a `/Services/Reflekt/` folder to `SprintRituals.slnx` with the three projects (mirror `/Services/Fokus/`). Do **not** copy Reflekt's `src/BuildingBlocks/*` or the Vue app.

**Accept:** three projects under `src/Services/Reflekt/`; listed in `SprintRituals.slnx`; `dotnet restore` resolves.

## Step 2 — Repoint BuildingBlocks; make the DbContext base generic; drop Reflekt's BuildingBlocks

Repoint every `ProjectReference`/`using` in Reflekt.Domain + Reflekt.Persistence to the target's BuildingBlocks. Base type names match; namespaces/projects differ:

| Reflekt | Target |
|---------|--------|
| `Blocks.Domain` (AggregateRoot, Entity, IDomainEvent, DomainException) | `Blocks.Domain.Entities` / `.Events` / `.Exceptions` |
| `Blocks.Persistence` (ApplicationDbContext, EntityConfiguration, RepositoryBase) | `Blocks.EntityFrameworkCore` |
| `Blocks.Exceptions.*`, `Blocks.Core.Guard*` | target supersets (no porting) |

ProjectRefs: Reflekt.Domain → `Blocks.Domain`; Reflekt.Persistence → `Blocks.Domain` + `Blocks.EntityFrameworkCore`; Reflekt.API → add `Blocks.AspNetCore` + `Blocks.FastEndpoints`.

**F4 — generic DbContext base:** change `ReflektDbContext : ApplicationDbContext(options)` → `ReflektDbContext : ApplicationDbContext<ReflektDbContext>(options)` (target base is `ApplicationDbContext<TDbContext>`).

**Base-shape verification (do before asserting green):** target `Entity.Id` is `public set` and drops `where TId : notnull`; target `AddDomainEvent` is `public`. Confirm no Reflekt aggregate or EntityConfiguration relies on the old `protected`/`notnull` shape. Reflekt's `IHasDomainEvents` → target uses `IAggregateRoot`; confirm aggregates still satisfy the interceptor's interface.

**Grep-acceptance:** `grep -rn "Blocks.Persistence\|using Blocks.Domain;\|namespace Blocks.Domain;" src/Services/Reflekt` → zero hits; Reflekt's `src/BuildingBlocks` referenced nowhere.

## Step 3 — Swap domain-event dispatch to the target's interceptor

In `Reflekt.Persistence/DependencyInjection.cs`, replace Reflekt's `DomainEventPublishingInterceptor` with the target's `DispatchDomainEventsInterceptor` (`Blocks.EntityFrameworkCore`, added to the `DbContextOptions`) + register `DomainEventPublisher` (`Blocks.FastEndpoints`) as `IDomainEventPublisher`. Follow `persistence-patterns` (interceptor + DbContext setup). Events unchanged.

**Accept:** target interceptor + publisher registered; no reference to Reflekt's old interceptor; a raised event still resolves its `Broadcast*Handler`.

## Step 4 — Central Package Management (re-baseline)

Reflekt is already on CPM. Re-baseline its versions against the target's `src/Directory.Packages.props` (**read `reflekt/Directory.Packages.props` for the source-of-truth versions Reflekt was building with**). Target wins on overlaps — but flag any Reflekt package whose target pin is a **downgrade** (potential breaking change). Add the two missing packages with concrete versions: `AngleSharp` and `Mapster.DependencyInjection`. Follow `central-package-management` (three-form grep).

**Accept:** both packages declared as `<PackageVersion>` at concrete versions; no Reflekt csproj carries `Version=`; three-form grep clean; downgrades (if any) noted.

## Step 5 — FastEndpoints currency (`Send*Async` → `Send.*`)

Reflekt's endpoints use FastEndpoints `Send` calls across **43 files** (grep at plan time). Migrate any legacy `SendOkAsync`/`SendAsync`/`Send*Async` to the modern `Send.*` API per ADR-011. Follow `framework-currency` (two-stage detection; the SignalR `Clients.*.SendAsync` calls are the documented false-positive — do **not** touch them).

**Accept:** `framework-currency` detection grep finds zero legacy FastEndpoints `Send*Async` in `src/Services/Reflekt`; SignalR `SendAsync` untouched; builds against FastEndpoints 8.1.

## Step 6 — Module DI extension + hub route (F2)

Refactor `AddReflektApi`/`AddReflektPersistence` + `Use*` into `AddReflektModule(IConfiguration)` + `MapReflektEndpoints()` per ADR-014. Follow `service-registration`.

- **Remove from the module** (Host owns once): `AddFastEndpoints()`, `AddOpenApi()`, `MapFastEndpoints()`, `MapFallbackToFile`, Scalar/OpenApi dev mapping, Reflekt's own `GlobalExceptionMiddleware` (delete it — Host uses `Blocks.AspNetCore.GlobalExceptionMiddleware`), and **`AddSignalR()`** (Fokus already registers it — `Fokus.API/DependencyInjection.cs:42`; do not double-register).
- **Keep in the module:** Mapster scan + `ServiceMapper`, options binding, `AddReflektOpenGraph()` (incl. its `HttpClient`), `PhaseActionCache`, `TimerService`.
- **`MapReflektEndpoints()` maps the hub at `/hubs/retro`** (NOT `/hubs/sprint` — taken by Fokus's `SprintHub`) + the `TimerService` expiry callback. The frontend's hub URL is realigned in RA5 (frontend not served yet).

**Accept:** `AddReflektModule`/`MapReflektEndpoints` exist; no module-level FastEndpoints/OpenApi/fallback/SignalR re-registration; hub at `/hubs/retro`; timer callback preserved.

## Step 7 — Auth: guest scheme + authorization policies on Identity's cookie (F3, Q1=A)

Reflekt's `AddReflektAuthentication` registers `AddAuthentication(Cookie default).AddCookie().AddGoogle().AddScheme<GuestToken>` — its cookie/Google scheme registration collides with Identity's in the Host. Reflekt's `AddReflektAuthorization` registers policies, three bound to the **Cookie** scheme (`ScrumMaster`, `OwnerOrScrumMaster`, `GoogleUser`), gating ~25 endpoints.

- **Drop** Reflekt's `AddAuthentication`/`AddCookie`/`AddGoogle` and `OnGoogleTicketReceived` (writes Reflekt `User` — RA2). 
- **Add** only the guest scheme onto the Host's Identity auth builder: `.AddScheme<AuthenticationSchemeOptions, GuestTokenAuthenticationHandler>(GuestTokenDefaults.AuthenticationScheme, _ => {})`. Mirror Identity's `AddIdentityAuth` composition.
- **Keep `AddReflektAuthorization()`** — its cookie-scheme policies resolve against **Identity's** cookie scheme (registered in the Host). The ~25 cookie-gated endpoints stay mapped and authenticate via Identity's cookie. **Known RA1 limitation:** they read Identity's `NameIdentifier`, not a Reflekt `User.Id`, so `ScrumMaster`/owner semantics are not yet correct — that is RA2 (`Participant`→`Person`), same spirit as the dead Google-login path. Do not fix here.
- **Unmap** only the Google/cookie *login* endpoints: `Features/Auth/{Login,Logout,Me}`, `Features/Participants/JoinSprintGoogle` — `// RA1: unmapped pending Identity unification (RA2, ADR-016)`.
- Confirm the global Identity `ActiveUserPreProcessor` does not hard-reject guest-scheme endpoints; if it does, scoping it is an **RA2 escalation** — document, don't fix.

**Accept:** one `AddAuthentication` (Identity's) + guest scheme; `AddReflektAuthorization` retained; four login endpoints unmapped; app boots; a guest joins a retro; a cookie-gated endpoint authenticates via Identity's cookie. **Confidence: medium** — the pre-processor/guest interaction is the spot to verify at boot.

## Step 8 — Host composition + `ReflektDbContext` wiring

In `src/Host/Program.cs` (Add/Init/Use): `builder.Services.AddReflektModule(builder.Configuration)`; add `typeof(Reflekt.API.DependencyInjection).Assembly` to the FastEndpoints `o.Assemblies`; `app.Migrate<ReflektDbContext>()` after Fokus; call the Reflekt seed; `app.MapReflektEndpoints()`. Follow `service-registration` + `persistence-patterns`.

- **Connection string:** add `ConnectionStrings:ReflektDatabase` → `data/reflekt.db` (mirror Fokus `Database`/Identity `IdentityDatabase`); point `ReflektDbContext` at it.
- **F5 — seeding:** `DbInitializer` is `internal`; expose a **public** seed extension (e.g. `SeedReflektDataAsync`) the Host can call, or add `InternalsVisibleTo` Host. Mirror Fokus's `SeedFokusDataAsync`.
- **Design-time factory:** add a `DesignTimeDbContextFactory` for `ReflektDbContext` in `Reflekt.Persistence` (Persistence-as-startup per `project-rules.md`; Reflekt's old `Program.cs` startup is gone).

**Accept:** Host registers Reflekt in all three blocks; public seed callable; Identity/Fokus registration untouched.

## Step 9 — Migration + green build

After the base swap (Step 2), run `dotnet ef migrations add ...` (Persistence-as-startup) and commit **whatever delta EF actually generates** — likely none (Reflekt's `EntityConfiguration<T>` does not map the target's audit props; `AuditedEntityConfiguration` is unused). Do **not** hand-author an audit-columns migration. Follow `persistence-patterns`.

**Accept (overall RA1):**
- `dotnet build SprintRituals.slnx` → green (0 errors).
- App boots; `data/reflekt.db` created with all Reflekt tables.
- Identity + Fokus endpoints + `MapIdentityGrpcServices` + Fokus's `/hubs/sprint` unaffected (spot-check one Fokus endpoint + the Fokus hub).
- `MapReflektEndpoints` mapped; `/hubs/retro` reachable; a guest joins a retro and a card broadcast round-trips.
- Four Google/cookie login endpoints unmapped with the RA2 marker; legacy `Send*Async` zero in Reflekt.

---

## Risks & out-of-scope

- **Auth semantics deferred (Step 7).** Cookie-gated endpoints authenticate via Identity's cookie but read Identity ids, not Reflekt `User` ids — functionally "authenticated but RA2-incomplete." Acceptable for a green-build checkpoint; full correctness is RA2.
- **`ActiveUserPreProcessor` × guest endpoints** — if it hard-requires an Identity cookie, scoping it is RA2, not RA1.
- **Hub route changed to `/hubs/retro`** — the Reflekt Vue client still points at `/hubs/sprint`; realigned in RA5 (frontend not served by the Host yet).
- **Audit props unmapped** — target `AggregateRoot` audit columns stay unmapped under Reflekt's plain configs; no behavior, no migration. Rich-aggregate/audit alignment is a later concern.
- **Reflekt unit tests** (`Reflekt.UnitTests`, `InternalsVisibleTo`) not brought in at RA1 — later decision.
- **Out of scope (do not touch):** `Sprint`→`RetroSession`, `User` removal, `Participant`→`Person`, Fokus snapshot/context panel, frontend merge.
