using System.Security.Cryptography;
using Blocks.Domain.Exceptions;

namespace Fokus.Domain.Auth;

public partial class Invitation
{
    public static Invitation Create(string email, string role, int invitedByUserId)
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(tokenBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        var createdAt = DateTime.UtcNow;

        return new Invitation
        {
            Email = email,
            Role = role,
            InvitedByUserId = invitedByUserId,
            Token = token,
            CreatedAt = createdAt,
            ExpiresAt = createdAt.AddDays(7),
            Status = "Pending"
        };
    }

    public void Accept(DateTime now)
    {
        if (Status != "Pending")
            throw new DomainException("Invitation is not in Pending status.");
        if (now > ExpiresAt)
            throw new DomainException("Invitation has expired.");

        Status = "Accepted";
        AcceptedAt = now;
    }

    public void Revoke()
    {
        if (Status != "Pending")
            throw new DomainException("Only pending invitations can be revoked.");

        Status = "Revoked";
    }
}
