# Communication Log — adhoc-Pass3-DomainEnrichment

Team mode: **Standard + Codex** · Branch: `main` · Started: 2026-06-06

| # | From | To | Message | Problem / Note |
|---|------|-----|---------|----------------|
| 1 | team-lead | architect | `Analyze adhoc-Pass3-DomainEnrichment.` (Phase 1) | Launch. Scope = VOs, domain services, no-DTO-in-domain (triage #1d/#4/#5/#6; ADR-004/005/006/007). |
| 2 | architect | team-lead | Phase 1 complete; Q1–Q5 raised in questions.md. Pass 3 is ~3–4× Pass 2; recommends 3a/3b/3c split. | Checkpoint. Routed Q1–Q4 to user; Q5 defaulted (skills sufficient). |
| 3 | user | team-lead | Q1=split, **3a only this run**; Q2=full ADR-005 (3c, future); Q3=AppSettings VOs + Identity now (3b, future); Q4=boundary mappers Features/Sync+Xray, Mapster IRegister. | Decisions locked. 3b/3c deferred to own runs. |
| 4 | team-lead | architect | `Write the plan` — scope **Pass 3a only** (ADR-006). Review mode: **critic**. | Phase 2 resume. 3b/3c decisions recorded for future runs. |
| 5 | architect | team-lead | "Done." plan.md written (6 steps, 3a-scoped). No review.md produced. | Plan grepped: 6 steps, ordering binding, skill-mapping present. Dispatching critic (separate review lane). |
| 6 | team-lead | critic | Review plan.md (Pass 3a, ADR-006). | Plan-review gate (critic mode). |
| 7 | critic | team-lead | **APPROVE** — no CRITICAL/HIGH; 1 MEDIUM + 2 LOW (advisory). Validated against review.md. | Plan auto-approved (no open questions). Committing plan. |

## Locked decisions (for future sub-pass runs)

- **3a (THIS run):** ADR-006 — drop `Fokus.Domain→Jira.Contracts`; relocate the 8 `From{Jira,Xray}` factories to boundary mappers in `Fokus.API/Features/Sync` + `Features/Xray`, Mapster `IRegister` with explicit member rules.
- **3b (future run):** ADR-007 — group the ~12 AppSettings scalars into config VOs; encapsulate Identity `Person`/`User`; leave Fokus sync aggregates anemic (honors B3 upsert).
- **3c (future run):** ADR-005 — full migration of all 15 analytics services to `Fokus.Domain/Analytics` calculators returning domain result records; endpoint owns separate response DTO + Mapster map even for 1:1 shapes.
- **Q5:** Pass-1 `domain-service`/`analytics-computation-service` skills are sufficient; no reference re-inspection.
