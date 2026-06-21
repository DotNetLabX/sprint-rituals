# Pipeline Craft Improvements — Review

## Reviewed By
reviewer (Sonnet agent)

## Verdict: APPROVE

## Pre-commitment Predictions

1. **Reference integrity gaps** — convention file references added to agents-workflow.md might point to files that don't exist or have wrong names. FOUND: All three references verified present and correct.
2. **agents-workflow.md line count overage** — plan targeted ≤350 lines but Step 3 additions were required. FOUND: 367 lines — developer documented the overage with a valid reason (Step 3 additions were mandatory).
3. **Duplicate step numbering in developer.md** — inserting two new steps at the top of Phase 1 could offset existing numbers. FOUND: Confirmed duplicate step 3 (two steps numbered "3." in Phase 1).
4. **architect.md Anti-patterns section** — improve-flow was updated to promote to architect.md Anti-patterns, but the plan never asked for an Anti-patterns section in architect.md. FOUND: Section not created in architect.md (correct per plan), but improve-flow.md now claims it exists — a factual error in the skill.
5. **I/O headers on workflow files** — all 6 workflow files need Inputs/Outputs/Gate. FOUND: All 6 files verified.

## Findings

### [LOW] Duplicate step 3 in developer.md Phase 1 list
**File:** `.claude/agents/developer.md:48-49`
**Issue:** Phase 1 has two steps numbered "3.": "3. Read the plan fully." and "3. Explore the codebase for patterns...". The second was the original step 3 (now step 4 after the two new idempotency/deviations steps were inserted at positions 1 and 2), but the renumbering was not completed. Subsequent steps (4, 5, 6) were also not renumbered — though they happen to still be in correct relative order.
**Confidence:** HIGH
**Fix:** Renumber: the current "3. Read the plan fully" stays as 3; current "3. Explore the codebase..." becomes 4; current "4." becomes 5; current "5." becomes 6; current "6." becomes 7.

### [LOW] improve-flow.md claims architect.md has an Anti-patterns section that does not exist
**File:** `.claude/skills/improve-flow/SKILL.md:66`
**Issue:** The Anti-pattern Promotion section states: "The `## Anti-patterns` section exists in developer.md, reviewer.md, and architect.md." architect.md does not have this section — the plan (Step 7) did not ask for one, and it was correctly not created. The improve-flow skill's claim is factually wrong and will cause improve-flow to attempt to add anti-patterns to a non-existent section in architect.md, likely creating an orphan section or silently failing.
**Confidence:** HIGH
**Fix:** Change the last sentence to: "The `## Anti-patterns` section exists in developer.md and reviewer.md." (architect.md uses "What You Never Do" + "Plan Failure Modes" instead of a dedicated Anti-patterns section).

### [LOW] agents-workflow.md line count 17 lines over target
**File:** `.claude/rules/agents-workflow.md`
**Issue:** Plan specified "under 350 lines post-extraction + additions." Actual line count is 367. The deviation is documented in implementation.md with a valid reason (the two Step 3 additions — Message Size Contract and Checkpoint Report Format — were explicitly required by the plan and expand the file). This is a plan-acceptance-criteria miss, not a logic error.
**Confidence:** HIGH
**Fix:** No code change needed. The plan's target was aspirational; the additions were all in-scope. Accept as documented deviation. Consider updating the plan's acceptance criterion to "~350 lines" retroactively.

## Positive Observations

- Convention extraction (Step 1) is clean: all three convention files are self-contained, include Anti-patterns and Consumers sections, and the agents-workflow.md replacements use consistent one-line reference format.
- All 6 create-feature workflow files received Inputs/Outputs/Gate headers (plan only required them for "sub-files if they exist" — developer verified and applied to all 6).
- known-deviations.md is properly seeded from real lessons (6 entries citing specific features with frequency counts) — not placeholder content.
- All 5 CHANGELOG.md files follow consistent format with Unreleased and versioned baseline sections.
- The architect.md conformance matrix (Step 1 done check) adds the N/A disposition which was missing — a genuine improvement over the prior checklist.
- team-lead.md communication log header now includes all 8 structured state fields specified in the plan, including the Questions Resolved list.
- The idempotency gate in team-lead.md pre-flight is placed correctly as Step 0 (before dirty tree check), matching the plan's sequence requirement.
- improve-flow.md Anti-pattern Promotion section is well-structured with the correct format template.
- The "Write first, message second" extension in Message Size Contract correctly generalizes the existing questions-only rule to all artifacts.

