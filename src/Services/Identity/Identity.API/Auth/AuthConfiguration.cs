using System.Security.Claims;
using Blocks.Contracts.Grpc;
using Blocks.Contracts.Grpc.DataContracts;
using Grpc.Core;
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
        var (firstName, lastName) = ExtractName(principal, email);

        var inviteToken = context.Properties?.Items.TryGetValue("invite_token", out var token) == true ? token : null;

        Identity.Domain.Users.User? user;

        if (!string.IsNullOrEmpty(inviteToken))
        {
            await HandleInviteRedemptionAsync(
                context, sp, personRepository, userRepository,
                googleId, email, firstName, lastName, pictureUrl, inviteToken);
            return;
        }

        var userCount = await userRepository.CountAsync();

        if (userCount == 0)
        {
            user = await HandleBootstrapAsync(personRepository, userRepository, googleId, firstName, lastName, email, pictureUrl);
        }
        else
        {
            user = await HandleRegularLoginAsync(context, personRepository, userRepository, googleId, email, firstName, lastName, pictureUrl);
            if (user is null) return;
        }

        await SignInAsync(context, personRepository, user!);
    }

    /// <summary>
    /// Handles the invite-token branch. Always completes the response (error redirect or cookie sign-in).
    /// Never returns without writing a response — caller must return immediately after awaiting this.
    /// </summary>
    private static async Task HandleInviteRedemptionAsync(
        TicketReceivedContext context,
        IServiceProvider sp,
        PersonRepository personRepository,
        UserRepository userRepository,
        string googleId,
        string email,
        string firstName,
        string lastName,
        string? pictureUrl,
        string inviteToken)
    {
        // Already-registered user path: never create a second User for the same PersonId (H2).
        var existingPerson = await personRepository.GetByExternalIdAsync(googleId)
                             ?? await personRepository.GetByEmailAsync(email);
        if (existingPerson is not null)
        {
            var existingUser = await userRepository.GetByPersonIdAsync(existingPerson.Id);
            if (existingUser is not null)
            {
                if (existingUser.IsActive)
                {
                    // Active user with invite token: ignore token, log in normally (Q5).
                    existingPerson.UpdateFromGoogle(firstName, lastName, pictureUrl);
                    existingUser.RecordLogin();
                    await personRepository.SaveChangesAsync();
                    await SignInAsync(context, personRepository, existingUser);
                    return;
                }
                else
                {
                    // Inactive user: do NOT silently reactivate via invite — reject deterministically.
                    context.Response.Redirect("/invite/error?reason=account-inactive");
                    context.HandleResponse();
                    return;
                }
            }
        }

        // Call Fokus's IInvitationService gRPC server atomically: validate + email-match + Accept.
        var invitationService = sp.GetRequiredService<IInvitationService>();
        RedeemInvitationResponse result;
        try
        {
            result = await invitationService.RedeemAsync(
                new RedeemInvitationRequest { Token = inviteToken, SigningInEmail = email });
        }
        catch (RpcException)
        {
            // Transport or server failure — redirect to a safe error page; never let an exception
            // escape the OAuth callback (M1).
            context.Response.Redirect("/invite/error?reason=error");
            context.HandleResponse();
            return;
        }

        if (result.Outcome != InvitationRedeemOutcome.Success)
        {
            var reason = OutcomeToReason(result.Outcome);
            context.Response.Redirect($"/invite/error?reason={reason}");
            context.HandleResponse();
            return;
        }

        // Provision the user with the invitation's role.
        var person = await ResolveOrCreatePersonAsync(personRepository, googleId, firstName, lastName, email, pictureUrl);
        var user = Identity.Domain.Users.User.CreateFromPerson(person.Id, result.Role ?? "Manager");
        await userRepository.AddAsync(user);
        await userRepository.SaveChangesAsync();

        // Raise UserCreated AFTER save so CountActiveAdminsAsync includes the new user.
        user.RaiseCreated();
        await userRepository.SaveChangesAsync();

        person.LinkUser(user.Id);
        await personRepository.SaveChangesAsync();

        await SignInAsync(context, personRepository, user);
    }

    private static async Task<Identity.Domain.Users.User> HandleBootstrapAsync(
        PersonRepository personRepository,
        UserRepository userRepository,
        string googleId,
        string firstName,
        string lastName,
        string email,
        string? pictureUrl)
    {
        var person = Identity.Domain.Persons.Person.CreateFromGoogle(googleId, firstName, lastName, email, pictureUrl);
        await personRepository.AddAsync(person);
        await personRepository.SaveChangesAsync();
        person.RaiseCreated();
        await personRepository.SaveChangesAsync();

        var user = Identity.Domain.Users.User.CreateFromPerson(person.Id, "Admin");
        await userRepository.AddAsync(user);
        await userRepository.SaveChangesAsync();

        person.LinkUser(user.Id);
        await personRepository.SaveChangesAsync();

        // Raise UserCreated AFTER save so CountActiveAdminsAsync == 1 when the handler fires.
        user.RaiseCreated();
        await userRepository.SaveChangesAsync();

        return user;
    }

    private static async Task<Identity.Domain.Users.User?> HandleRegularLoginAsync(
        TicketReceivedContext context,
        PersonRepository personRepository,
        UserRepository userRepository,
        string googleId,
        string email,
        string firstName,
        string lastName,
        string? pictureUrl)
    {
        var person = await personRepository.GetByExternalIdAsync(googleId)
                     ?? await personRepository.GetByEmailAsync(email);

        if (person is null)
        {
            context.Response.Redirect("/login?error=no-account");
            context.HandleResponse();
            return null;
        }

        var user = await userRepository.GetByPersonIdAsync(person.Id);

        if (user is null)
        {
            context.Response.Redirect("/login?error=no-account");
            context.HandleResponse();
            return null;
        }

        if (!user.IsActive)
        {
            context.Response.Redirect("/login?error=deactivated");
            context.HandleResponse();
            return null;
        }

        person.UpdateFromGoogle(firstName, lastName, pictureUrl);
        user.RecordLogin();
        await personRepository.SaveChangesAsync();
        return user;
    }

    /// <summary>Reuses an existing Person for the invited email, or creates a new one (Q4 / AC 5).</summary>
    private static async Task<Identity.Domain.Persons.Person> ResolveOrCreatePersonAsync(
        PersonRepository personRepository,
        string googleId,
        string firstName,
        string lastName,
        string email,
        string? pictureUrl)
    {
        // Check by ExternalId first (Google account may have been Jira-synced already).
        var person = await personRepository.GetByExternalIdAsync(googleId)
                     ?? await personRepository.GetByEmailAsync(email);

        if (person is not null)
            return person;

        person = Identity.Domain.Persons.Person.CreateFromGoogle(googleId, firstName, lastName, email, pictureUrl);
        await personRepository.AddAsync(person);
        await personRepository.SaveChangesAsync();
        person.RaiseCreated();
        await personRepository.SaveChangesAsync();
        return person;
    }

    private static async Task SignInAsync(
        TicketReceivedContext context,
        PersonRepository personRepository,
        Identity.Domain.Users.User user)
    {
        var claimPerson = await personRepository.GetByIdAsync(user.PersonId);
        if (claimPerson is null)
        {
            context.Response.Redirect("/login?error=no-account");
            context.HandleResponse();
            return;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new("person_id", user.PersonId.ToString()),
            new(ClaimTypes.Email, claimPerson.Email ?? string.Empty),
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

    private static (string firstName, string lastName) ExtractName(System.Security.Claims.ClaimsPrincipal principal, string email)
    {
        var firstName = principal.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty;
        var lastName = principal.FindFirstValue(ClaimTypes.Surname) ?? string.Empty;

        if (!string.IsNullOrEmpty(firstName) || !string.IsNullOrEmpty(lastName))
            return (firstName, lastName);

        var fullNameClaim = principal.FindFirstValue(ClaimTypes.Name) ?? email;
        var spaceIdx = fullNameClaim.IndexOf(' ');
        return spaceIdx >= 0
            ? (fullNameClaim[..spaceIdx], fullNameClaim[(spaceIdx + 1)..])
            : (fullNameClaim, string.Empty);
    }

    private static string OutcomeToReason(InvitationRedeemOutcome outcome) => outcome switch
    {
        InvitationRedeemOutcome.NotFound => "not-found",
        InvitationRedeemOutcome.Used => "used",
        InvitationRedeemOutcome.Revoked => "revoked",
        InvitationRedeemOutcome.Expired => "expired",
        InvitationRedeemOutcome.EmailMismatch => "email-mismatch",
        _ => "error"
    };
}
