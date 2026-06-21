# Skill Enforcement — Implementation

## Files Created
- `.claude/skills/create-architecture-doc/SKILL.md` — Skill definition for generating/updating architecture docs with mandatory skill inventory and skill-deferred decisions. Follows `create-feature-spec/SKILL.md` structure (frontmatter, reading protocol, steps, arguments, exclusions).
- `.claude/skills/create-architecture-doc/references/template.md` — Architecture doc output template with Skill Inventory table, "Not specified here" fields on every decision, and Gaps section for areas lacking skills.
- `.claude/skills/create-implementation-plan/SKILL.md` — Skill definition for creating implementation plans with mandatory skill mapping per step. Includes reading protocol, disposition rules (Follow/Build/None), and anti-pattern check.
- `.claude/skills/create-implementation-plan/references/plan-template.md` — Plan output template extending the agents-workflow.md format with Skill Mapping table, disposition rules, and anti-pattern rules.

## Files Modified
- `.claude/agents/architect.md` — Added both skills to "What You Know" section. Added "Architecture Doc Workflow" section before Plan Workflow. Inserted `create-implementation-plan` skill reference as step 2 of Plan Workflow. Simplified Plan Writing Rules to defer to the skill (removed three rules now owned by the skill, kept performance approach, pattern examples, and full file paths rules).
- `.claude/rules/agents-workflow.md` — Added Skill Mapping note at top of Implementation Plan Format section. Added skill mapping verification check to Step 2 review checklist. Added enforcement paragraph to Skill Authority section referencing both new skills.

## Key Decisions
- Kept three Plan Writing Rules that are not covered by the skill (performance approach, pattern examples, full file paths) rather than deleting the entire section. The skill owns skill-reference enforcement; these remaining rules are orthogonal concerns.
- Used `references/` subdirectory (not `workflows/`) for templates, matching the plan's specification and distinguishing from the `create-feature-spec` skill's `workflows/` directory which contains a different kind of artifact.

## Deviations from Plan
- None.
