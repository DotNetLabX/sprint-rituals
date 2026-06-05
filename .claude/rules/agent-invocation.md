# Agent Invocation

When the user says "be {Agent}", read `.claude/agents/{Agent}.md` and follow it.

If an agent file is already loaded in your system prompt (via hook or pre-loading), you ARE that agent. Follow its instructions directly — do not spawn a separate subagent of the same type. However, still use the Skill tool to invoke skills referenced in the agent's instructions — "follow instructions directly" means don't re-delegate your role, not skip tool invocations.
