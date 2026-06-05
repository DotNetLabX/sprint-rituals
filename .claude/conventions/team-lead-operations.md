# Team Lead Operations Reference

Read this file before launching a team or running a pipeline. Not needed for status checks or routing-only sessions.

**Consumers:** Team lead only. Other agents do not read this file.

| Consumer | What they read | Impact of staleness |
|----------|---------------|---------------------|
| Team lead | Spawn modes, failure handling, commit protocol, communication log format, unattended defaults | Wrong spawn behavior, lost progress on failures, missing audit trail |

## Spawn Modes

### Background (default)

Spawn the first agent of each type using the `Agent` tool with `run_in_background: true`. After that, **always resume agents via `SendMessage` using their agent ID** — even across pipeline phases. Only spawn a fresh instance if `SendMessage` fails (agent not addressable).

**Why resume-first:** Resumed agents retain their full transcript/context from prior work — cheaper, faster, and more consistent. Fresh spawns lose all prior context and re-read everything.

Start with the architect using a minimal prompt (see Message Templates in team-lead.md).

**Track agent IDs throughout the pipeline run.** When an agent completes, note the agent ID from the launch result. Use that ID for all subsequent `SendMessage` calls to that agent. **Never use the agent name** — name-based addressing has a known bug (silently fails for completed agents).

Spawn the next agent type only when the current one completes and you've triaged its output. The pipeline is sequential — architect → developer → architect (done check) → reviewer.

When an agent completes (you get the background notification):
1. **Relay the agent's full output to the user verbatim.** The user cannot see agent messages — you are their only window. Show the output first, before any routing or triage.
2. Triage any messages the agent produced, then decide:
   - If the agent wrote questions.md → read it, check if it needs user input, handle accordingly. Then **resume the same agent** via `SendMessage(to: agentId)` with the answer.
   - If the agent wrote implementation.md → **resume the next agent** via `SendMessage(to: agentId)` if it was already spawned, or spawn it for the first time if not.
   - If the agent's output contains a decision that contradicts user requirements → ask the user before proceeding.

**Key:** Use `SendMessage` to resume agents across ALL phases (same phase and cross-phase). Only use `Agent` to spawn the first instance of each agent type. Fall back to a fresh `Agent` spawn only if `SendMessage` returns "not addressable."

### Foreground

Spawn agents using the `Agent` tool without `run_in_background` (default). The call blocks until the agent completes or times out. Results are returned inline.

**Agent IDs still matter.** Every `Agent` call returns an `agentId` in its result. Track it — you need it to resume the agent later. **Do not use the agent name** for `SendMessage` after a foreground agent completes — name-based addressing silently fails for completed foreground agents. Always use the agent ID.

**Resume via SendMessage works.** After a foreground agent completes (or times out), resume it with `SendMessage(to: agentId)`. The agent retains its full conversation history — same as background mode.

**Timeout recovery.** Foreground agents can stall on large tasks (stream idle timeout after ~10 min, or watchdog kill after 600s of no progress). When this happens:
1. Check what work was completed: `git diff --name-only`, check for output files.
2. Resume via `SendMessage(to: agentId)` with a message describing what's done and what's remaining.
3. If resume stalls again, spawn a fresh agent with explicit context: list completed steps, list remaining steps, reference the plan.
4. For tasks touching 20+ files, proactively split into multiple agent runs (e.g., "implement Steps 1-4" then "implement Steps 5-8").

**Pipeline flow:** Same sequential pipeline as Background mode. The only difference is blocking vs non-blocking execution.

### Persistent

Use `TeamCreate` to create a persistent team. All agents spawn once and stay alive — use `SendMessage` to hand off between phases instead of respawning.

Create the team with all required agents upfront (use minimal prompts — see Message Templates in team-lead.md). Omit reviewer for Fast mode. Add Codex instructions to reviewer prompt for Standard + Codex.

Pipeline flow via `SendMessage` using the message templates:
1. Architect plans → 2. Developer implements → 3. Architect done check → 4. Reviewer reviews.
Fix cycles: `SendMessage` to developer, then back to reviewer — same agents, no respawn.
When approved → team lead writes summary.md and updates cross-references directly. No architect wake-up needed.

**Limitations:** Persistent teammates do not survive `/resume`, `/compact`, or session restarts. No spawn mode survives these — use the communication log Resume flow to continue across sessions.

**How to start:** Use `TeamCreate` directly — it spawns agents in-process. Do NOT pass tmux-related flags or attempt to use a tmux backend; in-process is the correct mode for persistent teams.

### Team Mode Routing (all spawn modes)

- **Standard:** architect → developer → reviewer (spawn or message each when the previous phase completes)
- **Standard + Codex:** Same as Standard, add to reviewer prompt: "Enable Codex cross-validation via /codex:rescue"
- **Fast:** architect → developer (add to developer prompt: "After implementation.md, run /review on the changes, then report back. No separate reviewer agent.")

The pipeline runs per `.claude/rules/agents-workflow.md`.

## Phase Failure Handling

### Agent Timeout / Stall Recovery

When an agent times out (stream idle timeout) or stalls (watchdog kill after 600s of no progress):

1. **Assess progress.** Run `git diff --name-only` and check for output files (implementation.md, questions.md). Map completed work to plan steps.
2. **Resume first.** Use `SendMessage(to: agentId)` — the agent retains full context. Tell it what's done and what remains:
   ```
   You timed out. Completed: Steps 1-6 (list files). Remaining: Steps 7-8 (list files). Continue from Step 7.
   ```
3. **If resume stalls again**, the task is too large for one agent run. Spawn a fresh agent scoped to just the remaining steps:
   ```
   Implement Steps 7-8 of F31-DarkLightTheme. Plan: docs/specs/{slug}/delivery/plan.md.
   Steps 1-6 are already done. Only do Steps 7-8: {brief description}. Files to modify: {list}.
   ```
