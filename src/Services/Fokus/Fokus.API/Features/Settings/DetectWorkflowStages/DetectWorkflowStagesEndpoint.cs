namespace Fokus.API.Features.Settings.DetectWorkflowStages;

[HttpGet("/api/settings/workflow-stages/detect")]
[Tags("Settings")]
public class DetectWorkflowStagesEndpoint(
    TicketRepository ticketRepository,
    AppSettingsRepository appSettingsRepository,
    WorkflowDetectionService detectionService)
    : EndpointWithoutRequest<DetectWorkflowStagesResponse>
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        var edgeData = await ticketRepository.GetTransitionEdgesForDetectionAsync(ct);
        var settings = await appSettingsRepository.GetAsync(ct);

        var result = detectionService.Detect(edgeData, settings.DoneStatuses);

        await SendOkAsync(new DetectWorkflowStagesResponse
        {
            Stages = result.Stages,
            Sidelined = result.Sidelined,
            Confidence = new DetectionConfidence
            {
                TransitionCount = result.TransitionCount,
                TicketCount = result.TicketCount,
                SprintCount = result.SprintCount
            }
        }, ct);
    }
}
