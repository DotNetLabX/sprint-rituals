namespace Fokus.API.Features.Sync.GetBoards;

public class GetBoardsResponse
{
    public List<BoardDto> Boards { get; set; } = [];
}

public class BoardDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}
