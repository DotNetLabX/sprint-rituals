# Stack Detection Playbook

How to extract and build the stack fingerprint from a project's CLAUDE.md.

## Source Sections

Read these CLAUDE.md sections in order:
1. `## Tech Stack` — primary source (frameworks, libraries, tools)
2. `## Frontend` — secondary source (UI framework, state management, styling)
3. `## Repo Structure` — tertiary (may mention framework-specific folder patterns like "csproj", "Dockerfile")

## Extraction Rules

### What to extract

Every proper noun that names a technology. Categories:

| Category | Examples |
|----------|---------|
| Languages | .NET, C#, Python, Go, TypeScript, JavaScript, Rust, Java, Ruby, Elixir |
| Frameworks | ASP.NET Core, FastEndpoints, Django, Flask, Gin, Vue, React, Angular, Next.js, Rails |
| Libraries | EF Core, Mapster, MassTransit, Pinia, Tailwind, FluentValidation, SQLAlchemy, Prisma |
| Infrastructure | gRPC, SignalR, RabbitMQ, Redis, SQLite, PostgreSQL, Docker, Kubernetes |
| File formats | csproj, slnx, sln, package.json, go.mod, Cargo.toml, pyproject.toml, Gemfile |

### What NOT to extract

- Architectural patterns: DDD, CQRS, Clean Architecture, Event Sourcing — these are stack-independent
- Generic protocols: HTTP, HTTPS, WebSocket, REST, GraphQL — too generic, high false-positive rate
- Generic concepts: OAuth, JWT, OpenAPI — protocol/standard names, not stack-specific implementations

## Alias Expansion

For each extracted term, add aliases that appear in code or documentation. The table below is a starting reference — extend it with project-specific terms found during extraction.

| Primary term | Aliases to also match |
|-------------|----------------------|
| .NET | dotnet, csharp, C# |
| ASP.NET Core | AddAuthentication, AddAuthorization, WebApplication |
| EF Core | Entity Framework, EntityFrameworkCore, DbContext, Migration, OnModelCreating |
| FastEndpoints | IEndpoint, Ep.Req, Ep.Res |
| Vue 3 | Vue, Composition API, defineComponent, script setup |
| Pinia | defineStore, storeToRefs, useStore |
| Tailwind | @apply, @theme, tailwind.config |
| MassTransit | IConsumer, IBus, IPublishEndpoint, MassTransitHostedService |
| gRPC | proto, Protobuf, code-first contract, ServiceContract |
| SignalR | Hub, HubConnection, HubContext |
| React | useState, useEffect, JSX, tsx |
| Django | urls.py, views.py, models.py, INSTALLED_APPS |
| Flask | @app.route, Blueprint |
| SQLAlchemy | declarative_base, Session, MetaData |

### How to extend

After building the initial alias table from known mappings:
1. Grep the source project's `.claude/skills/` for the primary terms
2. Note any framework-specific identifiers that appear frequently (class names, decorators, macros)
3. Add those as aliases — they're the terms most likely to leak into portable files

## Regex Construction

For each term + aliases:

1. **Escape special characters:** `.NET` -> `\.NET`, `C#` -> `C\#`, `C++` -> `C\+\+`
2. **Multi-word terms:** allow flexible whitespace. `EF Core` -> `EF\s+Core`
3. **Short ambiguous terms:** terms under 4 characters that are common English words need word boundaries AND tech context:
   - `Go` (language) -> `\bGo\b` only when near programming context (not "go to", "let's go")
   - `Vue` -> `\bVue\b` (less ambiguous, word boundary is sufficient)
   - `C#` -> `C\#` (the `#` disambiguates)
4. **Case sensitivity:** all patterns are case-insensitive EXCEPT:
   - Acronyms that clash with common words when lowercased (e.g., `GO` vs "go")
   - Single-letter prefixes (e.g., `C#` should not match `c#` in a CSS color)

## Fingerprint Output Format

Store as a structured list for Phase 2 consumption:

```
Term: FastEndpoints
  Aliases: IEndpoint, Ep.Req, Ep.Res
  Pattern: (?i)\bFastEndpoints\b|IEndpoint|Ep\.Req|Ep\.Res
  Category: framework

Term: EF Core
  Aliases: Entity Framework, EntityFrameworkCore, DbContext
  Pattern: (?i)EF\s*Core|Entity\s*Framework|EntityFrameworkCore|DbContext|OnModelCreating
  Category: library
```

## Edge Cases

- **Shared names:** "Redis" is both a technology and a common word in some languages. Include it — false positives are caught at CHECKPOINT 1.
- **Versioned names:** "Vue 3" should also match "Vue" without version. The version number is optional in the pattern.
- **Compound stack names:** ".NET 10 / ASP.NET Core" — extract both ".NET" and "ASP.NET Core" as separate terms.
- **Abbreviations in CLAUDE.md:** If the CLAUDE.md uses abbreviations (e.g., "FE" for FastEndpoints), note them but don't add as aliases unless they appear in `.claude/` files (too ambiguous).
