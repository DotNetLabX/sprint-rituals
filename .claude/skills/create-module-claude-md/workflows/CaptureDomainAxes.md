# Capture Domain Axes

Only run this workflow if `CaptureCommonAxes.md` resolved to archetype = **Domain**.

A Domain module owns its own aggregates, persistence, and (optionally) its own API layer. It is a mini-service without a process host — the host service provides the process, and usually the DbConnection too.

## Mandatory warning — read to the user before asking questions

> **Framework-host compatibility warning.** A Domain module with an API layer binds its endpoint framework choice to every host service that mounts it. If the module lives in multiple hosts with different frameworks, only **Minimal APIs with an explicit `Map{Name}Endpoints()` extension** works — it's the only framework-agnostic mounting approach. FastEndpoints and Carter rely on assembly-scanning inside the host and will only work if the host uses the same framework.
>
> Default choice for a module API: **Minimal APIs**. Override only if you know the module will live in one single-framework host forever.
>
> This is why some modules have no API today — their endpoint framework choice would have bound them to the host's framework, making them unusable from hosts with different frameworks.

Do **not** skip this warning. Paste it into the conversation before asking the user about the framework axis.

## Axes

| Axis | Values | Default | Notes |
|------|--------|---------|-------|
| Endpoint framework | `Minimal APIs` / `FastEndpoints` / `Carter + MediatR` / `None` | `Minimal APIs` | Picks the API scaffold shape. `None` means no API layer |
| Has API layer | Y / N | Y | Can be deferred to a later skill run |
| Persistence | `EF Core + SQL Server` / `EF Core + PostgreSQL` / `Redis` | match the first host | Drives Persistence scaffold |
| DB connection pattern | `embedded-host-connection` / `own-connection` | `embedded-host-connection` | Embedded = shares host's DB with a schema. Own = module has its own database |
| Schema name | PascalCase | `{Name}` | Only required when `embedded-host-connection`. Sets `HasDefaultSchema` |
| Cross-feature shared state | Y / N | N | Drives whether `.Application` project is created |
| Current hosts | list of service names | required | Services that mount the module today (check which services host modules) — informs the pattern |

## Derived rules

- If `Has API layer = N` → `Endpoint framework` is still captured for future reference but marked as `(deferred)`. The Developer scaffold skips the `.API` project.
- If `DB connection pattern = own-connection` → `Schema name` is optional and the module gets its own `ConnectionStrings:{Name}` entry in the host's appsettings.
- If `DB connection pattern = embedded-host-connection` → `HasDefaultSchema("{SchemaName}")` is mandatory in the DbContext and the migration history table must use that schema too.

## Ask-in-one-batch rule

Pre-fill from context, walk through the axes, ask the unanswered ones in a single batch, wait for confirmation.

Present the axis table to the user like this (with filled-in values) before writing CLAUDE.md:

```
Name: Audit
Archetype: Domain
Endpoint framework: Minimal APIs  [default]
Has API layer: Y  [default]
Persistence: EF Core + SQL Server
DB connection: embedded-host-connection  [default]
Schema: Audit  [default = Name]
Shared state: N
Current hosts: (list host service names)
```

## After this step

You have all axes needed for Domain archetype. Next: `WriteClaudeMd.md`.
