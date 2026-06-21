# ExtractJiraModule — Lessons

## Architect Lessons
- [APPLIED] When planning a module extraction that absorbs a BuildingBlock, verify the full dependency graph first — not just the direct consumers. `Fokus.Domain` referencing `Fokus.JiraContracts` (via `FromJira()` factory methods) created a circular dependency that blocked absorption. The plan should have included a dependency graph analysis step before committing to absorption.
- [TRACKED] Domain entities with `FromJira()` static factory methods create a coupling between Domain and external API contracts. This is by design (architecture doc: "Sync-from-External Domain Model") but constrains how those contracts can be reorganized. Future module extractions involving shared contracts should check for Domain-layer references.
- [APPLIED] The `create-module` skill's Component template assumes the Contracts project is self-contained. When the contract interface returns types from another project (like `Fokus.JiraContracts`), the developer must manually add those references post-scaffold. The skill could benefit from a "shared dependencies" axis in the CLAUDE.md template.

## Developer Lessons

## Reviewer Lessons
