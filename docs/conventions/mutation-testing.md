# Mutation Testing Conventions

How to write mutation-gated tests, run Stryker, and interpret results in this project.

## What "mutation-gated" means

A test is mutation-gated when Stryker confirms that at least one mutant in the production file
is killed by that test. Passing tests alone are not enough — a test that does not kill any
mutant is not protecting its rule.

**Rule:** a business rule is considered covered only when Stryker kills a mutant that encodes
a violation of that rule. KB entries are marked `mutation-gated: true` only after this check passes.

## Test style

Two complementary styles are used in `Fokus.Domain.Tests`:

### Example-based tests (xUnit [Fact])

Use AwesomeAssertions exclusively — no `Assert.*` calls.
Follow the naming and slice conventions in `docs/conventions/testing.md`.

```csharp
[Fact]
public void Should_Return100_When_ValueAtOrAboveGreenThreshold()
{
    HealthScoreCalculator.ScoreHigherIsBetter(value: 0.9m, green: 0.8m, amber: 0.5m)
        .Should().Be(100m);
}
```

### Property-based tests (FsCheck [Property])

Use `FsCheck.Xunit.v3` with the xUnit v3 runner. Properties must hold for ALL generated inputs.
The `[Property]` method takes typed parameters; FsCheck auto-generates values for each.
Return `bool` — no `Prop.ForAll` wrapper needed (that is the F# API; C# uses direct return).
Use FsCheck built-in wrapper types (`PositiveInt`, `NonNull<T>`, `NonEmptyString`) to constrain
inputs without rejection loops.

```csharp
// Simple: FsCheck generates arbitrary decimal values automatically
[Property]
public bool Direction_IsAlwaysOneOfThreeValues(decimal delta)
{
    var direction = MetricCardBuilder.Direction(delta);
    return direction == "up" || direction == "down" || direction == "flat";
}

// Constrained: use PositiveInt to avoid rejection; cast to decimal for the SUT
[Property]
public bool Direction_IsUp_When_DeltaIsPositive(PositiveInt n)
{
    return MetricCardBuilder.Direction((decimal)n.Get) == "up";
}
```

Namespaces needed: `FsCheck`, `FsCheck.Fluent`, `FsCheck.Xunit`.

## Folder layout

Mirror `Fokus.Domain`'s folder structure:

```
Fokus.Domain.Tests/
  Analytics/                     # mirrors Fokus.Domain/Analytics/
    HealthScoreCalculatorTests.cs
    MetricCardBuilderPropertyTests.cs
  _Fixtures/
    TestData.cs                  # static factory methods — one file per test project
```

Add sub-folders as new domain areas are covered (e.g., `Aggregates/` when Sprint behaviors are tested).

## Running tests

```bash
# Run all tests
dotnet test src/Services/Fokus/Fokus.Domain.Tests

# Run with verbosity to see FsCheck shrink output
dotnet test src/Services/Fokus/Fokus.Domain.Tests --logger "console;verbosity=detailed"
```

## Running Stryker (MTP runner)

Stryker is configured via `stryker-config.json` in the test project root.
The proven command (run from the test project directory):

```bash
cd src/Services/Fokus/Fokus.Domain.Tests
dotnet stryker
```

Key config settings (`stryker-config.json`):

```json
{
  "stryker-config": {
    "project": "Fokus.Domain.csproj",
    "test-runner": "mtp",
    "coverage-analysis": "off",
    "reporters": ["progress", "html"],
    "mutation-level": "standard"
  }
}
```

**`test-runner: mtp`** — required for xUnit v3. The legacy `dotnet` runner does not support v3.

**`coverage-analysis: off`** — required with MTP. The MTP runner does not support coverage-based
mutation filtering; enabling it causes Stryker to skip all mutants. Keep this off permanently
for the domain test project.

## Interpreting results

- **Killed** — the mutant changed behavior and a test caught it. Good.
- **Survived** — no test caught the mutation. Add a test or property that specifically targets
  the mutated line.
- **No coverage** — with `coverage-analysis: off` this should not appear. If it does, check
  that the test project references `Fokus.Domain.csproj` directly.
- **Timeout** — a property test may loop forever on a mutant. Add a `Prop.Timeout` or
  constrain generator inputs.

## Mutation score target

No hard threshold is enforced in CI during the pilot. The goal for Step 2b is:
every KB business-rule entry has at least one mutation-killed test. Track per-rule status
in the KB entry's `mutation-gated` field.
