# Questions File Format

Written by any agent when blocked or needing clarification. The recipient answers inline. All questions go in one file — `docs/specs/{slug}/delivery/questions.md`.

**Write first, message second.** Every agent must write its questions to the file before sending them as a message. The file is the log; the message is the notification.

```
# {Feature Name} — Questions

## Q1: [Short title]
**From:** [agent role]
**To:** [agent role, "PO", or "user" — see routing below]
**Status:** Open | Answered | Resolved
**Step:** [Plan step number and name, or "Phase 1 analysis" if pre-plan]
**File:** [File being worked on, if relevant]

**Context:** [What was being done, what was tried, what is unclear]

**Question:** [Specific question or decision needed]

### Answer
[Recipient fills this in, sets Status → Answered]
```

**Rules:**
- One question per section. Multiple blockers = multiple sections.
- Include enough context that the recipient can answer without reading the sender's work.
- If the answer changes the plan, architect updates the plan. Plan stays source of truth.
- Questions are numbered sequentially across the entire file (Q1, Q2, Q3...) regardless of sender.
- **To field routing:** Set `To: PO` for spec/product questions (the team lead routes through the PO escalation chain: PO answers from spec → user only if PO can't cite a section). Set `To: architect` for technical/plan questions from the developer. Set `To: user` only for pure preference questions with no spec or technical basis.

## Anti-patterns

- **Asking without context.** The recipient should be able to answer without reading your work. If the question omits what you tried, what you found, and why it's ambiguous — the recipient will spend time re-investigating what you already know.
- **Bundling unrelated questions.** Each Q section covers one blocker. If you have three unrelated questions, write Q1, Q2, Q3 as separate sections. Bundling forces the recipient to untangle them and makes it hard to mark individual questions Answered.
- **Wrong To: field.** Sending a spec/product question to the architect wastes a round-trip — the architect will route it to PO anyway. Sending a technical plan question to the user wastes their time on something the architect should decide. Use the routing rules above.

## Consumers

| Agent | Role | Routing chain |
|-------|------|--------------|
| Team Lead | Reads questions.md to triage routing | Routes to PO (product), architect (technical), or user (preference) |
| Architect | Answers technical/plan questions | Updates plan if answer changes it, sets Status → Answered |
| PO | Answers spec/product questions | Cites spec section; escalates to user if no citation exists |
| Developer | Reads answers before resuming | Resumes from the first unanswered step |
