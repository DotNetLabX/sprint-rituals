# Agent & Rules File Cleanup — Lessons

## Reviewer Lessons

- When a plan changes a rule that is referenced by both agent files AND skill files, the skill files must be in scope too. A two-file update that leaves a skill with contradictory parenthetical text creates a three-way inconsistency that the reviewer must flag. The plan's scope exclusion of skills was reasonable in intent, but the auto-approve rule change was a behavioral rule — any file that encodes that rule (not just agents-workflow.md and architect.md) needs to be updated together.
- For markdown-config-only cleanups (no source code), the "build" evidence row in the review table is not applicable. Adapt the evidence table to cover the actual verification steps: file existence checks, content searches, section-by-section reads. These are the equivalent of "compilation" for documentation.
- When a plan explicitly lists safety invariants, use those invariants to drive the pre-commitment predictions. This review correctly predicted a stale cross-reference because the auto-approve rule change was a behavioral invariant applied across multiple files — a natural place to miss one.
- When a cleanup plan explicitly excludes skills from scope, the reviewer should still grep for cross-references to any behavioral rule being changed. Scope exclusions govern what the developer changes proactively; they do not exempt a file from being checked if it encodes the changed rule.
- The cycle 2 fix added a hub-and-spoke reminder inline at the point of use ("Message team lead, never developer directly"). Reinforcing invariants at the point of use — rather than just correcting the stale text — is a pattern worth preserving.
