# Agent Coordination Protocol

Three-agent pipeline (architect, developer, reviewer) for feature development. Agents coordinate through file-based handoffs and **hub-and-spoke messaging** — all messages route through the team lead.

**Deploy to:** `.claude/rules/agents-workflow.md` (auto-loads every session)

## Slug and Path Resolution

The `{slug}` identifies a unit of work and follows these conventions:
- Internal feature: `F{N}-{Name}` — e.g., `F5-SprintSummaryCard`
- Jira epic: `{KEY}-{2-3-words}` — e.g., `ABC-5234-shelf-compliance-kpis`
- Jira issue: `{KEY}-{2-3-words}` — e.g., `ABC-5226-split-config`
- Ad-hoc: `adhoc-{Name}` — e.g., `adhoc-SyncRefactoring`
- Bug: `BUG-{N}-{name}` — e.g., `BUG-1-bug-count-zero`
- Gap: `GAP-{N}-{name}` — e.g., `GAP-3-sub-team-management-ui`

The team lead or PO assigns the slug at the start of each pipeline run and passes it to all downstream agents. Agents never derive the slug — they use exactly what was passed.

Standard paths:
```
docs/backlog.md                              ← feature sequence, dependencies, status
docs/specs/{slug}/
  definition/   ← spec.md (or epic.md / bug.md), help.tooltips.md, images
  delivery/     ← plan.md, implementation.md, review.md, lessons.md, summary.md, communication-log.md
```

For a Jira issue nested under an epic:
```
docs/specs/{epic-slug}/{issue-slug}/definition/
docs/specs/{epic-slug}/{issue-slug}/delivery/
```
Ad-hoc work has no `definition/` folder — only `delivery/`.

## Communication Model: Hub-and-Spoke

All agent messages go through the team lead. Agents never message each other directly.

**Why:** The team lead triages all communication, intercepts decisions that need user approval, and prevents agents from silently overriding user requirements.

**How agents send messages:** Address the team lead, specifying the intended recipient and content:
```
To team-lead: "For {recipient}: {message content}"
```

**How the team lead dispatches:** Read the message, decide:
1. **Routine handoff** (e.g., "ready for review") → forward to recipient.
2. **Decision that contradicts user requirements or changes scope** → ask the user first, then forward the answer.
3. **Question that needs user input** → relay to user, wait for answer, forward to agent.

## All Agents

- **Instructions from the team lead are user decisions.** Instructions relayed through the team lead represent user decisions already made. Agents may flag concerns or recommend alternatives *before* the decision, but once an instruction arrives, they execute it — they don't substitute their own judgment because they consider it more efficient or equivalent.
- **Rules go in files, not memory.** When a reusable rule or convention is identified, capture it in the appropriate rule or agent file — not in memory. Memory is for context that doesn't fit in rule files (user preferences, project state, external references). This applies to every agent, not just the team lead.

## Message Size Contract

Keep agent outputs focused. Content goes in files; messages are notifications with summaries.

| Output type | Max length | Rule |
|-------------|-----------|------|
| Analysis outputs (Phase 1) | ~500 words | Write detailed findings to questions.md or a notes file; message is the summary |
| Handoff messages | ~300 words | One paragraph of what was done + one paragraph of what's next |
| Checkpoint reports | Structured format | See Checkpoint Report Format in `.claude/conventions/pipeline-protocol.md` |

**Write first, message second** — this applies to ALL artifacts, not just questions. Implementation details go in implementation.md, review findings go in review.md, questions go in questions.md. Messages are notifications.

## Agents

| Agent | Model | Scope | Managed by |
|-------|-------|-------|------------|
| team-lead | claude-opus-4-6 | Pipeline orchestration, message routing, commit protocol | user |
| architect | claude-opus-4-6 | Plans, Step 1 review (done check), question answers, escalation decisions | team lead |
| po | claude-opus-4-6 | Feature shaping, spec writing, question answering | team lead |
| critic | claude-opus-4-6 | Cross-reference review of feature specs (vs v1.md) and plans (vs feature spec) | PO (spec reviews), architect (plan reviews) |
| developer | opusplan | Implementation, implementation.md, questions.md | team lead |
| reviewer | sonnet | Step 2 review (code review), severity-rated conformance checks | team lead |
| learner | claude-opus-4-6 | Lessons consolidation, pattern promotion to system files | team lead |
| solo | opusplan | Small fixes and scoped changes (1-3 files) | user |

