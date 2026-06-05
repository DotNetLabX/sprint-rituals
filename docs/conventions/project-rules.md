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
  BuildingBlocks/              # Shared libraries
    (app-agnostic)             #   Reusable primitives (no project knowledge)
    (app-specific)             #   gRPC contracts, integration event contracts
  Services/                    # Microservices — one folder per service
    {Svc}/
      {Svc}.API/               #   FastEndpoints feature slices, SignalR hubs, DI, migration startup host
      {Svc}.Domain/            #   Aggregates, value objects, domain events
      {Svc}.Persistence/       #   EF Core DbContext, configs, migrations, repositories
  Modules/                     # Modular monolith (Contracts + Implementation per module)
  ApiGateway/                  # YARP reverse proxy
docker-compose                 # Local dev environment
```

Default to **module**; only use a microservice when deployment/scaling/ownership demands it.

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
- **EF migrations:** `dotnet ef migrations add Name -p Services/{Svc}/{Svc}.Persistence -s Services/{Svc}/{Svc}.API`

## Plan-to-Code Boundary

- **Plan steps describe operations and acceptance criteria — not method body logic flows.** The developer decides internal code structure and decomposition.
- **When a plan step has >5 sequential sub-operations**, indicate they should be decomposed into private methods — don't describe one monolithic method.

## Build Verification

- `dotnet build` — run after every backend implementation step. Fresh output, not assumed.
- `npm run build` (from `client/`) — run after frontend changes. Must exit 0 with no TypeScript errors.
- `npx vitest run` (from `client/`) — run frontend tests when they exist.
- **File-lock MSB errors (MSB3026/MSB3027/MSB3492) on Windows are not compilation errors** — they mean the app or dev server is holding output DLLs. Filter with `error CS` to confirm zero actual compiler errors. Don't chase these as build failures.
- Debug artifact grep: `Console.WriteLine` (debugging), `TODO`, `HACK`, `FIXME`, commented-out code.
