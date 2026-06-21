using Blocks.AspNetCore.Middlewares;
using Blocks.EntityFrameworkCore.Extensions;
using FastEndpoints;
using Fokus.API;
using Fokus.Persistence;
using Identity.API;
using Identity.API.Auth;
using Identity.Persistence;
using MassTransit;
using Poker.API;
using Poker.Persistence;
using Reflekt.API;
using Reflekt.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// === Add ===
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddFokusModule(builder.Configuration);
builder.Services.AddReflektModule(builder.Configuration);
builder.Services.AddPokerModule(builder.Configuration);

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
        typeof(Poker.API.DependencyInjection).Assembly,
    ];
});

var app = builder.Build();

// === Init ===
app.Migrate<IdentityDbContext>();
app.Migrate<FokusDbContext>();
app.Migrate<ReflektDbContext>();
app.Migrate<PokerDbContext>();
await app.Services.SeedFokusDataAsync();
await app.Services.SeedReflektDataAsync();

// === Use ===
app.UseMiddleware<GlobalExceptionMiddleware>();

// Per-prefix static file serving — each SPA lives in its own wwwroot subfolder.
// Registration order matters: static files before auth so assets are served without cookie checks.
app.UseStaticFiles(new StaticFileOptions
{
    RequestPath = "/dashboard",
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(builder.Environment.WebRootPath, "dashboard"))
});
app.UseStaticFiles(new StaticFileOptions
{
    RequestPath = "/retro",
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(builder.Environment.WebRootPath, "retro"))
});
app.UseStaticFiles(new StaticFileOptions
{
    RequestPath = "/poker",
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(builder.Environment.WebRootPath, "poker"))
});

app.UseAuthentication();
app.UseAuthorization();

// API + hub endpoints registered before any SPA fallback so /api/*, /auth/*, /hubs/retro always match first.
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
app.MapPokerEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Per-prefix SPA fallbacks — deep links under each prefix serve the right index.html on hard refresh.
// Registered after all API/hub routes so /api/*, /auth/*, /hubs/* are never swallowed.
app.MapFallbackToFile("/dashboard/{*path:nonfile}", "dashboard/index.html");
app.MapFallbackToFile("/retro/{*path:nonfile}", "retro/index.html");
app.MapFallbackToFile("/poker/{*path:nonfile}", "poker/index.html");

// Thin routing shell at /: redirect signed-in users to /dashboard, unauthenticated to /dashboard/login.
// Default is dashboard login — per plan Step 4 open question resolved to dashboard as the default module.
app.MapGet("/", (HttpContext ctx) =>
    ctx.User.Identity?.IsAuthenticated == true
        ? Results.Redirect("/dashboard/")
        : Results.Redirect("/dashboard/login"))
    .ExcludeFromDescription();

app.Run();


