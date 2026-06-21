# F1–F3 Scaffolding

**Feature Spec:** None

## Context

Fokus is a greenfield project — no `src/` directory, no solution file, no frontend exists yet. This plan scaffolds the full foundation: .NET solution with the three-project split (API/Domain/Persistence), Vue 3 SPA with Vite + Tailwind + ApexCharts + Pinia, all 5 domain entities, EF Core persistence with SQLite, and the Settings system (AppSettings entity + CRUD endpoints + Settings UI).

This is a single service, not a microservice from the existing Articles codebase. There are no shared BuildingBlocks to reference. All base classes, interceptors, and infrastructure are built inline within the Fokus projects.

### Services impacted
- `Fokus.API` (new)
- `Fokus.Domain` (new)
- `Fokus.Persistence` (new)
- `client/` Vue SPA (new)

## Scope

### In scope
- F1: .NET solution, three-project split, Vue SPA with Vite + Tailwind + ApexCharts + Pinia, Vite build → wwwroot, dev proxy
- F2: All 5 entities (Sprint, Developer, Ticket, SprintMembership, StatusTransition), EF Core configs, composite keys, indexes, SQLite, initial migration
- F3: AppSettings entity (single-row, typed, JSON columns), GetSettings/SaveSettings endpoints, Settings UI page (board selector, done statuses, health thresholds + weights, workflow stages, developer sub-team tagging)

### Explicitly out of scope
- Jira integration (F4)
- Sync logic (F5)
- App shell / navigation chrome (F7) — only the SettingsView is built; other views are placeholder routes
- Authentication — v1 has no user auth
- SignalR, MassTransit, gRPC — not used in v1
- Docker / docker-compose — not needed yet for local dev

## Domain Model Changes

### New aggregates (from architecture v1 §4)

**Sprint** (aggregate root, natural key `Id` = Jira sprint ID)
- Properties: Id (int, PK), Name (string), StartDate (DateTime), EndDate (DateTime), BoardId (int), BoardName (string), State (SprintState enum), SyncedAt (DateTime)
- Navigation: Memberships (List\<SprintMembership\>)

**Ticket** (aggregate root, natural key `Key` = Jira issue key)
- Properties: Key (string, PK), Summary (string), IssueType (string), StoryPoints (decimal?), EpicKey (string?), EpicName (string?), AssigneeId (string?, FK → Developer), Priority (string), CurrentStatus (string), CreatedDate (DateTime), ResolvedDate (DateTime?)
- Navigation: StatusTransitions (List\<StatusTransition\>)

**Developer** (entity, natural key `AccountId` = Jira account ID)
- Properties: AccountId (string, PK), DisplayName (string), AvatarUrl (string?), SubTeam (string?), IsActive (bool)

**SprintMembership** (child entity of Sprint, composite PK)
- Properties: SprintId (int, FK → Sprint), TicketKey (string, FK → Ticket), AddedAt (DateTime), RemovedAt (DateTime?), WasCommitted (bool), FinalStatus (string), StoryPoints (decimal?)

**StatusTransition** (child entity of Ticket, auto PK)
- Properties: Id (int, auto PK), TicketKey (string, FK → Ticket), FromStatus (string), ToStatus (string), Timestamp (DateTime), AuthorId (string?)

### New entity for Settings (F3)

**AppSettings** (single-row entity, auto PK = 1)
- Properties: Id (int, PK, always 1), BoardId (int?), DoneStatuses (List\<string\>, JSON column), WorkflowStages (List\<string\>, JSON column), HealthThresholds (HealthThresholdConfig, JSON column — owned type), HealthWeights (HealthWeightConfig, JSON column — owned type)

**HealthThresholdConfig** (value object, stored as JSON)
- CompletionGreen (decimal), CompletionAmber (decimal), DisruptionGreen (decimal), DisruptionAmber (decimal), CarryOverGreen (decimal), CarryOverAmber (decimal)

**HealthWeightConfig** (value object, stored as JSON)
- Completion (int), Disruption (int), CarryOver (int)

### Enums
- `SprintState` { Active, Closed }

## Data Model Changes

All tables in SQLite via EF Core. Natural keys for Sprint, Ticket, Developer. Composite key for SprintMembership. Auto-increment for StatusTransition and AppSettings.

### Indexes (from architecture §11)
| Table | Index | Purpose |
|-------|-------|---------|
| SprintMembership | PK (SprintId, TicketKey) | Composite key |
| SprintMembership | IX_TicketKey | Carry-over queries |
| StatusTransition | IX_TicketKey_Timestamp | Cycle time computation |
| Ticket | IX_EpicKey | Epic progress queries |
| Ticket | IX_AssigneeId | Developer throughput queries |

### Migration
One initial migration covering all 6 tables (Sprint, Developer, Ticket, SprintMembership, StatusTransition, AppSettings) + seed row for AppSettings with defaults.

