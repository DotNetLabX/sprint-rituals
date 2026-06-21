# Product — index (backbone router)

This is the **always-load** product backbone. It is intentionally thin: it routes to each product's
own docs and to the cross-cutting suite strategy. Load the per-product subtree on demand for the
product you are working on; do **not** load every product's docs every task.

## Cross-cutting (backbone)

- [Agentic Rituals Suite strategy](suite-strategy.md) — how the products fit together as a suite.

## Products (load on demand)

| Product | Docs | Scope |
|---------|------|-------|
| Fokus | [product/fokus/](fokus/index.md) | Sprint analytics, Jira/Xray sync, health metrics |
| Reflekt | [product/reflekt/](reflekt/index.md) | Retrospectives (planning poker / retro sessions) |

> Identity (Person/User, auth) is currently cross-cutting and lives in architecture/conventions; it
> gets a `product/identity/` subtree only if it grows a product-level "what". (See ADR-023.)

---
Convention: `docs/architecture/decisions.md` → **ADR-023** (per-product namespacing + on-demand
loading). Structure mirrors `architecture/` and `kb/`.
