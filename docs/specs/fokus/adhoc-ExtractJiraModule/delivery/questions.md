# ExtractJiraModule — Questions

## Q1: Circular dependency — Fokus.Domain references Jira.Contracts, but Jira.Contracts references Fokus.Domain
**From:** developer
**To:** architect
**Status:** Open
**Step:** 3 — Absorb Fokus.JiraContracts into Jira.Contracts
**File:** `src/Services/Fokus/Fokus.Domain/Fokus.Domain.csproj`

**Context:** During Step 3, I found that `Fokus.Domain` references `Fokus.JiraContracts` — not just `Fokus.API/Infrastructure/Jira/`. Five domain behavior files use `using Fokus.JiraContracts;`:
- `Fokus.Domain/Developer/Behaviors/Developer.cs`
- `Fokus.Domain/Ticket/Behaviors/Ticket.cs`
- `Fokus.Domain/Ticket/Behaviors/StatusTransition.cs`
- `Fokus.Domain/Sprint/Behaviors/SprintMembership.cs`
- `Fokus.Domain/Sprint/Behaviors/Sprint.cs`

These files call `FromJira(JiraIssue dto)`, `FromJira(JiraSprint dto)`, etc. — they depend on the Jira response contracts.

The plan says `Jira.Contracts.csproj` should reference `Fokus.Domain` (for `SprintState` enum). But if `Fokus.Domain` also references `Jira.Contracts` (to replace `Fokus.JiraContracts`), that creates a circular dependency:

`Jira.Contracts` → `Fokus.Domain` → `Jira.Contracts`

**Question:** How should the circular dependency be resolved? Options I see:
1. Keep `Fokus.JiraContracts` as a standalone BuildingBlock (don't absorb it) and have both `Fokus.Domain` and `Jira.Contracts` reference it. `Jira.Contracts` only adds `IJiraClient`.
2. Move the `FromJira(...)` behavior methods out of `Fokus.Domain` into `Fokus.API` or a new mapping layer, so `Fokus.Domain` no longer depends on Jira contracts.
3. Remove the `SprintState` reference from `Jira.Contracts` — put the state filter in the implementation using strings, so `Jira.Contracts` has no dependency on `Fokus.Domain`.
4. Keep `SprintState` in `Jira.Contracts` as a copy (separate from the one in `Fokus.Domain`).

### Answer
**Status: Answered**

**Option 1: Keep `Fokus.JiraContracts` as a standalone BuildingBlock.** Do not absorb it.

Why the other options are wrong:
- **Option 2 (move FromJira out of Domain):** Breaks the architecture. The `FromJira()` methods are static factory methods on domain entities — they're aggregate behavior, not mapping logic. The architecture doc ("Sync-from-External Domain Model") explicitly designs entities to be populated from Jira. Moving factories out scatters entity creation logic and violates the domain-owns-its-behavior principle.
- **Option 3 (strings instead of SprintState):** Weakens type safety for no structural benefit. Callers lose compile-time validation of filter values.
- **Option 4 (duplicate SprintState):** Two enums with identical values in different namespaces — confusing, error-prone, requires mapping.

**Option 1 is correct** because `Fokus.JiraContracts` is genuinely shared infrastructure — it's referenced by both `Fokus.Domain` (in `FromJira()` factories) and `Jira.Contracts` (as return types in `IJiraClient`). This is exactly what BuildingBlocks are for: app-specific shared types that multiple projects depend on.

**Updated dependency graph:**
```
Jira.Contracts → Fokus.JiraContracts (for return types)
Jira.Contracts → Fokus.Domain (for SprintState)
Jira.RestApi → Jira.Contracts
Fokus.Domain → Fokus.JiraContracts (for FromJira() parameters)
Fokus.API → Jira.Contracts + Jira.RestApi + Fokus.Domain + Fokus.Persistence
```
No cycles.

**Plan impact:** Step 3 (absorb Fokus.JiraContracts) is removed. The plan reverts to 7 steps. `Jira.Contracts.csproj` references both `Fokus.JiraContracts` and `Fokus.Domain`. `Jira.RestApi.csproj` also needs `Fokus.JiraContracts` (for the Jira response contracts used in `IJiraApi`). The `Fokus.API.csproj` keeps its existing `Fokus.JiraContracts` reference.
