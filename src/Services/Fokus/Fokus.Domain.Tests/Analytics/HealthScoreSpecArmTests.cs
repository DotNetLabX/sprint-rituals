using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Xunit;
using Fokus.Domain.Analytics;

namespace Fokus.Domain.Tests.Analytics;

/// <summary>
/// Cover-from-spec probes for <see cref="HealthScoreCalculator"/> — the Generate output of the
/// F8 M1 merge (registry: docs/kb/golden/HealthScoreCalculator.md).
///
/// SR-16 (sub-scores bounded to 0-100) is the single generation-eligible rule of the F8 merge:
/// verified, layer domain-calc (calculator-home), unattested by the code arm
/// (docs/kb/health-score.md). The composite half of SR-16 (weighted average of bounded
/// sub-scores) is homed in SprintSummaryAnalyzer and follows arithmetically — see the registry
/// changelog inspection note.
///
/// Thresholds are constrained to the spec's committed domain (non-negative percentages, green
/// and amber correctly ordered per direction). The negative-threshold pathology documented in
/// docs/kb/health-score.md Edge Cases is outside that domain and deliberately not generated
/// against. Production code is never edited to make these pass.
/// </summary>
public class HealthScoreSpecArmTests
{
    [Property]
    [Trait("layer", "domain-calc")]
    [Trait("criticality", "core")]
    [Trait("runtime-cost", "fast")]
    public bool SpecSR16_HigherIsBetterSubScore_AlwaysWithinZeroToHundred(
        NonNegativeInt value, NonNegativeInt amber, PositiveInt greenAboveAmber)
    {
        var v = (decimal)value.Get;
        var a = (decimal)amber.Get;
        var g = a + greenAboveAmber.Get;

        var score = HealthScoreCalculator.ScoreHigherIsBetter(v, g, a);
        return score >= 0m && score <= 100m;
    }

    [Property]
    [Trait("layer", "domain-calc")]
    [Trait("criticality", "core")]
    [Trait("runtime-cost", "fast")]
    public bool SpecSR16_LowerIsBetterSubScore_AlwaysWithinZeroToHundred(
        NonNegativeInt value, NonNegativeInt green, PositiveInt amberAboveGreen)
    {
        var v = (decimal)value.Get;
        var g = (decimal)green.Get;
        var a = g + amberAboveGreen.Get;

        var score = HealthScoreCalculator.ScoreLowerIsBetter(v, g, a);
        return score >= 0m && score <= 100m;
    }
}
