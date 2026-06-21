# SkillCleanup -- Implementation

## Files Modified

### Step 1: Domain events -- two-variant pattern
- `.claude/skills/domain-patterns/SKILL.md` -- Replaced `IArticleAction` constraint with generic `TAction`, replaced `ArticleApproved`/`JournalCreated` examples with `OrderApproved`/`OrderCreated`, presented two first-class variants (A: with action tracking, B: aggregate reference only), removed "Journals uses" labeling
- `.claude/skills/create-aggregate/workflows/DomainEvent.md` -- Full rewrite: renamed sections to "Variant A"/"Variant B", replaced `IArticleAction` with `IAction`, replaced service-name handler headings with "MediatR Variant"/"FastEndpoints Variant", genericized location rules

### Step 2: Aggregate behavior templates
- `.claude/skills/create-aggregate/workflows/AggregateEfCore.md` -- Replaced `IArticleAction action` with `TAction action`, replaced service-name references in "Used by", reference paths, folder structure descriptions, and registration line
- `.claude/skills/create-aggregate/workflows/AggregateRedis.md` -- Replaced "Used by: Journals" with "Used by: Redis-backed services", genericized reference path and child entity example

### Step 3: State machine and event dispatch sections
- `.claude/skills/domain-patterns/SKILL.md` -- (Already handled in Step 1) Event dispatch table, IDomainEventPublisher table, and state machine section all genericized. Changed "Auth `User`" to "Identity `User`"

### Step 4: Value object and entity examples
- `.claude/skills/domain-patterns/SKILL.md` -- (Already handled in Step 1) Removed "Real examples" line, cleaned per-BC duplication note, replaced `List<Asset>` with `List<LineItem>`, replaced Typesetter behavior example with generic `AssignTo` example
- `.claude/skills/create-aggregate/workflows/ValueObject.md` -- Replaced `ArticleTitle` with `{ValueObjectName}`, removed "Used in Review's" service reference

### Step 5: Package names, solution file, and global usings
- `.claude/skills/create-service/workflows/ScaffoldCsprojFiles.md` -- Replaced all `Articles.Abstractions`, `Articles.Security`, `Articles.Integration.Contracts`, `Articles.Grpc.Contracts` with `{ProjectName}.*` placeholders. Replaced service-specific reference paths with `{Svc}` placeholders
- `.claude/skills/create-module/workflows/ScaffoldDomain.md` -- Replaced `Articles.Abstractions` with `{ProjectName}.Abstractions`, replaced `ArticleTimeline` paths with `{Module}` placeholders
- `.claude/skills/create-service/workflows/ScaffoldDomainProject.md` -- Replaced `Articles.Abstractions` with `{ProjectName}.Abstractions`, genericized reference path
- `.claude/skills/create-service/workflows/ScaffoldApiProject.md` -- Replaced `Articles.Security` with `{ProjectName}.Security`, replaced all service-specific reference paths
- `.claude/skills/create-service/workflows/ScaffoldInfrastructure.md` -- Replaced `Articles.sln` with `{SolutionFile}`, replaced service-specific reference paths
- `.claude/skills/create-module/workflows/ScaffoldInfrastructure.md` -- Replaced `Articles.sln` with `{SolutionFile}`
- `.claude/skills/create-service/SKILL.md` -- Replaced `Articles.sln` with "the solution file"
- `.claude/skills/create-module/SKILL.md` -- Replaced `Articles.sln` with "the solution file"
- `.claude/skills/create-service-claude-md/SKILL.md` -- Replaced `Articles.sln` with "the solution file"
- `.claude/skills/create-service/workflows/ScaffoldPersistenceProject.md` -- Replaced service-specific reference paths
- `.claude/skills/create-service/workflows/ScaffoldApplicationProject.md` -- Replaced service-specific reference paths and `Articles.IntegrationEvents.Contracts`
- `.claude/skills/create-module/workflows/ScaffoldComponent.md` -- Replaced `Articles.sln` reference

### Step 6: Reference paths
- `.claude/skills/create-service-claude-md/workflows/WriteClaudeMd.md` -- Replaced 5 service-specific exemplar paths with description-based guidance
- `.claude/skills/create-module/workflows/ScaffoldDomainApplication.md` -- Replaced `ArticleTimeline` and service-specific reference paths
- `.claude/skills/create-domain-event-handler/workflows/Handler.md` -- Full rewrite: removed `Reflekt` namespace/paths, genericized handler patterns and service-name variant headings
- `.claude/skills/create-feature/workflows/EndpointCarter.md` -- Replaced "Used by" service names and reference path
- `.claude/skills/create-feature/workflows/EndpointMinimalApi.md` -- Replaced "Used by" service names, reference path, `CreateArticleEndpoint` example, and file upload `articles/{articleId}` example
- `.claude/skills/create-feature/workflows/Handler.md` -- Replaced "Used by" service names and reference path
- `.claude/skills/create-feature/workflows/Mappings.md` -- Replaced reference path and `Article`/`ArticleResponse` inline mapping examples
- `.claude/skills/add-integration-event/workflows/Publisher.md` -- Full rewrite: genericized reference paths, removed service names from variant headings
- `.claude/skills/add-integration-event/workflows/Consumer.md` -- Full rewrite: genericized all reference paths, entity examples, naming conventions, and location table
- `.claude/skills/add-integration-event/workflows/EventContract.md` -- Full rewrite: replaced `Articles.Integration.Contracts` paths and `ArticleApprovedForReview` example
- `.claude/skills/cqrs-patterns/SKILL.md` -- Replaced "Production custom" with "Custom base (opt-in per service)", replaced registration reference path and `CreateArticleCommandValidator`
- `.claude/skills/create-grpc-contract/workflows/Server.md` -- Replaced service-specific reference path
- `.claude/skills/create-grpc-contract/workflows/Contract.md` -- Replaced `Articles.Grpc.Contracts` reference path

