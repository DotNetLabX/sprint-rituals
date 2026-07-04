# Stack Rules

Architectural guardrails, repo structure, and conventions. Referenced by all agents that plan, write, or review code.

## Source File Definition

Source files are files that contain application code. Agents restricted from writing source code must not create or modify these files:

- `src/**/*.cs` — all C# files under the services, modules, and building blocks tree
- `client/src/**/*.ts` — TypeScript files
- `client/src/**/*.vue` — Vue single-file components
- `client/src/**/*.css` — stylesheets

These are NOT source files (agents may read/write them freely):
- `docs/**` — documentation, specs, plans, KB entries
- `.claude/**` — agent definitions, rules, skills
- `client/index.html`, `client/*.config.*` — frontend build config
- Root config files (`*.slnx`, `docker-compose.*`, `*.json`, `*.md`)

## Document Lifecycle — Proposals vs Plans (binding)

A **proposal** and a **plan** are different artifacts at different lifecycle stages. Do not conflate them, and never call a draft a "plan."

| Term | Meaning | Home | State |
|------|---------|------|-------|
| **Proposal** | A draft idea, research, or evaluation that **must be refined** before it can be built | `docs/proposals/` | Not actionable — needs shaping (PO → spec) or a decision first |
| **Plan** | An implementation plan that is **ready to be built** | `docs/specs/{slug}/delivery/plan.md` | Actionable — the word "plan" means "go build this" |

Rules:
- **All plans live in a slug** (`docs/specs/{slug}/delivery/plan.md`). A plan never lives in `docs/proposals/`.
- When you say **"plan," it is ready to implement.** If it still needs refining, it is a **proposal**, not a plan.
- The `{slug}` is assigned by the team lead or PO (see agents-workflow); agents never derive it. A proposal graduates into a slug's `plan.md` when the work is scheduled.
- Idea/research/evaluation docs (e.g. the BR-coverage proposal, research, golden-set draft, the harness evaluation) correctly stay in `docs/proposals/` — they are proposals, not plans.

## Guardrails — DO NOT

- **No entity-wrapper service classes** (e.g. `ArticleService`, `SprintService`) — classes that accumulate business logic around a single entity. Focused operation services scoped to a feature area (e.g. `SprintIssueSyncService`) are allowed.
- **No repository interfaces** — single implementation, interfaces add zero value. Other components (modules, cross-cutting) do use interfaces where contracts are needed.
- **No god folders** (`Services/`, `Helpers/`, `Utils/`).
- **No bypassing domain rules** via EF configs or endpoints.
- **Domain events** = within service boundary. **Integration events** = cross-service.
- **No `.gitkeep` files** — don't track empty directories.
- **No secrets in commits or artifacts.** No agent may commit, include, or reference files containing secrets (`.env`, `credentials.json`, API keys, connection strings, tokens). If a file appears to contain secrets, warn the user instead of proceeding.

## Repo Structure

```
src/
  Host/                        # Single Web SDK deployable — composes all modules
  BuildingBlocks/              # Shared libraries
    (app-agnostic)             #   Reusable primitives (no project knowledge)
    Blocks.Contracts/          #   App-specific cross-module contracts: gRPC interfaces + integration event DTOs
  Services/                    # Domain modules — one folder per domain
    {Svc}/
      {Svc}.API/               #   FastEndpoints feature slices, SignalR hubs, DI (class library)
      {Svc}.Domain/            #   Aggregates, value objects, domain events
      {Svc}.Persistence/       #   EF Core DbContext, configs, migrations, repositories
  Modules/                     # External-system integration modules (Contracts + Implementation)
client/                        # Vue 3 SPA — built into src/Host/wwwroot/
```

This is a **modular monolith**: `src/Host` is the only Web SDK / deployable project. All `*.API` projects are class libraries composed into a single process. No API gateway. No docker-compose in this architecture.

## Project Reference Graph

```
API  ──▶  Domain
  │
  └─▶  Persistence  ──▶  Domain
```

- `API` references both `Domain` and `Persistence` directly. Endpoints consume concrete repositories — no abstraction layer.
- `Persistence` references `Domain`.
- `Domain` references nothing.

## Conventions

- **Domain events** carry aggregate reference, not individual properties.
- **Aggregate creation:** static factory when business rules or domain events are involved; `required init` properties when plain data.
- **FastEndpoints:** one endpoint class per feature, validator as sibling class in same folder.
- **Repositories** wrap `SaveChangesAsync` — endpoints never touch DbContext directly.
- **EF migrations:** `dotnet ef migrations add Name -p Services/{Svc}/{Svc}.Persistence -s Services/{Svc}/{Svc}.API` — the startup project (`-s`) must carry `Microsoft.EntityFrameworkCore.Design`. If the API project is a class library that does not include it, use the Persistence project as startup instead (`-s Services/{Svc}/{Svc}.Persistence`), provided it has an `IDesignTimeDbContextFactory`.

## Port Convention — `{AA}XY`

`AA` = app prefix (**51** = SprintRituals), `X` = protocol digit (0 = HTTP, 4 = HTTPS, 5 = gRPC), `Y` = service index. The modular monolith has one deployable — the Host is service index **0**. Never bind default ports (5000/5001/5173-auto); allocate from this table.

| Port | Protocol | Locked in | Status |
|------|----------|-----------|--------|
| **5100** | HTTP — API, SignalR, all SPAs | `src/Host/appsettings.json` `Kestrel:Endpoints:Http` | active |
| **5150** | gRPC (h2c, module-to-module) | `src/Host/appsettings.json` `Kestrel:Endpoints:Grpc` + `Grpc:Services:*:Url` | active |
| 5140 | HTTPS | — | reserved, opt-in |
| 5171 | Vite dev — dashboard (Fokus) | `client/apps/dashboard/vite.config.ts` (`strictPort`) | active |
| 5172 | Vite dev — retro (Reflekt) | `client/apps/retro/vite.config.ts` (`strictPort`) | active |
| 5173 | Vite dev — poker | `client/apps/poker/vite.config.ts` (`strictPort`) | active |

The explicit `Kestrel:Endpoints` block is authoritative — it overrides `ASPNETCORE_URLS` and `launchSettings.json`; keep `launchSettings.json` aligned to avoid the override warning. Vite `strictPort: true` fails fast instead of hopping ports. If a service is ever split out of the monolith, it takes the next service index (Y = 1, 2, …).

## Plan-to-Code Boundary

- **Plan steps describe operations and acceptance criteria — not method body logic flows.** The developer decides internal code structure and decomposition.
- **When a plan step has >5 sequential sub-operations**, indicate they should be decomposed into private methods — don't describe one monolithic method.

## Build Verification

- `dotnet build` — run after every backend implementation step. Fresh output, not assumed.
- `npm run build` (from `client/`) — run after frontend changes. Must exit 0 with no TypeScript errors.
- `npx vitest run` (from `client/`) — run frontend tests when they exist.
- **File-lock MSB errors (MSB3026/MSB3027/MSB3492) on Windows are not compilation errors** — they mean the app or dev server is holding output DLLs. Filter with `error CS` to confirm zero actual compiler errors. Don't chase these as build failures.
- Debug artifact grep: `Console.WriteLine` (debugging), `TODO`, `HACK`, `FIXME`, commented-out code.
