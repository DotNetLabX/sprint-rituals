using Microsoft.EntityFrameworkCore;
using Reflekt.Domain.Participants;

namespace Reflekt.Persistence.Repositories;

public sealed class ParticipantRepository(ReflektDbContext db) : Repository<Participant>(db)
{
    public Task<Participant?> GetByGuestTokenAsync(string guestToken, CancellationToken ct)
        => Db.Participants
            .SingleOrDefaultAsync(p => p.GuestToken == guestToken, ct);

    public Task<List<Participant>> GetByRetroSessionIdAsync(int retroSessionId, CancellationToken ct)
        => Db.Participants
            .Where(p => p.RetroSessionId == retroSessionId)
            .OrderBy(p => p.JoinedAt)
            .ToListAsync(ct);

    public Task<bool> ExistsByDisplayNameAsync(int retroSessionId, string displayName, CancellationToken ct)
        => Db.Participants
            .AnyAsync(p => p.RetroSessionId == retroSessionId && p.DisplayName.ToLower() == displayName.ToLower(), ct);

    // Authenticated suite members are keyed by Identity Person, not display name — used to make the
    // join idempotent so a returning member (reload, or re-opening a closed retro) re-enters cleanly.
    public Task<Participant?> FindByPersonIdAsync(int retroSessionId, int personId, CancellationToken ct)
        => Db.Participants
            .SingleOrDefaultAsync(p => p.RetroSessionId == retroSessionId && p.PersonId == personId, ct);

    public void Remove(Participant participant) => Db.Participants.Remove(participant);
}
