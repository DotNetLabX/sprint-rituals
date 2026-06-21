# Proposal — Per-product documentation namespacing for Nexus (monolith-safe)

**Status:** Draft
**Decision-maker:** Laurentiu (nexus plugin owner) — to be discussed with the nexus architect
**Recommendation:** Add an **opt-in per-product doc axis** to nexus — backbone `index.md` + on-demand `{area}/{product}/` subtrees in `product/`, `architecture/`, `kb/`; `specs/` stays flat — keyed off a small `docs/products.md` registry. **Flat tree stays the default**; the split activates only when the registry exists, so single-product/monolith repos are unaffected.
**Confidence:** Medium — the structure and the monolith-default principle are clear; the *resolution mechanism* (how nexus maps a unit of work to a product) has three viable options not yet chosen. Below-High by the P1 rule = discuss/decide the mechanism before ratifying, not a go/no-go.
**Impact:** 6 — improves multi-product repo ergonomics and keeps agent context lean; no user-facing product impact.
**Effort:** med
**Date:** 2026-06-21

---

## Need

Sprint-rituals hosts **multiple products** (Fokus, Reflekt, Identity) in one repo — microservice-shaped, with a single Host as the only (reversible) deviation (ADR-001/ADR-023). The docs ported from the original Fokus and Reflekt repos each carry their own `product/`, `architecture/`, and `kb/` trees. Nexus today assumes a **single flat tree** per area (one `architecture/index.md`, one `kb/index.md`, one `product/index.md`) and has **no** mechanism to load only the docs relevant to the product currently being worked on. Result: either everything is flattened together (context bloat + naming collisions across products) or knowledge is dropped.

**Hard requirement — graceful degradation to monolith.** Many target repos are *not* multi-product: a plain app or a modular monolith that does **not** want per-domain doc separation. Whatever nexus adopts **must leave those repos exactly as they are today** — flat tree, no `{product}/` subfolders, no routing, no registry. The per-product axis is **opt-in and auto-off**. This is the non-negotiable constraint; a solution that forces the split on every repo is rejected.

**Out of scope:** the actual content port of Fokus/Reflekt docs into this repo (a separate, repo-local effort); fixing cross-references inside ported docs; any change to `conventions/` (already cross-cutting and shared).

## Approach

Two layers, mirroring nexus's existing "always-load index + on-demand entries" model — extended with a product axis that is **invisible unless switched on**.

**1. Layout (when multi-product).** For `product/`, `architecture/`, `kb/`:
- a **backbone** at the area root — `index.md` as router + cross-cutting content (decision register, suite topology, glossary, cross-cutting KB) — **always-load, kept small**;
- a **per-product subtree** `{area}/{product}/` — the bulk — **load-on-demand**.
- `specs/{slug}/` stays **flat** (the slug already identifies the product).

**2. Activation + resolution (the variable part — see Alternatives).** A repo declares it is multi-product via a `docs/products.md` **registry** (recommended). The registry both (a) flips nexus into split-aware mode and (b) maps a unit of work → product (by module path and/or slug pattern). **No registry ⇒ flat mode ⇒ today's behavior, unchanged.** An agent resolves the product from the slug/area, then loads **only that product's** product+architecture+kb subtree **plus** the always-load backbone.

**3. The five nexus changes (detail + verified current state in `docs/plugin-feedback/nexus-1.16.1-2026-06-21.md`):**
- **P1** `create-architecture-doc` — backbone + `architecture/{product}/` (currently hardcodes one `architecture/index.md`).
- **P2** `kb-navigation.md` / `kb-entry-schema` / kb index — router over `kb/{product}/` + backbone (reuses the existing one-level `kb/{area}/` subfolder mechanic).
- **P3** `create-feature-spec` / product convention — `product/{product}/` + router index.
- **P4** `agents-workflow.md` paths — central-`docs/` rule, per-product namespacing, specs-flat.
- **P5** agent context-loading — slug→product resolution + always-vs-on-demand boundary (the only net-new mechanism; no slug→product hook exists today).

