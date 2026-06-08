namespace Fokus.Domain.Analytics;

// Domain result records for QaTrendsAnalyzer.

public sealed record QaTrendsSprintInfoData(int Id, string Name, DateTime StartDate, DateTime EndDate);

public sealed record QualityTrendEntryData(
    int SprintId,
    string SprintName,
    decimal CoverageRate,
    decimal PassRate,
    decimal ExecutionRate,
    string CoverageRag,
    string PassRateRag,
    string ExecutionRag);

public sealed record TestingVolumeEntryData(int SprintId, string SprintName, int TeCount, int BugsFound);

public sealed record DefectCorrelationDataPointData(
    int SprintId,
    string SprintName,
    decimal CoverageRate,
    decimal? NextSprintBugRatio,
    string? NextSprintName);

public sealed record DefectCorrelationResultData(
    List<DefectCorrelationDataPointData> DataPoints,
    decimal? PearsonR,
    int DataPointCount);

public sealed record QaTrendsResultData(
    bool HasQaData,
    List<QaTrendsSprintInfoData> Sprints,
    List<QualityTrendEntryData> QualityTrends,
    List<TestingVolumeEntryData> TestingVolume,
    DefectCorrelationResultData? DefectCorrelation);
