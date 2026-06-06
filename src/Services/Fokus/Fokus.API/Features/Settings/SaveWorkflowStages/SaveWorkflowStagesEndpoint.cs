namespace Fokus.API.Features.Settings.SaveWorkflowStages;

[HttpPut("/api/settings/workflow-stages")]
[Tags("Settings")]
[Authorize(Roles = "Admin")]
public class SaveWorkflowStagesEndpoint(AppSettingsRepository repository)
    : Endpoint<SaveWorkflowStagesCommand, List<string>>
{
    public override async Task HandleAsync(SaveWorkflowStagesCommand req, CancellationToken ct)
    {
        var existing = await repository.GetAsync(ct);
        existing.WorkflowStages = req.Stages;
        await repository.SaveAsync(existing, ct);
        await Send.OkAsync(existing.WorkflowStages, ct);
    }
}
