# Context Budget — measured accounting (session 3bde26f8)

**Why this exists:** I gave two contradictory estimates of harness overhead (~60–90k, then ~23–25k). Both were guesses. This is the measured truth, parsed from the session transcript.

**Method:** parsed `~/.claude/projects/session-folder/3bde26f8-…jsonl` (the append-only event log = ground truth of everything that flowed through the session) with `_ctxcount.py` / `_ctxcount2.py`. Token figures are `chars ÷ 4` (rough; code/paths/markdown tables tokenize *denser* than 4 ch/tok, so real tokens for retained prose run ~10–30% higher).

---

## Headline reconciliation: log = 629k, live window = 350k

| | tok (≈) | In the live 350k window? |
|---|--------:|---|
| **Raw transcript log total** | **628,786** | — (append-only log, not the window) |
| − `thinking` blocks | −114,555 | **No** — my reasoning is logged but not resent next turn |
| − transient per-turn attachments + meta (envelopes, system-reminders, cwd/git, queue/mode/title) | ~−130,000 | **Mostly no** — injected per-turn, not retained |
| + system prompt & tool schemas (NOT in the jsonl) | ~+15–25,000 | **Yes** — always in-window |
| **≈ live window** | **~350,000** | ✓ matches what you see |

**So the log being 629k while the window is 350k is expected** — ~115k is ephemeral thinking and ~130k is transient per-turn injection. Neither is the "350k" you're paying for each turn.

---

## What's actually in the LIVE window (the 350k)

Ranked by what I can control:

| Rank | Bucket | Log tok | Live? | Mine to fix? |
|------|--------|--------:|-------|--------------|
| 1 | **`tool_result` from `Read`** | 63,708 | mostly retained | **YES — the leak** |
| 2 | **`tool_use` SendMessage (12.6k) + Edit (11.8k)** + their results | ~37,000 | retained | **YES — verbose dispatches/edits** |
| 3 | Persistent harness: CLAUDE.md inj. (~12–25k live) + tool schemas/system (~15–25k, not in log) + team-lead persona (3.4k) | ~35,000 | retained | partly (trim) |
| 4 | `assistant:text` (my prose) | 13,920 | retained | YES |
| 5 | `Bash` results (5k) + other tool results | ~9,000 | retained | YES |
| 6 | `user:text` (your messages) | 5,389 | retained | no |
| — | `thinking` | 114,555 | **ephemeral** | indirectly (be decisive) |
| — | attachments/meta envelopes | ~142,000 | **mostly transient** | no (harness) |

---

## Measured raw log breakdown (hard data)

### By top-level event type
| chars | tok | type |
|------:|----:|------|
| 985,286 | 246,321 | assistant |
| 871,074 | 217,768 | user |
| 569,469 | 142,367 | attachment |
| 35,772 | 8,943 | system |
| 17,579 | 4,394 | last-prompt |
| 12,562 | 3,140 | queue-operation |
| 6,832 | 1,708 | ai-title |
| 5,957 | 1,489 | file-history-snapshot |
| 5,858 | 1,464 | permission-mode |
| 4,756 | 1,189 | mode |

### By semantic category
| chars | tok | category |
|------:|----:|----------|
| 569,469 | 142,367 | attachment (per-turn envelopes) |
| 458,222 | 114,555 | **assistant: thinking** (ephemeral) |
| 300,794 | 75,198 | **user: tool_result** |
| 142,042 | 35,510 | **assistant: tool_use** |
| 55,680 | 13,920 | assistant: text |
| 35,772 | 8,943 | system events |
| 21,558 | 5,389 | user: text |

### tool_result returned to me — by tool (this is #1 leak)
| chars | tok | tool |
|------:|----:|------|
| 254,833 | 63,708 | **Read** |
| 20,009 | 5,002 | Bash |
| 7,545 | 1,886 | Edit |
| 5,675 | 1,418 | Grep |
| 5,652 | 1,413 | SendMessage |
| 2,909 | 727 | AskUserQuestion |

### tool_use I emitted — by tool (this is #2 leak)
| chars | tok | tool |
|------:|----:|------|
| 50,308 | 12,577 | **SendMessage** (long dispatches) |
| 47,399 | 11,849 | **Edit** (~34 comm-log edits) |
| 13,142 | 3,285 | Bash |
| 9,761 | 2,440 | Agent |
| 8,744 | 2,186 | AskUserQuestion |
| 5,756 | 1,439 | Write |
| 5,527 | 1,381 | Read |

### Top individual blocks (the worst offenders)
Nearly all are **full reads of the delivery folder** + my **thinking**:
- 43.0k Read (review.md re-review) · 42.6k thinking · 33.1k thinking · 30.2k Read (plan.md) · 23.9k Read (questions.md) · 20.0k thinking · 18.9k Read (Codex runtime log) · 16.2k Read (refactor-triage.md) · 15.5k/15.1k/13.5k/13.2k/11.0k more delivery-folder Reads.

### Attachment bucket (142k) sub-breakdown
| chars | tok | tag |
|------:|----:|-----|
| 528,575 | 132,143 | per-turn envelopes (cwd/git/timestamps/system-reminders) across **773 records** (~685 ch avg) |
| 48,141 | 12,035 | CLAUDE.md injection (re-injected ~3× in log; ~1 copy live) |
| 2,573 | 643 | MCP instructions |

### Fixed harness files (measured by `wc`, for reference)
global CLAUDE.md 1,025t · project CLAUDE.md 1,377t · SessionStart rules dump 4,336t · team-lead persona 3,368t = **~10.1k measured**; + tool schemas/system (est, not in log) ~15–25k.

---

## Verdict on my earlier estimates

- **"Harness 60–90k" (1st answer): too high** — I swept transient attachments + ephemeral thinking into "harness."
- **"Harness 23–25k" (2nd answer): right order for the *fixed* files, but understated the *persistent* injected total** (CLAUDE.md re-injection + tool schemas + persona ≈ 25–35k live).
- **The real driver of 350k is NOT the harness.** It's (1) my full-artifact `Read`s (~64k) and (2) my verbose `SendMessage`/`Edit` traffic (~37k) — both 100% my behavior. Harness is a second-order ~25–35k.

---

## Reducible vs fixed

| Item | Now | Achievable | How |
|------|----:|-----------:|-----|
| Read tool_results | ~64k | ~15k | Grep verdict lines / read only changed sections; never re-read unchanged artifacts |
| SendMessage dispatches | ~13k | ~5k | Point to artifact paths; don't inline on-disk findings |
| Edit (comm-log) | ~12k | ~6k | Smaller, less-frequent log edits; fewer full-row rewrites |
| thinking | ~115k log | lower | Be decisive; less re-derivation (not in window, but adds latency/cost) |
| Harness rules dump | 4.3k | ~1.5k | Trim SessionStart to coordination-relevant rules |
| MCP/deferred tools | ~3–5k | ~1k | Disable unused servers (Canva/Figma/Gmail/Calendar/Drive) |

**Lean protocol now in force:** (1) own only `communication-log.md`; (2) `Grep` verdict lines, never full artifacts unless a verdict self-contradicts; (3) dispatches reference artifact paths, no inlined findings; (4) terse log edits.

*Method scripts `_ctxcount.py` / `_ctxcount2.py` were used to generate this and then removed (not pipeline artifacts).*
