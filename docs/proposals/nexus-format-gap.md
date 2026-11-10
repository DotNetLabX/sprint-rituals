# Nexus Plugin — Artifact-Format Gap Evaluation

**Status:** Evaluation / advisory (no code changes). For use in the plugin-build project.
**Date:** 2026-06-06
**Source project:** `sprint-rituals` (consumes `nexus` + `nexus-dotnet` plugins)
**Context:** While de-duplicating this project's local `.claude/` against the installed nexus plugins (full plugin-canonical: removed all plugin-provided agents/rules/skills, kept only project-only files), the `.claude/conventions/` folder was found to be **orphaned** — and the content turned out to be *missing from the plugin*, not duplicated by it.

---

## 1. The gap

The nexus plugin's `rules/agents-workflow.md` (Artifact Formats table) tells agents to use these **skills**:

| Artifact | Referenced as | Ships in plugin? |
|----------|---------------|------------------|
| Plan | `create-implementation-plan` skill | ✅ yes |
| Implementation | `implementation-format` skill | ❌ **missing** |
| Review | `review-format` skill | ❌ **missing** |
| Questions | `questions-format` skill | ❌ **missing** |
| Lessons | `lessons-format` skill | ❌ **missing** |
| Summary | `summary-format` skill | ❌ **missing** |

`agents/architect.md` also references the `questions-format` skill directly.

Exhaustive search across `nexus`, `nexus-dotnet`, and `nexus-net` confirms the 5 format definitions exist **nowhere** in the plugin — not as files, not as skills, and not inlined in any agent (distinctive content phrases return zero hits). They were almost certainly dropped during `export-cc-setup` when the plugin was exported from a source project.

**Consequence:** every nexus pipeline run references 5 format-skills that resolve to nothing; agents improvise the artifact shape. A consuming project only works because it carries its own `.claude/conventions/*.md` copies — which is the redundancy this cleanup was trying to eliminate. Shipping the formats in the plugin closes the loop: a nexus-using project then needs **zero** local `conventions/`.

---

## 2. Where each piece belongs

These are **pipeline/process** artifacts → they belong in the generic **`nexus`** plugin, not `nexus-dotnet`. Per the export rule, the pipeline layer must be **0% stack-coupled**, so anything .NET/DDD/tool-specific must be scrubbed before shipping.

| File (source) | Target in plugin | Portability | Scrub before shipping |
|---------------|------------------|-------------|------------------------|
| `implementation-format.md` | **skill** `nexus/skills/implementation-format/SKILL.md` | ✅ clean (KB refs OK — nexus owns KB rules) | none |
| `questions-format.md` | **skill** `nexus/skills/questions-format/SKILL.md` | ✅ clean | none |
| `lessons-format.md` | **skill** `nexus/skills/lessons-format/SKILL.md` | ✅ clean | none |
| `summary-format.md` | **skill** `nexus/skills/summary-format/SKILL.md` | ✅ clean | none |
| `review-format.md` | **skill** `nexus/skills/review-format/SKILL.md` | ⚠️ 2 stack leaks | "Domain rules enforced **in aggregates, not handlers**" (DDD assumption); example path `Features/Foo/FooEndpoint.cs` (FastEndpoints). Genericize both to architecture-neutral wording. |
| `pipeline-protocol.md` | **already inlined** in plugin agents (architect/team-lead carry an identical Coordination Protocol) — do **not** ship as a separate file | ⚠️ 1 project leak | "critic (Mode 1: spec vs **v1.md**)" → "spec vs product spec" |
| `team-lead-operations.md` | **inline into plugin `agents/team-lead.md`** (team-lead-only reference) | ⚠️ leaks + high value | "`/codex:rescue`" (codex plugin) → make optional/generic; example slug "F31-DarkLightTheme" → neutral. Verify the plugin's team-lead already carries: spawn modes, timeout/stall recovery, commit protocol, communication-log format, unattended defaults. |

---

## 3. The 5 format skills (drop-in)

Each source file's body **is** the format spec. Wrap it as a skill by prepending frontmatter; the skill `name` must match the reference in `agents-workflow.md` exactly.

```
nexus/skills/implementation-format/SKILL.md   name: implementation-format
nexus/skills/review-format/SKILL.md           name: review-format        (scrub DDD/FastEndpoints leaks)
nexus/skills/questions-format/SKILL.md         name: questions-format
nexus/skills/lessons-format/SKILL.md           name: lessons-format
nexus/skills/summary-format/SKILL.md           name: summary-format
```

Frontmatter pattern (example):

```yaml
---
name: implementation-format
description: Format spec for docs/specs/{slug}/delivery/implementation.md — sections, anti-patterns, consumers. Load when writing or reviewing an implementation file.
---
```

Body = the current file content verbatim (review-format scrubbed). The `## Anti-patterns` and `## Consumers` sections carry over unchanged — they're generic.

