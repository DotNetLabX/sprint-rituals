# Consumption Evaluation — Team-Lead vs Architect (session 3bde26f8)

Measured from the API `usage` block + per-file Read aggregation of each agent's transcript.
- Team-lead = main session `3bde26f8….jsonl`
- Architect = subagent `subagents/agent-a0ccb56748957a8e9.jsonl`

| Agent | Peak ctx | Read bucket | Re-read waste | OMC hook tax |
|---|--:|--:|--:|--:|
| Team-lead | **404k** | 68k | ~30k | **~120k** |
| Architect | **340k** | **86k** | **~39k** | ~11k |

**Headline:** the team-lead's bloat is hooks + reading-things-it-shouldn't; the architect's bloat is **re-reading** (it pays almost no hook tax — subagents don't accumulate the per-tool nags, so silencing hooks helps the *main* session, not the architect).

---

## A. Team-Lead reads (68k)

### What I did
Read `plan.md` ×3 (21k), `review.md` ×4 (13k), `questions.md` ×2 (6k), plus `implementation`, `refactor-triage`, `codex-review-impl`, `done-check`, `lessons` once each. Only `communication-log` (the artifact I own) was a legitimate repeat.

### Why
Two role violations dressed as diligence:
1. **Over-applied "Verdict Validation"** — it needs only *the verdict line + the count of open HIGH/CRITICAL*; I full-read `review.md` 4× instead of grepping two lines.
2. **Read `plan.md` 3× for no valid reason** — the team-lead does not review the plan; that's the critic's lane. Pure habit.

### What I should have done
The team-lead routes and validates; it does not read the work. Legitimate read set:
- `communication-log` (own it) — append/tail, not full re-read.
- `questions` — only to route, and usually already present in the agent's checkpoint message.
- Verdicts (`review`/`done-check`/`codex-*`) — **grep the verdict + severity count**, never the body.
- `plan` / `implementation` / `lessons` / triage / spec / source — **never**.

### Proposal
Enforce the persona's own rule: own `comm-log`, grep verdicts, never open `plan`/`implementation`. Takes #2 from **68k → ~5k (~50k saved)**. This is behavioral (config can't fix it without adding a hook, which we're removing).

---

## B. Architect reads (86k) — *different problem*

### What it did
- **`plan.md` ×12 = 24k** (~22k of it re-reads) — re-read the plan it was *authoring* on every review-fix cycle (Codex 9 + critic 3 + 2 touch-ups) and again for the done-check.
- `lessons` ×5, `questions` ×2, `decisions` ×2, `backlog` ×2 — smaller re-reads.
- **Legitimate one-time context ~35k**: `reference-patterns-domain/structure` (10k), `README` (4k), `refactor-triage` (4k), `decisions.md` (ADRs, 3.5k), `skill-adherence-root-cause` (3.9k), `nexus-format-gap` (2.6k), `skill-domain-summary` (3.3k), conventions, index.
- `SKILL.md` ×7 (8.8k) — mostly the 5 skills it was repairing/creating (legit; basename collision inflates the count).

### Why
The architect's base reading was **correct** — it *must* read the ADRs, references, triage and gap docs to plan. Its waste is narrower: it **re-read the artifact it was editing** (`plan.md` ×12) instead of holding it in context or editing targeted sections. Across three review rounds + the done-check, that compounded. Some re-reads were also forced post-compaction (Edit requires a prior Read).

### What it should have done
- Read references / ADRs **once, up front** (it mostly did — good).
- **Never re-read the plan it is editing** — the content is known after writing; edit targeted sections; the Edit tool only needs *one* prior Read, not twelve.
- Done-check: **grep the step list** and compare targeted sections, not full re-read of `plan` + `implementation`.

### Proposal
Architect read discipline targets the **~39k re-read waste** (vs ~35k legitimate context, which stays). Plan-authoring agents should treat the artifact-under-edit as in-context state, not re-fetch it each cycle. Hooks barely affect the architect — its only lever is re-read discipline.

---

## C. Cross-cutting: dispatch verbosity is paid twice

