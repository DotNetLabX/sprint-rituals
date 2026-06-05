---
name: create-service
description: Scaffolds an empty service skeleton from its CLAUDE.md — folder tree, csproj files with references, GlobalUsings, DI wire-up stubs, Program.cs, Dockerfile. Creates no business classes. Run create-aggregate and create-feature afterward.
---

# Create Service

Scaffolds a new service as an empty skeleton — folders, csproj files with correct references, infrastructure wire-up (Program.cs, DI stubs, appsettings, Dockerfile, docker-compose entry), but **no business classes**. After this skill runs the Developer runs `create-aggregate` for the first aggregate and `create-feature` for the first feature.

## Precondition

`src/Services/{Name}/CLAUDE.md` must already exist. It is produced by the Architect's `create-service-claude-md` skill. If the file is missing, **hard-error**:

> `src/Services/{Name}/CLAUDE.md` not found. Ask the Architect to run `create-service-claude-md` first.

Do not proceed without the CLAUDE.md.

## Steps

1. **Read the CLAUDE.md** — follow `workflows/ReadClaudeMd.md`. Extract every axis into a mental model.

2. **Create the folder tree** — follow `workflows/ScaffoldFolders.md`. Empty placeholder folders so it is obvious where things go.

3. **Create the csproj files with correct references** — follow `workflows/ScaffoldCsprojFiles.md`. Branches by framework, persistence, and shared-state axes.

4. **Scaffold the `.API` project files** — follow `workflows/ScaffoldApiProject.md`. Branches by endpoint framework.

5. **Scaffold the `.Domain` project files** — follow `workflows/ScaffoldDomainProject.md`. Mostly empty plus `GlobalUsings.cs`.

6. **Scaffold the `.Persistence` project files** — follow `workflows/ScaffoldPersistenceProject.md`. Branches by persistence technology.

7. **Scaffold the `.Application` project files** (conditional) — follow `workflows/ScaffoldApplicationProject.md`. Only when the CLAUDE.md says shared-state = Y.

8. **Scaffold the infrastructure** — follow `workflows/ScaffoldInfrastructure.md`. Updates `the solution file`, `docker-compose.yml`, `docker-compose.dcproj`. Runs `dotnet sln add` inline.

9. **Verify:** `dotnet build`. The solution must compile cleanly with only the empty skeleton.

10. **Report to the user:**
    > Service `{Name}` scaffolded. Run `create-aggregate` next for the first aggregate, then `create-feature` for the first feature.

## Arguments

Pass the service name: `/create-service Indexing`

The folder `src/Services/Indexing/` must already exist with a `CLAUDE.md` inside (from the Architect skill). The skill reads the axes from that file.

## What this skill does NOT do

- No aggregates, no features, no domain classes — those come from `create-aggregate` and `create-feature`.
- No API Gateway (YARP) route registration. If the service needs to be reachable via the gateway, the Developer adds the route manually in `ApiGateway/` after this skill runs.
- No migrations — run `dotnet ef migrations add InitialCreate` manually after `create-aggregate`.
- No root CLAUDE.md update — the service table in the repo-root CLAUDE.md is edited manually.
