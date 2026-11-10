# Reference Patterns — dotnet-microservices

Extracted from the reference codebase. Read-only research. All citations use `path:line` format relative to the reference codebase.

---

## 1. Central Package Management

### `Directory.Packages.props`

File: `src/Directory.Packages.props`

```xml
<!-- src/Directory.Packages.props:1-6 -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
  <ItemGroup>
    <!-- gRPC -->
    <PackageVersion Include="protobuf-net" Version="3.2.56" />
    <PackageVersion Include="FastEndpoints" Version="7.0.1" />
    <PackageVersion Include="MediatR" Version="13.0.0" />
    <PackageVersion Include="Carter" Version="9.0.0" />
    <PackageVersion Include="MassTransit.RabbitMQ" Version="8.5.2" />
    <PackageVersion Include="FluentValidation" Version="12.0.0" />
    <PackageVersion Include="Mapster" Version="7.4.0" />
    <!-- EF.Core -->
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="9.0.8" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.4" />
    <!-- ... all other packages ... -->
  </ItemGroup>
</Project>
```

Key facts:
- `ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>` at `src/Directory.Packages.props:3`
- `CentralPackageTransitivePinningEnabled>true` at `src/Directory.Packages.props:4` — pins transitive deps too
- All `<PackageVersion Include="..." Version="..." />` entries live only here — 82 lines total
- **No `Directory.Build.props`** was found anywhere under `src/` (glob returned no results)

### How a `.csproj` references a package without a version

From `src/Services/Submission/Submission.API/Submission.API.csproj:14-18`:
```xml
<ItemGroup>
  <PackageReference Include="Grpc.Net.Client" />
  <PackageReference Include="Microsoft.AspNetCore.OpenApi" />
  <PackageReference Include="Microsoft.Extensions.Http.Polly" />
  <PackageReference Include="Microsoft.VisualStudio.Azure.Containers.Tools.Targets" />
</ItemGroup>
```

No `Version` attribute — version is resolved from the central `Directory.Packages.props`.

---

## 2. Program.cs / Host Composition

### Three distinct patterns exist across services

#### Auth.API — FastEndpoints service (`src/Services/Auth/Auth.API/Program.cs`)

```csharp
// Auth.API/Program.cs:1-50
builder.Services
    .ConfigureApiOptions(builder.Configuration);

builder.Services
    .AddApiServices(builder.Configuration)
    .AddApplicationServices(builder.Configuration)
    .AddPersistenceServices(builder.Configuration);

app.Migrate<AuthDbContext>();
if (app.Environment.IsDevelopment())
    app.SeedTestData();

app
    .UseSwagger()
    .UseSwaggerUI()
    .UseHttpsRedirection()
    .UseMiddleware<GlobalExceptionMiddleware>()
    .UseRouting()
    .UseAuthentication()
    .UseAuthorization()
    .UseCustomFastEndpoints()   // wraps UseFastEndpoints(...)
    .UseSwaggerGen();           // FastEndpoints Swagger

app.MapGrpcService<PersonGrpcService>();  // gRPC server
```

#### Submission.API — Minimal API + MediatR service (`src/Services/Submission/Submission.API/Program.cs`)

```csharp
// Submission.API/Program.cs:14-20
builder.Services
    .ConfigureApiOptions(builder.Configuration)
    .AddApiServices(builder.Configuration)
    .AddApplicationServices(builder.Configuration)
    .AddPersistenceServices(builder.Configuration);

app.Migrate<SubmissionDbContext>();
if (app.Environment.IsDevelopment())
    app.Services.SeedTestData();

app.MapAllEndpoints();  // calls each static XEndpoint.Map(api) via extension
```

#### ArticleHub.API — Carter service (`src/Services/ArticleHub/ArticleHub.API/Program.cs`)

```csharp
// ArticleHub.API/Program.cs:13-17
builder.Services
    .ConfigureOptions(builder.Configuration)
    .AddApiAndApplicationServices(builder.Configuration)   // merges API+Application into one method
    .AddPersistenceServices(builder.Configuration);

app.Migrate<ArticleHubDbContext>();

var api = app.MapGroup("/api");
api.MapCarter();
```

### Decomposition pattern

Each service distributes registration across **three layer methods** (all `IServiceCollection` extension methods in `DependencyInjection.cs` / `DependecyInjection.cs` per layer):

| Method | Layer file | What it registers |
|---|---|---|
| `AddApiServices(config)` | `{Service}.API/DependencyInjection.cs` | FastEndpoints/Carter, Swagger, Auth, gRPC clients, HTTP context, authorization handlers |
| `AddApplicationServices(config)` | `{Service}.Application/DependencyInjection.cs` | MediatR, FluentValidation, Mapster, MassTransit, pipeline behaviors, domain services |
| `AddPersistenceServices(config)` | `{Service}.Persistence/DependencyInjection.cs` | DbContext, repositories |
| `ConfigureApiOptions(config)` | API layer | Options, JSON serializer config |