Every change is **additive and flag-gated by the registry's presence** — flat repos hit none of it.

**Sequencing:** P1–P4 (folder shape) land first and independently; P5 (routing) depends on the chosen resolution mechanism + the folders existing. Shape → registry → routing.

## Benefits

- **Multi-product repos:** lean per-task context (only the active product's depth loads), no cross-product naming collisions, a clean place for cross-cutting/suite docs (the backbone).
- **Monolith repos:** zero change — the flat tree they have today is the default path.
- **Nexus:** one coherent doc model that scales from a single app to a suite; future repo-split is just lifting a namespaced subtree.
- **This repo:** unblocks porting Fokus + Reflekt knowledge without flattening or losing it.

## Alternatives

**Axis A — How nexus detects "multi-product" (activation):**
- **A1 (recommended): `docs/products.md` registry.** Presence = split mode; absence = flat. Explicit, greppable, doubles as the resolution source. Cost: one new file convention.
- **A2: auto-detect by `{area}/{product}/` subfolders existing.** Zero new file, but implicit/magic — ambiguous when a repo has a stray subfolder; harder to reason about.
- **A3: a config flag in nexus settings.** Explicit but lives outside `docs/`, splitting the decision from the docs it governs.

**Axis B — How a unit of work resolves to a product (resolution):**
- **B1 (recommended): registry map** (module path and/or slug pattern → product). Deterministic, central, survives slug renames. Pairs with A1.
- **B2: front-matter `product:` on `spec.md` / a per-slug marker.** Local to the work, but absent on ad-hoc slugs and easy to forget.
- **B3: CLAUDE.md cascade per module** (`src/Services/{product}/CLAUDE.md`). Auto-loads on code touch, but **misses doc-only work** (writing a spec from `docs/`), which is exactly when agents need it.
- **B4: slug-prefix convention.** Cheapest, but the prefix is unreliable (`F12-…`, `adhoc-…` name no product).

**Axis C — Loading mechanism:**
- **C1 (recommended): prose rule in `agents-workflow.md` + per-agent "Context to load first" blocks** read the registry. No new hook, fits nexus's current model.
- **C2: a session hook that injects resolved product context.** More automatic, but adds a hook to maintain and is harder to make degrade cleanly for flat repos.

**Rejected outright:** colocating docs in `src/Services/{product}/docs/` (breaks nexus's "everything under `docs/`" assumption, splits backbone from leaves).

## Unresolved

1. **Pick one option per axis** (A/B/C). Recommended baseline: A1 + B1 + C1.
2. **`docs/products.md` format** — minimal schema: product name → module path glob(s) + slug pattern(s) + doc-subtree paths. (Can draft on request.)
3. **Backbone boundary** — which KB is truly cross-cutting (promoted to `kb/cross-cutting.md` + glossary) vs product-owned. Per-repo judgement; the convention only needs to *allow* both.
4. **Identity** — a third product, mostly cross-cutting (auth/Person). Likely backbone-heavy with a thin `identity/` subtree; confirm during the port.
5. **Migration ergonomics** — does nexus offer a one-time "flat → split" helper, or is it manual? Out of scope for ratification; note for later.

## Graduate-to-spec

Technical branch (ADR-28): on ratification this graduates to a **tech-spec in the nexus plugin repo** with ADRs extracted per axis decision (A/B/C). It does **not** graduate here — sprint-rituals is the *consuming* project; the binding change ships from `<plugin-repo>`. This repo keeps ADR-023 (the local layout decision) + `docs/plugin-feedback/nexus-1.16.1-2026-06-21.md` (the maintainer hand-off).

## Provenance

Session "folder-structure", 2026-06-21 (architect). Fed by: ADR-023; the verified nexus-1.16.1 context-loading investigation (captured in the plugin-feedback file); structural maps of `<fokus-repo>\docs` and `<reflekt-repo>\docs`.
