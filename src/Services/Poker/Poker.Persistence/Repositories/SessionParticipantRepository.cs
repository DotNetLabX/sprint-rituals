namespace Poker.Persistence.Repositories;

public class SessionParticipantRepository(PokerDbContext db)
    : RepositoryBase<PokerDbContext, SessionParticipant, Guid>(db)
{
    public async Task<SessionParticipant?> GetByGuestTokenAsync(string guestToken, CancellationToken ct = default)
        => await Entity.FirstOrDefaultAsync(p => p.GuestToken == guestToken, ct);

    public async Task<SessionParticipant?> GetByPersonIdInSessionAsync(
        Guid sessionId, int personId, CancellationToken ct = default)
        => await Entity.FirstOrDefaultAsync(
            p => p.SessionId == sessionId && p.PersonId == personId, ct);
}
