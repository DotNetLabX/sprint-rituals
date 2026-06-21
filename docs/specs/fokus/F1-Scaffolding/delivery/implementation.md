# F1-F3 Scaffolding — Implementation

## Files Created

### Solution
- `src/Fokus.slnx` — .NET 10 solution file (slnx format, default for .NET 10)

### Fokus.Domain
- `src/Services/Fokus/Fokus.Domain/Fokus.Domain.csproj` — pure C# class library, net10.0, no dependencies
- `src/Services/Fokus/Fokus.Domain/Entities/Sprint.cs` — aggregate root, natural key (Jira sprint ID), Memberships collection
- `src/Services/Fokus/Fokus.Domain/Entities/Ticket.cs` — aggregate root, natural key (Jira issue key), StatusTransitions collection, Assignee nav
- `src/Services/Fokus/Fokus.Domain/Entities/Developer.cs` — entity, natural key (Jira account ID)
- `src/Services/Fokus/Fokus.Domain/Entities/SprintMembership.cs` — child entity, composite PK, nav to Sprint + Ticket
- `src/Services/Fokus/Fokus.Domain/Entities/StatusTransition.cs` — child entity, auto PK, nav to Ticket
- `src/Services/Fokus/Fokus.Domain/Entities/AppSettings.cs` — single-row settings entity with CreateDefault() factory
- `src/Services/Fokus/Fokus.Domain/Enums/SprintState.cs` — Active, Closed
- `src/Services/Fokus/Fokus.Domain/ValueObjects/HealthThresholdConfig.cs` — POCO for JSON column
- `src/Services/Fokus/Fokus.Domain/ValueObjects/HealthWeightConfig.cs` — POCO for JSON column

### Fokus.Persistence
- `src/Services/Fokus/Fokus.Persistence/Fokus.Persistence.csproj` — references Domain, EF Core + SQLite packages
- `src/Services/Fokus/Fokus.Persistence/FokusDbContext.cs` — 6 DbSets, applies configs from assembly
- `src/Services/Fokus/Fokus.Persistence/DependencyInjection.cs` — AddFokusPersistence extension, registers DbContext + repositories
- `src/Services/Fokus/Fokus.Persistence/DesignTimeDbContextFactory.cs` — for EF Core tooling
- `src/Services/Fokus/Fokus.Persistence/Configurations/SprintConfiguration.cs` — PK ValueGeneratedNever, cascade Memberships
- `src/Services/Fokus/Fokus.Persistence/Configurations/TicketConfiguration.cs` — PK on Key, IX_EpicKey, IX_AssigneeId, cascade StatusTransitions
- `src/Services/Fokus/Fokus.Persistence/Configurations/DeveloperConfiguration.cs` — PK on AccountId
- `src/Services/Fokus/Fokus.Persistence/Configurations/SprintMembershipConfiguration.cs` — composite PK, IX_TicketKey
- `src/Services/Fokus/Fokus.Persistence/Configurations/StatusTransitionConfiguration.cs` — auto PK, IX_TicketKey_Timestamp
- `src/Services/Fokus/Fokus.Persistence/Configurations/AppSettingsConfiguration.cs` — JSON columns for thresholds/weights, value converters for List<string>
- `src/Services/Fokus/Fokus.Persistence/Repositories/SprintRepository.cs` — eager loads Memberships
- `src/Services/Fokus/Fokus.Persistence/Repositories/TicketRepository.cs` — eager loads StatusTransitions
- `src/Services/Fokus/Fokus.Persistence/Repositories/DeveloperRepository.cs`
- `src/Services/Fokus/Fokus.Persistence/Repositories/AppSettingsRepository.cs` — GetAsync (with auto-seed), SaveAsync (upsert)
- `src/Services/Fokus/Fokus.Persistence/Migrations/` — InitialCreate migration (auto-generated)

