# Agent & Rules File Cleanup

**Feature Spec:** None (ad-hoc cleanup)

## Context

Audit of `.claude/agents/` and `.claude/rules/` found duplicate content, contradictions with the canonical workflow protocol (`agents-workflow.md`), and a stale agent from a different project. This cleanup removes duplicates, fixes contradictions, and consolidates scattered rules -- without changing any behavior.

## Scope

**In scope:**
- Delete 3 files (stale agent, duplicate rule, rule being merged)
- Trim 2 duplicate sections from CLAUDE.md
- Consolidate model routing into pipeline-guardrails.md
- Fix 3 contradictions in architect.md
- Trim duplicate agent-selection section from team-lead.md
- Update auto-approve rule in agents-workflow.md and architect.md
- Add v2.md reference to critic.md and po.md
- Consolidate severity ratings (trim from reviewer.md, reference agents-workflow.md)
- Consolidate review output format (trim from reviewer.md, reference agents-workflow.md)

**Out of scope:**
- Any behavioral changes to the pipeline
- Source code modifications
- Content changes to product specs, architecture docs, or skills

## Safety Invariants (must NOT change)

- Hub-and-spoke messaging: all agent messages route through team lead. No instruction may bypass this.
- Pipeline sequence: architect -> developer -> architect (done check) -> reviewer.
- Severity definitions: CRITICAL/HIGH/MEDIUM/LOW meaning and verdict rules remain identical.
- Review output format: the template structure stays the same.
- All existing rules remain in force -- they just live in fewer places.

## Implementation Steps

### Step 1: Delete stale and duplicate files

Delete these 3 files:
- `.claude/agents/codebase-scanner.md` -- belongs to a different project ("Articles Application"). References Carter, MediatR IRequest, JWT, Redis.OM, multi-tenancy -- none exist in Reflekt.
- `.claude/rules/spec-authority.md` -- verbatim duplicate of CLAUDE.md "Authority & Spec Files" section (line 9).
- `.claude/rules/agent-model-routing.md` -- content being merged into `pipeline-guardrails.md` in Step 3.

**Accept:** Files removed from disk. No other file references these paths (they are standalone, not `@`-imported anywhere).

### Step 2: Trim CLAUDE.md duplicate sections

File: `CLAUDE.md`

**Remove the `## Agents` section** (lines 3-5):
```
## Agents

When the user says "be {Agent}", read `.claude/agents/{Agent}.md` and follow it.
```
This is already in `.claude/rules/agent-invocation.md` which auto-loads every session.

**Remove the `## Codebase Graph` section** (lines 157-163):
```
## Codebase Graph

This project has a graphify knowledge graph at `graphify-out/`.

- Before answering architecture or codebase questions, read graphify-out/GRAPH_REPORT.md for god nodes and community structure
- If graphify-out/wiki/index.md exists, navigate it instead of reading raw files
- After modifying code files in this session, run `graphify update .` to keep the graph current (AST-only, no API cost)
```
This is a verbatim duplicate of the `## graphify` section (lines 11-18) which remains.

**Accept:** CLAUDE.md has exactly one copy of each rule. The `## graphify` section (near top) is the surviving copy. The `## Authority & Spec Files` section (line 8-9) is the surviving copy of the spec authority rule.

### Step 3: Update pipeline-guardrails.md (merge model routing, remove CLAUDE.md duplicates)

File: `.claude/rules/pipeline-guardrails.md`

**Remove the first 2 bullets** (lines 3-4):
```
- **No god folders** (`Services/`, `Helpers/`, `Utils/`).
- **No `.gitkeep` files** -- don't track empty directories. Directories are created at runtime or when files are added.
```
These are verbatim duplicates of CLAUDE.md Guardrails (lines 152, 155). CLAUDE.md is the canonical location.

**Expand the "No Opus for general-purpose agents" bullet** to include the code example from the deleted `agent-model-routing.md`:

Before:
```
- **No Opus for general-purpose agents** -- always set `model: "sonnet"`. General-purpose agents do research and summarization, not deep reasoning.
```

