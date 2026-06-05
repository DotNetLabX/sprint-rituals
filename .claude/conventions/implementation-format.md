# Implementation File Format

Written by developer after each implementation round: `docs/specs/{slug}/delivery/implementation.md`.

```
# {Feature Name} — Implementation

## Files Created
- `full/path/to/File` — what it does

## Files Modified
- `full/path/to/File` — what changed and why

## Key Decisions
- Implementation choices not specified in the plan

## Carry-Over Findings
| Title | Severity | For | Evidence | Note |
|-------|----------|-----|----------|------|
| {title} | low/medium/high | reviewer/architect | {one-line evidence} | {note} |

## KB Changes
| Entry | Action | What changed |
|-------|--------|-------------|
| `docs/kb/{area}/{entry}.md` | NEW/UPDATE | {brief description} |

## Deviations from Plan
- Steps done differently, with reasons
```

## Anti-patterns

- **"Updated file" without explaining what changed.** Every Files Modified entry must state what changed and why — not just that the file was touched.
- **Omitting deviation reasons.** Every deviation needs a reason. "Plan said X, did Y" is incomplete — add why Y was better or necessary.
- **Listing files without linking to plan steps.** Files Modified should connect back to what the plan asked for. If a file is in the list but no plan step required it, explain why it was touched.
- **Writing all at once at the end.** Update implementation.md after each step completes — not all at the end. Enables resume-from-timeout and gives architect incremental visibility.

## Consumers

| Agent | What they read | Impact of stale data |
|-------|---------------|---------------------|
| Architect | Files Created/Modified, Deviations, Key Decisions | Done check misses gaps or falsely fails |
| Reviewer | All sections, especially Carry-Over Findings | Reviewer misses developer-flagged risks |
| Developer (resume) | All sections to determine which steps are already done | Duplicate work or missed steps on resume |