My `SendMessage` dispatches (13k in my window) are **copied into each subagent's `user_text`** — the architect's `user_text` was 25.6k, much of it my prompts + resumes. A verbose dispatch costs tokens in *both* windows. Terse, path-pointer dispatches shrink both sides at once.

---

## D. Plugin findings (OMC hooks + MCP) — the config-level levers

These are *not* behavioral; they're configuration, and they hit **every** session.

### D1. OMC hooks — ~120k (main session)
The biggest single bucket, and it's the plugin's own machinery, not agent behavior. Measured from `attachment` records:

| attachment kind | ~tok | source |
|---|--:|---|
| `hook_success` | 80k | the JSON every Pre/PostToolUse/Stop hook returns, on **every** tool call |
| `hook_additional_context` | 13k | the "Verify changes work…", "Use parallel execution…", "Extensive reading…" nags |
| `deferred_tools_delta` | 8k | MCP deferred tool-name list (see D3) |
| `skill_listing` | 6k | available-skills dump |

**Owning hooks** (omc `hooks/hooks.json`): `pre-tool-enforcer` (PreToolUse \*), `post-tool-verifier` (PostToolUse \*), `post-tool-rules-injector`, `project-memory-post` — all fire on `*` (every tool). Worst repeat: `{"continue":true,"suppressOutput":true}` ×340 = 32k.

**Key scoping fact:** subagents barely accumulate this (architect = 8 attachment records / ~11k). The hook tax is overwhelmingly a **main-session / team-lead** cost — so silencing it helps the orchestrator window, not the workers.

**nexus hooks are separate and essential** (`pipeline-gate`, SessionStart rules, `pre-compact`) — NOT touched.

**Fix (applied):** `OMC_SKIP_HOOKS=pre-tool-use,post-tool-use` added to `~/.claude/settings.json` and `~/.claude-omni/settings.json` (env block). Skip tokens verified in source: `pre-tool-enforcer.mjs`→`pre-tool-use`, `post-tool-verifier.mjs`→`post-tool-use`, `keyword-detector.mjs`→`keyword-detector`; `DISABLE_OMC=1` kills all. Takes effect next session start.

**What's lost:** nothing functional — only the per-tool advice injected to the model. **Agents are not hooks:** `document-specialist`, `critic`, `architect`, `analyst` keep working; keyword auto-triggers and project-memory also stay (we only skipped the two per-tool nags).

### D2. Savings ledger (full program)
| Lever | Type | ~Saves | Status |
|---|---|--:|---|
| Silence OMC per-tool hooks | config (env) | ~120k | ✅ applied (not nexus — OMC) |
| Team-lead read discipline (grep verdicts, own comm-log) | behavioral | ~50k | ✅ rule tightened — `team-lead.md` (Read Discipline table + Relay/Verdict grep mechanism + comm-log append/tail) |
| Architect re-read discipline (don't re-fetch the plan) | behavioral | ~39k | ✅ rule added — `architect.md` (Read Discipline §) |
| Terse dispatches + append-only log edits | behavioral | ~25k | ✅ rule reinforced — `team-lead.md` Message Templates (path-pointers, paid-twice) |
| Disable unused MCP connectors | config (`/mcp`) | ~10k | pending user (not nexus — account OAuth) |

### D3. MCP connectors — ~10k fixed, every session + every subagent
Each connected server injects its deferred tool **names** + its instruction block into the system context. Connected this session:

| Server | Recommendation |
|---|---|
| `claude_ai_Atlassian` | **KEEP** — project uses Jira |
| `claude_ai_Canva` | disable |
| `claude_ai_Figma` + `figma` | disable (unless doing Figma design) |
| `claude_ai_Gmail` | disable |
| `claude_ai_Google_Calendar` | disable |
| `claude_ai_Google_Drive` | disable |
| `plugin_oh-my-claudecode_t` | keep (omc tools: lsp / ast-grep / python_repl / memory) — optional to trim |

No local `.mcp.json` and nothing in `settings.json` — these are account-level OAuth connectors, toggled only via the `/mcp` command.

---

*Measured via per-agent transcript parse (API usage + Read-by-file with re-read counts) + omc `hooks/hooks.json` + skip-guard source. Probe scripts ran out-of-context and were removed.*
