# Scaffold Folders

Create the folder tree for the new service. Empty placeholder folders so it is obvious where each kind of file goes when the Developer runs `create-aggregate` and `create-feature` later.

## Base tree (always created)

```
src/Services/{Name}/
├── {Name}.API/
│   ├── Features/
│   ├── Properties/
├── {Name}.Domain/
│   ├── Entities/
│   ├── Events/
│   ├── ValueObjects/
├── {Name}.Persistence/
│   ├── EntityConfigurations/
│   ├── Repositories/
│   ├── Migrations/
│   ├── MasterData/
```

## Conditional tree

### If shared-state axis = Y → add `.Application`
```
└── {Name}.Application/
    ├── Dtos/
    ├── StateMachines/
    ├── Mappings/
    ├── Features/
```

The `Features/` folder in `.Application/` is where handlers live for MediatR services. For FastEndpoints services that happen to need a `.Application` project, leave `Features/` out and let the Developer add feature folders to `.API/Features/` instead.

**Rule:** create `.Application/Features/` **only when** the endpoint framework is Carter + MediatR or Minimal APIs + MediatR. For FastEndpoints services with shared state, omit `.Application/Features/`.

### If persistence = Redis → swap Persistence subfolders
```
{Name}.Persistence/
├── Repositories/
├── MasterData/
```

Redis services do not use EF Core entity configurations or migrations — omit those folders.

### If persistence = EF Core → keep full Persistence tree
Keep `EntityConfigurations/`, `Repositories/`, `Migrations/`, `MasterData/` as above.

### If endpoint framework = read-model → Features/ in `.API`
Read-model services use a flat `{Aggregate}/Consumers/` layout inside the API project rather than `Features/`. For read-model services:

```
{Name}.API/
├── Consumers/            (integration event consumers)
├── Queries/              (read endpoints)
```

Omit `Features/` for read-model services.

## How to create folders

Folders with no files must be created as tracked folders. If the file system can't track empty folders, add a `.gitkeep` placeholder file inside each empty folder:

```
{Name}.API/Features/.gitkeep
{Name}.Domain/Entities/.gitkeep
...
```

## After this step

Folders exist but nothing else. Next: `ScaffoldCsprojFiles.md` creates the project files with correct references.
