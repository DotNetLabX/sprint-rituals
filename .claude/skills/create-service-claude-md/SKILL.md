---
name: create-service-claude-md
description: Architect-only. Captures every axis needed to scaffold a new service and writes src/Services/{Name}/CLAUDE.md. Run this before the Developer's create-service skill.
user-invocable: true
---

# Create Service CLAUDE.md (Architect Reference)

This skill is for the **Architect agent only**. It produces `src/Services/{Name}/CLAUDE.md` — the single source of truth for scaffolding a new service. The Developer's `create-service` skill reads this file and branches on it.

## When to use

When a new service has been decided (via `system-design` or direct discussion with the user) and the Architect needs to capture the decisions before handing off to the Developer.

## Steps

1. **Pre-fill from conversation context.** The Architect has typically been discussing the service before invoking this skill. Pull every answer you can from the existing conversation. Do not re-ask what the user already said.

2. **Walk through the full axis checklist** — follow `workflows/CaptureAxes.md`. For every axis, either confirm the pre-filled value or mark it as unanswered.

3. **Ask the user about every unanswered axis in one batch.** No sequential Q&A — one consolidated question. Do not write the file yet.

4. **Confirmation step.** Present the full captured axis list back to the user: "Here's what I captured — any changes?" Wait for the user to confirm or correct.

5. **Write the CLAUDE.md file** — follow `workflows/WriteClaudeMd.md`. Output path: `src/Services/{Name}/CLAUDE.md`.

6. **Tell the user:** "Service CLAUDE.md ready at `src/Services/{Name}/CLAUDE.md`. The Developer can now run `/create-service {Name}`."

## Arguments

Pass the service name: `create-service-claude-md Indexing`

## What this skill does NOT do

- Does not scaffold any project files or folders — that is the Developer's `create-service` skill.
- Does not update `the solution file`, `docker-compose.yml`, or any other repo-level file.
- Does not create aggregates, features, or any business code.
- Does not write the feature plan — that is the Architect's normal plan workflow.
