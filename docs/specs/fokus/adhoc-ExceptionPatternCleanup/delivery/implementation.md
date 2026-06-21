# Exception Pattern Cleanup — Implementation

## Files Created
- `src/Services/Fokus/Fokus.API/GlobalUsings.cs` — global usings for API project
- `src/Services/Fokus/Fokus.Domain/GlobalUsings.cs` — global usings for Domain project
- `src/Services/Fokus/Fokus.Persistence/GlobalUsings.cs` — global usings for Persistence project

## Files Modified
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncSprints/SyncSprintsEndpoint.cs` — replaced AddError/SendErrorsAsync with throw BadRequestException/NotFoundException, removed redundant usings
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncBacklogSprints/SyncBacklogSprintsEndpoint.cs` — replaced AddError/SendErrorsAsync with throw BadRequestException, removed redundant usings
- ~30 additional files across API, Domain, and Persistence — removed redundant usings now covered by GlobalUsings.cs

## Key Decisions
- Split "one or both sprint IDs not found" into two separate NotFoundException throws for clearer error messages
- Excluded EF Core Migration files from global using cleanup (auto-generated, should not be touched)
- BoardId-not-configured stays as BadRequestException (system misconfiguration, not a missing resource)

## Deviations from Plan
- None (conversation-driven, no formal plan)
