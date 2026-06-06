using Jira.Contracts;

namespace Fokus.API.Features.Sprints.UpdateSprint;

[HttpPut("/api/sprints/{sprintId}")]
[Tags("Sprints")]
[Authorize(Roles = "Admin")]
public class UpdateSprintEndpoint(IJiraClient jiraClient, SprintRepository sprintRepository)
    : Endpoint<UpdateSprintCommand, UpdateSprintResponse>
{
    public override async Task HandleAsync(UpdateSprintCommand req, CancellationToken ct)
    {
        var sprint = await sprintRepository.GetByIdAsync(req.SprintId, ct);
        if (sprint is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await jiraClient.UpdateSprintAsync(sprint.Id, req.Name, req.StartDate, req.EndDate, req.Goal, ct);

        sprint.Name = req.Name;
        sprint.StartDate = req.StartDate;
        sprint.EndDate = req.EndDate;
        sprint.Goal = req.Goal;

        await sprintRepository.SaveChangesAsync(ct);

        await Send.OkAsync(new UpdateSprintResponse
        {
            Id = sprint.Id,
            Name = sprint.Name,
            StartDate = sprint.StartDate,
            EndDate = sprint.EndDate,
            Goal = sprint.Goal,
            State = sprint.State.ToString()
        }, ct);
    }
}
