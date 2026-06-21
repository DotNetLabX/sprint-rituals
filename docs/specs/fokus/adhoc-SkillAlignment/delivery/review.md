# SkillAlignment — Review

## Reviewed By
`reviewer` (Sonnet agent, Step 2 code review)

## Verdict: APPROVE

No CRITICAL or HIGH issues. Two MEDIUM issues — one design concern on method hiding, one skill gap. All plan-authorized deviations are justified and documented.

---

## Pre-commitment Predictions

| Prediction | Actual |
|-----------|--------|
| `IDomainEvent` may not implement `INotification` (FastEndpoints-only) | Confirmed — plan explicitly authorizes this |
| `DefaultDateSql` missing from `AuditedEntityConfiguration` | Confirmed — plan-authorized, valid reasoning |
| `ApplicationDbContext` cache helpers removed | Confirmed — plan-authorized, valid reasoning |
| `UpsertAsync` uses `new` not `override` — hiding risk | Confirmed — MEDIUM finding |
| `GlobalExceptionMiddleware` might match `HttpException` as catch-all | Not found — implementation correctly matches each subclass individually |

---

## Findings

### [MEDIUM] `UpsertAsync` method hiding is fragile

**File:** `src/Services/Fokus/Fokus.Persistence/Repositories/SprintRepository.cs:19`, `TicketRepository.cs:33`, `DeveloperRepository.cs:16`

**Issue:** All three domain repositories use `public new async Task UpsertAsync(...)` to shadow the base class method. The base `RepositoryBase.UpsertAsync` is not `virtual`, so C# requires `new` to suppress CS0108 — that part is correct. However, if any caller holds a `RepositoryBase<FokusDbContext, Sprint, int>` reference (e.g., a future test double or a generic helper), the base implementation will be dispatched instead of the entity-specific field-copy logic. The base uses `CurrentValues.SetValues(entity)`, which shallow-copies scalar properties but does NOT handle navigation properties or the deliberate field-by-field copy logic in the domain repos.

**Fix:** Make `RepositoryBase.UpsertAsync` virtual so domain repositories can `override` it. This turns a silent dispatch ambiguity into a compiler-enforced contract.

```csharp
// In RepositoryBase<TContext, TEntity, TKey>:
public virtual async Task UpsertAsync(TEntity entity, CancellationToken ct = default) { ... }

// In domain repos:
public override async Task UpsertAsync(...) { ... }
```

**Self-audit confidence:** HIGH. The current code is not a bug today (no callers hold base references), but it is a design smell that will become a real bug the first time a generic method or test accepts `RepositoryBase<>`.

---

### [MEDIUM] `SeedFromJsonFile` not implemented in `EntityConfiguration` base

**File:** `src/BuildingBlocks/Blocks.EntityFrameworkCore/EntityConfigurations/EntityConfiguration.cs`

**Issue:** The `persistence-patterns` skill states: "HasKey(e => e.Id) + auto-calls `builder.SeedFromJsonFile()`". The `EntityConfiguration.Configure()` method sets up the key and `ValueGeneratedOnAdd/Never` but does not call `SeedFromJsonFile`. The extension method `SeedFromJsonFile` does not exist anywhere in the codebase. The plan's step 4 says "Follow `persistence-patterns` skill" without noting this omission as a deviation.

This is not a Fokus v1 regression — no seed JSON files are used today. But it means the BuildingBlock is incomplete relative to the skill contract, and any future developer following the skill will find the method missing.

**Fix:** Either implement `EntityTypeBuilderExtensions.SeedFromJsonFile<T>()` in `Blocks.EntityFrameworkCore`, or document in `implementation.md` that this extension is deferred and why. The deviation was not listed under "Deviations from Plan."

**Self-audit confidence:** MEDIUM. This is a skill completeness gap rather than a functional bug. No Fokus behavior is affected in v1. Downgraded from HIGH because the plan does not explicitly call out `SeedFromJsonFile` as a required output of step 4, and Fokus uses no seed JSON files.

---

### [LOW] `issue.Key` (Jira DTO) passed to `ReplaceTransitionsAsync` in sync endpoints

**File:** `src/Services/Fokus/Fokus.API/Features/Sync/SyncSprintsEndpoint.cs:111`, `SyncBacklogEndpoint.cs:82`

**Issue:** The plan says to update all `Key`/`TicketKey`/`AccountId` references in endpoints. Both sync endpoints call `ticketRepository.ReplaceTransitionsAsync(issue.Key, ...)` where `issue` is a `JiraIssue` DTO. The DTO property `Key` is the Jira issue key string — semantically identical to `Ticket.Id`. This is NOT a logic error; the value is correct. However, `issue.Key` is a Jira DTO property, not a domain entity property, so it was never subject to the rename. The plan instruction to update endpoint references applied to domain entity property accesses, not Jira DTO accesses. No fix required — noted for completeness.

---

### [LOW] `NU1903` vulnerability warning on `Microsoft.Build.Tasks.Core` 17.7.2

**File:** `src/Services/Fokus/Fokus.API/Fokus.API.csproj`

**Issue:** Build produces a known high severity vulnerability warning for `Microsoft.Build.Tasks.Core` 17.7.2. This is a pre-existing transitive dependency, not introduced by this refactoring. Noted for tracking but not a blocker for this feature.

---

## Positive Observations