Suggested descriptions:
- **review-format** — "Format spec for the Step-2 review (severity ratings, verdict, findings, evidence). Load when writing or consuming review.md."
- **questions-format** — "Format spec for docs/specs/{slug}/delivery/questions.md — one blocker per section, To-field routing. Load when an agent is blocked."
- **lessons-format** — "Format spec for docs/specs/{slug}/delivery/lessons.md — per-role headings + improvement proposals. Load before any agent finishes its work."
- **summary-format** — "Format spec for docs/specs/{slug}/delivery/summary.md — written by team-lead after approval; its existence means the pipeline completed."

---

## 4. Related plugin gaps to fix in the same pass

1. **Skill-inventory scan blind spot (highest impact).** `create-implementation-plan` and `create-architecture-doc` build their skill inventory by *scanning `.claude/skills/`*. When skills ship from the plugin directory (not the project's `.claude/skills/`), that scan finds nothing, so every plan's Skill Mapping under-reports and architecture docs miss skill coverage. Decide how the inventory step discovers **plugin** skills (e.g., enumerate available skills rather than globbing `.claude/skills/`). This is the core design issue exposed by going plugin-canonical.

2. **`kb-entry-schema` reference form.** Plugin `kb-maintenance.md` correctly points to the `kb-entry-schema` *skill* (which ships). Keep the skill form; don't reintroduce a `docs/conventions/kb-entry-schema.md` path reference.

3. **Pipeline-layer leak audit.** Re-run the `export-cc-setup` leak check after adding these — the review-format DDD/FastEndpoints wording and the `v1.md` / `/codex:rescue` references are exactly the class of leak that check exists to catch.

---

## 5. Recommendation

1. Add the **5 formats as skills in `nexus`** (scrub review-format's 2 leaks).
2. **Do not** add `pipeline-protocol` / `team-lead-operations` as files — fold any missing detail into the already-inlined agent protocol and `agents/team-lead.md`, scrubbing the `v1.md` / `/codex:rescue` / example-slug leaks.
3. Fix the **skill-inventory scan** so plan/architecture-doc generation sees plugin-provided skills.

Net result: a project consuming nexus needs **zero** local `conventions/`, the 5 format-skill references resolve, and the pipeline stops improvising artifact shapes.

---

## 6. Bonus: `engineering-discipline.md` (optional plugin split)

`engineering-discipline.md` is an auto-loaded project rule (`.claude/rules/`) acting as a binding quality bar for every agent. **Verified absent from `nexus`, `nexus-dotnet`, and `nexus-net`** — not as a file, not inlined under another name. It's a project addition, never part of any nexus plugin.

About half of it is portable, though. If you want this discipline in the plugin, split it by layer:

| Section | Belongs in | Notes |
|---------|-----------|-------|
| **Problem-class rule** (fix the class + root cause, not the instance) | `nexus` (generic rule) | Stack-neutral. Pure engineering discipline. |
| **Skill-adherence guards** (plans map every step to a skill; "adapt" needs written justification; reviewer checks the mapping; skills stay portable + compilable; arch docs defer to skills) | `nexus` (generic rule) | Stack-neutral and reinforces nexus's skill-authority model directly. |
| **Decision order when code smells** (skill → reference project → fix/create the skill; never invent when a skill/reference exists) | `nexus` (generic rule) — but **parameterize the reference** | The *concept* is portable. The hardcoded path to the reference codebase is project-only → make "reference project" a placeholder the consuming project fills. |
| **Framework currency** (.NET 10, FastEndpoints `Send.*`, `Directory.Packages.props`) | `nexus-dotnet` (convention) | .NET-specific. |
| **Key coding decisions** (domain services not API service classes, no DTO in Domain, rich aggregates, feature file/naming shape, Mapster vs factory, thin `Program.cs` + module wiring) | `nexus-dotnet` (convention) | .NET/DDD-specific; digest of the project's ADR register. |
| The actual reference-project path + ADR-register link | **project-only** | Stays in the consuming project. |

Recommended shape: a generic `nexus/rules/engineering-discipline.md` (problem-class + skill-adherence + parameterized decision-order) plus the .NET specifics folded into `nexus-dotnet` conventions. A consuming project then keeps only its reference-project path and ADR link.

---

## Appendix — current source files (this project)

The clean, ready-to-wrap source bodies live in this project at:

```
.claude/conventions/implementation-format.md
.claude/conventions/review-format.md          ← scrub before shipping
.claude/conventions/questions-format.md
.claude/conventions/lessons-format.md
.claude/conventions/summary-format.md
.claude/conventions/pipeline-protocol.md      ← duplicate of inlined protocol; mine only the v1.md fix
.claude/conventions/team-lead-operations.md   ← inline into team-lead.md; scrub codex/slug leaks
```
