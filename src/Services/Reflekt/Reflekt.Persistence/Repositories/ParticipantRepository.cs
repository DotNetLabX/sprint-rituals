using Microsoft.EntityFrameworkCore;
using Reflekt.Domain.Participants;

namespace Reflekt.Persistence.Repositories;

public sealed class ParticipantRepository(ReflektDbContext db) : Repository<Participant>(db)
{
    public Task<Participant?> GetByGuestTokenAsync(string guestToken, CancellationToken ct)
        => Db.Participants
            .SingleOrDefaultAsync(p => p.GuestToken == guestToken, ct);

    public Task<List<Participant>> GetBySprintIdAsync(int sprintId, CancellationToken ct)
        => Db.Participants
            .Where(p => p.SprintId == sprintId)
            .OrderBy(p => p.JoinedAt)
            .ToListAsync(ct);

    public Task<bool> ExistsByDisplayNameAsync(int sprintId, string displayName, CancellationToken ct)
        => Db.Participants
            .AnyAsync(p => p.SprintId == sprintId && p.DisplayName.ToLower() == displayName.ToLower(), ct);

    public void Remove(Participant participant) => Db.Participants.Remove(participant);
}
