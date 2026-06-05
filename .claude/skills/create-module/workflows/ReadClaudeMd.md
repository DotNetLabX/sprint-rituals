# Read Module CLAUDE.md

## Path

`src/Modules/{Name}/CLAUDE.md`

## Hard-error if missing

If the file does not exist, stop immediately and tell the user:

> `src/Modules/{Name}/CLAUDE.md` not found. Ask the Architect to run `create-module-claude-md` first.

Do not scaffold anything.

## First: detect archetype

Read the `**Type:**` line near the top of the CLAUDE.md:

- `**Type:** Component (infra adapter)` → **Component** archetype
- `**Type:** Domain module` → **Domain** archetype

Every other axis depends on the archetype.

## Component archetype — axes to extract

| Axis | Location in CLAUDE.md | Used by |
|------|----------------------|---------|
| Module name | `# {Name} Module` header | folder path, project names |
| Contract interface | `**Contract:**` line | interface stub file name |
| Current implementations | `**Implementations:**` line | whether to scaffold `.Contracts` or skip it |
| First impl name | From the `**Implementations:**` list (or conversation context) | project name for the first/new implementation project |

### Second-run detection

Before scaffolding, check if `src/Modules/{Name}/{Name}.Contracts/` already exists. If yes → second run → only scaffold the new implementation project. If no → first run → scaffold both `.Contracts` and the first implementation.

## Domain archetype — axes to extract

| Axis | Location in CLAUDE.md | Used by |
|------|----------------------|---------|
| Module name | `# {Name} Module` header | folder path, project names, DbContext class name |
| Endpoint framework | `**Endpoint framework:**` line | `ScaffoldDomainApi.md` branch |
| Persistence | `**Persistence:**` line | `ScaffoldDomain.md` branch (EF vs Redis) |
| DB connection pattern | `**DB connection:**` line | `ScaffoldDomain.md` — embedded vs own-connection DI wiring |
| Schema name | `**Schema:**` line | `HasDefaultSchema()` call in DbContext (embedded pattern only) |
| Application layer | `**Application layer:**` line | whether `ScaffoldDomainApplication.md` runs |
| API layer | `**API layer:**` line | whether `ScaffoldDomainApi.md` runs |
| Current hosts | `**Current hosts:**` line | documentation only — informs the final report |

## Derived decisions

From the axes, derive:

- **Is this embedded or standalone?** — from `DB connection pattern`. Embedded = module does not own its `DbConnection` registration; the host does. Standalone = module reads `ConnectionStrings:{Name}` itself.
- **Needs MediatR?** — Domain modules with `Application layer: yes` and an API framework that uses MediatR. Component modules never need MediatR.
- **Needs assembly-scan note in host wiring docs?** — FastEndpoints / Carter API branches. Minimal APIs branch needs only an explicit `Map{Name}Endpoints()` call.

## Store axes in a working model

Keep the extracted axes accessible for the remaining workflows. Each subsequent workflow branches on these values.
