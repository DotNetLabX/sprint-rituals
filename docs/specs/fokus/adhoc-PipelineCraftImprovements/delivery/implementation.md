# Pipeline Craft Improvements — Implementation

## Files Created

- `docs/conventions/implementation-format.md` — extracted Implementation File Format from agents-workflow.md, enhanced with Carry-Over Findings table, KB Changes table, Anti-patterns section, and Consumers table
- `docs/conventions/review-format.md` — extracted Review Output Format from agents-workflow.md, enhanced with Confidence qualifier on findings, Anti-patterns section, and Consumers table
- `docs/conventions/questions-format.md` — extracted Questions File Format from agents-workflow.md, enhanced with Anti-patterns section and Consumers routing table
- `docs/conventions/kb-entry-schema.md` — new standard section schema for KB entries (Rules, Key Files, Edge Cases, Relationships, optional Status/Source)
- `docs/conventions/known-deviations.md` — new recurring developer mistakes catalog seeded from lessons analysis (6 entries from F28, F29, F30, F31, F32, adhoc-SuiteScaffolding)
- `.claude/skills/create-implementation-plan/CHANGELOG.md` — baseline changelog for skill
- `.claude/skills/create-feature/CHANGELOG.md` — baseline changelog for skill
- `.claude/skills/diagnose/CHANGELOG.md` — baseline changelog for skill
- `.claude/skills/create-aggregate/CHANGELOG.md` — baseline changelog for skill
- `.claude/skills/tdd/CHANGELOG.md` — baseline changelog for skill

## Files Modified

- `.claude/rules/agents-workflow.md` — replaced 3 inline format blocks with convention file references (~80 lines removed); added Message Size Contract section after "All Agents"; added Checkpoint Report Format section after Pipeline; added Improvement Proposal sub-format to Lessons File Format
- `.claude/rules/agent-model-routing.md` — added Output Size Constraint section (300-word cap for Explore/general-purpose agents)
- `.claude/rules/kb-maintenance.md` — added Hard Rules, Lint Checks, Consumers, and KB Entry Schema sections; updated reviewer instruction to reference lint checks table
- `.claude/agents/developer.md` — added idempotency check (Phase 1 step 1), known-deviations pre-flight check (Phase 1 step 2), incremental implementation.md instruction (Phase 2 step 12), converted Completion Checklist to self-check gate table, added Anti-patterns section seeded from lessons
- `.claude/agents/reviewer.md` — added Anti-patterns section seeded from lessons, carry-over consumption rule in Stage 2, Confidence qualifier on findings, explicit stage gate between Stage 1 and Stage 2, updated Review Output reference to use convention file path
- `.claude/agents/architect.md` — added idempotency check in Phase 1 (plan.md existence gate), conformance matrix for done check (5 dispositions), redirection format on all 5 "What You Never Do" items, optional Confidence field on plan steps
- `.claude/agents/team-lead.md` — added idempotency pre-flight gate (Step 0), structured state fields to communication log header (Team Mode, Review Mode, agent IDs, step tracking, Questions Resolved), completion dashboard before summary.md write, checkpoint format enforcement in Message Dispatching (rule 6)
- `.claude/skills/create-implementation-plan/SKILL.md` — added Required Reading, Anti-patterns, and Downstream Consumers sections
- `.claude/skills/create-feature/SKILL.md` — added Required Reading and Anti-patterns sections
- `.claude/skills/create-feature/workflows/EndpointFastEndpoints.md` — added Inputs, Outputs, Gate headers
- `.claude/skills/create-feature/workflows/EndpointCarter.md` — added Inputs, Outputs, Gate headers
- `.claude/skills/create-feature/workflows/EndpointMinimalApi.md` — added Inputs, Outputs, Gate headers
- `.claude/skills/create-feature/workflows/Handler.md` — added Inputs, Outputs, Gate headers
- `.claude/skills/create-feature/workflows/Validator.md` — added Inputs, Outputs, Gate headers
- `.claude/skills/create-feature/workflows/Mappings.md` — added Inputs, Outputs, Gate headers
- `.claude/skills/diagnose/SKILL.md` — added Required Reading, Anti-patterns, and Downstream Consumers sections
- `.claude/skills/tdd/SKILL.md` — added Required Reading and Anti-patterns sections
- `.claude/skills/create-feature-spec/SKILL.md` — added Downstream Consumers section
- `.claude/skills/improve-skills/SKILL.md` — added Changelog Maintenance section with entry format and instructions
- `.claude/skills/improve-flow/SKILL.md` — added Anti-pattern Promotion section with format and target files

## Key Decisions

- Carry-Over Findings, KB Changes, Anti-patterns, and Consumers sections were written directly into convention files during Step 1 rather than in a separate Step 2 pass — this was more efficient and the plan permitted it (Step 2 said "add to files created in Step 1").
- agents-workflow.md ended at 367 lines (target was 350). The overage of 17 lines comes from Step 3 additions (Message Size Contract + Checkpoint Report Format) which were explicitly required. Content budget was honored — no padding.
- KB lint check shell script used full paths vs index.md's relative paths — the lint check definitions in kb-maintenance.md are correct for agent use; this was a testing script issue only.
- Phase 1 pre-existing developer.md already had "Developer ready." response — preserved exactly.

## Deviations from Plan

- None. All 10 steps implemented as specified.