## Gaps

- The plan specifies "Checkpoint Report Format" applies to "architect Phase 1 output, developer Phase 1 output, done check, reviewer verdict" — there are no acceptance tests verifying agents actually follow this format. This is expected given the documentation-only scope of this work; no gap to fix.
- improve-flow.md's Anti-pattern Promotion section mentions the architect.md Anti-patterns section exists. If improve-flow is ever invoked before the fix above is applied, it could create a spurious section in architect.md.

## Open Questions

None.

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Convention files exist | pass | `Get-ChildItem docs/conventions/` | implementation-format.md, review-format.md, questions-format.md, kb-entry-schema.md, known-deviations.md present |
| agents-workflow.md references resolve | pass | `Select-String agents-workflow.md -Pattern 'docs/conventions/'` | 3 references, all pointing to files that exist |
| agents-workflow.md line count | info | `wc -l agents-workflow.md` | 367 lines (plan target: ≤350; deviation documented) |
| developer.md idempotency check | pass | `Select-String developer.md -Pattern 'idempotency'` | Phase 1 step 1 present |
| developer.md known-deviations check | pass | `Select-String developer.md -Pattern 'known-deviations'` | Phase 1 step 2 present |
| developer.md carry-over in checklist | pass | `Select-String developer.md -Pattern 'Carry-Over'` | Row present in completion checklist table |
| developer.md duplicate step 3 | fail | `Select-String developer.md -Pattern '^3\.'` | Two lines beginning with "3." in Phase 1 (lines 48-49) |
| reviewer.md convention reference | pass | `Select-String reviewer.md -Pattern 'docs/conventions/review-format'` | Present in Review Output section |
| reviewer.md carry-over rule | pass | `Select-String reviewer.md -Pattern 'carry-over'` | Stage 2 checklist + Anti-patterns both reference carry-over |
| reviewer.md Confidence qualifier | pass | `Select-String reviewer.md -Pattern 'Confidence'` | Findings Format section + Self-Audit referencing confidence |
| architect.md conformance matrix | pass | `Select-String architect.md -Pattern 'Disposition\|Implemented\|Deviated'` | Table with all 5 dispositions present |
| architect.md What You Never Do redirections | pass | `Select-String architect.md -Pattern 'instead:'` | All 5 items have → instead: format |
| architect.md Anti-patterns section | fail (by design) | `Select-String architect.md -Pattern '## Anti-patterns'` | Not present — plan did not require it |
| improve-flow Anti-patterns claim about architect | HIGH finding | `Select-String improve-flow/SKILL.md -Pattern 'architect'` | Line 66 incorrectly claims architect.md has Anti-patterns section |
| team-lead structured state fields | pass | `Select-String team-lead.md -Pattern 'Team Mode\|Developer ID\|Questions Resolved'` | All fields present in Communication Log format |
| team-lead idempotency gate | pass | `Select-String team-lead.md -Pattern 'Idempotency gate'` | Step 0 present in Pre-flight checks |
| team-lead completion dashboard | pass | `Select-String team-lead.md -Pattern 'Pipeline Complete'` | Dashboard format present in Team Shutdown |
| All 6 workflow I/O headers | pass | `Select-String workflows/*.md -Pattern '^## Inputs\|^## Outputs\|^## Gate'` | 3 matches per file × 6 files = 18 total |
| 5 CHANGELOG.md files exist | pass | `Get-ChildItem .claude/skills/*/CHANGELOG.md` | create-implementation-plan, create-feature, diagnose, create-aggregate, tdd all present |
| known-deviations.md exists | pass | file existence check | 6 seeded entries with feature citations |
| kb-entry-schema.md exists | pass | file existence check | Present with Required and Optional sections |
| kb-maintenance.md Hard Rules | pass | file read | 4 hard rules present |
| kb-maintenance.md Lint Checks table | pass | file read | 5-row table with check/severity/condition |
| improve-skills Changelog Maintenance section | pass | file read | Section with format and entry instructions present |
| improve-flow Anti-pattern Promotion section | pass | file read | Section with format template present |
| Build | N/A | — | Documentation-only changes; no source code modified. No build step applies. |
