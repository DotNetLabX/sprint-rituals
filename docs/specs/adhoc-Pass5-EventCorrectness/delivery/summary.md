# Pass 5: EventCorrectness — Summary

## Status: COMPLETE

## What Was Built

Closed four domain-event-correctness findings (B1–B2, B6–B7) from the refactor triage. B2 annotated `UserCreated` as a deferred-infra placeholder. B6 removed the redundant DB-level `HasDefaultValue("Manager")` on `Users.Role` (factory default is now the single source of truth) with an EF migration. B7 hardened `RepositoryBase.UpsertAsync` to copy values via EF `PropertyValues` so backing-field-mapped properties are correctly included; a guardrail was added to `docs/conventions/ef-core.md` documenting the live `public new` explicit-copy pattern and the forward-looking `PropertyAccessMode` condition.

## Key Outcomes

- 2 files created (EF migration + designer file)
- 5 files modified (`UserCreated.cs`, `UserConfiguration.cs`, `IdentityDbContextModelSnapshot.cs`, `RepositoryBase.cs`, `docs/conventions/ef-core.md`)
- Build: green (0 errors)
- Review: APPROVED — cycle 1/3, 0 CRITICAL, 0 HIGH, 1 LOW follow-up

## Deviations from Plan

- **Step 3 migration startup project:** Plan specified `-s Identity.API`; executed with `-s Identity.Persistence`. `Identity.API` lacks `Microsoft.EntityFrameworkCore.Design`; `Identity.Persistence` has both the package and `IDesignTimeDbContextFactory`. Migration output is identical.

## Notes

- The LOW follow-up from the reviewer: `docs/conventions/project-rules.md` line 67 migration command example should note that the startup project must carry `Microsoft.EntityFrameworkCore.Design`, or that `{Svc}.Persistence` can serve as startup when it has `IDesignTimeDbContextFactory`. Recommend addressing in a future cleanup pass.
- `RepositoryBase.UpsertAsync` has zero production callers (all four Fokus repos use `public new` overrides). The B7 fix hardens the base for future flat-entity repositories that do not override.
- `IsActive`'s parallel `HasDefaultValue(true)` duplication (factory also sets `IsActive = true`) is knowingly deferred — explicitly out of Q3 scope for this pass.
