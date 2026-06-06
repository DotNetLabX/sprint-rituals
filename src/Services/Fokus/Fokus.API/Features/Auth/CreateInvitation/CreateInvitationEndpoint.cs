using System.Security.Claims;

namespace Fokus.API.Features.Auth.CreateInvitation;

[HttpPost("/api/invitations")]
[Tags("Invitations")]
[Authorize(Roles = "Admin")]
public class CreateInvitationEndpoint(
    InvitationRepository invitationRepository,
    AppSettingsRepository appSettingsRepository)
    : Endpoint<CreateInvitationCommand, CreateInvitationResponse>
{
    public override async Task HandleAsync(CreateInvitationCommand req, CancellationToken ct)
    {
        var settings = await appSettingsRepository.GetAsync(ct);
        var email = req.Email.Trim().ToLowerInvariant();
        var role = req.Role ?? "Manager";

        if (settings.CompanyDomain is not null)
        {
            var emailDomain = email.Contains('@') ? email.Split('@')[1] : string.Empty;
            if (!string.Equals(emailDomain, settings.CompanyDomain, StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException($"Email must belong to the company domain '{settings.CompanyDomain}'.");
        }

        // Check for existing pending invitation
        var existingInvitation = await invitationRepository.GetPendingByEmailAsync(email, ct);
        if (existingInvitation is not null)
            throw new ConflictException("A pending invitation for this email already exists.");

        // inviterId read from cookie claim (Identity.User.Id via ClaimTypes.NameIdentifier)
        var inviterIdStr = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0";
        var inviterId = int.TryParse(inviterIdStr, out var parsed) ? parsed : 0;

        var invitation = Fokus.Domain.Auth.Invitation.Create(email, role, inviterId);
        await invitationRepository.AddAsync(invitation, ct);
        await invitationRepository.SaveChangesAsync(ct);

        var inviteLink = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/invite/{invitation.Token}";

        await Send.ResponseAsync(new CreateInvitationResponse
        {
            Id = invitation.Id,
            Email = invitation.Email,
            Role = invitation.Role,
            InviteLink = inviteLink,
            ExpiresAt = invitation.ExpiresAt
        }, 201, ct);
    }
}
