namespace Fokus.API.Features.Xray.SyncXray;

public class SyncXrayCommand
{
    public int[] SprintIds { get; set; } = [];
}

public class SyncXrayCommandValidator : Validator<SyncXrayCommand>
{
    public SyncXrayCommandValidator()
    {
        // SprintIds may be empty — empty means "all synced sprints" (server resolves scope)
        RuleForEach(x => x.SprintIds)
            .GreaterThan(0)
            .WithMessage("Each sprint ID must be greater than 0.");
    }
}

public class SyncXrayResponse
{
    public int TestExecutionsSynced { get; set; }
    public int TestRunsSynced { get; set; }
    public int TestSetsSynced { get; set; }
    public string[] Warnings { get; set; } = [];
}