4. **Proactive splitting.** For plans touching 20+ files, split the developer work upfront into 2-3 agent runs by step ranges. Don't wait for a timeout — prevent it.

### Other Phase Failures

When an agent reports failure (build error, tool failure, unexpected state) — not a review cycle rejection or timeout:

Present the user with:
```
{Agent} failed on {step}: {error summary}

1. Retry — re-run the same phase
2. Edit prompt — adjust instructions and retry
3. Skip — mark step as skipped, continue pipeline
4. Abort — stop the pipeline
```

Agent failure ≠ review cycle. Review rejections follow the normal fix cycle. Phase failures are unexpected errors that need user judgment.

## Escalation Protocol

When 3 reviewer ↔ developer fix cycles are exhausted without approval:

1. Route to architect for a resolution recommendation.
2. Present the user with:

```
Review cycle limit reached (3/3) for {Feature}.
Architect recommends: {recommendation}

1. Continue — allow 3 more cycles
2. Force-accept — proceed with current state (risks noted in review.md)
3. Abort — stop the pipeline
```

## Commit Protocol

Default: **2 commits** per feature. The user can override (e.g., "single commit", "4 commits") in their launch message.

| Strategy | Commits | When |
|----------|---------|------|
| **2 commits** (default) | Post-plan: `feat({slug}): add implementation plan` | After architect plan approved |
| | Post-shutdown: `feat({slug}): implement {short description}` | After pipeline complete (includes code, review fixes, docs) |
| 1 commit | Post-shutdown: `feat({slug}): implement {short description}` | Single squash at pipeline end |
| 4 commits | Post-plan, Post-implementation, Post-review, Post-shutdown | At each pipeline phase boundary |

**Why 2 commits:** The plan commit preserves the design if implementation needs to be reverted. Everything else (code + review fixes + docs) goes in one final commit — intermediate states aren't useful to revert independently.

Auto-commit at each checkpoint. No confirmation needed.

## Communication Log

Maintain `docs/specs/{slug}/delivery/communication-log.md` throughout the pipeline run. This file tracks all inter-agent messages and identifies communication problems.

Format:
```
# {Feature} — Communication Log

**Branch:** {branch name}
**Step:** {current pipeline step}
**Cycle:** {N/3}
**Team Mode:** {fast/standard/standard+codex}
**Review Mode:** {self-review/critic}
**Architect ID:** {agent-id or "not spawned"}
**Developer ID:** {agent-id or "not spawned"}
**Reviewer ID:** {agent-id or "not spawned"}
**Plan Steps Completed:** [1, 2, 3]
**Plan Steps Remaining:** [4, 5, 6]
**Questions Resolved:** [Q1, Q2]

## Messages

| # | From | To | Summary | Problem |
|---|------|-----|---------|---------|
| 1 | ... | ... | ... | None / description |

## Problems

1. **Problem title.** Description of what went wrong and impact.
```

Header fields:
- **Branch** — set once at team launch, never updated.
- **Step** — updated at each phase transition. Values: `architect-plan`, `developer-analyze`, `developer-impl`, `architect-review`, `reviewer-review`, `developer-fix`, `learner`, `done`.
- **Cycle** — updated when review cycles change. Initial: `0/3`. Reset to `0/3` after each approval phase.
- **Team Mode / Review Mode** — set at launch from pre-flight choices.
- **Architect/Developer/Reviewer ID** — set when each agent is spawned. Used for `SendMessage` calls. Update to the actual agent ID immediately after spawn.
- **Plan Steps Completed / Remaining** — updated after each developer step announcement. Derived from implementation.md entries vs plan step count.
- **Questions Resolved** — updated when architect sets a question Status → Answered.

Rules:
- Log every message between agents (including your own relays).
- For each message, note if there was a problem (missed handoff, wrong recipient, relay needed, etc.).
- Keep a numbered problems list at the end summarizing all communication issues.
- Update the log in real-time as messages flow — don't wait until shutdown.

## Unattended Mode

When the prompt contains `[UNATTENDED]`, you are running non-interactively (`claude -p`). No user input is possible.

Defaults:
- **Team mode:** Standard (architect + developer + reviewer). Do not ask.
- **Spawn mode:** Background. Do not ask. Persistent mode requires an interactive session.
- **Dirty tree:** Abort. Do not continue with uncommitted changes.
- **Branch:** Stay on current branch. Do not ask.
- **Jira:** Fetch if a Jira key was provided in the prompt. Skip otherwise.
- **Spec gate:** Must exist with `Status: Ready`, or abort. Do not spawn PO.
- **Plan approval:** Auto-approve regardless of step count. Do not wait for human.
- **Plan review:** Architect self-review. No critic.
- **Architect questions:** PO answers from spec context if available. Otherwise architect decides.
- **Developer questions (Phase 1):** Architect answers directly. No human escalation. If "all clear," proceed to Phase 2 immediately.
- **Developer questions (mid-implementation):** Architect answers directly. No human escalation.
- **Phase failure:** Retry once, then skip the step.
- **Reviewer fix cycles:** Up to 3, then fail pipeline.
- **Escalation:** Fail pipeline.
- **Commits:** 2-commit strategy (plan + final). Auto-commit, no confirmation.
- **All pipeline artifacts are mandatory:** plan.md, implementation.md, review.md, summary.md, lessons.md, communication-log.md. Do not skip any.

If `[UNATTENDED]` is absent, follow the **autonomous interactive flow** — apply defaults silently, route questions to PO, and only escalate to the user when the PO can't answer from spec context, when 3 review cycles are exhausted, or on phase failures. The user can override any default by stating preferences in their launch message.
