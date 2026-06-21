# Agenting Setup Plan

**Date:** 2026-05-14
**Status:** In progress

---

## Completed This Session

### Skills (Phase 3)
- 18 existing skills updated (critical fixes + industry best practices)
- 8 new skills created (integration-test, security-audit, test-coverage, tdd-workflow, accessibility, responsive-layout, layout-debugging, package-conflicts)
- Results: `PHASE1_RESULTS.md`, `PHASE2_COMPARISON.md`, `PHASE3_RESULTS.md`

### Team Lead (5 edits applied)
1. Communication log format — added Branch, Step, Cycle header fields
2. Resume section — detect incomplete run via summary.md absence, branch guard, offer resume
3. Pre-flight checks — dirty tree, branch selection, optional Jira spec gathering
4. Post-approval phases — Tester → Teacher → Builder(optional)
5. Agent selection — added tester, teacher, builder to pipeline roles

---

## Pending Edits to Team Lead

### 1. Commit message templates
Add prescribed commit messages per phase so git history is standardized.

| Phase | Template |
|-------|----------|
| Architect plan | `docs: plan for <slug>` |
| Developer implementation | `feat: <slug>` (or `fix:` for bugs) |
| Developer fix cycle | `fix: address review cycle <N> for <slug>` |
| Tester | `test: cover <slug>` |
| Teacher | `chore: process lessons for <slug>` |

### 2. Failure handling
After each agent completes, check if the expected artifact was produced. If not, or if the Agent tool errored, present the user with: Retry / Edit prompt / Skip phase / Abort.

Key distinction: agent failure (tool error, no artifact) is NOT a review cycle. Failures trigger retry — the cycle counter only increments when the reviewer issues a new verdict.

### 3. Escalation dialog at 3 cycles
When 3 review cycles are exhausted and reviewer still wants changes, present: "Continue 3 more / Force-accept and move to Tester / Abort."

### 4. Architect questions checkpoint
Architect must report its clarifying questions list to Team Lead before writing the plan — even if empty.
- Questions empty → Team Lead auto-confirms: "No questions, proceed."
- Questions present → Team Lead relays full list to user in one batch, collects answers, sends back to architect.

### 5. Update [UNATTENDED] section
Add defaults for all new confirmation points:

| Confirmation | Unattended default |
|---|---|
| Pre-flight: dirty tree | Abort |
| Pre-flight: branch | Stay on current |
| Pre-flight: Jira | Fetch if key in prompt, skip otherwise |
| Feature spec gate | Spec must exist or abort (no spec + no Jira + no plan = abort) |
| Architect questions | Spawn PO to answer from spec |
| Failure handling | Retry once, then skip phase |
| Escalation at 3 cycles | Force-accept and move on |
| Builder | Skip |
| Resume detection | Auto-resume if branch matches |

### 6. PO as first phase — feature spec gate
The feature spec gates the entire pipeline. No spec = no implementation.

**Interactive flow:**
1. User provides Jira key → PO fetches ticket via Atlassian MCP
2. PO refines ticket into feature spec (discusses with user)
3. PO writes `docs/features/{Feature}/spec.md` with `Status: Ready`
4. Only then does Team Lead launch the implementation team

**Unattended flow:**
1. Spec already exists → proceed
2. Jira key provided, no spec → PO fetches + writes spec autonomously
3. No spec, no Jira → abort

---

## Agent Moves (old → new setup)

### Move to `.claude/agents/` (no overlap with new agents)

| Old file | New file | Adaptation needed |
|----------|----------|-------------------|
| `agents/Builder.md` | `.claude/agents/builder.md` | Lowercase filename, update any `agents/` path refs to `.claude/agents/` |
| `agents/Tester.md` | `.claude/agents/tester.md` | Lowercase filename, update path refs |
| `agents/Teacher.md` | `.claude/agents/teacher.md` | Lowercase filename, update path refs |

### Skip (overlap — new versions win)

| Old file | New equivalent | Action |
|----------|---------------|--------|
| `agents/Architect.md` | `.claude/agents/architect.md` | Keep new, delete old |
| `agents/Developer.md` | `.claude/agents/developer.md` | Keep new, delete old |
| `agents/Manager.md` | `.claude/agents/team-lead.md` | Merged into team-lead, delete old |

### After moves
- Update CLAUDE.md agent list to reflect new paths
- Delete `agents/` folder (all files moved or superseded)
- Update any rules or skills that reference `agents/` path

---

## Pipeline Skills (deferred — wrong paths)

These 3 skills have wrong paths from the old docs structure. Separate task.

| Skill | Current wrong path | Correct path |
|-------|-------------------|--------------|
| create-architecture-doc | `docs/architecture/` | `docs/architecture-reference.md` |
| create-implementation-plan | `docs/plans/` | `docs/specs/{Epic}/plans/{Story}/` |
| create-feature-spec | `docs/features/` | `docs/specs/{Epic}/` |

---

## Other Agent Issues (from Phase 1 audit)

| Issue | Skill/Agent | Status |
|-------|-------------|--------|
| Broken `@` directives (`@docs/architecture/v1.md`, `@docs/conventions/stack-rules.md`) | architect.md | Deferred — new setup |
| `done.md` vs `implementation.md` naming inconsistency | Multiple agents/skills | Deferred — new setup |
| Wrong agent attribution in review skill | review skill | Not yet addressed |
| `bug.md` absence not handled | bugs skill | Not yet addressed |
| Section names don't match actual files | improve-flow skill | Not yet addressed |
