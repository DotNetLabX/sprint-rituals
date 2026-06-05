# Pass 0 — Foundations — Summary

## Status: COMPLETE

## What Was Built
Cross-cutting, behavior-preserving refactor establishing the modular-monolith foundations: Central Package Management across all 18 projects, migration of all legacy FastEndpoints `Send*Async` calls to the modern `Send.*` API, a thin composition root (`Host/Program.cs` reduced to Add/Init/Use with wiring relocated into module DI + an `app.Migrate<TDbContext>()` helper), and convention text corrected to the decided modular monolith.

## Key Outcomes
- **Files:** `src/Directory.Packages.props` (new), `Blocks.EntityFrameworkCore/Extensions/DbContextExtensions.cs` (new `Migrate<T>()` helper), 18 csproj (`Version=` stripped), all `Send*Async` call sites migrated (117 modern `Send.*`; 0 legacy), `Host/Program.cs` thinned, module `DependencyInjection.cs` files (MassTransit/gRPC/SignalR/seeding relocated), `docs/conventions/project-rules.md` rewritten.
- **Build:** `dotnet build SprintRituals.slnx` green — 0 `error CS`.
- **Review:** APPROVED. Cycle count: 1 fix cycle (HIGH-1 TOCTOU `FirstAsync` + MEDIUM-2 unused param) → re-review APPROVED, no regressions.

## Deviations from Plan
- Build verification batched per-step (declared known deviation in plan).
- `Migrate<T>()` SQLite dir-create extended beyond the SQL-Server reference shape (documented in implementation.md) — required to preserve first-run startup.
- Two false positives correctly excluded from the `Send*Async` sweep: `httpClient.SendAsync` (Xray.GraphQL) and `hubContext.Clients.All.SendAsync` (SignalR).

## Notes
- **Working tree is intermingled:** Pass 0 code changes sit on top of pre-existing uncommitted scaffolding (CLAUDE.md, docs/proposals/*, .claude/*, Blocks.Contracts/, Fokus.API/Grpc/, and prior edits to Program.cs/Host.csproj). A clean Pass-0-only code commit is not separable (intra-file overlap; interactive staging unavailable). Decide commit staging before committing the code.
- **Deferred to Pass 1 (skill repair):** create `central-package-management` + `framework-currency` skills; LOW-1 (make `Migrate<T>()` async). The `framework-currency` detection pattern must include the bare `SendAsync(...)` form and the SignalR/HttpClient false positives. See `lessons.md`.
- **Routed elsewhere:** architect agent-file stale convention block → learner (`improve-flow`); `Fokus/CLAUDE.md` v1 drift → Pass 4.
- **No KB impact** (`docs/kb/` empty — verified, Step 6).
- Lessons processing (learner pass) is available on request but optional.
