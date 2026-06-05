using Jira.Contracts;

namespace Fokus.API.Features.Sync.GetBoards;

[HttpGet("/api/boards")]
[Tags("Sync")]
public class GetBoardsEndpoint(IJiraClient jiraClient)
    : EndpointWithoutRequest<GetBoardsResponse>
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        var boards = await jiraClient.GetBoardsAsync(ct);

        await SendOkAsync(new GetBoardsResponse
        {
            Boards = boards.Select(b => new BoardDto
            {
                Id = b.Id,
                Name = b.Name,
                Type = b.Type
            }).ToList()
        }, ct);
    }
}
