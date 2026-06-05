# Scaffold Infrastructure

Wire the new service into the repository infrastructure: solution file, Dockerfile, docker-compose orchestration, and the Visual Studio docker-compose project. This is the final step — after it, the solution compiles and the service is launchable.

## Steps

1. **Add projects to the solution file**
2. **Create the service Dockerfile** (if docker = Y)
3. **Add a service block to `docker-compose.yml`** (if docker = Y)
4. **Reference the new API csproj from `docker-compose.dcproj`** (if docker = Y)
5. **Set `<DockerComposeProjectPath>` in the new API csproj** (already handled in `ScaffoldCsprojFiles.md` — verify only)

## Step 1: Add projects to `{SolutionFile}`

Run from the repo root:

```bash
dotnet sln src/{SolutionFile} add \
  src/Services/{Name}/{Name}.Domain/{Name}.Domain.csproj \
  src/Services/{Name}/{Name}.Persistence/{Name}.Persistence.csproj \
  src/Services/{Name}/{Name}.API/{Name}.API.csproj
```

Add `.Application` only if it was scaffolded:

```bash
dotnet sln src/{SolutionFile} add src/Services/{Name}/{Name}.Application/{Name}.Application.csproj
```

Use the `--solution-folder Services/{Name}` flag if the exemplar services are grouped in solution folders — check `{SolutionFile}` first.

## Step 2: Service Dockerfile

**Skip this step if docker axis = N.**

**Reference:** `src/Services/{Svc}/{Svc}.API/Dockerfile`

Create `src/Services/{Name}/{Name}.API/Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Services/{Name}/{Name}.API/{Name}.API.csproj", "Services/{Name}/{Name}.API/"]
COPY ["Services/{Name}/{Name}.Application/{Name}.Application.csproj", "Services/{Name}/{Name}.Application/"]
COPY ["Services/{Name}/{Name}.Persistence/{Name}.Persistence.csproj", "Services/{Name}/{Name}.Persistence/"]
COPY ["Services/{Name}/{Name}.Domain/{Name}.Domain.csproj", "Services/{Name}/{Name}.Domain/"]
# Add every BuildingBlocks reference used by the new service — copy from an existing service's Dockerfile
RUN dotnet restore "./Services/{Name}/{Name}.API/{Name}.API.csproj"
COPY . .
WORKDIR "/src/Services/{Name}/{Name}.API"
RUN dotnet build "./{Name}.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./{Name}.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "{Name}.API.dll"]
```

Omit the `.Application` COPY line if there is no Application project. List every BuildingBlocks and Module csproj that the service references — missing references cause `dotnet restore` to fail inside the container.

## Step 3: Add a service block to `docker-compose.yml`

**Reference:** existing service blocks in `src/docker-compose.yml`.

Add a new top-level service entry:

```yaml
  {name}.api:
    image: ${DOCKER_REGISTRY-}{name}api
    build:
      context: .
      dockerfile: Services/{Name}/{Name}.API/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_HTTP_PORTS=8080
      - ASPNETCORE_HTTPS_PORTS=8081
      - ConnectionStrings__Database=...   # match persistence axis
    ports:
      - "{http-port}:8080"
      - "{https-port}:8081"
    depends_on:
      - {db-container}       # sqlserver / postgres / redis
      - rabbitmq             # if integration events published or consumed
```

`depends_on` branches:
- **SQL Server:** `sqlserver`
- **PostgreSQL:** `postgres`
- **Redis:** `redis`
- **Any integration events:** add `rabbitmq`
- **gRPC client calls to other services:** do not add to depends_on (gRPC retries handle transient unavailability)

Do **not** add a new database container — reuse the shared `sqlserver` / `postgres` / `redis` container with a unique schema or database name.

## Step 4: Reference the new API csproj from `docker-compose.dcproj`

**Reference:** `src/docker-compose.dcproj`

Add a `<ProjectReference>` entry so Visual Studio's Docker Compose orchestrator knows about the new service:

```xml
<ItemGroup>
  <ProjectReference Include="Services\{Name}\{Name}.API\{Name}.API.csproj" />
</ItemGroup>
```

Match the path style used by existing entries (backslashes on Windows, forward slashes if that is the repo convention).

## Step 5: Verify `<DockerComposeProjectPath>` in the new csproj

`ScaffoldCsprojFiles.md` already set this property in the `.API` csproj:

```xml
<DockerComposeProjectPath>..\..\..\docker-compose.dcproj</DockerComposeProjectPath>
```

Just verify it is present. Without it, Visual Studio does not pick up the service in the Docker Compose launch target.

## Step 6: API Gateway (manual, not scaffolded)

Do **not** touch `src/ApiGateway/`. Routing through YARP is a deliberate decision the Developer (or Architect) makes per service. Leave a note in the final report:

> The service is not yet reachable through the API Gateway. Add a YARP route to `ApiGateway/appsettings.json` if external clients should reach it.

## Step 7: Final build

```bash
dotnet build src/{SolutionFile}
```

The solution must compile with zero errors and zero warnings introduced by the new service. If the build fails, fix the csproj references or DI stubs before reporting to the user. Do not leave a broken solution.

## After this step

The service is fully scaffolded: folder tree, csproj wiring, source stubs, Dockerfile, docker-compose entry, solution registration. The Developer runs `create-aggregate` next, then `create-feature`.
