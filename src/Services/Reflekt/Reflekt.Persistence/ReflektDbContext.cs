
using Microsoft.EntityFrameworkCore;
using Reflekt.Domain.CardGroups;
using Reflekt.Domain.Cards;
using Reflekt.Domain.Participants;
using Reflekt.Domain.Sprints;
using Reflekt.Domain.Teams;
using Reflekt.Domain.Users;
using Reflekt.Domain.Votes;

namespace Reflekt.Persistence;

public class ReflektDbContext(DbContextOptions<ReflektDbContext> options) : ApplicationDbContext<ReflektDbContext>(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Sprint> Sprints => Set<Sprint>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<CardGroup> CardGroups => Set<CardGroup>();
    public DbSet<Vote> Votes => Set<Vote>();
    public DbSet<EvidenceLink> EvidenceLinks => Set<EvidenceLink>();
    public DbSet<PhaseAction> PhaseActions => Set<PhaseAction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReflektDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
