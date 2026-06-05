namespace Fokus.Domain;

public class AppSettings
{
    public int Id { get; set; } = 1;
    public int? BoardId { get; set; }
    public List<string> DoneStatuses { get; set; } = ["Done", "Closed"];
    public List<string> WorkflowStages { get; set; } = [];
    public List<string> ExcludedFromScopeStatuses { get; set; } = [];
    public HealthThresholdConfig HealthThresholds { get; set; } = new();
    public HealthWeightConfig HealthWeights { get; set; } = new();
    public int BugRatioAlertThreshold { get; set; } = 50;
    public int BugRatioConsecutiveSprintCount { get; set; } = 2;
    public string? CycleTimeStartStage { get; set; } = null;
    public string? CycleTimeEndStage { get; set; } = null;
    public int SyncBackSprintCount { get; set; } = 20;
    public int PlanningWindowDays { get; set; } = 2;
    public int DefaultSpPerBug { get; set; } = 3;
    public decimal BugRatioTarget { get; set; } = 30;
    public string? CompanyDomain { get; set; } = null;
    public bool XrayEnabled { get; set; } = false;
    public string? XrayClientId { get; set; }
    public string? XrayClientSecret { get; set; }
    public QaHealthThresholdConfig QaHealthThresholds { get; set; } = new();
    public int QualityHealthWeight { get; set; } = 20;
    public QualitySubScoreWeightConfig QualitySubScoreWeights { get; set; } = new();

    public static AppSettings CreateDefault() => new()
    {
        Id = 1,
        BoardId = null,
        DoneStatuses = ["Done", "Closed"],
        WorkflowStages = [],
        ExcludedFromScopeStatuses = [],
        HealthThresholds = new HealthThresholdConfig(),
        HealthWeights = new HealthWeightConfig(),
        BugRatioAlertThreshold = 50,
        BugRatioConsecutiveSprintCount = 2,
        CycleTimeStartStage = null,
        CycleTimeEndStage = null,
        SyncBackSprintCount = 20,
        PlanningWindowDays = 2,
        DefaultSpPerBug = 3,
        BugRatioTarget = 30,
        CompanyDomain = null,
        XrayEnabled = false,
        XrayClientId = null,
        XrayClientSecret = null,
        QaHealthThresholds = new QaHealthThresholdConfig(),
        QualityHealthWeight = 20,
        QualitySubScoreWeights = new QualitySubScoreWeightConfig()
    };
}
