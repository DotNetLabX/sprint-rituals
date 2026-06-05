# Scaffold Module Infrastructure

Wire every project created in this skill run into the solution file, then verify the build. Modules do not get Dockerfiles or docker-compose entries — they run inside host services.

## Step 1: Add projects to `{SolutionFile}`

Run from the repo root. The exact list depends on the archetype and which conditional projects were scaffolded:

### Component archetype — first run

```bash
dotnet sln src/{SolutionFile} add \
  src/Modules/{Name}/{Name}.Contracts/{Name}.Contracts.csproj \
  src/Modules/{Name}/{Name}.{FirstImpl}/{Name}.{FirstImpl}.csproj
```

### Component archetype — second run (new impl only)

```bash
dotnet sln src/{SolutionFile} add src/Modules/{Name}/{Name}.{NewImpl}/{Name}.{NewImpl}.csproj
```

### Domain archetype

Always add `.Domain` + `.Persistence`:

```bash
dotnet sln src/{SolutionFile} add \
  src/Modules/{Name}/{Name}.Domain/{Name}.Domain.csproj \
  src/Modules/{Name}/{Name}.Persistence/{Name}.Persistence.csproj
```

Conditionally add `.Application`:

```bash
dotnet sln src/{SolutionFile} add src/Modules/{Name}/{Name}.Application/{Name}.Application.csproj
```

Conditionally add `.API`:

```bash
dotnet sln src/{SolutionFile} add src/Modules/{Name}/{Name}.API/{Name}.API.csproj
```

Use `--solution-folder Modules/{Name}` if the exemplar modules are grouped in solution folders — check `{SolutionFile}` first.

## Step 2: No Dockerfile, no docker-compose

Modules are class libraries mounted into host services. They do not run as their own containers. Explicitly do **not**:
- Create a Dockerfile in any module project
- Add a service block to `docker-compose.yml`
- Add a ProjectReference to `docker-compose.dcproj`

If the module has an API layer, the host service's existing docker container will serve the module's endpoints.

## Step 3: Host wiring — documentation only, no code change

The skill does **not** modify any host service csproj or startup code. That is a deliberate manual step the Developer takes when a host service adopts the module. Leave a note in the final report:

> The module is not wired into any host service. To adopt it, add a ProjectReference from the host's `.API` (or `.Application`) csproj to the module's csproj, call `services.Add{Name}Persistence(configuration)` in the host's DI setup, and (for Domain modules) call `app.Migrate<{Name}DbContext>()` at startup. If the module has an API layer with Minimal APIs, also call `app.Map{Name}Endpoints()` before `app.Run()`.

This keeps host adoption an explicit decision per service.

## Step 4: Final build

```bash
dotnet build src/{SolutionFile}
```

The solution must compile with zero errors and zero new warnings. If the build fails, fix csproj references or DI stubs before reporting success — do not leave a broken solution.

## After this step

The module is fully scaffolded and registered in the solution. Return to the SKILL.md final report step.
