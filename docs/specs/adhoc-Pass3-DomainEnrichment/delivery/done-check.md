# Pass 3a — DomainEnrichment — Done Check

## Verdict: PASS

All 6 plan steps implemented. Objective gate (build) green. Deviation is valid and satisfies ADR-006 intent.

---

## Step Dispositions

| Step | Title | Disposition | Notes |
|------|-------|-------------|-------|
| 1 | Jira→domain boundary mapper (5 Group-1) | Implemented | `JiraDomainMapper.cs` created under `Features/Sync/`. Implements `IRegister` for 3 clean 1:1 maps; 2 non-1:1 maps (`MapStatusTransitions`, `MapSprintMembership`) as plain static methods — plan explicitly sanctioned this. |
| 2 | Xray/TestSet domain-construction mapper (3 Group-2) | Implemented | `XrayDomainMapper.cs` created under `Features/Xray/`. No `IRegister` (no source DTO). Methods renamed `Create*` — plan flagged this as developer's call. |
| 3 | Repoint sync services + delete 8 domain factories | Implemented | All 7 `SprintIssueSyncService` + 3 `XrayIssueSyncService` call sites repointed. 8 factory methods deleted from domain (6 empty behavior files also deleted). `GetEffectiveSp` confirmed present on `SprintMembership`. Grep for `\.FromJira\|\.FromXray\|FromJiraIssueLink\|ListFromJira` → zero hits. |
| 4 | Drop `Fokus.Domain → Jira.Contracts` ref + stale global usings | Implemented (Deviated — valid) | `Fokus.Domain.csproj`, `Fokus.Domain/GlobalUsings.cs`, `Fokus.Persistence/GlobalUsings.cs` all cleaned. **Deviation:** `Fokus.API/GlobalUsings.cs` also had `global using Jira.Contracts;` removed — not in plan, but required by the `SprintState` move (name collision). 8 per-file `using Jira.Contracts;` added to affected API files. Deviation is within ADR-006 intent and correctly documented. |
| 5 | Correct stale `Fokus/CLAUDE.md` sync-mapping line | Implemented | `CLAUDE.md` line 35 now reads: "Jira and Xray DTOs are mapped to domain types by boundary mappers at the API layer (`Features/Sync/JiraDomainMapper.cs` … `Features/Xray/XrayDomainMapper.cs`). No DTO enters the Domain layer (ADR-006)." No `JiraMapper` claim remains. |
| 6 | Verification sweep + build | Implemented | All 6 checks passed (see Evidence below). |

---

## Deviation Analysis

### Deviation 1 — `SprintState` moved to `Fokus.Domain`

**Plan said:** Drop the `Jira.Contracts` project reference from `Fokus.Domain`.
**Developer did:** Also created `src/Services/Fokus/Fokus.Domain/Sprint/SprintState.cs` — a new domain-owned enum — because `Sprint.State` was typed as `Jira.Contracts.SprintState`. Without moving the enum the domain would still depend on `Jira.Contracts`.

**ADR-006 assessment: SATISFIES.** ADR-006 requires Fokus.Domain to depend on nothing but `Blocks.Domain`. `Jira.Contracts.SprintState` was a Jira type embedded in a domain property type — a direct ADR-006 violation. Moving it to the domain (as `Fokus.Domain.SprintState`) is the correct remediation. `Jira.Contracts.SprintState` remains in the Jira module for `IJiraClient.GetSprintsAsync` filter params; the two enums are now separate types with the same shape. They will diverge as the domain state model enriches.

**No residual Jira type in `Fokus.Domain`:** `rg -n "JiraIssue|JiraSprint|using Jira\.|Jira\." src/Services/Fokus/Fokus.Domain --glob "*.cs"` → zero hits. Confirmed.

### Deviation 2 — `global using Jira.Contracts;` removed from `Fokus.API/GlobalUsings.cs`

**Plan said:** Remove the global using from `Fokus.Domain/GlobalUsings.cs` and `Fokus.Persistence/GlobalUsings.cs` only.
**Developer did:** Also removed it from `Fokus.API/GlobalUsings.cs` and added 8 per-file explicit `using Jira.Contracts;` to affected files.

**Assessment: VALID.** The `SprintState` move created a namespace collision (`Fokus.Domain.SprintState` vs `Jira.Contracts.SprintState`) across all 35+ `Fokus.API` files that import `global using Fokus.Domain;`. Removing the global and using per-file explicit imports where Jira types are genuinely needed is the correct resolution. Files that only use `Fokus.Domain.SprintState` (analytics endpoints) are unaffected — they pick up the domain type via `global using Fokus.Domain;`. This is documented in `implementation.md` with a carry-over finding (SprintState duplication, severity: low).

---

## Evidence

| Check | Result | Evidence |
|-------|--------|----------|
| Build | PASS | `dotnet build SprintRituals.slnx` → `Build succeeded. 6 Warning(s), 0 Error(s)`. Warnings are pre-existing (NU1903 vulnerability, CS8629 nullable, CS9107 constructor capture) — none from Pass 3a changes. |
| Step 6.1 — `From*` factory grep | PASS | `rg -n "\.FromJira\|\.FromXray\|FromJiraIssueLink\|ListFromJira" src --glob "*.cs"` → zero hits (no domain factory definitions or calls remain). |
| Step 6.2 — Domain Jira-free | PASS | `rg -n "JiraIssue\|JiraSprint\|using Jira\.\|Jira\." src/Services/Fokus/Fokus.Domain --glob "*.cs"` → zero hits. |
| Step 6.3 — Csproj + Persistence using clean | PASS | `rg -n "Jira" src/Services/Fokus/Fokus.Domain/Fokus.Domain.csproj` → zero; `rg -n "Jira" src/Services/Fokus/Fokus.Persistence/GlobalUsings.cs` → zero. |
| Step 6.4 — Boundary mappers exist | PASS | `Features/Sync/JiraDomainMapper.cs` (implements `IRegister`); `Features/Xray/XrayDomainMapper.cs` (no `IRegister`, plain construction). Both confirmed. |
| Step 6.5 — `GetEffectiveSp` present | PASS | `rg -n "GetEffectiveSp" src/Services/Fokus/Fokus.Domain` → `Sprint/Behaviors/SprintMembership.cs:11`. |
| Step 6.6 — Domain `SprintState` | PASS | `Fokus.Domain/Sprint/SprintState.cs` exists; `Sprint.cs` property typed as `SprintState` (domain-owned). |
