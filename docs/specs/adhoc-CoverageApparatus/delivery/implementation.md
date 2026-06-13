# Implementation — adhoc-CoverageApparatus

## What Was Added

### Part A — Test Apparatus

#### 1. Central Package Management (src/Directory.Packages.props)

Added two test-only package versions:

```xml
<PackageVersion Include="AwesomeAssertions" Version="9.0.0" />
<PackageVersion Include="FsCheck.Xunit.v3" Version="3.3.3" />
```

Also upgraded xUnit packages to resolve version skew introduced by FsCheck.Xunit.v3:

```xml
<PackageVersion Include="xunit.v3" Version="3.2.2" />           <!-- was 2.0.3 -->
<PackageVersion Include="xunit.runner.visualstudio" Version="3.1.5" /> <!-- was 3.1.1 -->
```

**Why AwesomeAssertions over FluentAssertions:** FluentAssertions 7.x introduced a commercial
licence for non-OSS projects. AwesomeAssertions is the community MIT-licenced fork maintained
by the same contributors. It is a drop-in replacement (`using AwesomeAssertions;`). The
existing conventions doc already specified FluentAssertions-style assertions — AwesomeAssertions
satisfies that rule without licence exposure.

**Why FsCheck.Xunit.v3 over FsCheck.Xunit:** `FsCheck.Xunit` pulls in `xunit.core` (v2), which
conflicts with `xunit.v3.core`. `FsCheck.Xunit.v3` is the official xUnit v3-compatible adapter.

#### 2. Test csproj (src/Services/Fokus/Fokus.Domain.Tests/Fokus.Domain.Tests.csproj)

Added bare `PackageReference` entries (versions in CPM):

```xml
<PackageReference Include="AwesomeAssertions" />
<PackageReference Include="FsCheck.Xunit.v3" />
```

#### 3. Example-based tests (migrated + renamed)

`src/Services/Fokus/Fokus.Domain.Tests/Analytics/HealthScoreCalculatorTests.cs`

- Migrated from `Assert.*` to `AwesomeAssertions` (`.Should().Be()`, `.BeGreaterThan()`, etc.)
- Renamed methods to `Should_{Behavior}_When_{Condition}` per testing.md conventions
- Grouped into 3 numbered slices: ScoreHigherIsBetter, ScoreLowerIsBetter, CompositeRag
- 17 example-based tests, all passing

#### 4. Property-based tests (new)

`src/Services/Fokus/Fokus.Domain.Tests/Analytics/MetricCardBuilderPropertyTests.cs`

Demonstrates the FsCheck 3.x C# API. Target: `MetricCardBuilder` (pure static, zero
dependencies — ideal for property testing).

**FsCheck 3.x C# API (critical note):** `Prop.ForAll` is the F# API. C# `[Property]` methods
take typed parameters directly; FsCheck auto-generates values. Return `bool`. Use built-in
wrapper types (`PositiveInt`, `NonNull<T>`) to constrain inputs without rejection loops.

```csharp
[Property]
public bool Direction_IsAlwaysOneOfThreeValues(decimal delta)
{
    var direction = MetricCardBuilder.Direction(delta);
    return direction == "up" || direction == "down" || direction == "flat";
}

[Property]
public bool Direction_IsUp_When_DeltaIsPositive(PositiveInt n)
{
    return MetricCardBuilder.Direction((decimal)n.Get) == "up";
}
```

12 property-based tests across 3 slices:
- Direction invariants (always one of three values, up for positive, down for negative)
- Polarity invariants (always one of three values, neutral at zero, inverts when positiveUp flips)
- ComputeDelta null propagation and direction/polarity correctness

#### 5. Fixture scaffold

`src/Services/Fokus/Fokus.Domain.Tests/_Fixtures/TestData.cs`

Placeholder matching the static-factory-method convention from testing.md. Commented example
shows the Sprint factory shape for when Step 2b needs entity-level tests.

#### 6. Conventions doc

`docs/conventions/mutation-testing.md` — new file. Covers:
- What "mutation-gated" means and the kill-confirms-coverage rule
- Example-based (AwesomeAssertions) and property-based (FsCheck 3.x) test styles with
  correct API usage for each
- Folder layout mirroring Fokus.Domain
- Proven `dotnet stryker` command and config explanation
- `coverage-analysis: off` caveat (required with MTP runner, permanent for this project)
- Result interpretation and pilot mutation score target

Referenced from `docs/conventions/coding-conventions.md` via `@mutation-testing.md`.

### Part B — KB Scaffold

#### 7. docs/kb/index.md

KB index with status legend (placeholder / verified / mutation-gated) and entry table.
Starts with one entry: `health-score.md` (status: mutation-gated).

#### 8. docs/kb/_schema/entry-template.md

Template with required sections in the KB entry schema order:
Rules, Key Files, Edge Cases, Relationships, Source, plus mutation-gate tracking comments.

#### 9. docs/kb/health-score.md

First real KB entry. Covers `HealthScoreCalculator`:
- BR1–BR6: interpolation boundaries, clamping, RAG thresholds
- Key Files pointing to production implementation and test files
- Edge cases: divide-by-zero guards, degenerate config cases, Math.Max(0) floor
- Relationships to SprintSummaryCalculator, QaMetricsCalculator, Vue frontend

## Verify Results

### Build

```
Build succeeded.
```

### dotnet test

```
Passed! - Failed: 0, Passed: 29, Skipped: 0, Total: 29, Duration: 399ms
```

29 tests: 17 example-based (HealthScoreCalculatorTests) + 12 property-based
(MetricCardBuilderPropertyTests). All green.

### dotnet stryker (MTP runner — regression check)

Run: `2026-06-13.21-50-40`

| Status | Count |
|--------|-------|
| Killed | 79 |
| Survived | 3252 |
| Timeout | 0 |
| NoCoverage | 0 |
| CompileError | 554 |
| Ignored | 411 |

**79 killed > 0 — MTP runner config is intact, no regression.**

The 3252 survived are untested calculators across the full `Fokus.Domain` project — expected.
Our apparatus covers `HealthScoreCalculator` and `MetricCardBuilder` only. Step 2b will add
entries for additional calculators; Step 3 (Cover) will grow the kill count.

The 554 CompileError entries are Stryker-generated mutants that produced invalid C# (e.g.
removing a `return` from a non-void method) — Stryker correctly excludes these from the score.
