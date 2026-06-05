# Pipeline Guardrails

- **No Opus for general-purpose agents** — always set `model: "sonnet"`. General-purpose agents do research and summarization, not deep reasoning.

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

- **Never override specialized agent models** — specialized agents (architect, developer, reviewer, etc.) have their model baked into their `.md` frontmatter. Never pass a `model` parameter that overrides it.
- **Project agents own the pipeline** — for feature pipeline roles (architect, developer, reviewer), always use `.claude/agents/` project agents. Never substitute OMC equivalents (oh-my-claudecode:planner, oh-my-claudecode:executor, oh-my-claudecode:architect) for pipeline work.
- **OMC pipeline orchestration forbidden** — never use OMC's team/pipeline management (oh-my-claudecode:team, oh-my-claudecode:autopilot, oh-my-claudecode:ralph) to replace the project pipeline.
- **OMC specialists allowed** — OMC specialist agents (oh-my-claudecode:debugger, oh-my-claudecode:security-reviewer, oh-my-claudecode:tracer, oh-my-claudecode:code-simplifier, oh-my-claudecode:designer, etc.) are available for standalone tasks outside the pipeline.

## Output Size Constraint

Explore and general-purpose agents return a structured report under 300 words. Write detailed findings to disk (e.g., a notes file or questions.md); the message is a summary. Never return raw file dumps or full grep output as the agent response — summarize findings and cite file paths.
