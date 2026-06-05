# Scaffold API Project

Create the `.API` project source files. Branch on the endpoint framework axis.

## Common files (all framework branches)

### `{Name}.API/GlobalUsings.cs`

Start minimal — add to it as needed. **Reference:** `src/Services/{Svc}/{Svc}.API/GlobalUsings.cs`.

Typical usings for a new service:

```csharp
global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Routing;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Blocks.AspNetCore;
global using {ProjectName}.Security;
```

Add framework-specific and MediatR/FastEndpoints usings from the exemplar service.

### `{Name}.API/appsettings.json`

**Reference:** `src/Services/{Svc}/{Svc}.API/appsettings.json`

Sections to include (branch on axes):
- `ConnectionStrings:Database` — connection string for the chosen DB
- `ConnectionStrings:{secondary}` — if file storage is configured
- `Logging` — standard
- `AllowedHosts: "*"`
- `JwtOptions` — `{ Issuer, Audience, Secret }` — copy from exemplar
- `RabbitMqOptions` — only if integration events
- `EmailOptions` — only if EmailService module referenced
- Storage options — only if file storage is configured
- Persistence-specific options (e.g., `AzureBlobFileStorageOptions`, `MongoGridFsOptions`)

### `{Name}.API/appsettings.Development.json`

**Reference:** `src/Services/{Svc}/{Svc}.API/appsettings.Development.json`

Minimal — usually just verbose logging levels. Copy from exemplar.

### `{Name}.API/Properties/launchSettings.json`

**Reference:** `src/Services/{Svc}/{Svc}.API/Properties/launchSettings.json`

Set the HTTP and HTTPS ports from the port axis. Four profiles: `http`, `https`, `IIS Express`, `Docker` (if docker=Y).

## Framework branches

### Branch: FastEndpoints

**Reference exemplar:** `src/Services/{Svc}/{Svc}.API/Program.cs` and `src/Services/{Svc}/{Svc}.API/DependencyInjection.cs` (use a FastEndpoints service as exemplar).

**`{Name}.API/Program.cs`:**
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .Add{Name}Services(builder.Configuration);

// Persistence
builder.Services.Add{Name}Persistence(builder.Configuration);

// Application layer (if present)
builder.Services.Add{Name}Application(builder.Configuration);

// Shared modules (if any) — e.g., file storage, email
// builder.Services.AddAzureFileStorage(builder.Configuration);

var app = builder.Build();

app.Use{Name}Middleware();

app.Migrate<{Name}DbContext>();
if (app.Environment.IsDevelopment())
{
    // dev-only setup
}

app.Run();
```

**`{Name}.API/DependencyInjection.cs`:** registers FastEndpoints, Swagger, authentication, CORS, MassTransit (if integration events), gRPC clients. Follow an existing FastEndpoints service's `DependencyInjection.cs` structure.

### Branch: Carter + MediatR

**Reference exemplar:** `src/Services/{Svc}/{Svc}.API/Program.cs` and `src/Services/{Svc}/{Svc}.API/DependencyInjection.cs` (use a Carter + MediatR service as exemplar).

`Program.cs` follows the same shape as FastEndpoints but calls `AddCarter()` and `MapCarter()` instead of the FastEndpoints equivalents, and calls `AddMediatR(...)` with `{Name}.Application` assembly scan.

### Branch: Minimal APIs + MediatR

**Reference exemplar:** `src/Services/{Svc}/{Svc}.API/Program.cs` and `src/Services/{Svc}/{Svc}.API/DependencyInjection.cs` (use a Minimal APIs + MediatR service as exemplar).

Adds `app.MapAllEndpoints()` in `Program.cs` that calls into each endpoint's static `Map()` method. Register MediatR with the `{Name}.Application` assembly.

### Branch: Carter read-model

**Reference exemplar:** `src/Services/{Svc}/{Svc}.API/Program.cs` and `src/Services/{Svc}/{Svc}.API/DependencyInjection.cs` (use a Carter read-model service as exemplar).

No MediatR registration. Carter is added. MassTransit is registered with the consumer assembly (since read-model services are consumer-driven).

## Empty Features folder

Leave `{Name}.API/Features/` empty (except `.gitkeep` if needed). The Developer adds the first feature folder when running `create-feature`.

For read-model services, leave `{Name}.API/Consumers/` and `{Name}.API/Queries/` empty.

## After this step

The `.API` project has its source files and should compile once `.Domain` / `.Persistence` / `.Application` scaffolding finishes. Next: `ScaffoldDomainProject.md`.
