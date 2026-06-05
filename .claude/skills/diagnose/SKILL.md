---
name: diagnose
description: Structured debugging protocol — phased loop for bugs and regressions. Loaded by developer and solo agents when a bug is encountered or a test fails unexpectedly.
---

# Diagnose

Disciplined, phased debugging for hard bugs and performance regressions. Prevents ad-hoc thrashing by forcing sequential gates: you cannot skip a phase.

## When to Use

- A build error persists after an obvious fix attempt
- A runtime bug is reported or encountered mid-implementation
- A test fails for a non-obvious reason
- Performance regresses after a change
- The circuit breaker (3 failed attempts) is about to trigger — use this BEFORE escalating

## Phases

### Phase 1: Build a Feedback Loop

Before touching anything, establish a way to **see** the bug reproduce on demand. Pick the lightest option that gives signal:

1. **Failing test** — integration or unit test that exercises the broken path (preferred if test project exists)
2. **`.http` file** — request that triggers the failure (API bugs)
3. **Console app** — throwaway `Program.cs` that isolates the logic
4. **curl/Invoke-WebRequest** — manual HTTP request with expected vs actual output
5. **Frontend repro** — specific route + action sequence in browser (document it)
6. **EF query trace** — enable `EnableSensitiveDataLogging()` + log SQL to see what's generated
7. **WebSocket/realtime test** — connect via ws client, trigger event, observe payload
8. **git bisect** — when you know "it worked before" but not when it broke
9. **Differential** — compare working service/endpoint with broken one side-by-side
10. **Manual HITL** — last resort: document exact steps for user to reproduce, capture their output

**Gate:** You have a reproducible signal before proceeding. If you can't reproduce, state that and ask the user for reproduction steps.

### Phase 2: Reproduce

Run your feedback loop. Confirm it shows **exactly** the failure the user described (not a different failure). Document:
- Expected behavior
- Actual behavior
- Exact error message or incorrect output

**Gate:** The loop reproduces the user's reported symptom.

### Phase 3: Hypothesize

Generate 3–5 ranked hypotheses **before** testing any. Each must be falsifiable (you can describe what you'd see if it were true vs false).

Format:
```
H1: [Most likely] {hypothesis} — would show {evidence if true}
H2: {hypothesis} — would show {evidence if true}
H3: {hypothesis} — would show {evidence if true}
```

**Gate:** Hypotheses written. Test them in rank order, one at a time.

### Phase 4: Instrument

Probe **one variable at a time**. Tag all debug instrumentation with a unique prefix for easy cleanup:

```
[DEBUG-{4-char-hex}] — remove after diagnosis
```

Use your language's logging or print facility. Every debug line must include the `[DEBUG-xxxx]` tag so cleanup can grep for it. For performance regressions: use timing, not logs — wrap suspect sections with a stopwatch/timer and log elapsed time with the same tag prefix.

After each probe:
- Run the feedback loop
- Record: which hypothesis confirmed/eliminated, what new evidence
- Narrow or pivot

**Gate:** Root cause identified with evidence.

### Phase 5: Fix + Regression Guard

1. Write the fix (minimal change that addresses root cause)
2. Run the feedback loop — confirm it passes
3. If a test project exists: write a regression test that would have caught this
4. If no test project: document the regression scenario in the PR/commit message

**Gate:** Feedback loop passes. Fix is minimal.

### Phase 6: Cleanup + Post-Mortem

1. **Remove ALL debug instrumentation** — grep for your `[DEBUG-{prefix}]` tag:
   ```
   grep -r "DEBUG-a4f2" src/
   ```
2. **Verify** the original feedback loop still passes after cleanup
3. **Write lessons** — append to `docs/specs/{slug}/delivery/lessons.md` (if in pipeline) or state the finding (if solo/ad-hoc):
   - What was the root cause?
   - What would have prevented this?
   - Is there an architectural improvement needed? (flag for architect)
4. **Build verification** — build passes per Build Verification in project-rules

## Integration with Circuit Breaker

The developer/solo circuit breaker triggers after 3 failed attempts. This skill should be invoked **before** reaching that limit:

- Attempt 1 fails → invoke diagnose skill (Phase 1–4)
- If root cause found → Phase 5–6 (fix)
- If 3 hypotheses exhausted without root cause → escalate to architect with your evidence log

## Guardrails

- **Never skip phases** — especially Phase 3 (hypothesize before testing)
- **One variable at a time** — shotgun debugging (changing multiple things) resets you to Phase 1
- **Tag everything** — untagged debug output becomes production noise
- **Minimal fix** — don't refactor during a fix. Fix the bug, verify, then propose refactoring separately
- **No permanent logging for debugging** — if the insight is valuable long-term, it's observability (different concern, different PR)

## Required Reading

Before invoking this skill, ensure you have:
- The exact error message or failure symptom (copy it verbatim — every word matters)
- The build or test output from the failed attempt
- A description of what changed immediately before the failure (the last step you completed)

## Anti-patterns

- **Skipping Phase 3 (Hypothesize) and going straight to fixing.** Without ranked hypotheses, each fix attempt is a shot in the dark. The circuit breaker exists precisely because ad-hoc fixing compounds problems. Write the hypotheses before touching the code.
- **Changing multiple things in one fix attempt.** Bundling fixes resets you to Phase 1 — if it works, you don't know why; if it doesn't, you've made the state harder to reason about. One variable at a time.
- **Forgetting to remove debug instrumentation.** Tagged debug output (`[DEBUG-xxxx]`) left in after the fix becomes production noise. Phase 6 cleanup is not optional.

## Downstream Consumers

| Agent | How they use this skill | Impact if skipped |
|-------|------------------------|-------------------|
| Developer | Follows phases to diagnose build/runtime failures | Without structure, 3 failed attempts trigger circuit breaker and escalate to architect unnecessarily |
| Architect | Reviews evidence log when escalated | Incomplete phase logs make root cause analysis impossible; architect can't make a good decision |

## What This Skill Does NOT Do

- Replace the architect's escalation role — if you can't find root cause, escalate
- Guide test infrastructure setup — use `tdd` skill for that
- Fix performance at scale — this finds the bottleneck; optimization is separate work
- Handle flaky tests — flaky = non-deterministic; that's a different investigation pattern
