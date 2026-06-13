# Stryker MTP Re-run Result

**Date:** 2026-06-13  
**Stryker version:** 4.14.2  
**xUnit version:** xunit.v3 2.0.3  

## Path Used

**MTP runner** — primary path succeeded. Fallback to xUnit v2 was not needed.

## What Was Changed

1. Created `src/Services/Fokus/Fokus.Domain.Tests/stryker-config.json`:
   - `"test-runner": "mtp"` — routes Stryker through Microsoft.Testing.Platform instead of VSTest
   - `"coverage-analysis": "off"` — avoids MTP per-test coverage preview limitations
   - `"project": "Fokus.Domain.csproj"` — targets Fokus.Domain for mutation

2. Created `global.json` at repo root with `{ "test": { "runner": "Microsoft.Testing.Platform" } }` — **then immediately removed it** because it triggered SDK bug `KeyNotFoundException: key '8'` in the .NET 10.0.301 SDK's named-pipe MTP handshake (crash on `dotnet test`). The global.json approach is incompatible with this SDK version. Stryker's `--test-runner mtp` flag works independently of it.

## Stryker Mutant Counts

| Status       | Count |
|--------------|-------|
| Killed       | 32    |
| Survived     | 3299  |
| NoCoverage   | 0     |
| Timeout      | 0     |
| CompileError | 554   |
| Ignored      | 411   |
| **Total**    | 4296  |

**Overall mutation score: 0.96%**

## GO / NO-GO Verdict

**GO** — the success criterion (>0 mutants Killed) is met. The MTP runner is operational.

The 32 kills confirm that the 17 boundary tests for `HealthScoreCalculator` are exercising mutants and killing them correctly. The low overall score (0.96%) is expected: only `HealthScoreCalculator` has tests; the rest of `Fokus.Domain` (4296 mutants total across all analytics classes) is untested. This is the smoke-test scope — the GO verdict is for Step 1 of the Stryker integration, not full domain coverage.

## Errors Hit

### 1. `global.json` SDK crash (immediately reverted)
- **Error:** `KeyNotFoundException: The given key '8' was not present in the dictionary` in `Microsoft.DotNet.Cli.Commands.Test.TestApplicationHandler.OnHandshakeReceived`
- **Exit code:** 35
- **Root cause:** .NET SDK 10.0.301 MTP named-pipe protocol version mismatch — the SDK's MTP runner for `dotnet test` is not stable for this SDK version.
- **Resolution:** Removed `global.json`. Stryker's `--test-runner mtp` does not go through this code path.

### 2. Stryker compile errors (expected — safe mode)
- `CycleTimeCalculator.cs` line 154: `CS0411` — type argument inference fails under a specific mutation in `ComputeMultiSprint`. Safe-mode handled.
- `EpicProgressAnalyzer.cs` line 160: `CS1503` — nullable DateTime mutation causes type mismatch in `ComputeEpicProgress`. Safe-mode handled (2 occurrences).
- These are known Stryker behavior — it marks affected mutants as CompileError and skips them. No action needed.

### 3. MTP preview warning (informational)
- `The Microsoft Test Platform testrunner is currently in preview. Results should be verified since this feature is still being tested.`
- Not an error. Stryker proceeded normally.

## Files Modified

- `src/Services/Fokus/Fokus.Domain.Tests/stryker-config.json` — **created** (permanent artifact)
- `global.json` (repo root) — created and immediately removed (not committed)
