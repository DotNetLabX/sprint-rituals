# Exception Pattern Cleanup — Lessons

## Developer Lessons

- [APPLIED] **Handler error handling must use exceptions, not FastEndpoints primitives.** Inside `HandleAsync`, throw `NotFoundException`, `BadRequestException`, etc. The `GlobalExceptionMiddleware` maps them to HTTP responses. `AddError`/`SendErrorsAsync` is only for FluentValidation pre-handler validation. This keeps handler logic framework-agnostic — swapping FastEndpoints for MediatR requires no error-handling changes.
  - **Promote to:** `src/Services/Fokus/CLAUDE.md` under Endpoint pattern section
- [APPLIED] **GlobalUsings.cs should exist in all three service projects (API, Domain, Persistence)**, not just the API layer. Each project has its own set of frequently-used namespaces.
  - **Promote to:** `src/Services/Fokus/CLAUDE.md` under Key patterns section