### Step 7: "Used by" and variant table service names
- `.claude/skills/create-feature/workflows/EndpointFastEndpoints.md` -- Full rewrite: replaced "Used by: Auth, Journals, Production" with "Used by: FastEndpoints services", renamed "Production Variant: BaseEndpoint" to "Optional: Custom Base Endpoint", removed legacy naming notes
- `.claude/skills/create-feature/SKILL.md` -- Replaced service-name folder structure headings with framework names, replaced `Review.API`/`Submission.API` paths with `{Svc}.*`
- `.claude/skills/create-feature/workflows/Validator.md` -- Replaced "Production variant" with "Optional: if service defines a custom base validator"
- `.claude/skills/persistence-patterns/SKILL.md` -- Full rewrite: replaced all `SubmissionDbContext`/`ArticleRepository`/`Article` entity examples with generic placeholders, replaced service-name registration and seed variant tables with description-based tables
- `.claude/skills/redis-patterns/SKILL.md` -- Full rewrite: replaced all `Journal`/`Section`/`JournalDbContext`/`Journals.*` paths with generic placeholders

### Step 8: Existing registries
- `.claude/skills/add-integration-event/SKILL.md` -- Replaced events table with empty headers + note to check `{ProjectName}.Integration.Contracts`, replaced `ArticlePublished` argument example
- `.claude/skills/create-grpc-contract/SKILL.md` -- Replaced contracts table with empty headers + note to check `{ProjectName}.Grpc.Contracts`, replaced port convention with root CLAUDE.md reference, replaced `ArticleQueryService` argument example
- `.claude/skills/create-service-claude-md/workflows/CaptureAxes.md` -- Full rewrite: removed specific port pairs and service-name examples, replaced with root CLAUDE.md references and generic placeholders

### Step 9: Entity examples in persistence and error handling
- `.claude/skills/error-handling/SKILL.md` -- Replaced "Auth handlers, middleware" with "Authentication middleware", replaced `command.ArticleId` with `command.{Entity}Id`
- `.claude/skills/authorization-patterns/SKILL.md` -- Replaced `ArticleAccessAuthorizationHandler`/`ArticleRoleRequirement`/`IArticleAccessChecker` with `{Resource}*` placeholders, replaced `Articles.Security`/`Articles.Abstractions` paths with `{ProjectName}.*`

### Step 10: BaseEndpoint, BaseValidator, and framework-specific variants
- (Handled together with Steps 6-7 in EndpointFastEndpoints.md, Validator.md, cqrs-patterns/SKILL.md)

### Step 11: Remaining service-specific references
- `.claude/skills/system-design/SKILL.md` -- Replaced "Auth-style services", "Auth and Journals use this", "ArticleHub pattern", `Article.FromSubmission()` with generic equivalents
- `.claude/skills/create-aggregate/SKILL.md` -- Replaced "Auth `User`" with "Identity `User`", "Journals/Redis" with "Redis services", replaced `ArticleRevision` argument example
- `.claude/skills/create-service/workflows/ScaffoldFolders.md` -- Replaced service-name references in MediatR/FastEndpoints descriptions and read-model section heading
- `.claude/skills/create-service/workflows/ReadClaudeMd.md` -- Replaced `Articles.Grpc.Contracts` and `Articles.Integration.Contracts` with `{ProjectName}.*`
- `.claude/skills/create-module-claude-md/SKILL.md` -- Removed `ArticleTimeline` example
- `.claude/skills/create-module-claude-md/workflows/CaptureDomainAxes.md` -- Genericized the framework-host-compatibility warning, "Current hosts" axis description, and example axis table

## Key Decisions
- Rewrote 8 files entirely (DomainEvent.md, Handler.md, EndpointFastEndpoints.md, Publisher.md, Consumer.md, EventContract.md, persistence-patterns/SKILL.md, redis-patterns/SKILL.md, CaptureAxes.md, redis-patterns/SKILL.md) where the number of edits per file exceeded 5-6. This was more reliable than many small targeted edits.
- Used `{Svc}`, `{Entity}`, `{Module}`, `{ProjectName}`, `{SolutionFile}` placeholders consistently across all files.
- In code blocks that need to look like valid C#, used generic domain names (Order, LineItem, Assignee) instead of Reflekt-specific ones (Article, Asset, Typesetter).
- Replaced argument examples in SKILL.md files (e.g., `/create-aggregate ArticleRevision` -> `/create-aggregate OrderItem`) since these are user-facing suggestions.

## Deviations from Plan
- Steps 6-11 were executed together per-file rather than strictly sequentially, since many files had overlapping concerns from multiple steps. All plan items are accounted for.
- No deviations in substance -- all specified replacements were made.

## Verification (Step 12)
All 6 grep checks returned 0 matches:
1. `IArticleAction` -- 0 matches
2. `ArticleApproved|ArticleAccepted|ArticlePublished|ArticleReviewed` -- 0 matches
3. Service-specific paths (Submission/Review/Production/Auth/Journals/ArticleHub/Reflekt) -- 0 matches
4. `Articles.sln|Articles.Abstractions|Articles.Security|Articles.Integration|Articles.Grpc` -- 0 matches
5. `Typesetter|ArticleStateMachine|ArticleStage|ArticleActionType` -- 0 matches
6. `JournalDbContext|SubmissionDbContext|ReviewDbContext|ProductionDbContext` -- 0 matches

Additionally verified: `ArticleTimeline` -- 0 matches, `Article[A-Z]` -- 0 matches.
