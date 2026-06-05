namespace Fokus.API.Features.Settings.DetectWorkflowStages;

public class DetectWorkflowStagesResponse
{
    public List<string> Stages { get; set; } = new List<string>();
    public List<string> Sidelined { get; set; } = new List<string>();
    public DetectionConfidence Confidence { get; set; } = new DetectionConfidence();
}

public class DetectionConfidence
{
    public int TransitionCount { get; set; }
    public int TicketCount { get; set; }
    public int SprintCount { get; set; }
}
