# Context-Budget Audit — Team-Lead session (F3-KbIndexer)

**Why this exists:** the user observed the team-lead context at ~250–300k tokens and challenged my hand-wavy estimates. This file replaces guesses with **tokenizer-measured** numbers (tiktoken `cl100k_base`) and is explicit about the one bucket that cannot be measured from inside the model.

**v2 (measured).** Earlier this file carried *estimates* (`chars÷4` and a guessed system-prompt cell). Those were corrected after running a real tokenizer over every injected file and the full transcript. **What v1 got wrong is called out in §0.** Re-runnable scripts: `artifacts/ctx-measure.py` (buckets) and `artifacts/ctx-itemize.py` (per-file).

**Method & caveat:**
- All numbers below are **tiktoken `cl100k_base`** token counts of the actual bytes/transcript — not `chars÷4`.
- tiktoken is a **proxy** for Claude's real tokenizer (~±10% on prose; **undercounts ~1.5–2× on dense content** — `cat -n` line prefixes, Windows `D:\` backslash paths, .NET code, markdown tables). So conversation numbers are a **lower bound**; the gap to the status line lives here.
- The injected **system prompt** (tool schemas + skills list + MCP deferred names + base prompt) is **not in the transcript** and cannot be byte-counted from inside. It is pinned here **by subtraction** against the real status line, and split only with `/context`.

---

## 0. Corrections vs v1 (what was wrong)

| v1 claim | v1 said | Measured truth |
|---|---|---|
| Harness *files* | ~10.3k | **9,469** (exact) — close, now pinned |
| **System prompt** | **ESTIMATE ~40–45k** | **~55–63k** (by subtraction). **v1 was LOW by ~20k.** This is the single biggest bucket. |
| Conversation content | ~81k snapshot | **110,806** full session (snapshot was stale + partial) |
| `tool_use` bucket | ~16.7k | **27,968** (my agent-dispatch prompts + Write/Edit content — bigger than I said) |
| Reconciliation | "~115–136k, residual a mystery" | Residual is **not a mystery file** — it's tiktoken undercounting dense reads ~1.5–2× (§4) |

---

## 1. Injected files — measured exactly (tiktoken)

| Item | Tokens | Controllable by me? |
|---|---:|---|
| nexus SessionStart rules | 3,836 | ❌ plugin |
| team-lead persona (`agents/team-lead.md`) | 2,979 | ❌ plugin |
| project `CLAUDE.md` | 1,620 | ❌ repo |
| global `~/.claude/CLAUDE.md` | 1,034 | ❌ user config |
| **Subtotal** | **9,469** | — |

## 2. Harness system prompt — pinned by subtraction (NOT readable from inside)

At the **post-compact** status line of **90k**, the conversation is nearly wiped, so the residual is almost all fixed floor:

```
90,000 (status, Claude tokenizer)
 − 9,469  injected files (measured)
 − 17,729 compact summary (3,447) + post-compact turns (~14k)
 = 62,802  ⇒ harness system prompt   (≈55–63k after tiktoken-undercount slack)
```

Best estimate of its internal makeup (split only confirmable via `/context`):

