using Fokus.Domain.Analytics;
using Mapster;

namespace Fokus.API.Features.Analytics.GetQaTrends;

// Wire records for /api/analytics/qa-trends.

public record QaTrendsSprintInfo(int Id, string Name, DateTime StartDate, DateTime EndDate);

public record QualityTrendEntry(
    int SprintId,
    string SprintName,
    decimal CoverageRate,
    decimal PassRate,
    decimal ExecutionRate,
    string CoverageRag,
    string PassRateRag,
    string ExecutionRag);

public record TestingVolumeEntry(int SprintId, string SprintName, int TeCount, int BugsFound);

public record DefectCorrelationDataPoint(
    int SprintId,
    string SprintName,
    decimal CoverageRate,
    decimal? NextSprintBugRatio,
    string? NextSprintName);

public record DefectCorrelationResult(
    List<DefectCorrelationDataPoint> DataPoints,
    decimal? PearsonR,
    int DataPointCount);

public record QaTrendsResponse(
    bool HasQaData,
    List<QaTrendsSprintInfo> Sprints,
    List<QualityTrendEntry> QualityTrends,
    List<TestingVolumeEntry> TestingVolume,
    DefectCorrelationResult? DefectCorrelation);

// --- Mapster mappings ---

public class QaTrendsMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<QaTrendsSprintInfoData, QaTrendsSprintInfo>();
        config.NewConfig<QualityTrendEntryData, QualityTrendEntry>();
        config.NewConfig<TestingVolumeEntryData, TestingVolumeEntry>();
        config.NewConfig<DefectCorrelationDataPointData, DefectCorrelationDataPoint>();
        config.NewConfig<DefectCorrelationResultData, DefectCorrelationResult>();
        config.NewConfig<QaTrendsResultData, QaTrendsResponse>();
    }
}
