namespace Fokus.API.Features.Settings.SaveWorkflowStages;

public class SaveWorkflowStagesRequest
{
    public List<string> Stages { get; set; } = [];
}

public class SaveWorkflowStagesRequestValidator : Validator<SaveWorkflowStagesRequest>
{
    public SaveWorkflowStagesRequestValidator()
    {
        RuleForEach(x => x.Stages)
            .NotEmpty()
            .WithMessage("Stage must not be empty or blank.");
    }
}

[HttpPut("/api/settings/workflow-stages")]
[Tags("Settings")]
[Authorize(Roles = "Admin")]
public class SaveWorkflowStagesEndpoint(AppSettingsRepository repository)
    : Endpoint<SaveWorkflowStagesRequest, List<string>>
{
    public override async Task HandleAsync(SaveWorkflowStagesRequest req, CancellationToken ct)
    {
        var existing = await repository.GetAsync(ct);
        existing.WorkflowStages = req.Stages;
        await repository.SaveAsync(existing, ct);
        await SendOkAsync(existing.WorkflowStages, ct);
    }
}
