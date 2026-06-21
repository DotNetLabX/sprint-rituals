# JiraModuleRefactor — Implementation

## Files Created
- `src/Modules/Jira/Jira.Contracts/SprintState.cs` — SprintState enum moved from Fokus.Domain, namespace changed to Jira.Contracts
- `src/Modules/Jira/Jira.Contracts/JiraBoard.cs` — moved from Fokus.JiraContracts, namespace changed to Jira.Contracts
- `src/Modules/Jira/Jira.Contracts/JiraSprint.cs` — moved from Fokus.JiraContracts, namespace changed to Jira.Contracts
- `src/Modules/Jira/Jira.Contracts/JiraIssue.cs` — moved from Fokus.JiraContracts, namespace changed to Jira.Contracts
- `src/Modules/Jira/Jira.Contracts/JiraChangelog.cs` — moved from Fokus.JiraContracts, namespace changed to Jira.Contracts
- `src/Modules/Jira/Jira.Contracts/JiraPagedResult.cs` — moved from Fokus.JiraContracts, namespace changed to Jira.Contracts

## Files Modified
- `src/Modules/Jira/Jira.Contracts/Jira.Contracts.csproj` — removed ProjectReferences to Fokus.JiraContracts and Fokus.Domain (now zero project references)
- `src/Modules/Jira/Jira.Contracts/IJiraClient.cs` — removed using Fokus.Domain and Fokus.JiraContracts (all types now in same namespace)
- `src/Modules/Jira/Jira.RestApi/Jira.RestApi.csproj` — removed Fokus.JiraContracts reference, added Blocks.Exceptions and Microsoft.Extensions.Options.DataAnnotations references
- `src/Modules/Jira/Jira.RestApi/RestApiJiraClient.cs` — removed using Fokus.Domain and Fokus.JiraContracts
- `src/Modules/Jira/Jira.RestApi/IJiraApi.cs` — changed using Fokus.JiraContracts to using Jira.Contracts
- `src/Services/Fokus/Fokus.Domain/Fokus.Domain.csproj` — replaced Fokus.JiraContracts reference with Jira.Contracts reference
- `src/Services/Fokus/Fokus.Domain/Sprint/Sprint.cs` — added using Jira.Contracts (for SprintState)
- `src/Services/Fokus/Fokus.Domain/Sprint/Behaviors/Sprint.cs` — changed using Fokus.JiraContracts to using Jira.Contracts
- `src/Services/Fokus/Fokus.Domain/Sprint/Behaviors/SprintMembership.cs` — changed using Fokus.JiraContracts to using Jira.Contracts
- `src/Services/Fokus/Fokus.Domain/Developer/Behaviors/Developer.cs` — changed using Fokus.JiraContracts to using Jira.Contracts
- `src/Services/Fokus/Fokus.Domain/Ticket/Behaviors/Ticket.cs` — changed using Fokus.JiraContracts to using Jira.Contracts
- `src/Services/Fokus/Fokus.Domain/Ticket/Behaviors/StatusTransition.cs` — changed using Fokus.JiraContracts to using Jira.Contracts
- `src/Services/Fokus/Fokus.API/Fokus.API.csproj` — removed Fokus.JiraContracts reference
- `src/Fokus.slnx` — removed Fokus.JiraContracts project entry
- `src/Modules/Jira/CLAUDE.md` — updated to reflect new structure

## Files Deleted
- `src/Services/Fokus/Fokus.Domain/Enums/SprintState.cs` — moved to Jira.Contracts
- `src/BuildingBlocks/Fokus.JiraContracts/` — entire project deleted (all types merged into Jira.Contracts)
- `src/Services/Fokus/Fokus.Domain/Enums/` — empty directory removed

## Key Decisions
- Added `Blocks.Exceptions` project reference to `Jira.RestApi.csproj` — was previously available transitively through the old Fokus.JiraContracts/Fokus.Domain chain, needed explicitly after removing those references
- Added `Microsoft.Extensions.Options.DataAnnotations` package to `Jira.RestApi.csproj` — same transitive dependency issue, needed for `ValidateDataAnnotations()` in DependencyInjection.cs
- API endpoint files (SyncSprintsEndpoint, SyncBacklogEndpoint, GetJiraSprintsEndpoint) required no changes — they already had `using Jira.Contracts;` and `SprintState` resolves through that

## Deviations from Plan
- **Step 3 additional references:** Plan did not mention adding `Blocks.Exceptions` project reference or `Microsoft.Extensions.Options.DataAnnotations` package to `Jira.RestApi.csproj`. These were needed because removing the transitive dependency chain (through Fokus.JiraContracts and Fokus.Domain) broke resolution of `UnauthorizedException`/`BadGatewayException` and `ValidateDataAnnotations()`.
- **Sprint.cs entity file:** Plan Step 4 listed only behavior files for using directive changes. The entity definition file `Sprint.cs` also needed `using Jira.Contracts;` added because it uses `SprintState` in its property declaration and is in namespace `Fokus.Domain`, not `Jira.Contracts`.