After:
```
- **No Opus for general-purpose agents** -- always set `model: "sonnet"`. General-purpose agents do research and summarization, not deep reasoning.

  ```
  Agent({
    subagent_type: "general-purpose",
    model: "sonnet",   // REQUIRED
    ...
  })

  Agent({
    subagent_type: "Explore",
    model: "sonnet",   // REQUIRED
    ...
  })
  ```
```

**Accept:** `pipeline-guardrails.md` has 5 bullets (down from 7) plus a code example. No god-folders or .gitkeep rules here (those live in CLAUDE.md only). Model routing code example is preserved.

### Step 4: Fix architect.md contradictions

File: `.claude/agents/architect.md`

**4a. Fix hub-and-spoke violation in Plan Workflow step 9** (line 138).

Before:
```
9. **Auto-approve gate (after quality gate is resolved):** If the plan has <=11 steps AND you have no open questions for the human, consider the plan auto-approved -- message the **developer** directly to begin implementation. Do not message team lead for relay. Do not wait for human approval.
10. **If the plan has >11 steps or you have open questions:** Message the team lead with the plan summary and wait for human approval before proceeding. If >11 steps, also recommend how to split the developer (e.g., backend + frontend), including which steps go to which developer. The team lead decides.
11. User reviews and annotates. Revise until approved.
```

After:
```
9. **Auto-approve gate (after quality gate is resolved):** Plans are always auto-approved after architect/critic review. No human approval needed. Message the team lead: "For developer: Plan approved for {FeatureName} ({N} steps). Begin implementation."
10. **Splitting large plans:** If a plan has more than 10 steps, automatically consider splitting it into sequential sub-plans. Proceed with the split if each resulting sub-plan would have at least 2 steps. If splitting would produce any sub-plan with fewer than 2 steps, continue with the whole plan unsplit. When splitting, message the team lead with the sub-plan breakdown before proceeding.
```

This fixes: (a) the hub-and-spoke violation (architect now messages team lead, never developer directly), (b) the auto-approve threshold (now always auto-approve), (c) the splitting rule (new user-specified criteria).

**4b. Remove the "After Approval" section** (lines 213-220).

Remove:
```
## After Approval

When reviewer sends "APPROVED: {FeatureName}":

1. Update `docs/specs/{slug}/delivery/lessons.md` under `## Architect Lessons`.
2. Report to team lead: "APPROVED: {FeatureName}. Lessons written."

The team lead handles pipeline closure (summary.md, cross-references, backlog updates).
```

This contradicts `agents-workflow.md` which says:
- Lessons are written after Step 1 done check (line 113-116), not after final approval.
- After reviewer approval: "No architect wake-up needed -- lessons already written after Step 1" (line 132).

The correct timing is already documented in the "Step 1: Done Check" section of architect.md (line 178-179: "If pass: Message reviewer...") combined with `agents-workflow.md` line 113-116. No replacement text needed.

**Accept:** architect.md has no instruction to message developer directly. Plan auto-approve is unconditional. Lessons are written at Step 1 only. The "After Every Review Cycle" section (line 222-224) remains -- it covers updating lessons during review cycles, which is a different (valid) concern.

### Step 5: Trim team-lead.md Agent Selection section

File: `.claude/agents/team-lead.md`

**Replace the "Agent Selection" section** (lines 72-78):

Before:
```
## Agent Selection

**Pipeline roles** (architect, developer, reviewer, tester, teacher, builder) -> always use project agents from `.claude/agents/`. Never substitute OMC equivalents (oh-my-claudecode:planner, oh-my-claudecode:executor, oh-my-claudecode:architect) for pipeline work.

**OMC pipeline orchestration** (oh-my-claudecode:team, oh-my-claudecode:autopilot, oh-my-claudecode:ralph) -> never replaces the project pipeline.

**OMC specialists** (oh-my-claudecode:debugger, oh-my-claudecode:security-reviewer, oh-my-claudecode:tracer, oh-my-claudecode:code-simplifier, oh-my-claudecode:designer, etc.) -> available for standalone tasks outside the pipeline when the user requests them or the task benefits from specialized analysis.
```

After:
```
## Agent Selection