## Implementation Steps

### Step 1 — Create .NET solution and three-project skeleton

Create the solution file and three empty projects with correct references.

**Create:**
- `Fokus.sln` (solution root at `src/`)
- `src/Services/Fokus/Fokus.API/Fokus.API.csproj` — SDK Web, references Domain + Persistence
- `src/Services/Fokus/Fokus.Domain/Fokus.Domain.csproj` — SDK Library, no project references
- `src/Services/Fokus/Fokus.Persistence/Fokus.Persistence.csproj` — SDK Library, references Domain

**Package references:**
- `Fokus.API`: FastEndpoints, FastEndpoints.Swagger (for Scalar), Microsoft.AspNetCore.OpenApi, Scalar.AspNetCore, Microsoft.EntityFrameworkCore.Design, Mapster
- `Fokus.Domain`: (none — pure C#)
- `Fokus.Persistence`: Microsoft.EntityFrameworkCore.Sqlite, Microsoft.EntityFrameworkCore

**Reference graph:** API → Domain, API → Persistence → Domain. No Application project (FastEndpoints, no MediatR).

**Target framework:** `net10.0`

**Verify:** `dotnet build` from `src/` compiles cleanly.

### Step 2 — Scaffold API project (Program.cs, DI, middleware)

Wire up FastEndpoints, OpenAPI + Scalar, SQLite, static files serving, and CORS.

**Create:**
- `src/Services/Fokus/Fokus.API/Program.cs` — builder with FastEndpoints, Scalar (dev-only), static files (wwwroot), CORS same-origin, call `AddFokusPersistence`, migrate on startup
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — `AddFokusServices` extension (FastEndpoints registration, OpenAPI, Mapster)
- `src/Services/Fokus/Fokus.API/appsettings.json` — connection string `Data Source=fokus.db`, logging config
- `src/Services/Fokus/Fokus.API/appsettings.Development.json` — verbose logging
- `src/Services/Fokus/Fokus.API/Properties/launchSettings.json` — HTTP profile on port 5000

**Pattern:** Follow FastEndpoints setup from `create-service` skill's `ScaffoldApiProject.md`, adapted for Fokus (no auth, no MassTransit, no gRPC, SQLite instead of SQL Server). Scalar UI gated to `IsDevelopment()` per CLAUDE.md tech stack rules.

**Verify:** `dotnet build` passes. `dotnet run` starts and shows Scalar UI at `/scalar`.

### Step 3 — Scaffold Domain project (entities, enums, value objects)

Create all 5 entities + AppSettings + enums + value objects. Thin domain — entities are data containers per architecture v1 §4.

**Create:**
- `src/Services/Fokus/Fokus.Domain/Entities/Sprint.cs` — properties + `Memberships` collection
- `src/Services/Fokus/Fokus.Domain/Entities/Ticket.cs` — properties + `StatusTransitions` collection
- `src/Services/Fokus/Fokus.Domain/Entities/Developer.cs` — properties only
- `src/Services/Fokus/Fokus.Domain/Entities/SprintMembership.cs` — properties + navigation to Sprint and Ticket
- `src/Services/Fokus/Fokus.Domain/Entities/StatusTransition.cs` — properties + navigation to Ticket
- `src/Services/Fokus/Fokus.Domain/Entities/AppSettings.cs` — properties with JSON-backed complex types, static factory `CreateDefault()` for seed
- `src/Services/Fokus/Fokus.Domain/Enums/SprintState.cs` — Active, Closed
- `src/Services/Fokus/Fokus.Domain/ValueObjects/HealthThresholdConfig.cs` — POCO for JSON column (green/amber boundaries for 3 metrics)
- `src/Services/Fokus/Fokus.Domain/ValueObjects/HealthWeightConfig.cs` — POCO for JSON column (completion, disruption, carryOver weights)

**Design notes:**
- No base `AggregateRoot` class — Fokus is standalone, no shared BuildingBlocks. Entities are plain C# classes.
- No domain events in v1 per architecture doc.
- Sprint.Id is `int` (Jira sprint ID, not auto-increment). Developer.AccountId is `string`. Ticket.Key is `string`. All natural keys — `ValueGeneratedNever()` in EF config.
- SprintMembership has composite PK (SprintId, TicketKey).
- StatusTransition.Id is auto-increment.
- AppSettings.Id is always 1 (single-row pattern).

**Skill reference:** `domain-patterns` for general aggregate/entity patterns, adapted to Fokus's thin domain (no behavior split, no domain events).

### Step 4 — Scaffold Persistence project (DbContext, configs, repositories)

Create DbContext with all DbSets, entity configurations with indexes, and repositories.

**Create:**
- `src/Services/Fokus/Fokus.Persistence/FokusDbContext.cs` — DbContext with DbSets for all 6 entities, `OnModelCreating` applies configs from assembly
- `src/Services/Fokus/Fokus.Persistence/DependencyInjection.cs` — `AddFokusPersistence` extension, registers DbContext with SQLite, registers repositories
- `src/Services/Fokus/Fokus.Persistence/Configurations/SprintConfiguration.cs` — PK (Id), ValueGeneratedNever, owns Memberships collection
- `src/Services/Fokus/Fokus.Persistence/Configurations/TicketConfiguration.cs` — PK (Key), ValueGeneratedNever, IX_EpicKey, IX_AssigneeId, owns StatusTransitions
- `src/Services/Fokus/Fokus.Persistence/Configurations/DeveloperConfiguration.cs` — PK (AccountId), ValueGeneratedNever
- `src/Services/Fokus/Fokus.Persistence/Configurations/SprintMembershipConfiguration.cs` — composite PK (SprintId, TicketKey), IX_TicketKey, FKs to Sprint and Ticket
- `src/Services/Fokus/Fokus.Persistence/Configurations/StatusTransitionConfiguration.cs` — PK (Id) auto, IX_TicketKey_Timestamp composite index, FK to Ticket
- `src/Services/Fokus/Fokus.Persistence/Configurations/AppSettingsConfiguration.cs` — PK (Id), JSON columns for DoneStatuses, WorkflowStages, HealthThresholds, HealthWeights. Seed single default row.
- `src/Services/Fokus/Fokus.Persistence/Repositories/SprintRepository.cs` — concrete class (no interface per guardrails), wraps DbContext, includes eager loading for Memberships
- `src/Services/Fokus/Fokus.Persistence/Repositories/TicketRepository.cs` — eager loads StatusTransitions
- `src/Services/Fokus/Fokus.Persistence/Repositories/DeveloperRepository.cs`
- `src/Services/Fokus/Fokus.Persistence/Repositories/AppSettingsRepository.cs` — `GetAsync()` returns single row, `SaveAsync()` upserts

**Skill reference:** `persistence-patterns` for repository pattern and entity configuration patterns, adapted (no `AuditedEntityConfiguration` base, no shared BuildingBlocks — plain `IEntityTypeConfiguration<T>` implementations). SQLite JSON columns use EF Core 10's native JSON column support (`ToJson()` in `OwnsOne`).

**Verify:** `dotnet build` passes.

### Step 5 — Create initial EF Core migration + seed

Run EF migration to create the database schema. Seed AppSettings default row.

**Commands:**
```bash
dotnet ef migrations add InitialCreate -p Services/Fokus/Fokus.Persistence -s Services/Fokus/Fokus.API
```

**Seed:** AppSettings default row with:
- BoardId: null
- DoneStatuses: ["Done", "Closed"]
- WorkflowStages: [] (empty — auto-detected on first sync)
- HealthThresholds: { CompletionGreen: 80, CompletionAmber: 60, DisruptionGreen: 10, DisruptionAmber: 25, CarryOverGreen: 10, CarryOverAmber: 25 }
- HealthWeights: { Completion: 40, Disruption: 30, CarryOver: 30 }

Seed is done via `HasData` in `AppSettingsConfiguration`.

**Verify:** `dotnet ef database update` creates `fokus.db` without errors.

### Step 6 — Settings endpoints (GetSettings + SaveSettings)

Create the two FastEndpoints for F3's backend.

**Create:**
- `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsEndpoint.cs` — GET `/api/settings`, returns current AppSettings as DTO
- `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsResponse.cs` — response DTO
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveSettings/SaveSettingsEndpoint.cs` — PUT `/api/settings`, accepts full settings payload, validates, persists
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveSettings/SaveSettingsRequest.cs` — request DTO
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveSettings/SaveSettingsValidator.cs` — FluentValidation: BoardId > 0 if provided, DoneStatuses non-empty, weights sum to 100, thresholds in valid ranges
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveSettings/SaveSettingsResponse.cs` — response DTO (confirmation)

**Pattern:** FastEndpoints `Endpoint<TRequest, TResponse>` with handler logic inline. No MediatR. Validator auto-discovered by FastEndpoints. Follow `create-feature` skill's `EndpointFastEndpoints.md` pattern.

**Skill reference:** `create-feature` (FastEndpoints variant)

**Verify:** `dotnet build` passes. Scalar UI shows both endpoints.

### Step 7 — Scaffold Vue 3 SPA with Vite + Tailwind + Pinia + ApexCharts

Create the frontend project with all tooling configured.

**Create:**
- `client/` directory at repo root (per architecture v1 §2)
- `client/package.json` — Vue 3, TypeScript, Vite, Tailwind CSS 4, Pinia, vue3-apexcharts, apexcharts, vue-router
- `client/vite.config.ts` — proxy `/api/*` to `http://localhost:5000`, build output to `../src/Services/Fokus/Fokus.API/wwwroot/`
- `client/tsconfig.json` + `client/tsconfig.app.json` — strict TypeScript
- `client/index.html` — SPA entry point
- `client/src/main.ts` — Vue app bootstrap with Pinia, Router, ApexCharts plugin
- `client/src/App.vue` — root component with `<router-view />`
- `client/src/router.ts` — 5 routes: Dashboard, Developers, Sprints, Epics, Settings (only Settings has a real view; others are placeholder components)
- `client/src/env.d.ts` — Vue type declarations
- `client/tailwind.config.ts` (or `@tailwind` CSS import for Tailwind v4) — dark mode class strategy
- `client/src/assets/main.css` — Tailwind base/components/utilities imports, dark theme defaults

**Verify:** `npm install` + `npm run dev` starts dev server. `npm run build` outputs to `wwwroot/`.

### Step 8 — Frontend API client + types + settings store

Create the typed fetch layer, TypeScript interfaces, and Pinia store for settings.

**Create:**
- `client/src/api/client.ts` — base fetch wrapper (base URL `/api`, JSON headers, error handling)
- `client/src/api/settings.ts` — `getSettings()`, `saveSettings()` typed functions
- `client/src/types/index.ts` — TypeScript interfaces: `AppSettings`, `HealthThresholdConfig`, `HealthWeightConfig`, `SprintState`, matching API contracts
- `client/src/stores/settingsStore.ts` — Pinia store: `settings` state, `fetchSettings` action, `updateSettings` action

**Pattern:** Per architecture v1 §8 — stores call API module → API does fetch → response typed and stored in state.

### Step 9 — Settings UI view

Build the Settings page with all configuration sections from F3.

**Create:**
- `client/src/views/SettingsView.vue` — full settings page with sections:
  1. **Jira Board** — board ID input (number, will be enhanced with board selector in F4)
  2. **Done Statuses** — tag/chip input for status names (add/remove)
  3. **Workflow Stages** — ordered list with drag-to-reorder or up/down buttons (will be auto-populated by F6)
  4. **Health Thresholds** — 3 pairs of inputs (green/amber boundary for completion, disruption, carry-over rates)
  5. **Health Weights** — 3 number inputs summing to 100 (completion, disruption, carry-over)
  6. **Developer Sub-Teams** — listed for future use (actual developer CRUD comes with sync data)
- Save button calls `settingsStore.updateSettings()`
- Load on mount via `settingsStore.fetchSettings()`

**Create placeholder views:**
- `client/src/views/DashboardView.vue` — "Dashboard — coming soon"
- `client/src/views/DevelopersView.vue` — placeholder
- `client/src/views/SprintsView.vue` — placeholder
- `client/src/views/EpicsView.vue` — placeholder

**Design:** Tailwind dark theme, card-based sections, consistent spacing per architecture v1 §8 design system.

**Verify:** `npm run dev` → navigate to `/settings` → form renders, loads defaults from API, save round-trips correctly.

### Step 10 — Vite build integration + static files serving verification

Ensure the production build pipeline works end-to-end.

**Modify:**
- Verify `client/vite.config.ts` outputs to `src/Services/Fokus/Fokus.API/wwwroot/`
- Verify `Program.cs` serves static files with `app.UseStaticFiles()` and has SPA fallback (`app.MapFallbackToFile("index.html")`) so Vue Router works

**Commands:**
```bash
cd client && npm run build
dotnet run --project src/Services/Fokus/Fokus.API
```

**Verify:** Browser at `http://localhost:5000` serves the Vue SPA. Navigating to `/settings` works (SPA fallback). API calls to `/api/settings` work from the built SPA.

## Migration Notes

```bash
# From src/ directory:
dotnet ef migrations add InitialCreate -p Services/Fokus/Fokus.Persistence -s Services/Fokus/Fokus.API
dotnet ef database update -p Services/Fokus/Fokus.Persistence -s Services/Fokus/Fokus.API
```

Seed data: AppSettings row with ID=1 and default values (done via `HasData` in configuration).

## Testing Strategy

- **Build verification:** `dotnet build` must pass after steps 1–6.
- **Migration verification:** `dotnet ef database update` creates schema + seed row.
- **API verification:** Scalar UI at `/scalar` (dev only) shows GetSettings and SaveSettings endpoints.
- **Settings round-trip:** GET returns seed defaults → PUT with modified values → GET returns updated values.
- **Frontend build:** `npm run build` produces files in `wwwroot/`.
- **SPA serving:** Built SPA loads at `http://localhost:5000`, Vue Router navigation works.
- **Settings UI:** Form loads, edits save, page refresh shows persisted values.

## Open Questions

None — all requirements are clear from the backlog and architecture doc.
