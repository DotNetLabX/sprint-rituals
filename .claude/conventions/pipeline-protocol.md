# Pipeline Protocol

Pipeline-specific coordination rules. Loaded by pipeline agents (team-lead, architect, developer) via `@`.

For universal rules (slug, paths, communication model, cycle caps), see `.claude/rules/agents-workflow.md` (auto-loaded every session).

## Team Lead Rules

- Do not read files before delegating to agents. Send the file path in the message and let the agent read it.
- Only read a file yourself when you need its content to make a routing or coordination decision — not to relay it.
- Relay user questions about plan content to the architect — do not investigate or answer them yourself.
- **Triage all inter-agent messages.** Before forwarding, check: does this message reverse a user decision, change scope, or remove a plan step? If yes, escalate to the user first.

## Pipeline

```
Human -> PO (shape feature -> write spec)
                    |
         PO offers: cross-check or critic?
          | self                | critic
     PO cross-checks      PO spawns critic (Mode 1: spec vs v1.md)
          |                     | findings
     Status: Ready         PO fixes gaps -> Status: Ready
                    |
Human -> architect (analyze -- Phase 1)
                    |
              questions checkpoint (team lead triages)
                    |
         architect (write plan -- Phase 2)
                    |
         architect offers: self-review or critic?
          | self                | critic
     architect reviews     architect spawns critic (Mode 2: plan vs spec)
          |                     | findings
     plan approved         architect fixes gaps -> plan approved
                    |
         auto-approve always (split if >10 steps and each sub-plan >= 2 steps)
                    |
              developer (analyze plan -- Phase 1)
                    |
              questions checkpoint (team lead triages)
                    |
              developer (implement -- Phase 2 -> implementation.md)
                    |
              architect (Step 1: done check + write lessons)
               | fail          | pass
        developer (fix)    reviewer (Step 2: code review)
                           | approve       | request changes
                    team lead (close)    developer <-> reviewer
                                       (max 3 fix cycles)
                                            | exhausted
                                      architect (escalation)
```

## Checkpoint Report Format

Use this format at pipeline checkpoints: architect Phase 1 output, developer Phase 1 output, done check verdict, reviewer verdict.

```
{Phase Name} -- {Slug}
================================================
{2-4 headline metrics -- e.g., "10 steps analyzed, 0 questions, 3 patterns verified"}
{Table or list of key findings}
Needs your attention:
  1. {flagged item -- or "None"}
Action options:
  1. {default action} (recommended)
  2. {alternative}
  3. Stop
```

Team lead rule: if agent output at a checkpoint does not include action options, append them before relaying to the user.

## Message Handoffs (all via team lead)

Each handoff: trigger -> sender -> team lead action -> receiver.

### Developer -> Team Lead: Analysis complete (Phase 1)
**Trigger:** Developer finishes reading plan and exploring codebase.
**Developer says:** "For architect: Questions before implementing {FeatureName}: {list}" OR "All clear -- plan is unambiguous, patterns found, ready to implement."
**Team lead:** If questions -> forward to architect. If architect's answer would reverse a user decision -> ask user first. If "all clear" -> resume developer with "Implement."

### Developer -> Team Lead -> Architect: Ready for review
**Trigger:** Any implementation round complete -- implementation.md written/updated.
**Developer says:** "For architect: implementation.md written for {FeatureName}, ready for Step 1."
**Team lead:** Forward to architect.
**Architect:** Reads implementation.md against plan. Pass -> messages team lead for reviewer. Fail -> writes review.md, messages team lead for developer.

### Architect -> Team Lead -> Reviewer: Step 1 passed
**Trigger:** Done check passes.
**Architect:** Writes lessons to `docs/specs/{slug}/delivery/lessons.md` (has full context now -- plan vs implementation fresh in mind), then messages team lead.
**Architect says:** "For reviewer: Step 1 passed for {FeatureName}. Plan: docs/specs/{slug}/delivery/plan.md"
**Team lead:** Forward to reviewer.

### Reviewer -> Team Lead -> Developer: Fixes needed
**Trigger:** CRITICAL or HIGH issues found.
**Reviewer says:** "For developer: Fixes needed for {FeatureName}, see review.md. Cycle {N}/3."
**Team lead:** Forward to developer.

### Developer -> Team Lead -> Reviewer: Fixes applied
**Trigger:** All review items addressed.
**Developer says:** "For reviewer: Fixes applied for {FeatureName}, ready for re-review. Cycle {N}/3."
**Team lead:** Forward to reviewer.

### Reviewer -> Team Lead: Approved
**Trigger:** APPROVE verdict.
**Reviewer says:** "For team-lead: APPROVED: {FeatureName}."
**Team lead:** Writes summary.md, updates cross-references (spec Status, backlog). No architect wake-up needed -- lessons already written after Step 1.

### Reviewer -> Team Lead -> Architect: Escalation
**Trigger:** 3 fix cycles exhausted OR architecture decision needed.
**Reviewer says:** "For architect: ESCALATION for {FeatureName}: {reason}."
**Team lead:** Forward to architect.

### Developer -> Team Lead -> Architect: Question
**Trigger:** Blocker mid-implementation.
**Developer says:** "For architect: Blocked on step {N} for {FeatureName}. Question in questions.md."
**Team lead:** Read questions.md. If the answer would reverse a user decision, change scope, or remove a plan step -> **ask the user first** before forwarding. Otherwise, forward to architect.
**Architect:** Answers inline, updates plan if needed, messages team lead for developer.

### Architect answers that need user approval
**Trigger:** Architect's answer to a developer question would reverse a user decision, change scope, or remove a plan step.
**Architect says:** "For team lead: Question from developer requires user decision. Options: {A, B, C}. I recommend {X} because {reason}."
**Team lead:** Present options to user. Forward user's decision to architect. Architect updates plan and answers developer.
