---
name: extract-endpoint-types
description: Splits a FastEndpoints endpoint file into endpoint + command/query sibling file. Moves request, response, DTOs, and validator into the sibling. Keeps endpoint class focused on handler logic.
---

# Extract Endpoint Types

Splits a fat endpoint file that contains request, response, DTOs, validator, and handler into two sibling files in the same feature folder:
- `{Name}Endpoint.cs` — endpoint class only (handler logic)
- `{Name}Command.cs` or `{Name}Query.cs` — request, response, DTOs, validator

## When to Use

- An endpoint file has grown beyond the endpoint class itself
- Request/response types or DTOs are referenced by other files (services, tests)
- A plan step specifies CQRS separation of endpoint types

## Steps

1. **Identify the endpoint file** and classify as command (mutating) or query (read-only).

2. **Create the sibling file** — `{Name}Command.cs` for mutations, `{Name}Query.cs` for reads. Same folder, same namespace as the endpoint.

3. **Move these types to the sibling file:**
   - Request class (e.g., `SyncSprintsCommand`)
   - Response class (e.g., `SyncSprintsResponse`)
   - Any DTOs referenced only by the response
   - Validator class (e.g., `SyncSprintsCommandValidator`)

4. **Keep in the endpoint file:**
   - The endpoint class with `HandleAsync`
   - Any `using` statements needed only by the handler

5. **Carry over `using` statements** — copy only the usings the moved types actually need. Common ones: `FluentValidation` (for validator), domain namespaces (for DTOs referencing domain types).

6. **Add a `using` for the endpoint's namespace** in the command/query file if the types reference anything that stays in the endpoint file. Usually not needed since they share the same namespace.

7. **Verify the build** — `dotnet build` must pass.

## File Layout After

```
Features/{Area}/{Feature}/
  {Feature}Endpoint.cs      — endpoint class only
  {Feature}Command.cs        — request + response + DTOs + validator (mutating)
  — OR —
  {Feature}Query.cs           — request + response + DTOs + validator (read-only)
```

## Reference Examples

Look for existing command/query sibling files in the codebase:
- Query pattern: `Features/*/Get*/*Query.cs`
- Command pattern: `Features/*/Sync*/*Command.cs`

## What This Skill Does NOT Do

- Create new endpoints — use `create-feature` for that.
- Introduce MediatR or any mediator pattern — the endpoint still owns handler logic.
- Move handler logic out of the endpoint — use `extract-feature-service` for shared logic extraction.
