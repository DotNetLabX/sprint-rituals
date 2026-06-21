# Nexus plugin feedback — v1.16.1 — 2026-06-21

Portable feedback file (ADR-1). These items are **plugin-bound**: they target nexus shipped
skills, agent files, and pipeline path/context-loading rules that live in the version-keyed plugin
cache and cannot be edited from this consuming project. Send this file to the plugin maintainer; on
the maintainer's machine the same entries are applied directly in the plugin source repo
(`<plugin-repo>\plugins\nexus`).

Source project: **sprint-rituals**. Origin: an architect-led design discussion (session
"folder-structure", 2026-06-21) on how to organize the docs being ported from the original Fokus and
Reflekt repos into this modular-monolith-of-microservices. The decision is recorded locally as
**ADR-023** in `docs/architecture/decisions.md`; the items below are the slice of that decision that
must become a **nexus convention** rather than a one-off here.

**Status:** INVESTIGATED 2026-06-21 — nexus's current loading model has been mapped (findings
below); ready for learner classification + a convention-change pass. Not yet applied.

---

## The decision in one line

Docs are **microservice-shaped**: kept central under `docs/`, namespaced **per product** in
`product/`, `architecture/`, `kb/` (each = a small always-load backbone `index.md` + on-demand
`{area}/{product}/` subtrees), with `specs/` flat. Product depth is **loaded on demand**, routed by
**slug→product**; the cross-cutting backbone is **always loaded**. The single Host is incidental and
reversible — doc layout follows the service split, not the deployment topology.

---

## Investigation findings (nexus 1.16.1, verified 2026-06-21)

Source: read-only sweep of `<plugin-repo>\plugins\nexus`.

1. **No slug→product/area routing hook exists** anywhere — agents, `rules/agents-workflow.md`, and
   `hooks/scripts/` were all checked. The slug resolves to exactly one thing: `docs/specs/{slug}/`.
   The prefix (`ABC`, `adhoc-Reflekt…`) is never parsed to pick a doc area. **P5 is net-new.**
2. **KB loading is a global rule**, not per-agent: `rules/kb-navigation.md` (always-injected) says
   "read `docs/kb/index.md` (topic map) → follow the link to the entry." The index is **singular**.
   Entries already live at `docs/kb/{area}/{concept}.md` — a **one-level subfolder that exists today
   but is never interpreted as a product**. This is the one reusable seam for P2.
3. **All four doc skills assume a single flat tree:** `create-architecture-doc` hardcodes
   `docs/architecture/index.md` as "the canonical name every nexus agent reads";
   `create-feature-spec` hardcodes `docs/product/index.md` + `docs/architecture/index.md` as its
   reading protocol; `kb-entry-schema` + the kb index are singular. None support per-product splits.
4. **Always-load blocks today:** `docs/conventions/coding-conventions.md` (architect, developer,
   reviewer, solo); `docs/architecture/index.md` (architect only); `docs/kb/index.md` (solo only —
   on-demand for architect/developer); `docs/product/index.md` (on-demand, architect + PO). PO has
   no always-load block. Minor pre-existing bug: architect.md lists the architecture index as both
   always-load (line ~24) and on-demand (line ~116) — the always block is operative.
5. **No "docs must live under `docs/`" hard rule**, but every path reference uses the `docs/` prefix
   without exception (only `graphify-out/GRAPH_REPORT.md` lives outside, on-demand). Safe to make
   the central-`docs/` rule explicit (P4) — it codifies existing universal practice.

**Net:** P1–P4 = namespace skills/conventions that currently hardcode flat singletons (additive,
low risk, single-product repos must not regress). P5 = the one genuinely new mechanism (slug→product
resolution + the always-vs-on-demand boundary); P1–P4 are the folder shape it routes over.

---

## P1 — `create-architecture-doc`: backbone + per-product architecture split

- **Target:** `skills/create-architecture-doc/SKILL.md` + `references/template.md` (both Generate
  and Refresh modes).
- **Current:** hardcodes the single canonical `docs/architecture/index.md`.
- **Ask:** for a multi-module repo, Generate scaffolds a **backbone** `architecture/index.md` (router
  + decision register + cross-cutting/suite topology) **+** `architecture/{product}/` subfolders for
  product domain detail; Refresh recognizes the split. Single-module repos keep today's flat shape.

## P2 — kb schema + index + navigation rule: per-product KB with on-demand routing

- **Target:** `rules/kb-navigation.md` (the load rule — primary), `rules/kb-maintenance.md`,
  `skills/kb-entry-schema/SKILL.md`, and the `docs/kb/index.md` convention.
- **Current:** singular index; `docs/kb/{area}/{concept}.md` subfolder exists but `{area}` ≠ product.
- **Ask:** `kb/index.md` becomes a **router** pointing at backbone (`kb/cross-cutting.md`, glossary)
  + `kb/{product}/` subtrees; the navigation rule loads **only the active product's** subtree + the
  backbone. Reuse the existing subfolder mechanic; the new part is the product axis + routing.

## P3 — product-doc convention: per-product folders + router index

- **Target:** `skills/create-feature-spec/SKILL.md` reading protocol + wherever `docs/product/`
  structure is documented.
- **Current:** `docs/product/index.md` is a single file = the whole project's product source of truth.
- **Ask:** support `product/{product}/` + a `product/index.md` **router** (suite strategy = backbone);
  create-feature-spec resolves the product's index from the slug rather than reading one global file.

## P4 — agents-workflow path conventions: central docs, per-product namespacing, specs flat

- **Target:** `rules/agents-workflow.md` (§ Slug and Path Resolution).
- **Ask:** add the rules — (a) docs live centrally under `docs/`, **never** colocated in
  `src/Services/{module}/docs/`; (b) `product/`, `architecture/`, `kb/` are namespaced
  `{area}/{product}/` with a backbone index; (c) `specs/{slug}/` stays **flat** (the slug carries the
  product). This codifies existing universal `docs/` practice + the new product axis.

## P5 — agent context-loading: load product subtrees on demand by slug→product (the load-bearing item)

- **Target:** `rules/agents-workflow.md` context-loading rules + `rules/kb-navigation.md` + each
  agent's "Context to load first" block (architect/developer/reviewer/po/solo) + the `README.md`
  "project-owned docs" note.
- **Current:** no slug→product resolution exists; always-load blocks are fixed lists.
- **Ask:** define the **slug→product** resolution hook (primary) + area `index.md` router (secondary);
  an agent resolves the product from the slug, then loads **only that product's**
  product+architecture+kb subtree **plus** the always-load backbone, skipping the other product's
  depth. Define the always-load-vs-on-demand boundary so context stays lean in a multi-product repo.
  Fix the architect always/on-demand contradiction (finding 4) while in that file.

---

## Cross-reference

- Local decision: `docs/architecture/decisions.md` → **ADR-023**.
- Companion port-scoping work (what actually moves from the Fokus/Reflekt reference repos into this
  structure) is a separate effort — *not* in this file; this file is only the nexus-convention slice.
