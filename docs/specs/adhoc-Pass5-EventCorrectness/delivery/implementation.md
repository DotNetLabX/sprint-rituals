# Pass 5: EventCorrectness — Implementation

## Files Created

- `src/Services/Identity/Identity.Persistence/Migrations/20260609102332_RemoveUserRoleDbDefault.cs` — EF migration: `Up` drops `defaultValue: "Manager"` from `Users.Role`; `Down` restores it. `maxLength: 32, nullable: false` preserved. (Step 3 / B6)
- `src/Services/Identity/Identity.Persistence/Migrations/20260609102332_RemoveUserRoleDbDefault.Designer.cs` — EF-generated migration designer snapshot. (Step 3 / B6)

## Files Modified

- `src/Services/Identity/Identity.Domain/Users/Events/UserCreated.cs` — Added XML doc comment marking `UserCreated` as an intentional deferred-infra placeholder: not raised, not wired, must not be deleted as dead code, do not add RaiseCreated without promoting User to AggregateRoot. Record shape and `: IDomainEvent` declaration unchanged. (Step 1 / B2)
- `src/Services/Identity/Identity.Persistence/Configurations/UserConfiguration.cs` — Removed `.HasDefaultValue("Manager")` from the `Role` property configuration. `IsActive`'s `HasDefaultValue(true)` on line 23 is untouched (explicitly out of B6 scope per Q3 + plan Step 2 carve-out). Factory default in `User.CreateFromPerson(..., role = "Manager")` is now the single source of truth for the default role. (Step 2 / B6)
- `src/Services/Identity/Identity.Persistence/Migrations/IdentityDbContextModelSnapshot.cs` — Updated by `dotnet ef` to remove `HasDefaultValue("Manager")` from the `Role` property; `HasDefaultValue(true)` for `IsActive` remains. (Step 3 / B6)
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/Repositories/RepositoryBase.cs` — `UpsertAsync` update branch changed from `DbContext.Entry(existing).CurrentValues.SetValues(entity)` to `DbContext.Entry(existing).CurrentValues.SetValues(DbContext.Entry(entity).CurrentValues)`. The `SetValues(PropertyValues)` overload uses EF's property machinery and respects `PropertyAccessMode` (including `PropertyAccessMode.Field` for backing-field-mapped properties); the old `SetValues(object)` overload used plain reflection and missed backing-field-mapped properties. Insert branch, method signature, and all other members unchanged. In-line comment added explaining the rationale and the zero-caller reality. (Step 4 / B7)
- `docs/conventions/ef-core.md` — Added "Repository Upsert Pattern" section documenting: (a) the live `public new` explicit-copy pattern as the current rule; (b) the `SetValues(PropertyValues)` fix and why `SetValues(object)` is wrong; (c) the forward-looking `PropertyAccessMode` guardrail; (d) that the base method has no production callers so verification is compile-green + code reading, not a sync re-run. (Step 5 / B7 guardrail)

## Key Decisions

- **Migration startup project:** `dotnet ef migrations add` was run with `-s Services/Identity/Identity.Persistence` (not `Identity.API` as the plan suggested). `Identity.API.csproj` does not reference `Microsoft.EntityFrameworkCore.Design`; `Identity.Persistence` does and has a `DesignTimeDbContextFactory`. Using Persistence as the startup project is the correct pattern for this project — `DesignTimeDbContextFactory` exists precisely for this case. The plan's migration command specified `Identity.API` as the startup project, which failed; this deviation is justified by the project's actual setup. The migration output is identical regardless of which project is used as startup.
- **`SetValues(PropertyValues)` approach for B7:** Chose `DbContext.Entry(entity).CurrentValues` as the `PropertyValues` source. This attaches `entity` to the context transiently to read its values through EF's property machinery, then copies into `existing`'s `CurrentValues`. An alternative would be to explicitly attach `entity` as `Detached` then read its `CurrentValues` — functionally equivalent. The one-liner form keeps the method body minimal and consistent with the existing style.

## Carry-Over Findings

| Title | Severity | For | Evidence | Note |
|-------|----------|-----|----------|------|
| `Identity.API` missing `Microsoft.EntityFrameworkCore.Design` | low | reviewer | `Identity.API.csproj` has no Design package; migration command in `project-rules.md` specifies `-s {Svc}.API` but this fails for Identity | The `DesignTimeDbContextFactory` workaround works correctly; the project-rules.md migration command example may need a note that the startup project must carry Design, or that Persistence can be used when it has `DesignTimeDbContextFactory`. Not a bug — just a convention gap. |

## KB Changes

| Entry | Action | What changed |
|-------|--------|-------------|
| `docs/conventions/ef-core.md` | UPDATE | Added "Repository Upsert Pattern" guardrail section (Step 5) |

## Deviations from Plan

- **Step 3 migration command startup project:** Plan specified `-s Services/Identity/Identity.API`; actual command used `-s Services/Identity/Identity.Persistence`. Reason: `Identity.API` does not have `Microsoft.EntityFrameworkCore.Design`, which EF Tools requires in the startup project. `Identity.Persistence` has both the Design package and a `DesignTimeDbContextFactory` — the standard pattern for class-library API projects. Migration output is correct and passes all acceptance criteria.
