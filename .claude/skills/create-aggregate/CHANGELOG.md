# create-aggregate — Changelog

## [Unreleased]
- Initial changelog creation (adhoc-PipelineCraftImprovements)

## [1.0.0] — 2026-05-24
- Baseline version (reconstructed from git history — last known modification: 27e1934 [Claude-Code] Skills cleanup)
- Variant-aware: EF Core persistence vs Redis persistence
- Workflow files: AggregateEfCore.md, AggregateRedis.md, DomainEvent.md, ValueObject.md
- Partial class split pattern for aggregate state vs behavior
- Domain event raise-and-publish pattern via SaveChanges interceptor
