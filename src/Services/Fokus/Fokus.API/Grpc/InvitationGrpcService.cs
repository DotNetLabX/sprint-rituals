using Blocks.Contracts.Grpc;
using Blocks.Contracts.Grpc.DataContracts;
using Fokus.Persistence.Repositories;

namespace Fokus.API.Grpc;

/// <summary>
/// gRPC server for synchronous invitation redemption.
/// Fokus = server (owns the Invitation domain). Identity = client (calls from the Google auth callback).
/// One atomic call: validate usability, enforce case-insensitive email match,
/// call Invitation.Accept(now) on success, return outcome + role.
/// </summary>
public class InvitationGrpcService(InvitationRepository invitationRepository) : IInvitationService
{
    public async Task<RedeemInvitationResponse> RedeemAsync(RedeemInvitationRequest request, CancellationToken ct = default)
    {
        var invitation = await invitationRepository.GetByTokenAsync(request.Token, ct);

        // Pre-checks resolve all failure outcomes before calling Accept.
        // Accept itself throws DomainException on invalid state — pre-checks ensure it never reaches that.
        if (invitation is null)
            return Fail(InvitationRedeemOutcome.NotFound);

        if (invitation.Status == "Accepted")
            return Fail(InvitationRedeemOutcome.Used);

        if (invitation.Status == "Revoked")
            return Fail(InvitationRedeemOutcome.Revoked);

        if (invitation.IsExpired)
            return Fail(InvitationRedeemOutcome.Expired);

        // EmailMismatch is a redeem-only check — the validate endpoint never produces this outcome.
        if (!string.Equals(invitation.Email, request.SigningInEmail, StringComparison.OrdinalIgnoreCase))
            return Fail(InvitationRedeemOutcome.EmailMismatch);

        // All pre-checks passed — Accept will not throw.
        invitation.Accept(DateTime.UtcNow);
        await invitationRepository.SaveChangesAsync(ct);

        return new RedeemInvitationResponse
        {
            Outcome = InvitationRedeemOutcome.Success,
            Role = invitation.Role,
            InvitedEmail = invitation.Email,
        };
    }

    private static RedeemInvitationResponse Fail(InvitationRedeemOutcome outcome) =>
        new() { Outcome = outcome };
}
