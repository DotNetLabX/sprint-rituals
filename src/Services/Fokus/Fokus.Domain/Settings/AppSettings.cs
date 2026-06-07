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
    public QaHealthThresholdConfig QaHealthThresholds { get; set; } = new();
    public int QualityHealthWeight { get; set; } = 20;
    public QualitySubScoreWeightConfig QualitySubScoreWeights { get; set; } = new();
    public string? CompanyDomain { get; set; } = null;
    public BugRatioConfig BugRatio { get; set; } = new();
    public XrayConfig Xray { get; set; } = new();
    public CycleTimeConfig CycleTime { get; set; } = new();
    public SyncConfig Sync { get; set; } = new();

    public static AppSettings CreateDefault() => new()
    {
        Id = 1,
        BoardId = null,
        DoneStatuses = ["Done", "Closed"],
        WorkflowStages = [],
        ExcludedFromScopeStatuses = [],
        HealthThresholds = new HealthThresholdConfig(),
        HealthWeights = new HealthWeightConfig(),
        QaHealthThresholds = new QaHealthThresholdConfig(),
        QualityHealthWeight = 20,
        QualitySubScoreWeights = new QualitySubScoreWeightConfig(),
        CompanyDomain = null,
        BugRatio = new BugRatioConfig
        {
            AlertThreshold = 50,
            ConsecutiveSprintCount = 2,
            Target = 30,
            DefaultSpPerBug = 3
        },
        Xray = new XrayConfig
        {
            Enabled = false,
            ClientId = null,
            ClientSecret = null
        },
        CycleTime = new CycleTimeConfig
        {
            StartStage = null,
            EndStage = null
        },
        Sync = new SyncConfig
        {
            BackSprintCount = 20,
            PlanningWindowDays = 2
        }
    };
}
