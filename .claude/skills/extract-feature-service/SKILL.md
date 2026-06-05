---
name: extract-feature-service
description: Extracts shared handler logic from endpoints into a focused operation service. Named after the operation, scoped to a feature area. Single-purpose with result record pattern.
---

# Extract Feature Service

Extracts shared or complex handler logic from one or more endpoints into a focused service class. The service is named after the **operation** it performs, not the entity it touches. It lives in the feature area root and stays single-purpose.

## When to Use

- Two or more endpoints in the same feature area share handler logic
- A single endpoint's `HandleAsync` has grown too complex and the logic is reusable
- A plan step specifies extraction of shared logic into a service

## Steps

1. **Identify the shared logic** — the code block(s) that will move out of endpoint(s). Note the exact line ranges.

2. **Choose the service name** — name it after the operation, not the entity. Examples:
   - `SprintIssueSyncService` (syncs sprint issues)
   - `HealthScoreCalculator` (computes health scores)
   - Bad: `SprintService` (entity wrapper — violates guardrails)

3. **Define result record(s)** — each public method returns a result record. Define them in the same file, above the service class:
   ```csharp
   public record {Operation}Result({properties});
   ```

4. **Create the service file** — place it in the feature area root (parent of the endpoint folders):
   ```
   Features/{Area}/{ServiceName}.cs    ← service + result records
   Features/{Area}/{Sub}/Endpoint.cs   ← endpoints that consume it
   ```

5. **Use primary constructor injection** — the service takes its dependencies (repositories, clients, loggers) via primary constructor:
   ```csharp
   public class {ServiceName}(
       {Repository} repository,
       ILogger<{ServiceName}> logger)
   ```

6. **Move the logic** — extract the shared code into service methods. Each method should be a single cohesive operation with clear inputs and a result record output.

7. **Update the endpoint(s)** — inject the service, replace the extracted logic with a service method call, map the result record to the endpoint response.

8. **Register the service** — add `services.AddScoped<{ServiceName}>()` in the service's `DependencyInjection.cs`.

9. **Verify the build** — `dotnet build` must pass.

## Guardrails

- **Single-purpose** — if the service grows beyond one cohesive operation area, split it.
- **Not an entity wrapper** — `SprintService` that accumulates all sprint logic is a guardrail violation. Each service covers one operation.
- **Feature-scoped** — the service lives in the feature area, not in a `Services/` god folder.
- **No repository interfaces** — inject concrete repositories directly.

## Reference Examples

Look for existing feature services in the codebase:
- `Features/*/` — service files at the feature area root alongside endpoint subfolders

## What This Skill Does NOT Do

- Create entity-wrapper services — those violate guardrails.
- Move logic to a separate project — services stay in the API project.
- Introduce abstractions — no interface for the service unless it's a module contract.
