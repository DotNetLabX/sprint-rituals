# Knowledge Base Navigation

Before reading source files for domain, analytics, or feature questions, check `docs/kb/index.md` first.

1. Read `docs/kb/index.md` (topic map, ~30 lines)
2. Follow the link to the relevant entry
3. Get: business rules, computation formulas, edge cases, key file paths
4. Only then read the specific source files you need

This saves context — a KB entry gives you the business logic in ~60 lines vs reading 3-5 source files (~300+ lines).

## Glossary-First Naming

When naming new types, properties, or UI labels, read `docs/kb/glossary.md` first. Use the canonical term exactly. If the glossary says "Avoid" a synonym, don't use it in code or specs. Update the glossary when a new domain concept is introduced.

## Structural Navigation

The graphify structural graph at `graphify-out/GRAPH_REPORT.md` shows god nodes (most-connected types) and community clusters. Use it to find what touches what.