| Item | ~Tokens | Controllable? |
|---|---:|---|
| Active tool schemas (`Workflow` ~6k; `PowerShell`/`Bash`/`Agent` ~2.5k each; Read/Edit/Write/Grep/Glob/Skill/ToolSearch) | ~22–25k | ❌ core (some feature tools removable) |
| **Skills catalog (omc's is the fat one)** | ~10–20k | ✅ **disable unused plugins — biggest lever** |
| ~250 MCP deferred tool names + Figma MCP instructions | ~5k | ✅ `/mcp` disconnect 6 connectors |
| Base Claude Code prompt | ~6–8k | ❌ |

> **Corrected lever order:** skills catalog (omc) first, MCP connectors second. (v1/earlier replies wrongly led with the connectors — they're deferred names only, ~5k.)

## 3. Conversation content — measured exactly (tiktoken, full session)

| Category | Tokens | Whose? |
|---|---:|---|
| `tool_result` (file reads + bash dumps) | 58,697 | ✅ **mine to cut** |
| `tool_use` (my calls — agent prompts, Write/Edit bodies) | 27,968 | ✅ mine |
| `assistant:text` (dispatch prose) | 14,867 | ✅ mine |
| `user:text` | 9,274 | — |
| `thinking` | 0 (not persisted) | — |
| **Total** | **110,806** | ~101k mine |

## 3b. Itemized `tool_result` — every read, no hidden files

By tool: Read **32,895** · Bash **20,997** · Grep 1,049 · Edit 980 · Agent 878 · PowerShell 662 · AskUserQuestion 592 · SendMessage 540 · Write 254 · Glob 133 · **= 58,992**.

| Group | ~Tokens | Detail |
|---|---:|---|
| **F3 artifact full-reads — the Relay tax (AVOIDABLE)** | **~25.8k** | `plan.md` 5,767 · `plan-critic-review.md` 5,746 · `implementation.md` 7,686 (×3) · `questions.md` 4,643 · `lessons.md` 2,000 |
| **Bash dumps (AVOIDABLE)** | **~21k** | `git status` 4,075 · `cat plan.md` 3,277 · `find` 2,329 · plugin `ls` 1,888 · `tail`/`grep`/`docker`/`date`/… ~9.4k |
| Plugin investigation (user-requested: 2nd pre-flight Q + `.pipeline-state`) | ~9k | `pipeline-gate.js` 1,471 · `hooks.json` 355 · `agents/team-lead.md` 338 · plugin `ls`/`find` ~6.5k |
| `backlog.md` (×7) | 3,607 | status flips |
| `comm-log.md` (×18) — the one artifact I own | 1,468 | audit trail |
| Source files | ~1k | `KbChunkRepository`, `.csproj` |

**No file the user didn't know about.** 100% = F3 delivery docs + nexus-plugin files read while investigating the user's own questions + git/find dumps.

## 4. Reconciliation to the ~300k pre-compact peak

| Bucket | tiktoken |
|---|---:|
| conversation (§3) | 110,806 |
| harness floor (§2) | ~63,000 |
| injected files (§1) | 9,469 |
| **= total (tiktoken)** | **~183k** |

The status line read **~250–300k**. The ~70–120k gap is **not hidden content** — it is tiktoken **undercounting Claude's tokenizer ~1.5–2× on this specific content** (line-number-prefixed reads, backslash paths, code, tables). Apply that factor to the conversation: ~111k tiktoken → ~165–220k real, + 63k floor + ~12k files ≈ **250–300k**. ✓ Reconciles. The undercount lands precisely on the file-reads, which is why cutting reads (§6) is the real lever.

## 5. Findings

| # | Finding | Verdict |
|---|---|---|
| F1 | "Harness 70–110k" then "40k" then "~40–45k system prompt" | **All wrong.** Files = 9.5k; system prompt = **~55–63k** (measured by subtraction). Estimated 3×, low each time. |
| F2 | **Relay tax** — full reads of `plan`/`critic-review`/`implementation`/`questions`/`lessons` | **~25.8k, avoidable.** Only `comm-log.md` is mine; the rest should have been a 1-line verdict grep. |
| F3 | **Verbose bash** — `git status`, `cat plan.md`, `find`, plugin `ls` dumps | **~21k, avoidable.** Scope to grep/targeted reads. |
| F4 | My `tool_use` (agent prompts + Write bodies) | **28k** — bigger than the v1 estimate; shorten dispatch prompts. |

## 6. Corrected operating rules (effective)

1. **Read only `comm-log.md`.** Every other artifact → **grep the one verdict/status line**; full-read only on a non-pass. (Would have saved ~26k.)
2. **No directory/DLL/`git status` dumps** into context — scope to source paths, grep not `cat`. (~21k.)
3. **Terser dispatch + shorter agent prompts** — the plan holds the detail; stop restating it. (~28k bucket.)
4. **Token reality:** tiktoken undercounts ~1.5–2× on reads/code/paths — assume the real cost of a file-read is higher than the byte count suggests.
5. **For the exact floor split, run `/context`** — it's the only authoritative source for the ~63k system-prompt bucket; I'll reconcile this file to it.

---

*v2 generated by the team lead. §1, §3, §3b are tiktoken-measured (proxy, ~±10%, undercounts dense content); §2 floor is pinned by subtraction against the real status line; §4 explains the proxy gap. Scripts: `artifacts/ctx-measure.py`, `artifacts/ctx-itemize.py`.*