Example — `src/Services/Auth/Auth.Application/DependencyInjection.cs:8`:
```csharp
public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
```

Example — `src/Services/Auth/Auth.Persistence/DependencyInjection.cs:8`:
```csharp
public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration config)
{
    var connectionString = config.GetConnectionString("Database");
    services.AddDbContext<AuthDbContext>(opts => opts.UseSqlServer(connectionString));
    services.AddScoped<PersonRepository>();
    services.AddScoped<UserRepository>();
    return services;
}
```

### `app.Migrate<T>()` helper

Defined in `src/BuildingBlocks/Blocks.EntityFrameworkCore/Extensions/DbContextExtensions.cs:18-27`:
```csharp
public static IHost Migrate<TDbContext>(this IHost app)
    where TDbContext : DbContext
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<TDbContext>();
    context.Database.Migrate();
    return app;
}
```

Called in every service `Program.cs` as `app.Migrate<XDbContext>()` immediately after `builder.Build()`.

### Seeding

`app.SeedTestData()` / `app.Services.SeedTestData()` — called only inside `IsDevelopment()` guards. Defined per-service in `{Service}.Persistence/Data/Test/`.

### MassTransit

Wired in `AddApplicationServices` via `Blocks.Messaging` helper:

`src/BuildingBlocks/Blocks.Messaging/MassTransit/DependencyInjection.cs:12-43`:
```csharp
public static IServiceCollection AddMassTransitWithRabbitMQ(
    this IServiceCollection services, IConfiguration configuration, Assembly assembly)
{
    services.AddMassTransit(config =>
    {
        config.SetEndpointNameFormatter(new SnakeCaseWithServiceSuffixNameFormatter(serviceName));
        if (assembly != null)
            config.AddConsumers(assembly);
        config.UsingRabbitMq((context, rabbitConfig) =>
        {
            rabbitConfig.Host(new Uri(rabbitMqOptions.Host), rabbitMqOptions.VirtualHost, h => { ... });
            rabbitConfig.ConfigureEndpoints(context);
        });
    });
}
```

Called in Application layer: `src/Services/Submission/Submission.Application/DependencyInjection.cs:30`:
```csharp
.AddMassTransitWithRabbitMQ(configuration, Assembly.GetExecutingAssembly());
```

Consumers are auto-discovered from the calling assembly (Application layer).

### gRPC

**Server** (Auth.API): registered in `AddApiServices` via `services.AddCodeFirstGrpc(...)` and then `app.MapGrpcService<PersonGrpcService>()` in `Program.cs:48`.

**Client** (Submission.API): registered in `AddApiServices` via `services.AddCodeFirstGrpcClient<IPersonService>(grpcOptions, "Person")` — `src/Services/Submission/Submission.API/DependecyInjection.cs:63-64`.

gRPC wiring is **distributed to the module** that owns it (not centralized in a host).

---

## 3. Endpoint / Command / Query Structure

The reference uses **two endpoint frameworks** across services:

| Service | Framework |
|---|---|
| Auth.API | **FastEndpoints** |
| Submission.API | **Minimal API + MediatR** (static endpoint classes) |
| ArticleHub.API | **Carter + Minimal API** |

### FastEndpoints pattern (Auth.API)

Feature folder layout for `CreateAccount`:
```
src/Services/Auth/Auth.API/Features/Users/CreateAccount/
  CreateUserCommand.cs           ← request DTO + response record (combined)
  CreateUserCommandMapping.cs    ← AutoMapper Profile
  CreateUserCommandValidator.cs  ← AbstractValidator<CreateUserCommand>
  CreateUserEndpoint.cs          ← Endpoint<CreateUserCommand, CreateUserResponse>
  SendConfirmationEmailOnUserCreatedHandler.cs  ← IEventHandler (domain event)
```

`src/Services/Auth/Auth.API/Features/Users/CreateAccount/CreateUserEndpoint.cs:13-14`:
```csharp
public class CreateUserEndpoint(...) : Endpoint<CreateUserCommand, CreateUserResponse>
{
    public override async Task HandleAsync(CreateUserCommand command, CancellationToken ct)
    { ... }
}
```

Route/metadata via **attributes** (no separate `Configure()` method for most endpoints):
```csharp
// CreateUserEndpoint.cs:10-12
[Authorize(Roles = Articles.Security.Role.UserAdmin)]
[HttpPost("users")]
[Tags("Users")]
```

Some endpoints split `Configure()` into a partial class file (see SetPassword below).

**Request/response/validator co-located in the same feature folder** — the command IS the request DTO (no separate "Request" class).

