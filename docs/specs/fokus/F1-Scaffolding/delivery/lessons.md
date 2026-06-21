# F1-F3 Scaffolding — Lessons

## Executor Lessons

- [APPLIED] EF Core `HasData` is incompatible with entities that have `ToJson()` owned types. For single-row settings entities with JSON columns, seed programmatically at startup instead of via migration seed.
- [APPLIED] .NET 10 `dotnet new sln` creates `.slnx` files by default (new XML solution format), not the classic `.sln`. All `dotnet sln` commands work the same way — just use the `.slnx` extension.
- [TRACKED] FastEndpoints 6.0.0 is the current version for .NET 10 (5.x does not exist for this TFM). Same for Mapster 10.0.0.
- [TRACKED] When running `dotnet ef migrations add` with FastEndpoints, if no endpoints are registered yet, the host startup fails. A `IDesignTimeDbContextFactory` in the Persistence project bypasses this.
- [APPLIED] Tailwind CSS v4 uses `@import "tailwindcss"` in CSS and the `@tailwindcss/vite` plugin. No `tailwind.config.ts` file is needed.
- [APPLIED] TypeScript with `erasableSyntaxOnly` (Vue/Vite default) does not allow `enum` declarations. Use type unions instead: `type SprintState = 'Active' | 'Closed'`.
- [APPLIED] Collection expressions (`[]`) cannot be used in EF Core value converter lambda expressions (expression trees). Use `new List<string>()` instead.

## Planner Lessons

- [APPLIED] Plan specified `HasData` for seed without considering EF Core's `ToJson()` limitation. For future plans involving JSON-column entities, specify runtime seed instead.
- [APPLIED] Plan referenced `Fokus.sln` but .NET 10 defaults to `.slnx`. Plans should say "solution file" without specifying extension, or note the .NET 10 default.
- [TRACKED] Plan step granularity was right at 10 steps — no step was too large or too small for executor to handle in one pass.
- [TRACKED] The plan correctly identified that existing skills (`create-service`, `create-feature`, etc.) assume shared BuildingBlocks and instructed executor to adapt rather than follow blindly. This prevented wasted time.

## Skill Gaps

- [APPLIED] **Missing skill:** `create-standalone-service` — the existing `create-service` skill assumes shared BuildingBlocks exist (Blocks.Domain, Blocks.EntityFrameworkCore, etc.). For greenfield/standalone projects without shared infrastructure, a variant skill would avoid referencing nonexistent packages. Suggested coverage: standalone csproj setup with direct NuGet packages, inline base classes (or none), SQLite-first configuration.
