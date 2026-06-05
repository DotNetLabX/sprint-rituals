namespace Fokus.API.Features.Sync.GetStatuses;

public class GetStatusesResponse
{
    public List<StatusDto> Statuses { get; set; } = [];
}

public class StatusDto
{
    public string Name { get; set; } = string.Empty;
    public string CategoryKey { get; set; } = string.Empty;
}
