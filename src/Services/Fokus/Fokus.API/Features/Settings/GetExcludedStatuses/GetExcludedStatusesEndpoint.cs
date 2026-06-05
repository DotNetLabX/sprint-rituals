namespace Fokus.API.Features.Settings.GetExcludedStatuses;

[HttpGet("/api/settings/excluded-statuses")]
[Tags("Settings")]
public class GetExcludedStatusesEndpoint(AppSettingsRepository repository)
    : EndpointWithoutRequest<List<string>>
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        var settings = await repository.GetAsync(ct);
        await SendOkAsync(settings.ExcludedFromScopeStatuses, ct);
    }
}
