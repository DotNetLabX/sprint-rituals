---
name: improve-flow
description: Applies promoted lessons to flow artifacts — CLAUDE.md, agent files (.claude/agents/), and rule files (.claude/rules/). Invoked by the learner agent after classification and user approval. Do not invoke directly — the learner handles classification and approval.
---

# Improve Flow

Applies classified and approved lesson items to their target flow files. Each item has already been classified by the learner agent with a specific target, section, and action.

## Steps

1. **Receive the promotion list** — a set of items, each with: content, target file, target section, and action (add or update).

2. **For each target file**, grouped to minimize file reads:
   a. Read the current file content fully.
   b. Find the target section for each item going to this file.
   c. Final dedup check — verify the item isn't already expressed (different wording, same concept).
   d. Insert or update the item in the appropriate section.
   e. Preserve the file's existing style, formatting, and indentation.

3. **Verify consistency** — after all edits:
   - No duplicate entries introduced.
   - No sections broken by insertions.
   - File still reads coherently as a whole.

4. **Report** — return a list of all changes made: file path, section, what was added or updated.

## Target File Conventions

### CLAUDE.md

- Guardrails go under `## Guardrails — DO NOT`
- Framework-specific gotchas go near related existing sections. Create a subsection only if there's no natural fit.
- Conventions go near related existing conventions.
- Match the existing bullet style (` - **Bold lead** — explanation`).

### Agent files (`.claude/agents/`)

- **architect.md** — planning discipline items. Integrate into existing workflow sections (e.g., "Plan Writing Rules", "Before Acting") when the item extends an existing rule. Add a new section only when no existing section fits.
- **developer.md** — implementation discipline items. Same approach — extend "How You Work", "Exploration Before Implementation", or "Circuit Breaker" sections.
- **reviewer.md** — review calibration items. Extend "Stage 2: Code Quality", "Gap Analysis", or "Fresh Evidence" sections.
- Prefer integrating into existing sections over creating new ones. New sections fragment the agent's instructions.

### Rule files (`.claude/rules/`)

- Cross-cutting rules that apply to all agents or across the entire project.
- Add to an existing rule file if the topic matches.
- Create a new `.claude/rules/{topic}.md` file only if no existing file covers the topic.
- Rule files are auto-loaded every session — keep them concise.

## Style Rules

- Match the target file's voice and density. Agent files are direct and imperative. CLAUDE.md uses structured lists.
- Don't add attribution ("learned from JiraSync"). The lesson stands on its own.
- Don't add dates. The git history tracks when it was added.
- Condense the lesson to its actionable essence. Strip the story, keep the rule.

## Anti-pattern Promotion

When a lesson recurs 2+ times across features and maps to an agent file, check if that agent has an `## Anti-patterns` section. If yes, promote the recurring lesson as a new anti-pattern entry. Format:

```markdown
- **{Pattern name}.** {What goes wrong}. {Correct approach instead}.
```

Anti-patterns are a valid promotion target alongside rules, conventions, and agent instructions. The `## Anti-patterns` section exists in developer.md and reviewer.md.

## Post-Apply

After modifying agent files, rule files, or conventions, read `.claude/README.md` and update it if the change affects the system overview (new/removed agents, new rule files, changed information layers).

## Cross-Reference Lint

Before renaming or removing a rule, convention, or agent reference, grep all agent files, rule files, and convention files for references to the old name. Update or remove stale references before completing the change.

## What This Skill Does NOT Do

- Classify items — the learner already did that.
- Create or modify skills — that's `improve-skills`.
- Write application code.
- Decide what gets promoted — the learner decides, user approves.
- Tag items as [APPLIED] — the learner handles tagging after skill completes.
