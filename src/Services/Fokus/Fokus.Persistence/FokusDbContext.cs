using Blocks.EntityFrameworkCore;

namespace Fokus.Persistence;

public class FokusDbContext(DbContextOptions<FokusDbContext> options)
    : ApplicationDbContext<FokusDbContext>(options)
{
    public DbSet<Sprint> Sprints => Set<Sprint>();
    public DbSet<Developer> Developers => Set<Developer>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<SprintMembership> SprintMemberships => Set<SprintMembership>();
    public DbSet<StatusTransition> StatusTransitions => Set<StatusTransition>();
    public DbSet<AppSettings> AppSettings => Set<AppSettings>();
    public DbSet<DeveloperSprintCapacity> DeveloperSprintCapacities => Set<DeveloperSprintCapacity>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<TestExecution> TestExecutions => Set<TestExecution>();
    public DbSet<TestExecutionLink> TestExecutionLinks => Set<TestExecutionLink>();
    public DbSet<TestRun> TestRuns => Set<TestRun>();
    public DbSet<TestSet> TestSets => Set<TestSet>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FokusDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