See `pipeline-guardrails.md` for agent selection rules.
```

**Accept:** team-lead.md Agent Selection is a one-liner reference. The full rules remain in `pipeline-guardrails.md` (which auto-loads).

### Step 6: Update auto-approve rule in agents-workflow.md

File: `.claude/rules/agents-workflow.md`

**Update the Pipeline diagram comment** (line 89):

Before:
```
         auto-approve if <=11 steps & no open questions, else human approves
```

After:
```
         auto-approve always (split if >10 steps and each sub-plan >= 2 steps)
```

**Accept:** The pipeline diagram reflects the new auto-approve rule. No other text in agents-workflow.md references the old threshold (the detail lives in architect.md).

### Step 7: Add v2.md reference to critic.md and po.md

**File: `.claude/agents/critic.md`** (line 15)

Before:
```
@docs/product/v1.md
```

After:
```
@docs/product/v1.md
@docs/product/v2.md
```

**File: `.claude/agents/po.md`** (line 15)

Before:
```
@docs/product/v1.md
```

After:
```
@docs/product/v1.md
@docs/product/v2.md
```

**Accept:** Both agents load v1.md and v2.md. No other changes to these files.

### Step 8: Consolidate severity ratings and review output in reviewer.md

File: `.claude/agents/reviewer.md`

**8a. Replace the severity ratings block** (lines 56-63).

Before:
```
## Severity Ratings

Every finding gets a severity:

- **CRITICAL**: Security vulnerability, data loss risk, fundamentally wrong approach. Blocks merge.
- **HIGH**: Logic error, missing error handling, plan deviation without justification. Should fix.
- **MEDIUM**: Suboptimal pattern, minor inconsistency. Consider fixing.
- **LOW**: Style preference, minor improvement. Optional.
```

After:
```
## Severity Ratings

Severity ratings defined in `agents-workflow.md` (Review Checklist section). Every finding gets a severity rating per those definitions.
```

**8b. Replace the review output template** (lines 108-143).

Before (the full Review Output section with embedded template):
```
## Review Output

**Always write the file first, then message.** Do not include review findings in SendMessage -- the message is a notification, not the review itself. The file is the paper trail.

Write to `docs/specs/{slug}/delivery/review.md`:

[... full template ...]
```

After:
```
## Review Output

**Always write the file first, then message.** Do not include review findings in SendMessage -- the message is a notification, not the review itself. The file is the paper trail.

Write to `docs/specs/{slug}/delivery/review.md` following the Review Output Format in `agents-workflow.md`.

In the `## Reviewed By` section, state who performed this review: `reviewer` (Sonnet agent), `/review` (skill), `/codex:rescue` (Codex), or any combination. If Codex cross-validation was requested but unavailable, note that here.
```

The "Reviewed By" guidance is the only addition reviewer.md had beyond the canonical template, so it is preserved as a note. The canonical template in `agents-workflow.md` already includes the `## Reviewed By` placeholder (line 348-349).

**Accept:** reviewer.md references `agents-workflow.md` for both severity definitions and review output format. The "Reviewed By" guidance is preserved. No severity definition or template structure is lost.

## Testing Strategy

- Verify all 3 deleted files no longer exist on disk.
- Verify CLAUDE.md has no `## Agents` section and no `## Codebase Graph` section, but retains `## graphify` and `## Authority & Spec Files`.
- Verify `pipeline-guardrails.md` has 5 bullets (not 7) and includes the model routing code example.
- Verify `architect.md` never contains "message the developer directly" or "Do not message team lead for relay". Verify no "After Approval" section exists. Verify auto-approve text matches the new rule.
- Verify `team-lead.md` Agent Selection section is a one-liner.
- Verify `agents-workflow.md` pipeline diagram has updated auto-approve comment.
- Verify `critic.md` and `po.md` both have `@docs/product/v2.md`.
- Verify `reviewer.md` severity section references `agents-workflow.md`. Verify review output section references `agents-workflow.md` and retains "Reviewed By" guidance.
- Read each modified file end-to-end to confirm no accidental deletions or formatting breaks.

## Open Questions

None.
