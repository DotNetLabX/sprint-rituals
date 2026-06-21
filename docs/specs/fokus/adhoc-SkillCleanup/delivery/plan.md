# Skill Cleanup — Remove Reflekt-Specific References

**Feature Spec:** None

## Context

Skills in `.claude/skills/` were extracted from the Reflekt codebase and contain app-specific references (entity names, package names, service names, file paths, port numbers) that make them non-portable. 45 of 57 skill files contain leakage. This cleanup generalizes all skills so they work for any project using this skill library.

No feature spec required — this is infrastructure maintenance, not a user-facing feature.

## Scope

**In scope:** All files in `.claude/skills/`. Replace app-specific references with generic patterns.

**Out of scope:** Creating new skills (proposal's Skill 1 `create-architecture-doc` and Skill 2 `create-implementation-plan`). That's Phase 2.

## Implementation Steps

### Step 1: Domain events — two-variant pattern

**Files:**
- `.claude/skills/domain-patterns/SKILL.md` (lines 78-89)
- `.claude/skills/create-aggregate/workflows/DomainEvent.md` (full file)

Present two first-class domain event variants (not "full" vs "simple" — both are equal):

**Variant A — With action tracking:**
```csharp
public abstract record DomainEvent<TAction>(TAction Action) : IDomainEvent;
public record OrderApproved(Order Order, IAction Action) : DomainEvent<IAction>(Action);
```
Note: `TAction` is a per-service action marker interface (e.g., `IOrderAction`). Services that track which action triggered an event use this variant.

**Variant B — Aggregate reference only:**
```csharp
public sealed record OrderCreated(Order Order) : IDomainEvent;
```
Services where events are simple notifications use this variant.

Replace all instances of:
- `IArticleAction` → `IAction` (generic) or `TAction`
- `ArticleApproved(Article Article, IArticleAction Action)` → `OrderApproved(Order Order, IAction Action)`
- `JournalCreated(Journal Journal)` → `OrderCreated(Order Order)` (and stop labeling it "Journals variant")
- `DomainEvent<TAction>(TAction Action) where TAction : IArticleAction` → `DomainEvent<TAction>(TAction Action)` (remove the constraint)

In `DomainEvent.md`:
- Section "With Action (services using IArticleAction)" → "Variant A: With Action Tracking"
- Section for Journals → "Variant B: Aggregate Reference Only"
- `IArticleAction action` in raising events template → `IAction action`
- MediatR/FastEndpoints handler variant headings: drop service names, use "MediatR Variant" / "FastEndpoints Variant"
- Location rules for Review / Submission / Production → generic: "Place in `{Svc}.Domain/Events/` or co-located with the aggregate in `{Svc}.Domain/{AggregateName}/Events/`"

### Step 2: Aggregate behavior templates

**Files:**
- `.claude/skills/create-aggregate/workflows/AggregateEfCore.md`

Replace:
- `IArticleAction action` → `TAction action` in behavior method template
- `Typesetter`, `ArticleStateMachineFactory` → generic names (`assignee`, `stateMachineFactory`)
- `TypesetterAssigned(typesetter.Id, typesetter.UserId!.Value, action)` → `AssigneeChanged(this, action)`
- "Used by: Auth, Submission, Review, Production" → "Used by: EF Core services"
- Reference path `src/Services/Submission/Submission.Domain/Entities/Article.cs` → `src/Services/{Svc}/{Svc}.Domain/Entities/{Aggregate}.cs`
- Reference path `src/Services/Submission/Submission.Domain/Behaviors/Article.cs` → `src/Services/{Svc}/{Svc}.Domain/Behaviors/{Aggregate}.cs`
- "Review and Production follow this; Submission may vary" → "Group by aggregate when service has multiple aggregates; flat layout acceptable for single-aggregate services"
- "Review, Production, and Auth" → "Services with multiple aggregates"
- "Submission uses a flat layout" → "Single-aggregate services may use flat layout"
- Registration: "auto-discovered via AddDerivedTypesOf in Submission/Review" → "Auto via assembly scan (`AddDerivedTypesOf(typeof(Repository<>))`) or manual `AddScoped<{Entity}Repository>()` per repository"

Also in `AggregateRedis.md`:
- "Used by: Journals" → "Used by: Redis-backed services"
- `src/Services/Journals/Journals.Domain/Journals/Journal.cs` → `src/Services/{Svc}/{Svc}.Domain/{Aggregate}/{Aggregate}.cs`
- Replace `Journal`/`Section` entity names → `{Aggregate}`/`{ChildEntity}` or generic names

### Step 3: State machine and event dispatch sections

**Files:**
- `.claude/skills/domain-patterns/SKILL.md` (lines 94-124)

State machine (lines 114-124):
- `ArticleStateMachineFactory` → `{Aggregate}StateMachineFactory`
- `ArticleStageTransition` → `{Aggregate}StageTransition`
- `ArticleActionType` → `{Aggregate}ActionType`
- `ArticleStage` → `{Aggregate}Stage`
- `src/Services/Submission/Submission.Application/StateMachines/` → `src/Services/{Svc}/{Svc}.Application/StateMachines/`
- Remove "replace ArticleStage/ArticleActionType with any entity states/transitions" line since names are already generic

Event dispatch (lines 94-112):
- "Used by Submission, Review" → "Standard (default for most services)"
- "Used by Production" → "Transactional (when event handlers write to the same DB in the same request)"
- "Production + ArticleTimeline sharing a transaction" → "when domain event handlers write to the same DB in the same request (e.g., aggregate + timeline sharing a DbContext)"
- Publisher table: "Submission, Review, Production" → "MediatR services"
- Publisher table: "Auth, Journals" → "FastEndpoints-native services"
- "Production uses FastEndpoints for endpoints but MediatR for event dispatch" → "Some services mix endpoint frameworks — FastEndpoints for HTTP but MediatR for event dispatch (when INotificationHandler pipeline is needed)"

### Step 4: Value object and entity examples

**Files:**
- `.claude/skills/domain-patterns/SKILL.md` (lines 29-62)
- `.claude/skills/create-aggregate/workflows/ValueObject.md`

In `domain-patterns/SKILL.md`:
- "Real examples: `Auth.Domain/Persons/ValueObjects/EmailAddress.cs`, `Production.Domain/Assets/ValueObjects/AssetName.cs`" → remove the "Real examples" line. The `EmailAddress` code block above it is already a sufficient generic example.
- Per-BC duplication note: remove "EmailAddress in Auth, Review, Production" → keep the principle ("Per-bounded-context duplication is intentional") without naming specific services
- `List<Asset> _assets` / `IReadOnlyList<Asset> Assets` → `List<{ChildEntity}> _{childEntities}` / `IReadOnlyList<{ChildEntity}> {ChildEntities}` (or use `LineItem` as a concrete example)
- Behavior method convention: remove "Review and Submission vary in ordering" → "Prefer action last when writing new code"
- Replace entire Production code example (Typesetter) with a generic one

In `ValueObject.md`:
- `ArticleTitle` → `OrderTitle` or `{ValueObjectName}` in the table
- "Used in Review's EmailAddress" → remove the service reference

### Step 5: Package names, solution file, and global usings

**Files:** All files in `create-service/workflows/`, `create-module/workflows/`, `create-service-claude-md/`

Replace throughout:
- `Articles.Abstractions` → `{ProjectName}.Abstractions`
- `Articles.Security` → `{ProjectName}.Security`
- `Articles.Integration.Contracts` → `{ProjectName}.Integration.Contracts`
- `Articles.IntegrationEvents.Contracts` → `{ProjectName}.Integration.Contracts`
- `Articles.Grpc.Contracts` → `{ProjectName}.Grpc.Contracts`
- `Articles.sln` → the solution file (use phrasing like "the solution file at repo root" or `{SolutionFile}` placeholder)
- `global using Articles.Abstractions;` → `global using {ProjectName}.Abstractions;`
- `global using Articles.Security;` → `global using {ProjectName}.Security;`
- Every `dotnet sln src/Articles.sln add` → `dotnet sln {SolutionFile} add`
- Every `dotnet build src/Articles.sln` → `dotnet build {SolutionFile}`

Specific files with dense occurrences:
- `ScaffoldCsprojFiles.md` — lines 25, 97, 99, 148, 157
- `ScaffoldDomain.md` — lines 41, 52
- `ScaffoldDomainProject.md` — line 13
- `ScaffoldApiProject.md` — line 21
- `ScaffoldInfrastructure.md` (both service and module) — all `dotnet sln` commands
- `create-service/SKILL.md` — line 34
- `create-module/SKILL.md` — line 33
- `create-service-claude-md/SKILL.md` — line 36

### Step 6: Reference paths — Real examples sections

**Files:** All workflow files with `**Reference:**` or `**File:**` lines (25+ files — see audit for full list)

Replace all `src/Services/{ReflektService}/{ReflektService}.{Layer}/...` paths with `src/Services/{Svc}/{Svc}.{Layer}/...`. Keep the file name structure since that's the pattern being taught.

Mapping:
- `src/Services/Submission/Submission.*` → `src/Services/{Svc}/{Svc}.*`
- `src/Services/Review/Review.*` → `src/Services/{Svc}/{Svc}.*`
- `src/Services/Production/Production.*` → `src/Services/{Svc}/{Svc}.*`
- `src/Services/Auth/Auth.*` → `src/Services/{Svc}/{Svc}.*`
- `src/Services/Journals/Journals.*` → `src/Services/{Svc}/{Svc}.*`
- `src/Services/ArticleHub/ArticleHub.*` → `src/Services/{Svc}/{Svc}.*`
- `src/Services/Reflekt/Reflekt.*` → `src/Services/{Svc}/{Svc}.*`
- `src/Modules/ArticleTimeline/ArticleTimeline.*` → `src/Modules/{Module}/{Module}.*`
- `src/BuildingBlocks/Articles.*/...` → `src/BuildingBlocks/{ProjectName}.*/...`

**Do not** break the file name patterns — e.g., `{FeatureName}Endpoint.cs` stays; only the service/project prefix changes.

### Step 7: "Used by" and variant table service names

**Files:** `create-feature/SKILL.md`, `create-feature/workflows/*.md`, `create-aggregate/workflows/*.md`, `persistence-patterns/SKILL.md`, `cqrs-patterns/SKILL.md`, `system-design/SKILL.md`, `add-integration-event/workflows/*.md`

Replace service-name branching with description-based branching:
- "Used by: Auth, Journals, Production" → "Used by: FastEndpoints services (check service CLAUDE.md)"
- "Used by: Review, ArticleHub" → "Used by: Carter + MediatR services"
- "Used by: Submission" → "Used by: Minimal API + MediatR services"
- "FastEndpoints Variant (Auth, Journals)" → "FastEndpoints Variant"
- "MediatR Variant (Submission, Review, Production)" → "MediatR Variant"
- "Auth-style services" → "FastEndpoints services"
- "ArticleHub pattern" → "read-model services" or "Carter read-model pattern"

Add to every variant section: "Check the service's CLAUDE.md for which variant to use."

Also in persistence-patterns:
- "Auto: Submission, Review" / "Manual: Production, ArticleHub, Auth" → "Auto (assembly scan)" / "Manual (per-repository AddScoped)"
- "SQL Server: Auth, Submission, Review, Production" / "PostgreSQL: ArticleHub" → "SQL Server (default)" / "PostgreSQL (opt-in per service)" / "SQLite (lightweight single-node)"
- "OwnsOne (Auth)" / "ComplexProperty (Review)" → describe by pattern, not by service
- "Submission, Review, Production" / "Auth" / "Journals" in seed variant table → describe by mechanism

### Step 8: Existing registries — events, gRPC, ports

**Files:**
- `.claude/skills/add-integration-event/SKILL.md` (lines 23-31)
- `.claude/skills/create-grpc-contract/SKILL.md` (lines 23-26)
- `.claude/skills/create-service-claude-md/workflows/CaptureAxes.md` (line 45)

Integration events table → empty with column headers and a note:
```
| Event | Publisher | Consumers | Contract Location |
|-------|-----------|-----------|-------------------|
| (check src/BuildingBlocks/{ProjectName}.Integration.Contracts/) | | | |
```

gRPC contracts table → empty with headers and a note:
```
| Contract | Service | Port | Location |
|----------|---------|------|----------|
| (check src/BuildingBlocks/{ProjectName}.Grpc.Contracts/) | | | |
```

Port registry → empty with note: "Check the root CLAUDE.md Port Convention section for assigned service indices and derived ports."

Also in `CaptureAxes.md`:
- Remove the specific port pairs "4401/4451 (Auth), 4402/4452 (Journals)..." 
- Replace with: "Check root CLAUDE.md for the current service index table. Next available index = last assigned + 1."
- Replace `IPersonService (Auth)`, `IJournalService (Journals)` → generic: "e.g., `I{Entity}Service` in `{ProjectName}.Grpc.Contracts/{ServiceName}/`"
- Replace "from `Articles.Integration.Contracts`" → "from `{ProjectName}.Integration.Contracts`"

### Step 9: Entity examples in persistence and error handling skills

**Files:**
- `.claude/skills/persistence-patterns/SKILL.md`
- `.claude/skills/redis-patterns/SKILL.md`
- `.claude/skills/error-handling/SKILL.md`
- `.claude/skills/authorization-patterns/SKILL.md`

In `persistence-patterns/SKILL.md`:
- `SubmissionDbContext` → `{Svc}DbContext`
- `ArticleRepository(SubmissionDbContext)` → `{Entity}Repository({Svc}DbContext)`
- `.Include(e => e.Actors).ThenInclude(e => e.Person).Include(e => e.Assets)` → `.Include(e => e.{Navigation}).ThenInclude(e => e.{ChildNavigation})`
- `GetFullArticleByIdAsync` → `GetFullByIdAsync` or `Get{Entity}ByIdAsync`
- `ArticleEntityConfiguration : AuditedEntityConfiguration<Article>` → `{Entity}EntityConfiguration : AuditedEntityConfiguration<{Entity}>`
- Seed data: `SeedFromJsonFile<Person>()`, `SeedFromJsonFile<Journal>()`, `SeedFromJsonFile<Article>()` → `SeedFromJsonFile<{Entity}>()`
- `app.Migrate<SubmissionDbContext>()` → `app.Migrate<{Svc}DbContext>()`
- "IdentityDbContext — Auth" → "IdentityDbContext — services using ASP.NET Identity"
- `CreateArticleCommandValidator` → `{FeatureName}CommandValidator`

In `redis-patterns/SKILL.md`:
- Replace all `Journal`/`Section`/`JournalDbContext` → `{Entity}`/`{ChildEntity}`/`{Svc}DbContext`
- Replace all `src/Services/Journals/...` paths → `src/Services/{Svc}/...`
- Replace `IRedisCollection<Journal>` → `IRedisCollection<{Entity}>`
- Replace `services.AddSingleton<JournalDbContext>()` → `services.AddSingleton<{Svc}DbContext>()`

In `error-handling/SKILL.md`:
- `command.ArticleId` → `command.{Entity}Id`
- "Auth handlers, middleware" → "authentication middleware"

In `authorization-patterns/SKILL.md`:
- `ArticleAccessAuthorizationHandler` → `{Entity}AccessAuthorizationHandler`
- `ArticleRoleRequirement` → `{Entity}RoleRequirement`
- `IArticleAccessChecker` → `I{Entity}AccessChecker`
- All `src/BuildingBlocks/Articles.Security/...` and `Articles.Abstractions/...` paths → `src/BuildingBlocks/{ProjectName}.Security/...` and `{ProjectName}.Abstractions/...`

### Step 10: BaseEndpoint, BaseValidator, and framework-specific variants

**Files:**
- `.claude/skills/create-feature/workflows/EndpointFastEndpoints.md`
- `.claude/skills/create-feature/workflows/Validator.md`
- `.claude/skills/cqrs-patterns/SKILL.md`

In `EndpointFastEndpoints.md`:
- "Used by: Auth, Journals, Production" → "Used by: FastEndpoints services"
- Reference path `src/Services/Auth/Auth.API/...` → `src/Services/{Svc}/{Svc}.API/...`
- "Production Variant: BaseEndpoint" heading → "Optional: Custom Base Endpoint"
- Description: "If the service defines a custom base endpoint (check service CLAUDE.md), extend it instead of `Endpoint<TReq, TRes>`"
- Remove `src/Services/Production/Production.API/Features/_Shared/BaseEndpoint.cs` path
- "Journals: handler class name sometimes ends with QueryHandler..." → remove (legacy naming note for a service that doesn't exist here)
- "Production validators extend BaseValidator<T>" → "If the service defines a custom base validator (check service CLAUDE.md), extend it instead of `Validator<T>`"

In `Validator.md`:
- "Production variant: extend BaseValidator<T>" → "Optional: if service defines a custom base validator, extend it"

In `cqrs-patterns/SKILL.md`:
- "Production custom / BaseValidator<T>" → "Custom base (opt-in per service)"
- `src/Services/Submission/Submission.Application/DependencyInjection.cs` → `src/Services/{Svc}/{Svc}.Application/DependencyInjection.cs`
- `CreateArticleCommandValidator` → `{FeatureName}CommandValidator`

### Step 11: Remaining service-specific references

**Files:**
- `.claude/skills/create-domain-event-handler/workflows/Handler.md`
- `.claude/skills/create-feature/workflows/EndpointCarter.md`
- `.claude/skills/create-feature/workflows/EndpointMinimalApi.md`
- `.claude/skills/create-feature/workflows/Handler.md`
- `.claude/skills/create-feature/workflows/Mappings.md`
- `.claude/skills/create-grpc-contract/workflows/Contract.md`
- `.claude/skills/create-grpc-contract/workflows/Server.md`
- `.claude/skills/create-grpc-contract/workflows/Client.md`
- `.claude/skills/add-integration-event/workflows/EventContract.md`
- `.claude/skills/add-integration-event/workflows/Publisher.md`
- `.claude/skills/add-integration-event/workflows/Consumer.md`
- `.claude/skills/create-module-claude-md/SKILL.md` and `workflows/*.md`
- `.claude/skills/create-module/workflows/ScaffoldDomainApplication.md`
- `.claude/skills/create-service/workflows/ReadClaudeMd.md`
- `.claude/skills/create-service/workflows/ScaffoldFolders.md`

Apply the same rules from steps 5-7:
- All service names → description-based or `{Svc}` placeholder
- All file paths → `src/Services/{Svc}/...` or `src/Modules/{Module}/...`
- All package names → `{ProjectName}.*`
- "Reflekt" in any heading or text → remove
- Entity names (`Article`, `Journal`, `Person`, `Asset`, etc.) in code examples → generic equivalents
- `Article.FromSubmission()` → `{Entity}.Create(...)` or `{Entity}.From{Source}()`
- `ArticleAcceptedForProductionConsumer` → `{EventName}Consumer`
- `ArticleTimeline` → `{Module}`
- Service CLAUDE.md exemplar paths (`src/Services/Review/CLAUDE.md`, etc.) → `src/Services/{Svc}/CLAUDE.md`

In `Consumer.md`:
- `await GetOrCreateJournal(dto)` → `await GetOrCreate{Entity}(dto)`
- Location table: service-specific folder paths → generic `{Svc}.API/{Domain}/Consumers/`

In `EventContract.md`:
- `BuildingBlocks/Articles.Integration.Contracts/{Domain}/` → `BuildingBlocks/{ProjectName}.Integration.Contracts/{Domain}/`
- `ArticleApprovedForReviewEvent` → `{EventName}Event`

In `create-module-claude-md`:
- `ArticleTimeline` → `{Module}` (everywhere)
- "Current hosts: Production, Review" → "Current hosts: (check which services host modules)"
- `ArticleTimeline has no API today...` → rephrase generically

### Step 12: Verify consistency

After all changes, run these checks (all must return zero matches):
```
grep -r "IArticleAction" .claude/skills/
grep -r "ArticleApproved\|ArticleAccepted\|ArticlePublished\|ArticleReviewed" .claude/skills/
grep -r "src/Services/Submission\|src/Services/Review\|src/Services/Production\|src/Services/Auth\|src/Services/Journals\|src/Services/ArticleHub\|src/Services/Reflekt" .claude/skills/
grep -r "Articles\.sln\|Articles\.Abstractions\|Articles\.Security\|Articles\.Integration\|Articles\.Grpc" .claude/skills/
grep -r "Typesetter\|ArticleStateMachine\|ArticleStage\|ArticleActionType" .claude/skills/
grep -r "JournalDbContext\|SubmissionDbContext\|ReviewDbContext\|ProductionDbContext" .claude/skills/
```

Allowed exceptions:
- The word "Article" in a generic context like "an article or blog post" (natural language, not a domain reference)
- `{Entity}` placeholders that happen to be near the word

## Testing Strategy

- Grep verification (step 12) catches any missed references
- Read each modified SKILL.md and confirm code templates are syntactically valid
- Confirm variant tables still make sense when read without Reflekt context

## Open Questions

None — the user confirmed the two-variant approach for domain events (with action / without action).
