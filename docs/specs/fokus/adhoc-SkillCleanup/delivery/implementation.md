# SkillCleanup — Implementation

## Files Modified

### Step 1: Domain events — two-variant pattern
- `.claude/skills/domain-patterns/SKILL.md` — replaced `IArticleAction` with generic `TAction`, replaced `ArticleApproved`/`JournalCreated` examples with `OrderApproved`/`OrderCreated`, presented two first-class variants (A: with action tracking, B: aggregate reference only)
- `.claude/skills/create-aggregate/workflows/DomainEvent.md` — full rewrite: renamed sections to "Variant A"/"Variant B", replaced `IArticleAction` with `IAction`, genericized location rules and handler variant headings

### Step 2: Aggregate behavior templates
- `.claude/skills/create-aggregate/workflows/AggregateEfCore.md` — replaced `IArticleAction action` with `TAction action`, genericized "Used by", reference paths, folder structure descriptions, and registration line
- `.claude/skills/create-aggregate/workflows/AggregateRedis.md` — replaced "Used by: Journals" with "Used by: Redis-backed services", genericized reference path and child entity example

### Step 3: State machine and event dispatch sections
- `.claude/skills/domain-patterns/SKILL.md` — (handled in Step 1) event dispatch table, IDomainEventPublisher table, and state machine section all genericized

### Step 4: Value object and entity examples
- `.claude/skills/domain-patterns/SKILL.md` — (handled in Step 1) removed "Real examples" line, replaced `List<Asset>` with `List<LineItem>`, replaced Typesetter behavior example with generic `AssignTo`
- `.claude/skills/create-aggregate/workflows/ValueObject.md` — replaced `ArticleTitle` with `{ValueObjectName}`, removed "Used in Review's" service reference

### Step 5: Package names, solution file, and global usings
- `.claude/skills/create-service/workflows/ScaffoldCsprojFiles.md` — replaced `Articles.*` package refs with `{ProjectName}.*`, replaced service-specific reference paths
- `.claude/skills/create-module/workflows/ScaffoldDomain.md` — replaced `Articles.Abstractions` with `{ProjectName}.Abstractions`, genericized `ArticleTimeline` paths
- `.claude/skills/create-service/workflows/ScaffoldDomainProject.md` — replaced `Articles.Abstractions`, genericized reference path
- `.claude/skills/create-service/workflows/ScaffoldApiProject.md` — replaced `Articles.Security`, genericized reference paths
- `.claude/skills/create-service/workflows/ScaffoldInfrastructure.md` — replaced `Articles.sln` with `{SolutionFile}`, genericized reference paths
- `.claude/skills/create-module/workflows/ScaffoldInfrastructure.md` — replaced `Articles.sln` with `{SolutionFile}`
- `.claude/skills/create-service/SKILL.md` — replaced `Articles.sln` with "the solution file"
- `.claude/skills/create-module/SKILL.md` — replaced `Articles.sln` with "the solution file"
- `.claude/skills/create-service-claude-md/SKILL.md` — replaced `Articles.sln` with "the solution file"
- `.claude/skills/create-service/workflows/ScaffoldPersistenceProject.md` — genericized reference paths
- `.claude/skills/create-service/workflows/ScaffoldApplicationProject.md` — replaced `Articles.IntegrationEvents.Contracts`, genericized reference paths
- `.claude/skills/create-module/workflows/ScaffoldComponent.md` — replaced `Articles.sln` reference

### Step 6: Reference paths
- `.claude/skills/create-service-claude-md/workflows/WriteClaudeMd.md` — replaced 5 service-specific exemplar paths with description-based guidance
- `.claude/skills/create-module/workflows/ScaffoldDomainApplication.md` — replaced `ArticleTimeline` and service-specific reference paths
- `.claude/skills/create-domain-event-handler/workflows/Handler.md` — full rewrite: removed `Reflekt` namespace/paths, genericized handler patterns and variant headings
- `.claude/skills/create-feature/workflows/EndpointCarter.md` — replaced "Used by" service names and reference path
- `.claude/skills/create-feature/workflows/EndpointMinimalApi.md` — replaced "Used by" service names, reference path, and entity examples
- `.claude/skills/create-feature/workflows/Handler.md` — replaced "Used by" service names and reference path
- `.claude/skills/create-feature/workflows/Mappings.md` — replaced reference path and `Article`/`ArticleResponse` inline mapping examples
- `.claude/skills/add-integration-event/workflows/Publisher.md` — full rewrite: genericized reference paths, removed service names from variant headings
- `.claude/skills/add-integration-event/workflows/Consumer.md` — full rewrite: genericized all reference paths, entity examples, naming conventions, location table
- `.claude/skills/add-integration-event/workflows/EventContract.md` — full rewrite: replaced `Articles.Integration.Contracts` paths and `ArticleApprovedForReview` example
- `.claude/skills/cqrs-patterns/SKILL.md` — replaced "Production custom" with "Custom base (opt-in per service)", genericized registration reference path and `CreateArticleCommandValidator`
- `.claude/skills/create-grpc-contract/workflows/Server.md` — replaced service-specific reference path
- `.claude/skills/create-grpc-contract/workflows/Contract.md` — replaced `Articles.Grpc.Contracts` reference path