## Pipeline Protocol

For the full pipeline flow, checkpoint format, and message handoffs, see `.claude/conventions/pipeline-protocol.md` (loaded by pipeline agents via `@`).

## Pipeline Modes

The pipeline is not a rigid sequence — ceremony scales with uncertainty. Multiple entry points and team configurations exist.

### Entry Points

| Entry Point | First Agent | What's Skipped | Use Case |
|-------------|------------|----------------|----------|
| `be solo` | solo | Everything — no pipeline, no plan/review | Bug fixes, 1-3 file changes |
| `be architect` | architect | PO, spec, team orchestration | Refactoring plans, tech debt, ad-hoc analysis |
| Team lead (existing plan) | developer | PO + architect planning | Plan already written via `be architect` |
| Team lead (full pipeline) | PO or architect | Nothing | Complex features needing discovery and alignment |

### Team Configurations

| Config | Agents | When |
|--------|--------|------|
| **Fast** | architect + developer (developer self-reviews) | Well-understood patterns, internal tooling |
| **Standard** | architect + developer + reviewer | Production features, team alignment needed |
| **Standard + Codex** | Standard + multi-model cross-validation | Security-sensitive, data-accuracy critical |

### Key Principles

- PO phase is optional — architect can plan directly from incomplete tickets
- Critic is always optional with confirmation — never forced
- Solo handles its own scope — small improvements never enter the pipeline
- Known patterns get less process; novel work gets more

## Cycle Caps

| Loop | Max | Escalation |
|------|-----|------------|
| Reviewer ↔ Developer fix cycle | 3 | → Architect |
| Developer questions (same area) | 3 | → Human |
| Architect escalation resolution | 1 | → Human |

After escalation to human, agents STOP and wait.

## Artifact Formats

| Artifact | Format Definition |
|----------|-------------------|
| Plan | `.claude/skills/create-implementation-plan/references/plan-template.md` |
| Implementation | `.claude/conventions/implementation-format.md` |
| Summary | `.claude/conventions/summary-format.md` |
| Questions | `.claude/conventions/questions-format.md` |
| Lessons | `.claude/conventions/lessons-format.md` |
| Review | `.claude/conventions/review-format.md` |

## Convention Locations

| Location | Scope | What lives here |
|----------|-------|-----------------|
| `.claude/rules/` | Auto-loaded every session | Universal behavioral constraints (guardrails, KB navigation, pipeline guardrails) |
| `.claude/conventions/` | Loaded via `@` by pipeline agents | Pipeline artifact formats, coordination protocol |
| `.claude/agents/` | Loaded on `be {agent}` | Agent-specific instructions, boundaries, communication rules |
| `.claude/skills/` | Invoked on demand | Reusable code patterns and scaffolding workflows |
| `docs/conventions/` | Loaded via `@` by agents | Stack-specific coding standards, project rules |

New instructions go in the narrowest applicable scope. See `.claude/README.md` for the full decision tree.

**Maintenance:** When adding or removing agents, rules, conventions, or skills, read `.claude/README.md` and update it to reflect the change.

## Skill Authority

Skills in `.claude/skills/` are the authoritative source for patterns.

- **Skill exists, codebase has what it needs:** Follow the skill.
- **Skill exists, codebase missing something it references:** Build it. Escalate to architect only if scope seems too large.
- **No matching skill:** Architect may inline snippets. Log the missing skill in lessons.md.

The `create-architecture-doc` and `create-implementation-plan` skills enforce this principle via mandatory skill inventory checks. Architecture docs defer implementation patterns to skills. Plans map each step to a skill or justify inline detail.
