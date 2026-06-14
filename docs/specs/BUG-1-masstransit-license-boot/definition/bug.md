# BUG-1 — Host does not boot: MassTransit 9 license required

**Status:** Open
**Severity:** High (blocks running the integrated monolith end-to-end)
**Discovered:** 2026-06-14, during adhoc-ReflektLiftAndShift (RA1) Codex boot probe
**Scope:** Pre-existing, independent of RA1 (Fokus / MassTransit — RA1 did not touch either)

## Symptom

Running the integrated Host (`src/Host`, e.g. `dotnet run` or `Host.exe`) applies the
Identity, Fokus, and Reflekt EF migrations, then crashes while building the bus endpoint graph:

```
MassTransit.ConfigurationException: License must be specified ...
```

The app never reaches `app.Run()`. This appears to be the first end-to-end boot attempt of the
monolith (recent work was test-harness focused), so the blocker has gone unnoticed until now.

## Root cause

- MassTransit is wired in `src/Services/Fokus/Fokus.API/DependencyInjection.cs:36`
  (`AddMassTransit(x => x.UsingInMemory(...))`).
- The pinned version is **`MassTransit 9.1.2`** (`src/Directory.Packages.props:21-22`).
  MassTransit 9.x enforces a **commercial/community license at runtime**, validated when the
  bus endpoint graph is configured at startup.
- **No license is configured** anywhere in source or `appsettings*.json`
  (no `SetLicense`, `MT_LICENSE`, `MT_LICENSE_PATH`, or license file).

Confirmed not RA1-introduced: MassTransit `9.1.2` is identical at HEAD (`9a58739`) and in the
working tree; RA1's only `Directory.Packages.props` edits were Mapster 7.4→10.0 + AngleSharp.

## Candidate fixes (decide during planning)

1. **Register a MassTransit license** — community (free, registration required) or commercial:
   `x.SetLicense(...)` / `MT_LICENSE` env var / license file. Needs a key from the owner's
   MassTransit account. Keeps the current v9 API surface in Fokus.
2. **Downgrade MassTransit 9 → 8 (latest free)** — v8 has no license gate, but v8↔v9 has breaking
   API changes; the existing Fokus MassTransit/consumer code (written for v9) would need review.

Recommendation deferred to the fix pipeline — option 1 is lower-churn if a license key is available.

## Acceptance

`dotnet run` on `src/Host` boots cleanly to `app.Run()` with Identity, Fokus, and Reflekt all
registered, and a basic request authenticates. Add a startup smoke check so this regression is
caught automatically in future.
