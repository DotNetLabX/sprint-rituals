# Review Format

## Review Checklist

### Step 1: Done check (architect, no code reading)
- Every plan step has a corresponding implementation.md entry
- No plan steps missing or silently skipped
- Reported deviations have reasons
- No unexpected files or scope creep

### Step 2: Code review (reviewer, reads implementation)
- Each plan instruction has corresponding code that matches
- Referenced patterns and skills were followed
- Naming conventions match coding standards
- Build passes (fresh output, not assumed)
- No new patterns invented
- Domain rules enforced in aggregates, not handlers
- Deviations flagged with verdict: plan wrong or code wrong
- Security: no hardcoded secrets, inputs validated, no injection vectors
- Logic: all branches reachable, no off-by-one, null handling correct
- Performance: check for performance anti-patterns per loaded conventions
- Skill mapping verified: any "None" disposition in the Skill Mapping was warranted (no existing skill actually covers the step)

## Severity Ratings
- **CRITICAL**: Security vulnerability, data loss risk, fundamentally wrong approach. Blocks merge.
- **HIGH**: Logic error, missing error handling, plan deviation without justification. Should fix.
- **MEDIUM**: Suboptimal pattern, minor inconsistency. Consider fixing.
- **LOW**: Style preference, minor improvement. Optional.

## Verdict
- **APPROVE**: No CRITICAL or HIGH issues.
- **REQUEST CHANGES**: Any CRITICAL or HIGH issue present.
- **COMMENT**: Only MEDIUM/LOW, no blockers.

## Review Output Format

Reviewer saves to `docs/specs/{slug}/delivery/review.md`.

```
# {Feature Name} — Review

## Reviewed By
[Who performed this review: reviewer (Sonnet), /review (skill), /codex:rescue (Codex), or combination]

## Verdict: APPROVE | REQUEST CHANGES | COMMENT

## Pre-commitment Predictions
- [What you expected to find vs what you found]

## Findings

### [SEVERITY] Finding title
**File:** `path/to/file:line`
**Issue:** What's wrong
**Fix:** Specific suggestion
**Confidence:** HIGH | MEDIUM | LOW

## Positive Observations
- [What was done well]

## Gaps
- [Edge cases or paths not covered]

## Open Questions
- [Low-confidence findings moved here by self-audit]

## Evidence
| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Build | pass/fail | [per project-rules] | [summary] |
```

## Anti-patterns

- **Findings without file:line evidence.** Every CRITICAL or HIGH finding must cite an exact file path and line number. "The handler doesn't validate input" is not a finding — `Features/Foo/FooEndpoint.cs:42` is.
- **Severity inflation.** Style preferences, naming choices, and minor inconsistencies are LOW by definition. Flagging them as HIGH inflates the fix burden and trains the developer to ignore severity ratings. When in doubt, write it as LOW or move to Open Questions.
- **Approving without fresh build output.** "Should work" is not evidence. Every approval must include a build check row in the Evidence table with actual command output. Claiming the build passes without running it is grounds for REQUEST CHANGES on the review itself.
- **Rubber-stamping fix cycles.** On cycles 2 and 3, re-check the areas adjacent to each fix — not just the exact lines changed. A fix that introduces a regression in a nearby call site is still a failed review.

## Consumers

| Agent | What they read | Action taken |
|-------|---------------|-------------|
| Developer | All findings (CRITICAL/HIGH first) | Fixes each finding, notes in implementation.md |
| Architect (escalation) | All sections | Decides plan wrong vs code wrong |
| Team Lead | Verdict line | Routes to developer (REQUEST CHANGES) or closes (APPROVE) |
