namespace Fokus.Domain.Analytics;

/// <summary>
/// Shared domain record for the 6 base fields common to all 3c-B metric cards.
/// Returned by CarryOverAnalyzer, ScopeChangeAnalyzer, BugRatioAnalyzer, CycleTimeAnalyzer.
/// Wire-layer card types (ScopeMetricCard, BugRatioMetricCard, CycleTimeMetricCard) map FROM this record
/// at the endpoint boundary (ADR-010). SprintMetricCardData and QaMetricCardData are isolated exceptions
/// because they carry Sparkline + Rag fields not present here.
/// </summary>
public sealed record MetricCardData(
    string Name,
    decimal Value,
    string DisplayValue,
    decimal? Delta,
    string? DeltaDirection,
    string? DeltaPolarity);
