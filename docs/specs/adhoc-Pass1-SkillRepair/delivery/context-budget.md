# Context Budget — corrected, API-measured accounting (session 3bde26f8)

**Why this was rewritten:** the first version of this file had two real errors the user caught:
1. It claimed **`thinking` ≈ 114,555 tok**. That was a **phantom** — thinking is stored *signature-only* (encrypted) in the transcript; readable thinking text = **0 chars**. The 114k never existed and broke the whole reconciliation.
2. It blamed a "**135k of per-turn envelopes (cwd/git/timestamps)**". Wrong attribution. cwd/git/timestamps are tiny top-level fields. That bucket is **OMC hook output** (~115k tok) — see below.

**Ground-truth instrument (new):** the API's own `usage` block on every assistant record (`input_tokens + cache_read_input_tokens + cache_creation_input_tokens`). This is what the model actually received — not `chars ÷ 4`. The old file used chars/4, which systematically **undercounts** real tokens for code/markdown/JSON by ~10–30%. All headline numbers below are now API-measured.

---

## Headline (API-measured)

| Moment | Context tokens | Source |
|---|--:|---|
| **Session start baseline** (turn 0) | **45,360** | system prompt + tool schemas + CLAUDE.md + SessionStart + persona, before any conversation |
| **Peak** (pre-compact) | **404,095** | the real number — the "350k" was eyeballed off the status line |
| **Now** (post-`/compact`, still climbing) | **~128,000** | `/compact` cut it to ~97k; the budget investigation itself has since pushed it to ~128k |

The window grows **45k → 404k** steadily: every `Read`/`Edit`/`Bash`/`SendMessage` appends its tool_result **and** the OMC hook output for that call, and all of it stays resident until compaction.

> **Snapshot note.** Only **baseline (45,360)** and **peak (404,095)** are fixed historical points. All record counts and cumulative-tok figures below are a **live snapshot** (transcript = 2,097 records at time of measurement) and keep rising as work continues — treat them as orders of magnitude, not frozen totals.

---

## The fixed harness baseline = ~45k (the user was right)

Turn 0 total = **45,360 tok** with *zero* conversation yet. Decomposed:
- **System prompt + tool schemas ≈ 37k.** The tool schemas dominate — this session loads a very large MCP surface (Atlassian, Canva, Figma, Gmail, Google Calendar, Google Drive, oh-my-claudecode ~80 tools) plus the deferred-tool list. This is the single biggest fixed cost and the one I previously dismissed as "15–25k, second-order."
- **Fixed files ≈ 8–10k:** global CLAUDE.md 1,025t · project CLAUDE.md 1,377t · SessionStart rules dump 4,336t · team-lead persona 3,368t.

**Verdict on the old estimates, corrected:** "harness 60–90k" was too high; "harness 23–35k" was **too low** — the true fixed baseline is **~45k**, dominated by tool schemas. The user's first instinct ("harness — too big, count exactly what you load") was the correct one.

---

## "135k of what?" → it's OMC hook output (~120k tok, 1,083 records)

The mystery bucket is **`attachment`-type records = ~120,000 tok across 1,083 records** (and climbing), and it is **100% hook machinery**, not cwd/git/timestamps:

| attachment kind | ~tok | what it is |
|---|--:|---|
| `hook_success` | **81,552** | the JSON every PreToolUse/PostToolUse/Stop hook returns, on **every** tool call |
| `hook_additional_context` | 13,462 | the "Verify changes work…", "Use parallel execution…", "Code modified…" nags appended to tool results |
| `deferred_tools_delta` | 8,132 | the deferred MCP tool list |
| `skill_listing` | 6,285 | available-skills dump |
| `invoked_skills` | 3,507 | |
| `file` | 3,606 | |
| `output_style` / `mcp_instructions_delta` / `task_reminder` / misc | ~3,400 | |

**The worst repeat offenders** (same string, re-emitted):
- `{"continue":true,"suppressOutput":true}` — **×340** = 31,583 tok
- `{"continue":true,"hookSpecificOutput":{…}}` — **×188** = 25,227 tok
- `{"continue":true,"suppressOutput":true}` (variant) — ×67 = 6,400 tok
- SessionStart hook dumps — ×2 = ~9,400 tok

By hook event: PostToolUse:Edit 12.8k · PostToolUse:Read 9.9k · PostToolUse:Bash 9.2k · Stop 8.0k · PreToolUse:Bash 7.2k · PreToolUse:Edit 6.5k · … Every tool call pays a PreToolUse **and** PostToolUse hook tax, and the output is retained in-window. **This is the most reducible bucket on the board** — quieting OMC hooks (`suppressOutput`, trim `additionalContext`, or `OMC_SKIP_HOOKS`) would cut ~90–115k cumulative.

---

## Full decomposition of the 404k peak (API-anchored)

| Bucket | ~tok | Mine to fix? |
|---|--:|---|
| System prompt + tool schemas (MCP surface) | ~37k | partly — disable unused MCP servers |
| Fixed files (CLAUDE.md ×2, rules dump, persona) | ~9k | trim rules dump |
| **OMC hook attachments** (hook_success + additional_context + listings) | **~115k** | **YES — quiet the hooks** |
| `tool_result` (Read **63k**, Bash 8k, Grep, Edit, SendMessage…) | ~78k | **YES — my full-artifact Reads** |
| `tool_use` (Edit 14k, SendMessage 13k, Write 7k, Bash 3.5k…) | ~43k | **YES — verbose dispatches/edits** |
| `assistant` text (my prose) | ~15k | YES |
| `user` text (your messages) | ~10k | no |
| small meta (file-history 5k, hookInfos 4k, queue-op 2.4k, ai-title 0.7k) | ~13k | no (harness) |
| `thinking` | **~0 stored** | n/a — signature-only, not text |

Sum of identifiable ≈ 320k by chars/4; the gap to the API's 404k is tokenizer density (chars/4 undercounts code/JSON/markdown by 10–30%) + cache-accounting. The buckets reconcile within that margin — no missing 190k this time.

---

## The two leaks ranked, with the fix

1. **OMC hooks — ~115k cumulative.** Fires Pre+Post on every tool call; output retained. *Fix:* `suppressOutput` on the noisy hooks, trim `hook_additional_context`, or `OMC_SKIP_HOOKS` / `DISABLE_OMC` for coordination-only sessions. Biggest single win.
2. **My full-artifact `Read`s — ~63k.** *Fix (lean protocol, in force):* `Grep` verdict lines; read only changed sections; never re-read unchanged artifacts.
3. **My `SendMessage`/`Edit`/`Write` traffic — ~34k.** *Fix:* dispatches point to artifact paths, no inlined findings; terse, infrequent log edits.
4. **Tool schemas — ~37k fixed.** *Fix:* disable MCP servers not needed this session (Canva/Figma/Gmail/Calendar/Drive).

**Lean protocol (still in force):** own only `communication-log.md`; Grep verdicts, never full-read peer artifacts; path-pointer dispatches; terse log edits.

---

## Method (reproducible)

Parsed `~/.claude/projects/session-folder/3bde26f8-….jsonl` (2,097 records at snapshot) with a Python probe: classified every record by `type`, summed `attachment` payloads by `attachment.type` + signature, summed `message.content` blocks by kind, and — crucially — read the API `usage` block per assistant turn for the **true** token curve (45,360 baseline → 404,095 peak → 97k right after `/compact`, ~128k now). Probe scripts ran out-of-context (temp dir) and were removed; they are not pipeline artifacts. *The authoritative live-window number is always Claude Code's `/context` command or the `usage` block — not `chars ÷ 4`.*
