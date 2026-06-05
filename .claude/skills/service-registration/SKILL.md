# service-registration

Service DI registration — layer structure, where each dependency type goes, Host composition for modular monolith. Loaded when registering dependencies, adding services to the Host, or modifying DependencyInjection.cs files.

## Reference

`src/Services/` — all services follow this pattern. Fokus (FastEndpoints) demonstrates the standard DI structure with API + Persistence layers (no Application project).

## Layer Structure

Each service has up to 3 DI layers, registered in strict order:

```
1. AddApiServices(IServiceCollection, IConfiguration)         ← API project
2. AddApplicationServices(IServiceCollection, IConfiguration) ← Application project (if exists)
3. AddPersistenceServices(IServiceCollection, IConfiguration) ← Persistence project
```

If the service has no Application project (e.g., FastEndpoints with handler logic in endpoints), skip layer 2. Only layers 1 and 3 remain.

Each layer lives in its own `DependencyInjection.cs` static class within the respective project.

## What Goes Where

### API layer (`AddApiServices`)

- Endpoint framework registration (`AddFastEndpoints`, `AddCarter`, etc.)
- OpenAPI / Swagger registration
- Authentication + Authorization
- `IClaimsProvider` / `IRouteProvider` / `HttpContextProvider`
- gRPC **clients** (`AddCodeFirstGrpcClient<T>`)
- gRPC **server** registration (`AddCodeFirstGrpc`)
- Authorization handlers
- `RequestContext` (scoped)
- External HTTP clients
- File storage services
- Email services

### Application layer (`AddApplicationServices`)

- MediatR registration + open pipeline behaviors (Validation, Logging, AssignUserId)
- FluentValidation assembly scanning (`AddValidatorsFromAssemblyContaining<T>`)
- Mapster configuration (`AddMapsterConfigsFromAssemblyContaining<T>`)
- MassTransit + RabbitMQ (`AddMassTransitWithRabbitMQ`)
- `IDomainEventPublisher`
- Domain service factories (e.g., state machine factories)
- Access checkers / domain-level authorization

### Persistence layer (`AddPersistenceServices`)

- `DbContext` registration (with interceptors)
- `ISaveChangesInterceptor` (scoped — `DispatchDomainEventsInterceptor`)
- Shared `DbConnection` (if transaction provider needed)
- `TransactionProvider`
- `Repository<>` open generic + `AddDerivedTypesOf(typeof(Repository<>))`
- Named/specialized repositories (explicit registration overrides)
- `DatabaseCacheLoader` hosted service (if caching entity data)

## Registration Patterns

### DbContext (standard — SQL Server with shared connection)

```csharp
services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();
services.AddScoped<DbConnection>(provider => new SqlConnection(connectionString));
services.AddDbContext<{Service}DbContext>((provider, options) =>
{
    var dbConnection = provider.GetRequiredService<DbConnection>();
    options.AddInterceptors(provider.GetServices<ISaveChangesInterceptor>());
    options.UseSqlServer(dbConnection);
});
```

### DbContext (simple — SQLite, no shared connection)

```csharp
services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();
services.AddDbContext<{Service}DbContext>((provider, options) =>
{
    options.AddInterceptors(provider.GetServices<ISaveChangesInterceptor>());
    options.UseSqlite(configuration.GetConnectionString("{ConnectionName}"));
});
```

### DbContext (ASP.NET Core Identity)

```csharp
services.AddDbContext<{Service}DbContext>(opts => opts.UseSqlite(connectionString));
services.AddIdentityCore<User>(options => { /* lockout, password rules */ })
    .AddRoles<Role>()
    .AddEntityFrameworkStores<{Service}DbContext>()
    .AddSignInManager<SignInManager<User>>()
    .AddDefaultTokenProviders();
```

### Repositories

```csharp
services.AddScoped(typeof(Repository<>));
services.AddDerivedTypesOf(typeof(Repository<>));  // auto-registers all concrete subclasses
services.AddScoped<SpecialRepository>();           // explicit override if needed
```

### gRPC client

```csharp
var grpcOptions = config.GetSectionByTypeName<GrpcServicesOptions>();
services.AddCodeFirstGrpcClient<IPersonService>(grpcOptions, "Person");
```

### gRPC server (in Program.cs middleware section)

```csharp
app.MapGrpcService<PersonGrpcService>();
```

## Options Validation

Before layer registrations, validate options:

```csharp
public static void ConfigureApiOptions(this IServiceCollection services, IConfiguration config)
{
    services
        .AddAndValidateOptions<RabbitMqOptions>(config)
        .AddAndValidateOptions<TransactionOptions>(config);
}
```

Called first in Program.cs: `builder.Services.ConfigureApiOptions(builder.Configuration);`

## Modular Monolith: Host Composition

When services are composed into a single Host (not self-hosting):

### Service exposes a combined registration method

```csharp
// {Service}.API/DependencyInjection.cs
public static IServiceCollection Add{Service}Module(this IServiceCollection services, IConfiguration configuration)
{
    services.AddApiServices(configuration);        // internal
    services.AddPersistenceServices(configuration); // internal
    return services;
}

public static WebApplication Map{Service}Endpoints(this WebApplication app)
{
    // endpoint mapping specific to this service (e.g., MapLoginEndpoint for minimal API routes)
    return app;
}
```

### Host calls each service

```csharp
// Host/Program.cs — Add section
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddFokusModule(builder.Configuration);

// Host/Program.cs — InitData section
app.Migrate<IdentityDbContext>();
app.Migrate<FokusDbContext>();

// Host/Program.cs — Use section (shared pipeline)
// ... middleware ...
// ... endpoint framework with multi-assembly scanning ...

app.MapIdentityEndpoints();
app.MapFokusEndpoints();
```

### FastEndpoints multi-assembly scanning

When the Host uses FastEndpoints and endpoints live in service class libraries:

```csharp
app.UseFastEndpoints(c =>
{
    c.Assemblies = [
        typeof({Service1}.API.DependencyInjection).Assembly,
        typeof({Service2}.API.DependencyInjection).Assembly
    ];
});
```

## Program.cs Structure (self-hosted service)

Three sections, always in this order:

```csharp
// === Add ===
builder.Services.ConfigureApiOptions(builder.Configuration);
builder.Services
    .AddApiServices(builder.Configuration)
    .AddApplicationServices(builder.Configuration)
    .AddPersistenceServices(builder.Configuration);

var app = builder.Build();

// === InitData ===
app.Migrate<{Service}DbContext>();
if (app.Environment.IsDevelopment()) app.SeedTestData();

// === Use ===
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
// endpoint framework mapping
app.Run();
```

## Naming Conventions

| Item | Convention |
|------|-----------|
| DI class | `DependencyInjection` (static class, one per project) |
| API registration | `AddApiServices` |
| Application registration | `AddApplicationServices` |
| Persistence registration | `AddPersistenceServices` |
| Combined (modular monolith) | `Add{Service}Module` |
| Endpoint mapping | `Map{Service}Endpoints` |
| Options validation | `ConfigureApiOptions` |
| Return type | `IServiceCollection` (for chaining) |
| Parameters | `(this IServiceCollection services, IConfiguration configuration)` |

## When to Load This Skill

- Adding a new dependency to an existing service
- Creating a new service (alongside `create-service`)
- Wiring a service into the Host
- Moving registrations between layers
- Adding gRPC clients or servers
- Registering new repositories or interceptors
