# Lessons File Format

All pipeline agents append under their own heading: `docs/specs/{slug}/delivery/lessons.md`. The PO writes lessons during spec shaping (before the delivery/ folder exists) — create the folder and lessons file if needed.

```
# {Feature Name} — Lessons

## PO Lessons
- Spec gaps the critic caught
- Research that changed a decision
- Questions that should have been asked earlier or differently

## Architect Lessons
- Plan instructions that were ambiguous
- Architecture decisions needing documentation
- Skill gaps discovered

## Developer Lessons
- Patterns discovered not yet documented
- Inconsistencies found
- Steps missing from a skill

## Reviewer Lessons
- Review criteria that were unclear
- Recurring code quality issues
- Patterns that should become conventions

## Solo Lessons
- Patterns discovered not yet documented
- Deviations from conventions found
- Missing skills or conventions

## Skill Gaps
- Skills referenced in plan steps but not found
- Recurring patterns that should become skills
```

For systemic issues that recur across features, append an improvement proposal inside the relevant heading:

```
### Improvement Proposal (optional, for systemic issues)
**Target:** {file path — agent file, skill, convention, or rule}
**Change:** {what to add/modify}
**Evidence:** {which features demonstrated this, with links}
**Priority:** {low/medium/high}
```

Only add items not already in CLAUDE.md, convention files, skills, or agent files. Update before `/compact` or `/clear`.

**Mandatory:** Every agent must write lessons before finishing its work. This is not optional — if you learned something (a gap, a pattern, a mistake, an ambiguity), write it down. If the lessons file doesn't exist yet, create it. If your heading already exists, append to it. No agent exits without writing lessons.
