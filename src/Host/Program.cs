using Blocks.AspNetCore.Middlewares;
using Blocks.EntityFrameworkCore.Extensions;
using FastEndpoints;
using Fokus.API;
using Fokus.Persistence;
using Identity.API;
using Identity.API.Auth;
using Identity.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// === Add ===
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddFokusModule(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddFastEndpoints(o =>
{
    o.Assemblies =
    [
        typeof(Identity.API.DependencyInjection).Assembly,
        typeof(Fokus.API.DependencyInjection).Assembly
    ];
});

var app = builder.Build();

// === Init ===
app.Migrate<IdentityDbContext>();
app.Migrate<FokusDbContext>();
await app.Services.SeedFokusDataAsync();

// === Use ===
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapIdentityEndpoints();

app.UseFastEndpoints(c =>
{
    c.Endpoints.Configurator = ep =>
    {
        ep.PreProcessor<ActiveUserPreProcessor>(Order.Before);
    };
});

app.UseFokusMiddleware();
app.MapFokusEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapFallbackToFile("index.html");

app.Run();
