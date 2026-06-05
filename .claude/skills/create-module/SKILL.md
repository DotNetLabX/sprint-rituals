---
name: create-module
description: Scaffolds an empty module skeleton from its CLAUDE.md. Branches on archetype — Component (Contracts + first implementation) or Domain (Domain + Persistence + optional Application + optional API as class library).
---

# Create Module

Scaffolds a new module as an empty skeleton. Modules come in two archetypes; the skill branches on the one recorded in the CLAUDE.md:

- **Component** — `.Contracts` interface project + one or more implementation projects. Stateless infra adapters.
- **Domain** — `.Domain` + `.Persistence` (+ optional `.Application`, `.API`). Owns its own aggregates, persists them, optionally exposes endpoints as a class-library API that hosts mount via a `Map{Name}Endpoints()` extension.

After this skill runs, the Developer runs `create-aggregate` (Domain only) and `create-feature` for business work.

## Precondition

`src/Modules/{Name}/CLAUDE.md` must already exist. It is produced by the Architect's `create-module-claude-md` skill. If the file is missing, **hard-error**:

> `src/Modules/{Name}/CLAUDE.md` not found. Ask the Architect to run `create-module-claude-md` first.

Do not scaffold anything without the CLAUDE.md.

## Steps

1. **Read the CLAUDE.md** — follow `workflows/ReadClaudeMd.md`. Extract the archetype and every axis into a working model.

2. **Branch on archetype:**
   - **Component** → follow `workflows/ScaffoldComponent.md`. Handles both first-run (creates `.Contracts` + first impl) and second-run (creates new impl only, reuses existing `.Contracts`).
   - **Domain** → follow `workflows/ScaffoldDomain.md`. Creates `.Domain` + `.Persistence` always.
     - If CLAUDE.md says `Application layer: yes` → also follow `workflows/ScaffoldDomainApplication.md`.
     - If CLAUDE.md says `API layer: yes` → also follow `workflows/ScaffoldDomainApi.md`. Branches by framework.

3. **Scaffold infrastructure** — follow `workflows/ScaffoldInfrastructure.md`. Updates `the solution file` with `dotnet sln add` for every created csproj.

4. **Verify:** `dotnet build`. The solution must compile with only the empty skeleton in place.

5. **Report to the user.** Tailor the message per archetype:
   - **Component:**
     > Module `{Name}` scaffolded with contract `{IContractName}` and implementation `{FirstImpl}`. Host services reference `{Name}.Contracts` + `{Name}.{FirstImpl}`. Add more implementations by re-running `create-module {Name}` with a new impl name.
   - **Domain:**
     > Module `{Name}` scaffolded. Host services mount it via `services.Add{Name}Persistence(configuration)` + `app.Migrate<{Name}DbContext>()`{, and `app.Map{Name}Endpoints()` if API layer is present}. Run `create-aggregate` next for the first aggregate.

## Arguments

Pass the module name: `/create-module Audit`

The folder `src/Modules/{Name}/` must already exist with a `CLAUDE.md` inside (from the Architect skill). The skill reads the axes from that file.

## What this skill does NOT do

- No aggregates, domain events, or value objects — those come from `create-aggregate`.
- No first feature — that comes from `create-feature`.
- No host wiring — the Developer manually adds the `Add{Name}Persistence`/`Map{Name}Endpoints` calls to host services that mount the module. The skill does not assume which hosts adopt it.
- No API Gateway routes. Modules are never reached through YARP directly.
- No root CLAUDE.md update — the module table in the repo-root CLAUDE.md is edited manually.
