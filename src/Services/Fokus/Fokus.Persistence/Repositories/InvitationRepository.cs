namespace Fokus.Persistence.Repositories;

public class InvitationRepository(FokusDbContext db)
{
    public async Task<Invitation?> GetByTokenAsync(string token, CancellationToken ct = default) =>
        await db.Invitations.SingleOrDefaultAsync(i => i.Token == token, ct);

    public async Task<Invitation?> GetPendingByEmailAsync(string email, CancellationToken ct = default) =>
        await db.Invitations.SingleOrDefaultAsync(
            i => i.Email.ToLower() == email.ToLower() && i.Status == "Pending", ct);

    public async Task<List<Invitation>> GetAllAsync(CancellationToken ct = default) =>
        await db.Invitations.OrderByDescending(i => i.CreatedAt).ToListAsync(ct);

    public async Task<Invitation?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await db.Invitations.SingleOrDefaultAsync(i => i.Id == id, ct);

    public async Task AddAsync(Invitation invitation, CancellationToken ct = default) =>
        await db.Invitations.AddAsync(invitation, ct);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        await db.SaveChangesAsync(ct);
}
