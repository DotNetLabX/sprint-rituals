using Blocks.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Poker.Domain.Planning;
using Poker.Domain.Planning.Items;
using Poker.Domain.Planning.Participants;
using Poker.Domain.Planning.Votes;

namespace Poker.Persistence;

public class PokerDbContext(DbContextOptions<PokerDbContext> options) : ApplicationDbContext<PokerDbContext>(options)
{
    public DbSet<PlanningSession> PlanningSessions => Set<PlanningSession>();
    public DbSet<SessionParticipant> SessionParticipants => Set<SessionParticipant>();
    public DbSet<EstimationItem> EstimationItems => Set<EstimationItem>();
    public DbSet<Vote> Votes => Set<Vote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PokerDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