- **Plan conformance is excellent.** All 12 steps are implemented. Every file the plan specified was created or modified. Entity renames cascade correctly through domain, persistence, configurations, repositories, mapper, and migration.
- **Build: 0 errors.** The 6 new BuildingBlocks packages, Fokus.Domain, Fokus.Persistence, and Fokus.API all compile cleanly together.
- **`GlobalExceptionMiddleware` correctly matches subclasses individually** (not a generic `HttpException` catch-all), matching the skill specification precisely.
- **`DispatchDomainEventsInterceptor` implementation is clean.** It correctly collects events before clearing (prevents race between collection and clear), then publishes. Pattern is correct.
- **`IDomainEvent` extends `IEvent` only.** The plan explicitly authorized FastEndpoints-only for Fokus — this is the right call for a single-framework service.
- **`AuditedEntityConfiguration<TEntity>` and `AuditedEntityConfiguration<TEntity, TKey>` dual variants** are a clean solution for the int-PK / string-PK split. Both `SprintConfiguration` (int PK) and `TicketConfiguration` (string PK) are correctly typed.
- **Deviation documentation is thorough.** The three plan deviations (`DefaultDateSql`, cache helpers, implementation order) are all documented with clear technical rationale in `implementation.md`.
- **Migration is fresh and correct.** Column renames (`Ticket.Id`, `Developer.Id`, `SprintMembership.TicketId`, `StatusTransition.TicketId`) and new audit columns (`CreatedById`, `CreatedOn`, `LastModifiedById`, `LastModifiedOn`) on Sprint and Ticket tables are all present. Pre-production drop-and-recreate is appropriate.
- **`JiraApiException` correctly kept separate.** The decision not to extend `HttpException` is sound — it uses `System.Net.HttpStatusCode` (not int), carries `JiraMessage`, and has custom per-status endpoint handling. Extending would lose type fidelity for no gain.
- **`DomainEventPublisher` registers as scoped** in DI. This matches `DispatchDomainEventsInterceptor` which is also scoped and depends on it. Lifetime chain is consistent.
- **Service CLAUDE.md is complete.** All axes filled — endpoint framework, persistence, event dispatch, auth status, natural key pattern, computed metrics note, AppSettings singleton behavior.

---

## Gaps

- **`SeedFromJsonFile` extension is absent** (see MEDIUM finding above). The persistence-patterns skill's seed-from-JSON infrastructure is deferred but not flagged as such.
- **No `IRepository` interface (Tier 1)** — this is correct per project guardrail ("no repository interfaces"). The skill describes it; the guardrail overrides it. Not a gap for Fokus.
- **Concurrency token** (`HasConcurrencyToken`) is available on `AuditedEntityConfiguration` but Sprint and Ticket configurations do not enable it. Correct for v1 — no concurrent edit scenarios. Worth knowing this lever exists when optimistic concurrency is needed.
- **`AppSettingsRepository.SaveAsync`** calls `db.Entry(existing).CurrentValues.SetValues(settings)` and then separately copies the owned entity navigation properties (`HealthThresholds`, `HealthWeights`, `DoneStatuses`, `WorkflowStages`). This is pre-existing pattern, not introduced by this refactoring. But the manual copy is fragile — adding a new AppSettings property won't automatically copy. Out of scope for this review.

---

## Open Questions

- Should `SeedFromJsonFile` be implemented now in `Blocks.EntityFrameworkCore` to complete the skill contract, or is it explicitly deferred to a later BuildingBlocks iteration? This is an architect-level scope decision, not a developer fix.

---

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Build | pass | `dotnet build src/Fokus.slnx` | 0 errors, 2 warnings (pre-existing NU1903 on transitive dep) |
| Entity renames in domain | pass | Read Sprint.cs, Ticket.cs, Developer.cs, StatusTransition.cs, SprintMembership.cs | All extend correct base classes; `Id` property inherited; `TicketId` renamed correctly |
| Cascade in configs | pass | Read all 5 Fokus configuration files | `HasKey(e => e.Id)` used throughout; `TicketId` FK in SprintMembership and StatusTransition |
| Cascade in mapper | pass | Read JiraMapper.cs | `Id = dto.Key`, `Id = assignee.AccountId`, `TicketId = dto.Key` — all correct |
| Migration columns | pass | Read 20260506192926_InitialCreate.cs | Developers.Id, Tickets.Id, SprintMembership.TicketId, audit columns on Sprint+Ticket all present |
| Interceptor wiring | pass | Read DependencyInjection.cs | `IDomainEventPublisher` + `ISaveChangesInterceptor` registered scoped; interceptor added to DbContextOptions |
| Middleware registration | pass | Read Program.cs | `app.UseMiddleware<GlobalExceptionMiddleware>()` before `UseStaticFiles` and `UseFokusMiddleware` |
| Skill: error-handling | pass | Read Guard.cs, GuardExtensions.cs, HttpException hierarchy, GlobalExceptionMiddleware.cs | All types present; middleware maps subclasses individually as required |
| Skill: domain-patterns | pass | Read all Blocks.Domain files | Entity, AggregateRoot, ValueObject, IAggregateRoot, IDomainEvent, IDomainEventPublisher all correct |
| Skill: persistence-patterns | partial | Read RepositoryBase, EntityConfiguration, AuditedEntityConfiguration | Core patterns correct; SeedFromJsonFile absent (MEDIUM gap) |
| Guardrail: no repo interfaces | pass | Grepped for IRepository | No IRepository interface present |
| Guardrail: no service classes | pass | No `*Service.cs` files in Features/ | Clean vertical slices |