### Step 7: "Used by" and variant table service names
- `.claude/skills/create-feature/workflows/EndpointFastEndpoints.md` — full rewrite: replaced "Used by: Auth, Journals, Production" with "Used by: FastEndpoints services", renamed "Production Variant: BaseEndpoint" to "Optional: Custom Base Endpoint"
- `.claude/skills/create-feature/SKILL.md` — replaced service-name folder structure headings with framework names
- `.claude/skills/create-feature/workflows/Validator.md` — replaced "Production variant" with "Optional: if service defines a custom base validator"
- `.claude/skills/persistence-patterns/SKILL.md` — full rewrite: replaced all `SubmissionDbContext`/`ArticleRepository`/`Article` entity examples with generic placeholders, replaced service-name tables with description-based tables
- `.claude/skills/redis-patterns/SKILL.md` — full rewrite: replaced all `Journal`/`Section`/`JournalDbContext`/`Journals.*` paths with generic placeholders

### Step 8: Existing registries
- `.claude/skills/add-integration-event/SKILL.md` — replaced events table with empty headers + note to check `{ProjectName}.Integration.Contracts`
- `.claude/skills/create-grpc-contract/SKILL.md` — replaced contracts table with empty headers + note to check `{ProjectName}.Grpc.Contracts`, replaced port convention with root CLAUDE.md reference
- `.claude/skills/create-service-claude-md/workflows/CaptureAxes.md` — full rewrite: removed specific port pairs and service-name examples, replaced with root CLAUDE.md references and generic placeholders

### Step 9: Entity examples in persistence and error handling
- `.claude/skills/error-handling/SKILL.md` — replaced "Auth handlers, middleware" with "Authentication middleware", replaced `command.ArticleId` with `command.{Entity}Id`
- `.claude/skills/authorization-patterns/SKILL.md` — replaced `ArticleAccessAuthorizationHandler`/`ArticleRoleRequirement`/`IArticleAccessChecker` with `{Resource}*` placeholders, replaced `Articles.Security`/`Articles.Abstractions` paths

### Steps 10–11: BaseEndpoint/BaseValidator, remaining references
- `.claude/skills/system-design/SKILL.md` — replaced "Auth-style services", "ArticleHub pattern", `Article.FromSubmission()` with generic equivalents
- `.claude/skills/create-aggregate/SKILL.md` — replaced "Auth `User`" with "Identity `User`", genericized argument example
- `.claude/skills/create-service/workflows/ScaffoldFolders.md` — replaced service-name references in MediatR/FastEndpoints descriptions and read-model section heading
- `.claude/skills/create-service/workflows/ReadClaudeMd.md` — replaced `Articles.Grpc.Contracts` and `Articles.Integration.Contracts` with `{ProjectName}.*`
- `.claude/skills/create-module-claude-md/SKILL.md` — removed `ArticleTimeline` example
- `.claude/skills/create-module-claude-md/workflows/CaptureDomainAxes.md` — genericized framework-host-compatibility warning, "Current hosts" axis description, example axis table

## Key Decisions
- Used `{Svc}`, `{Entity}`, `{Module}`, `{ProjectName}`, `{SolutionFile}` placeholders consistently across all files.
- Files with 5+ targeted edits were rewritten in full for reliability.
- In code blocks, generic domain names (`Order`, `LineItem`, `Assignee`) replace Reflekt-specific names (`Article`, `Asset`, `Typesetter`).
- Variant headings drop service names; every variant section adds "Check the service's CLAUDE.md for which variant to use."

## Deviations from Plan
- Steps 6–11 were executed together per-file rather than strictly sequentially — many files had overlapping concerns from multiple steps. All plan items are accounted for.

## Verification (Step 12)
All 6 grep checks return 0 matches:
1. `IArticleAction` — 0 matches
2. `ArticleApproved|ArticleAccepted|ArticlePublished|ArticleReviewed` — 0 matches
3. Service-specific paths (Submission/Review/Production/Auth/Journals/ArticleHub/Reflekt) — 0 matches
4. `Articles.sln|Articles.Abstractions|Articles.Security|Articles.Integration|Articles.Grpc` — 0 matches
5. `Typesetter|ArticleStateMachine|ArticleStage|ArticleActionType` — 0 matches
6. `JournalDbContext|SubmissionDbContext|ReviewDbContext|ProductionDbContext` — 0 matches
