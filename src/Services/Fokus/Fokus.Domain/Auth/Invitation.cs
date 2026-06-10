namespace Fokus.Domain.Auth;

public partial class Invitation : Entity<int>
{
    public required string Email { get; set; }
    public string Role { get; set; } = "Manager";
    public int InvitedByUserId { get; set; }
    public required string Token { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// EF optimistic-concurrency token. Updated by EF on every save.
    /// Prevents two simultaneous redemption requests from both succeeding on the same invitation.
    /// </summary>
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();

    public bool IsExpired => Status == "Pending" && DateTime.UtcNow > ExpiresAt;
}
