using System.Security.Claims;
using Identity.Persistence;
using Identity.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.API.Auth;

public static class AuthConfiguration
{
    public static IServiceCollection AddIdentityAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthorization();

        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.SlidingExpiration = true;
                options.LoginPath = "/auth/login";
                options.Events.OnRedirectToLogin = ctx =>
                {
                    ctx.Response.StatusCode = 401;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = ctx =>
                {
                    ctx.Response.StatusCode = 403;
                    return Task.CompletedTask;
                };
            })
            .AddGoogle(options =>
            {
                configuration.GetSection("Google").Bind(options);
                options.CallbackPath = "/auth/callback";
                options.SaveTokens = false;
                options.Events.OnTicketReceived = async context =>
                {
                    await HandleTicketReceivedAsync(context);
                };
            });

        return services;
    }

    private static async Task HandleTicketReceivedAsync(TicketReceivedContext context)
    {
        var sp = context.HttpContext.RequestServices;
        var personRepository = sp.GetRequiredService<PersonRepository>();
        var userRepository = sp.GetRequiredService<UserRepository>();

        var principal = context.Principal!;
        var googleId = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var email = (principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty).ToLowerInvariant();
        var pictureUrl = principal.FindFirstValue("urn:google:picture");

        // Google name mapping (F3): given_name / family_name; fallback to splitting Name claim
        var firstName = principal.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty;
        var lastName = principal.FindFirstValue(ClaimTypes.Surname) ?? string.Empty;
        if (string.IsNullOrEmpty(firstName) && string.IsNullOrEmpty(lastName))
        {
            var fullNameClaim = principal.FindFirstValue(ClaimTypes.Name) ?? email;
            var spaceIdx = fullNameClaim.IndexOf(' ');
            if (spaceIdx >= 0)
            {
                firstName = fullNameClaim[..spaceIdx];
                lastName = fullNameClaim[(spaceIdx + 1)..];
            }
            else
            {
                firstName = fullNameClaim;
                lastName = string.Empty;
            }
        }

        // Read invite token from auth properties (deferred — scaffold stub)
        var inviteToken = context.Properties?.Items.TryGetValue("invite_token", out var token) == true ? token : null;

        Identity.Domain.Users.User? user;

        if (!string.IsNullOrEmpty(inviteToken))
        {
            // DEFERRED (infra increment): invite-token redemption path
            // Validate Invitation from Fokus module (cross-module plumbing not yet wired)
            context.Response.Redirect("/invite/error?reason=not-implemented");
            context.HandleResponse();
            return;
        }

        var userCount = await userRepository.CountAsync();

        if (userCount == 0)
        {
            // Bootstrap path — first user becomes Admin
            var person = Identity.Domain.Persons.Person.CreateFromGoogle(googleId, firstName, lastName, email, pictureUrl);
            await personRepository.AddAsync(person);
            await personRepository.SaveChangesAsync();
            // Raise PersonCreated after save — Id is now the real EF-assigned PK (not 0)
            person.RaiseCreated();
            await personRepository.SaveChangesAsync();

            user = Identity.Domain.Users.User.CreateFromPerson(person.Id, "Admin");
            await userRepository.AddAsync(user);
            await userRepository.SaveChangesAsync();

            person.LinkUser(user.Id);
            await personRepository.SaveChangesAsync();

            // DEFERRED (infra increment): CompanyDomain auto-seed (F1)
            // AppSettings lives in fokus.db (different module). Seed belongs on Fokus side
            // when invitation validation is wired. Do not create Identity→Fokus coupling here.
        }
        else
        {
            // Regular login path — find Person by ExternalId (GoogleId) then email fallback
            var person = await personRepository.GetByExternalIdAsync(googleId)
                         ?? await personRepository.GetByEmailAsync(email);

            if (person is null)
            {
                context.Response.Redirect("/login?error=no-account");
                context.HandleResponse();
                return;
            }

            user = await userRepository.GetByPersonIdAsync(person.Id);

            if (user is null)
            {
                context.Response.Redirect("/login?error=no-account");
                context.HandleResponse();
                return;
            }

            if (!user.IsActive)
            {
                context.Response.Redirect("/login?error=deactivated");
                context.HandleResponse();
                return;
            }

            // Update person name/picture from Google on each login
            person.UpdateFromGoogle(firstName, lastName, pictureUrl);
            user.RecordLogin();
            await personRepository.SaveChangesAsync();
        }

        // Fetch person for claims (need FullName, Email, PictureUrl)
        var claimPerson = await personRepository.GetByIdAsync(user.PersonId);
        if (claimPerson is null)
        {
            context.Response.Redirect("/login?error=no-account");
            context.HandleResponse();
            return;
        }

        // Build claims principal — DisplayName = Person.FullName (F3 parity)
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, claimPerson.Email),
            new(ClaimTypes.Name, claimPerson.FullName),
            new(ClaimTypes.Role, user.Role)
        };

        if (claimPerson.PictureUrl is not null)
            claims.Add(new Claim("avatar_url", claimPerson.PictureUrl));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var userPrincipal = new ClaimsPrincipal(identity);

        await context.HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            userPrincipal,
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30) });

        context.Response.Redirect("/");
        context.HandleResponse();
    }
}
