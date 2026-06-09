# EF Core Conventions

Programming conventions for EF Core specific to this codebase. Referenced by developer and reviewer agents.

## Navigation Loading

- `ThenInclude` chains continue from the last included navigation. Pattern: `Include(s => s.Memberships).ThenInclude(m => m.Ticket).ThenInclude(t => t.Assignee)`.
- `Contains` on a fetched `List<string>` translates to SQL `IN (...)` correctly. For very large sets this would need batching, but Fokus dataset is bounded.
- **Missing `ThenInclude` causes silent null/zero — not a runtime error.** When service logic accesses `link.Ticket?.IssueType` but the repository only includes `link` (not `.ThenInclude(l => l.Ticket)`), the navigation property is null. Computations silently produce 0 or skip entries. Always match `ThenInclude` depth to the deepest navigation property accessed in downstream service code.

## Cross-Aggregate Queries

- For analytic queries that join across aggregates, add the query method to the repository that owns the primary data (e.g., `TicketRepository` for status transitions). Don't create a separate read-model query class until the pattern recurs.

## Migrations

- `dotnet ef migrations remove --force` works cleanly when the migration has not been applied to a database. No need to delete files manually.
- When running `dotnet ef migrations add` with FastEndpoints and no endpoints are registered yet, the host startup fails. Add `IDesignTimeDbContextFactory` in the Persistence project to bypass this.

## Migration Defaults

- **Verify `defaultValue` in `AddColumn` migrations matches the entity property initializer.** EF Core scaffolds the CLR default (`false` for `bool`, `0m` for `decimal`) unless `HasDefaultValue` is configured. This is almost always wrong for columns with a meaningful default (e.g., `IsActive = true`, `BugRatioTarget = 30m`). Compare every new `AddColumn<T>` migration default against both the C# property initializer and `CreateDefault()` factory. If they differ, set `HasDefaultValue(...)` in the entity configuration so EF generates the correct default, then regenerate or patch the migration.

## Additional Gotchas

- `First` vs `FirstOrDefault` at service boundaries: even in single-user SQLite apps, prefer `FirstOrDefault` with null handling over `First` which throws an unhandled 500.
- `required int` on composite key members is more defensively correct than plain `int` — int defaults to 0 which can silently produce invalid FK records.
- `HasData` + `ToJson()` are incompatible. EF Core cannot seed entities via `HasData` when the entity (or an owned type on it) uses `ToJson()` column mapping. Use runtime seeding (`SeedTestData`) instead.
- Collection expressions (`[]`) fail in expression tree lambdas. EF Core translates lambdas to SQL via expression trees. C# 12 collection expressions are not representable as expression trees — use `new List<T>()` or `Array.Empty<T>()` explicitly.

## Repository Upsert Pattern

**Live pattern (what the codebase uses today):** Repositories with non-trivial upsert/copy semantics override `UpsertAsync` with `public new` and perform explicit per-column assignment (`SprintRepository`, `TicketRepository`, `DeveloperRepository`, `TestExecutionRepository` all follow this pattern). The base `RepositoryBase.UpsertAsync` is the fallback for simple flat entities and currently has no production callers.

**Base method behavior (after B7 fix):** `RepositoryBase.UpsertAsync` copies values via `DbContext.Entry(existing).CurrentValues.SetValues(DbContext.Entry(entity).CurrentValues)`. The `SetValues(PropertyValues)` overload goes through EF's property machinery and respects `PropertyAccessMode` — including `PropertyAccessMode.Field` for backing-field-mapped properties. Do not revert to `SetValues(entity)` (passing the detached POCO directly) — that uses plain reflection and misses backing-field-mapped properties.

**Forward-looking guardrail:** If a future repository drops its `public new` override and relies on the base `UpsertAsync`, and if its aggregate uses private backing fields, those fields must be configured with the matching `PropertyAccessMode` so the base copy picks them up.

**Verification reality:** The base method has no end-to-end test path (no production callers); verification is compile-green + code reading, not a sync re-run. A sprint/ticket sync exercises the `public new` overrides, not the base.