Type naming convention: **`{Action}{Entity}Command`** for mutations, **`{Action}{Entity}Query`** for reads, **`{Entity}Response`** / **`{Action}{Entity}Response`** for responses.

Example from `CreateUserCommand.cs:5`: `public class CreateUserCommand`
Example from `Submission.Application/.../GetArticle/GetArticleQuery.cs`: `GetArticleQuery`

### Minimal API pattern (Submission.API)

`src/Services/Submission/Submission.API/Endpoints/CreateArticleEndpoint.cs`:
```csharp
public static class CreateArticleEndpoint
{
    public static void Map(this IEndpointRouteBuilder app)
    {
        app.MapPost("/articles", async (CreateArticleCommand command, ISender sender) =>
        {
            var response = await sender.Send(command);
            return Results.Created($"/api/articles/{response.Id}", response);
        })
        .RequireRoleAuthorization(Role.Author)
        .WithName("CreateArticle").WithTags("Articles")
        .Produces<IdResponse>(StatusCodes.Status201Created);
    }
}
```

The `CreateArticleCommand` **is the HTTP request body** — no separate request DTO.

Commands and queries live in the Application layer under `Features/{FeatureName}/`:
```
src/Services/Submission/Submission.Application/Features/CreateArticle/
  CreateArticleCommand.cs           ← record + AbstractValidator<CreateArticleCommand> (combined in same file)
  CreateArticleCommandHandler.cs    ← IRequestHandler<CreateArticleCommand, ...>
```

`src/Services/Submission/Submission.Application/Features/CreateArticle/CreateArticleCommand.cs:3-7`:
```csharp
public record CreateArticleCommand(int JournalId, string Title, ArticleType Type, string Scope)
    : ArticleCommand
{
    public override ArticleActionType ActionType => ArticleActionType.CreateArticle;
}
```

Endpoint registration aggregated in `src/Services/Submission/Submission.API/Endpoints/XEndpointRegistration.cs`:
```csharp
public static IEndpointRouteBuilder MapAllEndpoints(this IEndpointRouteBuilder app)
{
    var api = app.MapGroup("/api");
    GetArticleEndpoint.Map(api);
    CreateArticleEndpoint.Map(api);
    // ...
    return app;
}
```

### Carter pattern (ArticleHub.API)

`src/Services/ArticleHub/ArticleHub.API/Articles/GetArticle/GetArticleEndpoint.cs`:
```csharp
public class GetArticleEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/articles/{articleId:int}", async (int articleId, ArticleGraphQLReadStore graphQLReadStore, CancellationToken ct) =>
        { ... })
        .RequireAuthorization()
        .WithName("GetArticle").WithTags("Articles");
    }
}
```

No separate command/query objects — handler logic is inline in the Carter module. No MediatR in ArticleHub.

---

## 4. Validator Placement & Naming

Two patterns exist:

**Pattern A — Same file as command (Submission.Application)**

`src/Services/Submission/Submission.Application/Features/CreateArticle/CreateArticleCommand.cs:9-23`:
```csharp
public class CreateArticleCommandValidator : AbstractValidator<CreateArticleCommand>
{
    public CreateArticleCommandValidator()
    {
        RuleFor(x => x.Title).NotEmptyWithMessage(...).MaximumLengthWithMessage(...);
        RuleFor(x => x.Scope).NotEmptyWithMessage(...).MaximumLengthWithMessage(...);
        RuleFor(c => c.JournalId).GreaterThan(0)...;
    }
}
```

Validator class is declared in the same `.cs` file as the command record.

**Pattern B — Separate file in same feature folder (Auth.API FastEndpoints)**

`src/Services/Auth/Auth.API/Features/Users/CreateAccount/CreateUserCommandValidator.cs`:
```csharp
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{ ... }
```

Naming convention (both patterns): **`{CommandOrQueryName}Validator`** — e.g., `CreateUserCommandValidator`, `CreateArticleCommandValidator`, `LoginCommandValidator`.

Validators are registered via `AddValidatorsFromAssemblyContaining<T>()` — see `src/Services/Submission/Submission.Application/DependencyInjection.cs:21`:
```csharp
.AddValidatorsFromAssemblyContaining<CreateArticleCommandValidator>()
```

FastEndpoints auto-discovers validators from the assembly without explicit registration.

---

## 5. Layering

### Auth service — 4 projects (API, Application, Domain, Persistence)

```
Auth.API       → Auth.Application, Auth.Persistence, Auth.Domain, BuildingBlocks/*
Auth.Application → Auth.Persistence, Auth.Domain, Modules/EmailService.Contracts
Auth.Persistence → Auth.Domain, BuildingBlocks/Blocks.EntityFrameworkCore
Auth.Domain    → BuildingBlocks/Articles.Abstractions, BuildingBlocks/Blocks.Domain
```

