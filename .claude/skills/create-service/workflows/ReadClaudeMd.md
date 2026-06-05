# Read Service CLAUDE.md

## Path

`src/Services/{Name}/CLAUDE.md`

## Hard-error if missing

If the file does not exist, stop immediately and tell the user:

> `src/Services/{Name}/CLAUDE.md` not found. Ask the Architect to run `create-service-claude-md` first.

Do not scaffold anything.

## Axes to extract

Parse the CLAUDE.md and extract every axis into a working model. The file format is the one produced by `create-service-claude-md/workflows/WriteClaudeMd.md` — stable section headers.

| Axis | Location in CLAUDE.md | Used by |
|------|----------------------|---------|
| Service name | `# {Name} Service` header | folder paths, csproj names |
| Endpoint framework | `**Endpoint framework:**` line | `ScaffoldApiProject.md` branch |
| Database | `**Database:**` line | `ScaffoldPersistenceProject.md` branch + docker-compose `depends_on` |
| HTTP/HTTPS ports | `**Port:**` line | `launchSettings.json`, `appsettings.json`, docker-compose |
| Application layer Y/N | `## Domain model` → `**Application layer:**` line | whether `ScaffoldApplicationProject.md` runs |
| gRPC clients consumed | `## gRPC clients` section | `.Application` or `.API` ProjectReference to `{ProjectName}.Grpc.Contracts` + DI registrations |
| gRPC server exposed | `## gRPC server` section | `.API` DI registrations, port mapping |
| Integration events published | `## Integration events` → `**Published:**` | `.API` or `.Application` references to `{ProjectName}.Integration.Contracts` + `Blocks.Messaging` |
| Integration events consumed | `## Integration events` → `**Consumed:**` | same as above + `depends_on: rabbitmq` in docker-compose |
| File storage | `## File storage` section (optional) | extra ProjectReference to the module (e.g., `FileService.AzureBlob`) |

## Derived decisions

From the axes, derive:

- **Use docker?** — default yes. If CLAUDE.md contains a `**Docker: N**` override line, skip Dockerfile + docker-compose updates.
- **MediatR needed?** — yes for Carter + MediatR, Minimal APIs + MediatR. No for FastEndpoints, Carter read-model.
- **Blocks.MediatR reference** — yes when MediatR needed.
- **Blocks.FastEndpoints reference** — yes when FastEndpoints.
- **EF Core building block reference** — yes when Persistence is EF Core (SQL Server, PostgreSQL).
- **Blocks.Redis reference** — yes when Persistence is Redis.
- **Blocks.Messaging reference** — yes when any integration events are published or consumed.

## Store axes in a working model

Keep the extracted axes accessible for the remaining workflows (`ScaffoldFolders`, `ScaffoldCsprojFiles`, etc.). Each subsequent workflow branches on these values.
