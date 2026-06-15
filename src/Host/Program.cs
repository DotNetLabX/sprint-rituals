using Blocks.AspNetCore.Middlewares;
using Blocks.EntityFrameworkCore.Extensions;
using FastEndpoints;
using Fokus.API;
using Fokus.Persistence;
using Identity.API;
using Identity.API.Auth;
using Identity.Persistence;
using MassTransit;
using Reflekt.API;
using Reflekt.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// === Add ===
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddFokusModule(builder.Configuration);
builder.Services.AddReflektModule(builder.Configuration);

// MassTransit — single in-memory bus shared across all modules (ADR-019, Step 4).
// Each module exposes AddXxxMessaging(cfg) to register its consumers onto the shared bus.
builder.Services.AddMassTransit(cfg =>
{
    cfg.AddFokusMessaging();
    cfg.AddReflektMessaging();
    cfg.UsingInMemory((context, busCfg) => busCfg.ConfigureEndpoints(context));
});
builder.Services.AddOpenApi();
builder.Services.AddFastEndpoints(o =>
{
    o.Assemblies =
    [
        typeof(Identity.API.DependencyInjection).Assembly,
        typeof(Fokus.API.DependencyInjection).Assembly,
        typeof(Reflekt.API.DependencyInjection).Assembly,
    ];
});

var app = builder.Build();

// === Init ===
app.Migrate<IdentityDbContext>();
app.Migrate<FokusDbContext>();
app.Migrate<ReflektDbContext>();
await app.Services.SeedFokusDataAsync();
await app.Services.SeedReflektDataAsync();

// === Use ===
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapIdentityEndpoints();
app.MapIdentityGrpcServices();

app.UseFastEndpoints(c =>
{
    c.Endpoints.Configurator = ep =>
    {
        ep.PreProcessor<ActiveUserPreProcessor>(Order.Before);
    };
});

app.UseFokusMiddleware();
app.MapFokusEndpoints();
app.MapReflektEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapFallbackToFile("index.html");

app.Run();