From csproj files:
- `src/Services/Auth/Auth.API/Auth.API.csproj:28-34`: references Application + Persistence + Domain + BB
- `src/Services/Auth/Auth.Application/Auth.Application.csproj:10-12`: references Persistence + Domain
- `src/Services/Auth/Auth.Persistence/Auth.Persistence.csproj:16-17`: references Blocks.EntityFrameworkCore + Domain
- `src/Services/Auth/Auth.Domain/Auth.Domain.csproj:9-11`: references Articles.Abstractions + Blocks.Domain

### Submission service — 4 projects (API, Application, Domain, Persistence)

```
Submission.API        → Submission.Application, BuildingBlocks/*, Modules/*
Submission.Application → Submission.Persistence, Submission.Domain (transitively), BuildingBlocks/Blocks.MediatR, Blocks.Messaging
Submission.Persistence → Submission.Domain, Blocks.EntityFrameworkCore
Submission.Domain     → Articles.Abstractions, Blocks.Domain, Blocks.Exceptions, Blocks.Core, Modules/FileService.Contracts
```

From `src/Services/Submission/Submission.API/Submission.API.csproj`: API references `Submission.Application` only (gets Domain + Persistence transitively).

From `src/Services/Submission/Submission.Application/Submission.Application.csproj`: Application references `Submission.Persistence`.

**Key observation:** API → Application → Persistence → Domain (strict downward chain). API does NOT directly reference Domain or Persistence separately in Submission — it goes through Application.

### ArticleHub service — 3 projects (API, Domain, Persistence) — no Application layer

```
ArticleHub.API         → ArticleHub.Persistence, BuildingBlocks/*, Carter
ArticleHub.Persistence → ArticleHub.Domain
ArticleHub.Domain      → (domain primitives only)
```

No Application layer — Carter endpoints call persistence directly.

### Vertical slice variant (Auth.API)

Auth.API collapses Application into API: handler logic lives directly in `Endpoint<TReq, TRes>.HandleAsync`. No separate Application project. Auth.API.csproj references Application + Persistence + Domain directly.

---

## 6. FastEndpoints Specifics

**The reference uses FastEndpoints** (Auth.API). It uses the **modern `Send.*` API** — specifically `Send.OkAsync(...)`.

`src/Services/Auth/Auth.API/Features/Users/CreateAccount/CreateUserEndpoint.cs:47`:
```csharp
await Send.OkAsync(new CreateUserResponse(command.Email, user.Id, resetPasswordToken));
```

`src/Services/Auth/Auth.API/Features/Users/Login/LoginEndpoint.cs:39`:
```csharp
await Send.OkAsync(new LoginResponse(command.Email, jwtToken, refreshToken.Token));
```

`src/Services/Auth/Auth.API/Features/Users/SetPassword/SetPasswordEndpoint.cs:20`:
```csharp
await Send.OkAsync(new SetPasswordResponse(command.Email));
```

The older `SendOkAsync()` instance method is **not used anywhere** in the reference codebase.

### Configure() split into partial class

Some endpoints split routing config into a `*.Configure.cs` partial file:

`src/Services/Auth/Auth.API/Features/Users/SetPassword/SetPasswordEndpoint.Configure.cs`:
```csharp
public partial class SetPasswordEndpoint
{
    public override void Configure()
    {
        AllowAnonymous();
        Post("/password/first-time", "/password/reset");
        Description(x => x
            .WithSummary("Set or reset user password")
            .WithTags("Password")
            .Produces<SetPasswordResponse>(StatusCodes.Status200OK)
            ...);
    }
}
```

Most endpoints use **attribute routing** (`[HttpPost("users")]`, `[Authorize(...)]`, `[Tags(...)]`) instead of `Configure()`. The partial-class `Configure.cs` pattern is used when more complex routing is needed (e.g., multiple routes, `AllowAnonymous()`, `Description()`).

### `UseCustomFastEndpoints` wrapper

`src/BuildingBlocks/Blocks.FastEndpoints/Extensions.cs:9-38`: wraps `UseFastEndpoints` with shared config — route prefix `"api"`, JSON enum converter, custom error response builder. Called in Program.cs after `UseAuthorization()`.

### Validators

FastEndpoints auto-discovers `AbstractValidator<T>` implementations from registered assemblies. No explicit validator DI registration needed in the API layer.

### Domain event publishing

`src/BuildingBlocks/Blocks.FastEndpoints/DomainEventPublisher.cs` — `IDomainEventPublisher` is registered in `AddApiServices`. Endpoints call `await PublishAsync(new UserCreated(...))` (FastEndpoints `IEventHandler` bus) for in-process domain events.