### Fokus.API
- `src/Services/Fokus/Fokus.API/Fokus.API.csproj` — references Domain + Persistence, FastEndpoints 6.0, Scalar, Mapster 10.0, OpenAPI
- `src/Services/Fokus/Fokus.API/Program.cs` — app bootstrap, migration on startup, seed AppSettings, static files, SPA fallback
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — AddFokusServices + UseFokusMiddleware (FastEndpoints, Swagger, OpenAPI, Scalar dev-only)
- `src/Services/Fokus/Fokus.API/appsettings.json` — SQLite connection string
- `src/Services/Fokus/Fokus.API/appsettings.Development.json` — verbose logging
- `src/Services/Fokus/Fokus.API/Properties/launchSettings.json` — HTTP on port 5000
- `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsEndpoint.cs` — GET /api/settings
- `src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsResponse.cs`
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveSettings/SaveSettingsEndpoint.cs` — PUT /api/settings
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveSettings/SaveSettingsRequest.cs`
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveSettings/SaveSettingsResponse.cs`
- `src/Services/Fokus/Fokus.API/Features/Settings/SaveSettings/SaveSettingsValidator.cs` — FluentValidation rules

### Frontend (client/)
- `client/package.json` — Vue 3, TypeScript, Vite, Tailwind CSS 4, Pinia, vue3-apexcharts, vue-router
- `client/vite.config.ts` — proxy /api to localhost:5000, build output to wwwroot
- `client/index.html` — dark theme, Fokus title
- `client/tsconfig.json`, `client/tsconfig.app.json`, `client/tsconfig.node.json`
- `client/src/main.ts` — Vue app with Pinia, Router, ApexCharts
- `client/src/App.vue` — root with router-view
- `client/src/router.ts` — 5 routes (Dashboard, Developers, Sprints, Epics, Settings)
- `client/src/assets/main.css` — Tailwind v4 import
- `client/src/types/index.ts` — AppSettings, HealthThresholdConfig, HealthWeightConfig, SprintState
- `client/src/api/client.ts` — base fetch wrapper
- `client/src/api/settings.ts` — getSettings, saveSettings
- `client/src/stores/settingsStore.ts` — Pinia store with fetch/update actions
- `client/src/views/SettingsView.vue` — full settings page (board, done statuses, workflow stages, thresholds, weights)
- `client/src/views/DashboardView.vue` — placeholder
- `client/src/views/DevelopersView.vue` — placeholder
- `client/src/views/SprintsView.vue` — placeholder
- `client/src/views/EpicsView.vue` — placeholder

## Key Decisions

- **Solution file format**: .NET 10 defaults to `.slnx` (new XML format), not `.sln`. Used the default.
- **Package versions**: FastEndpoints resolved to 6.0.0, Mapster to 10.0.0 (latest available for .NET 10). Plan specified older versions that don't exist for this target framework.
- **No HasData for AppSettings**: EF Core does not support `HasData` on entities with JSON-mapped owned types (`ToJson()`). Seed is done programmatically in `Program.cs` on startup instead.
- **DesignTimeDbContextFactory**: Added to enable EF migrations, since FastEndpoints throws when no endpoints exist and the host fails to build the service provider at design time.
- **SprintMembership FK configuration**: Configured both sides of the relationship (Sprint → Memberships, Ticket → SprintMembership) with cascading deletes.
- **Tailwind CSS v4**: Uses `@import "tailwindcss"` instead of the v3 `@tailwind base/components/utilities` directives. No `tailwind.config.ts` needed — v4 uses CSS-first configuration.
- **SprintState as type union**: TypeScript `erasableSyntaxOnly` mode (default in Vue template) doesn't allow `enum`. Used `type SprintState = 'Active' | 'Closed'` instead.

## Deviations from Plan

- **HasData seed removed** (Step 5): Plan specified `HasData` in `AppSettingsConfiguration` for the default row. EF Core rejects this when the entity has `ToJson()` owned types. Moved seed to `Program.cs` runtime startup. Functionally equivalent — the seed row is always present.
- **No route prefix in FastEndpoints config** (Step 6): Plan mentioned `RoutePrefix = "api"` but endpoints define full `/api/...` paths in their `Configure()` method. Using both would double the prefix. Kept full paths in endpoints, no global prefix.
