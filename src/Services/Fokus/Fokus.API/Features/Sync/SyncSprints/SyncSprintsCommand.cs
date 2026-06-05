using Fokus.API.Features.Sync;

namespace Fokus.API.Features.Sync.SyncSprints;

public class SyncSprintsCommand
{
    public int FromSprintId { get; set; }
    public int ToSprintId { get; set; }
}

public class XraySyncSummary
{
    public int TestExecutionsSynced { get; set; }
    public int TestRunsSynced { get; set; }
    public int TestSetsSynced { get; set; }
    public string[] Warnings { get; set; } = [];
}

public class SyncSprintsResponse
{
    public int SprintsAttempted { get; set; }
    public int SprintsSynced { get; set; }
    public int TicketsUpserted { get; set; }
    public int DevelopersDiscovered { get; set; }
    public List<SprintSyncFailure> Failures { get; set; } = [];
    public XraySyncSummary? Xray { get; set; }
}

public class SyncSprintsCommandValidator : Validator<SyncSprintsCommand>
{
    public SyncSprintsCommandValidator()
    {
        RuleFor(x => x.FromSprintId)
            .GreaterThan(0)
            .WithMessage("FromSprintId must be greater than 0.");

        RuleFor(x => x.ToSprintId)
            .GreaterThan(0)
            .WithMessage("ToSprintId must be greater than 0.");
    }
}
