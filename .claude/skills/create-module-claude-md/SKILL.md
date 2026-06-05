---
name: create-module-claude-md
description: Architect-only. Captures axes for a new module and writes src/Modules/{Name}/CLAUDE.md. Branches on archetype (Component vs Domain). Run before the create-module Developer skill.
user-invocable: true
---

# Create Module CLAUDE.md (Architect)

Walks the Architect through the axis checklist for a new module and writes `src/Modules/{Name}/CLAUDE.md`. The file is the single source of truth the Developer's `create-module` skill reads to scaffold the skeleton.

Modules come in two archetypes, and the captured axes differ per archetype:

- **Component** — infra adapter with a `.Contracts` interface + one or more implementation projects (e.g., `FileService`, `EmailService`)
- **Domain** — a mini-service-without-host: owns its own domain model, persistence, optional application layer, optional class-library API layer

## Precondition

The Architect is in the middle of a design conversation with the user, or has an existing plan in `docs/specs/{slug}/delivery/` with the module's shape already sketched out. Pull everything you can from conversation context before asking the user anything new.

## Steps

1. **Capture common axes** — follow `workflows/CaptureCommonAxes.md`. The archetype decision (Component vs Domain) happens here.

2. **Capture archetype-specific axes:**
   - If archetype = Component → follow `workflows/CaptureComponentAxes.md`
   - If archetype = Domain → follow `workflows/CaptureDomainAxes.md` (includes the mandatory framework-host-compatibility warning)

3. **Confirm with the user.** Present the full axis list and ask: "Here's what I captured — any changes?" Do not proceed until the user confirms or corrects.

4. **Write the CLAUDE.md file** — follow `workflows/WriteClaudeMd.md`. Two templates, one per archetype.

5. **Tell the user:**
   > `src/Modules/{Name}/CLAUDE.md` written. The Developer can now run `create-module {Name}` in a Developer session to scaffold the skeleton.

## What this skill does NOT do

- Does not scaffold any folders or files. Only writes the CLAUDE.md.
- Does not update `src/Modules/*.sln` or any solution file.
- Does not run `dotnet` commands.
- Does not make architectural decisions silently — every axis is confirmed with the user.

## Arguments

Pass the module name: `create-module-claude-md Audit`
