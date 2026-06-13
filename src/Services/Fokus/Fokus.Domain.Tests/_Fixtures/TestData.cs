namespace Fokus.Domain.Tests._Fixtures;

/// <summary>
/// Static factory methods for domain objects used across the mutation-gated test suite.
/// Add factories here as new calculators are covered. Keep defaults valid; override specific fields via params.
/// </summary>
public static class TestData
{
    // Placeholder — populated as Step 2b business-rule records are verified and covered.
    // Example shape (uncomment and adapt when Sprint entity factories are needed):
    //
    // public static Sprint ActiveSprint(DateTime? startDate = null, DateTime? endDate = null, int id = 1)
    //     => new Sprint { Id = id, StartDate = startDate ?? DateTime.UtcNow.AddDays(-7),
    //                     EndDate = endDate ?? DateTime.UtcNow.AddDays(7), State = "active" };
}
