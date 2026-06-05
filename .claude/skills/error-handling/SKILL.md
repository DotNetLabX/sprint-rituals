---
name: error-handling
description: Exception hierarchy, guard utilities, global error middleware. Loaded when implementing error handling or throwing domain/HTTP exceptions.
user-invocable: true
---

# Error Handling

## Exception Hierarchy

**Files:** `src/BuildingBlocks/Blocks.Exceptions/`

```
HttpException
├── BadRequestException   → 400
├── NotFoundException     → 404
└── UnauthorizedException → 401

DomainException (in Blocks.Domain) → mapped to 400 by middleware
```

### Usage Rules

| Exception | When to Use | Where |
|-----------|-------------|-------|
| `DomainException` | Domain rule violation inside aggregate | Domain layer (behavior methods) |
| `BadRequestException` | Input validation failure | Handlers, endpoints, validators |
| `NotFoundException` | Entity not found by ID | Repositories, handlers |
| `UnauthorizedException` | Auth/permission failure | Authentication middleware |
| Custom `{Context}Exception : DomainException` | Service-specific domain errors | Domain layer |

### Extension Points
The hierarchy currently covers 400, 401, 404. If needed, extend with:
- `ConflictException` → 409 (optimistic concurrency, duplicate)
- `ForbiddenException` → 403 (authenticated but not authorized)

Follow the same pattern: extend `HttpException`, set `StatusCode` in constructor.

## Global Exception Middleware

**File:** `src/BuildingBlocks/Blocks.AspNetCore/Middlewares/GlobalExceptionMiddleware.cs`

Type-switch mapping inside the middleware class (`MapStatusCode`):
- `ValidationException` → 400 (separate handler with structured errors)
- `ArgumentException` → 400
- `BadRequestException` → 400
- `NotFoundException` → 404
- `DomainException` → 400
- `UnauthorizedException` → 401
- `OperationCanceledException` → 499 (client disconnect, separate catch)
- Unhandled → 500

Note: each subclass is matched individually — there is no generic `HttpException` case. Dev-only stack traces via `IsDevelopment()`.

General errors return:
```json
{ "StatusCode": 400, "Message": "...", "TraceId": "...", "Details": null }
```

Validation errors return structured format with flat error array:
```json
{
  "StatusCode": 400,
  "Message": "One or more validation errors occurred.",
  "TraceId": "...",
  "Errors": [
    { "PropertyName": "Name", "ErrorMessage": "'Name' must not be empty." },
    { "PropertyName": "Email", "ErrorMessage": "'Email' is not a valid email address." }
  ],
  "Details": null
}
```

## Guard Utilities

**Files:** `src/BuildingBlocks/Blocks.Core/Guard.cs`, `src/BuildingBlocks/Blocks.Core/GuardExtensions.cs`

Small but expressive set:
- `Guard.NotFound(value)` — throws `NotFoundException` if null, returns non-null value
- `Guard.ThrowIfNullOrWhiteSpace(value)` — for value object factories
- `entity.OrThrowNotFound()` — extension method, chainable
- `Guard.ThrowIfFalse(condition, message)` — general assertion

Pattern: use Guards for single-line preconditions, throw directly for complex conditions.

```csharp
// Guard in value object factory
public static EmailAddress Create(string value)
{
    Guard.ThrowIfNullOrWhiteSpace(value);
    return new EmailAddress(value);
}

// Guard in handler
var entity = await _repository.FindByIdOrThrowAsync(command.{Entity}Id);
```

To extend: add new static methods to `Guard` class or new extension methods following the same pattern.
