# Architecture

**How to read this:** `decisions.md` is the authoritative, current register — it wins over everything below where they conflict. `fokus/v1.md` is the Fokus standalone era (historical, partially superseded). Suite-level integration decisions live in `../proposals/suite-integration-architecture.md`. Per-product architecture is namespaced under `fokus/`, `reflekt/`, `poker/` (ADR-023).

**Reference project:** the reference codebase — canonical pattern source for the skill→reference decision order (see `decisions.md` ADR-002/003 and the agent plugin's `engineering-discipline` rule).

@decisions.md

---

## Per-product architecture

Product-specific detail, namespaced per product (ADR-023). The backbone above (`decisions.md`) is always-load; the per-product detail is inlined here for now — a future pass converts these to on-demand links per ADR-023.

### Fokus (standalone-era v1)

Superseded where it conflicts with `decisions.md` (deployment model, auth, gRPC/MassTransit/MediatR scope, thin-domain-as-end-state). Still valid for domain-model classification, the HealthScore formula, and indexes.

@fokus/v1.md

### Reflekt

@reflekt/v1.md

### Poker (active product draft)

> `poker/v1.md` is titled "Product Specification" — a product-vision doc currently filed under `architecture/`. Consider relocating to `product/poker/v1.md` to match `product/{fokus,reflekt}/`.

@poker/v1.md
