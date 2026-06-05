using Blocks.AspNetCore.Middlewares;
using FastEndpoints;
using Fokus.API;
using Fokus.Persistence;
using Identity.API;
using Identity.API.Auth;
using Identity.API.Features.Auth.Login;
using Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Xray.GraphQL;

var builder = WebApplication.CreateBuilder(args);

// Module registrations
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddFokusModule(builder.Configuration);

// OpenAPI (gated to Development below)
builder.Services.AddOpenApi();

// FastEndpoints — both module assemblies
builder.Services.AddFastEndpoints(o =>
{
    o.Assemblies =
    [
        typeof(Identity.API.DependencyInjection).Assembly,
        typeof(Fokus.API.DependencyInjection).Assembly
    ];
});

var app = builder.Build();

// Ensure data directory exists and run migrations — Identity first, then Fokus
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;

    // Identity DB
    var identityDb = sp.GetRequiredService<IdentityDbContext>();
    var identityConnStr = identityDb.Database.GetConnectionString();
    if (identityConnStr is not null)
    {
        var identityPath = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(identityConnStr).DataSource;
        var identityDir = Path.GetDirectoryName(Path.IsPathRooted(identityPath)
            ? identityPath
            : Path.Combine(Directory.GetCurrentDirectory(), identityPath));
        if (!string.IsNullOrEmpty(identityDir)) Directory.CreateDirectory(identityDir);
    }
    await identityDb.Database.MigrateAsync();

    // Fokus DB
    var fokusDb = sp.GetRequiredService<FokusDbContext>();
    var fokusConnStr = fokusDb.Database.GetConnectionString();
    if (fokusConnStr is not null)
    {
        var fokusPath = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(fokusConnStr).DataSource;
        var fokusDir = Path.GetDirectoryName(Path.IsPathRooted(fokusPath)
            ? fokusPath
            : Path.Combine(Directory.GetCurrentDirectory(), fokusPath));
        if (!string.IsNullOrEmpty(fokusDir)) Directory.CreateDirectory(fokusDir);
    }
    await fokusDb.Database.MigrateAsync();

    // Seed Fokus AppSettings default row if missing
    if (!await fokusDb.AppSettings.AnyAsync())
    {
        fokusDb.AppSettings.Add(Fokus.Domain.AppSettings.CreateDefault());
        await fokusDb.SaveChangesAsync();
    }

    // Seed Xray settings from config if present
    var xrayOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<XrayOptions>>().Value;
    if (!string.IsNullOrEmpty(xrayOptions.ClientId) && !string.IsNullOrEmpty(xrayOptions.ClientSecret))
    {
        var settings = await fokusDb.AppSettings.FirstAsync();
        settings.XrayEnabled = true;
        settings.XrayClientId = xrayOptions.ClientId;
        settings.XrayClientSecret = xrayOptions.ClientSecret;
        await fokusDb.SaveChangesAsync();
    }
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

// Login endpoint (minimal API — Google OAuth challenge)
LoginEndpoint.MapLoginEndpoint(app);

// FastEndpoints — global ActiveUserPreProcessor
app.UseFastEndpoints(c =>
{
    c.Endpoints.Configurator = ep =>
    {
        ep.PreProcessor<ActiveUserPreProcessor>(Order.Before);
    };
});

// Fokus SignalR hub
app.UseFokusMiddleware();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapFallbackToFile("index.html");

app.Run();
